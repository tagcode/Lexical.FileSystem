// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           28.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Internal;
using Lexical.FileSystem.Utility;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Virtual filesystem.
    /// </summary>
    public class VirtualFileSystem : FileSystemBase, IFileSystemOptionPath, IFileSystemMount //, IFileSystemBrowse, IFileSystemCreateDirectory, IFileSystemDelete, IFileSystemObserve, IFileSystemMove, IFileSystemOpen, IFileSystemDisposable, IFileSystemMount
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
        Node root;

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
            root = new Node(this, null, "", DateTimeOffset.UtcNow);
        }

        /// <summary>
        /// Get node by <paramref name="path"/>.
        /// Caller must ensure that lock is acquired.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>node or null</returns>
        Node GetNode(string path)
        {
            // "" refers to root
            if (path == "") return root;
            // Node cursor
            Node cursor = root;
            // Path '/' splitter, enumerates name strings from root towards tail
            PathEnumerator2 enumr = new PathEnumerator2(path);
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
        bool GetParentAndName(string path, out StringSegment parentPath, out StringSegment name, out Node parent)
        {
            // Special case for root
            if (path == "") { parentPath = StringSegment.Empty; name = StringSegment.Empty; parent = root; return false; }
            // Path '/' splitter, enumerates name strings from root towards tail
            PathEnumerator2 enumr = new PathEnumerator2(path);
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
            Node cursor = root;
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
        /// A node in virtual filesystem. Represents a directory or mountpoint.
        /// Directory can be upgraded to mountpoint.
        /// </summary>
        class Node : IDisposable
        {
            /// <summary>
            /// Cached path. 
            /// </summary>
            protected internal string path;

            /// <summary>
            /// Name of the entry.
            /// </summary>
            protected internal string name;

            /// <summary>
            /// Has node been deleted.
            /// </summary>
            protected internal bool isDeleted;

            /// <summary>
            /// Last modified time.
            /// </summary>
            protected internal DateTimeOffset lastModified;

            /// <summary>
            /// Last access time.
            /// </summary>
            protected internal DateTimeOffset lastAccess;

            /// <summary>
            /// Parent filesystem.
            /// </summary>
            protected VirtualFileSystem filesystem;

            /// <summary>
            /// Parent node.
            /// </summary>
            protected internal Node parent;

            /// <summary>
            /// Cached entry
            /// </summary>
            protected IFileSystemEntry entry;

            /// <summary>
            /// Get or create entry.
            /// </summary>
            public IFileSystemEntry Entry => entry ?? (entry = CreateEntry());

            /// <summary>
            /// Files and directories. Lazy construction. Modified under m_lock.
            /// </summary>
            protected internal Dictionary<StringSegment, Node> contents = new Dictionary<StringSegment, Node>();

            /// <summary>
            /// Cached child entries
            /// </summary>
            protected IFileSystemEntry[] childEntries;

            /// <summary>
            /// Get or create child entries.
            /// </summary>
            public IFileSystemEntry[] ChildEntries
            {
                get
                {
                    if (childEntries != null) return childEntries;
                    int c = contents.Count;
                    IFileSystemEntry[] array = new IFileSystemEntry[c];
                    int i = 0;
                    foreach (Node e in contents.Values) array[i++] = e.Entry;
                    return childEntries = array;
                }
            }

            /// <summary>
            /// Path to the entry.
            /// </summary>
            public string Path
            {
                get
                {
                    // Get reference of previous cached value
                    string _path = path;
                    // Return previous cached value
                    if (_path != null) return _path;
                    // Get reference of parent
                    Node _parent = parent;
                    // Case for root
                    if (_parent == null) return path = "";
                    // Case for first level paths
                    if (_parent == filesystem.root) return path = (name == "" ? "/" : name);
                    // 2nd+ level paths
                    return path = _parent.Path == "/" && name != "" ? _parent.Path + name : _parent.Path + "/" + name;
                }
            }

            /// <summary>
            /// Create entry
            /// </summary>
            /// <param name="filesystem"></param>
            /// <param name="parent"></param>
            /// <param name="name"></param>
            /// <param name="lastModified"></param>
            public Node(VirtualFileSystem filesystem, Node parent, string name, DateTimeOffset lastModified)
            {
                this.filesystem = filesystem ?? throw new ArgumentNullException(nameof(filesystem));
                this.parent = parent;
                this.name = name ?? throw new ArgumentNullException(nameof(name));
                this.lastModified = lastModified;
            }

            /// <summary>
            /// Create entry snapshot.
            /// </summary>
            /// <returns></returns>
            public IFileSystemEntry CreateEntry()
                => new FileSystemEntryDirectory(filesystem, Path, name, lastModified, lastAccess, filesystem);

            /// <summary>
            /// Flush cached array of child entries.
            /// </summary>
            public void FlushChildEntries()
            {
                childEntries = null;
            }

            /// <summary>
            /// Enumerate self and subtree.
            /// </summary>
            /// <returns></returns>
            public IEnumerable<Node> VisitTree()
            {
                Queue<Node> queue = new Queue<Node>();
                queue.Enqueue(this);
                while (queue.Count > 0)
                {
                    Node n = queue.Dequeue();
                    yield return n;
                    foreach (Node c in contents.Values)
                        queue.Enqueue(c);
                }
            }

            /// <summary>
            /// Flush cached path info
            /// </summary>
            public void FlushPath()
            {
                path = null;
                entry = null;
            }

            /// <summary>
            /// Flush cached entry info.
            /// </summary>
            public void FlushEntry()
            {
                entry = null;
            }

            /// <summary>
            /// Delete node
            /// </summary>
            public virtual void Dispose()
            {
                this.isDeleted = true;
            }

            /// <summary>
            /// Print info
            /// </summary>
            /// <returns></returns>
            public override string ToString() => Path;
        }

        IFileSystem IFileSystemMount.Mount(string path, IFileSystem filesystem, IFileSystemOption mountOption) => Mount(path, filesystem, mountOption);
        IFileSystem IFileSystemMount.Unmount(string path) => Unmount(path);

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
        {
            return this;
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
            return this;
        }

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

        public IFileSystemEntryMount[] ListMounts()
        {
            throw new NotImplementedException();
        }
    }
}
