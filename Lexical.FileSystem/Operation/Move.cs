// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           17.10.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.IO;

namespace Lexical.FileSystem.Operation
{
    /// <summary>Move/rename file or directory</summary>
    public class Move : OperationBase
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

        /// <summary>Set to true if <see cref="IOperation.Run"/> moved src.</summary>
        bool moved;
        /// <summary>Set to true if <see cref="IOperation.Run"/> deleted previous dst.</summary>
        bool deletedPrev;

        /// <summary>Create move op.</summary>
        public Move(IOperationSession session, IFileSystem srcFilesystem, string srcPath, IFileSystem dstFilesystem, string dstPath, IOption srcOption = null, IOption dstOption = null, OperationPolicy policy = OperationPolicy.Unset) : base(session, policy)
        {
            this.srcFileSystem = srcFilesystem ?? throw new ArgumentNullException(nameof(srcFilesystem));
            this.dstFileSystem = dstFilesystem ?? throw new ArgumentNullException(nameof(dstFilesystem));
            this.srcPath = srcPath ?? throw new ArgumentNullException(nameof(srcPath));
            this.dstPath = dstPath ?? throw new ArgumentNullException(nameof(dstPath));
            if (srcFileSystem != dstFileSystem) throw new ArgumentException($"Move implementation requires that {nameof(srcFilesystem)} and {nameof(dstFilesystem)} are same. Use MoveTree instead.");
            this.srcOption = srcOption;
            this.Option = dstOption;
        }

        /// <summary>Estimate viability of operation.</summary>
        /// <exception cref="FileNotFoundException">If <see cref="SrcPath"/> is not found.</exception>
        /// <exception cref="FileSystemExceptionEntryExists">If entry at <see cref="Path"/> already exists.</exception>
        protected override void InnerEstimate()
        {
            // Cannot move
            if (!SrcFileSystem.CanMove()) throw new NotSupportedException("Move");

            // Test that source file exists
            if (SrcFileSystem.CanGetEntry())
            {
                try
                {
                    IEntry e = SrcFileSystem.GetEntry(SrcPath, this.srcOption.OptionIntersection(session.Option));
                    // Src not found
                    if (e == null)
                    {
                        // Throw
                        if (EffectivePolicy.HasFlag(OperationPolicy.SrcThrow)) throw new FileNotFoundException(SrcPath);
                        // Skip
                        if (EffectivePolicy.HasFlag(OperationPolicy.SrcSkip)) { SetState(OperationState.Skipped); return; }
                    }
                }
                catch (NotSupportedException)
                {
                    // GetEntry is not supported
                }
            }

            // Test that dest file doesn't exist
            if (dstFileSystem.CanGetEntry())
            {
                try
                {
                    IEntry dstEntry = dstFileSystem.GetEntry(Path, this.Option.OptionIntersection(session.Option));
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
            // Test dst
            try
            {
                IEntry dstEntry = dstFileSystem.GetEntry(dstPath, this.Option.OptionIntersection(session.Option));

                // Dst exists
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
                    if (EffectivePolicy.HasFlag(OperationPolicy.DstSkip)) { CanRollback = true; moved = false; deletedPrev = false; return; }

                    // Delete prev
                    if (EffectivePolicy.HasFlag(OperationPolicy.DstOverwrite)) { FileSystem.Delete(Path, recurse: false, this.Option.OptionIntersection(session.Option)); deletedPrev = true; CanRollback = false; }
                }
                else
                {
                    CanRollback = true;
                }

            }
            catch (NotSupportedException)
            {
                // Dst cannot get entry
            }

            // Move
            SrcFileSystem.Move(SrcPath, dstPath, srcOption);
            moved = true;
        }

        /// <summary>Create rollback</summary>
        /// <returns>rollback or null</returns>
        public override IOperation CreateRollback()
        {
            if (moved && !deletedPrev) return new Move(session, dstFileSystem, dstPath, SrcFileSystem, SrcPath, Option, srcOption, OpPolicy);
            return null;
        }

        /// <summary>Print info</summary>
        public override string ToString() => $"Move(Src={srcPath}, Dst={dstPath}, State={CurrentState})";
    }

}
