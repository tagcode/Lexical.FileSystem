// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Lexical.FileSystem.Utility
{
    /// <summary>
    /// Memory based file. File is based on blocks (1024 bytes with default settings).
    /// 
    /// Multiple concurrent streams can be opened to the file. 
    /// 
    /// Maximum length of the file is <see cref="int.MaxValue"/>*<see cref="BlockSize"/>.
    /// 
    /// Disposing is optional and can be left to be garbage collected. Disposing, how ever, closes observers.
    /// </summary>
    public class MemoryFile : IObservable<MemoryFile.ModifiedEvent>, IDisposable
    {
        /// <summary>
        /// Allocated blocks
        /// </summary>
        protected internal List<byte[]> blocks = new List<byte[]>();

        /// <summary>
        /// Lock for modifying <see cref="blocks"/>.
        /// </summary>
        protected ReaderWriterLock blockLock = new ReaderWriterLock();

        /// <summary>
        /// Critical section lock for opening streams, checks read/write permission.
        /// </summary>
        protected object m_stream_lock = new object();

        /// <summary>
        /// Observers
        /// </summary>
        ArrayList<IObserver<ModifiedEvent>> observers = new ArrayList<IObserver<ModifiedEvent>>();

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
        /// Total length.
        /// </summary>
        protected internal long length;

        /// <summary>
        /// File length
        /// </summary>
        public long Length => length;

        /// <summary>
        /// Datetime when file was last modified
        /// </summary>
        public DateTimeOffset LastModified { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Block size
        /// </summary>
        public readonly long BlockSize;

        /// <summary>
        /// Block pool that allocates memory blocks.
        /// </summary>
        protected IBlockPool blockPool;

        /// <summary>Path hint provided in construction. Used in exceptions and in ToString(). Readable as public property.</summary>
        public string Path { get; protected set; }

        /// <summary>
        /// Create memory based file.
        /// </summary>
        public MemoryFile()
        {
            this.BlockSize = 1024;
            this.blockPool = new BlockPoolPseudo((int)BlockSize);
        }

        /// <summary>
        /// Create memory based file.
        /// </summary>
        /// <param name="blockSize"></param>
        public MemoryFile(int blockSize)
        {
            if (blockSize < 16) throw new ArgumentOutOfRangeException(nameof(blockSize));
            this.BlockSize = blockSize;
            this.blockPool = new BlockPoolPseudo(blockSize);
        }

        /// <summary>
        /// Create memory based file.
        /// </summary>
        /// <param name="blockPool"></param>
        public MemoryFile(IBlockPool blockPool)
        {
            this.blockPool = blockPool ?? throw new ArgumentNullException(nameof(blockPool));
            this.BlockSize = blockPool.BlockSize;
        }

        /// <summary>
        /// Create memory based file.
        /// </summary>
        /// <param name="blockPool"></param>
        /// <param name="pathHint">Path to use in ToString()</param>
        public MemoryFile(IBlockPool blockPool, string pathHint = null)
        {
            this.blockPool = blockPool ?? throw new ArgumentNullException(nameof(blockPool));
            this.BlockSize = blockPool.BlockSize;
            this.Path = pathHint;
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
            ArrayList<IObserver<ModifiedEvent>> observers;

            public ObserverHandle(IObserver<ModifiedEvent> observer, ArrayList<IObserver<ModifiedEvent>> observers)
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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose memory file.
        /// </summary>
        /// <param name="disposing">if false, called from finalized and needs to dispose only unmanaged resources</param>
        protected virtual void Dispose(bool disposing)
        {
            // Mark disposed
            isDisposed = true;

            // Return blocks
            {
                var _blockPool = this.blockPool;
                if (_blockPool is BlockPoolPseudo == false)
                {
                    // Clear blocks
                    bool clear = false;
                    lock (m_stream_lock) clear = streams.Count == 0;
                    if (clear) Clear();
                }
            }

            // Dispose managed resources
            if (disposing)
            {
                // Remove observers
                while (observers.Count > 0)
                {
                    var array = observers.Array;
                    foreach (var observer in array)
                    {
                        observer.OnCompleted();
                        observers.Remove(observer);
                    }
                }
            }
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
            foreach (IObserver<ModifiedEvent> observer in _observers)
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
            lock (m_stream_lock)
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
                Stream stream = new Stream(this, fileAccess, fileShare);
                streams.Add(stream);
                return stream;
            }
        }

        /// <summary>
        /// Clear file
        /// </summary>
        public void Clear()
        {
            // Take reference of blockpool
            var _blockPool = this.blockPool;
            if (_blockPool == null) return;

            // Write
            blockLock.AcquireWriterLock(int.MaxValue);
            try
            {
                foreach (byte[] block in blocks)
                    _blockPool.Return(block);
                blocks.Clear();
                length = 0L;
            } finally
            {
                blockLock.ReleaseWriterLock();
            }
        }

        /// <summary>
        /// Stream to <see cref="MemoryFile"/>.
        /// </summary>
        public class Stream : StreamDisposeList
        {
            /// <summary>
            /// Parent
            /// </summary>
            protected MemoryFile parent;

            /// <summary>
            /// Blocks
            /// </summary>
            protected List<byte[]> blocks;

            /// <summary>
            /// Block lock object for modifying <see cref="blocks"/>.
            /// </summary>
            protected ReaderWriterLock blockLock;

            /// <summary>
            /// Block size
            /// </summary>
            protected long blockSize;

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

            /// <inheritdoc/>
            public override bool CanRead => canRead;
            /// <inheritdoc/>
            public override bool CanSeek => true;
            /// <inheritdoc/>
            public override bool CanWrite => canWrite;

            /// <summary>File length</summary>
            public override long Length => parent.Length;

            /// <summary>
            /// Position of the stream.
            /// </summary>
            public override long Position
            {
                get => position;
                set
                {
                    if (value < 0) throw new ArgumentOutOfRangeException("position");
                    position = value;
                }
            }

            /// <summary>
            /// Create stream.
            /// </summary>
            /// <param name="parent"></param>
            /// <param name="fileAccess"></param>
            /// <param name="fileShare"></param>
            public Stream(MemoryFile parent, FileAccess fileAccess, FileShare fileShare)
            {
                this.parent = parent;
                this.blocks = parent.blocks;
                this.blockLock = parent.blockLock;
                this.blockSize = parent.BlockSize;
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
                blockLock.AcquireReaderLock(int.MaxValue);
                try
                {
                    // Position for this thread.
                    long _position = position;
                    // Assert arguments
                    if (_position < 0L || _position > parent.length) throw new ArgumentOutOfRangeException(nameof(Position));
                    // Number of bytes to read
                    int bytesToRead = (int)Math.Min(/*bytes available*/parent.length - _position, /*requested count*/count);
                    // Bytes to go
                    count = bytesToRead;
                    // Read until c is 0
                    while (count > 0)
                    {
                        int blockIndex = (int)(_position / blockSize);
                        int blockPosition = (int)(_position % blockSize);
                        byte[] block = blocks[blockIndex];
                        int bytesToReadFromBlock = (int)Math.Min(/*Bytes remaining in block*/blockSize - blockPosition, /*bytes to read*/count);
                        Array.Copy(block, blockPosition, buffer, offset, bytesToReadFromBlock);
                        offset += bytesToReadFromBlock;
                        count -= bytesToReadFromBlock;
                        _position += bytesToReadFromBlock;
                    }
                    // Set stream position
                    position = _position;
                    // How many bytes were read
                    return bytesToRead;
                }
                finally
                {
                    blockLock.ReleaseReaderLock();
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
                blockLock.AcquireReaderLock(int.MaxValue);
                try
                {
                    // Position for this thread.
                    long _position = position;
                    // Asserts
                    if (position < 0 || position >= parent.Length) return -1;
                    //
                    int blockIndex = (int)(_position / blockSize);
                    int blockPosition = (int)(_position % blockSize);
                    byte[] block = blocks[blockIndex];
                    // Update position
                    position = _position + 1L;
                    return block[blockPosition];
                }
                finally
                {
                    blockLock.ReleaseReaderLock();
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

                if (origin == SeekOrigin.Begin) return position = offset;
                if (origin == SeekOrigin.Current) return position += offset;
                if (origin == SeekOrigin.End) return (position = blocks.Count - offset);
                throw new ArgumentException(nameof(origin));
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
                if (newLength < 0 || newLength > Int32.MaxValue) throw new ArgumentOutOfRangeException(nameof(newLength));

                // Write
                blockLock.AcquireWriterLock(int.MaxValue);
                try
                {
                    // Nothing to do
                    if (newLength == parent.length) { }
                    // Clear
                    else if (newLength == 0L)
                    {
                        if (blocks.Count > 0)
                        {
                            parent.blockPool.Return(blocks.ToArray());
                            blocks.Clear();
                        }
                        parent.length = 0L;
                    }
                    else
                    {
                        // Count
                        int newBlockCount = (int)((newLength + blockSize - 1) / blockSize);
                        // Reduce block count
                        if (newLength < parent.Length)
                        {
                            // Remove blocks
                            while (newBlockCount < blocks.Count)
                            {
                                int blockIx = blocks.Count - 1;
                                byte[] block = blocks[blockIx];
                                blocks.RemoveAt(blockIx);
                                parent.blockPool.Return(block);
                            }
                        }
                        else
                        // Grow
                        if (newLength > parent.Length)
                        {
                            // Zero last bytes of last block
                            if (blocks.Count > 0)
                            {
                                byte[] block = blocks[blocks.Count - 1];
                                int lastByteInLastBlock = (int)(parent.Length % blockSize);
                                int lastByteInLastBlockInNewLength = newLength > blocks.Count * blockSize ? (int)blockSize : (int)(newLength % blockSize);
                                for (int i = lastByteInLastBlock; i < lastByteInLastBlockInNewLength; i++) block[i] = 0;
                            }
                            // Add blocks
                            while (newBlockCount > blocks.Count)
                            {
                                byte[] block;
                                // Allocate block
                                if (parent.blockPool.TryAllocate(out block))
                                {
                                    // Clean block if needed
                                    if (!parent.blockPool.ClearsRecycledBlocks) WipeBlock(block);
                                    // Add block
                                    blocks.Add(block);
                                    // Set length
                                    parent.length = Math.Min(newLength, blocks.Count * blockSize);
                                }
                                // Ran out of disk space
                                else throw new FileSystemExceptionOutOfDiskSpace(null, parent.Path);
                            }
                        }
                    }
                    // Set new length
                    parent.length = newLength;
                    if (position > newLength) position = newLength;
                }
                finally
                {
                    blockLock.ReleaseWriterLock();
                }

                // Send event
                parent.SendChangeEvent();
            }

            /// <summary>Zero <paramref name="block"/> contents.</summary>
            /// <param name="block"></param>
            static void WipeBlock(byte[] block) { for (int i = 0; i < block.Length; i++) block[i] = 0; }

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
                if (offset + count > buffer.Length) throw new ArgumentOutOfRangeException(nameof(count));
                // Nothing to do
                if (count == 0) return;

                // Write
                blockLock.AcquireWriterLock(int.MaxValue);
                try
                {
                    // Assert
                    if (position < 0L) throw new ArgumentOutOfRangeException(nameof(Position));

                    // Position of this stream
                    long _position = position;

                    // Overwrite to existing blocks
                    long maxLength = blocks.Count * blockSize;
                    if (_position < maxLength)
                    {
                        // Bytes to overwrite
                        long bytesToOverwrite = Math.Min(/*Bytes until end*/maxLength - _position, /*Writes that need writing*/count);
                        // Write
                        while (bytesToOverwrite > 0L)
                        {
                            int blockIndex = (int)(_position / blockSize);
                            int blockPosition = (int)(_position % blockSize);
                            int bytesToWriteToThisBlock = (int)Math.Min(/*bytes remaining in block*/blockSize - blockPosition, /*bytes to write*/bytesToOverwrite);
                            byte[] block = blocks[blockIndex];
                            Array.Copy(buffer, offset, block, blockPosition, bytesToWriteToThisBlock);
                            offset += bytesToWriteToThisBlock;
                            _position += bytesToWriteToThisBlock;
                            bytesToOverwrite -= bytesToWriteToThisBlock;
                            count -= bytesToWriteToThisBlock;
                            if (parent.length < _position) parent.length = _position;
                        }
                    }

                    // Append to new blocks
                    if (_position >= parent.length)
                    {
                        while (count > 0)
                        {
                            byte[] block;
                            // Allocate block
                            if (parent.blockPool.TryAllocate(out block)) blocks.Add(block);
                            // Ran out of disk space
                            else throw new FileSystemExceptionOutOfDiskSpace(null, parent.Path);

                            int bytesToWriteToThisBlock = (int)Math.Min(/*bytes remaining in block*/block.Length, /*bytes to write*/count);
                            Array.Copy(buffer, offset, block, 0, bytesToWriteToThisBlock);
                            offset += bytesToWriteToThisBlock;
                            _position += bytesToWriteToThisBlock;
                            count -= bytesToWriteToThisBlock;
                            position = _position;
                            if (parent.length < _position) parent.length = _position;
                        }
                    }

                    // Update position
                    position = _position;
                }
                finally
                {
                    blockLock.ReleaseWriterLock();
                }

                // Send event
                parent.SendChangeEvent();
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
                blockLock.AcquireWriterLock(int.MaxValue);
                try
                {
                    // Assert
                    if (position < 0L) throw new ArgumentOutOfRangeException(nameof(Position));
                    // Position of this thread
                    long _position = position;
                    // Write in existing block
                    long maxLength = blocks.Count * blockSize;
                    if (_position < maxLength)
                    {
                        int blockIndex = (int)(_position / blockSize);
                        int blockPosition = (int)(_position % blockSize);
                        byte[] block = blocks[blockIndex];
                        block[blockPosition] = value;
                        _position++;
                        position = _position;
                        if (parent.length < _position) parent.length = _position;
                    }
                    else
                    // Append block
                    {
                        byte[] block = null;
                        while (_position >= blocks.Count * blockSize)
                        {
                            // Allocate block
                            if (parent.blockPool.TryAllocate(out block)) blocks.Add(block);
                            // Ran out of disk space
                            else throw new FileSystemExceptionOutOfDiskSpace(null, parent.Path);
                        }
                        int blockPosition = (int)(_position % blockSize);
                        block[blockPosition] = value;
                        _position++;
                        position = _position;
                        if (parent.length < _position) parent.length = _position;
                    }
                }
                finally
                {
                    blockLock.ReleaseWriterLock();
                }

                // Send event
                parent.SendChangeEvent();
            }

            /// <summary>
            /// Close stream, relase share protections in <see cref="MemoryFile"/>.
            /// </summary>
            protected override void InnerDispose(ref StructList4<Exception> disposeErrors)
            {
                bool clearParent = false;
                // Remove self from parent
                lock (parent.m_stream_lock)
                {
                    bool removed = parent.streams.Remove(this);
                    clearParent = removed && parent.streams.Count == 0 && parent.isDisposed;
                }

                if (clearParent) parent.Clear();
            }
        }

        /// <summary>Print info</summary>
        public override string ToString()
            => Path != null ? Path : GetType().Name;
    }
}
