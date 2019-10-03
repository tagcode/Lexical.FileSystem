// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;

namespace Lexical.FileSystem.Decoration
{
    /// <summary>
    /// Abstract base class for decorated entry.
    /// </summary>
    public abstract class FileSystemEntryDecoration : IFileSystemEntryDecoration, IFileSystemEntryFile, IFileSystemEntryDirectory, IFileSystemEntryDrive, IFileSystemEntryMount
    {
        /// <summary>
        /// Decorate filesystem.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="newFileSystem"></param>
        /// <returns>decorated entry</returns>
        public static IFileSystemEntry DecorateFileSystem(IFileSystemEntry entry, IFileSystem newFileSystem)
            => new NewFileSystem(entry, newFileSystem);

        /// <summary>
        /// Decorate filesystem and path.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="newFileSystem"></param>
        /// <param name="newPath"></param>
        /// <returns>decorated entry</returns>
        public static IFileSystemEntry DecorateFileSystemAndPath(IFileSystemEntry entry, IFileSystem newFileSystem, string newPath)
            => new NewFileSystemAndPath(entry, newFileSystem, newPath);

        /// <summary>
        /// Decorate filesystem and path, and add option modifier.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="newFileSystem"></param>
        /// <param name="newPath"></param>
        /// <param name="optionModifier">(optional) option that will be applied to original option with intersection</param>
        /// <returns>decorated entry</returns>
        public static IFileSystemEntry DecorateFileSystemPathAndOptionModifier(IFileSystemEntry entry, IFileSystem newFileSystem, string newPath, IFileSystemOption optionModifier)
            => new NewFileSystemPathAndOptionModifier(entry, newFileSystem, newPath, optionModifier);        

        /// <summary>
        /// Original entry that is being decorated.
        /// </summary>
        public virtual IFileSystemEntry Original { get; protected set; }
        /// <inheritdoc/>
        public virtual IFileSystem FileSystem => Original.FileSystem;
        /// <inheritdoc/>
        public virtual string Path => Original.Path;
        /// <inheritdoc/>
        public virtual string Name => Original.Name;
        /// <inheritdoc/>
        public virtual DateTimeOffset LastModified => Original.LastModified;
        /// <inheritdoc/>
        public virtual DateTimeOffset LastAccess => Original.LastAccess;
        /// <inheritdoc/>
        public virtual bool IsFile => Original.IsFile();
        /// <inheritdoc/>
        public virtual long Length => Original.Length();
        /// <inheritdoc/>
        public virtual bool IsDirectory => Original.IsDirectory();
        /// <inheritdoc/>
        public virtual IFileSystemOption Option => Original.Options();
        /// <inheritdoc/>
        public virtual bool IsDrive => Original.IsDrive();
        /// <inheritdoc/>
        public virtual bool IsMount => Original.IsMount();

        /// <summary>
        /// Create decorated entry.
        /// </summary>
        /// <param name="original"></param>
        public FileSystemEntryDecoration(IFileSystemEntry original)
        {
            Original = original ?? throw new ArgumentNullException(nameof(original));
        }

        /// <summary>
        /// New overriding filesystem.
        /// </summary>
        public class NewFileSystem : FileSystemEntryDecoration
        {
            /// <summary>New overriding filesystem.</summary>
            protected IFileSystem newFileSystem;
            /// <summary>New overriding filesystem.</summary>
            public override IFileSystem FileSystem => newFileSystem;
            /// <summary>
            /// Create decoration with <paramref name="newFileSystem"/>.
            /// </summary>
            /// <param name="original"></param>
            /// <param name="newFileSystem"></param>
            public NewFileSystem(IFileSystemEntry original, IFileSystem newFileSystem) : base(original)
            {
                this.newFileSystem = newFileSystem;
            }
        }

        /// <summary>
        /// New overriding filesystem and Path
        /// </summary>
        public class NewFileSystemAndPath : FileSystemEntryDecoration
        {
            /// <summary>New overriding filesystem.</summary>
            protected IFileSystem newFileSystem;
            /// <summary>New overriding path.</summary>
            protected string newPath;
            /// <summary>New overriding filesystem.</summary>
            public override IFileSystem FileSystem => newFileSystem;
            /// <summary>New path.</summary>
            public override string Path => newPath;
            /// <summary>
            /// Create decoration with <paramref name="newFileSystem"/>.
            /// </summary>
            /// <param name="original"></param>
            /// <param name="newFileSystem"></param>
            /// <param name="newPath"></param>
            public NewFileSystemAndPath(IFileSystemEntry original, IFileSystem newFileSystem, string newPath) : base(original)
            {
                this.newFileSystem = newFileSystem;
                this.newPath = newPath;
            }
        }

        /// <summary>
        /// New overriding filesystem, Path and Option modifier
        /// </summary>
        public class NewFileSystemPathAndOptionModifier : FileSystemEntryDecoration
        {
            /// <summary>New overriding filesystem.</summary>
            protected IFileSystem newFileSystem;
            /// <summary>New overriding path.</summary>
            protected string newPath;
            /// <summary>New overriding filesystem.</summary>
            public override IFileSystem FileSystem => newFileSystem;
            /// <summary>New path.</summary>
            public override string Path => newPath;
            /// <summary>(optional) Option that will be intersected lazily with original options.</summary>
            protected IFileSystemOption optionModifier;
            /// <summary>Lazily construction intersection of <see cref="optionModifier"/> and <see cref="Original"/>.Option()</summary>
            protected IFileSystemOption optionIntersection;
            /// <summary>Intersection of <see cref="Original"/>.Option() and <see cref="optionModifier"/></summary>
            public override IFileSystemOption Option => optionIntersection ?? (optionIntersection = FileSystemOption.Intersection(optionModifier, Original.Options()));
            /// <summary>
            /// Create decoration with <paramref name="newFileSystem"/>.
            /// </summary>
            /// <param name="original"></param>
            /// <param name="newFileSystem"></param>
            /// <param name="newPath"></param>
            /// <param name="optionModifier">(optional) option that will be applied to original option with intersection</param>
            public NewFileSystemPathAndOptionModifier(IFileSystemEntry original, IFileSystem newFileSystem, string newPath, IFileSystemOption optionModifier) : base(original)
            {
                this.newFileSystem = newFileSystem;
                this.newPath = newPath;
                this.optionModifier = optionModifier;
            }
        }

    }

}
