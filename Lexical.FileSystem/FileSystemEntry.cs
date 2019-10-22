// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Decoration;
using System;
using System.IO;

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
        /// Create entry without file attributes.
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

        /// <summary>Entry with custom option.</summary>
        public class AndOption : FileSystemEntryFile, IFileSystemEntryOptions
        {
            /// <summary>Options that describe features and capabilities in that apply to this entry.</summary>
            public IFileSystemOption Options { get; protected set; }
            /// <summary>Create entry with custom option</summary>
            public AndOption(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess, long length, IFileSystemOption options) : base(filesystem, path, name, lastModified, lastAccess, length)
            {
                this.Options = options;
            }
        }

        /// <summary>Entry with file attributes.</summary>
        public class WithAttributes : FileSystemEntryFile, IFileSystemEntryFileAttributes
        {
            /// <summary>
            /// Has file attributes
            /// </summary>
            public bool HasFileAttributes => true;

            /// <summary>
            /// File attributes
            /// </summary>
            public FileAttributes FileAttributes { get; protected set; }

            /// <summary>Create entry with custom option</summary>
            public WithAttributes(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess, long length, FileAttributes fileAttributes) : base(filesystem, path, name, lastModified, lastAccess, length)
            {
                this.FileAttributes = fileAttributes;
            }

            /// <summary>Entry with custom option.</summary>
            public new class AndOption : WithAttributes, IFileSystemEntryOptions
            {
                /// <summary>Options that describe features and capabilities in that apply to this entry.</summary>
                public IFileSystemOption Options { get; protected set; }
                /// <summary>Create entry with custom option</summary>
                public AndOption(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess, long length, FileAttributes fileAttributes, IFileSystemOption options) : base(filesystem, path, name, lastModified, lastAccess, length, fileAttributes)
                {
                    this.Options = options;
                }
            }
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
        /// <param name="filesystem"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="lastModified"></param>
        /// <param name="lastAccess"></param>
        public FileSystemEntryDirectory(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess) : base(filesystem, path, name, lastModified, lastAccess)
        {
        }

        /// <summary>Directory with custom option.</summary>
        public class AndOption : FileSystemEntryDirectory, IFileSystemEntryOptions
        {
            /// <summary>Options that describe features and capabilities in that apply to this entry.</summary>
            public IFileSystemOption Options { get; protected set; }

            /// <summary>Create entry with custom option</summary>
            public AndOption(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess, IFileSystemOption options) : base(filesystem, path, name, lastModified, lastAccess)
            {
                this.Options = options;
            }
        }

        /// <summary>Entry with file attributes.</summary>
        public class WithAttributes : FileSystemEntryDirectory, IFileSystemEntryFileAttributes
        {
            /// <summary>
            /// Has file attributes
            /// </summary>
            public bool HasFileAttributes => true;

            /// <summary>
            /// File attributes
            /// </summary>
            public FileAttributes FileAttributes { get; protected set; }

            /// <summary>Create entry with custom option</summary>
            public WithAttributes(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess, FileAttributes fileAttributes) : base(filesystem, path, name, lastModified, lastAccess)
            {
                this.FileAttributes = fileAttributes;
            }

            /// <summary>Entry with custom option.</summary>
            public new class AndOption : WithAttributes, IFileSystemEntryOptions
            {
                /// <summary>Options that describe features and capabilities in that apply to this entry.</summary>
                public IFileSystemOption Options { get; protected set; }
                /// <summary>Create entry with custom option</summary>
                public AndOption(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess, FileAttributes fileAttributes, IFileSystemOption options) : base(filesystem, path, name, lastModified, lastAccess, fileAttributes)
                {
                    this.Options = options;
                }
            }
        }

    }

    /// <summary>
    /// Drive entry.
    /// </summary>
    public class FileSystemEntryDrive : FileSystemEntryBase, IFileSystemEntryDrive, IFileSystemEntryDirectory
    {
        /// <summary>
        /// Tests if entry represents a directory.
        /// </summary>
        public bool IsDirectory { get; protected set; }

        /// <summary>
        /// Tests if entry represents a drive.
        /// </summary>
        public bool IsDrive => true;

        /// <summary>
        /// Drive type.
        /// </summary>
        public System.IO.DriveType DriveType { get; protected set; }

        /// <summary>
        /// Free space, -1L if unknown.
        /// </summary>
        public long DriveFreeSpace { get; protected set; }

        /// <summary>
        /// Total size of drive or volume. -1L if unkown.
        /// </summary>
        public long DriveSize { get; protected set; }

        /// <summary>
        /// Label, or null if unknown.
        /// </summary>
        public String DriveLabel { get; protected set; }

        /// <summary>
        /// File system format.
        /// 
        /// Examples:
        /// <list type="bullet">
        ///     <item>NTFS</item>
        ///     <item>FAT32</item>
        /// </list>
        /// </summary>
        public String DriveFormat { get; protected set; }

        /// <summary>
        /// Create entry
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="lastModified"></param>
        /// <param name="lastAccess"></param>
        /// <param name="driveType"></param>
        /// <param name="driveFreeSpace"></param>
        /// <param name="driveSize"></param>
        /// <param name="driveLabel"></param>
        /// <param name="driveFormat"></param>
        /// <param name="isDirectory">Does entry represent a directory (is entry ready, mounted and readable)</param>
        public FileSystemEntryDrive(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess, System.IO.DriveType driveType, long driveFreeSpace, long driveSize, string driveLabel, string driveFormat, bool isDirectory) : base(filesystem, path, name, lastModified, lastAccess)
        {
            this.DriveType = driveType;
            this.DriveFreeSpace = driveFreeSpace;
            this.DriveSize = driveSize;
            this.DriveLabel = driveLabel;
            this.DriveFormat = driveFormat;
            this.IsDirectory = isDirectory;
        }

        /// <summary>Entry with custom option.</summary>
        public class AndOption : FileSystemEntryDrive, IFileSystemEntryOptions
        {
            /// <summary>Options that describe features and capabilities in that apply to this entry.</summary>
            public IFileSystemOption Options { get; protected set; }
            /// <summary>Create entry with custom option</summary>
            public AndOption(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess, System.IO.DriveType driveType, long driveFreeSpace, long driveSize, string driveLabel, string driveFormat, bool isDirectory, IFileSystemOption options) : base(filesystem, path, name, lastModified, lastAccess, driveType, driveFreeSpace, driveSize, driveLabel, driveFormat, isDirectory)
            {
                this.Options = options;
            }
        }


    }

    /// <summary>
    /// Mount or decoration entry.
    /// </summary>
    public class FileSystemEntryMount : FileSystemEntryDirectory, IFileSystemEntryMount
    {
        /// <summary>
        /// Tests if entry represents a mount root.
        /// </summary>
        public bool IsMountPoint => true;

        /// <summary>
        /// Mounts
        /// </summary>
        public FileSystemAssignment[] Mounts { get; protected set; }

        /// <summary>
        /// Create entry
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="lastModified"></param>
        /// <param name="lastAccess"></param>
        /// <param name="mounts"></param>
        public FileSystemEntryMount(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess, FileSystemAssignment[] mounts) : base(filesystem, path, name, lastModified, lastAccess)
        {
            this.Mounts = mounts;
        }

        /// <summary>Entry with custom option.</summary>
        public new class AndOption : FileSystemEntryMount, IFileSystemEntryOptions
        {
            /// <summary>Options that describe features and capabilities in that apply to this entry.</summary>
            public IFileSystemOption Options { get; protected set; }
            /// <summary>Create entry with custom option</summary>
            public AndOption(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess, FileSystemAssignment[] mounts, IFileSystemOption options) : base(filesystem, path, name, lastModified, lastAccess, mounts)
            {
                this.Options = options;
            }
        }

        /// <summary>Entry with file attributes.</summary>
        public new class WithAttributes : FileSystemEntryMount, IFileSystemEntryFileAttributes
        {
            /// <summary>
            /// Has file attributes
            /// </summary>
            public bool HasFileAttributes => true;

            /// <summary>
            /// File attributes
            /// </summary>
            public FileAttributes FileAttributes { get; protected set; }

            /// <summary>Create entry with custom option</summary>
            public WithAttributes(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess, FileSystemAssignment[] mounts, FileAttributes fileAttributes) : base(filesystem, path, name, lastModified, lastAccess, mounts)
            {
                this.FileAttributes = fileAttributes;
            }

            /// <summary>Entry with custom option.</summary>
            public new class AndOption : WithAttributes, IFileSystemEntryOptions
            {
                /// <summary>Options that describe features and capabilities in that apply to this entry.</summary>
                public IFileSystemOption Options { get; protected set; }
                /// <summary>Create entry with custom option</summary>
                public AndOption(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess, FileSystemAssignment[] mounts, FileAttributes fileAttributes, IFileSystemOption options) : base(filesystem, path, name, lastModified, lastAccess, mounts, fileAttributes)
                {
                    this.Options = options;
                }
            }
        }
    }

}

