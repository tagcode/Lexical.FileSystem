// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Decoration;
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
        /// Last access time of entry. If Unknown returns <see cref="DateTimeOffset.MinValue"/>.
        /// </summary>
        public DateTimeOffset LastAccess { get; protected set; }

        /// <summary>
        /// Create entry
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="lastModified"></param>
        /// <param name="lastAccess"></param>
        public FileSystemEntryBase(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess)
        {
            FileSystem = filesystem;
            Path = path;
            Name = name;
            LastModified = lastModified;
            LastAccess = lastAccess;
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
        /// <param name="filesystem"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="lastModified"></param>
        /// <param name="lastAccess"></param>
        /// <param name="length"></param>
        public FileSystemEntryFile(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess, long length) : base(filesystem, path, name, lastModified, lastAccess)
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
        /// Options that describe features and capabilities in that apply to entries in this directory.
        /// </summary>
        public IFileSystemOption Option { get; protected set; }

        /// <summary>
        /// Create entry
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="lastModified"></param>
        /// <param name="lastAccess"></param>
        /// <param name="options"></param>
        public FileSystemEntryDirectory(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess, IFileSystemOption options) : base(filesystem, path, name, lastModified, lastAccess)
        {
            this.Option = options ?? throw new ArgumentNullException(nameof(options));
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
        /// <param name="filesystem"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="lastModified"></param>
        /// <param name="lastAccess"></param>
        public FileSystemEntryDrive(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess) : base(filesystem, path, name, lastModified, lastAccess)
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
        /// Options that apply to this directory.
        /// </summary>
        public IFileSystemOption Option { get; protected set; }

        /// <summary>
        /// Create entry
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="lastModified"></param>
        /// <param name="lastAccess"></param>
        /// <param name="options"></param>
        public FileSystemEntryDriveDirectory(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess, IFileSystemOption options) : base(filesystem, path, name, lastModified, lastAccess)
        {
            this.Option = options ?? throw new ArgumentNullException(nameof(options));
        }
    }

    /// <summary>
    /// Mount directory entry.
    /// </summary>
    public class FileSystemEntryMount : FileSystemEntryDirectory, IFileSystemEntryMount
    {
        /// <summary>
        /// Tests if entry represents a mount root.
        /// </summary>
        public bool IsMount => true;

        /// <summary>
        /// Create entry
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="lastModified"></param>
        /// <param name="lastAccess"></param>
        /// <param name="options"></param>
        public FileSystemEntryMount(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess, IFileSystemOption options) : base(filesystem, path, name, lastModified, lastAccess, options)
        {
        }
    }

}
