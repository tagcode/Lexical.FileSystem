// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           3.11.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Internal;
using Lexical.FileSystem.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Simple "http://" based filesystem that can download documents.
    /// </summary>
    public class HttpFileSystem : FileSystemBase, IFileSystemOpen, IFileSystemDelete
    {
        /// <summary>
        /// Token key for http headers. 
        /// 
        /// Token type is IEnumerable{KeyValuePair{string, IEnumerable{string}}}.
        /// "System.Net.Http.Headers.HttpHeaders"
        /// </summary>
        public const string TOKEN_HEADERS = "System.Net.Http.Headers.HttpHeaders";

        /// <summary>
        /// Token key for authentication. 
        /// 
        /// Token type is <see cref="AuthenticationHeaderValue"/>.
        /// </summary>
        public const string TOKEN_AUTHENTICATION = "System.Net.Http.Headers.AuthenticationHeaderValue";

        /// <summary>Default singleton instance.</summary>
        static Lazy<HttpFileSystem> instance = new Lazy<HttpFileSystem>(() => new HttpFileSystem.NonDisposable());
        /// <summary>Default singleton instance.</summary>
        public static HttpFileSystem Instance => instance.Value;

        /// <summary></summary>
        public bool CanOpen => options.CanOpen;
        /// <summary></summary>
        public bool CanRead => options.CanRead;
        /// <summary></summary>
        public bool CanWrite => options.CanWrite;
        /// <summary></summary>
        public bool CanCreateFile => options.CanCreateFile;
        /// <summary></summary>
        public bool CanDelete => options.CanDelete;

        /// <summary>Options</summary>
        protected Options options;
        /// <summary>(optional) token</summary>
        protected IFileSystemToken token;

        /// <summary>options</summary>
        public class Options : IFileSystemOptionOpen, IFileSystemOptionDelete, IFileSystemOptionSubPath
        {
            private static Options instance = new Options();
            /// <summary>Instance with all true</summary>
            public static Options Instance => instance;
            /// <inheritdoc/>
            public bool CanOpen { get; set; } = true;
            /// <inheritdoc/>
            public bool CanRead { get; set; } = true;
            /// <inheritdoc/>
            public bool CanWrite { get; set; } = true;
            /// <inheritdoc/>
            public bool CanDelete { get; set; } = true;
            /// <inheritdoc/>
            public string SubPath { get; set; }
            /// <inheritdoc/>
            public bool CanCreateFile { get; set; } = true;

            /// <summary>Read from <paramref name="option"/></summary>
            /// <param name="option"></param>
            /// <returns>this</returns>
            public static Options Read(IFileSystemOption option)
            {
                Options result = new Options();
                IFileSystemOptionOpen open = option.AsOption<IFileSystemOptionOpen>();
                if (open != null)
                {
                    result.CanCreateFile = open.CanCreateFile;
                    result.CanOpen = open.CanOpen;
                    result.CanRead = open.CanRead;
                    result.CanWrite = open.CanWrite;
                }
                IFileSystemOptionDelete delete = option.AsOption<IFileSystemOptionDelete>();
                if (delete != null)
                {
                    result.CanDelete = delete.CanDelete;
                }
                IFileSystemOptionSubPath subpath = option.AsOption<IFileSystemOptionSubPath>();
                if (subpath != null)
                {
                    result.SubPath = subpath.SubPath;
                }
                return result;
            }

        }

        /// <summary>
        /// Client object.
        /// </summary>
        protected HttpClient httpClient;

        /// <summary>
        /// Create http:// filesystem.
        /// 
        /// HttpFileSystem is intended 
        /// </summary>
        /// <param name="httpClient">(optional) httpClient instance to use, or null to have new created</param>
        /// <param name="option">(optional) options</param>
        public HttpFileSystem(HttpClient httpClient = default, IFileSystemOption option = null) : base()
        {
            this.httpClient = httpClient ?? new HttpClient();
            if (option != null)
            {
                this.options = Options.Read(option);
                this.token = option.AsOption<IFileSystemToken>();
            }
            else this.options = Options.Instance;
        }

        /// <summary>
        /// Non-disposable <see cref="HttpFileSystem"/> disposes and cleans all attached <see cref="IDisposable"/> on dispose, but doesn't go into disposed state.
        /// </summary>
        public class NonDisposable : HttpFileSystem
        {
            /// <summary>Create non-disposable http-filesystem.</summary>
            public NonDisposable() : base() { SetToNonDisposable(); }
        }

        /// <summary>
        /// Reads possible tokens from two sources, this.tokens and from <paramref name="t"/>.
        /// Writes to <paramref name="headers"/>.
        /// 
        /// Searches for following keys:
        /// <list type="bullet">
        ///     <item>"System.Net.Http.Headers.HttpHeaders" as IEnumerable{KeyValuePair{string, IEnumerable{string}}}</item>
        /// </list>
        /// 
        /// Appends "User-Agent" is one is not set.
        /// </summary>
        /// <param name="uri">Uri to use as token query criteria</param>
        /// <param name="t">(optional)</param>
        /// <param name="headers"></param>
        protected virtual void ReadTokenToHeaders(string uri, IFileSystemToken t, HttpHeaders headers)
        {
            // Headers
            IEnumerable<KeyValuePair<string, IEnumerable<string>>>[] headers_array;

            // Read from local token
            if (this.token.TryGetAll(uri, TOKEN_HEADERS, out headers_array))
            {
                foreach (var _headers in headers_array)
                    foreach (KeyValuePair<string, IEnumerable<string>> header in _headers)
                        headers.Add(header.Key, header.Value);
            }
            // Read from parameter token
            if (t.TryGetAll(uri, TOKEN_HEADERS, out headers_array))
            {
                foreach (var _headers in headers_array)
                    foreach (KeyValuePair<string, IEnumerable<string>> header in _headers)
                        headers.Add(header.Key, header.Value);
            }

            // Add User-Agent
            if (!headers.Contains("User-Agent")) headers.Add("User-Agent", "Lexical.FileSystem");
        }

        /// <summary>
        /// Open a file for reading or writing. 
        /// 
        /// To GET a file, the combination of <see cref="FileMode.Open"/> and <see cref="FileAccess.Read"/> issues a GET request on <paramref name="uri"/>.
        /// Returns async stream that may not yet have been fully loaded.
        /// 
        /// To PUT a file, the combination of <see cref="FileMode.Create"/>, <see cref="FileMode.CreateNew"/> or <see cref="FileMode.Truncate"/>, and <see cref="FileAccess.Write"/> issues a request on <paramref name="uri"/>.
        /// Returns a memory stream that can be written to. 
        /// 
        /// <paramref name="fileShare"/> is ignored.
        /// 
        /// Authentication header can be placed in <paramref name="token"/> as instance of <see cref="AuthenticationHeaderValue"/> wrapped in (for example) <see cref="FileSystemToken"/> or <see cref="FileSystemTokenList"/>.
        /// 
        /// Other <see cref="HttpHeaders"/> can also placed in <paramref name="token"/>.
        /// 
        /// <see cref="CancellationToken"/> can be placed in <paramref name="token"/>.
        /// </summary>
        /// <param name="uri">Relative path to file. Directory separator is "/". Root is without preceding "/", e.g. "dir/file.xml"</param>
        /// <param name="fileMode">determines whether to open or to create the file</param>
        /// <param name="fileAccess">how to access the file, read, write or read and write</param>
        /// <param name="fileShare">how the file will be shared by processes</param>
        /// <param name="token">(optional) Credentials</param>
        /// <returns>open file stream</returns>
        /// <exception cref="FileSystemException">On unexpected IO error</exception>
        /// <exception cref="ArgumentNullException"><paramref name="uri"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="uri"/> is an empty string (""), contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support opening files</exception>
        /// <exception cref="FileNotFoundException">The file cannot be found, such as when mode is FileMode.Truncate or FileMode.Open, and and the file specified by path does not exist. The file must already exist in these modes.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="fileMode"/>, <paramref name="fileAccess"/> or <paramref name="fileShare"/> contains an invalid value.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="uri"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        /// <exception cref="FileSystemExceptionNoReadAccess">No read access</exception>
        /// <exception cref="FileSystemExceptionNoWriteAccess">No write access</exception>
        /// <exception cref="OperationCanceledException">operation canceled</exception>
        public Stream Open(string uri, FileMode fileMode, FileAccess fileAccess, FileShare fileShare, IFileSystemToken token = null)
        {
            // Take reference
            var _httpClient = httpClient;
            // Assert not disposed
            if (_httpClient == null || IsDisposing) throw new ObjectDisposedException(nameof(HttpFileSystem));

            // Append subpath
            string _subpath = token.SubPath() ?? this.options.SubPath;
            if (_subpath != null) uri = _subpath + uri;

            // Cancel token
            CancellationToken cancel = default;
            token.TryGet(uri, out cancel);

            try
            {
                // GET
                if (fileMode == FileMode.Open && fileAccess == FileAccess.Read)
                {
                    // Assert allowed
                    if (!options.CanOpen || !options.CanRead) throw new FileSystemExceptionOptionNotSupported(this, uri, options, typeof(IFileSystemOptionOpen));

                    // Request object
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
                    // Read token
                    ReadTokenToHeaders(uri, token, request.Headers);
                    // Read authentication token
                    AuthenticationHeaderValue authenticationHeader;
                    if (this.token.TryGet(uri, out authenticationHeader) || token.TryGet(uri, out authenticationHeader)) request.Headers.Authorization = authenticationHeader;

                    // Start GET
                    Task<HttpResponseMessage> t = _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                    // Wait for headers to complete
                    t.Wait(cancel);
                    // Get result object
                    HttpResponseMessage response = t.Result;
                    // Assert ok
                    if (!response.IsSuccessStatusCode) throw new FileSystemException(this, uri, response.StatusCode.ToString());

                    // Stream Task
                    Task<Stream> tt = response.Content.ReadAsStreamAsync();
                    // Wait for stream
                    tt.Wait(cancel);
                    // return stream
                    return tt.Result;
                }
                // PUT 
                else if ((fileMode == FileMode.Create || fileMode == FileMode.CreateNew || fileMode == FileMode.Truncate) && fileAccess == FileAccess.Write)
                {
                    // Assert allowed
                    if ((!options.CanOpen && !options.CanCreateFile) || !options.CanWrite) throw new FileSystemExceptionOptionNotSupported(this, uri, options, typeof(IFileSystemOptionOpen));
                    // Request
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, uri);
                    // Read token
                    ReadTokenToHeaders(uri, token, request.Headers);

                    // Content
                    FileSystemHttpContent httpContent = new FileSystemHttpContent();
                    // Start PUT
                    Task<HttpResponseMessage> responseTask = _httpClient.PutAsync(uri, httpContent, cancel);
                    responseTask.ContinueWith(t => httpContent.Semaphore.Release());
                    // Wait for http-content to start writing content
                    httpContent.Semaphore.Wait(cancel);
                    // Get write stream
                    Stream writeStream = httpContent.Stream;

                    // Got an error on t
                    if (writeStream == null)
                    {
                        // Wait
                        responseTask.Wait(cancel);
                        // Get result object
                        HttpResponseMessage response = responseTask.Result;
                        // Assert ok
                        if (!response.IsSuccessStatusCode) throw new FileSystemException(this, uri, response.StatusCode.ToString());
                        // Something went unexpectedly wrong
                        throw new FileSystemException(this, uri, "Operation failed, unknown reason.");
                    }

                    // Wrap write stream
                    Stream wrappedStream = new WriteStream(writeStream, httpContent.tcs, responseTask, this, uri);
                    // Return stream for writing
                    return wrappedStream;
                }
                // Combination not supported
                else throw new NotSupportedException($"FileMode={fileMode}, FileAccess={fileAccess}");
            }
            catch (AggregateException e)
            {
                Exception _e = e;
                if (e.InnerExceptions.Count == 1) _e = e.InnerExceptions.First();
                throw new FileSystemException(this, uri, e.Message, e);
            }
            catch (Exception e)
            {
                throw new FileSystemException(this, uri, e.Message, e);
            }
        }

        /// <summary>
        /// Stream that notifies <see cref="FileSystemHttpContent"/> when closed.
        /// </summary>
        protected class WriteStream : StreamDisposeListDecoration
        {
            /// <summary>Task to notify</summary>
            protected TaskCompletionSource<Stream> taskToNotifyOnceClosed;

            /// <summary>Response task</summary>
            protected Task<HttpResponseMessage> responseTask;

            /// <summary>Filesystem</summary>
            protected IFileSystem filesystem;

            /// <summary>Uri</summary>
            protected string uri;

            /// <summary>Create write stream</summary>
            /// <param name="sourceStream"></param>
            /// <param name="taskToNotifyOnceClosed">Task to notify when stream is completed</param>
            /// <param name="responseTask">request task</param>
            /// <param name="filesystem"></param>
            /// <param name="uri"></param>
            public WriteStream(Stream sourceStream, TaskCompletionSource<Stream> taskToNotifyOnceClosed, Task<HttpResponseMessage> responseTask, IFileSystem filesystem, string uri) : base(sourceStream)
            {
                this.taskToNotifyOnceClosed = taskToNotifyOnceClosed;
                this.responseTask = responseTask;
                this.filesystem = filesystem;
                this.uri = uri;
            }

            /// <summary>
            /// Notify <see cref="FileSystemHttpContent"/>.
            /// </summary>
            /// <param name="disposeErrors"></param>
            protected override void InnerDispose(ref StructList4<Exception> disposeErrors)
            {
                // Notify stream closed
                taskToNotifyOnceClosed.SetResult(Source);

                // Forward any errors from RequestTask to the caller of Stream.Close() or Stream.Dispose()
                HttpResponseMessage responseMessage = responseTask.Result;
                if (!responseMessage.IsSuccessStatusCode) throw new FileSystemException(filesystem, uri, responseMessage.StatusCode.ToString());
            }
        }

        /// <summary>
        /// Captures stream and signals on capture.
        /// </summary>
        protected class FileSystemHttpContent : HttpContent
        {
            /// <summary>
            /// Used for signaling completion.
            /// </summary>
            public readonly TaskCompletionSource<Stream> tcs = new TaskCompletionSource<Stream>();

            /// <summary>
            /// Captured stream.
            /// </summary>
            public Stream Stream;

            /// <summary>
            /// Signal semaphore
            /// </summary>
            public SemaphoreSlim Semaphore = new SemaphoreSlim(0, 1);


            /// <summary>
            /// Serialize the HTTP content to a stream as an asynchronous operation.
            /// </summary>
            /// <param name="stream">The target stream.</param>
            /// <param name="context">Information about the transport (channel binding token, for example). This parameter may be null.</param>
            /// <returns>The task object representing the asynchronous operation.</returns>
            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                // Capture Stream
                this.Stream = stream;
                // Signal stream has been received.
                Semaphore.Release();
                // Return task.
                return tcs.Task;
            }

            /// <summary>
            /// Determines whether the HTTP content has a valid length in bytes.
            /// </summary>
            /// <param name="length"></param>
            /// <returns></returns>
            protected override bool TryComputeLength(out long length)
            {
                length = 0L;
                return false;
            }
        }

        /// <summary>
        /// Delete resource at <paramref name="uri"/>.
        /// 
        /// <paramref name="recurse"/> is ignored..
        /// 
        /// Authentication header can be placed in <paramref name="token"/> as instance of <see cref="AuthenticationHeaderValue"/> wrapped in (for example) <see cref="FileSystemToken"/> or <see cref="FileSystemTokenList"/>.
        /// 
        /// Other <see cref="HttpHeaders"/> can also placed in <paramref name="token"/>.
        /// 
        /// <see cref="CancellationToken"/> can be placed in <paramref name="token"/>.
        /// </summary>
        /// <param name="uri">path to a file or directory</param>
        /// <param name="recurse">value is ignored</param>
        /// <param name="token">(optional) filesystem implementation specific token, such as session, security token or credential. Used for authorizing or facilitating the action.</param>
        /// <exception cref="FileNotFoundException">The specified path is invalid.</exception>
        /// <exception cref="IOException">On unexpected IO error, or if <paramref name="uri"/> refered to a directory that wasn't empty and <paramref name="recurse"/> is false, or trying to delete root when not allowed</exception>
        /// <exception cref="ArgumentNullException"><paramref name="uri"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="uri"/> contains invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support deleting files</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="uri"/> refers to non-file device</exception>
        /// <exception cref="ObjectDisposedException"/>
        public void Delete(string uri, bool recurse = false, IFileSystemToken token = null)
        {
            // Take reference
            var _httpClient = httpClient;
            // Assert not disposed
            if (_httpClient == null || IsDisposing) throw new ObjectDisposedException(nameof(HttpFileSystem));
            // Assert allowed
            if (!options.CanDelete) throw new FileSystemExceptionOptionNotSupported(this, uri, options, typeof(IFileSystemOptionOpen));
            // Append subpath
            string _subpath = token.SubPath() ?? this.options.SubPath;
            if (_subpath != null) uri = _subpath + uri;

            // Cancel token
            CancellationToken cancel = default;
            token.TryGet(uri, out cancel);

            try
            {
                // Request object
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, uri);
                // Read token
                ReadTokenToHeaders(uri, token, request.Headers);
                // Read authentication token
                AuthenticationHeaderValue authenticationHeader;
                if (this.token.TryGet(uri, out authenticationHeader) || token.TryGet(uri, out authenticationHeader)) request.Headers.Authorization = authenticationHeader;

                // Start DELETE
                Task<HttpResponseMessage> t = _httpClient.SendAsync(request);
                // Wait for headers to complete
                t.Wait(cancel);
                // Get result object
                HttpResponseMessage response = t.Result;
                // Assert ok
                if (!response.IsSuccessStatusCode) throw new FileSystemException(this, uri, response.ReasonPhrase);
            }
            catch (AggregateException e)
            {
                Exception _e = e;
                if (e.InnerExceptions.Count == 1) _e = e.InnerExceptions.First();
                throw new FileSystemException(this, uri, e.Message, e);
            }
            catch (Exception e) when (e is FileSystemException == false)
            {
                throw new FileSystemException(this, uri, e.Message, e);
            }

        }

        /// <summary>Handle dispose</summary>
        protected override void InnerDispose(ref StructList4<Exception> disposeErrors)
        {
            // Dereference client, GC will dispose it 
            httpClient = null;
        }

        /// <summary>
        /// Adds the attached <see cref="HttpClient"/> to be disposed along with this filesystem.
        /// </summary>
        /// <returns>self</returns>
        public HttpFileSystem AddSourceToBeDisposed()
        {
            AddDisposable(httpClient);
            return this;
        }

        /// <summary>
        /// Invoke <paramref name="disposeAction"/> on the dispose of the object.
        /// 
        /// If parent object is disposed or being disposed, the disposable will be disposed immedialy.
        /// </summary>
        /// <param name="disposeAction"></param>
        /// <param name="state"></param>
        /// <returns>self</returns>
        public HttpFileSystem AddDisposeAction(Action<object> disposeAction, object state)
        {
            ((IDisposeList)this).AddDisposeAction(disposeAction, state);
            return this;
        }

        /// <summary>
        /// Add <paramref name="disposable"/> to list of objects to be disposed along with the system.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns>filesystem</returns>
        public HttpFileSystem AddDisposable(object disposable)
        {
            ((IDisposeList)this).AddDisposable(disposable);
            return this;
        }

        /// <summary>
        /// Add <paramref name="disposables"/> to list of objects to be disposed along with the system.
        /// </summary>
        /// <param name="disposables"></param>
        /// <returns>filesystem</returns>
        public HttpFileSystem AddDisposables(IEnumerable disposables)
        {
            ((IDisposeList)this).AddDisposables(disposables);
            return this;
        }

        /// <summary>
        /// Remove <paramref name="disposable"/> from dispose list.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns></returns>
        public HttpFileSystem RemoveDisposable(object disposable)
        {
            ((IDisposeList)this).RemoveDisposable(disposable);
            return this;
        }

        /// <summary>
        /// Remove <paramref name="disposables"/> from dispose list.
        /// </summary>
        /// <param name="disposables"></param>
        /// <returns></returns>
        public HttpFileSystem RemoveDisposables(IEnumerable disposables)
        {
            ((IDisposeList)this).RemoveDisposables(disposables);
            return this;
        }

        /// <summary>Print info</summary>
        public override string ToString() => "HttpFileSystem";
    }
}
