// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           15.10.2019
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
using System.Text.RegularExpressions;
using System.Threading;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Virtual filesystem.
    /// </summary>
    public class VirtualFileSystem : FileSystemBase, IFileSystemOptionPath, IFileSystemMount, IFileSystemBrowse, IFileSystemOpen, IFileSystemCreateDirectory, IFileSystemObserve, IFileSystemDelete, IFileSystemFileAttribute, IFileSystemMove
    {
        /// <summary>
        /// Reader writer lock for modifying vfs directory structure. 
        /// </summary>
        ReaderWriterLock vfsLock = new ReaderWriterLock();

        /// <summary>
        /// Root node
        /// </summary>
        Directory vfsRoot;

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
        public virtual bool CanObserve => true;
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
        public virtual bool CanListMountPoints => true;
        /// <inheritdoc/>
        public virtual bool CanSetFileAttribute => true;

        /// <summary>
        /// Create virtual filesystem.
        /// </summary>
        public VirtualFileSystem() : base()
        {
            vfsRoot = new Directory(this, null, "", DateTimeOffset.UtcNow);
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

            // Stack of nodes that start with path and have a mounted filesystem
            StructList4<FileSystemDecoration> mountpoints = new StructList4<FileSystemDecoration>();
            // Snapshot of vfs entries
            StructList4<IFileSystemEntry> vfsEntries = new StructList4<IFileSystemEntry>();
            // Lock for the duration of tree traversal
            vfsLock.AcquireReaderLock(int.MaxValue);
            // Was vfs directory found at path argument
            bool directoryAtPath;
            try
            {
                // Vfs Node
                Directory directory;
                // Search for vfs node
                directoryAtPath = GetVfsDirectory(path, out directory, ref mountpoints, true);
                // Browse the vfs directory (mountpoint) at the path
                if (directoryAtPath && directory.children.Count>0) foreach(var c in directory.children) vfsEntries.Add(c.Value.Entry);
            }
            finally
            {
                vfsLock.ReleaseReaderLock();
            }
            // Found nothing.
            if (mountpoints.Count == 0 && vfsEntries.Count == 0)
            {
                if (directoryAtPath) return new IFileSystemEntry[0];
                throw new DirectoryNotFoundException(path);
            }
            // Return vfs contents
            if (mountpoints.Count == 0 && vfsEntries.Count > 0) return vfsEntries.ToArray();
            // Return already decorated contents
            if (mountpoints.Count == 1 && vfsEntries.Count == 0) return mountpoints[0].Browse(path);

            // Create union of mountpoints and final directory. Unify overlapping content if same name. Priority: vfs, mountpoints
            // Estimation of entry count
            int entryCount = vfsEntries.Count;
            // Browse each mountpoint
            StructList2<IFileSystemEntry[]> entryArrays = new StructList2<IFileSystemEntry[]>();
            for (int i = mountpoints.Count-1; i >= 0; i--)
            {
                var fs = mountpoints[i];
                if (!fs.CanBrowse()) continue;
                try
                {
                    IFileSystemEntry[] _entries = fs.Browse(path);
                    if (_entries.Length == 0) continue;
                    entryArrays.Add(_entries);
                    entryCount += _entries.Length;
                }
                catch (DirectoryNotFoundException) { }
                catch (NotSupportedException) { }
            }

            // Create hashset for removing overlapping entry names
            Dictionary<StringSegment, IFileSystemEntry> entries = new Dictionary<StringSegment, IFileSystemEntry>(entryCount, StringSegment.Comparer.Instance);
            // Add vfs
            for (int i = 0; i < vfsEntries.Count; i++)
            {
                // Get mount entry
                IFileSystemEntry e = vfsEntries[i];
                // key for dictionary to match with files of same path
                StringSegment key = e.Path.Length>0 && e.Path[e.Path.Length-1] == '/' ? new StringSegment(e.Path, 0, e.Path.Length-1) : new StringSegment(e.Path);
                // Remove already existing entry
                entries[key] = e;
            }
            // Add entries from mounted filesystems
            for (int i = 0; i < entryArrays.Count; i++)
            {
                foreach (IFileSystemEntry e in entryArrays[i])
                {
                    // key for dictionary to match with files of same path
                    StringSegment key = e.IsDirectory() && e.Path.Length > 0 && e.Path[e.Path.Length - 1] == '/' ? new StringSegment(e.Path, 0, e.Path.Length - 1) : new StringSegment(e.Path);
                    //
                    IFileSystemEntry prevEntry;
                    // Unify entries
                    if (entries.TryGetValue(key, out prevEntry)) entries[key] = new FileSystemEntryPairDecoration(prevEntry, e);
                    // Add entry
                    else entries[key] = e;
                }
            }
            // Return
            return entries.Values.ToArray();
        }

        /// <inheritdoc/>
        public IFileSystemEntry GetEntry(string path)
        {
            // Assert argument
            if (path == null) throw new ArgumentNullException(nameof(path));
            // Assert not disposed
            if (IsDisposing) throw new ObjectDisposedException(GetType().Name);

            // Stack of nodes that start with path and have a mounted filesystem
            StructList4<FileSystemDecoration> mountpoints = new StructList4<FileSystemDecoration>();
            // Vfs Node
            Directory directory;
            // Found vfs at path
            bool directoryAtPath;
            // Vfs mount entry
            IFileSystemEntry entry = null;
            // Lock for the duration of tree traversal
            vfsLock.AcquireReaderLock(int.MaxValue);
            try
            {
                // Search for vfs node
                directoryAtPath = GetVfsDirectory(path, out directory, ref mountpoints, false);
                // Browse the vfs directory at path
                if (directoryAtPath) entry = directory.Entry;
            }
            finally
            {
                vfsLock.ReleaseReaderLock();
            }

            // Try to get entry from mounted filesystems
            for (int i = mountpoints.Count-1; i >= 0; i--)
            {
                var fs = mountpoints[i];
                if (!fs.CanGetEntry()) continue;
                try
                {
                    IFileSystemEntry e = fs.GetEntry(path);
                    if (e != null) entry = entry != null ? new FileSystemEntryPairDecoration(entry, e) : e;
                }
                catch (NotSupportedException) { }
            }
            // Nothing
            return entry;
        }

        /// <summary>
        /// Traverse vfs directory tree along <paramref name="path"/>, and return the <see cref="Directory"/> instance.
        /// 
        /// If <paramref name="path"/> cannot be found, returns false and the last <see cref="Directory"/> that was found.
        /// Adds each mountpoint along the way to <paramref name="mountpoints"/> collection.
        /// 
        /// The caller must have <see cref="vfsLock"/> read or write lock.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="directory"></param>
        /// <param name="mountpoints">place all assigned filesystems that were found in the traversal of <paramref name="path"/>. Added in order from root towards <paramref name="path"/>.</param>
        /// <param name="throwOnError">if true and <paramref name="path"/> refers beyond root, then throw <see cref="DirectoryNotFoundException"/></param>
        /// <returns>true if directory was found at <paramref name="path"/>, else false and the last directory that was found</returns>
        /// <exception cref="DirectoryNotFoundException">If <paramref name="path"/> refers beyond root and <paramref name="throwOnError"/> is true</exception>
        bool GetVfsDirectory(string path, out Directory directory, ref StructList4<FileSystemDecoration> mountpoints, bool throwOnError)
        {
            // Special case root
            if (path == "") { if (vfsRoot.mount != null) mountpoints.Add(vfsRoot.mount); directory = vfsRoot; return true; }
            // Start
            Directory cursor = vfsRoot; directory = vfsRoot;
            // Path '/' splitter, enumerates name strings from root towards tail
            PathEnumerator enumr = new PathEnumerator(path, true);
            // Add to collection
            if (cursor.mount != null) mountpoints.Add(cursor.mount);
            // Traverse path in name parts
            while (enumr.MoveNext())
            {
                // Name
                StringSegment name = enumr.Current;
                // "."
                if (name.Equals(StringSegment.Dot)) continue;
                // ".."
                if (name.Equals(StringSegment.DotDot))
                {
                    if (cursor.parent == null)
                    {
                        if (throwOnError) throw new DirectoryNotFoundException(path);
                        return false;
                    }
                    cursor = cursor.parent;
                    continue;
                }
                // Failed to find child entry
                else if (!cursor.children.TryGetValue(name, out cursor)) return false;
                // Add to collection
                if (cursor.mount != null) mountpoints.Add(cursor.mount);
                // Update result 
                directory = cursor;
            }
            // Path was matched with vfs directory
            return true;
        }

        /* Vfs maintains a tree of virtual directory nodes. 
         * One FileSystemDecoration can be mounted to each node, though, however, multiple IFileSystems can be assigned to on FileSystemDecoration.
         * 
         * ""                                                       <- vfsRoot
         * ├──"C:"                                                  <- vfs Directory
         * │  ├──"Users" - FileSystemDecoration(IMemoryFileSystem)  <- mounted vfs directory
         * 
         * 
         */

        /// <summary>
        /// Vfs directory. 
        /// They are points where other filesystems are mounted to.
        /// 
        /// Virtual tree directories are created with Mount() and deleted with Unmount(). 
        /// Vfs doesn't allow creating mountpoints with CreateDirectory() and Delete().
        /// 
        /// Vfs tree structure is read and written to under <see cref="vfsLock"/>.
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
            protected VirtualFileSystem vfs;
            /// <summary>Parent node.</summary>
            protected internal Directory parent;
            /// <summary>Cached entry</summary>
            protected IFileSystemEntryMount entry;
            /// <summary>Get or create entry.</summary>
            public IFileSystemEntryMount Entry => entry ?? (entry = CreateEntry());
            /// <summary>Files and directories. Lazy construction. Reads and modifications under parent's m_lock.</summary>
            protected internal Dictionary<StringSegment, Directory> children = new Dictionary<StringSegment, Directory>();
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
                    int c = children.Count;
                    IFileSystemEntry[] array = new IFileSystemEntry[c];
                    int i = 0;
                    foreach (Directory e in children.Values) array[i++] = e.Entry;
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
                    // 2nd+ level paths
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
                this.vfs = filesystem ?? throw new ArgumentNullException(nameof(filesystem));
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
            {
                var _mount = mount;
                if (_mount != null)
                {
                    IFileSystemEntryMount e = _mount.GetEntry(Path) as IFileSystemEntryMount;
                    if (e != null) return e;
                }
                return new FileSystemEntryMount(vfs, Path, name, lastModified, lastAccess, null, null);
            }

            /// <summary>
            /// Enumerate self and subtree.
            /// </summary>
            /// <param name="parents">visit parent nodes</param>
            /// <param name="self">visit this node</param>
            /// <param name="decendents">visit children and thier decendents</param>
            /// <returns></returns>
            public IEnumerable<Directory> Visit(bool parents, bool self, bool decendents)
            {
                // Visit parents
                if (parents)
                {
                    Directory cursor = parent;
                    while (cursor != null)
                    {
                        yield return cursor;
                        cursor = cursor.parent;
                    }
                }

                // Visit self
                if (self) yield return this;

                // Visit decedents
                if (decendents && children.Count>0)
                {
                    Queue<Directory> queue = new Queue<Directory>();
                    foreach(Directory child in children.Values) queue.Enqueue(child);
                    while (queue.Count > 0)
                    {
                        Directory n = queue.Dequeue();
                        yield return n;
                        foreach (Directory c in n.children.Values)
                            queue.Enqueue(c);
                    }
                }
            }

            /// <summary>
            /// Tests this <paramref name="lowerDirectory"/> is child of this directory.
            /// </summary>
            /// <param name="lowerDirectory"></param>
            /// <returns></returns>
            public bool IsParentOf(Directory lowerDirectory)
            {
                for (Directory cursor = lowerDirectory; cursor != null; cursor = cursor.parent)
                    if (cursor == this) return true;
                return false;
            }

            /// <summary>Delete node</summary>
            public virtual void Dispose() { this.isDeleted = true; }
            /// <summary>Flush cached entry info.</summary>
            public void FlushEntry() => entry = null;
            /// <summary>Flush cached array of child entries.</summary>
            public void FlushChildEntries() => childEntries = null;
            /// <summary>Flush cached path string and entry</summary>
            public void FlushPath() { path = null; entry = null; }
            /// <summary>Print info</summary>
            public override string ToString() => Path;
        }

        /* Observers use a model where observers are placed on a tree structure. 
         * Location is based on the stem part of the glob pattern. Stem is extracted with GlobPatternInfo.
         * Observer tree is separate from vfs mount directory tree.
         * 
         * For instance, if observer is created with glob pattern "C:/Temp/**.zip/**.txt", then a nodes "", "C:", "Temp" are created to place the observer.
         * 
         * ""
         * ├──"C:"
         * │  ├── "Temp" <- observer is placed here
         * 
         * Lifecycle. Observer remains alive until it is disposed, or until VirtualFileSystem is disposed.
         * Any filesystem mounting and unmounting does not dispose observer.
         * 
         * Observer forwards events from mounted filesystems and from modifications of vfs tree structure. 
         * Mounting and unmounting creates virtual folders create pseudo-Create and Delete events.
         */

        /// <summary>
        /// Reader writer lock for modifying observer tree.
        /// </summary>
        protected ReaderWriterLock observerLock = new ReaderWriterLock();

        /// <summary>
        /// Observer tree root. Read and modified only under <see cref="observerLock"/>.
        /// </summary>
        protected ObserverNode observerRoot = new ObserverNode(null, "");

        /// <summary>
        /// Get or create observer node.
        /// </summary>
        /// <param name="observerPath">the stem part from glob pattern</param>
        /// <param name="handleToAdd">(optional) Observer handle to add while in lock</param>
        /// <returns>observer node</returns>
        /// <exception cref="DirectoryNotFoundException">if refers beyond parent with ".."</exception>
        protected ObserverNode GetOrCreateObserverNode(string observerPath, ObserverHandle handleToAdd)
        {
            // Assert arguments
            if (observerPath == null) throw new ArgumentNullException(nameof(observerPath));
            // Special case, root
            if (observerPath == "")
            {
                if (handleToAdd != null) {
                    LockCookie writeLock_ = observerLock.UpgradeToWriterLock(int.MaxValue);
                    observerRoot.observers.Add(handleToAdd);
                    handleToAdd.observerNode = observerRoot;
                    observerLock.DowngradeFromWriterLock(ref writeLock_);
                }
                return observerRoot;
            }
            // Start from root
            ObserverNode cursor = observerRoot;
            // Path '/' splitter, enumerates name strings from root towards tail
            PathEnumerator enumr = new PathEnumerator(observerPath, true);
            // Writer lock
            LockCookie writeLock = default;
            // Get read lock
            observerLock.AcquireReaderLock(int.MaxValue);
            try
            {
                // Traverse path in name parts
                while (enumr.MoveNext())
                {
                    // Name
                    StringSegment name = enumr.Current;
                    // "."
                    if (name.Equals(StringSegment.Dot)) continue;
                    // ".."
                    if (name.Equals(StringSegment.DotDot))
                    {
                        if (cursor.parent == null) throw new DirectoryNotFoundException(observerPath);
                        cursor = cursor.parent;
                        continue;
                    }
                    // Failed to find child entry
                    ObserverNode child;
                    if (cursor.children.TryGetValue(name, out child))
                    {
                        cursor = child;
                    }
                    else 
                    {
                        if (writeLock == default) writeLock = observerLock.UpgradeToWriterLock(int.MaxValue);
                        ObserverNode newNode = new ObserverNode(cursor, name);
                        // Try reading again in write lock, if still fails, place newNode
                        if (cursor.children.TryGetValue(name, out child)) cursor = child; else { cursor.children[name] = newNode; cursor = newNode; }
                    }
                }
                // Return node at cursor
                return cursor;
            }
            finally
            {
                // Add handle to node
                if (handleToAdd != null)
                {
                    if (writeLock == default) writeLock = observerLock.UpgradeToWriterLock(int.MaxValue);
                    cursor.observers.Add(handleToAdd);
                    handleToAdd.observerNode = cursor;
                }
                // Release write lock
                if (writeLock != default) observerLock.DowngradeFromWriterLock(ref writeLock);
                // Release read lock
                observerLock.ReleaseReaderLock();
            }
        }

        /// <summary>
        /// Get ObserverNode at <paramref name="path"/>.
        /// 
        /// If ObserverNode is found at <paramref name="path"/>, then returns true and the the node in <paramref name="result"/>,
        /// if not then returns the closest found node in <paramref name="result"/> and false.
        /// 
        /// The caller should have <see cref="observerLock"/> before calling, in order to be able to use the result ObserverNode.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="result"></param>
        /// <returns>true if observer node was found at <paramref name="path"/></returns>
        protected bool GetObserverNode(string path, out ObserverNode result)
        {
            // Assert arguments
            if (path == null) throw new ArgumentNullException(nameof(path));
            // Special case, root
            if (path == "") { result = observerRoot; return true; }
            // Start from root
            ObserverNode cursor = observerRoot;
            // Path '/' splitter, enumerates name strings from root towards tail
            PathEnumerator enumr = new PathEnumerator(path, true);
            // Get read lock
            observerLock.AcquireReaderLock(int.MaxValue);
            try
            {
                // Traverse path in name parts
                while (enumr.MoveNext())
                {
                    // Name
                    StringSegment name = enumr.Current;
                    // "."
                    if (name.Equals(StringSegment.Dot)) continue;
                    // ".."
                    if (name.Equals(StringSegment.DotDot))
                    {
                        if (cursor.parent == null) { result = cursor; return false; }
                        cursor = cursor.parent;
                        continue;
                    }
                    // Failed to find child entry
                    ObserverNode child;
                    if (cursor.children.TryGetValue(name, out child))
                    {
                        cursor = child;
                    }
                    else
                    {
                        result = cursor; return false;
                    }
                }
                // Return node at cursor
                result = cursor;
                return true;
            }
            finally
            {
                // Release read lock
                observerLock.ReleaseReaderLock();
            }
        }

        /// <summary>
        /// Get observer handles.
        /// 
        /// If <paramref name="atThis"/> is true, then return observers at <paramref name="path"/>.
        /// If <paramref name="atParents"/> is true, then observers before <paramref name="path"/>,
        /// and if <paramref name="atDecendents"/> is true, then observers after <paramref name="path"/>.
        /// </summary>
        /// <param name="path">Path to search from</param>
        /// <param name="atParents">If true, then add handles that are parent to <paramref name="path"/></param>
        /// <param name="atThis">If true, then add handles at <paramref name="path"/></param>
        /// <param name="atDecendents">If true, then add handles that are decendents at <paramref name="path"/></param>
        /// <param name="observers">Record to place results</param>
        protected void GetObserverHandles(string path, bool atParents, bool atThis, bool atDecendents, ref StructList12<ObserverHandle> observers)
        {
            // Create list of intersecting observers
            observerLock.AcquireReaderLock(int.MaxValue);
            try
            {
                // Get observer node
                ObserverNode observerNode;
                bool foundAtPath = GetObserverNode(path, out observerNode);

                // Add at observerNode
                if ((foundAtPath && atThis) || (!foundAtPath && atParents)) foreach (ObserverHandle h in observerNode.observers) observers.Add(h);

                if (atParents)
                    foreach (ObserverNode n in observerNode.Visit(true, false, false))
                        foreach (ObserverHandle h in n.observers) observers.Add(h);

                if (foundAtPath && atDecendents)
                    foreach (ObserverNode n in observerNode.Visit(false, false, true))
                        foreach (ObserverHandle h in n.observers) observers.Add(h);
            }
            finally
            {
                observerLock.ReleaseReaderLock();
            }
        }

        /// <summary>
        /// Add observer.
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="observer"></param>
        /// <param name="state"></param>
        /// <param name="eventDispatcher">(optional) </param>
        /// <returns></returns>
        public virtual IFileSystemObserver Observe(string filter, IObserver<IFileSystemEvent> observer, object state = null, IFileSystemEventDispatcher eventDispatcher = default)
        {
            // Assert not disposed
            if (IsDisposed) throw new ObjectDisposedException(GetType().FullName);
            // Assert supported
            if (!this.CanObserve) throw new NotSupportedException(nameof(Observe));
            // Assert arguments
            if (filter == null) throw new ArgumentNullException(nameof(filter));
            if (observer == null) throw new ArgumentNullException(nameof(observer));

            // Parse filter
            GlobPatternInfo info = new GlobPatternInfo(filter);
            // Create handle
            ObserverHandle adapter = new ObserverHandle(this, null /*set below*/, this, filter, observer, state, eventDispatcher);
            // Add to observer tree
            GetOrCreateObserverNode(info.Stem, adapter);
            // Send IFileSystemEventStart, must be sent before subscribing forwardees
            observer.OnNext(new FileSystemEventStart(adapter, DateTimeOffset.UtcNow));

            try
            {
                // Create observers in mounted filesystems and start forwarding events
                // Lock for the duration of tree traversal
                vfsLock.AcquireReaderLock(int.MaxValue);
                try
                {
                    // Start
                    Directory cursor = vfsRoot, finalCursor = vfsRoot;
                    // Path '/' splitter, enumerates name strings from root towards tail
                    PathEnumerator enumr = new PathEnumerator(info.Stem, true);
                    // Traverse path in name parts
                    if (info.Stem != "") 
                        while (enumr.MoveNext())
                        {
                            // Name
                            StringSegment name = enumr.Current;
                            // "."
                            if (name.Equals(StringSegment.Dot)) continue;
                            // ".."
                            if (name.Equals(StringSegment.DotDot))
                            {
                                if (cursor.parent == null) throw new DirectoryNotFoundException(info.Stem); // doesn't go here, already checked above in observer tree
                                cursor = cursor.parent;
                                continue;
                            }
                            // Failed to find child entry
                            if (!cursor.children.TryGetValue(name, out cursor)) break;
                            // Move finalCursor
                            finalCursor = cursor;
                        }

                    // Test if any existing mount in vfs tree intersects with observer's filter
                    foreach (Directory d in finalCursor.Visit(true, true, true))
                    {
                        // No mounts
                        if (d.mount == null) continue;
                        // Get component filesystems
                        FileSystemDecoration.Component[] components = d.mount.components.Array;
                        // No components
                        if (components.Length == 0) continue;
                        // Test if filter intersects with the mount
                        string intersection = GlobPatternSet.Intersection(d.Path + "**", filter);
                        // No intersection
                        if (intersection == null) continue;

                        // Observe each component
                        foreach (FileSystemDecoration.Component component in components)
                        {
                            // Assert can observe
                            if (!component.Option.CanObserve) continue;
                            // Convert Path
                            String childPath;
                            if (!component.Path.ParentToChild(intersection, out childPath)) continue;
                            try
                            {
                                // Try Observe
                                IDisposable disposable = component.FileSystem.Observe(childPath, adapter, new ObserverDecorator.StateInfo(component.Path, component), eventDispatcher);
                                // Attach disposable
                                ((IDisposeList)adapter).AddDisposable(disposable);
                            }
                            catch (NotSupportedException) { }
                            catch (ArgumentException) { } // FileSystem.PatternObserver throws directory is not found, TODO create contract for proper exception
                        }
                    }

                    // Return
                    return adapter;
                }
                finally
                {
                    vfsLock.ReleaseReaderLock();
                }
            }
            // Update references in the expception and let it fly
            catch (FileSystemException e) when (FileSystemExceptionUtil.Set(e, this, filter))
            {
                // Never goes here
                throw new NotSupportedException(nameof(Observe));
            }
            // Dispose handle if something goes wrong, but let exception fly
            catch (Exception) when (DisposeObserver(adapter)) { /*Never goes here*/ throw new Exception(); }
            bool DisposeObserver(ObserverHandle handle) { handle?.Dispose(); return false; }
        }


        /// <summary>
        /// Observer
        /// </summary>
        protected internal class ObserverHandle : ObserverDecorator
        {
            /// <summary>Filter pattern that is used for filtering events by path.</summary>
            protected internal Regex filterPattern;
            /// <summary>Accept all pattern "**".</summary>
            protected internal bool acceptAll;
            /// <summary>Time when observing started.</summary>
            protected internal DateTimeOffset startTime = DateTimeOffset.UtcNow;
            /// <summary>Place where observer is held in observer tree.</summary>
            protected internal ObserverNode observerNode;
            /// <summary>Parent object.</summary>
            protected internal VirtualFileSystem vfs;

            /// <summary>
            /// Create adapter observer.
            /// </summary>
            /// <param name="vfs">parent object</param>
            /// <param name="observerNode">(optional) node where handle is placed in the tree. Can be assigned later</param>
            /// <param name="sourceFileSystem">File system to show as the source of forwarded events (in <see cref="IFileSystemEvent.Observer"/>)</param>
            /// <param name="filter"></param>
            /// <param name="observer">The observer were decorated events are forwarded to</param>
            /// <param name="state"></param>
            /// <param name="eventDispatcher">(optional) </param>
            public ObserverHandle(VirtualFileSystem vfs, ObserverNode observerNode, IFileSystem sourceFileSystem, string filter, IObserver<IFileSystemEvent> observer, object state, IFileSystemEventDispatcher eventDispatcher) : base(sourceFileSystem, filter, observer, state, eventDispatcher, false)
            {
                this.vfs = vfs;
                this.observerNode = observerNode;
                if (filter == "**") acceptAll = true;
                else this.filterPattern = GlobPatternRegexFactory.Slash.CreateRegex(filter);
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
                var _vfs = vfs;
                var _observerNode = Interlocked.CompareExchange(ref observerNode, null, observerNode);
                if (_observerNode!=null) _observerNode.observers.Remove(this);
                base.InnerDispose(ref errors);

                // Prune observer node from tree
                if (_vfs != null && _observerNode != null)
                {
                    _vfs.observerLock.AcquireWriterLock(int.MaxValue);
                    try
                    {
                        ObserverNode cursor = _observerNode;
                        // Disconnect from parent, if no children
                        while (cursor != null && cursor.children.Count == 0 && cursor.parent != null)
                        {
                            // Disconnect from parent
                            cursor.parent.children.Remove(cursor.name);
                            // Move towards parent
                            cursor = cursor.parent;
                        }
                    } finally
                    {
                        _vfs.observerLock.ReleaseWriterLock();
                    }
                    vfs = null;
                }
            }

            /// <summary>Print info</summary>
            public override string ToString() => $"VirtualFileSystem.Observer({Filter})";
        }

        /// <summary>
        /// Observer tree node. Node represents all the observers placed in a path. Path represents the stem part of <see cref="GlobPatternInfo"/>.
        /// Root node represents "" path. 
        /// 
        /// Observers can be placed before mounting, or after mounting. 
        /// 
        /// Observer tree is read and modified under <see cref="observerLock"/>.
        /// </summary>
        protected internal class ObserverNode
        {
            /// <summary>Cached path.</summary>
            protected internal string path;
            /// <summary>Name of the node.</summary>
            protected internal string name;
            /// <summary>Parent node</summary>
            protected internal ObserverNode parent;
            /// <summary>Files and directories. Lazy construction. Reads and modifications under parent's m_lock.</summary>
            protected internal Dictionary<StringSegment, ObserverNode> children = new Dictionary<StringSegment, ObserverNode>();
            /// <summary>Observers that are on this node.</summary>
            protected internal ArrayList<ObserverHandle> observers = new ArrayList<ObserverHandle>();
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
                    ObserverNode _parent = parent;
                    // Case for root
                    if (_parent == null) return path = "";
                    // k2nd+ level paths
                    return _parent.Path + name + "/";
                }
            }
            /// <summary>
            /// Crate observer node.
            /// </summary>
            /// <param name="parent"></param>
            /// <param name="name"></param>
            public ObserverNode(ObserverNode parent, string name)
            {
                this.parent = parent;
                this.name = name;
            }

            /// <summary>
            /// Enumerate self and subtree.
            /// </summary>
            /// <param name="parents">visit parent nodes</param>
            /// <param name="self">visit this node</param>
            /// <param name="decendents">visit children and thier decendents</param>
            /// <returns></returns>
            public IEnumerable<ObserverNode> Visit(bool parents, bool self, bool decendents)
            {
                // Visit parents
                if (parents)
                {
                    ObserverNode cursor = parent;
                    while (cursor != null)
                    {
                        yield return cursor;
                        cursor = cursor.parent;
                    }
                }

                // Visit self
                if (self) yield return this;

                // Visit decedents
                if (decendents && children.Count > 0)
                {
                    Queue<ObserverNode> queue = new Queue<ObserverNode>();
                    foreach (ObserverNode child in children.Values) queue.Enqueue(child);
                    while (queue.Count > 0)
                    {
                        ObserverNode n = queue.Dequeue();
                        yield return n;
                        foreach (ObserverNode c in n.children.Values)
                            queue.Enqueue(c);
                    }
                }
            }
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
            => Mount(path, new FileSystemAssignment(filesystem, mountOption));

        /// <summary>
        /// Mount <paramref name="filesystems"/> at <paramref name="path"/> in the parent filesystem.
        /// 
        /// If <paramref name="path"/> is already mounted, then replaces previous mount.
        /// If there is an open stream to previously mounted filesystem, that stream is unlinked from the filesystem.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="filesystems"></param>
        /// <returns>this (parent filesystem)</returns>
        /// <exception cref="NotSupportedException">If operation is not supported</exception>
        public VirtualFileSystem Mount(string path, params (IFileSystem filesystem, IFileSystemOption mountOption)[] filesystems)
            => Mount(path, filesystems.Select(fs=>new FileSystemAssignment(fs.filesystem, fs.mountOption)).ToArray());

        /// <summary>
        /// Mount <paramref name="mounts"/> at <paramref name="path"/> in the parent filesystem.
        /// 
        /// If <paramref name="path"/> is already mounted, then replaces previous mount.
        /// If there is an open stream to previously mounted filesystem, that stream is unlinked from the filesystem.
        /// </summary>
        /// <param name="path">path to the directory where to mount <paramref name="mounts"/></param>
        /// <param name="mounts">(optional)filesystems and options</param>
        /// <returns>this (parent filesystem)</returns>
        /// <exception cref="NotSupportedException">If operation is not supported</exception>
        /// <exception cref="DirectoryNotFoundException">If <paramref name="path"/> refers beyond root with ".."</exception>
        public VirtualFileSystem Mount(string path, params FileSystemAssignment[] mounts)
        {
            // Assert argument
            if (path == null) throw new ArgumentNullException(nameof(path));
            // Assert not disposed
            if (IsDisposing) throw new ObjectDisposedException(GetType().Name);
            // Datetime
            DateTimeOffset now = DateTimeOffset.UtcNow;
            // vfs directories created, for events
            StructList4<string> vfsDirectoriesCreated = new StructList4<string>();
            StructList4<string> vfsDirectoriesRemoved = new StructList4<string>();
            // Take snapshot of observers
            StructList2<FileSystemDecoration.Component> componentsAdded = new StructList2<FileSystemDecoration.Component>();
            StructList2<FileSystemDecoration.Component> componentsRemoved = new StructList2<FileSystemDecoration.Component>();
            StructList2<FileSystemDecoration.Component> componentsReused = new StructList2<FileSystemDecoration.Component>();

            // Write Lock
            vfsLock.AcquireWriterLock(int.MaxValue);
            try
            {
                // Follow path and get-or-create nodes
                Directory cursor = vfsRoot;
                // Split path at '/' slashes
                PathEnumerator enumr = new PathEnumerator(path, ignoreTrailingSlash: true);
                // Follow names
                if (path != "") while (enumr.MoveNext())
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
                        Directory child;
                        if (!cursor.children.TryGetValue(name, out child))
                        {
                            // Create child directory
                            Directory newDirectory = new Directory(this, cursor, name, now);
                            // Add up for events later
                            vfsDirectoriesCreated.Add(newDirectory.Path);
                            // Update time of parent
                            cursor.lastModified = now;
                            // Add child to parent
                            cursor.children[enumr.Current] = newDirectory;
                            // Flush caches
                            cursor.FlushChildEntries();
                            cursor.FlushEntry();
                            // Move cursor to child
                            cursor = newDirectory;
                        }
                        else
                        {
                            cursor = child;
                        }
                    }

                // Create container for components.
                if (cursor.mount == null) cursor.mount = new FileSystemDecoration(this, cursor.Path);
                // Set components
                cursor.mount.SetComponents(ref componentsAdded, ref componentsRemoved, ref componentsReused, cursor.Path, mounts);
                // Root entry changed
                cursor.FlushEntry();
                // Change relative ".." path to absolute, so that events in ProcessMountEvents will have better paths.
                path = cursor.Path;
            }
            finally
            {
                vfsLock.ReleaseWriterLock();
            }

            // Process events
            ProcessMountEvents(path, ref vfsDirectoriesCreated, ref vfsDirectoriesRemoved, ref componentsAdded, ref componentsRemoved);

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
            // Assert argument
            if (path == null) throw new ArgumentNullException(nameof(path));
            // Assert not disposed
            if (IsDisposing) throw new ObjectDisposedException(GetType().Name);
            // Datetime
            DateTimeOffset now = DateTimeOffset.UtcNow;
            // vfs directories created, for events
            StructList4<string> vfsDirectoriesCreated = new StructList4<string>();
            StructList4<string> vfsDirectoriesRemoved = new StructList4<string>();
            // Take snapshot of observers
            StructList2<FileSystemDecoration.Component> componentsAdded = new StructList2<FileSystemDecoration.Component>();
            StructList2<FileSystemDecoration.Component> componentsRemoved = new StructList2<FileSystemDecoration.Component>();
            StructList2<FileSystemDecoration.Component> componentsReused = new StructList2<FileSystemDecoration.Component>();

            // Write Lock
            vfsLock.AcquireWriterLock(int.MaxValue);
            try
            {
                // Follow path and get-or-create nodes
                Directory cursor = vfsRoot;
                // Split path at '/' slashes
                PathEnumerator enumr = new PathEnumerator(path, ignoreTrailingSlash: true);
                if (path != "") while (enumr.MoveNext())
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
                        if (!cursor.children.TryGetValue(name, out cursor)) throw new DirectoryNotFoundException(path.Substring(0, name.Length));
                    }

                // Remove components
                if (cursor.mount != null) cursor.mount.SetComponents(ref componentsAdded, ref componentsRemoved, ref componentsReused, cursor.Path);

                // Just in case some other thread uses.
                cursor.FlushEntry();

                // Change relative ".." path to absolute, so that events in ProcessMountEvents will have better paths.
                path = cursor.Path;

                // Disconnect from parent, if no children
                while (cursor != null && cursor.children.Count == 0 && cursor.parent != null)
                {
                    // Mark for events
                    vfsDirectoriesRemoved.Add(cursor.Path);
                    // Remove from parent
                    cursor.parent.children.Remove(new StringSegment(cursor.name));
                    cursor.parent.lastModified = now;
                    // Dispose decoration
                    cursor.mount?.Dispose();
                    // Move towards parent
                    cursor = cursor.parent;
                }
            }
            finally
            {
                vfsLock.ReleaseWriterLock();
            }

            // Process events
            ProcessMountEvents(path, ref vfsDirectoriesCreated, ref vfsDirectoriesRemoved, ref componentsAdded, ref componentsRemoved);

            return this;
        }

        /// <summary>
        /// Helper function for Mount and Unmount, processes changes to events, and dispatches them.
        /// </summary>
        /// <param name="path">Mount path</param>
        /// <param name="vfsDirectoriesCreated"></param>
        /// <param name="vfsDirectoriesRemoved"></param>
        /// <param name="componentsAdded"></param>
        /// <param name="componentsRemoved"></param>
        protected internal void ProcessMountEvents(string path, ref StructList4<string> vfsDirectoriesCreated, ref StructList4<string> vfsDirectoriesRemoved, ref StructList2<FileSystemDecoration.Component> componentsAdded, ref StructList2<FileSystemDecoration.Component> componentsRemoved)
        {
            // Nothing to do
            if (vfsDirectoriesCreated.Count == 0 && vfsDirectoriesRemoved.Count == 0 && componentsAdded.Count == 0 && componentsRemoved.Count == 0) return;
            // Datetime
            DateTimeOffset now = DateTimeOffset.UtcNow;
            // Gather events here
            StructList12<IFileSystemEvent> events = new StructList12<IFileSystemEvent>();
            // Gather observers here
            StructList12<ObserverHandle> observers = new StructList12<ObserverHandle>();

            // Vfs Directory create events
            if (vfsDirectoriesCreated.Count > 0)
            {
                GetObserverHandles(path, true, true, false, ref observers);

                // Find cross-section of added dirs and interested nodes
                for (int i = 0; i < vfsDirectoriesCreated.Count; i++)
                {
                    string newVfsPath = vfsDirectoriesCreated[i];
                    for (int j = 0; j < observers.Count; j++)
                    {
                        // Qualify
                        if (!observers[j].Qualify(newVfsPath)) continue;
                        // Create event
                        IFileSystemEvent e = new FileSystemEventCreate(observers[j], now, newVfsPath);
                        // Add to be dispatched
                        events.Add(e);
                    }
                }
                observers.Clear();
            }

            // VFS Directory create events
            if (vfsDirectoriesRemoved.Count > 0)
            {
                GetObserverHandles(path, true, true, false, ref observers);

                // Find cross-section of added dirs and interested nodes
                for (int i = 0; i < vfsDirectoriesRemoved.Count; i++)
                {
                    string newVfsPath = vfsDirectoriesRemoved[i];
                    for (int j = 0; j < observers.Count; j++)
                    {
                        // Qualify
                        if (!observers[j].Qualify(newVfsPath)) continue;
                        // Create event
                        IFileSystemEvent e = new FileSystemEventDelete(observers[j], now, newVfsPath);
                        // Add to be dispatched
                        events.Add(e);
                    }
                }
                observers.Clear();
            }

            // Process added and removed filesystems
            if (componentsAdded.Count > 0 || componentsRemoved.Count > 0)
            {
                GetObserverHandles(path, true, true, true, ref observers);
                for (int i = 0; i < observers.Count; i++)
                {
                    ObserverHandle observer = observers[i];

                    // Added filesystems
                    for (int j = 0; j < componentsAdded.Count; j++)
                    {
                        FileSystemDecoration.Component c = componentsAdded[j];

                        // FileSystem is not observed
                        if (!c.Option.CanObserve) continue;

                        string intersection = GlobPatternSet.Intersection(observer.Filter, c.Path.ParentPath + "**");
                        if (intersection == null) continue;

                        string childFilter;
                        if (!c.Path.ParentToChild(intersection, out childFilter)) continue;

                        foreach (var entry in new FileScanner(c.FileSystem).AddGlobPattern(childFilter))
                        {
                            string parentPath;
                            if (!c.Path.ChildToParent(entry.Path, out parentPath)) continue;
                            IFileSystemEvent e = new FileSystemEventCreate(observer, now, parentPath);
                            events.Add(e);
                        }
                    }

                    // Removed filesystems
                    for (int j = 0; j < componentsRemoved.Count; j++)
                    {
                        FileSystemDecoration.Component c = componentsRemoved[j];

                        // FileSystem is not observed
                        if (!c.Option.CanObserve) continue;

                        string intersection = GlobPatternSet.Intersection(observer.Filter, c.Path.ParentPath + "**");
                        if (intersection == null) continue;

                        string childFilter;
                        if (!c.Path.ParentToChild(intersection, out childFilter)) continue;

                        foreach (var entry in new FileScanner(c.FileSystem).AddGlobPattern(childFilter))
                        {
                            string parentPath;
                            if (!c.Path.ChildToParent(entry.Path, out parentPath)) continue;
                            IFileSystemEvent e = new FileSystemEventDelete(observer, now, parentPath);
                            events.Add(e);
                        }
                    }
                }
            }

            // Dispatch events
            if (events.Count > 0) DispatchEvents(ref events);
        }

        IFileSystem IFileSystemMount.Mount(string path, params FileSystemAssignment[] mounts) => Mount(path, mounts);
        IFileSystem IFileSystemMount.Unmount(string path) => Unmount(path);

        /// <summary>
        /// List all mounts
        /// </summary>
        /// <returns></returns>
        public IFileSystemEntryMount[] ListMountPoints()
        {
            // Assert not disposed
            if (IsDisposing) throw new ObjectDisposedException(GetType().Name);
            // Lock
            vfsLock.AcquireReaderLock(int.MaxValue);
            try
            {
                return vfsRoot.Visit(false, true, true).Where(d=>d.mount!=null).Select(d => d.Entry).ToArray();
            }
            finally
            {
                vfsLock.ReleaseReaderLock();
            }
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
            // Assert argument
            if (path == null) throw new ArgumentNullException(nameof(path));
            // Assert not disposed
            if (IsDisposing) throw new ObjectDisposedException(GetType().Name);

            // Stack of nodes that start with path and have a mounted filesystem
            StructList4<FileSystemDecoration> mountpoints = new StructList4<FileSystemDecoration>();
            // Vfs Node
            Directory directory;
            // Lock for the duration of tree traversal
            vfsLock.AcquireReaderLock(int.MaxValue);
            try
            {
                // Search for vfs node
                GetVfsDirectory(path, out directory, ref mountpoints, true);
            }
            finally
            {
                vfsLock.ReleaseReaderLock();
            }

            // Try to open with mounted filesystems
            bool supported = false;
            for (int i = mountpoints.Count-1; i >= 0; i--)
            {
                var fs = mountpoints[i];
                // Get fs option
                var option = fs.AsOption<IFileSystemOptionOpen>();
                // No feature
                if (option == null) continue;
                // fs cannot open
                if (!option.CanOpen) continue;
                // fs cannot read
                if (!option.CanRead && (fileAccess & FileAccess.Read) != 0) continue;
                // fs cannot write
                if (!option.CanWrite && (fileAccess & FileAccess.Write) != 0) continue;
                // fs cannot create
                if (!option.CanCreateFile && (fileMode & (FileMode.Append | FileMode.Create | FileMode.CreateNew | FileMode.OpenOrCreate)) != 0) continue;

                try
                {
                    return fs.Open(path, fileMode, fileAccess, fileShare);
                }
                catch (NotSupportedException) { }
                catch (FileNotFoundException) { supported = true; }
            }
            if (!supported) throw new NotSupportedException(nameof(Open));
            throw new FileNotFoundException(path);
        }

        /// <summary>
        /// Create a directory, or multiple cascading directories.
        /// 
        /// If directory at <paramref name="path"/> already exists, then returns without exception.
        /// <paramref name="path"/> should end with directory separator character '/'.
        /// </summary>
        /// <param name="path">Relative path to file. Directory separator is "/". The root is without preceding slash "", e.g. "dir/dir2"</param>
        /// <returns>true if directory exists after the method, false if directory doesn't exist</returns>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is an empty string (""), contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support create directory</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        public void CreateDirectory(string path)
        {
            // Assert argument
            if (path == null) throw new ArgumentNullException(nameof(path));
            // Assert not disposed
            if (IsDisposing) throw new ObjectDisposedException(GetType().Name);

            // Stack of nodes that start with path and have a mounted filesystem
            StructList4<FileSystemDecoration> mountpoints = new StructList4<FileSystemDecoration>();
            // Vfs Node
            Directory directory;
            // Lock for the duration of tree traversal
            vfsLock.AcquireReaderLock(int.MaxValue);
            try
            {
                // Search for vfs node
                GetVfsDirectory(path, out directory, ref mountpoints, true);
            }
            finally
            {
                vfsLock.ReleaseReaderLock();
            }

            // Try to create direcotry with mounted filesystems
            for (int i = mountpoints.Count-1; i >= 0; i--)
            {
                var fs = mountpoints[i];
                // fs cannot open
                if (!fs.CanCreateDirectory()) continue;
                try
                {
                    fs.CreateDirectory(path);
                    return;
                }
                catch (NotSupportedException) { }
            }

            // Failed
            throw new NotSupportedException(nameof(CreateDirectory));
        }

        /// <summary>
        /// Delete a file or directory.
        /// 
        /// If <paramref name="path"/> is directory, then it should end with directory separator character '/', for example "dir/".
        /// 
        /// If <paramref name="recurse"/> is false and <paramref name="path"/> is a directory that is not empty, then <see cref="IOException"/> is thrown.
        /// If <paramref name="recurse"/> is true, then any file or directory in <paramref name="path"/> is deleted as well.
        /// </summary>
        /// <param name="path">path to a file or directory</param>
        /// <param name="recurse">if path refers to directory, recurse into sub directories</param>
        /// <exception cref="FileNotFoundException">The specified path is invalid.</exception>
        /// <exception cref="IOException">On unexpected IO error, or if <paramref name="path"/> refered to a directory that wasn't empty and <paramref name="recurse"/> is false, or trying to delete root when not allowed</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> contains invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support deleting files</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="path"/> refers to non-file device</exception>
        /// <exception cref="ObjectDisposedException"/>
        public void Delete(string path, bool recurse = false)
        {
            // Assert argument
            if (path == null) throw new ArgumentNullException(nameof(path));
            // Assert not disposed
            if (IsDisposing) throw new ObjectDisposedException(GetType().Name);

            // Stack of nodes that start with path and have a mounted filesystem
            StructList4<FileSystemDecoration> mountpoints = new StructList4<FileSystemDecoration>();
            // Lock for the duration of tree traversal
            vfsLock.AcquireReaderLock(int.MaxValue);
            try
            {
                // Vfs Node
                Directory directory;
                // Search for vfs node
                GetVfsDirectory(path, out directory, ref mountpoints, true);
            }
            finally
            {
                vfsLock.ReleaseReaderLock();
            }

            // Run delete through all components
            // Try to open with mounted filesystems
            bool supported = false;
            for (int i = mountpoints.Count - 1; i >= 0; i--)
            {
                var fs = mountpoints[i];
                // Cannot Delete
                if (!fs.CanDelete()) continue;

                try
                {
                    fs.Delete(path, recurse);
                    // We got something
                    return; 
                }
                catch (FileNotFoundException) { supported = true; }
                catch (DirectoryNotFoundException) { supported = true; }
                catch (NotSupportedException) { }
            }
            if (!supported) throw new NotSupportedException(nameof(Delete));
            throw new FileNotFoundException(path);
        }

        /// <summary>
        /// Set <paramref name="fileAttribute"/> on <paramref name="path"/>.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="fileAttribute"></param>
        /// <exception cref="FileNotFoundException"><paramref name="path"/> is not found</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid. For example, it's on an unmapped drive. Only thrown when setting the property value.</exception>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support browse</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"></exception>
        public void SetFileAttribute(string path, FileAttributes fileAttribute)
        {
            // Assert argument
            if (path == null) throw new ArgumentNullException(nameof(path));
            // Assert not disposed
            if (IsDisposing) throw new ObjectDisposedException(GetType().Name);

            // Stack of nodes that start with path and have a mounted filesystem
            StructList4<FileSystemDecoration> mountpoints = new StructList4<FileSystemDecoration>();
            // Lock for the duration of tree traversal
            vfsLock.AcquireReaderLock(int.MaxValue);
            try
            {
                // Vfs Node
                Directory directory;
                // Search for vfs node
                GetVfsDirectory(path, out directory, ref mountpoints, true);
            }
            finally
            {
                vfsLock.ReleaseReaderLock();
            }

            // Run SetFileSystem through all components
            bool supported = false;
            for (int i = mountpoints.Count - 1; i >= 0; i--)
            {
                var fs = mountpoints[i];
                // Cannot Delete
                if (!fs.CanSetFileAttribute()) continue;

                try
                {
                    fs.SetFileAttribute(path, fileAttribute);
                    // We got something
                    return;
                }
                catch (FileNotFoundException) { supported = true; }
                catch (DirectoryNotFoundException) { supported = true; }
                catch (NotSupportedException) { }
            }
            if (!supported) throw new NotSupportedException(nameof(SetFileAttribute));
            throw new FileNotFoundException(path);
        }


        /// <summary>
        /// Move/rename a file or directory. 
        /// 
        /// If <paramref name="srcPath"/> and <paramref name="dstPath"/> refers to a directory, then the path names 
        /// should end with directory separator character '/'.
        /// </summary>
        /// <param name="srcPath">old path of a file or directory</param>
        /// <param name="dstPath">new path of a file or directory</param>
        /// <exception cref="FileNotFoundException">The specified <paramref name="srcPath"/> is invalid.</exception>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException">path is null</exception>
        /// <exception cref="ArgumentException">path is an empty string (""), contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support renaming/moving files</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">path refers to non-file device, or an entry already exists at <paramref name="dstPath"/></exception>
        /// <exception cref="ObjectDisposedException"/>
        public void Move(string srcPath, string dstPath)
        {
            // Assert arguments
            if (srcPath == null) throw new ArgumentNullException(nameof(srcPath));
            if (dstPath == null) throw new ArgumentNullException(nameof(dstPath));
            // Assert not disposed
            if (IsDisposing) throw new ObjectDisposedException(GetType().Name);

            // Stack of nodes that start with path and have a mounted filesystem
            StructList4<FileSystemDecoration> mountpoints = new StructList4<FileSystemDecoration>();
            // Lock for the duration of tree traversal
            vfsLock.AcquireReaderLock(int.MaxValue);
            try
            {
                // Vfs Node
                Directory directory;
                // Search for vfs node
                GetVfsDirectory(srcPath, out directory, ref mountpoints, true);
            }
            finally
            {
                vfsLock.ReleaseReaderLock();
            }

            // Extract components from mounts, shuffle so that highest priority component is on first index 0
            StructList4<FileSystemDecoration.Component> components = new StructList4<FileSystemDecoration.Component>();
            for (int i = mountpoints.Count - 1; i >= 0; i--)
                foreach (var c in mountpoints[i].components.Array) components.Add(c);

            // Zero components
            if (components.Count == 0)
            {
                // Assert can move
                if (!this.CanMove) throw new NotSupportedException(nameof(Move));
                // Nothing to move
                throw new FileNotFoundException(srcPath);
            }

            // One component
            if (components.Count == 1)
            {
                // Get reference
                var component = components[0];
                // Assert can move
                if (!component.Option.CanMove) throw new NotSupportedException(nameof(Move));
                // Convert paths
                String componentSrcPath, componentDstPath;
                if (!component.Path.ParentToChild(srcPath, out componentSrcPath)) throw new FileNotFoundException(srcPath);
                if (!component.Path.ParentToChild(dstPath, out componentDstPath)) throw new FileNotFoundException(dstPath);
                // Move
                component.FileSystem.Move(componentSrcPath, componentDstPath);
                // Done
                return;
            }

            // Get parent path
            string newPathParent = PathEnumerable.GetParent(dstPath);
            FileSystemDecoration.Component srcComponent = null, dstComponent = null;
            string srcComponentPath = null, dstComponentPath = null;
            for (int i=0; i<components.Count; i++)
            {
                FileSystemDecoration.Component component = components[i];
                // Estimate if component suits as source of move op
                if (srcComponent == null && component.Path.ParentToChild(srcPath, out srcComponentPath))
                {
                    try
                    {
                        if (component.FileSystem.Exists(srcComponentPath)) srcComponent = component;
                    }
                    catch (NotSupportedException)
                    {
                        // We don't know if this is good source component
                    }
                }

                // Try converting path
                string dstParent;
                // Estimate if component suits as dst of move
                if (dstComponent == null && component.Path.ParentToChild(newPathParent, out dstParent) && component.Path.ParentToChild(dstPath, out dstComponentPath))
                {
                    try
                    {
                        IFileSystemEntry e = component.FileSystem.GetEntry(dstParent);
                        if (e != null && e.IsDirectory()) dstComponent = component;
                    }
                    catch (NotSupportedException)
                    {
                        // We don't know if this is good dst component
                    }
                }
            }

            // Found suitable components
            if (srcComponent != null && dstComponent != null)
            {
                // Move locally
                if (srcComponent.FileSystem.Equals(dstComponent.FileSystem) || dstComponent.FileSystem.Equals(srcComponent.FileSystem)) srcComponent.FileSystem.Move(srcComponentPath, dstComponentPath);
                // Copy+Delete
                else srcComponent.FileSystem.Transfer(srcComponentPath, dstComponent.FileSystem, dstComponentPath);
                return;
            }

            // Could not figure out from where to which, try each afawk (but not all permutations)
            bool supported = false;
            for (int i = 0; i < components.Count; i++)
            {
                FileSystemDecoration.Component component = components[i];
                // Estimate if component suits as source of move op
                if (srcComponent == null && !component.Path.ParentToChild(srcPath, out srcComponentPath)) continue;
                if (dstComponent == null && !component.Path.ParentToChild(dstPath, out dstComponentPath)) continue;

                FileSystemDecoration.Component sc = srcComponent ?? component, dc = dstComponent ?? component;
                try
                {
                    // Move locally
                    if (sc.FileSystem.Equals(dc.FileSystem) || dc.FileSystem.Equals(sc.FileSystem)) sc.FileSystem.Move(srcComponentPath, dstComponentPath);
                    // Copy+Delete
                    else sc.FileSystem.Transfer(srcComponentPath, dc.FileSystem, dstComponentPath);
                    return;
                }
                catch (FileNotFoundException) { supported = true; }
                catch (NotSupportedException) { }
            }
            // Failed
            if (!supported) throw new NotSupportedException(nameof(Move));
            throw new FileNotFoundException(srcPath);
        }

        /// <summary>
        /// Handle dispose
        /// </summary>
        /// <param name="disposeErrors"></param>
        protected override void InnerDispose(ref StructList4<Exception> disposeErrors)
        {

            // Gather observer node
            StructList12<ObserverHandle> handles = new StructList12<ObserverHandle>();
            observerLock.AcquireWriterLock(int.MaxValue);
            try
            {
                foreach (var n in observerRoot.Visit(false, true, true))
                {
                    var array = n.observers.Array;
                    foreach (var oh in array)
                    {
                        handles.Add(oh);
                        n.observers.Remove(oh);
                    }
                }
            } finally
            {
                observerLock.ReleaseWriterLock();
            }
            // Dispose gathered handles
            for (int i = 0; i < handles.Count; i++)
                try
                {
                    handles[i].Dispose();
                }
                catch (Exception e)
                {
                    disposeErrors.Add(e);
                }

            // Gather assigned filesystems
            StructList12<FileSystemDecoration> decorations = new StructList12<FileSystemDecoration>();
            vfsLock.AcquireWriterLock(int.MaxValue);
            try
            {
                foreach (var e in vfsRoot.Visit(false, true, true))
                    if (e.mount != null)
                        decorations.Add(e.mount);
                vfsRoot.children.Clear();
            }
            finally
            {
                vfsLock.ReleaseWriterLock();
            }

            for (int i = 0; i < decorations.Count; i++)
            {
                try
                {
                    decorations[i].Dispose();
                }
                catch (Exception e)
                {
                    disposeErrors.Add(e);
                }
            }
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
        public VirtualFileSystem AddDisposables(IEnumerable disposables)
        {
            ((IDisposeList)this).AddDisposables(disposables);
            return this;
        }

        /// <summary>
        /// Add mounted <see cref="IFileSystem"/>s to be disposed along with this decoration.
        /// 
        /// Disposes only those filesystems that were mounted at the time of vfs's dispose.
        /// </summary>
        /// <returns>self</returns>
        public VirtualFileSystem AddMountsToBeDisposed()
        {
            AddDisposeAction(vfs =>
            {
                StructList1<Exception> errors = new StructList1<Exception>();

                // Get disposables
                StructList4<IDisposable> disposables = new StructList4<IDisposable>();
                vfsLock.AcquireReaderLock(int.MaxValue);
                try
                {
                    foreach (var node in vfsRoot.Visit(false, true, true))
                        foreach (IDisposable d in node.mount.DisposableDecorees)
                            disposables.Add(d);
                }
                finally
                {
                    vfsLock.ReleaseReaderLock();
                }

                if (disposables.Count == 0) return;
                if (disposables.Count == 1) { disposables[0].Dispose(); return; }

                // Dispose each, aggregate errors
                for (int i=0; i<disposables.Count; i++)
                {
                    try
                    {
                        disposables[i].Dispose();
                    }
                    catch (Exception e)
                    {
                        errors.Add(e);
                    }
                }

                // Forward errors
                if (errors.Count > 0) throw new AggregateException(errors.ToArray());
            });
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
        public VirtualFileSystem RemoveDisposables(IEnumerable disposables)
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
