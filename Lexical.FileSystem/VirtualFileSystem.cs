// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           28.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Decoration;
using Lexical.FileSystem.Internal;
using Lexical.FileSystem.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Virtual filesystem.
    /// </summary>
    public class VirtualFileSystem : FileSystemBase, IFileSystemOptionPath, IFileSystemMount, IFileSystemBrowse//, IFileSystemCreateDirectory, IFileSystemDelete, IFileSystemObserve, IFileSystemMove, IFileSystemOpen, IFileSystemDisposable, IFileSystemMount
    {
        /// <summary>
        /// Reader writer lock for modifying directory structure. 
        /// </summary>
        ReaderWriterLock m_lock = new ReaderWriterLock();

        /// <summary>
        /// List of observers.
        /// </summary>
        CopyOnWriteList<ObserverHandle> observers = new CopyOnWriteList<ObserverHandle>();

        /// <summary>
        /// A snapshot of observers.
        /// </summary>
        ObserverHandle[] Observers => observers.Array;

        /// <summary>
        /// Root node
        /// </summary>
        Directory root;

        /// <inheritdoc/>
        public FileSystemCaseSensitivity CaseSensitivity => FileSystemCaseSensitivity.Inconsistent;
        /// <inheritdoc/>
        public bool EmptyDirectoryName => true;
        /// <inheritdoc/>
        public virtual bool CanBrowse => true;
        /// <inheritdoc/>
        public virtual bool CanGetEntry => true;
        /// <inheritdoc/>
        public virtual bool CanCreateDirectory => true;
        /// <inheritdoc/>
        public virtual bool CanDelete => true;
        /// <inheritdoc/>
        public override bool CanObserve => true;
        /// <inheritdoc/>
        public virtual bool CanMove => true;
        /// <inheritdoc/>
        public virtual bool CanOpen => true;
        /// <inheritdoc/>
        public virtual bool CanRead => true;
        /// <inheritdoc/>
        public virtual bool CanWrite => true;
        /// <inheritdoc/>
        public virtual bool CanCreateFile => true;
        /// <inheritdoc/>
        public virtual bool CanMount => true;
        /// <inheritdoc/>
        public virtual bool CanUnmount => true;
        /// <inheritdoc/>
        public virtual bool CanListMounts => true;

        /// <summary>
        /// Create virtual filesystem.
        /// </summary>
        public VirtualFileSystem() : base()
        {
            root = new Directory(this, null, "", DateTimeOffset.UtcNow);
        }

        /// <summary>
        /// Non-disposable <see cref="VirtualFileSystem"/> disposes and cleans all attached <see cref="IDisposable"/> on dispose, but doesn't go into disposed state.
        /// </summary>
        public class NonDisposable : VirtualFileSystem
        {
            /// <summary>Create non-disposable virtual filesystem.</summary>
            public NonDisposable() : base() { SetToNonDisposable(); }
        }

        /// <inheritdoc/>
        /// <exception cref="DirectoryNotFoundException">If <paramref name="path"/> goes beyond root with ".."</exception>
        public IFileSystemEntry[] Browse(string path)
        {
            // Assert argument
            if (path == null) throw new ArgumentNullException(nameof(path));
            // Assert not disposed
            if (IsDisposing) throw new ObjectDisposedException(GetType().Name);
            // Read lock
            m_lock.AcquireReaderLock(int.MaxValue);
            try
            {
                // Start
                Directory cursor = root, finalDirectory = root;
                // Stack of nodes that start with path and have a mounted filesystem
                StructList2<FileSystemDecoration> mountPoints = new StructList2<FileSystemDecoration>();
                // Path '/' splitter, enumerates name strings from root towards tail
                PathEnumerator enumr = new PathEnumerator(path, true);
                // Seach each node that start with path
                while (enumr.MoveNext())
                {
                    // Add to stack
                    if (cursor.mount!=null) mountPoints.Add(cursor.mount);
                    // Name
                    StringSegment name = enumr.Current;
                    // "."
                    if (name.Equals(StringSegment.Dot)) continue;
                    // ".."
                    if (name.Equals(StringSegment.DotDot))
                    {
                        if (cursor.parent == null) throw new DirectoryNotFoundException(path);
                        cursor = cursor.parent;
                        continue;
                    }
                    // Failed to find child entry
                    if (!cursor.contents.TryGetValue(name, out cursor)) { finalDirectory = null; break; }
                    // Move final down
                    finalDirectory = cursor;
                }
                // Number of sources (to unify)
                int sourceCount = mountPoints.Count + (finalDirectory == null ? 0 : 1);
                // No mounted points were found, and no virtual directories.
                if (sourceCount == 0) throw new DirectoryNotFoundException(path);
                // One source, no unifying needed.
                if (sourceCount == 1)
                {

                }
                else
                // Create union of mountpoints and final directory. Remove overlapping content.
                {

                }
                return null;
            }
            finally
            {
                m_lock.ReleaseReaderLock();
            }
        }

        /// <inheritdoc/>
        public IFileSystemEntry GetEntry(string path)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get node by <paramref name="path"/>.
        /// Caller must ensure that lock is acquired.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>node or null</returns>
        Directory GetNode(string path)
        {
            // "" refers to root
            if (path == "") return root;
            // Node cursor
            Directory cursor = root;
            // Path '/' splitter, enumerates name strings from root towards tail
            PathEnumerator enumr = new PathEnumerator(path, true);
            // Get next name from the path
            while (enumr.MoveNext())
            {
                // Name
                StringSegment name = enumr.Current;
                // "."
                if (name.Equals(StringSegment.Dot)) continue;
                // ".."
                if (name.Equals(StringSegment.DotDot))
                {
                    if (cursor.parent == null) return null;
                    cursor = cursor.parent;
                    continue;
                }
                // Failed to find child entry
                if (!cursor.contents.TryGetValue(name, out cursor))
                    return null;
            }
            return cursor;
        }

        /// <summary>
        /// Split <paramref name="path"/> into <paramref name="parentPath"/> and <paramref name="name"/>.
        /// 
        /// Also searches for <paramref name="parent"/> directory node.
        /// </summary>
        /// <param name="path">path to parse</param>
        /// <param name="parentPath">path to parent</param>
        /// <param name="name"></param>
        /// <param name="parent">(optional) parent object</param>
        /// <returns>true parent was successfully found</returns>
        bool GetParentAndName(string path, out StringSegment parentPath, out StringSegment name, out Directory parent)
        {
            // Special case for root
            if (path == "") { parentPath = StringSegment.Empty; name = StringSegment.Empty; parent = root; return false; }
            // Path '/' splitter, enumerates name strings from root towards tail
            PathEnumerator enumr = new PathEnumerator(path, true);
            // Path's name parts
            StructList12<StringSegment> names = new StructList12<StringSegment>();
            // Split path into names
            while (enumr.MoveNext()) names.Add(enumr.Current);
            // Unexpected error
            if (names.Count == 0) { name = StringSegment.Empty; parentPath = StringSegment.Empty; parent = null; return false; }
            // Separate to parentPath and name
            if (names.Count == 1) { name = names[0]; parentPath = StringSegment.Empty; }
            else { name = names[names.Count - 1]; parentPath = new StringSegment(path, names[0].Start, names[names.Count - 2].Start + names[names.Count - 2].Length); }
            // Search parent directory
            Directory cursor = root;
            for (int i = 0; i < names.Count - 1; i++)
            {
                // Name
                StringSegment cursorName = names[i];
                // "."
                if (cursorName.Equals(StringSegment.Dot)) continue;
                // ".."
                if (cursorName.Equals(StringSegment.DotDot))
                {
                    if (cursor.parent == null) { parent = null; return false; }
                    cursor = cursor.parent;
                    continue;
                }
                // Failed to find child entry
                if (!cursor.contents.TryGetValue(cursorName, out cursor)) { parent = null; return false; }
            }
            parent = cursor;
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="observer"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public override IFileSystemObserver Observe(string filter, IObserver<IFileSystemEvent> observer, object state = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Observer
        /// </summary>
        class ObserverHandle : FileSystemObserverHandleBase, IFileSystemEventStart
        {
            /// <summary>Filter pattern that is used for filtering events by path.</summary>
            Regex filterPattern;

            /// <summary>Accept all pattern "**".</summary>
            bool acceptAll;

            /// <summary>Time when observing started.</summary>
            DateTimeOffset startTime = DateTimeOffset.UtcNow;

            /// <summary>
            /// Create new observer.
            /// </summary>
            /// <param name="filesystem"></param>
            /// <param name="filter">path filter as glob pattenrn. "*" any sequence of charaters within a directory, "**" any sequence of characters, "?" one character. E.g. "**/*.txt"</param>
            /// <param name="observer"></param>
            /// <param name="state"></param>
            public ObserverHandle(VirtualFileSystem filesystem, string filter, IObserver<IFileSystemEvent> observer, object state) : base(filesystem, filter, observer, state)
            {
                if (filter == "**") acceptAll = true;
                else this.filterPattern = GlobPatternFactory.Slash.CreateRegex(filter);
            }

            /// <summary>
            /// Tests whether <paramref name="path"/> qualifies the filter.
            /// </summary>
            /// <param name="path"></param>
            /// <returns></returns>
            public bool Qualify(string path)
                => acceptAll || filterPattern.IsMatch(path);

            /// <summary>
            /// Remove this handle from collection of observers.
            /// </summary>
            /// <param name="errors"></param>
            protected override void InnerDispose(ref StructList4<Exception> errors)
            {
                base.InnerDispose(ref errors);
                (this.FileSystem as VirtualFileSystem).observers.Remove(this);
            }

            IFileSystemObserver IFileSystemEvent.Observer => this;
            DateTimeOffset IFileSystemEvent.EventTime => startTime;
            string IFileSystemEvent.Path => null;
        }

        /// <summary>
        /// Virtual directory in virtual filesystem.
        /// 
        /// Mounted filesystem can be attached to virtual directory.
        /// </summary>
        class Directory : IDisposable
        {
            /// <summary>Cached path.</summary>
            protected internal string path;
            /// <summary>Name of the entry.</summary>
            protected internal string name;
            /// <summary>Has node been deleted.</summary>
            protected internal bool isDeleted;
            /// <summary>Last modified time.</summary>
            protected internal DateTimeOffset lastModified;
            /// <summary>Last access time.</summary>
            protected internal DateTimeOffset lastAccess;
            /// <summary>Parent filesystem.</summary>
            protected VirtualFileSystem filesystem;
            /// <summary>Parent node.</summary>
            protected internal Directory parent;
            /// <summary>Cached entry</summary>
            protected IFileSystemEntryMount entry;
            /// <summary>Get or create entry.</summary>
            public IFileSystemEntryMount Entry => entry ?? (entry = CreateEntry());
            /// <summary>Files and directories. Lazy construction. Reads and modifications under parent's m_lock.</summary>
            protected internal Dictionary<StringSegment, Directory> contents = new Dictionary<StringSegment, Directory>();
            /// <summary>Cached child entries</summary>
            protected IFileSystemEntry[] childEntries;
            /// <summary>The mounted filesystem.</summary>
            protected internal FileSystemDecoration mount;
            /// <summary>Get or create child entries.</summary>
            public IFileSystemEntry[] ChildEntries
            {
                get
                {
                    if (childEntries != null) return childEntries;
                    int c = contents.Count;
                    IFileSystemEntry[] array = new IFileSystemEntry[c];
                    int i = 0;
                    foreach (Directory e in contents.Values) array[i++] = e.Entry;
                    return childEntries = array;
                }
            }

            /// <summary>Path</summary>
            public string Path
            {
                get
                {
                    // Get reference of previous cached value
                    string _path = path;
                    // Return previous cached value
                    if (_path != null) return _path;
                    // Get reference of parent
                    Directory _parent = parent;
                    // Case for root
                    if (_parent == null) return path = "";
                    // k2nd+ level paths
                    return _parent.Path + name + "/";
                }
            }

            /// <summary>
            /// Create entry
            /// </summary>
            /// <param name="filesystem"></param>
            /// <param name="parent"></param>
            /// <param name="name"></param>
            /// <param name="lastModified"></param>
            public Directory(VirtualFileSystem filesystem, Directory parent, string name, DateTimeOffset lastModified)
            {
                this.filesystem = filesystem ?? throw new ArgumentNullException(nameof(filesystem));
                this.parent = parent;
                this.name = name ?? throw new ArgumentNullException(nameof(name));
                this.lastModified = lastModified;
                this.lastAccess = lastModified;
                this.mount = null;
            }

            /// <summary>
            /// Create entry snapshot.
            /// </summary>
            /// <returns></returns>
            public IFileSystemEntryMount CreateEntry()
                => new FileSystemEntryMount(filesystem, Path, name, lastModified, lastAccess, filesystem);

            /// <summary>
            /// Enumerate self and subtree.
            /// </summary>
            /// <returns></returns>
            public IEnumerable<Directory> VisitTree()
            {
                Queue<Directory> queue = new Queue<Directory>();
                queue.Enqueue(this);
                while (queue.Count > 0)
                {
                    Directory n = queue.Dequeue();
                    yield return n;
                    foreach (Directory c in contents.Values)
                        queue.Enqueue(c);
                }
            }

            /// <summary>
            /// Delete node
            /// </summary>
            public virtual void Dispose()
            {
                this.isDeleted = true;
            }


            /// <summary>Flush cached entry info.</summary>
            public void FlushEntry() => entry = null;
            /// <summary>Flush cached array of child entries.</summary>
            public void FlushChildEntries() => childEntries = null;
            /// <summary>Flush cached path string and entry</summary>
            public void FlushPath() { path = null; entry = null; }

            /// <summary>
            /// Print info
            /// </summary>
            /// <returns></returns>
            public override string ToString() => Path;
        }

        /// <summary>
        /// Mount <paramref name="filesystem"/> at <paramref name="path"/> in the parent filesystem.
        /// 
        /// If <paramref name="path"/> is already mounted, then replaces previous mount.
        /// If there is an open stream to previously mounted filesystem, that stream is unlinked from the filesystem.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="filesystem"></param>
        /// <param name="mountOption">(optional)</param>
        /// <returns>this (parent filesystem)</returns>
        /// <exception cref="NotSupportedException">If operation is not supported</exception>
        public VirtualFileSystem Mount(string path, IFileSystem filesystem, IFileSystemOption mountOption = null)
            => Mount(path, (filesystem, mountOption));

        /// <summary>
        /// Mount <paramref name="filesystems"/> at <paramref name="path"/> in the parent filesystem.
        /// 
        /// If <paramref name="path"/> is already mounted, then replaces previous mount.
        /// If there is an open stream to previously mounted filesystem, that stream is unlinked from the filesystem.
        /// </summary>
        /// <param name="path">path to the directory where to mount <paramref name="filesystems"/></param>
        /// <param name="filesystems">(optional)filesystems and options</param>
        /// <returns>this (parent filesystem)</returns>
        /// <exception cref="NotSupportedException">If operation is not supported</exception>
        /// <exception cref="DirectoryNotFoundException">If <paramref name="path"/> refers beyond root with ".."</exception>
        public VirtualFileSystem Mount(string path, params (IFileSystem filesystem, IFileSystemOption mountOption)[] filesystems)
        {
            // Assert argument
            if (path == null) throw new ArgumentNullException(nameof(path));
            // Assert not disposed
            if (IsDisposing) throw new ObjectDisposedException(GetType().Name);
            // Datetime
            DateTimeOffset now = DateTimeOffset.UtcNow;
            // Queue of events
            StructList12<IFileSystemEvent> events = new StructList12<IFileSystemEvent>();
            // Take snapshot of observers
            ObserverHandle[] observers = this.Observers;
            // Create decoration filesystem (or null)
            FileSystemDecoration fs =
                filesystems == null || filesystems.Length == 0 ? null :
                filesystems.Length == 1 ? new FileSystemDecoration(this, path, filesystems[0].filesystem, filesystems[0].mountOption) :
                new FileSystemDecoration(this, filesystems.Select(p => (path, p.filesystem, p.mountOption)).ToArray());

            // Write Lock
            m_lock.AcquireWriterLock(int.MaxValue);
            try
            {
                // Follow path and get-or-create nodes
                Directory cursor = root;
                // Split path at '/' slashes
                PathEnumerator enumr = new PathEnumerator(path, ignoreTrailingSlash: true);
                while (enumr.MoveNext())
                {
                    // Name
                    StringSegment name = enumr.Current;
                    // Update last access
                    cursor.lastAccess = now;
                    // "."
                    if (name.Equals(StringSegment.Dot)) continue;
                    // ".."
                    if (name.Equals(StringSegment.DotDot))
                    {
                        // ".." -> exception
                        if (cursor.parent == null) throw new DirectoryNotFoundException(path);
                        // Go towards parent.
                        cursor = cursor.parent;
                        // Next path segment
                        continue;
                    }
                    // Create node
                    if (!cursor.contents.TryGetValue(name, out cursor))
                    {
                        // Create child directory
                        Directory newDirectory = new Directory(this, cursor, name, now);
                        // Add event about child being created
                        if (observers != null /*&& GetEntry not yet implemented !this.Exists(path)*/)
                            foreach (ObserverHandle observer in observers)
                            {
                                if (observer.Qualify(newDirectory.Path)) events.Add(new FileSystemEventCreate(observer, now, newDirectory.Path));
                            }
                        // Update time of parent
                        cursor.lastModified = now;
                        // Add child to parent
                        cursor.contents[enumr.Current] = newDirectory;
                        // Flush caches
                        cursor.FlushChildEntries();
                        cursor.FlushEntry();
                        // Move cursor to child
                        cursor = newDirectory;
                    }
                }

                // New mount
                if (cursor.mount == null)
                {
                    cursor.mount = fs;
                    // TODO Events
                } else
                // Replace mount
                {
                    cursor.mount = fs;
                    // TODO Events
                }

                
            }
            finally
            {
                m_lock.ReleaseWriterLock();
            }

            // Send events
            if (events.Count > 0) SendEvents(ref events);

            return this;
        }

        /// <summary>
        /// List all mounts
        /// </summary>
        /// <returns></returns>
        public IFileSystemEntryMount[] ListMounts()
        {
            // Assert not disposed
            if (IsDisposing) throw new ObjectDisposedException(GetType().Name);
            // Lock
            m_lock.AcquireReaderLock(int.MaxValue);
            try
            {
                List<IFileSystemEntryMount> result = new List<IFileSystemEntryMount>();
                foreach (Directory node in root.VisitTree()) result.Add(node.Entry);
                return result.ToArray();
            }
            finally
            {
                m_lock.ReleaseReaderLock();
            }
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
        public VirtualFileSystem Unmount(string path)
        {
            // Assert argument
            if (path == null) throw new ArgumentNullException(nameof(path));
            // Assert not disposed
            if (IsDisposing) throw new ObjectDisposedException(GetType().Name);
            // Datetime
            DateTimeOffset now = DateTimeOffset.UtcNow;
            // Queue of events
            StructList12<IFileSystemEvent> events = new StructList12<IFileSystemEvent>();
            // Take snapshot of observers
            ObserverHandle[] observers = this.Observers;

            // Write Lock
            m_lock.AcquireWriterLock(int.MaxValue);
            try
            {
                // Follow path and get-or-create nodes
                Directory cursor = root;
                // Split path at '/' slashes
                PathEnumerator enumr = new PathEnumerator(path, ignoreTrailingSlash: true);
                while (enumr.MoveNext())
                {
                    // Name
                    StringSegment name = enumr.Current;
                    // Update last access
                    cursor.lastAccess = now;
                    // "."
                    if (name.Equals(StringSegment.Dot)) continue;
                    // ".."
                    if (name.Equals(StringSegment.DotDot))
                    {
                        // ".." -> exception
                        if (cursor.parent == null) throw new DirectoryNotFoundException(path);
                        // Go towards parent.
                        cursor = cursor.parent;
                        // Next path segment
                        continue;
                    }
                    // Node was not found
                    if (!cursor.contents.TryGetValue(name, out cursor)) throw new DirectoryNotFoundException(path.Substring(0, name.Length));
                }

                // Disconnect from parent, if no children
                while (cursor != null && cursor.contents.Count == 0 && cursor.parent != null)
                {
                    // Remove from parent
                    cursor.parent.contents.Remove(new StringSegment(cursor.name));
                    cursor.parent.lastModified = now;
                    // TODO events

                    // Move towards parent
                    cursor = cursor.parent;
                }

            }
            finally
            {
                m_lock.ReleaseWriterLock();
            }

            // Send events
            if (events.Count > 0) SendEvents(ref events);

            return this;
        }

        IFileSystem IFileSystemMount.Mount(string path, IFileSystem filesystem, IFileSystemOption mountOption) => Mount(path, (filesystem, mountOption));
        IFileSystem IFileSystemMount.Unmount(string path) => Unmount(path);

        /// <summary>
        /// Handle dispose
        /// </summary>
        /// <param name="disposeErrors"></param>
        protected override void InnerDispose(ref StructList4<Exception> disposeErrors)
        {
        }

        /// <summary>
        /// Invoke <paramref name="disposeAction"/> on the dispose of the object.
        /// 
        /// If parent object is disposed or being disposed, the disposable will be disposed immedialy.
        /// </summary>
        /// <param name="disposeAction"></param>
        /// <returns>self</returns>
        public VirtualFileSystem AddDisposeAction(Action<VirtualFileSystem> disposeAction)
        {
            // Argument error
            if (disposeAction == null) throw new ArgumentNullException(nameof(disposeAction));
            // Parent is disposed/ing
            if (IsDisposing) { disposeAction(this); return this; }
            // Adapt to IDisposable
            IDisposable disposable = new DisposeAction<VirtualFileSystem>(disposeAction, this);
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
        public VirtualFileSystem AddDisposeAction(Action<object> disposeAction, object state)
        {
            ((IDisposeList)this).AddDisposeAction(disposeAction, state);
            return this;
        }

        /// <summary>
        /// Add <paramref name="disposable"/> to list of objects to be disposed along with the system.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns>filesystem</returns>
        public VirtualFileSystem AddDisposable(object disposable)
        {
            ((IDisposeList)this).AddDisposable(disposable);
            return this;
        }

        /// <summary>
        /// Add <paramref name="disposables"/> to list of objects to be disposed along with the system.
        /// </summary>
        /// <param name="disposables"></param>
        /// <returns>filesystem</returns>
        public VirtualFileSystem AddDisposables(IEnumerable<object> disposables)
        {
            ((IDisposeList)this).AddDisposables(disposables);
            return this;
        }

        /// <summary>
        /// Remove <paramref name="disposable"/> from dispose list.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns></returns>
        public VirtualFileSystem RemoveDisposable(object disposable)
        {
            ((IDisposeList)this).RemoveDisposable(disposable);
            return this;
        }

        /// <summary>
        /// Remove <paramref name="disposables"/> from dispose list.
        /// </summary>
        /// <param name="disposables"></param>
        /// <returns></returns>
        public VirtualFileSystem RemoveDisposables(IEnumerable<object> disposables)
        {
            ((IDisposeList)this).RemoveDisposables(disposables);
            return this;
        }

        /// <summary>
        /// Print info
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => GetType().Name;
    }
}
