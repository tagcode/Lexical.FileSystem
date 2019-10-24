// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           17.10.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Threading;

namespace Lexical.FileSystem.Utility
{
    /// <summary>
    /// Block pool allocates and recycles memory blocks that are used for buffers.
    /// </summary>
    public class BlockPool : IBlockPool
    {
        /// <summary>Block size, typically 4096</summary>
        public readonly int BlockSize;
        /// <summary>Maximum number of blocks to dispence</summary>
        public readonly long MaxBlockCount;
        /// <summary>Maximum number of blocks to keep in recycle queue</summary>
        public readonly int MaxRecycleQueue;
        /// <summary>Clear recycled blocks with 0 values</summary>
        public readonly bool ClearsRecycledBlocks;
        /// <summary>Number of blocks dispenced currently (not correlated with recycled blocks)</summary>
        protected long blocksAllocated;
        /// <summary>lock</summary>
        protected object m_lock = new object();
        /// <summary>lock</summary>
        protected List<byte[]> recycledBlocks;
        /// <summary>Number of bytes allocated</summary>
        public long BytesAllocated => blocksAllocated * BlockSize;
        /// <summary>Number of bytes available for allocation</summary>
        public long BytesAvailable => (MaxBlockCount - blocksAllocated) * BlockSize;

        int IBlockPool.BlockSize => BlockSize;
        long IBlockPool.MaxBlockCount => MaxBlockCount;
        int IBlockPool.MaxRecycleQueue => MaxRecycleQueue;
        bool IBlockPool.ClearsRecycledBlocks => ClearsRecycledBlocks;

        /// <summary>Create block pool.</summary>
        /// <param name="blockSize">block size</param>
        /// <param name="maxBlockCount">maximum number of blocks to disposense concurrently</param>
        /// <param name="maxRecycleQueue">maximum number of blocks to recycle, 0 to not recycle blocks ever</param>
        /// <param name="clearRecycledBlocks">if true, clears recycled blocks with 0 values</param>
        public BlockPool(int blockSize = 4096, long maxBlockCount = 65536, int maxRecycleQueue = 64, bool clearRecycledBlocks = false)
        {
            if (blockSize < 16) throw new ArgumentOutOfRangeException(nameof(blockSize));
            if (maxBlockCount < 1) throw new ArgumentOutOfRangeException(nameof(maxBlockCount));
            if (maxRecycleQueue < 0) throw new ArgumentOutOfRangeException(nameof(maxRecycleQueue));
            BlockSize = blockSize;
            MaxBlockCount = maxBlockCount;
            MaxRecycleQueue = (int)Math.Min(maxBlockCount, maxRecycleQueue);
            recycledBlocks = MaxRecycleQueue == 0 ? null : new List<byte[]>(Math.Min(MaxRecycleQueue, 64));
            ClearsRecycledBlocks = clearRecycledBlocks;
        }

        /// <summary>
        /// Try to allocate block, if no blocks are available returns false and null.
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public bool TryAllocate(out byte[] block)
        {
            // Lock
            Monitor.Enter(m_lock);
            try
            {
                // No block available
                if (blocksAllocated >= MaxBlockCount) { block = null; return false; }
                // Take from pool
                if (MaxRecycleQueue > 0 && recycledBlocks.Count > 0)
                {
                    Interlocked.Increment(ref blocksAllocated);
                    int blockIx = recycledBlocks.Count - 1;
                    block = recycledBlocks[blockIx];
                    recycledBlocks.RemoveAt(blockIx);
                    if (ClearsRecycledBlocks) Array.Clear(block, 0, BlockSize);
                    return true;
                }
                else
                // Allocate new
                {
                    Interlocked.Increment(ref blocksAllocated);
                    block = new byte[BlockSize];
                    return true;
                }
            }
            finally
            {
                Monitor.Exit(m_lock);
            }
        }

        /// <summary>
        /// Allocate block. If no blocks are available, then waits until one is returned.
        /// </summary>
        /// <returns></returns>
        public byte[] Allocate()
        {
            // Lock
            Monitor.Enter(m_lock);
            try
            {
                // Wait until blocks are available
                while (blocksAllocated >= MaxBlockCount) Monitor.Wait(m_lock);
                // Take from pool
                if (MaxRecycleQueue > 0 && recycledBlocks.Count > 0)
                {
                    Interlocked.Increment(ref blocksAllocated);
                    int blockIx = recycledBlocks.Count - 1;
                    byte[] data = recycledBlocks[blockIx];
                    recycledBlocks.RemoveAt(blockIx);
                    if (ClearsRecycledBlocks) Array.Clear(data, 0, BlockSize);
                    return data;
                }
                else
                // Allocate new
                {
                    Interlocked.Increment(ref blocksAllocated);
                    byte[] data = new byte[BlockSize];
                    return data;
                }
            }
            finally
            {
                Monitor.Exit(m_lock);
            }
        }

        /// <summary>
        /// Return block to the block pool.
        /// 
        /// If there is a thread waiting to get a block, then the thread is woken up and distributed the block.
        /// </summary>
        /// <param name="block">block to return</param>
        public void Return(byte[] block)
        {
            // Assert argument
            if (block == null) throw new ArgumentNullException(nameof(block));
            //
            if (block.Length != BlockSize) throw new ArgumentException("Wrong blocksize");

            // Lock
            Monitor.Enter(m_lock);
            try
            {
                // Mark one block released
                Interlocked.Decrement(ref blocksAllocated);
                // Recycle block
                if (MaxRecycleQueue > 0 && recycledBlocks.Count < MaxRecycleQueue) recycledBlocks.Add(block);
                // Wakeup one thread
                Monitor.Pulse(m_lock);
            }
            finally
            {
                // Unlock
                Monitor.Exit(m_lock);
            }
        }

