// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           25.9.2019
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
using System.Threading;

namespace Lexical.FileSystem.FileProvider
{
    /// <summary>
    /// Adapts <see cref="IFileSystem"/> into <see cref="IFileProvider"/>.
    /// 
    /// The recommended way to create <see cref="FileSystemProvider"/> is to use
    /// the extension method in <see cref="FileProviderExtensions.ToFileProvider(IFileSystem)"/>.
    /// </summary>
    public class FileSystemProvider : DisposeList, IFileProviderDisposable
    {
        /// <summary>
        /// Source filesystem
        /// </summary>
        public IFileSystem FileSystem { get; protected set; }

        /// <summary>
        /// Source filesystem casted to <see cref="IDisposable"/>. Value is null if <see cref="FileSystem"/> doesn't implement <see cref="IDisposable"/>.
        /// </summary>
        public IDisposable FileSystemDisposable => FileSystem as IDisposable;

        /// <summary>
        /// Create adapter that adapts <paramref name="sourceFilesystem"/> into <see cref="IFileProvider"/>.
        /// </summary>
        /// <param name="sourceFilesystem"></param>
        public FileSystemProvider(IFileSystem sourceFilesystem) : base()
        {
            FileSystem = sourceFilesystem ?? throw new ArgumentNullException(nameof(sourceFilesystem));
        }

        /// <summary>
        /// Get directory contents.
        /// </summary>
        /// <param name="subpath"></param>
        /// <returns>Directory contents</returns>
        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            try
            {
                // Read entry.
                IFileSystemEntry entry = FileSystem.GetEntry(subpath);
                // Directory doesn't exist
                if (entry == null || !entry.IsDirectory()) return NotFoundDirectoryContents.Singleton;
                // Browse
                IFileSystemEntry[] entries = FileSystem.Browse(subpath);
                // Create infos
                IFileInfo[] infos = new IFileInfo[entries.Length];
                for (int i = 0; i < entries.Length; i++) infos[i] = new FileInfo(FileSystem, entries[i]);
                // Wrap
                return new DirectoryContents(infos);
            }
            catch (DirectoryNotFoundException)
            {
                // Directory doesn't exist
                return NotFoundDirectoryContents.Singleton;
            }
        }

        /// <summary>
        /// Directory contents
        /// </summary>
        public class DirectoryContents : IDirectoryContents
        {
            /// <summary>
            /// Snapshot of directory entries
            /// </summary>
            public readonly IFileInfo[] Entries;

            /// <summary>
            /// Directory exists
            /// </summary>
            public bool Exists => true;

            /// <summary>
            /// Create directory contents.
            /// </summary>
            /// <param name="entries"></param>
            public DirectoryContents(IFileInfo[] entries)
            {
                this.Entries = entries ?? throw new ArgumentNullException(nameof(entries));
            }

            /// <summary>
            /// Enumerate file infos.
            /// </summary>
            /// <returns></returns>
            public IEnumerator<IFileInfo> GetEnumerator()
                => ((IEnumerable<IFileInfo>)Entries).GetEnumerator();

            /// <summary>
            /// Enumerate file infos.
            /// </summary>
            /// <returns></returns>
            IEnumerator IEnumerable.GetEnumerator()
                => ((IEnumerable<IFileInfo>)Entries).GetEnumerator();
        }

        /// <summary>
        /// Locate a file at the given path.
        /// </summary>
        /// <param name="subpath">Relative path that identifies the file.</param>
        /// <returns>The file information. Caller must check Exists property.</returns>
        public IFileInfo GetFileInfo(string subpath)
        {
            // Get entry info
            IFileSystemEntry entry = FileSystem.GetEntry(subpath);
            // Not found
            if (entry == null || !entry.IsFile()) return new NotFoundFileInfo(subpath);
            // Adapt
            return new FileInfo(FileSystem, entry);
        }

