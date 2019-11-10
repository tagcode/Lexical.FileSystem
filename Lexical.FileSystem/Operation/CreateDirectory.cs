// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           17.10.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Lexical.FileSystem.Operation
{
    /// <summary>Create directory</summary>
    public class CreateDirectory : OperationBase
    {
        /// <summary>Target filesystem</summary>
        protected IFileSystem fileSystem;
        /// <summary>Target path</summary>
        protected string path;
        /// <summary>Target filesystem</summary>
        public override IFileSystem FileSystem => fileSystem;
        /// <summary>Target path</summary>
        public override String Path => path;
        /// <summary>Directories created in Run().</summary>
        protected List<string> DirectoriesCreated = new List<string>();
        /// <summary>Target filesystem option or token</summary>
        protected IOption Option;

        /// <summary>
        /// Create create directory op.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="filesystem"></param>
        /// <param name="path"></param>
        /// <param name="option"></param>
        /// <param name="policy">(optional) Responds to <see cref="OperationPolicy.DstThrow"/>, <see cref="OperationPolicy.DstSkip"/> and <see cref="OperationPolicy.DstOverwrite"/> policies</param>
        public CreateDirectory(IOperationSession session, IFileSystem filesystem, string path, IOption option = null, OperationPolicy policy = OperationPolicy.Unset) : base(session, policy)
        {
            this.fileSystem = filesystem ?? throw new ArgumentNullException(nameof(filesystem));
            this.path = path ?? throw new ArgumentNullException(nameof(path));
            // Can rollback if can delete
            this.CanRollback = filesystem.CanDelete();
            this.Option = option;
        }

        /// <summary>Estimate viability of operation.</summary>
        /// <exception cref="FileNotFoundException">If <see cref="Path"/> is not found.</exception>
        protected override void InnerEstimate()
        {
            // Cannot create directory
            if (!FileSystem.CanCreateDirectory()) throw new NotSupportedException("CreateDirectory");
            // Test that directory already exists
            if (FileSystem.CanGetEntry())
            {
                try
                {
                    IEntry e = FileSystem.GetEntry(Path, this.Option.OptionIntersection(session.Option));
                    // Directory already exists
                    if (e != null)
                    {
                        // Overwrite
                        if (EffectivePolicy.HasFlag(OperationPolicy.DstThrow))
                        {
                            // Nothing will be done
                            CanRollback = true;
                            if (e.IsDirectory()) throw new FileSystemExceptionDirectoryExists(FileSystem, Path);
                            else if (e.IsFile()) throw new FileSystemExceptionFileExists(FileSystem, Path);
                            else throw new FileSystemExceptionEntryExists(FileSystem, Path);
                        }
                        // Skip
                        if (EffectivePolicy.HasFlag(OperationPolicy.DstSkip)) { CanRollback = true; SetState(OperationState.Skipped); return; }
                        // Delete prev
                        if (EffectivePolicy.HasFlag(OperationPolicy.DstOverwrite))
                        {
                            // Is going to be deleted
                            if (e.IsFile()) CanRollback = false;
                            // Is going to be skipped
                            else if (e.IsDirectory()) CanRollback = true;
                        }
                    }
                    else
                    {
                        // Directory not found, can rollback
                        CanRollback = true;
                    }
                }
                catch (NotSupportedException) { }
            }
        }

        /// <summary>Create direcotry</summary>
        /// <exception cref="FileNotFoundException">If <see cref="Path"/> is not found.</exception>
        /// <exception cref="FileSystemExceptionEntryExists">If file or directory already existed at <see cref="Path"/> and <see cref="OperationPolicy.DstThrow"/> is true.</exception>
        protected override void InnerRun()
        {
            // Cannot get entry
            if (!FileSystem.CanGetEntry()) { CreateBlind(); return; }

            try
            {

                // Test that directory already exists
                if (FileSystem.CanGetEntry())
                {
                    try
                    {
                        IEntry e = FileSystem.GetEntry(Path, this.Option.OptionIntersection(session.Option));
                        // Directory already exists
                        if (e != null)
                        {
                            // Throw
                            if (EffectivePolicy.HasFlag(OperationPolicy.DstThrow))
                            {
                                // Nothing is done
                                CanRollback = true;
                                if (e.IsDirectory()) throw new FileSystemExceptionDirectoryExists(FileSystem, Path);
                                else if (e.IsFile()) throw new FileSystemExceptionFileExists(FileSystem, Path);
                                else throw new FileSystemExceptionEntryExists(FileSystem, Path);
                            }
                            // Skip
                            if (EffectivePolicy.HasFlag(OperationPolicy.DstSkip)) { CanRollback = true; SetState(OperationState.Skipped); return; }
                            // Delete prev
                            if (EffectivePolicy.HasFlag(OperationPolicy.DstOverwrite))
                            {
                                // Delete File
                                if (e.IsFile()) { CanRollback = false; FileSystem.Delete(Path, recurse: false, this.Option.OptionIntersection(session.Option)); }
                                // Skip
                                else if (e.IsDirectory()) { CanRollback = true; SetState(OperationState.Skipped); return; }
                            }
                        }
                    }
                    catch (NotSupportedException) { }
                }

                // Enumerate paths
                PathEnumerator etor = new PathEnumerator(Path, true);
                while (etor.MoveNext())
                {
                    string path = Path.Substring(0, etor.Current.Length + etor.Current.Start);
                    IEntry e = FileSystem.GetEntry(path, this.Option.OptionIntersection(session.Option));

                    // Entry exists
                    if (e != null) continue;

                    FileSystem.CreateDirectory(path, this.Option.OptionIntersection(session.Option));
                    DirectoriesCreated.Add(path);
                }
            }
            catch (NotSupportedException)
            {
                CreateBlind();
            }
        }

        /// <summary>Create directory blind</summary>
        void CreateBlind()
        {
            try
            {
                // Create directory
                FileSystem.CreateDirectory(Path, this.Option.OptionIntersection(session.Option));
                //
                DirectoriesCreated.Add(Path);
            }
            catch (FileSystemExceptionFileExists) when (!EffectivePolicy.HasFlag(OperationPolicy.DstThrow)) { }
            catch (FileSystemExceptionDirectoryExists) when (!EffectivePolicy.HasFlag(OperationPolicy.DstThrow)) { }
        }

        /// <summary>Create rollback</summary>
        /// <returns>op or null</returns>
        public override IOperation CreateRollback()
        {
            // Nothing to do
            if (DirectoriesCreated.Count == 0) return null;
            // Delete the one directory we created
            if (DirectoriesCreated.Count == 1) return new Delete(session, FileSystem, DirectoriesCreated[0], false, Option, OpPolicy);
            // Delete the directories we created, in reverse order
            return new Batch(session, OpPolicy, DirectoriesCreated.Select(d => new Delete(session, FileSystem, d, false, Option, OpPolicy)).Reverse());
        }

        /// <summary>Print info</summary>
        public override string ToString() => $"CreateDirectory(Path={Path}, CurrentState={CurrentState})";
    }
}
