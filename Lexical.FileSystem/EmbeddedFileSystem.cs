// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Lexical.FileSystem
{
    /// <summary>
    /// File System that represents embedded resources of an <see cref="System.Reflection.Assembly"/>.
    /// </summary>
    public class EmbeddedFileSystem : FileSystemBase, IFileSystemBrowse, IFileSystemOpen, IPathInfo
    {
        /// <summary>
        /// Associated Assembly
        /// </summary>
        public readonly Assembly Assembly;

        /// <summary>
        /// Snapshot of entries.
        /// </summary>
        protected IDirectoryContent entries;
        /// <summary>
        /// Lazy construction of entries.
        /// </summary>
        protected IDirectoryContent Entries => entries ?? (entries = CreateEntries());
        /// <summary>
        /// Snapshot of entries as map
        /// </summary>
        protected Dictionary<string, IEntry> entryMap;
        /// <summary>
        /// Lazy construction of entries as map.
        /// </summary>
        protected Dictionary<string, IEntry> EntryMap => entryMap ?? (Entries.ToDictionary(e => e.Path));

        /// <inheritdoc/>
        public FileSystemCaseSensitivity CaseSensitivity => FileSystemCaseSensitivity.CaseSensitive;
        /// <inheritdoc/>
        public bool EmptyDirectoryName => false;
        /// <inheritdoc/>
        public virtual bool CanBrowse => true;
        /// <inheritdoc/>
        public virtual bool CanGetEntry => true;
        /// <inheritdoc/>
        public virtual bool CanOpen => true;
        /// <inheritdoc/>
        public virtual bool CanRead => true;
        /// <inheritdoc/>
        public virtual bool CanWrite => false;
        /// <inheritdoc/>
        public virtual bool CanCreateFile => false;
        /// <inheritdoc/>
        public virtual bool CanObserve => false;

        /// <summary>
        /// Root entry
        /// </summary>
        protected IEntry rootEntry;

        /// <summary>
        /// Create embedded 
        /// </summary>
        /// <param name="assembly"></param>
        public EmbeddedFileSystem(Assembly assembly)
        {
            this.Assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
            this.rootEntry = new DirectoryEntry(this, "", "", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, assembly.Location);
        }

        /// <summary>
        /// Create a snapshot of entries.
        /// </summary>
        /// <returns></returns>
        protected IDirectoryContent CreateEntries()
        {
            string[] names = Assembly.GetManifestResourceNames();

            // Get file time, or use Unix time 0.
            DateTimeOffset writetime;
            if (Assembly.Location != null && File.Exists(Assembly.Location))
                writetime = new FileInfo(Assembly.Location).LastWriteTimeUtc;
            else
                writetime = DateTimeOffset.MinValue;

            IEntry[] result = new IEntry[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                result[i] = new FileEntry(this, names[i], names[i], writetime, DateTimeOffset.MinValue, -1L, null);
            }
            return new DirectoryContent(this, "", result);
        }

        /// <summary>
        /// Browse a list of embedded resources.
        /// 
        /// For example:
        ///     "assembly.res1"
        ///     "assembly.res2"
        /// </summary>
        /// <param name="path"></param>
        /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
        /// <returns></returns>
        public IDirectoryContent Browse(string path, IOption option = null)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (IsDisposed) throw new ObjectDisposedException(GetType().FullName);
            if (path == "") return entries ?? (entries = CreateEntries());
            return new DirectoryNotFound(this, path);
        }

        /// <summary>
        /// Get entry of a single file or directory.
        /// </summary>
        /// <param name="path">path to a directory or to a single file, "" is root, separator is "/"</param>
        /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
        /// <returns>entry, or null if entry is not found</returns>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support exists</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        public IEntry GetEntry(string path, IOption option = null)
        {
            if (path == null) throw new ArgumentNullException(path);
            if (IsDisposed) throw new ObjectDisposedException(GetType().FullName);
            if (path == "") return rootEntry;
            IEntry e;
            if (EntryMap.TryGetValue(path, out e)) return e;
            return null;
        }

        /// <summary>
        /// Open embedded resource for reading.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="fileMode"></param>
        /// <param name="fileAccess"></param>
        /// <param name="fileShare"></param>
        /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
        /// <returns></returns>
        public Stream Open(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare, IOption option = null)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (IsDisposed) throw new ObjectDisposedException(GetType().FullName);
            if (fileMode != FileMode.Open) throw new IOException($"Cannot open embedded resouce in FileMode={fileMode}");
            if (fileAccess != FileAccess.Read) throw new IOException($"Cannot open embedded resouce in FileAccess={fileAccess}");
            Stream s = Assembly.GetManifestResourceStream(path);
            if (s == null) throw new FileNotFoundException(path);
            return s;
        }

        /// <summary>
        /// Invoke <paramref name="disposeAction"/> on the dispose of the object.
        /// 
        /// If parent object is disposed or being disposed, the disposable will be disposed immedialy.
        /// </summary>
        /// <param name="disposeAction"></param>
        /// <returns>self</returns>
        public EmbeddedFileSystem AddDisposeAction(Action<EmbeddedFileSystem> disposeAction)
        {
            // Argument error
            if (disposeAction == null) throw new ArgumentNullException(nameof(disposeAction));
            // Parent is disposed/ing
            if (IsDisposing) { disposeAction(this); return this; }
            // Adapt to IDisposable
            IDisposable disposable = new DisposeAction<EmbeddedFileSystem>(disposeAction, this);
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
        public EmbeddedFileSystem AddDisposeAction(Action<object> disposeAction, object state)
        {
            ((IDisposeList)this).AddDisposeAction(disposeAction, state);
            return this;
        }

        /// <summary>
        /// Add <paramref name="disposable"/> to list of objects to be disposed along with the system.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns>filesystem</returns>
        public EmbeddedFileSystem AddDisposable(object disposable)
        {
            ((IDisposeList)this).AddDisposable(disposable);
            return this;
        }

        /// <summary>
        /// Add <paramref name="disposables"/> to list of objects to be disposed along with the system.
        /// </summary>
        /// <param name="disposables"></param>
        /// <returns>filesystem</returns>
        public EmbeddedFileSystem AddDisposables(IEnumerable disposables)
        {
            ((IDisposeList)this).AddDisposables(disposables);
            return this;
        }

        /// <summary>
        /// Remove <paramref name="disposable"/> from dispose list.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns></returns>
        public EmbeddedFileSystem RemoveDisposable(object disposable)
        {
            ((IDisposeList)this).RemoveDisposable(disposable);
            return this;
        }

        /// <summary>
        /// Remove <paramref name="disposables"/> from dispose list.
        /// </summary>
        /// <param name="disposables"></param>
        /// <returns></returns>
        public EmbeddedFileSystem RemoveDisposables(IEnumerable disposables)
        {
            ((IDisposeList)this).RemoveDisposables(disposables);
            return this;
        }

        /// <summary>
        /// Print info
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => Assembly.FullName;

        /// <inheritdoc/>
        public virtual IFileSystemObserver Observe(string filter, IObserver<IEvent> observer, object state = null, IEventDispatcher eventDispatcher = null)
            => throw new NotSupportedException(nameof(Observe));
    }
}