        /// <summary>
        /// Adapts <see cref="IFileSystemEntry"/> to <see cref="IFileInfo"/>.
        /// </summary>
        public class FileInfo : IFileInfo
        {
            /// <summary>Entry from filesystem.</summary>
            public IFileSystemEntry Entry { get; protected set; }
            /// <summary>Associated filesystem.</summary>
            public IFileSystem FileSystem { get; protected set; }
            /// <inheritdoc/>
            public bool Exists => true;
            /// <inheritdoc/>
            public long Length => Entry.Length();
            /// <inheritdoc/>
            public string PhysicalPath => null;
            /// <inheritdoc/>
            public string Name => Entry.Name;
            /// <inheritdoc/>
            public DateTimeOffset LastModified => Entry.LastModified;
            /// <inheritdoc/>
            public bool IsDirectory => Entry.IsDirectory();

            /// <summary>
            /// Create <see cref="IFileInfo"/> from <paramref name="entry"/>.
            /// </summary>
            /// <param name="filesystem"></param>
            /// <param name="entry"></param>
            public FileInfo(IFileSystem filesystem, IFileSystemEntry entry)
            {
                this.FileSystem = filesystem ?? throw new ArgumentNullException(nameof(filesystem));
                this.Entry = entry ?? throw new ArgumentNullException(nameof(entry));
            }

