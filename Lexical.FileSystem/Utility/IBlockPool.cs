// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           17.10.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
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
        /// <param name="data"></param>
        /// <returns>true if <paramref name="data"/> was placed with block, false if there were not free space</returns>
        bool TryAllocate(out byte[] data);

        /// <summary>
        /// Allocate block. If no blocks are available, then waits until one is returned.
        /// </summary>
        /// <returns>block</returns>
        byte[] Allocate();

        /// <summary>
        /// Return block to the block pool.
        /// 
        /// If there is a thread waiting to get a block, then the thread is woken up and distributed the block.
        /// </summary>
        /// <param name="data">block to return</param>
        void Return(byte[] data);
    }
}