        /// <summary>
        /// Return <paramref name="blocks"/> to the block pool.
        /// 
        /// If there is a thread waiting to get a block, then the thread is woken up and distributed one.
        /// </summary>
        /// <param name="blocks">block to return</param>
        public void Return(IEnumerable<byte[]> blocks)
        {
            // Assert argument
            if (blocks == null) throw new ArgumentNullException(nameof(blocks));

            // Lock
            Monitor.Enter(m_lock);
            try
            {
                foreach (byte[] block in blocks)
                {
                    // Assert
                    if (block == null) continue;
                    // Assert
                    if (block.Length != BlockSize) throw new ArgumentException("Wrong blocksize");
                    // Mark one block released
                    Interlocked.Decrement(ref blocksAllocated);
                    // Recycle block
                    if (MaxRecycleQueue > 0 && recycledBlocks.Count < MaxRecycleQueue) recycledBlocks.Add(block);
                    // Wakeup one thread
                    Monitor.Pulse(m_lock);
                }
            }
            finally
            {
                // Unlock
                Monitor.Exit(m_lock);
            }
        }

        /// <summary>
        /// Disconnect <paramref name="block"/> from pool. <paramref name="block"/> is a block that has been allocated by the pool earlier on.
        /// 
        /// The caller gets the ownership of <paramref name="block"/>.
        /// </summary>
        /// <param name="block"></param>
        public void Disconnect(byte[] block)
        {
            // Assert argument
            if (block == null) throw new ArgumentNullException(nameof(block));
            // Assert
            if (block.Length != BlockSize) throw new ArgumentException("Wrong blocksize");

            // Lock
            Monitor.Enter(m_lock);
            try
            {
                // Mark one block released
                Interlocked.Decrement(ref blocksAllocated);
                // Wakeup one thread
                Monitor.Pulse(m_lock);
            }
            finally
            {
                // Unlock
                Monitor.Exit(m_lock);
            }

        }

        /// <summary>
        /// Disconnect <paramref name="blocks"/> from the pool. <paramref name="blocks"/> have been allocated by the pool earlier on.
        /// 
        /// The caller gets the ownership of <paramref name="blocks"/>.
        /// </summary>
        /// <param name="blocks"></param>
        public void Disconnect(IEnumerable<byte[]> blocks)
        {
            // Assert arguments
            if (blocks == null) throw new ArgumentNullException(nameof(blocks));

            // Number of released blocks
            int count = 0;

            // Each block
            foreach (byte[] block in blocks)
            {
                // Assert
                if (block == null) continue;
                // Assert
                if (block.Length != BlockSize) throw new ArgumentException("Wrong blocksize");
                // Number of threads to wakeup
                count++;
            }

            // Wake up threads
            if (count > 0)
            {
                // Lock
                Monitor.Enter(m_lock);
                try
                {
                    // Wakeup one thread
                    for (int i = 0; i < count; i++)
                    {
                        // Mark one block released
                        Interlocked.Decrement(ref blocksAllocated);
                        Monitor.Pulse(m_lock);
                    }
                }
                finally
                {
                    // Unlock
                    Monitor.Exit(m_lock);
                }
            }
        }

        /// <summary>Print Info</summary>
        public override string ToString() => $"{GetType().Name}(BlockSize={BlockSize}, MaxBlockCount={MaxBlockCount}, MaxRecycleQueue={MaxRecycleQueue}, ClearsRecycledBlocks={ClearsRecycledBlocks}, BlocksAllocated={blocksAllocated}, BytesAllocated={BytesAllocated}, BytesAvailable={BytesAvailable})";
    }

    /// <summary>
    /// Pseudo Block pool that always allocates new block. Doesn't recycle blocks. Doesn't keep track of the number of returned blocks.
    /// </summary>
    public class BlockPoolPseudo : IBlockPool
    {
        static IBlockPool instance = new BlockPoolPseudo();
        /// <summary>Singleton instance</summary>
        public static IBlockPool Instance => instance;
        /// <summary></summary>
        public int BlockSize { get; protected set; }
        /// <summary></summary>
        public long MaxBlockCount => long.MaxValue;
        /// <summary></summary>
        public int MaxRecycleQueue => 0;
        /// <summary></summary>
        public bool ClearsRecycledBlocks => true;
        /// <summary></summary>
        public long BytesAllocated => 0L;
        /// <summary></summary>
        public long BytesAvailable => long.MaxValue;

        /// <summary>
        /// Create pseudo block pool.
        /// </summary>
        /// <param name="blockSize"></param>
        public BlockPoolPseudo(int blockSize = 1024)
        {
            this.BlockSize = blockSize;
        }

        /// <summary>Allocate block</summary>
        public byte[] Allocate() => new byte[BlockSize];

        /// <summary>Return block</summary>
        public void Return(byte[] data) { }

        /// <summary>Return blocks</summary>
        public void Return(IEnumerable<byte[]> blocks) { }

        /// <summary>Allocate block</summary>
        public bool TryAllocate(out byte[] data)
        {
            data = new byte[BlockSize];
            return true;
        }

        /// <summary>Disconnect block</summary>
        public void Disconnect(byte[] block) { }

        /// <summary>Disconnect blocks</summary>
        public void Disconnect(IEnumerable<byte[]> blocks) { }

        /// <summary>Print Info</summary>
        public override string ToString() => $"{GetType().Name}(BlockSize={BlockSize})";
    }


}
