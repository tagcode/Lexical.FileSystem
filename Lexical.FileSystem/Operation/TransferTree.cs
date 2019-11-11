// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           17.10.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;

namespace Lexical.FileSystem.Operation
{
    /// <summary>
    /// Move/rename a file or directory tree by copying and deleting files.
    /// </summary>
    public class TransferTree : Batch
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

        /// <summary>Create move op.</summary>
        public TransferTree(OperationSession session, IFileSystem srcFilesystem, string srcPath, IFileSystem dstFilesystem, string dstPath, IOption srcOption = null, IOption dstOption = null, OperationPolicy policy = OperationPolicy.Unset) : base(session, policy)
        {
            this.srcFileSystem = srcFilesystem ?? throw new ArgumentNullException(nameof(srcFilesystem));
            this.dstFileSystem = dstFilesystem ?? throw new ArgumentNullException(nameof(dstFilesystem));
            this.srcPath = srcPath ?? throw new ArgumentNullException(nameof(srcPath));
            this.dstPath = dstPath ?? throw new ArgumentNullException(nameof(dstPath));
            this.srcOption = srcOption;
            this.Option = dstOption;
        }

        /// <summary>Scan tree, and add ops</summary>
        protected override void InnerEstimate()
        {
            PathConverter pathConverter = new PathConverter(SrcPath, Path);
            List<IEntry> queue = new List<IEntry>();

            // Src
            IEntry e = SrcFileSystem.GetEntry(SrcPath, srcOption);

            // Src not found
            if (e == null)
            {
                // Throw
                if (EffectivePolicy.HasFlag(OperationPolicy.SrcThrow)) throw new FileNotFoundException(SrcPath);
                // Skip
                if (EffectivePolicy.HasFlag(OperationPolicy.SrcSkip)) { SetState(OperationState.Skipped); return; }
                // Fail anyway
                throw new FileNotFoundException(SrcPath);
            }

            List<Delete> deleteDirs = new List<Delete>();
            queue.Add(e);
            while (queue.Count > 0)
            {
                try
                {
                    // Next entry
                    int lastIx = queue.Count - 1;
                    IEntry entry = queue[lastIx];
                    queue.RemoveAt(lastIx);

                    // Omit package mounts
                    if (session.Policy.HasFlag(OperationPolicy.OmitMountedPackages) && entry.IsPackageMount()) continue;

                    // Process directory
                    if (entry.IsDirectory())
                    {
                        // Browse children
                        IDirectoryContent content = SrcFileSystem.Browse(entry.Path, srcOption.OptionIntersection(session.Option));
                        // Assert children don't refer to the parent of the parent
                        foreach (IEntry child in content) if (entry.Path.StartsWith(child.Path)) throw new IOException($"{child.Path} cannot be child of {entry.Path}");
                        // Visit child
                        for (int i = content.Count - 1; i >= 0; i--) queue.Add(content[i]);
                        // Convert path
                        string _dstPath;
                        if (!pathConverter.ParentToChild(entry.Path, out _dstPath)) throw new Exception("Failed to convert path");
                        // Add op
                        if (_dstPath != "")
                        {
                            Ops.Add(new CreateDirectory(session, FileSystem, _dstPath, Option.OptionIntersection(session.Option), OpPolicy));
                            deleteDirs.Add(new Delete(session, SrcFileSystem, entry.Path, false, srcOption.OptionIntersection(session.Option), OpPolicy | OperationPolicy.EstimateOnRun,
                                rollback: new CreateDirectory(session, SrcFileSystem, entry.Path, srcOption.OptionIntersection(session.Option), OpPolicy)));
                        }
                    }

                    // Process file
                    else if (entry.IsFile())
                    {
                        // Convert path
                        string _dstPath;
                        if (!pathConverter.ParentToChild(entry.Path, out _dstPath)) throw new Exception("Failed to convert path");
                        // Add op
                        Ops.Add(new CopyFile(session, SrcFileSystem, entry.Path, FileSystem, _dstPath, srcOption.OptionIntersection(session.Option), Option.OptionIntersection(session.Option), OpPolicy));
                        Ops.Add(new Delete(session, SrcFileSystem, entry.Path, false, srcOption.OptionIntersection(session.Option), OpPolicy,
                            rollback: new CopyFile(session, FileSystem, _dstPath, SrcFileSystem, entry.Path, Option.OptionIntersection(session.Option), srcOption.OptionIntersection(session.Option), OpPolicy)));
                    }

                }
                catch (Exception error) when (SetError(error)) { }
            }

            // Add delete directories
            for (int i = deleteDirs.Count - 1; i >= 0; i--)
                Ops.Add(deleteDirs[i]);

            base.InnerEstimate();
        }

        /// <summary>Print info</summary>
        public override string ToString() => $"TransferTree(Src={SrcPath}, Dst={Path})";
    }
}
