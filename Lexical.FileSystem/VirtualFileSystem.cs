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
            StructList4<FileSystemDecoration> mountPoints = new StructList4<FileSystemDecoration>();
            // Snapshot of vfs entries
            StructList4<IFileSystemEntry> vfsEntries = new StructList4<IFileSystemEntry>();
            // Lock for the duration of tree traversal
            vfsLock.AcquireReaderLock(int.MaxValue);
            try
            {
                // Start
                Directory cursor = vfsRoot, finalDirectory = vfsRoot;
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
                        if (!cursor.children.TryGetValue(name, out cursor)) { finalDirectory = null; break; }
                        // Move final down
                        finalDirectory = cursor;
                    }
                // Add vfs entries
                if (finalDirectory != null) foreach (var kp in finalDirectory.children) vfsEntries.Add(kp.Value.Entry);
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
                } catch (DirectoryNotFoundException)
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
                if (path == "") return vfsRoot.Entry;
                // Start
                Directory cursor = vfsRoot, finalDirectory = vfsRoot;
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
                    if (!cursor.children.TryGetValue(name, out cursor)) { finalDirectory = null; break; }
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
                    observerRoot.observers.Add(handleToAdd);
                    handleToAdd.observerNode = observerRoot;
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
                        foreach (ObserverHandle h in observerNode.observers) observers.Add(h);

                if (foundAtPath && atDecendents)
                    foreach (ObserverNode n in observerNode.Visit(false, false, true))
                        foreach (ObserverHandle h in observerNode.observers) observers.Add(h);
            }
            finally
            {
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
            ObserverHandle adapter = new ObserverHandle(this, null /*set below*/, this, filter, observer, state);
            // Add to observer tree
            GetOrCreateObserverNode(info.Stem, adapter);
            // Send IFileSystemEventStart, must be sent before subscribing forwarders
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
                        FileSystemDecoration.Component[] components = d.mount.componentList.Array;
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
                                IDisposable disposable = component.FileSystem.Observe(childPath, adapter, new ObserverDecorator.StateInfo(component.Path, component));
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
            public ObserverHandle(VirtualFileSystem vfs, ObserverNode observerNode, IFileSystem sourceFileSystem, string filter, IObserver<IFileSystemEvent> observer, object state) : base(sourceFileSystem, filter, observer, state, false)
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
            protected internal CopyOnWriteList<ObserverHandle> observers = new CopyOnWriteList<ObserverHandle>();
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
            // vfs directories created, for events
            StructList4<string> vfsDirectoriesCreated = new StructList4<string>();
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
                if (cursor.mount == null) cursor.mount = new FileSystemDecoration(this, new (string, IFileSystem, IFileSystemOption)[0]);
                // Set components
                cursor.mount.SetComponents(ref componentsAdded, ref componentsRemoved, ref componentsReused, filesystems.Select(p => (path, p.filesystem, p.mountOption)).ToArray());
            }
            finally
            {
                vfsLock.ReleaseWriterLock();
            }

            // Process events
            if (vfsDirectoriesCreated.Count > 0 || componentsAdded.Count > 0 || componentsRemoved.Count > 0)
            {
                // Datetime
                now = DateTimeOffset.UtcNow;
                StructList12<IFileSystemEvent> events = new StructList12<IFileSystemEvent>();
                StructList12<ObserverHandle> observers = new StructList12<ObserverHandle>();

                // VFS Directory create events
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

                if (componentsAdded.Count>0 || componentsRemoved.Count > 0)
                {
                    GetObserverHandles(path, true, true, true, ref observers);
                    for (int i=0; i<observers.Count; i++)
                    {
                        ObserverHandle observer = observers[i];

                        // Added filesystems
                        for (int j=0; j<componentsAdded.Count; j++)
                        {
                            FileSystemDecoration.Component c = componentsAdded[j];

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
                return vfsRoot.Visit(false, true, true).Select(n => n.Entry).ToArray();
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

                // Disconnect from parent, if no children
                while (cursor != null && cursor.children.Count == 0 && cursor.parent != null)
                {
                    // Remove from parent
                    cursor.parent.children.Remove(new StringSegment(cursor.name));
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
            if (events.Count > 0) DispatchEvents(ref events);

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
