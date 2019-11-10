// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           17.10.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace Lexical.FileSystem.Operation
{
    /// <summary>Copy file</summary>
    public class CopyFile : OperationBase
    {
        /// <summary>Source filesystem</summary>
        protected IFileSystem srcFileSystem;
        /// <summary>Target filesystem</summary>
        protected IFileSystem dstFileSystem;
        /// <summary>Source path</summary>
        protected string srcPath;
        /// <summary>Target path</summary>
        protected string dstPath;

        /// <summary>Target filesystem</summary>
        public override IFileSystem FileSystem => dstFileSystem;
        /// <summary>Target path</summary>
        public override String Path => dstPath;
        /// <summary>Source filesystem</summary>
        public override IFileSystem SrcFileSystem => srcFileSystem;
        /// <summary>Source path</summary>
        public override string SrcPath => srcPath;

        /// <summary>Src filesystem option or token</summary>
        protected IOption srcOption;
        /// <summary>Target filesystem option or token</summary>
        protected IOption Option;

        /// <summary>Was file overwritten</summary>
        public bool Overwritten { get; protected set; }

        /// <summary>Set to true if <see cref="IOperation.Run"/> created file.</summary>
        protected bool createdFile;
        /// <summary>Set to true if <see cref="IOperation.Run"/> copied.</summary>
        protected bool copied;
        /// <summary>Set to true if <see cref="IOperation.Run"/> previous entry existed.</summary>
        protected bool prevExisted;

        /// <summary>Create copy file op.</summary>
        public CopyFile(IOperationSession session, IFileSystem srcFilesystem, string srcPath, IFileSystem dstFilesystem, string dstPath, IOption srcOption = null, IOption dstOption = null, OperationPolicy policy = OperationPolicy.Unset) : base(session, policy)
        {
            this.srcFileSystem = srcFilesystem ?? throw new ArgumentNullException(nameof(srcFilesystem));
            this.dstFileSystem = dstFilesystem ?? throw new ArgumentNullException(nameof(dstFilesystem));
            this.srcPath = srcPath ?? throw new ArgumentNullException(nameof(srcPath));
            this.dstPath = dstPath ?? throw new ArgumentNullException(nameof(dstPath));
            this.srcOption = srcOption;
            this.Option = dstOption;
        }

        /// <summary>Estimate viability of operation.</summary>
        /// <exception cref="FileNotFoundException">If <see cref="SrcPath"/> is not found.</exception>
        /// <exception cref="FileSystemExceptionEntryExists">If <see cref="Path"/> already exists.</exception>
        protected override void InnerEstimate()
        {
            // Cannot create
            if (!FileSystem.CanOpen() || !FileSystem.CanCreateFile()) throw new NotSupportedException("Copy");

            // Test that source file exists
            if (SrcFileSystem.CanGetEntry())
            {
                try
                {
                    IEntry e = SrcFileSystem.GetEntry(SrcPath, srcOption.OptionIntersection(session.Option));
                    // Src not found
                    if (e == null)
                    {
                        // Throw
                        if (EffectivePolicy.HasFlag(OperationPolicy.SrcThrow)) throw new FileNotFoundException(SrcPath);
                        // Skip
                        if (EffectivePolicy.HasFlag(OperationPolicy.SrcSkip)) { SetState(OperationState.Skipped); return; }
                    }
                    // Set length
                    if (e.Length() > 0L) this.TotalLength = e.Length();
                }
                catch (NotSupportedException)
                {
                    // GetEntry is not supported
                }
            }

            // Test that dest file doesn't exist
            if (FileSystem.CanGetEntry())
            {
                try
                {
                    IEntry dstEntry = dstFileSystem.GetEntry(Path, Option.OptionIntersection(session.Option));
                    prevExisted = dstEntry != null;
                    if (dstEntry != null)
                    {
                        // Dst exists
                        if (EffectivePolicy.HasFlag(OperationPolicy.DstThrow))
                        {
                            if (dstEntry.IsFile()) throw new FileSystemExceptionFileExists(FileSystem, Path);
                            else if (dstEntry.IsDirectory()) throw new FileSystemExceptionDirectoryExists(FileSystem, Path);
                            else throw new FileSystemExceptionEntryExists(FileSystem, Path);
                        }

                        // Skip op
                        else if (EffectivePolicy.HasFlag(OperationPolicy.DstSkip))
                        {
                            SetState(OperationState.Skipped);
                            // Nothing to rollback, essentially ok
                            CanRollback = true;
                        }

                        // Overwrite
                        else if (EffectivePolicy.HasFlag(OperationPolicy.DstOverwrite))
                        {
                            // Cannot rollback
                            CanRollback = false;
                        }
                    }
                    else
                    {
                        // Prev file didn't exist, can be rollbacked
                        CanRollback = true;
                    }
                }
                catch (NotSupportedException)
                {
                    // GetEntry is not supported
                }
            }
        }

        /// <summary>Run operation</summary>
        /// <exception cref="IOException"></exception>
        /// <exception cref="Exception">Unexpected error</exception>
        protected override void InnerRun()
        {
            InnerEstimate();

            // Queue of blocks
            BlockingCollection<Block> queue = new BlockingCollection<Block>();
            try
            {
                // CancelToken to monitor
                CancellationToken token = session.CancelSrc.Token;

                // Create mode
                FileMode createMode = session.Policy.HasFlag(OperationPolicy.DstOverwrite) ? FileMode.Create : FileMode.CreateNew;

                // Write stream
                using (Stream s = dstFileSystem.Open(dstPath, createMode, FileAccess.Write, FileShare.ReadWrite, Option.OptionIntersection(session.Option)))
                {
                    // Created file
                    this.createdFile = !prevExisted;
                    // We have overwritten the prev file
                    this.Overwritten = prevExisted && createMode == FileMode.Create;

                    // Start read thread
                    new Thread(() => ReadFile(queue)).Start();

                    long progressReminder = 0L;

                    while (true)
                    {
                        // Read block
                        Block block = queue.Take(token);
                        try
                        {
                            // Assert session is not cancelled
                            if (session.CancelSrc.IsCancellationRequested && CurrentState != OperationState.Error) { SetState(OperationState.Cancelled); return; }
                            // Read error, abort
                            if (CurrentState != OperationState.Running) break;
                            // We got some data
                            if (block.count > 0)
                            {
                                // Write data
                                s.Write(block.data, 0, block.count);
                                // Update progress position
                                Progress += block.count;
                                progressReminder += block.count;
                                // Time to send progress event
                                if (session.ProgressInterval > 0L && progressReminder > session.ProgressInterval && session.HasObservers)
                                {
                                    progressReminder %= session.ProgressInterval;
                                    session.DispatchEvent(new OperationEvent.Progress(this, Progress, TotalLength));
                                }
                            }
                            // EOF or error
                            else if (block.count <= 0) break;
                        }
                        finally
                        {
                            // Return block to pool
                            if (block.data != null) session.BlockPool.Return(block.data);
                        }
                    }

                    copied = true;
                }
            }
            // Capture error and set state
            catch (Exception e) when (SetError(e)) { }
            finally
            {
                // Return blocks to pool
                Block b;
                while (queue.TryTake(out b)) if (b.data != null) session.BlockPool.Return(b.data);
            }
        }

        /// <summary>
        /// Method that reads source file and places blocks into <paramref name="queue"/>. The receiver must return blocks to blockpool.
        /// 
        /// This method is to be ran in a concurrent thread.
        /// 
        /// If cancel is requested, aborts.
        /// 
        /// If ioexception occurs, then sets op state to error.
        /// </summary>
        /// <param name="queue"></param>
        void ReadFile(BlockingCollection<Block> queue)
        {
            try
            {
                // CancelToken to monitor
                CancellationToken token = session.CancelSrc.Token;

                if (token.IsCancellationRequested) return;

                // Open file
                Stream s = SrcFileSystem.Open(SrcPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, srcOption.OptionIntersection(session.Option));
                try
                {
                    int len = 0;
                    do
                    {
                        // Cancelled, abort
                        if (token.IsCancellationRequested || CurrentState != OperationState.Running) { queue.Add(new Block { data = null, count = -1 /* cancellation */ }); return; }
                        // Allocate block
                        byte[] data = session.BlockPool.Allocate();
                        // Read data to block
                        len = s.Read(data, 0, data.Length);
                        // Pass block
                        if (len > 0) queue.Add(new Block { data = data, count = len });
                    } while (len > 0);
                    // Notify of eos with 0 length
                    queue.Add(new Block { data = null, count = 0 });
                }
                finally
                {
                    s.Dispose();
                }
            }
            // Capture error and set state
            catch (Exception e)
            {
                // Set state of op to error
                SetError(e);
                // Notify of error with -2 length
                queue.Add(new Block { data = null, count = -2 });
            }
        }

        /// <summary>Create rollback operation.</summary>
        /// <returns>null or rollback operation</returns>
        public override IOperation CreateRollback()
            => createdFile && !Overwritten ? new Delete(session, FileSystem, Path, false, Option, OpPolicy) : null;

        struct Block
        {
            public byte[] data;
            public int count;
        }

        /// <summary>Print info</summary>
        public override string ToString() => $"CopyFile(Src={srcPath}, Dst={dstPath}, State={CurrentState})";
    }
}
