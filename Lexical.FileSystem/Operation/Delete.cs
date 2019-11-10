// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           17.10.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.IO;

namespace Lexical.FileSystem.Operation
{
    /// <summary>Delete file or directory</summary>
    public class Delete : OperationBase
    {
        /// <summary>Target filesystem</summary>
        protected IFileSystem fileSystem;
        /// <summary>Target path</summary>
        protected string path;
        /// <summary>Target filesystem</summary>
        public override IFileSystem FileSystem => fileSystem;
        /// <summary>Target path</summary>
        public override String Path => path;
        /// <summary>Delete tree recursively</summary>
        public bool Recurse { get; protected set; }
        /// <summary>Rollback operation.</summary>
        protected OperationBase rollback;
        /// <summary>Target filesystem option or token</summary>
        protected IOption Option;

        /// <summary>
        /// Create delete directory op.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="filesystem"></param>
        /// <param name="path"></param>
        /// <param name="recurse"></param>
        /// <param name="option">(optional) </param>
        /// <param name="policy">(optional) Responds to <see cref="OperationPolicy.DstThrow"/> and <see cref="OperationPolicy.DstSkip"/> policies.</param>
        /// <param name="rollback">(optional) Rollback operation</param>
        public Delete(IOperationSession session, IFileSystem filesystem, string path, bool recurse, IOption option = null, OperationPolicy policy = OperationPolicy.Unset, OperationBase rollback = null) : base(session, policy)
        {
            this.fileSystem = filesystem ?? throw new ArgumentNullException(nameof(filesystem));
            this.path = path ?? throw new ArgumentNullException(nameof(path));
            this.Recurse = recurse;
            this.rollback = rollback;
            this.CanRollback = rollback != null;
            this.Option = option;
        }

        /// <summary>Estimate viability of operation.</summary>
        /// <exception cref="FileNotFoundException">If <see cref="Path"/> is not found.</exception>
        protected override void InnerEstimate()
        {
            // Assert dest
            if (EffectivePolicy.HasFlag(OperationPolicy.DstThrow) && FileSystem.CanGetEntry())
            {
                try
                {
                    IEntry e = FileSystem.GetEntry(Path, this.Option.OptionIntersection(session.Option));
                    // Not found
                    if (e == null)
                    {
                        // Skip
                        if (EffectivePolicy.HasFlag(OperationPolicy.DstSkip)) { CanRollback = true; SetState(OperationState.Skipped); return; }
                        // Throw
                        if (EffectivePolicy.HasFlag(OperationPolicy.DstThrow)) throw new FileNotFoundException(Path);
                    }
                }
                catch (NotSupportedException) { }
            }
        }

        /// <summary>Run op</summary>
        /// <exception cref="FileNotFoundException">If <see cref="Path"/> is not found.</exception>
        protected override void InnerRun()
        {
            try
            {
                // Delete directory
                FileSystem.Delete(Path, Recurse, this.Option.OptionIntersection(session.Option));
            }
            catch (FileNotFoundException) when (!EffectivePolicy.HasFlag(OperationPolicy.DstThrow)) { }
            catch (DirectoryNotFoundException) when (!EffectivePolicy.HasFlag(OperationPolicy.DstThrow)) { }
        }

        /// <summary>Create rollback op</summary>
        /// <returns></returns>
        public override IOperation CreateRollback()
            => CurrentState == OperationState.Completed ? rollback : null;

        /// <summary>Print info</summary>
        public override string ToString() => $"Delete(Path={Path}, Recurse={Recurse}, State={CurrentState})";
    }

}
