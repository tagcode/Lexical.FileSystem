// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           29.10.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.IO;

namespace Lexical.FileSystem.Utility
{
    /// <summary>
    /// Memory pool that can lease memory.
    /// 
    /// See subinterfaces:
    /// <list type="bullet">
    ///     <item><see cref="IMemoryPool{T}"/></item>
    ///     <item><see cref="IMemoryPoolArray"/></item>
    ///     <item><see cref="IMemoryPoolBlock"/></item>
    /// </list>
    /// </summary>
    public interface IMemoryPool
    {
        /// <summary>Total amount of bytes available and allocated.</summary>
        long TotalLength { get; }
        /// <summary>Bytes allocated to leasers</summary>
        long Allocated { get; }
        /// <summary>Available for allocation</summary>
        long Available { get; }
        /// <summary>Policy whether return memory is cleared.</summary>
        bool ClearsRecycledBlocks { get; set; }
        /// <summary><see cref="IMemory"/> subtypes that are supported by this pool.</summary>
        Type[] MemoryTypes { get; }
    }

    /// <summary>
    /// Memory pool that can lease memory as <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IMemoryPool<T> : IMemoryPool where T : IMemory
    {
        /// <summary>
        /// Try to allocate memory, if not enough memory is available, then returns false and null.
        /// </summary>
        /// <param name="length"></param>
        /// <param name="memory"></param>
        /// <returns>true if <paramref name="memory"/> was placed with block, false if there were not free space</returns>
        /// <exception cref="IOException"></exception>
        bool TryAllocate(long length, out T memory);

        /// <summary>
        /// Allocate memory. If not enough memory is available, then waits until there is.
        /// </summary>
        /// <returns>block</returns>
        /// <exception cref="IOException"></exception>
        T Allocate(long length);
    }

    /// <summary>
    /// Memory pool that can lease byte arrays.
    /// </summary>
    public interface IMemoryPoolArray : IMemoryPool<IMemoryArray>
    {
    }

    /// <summary>
    /// Memory pool that can lease blocks.
    /// </summary>
    public interface IMemoryPoolBlock : IMemoryPool<IMemoryBlock>
    {
    }

    /// <summary>
    /// Abstract memory block. Dispose to return to pool.
    /// </summary>
    public interface IMemory : IDisposable
    {
        /// <summary>Leasing pool.</summary>
        IMemoryPool Pool { get; }
        /// <summary>Block length</summary>
        long Length { get; }
    }

    /// <summary>
    /// Leased memory block that can be written and read. Dispose to return to pool.
    /// </summary>
    public interface IMemoryBlock : IMemory
    {
        /// <summary>
        /// Read from block.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <param name="buffer"></param>
        /// <exception cref="IOException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ArgumentException">The sum of offset and count is larger than the buffer length.</exception>
        /// <exception cref="ObjectDisposedException">object has been returned to the leaser</exception>
        void Read(long offset, int length, byte[] buffer);

        /// <summary>
        /// Write to block.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <param name="buffer"></param>
        /// <exception cref="IOException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ArgumentException">The sum of offset and count is larger than the buffer length.</exception>
        /// <exception cref="ObjectDisposedException">object has been returned to the leaser</exception>
        void Write(long offset, int length, byte[] buffer);
    }

    /// <summary>
    /// Leased memory array. Dispose to return to pool.
    /// </summary>
    public interface IMemoryArray : IMemory
    {
        /// <summary>Reference to array.</summary>
        byte[] Array { get; }
    }

    /// <summary>
    /// Memory pool extensions
    /// </summary>
    public static class IMemoryPoolExtensions
    {
        /// <summary>
        /// Try to allocate memory, if not enough memory is available, then returns false and null.
        /// </summary>
        /// <param name="memoryPool"></param>
        /// <param name="length"></param>
        /// <param name="memory"></param>
        /// <returns>true if <paramref name="memory"/> was placed with block, false if there were not free space</returns>
        /// <exception cref="IOException"></exception>
        /// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not supported.</exception>
        public static bool TryAllocate<T>(this IMemoryPool memoryPool, long length, out T memory) where T : IMemory
        {
            if (memoryPool is IMemoryPool<T> t_pool) return t_pool.TryAllocate(length, out memory);
            memory = default;
            return false;
        }

        /// <summary>
        /// Allocate memory. If not enough memory is available, then waits until there is.
        /// </summary>
        /// <param name="memoryPool"></param>
        /// <param name="length"></param>
        /// <returns>block</returns>
        /// <exception cref="IOException"></exception>
        /// <exception cref="NotSupportedException">if <typeparamref name="T"/> is not supported.</exception>
        public static T Allocate<T>(this IMemoryPool memoryPool, long length) where T : IMemory
            => memoryPool is IMemoryPool<T> t_pool ? t_pool.Allocate<T>(length) : throw new NotSupportedException(nameof(T));

    }

}
