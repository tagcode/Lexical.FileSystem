// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Internal;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System;
using System.IO;
using System.Security;
using System.Threading;

namespace Lexical.FileSystem
{
    /// <summary>
    /// File system that reads, observes and browses files from <see cref="IFileProvider"/> source.
    /// </summary>
    public class FileProviderSystem : FileSystemBase, IFileSystemBrowse, IFileSystemObserve, IFileSystemOpen
    {
        /// <summary>
        /// Optional subpath within the source <see cref="fileProvider"/>.
        /// </summary>
        protected String SubPath;

        /// <summary>
        /// Source file provider. This value is nulled upon dispose.
        /// </summary>
        protected IFileProvider fileProvider;

        /// <summary>
        /// IFileProvider capabilities
        /// </summary>
        protected FileSystemCapabilities capabilities;

        /// <summary>
        /// IFileProvider capabilities
        /// </summary>
        public override FileSystemCapabilities Capabilities => capabilities;

        /// <summary>
        /// Create file provider based file system.
        /// </summary>
        /// <param name="fileProvider"></param>
        /// <param name="subpath">(optional) subpath within the file provider</param>
        /// <param name="capabilities">file provider capabilities</param>
        public FileProviderSystem(IFileProvider fileProvider, string subpath = null, FileSystemCapabilities capabilities = FileSystemCapabilities.Open | FileSystemCapabilities.Read | FileSystemCapabilities.Observe | FileSystemCapabilities.Browse) : base()
        {
            this.fileProvider = fileProvider ?? throw new ArgumentNullException(nameof(subpath));
            this.SubPath = subpath;
            this.capabilities = capabilities;
        }

        /// <summary>
        /// Open a file for reading. 
        /// </summary>
        /// <param name="path">Relative path to file. Directory separator is "/". Root is without preceding "/", e.g. "dir/file.xml"</param>
        /// <param name="fileMode">determines whether to open or to create the file</param>
        /// <param name="fileAccess">how to access the file, read, write or read and write</param>
        /// <param name="fileShare">how the file will be shared by processes</param>
        /// <returns>open file stream</returns>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is an empty string (""), contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support opening files</exception>
        /// <exception cref="FileNotFoundException">The file cannot be found, such as when mode is FileMode.Truncate or FileMode.Open, and and the file specified by path does not exist. The file must already exist in these modes.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="fileMode"/>, <paramref name="fileAccess"/> or <paramref name="fileShare"/> contains an invalid value.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        public Stream Open(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
        {
            // Check mode is for opening existing file
            if (fileMode != FileMode.Open) throw new NotSupportedException("FileMode = " + fileMode + " is not supported");
            // Check access is for reading
            if (fileAccess != FileAccess.Read) throw new NotSupportedException("FileAccess = " + fileAccess + " is not supported");
            // Make path
            string concatenatedPath = SubPath == null ? path : (SubPath.EndsWith("/") || SubPath.EndsWith("\\")) ? SubPath + path : SubPath + "/" + path;
            // Is disposed?
            IFileProvider fp = fileProvider;
            if (fp == null) throw new ObjectDisposedException(nameof(FileProviderSystem));
            // Does file exist?
            IFileInfo fi = fp.GetFileInfo(concatenatedPath);
            if (!fi.Exists) throw new FileNotFoundException(path);
            // Read
            Stream s = fi.CreateReadStream();
            if (s == null) throw new FileNotFoundException(path);
            // Ok
            return s;
        }

        /// <summary>
        /// Browse a directory for file and subdirectory entries.
        /// </summary>
        /// <param name="path">path to directory, "" is root, separator is "/"</param>
        /// <returns>a snapshot of file and directory entries</returns>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support browse</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        public FileSystemEntry[] Browse(string path)
        {
            // Make path
            string concatenatedPath = SubPath == null ? path : (SubPath.EndsWith("/") || SubPath.EndsWith("\\")) ? SubPath + path : SubPath + "/" + path;
            // Is disposed?
            IFileProvider fp = fileProvider;
            if (fp == null) throw new ObjectDisposedException(nameof(FileProviderSystem));
            // Browse
            IDirectoryContents contents = fp.GetDirectoryContents(concatenatedPath);
            if (contents.Exists)
            {
                // Convert result
                StructList24<FileSystemEntry> list = new StructList24<FileSystemEntry>();
                foreach (IFileInfo _fi in contents)
                {
                    list.Add(new FileSystemEntry { FileSystem = this, LastModified = _fi.LastModified, Name = _fi.Name, Path = concatenatedPath.Length > 0 ? concatenatedPath + "/" + _fi.Name : _fi.Name, Length = _fi.IsDirectory ? -1L : _fi.Length, Type = _fi.IsDirectory ? FileSystemEntryType.Directory : FileSystemEntryType.File });
                }
                return list.ToArray();
            }

            IFileInfo fi = fp.GetFileInfo(concatenatedPath);
            if (fi.Exists)
            {
                FileSystemEntry e = new FileSystemEntry { FileSystem = this, LastModified = fi.LastModified, Name = fi.Name, Path = concatenatedPath, Length = fi.IsDirectory ? -1L : fi.Length, Type = fi.IsDirectory ? FileSystemEntryType.Directory : FileSystemEntryType.File };
                return new FileSystemEntry[] { e };
            }

            throw new DirectoryNotFoundException(path);
        }

