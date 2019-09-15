// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Base implementation for <see cref="IFileSystemEvent"/> classes. Entry is a snapshot at the time of creation.
    /// 
    /// See sub-classes:
    /// <list type="bullet">
    ///     <item><see cref="FileSystemEntryFile"/></item>
    ///     <item><see cref="FileSystemEntryDirectory"/></item>
    ///     <item><see cref="FileSystemEntryDrive"/></item>
    ///     <item><see cref="FileSystemEntryDriveDirectory"/></item>
    ///     <item><see cref="FileSystemEntryDecoration"/></item>
    /// </list>
    /// </summary>
    public abstract class FileSystemEntryBase : IFileSystemEntry
    {
        /// <summary>
        /// (optional) Associated file system.
        /// </summary>
        public IFileSystem FileSystem { get; protected set; }

        /// <summary>
        /// Path that is relative to the <see cref="IFileSystem"/>.
        /// Separator is "/".
        /// </summary>
        public string Path { get; protected set; }

        /// <summary>
        /// Entry name in its parent context.
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Date time of last modification.
        /// </summary>
        public DateTimeOffset LastModified { get; protected set; }

        /// <summary>
        /// Create entry
        /// </summary>
        /// <param name="fileSystem"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="lastModified"></param>
        public FileSystemEntryBase(IFileSystem fileSystem, string path, string name, DateTimeOffset lastModified)
        {
            FileSystem = fileSystem;
            Path = path;
            Name = name;
            LastModified = lastModified;
        }

        /// <summary>
        /// Print info.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => Path;
    }

    /// <summary>
    /// File entry.
    /// </summary>
    public class FileSystemEntryFile : FileSystemEntryBase, IFileSystemEntryFile
    {
        /// <summary>
        /// Tests if entry represents a file.
        /// </summary>
        public bool IsFile => true;

        /// <summary>
        /// File length. -1 if is length is unknown.
        /// </summary>
        public long Length { get; protected set; }

        /// <summary>
        /// Create entry
        /// </summary>
        /// <param name="fileSystem"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="lastModified"></param>
        /// <param name="length"></param>
        public FileSystemEntryFile(IFileSystem fileSystem, string path, string name, DateTimeOffset lastModified, long length) : base(fileSystem, path, name, lastModified)
        {
            Length = length;
        }
    }

    /// <summary>
    /// Directory entry.
    /// </summary>
    public class FileSystemEntryDirectory : FileSystemEntryBase, IFileSystemEntryDirectory
    {
        /// <summary>
        /// Tests if entry represents a directory.
        /// </summary>
        public bool IsDirectory => true;

        /// <summary>
        /// Create entry
        /// </summary>
        /// <param name="fileSystem"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="lastModified"></param>
        public FileSystemEntryDirectory(IFileSystem fileSystem, string path, string name, DateTimeOffset lastModified) : base(fileSystem, path, name, lastModified)
        {
        }
    }

    /// <summary>
    /// Drive entry.
    /// </summary>
    public class FileSystemEntryDrive : FileSystemEntryBase, IFileSystemEntryDrive
    {
        /// <summary>
        /// Tests if entry represents a drive.
        /// </summary>
        public bool IsDrive => true;

        /// <summary>
        /// Create entry
        /// </summary>
        /// <param name="fileSystem"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="lastModified"></param>
        public FileSystemEntryDrive(IFileSystem fileSystem, string path, string name, DateTimeOffset lastModified) : base(fileSystem, path, name, lastModified)
        {
        }
    }

    /// <summary>
    /// Drive entry that is also a directory (browsable).
    /// </summary>
    public class FileSystemEntryDriveDirectory : FileSystemEntryDrive, IFileSystemEntryDirectory
    {
        /// <summary>
        /// Tests if entry represents a directory.
        /// </summary>
        public bool IsDirectory => true;

        /// <summary>
        /// Create entry
        /// </summary>
        /// <param name="fileSystem"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="lastModified"></param>
        public FileSystemEntryDriveDirectory(IFileSystem fileSystem, string path, string name, DateTimeOffset lastModified) : base(fileSystem, path, name, lastModified)
        {
        }
    }

    /// <summary>
    /// Abstract base class for decorated entry.
    /// </summary>
    public abstract class FileSystemEntryDecoration : IFileSystemEntryDecoration, IFileSystemEntryFile, IFileSystemEntryDirectory, IFileSystemEntryDrive
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
        public virtual bool IsDrive => Original.IsDrive();

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
