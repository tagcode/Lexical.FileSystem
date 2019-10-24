// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           17.10.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System.Collections.Generic;

namespace Lexical.FileSystem.Utility
{
    /// <summary>
    /// Block pool allocates and recycles memory blocks that are used for buffers.
    /// </summary>
    public interface IBlockPool
    {
        /// <summary>Block size, typically 4096</summary>
        int BlockSize { get; }
        /// <summary>Maximum number of blocks to dispence</summary>
        long MaxBlockCount { get; }
        /// <summary>Maximum number of blocks to keep in recycle queue</summary>
        int MaxRecycleQueue { get; }
        /// <summary>Clears recycled blocks</summary>
        bool ClearsRecycledBlocks { get; }
        /// <summary>Number of bytes allocated</summary>
        long BytesAllocated { get; }
        /// <summary>Number of bytes available for allocation</summary>
        long BytesAvailable { get; }

        /// <summary>
        /// Try to allocate block, if no blocks are available returns false and null.
        /// </summary>
        /// <param name="block"></param>
        /// <returns>true if <paramref name="block"/> was placed with block, false if there were not free space</returns>
        bool TryAllocate(out byte[] block);

        /// <summary>
        /// Allocate block. If no blocks are available, then waits until one is returned.
        /// </summary>
        /// <returns>block</returns>
        byte[] Allocate();

        /// <summary>
        /// Return <paramref name="block"/> back to the block pool.
        /// 
        /// If there is a thread waiting to get a block, then the thread is woken up and is distributed the block.
        /// </summary>
        /// <param name="block">block to return</param>
        void Return(byte[] block);

        /// <summary>
        /// Return <paramref name="blocks"/> back to the block pool.
        /// 
        /// If there is a thread waiting to get a block, then the thread is woken up and provided one.
        /// </summary>
        /// <param name="blocks">block to return</param>
        void Return(IEnumerable<byte[]> blocks);

        /// <summary>
        /// Disconnect <paramref name="block"/> from pool. <paramref name="block"/> is a block that has been allocated by the pool earlier on.
        /// 
        /// The caller gets the ownership of <paramref name="block"/>.
        /// </summary>
        /// <param name="block"></param>
        void Disconnect(byte[] block);

        /// <summary>
        /// Disconnect <paramref name="blocks"/> from the pool. <paramref name="blocks"/> have been allocated by the pool earlier on.
        /// 
        /// The caller gets the ownership of <paramref name="blocks"/>.
        /// </summary>
        /// <param name="blocks"></param>
        void Disconnect(IEnumerable<byte[]> blocks);
    }
}
