// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;

namespace Lexical.FileSystem
{
    /// <summary>
    /// File-system entry.
    /// Entry is a snapshot at the time of creationg.
    /// </summary>
    public class FileSystemEntry : IFileSystemEntry
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
        public FileSystemEntry(IFileSystem fileSystem, string path, string name, DateTimeOffset lastModified)
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
    public class FileSystemEntryFile : FileSystemEntry, IFileSystemEntryFile
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
    public class FileSystemEntryDirectory : FileSystemEntry, IFileSystemEntryDirectory
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
    public class FileSystemEntryDrive : FileSystemEntry, IFileSystemEntryDrive
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

}
