// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           28.9.2019
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
        /// Reader writer lock for modifying vfs directory structure. 
        /// </summary>
        ReaderWriterLock vfsLock = new ReaderWriterLock();

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

            // Stack of nodes that start with path and have a mounted filesystem
            StructList4<FileSystemDecoration> mountPoints = new StructList4<FileSystemDecoration>();
            // Snapshot of vfs entries
            StructList4<IFileSystemEntry> vfsEntries = new StructList4<IFileSystemEntry>();
            // Lock for the duration of tree traversal
            vfsLock.AcquireReaderLock(int.MaxValue);
            try
            {
                // Start
                Directory cursor = root, finalDirectory = root;
                // Path '/' splitter, enumerates name strings from root towards tail
                PathEnumerator enumr = new PathEnumerator(path, true);
                // Special case, root
                if (path == "")
                {
                    // Add to stack
                    if (cursor.mount != null) mountPoints.Add(cursor.mount);
                }
                // Traverse path in name parts
                else while (enumr.MoveNext())
                    {
                        // Add to stack
                        if (cursor.mount != null) mountPoints.Add(cursor.mount);
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
                // Add vfs entries
                if (finalDirectory != null) foreach (var kp in finalDirectory.contents) vfsEntries.Add(kp.Value.Entry);
            }
            finally
            {
                vfsLock.ReleaseReaderLock();
            }
            // Found nothing.
            if (mountPoints.Count == 0 && vfsEntries.Count == 0) throw new DirectoryNotFoundException(path);
            // Return vfs contents
            if (mountPoints.Count == 0 && vfsEntries.Count > 0) return vfsEntries.ToArray();
            // Return already decorated contents
            if (mountPoints.Count > 0 && vfsEntries.Count == 0) mountPoints[0].Browse(path);

            // Create union of mountpoints and final directory. Remove overlapping content if same name. Priority: vfs, mountpoints
            // Estimation of entry count
            int entryCount = vfsEntries.Count;
            // Browse each decoration
            StructList4<IFileSystemEntry[]> entryArrays = new StructList4<IFileSystemEntry[]>();
            for (int i = 0; i < mountPoints.Count; i++)
            {
                try
                {
                    entryArrays.Add(mountPoints[i].Browse(path));
                    entryCount += entryArrays[i].Length;
                } catch (DirectoryNotFoundException fnf)
                {
                    // Continue
                }
            }

            // Create hashset for removing overlapping entry names
            HashSet<string> filenames = new HashSet<string>(StringComparer.InvariantCulture);
            // Container for result
            List<IFileSystemEntry> entries = new List<IFileSystemEntry>(entryCount/*most likely count and max count*/);
            // Add vfs
            for (int i = 0; i < vfsEntries.Count; i++)
            {
                // Get mount entry
                IFileSystemEntry e = vfsEntries[i];
                // Remove already existing entry
                if (filenames.Add(e.Name)) entries.Add(e);
            }
            // Add entries from mounted filesystems
            for (int i = 0; i < entryArrays.Count; i++)
            {
                foreach (IFileSystemEntry e in entryArrays[i])
                {
                    // Remove already existing entry
                    if (filenames != null && !filenames.Add(e.Name)) continue;
                    // Add to list
                    entries.Add(e);
                }
            }
            // Return
            return entries.ToArray();
        }

        /// <inheritdoc/>
        public IFileSystemEntry GetEntry(string path)
        {
            // Assert argument
            if (path == null) throw new ArgumentNullException(nameof(path));
            // Assert not disposed
            if (IsDisposing) throw new ObjectDisposedException(GetType().Name);

            // Stack of nodes that start with path and have a mounted filesystem
            StructList4<FileSystemDecoration> mountPoints = new StructList4<FileSystemDecoration>();
            // Lock for the duration of tree traversal
            vfsLock.AcquireReaderLock(int.MaxValue);
            try
            {
                // Special case, root
                if (path == "") return root.Entry;
                // Start
                Directory cursor = root, finalDirectory = root;
                // Path '/' splitter, enumerates name strings from root towards tail
                PathEnumerator enumr = new PathEnumerator(path, true);
                // Traverse path in name parts
                while (enumr.MoveNext())
                {
                    // Add to stack
                    if (cursor.mount != null) mountPoints.Add(cursor.mount);
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
                    // Move cursor
                    finalDirectory = cursor;
                }
                // Return vfs entry
                if (finalDirectory != null) return finalDirectory.Entry;
                // Try to get entry from mounted filesystems
                for (int i = 0; i < mountPoints.Count; i++)
                {
                    IFileSystemEntry e = mountPoints[i].GetEntry(path);
                    if (e != null) return e;
                }
                // Nothing
                return null;
            }
            finally
            {
                vfsLock.ReleaseReaderLock();
            }
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
            public IEnumerable<Directory> VisitDecedents()
            {
                Queue<Directory> queue = new Queue<Directory>();
                queue.Enqueue(this);
                while (queue.Count > 0)
                {
                    Directory n = queue.Dequeue();
                    yield return n;
                    foreach (Directory c in n.contents.Values)
                        queue.Enqueue(c);
                }
            }

            /// <summary>
            /// Visits parents, excluding self.
            /// </summary>
            /// <returns></returns>
            public IEnumerable<Directory> VisitParents()
            {
                Directory cursor = parent;
                while (cursor != null)
                {
                    yield return cursor;
                    cursor = cursor.parent;
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
         * Mounting and unmounting creates virtual folders, which create Create and Delete events.
         */

        /// <summary>
        /// Reader writer lock for modifying observer tree.
        /// </summary>
        ReaderWriterLock observerLock = new ReaderWriterLock();

        /// <summary>
        /// Observer tree root. Read and modified only under <see cref="observerLock"/>.
        /// </summary>
        ObserverNode observerRoot = new ObserverNode(null, "");

        /// <summary>
        /// Get or create observer node.
        /// </summary>
        /// <param name="observerPath">the stem part from glob pattern</param>
        /// <param name="handleToAdd">(optional) Observer handle to add while in lock</param>
        /// <returns>observer node</returns>
        /// <exception cref="DirectoryNotFoundException">if refers beyond parent with ".."</exception>
        ObserverNode GetOrCreateObserverNode(string observerPath, ObserverHandle handleToAdd)
        {
            // Assert arguments
            if (observerPath == null) throw new ArgumentNullException(nameof(observerPath));
            // Special case, root
            if (observerPath == "") return observerRoot;
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
        /// 
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="observer"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public override IFileSystemObserver Observe(string filter, IObserver<IFileSystemEvent> observer, object state = null)
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
            ObserverHandle adapter = new ObserverHandle(null /*set below*/, this, filter, observer, state);
            // Add to observer tree
            ObserverNode node = GetOrCreateObserverNode(info.Stem, adapter);

            try
            {
                // Create observers in mounted filesystems and start forwarding events
                // Lock for the duration of tree traversal
                vfsLock.AcquireReaderLock(int.MaxValue);
                try
                {
                    // Start
                    Directory cursor = root, finalCursor = root;
                    // Path '/' splitter, enumerates name strings from root towards tail
                    PathEnumerator enumr = new PathEnumerator(info.Stem, true);
                    // Traverse path in name parts
                    if (info.Stem != "") while (enumr.MoveNext())
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
                            if (!cursor.contents.TryGetValue(name, out cursor)) break;
                            // Move finalCursor
                            finalCursor = cursor;
                        }

                    // Test if any mount in child tree intersects with filter
                    foreach (Directory d in finalCursor.VisitDecedents().Concat(finalCursor.VisitParents()))
                    {
                        // No mounts
                        if (d.mount == null) continue;
                        // Get component filesystems
                        FileSystemDecoration.Component[] components = d.mount.componentList.Array;
                        // No components
                        if (components.Length == 0) continue;
                        // Test if filter intersects with the mount
                        string intersection = finalCursor.IsParentOf(d) ? GlobPatternSet.Intersection(d.Path + "**", filter) : filter;
                        // No intersection
                        if (intersection == null) continue;

                        // Observe each component
                        foreach (FileSystemDecoration.Component component in components)
                        {
                            // Assert can observe
                            if (!component.Option.CanObserve) continue;
                            // Convert Path
                            String childPath, stem;
                            if (!component.Path.ParentToChild(intersection, out childPath) || !component.Path.ParentToChild(new GlobPatternInfo(intersection).Stem, out stem)) continue;
                            try
                            {
                                // Entry doesn't exist.
                                //if (component.FileSystem.CanGetEntry() && !component.FileSystem.Exists(stem)) continue;
                                // Try Observe
                                IDisposable disposable = component.FileSystem.Observe(childPath, adapter, new ObserverDecorator.StateInfo(component.Path, component));
                                // Attach disposable
                                ((IDisposeList)adapter).AddDisposable(disposable);
                            }
                            catch (NotSupportedException) { }
                            catch (ArgumentException) { } // FileSystem.PatternObserver throws directory is not found, TODO create contract for proper exception
                        }
                    }

                    // Send IFileSystemEventStart
                    observer.OnNext(adapter);
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
            catch (Exception) when (DisposeObserver(adapter)) { /*Never goes here*/ throw new Exception(); }
            finally
            {
            }
        }

        static bool DisposeObserver(ObserverHandle handle)
        {
            handle?.Dispose();
            return false;
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

            /// <summary>
            /// Create adapter observer.
            /// </summary>
            /// <param name="observerNode">(optional) node where handle is placed in the tree. Can be assigned later</param>
            /// <param name="sourceFileSystem">File system to show as the source of forwarded events (in <see cref="IFileSystemEvent.Observer"/>)</param>
            /// <param name="filter"></param>
            /// <param name="observer">The observer were decorated events are forwarded to</param>
            /// <param name="state"></param>
            /// then diposes this object and sends <see cref="IObserver{T}.OnCompleted"/> to <see cref="Observer"/>.</param>
            public ObserverHandle(ObserverNode observerNode, IFileSystem sourceFileSystem, string filter, IObserver<IFileSystemEvent> observer, object state) : base(sourceFileSystem, filter, observer, state, false)
            {
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
                var _observerNode = Interlocked.CompareExchange(ref observerNode, null, observerNode);
                if (_observerNode!=null) _observerNode.observers.Remove(this);
                base.InnerDispose(ref errors);

                // TODO Prune observer tree from leafs
            }

        }


        /// <summary>
        /// Node for observers. Node represents a path structure of the glob pattern.
        /// Observer is placed on a node that represents the the stem part of <see cref="GlobPatternInfo"/>.
        /// Root node represents "" stem.
        /// 
        /// Observer tree is read and modified only under <see cref="observerLock".
        /// </summary>
        protected internal class ObserverNode
        {
            /// <summary>Name of the node.</summary>
            protected internal string name;
            /// <summary>Parent node</summary>
            protected internal ObserverNode parent;
            /// <summary>Files and directories. Lazy construction. Reads and modifications under parent's m_lock.</summary>
            protected internal Dictionary<StringSegment, ObserverNode> children = new Dictionary<StringSegment, ObserverNode>();
            /// <summary>Observers that are on this node.</summary>
            protected internal CopyOnWriteList<ObserverHandle> observers = new CopyOnWriteList<ObserverHandle>();

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
            ObserverHandle[] observers = new ObserverHandle[0]; // <- TODO UPdate to new observer tree

            // Write Lock
            vfsLock.AcquireWriterLock(int.MaxValue);
            try
            {
                // Follow path and get-or-create nodes
                Directory cursor = root;
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
                        if (!cursor.contents.TryGetValue(name, out child))
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
                        else
                        {
                            cursor = child;
                        }
                    }

                // New mount
                if (cursor.mount == null)
                {
                    // Create decoration filesystem (or null)
                    cursor.mount =
                        filesystems == null || filesystems.Length == 0 ? new FileSystemDecoration(filesystem: null, option: null) :
                        filesystems.Length == 1 ? new FileSystemDecoration(this, path, filesystems[0].filesystem, filesystems[0].mountOption) :
                        new FileSystemDecoration(this, filesystems.Select(p => (path, p.filesystem, p.mountOption)).ToArray());

                    // TODO Events
                }
                else
                // Replace mount
                {
                    cursor.mount.SetComponents(filesystems.Select(p => (path, p.filesystem, p.mountOption)).ToArray());
                    // TODO Events
                }
            }
            finally
            {
                vfsLock.ReleaseWriterLock();
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
            vfsLock.AcquireReaderLock(int.MaxValue);
            try
            {
                List<IFileSystemEntryMount> result = new List<IFileSystemEntryMount>();
                foreach (Directory node in root.VisitDecedents()) result.Add(node.Entry);
                return result.ToArray();
            }
            finally
            {
                vfsLock.ReleaseReaderLock();
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
            ObserverHandle[] observers = new ObserverHandle[0]; // <- TODO UPdate to new observer tree

            // Write Lock
            vfsLock.AcquireWriterLock(int.MaxValue);
            try
            {
                // Follow path and get-or-create nodes
                Directory cursor = root;
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
                        if (!cursor.contents.TryGetValue(name, out cursor)) throw new DirectoryNotFoundException(path.Substring(0, name.Length));
                    }

                // Disconnect from parent, if no children
                while (cursor != null && cursor.contents.Count == 0 && cursor.parent != null)
                {
                    // Remove from parent
                    cursor.parent.contents.Remove(new StringSegment(cursor.name));
                    cursor.parent.lastModified = now;
                    // Dispose decoration
                    cursor.mount?.Dispose();

                    // TODO events

                    // Move towards parent
                    cursor = cursor.parent;
                }

            }
            finally
            {
                vfsLock.ReleaseWriterLock();
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
            StructList12<FileSystemDecoration> decorations = new StructList12<FileSystemDecoration>();
            vfsLock.AcquireWriterLock(int.MaxValue);
            try
            {
                foreach (var e in root.VisitDecedents())
                    if (e.mount != null)
                        decorations.Add(e.mount);
                root.contents.Clear();
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