            /// <inheritdoc/>
            public Stream CreateReadStream()
                => FileSystem.Open(Entry.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        /// <summary>
        /// Enumerate a directory at the given path, if any.
        /// </summary>
        /// <param name="filter">Relative path that identifies the directory.</param>
        /// <returns>Returns the contents of the directory.</returns>
        public IChangeToken Watch(string filter)
        {
            WatchToken token = new WatchToken();
            IFileSystemObserver disposable = FileSystem.Observe(filter, token);
            Interlocked.CompareExchange(ref token.observerHandle, disposable, null);
            return token;
        }

        class WatchToken : IChangeToken, IObserver<IFileSystemEvent>
        {
            /// <summary>
            /// Has there been change.
            /// </summary>
            public bool HasChanged { get; protected set; }

            /// <summary>
            /// Is RegisterChangeCallback supported
            /// </summary>
            public bool ActiveChangeCallbacks => true;

            /// <summary>
            /// Reference to observer handle
            /// </summary>
            public IFileSystemObserver observerHandle;

            /// <summary>
            /// Registered callbacks.
            /// </summary>
            StructList1<CallbackHandle> callbacks = new StructList1<CallbackHandle>();

            /// <summary>
            /// Is closed
            /// </summary>
            bool closed;

            public IDisposable RegisterChangeCallback(Action<object> callback, object state)
            {
                CallbackHandle handle = new CallbackHandle(this, callback, state);
                lock (this)
                {
                    if (!closed) callbacks.Add(handle);
                }
                return handle;
            }

            void UnregisterCallback(CallbackHandle handle)
            {
                lock(this)
                {
                    callbacks.Remove(handle);
                }
            }

            void CallCallbacks()
            {
                StructList1<CallbackHandle> _callbacks = default;
                lock (this)
                {
                    if (closed) return;
                    if (callbacks.Count == 0) return;
                    _callbacks = callbacks;
                    callbacks = default;
                }
                // Call callbacks
                StructList1<Exception> errors = new StructList1<Exception>();
                for (int i = 0; i < _callbacks.Count; i++)
                {
                    try
                    {
                        _callbacks[i].RunAction();
                    } catch (Exception e)
                    {
                        errors.Add(e);
                    }
                }
                if (errors.Count > 0) throw new AggregateException(errors.ToArray());
            }

            /// <summary>
            /// File system ends observer.
            /// </summary>
            void IObserver<IFileSystemEvent>.OnCompleted()
            {
                closed = true;
                // Only one thread can call Dispose()
                IFileSystemObserver _handle = Interlocked.CompareExchange(ref observerHandle, null, observerHandle);
                // Cancel FileSystem observing
                _handle?.Dispose();
            }

            /// <summary>
            /// File system has an internal error
            /// </summary>
            /// <param name="error"></param>
            void IObserver<IFileSystemEvent>.OnError(Exception error)
            {
            }

            /// <summary>
            /// Process event.
            /// </summary>
            /// <param name="value"></param>
            void IObserver<IFileSystemEvent>.OnNext(IFileSystemEvent value)
            {
                // Take reference to observer handle
                if (observerHandle == null && value.Observer != null) observerHandle = value.Observer;
                // Filesystem has changed
                if (value is IFileSystemEventChange || value is IFileSystemEventCreate || value is IFileSystemEventDelete || value is IFileSystemEventRename)
                {
                    // Mark changed
                    HasChanged = true;
                    try
                    {
                        // Call callbacks
                        CallCallbacks();
                    }
                    finally
                    {
                        // No more registering
                        closed = true;
                        // Only one thread can call Dispose()
                        IFileSystemObserver _handle = Interlocked.CompareExchange(ref observerHandle, null, observerHandle);
                        // Cancel FileSystem observing
                        _handle?.Dispose();
                    }
                }
            }

            class CallbackHandle : IDisposable
            {
                public WatchToken token;
                public Action<object> action;
                public object state;

                public CallbackHandle(WatchToken token, Action<object> action, object state)
                {
                    this.token = token;
                    this.action = action;
                    this.state = state;
                }

                /// <summary>
                /// Runs the associated action and clears references.
                /// Action can be ran by only one thread.
                /// Action is not ran if <see cref="Dispose"/> has been called.
                /// </summary>
                public void RunAction()
                {
                    object _state = state;
                    Action<object> _action = Interlocked.CompareExchange(ref action, null, action);
                    if (_action != null)
                    {
                        _action(_state);
                        state = null;
                    }
                }

                /// <summary>
                /// Dispose handle. This will prevent action being called.
                /// </summary>
                public void Dispose()
                {
                    WatchToken _token = Interlocked.CompareExchange(ref token, null, token);
                    if (_token != null) _token.UnregisterCallback(this);
                    action = null;
                    state = null;
                    token = null;
                }
            }

        }

        /// <summary>
        /// Invoke <paramref name="disposeAction"/> on the dispose of the object.
        /// 
        /// If parent object is disposed or being disposed, the disposable will be disposed immedialy.
        /// </summary>
        /// <param name="disposeAction"></param>
        /// <returns>filesystem</returns>
        public FileSystemProvider AddDisposeAction(Action<FileSystemProvider> disposeAction)
        {
            // Argument error
            if (disposeAction == null) throw new ArgumentNullException(nameof(disposeAction));
            // Parent is disposed/ing
            if (IsDisposing) { disposeAction(this); return this; }
            // Adapt to IDisposable
            IDisposable disposable = new DisposeAction<FileSystemProvider>(disposeAction, this);
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
        public new FileSystemProvider AddDisposeAction(Action<object> disposeAction, object state)
        {
            base.AddDisposeAction(disposeAction, state);
            return this;
        }

        /// <summary>
        /// Add <paramref name="disposable"/> to list of objects to be disposed along with the system.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns>self</returns>
        public FileSystemProvider AddDisposable(object disposable)
        {
            ((IDisposeList)this).AddDisposable(disposable);
            return this;
        }

        /// <summary>
        /// Add <paramref name="disposables"/> to list of objects to be disposed along with the system.
        /// </summary>
        /// <param name="disposables"></param>
        /// <returns>self</returns>
        public FileSystemProvider AddDisposables(IEnumerable<object> disposables)
        {
            ((IDisposeList)this).AddDisposables(disposables);
            return this;
        }

        /// <summary>
        /// Remove <paramref name="disposable"/> from dispose list.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns></returns>
        public FileSystemProvider RemoveDisposable(object disposable)
        {
            ((IDisposeList)this).RemoveDisposable(disposable);
            return this;
        }

        /// <summary>
        /// Remove <paramref name="disposables"/> from dispose list.
        /// </summary>
        /// <param name="disposables"></param>
        /// <returns></returns>
        public FileSystemProvider RemoveDisposables(IEnumerable<object> disposables)
        {
            ((IDisposeList)this).RemoveDisposables(disposables);
            return this;
        }

        /// <summary>
        /// Print info
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => FileSystem.ToString();
    }
}
