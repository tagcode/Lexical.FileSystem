// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.IO;

namespace Lexical.FileSystem
{
    // <IFileSystemEntry>
    /// <summary>
    /// Entry that represents a node of a <see cref="IFileSystem"/>.
    /// 
    /// The entry represents the snapshot state at the time of creation.
    /// 
    /// See sub-interfaces:
    /// <list type="bullet">
    ///     <item><see cref="IFileSystemEntryFile"/></item>
    ///     <item><see cref="IFileSystemEntryDirectory"/></item>
    ///     <item><see cref="IFileSystemEntryDrive"/></item>
    ///     <item><see cref="IFileSystemEntryMount"/></item>
    ///     <item><see cref="IFileSystemEntryOptions"/></item>
    ///     <item><see cref="IFileSystemEntryFileAttributes"/></item>
    ///     <item><see cref="IFileSystemEntryPhysicalPath"/></item>
    /// </list>    
    /// </summary>
    public interface IFileSystemEntry
    {
        /// <summary>
        /// (optional) Associated file system.
        /// </summary>
        IFileSystem FileSystem { get; }

        /// <summary>
        /// Path that is relative to the <see cref="IFileSystem"/>.
        /// 
        /// Separator is forward slash "/".
        /// Directories end with "/" unless root directory.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Entry name in its parent context.
        /// 
        /// All characters are legal, including control characters, except forward slash '/'. 
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Date time of last modification. In UTC time, if possible. If Unknown returns <see cref="DateTimeOffset.MinValue"/>.
        /// </summary>
        DateTimeOffset LastModified { get; }

        /// <summary>
        /// Last access time of entry. If Unknown returns <see cref="DateTimeOffset.MinValue"/>.
        /// </summary>
        DateTimeOffset LastAccess { get; }
    }
    // </IFileSystemEntry>

    // <IFileSystemEntryFile>
    /// <summary>
    /// File entry
    /// </summary>
    public interface IFileSystemEntryFile : IFileSystemEntry
    {
        /// <summary>
        /// Tests if entry represents a file.
        /// </summary>
        bool IsFile { get; }

        /// <summary>
        /// File length. -1 if is length is unknown.
        /// </summary>
        long Length { get; }
    }
    // </IFileSystemEntryFile>

    // <IFileSystemEntryDirectory>
    /// <summary>
    /// Directory entry that can be browsed for contents with <see cref="IFileSystemBrowse"/>.
    /// </summary>
    public interface IFileSystemEntryDirectory : IFileSystemEntry
    {
        /// <summary>
        /// Tests if entry represents a directory.
        /// </summary>
        bool IsDirectory { get; }
    }
    // </IFileSystemEntryDirectory>

    // <IFileSystemEntryDrive>
    /// <summary>
    /// Drive or volume entry. 
    /// 
    /// If drive class is browsable, then the implementation also implements <see cref="IFileSystemEntryDirectory"/>.
    /// </summary>
    public interface IFileSystemEntryDrive : IFileSystemEntry
    {
        /// <summary>
        /// Tests if entry represents a drive or volume.
        /// </summary>
        bool IsDrive { get; }

        /// <summary>
        /// Drive type.
        /// </summary>
        DriveType DriveType { get; }

        /// <summary>
        /// Free space, -1L if unknown.
        /// </summary>
        long DriveFreeSpace { get; }

        /// <summary>
        /// Total size of drive or volume. -1L if unkown.
        /// </summary>
        long DriveSize { get; }

        /// <summary>
        /// Label, or null if unknown.
        /// </summary>
        String DriveLabel { get; }

        /// <summary>
        /// File system format.
        /// 
        /// Examples:
        /// <list type="bullet">
        ///     <item>NTFS</item>
        ///     <item>FAT32</item>
        /// </list>
        /// </summary>
        String DriveFormat { get; }
    }
    // </IFileSystemEntryDrive>

    // <IFileSystemEntryMount>
    /// <summary>
    /// Entry represents a mount point (decoration or virtual filesystem directory). 
    /// </summary>
    public interface IFileSystemEntryMount : IFileSystemEntry
    {
        /// <summary>
        /// Tests if directory represents a mount point.
        /// </summary>
        bool IsMountPoint { get; }

        /// <summary>
        /// (optional) Manually mounted filesystem(s).
        /// </summary>
        FileSystemAssignment[] Mounts { get; }
    }
    // </IFileSystemEntryMount>

    // <IFileSystemEntryDecoration>
    /// <summary>
    /// Optional interface that exposes decoree.
    /// </summary>
    public interface IFileSystemEntryDecoration : IFileSystemEntry
    {
        /// <summary>
        /// (Optional) Original entry that is being decorated.
        /// </summary>
        IFileSystemEntry Original { get; }
    }
    // </IFileSystemEntryDecoration>

    // <IFileSystemEntryOptions>
    /// <summary>
    /// Entry specific filesystem capability options.
    /// </summary>
    public interface IFileSystemEntryOptions : IFileSystemEntry
    {
        /// <summary>
        /// (optional) Options that apply to this entry. The options here are equal or subset of the options in the parenting <see cref="IFileSystem"/>.
        /// </summary>
        IFileSystemOption Options { get; }
    }
    // </IFileSystemEntryOptions>

    // <IFileSystemEntryFileAttributes>
    /// <summary>
    /// Entry file Attributes.
    /// </summary>
    public interface IFileSystemEntryFileAttributes : IFileSystemEntry
    {
        /// <summary>
        /// True, if has attached <see cref="FileAttributes"/>.
        /// </summary>
        bool HasFileAttributes { get; }

        /// <summary>
        /// (optional) File attributes
        /// </summary>
        FileAttributes FileAttributes { get; }
    }
    // </IFileSystemEntryFileAttributes>

    // <IFileSystemEntryPhysicalPath>
    /// <summary>
    /// Optional interface for entries that may have a physical file or directory path.
    /// </summary>
    public interface IFileSystemEntryPhysicalPath : IFileSystemEntry
    {
        /// <summary>
        /// (optional) Physical (OS) path to file or directory.
        /// </summary>
        String PhysicalPath { get; }
    }
    // </IFileSystemEntryPhysicalPath>
}
