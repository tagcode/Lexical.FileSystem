// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;

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

        /// <summary>
        /// Options that apply to this directory. 
        /// 
        /// The options returned here is equal to or a subset of options in the parent <see cref="IFileSystem"/>.
        /// </summary>
        IFileSystemOption Option { get; }
    }
    // </IFileSystemEntryDirectory>

    // <IFileSystemEntryDrive>
    /// <summary>
    /// Drive entry. 
    /// 
    /// If drive class is browsable, then the implementation also implements <see cref="IFileSystemEntryDirectory"/>.
    /// </summary>
    public interface IFileSystemEntryDrive : IFileSystemEntry
    {
        /// <summary>
        /// Tests if entry represents a drive.
        /// </summary>
        bool IsDrive { get; }
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
    /// Entry that is actually a decoration. 
    /// 
    /// Decorating classes can implement this interface if they want to expose the original entry.
    /// </summary>
    public interface IFileSystemEntryDecoration : IFileSystemEntry
    {
        /// <summary>
        /// (Optional) Original entry that is being decorated.
        /// </summary>
        IFileSystemEntry Original { get; }
    }
    // </IFileSystemEntryDecoration>

}