        /// <summary>
        /// Attach an <paramref name="observer"/> on to a single file or directory. 
        /// Observing a directory will observe the whole subtree.
        /// </summary>
        /// <param name="path">path to file or directory. The directory separator is "/". The root is without preceding slash "", e.g. "dir/dir2"</param>
        /// <param name="observer"></param>
        /// <returns>dispose handle</returns>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support observe</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        public IDisposable Observe(string path, IObserver<FileSystemEntryEvent> observer)
        {
            // Make path
            string concatenatedPath = SubPath == null ? path : (SubPath.EndsWith("/") || SubPath.EndsWith("\\")) ? SubPath + path : SubPath + "/" + path;
            // Is disposed?
            IFileProvider fp = fileProvider;
            if (fp == null) throw new ObjectDisposedException(nameof(FileProviderSystem));
            // Observe
            return new Watcher(this, fp, observer, concatenatedPath, path);
        }

        /// <summary>
        /// File watcher.
        /// </summary>
        public class Watcher : IDisposable
        {
            /// <summary>
            /// Path to supply to <see cref="fileProvider"/>.
            /// </summary>
            public readonly string FileProviderPath;

            /// <summary>
            /// Relative path. Path is relative to the <see cref="fileSystem"/>'s root.
            /// </summary>
            public readonly string RelativePath;

            /// <summary>
            /// Associated observer
            /// </summary>
            protected IObserver<FileSystemEntryEvent> observer;

            /// <summary>
            /// The parent file system.
            /// </summary>
            protected IFileSystem fileSystem;

            /// <summary>
            /// File provider
            /// </summary>
            protected IFileProvider fileProvider;

            /// <summary>
            /// Watcher class
            /// </summary>
            protected IDisposable watcher;

            /// <summary>
            /// Previous state of file existing.
            /// </summary>
            protected int existed;

            /// <summary>
            /// Change token
            /// </summary>
            protected IChangeToken changeToken;

            /// <summary>
            /// Print info
            /// </summary>
            /// <returns></returns>
            public override string ToString()
                => fileProvider?.ToString() ?? "disposed";

            /// <summary>
            /// Create observer for one file.
            /// </summary>
            /// <param name="system"></param>
            /// <param name="fileProvider"></param>
            /// <param name="observer"></param>
            /// <param name="fileProviderPath">Absolute path</param>
            /// <param name="relativePath">Relative path (separator is '/')</param>
            public Watcher(IFileSystem system, IFileProvider fileProvider, IObserver<FileSystemEntryEvent> observer, string fileProviderPath, string relativePath)
            {
                this.fileSystem = system ?? throw new ArgumentNullException(nameof(system));
                this.observer = observer ?? throw new ArgumentNullException(nameof(observer));
                this.fileProvider = fileProvider ?? throw new ArgumentNullException(nameof(fileProvider));
                this.FileProviderPath = fileProviderPath ?? throw new ArgumentNullException(nameof(fileProviderPath));
                this.RelativePath = relativePath ?? throw new ArgumentNullException(nameof(relativePath));
                this.changeToken = fileProvider.Watch(FileProviderPath);
                this.watcher = changeToken.RegisterChangeCallback(OnEvent, this);
                this.existed = fileProvider.GetFileInfo(FileProviderPath).Exists ? 1 : 0;
            }

            /// <summary>
            /// Forward event
            /// </summary>
            /// <param name="sender"></param>
            void OnEvent(object sender)
            {
                var _observer = observer;
                if (_observer == null) return;

                // Disposed
                IFileProvider _fileProvider = fileProvider;
                IFileSystem _fileSystem = fileSystem;
                if (_fileProvider == null || _fileSystem == null) return;

                // Figure out change type
                bool exists = _fileProvider.GetFileInfo(FileProviderPath).Exists;
                bool _existed = Interlocked.CompareExchange(ref existed, exists ? 1 : 0, existed) == 1;

                WatcherChangeTypes eventType = default;
                if (_existed)
                {
                    eventType = exists ? WatcherChangeTypes.Changed : WatcherChangeTypes.Deleted;
                }
                else
                {
                    eventType = exists ? WatcherChangeTypes.Created : WatcherChangeTypes.Deleted;
                }

                FileSystemEntryEvent ae = new FileSystemEntryEvent { FileSystem = _fileSystem, ChangeEvents = eventType, Path = RelativePath };
                observer.OnNext(ae);
            }

            /// <summary>
            /// Dispose observer
            /// </summary>
            public void Dispose()
            {
                var _watcher = watcher;
                var _observer = observer;

                // Clear file system reference, and remove watcher from dispose list.
                IFileSystem _fileSystem = Interlocked.Exchange(ref fileSystem, null);
                if (_fileSystem is FileSystemBase __fileSystem) __fileSystem.RemoveDisposableBase(this);

                StructList2<Exception> errors = new StructList2<Exception>();
                if (_observer != null)
                {
                    observer = null;
                    try
                    {
                        _observer.OnCompleted();
                    }
                    catch (Exception e)
                    {
                        errors.Add(e);
                    }
                }

                if (_watcher != null)
                {
                    watcher = null;
                    try
                    {
                        _watcher.Dispose();
                    }
                    catch (Exception e)
                    {
                        errors.Add(e);
                    }
                }

                if (errors.Count > 0) throw new AggregateException(errors);
                fileProvider = null;
            }
        }

        /// <summary>
        /// Add <paramref name="disposable"/> to list of objects to be disposed along with the system.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns>filesystem</returns>
        public FileProviderSystem AddDisposable(object disposable) => AddDisposableBase(disposable) as FileProviderSystem;

        /// <summary>
        /// Remove disposable from dispose list.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns></returns>
        public FileProviderSystem RemoveDisposable(object disposable) => RemoveDisposableBase(disposable) as FileProviderSystem;

    }
}