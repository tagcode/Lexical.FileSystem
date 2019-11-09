// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           3.11.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Decoration;
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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Simple "http://" based filesystem that can download documents.
    /// </summary>
    public class HttpFileSystem : FileSystemBase, IFileSystemOpen, IFileSystemDelete, IFileSystemBrowse
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
        public virtual bool CanOpen => options.CanOpen;
        /// <summary></summary>
        public virtual bool CanRead => options.CanRead;
        /// <summary></summary>
        public virtual bool CanWrite => options.CanWrite;
        /// <summary></summary>
        public virtual bool CanCreateFile => options.CanCreateFile;
        /// <summary></summary>
        public virtual bool CanDelete => options.CanDelete;
        /// <summary></summary>
        public virtual bool CanBrowse => options.CanBrowse;
        /// <summary></summary>
        public virtual bool CanGetEntry => options.CanGetEntry;

        /// <summary>Options</summary>
        protected Options options;
        /// <summary>(optional) token</summary>
        protected IToken token;

        /// <summary>options</summary>
        public class Options : IOpenOption, IDeleteOption, IBrowseOption, ISubPathOption
        {
            private static Options instance = new Options();
            /// <summary>Instance with all true</summary>
            public static Options Instance => instance;
            /// <inheritdoc/>
            public virtual bool CanOpen { get; set; } = true;
            /// <inheritdoc/>
            public virtual bool CanRead { get; set; } = true;
            /// <inheritdoc/>
            public virtual bool CanWrite { get; set; } = true;
            /// <inheritdoc/>
            public virtual bool CanCreateFile { get; set; } = true;
            /// <inheritdoc/>
            public virtual bool CanDelete { get; set; } = true;
            /// <inheritdoc/>
            public virtual bool CanBrowse { get; set; } = true;
            /// <inheritdoc/>
            public virtual bool CanGetEntry { get; set; } = true;
            /// <inheritdoc/>
            public virtual string SubPath { get; set; }

            /// <summary>Read from <paramref name="option"/></summary>
            /// <param name="option"></param>
            /// <returns>this</returns>
            public virtual Options Read(IOption option)
            {
                IOpenOption open = option.AsOption<IOpenOption>();
                if (open != null)
                {
                    CanCreateFile = open.CanCreateFile;
                    CanOpen = open.CanOpen;
                    CanRead = open.CanRead;
                    CanWrite = open.CanWrite;
                }
                IDeleteOption delete = option.AsOption<IDeleteOption>();
                if (delete != null)
                {
                    CanDelete = delete.CanDelete;
                }
                IBrowseOption browse = option.AsOption<IBrowseOption>();
                if (browse != null)
                {
                    CanBrowse = browse.CanBrowse;
                    CanGetEntry = browse.CanGetEntry;
                }
                ISubPathOption subpath = option.AsOption<ISubPathOption>();
                if (subpath != null)
                {
                    SubPath = subpath.SubPath;
                }
                return this;
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
        public HttpFileSystem(HttpClient httpClient = default, IOption option = null) : base()
        {
            this.httpClient = httpClient ?? new HttpClient();
            if (option != null)
            {
                this.options = CreateOptions().Read(option);
                this.token = option.AsOption<IToken>();
            }
            else this.options = Options.Instance;
        }

        /// <summary>Override this</summary>
        protected virtual Options CreateOptions() => new Options();

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
        protected virtual void ReadTokenToHeaders(string uri, IOption t, HttpHeaders headers)
        {
            // Headers
            IEnumerable<KeyValuePair<string, IEnumerable<string>>>[] headers_array;

            // Read from local token
            if (this.token.TryGetAllTokens(uri, TOKEN_HEADERS, out headers_array))
            {
                foreach (var _headers in headers_array)
                    foreach (KeyValuePair<string, IEnumerable<string>> header in _headers)
                        headers.Add(header.Key, header.Value);
            }
            // Read from parameter token
            if (t.TryGetAllTokens(uri, TOKEN_HEADERS, out headers_array))
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
        /// Authentication header can be placed in <paramref name="option"/> as instance of <see cref="AuthenticationHeaderValue"/> wrapped in (for example) <see cref="Token"/> or <see cref="TokenList"/>.
        /// 
        /// Other <see cref="HttpHeaders"/> can also placed in <paramref name="option"/>.
        /// 
        /// <see cref="CancellationToken"/> can be placed in <paramref name="option"/>.
        /// </summary>
        /// <param name="uri">Relative path to file. Directory separator is "/". Root is without preceding "/", e.g. "dir/file.xml"</param>
        /// <param name="fileMode">determines whether to open or to create the file</param>
        /// <param name="fileAccess">how to access the file, read, write or read and write</param>
        /// <param name="fileShare">how the file will be shared by processes</param>
        /// <param name="option">(optional) Credentials</param>
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
        public Stream Open(string uri, FileMode fileMode, FileAccess fileAccess, FileShare fileShare, IOption option = null)
        {
            // Take reference
            var _httpClient = httpClient;
            // Assert not disposed
            if (_httpClient == null || IsDisposing) throw new ObjectDisposedException(nameof(HttpFileSystem));

            // Append subpath
            string _subpath = option.SubPath() ?? this.options.SubPath;
            if (_subpath != null) uri = _subpath + uri;

            // Cancel token
            CancellationToken cancel = default;
            option.TryGetToken(uri, out cancel);

            try
            {
                // Check canceled
                if (cancel.IsCancellationRequested) throw new OperationCanceledException();
                // GET
                if (fileMode == FileMode.Open && fileAccess == FileAccess.Read)
                {
                    // Assert allowed
                    if (!options.CanOpen || !options.CanRead || !option.CanOpen(true) || !option.CanRead(true)) throw new NotSupportedException(nameof(Open));

                    // Request object
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
                    // Read token
                    ReadTokenToHeaders(uri, option, request.Headers);
                    // Read authentication token
                    AuthenticationHeaderValue authenticationHeader;
                    if (this.token.TryGetToken(uri, out authenticationHeader) || option.TryGetToken(uri, out authenticationHeader)) request.Headers.Authorization = authenticationHeader;

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
                else if ((fileMode == FileMode.Create || fileMode == FileMode.CreateNew || fileMode == FileMode.OpenOrCreate || fileMode == FileMode.Truncate) && fileAccess == FileAccess.Write)
                {
                    // Assert allowed
                    if ((!options.CanOpen && !options.CanCreateFile && !option.CanOpen(true) && !option.CanCreateFile(true)) || !options.CanWrite || !option.CanWrite(true)) throw new NotSupportedException(nameof(Open));
                    // Request
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, uri);
                    // Read token
                    ReadTokenToHeaders(uri, option, request.Headers);

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
                throw new FileSystemException(this, uri, _e.Message, _e);
            }
            catch (Exception e) when (e is FileSystemException == false)
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
        /// Authentication header can be placed in <paramref name="option"/> as instance of <see cref="AuthenticationHeaderValue"/> wrapped in (for example) <see cref="Token"/> or <see cref="TokenList"/>.
        /// 
        /// Other <see cref="HttpHeaders"/> can also placed in <paramref name="option"/>.
        /// 
        /// <see cref="CancellationToken"/> can be placed in <paramref name="option"/>.
        /// </summary>
        /// <param name="uri">path to a file or directory</param>
        /// <param name="recurse">value is ignored</param>
        /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
        /// <exception cref="FileNotFoundException">The specified path is invalid.</exception>
        /// <exception cref="IOException">On unexpected IO error, or if <paramref name="uri"/> refered to a directory that wasn't empty and <paramref name="recurse"/> is false, or trying to delete root when not allowed</exception>
        /// <exception cref="ArgumentNullException"><paramref name="uri"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="uri"/> contains invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support deleting files</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="uri"/> refers to non-file device</exception>
        /// <exception cref="ObjectDisposedException"/>
        public void Delete(string uri, bool recurse = false, IOption option = null)
        {
            // Take reference
            var _httpClient = httpClient;
            // Assert not disposed
            if (_httpClient == null || IsDisposing) throw new ObjectDisposedException(nameof(HttpFileSystem));
            // Assert allowed
            if (!options.CanDelete || !option.CanDelete(true)) throw new NotSupportedException(nameof(Delete));
            // Append subpath
            string _subpath = option.SubPath() ?? this.options.SubPath;
            if (_subpath != null) uri = _subpath + uri;

            // Cancel token
            CancellationToken cancel = default;
            option.TryGetToken(uri, out cancel);

            try
            {
                // Request object
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, uri))
                {
                    // Read token
                    ReadTokenToHeaders(uri, option, request.Headers);
                    // Read authentication token
                    AuthenticationHeaderValue authenticationHeader;
                    if (this.token.TryGetToken(uri, out authenticationHeader) || option.TryGetToken(uri, out authenticationHeader)) request.Headers.Authorization = authenticationHeader;

                    // Start DELETE
                    Task<HttpResponseMessage> t = _httpClient.SendAsync(request);
                    // Wait for headers to complete
                    t.Wait(cancel);
                    // Get result object
                    HttpResponseMessage response = t.Result;
                    // Assert ok
                    if (!response.IsSuccessStatusCode) throw new FileSystemException(this, uri, response.ReasonPhrase);
                }
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

        /// <summary>
        /// Read <paramref name="uri"/> and parse anchors for file references.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="option"></param>
        /// <returns>child links</returns>
        public IEntry[] Browse(string uri, IOption option = null)
        {
            // Take reference
            var _httpClient = httpClient;
            // Assert not disposed
            if (_httpClient == null || IsDisposing) throw new ObjectDisposedException(nameof(HttpFileSystem));
            // Assert allowed
            if (!options.CanBrowse || !option.CanBrowse(true)) throw new NotSupportedException(nameof(Browse));
            // Path converter
            IPathConverter pathConverter;
            // Append subpath
            string _subpath = option.SubPath() ?? this.options.SubPath;
            if (_subpath != null)
            {
                uri = _subpath + uri;
                pathConverter = new PathConverter("", _subpath);
            } else
            {
                pathConverter = new PathConverter("", "");
            }

            // Cancel token
            CancellationToken cancel = default;
            option.TryGetToken(uri, out cancel);

            try
            {
                // Request object
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri))
                {
                    // Read token
                    ReadTokenToHeaders(uri, option, request.Headers);
                    // Read authentication token
                    AuthenticationHeaderValue authenticationHeader;
                    if (this.token.TryGetToken(uri, out authenticationHeader) || option.TryGetToken(uri, out authenticationHeader)) request.Headers.Authorization = authenticationHeader;

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
                    using (var s = tt.Result)
                    {
                        // Wait for stream
                        tt.Wait(cancel);
                        // Parse files
                        IEntry[] entries = ReadEntries(uri, s, pathConverter).ToArray();
                        // Return entries
                        return entries;
                    }
                }
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

        /// <summary>
        /// Tests if document exists without attempting to download it completely.
        /// 
        /// Estimates that <paramref name="uri"/> refers to a directory if path ends with '/'.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public IEntry GetEntry(string uri, IOption option = null)
        {
            // Take reference
            var _httpClient = httpClient;
            // Assert not disposed
            if (_httpClient == null || IsDisposing) throw new ObjectDisposedException(nameof(HttpFileSystem));
            // Assert allowed
            if (!options.CanGetEntry || !option.CanGetEntry(true)) throw new NotSupportedException(nameof(GetEntry));
            // Get subpath
            string _subpath = option.SubPath() ?? this.options.SubPath;
            // Append subpath, if needed
            string uri_ = _subpath != null ? _subpath + uri : uri;

            // Cancel token
            CancellationToken cancel = default;
            option.TryGetToken(uri_, out cancel);

            try
            {
                // Request object
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri_))
                {
                    // Read token
                    ReadTokenToHeaders(uri_, option, request.Headers);
                    // Read authentication token
                    AuthenticationHeaderValue authenticationHeader;
                    if (this.token.TryGetToken(uri_, out authenticationHeader) || option.TryGetToken(uri_, out authenticationHeader)) request.Headers.Authorization = authenticationHeader;

                    // Start GET
                    Task<HttpResponseMessage> t = _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                    // Wait for headers to complete
                    t.Wait(cancel);
                    // Get result object
                    HttpResponseMessage response = t.Result;
                    // Assert ok
                    if (!response.IsSuccessStatusCode) return null;
                    // Length
                    long? length = response.Content.Headers.ContentLength;
                    // Last modified
                    DateTimeOffset? lastModified = response.Content.Headers.LastModified;
                    // Parse uri into parts
                    Uri _uri = new Uri(uri_);
                    // Entry name
                    string name = GetEntryName(_uri.AbsoluteUri);
                    // Is directory
                    if (_uri.AbsolutePath.EndsWith("/")) return new DirectoryEntry(this, uri, name, lastModified ?? DateTimeOffset.MinValue, DateTimeOffset.MinValue, null);
                    // Is file
                    else return new FileEntry(this, uri, name, lastModified ?? DateTimeOffset.MinValue, DateTimeOffset.MinValue, length ?? -1L, null);
                }
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

        /// <summary>
        /// Extracts file or directory name from <paramref name="uri"/>.
        /// 
        /// Examples:
        /// <list type="bullet">
        ///     <item>http://lexical.fi/Dir/                      -> "Dir"</item>
        ///     <item>http://lexical.fi/Dir/file.txt              -> "file.txt"</item>
        ///     <item>http://lexical.fi/Dir/?query=value          -> "Dir"</item>
        ///     <item>http://lexical.fi/Dir/file.txt?query=value  -> "file.txt"</item>
        /// </list>
        /// </summary>
        /// <param name="uri"></param>
        /// <returns>extracted name or null</returns>
        public static string GetEntryName(string uri)
        {
            int endIx = uri.Length-1;
            int queryIx = uri.IndexOf('?');
            if (queryIx > 0) { endIx = queryIx - 1; if (endIx < 0 || endIx>=uri.Length) return null; }
            if (endIx < 0 || endIx >= uri.Length) return null;
            if (uri[endIx] == '/') endIx--;
            if (endIx < 0 || endIx >= uri.Length) return null;
            int slashIx = uri.LastIndexOf('/', endIx);
            if (slashIx >= uri.Length) return null;
            string name = slashIx < 0 ? uri : uri.Substring(slashIx+1, endIx-slashIx);
            return name;
        }

        /// <summary>
        /// Pattern that searches for html anchors
        /// </summary>
        static Regex anchorPattern = new Regex("\\<a\\s*.*href=\"([^\"]*)\".*\\>", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        /// <summary>
        /// Reads through html <paramref name="document"/> and scans for anchor elements with "href" attributes.
        /// Validates local or global links that refer to a subfile or subdirectory.
        /// </summary>
        /// <param name="baseUri">The uri where the document was loaded. Used for creating absolute uris from relative uris.</param>
        /// <param name="document">document data as stream</param>
        /// <param name="pathConverter">convert child urls (the real URIs) back to parent (API caller's) path format</param>
        /// <returns>array of files</returns>
        /// <exception cref="Exception"></exception>
        public virtual IEnumerable<IEntry> ReadEntries(string baseUri, Stream document, IPathConverter pathConverter)
        {
            // Result set
            HashSet<string> yielded = new HashSet<string>();
            // Parse base uri
            Uri _baseUri = new Uri(baseUri);
            // Get absolute uri
            string baseUriAbsolute = _baseUri.GetComponents(UriComponents.Scheme | UriComponents.UserInfo | UriComponents.Host | UriComponents.Port | UriComponents.Path, UriFormat.Unescaped);
            // Create reader
            using (TextReader reader = new StreamReader(document, detectEncodingFromByteOrderMarks: true))
            {
                // Read document as text
                string html = reader.ReadToEnd();
                // Search achors
                foreach(Match m in anchorPattern.Matches(html))
                {
                    // Get href
                    string href = m.Groups[1].Value;
                    // Test not empty
                    if (String.IsNullOrEmpty(href)) continue;
                    // Parse attribute
                    href = HttpUtility.HtmlDecode(href);
                    // Entry
                    IEntry entry = null;
                    try
                    {
                        // Parse uri
                        Uri uri = new Uri(_baseUri, href);
                        // Get absolute uri
                        string absoluteUri = uri.GetComponents(UriComponents.Scheme | UriComponents.UserInfo | UriComponents.Host | UriComponents.Port | UriComponents.Path, UriFormat.Unescaped);
                        // Test that is child of baseUri
                        if (absoluteUri.Length <= baseUriAbsolute.Length || !absoluteUri.StartsWith(baseUriAbsolute)) continue;
                        // Test only one slash after baseUriAbsolute
                        int slashCount = 0;
                        int startIx = baseUriAbsolute.EndsWith("/") ? baseUriAbsolute.Length : baseUriAbsolute.Length + 1;
                        for (int i=startIx; i<absoluteUri.Length-1; i++)
                        {
                            // Not slash
                            if (absoluteUri[i] != '/') continue;
                            // Add slash
                            slashCount++;
                            break;
                        }
                        // Too many slashes "<baseuri>/Dir/Dir/file", thereof not immediate child
                        if (slashCount >= 1) continue;
                        // Already yielded?
                        if (!yielded.Add(absoluteUri)) continue;
                        // Extract name
                        String name = GetEntryName(absoluteUri);
                        // Convert path
                        string parentUri;
                        if (!pathConverter.ChildToParent(absoluteUri, out parentUri)) continue;
                        // Is directory
                        if (absoluteUri.EndsWith("/")) entry = new DirectoryEntry(this, parentUri, name, DateTimeOffset.MinValue, DateTimeOffset.MinValue, null);
                        // Is unknown if is file or directory, so report as both
                        else entry = new DirectoryAndFileEntry(this, parentUri, name, DateTimeOffset.MinValue, DateTimeOffset.MinValue, -1L, null);
                    }
                    catch (Exception)
                    {
                        // bad uri
                        continue;
                    }
                    // Yield entry
                    if (entry != null) yield return entry;
                }
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
