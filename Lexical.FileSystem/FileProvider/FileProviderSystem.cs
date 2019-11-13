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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading.Tasks;

namespace Lexical.FileSystem.Decoration
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
        static IEntry[] NO_ENTRIES = new IEntry[0];

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

        /// <inheritdoc/>
        public virtual bool CanBrowse => options.CanBrowse;
        /// <inheritdoc/>
        public virtual bool CanGetEntry => options.CanGetEntry;
        /// <inheritdoc/>
        public virtual bool CanObserve => options.CanObserve;
        /// <inheritdoc/>
        public virtual bool CanOpen => options.CanOpen;
        /// <inheritdoc/>
        public virtual bool CanRead => options.CanRead;
        /// <inheritdoc/>
        public virtual bool CanWrite => false;
        /// <inheritdoc/>
        public virtual bool CanCreateFile => false;

        /// <summary>
        /// <see cref="FileProvider"/> is physical fileprovider.
        /// </summary>
        protected bool isPhysicalFileProvider;

        /// <summary>
        /// Root entry. Constructed only for PhysicalFileProvider. For other, is constructed at runtime.
        /// </summary>
        protected IEntry rootEntry;

        /// <summary>
        /// Options all
        /// </summary>
        protected Options options;

        /// <summary>
        /// Create file provider based file system.
        /// </summary>
        /// <param name="sourceFileProvider"></param>
        /// <param name="option"></param>
        public FileProviderSystem(IFileProvider sourceFileProvider, IOption option = null) : base()
        {
            this.fileProvider = sourceFileProvider ?? throw new ArgumentNullException(nameof(sourceFileProvider));
            this.options = option == null ? Options.AllEnabled : Options.Read(option);
            this.isPhysicalFileProvider = sourceFileProvider.GetType().FullName == "Microsoft.Extensions.FileProviders.PhysicalFileProvider";
            if (this.isPhysicalFileProvider)
            {
                // Get physical path with reflection
                try
                {
                    string physicalPath = sourceFileProvider.GetType().GetProperty("Root").GetValue(sourceFileProvider) as string;
                    this.rootEntry = new DirectoryEntry(this, "", "", DateTimeOffset.MinValue, DateTimeOffset.MinValue, physicalPath);
                }
                catch (Exception) { } // Reflection error
            }
        }

        /// <summary>FileProvider options</summary>
        public class Options : IObserveOption, IOpenOption, IBrowseOption, ITokenEnumerable
        {
            static IToken[] no_tokens = new IToken[0];
            static Options allEnabled = new Options(true, true, true, true, true, true, true);
            /// <summary></summary>
            public static Options AllEnabled => allEnabled;

            /// <summary></summary>
            public bool CanBrowse { get; protected set; } = true;
            /// <summary></summary>
            public bool CanGetEntry { get; protected set; } = true;
            /// <summary></summary>
            public bool CanOpen { get; protected set; } = true;
            /// <summary></summary>
            public bool CanRead { get; protected set; } = true;
            /// <summary></summary>
            public bool CanWrite { get; protected set; } = true;
            /// <summary></summary>
            public bool CanCreateFile { get; protected set; } = true;
            /// <summary></summary>
            public bool CanObserve { get; protected set; } = true;

            /// <summary>Tokens</summary>
            protected IToken[] tokens = no_tokens;


            /// <summary>Create options</summary>
            public Options()
            {
            }

            /// <summary>Create options</summary>
            public Options(bool canBrowse, bool canGetEntry, bool canOpen, bool canRead, bool canWrite, bool canCreateFile, bool canObserve)
            {
                CanBrowse = canBrowse;
                CanGetEntry = canGetEntry;
                CanOpen = canOpen;
                CanRead = canRead;
                CanWrite = canWrite;
                CanCreateFile = canCreateFile;
                CanObserve = canObserve;
            }

            /// <summary>
            /// Read options from <paramref name="option"/> and return flattened object.
            /// </summary>
            /// <param name="option"></param>
            /// <returns></returns>
            public static Options Read(IOption option)
            {
                Options result = new Options();

                IOpenOption open = option.AsOption<IOpenOption>();
                if (open != null)
                {
                    result.CanCreateFile = open.CanCreateFile;
                    result.CanOpen = open.CanOpen;
                    result.CanRead = open.CanRead;
                    result.CanWrite = open.CanWrite;
                }
                IBrowseOption browse = option.AsOption<IBrowseOption>();
                if (browse != null)
                {
                    result.CanBrowse = browse.CanBrowse;
                }
                IObserveOption observe = option.AsOption<IObserveOption>();
                if (observe != null)
                {
                    result.CanObserve = observe.CanObserve;
                }
                IToken token = option.AsOption<IToken>();
                if (token != null)
                {
                    var enumr = option.ListTokens(false);
                    result.tokens = enumr is IToken[] arr ? arr : enumr.ToArray();
                }
                return result;
            }

            /// <summary>Get enumerator</summary>
            public IEnumerator<IToken> GetEnumerator() => ((IEnumerable<IToken>)tokens).GetEnumerator();
            /// <summary>Get enumerator</summary>
            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<IToken>)tokens).GetEnumerator();
        }


        /// <summary>
        /// Open a file for reading. 
        /// </summary>
        /// <param name="path">Relative path to file. Directory separator is "/". Root is without preceding "/", e.g. "dir/file.xml"</param>
        /// <param name="fileMode">determines whether to open or to create the file</param>
        /// <param name="fileAccess">how to access the file, read, write or read and write</param>
        /// <param name="fileShare">how the file will be shared by processes</param>
        /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
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
        public Stream Open(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare, IOption option = null)
        {
            // Assert open is enabled
            if (!CanOpen || !option.CanOpen(true)) throw new NotSupportedException(nameof(Open));
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
        /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
        /// <returns>a snapshot of file and directory entries</returns>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support browse</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        public IDirectoryContent Browse(string path, IOption option = null)
        {
            // Assert supported
            if (!CanBrowse || !option.CanBrowse(true)) throw new NotSupportedException(nameof(Browse));
            // Make path
            if (isPhysicalFileProvider && path.Contains(@"\")) path = path.Replace(@"\", "/");
            // Is disposed?
            IFileProvider fp = fileProvider;
            if (fp == null) throw new ObjectDisposedException(nameof(FileProviderSystem));
            // Browse
            IDirectoryContents contents = fp.GetDirectoryContents(path);
            // Not found
            if (!contents.Exists) return new DirectoryNotFound(this, path);
            // Convert result
            StructList24<IEntry> list = new StructList24<IEntry>();
            foreach (IFileInfo info in contents)
            {
                if (info.IsDirectory)
                {
                    IEntry e = new DirectoryEntry(this, String.IsNullOrEmpty(path) ? info.Name + "/" : (path.EndsWith("/") ?path+info.Name+"/":path+"/"+info.Name+"/"), info.Name, info.LastModified, DateTimeOffset.MinValue, info.PhysicalPath);
                    list.Add(e);
                }
                else
                {
                    IEntry e = new FileEntry(this, String.IsNullOrEmpty(path) ? info.Name : (path.EndsWith("/") ? path + info.Name : path + "/" + info.Name), info.Name, info.LastModified, DateTimeOffset.MinValue, info.Length, info.PhysicalPath);
                    list.Add(e);
                }
            }
            // Return contents
            return new DirectoryContent(this, path, list.ToArray());
        }

        /// <summary>
        /// Get entry of a single file or directory.
        /// </summary>
        /// <param name="path">path to a directory or to a single file, "" is root, separator is "/"</param>
        /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
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
        public IEntry GetEntry(string path, IOption option = null)
        {
            // Assert allowed
            if (!CanGetEntry || !option.CanGetEntry(true)) throw new NotSupportedException(nameof(GetEntry));
            //
            if (path == "" && rootEntry != null) return rootEntry;
            // Make path
            if (isPhysicalFileProvider && path.Contains(@"\")) path = path.Replace(@"\", "/");
            // Is disposed?
            IFileProvider fp = fileProvider;
            if (fp == null) throw new ObjectDisposedException(nameof(FileProviderSystem));

            // File
            IFileInfo fi = fp.GetFileInfo(path);
            if (fi.Exists)
            {
                if (fi.IsDirectory)
                {
                    if (path != "" && !path.EndsWith("/") && !path.EndsWith("\\")) path = path + "/";
                    return new DirectoryEntry(this, path, fi.Name, fi.LastModified, DateTimeOffset.MinValue, fi.PhysicalPath);
                }
                else return new FileEntry(this, path, fi.Name, fi.LastModified, DateTimeOffset.MinValue, fi.Length, fi.PhysicalPath);
            }

            // Directory
            IDirectoryContents contents = fp.GetDirectoryContents(path);
            if (contents.Exists)
            {
                string name;
                if (path == "")
                {
                    name = "";
                    path = "";
                }
                else if (!path.EndsWith("/") && !path.EndsWith("\\"))
                {
                    name = Path.GetFileName(path);
                    path = path + "/";
                }
                else
                {
                    name = Path.GetDirectoryName(path);
                }
                return new DirectoryEntry(this, path, name, DateTimeOffset.MinValue, DateTimeOffset.MinValue, null);
            }

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
        /// <param name="eventDispatcher">(optional) event dispatcher</param>
        /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
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
        public virtual IFileSystemObserver Observe(string filter, IObserver<IEvent> observer, object state = null, IEventDispatcher eventDispatcher = default, IOption option = null)
        {
            // Assert observe is enabled.
            if (!CanObserve || !option.CanObserve(true)) throw new NotSupportedException(nameof(Observe));
            // Assert not diposed
            IFileProvider fp = fileProvider;
            if (fp == null) return new DummyObserver(this, filter, observer, state, eventDispatcher);
            // Parse filter
            GlobPatternInfo patternInfo = new GlobPatternInfo(filter);
            // Monitor single file (or dir, we don't know "dir")
            if (patternInfo.SuffixDepth==0)
            {
                // Create observer that watches one file
                FileObserver handle = new FileObserver(this, filter, observer, state, eventDispatcher, option.OptionIntersection(this.options));
                // Send handle
                observer.OnNext( new StartEvent(handle, DateTimeOffset.UtcNow));
                // Return handle
                return handle;
            }
            else
            // Has wildcards, e.g. "**/file.txt"
            {
                // Create handle
                PatternObserver handle = new PatternObserver(this, patternInfo, observer, state, eventDispatcher);
                // Send handle
                observer.OnNext(new StartEvent(handle, DateTimeOffset.UtcNow));
                // Return handle
                return handle;
            }
        }

        /// <summary>
        /// Single file observer.
        /// </summary>
        public class FileObserver : ObserverBase
        {
            /// <summary>
            /// Filesystem
            /// </summary>
            protected IFileProvider FileProvider => ((FileProviderSystem)this.FileSystem).FileProvider;

            /// <summary>
            /// Previous state of the file.
            /// </summary>
            protected IEntry previousEntry;

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

            /// <summary></summary>
            protected IOption option;

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
            /// <param name="eventDispatcher">(optional)</param>
            /// <param name="option">(optional)</param>
            public FileObserver(IFileSystem filesystem, string path, IObserver<IEvent> observer, object state, IEventDispatcher eventDispatcher, IOption option)
                : base(filesystem, path, observer, state, eventDispatcher)
            {
                this.changeToken = FileProvider.Watch(path);
                this.previousEntry = FileSystem.GetEntry(Filter, option); 
                this.watcher = changeToken.RegisterChangeCallback(OnEvent, this);
                this.option = option;
            }

            /// <summary>
            /// Forward event
            /// </summary>
            /// <param name="sender"></param>
            void OnEvent(object sender)
            {
                // Get observer
                var _observer = Observer;
                // No observer
                if (_observer == null) return;
                // Get dispatcher
                var _dispatcher = Dispatcher ?? EventDispatcher.Instance;

                // Disposed
                IFileProvider _fileProvider = FileProvider;
                if (_fileProvider == null || _observer == null) return;

                // Event to send
                IEvent _event = null;

                // Create new token
                if (!IsDisposing) this.changeToken = FileProvider.Watch(Filter);

                // Figure out change type
                IEntry currentEntry = FileSystem.GetEntry(Filter, option);
                bool exists = currentEntry != null;
                bool existed = previousEntry != null;
                DateTimeOffset time = DateTimeOffset.UtcNow;

                if (exists && existed) _event = new ChangeEvent(this, time, Filter);
                else if (exists && !existed) _event = new CreateEvent(this, time, Filter);
                else if (!exists && existed) _event = new DeleteEvent(this, time, Filter);

                // Replace entry
                previousEntry = currentEntry;

                // Send event
                if (_event != null) _dispatcher.DispatchEvent(_event);

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
        }

        /// <summary>
        /// Observer that monitors a range of files with glob pattern.
        /// 
        /// Since <see cref="IFileProvider"/> doesn't provide information about what was changed,
        /// this observer implementation reads a whole snapshot of the whole file provider, in 
        /// order to determine the changes.
        /// </summary>
        public class PatternObserver : ObserverBase
        {
            /// <summary>
            /// Filesystem
            /// </summary>
            protected IFileProvider FileProvider => ((FileProviderSystem)this.FileSystem).FileProvider;

            /// <summary>
            /// Previous state of file existing.
            /// </summary>
            protected IEntry[] previousEntries;

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
            protected Dictionary<string, IEntry> previousSnapshot;

            /// <summary>Time when observing started.</summary>
            protected DateTimeOffset startTime = DateTimeOffset.UtcNow;

            /// <summary>
            /// Create observer for one file.
            /// </summary>
            /// <param name="filesystem"></param>
            /// <param name="patternInfo"></param>
            /// <param name="observer"></param>
            /// <param name="state"></param>
            /// <param name="eventDispatcher"></param>
            public PatternObserver(IFileSystem filesystem, GlobPatternInfo patternInfo, IObserver<IEvent> observer, object state, IEventDispatcher eventDispatcher = default)
                : base(filesystem, patternInfo.Pattern, observer, state, eventDispatcher)
            {
                this.changeToken = FileProvider.Watch(patternInfo.Pattern);
                this.previousSnapshot = ReadSnapshot();
                this.watcher = changeToken.RegisterChangeCallback(OnEvent, this);
            }

            /// <summary>
            /// Read a snapshot of files and folders that match filter.
            /// </summary>
            /// <returns></returns>
            Dictionary<string, IEntry> ReadSnapshot()
            {
                Dictionary<string, IEntry> result = new Dictionary<string, IEntry>();
                FileScanner scanner = new FileScanner(FileSystem).AddGlobPattern(Filter).SetReturnDirectories(true);

                // Run scan
                foreach (IEntry entry in scanner)
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
                Dictionary<string, IEntry> newSnapshot = ReadSnapshot();

                // List of events
                StructList12<IEvent> events = new StructList12<IEvent>();

                // Find adds
                foreach (KeyValuePair<string, IEntry> newEntry in newSnapshot)
                {
                    string path = newEntry.Key;
                    // Find matching previous entry
                    IEntry prevEntry;
                    if (previousSnapshot.TryGetValue(path, out prevEntry))
                    {
                        // Send change event
                        if (!EntryComparer.PathDateLengthTypeEqualityComparer.Equals(newEntry.Value, prevEntry))
                            events.Add(new ChangeEvent(this, time, path));
                    }
                    // Send create event
                    else events.Add(new CreateEvent(this, time, path));
                }
                // Find Deletes
                foreach (KeyValuePair<string, IEntry> oldEntry in previousSnapshot)
                {
                    string path = oldEntry.Key;
                    if (!newSnapshot.ContainsKey(path)) events.Add(new DeleteEvent(this, time, path));
                }

                // Replace entires
                previousSnapshot = newSnapshot;

                // Dispatch events
                if (events.Count > 0) ((FileSystemBase)this.FileSystem).DispatchEvents(ref events);

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
        }

        /// <summary>
        /// Add the source <see cref="IFileProvider"/> instance to be disposed along with this decoration.
        /// </summary>
        /// <returns>self</returns>
        public FileProviderSystem AddSourceToBeDisposed()
        {
            AddDisposable(this.FileProvider);
            return this;
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
        public FileProviderSystem AddDisposables(IEnumerable disposables)
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
        public FileProviderSystem RemoveDisposables(IEnumerable disposables)
        {
            ((IDisposeList)this).RemoveDisposables(disposables);
            return this;
        }

    }
}