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
    /// <summary>Copy a file or directory tree</summary>
    public class CopyTree : Batch
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
        public CopyTree(IOperationSession session, IFileSystem srcFilesystem, string srcPath, IFileSystem dstFilesystem, string dstPath, IOption srcOption = null, IOption dstOption = null, OperationPolicy policy = OperationPolicy.Unset) : base(session, policy)
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
        /// <exception cref="FileSystemExceptionFileExists">If <see cref="Path"/> already exists.</exception>
        protected override void InnerEstimate()
        {
            PathConverter pathConverter = new PathConverter(SrcPath, Path);
            List<IEntry> queue = new List<IEntry>();

            // Src
            IEntry e = SrcFileSystem.GetEntry(SrcPath, srcOption.OptionIntersection(session.Option));
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
                        IEntry[] children = SrcFileSystem.Browse(entry.Path, srcOption.OptionIntersection(session.Option));
                        // Assert children don't refer to the parent of the parent
                        foreach (IEntry child in children) if (entry.Path.StartsWith(child.Path)) throw new IOException($"{child.Path} cannot be child of {entry.Path}");
                        // Visit child
                        for (int i = children.Length - 1; i >= 0; i--) queue.Add(children[i]);
                        // Convert path
                        string _dstPath;
                        if (!pathConverter.ParentToChild(entry.Path, out _dstPath)) throw new Exception("Failed to convert path");
                        // Add op
                        if (_dstPath != "") Ops.Add(new CreateDirectory(session, FileSystem, _dstPath, Option.OptionIntersection(session.Option), OpPolicy));
                    }

                    // Process file
                    else if (entry.IsFile())
                    {
                        // Convert path
                        string _dstPath;
                        if (!pathConverter.ParentToChild(entry.Path, out _dstPath)) throw new Exception("Failed to convert path");
                        // Add op
                        Ops.Add(new CopyFile(session, SrcFileSystem, entry.Path, FileSystem, _dstPath, srcOption.OptionIntersection(session.Option), Option.OptionIntersection(session.Option), OpPolicy));
                    }
                }
                catch (Exception error) when (SetError(error)) { }
            }

            base.InnerEstimate();
        }

        /// <summary>Print info</summary>
        public override string ToString() => $"CopyTree(Src={SrcPath}, Dst={Path})";
    }
}
