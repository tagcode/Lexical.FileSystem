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
    public abstract class FileSystemEntryDecoration : IFileSystemEntryDecoration, IFileSystemEntryFile, IFileSystemEntryDirectory, IFileSystemEntryDrive, IFileSystemEntryMountpoint
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
        public virtual bool IsFile => Original.IsFile();
        /// <inheritdoc/>
        public virtual long Length => Original.Length();
        /// <inheritdoc/>
        public virtual bool IsDirectory => Original.IsDirectory();
        /// <inheritdoc/>
        public virtual IFileSystemOption Options => Original.Options();
        /// <inheritdoc/>
        public virtual bool IsDrive => Original.IsDrive();
        /// <inheritdoc/>
        public virtual bool IsMountpoint => Original.IsMountpoint();

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
        protected class NewFileSystem : FileSystemEntryDecoration
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
        protected class NewFileSystemAndPath : FileSystemEntryDecoration
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
    }

}
