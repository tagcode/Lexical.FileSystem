// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Internal;
using Lexical.FileSystem.Utility;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Threading.Tasks;

namespace Lexical.FileSystem.FileProvider
{
    /// <summary>
    /// File system that reads, observes and browses files from <see cref="IFileProvider"/> source.
    /// 
    /// The recommended way to create <see cref="FileSystemProvider"/> is to use
    /// the extension method in <see cref="FileProviderExtensions.ToFileSystem(IFileProvider, bool, bool, bool)"/>.
    /// 
    /// WARNING: The Observe implementation browses the subtree of the watched directory path in order to create delta of changes.
    /// </summary>
    public class FileProviderSystem : FileSystemBase, IFileSystemBrowse, IFileSystemObserve, IFileSystemOpen
    {
        static IFileSystemEntry[] NO_ENTRIES = new IFileSystemEntry[0];

        /// <summary>
        /// Source file provider. This value is nulled on dispose.
        /// </summary>
        protected IFileProvider fileProvider;

        /// <summary>
        /// Source file provider. This value is nulled on dispose.
        /// </summary>
        public IFileProvider FileProvider => fileProvider;

        /// <summary>
        /// Source file provider casted to <see cref="IDisposable"/>. Value is null if <see cref="FileProvider"/> doesn't implement <see cref="IDisposable"/>.
        /// </summary>
        public IDisposable FileProviderDisposable => fileProvider as IDisposable;

        bool canObserve;
        /// <inheritdoc/>
        public virtual bool CanBrowse { get; protected set; }
        /// <inheritdoc/>
        public virtual bool CanGetEntry { get; protected set; }
        /// <inheritdoc/>
        public override bool CanObserve => canObserve;
        /// <inheritdoc/>
        public virtual bool CanOpen { get; protected set; }
        /// <inheritdoc/>
        public virtual bool CanRead { get; protected set; }
        /// <inheritdoc/>
        public virtual bool CanWrite => false;
        /// <inheritdoc/>
        public virtual bool CanCreateFile => false;

        /// <summary>
        /// <see cref="FileProvider"/> is physical fileprovider.
        /// </summary>
        protected bool isPhysicalFileProvider;

        /// <summary>
        /// Create file provider based file system.
        /// </summary>
        /// <param name="sourceFileProvider"></param>
        /// <param name="canBrowse">if true allows to forward Browse</param>
        /// <param name="canObserve">if true allows to forward Observe</param>
        /// <param name="canOpen">if true allows to forward Open</param>
        public FileProviderSystem(IFileProvider sourceFileProvider, bool canBrowse = true, bool canObserve = true, bool canOpen = true) : base()
        {
            this.fileProvider = sourceFileProvider ?? throw new ArgumentNullException(nameof(sourceFileProvider));
            this.CanBrowse = this.CanGetEntry = canBrowse;
            this.canObserve = canObserve;
            this.CanOpen = this.CanRead = canOpen;
            this.isPhysicalFileProvider = sourceFileProvider.GetType().FullName == "Microsoft.Extensions.FileProviders.PhysicalFileProvider";
        }

