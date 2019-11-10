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
    /// File entry.
    /// </summary>
    public class FileEntry : EntryBase, IFileEntry, IEntryPhysicalPath
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
        /// (optional) Physical (OS) path to the file.
        /// </summary>
        public string PhysicalPath { get; protected set; }

        /// <summary>
        /// Create entry without file attributes.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="lastModified"></param>
        /// <param name="lastAccess"></param>
        /// <param name="length"></param>
        /// <param name="physicalPath"></param>
        public FileEntry(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess, long length, string physicalPath) : base(filesystem, path, name, lastModified, lastAccess)
        {
            this.Length = length;
            this.PhysicalPath = physicalPath;
        }

        /// <summary>Entry with custom option.</summary>
        public class AndOption : FileEntry, IEntryOptions
        {
            /// <summary>Options that describe features and capabilities in that apply to this entry.</summary>
            public IOption Options { get; protected set; }
            /// <summary>Create entry with custom option</summary>
            public AndOption(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess, long length, IOption options, string physicalPath) : base(filesystem, path, name, lastModified, lastAccess, length, physicalPath)
            {
                this.Options = options;
            }
        }

        /// <summary>Entry with file attributes.</summary>
        public class WithAttributes : FileEntry, IEntryFileAttributes
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
            public WithAttributes(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess, long length, FileAttributes fileAttributes, string physicalPath) : base(filesystem, path, name, lastModified, lastAccess, length, physicalPath)
            {
                this.FileAttributes = fileAttributes;
            }

            /// <summary>Entry with custom option.</summary>
            public new class AndOption : WithAttributes, IEntryOptions
            {
                /// <summary>Options that describe features and capabilities in that apply to this entry.</summary>
                public IOption Options { get; protected set; }
                /// <summary>Create entry with custom option</summary>
                public AndOption(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess, long length, FileAttributes fileAttributes, IOption options, string physicalPath) : base(filesystem, path, name, lastModified, lastAccess, length, fileAttributes, physicalPath)
                {
                    this.Options = options;
                }
            }
        }
    }

    /// <summary>
    /// File entry.
    /// </summary>
    public class DirectoryAndFileEntry : FileEntry, IFileEntry, IDirectoryEntry, IEntryPhysicalPath
    {
        /// <summary>
        /// Test if is a directory
        /// </summary>
        public bool IsDirectory => true;

        /// <summary>
        /// Create entry without file attributes.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="lastModified"></param>
        /// <param name="lastAccess"></param>
        /// <param name="length"></param>
        /// <param name="physicalPath"></param>
        public DirectoryAndFileEntry(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess, long length, string physicalPath) : base(filesystem, path, name, lastModified, lastAccess, length, physicalPath)
        {
        }

        /// <summary>Entry with custom option.</summary>
        public new class AndOption : DirectoryAndFileEntry, IEntryOptions
        {
            /// <summary>Options that describe features and capabilities in that apply to this entry.</summary>
            public IOption Options { get; protected set; }
            /// <summary>Create entry with custom option</summary>
            public AndOption(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess, long length, IOption options, string physicalPath) : base(filesystem, path, name, lastModified, lastAccess, length, physicalPath)
            {
                this.Options = options;
            }
        }

        /// <summary>Entry with file attributes.</summary>
        public new class WithAttributes : DirectoryAndFileEntry, IEntryFileAttributes
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
            public WithAttributes(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess, long length, FileAttributes fileAttributes, string physicalPath) : base(filesystem, path, name, lastModified, lastAccess, length, physicalPath)
            {
                this.FileAttributes = fileAttributes;
            }

            /// <summary>Entry with custom option.</summary>
            public new class AndOption : WithAttributes, IEntryOptions
            {
                /// <summary>Options that describe features and capabilities in that apply to this entry.</summary>
                public IOption Options { get; protected set; }
                /// <summary>Create entry with custom option</summary>
                public AndOption(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess, long length, FileAttributes fileAttributes, IOption options, string physicalPath) : base(filesystem, path, name, lastModified, lastAccess, length, fileAttributes, physicalPath)
                {
                    this.Options = options;
                }
            }
        }
    }


    /// <summary>
    /// Directory entry.
    /// </summary>
    public class DirectoryEntry : EntryBase, IDirectoryEntry, IEntryPhysicalPath
    {
        /// <summary>
        /// Tests if entry represents a directory.
        /// </summary>
        public bool IsDirectory => true;

        /// <summary>
        /// (optional) Physical path
        /// </summary>
        public string PhysicalPath { get; protected set; }

        /// <summary>
        /// Create entry
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="lastModified"></param>
        /// <param name="lastAccess"></param>
        /// <param name="physicalPath"></param>
        public DirectoryEntry(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess, string physicalPath) : base(filesystem, path, name, lastModified, lastAccess)
        {
            this.PhysicalPath = physicalPath;
        }

        /// <summary>Directory with custom option.</summary>
        public class AndOption : DirectoryEntry, IEntryOptions
        {
            /// <summary>Options that describe features and capabilities in that apply to this entry.</summary>
            public IOption Options { get; protected set; }

            /// <summary>Create entry with custom option</summary>
            public AndOption(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess, string physicalPath, IOption options) : base(filesystem, path, name, lastModified, lastAccess, physicalPath)
            {
                this.Options = options;
            }
        }

        /// <summary>Entry with file attributes.</summary>
        public class WithAttributes : DirectoryEntry, IEntryFileAttributes
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
            public WithAttributes(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess, FileAttributes fileAttributes, string physicalPath) : base(filesystem, path, name, lastModified, lastAccess, physicalPath)
            {
                this.FileAttributes = fileAttributes;
            }

            /// <summary>Entry with custom option.</summary>
            public new class AndOption : WithAttributes, IEntryOptions
            {
                /// <summary>Options that describe features and capabilities in that apply to this entry.</summary>
                public IOption Options { get; protected set; }
                /// <summary>Create entry with custom option</summary>
                public AndOption(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess, FileAttributes fileAttributes, IOption options, string physicalPath) : base(filesystem, path, name, lastModified, lastAccess, fileAttributes, physicalPath)
                {
                    this.Options = options;
                }
            }
        }

    }

    /// <summary>
    /// Drive entry.
    /// </summary>
    public class DriveEntry : EntryBase, IDriveEntry, IDirectoryEntry, IEntryPhysicalPath
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
        /// (optional) Physical path
        /// </summary>
        public string PhysicalPath { get; protected set; }

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
        /// <param name="physicalPath"></param>
        public DriveEntry(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess, System.IO.DriveType driveType, long driveFreeSpace, long driveSize, string driveLabel, string driveFormat, bool isDirectory, string physicalPath) : base(filesystem, path, name, lastModified, lastAccess)
        {
            this.DriveType = driveType;
            this.DriveFreeSpace = driveFreeSpace;
            this.DriveSize = driveSize;
            this.DriveLabel = driveLabel;
            this.DriveFormat = driveFormat;
            this.IsDirectory = isDirectory;
            this.PhysicalPath = physicalPath;
        }

        /// <summary>Entry with custom option.</summary>
        public class AndOption : DriveEntry, IEntryOptions
        {
            /// <summary>Options that describe features and capabilities in that apply to this entry.</summary>
            public IOption Options { get; protected set; }
            /// <summary>Create entry with custom option</summary>
            public AndOption(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess, System.IO.DriveType driveType, long driveFreeSpace, long driveSize, string driveLabel, string driveFormat, bool isDirectory, IOption options, string physicalPath) : base(filesystem, path, name, lastModified, lastAccess, driveType, driveFreeSpace, driveSize, driveLabel, driveFormat, isDirectory, physicalPath)
            {
                this.Options = options;
            }
        }


    }

    /// <summary>
    /// Mount or decoration entry.
    /// </summary>
    public class MountEntry : DirectoryEntry, IMountEntry
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
        /// <param name="physicalPath"></param>
        /// <param name="mounts"></param>
        public MountEntry(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess, string physicalPath, FileSystemAssignment[] mounts) : base(filesystem, path, name, lastModified, lastAccess, physicalPath)
        {
            this.Mounts = mounts;
        }

        /// <summary>Entry with custom option.</summary>
        public new class AndOption : MountEntry, IEntryOptions
        {
            /// <summary>Options that describe features and capabilities in that apply to this entry.</summary>
            public IOption Options { get; protected set; }
            /// <summary>Create entry with custom option</summary>
            public AndOption(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess, string physicalPath, FileSystemAssignment[] mounts, IOption options) : base(filesystem, path, name, lastModified, lastAccess, physicalPath, mounts)
            {
                this.Options = options;
            }
        }

        /// <summary>Entry with file attributes.</summary>
        public new class WithAttributes : MountEntry, IEntryFileAttributes
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
            public WithAttributes(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess, string physicalPath, FileSystemAssignment[] mounts, FileAttributes fileAttributes) : base(filesystem, path, name, lastModified, lastAccess, physicalPath, mounts)
            {
                this.FileAttributes = fileAttributes;
            }

            /// <summary>Entry with custom option.</summary>
            public new class AndOption : WithAttributes, IEntryOptions
            {
                /// <summary>Options that describe features and capabilities in that apply to this entry.</summary>
                public IOption Options { get; protected set; }
                /// <summary>Create entry with custom option</summary>
                public AndOption(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess, string physicalPath, FileSystemAssignment[] mounts, FileAttributes fileAttributes, IOption options) : base(filesystem, path, name, lastModified, lastAccess, physicalPath, mounts, fileAttributes)
                {
                    this.Options = options;
                }
            }
        }
    }

}

