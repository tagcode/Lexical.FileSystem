// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Composition of multiple <see cref="IFileSystem"/>s.
    /// </summary>
    public class FileSystemComposition : FileSystemBase, IEnumerable<IFileSystem>, IFileSystemBrowse, IFileSystemObserve, IFileSystemOpen, IFileSystemDelete, IFileSystemMove, IFileSystemCreateDirectory, IFileSystemReference
    {
        /// <summary>
        /// File system components.
        /// </summary>
        protected IFileSystem[] fileSystems;

        /// <summary>
        /// Count 
        /// </summary>
        public int Count => fileSystems.Length;

        /// <summary>
        /// Union of capabilities
        /// </summary>
        protected FileSystemFeatures features;

        /// <summary>
        /// Union of capabilities.
        /// </summary>
        public override FileSystemFeatures Features => features;

        /// <summary>
        /// File system components.
        /// </summary>
        public IFileSystem[] FileSystems => fileSystems;

        /// <inheritdoc/>
        public virtual bool CanBrowse { get; protected set; }
        /// <inheritdoc/>
        public virtual bool CanGetEntry { get; protected set; }
        /// <inheritdoc/>
        public virtual bool CanObserve { get; protected set; }
        /// <inheritdoc/>
        public virtual bool CanOpen { get; protected set; }
        /// <inheritdoc/>
        public virtual bool CanRead { get; protected set; }
        /// <inheritdoc/>
        public virtual bool CanWrite { get; protected set; }
        /// <inheritdoc/>
        public virtual bool CanCreateFile { get; protected set; }
        /// <inheritdoc/>
        public virtual bool CanDelete { get; protected set; }
        /// <inheritdoc/>
        public virtual bool CanMove { get; protected set; }
        /// <inheritdoc/>
        public virtual bool CanCreateDirectory { get; protected set; }
        /// <inheritdoc/>
        public virtual bool CanReference { get; protected set; }
        /// <inheritdoc/>
        public string Reference => throw new NotSupportedException();

        /// <summary>
        /// Create composition of file systems
        /// </summary>
        /// <param name="fileSystems"></param>
        public FileSystemComposition(params IFileSystem[] fileSystems)
        {
            this.fileSystems = fileSystems;
            CanReference = true;
            foreach (IFileSystem fs in fileSystems)
            {
                features |= fs.Features;
                CanBrowse |= fs.CanBrowse();
                CanGetEntry |= fs.CanGetEntry();
                CanObserve |= fs.CanObserve();
                CanOpen |= fs.CanOpen();
                CanRead |= fs.CanRead();
                CanWrite |= fs.CanWrite();
                CanCreateFile |= fs.CanCreateFile();
                CanDelete |= fs.CanDelete();
                CanMove |= fs.CanMove();
                CanCreateDirectory |= fs.CanCreateDirectory();
                CanReference &= fs.CanReference();
            }
            CanReference = false; // Not implemented
        }

        /// <summary>
        /// Create colletion of file systems
        /// </summary>
        /// <param name="fileSystems"></param>
        public FileSystemComposition(IEnumerable<IFileSystem> fileSystems)
        {
            this.fileSystems = fileSystems.ToArray();
            foreach (IFileSystem fs in this.fileSystems) features |= fs.Features;
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
            StructList24<IFileSystemEntry> entries = new StructList24<IFileSystemEntry>();
            bool exists = false, supported = false;
            foreach (var filesystem in fileSystems)
            {
                if (!filesystem.CanBrowse()) continue;
                try
                {
                    IFileSystemEntry[] list = filesystem.Browse(path);
                    exists = true; supported = true;
                    foreach (IFileSystemEntry e in list)
                    {
                        entries.Add(new DecoratedEntry(this, e));
                    }
                }
                catch (DirectoryNotFoundException) { supported = true; }
                catch (NotSupportedException) { }
            }
            if (!supported) throw new NotSupportedException(nameof(Browse));
            if (!exists) throw new DirectoryNotFoundException(path);
            return entries.ToArray();
        }

        class DecoratedEntry : IFileSystemEntryFile, IFileSystemEntryDirectory, IFileSystemEntryDrive
        {
            public IFileSystemEntry Source { get; protected set; }
            public IFileSystem FileSystem { get; protected set; }
            public string Path => Source.Path;
            public string Name => Source.Name;
            public DateTimeOffset LastModified => Source.LastModified;
            public bool IsFile => Source.IsFile();
            public long Length => Source.Length();
            public bool IsDrive => Source.IsDrive();
            public bool IsDirectory => Source.IsDirectory();
            public DecoratedEntry(IFileSystem fileSystem, IFileSystemEntry source)
            {
                Source = source;
                FileSystem = fileSystem;
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
            bool supported = false;
            foreach (var filesystem in fileSystems)
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
            bool supported = false;
            foreach (var filesystem in fileSystems)
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
            bool supported = false;
            bool ok = false;
            foreach (var filesystem in fileSystems)
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
            bool supported = false;
            bool ok = false;
            foreach (IFileSystem filesystem in fileSystems)
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
            bool supported = false;
            bool ok = false;
            foreach (IFileSystem filesystem in fileSystems)
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
        public IFileSystemObserveHandle Observe(string path, IObserver<IFileSystemEvent> observer, object state = null)
        {
            StructList12<IDisposable> disposables = new StructList12<IDisposable>();
            ObserverAdapter adapter = new ObserverAdapter(this, path, observer, state);
            foreach (var filesystem in fileSystems)
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
            return adapter;
        }

        class ObserverAdapter : IFileSystemObserveHandle, IObserver<IFileSystemEvent>
        {
            public IDisposable[] disposables;
            public IFileSystem FileSystem { get; protected set; }
            public string Filter { get; protected set; }
            public IObserver<IFileSystemEvent> Observer { get; protected set; }
            public object State { get; protected set; }

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
                => Observer.OnNext(FileSystemComposition.AdaptEvent(@event, this));

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
        }

        /// <summary>
        /// Convert <paramref name="e"/> to implement <see cref="IFileSystemCompositionEvent"/> when possible.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="observer">overriding observer</param>
        /// <returns></returns>
        static IFileSystemEvent AdaptEvent(IFileSystemEvent e, IFileSystemObserveHandle observer)
        {
            switch(e)
            {
                case FileSystemCreateEvent ce: return new CompositionCreateEvent(observer, ce.Observer.FileSystem, ce.EventTime, ce.Path);
                case FileSystemDeleteEvent de: return new CompositionDeleteEvent(observer, de.Observer.FileSystem, de.EventTime, de.Path);
                case FileSystemChangeEvent ce: return new CompositionChangeEvent(observer, ce.Observer.FileSystem, ce.EventTime, ce.Path);
                case FileSystemRenameEvent re: return new CompositionRenameEvent(observer, re.Observer.FileSystem, re.EventTime, re.OldPath, re.NewPath);
                case FileSystemErrorEvent ee: return new CompositionErrorEvent(observer, ee.Observer.FileSystem, ee.EventTime, ee.Error);
                default: return e;
            }
        }

        class CompositionCreateEvent : FileSystemCreateEvent, IFileSystemCompositionEvent
        {
            /// <summary>
            /// Sending file-system.
            /// </summary>
            public IFileSystem OriginalFileSystem { get; protected set; }

            /// <inheritdoc/>
            public CompositionCreateEvent(IFileSystemObserveHandle observer, IFileSystem originalFileSystem, DateTimeOffset eventTime, string path) : base(observer, eventTime, path) { OriginalFileSystem = originalFileSystem; }
        }

        class CompositionDeleteEvent : FileSystemDeleteEvent, IFileSystemCompositionEvent
        {
            /// <summary>
            /// Sending file-system.
            /// </summary>
            public IFileSystem OriginalFileSystem { get; protected set; }

            /// <inheritdoc/>
            public CompositionDeleteEvent(IFileSystemObserveHandle observer, IFileSystem originalFileSystem, DateTimeOffset eventTime, string path) : base(observer, eventTime, path) { OriginalFileSystem = originalFileSystem; }
        }

        class CompositionChangeEvent : FileSystemChangeEvent, IFileSystemCompositionEvent
        {
            /// <summary>
            /// Sending file-system.
            /// </summary>
            public IFileSystem OriginalFileSystem { get; protected set; }

            /// <inheritdoc/>
            public CompositionChangeEvent(IFileSystemObserveHandle observer, IFileSystem originalFileSystem, DateTimeOffset eventTime, string path) : base(observer, eventTime, path) { OriginalFileSystem = originalFileSystem; }
        }

        class CompositionRenameEvent : FileSystemRenameEvent, IFileSystemCompositionEvent
        {
            /// <summary>
            /// Sending file-system.
            /// </summary>
            public IFileSystem OriginalFileSystem { get; protected set; }

            /// <inheritdoc/>
            public CompositionRenameEvent(IFileSystemObserveHandle observer, IFileSystem originalFileSystem, DateTimeOffset eventTime, string oldPath, string newPath) : base(observer, eventTime, oldPath, newPath) { OriginalFileSystem = originalFileSystem; }
        }

        class CompositionErrorEvent : FileSystemErrorEvent, IFileSystemCompositionEvent
        {
            /// <summary>
            /// Sending file-system.
            /// </summary>
            public IFileSystem OriginalFileSystem { get; protected set; }

            /// <inheritdoc/>
            public CompositionErrorEvent(IFileSystemObserveHandle observer, IFileSystem originalFileSystem, DateTimeOffset eventTime, Exception error) : base(observer, eventTime, error, null) { OriginalFileSystem = originalFileSystem; }
        }

        /// <summary>
        /// Add <paramref name="disposable"/> to list of objects to be disposed along with the system.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns>filesystem</returns>
        public FileSystemComposition AddDisposable(object disposable) => AddDisposableBase(disposable) as FileSystemComposition;

        /// <summary>
        /// Remove disposable from dispose list.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns></returns>
        public FileSystemComposition RemoveDisposable(object disposable) => RemoveDisposableBase(disposable) as FileSystemComposition;

        /// <summary>
        /// Get file systems
        /// </summary>
        /// <returns></returns>
        public IEnumerator<IFileSystem> GetEnumerator()
            => ((IEnumerable<IFileSystem>)fileSystems).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => fileSystems.GetEnumerator();

        /// <summary>
        /// Print info
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => String.Join<IFileSystem>(", ", fileSystems);

    }

}