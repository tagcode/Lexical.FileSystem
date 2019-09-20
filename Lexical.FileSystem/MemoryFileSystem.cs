﻿// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Internal;
using Lexical.FileSystem.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Lexical.FileSystem
{
    /// <summary>
    /// In-memory filesystem.
    /// 
    /// MemoryFileSystem is limited to 2GB files.
    /// </summary>
    public class MemoryFileSystem : FileSystemBase, IFileSystemBrowse, IFileSystemCreateDirectory, IFileSystemDelete, IFileSystemObserve, IFileSystemMove, IFileSystemOpen, IFileSystemDisposable
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

        /// <summary>
        /// Policy of whether trailing slash is ignored from paths.
        /// For example, if true "/mnt/dir/" refers to directory ["mnt", "dir"].
        /// If false "/mnt/dir/2 refers to directory ["mnt", "dir", ""].
        /// </summary>
        protected bool ignoreTrailingSlash;

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
            this.root = new Directory(this, null, "", DateTimeOffset.UtcNow);
            this.taskFactory = Task.Factory;
            this.processEventsAction = processEvents;
        }

        /// <summary>
        /// Create new in-memory filesystem.
        /// </summary>
        /// <param name="ignoreTrailingSlash">
        /// Policy of whether trailing slash is ignored from paths.
        /// For example, if true "/mnt/dir/" refers to directory ["mnt", "dir"].
        /// If false "/mnt/dir/2 refers to directory ["mnt", "dir", ""].
        /// </param>
        public MemoryFileSystem(bool ignoreTrailingSlash)
        {
            this.root = new Directory(this, null, "", DateTimeOffset.UtcNow);
            this.taskFactory = Task.Factory;
            this.processEventsAction = processEvents;
            this.ignoreTrailingSlash = ignoreTrailingSlash;
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
            m_lock.AcquireReaderLock(int.MaxValue);
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
                Node node = GetNode(path);
                // Not found
                if (node == null) throw new FileNotFoundException(path);
                // IFileSystemEntry
                return node.CreateEntry();
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
            m_lock.AcquireWriterLock(int.MaxValue);
            try
            {
                Node node = root;
                PathEnumerator enumr = new PathEnumerator(path, ignoreTrailingSlash);
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
                            Directory newDirectory = new Directory(this, dir, name, DateTimeOffset.UtcNow);
                            // Add event about parent modified and child created
                            if (observers != null)
                                foreach (ObserverHandle observer in observers)
                                {                                    
                                    if (observer.Qualify(newDirectory.Path)) events.Add(new FileSystemEventCreate(observer, time, newDirectory.Path));
                                }
                            // Update time of parent
                            dir.lastModified = time;
                            // Add child to parent
                            dir.contents[enumr.Current] = newDirectory;
                            // Recurse into child
                            node = newDirectory;
                        }
                    }
                    else
                    {
                        // Parent is a file and cannot contain futher subnodes.
                        throw new IOException("Cannot create file under a file ("+node.Path+")");
                    }
                }
            }
            finally
            {
                m_lock.ReleaseWriterLock();
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
            m_lock.AcquireWriterLock(int.MaxValue);
            try
            {
                // Find file or directory
                Node node = GetNode(path);
                // Not found
                if (node == null) throw new FileNotFoundException(path);
                // Assert not root
                if (node.path == "") throw new IOException("Cannot delete root.");
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
                    node.Dispose();
                }
                // Non-empty directory
                else if (node is Directory dir)
                {
                    // Assert recursive is 'true'.
                    if (!recursive) throw new IOException("Cannot delete non-empty directory (" + path + ")");
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
                        n.Dispose();
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
                m_lock.ReleaseWriterLock();
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
            // Events
            StructList12<IFileSystemEvent> events = new StructList12<IFileSystemEvent>();
            // Take snapshot of observers
            ObserverHandle[] observers = this.Observers;
            // Write lock
            m_lock.AcquireWriterLock(int.MaxValue);
            try
            {
                // Find paths
                Node oldNode = GetNode(oldPath), newNode = GetNode(newPath);
                // Not found
                if (oldNode == null) throw new FileNotFoundException(oldPath);
                // Assert not root
                if (oldNode.path == "") throw new IOException("Cannot move root.");
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
                PathEnumerator enumr = new PathEnumerator(newPath, ignoreTrailingSlash);
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
                // Single file or empty dir
                if (oldNode is File || (oldNode is Directory dir_ && dir_.contents.Count == 0))
                {
                    // prev path
                    string c_oldPath = oldNode.Path;
                    // Move folder to new parent
                    if (oldNode.parent != newParent) oldNode.parent = newParent;
                    // Flush cached path
                    oldNode.path = null;
                    // new path
                    string c_newPath = oldNode.Path;
                    if (observers != null)
                        foreach (ObserverHandle observer in observers)
                            if (observer.Qualify(c_oldPath) || observer.Qualify(c_newPath))
                                events.Add(new FileSystemEventRename(observer, time, c_oldPath, c_newPath));
                }
                else {
                    // Nodes and old paths
                    StructList12<(Node, string)> list = new StructList12<(Node, string)>();
                    // Visit tree and capture old path
                    foreach (Node c in oldNode.VisitTree()) list.Add((c, c.Path));
                    // Move folder to new parent
                    if (oldNode.parent != newParent) oldNode.parent = newParent;
                    // Visit list again
                    for (int i=0; i<list.Count; i++)
                    {
                        (Node c, string c_oldPath) = list[i];
                        c.path = null;
                        string c_newPath = c.Path;
                        if (observers != null)
                            foreach (ObserverHandle observer in observers)
                                if (observer.Qualify(c_oldPath) || observer.Qualify(c_newPath))
                                    events.Add(new FileSystemEventRename(observer, time, c_oldPath, c_newPath));
                    }
                }
                // Change directory times
                oldParent.lastModified = time;
                newParent.lastModified = time;
            }
            finally
            {
                m_lock.ReleaseWriterLock();
            }

            // Send events
            if (events.Count > 0) SendEvents(ref events);
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
            // Assert not disposed
            if (IsDisposing) throw new ObjectDisposedException(GetType().Name);
            // Datetime
            DateTimeOffset time = DateTimeOffset.UtcNow;
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
                    LockCookie coockie = m_lock.UpgradeToWriterLock(int.MaxValue);
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
                        if (!GetParentAndName(path, out parentPath, out name, out parent)) throw new IOException(path);

                        // Create file.
                        File f = new File(this, parent, name, time);
                        // Open stream
                        stream = f.Open(fileAccess, fileShare);
                        // Attach to parent
                        parent.contents[name] = f;
                        // Create event
                        if (observers != null)
                            foreach (ObserverHandle observer in observers)
                                if (observer.Qualify(path))
                                    events.Add(new FileSystemEventCreate(observer, time, path));
                        // Return stream
                        return stream;
                    }
                    finally
                    {
                        m_lock.DowngradeFromWriterLock(ref coockie);
                    }
                }
                else if (fileMode == FileMode.Create)
                {
                    // Create file
                    LockCookie coockie = m_lock.UpgradeToWriterLock(int.MaxValue);
                    try
                    {
                        // Find entry again under write lock.
                        node = GetNode(path);
                        // Split path to parent and filename parts, also search for parent directory
                        StringSegment parentPath, name;
                        Directory parent;
                        if (!GetParentAndName(path, out parentPath, out name, out parent)) throw new IOException(path);
                        // Is directory
                        if (node is Directory) throw new FileSystemExceptionDirectoryExists(this, parentPath);

                        // Previous file is unlinked
                        if (node is File && parent.contents.Remove(name))
                        {
                            // Create event
                            if (observers != null)
                                foreach (ObserverHandle observer in observers)
                                    if (observer.Qualify(path))
                                        events.Add(new FileSystemEventDelete(observer, time, path));
                        }

                        // Create file.
                        File f = new File(this, parent, name, time);
                        // Open stream
                        stream = f.Open(fileAccess, fileShare);
                        // Attach to parent
                        parent.contents[name] = f;
                        // Create event
                        if (observers != null)
                            foreach (ObserverHandle observer in observers)
                                if (observer.Qualify(path))
                                    events.Add(new FileSystemEventCreate(observer, time, path));
                        // Return stream
                        return stream;
                    }
                    finally
                    {
                        m_lock.DowngradeFromWriterLock(ref coockie);
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
                        GetParentAndName(path, out parentPath, out name, out parent);
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
                    {
                        // Split path to parent and filename parts, also search for parent directory
                        StringSegment parentPath, name;
                        Directory parent;
                        if (!GetParentAndName(path, out parentPath, out name, out parent)) throw new IOException(path);

                        // Create file
                        File f = new File(this, parent, name, time);
                        // Open stream
                        stream = f.Open(fileAccess, fileShare);
                        // Attach to parent
                        parent.contents[name] = f;
                        // Create event
                        if (observers != null)
                            foreach (ObserverHandle observer in observers)
                                if (observer.Qualify(path))
                                    events.Add(new FileSystemEventCreate(observer, time, path));
                        // Return stream
                        return stream;
                    }
                }

                throw new ArgumentException(nameof(fileMode));
            }
            finally
            {
                m_lock.ReleaseReaderLock();

                // Send events
                if (events.Count > 0) SendEvents(ref events);
            }

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
            PathEnumerator enumr = new PathEnumerator(path, ignoreTrailingSlash);
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
            // Path '/' splitter, enumerates name strings from root towards tail
            PathEnumerator enumr = new PathEnumerator(path, ignoreTrailingSlash);
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
            Node node = root;
            for (int i=0; i<names.Count-1; i++)
            {
                // Get entry under lock.
                if (node is Directory dir)
                {
                    // Failed to find child entry
                    if (!dir.contents.TryGetValue(names[i], out node)) { parent = null; return false; }
                }
                else
                {
                    // Parent is a file and cannot contain further subentries.
                    parent = null; return false;
                }
            }
            parent = node as Directory;
            return node is Directory;
        }

        /// <summary>
        /// Send <paramref name="events"/> to observers with <see cref="taskFactory"/>.
        /// If <see cref="taskFactory"/> is null, then sends events in the running thread.
        /// </summary>
        /// <param name="events"></param>
        void SendEvents(ref StructList12<IFileSystemEvent> events)
        {
            // Don't send events anymore
            if (IsDisposing) return;
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
            /// Accept all pattern "**".
            /// </summary>
            bool acceptAll;

            /// <summary>
            /// Create new observer.
            /// </summary>
            /// <param name="fileSystem"></param>
            /// <param name="filter">path filter as glob pattenrn. "*" any sequence of charaters within a directory, "**" any sequence of characters, "?" one character. E.g. "**/*.txt"</param>
            /// <param name="observer"></param>
            /// <param name="state"></param>
            public ObserverHandle(MemoryFileSystem fileSystem, string filter, IObserver<IFileSystemEvent> observer, object state) : base(fileSystem, filter, observer, state)
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
                (this.FileSystem as MemoryFileSystem).observers.Remove(this);
            }
        }

        /// <summary>
        /// Parent type for <see cref="Directory"/> and <see cref="MemoryFile"/>.
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
                this.parent = parent;
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
        class File : Node, IObserver<MemoryFile.ModifiedEvent>
        {
            /// <summary>
            /// Memory file
            /// </summary>
            protected internal MemoryFile memoryFile;

            /// <summary>
            /// Create file entry.
            /// </summary>
            /// <param name="filesystem"></param>
            /// <param name="parent"></param>
            /// <param name="name"></param>
            /// <param name="lastModified"></param>
            public File(MemoryFileSystem filesystem, Directory parent, string name, DateTimeOffset lastModified) : base(filesystem, parent, name, lastModified)
            {
                memoryFile = new MemoryFile();
                memoryFile.Subscribe(this);
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

            void IObserver<MemoryFile.ModifiedEvent>.OnCompleted() { }
            void IObserver<MemoryFile.ModifiedEvent>.OnError(Exception error) { }

            /// <summary>
            /// File was modified
            /// </summary>
            /// <param name="value"></param>
            void IObserver<MemoryFile.ModifiedEvent>.OnNext(MemoryFile.ModifiedEvent value)
            {
                // Update time
                if (value.Time > this.lastModified) this.lastModified = value.Time;
                // Notify subscribers of filesystem change
                ObserverHandle[] observers = filesystem.Observers;
                if (observers.Length>0)
                {
                    // Queue of events
                    StructList12<IFileSystemEvent> events = new StructList12<IFileSystemEvent>();
                    foreach (ObserverHandle observer in observers)
                    {
                        // Add event
                        if (observer.Qualify(Path)) events.Add(new FileSystemEventChange(observer, value.Time, Path));
                    }
                    // Send events
                    if (events.Count > 0) filesystem.SendEvents(ref events);
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
                // Get a reference
                var _memoryFile = memoryFile;
                // Test object is not deleted
                if (_memoryFile == null || isDeleted) throw new FileNotFoundException(Path);
                // Open stream
                return _memoryFile.Open(fileAccess, fileShare);
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

    /// <summary>
    /// Memory file where multiple streams can be opened.
    /// 
    /// MemoryFile is limited to 2GB files.
    /// </summary>
    public class MemoryFile : IObservable<MemoryFile.ModifiedEvent>, IDisposable
    {
        /// <summary>
        /// Data
        /// </summary>
        protected internal List<byte> data = new List<byte>();

        /// <summary>
        /// Lock for modifying <see cref="data"/>.
        /// </summary>
        protected ReaderWriterLock dataLock = new ReaderWriterLock();

        /// <summary>
        /// Critical section lock for acquiring read/write permission, and modifying <see cref="streams"/>.
        /// </summary>
        protected object m_lock = new object();

        /// <summary>
        /// Observers
        /// </summary>
        CopyOnWriteList<IObserver<ModifiedEvent>> observers = new CopyOnWriteList<IObserver<ModifiedEvent>>();

        /// <summary>
        /// Open streams
        /// </summary>
        List<MemoryFile.Stream> streams = new List<MemoryFile.Stream>();

        /// <summary>
        /// Last time change event was sent.
        /// </summary>
        protected DateTimeOffset lastChangeEvent = DateTimeOffset.MinValue;

        /// <summary>
        /// Time to wait between forwarding change events to observers.
        /// </summary>
        static public TimeSpan ChangeEventTolerance = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// Is object disposed.
        /// </summary>
        protected bool isDisposed;

        /// <summary>
        /// File length
        /// </summary>
        public long Length
        {
            get
            {
                lock (dataLock) return data.Count;
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
        /// Event that notifies about modifying the file.
        /// </summary>
        public struct ModifiedEvent
        {
            /// <summary>
            /// The file that was modified
            /// </summary>
            public readonly MemoryFile File;

            /// <summary>
            /// Time of event
            /// </summary>
            public readonly DateTimeOffset Time;

            /// <summary>
            /// Create event
            /// </summary>
            /// <param name="file"></param>
            /// <param name="time"></param>
            public ModifiedEvent(MemoryFile file, DateTimeOffset time)
            {
                File = file;
                Time = time;
            }
        }

        /// <summary>
        /// Subscribe to memory file.
        /// </summary>
        /// <param name="observer"></param>
        /// <returns></returns>
        public IDisposable Subscribe(IObserver<ModifiedEvent> observer)
        {
            if (isDisposed) throw new ObjectDisposedException(GetType().FullName);
            observers.Add(observer);
            return new ObserverHandle(observer, observers);
        }

        /// <summary>
        /// Handle that removes <see cref="observer"/> from <see cref="observers"/> when disposed.
        /// </summary>
        class ObserverHandle : IDisposable
        {
            IObserver<ModifiedEvent> observer;
            CopyOnWriteList<IObserver<ModifiedEvent>> observers;

            public ObserverHandle(IObserver<ModifiedEvent> observer, CopyOnWriteList<IObserver<ModifiedEvent>> observers)
            {
                this.observer = observer ?? throw new ArgumentNullException(nameof(observer));
                this.observers = observers ?? throw new ArgumentNullException(nameof(observers));
            }

            public void Dispose()
            {
                observers.Remove(observer);
                observer.OnCompleted();
            }
        }

        /// <summary>
        /// Dispose memory file.
        /// </summary>
        public void Dispose()
        {
            // Mark disposed
            isDisposed = true;

            // Remove observers
            while (observers.Count>0)
            {
                var array = observers.Array;
                foreach(var observer in array)
                {
                    observer.OnCompleted();
                    observers.Remove(observer);
                }
            }

            // Dispose lock.
            //m_lock.Dispose();
        }

        /// <summary>
        /// Send change event, if needed.
        /// </summary>
        protected void SendChangeEvent()
        {
            // Don't send if disposed
            if (isDisposed) return;
            // Take snapshot of observers
            IObserver<ModifiedEvent>[] _observers = observers.Array;
            // No observers
            if (_observers.Length == 0) return;
            // Current time
            DateTimeOffset now = DateTimeOffset.UtcNow;
            // Did we already send a notification less than 500ms ago?
            if (now - lastChangeEvent < ChangeEventTolerance) return;
            // Update the time event was notified
            lastChangeEvent = now;
            // Send event.
            foreach (IObserver<ModifiedEvent> observer in observers)
            {
                // Add event
                observer.OnNext(new ModifiedEvent(this, now));
            }
        }

        /// <summary>
        /// Open a new stream to memory file.
        /// </summary>
        /// <param name="fileAccess"></param>
        /// <param name="fileShare"></param>
        /// <returns></returns>
        /// <exception cref="FileSystemExceptionNoReadAccess">No read access</exception>
        /// <exception cref="FileSystemExceptionNoWriteAccess">No write access</exception>
        public Stream Open(FileAccess fileAccess, FileShare fileShare)
        {            
            lock(m_lock)
            {
                bool readAllowed = true, writeAllowed = true;
                foreach (var s in streams)
                {
                    readAllowed &= (s.FileShare & FileShare.Read) == FileShare.Read;
                    writeAllowed &= (s.FileShare & FileShare.Write) == FileShare.Write;
                }
                // Read is not allowed
                if (fileAccess.HasFlag(FileAccess.Read) && !readAllowed) throw new FileSystemExceptionNoReadAccess();
                // Write is not allowed
                if (fileAccess.HasFlag(FileAccess.Write) && !writeAllowed) throw new FileSystemExceptionNoWriteAccess();

                // Create stream
                Stream stream = new Stream(this, data, dataLock, fileAccess, fileShare);
                streams.Add(stream);
                return stream;
            }
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
            /// Bytes
            /// </summary>
            protected List<byte> data;

            /// <summary>
            /// Lock object for modifying <see cref="data"/>.
            /// </summary>
            protected ReaderWriterLock dataLock;

            /// <summary>
            /// File access
            /// </summary>
            public readonly FileAccess FileAccess;

            /// <summary>
            /// Share
            /// </summary>
            public readonly FileShare FileShare;

            /// <summary>
            /// Stream position.
            /// </summary>
            protected long position;

            /// <summary>
            /// Permissions
            /// </summary>
            bool canRead, canWrite;

            /// <summary>
            /// Disposed status
            /// 
            /// 0L - not disposed
            /// 1L - dispose started
            /// 2L - disposed
            /// </summary>
            protected long dispose;

            /// <summary>
            /// Test if stream is disposed
            /// </summary>
            public bool IsDisposed => Interlocked.Read(ref dispose) >= 1L;

            /// <inheritdoc/>
            public override bool CanRead => canRead;
            /// <inheritdoc/>
            public override bool CanSeek => true;
            /// <inheritdoc/>
            public override bool CanWrite => canWrite;

            /// <summary>File length</summary>
            public override long Length
            {
                get
                {
                    lock (dataLock) return data.Count;
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
                    lock (dataLock)
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
            public Stream(MemoryFile parent, List<byte> data, ReaderWriterLock m_lock, FileAccess fileAccess, FileShare fileShare)
            {
                this.parent = parent;
                this.data = data;
                this.dataLock = m_lock;
                this.FileAccess = fileAccess;
                this.FileShare = fileShare;
                this.canRead = (FileAccess & FileAccess.Read) == FileAccess.Read;
                this.canWrite = (FileAccess & FileAccess.Write) == FileAccess.Write;
            }

            /// <summary>No action</summary>
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
                // Assert not disposed
                if (IsDisposed) throw new ObjectDisposedException(nameof(MemoryFile));
                // Assert has read access
                if (!canRead) throw new FileSystemExceptionNoReadAccess();
                // Assert args
                if (buffer == null) throw new ArgumentNullException(nameof(buffer));
                if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
                if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

                // Read
                dataLock.AcquireReaderLock(int.MaxValue);
                try {
                    // Assert arguments
                    if (position < 0L || position > data.Count) throw new ArgumentOutOfRangeException(nameof(Position));
                    int c = Math.Min(/*bytes available*/data.Count-(int)position, /*requested count*/count);
                    data.CopyTo((int)position, buffer, offset, c);
                    position += c;
                    return c;
                } finally
                {
                    dataLock.ReleaseReaderLock();
                }
            }

            /// <summary>
            /// Reads a byte from the stream and advances the position within the stream by one byte, or returns -1 if at the end of the stream.
            /// </summary>
            /// <returns>The unsigned byte cast to an Int32, or -1 if at the end of the stream.</returns>
            /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed.</exception>
            public override int ReadByte()
            {
                // Assert not disposed
                if (IsDisposed) throw new ObjectDisposedException(nameof(MemoryFile));
                // Assert has read access
                if (!canRead) throw new FileSystemExceptionNoReadAccess();

                // Read
                dataLock.AcquireReaderLock(int.MaxValue);
                try
                {
                    if (position < 0 || position >= data.Count) return -1;
                    return data[(int)(position++)];
                }
                finally
                {
                    dataLock.ReleaseReaderLock();
                }
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
                // Assert not disposed
                if (IsDisposed) throw new ObjectDisposedException(nameof(MemoryFile));

                dataLock.AcquireReaderLock(int.MaxValue);
                try
                {
                    if (origin == SeekOrigin.Begin) return position = offset;
                    if (origin == SeekOrigin.Current) return position += offset;
                    if (origin == SeekOrigin.End) return (position = data.Count - offset);
                    return 0L;
                }
                finally
                {
                    dataLock.ReleaseReaderLock();
                }
            }

            /// <summary>
            /// Sets the length of the current stream.
            /// </summary>
            /// <param name="newLength">The desired length of the current stream in bytes.</param>
            /// <exception cref="IOException">An I/O error occurs</exception>
            /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed.</exception>
            public override void SetLength(long newLength)
            {
                // Assert not disposed
                if (IsDisposed) throw new ObjectDisposedException(nameof(MemoryFile));
                // Assert has write access
                if (!canWrite) throw new FileSystemExceptionNoWriteAccess();
                // Assert args
                if (newLength < 0 || newLength>Int32.MaxValue) throw new ArgumentOutOfRangeException(nameof(newLength));

                // Write
                dataLock.AcquireWriterLock(int.MaxValue);
                try
                {
                    // new length
                    int c = (int)newLength;
                    // Shorten
                    if (data.Count > c)
                    {
                        data.RemoveRange(c, data.Count - c);
                        if (position > newLength) position = newLength;
                    } else 
                    // Grow
                    if (data.Count < c)
                    {
                        while (data.Count < c) data.Add(0);
                    }
                }
                finally
                {
                    dataLock.ReleaseWriterLock();
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
                // Assert not disposed
                if (IsDisposed) throw new ObjectDisposedException(nameof(MemoryFile));
                // Assert has write access
                if (!canWrite) throw new FileSystemExceptionNoWriteAccess();
                // Assert args
                if (buffer == null) throw new ArgumentNullException(nameof(buffer));
                if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
                if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
                if (offset+count > buffer.Length) throw new ArgumentOutOfRangeException(nameof(count));

                // Write
                dataLock.AcquireWriterLock(int.MaxValue);
                try
                {
                    // Assert
                    if (position < 0L || position > data.Count) throw new ArgumentOutOfRangeException(nameof(Position));

                    // Position (int)
                    int p = (int)position;

                    // Overwrite
                    if (p < data.Count)
                    {
                        // Bytes to overwrite
                        int c = Math.Min(/*Bytes until end*/data.Count - p, /*Writes that need writing*/count);
                        // Write
                        for (int i = 0; i < c; i++) data[p++] = buffer[offset++];
                        count -= c;
                        offset += c;
                    }

                    // Append
                    if (p>=data.Count)
                    {
                        // Append byte
                        while (count-- > 0)
                        {
                            data.Add(buffer[offset++]);
                            p++;
                        }                        
                    }

                    // Update position
                    position = p;
                }
                finally
                {
                    dataLock.ReleaseWriterLock();
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
                // Assert not disposed
                if (IsDisposed) throw new ObjectDisposedException(nameof(MemoryFile));
                // Assert has write access
                if (!canWrite) throw new FileSystemExceptionNoWriteAccess();

                // Write
                dataLock.AcquireWriterLock(int.MaxValue);
                try
                {
                    // Assert
                    if (position < 0L || position > data.Count) throw new ArgumentOutOfRangeException(nameof(Position));
                    // Position (int)
                    int p = (int)position;
                    // Overwrite
                    if (position < data.Count) data[p++] = value;
                    // Append
                    else if (position == data.Count) 
                    {
                        data.Add(value);
                        p++;
                    }
                    // Update position
                    position = p;
                }
                finally
                {
                    dataLock.ReleaseWriterLock();
                }
            }

            /// <summary>
            /// Close stream, relase share protections in <see cref="MemoryFile"/>.
            /// </summary>
            /// <param name="disposing"></param>
            protected override void Dispose(bool disposing)
            {
                // Start dispose
                Interlocked.CompareExchange(ref dispose, 1L, 0L);
                // Remove self from parent
                lock(parent.m_lock) parent.streams.Remove(this);                
                // Dispose stream
                base.Dispose(disposing);
                // End dispose
                Interlocked.CompareExchange(ref dispose, 2L, 1L);
            }
        }
    }
}