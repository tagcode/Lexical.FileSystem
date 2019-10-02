// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
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
using System.Security;
using System.Threading.Tasks;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Composition of multiple <see cref="IFileSystem"/>s.
    /// </summary>
    public class FileSystemComposition : FileSystemBase, IEnumerable<IFileSystem>, IFileSystemBrowse, IFileSystemObserve, IFileSystemOpen, IFileSystemDelete, IFileSystemMove, IFileSystemCreateDirectory, IFileSystemMount, IFileSystemOptionPath
    {
        /// <summary>Zero entries</summary>
        static IFileSystemEntry[] noEntries = new IFileSystemEntry[0];

        /// <summary>
        /// File system components.
        /// </summary>
        protected IFileSystem[] filesystems;

        /// <summary>
        /// FileSystem specific setups.
        /// </summary>
        protected Component[] components;

        /// <summary>
        /// Count 
        /// </summary>
        public int Count => filesystems.Length;

        /// <summary>
        /// File system components.
        /// </summary>
        public IFileSystem[] FileSystems => filesystems;

        /// <summary>Union of options.</summary>
        protected Options option;

        /// <summary>Is there only one component.</summary>
        protected bool one;

        /// <inheritdoc/>
        public FileSystemCaseSensitivity CaseSensitivity => option.CaseSensitivity;
        /// <inheritdoc/>
        public bool EmptyDirectoryName => option.EmptyDirectoryName;
        /// <inheritdoc/>
        public virtual bool CanBrowse => option.CanBrowse;
        /// <inheritdoc/>
        public virtual bool CanGetEntry => option.CanGetEntry;
        /// <inheritdoc/>
        public override bool CanObserve => option.CanObserve;
        /// <inheritdoc/>
        public virtual bool CanOpen => option.CanOpen;
        /// <inheritdoc/>
        public virtual bool CanRead => option.CanRead;
        /// <inheritdoc/>
        public virtual bool CanWrite => option.CanWrite;
        /// <inheritdoc/>
        public virtual bool CanCreateFile => option.CanCreateFile;
        /// <inheritdoc/>
        public virtual bool CanDelete => option.CanDelete;
        /// <inheritdoc/>
        public virtual bool CanMove => option.CanMove();
        /// <inheritdoc/>
        public virtual bool CanCreateDirectory => option.CanCreateDirectory;
        /// <inheritdoc/>
        public bool CanMount => option.CanMount;
        /// <inheritdoc/>
        public bool CanUnmount => option.CanUnmount;
        /// <inheritdoc/>
        public bool CanListMounts => option.CanListMounts;

        /// <summary>
        /// Root entry
        /// </summary>
        protected IFileSystemEntry rootEntry;

        /// <summary>
        /// Create decorated filesystem.
        /// 
        /// Modifies the permissions of <paramref name="filesystem"/>. 
        /// The effective options will be an intersection of option in <paramref name="filesystem"/> and <paramref name="option"/>.
        /// 
        /// <see cref="IFileSystemOptionMountPath"/> exposes a subpath of <paramref name="filesystem"/>.
        /// <see cref="FileSystemOption.ReadOnly"/> decorates filesystem in readonly mode.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="option">(optional) decoration option</param>
        public FileSystemComposition(IFileSystem filesystem, IFileSystemOption option)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            this.rootEntry = new FileSystemEntryDirectory(this, "", "", now, now, this);
            this.components = new Component[] { new Component(filesystem, option) };
            this.filesystems = new IFileSystem[] { filesystem };
            this.option = Options.Read(this.components[0].Option);
            this.one = true;
        }

        /// <summary>
        /// Create composition of filesystems
        /// </summary>
        /// <param name="filesystems"></param>
        public FileSystemComposition(params IFileSystem[] filesystems)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            this.rootEntry = new FileSystemEntryDirectory(this, "", "", now, now, this);
            this.components = filesystems.Select(fs => new Component(fs, null)).ToArray();
            this.filesystems = filesystems.ToArray();
            this.option = Options.Read(FileSystemOption.Union(this.components.Select(s => s.Option)));
            this.one = filesystems.Length == 1;
        }

        /// <summary>
        /// Create composition of filesystems.
        /// 
        /// Optional FileSystem specific options can be given for each filesystem. 
        /// An intersection of filesystem and option are used, so the option reduces 
        /// the options of the filesystem.
        /// 
        /// <see cref="IFileSystemOptionMountPath"/> option can be used to use subpath of <see cref="IFileSystem"/>.
        /// </summary>
        /// <param name="filesystems"></param>
        public FileSystemComposition(params (IFileSystem filesystem, IFileSystemOption option)[] filesystems)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            this.rootEntry = new FileSystemEntryDirectory(this, "", "", now, now, this);
            this.components = filesystems.Select(p => new Component(p.filesystem, p.option)).ToArray();
            this.filesystems = components.Select(c => c.FileSystem).ToArray();
            this.option = Options.Read(FileSystemOption.Union(this.components.Select(s => s.Option)));
            this.one = filesystems.Length == 1;
        }

        /// <summary>FileSystem specific information</summary>
        protected class Component
        {
            /// <summary>FileSystem component</summary>
            public IFileSystem FileSystem;

            /// <summary>Intersection of option in <see cref="FileSystem"/> and option that was provided in constructor.</summary>
            public Options Option;

            /// <summary>(optional) The option parameter that was provided in construction</summary>
            public IFileSystemOption OptionParameter;

            /// <summary>Tool that converts paths.</summary>
            public PathConversionTool Path;

            /// <summary>Create component info.</summary>
            public Component(IFileSystem filesystem, IFileSystemOption option)
            {
                this.OptionParameter = option;
                this.Option = Options.Read( option == null ? filesystem : FileSystemOption.Intersection(filesystem, option) );
                this.FileSystem = filesystem;
                this.Path = new PathConversionTool("", option.MountPath() ?? "");
            }
        }

        /// <summary>
        /// Set <paramref name="eventHandler"/> to be used for handling observer events.
        /// 
        /// If <paramref name="eventHandler"/> is null, then events are processed in the threads
        /// that make modifications to filesystem.
        /// </summary>
        /// <param name="eventHandler">(optional) factory that handles observer events</param>
        /// <returns>memory filesystem</returns>
        public FileSystemComposition SetEventDispatcher(TaskFactory eventHandler)
        {
            ((IFileSystemObserve)this).SetEventDispatcher(eventHandler);
            return this;
        }

        /// <summary>
        /// Create colletion of file systems
        /// </summary>
        /// <param name="filesystemsEnumrable"></param>
        public FileSystemComposition(IEnumerable<IFileSystem> filesystemsEnumrable) : this(filesystems: filesystemsEnumrable.ToArray())
        {
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
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (IsDisposed) throw new ObjectDisposedException(GetType().FullName);

            try
            {
                /*
                if (one)
                {
                    Component c = components[0];
                    if (!c.Option.CanBrowse) throw new NotSupportedException(nameof(Browse));
                    StringSegment childPath;
                    if (!c.Path.ParentToChild(path, out childPath)) return noEntries;
                    IFileSystemEntry[] childEntries = c.FileSystem.Browse(childPath);
                    // Result array to be filled
                    IFileSystemEntry[] result = new IFileSystemEntry[childEntries.Length];
                    // Is result array filled with null enties
                    int nullCount = 0;
                    // Decorate elements
                    for (int i=0; i<childEntries.Length; i++)
                    {
                        // Get entry
                        IFileSystemEntry e = childEntries[i];
                        // Convert path
                        StringSegment parentPath;
                        if (!c.Path.ChildToParent(e.Path, out parentPath))
                        {
                            // Path conversion failed. Omit entry. Remove it later
                            nullCount++;
                            continue;
                        }
                        // Decorate
                        result[i] = FileSystemEntryDecoration.DecorateFileSystemAndPath(e, this, parentPath);
                    }
                    // Remove null entries
                    if (nullCount>0)
                    {
                        IFileSystemEntry[] newResult = new IFileSystemEntry[result.Length - nullCount];
                        int ix = 0;
                        foreach (var e in result) if (e != null) newResult[ix++] = e;
                        result = newResult;
                    }
                    return result;
                }*/

                StructList24<IFileSystemEntry> entries = new StructList24<IFileSystemEntry>();
                bool exists = false, supported = false;

                IFileSystem[] _filesystems = filesystems;
                if (_filesystems.Length == 0) return noEntries;

                HashSet<string> filenames = _filesystems.Length <= 1 ? null : new HashSet<string>();
                foreach (var filesystem in _filesystems)
                {
                    if (!filesystem.CanBrowse()) continue;
                    try
                    {
                        IFileSystemEntry[] list = filesystem.Browse(path);
                        exists = true; supported = true;
                        foreach (IFileSystemEntry e in list)
                        {
                            // Already exists
                            if (filenames != null && !filenames.Add(e.Name)) continue;
                            // Decorate
                            IFileSystemEntry ee = FileSystemEntryDecoration.DecorateFileSystem(e, this);
                            // Add to result
                            entries.Add(ee);
                        }
                    }
                    catch (DirectoryNotFoundException) { supported = true; }
                    catch (NotSupportedException) { }
                }
                if (!supported) throw new NotSupportedException(nameof(Browse));
                if (!exists) throw new DirectoryNotFoundException(path);
                return entries.ToArray();
            }
            // Update references in the expception and let it fly
            catch (FileSystemException e) when (FileSystemExceptionUtil.Set(e, this, path))
            {
                // Never goes here
                return noEntries;
            }
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
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (IsDisposed) throw new ObjectDisposedException(GetType().FullName);
            if (path == "") return rootEntry;

            bool supported = false;
            foreach (var filesystem in filesystems)
            {
                if (!filesystem.CanGetEntry()) continue;
                try
                {
                    IFileSystemEntry e = filesystem.GetEntry(path);
                    if (e != null) return e;
                    supported = true;
                }
                catch (DirectoryNotFoundException) { supported = true; }
                catch (NotSupportedException) { }
            }
            if (!supported) throw new NotSupportedException(nameof(Browse));
            return null;
        }

        /// <summary>
        /// Open a file for reading and/or writing. File can be created when <paramref name="fileMode"/> is <see cref="FileMode.Create"/> or <see cref="FileMode.CreateNew"/>.
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
        /// <exception cref="ObjectDisposedException"/>
        public Stream Open(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (IsDisposed) throw new ObjectDisposedException(GetType().FullName);

            bool supported = false;
            foreach (var filesystem in filesystems)
            {
                if (!filesystem.CanOpen()) continue;
                try
                {
                    return filesystem.Open(path, fileMode, fileAccess, fileShare);
                }
                catch (FileNotFoundException) { supported = true; }
                catch (NotSupportedException) { }
            }
            if (!supported) throw new NotSupportedException(nameof(Browse));
            throw new FileNotFoundException(path);
        }

        /// <summary>
        /// Delete a file or directory.
        /// 
        /// If <paramref name="recursive"/> is false and <paramref name="path"/> is a directory that is not empty, then <see cref="IOException"/> is thrown.
        /// If <paramref name="recursive"/> is true, then any file or directory within <paramref name="path"/> is deleted as well.
        /// </summary>
        /// <param name="path">path to a file or directory</param>
        /// <param name="recursive">if path refers to directory, recurse into sub directories</param>
        /// <exception cref="FileNotFoundException">The specified path is invalid.</exception>
        /// <exception cref="IOException">On unexpected IO error, or if <paramref name="path"/> refered to a directory that wasn't empty and <paramref name="recursive"/> is false</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is an empty string (""), contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support deleting files</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="path"/> refers to non-file device</exception>
        /// <exception cref="ObjectDisposedException"/>
        public void Delete(string path, bool recursive = false)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (IsDisposed) throw new ObjectDisposedException(GetType().FullName);

            bool supported = false;
            bool ok = false;
            foreach (var filesystem in filesystems)
            {
                if (!filesystem.CanDelete()) continue;
                try
                {
                    filesystem.Delete(path, recursive);
                    ok = true; supported = true;
                }
                catch (FileNotFoundException) { supported = true; }
                catch (NotSupportedException) { }
            }
            if (!supported) throw new NotSupportedException(nameof(Browse));
            if (!ok) throw new FileNotFoundException(path);
        }

        /// <summary>
        /// Try to move/rename a file or directory.
        /// </summary>
        /// <param name="oldPath">old path of a file or directory</param>
        /// <param name="newPath">new path of a file or directory</param>
        /// <exception cref="FileNotFoundException">The specified <paramref name="oldPath"/> is invalid.</exception>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="FileNotFoundException">The specified path is invalid.</exception>
        /// <exception cref="ArgumentNullException">path is null</exception>
        /// <exception cref="ArgumentException">path is an empty string (""), contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support renaming/moving files</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">path refers to non-file device, or an entry already exists at <paramref name="newPath"/></exception>
        /// <exception cref="ObjectDisposedException"/>
        public void Move(string oldPath, string newPath)
        {
            if (oldPath == null) throw new ArgumentNullException(nameof(oldPath));
            if (newPath == null) throw new ArgumentNullException(nameof(newPath));
            if (IsDisposed) throw new ObjectDisposedException(GetType().FullName);

            bool supported = false;
            bool ok = false;
            foreach (IFileSystem filesystem in filesystems)
            {
                if (!filesystem.CanMove()) continue;
                try
                {
                    filesystem.Move(oldPath, newPath);
                    ok = true; supported = true;
                }
                catch (FileNotFoundException) { supported = true; }
                catch (NotSupportedException) { }
            }
            if (!supported) throw new NotSupportedException(nameof(Browse));
            if (!ok) throw new FileNotFoundException(oldPath);
        }

        /// <summary>
        /// Create a directory, or multiple cascading directories.
        /// 
        /// If directory at <paramref name="path"/> already exists, then returns without exception.
        /// </summary>
        /// <param name="path">Relative path to file. Directory separator is "/". The root is without preceding slash "", e.g. "dir/dir2"</param>
        /// <returns>true if directory exists after the method, false if directory doesn't exist</returns>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is an empty string (""), contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support create directory</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        public void CreateDirectory(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (IsDisposed) throw new ObjectDisposedException(GetType().FullName);

            bool supported = false;
            bool ok = false;
            foreach (IFileSystem filesystem in filesystems)
            {
                if (!filesystem.CanCreateDirectory()) continue;
                try
                {
                    filesystem.CreateDirectory(path);
                    ok = true; supported = true;
                }
                catch (FileNotFoundException) { supported = true; }
                catch (NotSupportedException) { }
            }
            if (!supported) throw new NotSupportedException(nameof(Browse));
            if (!ok) throw new FileNotFoundException(path);
        }

        /// <summary>
        /// Attach an <paramref name="observer"/> on to a single file or directory. 
        /// Observing a directory will observe the whole subtree.
        /// </summary>
        /// <param name="path">path to file or directory. The directory separator is "/". The root is without preceding slash "", e.g. "dir/dir2"</param>
        /// <param name="observer"></param>
        /// <param name="state">(optional) </param>
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
        public override IFileSystemObserver Observe(string path, IObserver<IFileSystemEvent> observer, object state = null)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (IsDisposed) throw new ObjectDisposedException(GetType().FullName);

            StructList12<IDisposable> disposables = new StructList12<IDisposable>();
            ObserverAdapter adapter = new ObserverAdapter(this, path, observer, state);
            foreach (var filesystem in filesystems)
            {
                if (!filesystem.CanObserve()) continue;
                try
                {
                    IDisposable disposable = filesystem.Observe(path, adapter);
                    disposables.Add(disposable);
                }
                catch (NotSupportedException) { }
            }
            if (disposables.Count == 0) throw new NotSupportedException(nameof(Observe));
            adapter.disposables = disposables.ToArray();

            // Send IFileSystemEventStart
            observer.OnNext(adapter);

            return adapter;
        }

        class ObserverAdapter : IFileSystemObserver, IObserver<IFileSystemEvent>, IFileSystemEventStart
        {
            public IDisposable[] disposables;
            public IFileSystem FileSystem { get; protected set; }
            public string Filter { get; protected set; }
            public IObserver<IFileSystemEvent> Observer { get; protected set; }
            public object State { get; protected set; }

            /// <summary>Time when observing started.</summary>
            DateTimeOffset startTime = DateTimeOffset.UtcNow;

            public ObserverAdapter(IFileSystem filesystem, string filter, IObserver<IFileSystemEvent> observer, object state)
            {
                this.FileSystem = filesystem;
                this.Filter = filter;
                this.Observer = observer;
                this.State = state;
            }

            public void OnCompleted()
                => Observer.OnCompleted();

            public void OnError(Exception error)
                => Observer.OnError(error);

            public void OnNext(IFileSystemEvent @event)
                => ((FileSystemBase)this.FileSystem).SendEvent(FileSystemEventDecoration.DecorateObserver(@event, this));

            public void Dispose()
            {
                StructList4<Exception> errors = new StructList4<Exception>();
                foreach (IDisposable d in disposables)
                {
                    try
                    {
                        d.Dispose();
                    }
                    catch (AggregateException ae)
                    {
                        foreach (Exception e in ae.InnerExceptions) errors.Add(e);
                    }
                    catch (Exception e)
                    {
                        errors.Add(e);
                    }
                }

                if (errors.Count > 0) throw new AggregateException(errors);
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
        /// <returns>self</returns>
        public FileSystemComposition AddDisposeAction(Action<FileSystemComposition> disposeAction)
        {
            // Argument error
            if (disposeAction == null) throw new ArgumentNullException(nameof(disposeAction));
            // Parent is disposed/ing
            if (IsDisposing) { disposeAction(this); return this; }
            // Adapt to IDisposable
            IDisposable disposable = new DisposeAction<FileSystemComposition>(disposeAction, this);
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
        public FileSystemComposition AddDisposeAction(Action<object> disposeAction, object state)
        {
            ((IDisposeList)this).AddDisposeAction(disposeAction, state);
            return this;
        }

        /// <summary>
        /// Add <paramref name="disposable"/> to list of objects to be disposed along with the system.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns>filesystem</returns>
        public FileSystemComposition AddDisposable(object disposable)
        {
            ((IDisposeList)this).AddDisposable(disposable);
            return this;
        }

        /// <summary>
        /// Add <paramref name="disposables"/> to list of objects to be disposed along with the system.
        /// </summary>
        /// <param name="disposables"></param>
        /// <returns>filesystem</returns>
        public FileSystemComposition AddDisposables(IEnumerable<object> disposables)
        {
            ((IDisposeList)this).AddDisposables(disposables);
            return this;
        }

        /// <summary>
        /// Remove <paramref name="disposable"/> from dispose list.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns></returns>
        public FileSystemComposition RemoveDisposable(object disposable)
        {
            ((IDisposeList)this).RemoveDisposable(disposable);
            return this;
        }

        /// <summary>
        /// Remove <paramref name="disposables"/> from dispose list.
        /// </summary>
        /// <param name="disposables"></param>
        /// <returns></returns>
        public FileSystemComposition RemoveDisposables(IEnumerable<object> disposables)
        {
            ((IDisposeList)this).RemoveDisposables(disposables);
            return this;
        }

        /// <summary>
        /// Get file systems
        /// </summary>
        /// <returns></returns>
        public IEnumerator<IFileSystem> GetEnumerator()
            => ((IEnumerable<IFileSystem>)filesystems).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => filesystems.GetEnumerator();

        /// <summary>
        /// Print info
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => String.Join<IFileSystem>(", ", filesystems);

        /// <summary>
        /// Mount <paramref name="filesystem"/> at <paramref name="path"/> in the parent filesystem.
        /// 
        /// If <paramref name="path"/> is already mounted, then replaces previous mount.
        /// If there is an open stream to previously mounted filesystem, that stream is unlinked from the filesystem.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="filesystem"></param>
        /// <returns>this (parent filesystem)</returns>
        /// <exception cref="NotSupportedException">If operation is not supported</exception>
        public FileSystemComposition Mount(string path, IFileSystem filesystem)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Unmount a filesystem at <paramref name="path"/>.
        /// 
        /// If there is no mount at <paramref name="path"/>, then does nothing.
        /// If there is an open stream to previously mounted filesystem, that stream is unlinked from the filesystem.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>this (parent filesystem)</returns>
        /// <exception cref="NotSupportedException">If operation is not supported</exception>
        public FileSystemComposition Unmount(string path)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// List all mounts.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">If operation is not supported</exception>
        public IFileSystemEntryMount[] ListMounts()
        {
            throw new NotImplementedException();
        }

        IFileSystem IFileSystemMount.Mount(string path, IFileSystem filesystem) => Mount(path, filesystem);
        IFileSystem IFileSystemMount.Unmount(string path) => Unmount(path);

        /// <summary>Flattened options for (slight) performance.</summary>
        protected class Options : IFileSystemOptionBrowse, IFileSystemOptionObserve, IFileSystemOptionOpen, IFileSystemOptionDelete, IFileSystemOptionMove, IFileSystemOptionCreateDirectory, IFileSystemOptionMount, IFileSystemOptionPath
        {
            /// <inheritdoc/>
            public bool CanBrowse { get; set; }
            /// <inheritdoc/>
            public bool CanGetEntry { get; set; }
            /// <inheritdoc/>
            public bool CanObserve { get; set; }
            /// <inheritdoc/>
            public bool CanSetEventDispatcher { get; set; }
            /// <inheritdoc/>
            public bool CanOpen { get; set; }
            /// <inheritdoc/>
            public bool CanRead { get; set; }
            /// <inheritdoc/>
            public bool CanWrite { get; set; }
            /// <inheritdoc/>
            public bool CanCreateFile { get; set; }
            /// <inheritdoc/>
            public bool CanDelete { get; set; }
            /// <inheritdoc/>
            public bool CanMove { get; set; }
            /// <inheritdoc/>
            public bool CanCreateDirectory { get; set; }
            /// <inheritdoc/>
            public bool CanMount { get; set; }
            /// <inheritdoc/>
            public bool CanUnmount { get; set; }
            /// <inheritdoc/>
            public bool CanListMounts { get; set; }
            /// <inheritdoc/>
            public FileSystemCaseSensitivity CaseSensitivity { get; set; }
            /// <inheritdoc/>
            public bool EmptyDirectoryName { get; set; }

            /// <summary>
            /// Read options from <paramref name="ooption"/> and return flattened object.
            /// </summary>
            /// <param name="ooption"></param>
            /// <returns></returns>
            public static Options Read(IFileSystemOption ooption)
            {
                Options result = new Options();
                result.CanBrowse = ooption.CanBrowse();
                result.CanGetEntry = ooption.CanGetEntry();
                result.CanObserve = ooption.CanObserve();
                result.CanSetEventDispatcher = ooption.CanSetEventDispatcher();
                result.CanOpen = ooption.CanOpen();
                result.CanRead = ooption.CanRead();
                result.CanWrite = ooption.CanWrite();
                result.CanCreateFile = ooption.CanCreateFile();
                result.CanDelete = ooption.CanDelete();
                result.CanMount = ooption.CanMount();
                result.CanCreateFile = ooption.CanCreateFile();
                result.CanDelete = ooption.CanDelete();
                result.CanMove = ooption.CanMove();
                result.CanCreateDirectory = ooption.CanCreateDirectory();
                result.CanMount = ooption.CanMount();
                result.CanUnmount = ooption.CanUnmount();
                result.CanListMounts = ooption.CanListMounts();
                return result;
            }
        }
    }

}