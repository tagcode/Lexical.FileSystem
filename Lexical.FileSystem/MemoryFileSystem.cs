// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Internal;
using Lexical.FileSystem.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Lexical.FileSystem
{
    /// <summary>
    /// In-memory filesystem
    /// </summary>
    public class MemoryFileSystem : FileSystemBase, IFileSystemBrowse, IFileSystemCreateDirectory, IFileSystemDelete, IFileSystemObserve, IFileSystemMove, IFileSystemOpen, IFileSystemDisposable
    {
        /// <summary>
        /// Root directory
        /// </summary>
        Directory root;

        /// <summary>
        /// Reader writer lock.
        /// </summary>
        ReaderWriterLockSlim m_lock = new ReaderWriterLockSlim();

        /// <summary>
        /// List of observers.
        /// </summary>
        CopyOnWriteList<ObserverHandle> observers = new CopyOnWriteList<ObserverHandle>();

        /// <summary>
        /// A snapshot of observers.
        /// </summary>
        ObserverHandle[] Observers => observers.Array;

        /// <summary>
        /// Task-factory that is used for sending events.
        /// If factory is set to null, then events are processed in the current thread.
        /// </summary>
        TaskFactory taskFactory;

        /// <inheritdoc/>
        public override FileSystemFeatures Features => FileSystemFeatures.CaseSensitive;
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
        /// <summary>Delegate that processes events</summary>
        Action<object> processEventsAction;

        /// <summary>
        /// Create new in-memory filesystem.
        /// </summary>
        public MemoryFileSystem()
        {
            root = new Directory(this, null, "", DateTimeOffset.UtcNow);
            this.taskFactory = Task.Factory;
            this.processEventsAction = processEvents;
        }

        /// <summary>
        /// Set <paramref name="taskFactory"/> to be used for handling observer events.
        /// 
        /// If <paramref name="taskFactory"/> is null, then events are processed in the threads
        /// that make modifications to memory filesytem.
        /// </summary>
        /// <param name="taskFactory">(optional) factory that handles observer events</param>
        /// <returns>memory filesystem</returns>
        public MemoryFileSystem SetTaskFactory(TaskFactory taskFactory)
        {
            this.taskFactory = taskFactory;
            return this;
        }

        /// <summary>
        /// Browse a directory for file and subdirectory entries.
        /// </summary>
        /// <param name="path">path to a directory or to a single file, "" is root, separator is "/"</param>
        /// <returns>a snapshot of file and directory entries</returns>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support browse</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        public IFileSystemEntry[] Browse(string path)
        {
            // Assert not disposed
            if (IsDisposing) throw new ObjectDisposedException(GetType().Name);
            // Read lock
            m_lock.EnterReadLock();
            try
            {
                // Find entry
                Node node = GetNode(path);
                // Directory
                if (node is Directory dir_)
                {
                    // List entries
                    int c = dir_.contents.Count;
                    IFileSystemEntry[] array = new IFileSystemEntry[c];
                    int i = 0;
                    foreach (Node e in dir_.contents.Values) array[i++] = e.CreateEntry();
                    return array;
                }
                else
                // File
                if (node is File)
                {
                    return new IFileSystemEntry[] { node.CreateEntry() };
                }

                // Entry was not found, was not dir or file
                throw new DirectoryNotFoundException(path);
            }
            finally
            {
                m_lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Get entry of a single file or directory.
        /// </summary>
        /// <param name="path">path to a directory or to a single file, "" is root, separator is "/"</param>
        /// <returns>entry, or null if entry is not found</returns>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support exists</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        public IFileSystemEntry GetEntry(string path)
        {
            // Assert not disposed
            if (IsDisposing) throw new ObjectDisposedException(GetType().Name);
            // Read lock
            m_lock.EnterReadLock();
            try
            {
                // Find entry
                Node node = GetNode(path);
                // Not found
                if (node == null) throw new FileNotFoundException(path);
                // IFileSystemEntry
                return node.CreateEntry();
            }
            finally
            {
                m_lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Create a directory, or multiple cascading directories.
        /// 
        /// If directory at <paramref name="path"/> already exists, then returns without exception.
        /// </summary>
        /// <param name="path">Relative path to file. Directory separator is "/". The root is without preceding slash "", e.g. "dir/dir2"</param>
        /// <returns>true if directory exists after the method, false if directory doesn't exist</returns>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is an empty string (""), contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support create directory</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        public void CreateDirectory(string path)
        {
            // Assert not disposed
            if (IsDisposing) throw new ObjectDisposedException(GetType().Name);
            // Datetime
            DateTimeOffset time = DateTimeOffset.UtcNow;
            // Queue of events
            StructList12<IFileSystemEvent> events = new StructList12<IFileSystemEvent>();
            // Take snapshot of observers
            ObserverHandle[] observers = this.Observers;
            // Write lock
            m_lock.EnterWriteLock();
            try
            {
                Node node = root;
                PathEnumerator enumr = new PathEnumerator(path);
                while (enumr.MoveNext())
                {
                    // Get entry under lock.
                    if (node is Directory dir)
                    {
                        // No child by name
                        if (!dir.contents.TryGetValue(enumr.Current, out node))
                        {
                            string name = enumr.Current;
                            // Create child directory
                            Directory child = new Directory(this, dir, name, DateTimeOffset.UtcNow);
                            // Add event about parent modified and child created
                            if (observers != null)
                                foreach (ObserverHandle observer in observers)
                                {                                    
                                    if (observer.Qualify(child.Path)) events.Add(new FileSystemEventCreate(observer, time, child.Path));
                                }
                            // Update time of parent
                            node.lastModified = time;
                            // Add child to parent
                            ((Directory)node).contents[enumr.Current] = child;
                        }
                    }
                    else
                    {
                        // Parent is a file and cannot contain futher subnodes.
                        throw new InvalidOperationException("Cannot create file under a file ("+node.Path+")");
                    }
                }
            }
            finally
            {
                m_lock.ExitWriteLock();
            }

            // Send events
            if (events.Count>0) SendEvents(ref events);
        }

        /// <summary>
        /// Delete a file or directory.
        /// 
        /// If <paramref name="recursive"/> is false and <paramref name="path"/> is a directory that is not empty, then <see cref="IOException"/> is thrown.
        /// If <paramref name="recursive"/> is true, then any file or directory in <paramref name="path"/> is deleted as well.
        /// </summary>
        /// <param name="path">path to a file or directory</param>
        /// <param name="recursive">if path refers to directory, recurse into sub directories</param>
        /// <exception cref="FileNotFoundException">The specified path is invalid.</exception>
        /// <exception cref="IOException">On unexpected IO error, or if <paramref name="path"/> refered to a directory that wasn't empty and <paramref name="recursive"/> is false</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is an empty string (""), contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support deleting files</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="path"/> refers to non-file device</exception>
        /// <exception cref="ObjectDisposedException"/>
        public void Delete(string path, bool recursive = false)
        {
            // Assert not disposed
            if (IsDisposing) throw new ObjectDisposedException(GetType().Name);
            // Datetime
            DateTimeOffset time = DateTimeOffset.UtcNow;
            // Queue of events
            StructList12<IFileSystemEvent> events = new StructList12<IFileSystemEvent>();
            // Take snapshot of observers
            ObserverHandle[] observers = this.Observers;
            // Write lock
            m_lock.EnterWriteLock();
            try
            {
                // Find file or directory
                Node node = GetNode(path);
                // Not found
                if (node == null) throw new FileNotFoundException(path);
                // Assert not root
                if (node.path == "") throw new InvalidOperationException("Cannot delete root.");
                // Get parent
                Directory parent = node.parent;
                // Parent not found?
                if (parent == null) throw new FileNotFoundException(path);
                // Delete file or empty dir
                if ((node is File) || (node is Directory directory && directory.contents.Count == 0))
                {
                    // Remove from parent
                    parent.contents.Remove(new StringSegment(node.name));
                    // Update parent datetime
                    parent.lastModified = time;
                    // Create delete event
                    if (observers != null)
                        foreach (ObserverHandle observer in observers)
                            if (observer.Qualify(node.path)) events.Add(new FileSystemEventDelete(observer, time, node.path));
                    // Mark file/dir deleted
                    node.isDeleted = true;
                }
                // Non-empty directory
                else if (node is Directory dir)
                {
                    // Assert recursive is 'true'.
                    if (!recursive) throw new InvalidOperationException("Cannot delete non-empty directory (" + path + ")");
                    // Update parent datetime
                    parent.lastModified = time;
                    // Visit whole tree and delete everything
                    foreach (Node n in dir.VisitTree())
                    {
                        // Create delete event
                        if (observers != null)
                            foreach (ObserverHandle observer in observers)
                                if (observer.Qualify(n.path)) events.Add(new FileSystemEventDelete(observer, time, n.path));
                        // Mark deleted
                        n.isDeleted = true;
                    }
                    // Wipe parent
                    parent.contents.Clear();
                }
                else
                {
                    // Should not go here
                    throw new InvalidOperationException("Unexpected state");
                }
            }
            finally
            {
                m_lock.ExitWriteLock();
            }

            // Send events
            if (events.Count > 0) SendEvents(ref events);
        }

        /// <summary>
        /// Try to move/rename a file or directory.
        /// </summary>
        /// <param name="oldPath">old path of a file or directory</param>
        /// <param name="newPath">new path of a file or directory</param>
        /// <exception cref="FileNotFoundException">The specified <paramref name="oldPath"/> is invalid.</exception>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="ArgumentNullException">path is null</exception>
        /// <exception cref="ArgumentException">path is an empty string (""), contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support renaming/moving files</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="InvalidOperationException">path refers to non-file device, or an entry already exists at <paramref name="newPath"/></exception>
        /// <exception cref="ObjectDisposedException"/>
        public void Move(string oldPath, string newPath)
        {
            // Assert not disposed
            if (IsDisposing) throw new ObjectDisposedException(GetType().Name);
            // Datetime
            DateTimeOffset time = DateTimeOffset.UtcNow;
            // Queue of events
            StructList12<IFileSystemEvent> events = new StructList12<IFileSystemEvent>();
            // Take snapshot of observers
            ObserverHandle[] observers = this.Observers;
            // Write lock
            m_lock.EnterWriteLock();
            try
            {
                // Find paths
                Node oldNode = GetNode(oldPath), newNode = GetNode(newPath);
                // Not found
                if (oldNode == null) throw new FileNotFoundException(oldPath);
                // Assert not root
                if (oldNode.path == "") throw new InvalidOperationException("Cannot move root.");
                // Target file already exists
                if (newNode != null) throw new InvalidOperationException(newPath + " already exists");
                // Get parents
                Directory oldParent = oldNode.parent;
                // Parent not found
                if (oldParent == null) throw new FileNotFoundException(oldPath);

                // Split newPath into parent directory and name.
                Directory dir = root, newParent = null;
                string newName = null;
                // Path '/' splitter, enumerates name strings from root towards tail
                PathEnumerator enumr = new PathEnumerator(newPath);
                // Get next name from the path
                while (enumr.MoveNext())
                {
                    // Find child
                    Node child;
                    if (dir.contents.TryGetValue(enumr.Current, out child))
                    // Name matched an entry
                    {
                        newParent = null;
                        newName = null;
                        // Entry is directory
                        if (child is Directory d) dir = d;
                        // Entry is file
                        else throw new InvalidOperationException("Cannot move over existing file "+child.Path);
                    } else
                    // name did not match anything in the directory
                    {
                        newName = enumr.Current;
                        newParent = dir;
                    }
                }
                // Unexpected error
                if (newParent == null || newName == null) throw new InvalidOperationException(newPath);

                // Nothing to do (check this after proper asserts)
                if (oldPath == newPath) return;

                // Rename
                oldNode.name = newName;
                // Move folder
                if (oldNode.parent != newParent) oldNode.parent = newParent;
                // Visit tree
                foreach (Node c in oldNode.VisitTree())
                {
                    // Reset cache
                    c.path = null;
                    // Create event
                    /*
                    if (observers != null)
                        foreach (ObserverHandle observer in observers)
                            if (observer.Qualify(n.path))
                                events.Add(new FileSystemEventRename(observer, time, n.path));
                                */

                }
                // Change directory times
                oldParent.lastModified = time;
                newParent.lastModified = time;
            }
            finally
            {
                m_lock.ExitWriteLock();
            }

            // Send events
            if (events.Count > 0) SendEvents(ref events);
        }

        /// <inheritdoc/>
        public Stream Open(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
        {
            // Assert not disposed
            if (IsDisposing) throw new ObjectDisposedException(GetType().Name);
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public IFileSystemObserverHandle Observe(string filter, IObserver<IFileSystemEvent> observer, object state = null)
        {
            // Assert not disposed
            if (IsDisposing) throw new ObjectDisposedException(GetType().Name);
            ObserverHandle handle = new ObserverHandle(this, filter, observer, state);
            observers.Add(handle);
            return handle;
        }

        /// <summary>
        /// Get node by <paramref name="path"/>.
        /// Caller must ensure that lock is acquired.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>node or null</returns>
        Node GetNode(string path)
        {
            Node node = root;
            // Path '/' splitter, enumerates name strings from root towards tail
            PathEnumerator enumr = new PathEnumerator(path);
            // Get next name from the path
            while (enumr.MoveNext())
            {
                // "" Represents current dir
                //if (StringSegment.Comparer.Instance.Equals(enumr.Current, StringSegment.Empty)) continue;

                // Get entry under lock.
                if (node is Directory dir)
                {
                    // Failed to find child entry
                    if (!dir.contents.TryGetValue(enumr.Current, out node)) return null;
                }
                else
                {
                    // Parent is a file and cannot contain further subentries.
                    return null;
                }
            }
            return node;
        }

        /// <summary>
        /// Send <paramref name="events"/> to observers with <see cref="taskFactory"/>.
        /// If <see cref="taskFactory"/> is null, then sends events in the running thread.
        /// </summary>
        /// <param name="events"></param>
        void SendEvents(ref StructList12<IFileSystemEvent> events)
        {
            // Nothing to do
            if (events.Count == 0) return;
            // Get taskfactory
            TaskFactory _taskFactory = taskFactory;
            // Send events in this thread
            if (_taskFactory == null)
            {
                // Errors
                StructList4<Exception> errors = new StructList4<Exception>();
                foreach (IFileSystemEvent e in events)
                {
                    try
                    {
                        e.Observer.Observer.OnNext(e);
                    }
                    catch (Exception error)
                    {
                        // Bumerang error
                        try
                        {
                            e.Observer.Observer.OnError(error);
                        }
                        catch (Exception error2)
                        {
                            // 
                            errors.Add(error2);
                        }
                    }
                }
                if (errors.Count > 0) throw new AggregateException(errors.ToArray());
            }
            else
            // Create task that processes events.
            {
                _taskFactory.StartNew(processEventsAction, events.ToArray());
            }
        }

        /// <summary>
        /// Forward events to observers in the running thread.
        /// </summary>
        /// <param name="events">IFileSystemEvent[]</param>
        static void processEvents(object events)
        {
            // Errors
            StructList4<Exception> errors = new StructList4<Exception>();
            foreach (IFileSystemEvent e in (IFileSystemEvent[])events)
            {
                try
                {
                    e.Observer.Observer.OnNext(e);
                }
                catch (Exception error)
                {
                    // Bumerang error
                    try
                    {
                        e.Observer.Observer.OnError(error);
                    }
                    catch (Exception error2)
                    {
                        // 
                        errors.Add(error2);
                    }
                }
            }
            if (errors.Count > 0) throw new AggregateException(errors.ToArray());
        }

        /// <summary>
        /// Handle dispose
        /// </summary>
        /// <param name="disposeErrors"></param>
        protected override void InnerDispose(ref StructList4<Exception> disposeErrors)
        {
            m_lock.Dispose();
        }

        /// <summary>
        /// Add <paramref name="disposable"/> to list of objects to be disposed along with the system.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns>filesystem</returns>
        public MemoryFileSystem AddDisposable(object disposable)
        {
            ((IDisposeList)this).AddDisposable(disposable);
            return this;
        }

        /// <summary>
        /// Add <paramref name="disposables"/> to list of objects to be disposed along with the system.
        /// </summary>
        /// <param name="disposables"></param>
        /// <returns>filesystem</returns>
        public MemoryFileSystem AddDisposables(IEnumerable<object> disposables)
        {
            ((IDisposeList)this).AddDisposables(disposables);
            return this;
        }

        /// <summary>
        /// Remove <paramref name="disposable"/> from dispose list.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns></returns>
        public MemoryFileSystem RemoveDisposable(object disposable)
        {
            ((IDisposeList)this).RemoveDisposable(disposable);
            return this;
        }

        /// <summary>
        /// Remove <paramref name="disposables"/> from dispose list.
        /// </summary>
        /// <param name="disposables"></param>
        /// <returns></returns>
        public MemoryFileSystem RemoveDisposables(IEnumerable<object> disposables)
        {
            ((IDisposeList)this).RemoveDisposables(disposables);
            return this;
        }

        /// <summary>
        /// Observer
        /// </summary>
        class ObserverHandle : FileSystemObserverHandleBase
        {
            /// <summary>
            /// Filter pattern that is used for filtering events by path.
            /// </summary>
            Regex filterPattern;

            /// <summary>
            /// Create new observer.
            /// </summary>
            /// <param name="fileSystem"></param>
            /// <param name="filter">path filter as glob pattenrn. "*" any sequence of charaters within a directory, "**" any sequence of characters, "?" one character. E.g. "**/*.txt"</param>
            /// <param name="observer"></param>
            /// <param name="state"></param>
            public ObserverHandle(MemoryFileSystem fileSystem, string filter, IObserver<IFileSystemEvent> observer, object state) : base(fileSystem, filter, observer, state)
            {
                this.filterPattern = GlobPatternFactory.Slash.CreateRegex(filter);
            }

            /// <summary>
            /// Tests whether <paramref name="path"/> qualifies the filter.
            /// </summary>
            /// <param name="path"></param>
            /// <returns></returns>
            public bool Qualify(string path)
                => filterPattern.IsMatch(path);

            /// <summary>
            /// Remove this handle from collection of observers.
            /// </summary>
            /// <param name="errors"></param>
            protected override void InnerDispose(ref StructList4<Exception> errors)
            {
                base.InnerDispose(ref errors);
                (this.FileSystem as MemoryFileSystem).observers.Remove(this);
            }
        }

        /// <summary>
        /// Parent type for <see cref="Directory"/> and <see cref="MemoryFile"/>.
        /// </summary>
        abstract class Node
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
            /// Parent filesystem.
            /// </summary>
            protected MemoryFileSystem filesystem;

            /// <summary>
            /// Parent directory.
            /// </summary>
            protected internal Directory parent;

            /// <summary>
            /// Path to the entry.
            /// </summary>
            public string Path
            {
                get
                {
                    Directory _parent = parent;
                    return path ?? (path = _parent == filesystem.root ? name : _parent.Path + "/" + name);
                }
            }

            /// <summary>
            /// Create entry
            /// </summary>
            /// <param name="filesystem"></param>
            /// <param name="parent"></param>
            /// <param name="name"></param>
            /// <param name="lastModified"></param>
            protected Node(MemoryFileSystem filesystem, Directory parent, string name, DateTimeOffset lastModified)
            {
                this.filesystem = filesystem ?? throw new ArgumentNullException(nameof(filesystem));
                this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
                this.name = name ?? throw new ArgumentNullException(nameof(name));
                this.lastModified = lastModified;
            }

            /// <summary>
            /// Create entry snapshot.
            /// </summary>
            /// <returns></returns>
            public abstract IFileSystemEntry CreateEntry();

            /// <summary>
            /// Visit self and subtree.
            /// </summary>
            /// <returns></returns>
            public abstract IEnumerable<Node> VisitTree();
        }

        /// <summary>
        /// In-memory directory where in-memory files can be created.
        /// </summary>
        class Directory : Node
        {
            /// <summary>
            /// Files and directories. Lazy construction. Modified under m_lock.
            /// </summary>
            protected internal Dictionary<StringSegment, Node> contents = new Dictionary<StringSegment, Node>();

            /// <summary>
            /// Create directory entry
            /// </summary>
            /// <param name="filesystem"></param>
            /// <param name="parent"></param>
            /// <param name="name"></param>
            /// <param name="lastModified"></param>
            public Directory(MemoryFileSystem filesystem, Directory parent, string name, DateTimeOffset lastModified) : base(filesystem, parent, name, lastModified)
            {
            }

            /// <summary>
            /// Create entry snapshot.
            /// </summary>
            /// <returns></returns>
            public override IFileSystemEntry CreateEntry()
                => new FileSystemEntryDirectory(filesystem, Path, name, lastModified);

            /// <summary>
            /// Enumerate self and subtree.
            /// </summary>
            /// <returns></returns>
            public override IEnumerable<Node> VisitTree()
            {
                Queue<Node> queue = new Queue<Node>();
                queue.Enqueue(this);
                while (queue.Count>0)
                {
                    Node n = queue.Dequeue();
                    yield return n;
                    if (n is Directory dir)
                        foreach (Node c in dir.contents.Values)
                            queue.Enqueue(c);
                }                
            }
        }

        /// <summary>
        /// Memory file
        /// </summary>
        class File : Node
        {
            /// <summary>
            /// Memory file
            /// </summary>
            protected internal MemoryFile memoryFile = new MemoryFile();

            /// <summary>
            /// Create file entry.
            /// </summary>
            /// <param name="filesystem"></param>
            /// <param name="parent"></param>
            /// <param name="name"></param>
            /// <param name="lastModified"></param>
            public File(MemoryFileSystem filesystem, Directory parent, string name, DateTimeOffset lastModified) : base(filesystem, parent, name, lastModified)
            {
            }

            /// <summary>
            /// Create entry snapshot.
            /// </summary>
            /// <returns></returns>
            public override IFileSystemEntry CreateEntry()
            {
                // Create entry snapshot
                return new FileSystemEntryFile(filesystem, Path, name, memoryFile.LastModified, memoryFile.Length);
            }

            /// <summary>
            /// Open a new stream to the file memory
            /// </summary>
            /// <param name="fileMode"></param>
            /// <param name="fileAccess"></param>
            /// <param name="fileShare"></param>
            /// <returns></returns>
            public Stream Open(FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Enumerate self.
            /// </summary>
            /// <returns></returns>
            public override IEnumerable<Node> VisitTree()
            {
                yield return this;
            }
        }
    }

    /// <summary>
    /// Memory file where multiple streams can be opened.
    /// </summary>
    public class MemoryFile
    {
        /// <summary>
        /// Data
        /// </summary>
        protected internal List<byte> data = new List<byte>();

        /// <summary>
        /// Lock object for modifying <see cref="data"/>.
        /// </summary>
        protected object m_lock = new object();

        /// <summary>
        /// Open streams. Constructed lazily. Modified under m_lock.
        /// </summary>
        protected internal List<Stream> streams;


        /// <summary>
        /// File length
        /// </summary>
        public long Length
        {
            get
            {
                lock (m_lock) return data.Count;
            }
        }

        /// <summary>
        /// Datetime when file was last modified
        /// </summary>
        public DateTimeOffset LastModified { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Create memory based file.
        /// </summary>
        public MemoryFile() 
        {
        }
            
        /// <summary>
        /// Open a new stream to the file memory
        /// </summary>
        /// <param name="fileMode"></param>
        /// <param name="fileAccess"></param>
        /// <param name="fileShare"></param>
        /// <returns></returns>
        public Stream Open(FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Stream to <see cref="MemoryFile"/>.
        /// </summary>
        public class Stream : System.IO.Stream
        {
            /// <summary>
            /// Parent
            /// </summary>
            protected MemoryFile parent;

            /// <summary>
            /// Data
            /// </summary>
            protected List<byte> data;

            /// <summary>
            /// Lock object for modifying <see cref="data"/>.
            /// </summary>
            protected object m_lock;

            /// <summary>
            /// File access
            /// </summary>
            protected FileAccess fileAccess;

            /// <summary>
            /// Share
            /// </summary>
            protected FileShare fileShare;

            /// <summary>
            /// Stream position.
            /// </summary>
            protected long position;

            /// <inheritdoc/>
            public override bool CanRead => (fileAccess & FileAccess.Read) == FileAccess.Read;
            /// <inheritdoc/>
            public override bool CanSeek => true;
            /// <inheritdoc/>
            public override bool CanWrite => (fileAccess & FileAccess.Write) == FileAccess.Write;

            /// <summary>File length</summary>
            public override long Length
            {
                get
                {
                    lock (m_lock) return data.Count;
                }
            }

            /// <summary>
            /// Position of the stream.
            /// </summary>
            public override long Position
            {
                get => position;
                set
                {
                    if (value < 0) throw new IOException("position");
                    lock (m_lock)
                    {
                        if (value > Length) throw new IOException("position");
                        position = value;
                    }
                }
            }

            /// <summary>
            /// Create stream.
            /// </summary>
            /// <param name="parent"></param>
            /// <param name="data"></param>
            /// <param name="m_lock"></param>
            /// <param name="fileAccess"></param>
            /// <param name="fileShare"></param>
            public Stream(MemoryFile parent, List<byte> data, object m_lock, FileAccess fileAccess, FileShare fileShare)
            {
                this.parent = parent;
                this.data = data;
                this.m_lock = m_lock;
                this.fileAccess = fileAccess;
                this.fileShare = fileShare;
            }

            /// <inheritdoc/>
            public override void Flush() { }

            /// <summary>
            /// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
            /// </summary>
            /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
            /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
            /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
            /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
            /// <exception cref="ArgumentException">The sum of offset and count is larger than the buffer length.</exception>
            /// <exception cref="ArgumentNullException">buffer is null.</exception>
            /// <exception cref="ArgumentOutOfRangeException">offset or count is negative.</exception>
            /// <exception cref="IOException">An I/O error occurs</exception>
            /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed.</exception>
            public override int Read(byte[] buffer, int offset, int count)
            {
                lock (m_lock)
                {
                    return 0;
                }
            }

            /// <summary>
            /// Reads a byte from the stream and advances the position within the stream by one byte, or returns -1 if at the end of the stream.
            /// </summary>
            /// <returns>The unsigned byte cast to an Int32, or -1 if at the end of the stream.</returns>
            /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed.</exception>
            public override int ReadByte()
            {
                return base.ReadByte();
            }

            /// <summary>
            /// Sets the position within the current stream.
            /// </summary>
            /// <param name="offset">A byte offset relative to the origin parameter.</param>
            /// <param name="origin">A value of type System.IO.SeekOrigin indicating the reference point used to obtain the new position.</param>
            /// <returns>The new position within the current stream.</returns>
            /// <exception cref="IOException">An I/O error occurs</exception>
            /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed.</exception>
            public override long Seek(long offset, SeekOrigin origin)
            {
                lock (m_lock)
                {
                    return 0L;
                }
            }

            /// <summary>
            /// Sets the length of the current stream.
            /// </summary>
            /// <param name="value">The desired length of the current stream in bytes.</param>
            /// <exception cref="IOException">An I/O error occurs</exception>
            /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed.</exception>
            public override void SetLength(long value)
            {
                lock (m_lock)
                {

                }
            }

            /// <summary>
            /// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
            /// </summary>
            /// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream.</param>
            /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
            /// <param name="count">The number of bytes to be written to the current stream.</param>
            /// <exception cref="ArgumentException">The sum of offset and count is greater than the buffer length.</exception>
            /// <exception cref="ArgumentNullException">buffer is null.</exception>
            /// <exception cref="ArgumentOutOfRangeException">offset or count is negative.</exception>
            /// <exception cref="IOException">An I/O error occured, such as the specified file cannot be found.</exception>
            /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed.</exception>
            public override void Write(byte[] buffer, int offset, int count)
            {
                lock (m_lock)
                {

                }
            }

            /// <summary>
            /// Writes a byte to the current position in the stream and advances the position within the stream by one byte.
            /// </summary>
            /// <param name="value">The byte to write to the stream.</param>
            /// <exception cref="IOException">An I/O error occured, such as the specified file cannot be found.</exception>
            /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed.</exception>
            public override void WriteByte(byte value)
            {
                base.WriteByte(value);
            }

            /// <summary>
            /// Close stream, relase share protections in <see cref="MemoryFile"/>.
            /// </summary>
            /// <param name="disposing"></param>
            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
            }
        }
    }
}