        /// <summary>
        /// Set <paramref name="eventHandler"/> to be used for handling observer events.
        /// 
        /// If <paramref name="eventHandler"/> is null, then events are processed in the running thread.
        /// </summary>
        /// <param name="eventHandler">(optional) factory that handles observer events</param>
        /// <returns>memory filesystem</returns>
        public FileProviderSystem SetEventDispatcher(TaskFactory eventHandler)
        {
            ((IFileSystemObserve)this).SetEventDispatcher(eventHandler);
            return this;
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
            // Assert open is enabled
            if (!CanOpen) throw new NotSupportedException("Open is not supported");
            // Check mode is for opening existing file
            if (fileMode != FileMode.Open) throw new NotSupportedException("FileMode = " + fileMode + " is not supported");
            // Check access is for reading
            if (fileAccess != FileAccess.Read) throw new NotSupportedException("FileAccess = " + fileAccess + " is not supported");
            // Make path
            if (isPhysicalFileProvider && path.Contains(@"\")) path = path.Replace(@"\", "/");
            // Is disposed?
            IFileProvider fp = fileProvider;
            if (fp == null) throw new ObjectDisposedException(nameof(FileProviderSystem));
            // Does file exist?
            IFileInfo fi = fp.GetFileInfo(path);
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
        public IFileSystemEntry[] Browse(string path)
        {
            // Assert browse is enabled
            if (!CanBrowse) throw new NotSupportedException("Browse is not supported");
            // Make path
            if (isPhysicalFileProvider && path.Contains(@"\")) path = path.Replace(@"\", "/");
            // Is disposed?
            IFileProvider fp = fileProvider;
            if (fp == null) throw new ObjectDisposedException(nameof(FileProviderSystem));
            // Browse
            IDirectoryContents contents = fp.GetDirectoryContents(path);
            if (contents.Exists)
            {
                // Convert result
                StructList24<IFileSystemEntry> list = new StructList24<IFileSystemEntry>();
                foreach (IFileInfo _fi in contents)
                {
                    string entryPath = path.Length > 0 ? path + "/" + _fi.Name : _fi.Name;
                    IFileSystemEntry e =
                        _fi.IsDirectory ?
                        (IFileSystemEntry)new FileSystemEntryDirectory(this, entryPath, _fi.Name, _fi.LastModified, DateTimeOffset.MinValue, this) :
                        (IFileSystemEntry)new FileSystemEntryFile(this, entryPath, _fi.Name, _fi.LastModified, DateTimeOffset.MinValue, _fi.Length);
                    list.Add(e);
                }
                return list.ToArray();
            }

            IFileInfo fi = fp.GetFileInfo(path);
            if (fi.Exists)
            {
                IFileSystemEntry e = new FileSystemEntryFile(this, path, fi.Name, fi.LastModified, DateTimeOffset.MinValue, fi.Length);
                return new IFileSystemEntry[] { e };
            }

            throw new DirectoryNotFoundException(path);
        }

        /// <summary>
        /// Get entry of a single file or directory.
        /// </summary>
        /// <param name="path">path to a directory or to a single file, "" is root, separator is "/"</param>
        /// <returns>entry, or null if entry is not found</returns>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support exists</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        public IFileSystemEntry GetEntry(string path)
        {
            if (path == "") return new FileSystemEntryDirectory(this, "", "", DateTimeOffset.MinValue, DateTimeOffset.MinValue, this);
            // Make path
            if (isPhysicalFileProvider && path.Contains(@"\")) path = path.Replace(@"\", "/");
            // Is disposed?
            IFileProvider fp = fileProvider;
            if (fp == null) throw new ObjectDisposedException(nameof(FileProviderSystem));

            // File
            IFileInfo fi = fp.GetFileInfo(path);
            if (fi.Exists)
                return fi.IsDirectory ?
                    new FileSystemEntryDirectory(this, path, fi.Name, fi.LastModified, DateTimeOffset.MinValue, this) :
                    (IFileSystemEntry)new FileSystemEntryFile(this, path, fi.Name, fi.LastModified, DateTimeOffset.MinValue, fi.Length);

            // Directory
            IDirectoryContents contents = fp.GetDirectoryContents(path);
            if (contents.Exists) return new FileSystemEntryDirectory(this, path, Path.GetDirectoryName(path), DateTimeOffset.MinValue, DateTimeOffset.MinValue, this);

            // Nothing was found
            return null;
        }

        /// <summary>
        /// Attach an <paramref name="observer"/> on to a single file or directory. 
        /// Observing a directory will observe the whole subtree.
        /// 
        /// WARNING: The Observe implementation browses the subtree of the watched directory path in order to create delta of changes.
        /// </summary>
        /// <param name="filter">path to file or directory. The directory separator is "/". The root is without preceding slash "", e.g. "dir/dir2"</param>
        /// <param name="observer"></param>
        /// <param name="state">(optional) </param>
        /// <returns>dispose handle</returns>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="filter"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="filter"/> contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support observe</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="filter"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        public override IFileSystemObserver Observe(string filter, IObserver<IFileSystemEvent> observer, object state = null)
        {
            // Assert observe is enabled.
            if (!CanObserve) throw new NotSupportedException("Observe not supported.");
            // Assert not diposed
            IFileProvider fp = fileProvider;
            if (fp == null) return new DummyObserver(this, filter, observer, state);
            // Parse filter
            GlobPatternInfo filterInfo = new GlobPatternInfo(filter);
            // Monitor single file (or dir, we don't know "dir")
            if (!filterInfo.HasWildcards)
            {
                // "dir/" observes nothing
                if (filter.EndsWith("/")) return new DummyObserver(this, filter, observer, state);

                // Create observer that watches one file
                FileObserver handle = new FileObserver(this, filter, observer, state);
                // Send handle
                observer.OnNext(handle);
                // Return handle
                return handle;
            }
            else
            // Has wildcards, e.g. "**/file.txt"
            {
                // Create handle
                PatternObserver handle = new PatternObserver(this, filterInfo, observer, state);
                // Send handle
                observer.OnNext(handle);
                // Return handle
                return handle;
            }
        }

        /// <summary>
        /// Single file observer.
        /// </summary>
        public class FileObserver : FileSystemObserverHandleBase, IFileSystemEventStart
        {
            /// <summary>
            /// Filesystem
            /// </summary>
            protected IFileProvider FileProvider => ((FileProviderSystem)this.FileSystem).FileProvider;

            /// <summary>
            /// Previous state of the file.
            /// </summary>
            protected IFileSystemEntry previousEntry;

            /// <summary>
            /// Change token
            /// </summary>
            protected IChangeToken changeToken;

            /// <summary>
            /// Change token callback handle.
            /// </summary>
            protected IDisposable watcher;

            /// <summary>Time when observing started.</summary>
            protected DateTimeOffset startTime = DateTimeOffset.UtcNow;

            /// <summary>
            /// Print info
            /// </summary>
            /// <returns></returns>
            public override string ToString()
                => FileSystem?.ToString();

            /// <summary>
            /// Create observer for one file.
            /// </summary>
            /// <param name="filesystem"></param>
            /// <param name="path"></param>
            /// <param name="observer"></param>
            /// <param name="state"></param>
            public FileObserver(IFileSystem filesystem, string path, IObserver<IFileSystemEvent> observer, object state)
                : base(filesystem, path, observer, state)
            {
                this.changeToken = FileProvider.Watch(path);
                this.previousEntry = ReadFileEntry();
                this.watcher = changeToken.RegisterChangeCallback(OnEvent, this);
            }

            IFileSystemEntry ReadFileEntry()
            {
                IFileSystemEntry[] entries = FileSystem.Browse(Filter);
                if (entries.Length == 1) return entries[0];
                return null;
            }

            /// <summary>
            /// Forward event
            /// </summary>
            /// <param name="sender"></param>
            void OnEvent(object sender)
            {
                var _observer = Observer;
                if (_observer == null) return;

                // Disposed
                IFileProvider _fileProvider = FileProvider;
                if (_fileProvider == null || _observer == null) return;

                // Event to send
                IFileSystemEvent _event = null;

                // Create new token
                if (!IsDisposing) this.changeToken = FileProvider.Watch(Filter);

                // Figure out change type
                IFileSystemEntry currentEntry = ReadFileEntry();
                bool exists = currentEntry != null;
                bool existed = previousEntry != null;
                DateTimeOffset time = DateTimeOffset.UtcNow;

                if (exists && existed) _event = new FileSystemEventChange(this, time, Filter);
                else if (exists && !existed) _event = new FileSystemEventCreate(this, time, Filter);
                else if (!exists && existed) _event = new FileSystemEventDelete(this, time, Filter);

                // Replace entry
                previousEntry = currentEntry;

                // Send event
                if (_event != null) ((FileSystemBase)this.FileSystem).SendEvent(_event);

                // Start watching again
                if (!IsDisposing) this.watcher = changeToken.RegisterChangeCallback(OnEvent, this);
            }

            /// <summary>
            /// Dispose observer
            /// </summary>
            protected override void InnerDispose(ref StructList4<Exception> errors)
            {
                base.InnerDispose(ref errors);
                var _watcher = watcher;

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
            }

            IFileSystemObserver IFileSystemEvent.Observer => this;
            DateTimeOffset IFileSystemEvent.EventTime => startTime;
            string IFileSystemEvent.Path => null;
        }

        /// <summary>
        /// Observer that monitors a range of files with glob pattern.
        /// 
        /// Since <see cref="IFileProvider"/> doesn't provide information about what was changed,
        /// this observer implementation reads a whole snapshot of the whole file provider, in 
        /// order to determine the changes.
        /// </summary>
        public class PatternObserver : FileSystemObserverHandleBase, IFileSystemEventStart
        {
            /// <summary>
            /// Filesystem
            /// </summary>
            protected IFileProvider FileProvider => ((FileProviderSystem)this.FileSystem).FileProvider;

            /// <summary>
            /// Previous state of file existing.
            /// </summary>
            protected IFileSystemEntry[] previousEntries;

            /// <summary>
            /// Change token
            /// </summary>
            protected IChangeToken changeToken;

            /// <summary>
            /// Change token handle
            /// </summary>
            protected IDisposable watcher;

            /// <summary>
            /// Previous snapshot of detected dirs and files
            /// </summary>
            protected Dictionary<string, IFileSystemEntry> previousSnapshot;

            /// <summary>Time when observing started.</summary>
            protected DateTimeOffset startTime = DateTimeOffset.UtcNow;

            /// <summary>
            /// Create observer for one file.
            /// </summary>
            /// <param name="filesystem"></param>
            /// <param name="filterInfo"></param>
            /// <param name="observer"></param>
            /// <param name="state"></param>
            public PatternObserver(IFileSystem filesystem, GlobPatternInfo filterInfo, IObserver<IFileSystemEvent> observer, object state)
                : base(filesystem, filterInfo.Source, observer, state)
            {
                this.changeToken = FileProvider.Watch(filterInfo.Source);
                this.previousSnapshot = ReadSnapshot();
                this.watcher = changeToken.RegisterChangeCallback(OnEvent, this);
            }

            /// <summary>
            /// Read a snapshot of files and folders that match filter.
            /// </summary>
            /// <returns></returns>
            Dictionary<string, IFileSystemEntry> ReadSnapshot()
            {
                Dictionary<string, IFileSystemEntry> result = new Dictionary<string, IFileSystemEntry>();
                FileScanner scanner = new FileScanner(FileSystem).AddGlobPattern(Filter).SetReturnDirectories(true);

                // Run scan
                foreach (IFileSystemEntry entry in scanner)
                    result[entry.Path] = entry;

                return result;
            }

            /// <summary>
            /// Forward event
            /// </summary>
            /// <param name="sender"></param>
            void OnEvent(object sender)
            {
                var _observer = Observer;
                if (_observer == null) return;

                // Create new token
                if (!IsDisposing) this.changeToken = FileProvider.Watch(Filter);

                // Get new snapshot 
                DateTimeOffset time = DateTimeOffset.UtcNow;
                Dictionary<string, IFileSystemEntry> newSnapshot = ReadSnapshot();

                // List of events
                StructList12<IFileSystemEvent> events = new StructList12<IFileSystemEvent>();

                // Find adds
                foreach (KeyValuePair<string, IFileSystemEntry> newEntry in newSnapshot)
                {
                    string path = newEntry.Key;
                    // Find matching previous entry
                    IFileSystemEntry prevEntry;
                    if (previousSnapshot.TryGetValue(path, out prevEntry))
                    {
                        // Send change event
                        if (!FileSystemEntryComparer.PathDateLengthTypeEqualityComparer.Equals(newEntry.Value, prevEntry))
                            events.Add(new FileSystemEventChange(this, time, path));
                    }
                    // Send create event
                    else events.Add(new FileSystemEventCreate(this, time, path));
                }
                // Find Deletes
                foreach (KeyValuePair<string, IFileSystemEntry> oldEntry in previousSnapshot)
                {
                    string path = oldEntry.Key;
                    if (!newSnapshot.ContainsKey(path)) events.Add(new FileSystemEventDelete(this, time, path));
                }

                // Replace entires
                previousSnapshot = newSnapshot;

                // Dispatch events
                if (events.Count > 0) ((FileSystemBase)this.FileSystem).SendEvents(ref events);

                // Start watching again
                if (!IsDisposing) this.watcher = changeToken.RegisterChangeCallback(OnEvent, this);
            }

            /// <summary>
            /// Dispose observer
            /// </summary>
            protected override void InnerDispose(ref StructList4<Exception> errors)
            {
                base.InnerDispose(ref errors);
                var _watcher = watcher;

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
            }

            IFileSystemObserver IFileSystemEvent.Observer => this;
            DateTimeOffset IFileSystemEvent.EventTime => startTime;
            string IFileSystemEvent.Path => null;
        }

        /// <summary>
        /// Invoke <paramref name="disposeAction"/> on the dispose of the object.
        /// 
        /// If parent object is disposed or being disposed, the disposable will be disposed immedialy.
        /// </summary>
        /// <param name="disposeAction"></param>
        /// <returns>filesystem</returns>
        public FileProviderSystem AddDisposeAction(Action<FileProviderSystem> disposeAction)
        {
            // Argument error
            if (disposeAction == null) throw new ArgumentNullException(nameof(disposeAction));
            // Parent is disposed/ing
            if (IsDisposing) { disposeAction(this); return this; }
            // Adapt to IDisposable
            IDisposable disposable = new DisposeAction<FileProviderSystem>(disposeAction, this);
            // Add to list
            lock (m_disposelist_lock) disposeList.Add(disposable);
            // Check parent again
            if (IsDisposing) { lock (m_disposelist_lock) disposeList.Remove(disposable); disposable.Dispose(); return this; }
            // OK
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
        public FileProviderSystem AddDisposeAction(Action<object> disposeAction, object state)
        {
            ((IDisposeList)this).AddDisposeAction(disposeAction, state);
            return this;
        }

        /// <summary>
        /// Add <paramref name="disposable"/> to list of objects to be disposed along with the system.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns>filesystem</returns>
        public FileProviderSystem AddDisposable(object disposable)
        {
            ((IDisposeList)this).AddDisposable(disposable);
            return this;
        }

        /// <summary>
        /// Add <paramref name="disposables"/> to list of objects to be disposed along with the system.
        /// </summary>
        /// <param name="disposables"></param>
        /// <returns>filesystem</returns>
        public FileProviderSystem AddDisposables(IEnumerable<object> disposables)
        {
            ((IDisposeList)this).AddDisposables(disposables);
            return this;
        }

        /// <summary>
        /// Remove <paramref name="disposable"/> from dispose list.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns>filesystem</returns>
        public FileProviderSystem RemoveDisposable(object disposable)
        {
            ((IDisposeList)this).RemoveDisposable(disposable);
            return this;
        }

        /// <summary>
        /// Remove <paramref name="disposables"/> from dispose list.
        /// </summary>
        /// <param name="disposables"></param>
        /// <returns>filesystem</returns>
        public FileProviderSystem RemoveDisposables(IEnumerable<object> disposables)
        {
            ((IDisposeList)this).RemoveDisposables(disposables);
            return this;
        }

    }
}