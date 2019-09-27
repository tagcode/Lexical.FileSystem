// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           27.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Lexical.FileSystem.Utility
{
    /// <summary>
    /// Base implementation of <see cref="Stream"/> decoration.
    /// </summary>
    public class StreamDecoration : Stream
    {
        /// <summary>
        /// Source stream.
        /// </summary>
        public readonly Stream Source;

        /// <summary>
        /// Create <see cref="Stream"/> decoration.
        /// </summary>
        /// <param name="sourceStream">source stream that is to be decorated</param>
        public StreamDecoration(Stream sourceStream)
        {
            this.Source = sourceStream ?? throw new ArgumentNullException(nameof(sourceStream));
        }

        /// <inheritdoc/>
        public override bool CanRead => Source.CanRead;
        /// <inheritdoc/>
        public override bool CanSeek => Source.CanSeek;
        /// <inheritdoc/>
        public override bool CanWrite => Source.CanWrite;
        /// <inheritdoc/>
        public override long Length => Source.Length;
        /// <inheritdoc/>
        public override long Position { get => Source.Position; set => Source.Position = value; }
        /// <inheritdoc/>
        public override void Flush() => Source.Flush();
        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count) => Source.Read(buffer, offset, count);
        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin) => Source.Seek(offset, origin);
        /// <inheritdoc/>
        public override void SetLength(long value) => Source.SetLength(value);
        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count) => Source.Write(buffer, offset, count);
        /// <inheritdoc/>
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) => Source.BeginRead(buffer, offset, count, callback, state);
        /// <inheritdoc/>
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) => Source.BeginWrite(buffer, offset, count, callback, state);
        /// <inheritdoc/>
        public override bool CanTimeout => Source.CanTimeout;
        /// <inheritdoc/>
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => Source.CopyToAsync(destination, bufferSize, cancellationToken);
        /// <inheritdoc/>
        public override int EndRead(IAsyncResult asyncResult) => Source.EndRead(asyncResult);
        /// <inheritdoc/>
        public override void EndWrite(IAsyncResult asyncResult) => Source.EndWrite(asyncResult);
        /// <inheritdoc/>
        public override Task FlushAsync(CancellationToken cancellationToken) => Source.FlushAsync(cancellationToken);
        /// <inheritdoc/>
        public override object InitializeLifetimeService() => Source.InitializeLifetimeService();
        /// <inheritdoc/>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => Source.ReadAsync(buffer, offset, count, cancellationToken);
        /// <inheritdoc/>
        public override int ReadByte() => Source.ReadByte();
        /// <inheritdoc/>
        public override int ReadTimeout { get => Source.ReadTimeout; set => Source.ReadTimeout = value; }
        /// <inheritdoc/>
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => Source.WriteAsync(buffer, offset, count, cancellationToken);
        /// <inheritdoc/>
        public override void WriteByte(byte value) => Source.WriteByte(value);
        /// <inheritdoc/>
        public override int WriteTimeout { get => Source.WriteTimeout; set => Source.WriteTimeout = value; }
        /// <inheritdoc/>
        public override void Close() => Source.Close();
        /// <inheritdoc/>
        protected override void Dispose(bool disposing) => Source.Dispose();
        /// <inheritdoc/>
        public override bool Equals(object obj) => Source.Equals(obj);
        /// <inheritdoc/>
        public override int GetHashCode() => Source.GetHashCode();
        /// <inheritdoc/>
        public override string ToString() => $"{GetType().Name}({Source})";
    }
}
