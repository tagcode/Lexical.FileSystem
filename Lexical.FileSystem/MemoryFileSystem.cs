// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Internal;
using Lexical.FileSystem.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Lexical.FileSystem
{
    /// <summary>
    /// In memory filesystem with file and directory structure.
    /// 
    /// Directory separator character is forward slash '/'.  All characters exept '/' are valid file names. Directories can have empty name "". 
    /// Names "." and ".." are reserved for current and parent directories.
    /// 
    /// Maximum file length is <see cref="int.MaxValue"/>*<see cref="BlockSize"/>.
    /// The default blocksize is 1024 which allows 2TB - 1KB files.
    /// </summary>
    public class MemoryFileSystem : FileSystemBase, IFileSystemBrowse, IFileSystemCreateDirectory, IFileSystemDelete, IFileSystemObserve, IFileSystemMove, IFileSystemOpen, IFileSystemDisposable, IFileSystemOptionPath
    {
        /// <summary>
        /// Root directory
        /// </summary>
        Directory root;

        /// <summary>
        /// Reader writer lock for modifying directory structure. 
        /// </summary>
        ReaderWriterLock m_lock = new ReaderWriterLock();

        /// <summary>
        /// List of observers.
        /// </summary>
        ArrayList<ObserverHandle> observers = new ArrayList<ObserverHandle>();

        /// <summary>
        /// A snapshot of observers.
        /// </summary>
        ObserverHandle[] Observers => observers.Array;

        /// <inheritdoc/>
        public FileSystemCaseSensitivity CaseSensitivity => FileSystemCaseSensitivity.CaseSensitive;
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

        /// <summary>Block size</summary>
        public readonly long BlockSize;

        /// <summary>Block pool that dispenses blocks</summary>
        protected IBlockPool blockPool;

        /// <summary>
        /// Create new in-memory filesystem.
        /// </summary>
        public MemoryFileSystem() : base()
        {
            this.root = new Directory(this, null, "", DateTimeOffset.UtcNow);
            this.BlockSize = 1024L;
            this.blockPool = new BlockPool((int)BlockSize, int.MaxValue, 0, true);
        }

        /// <summary>
        /// Create new in-memory filesystem with <paramref name="blockSize"/> block size.
        /// </summary>
        /// <param name="blockSize"></param>
        public MemoryFileSystem(int blockSize) : base()
        {
            this.root = new Directory(this, null, "", DateTimeOffset.UtcNow);
            if (blockSize < 16L) throw new ArgumentOutOfRangeException(nameof(blockSize));
            this.BlockSize = blockSize;
            long maxBlockCount = int.MaxValue;
            this.blockPool = new BlockPool(blockSize, maxBlockCount, 0, true);
        }

        /// <summary>
        /// Create new in-memory filesystem with <paramref name="blockSize"/> block size and limited space with <paramref name="maxSpace"/> parameter.
        /// </summary>
        /// <param name="blockSize"></param>
        /// <param name="maxSpace">maximum space in bytes, truncated to block size upwards</param>
        public MemoryFileSystem(int blockSize, long maxSpace) : base()
        {
            this.root = new Directory(this, null, "", DateTimeOffset.UtcNow);
            if (blockSize < 16L) throw new ArgumentOutOfRangeException(nameof(blockSize));
            this.BlockSize = blockSize;
            long maxBlockCount = (maxSpace+blockSize-1) / blockSize;
            if (maxBlockCount > int.MaxValue) throw new ArgumentOutOfRangeException($"Max block count cannot be over {int.MaxValue}. Please increase block size.");
            this.blockPool = new BlockPool(blockSize, maxBlockCount, 0, true);
        }

        /// <summary>
        /// Create new in-memory filesystem that that allocates blocks with <paramref name="blockPool"/>.
        /// 
        /// <paramref name="blockPool"/> can be shared with other <see cref="MemoryFileSystem"/> implementations for shared free space quota.
        /// </summary>
        /// <param name="blockPool"></param>
        internal MemoryFileSystem(IBlockPool blockPool) : base()
        {
            // Is not implemented yet completely //
            // TODO
            //   When disposed release blocks
            //   When delete files or directories, release blocks
            //   When file is truncated release blocks
            //   When file allocates blocks use blockPool
            this.root = new Directory(this, null, "", DateTimeOffset.UtcNow);
            this.blockPool = blockPool ?? throw new ArgumentNullException(nameof(blockPool));
            this.BlockSize = blockPool.BlockSize;
        }

        /// <summary>
        /// Non-disposable <see cref="MemoryFileSystem"/> cleans all files on dispose, closes observers, but doesn't go into disposed state.
        /// </summary>
        public class NonDisposable : MemoryFileSystem
        {
            /// <summary>Create non-disposable memory filesystem.</summary>
            public NonDisposable() : base() { SetToNonDisposable(); }
            /// <summary>Create non-disposable memory filesystem.</summary>
            public NonDisposable(int blockSize) : base(blockSize) { SetToNonDisposable(); }
            /// <summary>Create non-disposable memory filesystem.</summary>
            public NonDisposable(IBlockPool blockPool) : base(blockPool) { SetToNonDisposable(); }
            /// <summary>Clean files</summary>
            /// <param name="disposeErrors"></param>
            protected override void InnerDispose(ref StructList4<Exception> disposeErrors)
            {
                // Close handles, dispose attached disposables
                base.InnerDispose(ref disposeErrors);
                // Write lock
                m_lock.AcquireWriterLock(int.MaxValue);
                try
                {
                    // Clear root
                    this.root.children.Clear();
                    // Touch
                    this.root.lastAccess = this.root.lastModified = DateTimeOffset.UtcNow;
                }
                finally
                {
                    m_lock.ReleaseWriterLock();
                }
            }
        }


        /// <summary>
        /// Set <paramref name="eventHandler"/> to be used for handling observer events.
        /// 
        /// If <paramref name="eventHandler"/> is null, then events are processed in the running thread.
        /// </summary>
        /// <param name="eventHandler">(optional) factory that handles observer events</param>
        /// <returns>memory filesystem</returns>
        public MemoryFileSystem SetEventDispatcher(TaskFactory eventHandler)
        {
            ((IFileSystemObserve)this).SetEventDispatcher(eventHandler);
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
            // Assert argument
            if (path == null) throw new ArgumentNullException(nameof(path));
            // Assert not disposed
            if (IsDisposing) throw new ObjectDisposedException(GetType().Name);
            // Read lock
            m_lock.AcquireReaderLock(int.MaxValue);
            try
            {
                // Find entry
                Node node = path == "" ? root : GetNode(path);
                // Directory
                if (node is Directory dir_) return dir_.ChildEntries;
                // File
                if (node is File) return new IFileSystemEntry[] { node.Entry };
                // Entry was not found, was not dir or file
                throw new DirectoryNotFoundException(path);
            }
            finally
            {
                m_lock.ReleaseReaderLock();
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
            m_lock.AcquireReaderLock(int.MaxValue);
            try
            {
                // Find entry
                Node node = path == "" ? root : GetNode(path);
                // Not found
                if (node == null) return null;
                // IFileSystemEntry
                return node.Entry;
            }
            finally
            {
                m_lock.ReleaseReaderLock();
            }
        }

        /// <summary>
        /// Create a directory, or multiple cascading directories.
        /// 
        /// If directory at <paramref name="path"/> already exists, then returns without exception.
        /// </summary>
        /// <param name="path">Relative path to file. Directory separator is "/". The root is without preceding slash "", e.g. "dir/dir2"</param>
        /// <returns>true if directory exists after the method, false if directory doesn't exist</returns>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid, such as parent beyond root "../dir".</exception>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is an empty string (""), contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support create directory</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        public void CreateDirectory(string path)
        {
            // Assert argument
            if (path == null) throw new ArgumentNullException(nameof(path));
            // Special case "" is root.
            if (path == "") throw new ArgumentException("Please create \"\" named directory with slash separator \"/\".");
            // Assert not disposed
            if (IsDisposing) throw new ObjectDisposedException(GetType().Name);
            // Datetime
            DateTimeOffset now = DateTimeOffset.UtcNow;
            // Queue of events
            StructList12<IFileSystemEvent> events = new StructList12<IFileSystemEvent>();
            // Take snapshot of observers
            ObserverHandle[] observers = this.Observers;
            // Write lock
            m_lock.AcquireWriterLock(int.MaxValue);
            try
            {
                // Cursor starts at root
                Node cursor = root;
                // Split path at '/' slashes
                PathEnumerator enumr = new PathEnumerator(path, ignoreTrailingSlash: true);
                while (enumr.MoveNext())
                {
                    // Name
                    StringSegment name = enumr.Current;
                    // Update last access
                    cursor.lastAccess = now;
                    // Get entry under lock.
                    if (cursor is Directory directory)
                    {
                        // "."
                        if (name.Equals(StringSegment.Dot)) continue;
                        // ".."
                        if (name.Equals(StringSegment.DotDot))
                        {
                            // ".." -> exception
                            if (directory.parent == null) throw new DirectoryNotFoundException(path);
                            // Go towards parent.
                            cursor = directory.parent;
                            // Next path segment
                            continue;
                        }
                        // No child was found by name
                        if (!directory.children.TryGetValue(name, out cursor))
                        {
                            // Create child directory
                            Directory newDirectory = new Directory(this, directory, name, now);
                            // Add event about child being created
                            if (observers != null)
                                foreach (ObserverHandle observer in observers)
                                {
                                    if (observer.Qualify(newDirectory.Path)) events.Add(new FileSystemEventCreate(observer, now, newDirectory.Path));
                                }
                            // Update time of parent
                            directory.lastModified = now;
                            // Add child to parent
                            directory.children[enumr.Current] = newDirectory;
                            // Flush caches
                            directory.FlushChildEntries();
                            directory.FlushEntry();
                            // Move cursor to child
                            cursor = newDirectory;
                        }
                    }
                    else
                    {
                        // Parent is a file and cannot contain futher subnodes.
                        throw new IOException("Cannot create file under a file (" + cursor.Path + ")");
                    }
                }
            }
            finally
            {
                m_lock.ReleaseWriterLock();
            }

            // Send events
            if (events.Count > 0) DispatchEvents(ref events);
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
        /// <exception cref="IOException">On unexpected IO error, or if <paramref name="path"/> refered to a directory that wasn't empty and <paramref name="recursive"/> is false.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support deleting files</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="path"/> refers to non-file device</exception>
        /// <exception cref="ObjectDisposedException"/>
        /// <exception cref="FileSystemExceptionNoWriteAccess">When trying to delete root</exception>
        public void Delete(string path, bool recursive = false)
        {
            // Assert argument
            if (path == null) throw new ArgumentNullException(nameof(path));
            // Assert not disposed
            if (IsDisposing) throw new ObjectDisposedException(GetType().Name);
            // 
            if (path == "") throw new FileSystemExceptionNoWriteAccess(this, path);
            // Datetime
            DateTimeOffset now = DateTimeOffset.UtcNow;
            // Queue of events
            StructList12<IFileSystemEvent> events = new StructList12<IFileSystemEvent>();
            // Take snapshot of observers
            ObserverHandle[] observers = this.Observers;
            // Write lock
            m_lock.AcquireWriterLock(int.MaxValue);
            try
            {
                // Find file or directory
                Node node = GetNode(path);
                // Not found
                if (node == null) throw new FileNotFoundException(path);
                // Assert not root
                if (node.Path == "") throw new FileSystemExceptionNoWriteAccess(this, path);
                // Get parent
                Directory parent = node.parent;
                // Parent not found?
                if (parent == null) throw new FileNotFoundException(path);
                // Delete file or empty dir
                if ((node is File) || (node is Directory directory && directory.children.Count == 0))
                {
                    // Remove from parent
                    parent.children.Remove(new StringSegment(node.name));
                    // Update parent datetime
                    parent.lastModified = now;
                    // Create delete event
                    if (observers != null)
                        foreach (ObserverHandle observer in observers)
                            if (observer.Qualify(node.Path)) events.Add(new FileSystemEventDelete(observer, now, node.Path));
                    // Flush caches
                    parent.FlushChildEntries();
                    parent.FlushEntry();
                    // Mark file/dir deleted
                    node.Dispose();
                }
                // Non-empty directory
                else if (node is Directory dir)
                {
                    // Assert recursive is 'true'.
                    if (!recursive) throw new IOException("Cannot delete non-empty directory (" + path + ")");
                    // Visit whole tree and delete everything
                    foreach (Node n in dir.VisitTree())
                    {
                        // Create delete event
                        if (observers != null)
                            foreach (ObserverHandle observer in observers)
                                if (observer.Qualify(n.Path)) events.Add(new FileSystemEventDelete(observer, now, n.Path));
                        // Mark deleted
                        n.Dispose();
                        n.FlushEntry();
                    }
                    // Remove from parent
                    parent.children.Remove(new StringSegment(node.name));
                    // Update parent datetime
                    parent.lastModified = now;
                    // Flush caches
                    parent.FlushChildEntries();
                    parent.FlushEntry();
                }
                else
                {
                    // Should not go here
                    throw new InvalidOperationException("Unexpected state");
                }
            }
            finally
            {
                m_lock.ReleaseWriterLock();
            }

            // Send events
            if (events.Count > 0) DispatchEvents(ref events);
        }

        /// <summary>
        /// Move/rename a file or directory.
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
            // Assert arguments
            if (oldPath == null) throw new ArgumentNullException(nameof(oldPath));
            if (newPath == null) throw new ArgumentNullException(nameof(newPath));
            if (oldPath == "") throw new IOException("Cannot move root \"\".");
            if (newPath == "") throw new IOException("Cannot move over root \"\".");
            // Assert not disposed
            if (IsDisposing) throw new ObjectDisposedException(GetType().Name);
            // Datetime
            DateTimeOffset time = DateTimeOffset.UtcNow;
            // Events
            StructList12<IFileSystemEvent> events = new StructList12<IFileSystemEvent>();
            // Take snapshot of observers
            ObserverHandle[] observers = this.Observers;
            // Write lock
            m_lock.AcquireWriterLock(int.MaxValue);
            try
            {
                // Find paths
                Node node = GetNode(oldPath);
                // Not found
                if (node == null) throw new FileNotFoundException(oldPath);
                // Get parents
                Directory oldParent = node.parent;
                // Parent not found
                if (oldParent == null) throw new FileNotFoundException(oldPath);

                // Search newPath parent directory and parse name.
                StringSegment parentPath, newName;
                Directory newParent;
                if (!GetParentAndName(newPath, out parentPath, out newName, out newParent))
                    /*New parent was not found*/
                    throw new DirectoryNotFoundException(parentPath);
                // Nothing to do (check this after proper asserts)
                if (oldParent == newParent && newName == node.name) return;
                // Target file already exists
                Node previouslyExistingNode;
                if (newParent.children.TryGetValue(newName, out previouslyExistingNode))
                    throw previouslyExistingNode is File ? new FileSystemExceptionFileExists(this, newPath) : (Exception)new FileSystemExceptionDirectoryExists(this, newPath);

                // Single file or empty dir
                if (node is File || (node is Directory dir_ && dir_.children.Count == 0))
                {
                    // prev path
                    string c_oldPath = node.Path;
                    // Disconnect from previous parent
                    node.parent.children.Remove(new StringSegment(node.name));
                    // Rename
                    node.name = newName;
                    // Move folder to new parent
                    node.parent = newParent;
                    // Connect to new parent
                    newParent.children[new StringSegment(newName)] = node;
                    // Flush cached path
                    node.path = null;
                    node.FlushPath();
                    node.FlushChildEntries();
                    node.FlushEntry();
                    // new path
                    string c_newPath = node.Path;
                    // Create event
                    if (observers != null)
                        foreach (ObserverHandle observer in observers)
                            if (observer.Qualify(c_oldPath) || observer.Qualify(c_newPath))
                                events.Add(new FileSystemEventRename(observer, time, c_oldPath, c_newPath));
                }
                else
                // Non-empty directory
                {
                    // Nodes and old paths
                    StructList12<(Node, string)> list = new StructList12<(Node, string)>();
                    // Visit tree and capture old path
                    foreach (Node c in node.VisitTree()) list.Add((c, c.Path));
                    // Disconnect from previous parent
                    node.parent.children.Remove(new StringSegment(node.name));
                    // Rename
                    node.name = newName;
                    // Move folder to new parent
                    node.parent = newParent;
                    // Connect to new parent
                    newParent.children[new StringSegment(newName)] = node;
                    // Visit list again
                    for (int i = 0; i < list.Count; i++)
                    {
                        (Node c, string c_oldPath) = list[i];
                        c.path = null;
                        string c_newPath = c.Path;
                        if (observers != null)
                            foreach (ObserverHandle observer in observers)
                                if (observer.Qualify(c_oldPath) || observer.Qualify(c_newPath))
                                    events.Add(new FileSystemEventRename(observer, time, c_oldPath, c_newPath));
                        c.FlushEntry();
                    }
                }
                // Change directory times
                oldParent.lastModified = time;
                newParent.lastModified = time;
                // Flush caches
                oldParent.FlushChildEntries();
                newParent.FlushChildEntries();
                oldParent.FlushEntry();
                newParent.FlushEntry();
            }
            finally
            {
                m_lock.ReleaseWriterLock();
            }

            // Send events
            if (events.Count > 0) DispatchEvents(ref events);
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
        /// <exception cref="FileSystemExceptionNoReadAccess">No read access</exception>
        /// <exception cref="FileSystemExceptionNoWriteAccess">No write access</exception>
        /// <exception cref="FileSystemExceptionDirectoryExists">Directory already exists</exception>
        /// <exception cref="FileSystemExceptionFileExists">File already exists</exception>
        public Stream Open(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
        {
            // Assert argument
            if (path == null) throw new ArgumentNullException(nameof(path));
            // Assert argument
            if (path == "") throw new IOException("Cannot open root directory.");
            // Assert not disposed
            if (IsDisposing) throw new ObjectDisposedException(GetType().Name);
            // Datetime
            DateTimeOffset now = DateTimeOffset.UtcNow;
            // Events
            StructList12<IFileSystemEvent> events = new StructList12<IFileSystemEvent>();
            // Take snapshot of observers
            ObserverHandle[] observers = this.Observers;
            // result stream.
            Stream stream = null;
            // Read lock
            m_lock.AcquireReaderLock(int.MaxValue);
            try
            {
                // Find entry
                Node node = GetNode(path);
                // Is directory
                if (node is Directory) throw new FileSystemExceptionDirectoryExists(this, path);
                // Create new file, throw if found
                if (fileMode == FileMode.CreateNew)
                {
                    // Cannot create, file already exists
                    if (node != null) throw new FileSystemExceptionFileExists(this, path);

                    // Create file
                    LockCookie cookie = m_lock.UpgradeToWriterLock(int.MaxValue);
                    try
                    {
                        // Find entry again under write lock.
                        node = GetNode(path);
                        // Is directory
                        if (node is Directory) throw new FileSystemExceptionDirectoryExists(this, path);
                        // Cannot create, file already exists
                        if (node != null) throw new FileSystemExceptionFileExists(this, path);

                        // Split path to parent and filename parts, also search for parent directory
                        StringSegment parentPath, name;
                        Directory parent;
                        if (!GetParentAndName(path, out parentPath, out name, out parent)) throw new DirectoryNotFoundException(parentPath);
                        // Invalid name
                        if (name == StringSegment.Dot || name == StringSegment.DotDot || name == StringSegment.Empty) throw new FileSystemExceptionInvalidName(this, path);
                        // Create file.
                        File f = new File(this, parent, name, now);
                        // Open stream
                        stream = f.Open(fileAccess, fileShare);
                        // Attach to parent
                        parent.children[name] = f;
                        parent.lastModified = now;
                        parent.FlushEntry();
                        parent.FlushChildEntries();
                        // Create event
                        if (observers != null)
                            foreach (ObserverHandle observer in observers)
                                if (observer.Qualify(path))
                                    events.Add(new FileSystemEventCreate(observer, now, path));
                        // Return stream
                        return stream;
                    }
                    finally
                    {
                        m_lock.DowngradeFromWriterLock(ref cookie);
                    }
                }
                else if (fileMode == FileMode.Create)
                {
                    // Create file
                    LockCookie cookie = m_lock.UpgradeToWriterLock(int.MaxValue);
                    try
                    {
                        // Find entry again under write lock.
                        node = GetNode(path);
                        // Split path to parent and filename parts, also search for parent directory
                        StringSegment parentPath, name;
                        Directory parent;
                        if (!GetParentAndName(path, out parentPath, out name, out parent)) throw new DirectoryNotFoundException(parentPath);
                        // Invalid name
                        if (name == StringSegment.Dot || name == StringSegment.DotDot || name == StringSegment.Empty) throw new FileSystemExceptionInvalidName(this, path);
                        // Is directory
                        if (node is Directory) throw new FileSystemExceptionDirectoryExists(this, parentPath);

                        // Previous file is unlinked
                        if (node is File && parent.children.Remove(name))
                        {
                            // Create event
                            if (observers != null)
                                foreach (ObserverHandle observer in observers)
                                    if (observer.Qualify(path))
                                        events.Add(new FileSystemEventDelete(observer, now, path));
                        }

                        // Create file.
                        File f = new File(this, parent, name, now);
                        // Open stream
                        stream = f.Open(fileAccess, fileShare);
                        // Attach to parent
                        parent.children[name] = f;
                        parent.lastModified = now;
                        parent.FlushEntry();
                        parent.FlushChildEntries();
                        // Create event
                        if (observers != null)
                            foreach (ObserverHandle observer in observers)
                                if (observer.Qualify(path))
                                    events.Add(new FileSystemEventCreate(observer, now, path));
                        // Return stream
                        return stream;
                    }
                    finally
                    {
                        m_lock.DowngradeFromWriterLock(ref cookie);
                    }
                }
                else if (fileMode == FileMode.Open)
                {
                    // Is directory
                    if (node is Directory)
                    {
                        // Split path to parent and filename parts, also search for parent directory
                        StringSegment parentPath, name;
                        Directory parent;
                        if (!GetParentAndName(path, out parentPath, out name, out parent)) throw new DirectoryNotFoundException(parentPath);
                        throw new FileSystemExceptionDirectoryExists(this, parentPath);
                    }
                    // Open file
                    if (node is File f) return f.Open(fileAccess, fileShare);
                    // File not found
                    throw new FileNotFoundException(path);
                }
                else if (fileMode == FileMode.OpenOrCreate)
                {
                    // Is directory
                    if (node is Directory)
                    {
                        // Split path to parent and filename parts, also search for parent directory
                        StringSegment parentPath, name;
                        Directory parent;
                        GetParentAndName(path, out parentPath, out name, out parent);
                        throw new FileSystemExceptionDirectoryExists(this, parentPath);
                    }
                    // Open file
                    if (node is File existingFile) return existingFile.Open(fileAccess, fileShare);

                    // Create file
                    LockCookie cookie = m_lock.UpgradeToWriterLock(int.MaxValue);
                    try
                    {
                        // Split path to parent and filename parts, also search for parent directory
                        StringSegment parentPath, name;
                        Directory parent;
                        if (!GetParentAndName(path, out parentPath, out name, out parent)) throw new DirectoryNotFoundException(parentPath);
                        // Invalid name
                        if (name == StringSegment.Dot || name == StringSegment.DotDot || name == StringSegment.Empty) throw new FileSystemExceptionInvalidName(this, path);

                        // Create file
                        File f = new File(this, parent, name, now);
                        // Open stream
                        stream = f.Open(fileAccess, fileShare);
                        // Attach to parent
                        parent.children[name] = f;
                        parent.lastModified = now;
                        parent.FlushEntry();
                        parent.FlushChildEntries();
                        // Create event
                        if (observers != null)
                            foreach (ObserverHandle observer in observers)
                                if (observer.Qualify(path))
                                    events.Add(new FileSystemEventCreate(observer, now, path));
                        // Return stream
                        return stream;
                    }
                    finally
                    {
                        m_lock.DowngradeFromWriterLock(ref cookie);
                    }
                }

                throw new ArgumentException(nameof(fileMode));
            }
            finally
            {
                m_lock.ReleaseReaderLock();

                // Send events
                if (events.Count > 0) DispatchEvents(ref events);
            }

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
            // Current time
            DateTimeOffset now = DateTimeOffset.UtcNow;
            // Path '/' splitter, enumerates name strings from root towards tail
            PathEnumerator enumr = new PathEnumerator(path, ignoreTrailingSlash: true);
            // Get next name from the path
            while (enumr.MoveNext())
            {
                // Update last access
                cursor.lastAccess = now;
                // Name
                StringSegment name = enumr.Current;
                // Get entry under lock.
                if (cursor is Directory directory)
                {
                    // "."
                    if (name.Equals(StringSegment.Dot)) continue;
                    // ".."
                    if (name.Equals(StringSegment.DotDot))
                    {
                        if (directory.parent == null) return null;
                        cursor = directory.parent;
                        continue;
                    }
                    // Failed to find child entry
                    if (!directory.children.TryGetValue(name, out cursor))
                        return null;
                }
                else
                {
                    // Parent is a file and cannot contain further subentries.
                    return null;
                }
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
            PathEnumerator enumr = new PathEnumerator(path, ignoreTrailingSlash: true);
            // Path's name parts
            StructList12<StringSegment> names = new StructList12<StringSegment>();
            // Split path into names
            while (enumr.MoveNext()) names.Add(enumr.Current);
            // Unexpected error
            if (names.Count == 0) { name = StringSegment.Empty; parentPath = StringSegment.Empty; parent = null; return false; }
            // Separate to parentPath and name
            if (names.Count == 1) { name = names[0]; parentPath = StringSegment.Empty; }
            else { name = names[names.Count - 1]; parentPath = new StringSegment(path, names[0].Start, names[names.Count - 2].Start + names[names.Count - 2].Length); }
            // Current time
            DateTimeOffset now = DateTimeOffset.UtcNow;
            // Search parent directory
            Node cursor = root;
            for (int i = 0; i < names.Count - 1; i++)
            {
                // Name
                StringSegment cursorName = names[i];
                // Update last access
                cursor.lastAccess = now;
                // Get entry under lock.
                if (cursor is Directory directory)
                {
                    // "."
                    if (cursorName.Equals(StringSegment.Dot)) continue;
                    // ".."
                    if (cursorName.Equals(StringSegment.DotDot))
                    {
                        if (directory.parent == null) { parent = null; return false; }
                        cursor = directory.parent;
                        continue;
                    }
                    // Failed to find child entry
                    if (!directory.children.TryGetValue(cursorName, out cursor)) { parent = null; return false; }
                }
                else
                {
                    // Parent is a file and cannot contain further subentries.
                    parent = null; return false;
                }
            }
            parent = cursor as Directory;
            return cursor is Directory;
        }

        /// <summary>
        /// Handle dispose
        /// </summary>
        /// <param name="disposeErrors"></param>
        protected override void InnerDispose(ref StructList4<Exception> disposeErrors)
        {
            // Snapshot of handles
            ObserverHandle[] observerArray = observers.Array;
            // Close each handle
            foreach (ObserverHandle observerHandle in observerArray)
            {
                // Close handle
                try
                {
                    observerHandle.Dispose();
                }
                catch (Exception)
                {
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
        public MemoryFileSystem AddDisposeAction(Action<MemoryFileSystem> disposeAction)
        {
            // Argument error
            if (disposeAction == null) throw new ArgumentNullException(nameof(disposeAction));
            // Parent is disposed/ing
            if (IsDisposing) { disposeAction(this); return this; }
            // Adapt to IDisposable
            IDisposable disposable = new DisposeAction<MemoryFileSystem>(disposeAction, this);
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
        public MemoryFileSystem AddDisposeAction(Action<object> disposeAction, object state)
        {
            ((IDisposeList)this).AddDisposeAction(disposeAction, state);
            return this;
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
        public MemoryFileSystem AddDisposables(IEnumerable disposables)
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
        public MemoryFileSystem RemoveDisposables(IEnumerable disposables)
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

        /// <inheritdoc/>
        public override IFileSystemObserver Observe(string filter, IObserver<IFileSystemEvent> observer, object state = null)
        {
            // Assert not disposed
            if (IsDisposing) throw new ObjectDisposedException(GetType().Name);
            ObserverHandle handle = new ObserverHandle(this, filter, observer, state);
            observers.Add(handle);
            // Send IFileSystemEventStart
            observer.OnNext(new FileSystemEventStart(handle, DateTimeOffset.UtcNow));
            return handle;
        }

        /// <summary>
        /// Observer
        /// </summary>
        class ObserverHandle : FileSystemObserverHandleBase
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
            public ObserverHandle(MemoryFileSystem filesystem, string filter, IObserver<IFileSystemEvent> observer, object state) : base(filesystem, filter, observer, state)
            {
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
                base.InnerDispose(ref errors);
                (this.FileSystem as MemoryFileSystem).observers.Remove(this);
            }
        }

        /// <summary>
        /// Parent type for <see cref="Directory"/> and <see cref="MemoryFile"/>.
        /// 
        /// Node class must be accessed only under reader or writer lock.
        /// </summary>
        abstract class Node : IDisposable
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
            protected MemoryFileSystem filesystem;

            /// <summary>
            /// Parent directory.
            /// </summary>
            protected internal Directory parent;

            /// <summary>
            /// Cached entry
            /// </summary>
            protected IFileSystemEntry entry;

            /// <summary>
            /// Get or create entry.
            /// </summary>
            public IFileSystemEntry Entry => entry ?? (entry = CreateEntry());

            /// <summary>
            /// Path to the entry.
            /// </summary>
            public abstract string Path { get; }

            /// <summary>
            /// Create entry
            /// </summary>
            /// <param name="filesystem"></param>
            /// <param name="parent"></param>
            /// <param name="name"></param>
            /// <param name="time">time for lastmodified and lastaccess</param>
            protected Node(MemoryFileSystem filesystem, Directory parent, string name, DateTimeOffset time)
            {
                this.filesystem = filesystem ?? throw new ArgumentNullException(nameof(filesystem));
                this.parent = parent;
                this.name = name ?? throw new ArgumentNullException(nameof(name));
                this.lastModified = time;
                this.lastAccess = time;
            }

            /// <summary>Create entry snapshot.</summary>
            public abstract IFileSystemEntry CreateEntry();
            /// <summary>Visit self and subtree.</summary>
            public abstract IEnumerable<Node> VisitTree();

            /// <summary>Flush cached path info</summary>
            public void FlushPath() { path = null; entry = null; }
            /// <summary>Flush cached entry info.</summary>
            public void FlushEntry() => entry = null;
            /// <summary>Flush child entries</summary>
            public virtual void FlushChildEntries() { }
            /// <summary>Delete node</summary>
            public virtual void Dispose() => this.isDeleted = true;
            /// <summary>Print info</summary>
            public override string ToString() => Path;
        }

        /// <summary>
        /// In-memory directory where in-memory files can be created.
        /// </summary>
        class Directory : Node
        {
            /// <summary>
            /// Files and directories. Lazy construction. Modified under m_lock.
            /// </summary>
            protected internal Dictionary<StringSegment, Node> children = new Dictionary<StringSegment, Node>();

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
                    int c = children.Count;
                    IFileSystemEntry[] array = new IFileSystemEntry[c];
                    int i = 0;
                    foreach (Node e in children.Values) array[i++] = e.Entry;
                    return childEntries = array;
                }
            }

            /// <summary>
            /// Path to the entry.
            /// </summary>
            public override string Path
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
            public override IFileSystemEntry CreateEntry() =>
                parent == null ? 
                /*Root*/     (IFileSystemEntry) new FileSystemEntryDrive(filesystem, Path, name, lastModified, lastAccess, DriveType.Ram, -1L, -1L, null, null, true) : 
                /*non-root*/ (IFileSystemEntry) new FileSystemEntryDirectory(filesystem, Path, name, lastModified, lastAccess);

            /// <summary>
            /// Flush cached array of child entries.
            /// </summary>
            public override void FlushChildEntries()
            {
                childEntries = null;
            }

            /// <summary>
            /// Enumerate self and subtree.
            /// </summary>
            /// <returns></returns>
            public override IEnumerable<Node> VisitTree()
            {
                Queue<Node> queue = new Queue<Node>();
                queue.Enqueue(this);
                while (queue.Count > 0)
                {
                    Node n = queue.Dequeue();
                    yield return n;
                    if (n is Directory dir)
                        foreach (Node c in dir.children.Values)
                            queue.Enqueue(c);
                }
            }
        }

        /// <summary>
        /// Memory file
        /// </summary>
        class File : Node, IObserver<MemoryFile.ModifiedEvent>
        {
            /// <summary>
            /// Memory file
            /// </summary>
            protected internal MemoryFile memoryFile;

            /// <summary>
            /// Path to the entry.
            /// </summary>
            public override string Path
            {
                get
                {
                    // Get reference of previous cached value
                    string _path = path;
                    // Return previous cached value
                    if (_path != null) return _path;
                    // Get reference of parent
                    Directory _parent = parent;
                    // k2nd+ level paths
                    return _parent.Path + name;
                }
            }

            /// <summary>
            /// Create file entry.
            /// </summary>
            /// <param name="filesystem"></param>
            /// <param name="parent"></param>
            /// <param name="name"></param>
            /// <param name="lastModified"></param>
            public File(MemoryFileSystem filesystem, Directory parent, string name, DateTimeOffset lastModified) : base(filesystem, parent, name, lastModified)
            {
                memoryFile = new MemoryFile(filesystem.blockPool, Path);
                memoryFile.Subscribe(this);
            }

            /// <summary>
            /// Create entry snapshot.
            /// </summary>
            /// <returns></returns>
            public override IFileSystemEntry CreateEntry()
            {
                // Create entry snapshot
                return new FileSystemEntryFile(filesystem, Path, name, memoryFile.LastModified, memoryFile.LastModified > lastAccess ? memoryFile.LastModified : lastAccess, memoryFile.Length);
            }

            void IObserver<MemoryFile.ModifiedEvent>.OnCompleted() { }
            void IObserver<MemoryFile.ModifiedEvent>.OnError(Exception error) { }

            /// <summary>
            /// File was modified
            /// </summary>
            /// <param name="value"></param>
            void IObserver<MemoryFile.ModifiedEvent>.OnNext(MemoryFile.ModifiedEvent value)
            {
                // Update time
                if (value.Time > this.lastModified)
                {
                    this.lastModified = value.Time;
                    this.lastAccess = value.Time;
                }
                // Notify subscribers of filesystem change
                ObserverHandle[] observers = filesystem.Observers;
                if (observers.Length > 0)
                {
                    // Queue of events
                    StructList12<IFileSystemEvent> events = new StructList12<IFileSystemEvent>();
                    foreach (ObserverHandle observer in observers)
                    {
                        // Add event
                        if (observer.Qualify(Path)) events.Add(new FileSystemEventChange(observer, value.Time, Path));
                    }
                    // Send events
                    if (events.Count > 0) filesystem.DispatchEvents(ref events);
                }
            }

            /// <summary>
            /// Open a new stream to the file memory
            /// </summary>
            /// <param name="fileAccess"></param>
            /// <param name="fileShare"></param>
            /// <returns></returns>
            /// <exception cref="FileSystemExceptionNoReadAccess">No read access</exception>
            /// <exception cref="FileSystemExceptionNoWriteAccess">No write access</exception>
            public Stream Open(FileAccess fileAccess, FileShare fileShare)
            {
                try
                {
                    // Get a reference
                    var _memoryFile = memoryFile;
                    // Test object is not deleted
                    if (_memoryFile == null || isDeleted) throw new FileNotFoundException(Path);
                    // Open stream
                    return _memoryFile.Open(fileAccess, fileShare);
                }
                catch (FileSystemException e)
                when ( /*Attach filesystem and path*/ e.Set(filesystem, Path))
                { /*Never goes here*/ return null; }
            }

            /// <summary>
            /// Enumerate self.
            /// </summary>
            /// <returns></returns>
            public override IEnumerable<Node> VisitTree()
            {
                yield return this;
            }

            /// <summary>
            /// Delete file
            /// </summary>
            public override void Dispose()
            {
                base.Dispose();
                memoryFile.Dispose();
                memoryFile = null;
            }
        }
    }
}
