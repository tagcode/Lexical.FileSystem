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
        /// <inheritdoc/>
        public virtual FileSystemFeatures Features => FileSystemFeatures.CaseSensitive;
        /// <inheritdoc/>
        public virtual bool CanBrowse => throw new NotImplementedException();
        /// <inheritdoc/>
        public virtual bool CanGetEntry => throw new NotImplementedException();
        /// <inheritdoc/>
        public virtual bool CanCreateDirectory => throw new NotImplementedException();
        /// <inheritdoc/>
        public virtual bool CanDelete => throw new NotImplementedException();
        /// <inheritdoc/>
        public virtual bool CanObserve => throw new NotImplementedException();
        /// <inheritdoc/>
        public virtual bool CanMove => throw new NotImplementedException();
        /// <inheritdoc/>
        public virtual bool CanOpen => throw new NotImplementedException();
        /// <inheritdoc/>
        public virtual bool CanRead => throw new NotImplementedException();
        /// <inheritdoc/>
        public virtual bool CanWrite => throw new NotImplementedException();
        /// <inheritdoc/>
        public virtual bool CanCreateFile => throw new NotImplementedException();

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

        /// <summary>
        /// Create new in-memory filesystem.
        /// </summary>
        public MemoryFileSystem()
        {
            root = new Directory(this, "", "", DateTimeOffset.UtcNow);
            this.taskFactory = Task.Factory;
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
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        public IFileSystemEntry[] Browse(string path)
        {
            m_lock.EnterReadLock();
            try
            {

            } finally
            {
                m_lock.ExitReadLock();
            }
            Entry entry = root;
            PathEnumerator enumr = new PathEnumerator(path);
            while (enumr.MoveNext())
            {
                // "" Represents current dir
                if (StringSegment.Comparer.Instance.Equals(enumr.Current, StringSegment.Empty)) continue;

                // Get entry under lock.
                Entry child;
                    if (entry is Directory dir)
                    {
                        // Failed to find child entry
                        if (!dir.contents.TryGetValue(enumr.Current, out child)) throw new DirectoryNotFoundException(path);
                    } else {
                        // Parent is a file and cannot contain futher subentries.
                        throw new DirectoryNotFoundException(path);
                    }
            }

            // Create entry
            if (entry is Directory dir_)
            {
                // List entries
                    int c = dir_.contents.Count;
                    IFileSystemEntry[] array = new IFileSystemEntry[c];
                    int i = 0;
                    foreach (Entry e in dir_.contents.Values)
                        array[i++] = e.CreateEntry();
                    return array;
            } else
            // List file entry
            if (entry is File)
            {
                return new IFileSystemEntry[] { entry.CreateEntry() };
            }
            // Entry was not dir or file
            throw new DirectoryNotFoundException(path);
        }

        /// <summary>
        /// Get file or path entry.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>entry or null</returns>
        Entry FindEntry(string path)
        {
            Entry entry = root;
            PathEnumerator enumr = new PathEnumerator(path);
            while (enumr.MoveNext())
            {
                // "" Represents current dir
                if (StringSegment.Comparer.Instance.Equals(enumr.Current, StringSegment.Empty)) continue;

                // Get entry under lock.
                Entry child;
                    if (entry is Directory dir)
                    {
                        // Failed to find child entry
                        if (!dir.contents.TryGetValue(enumr.Current, out child)) return null;
                    }
                    else
                    {
                        // Parent is a file and cannot contain futher subentries.
                        return null;
                    }
            }
            return entry;
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
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        public IFileSystemEntry GetEntry(string path)
        {
            Entry entry = root;
            PathEnumerator enumr = new PathEnumerator(path);
            while (enumr.MoveNext())
            {
                // "" Represents current dir
                if (StringSegment.Comparer.Instance.Equals(enumr.Current, StringSegment.Empty)) continue;

                // Get entry under lock.
                Entry child;
                    if (entry is Directory dir)
                    {
                        // Failed to find child entry
                        if (!dir.contents.TryGetValue(enumr.Current, out child)) throw new DirectoryNotFoundException(path);
                    }
                    else
                    {
                        // Parent is a file and cannot contain futher subentries.
                        throw new DirectoryNotFoundException(path);
                    }
            }

            // Return entry
            return entry.CreateEntry();
        }

        /// <inheritdoc/>
        public void CreateDirectory(string path)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public void Delete(string path, bool recursive = false)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public void Move(string oldPath, string newPath)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Stream Open(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public IFileSystemObserverHandle Observe(string filter, IObserver<IFileSystemEvent> observer, object state = null)
        {
            ObserverHandle handle = new ObserverHandle(this, filter, observer, state);
            observers.Add(handle);
            return handle;
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
            base.AddDisposableBase(disposable);
            return this;
        }

        /// <summary>
        /// Add <paramref name="disposables"/> to list of objects to be disposed along with the system.
        /// </summary>
        /// <param name="disposables"></param>
        /// <returns>filesystem</returns>
        public MemoryFileSystem AddDisposables(IEnumerable<object> disposables)
        {
            base.AddDisposablesBase(disposables);
            return this;
        }

        /// <summary>
        /// Remove <paramref name="disposable"/> from dispose list.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns></returns>
        public MemoryFileSystem RemoveDisposable(object disposable)
        {
            base.RemoveDisposableBase(disposable);
            return this;
        }

        /// <summary>
        /// Remove <paramref name="disposables"/> from dispose list.
        /// </summary>
        /// <param name="disposables"></param>
        /// <returns></returns>
        public MemoryFileSystem RemoveDisposables(IEnumerable<object> disposables)
        {
            base.RemoveDisposablesBase(disposables);
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
                => filterPattern.IsMatch(path) || /*workaround*/filterPattern.IsMatch("/" + path);

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
        abstract class Entry
        {
            /// <summary>
            /// Path to the entry.
            /// </summary>
            protected internal string path;

            /// <summary>
            /// Name of the entry.
            /// </summary>
            protected internal string name;

            /// <summary>
            /// Last modified time.
            /// </summary>
            protected DateTimeOffset lastModified;

            /// <summary>
            /// Parent filesystem.
            /// </summary>
            protected MemoryFileSystem filesystem;

            /// <summary>
            /// Create entry
            /// </summary>
            /// <param name="filesystem"></param>
            /// <param name="path"></param>
            /// <param name="name"></param>
            /// <param name="lastModified"></param>
            protected Entry(MemoryFileSystem filesystem, string path, string name, DateTimeOffset lastModified)
            {
                this.filesystem = filesystem ?? throw new ArgumentNullException(nameof(filesystem));
                this.path = path ?? throw new ArgumentNullException(nameof(path));
                this.name = name ?? throw new ArgumentNullException(nameof(name));
                this.lastModified = lastModified;
            }

            /// <summary>
            /// Create entry snapshot.
            /// </summary>
            /// <returns></returns>
            public abstract IFileSystemEntry CreateEntry();
        }

        /// <summary>
        /// In-memory directory where in-memory files can be created.
        /// </summary>
        class Directory : Entry
        {
            /// <summary>
            /// Files and directories. Lazy construction. Modified under m_lock.
            /// </summary>
            protected internal Dictionary<StringSegment, Entry> contents = new Dictionary<StringSegment, Entry>();

            /// <summary>
            /// Create directory entry
            /// </summary>
            /// <param name="filesystem"></param>
            /// <param name="path"></param>
            /// <param name="name"></param>
            /// <param name="lastModified"></param>
            public Directory(MemoryFileSystem filesystem, string path, string name, DateTimeOffset lastModified) : base(filesystem, path, name, lastModified)
            {
            }

            /// <inheritdoc/>
            public IFileSystemEntry[] Browse(string path)
            {
                throw new NotImplementedException();
            }
            /// <inheritdoc/>
            public void CreateDirectory(string path)
            {
                throw new NotImplementedException();
            }
            /// <inheritdoc/>
            public void Delete(string path, bool recursive = false)
            {
                throw new NotImplementedException();
            }
            /// <inheritdoc/>
            public void Move(string oldPath, string newPath)
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Create entry snapshot.
            /// </summary>
            /// <returns></returns>
            public override IFileSystemEntry CreateEntry()
                => new FileSystemEntryDirectory(filesystem, path, name, lastModified);
        }


        /// <summary>
        /// Memory file
        /// </summary>
        class File : Entry
        {
            /// <summary>
            /// Memory file
            /// </summary>
            protected internal MemoryFile memoryFile = new MemoryFile();

            /// <summary>
            /// Create file entry.
            /// </summary>
            /// <param name="filesystem"></param>
            /// <param name="path"></param>
            /// <param name="name"></param>
            /// <param name="lastModified"></param>
            public File(MemoryFileSystem filesystem, string path, string name, DateTimeOffset lastModified) : base(filesystem, path, name, lastModified)
            {
            }

            /// <summary>
            /// Create entry snapshot.
            /// </summary>
            /// <returns></returns>
            public override IFileSystemEntry CreateEntry()
            {
                // Create entry snapshot
                return new FileSystemEntryFile(filesystem, path, name, memoryFile.LastModified, memoryFile.Length);
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
