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
    /// <summary>Delete a file or directory tree</summary>
    public class DeleteTree : Batch
    {
        /// <summary>Target filesystem</summary>
        protected IFileSystem fileSystem;
        /// <summary>Target path</summary>
        protected string path;
        /// <summary>Target filesystem</summary>
        public override IFileSystem FileSystem => fileSystem;
        /// <summary>Target path</summary>
        public override String Path => path;

        /// <summary>Src filesystem option or token</summary>
        protected IOption srcOption;
        /// <summary>Target filesystem option or token</summary>
        protected IOption Option;

        /// <summary>Create move op.</summary>
        public DeleteTree(IOperationSession session, IFileSystem filesystem, string path, IOption srcOption = null, IOption dstOption = null, OperationPolicy policy = OperationPolicy.Unset) : base(session, policy)
        {
            this.fileSystem = filesystem ?? throw new ArgumentNullException(nameof(filesystem));
            this.path = path ?? throw new ArgumentNullException(nameof(path));
            this.srcOption = srcOption;
            this.Option = dstOption;
        }

        /// <summary>Estimate viability of operation.</summary>
        /// <exception cref="FileNotFoundException">If <see cref="Path"/> is not found.</exception>
        /// <exception cref="FileSystemExceptionFileExists">If <see cref="Path"/> already exists.</exception>
        protected override void InnerEstimate()
        {
            List<Delete> dirDeletes = new List<Delete>();
            try
            {
                List<IEntry> queue = new List<IEntry>();
                IEntry e = FileSystem.GetEntry(Path, Option.OptionIntersection(session.Option));
                if (e == null) throw new FileNotFoundException(Path);
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
                            IEntry[] children = FileSystem.Browse(entry.Path, Option.OptionIntersection(session.Option));
                            // Assert children don't refer to the parent of the parent
                            foreach (IEntry child in children) if (entry.Path.StartsWith(child.Path)) throw new IOException($"{child.Path} cannot be child of {entry.Path}");
                            // Visit children
                            for (int i = children.Length - 1; i >= 0; i--) queue.Add(children[i]);
                            // Add op
                            dirDeletes.Add(new Delete(session, FileSystem, entry.Path, false));
                        }

                        // Process file
                        else if (entry.IsFile())
                        {
                            // Add op
                            Ops.Add(new Delete(session, FileSystem, entry.Path, false, Option.OptionIntersection(session.Option), OpPolicy));
                        }
                    }
                    catch (Exception error) when (SetError(error)) { }
                }
            }
            finally
            {
                // Add directory deletes
                for (int i = dirDeletes.Count - 1; i >= 0; i--)
                    Ops.Add(dirDeletes[i]);
            }

            // Estimate added ops
            base.InnerEstimate();
        }

        /// <summary>Print info</summary>
        public override string ToString() => $"DeleteTree({Path})";
    }
}
