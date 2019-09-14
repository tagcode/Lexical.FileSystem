// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Lexical.FileSystem
{
    /// <summary>
    /// In-memory filesystem
    /// </summary>
    public class MemoryFileSystem : IFileSystemBrowse, IFileSystemCreateDirectory, IFileSystemDelete, IFileSystemObserve, IFileSystemMove, IFileSystemOpen
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
        public IFileSystemEntry GetEntry(string path)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public void Move(string oldPath, string newPath)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public IFileSystemObserverHandle Observe(string filter, IObserver<IFileSystemEvent> observer, object state = null)
        {
            throw new NotImplementedException();
        }

        public Stream Open(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// In-memory directory where in-memory files can be created.
    /// </summary>
    class MemoryDirectory
    {
        /// <summary>
        /// Lock to data and file.
        /// </summary>
        protected object m_lock = new object();

        /// <summary>
        /// Subdirectories. Lazy construction. Modified under <see cref="m_lock"/>.
        /// </summary>
        Dictionary<string, MemoryDirectory> directories;

        /// <summary>
        /// Files. Lazy construction. Modified under <see cref="m_lock"/>.
        /// </summary>
        Dictionary<string, MemoryFile> files;


    }

    /// <summary>
    /// Memory file
    /// </summary>
    class MemoryFile
    {
        /// <summary>
        /// Data
        /// </summary>
        protected List<byte> data = new List<byte>();

        /// <summary>
        /// Lock to data and file.
        /// </summary>
        protected object m_lock = new object();
    }

    /// <summary>
    /// Stream to <see cref="MemoryFile"/>.
    /// </summary>
    class MemoryFileStream : Stream
    {
        /// <summary>
        /// Data
        /// </summary>
        protected List<byte> data = new List<byte>();

        /// <summary>
        /// Lock handle to <see cref="data"/>.
        /// </summary>
        protected object m_lock;

        /// <summary>
        /// Stream file mode
        /// </summary>
        protected bool canRead, canWrite;

        /// <summary>
        /// Stream position.
        /// </summary>
        protected long position;

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
