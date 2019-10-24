// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.IO;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Extension methods for <see cref="IFileSystemEntry"/>.
    /// </summary>
    public static partial class IFileSystemEntryExtensions
    {
        /// <summary>
        /// File length. -1 if is length is unknown.
        /// </summary>
        /// <returns>File length. -1 if is length is unknown.</returns>
        public static long Length(this IFileSystemEntry entry)
            => entry is IFileSystemEntryFile file ? file.Length : -1L;

        /// <summary>
        /// Tests if <paramref name="entry"/> represents a file.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public static bool IsFile(this IFileSystemEntry entry)
            => entry is IFileSystemEntryFile file ? file.IsFile : false;

        /// <summary>
        /// Tests if <paramref name="entry"/> represents a directory.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public static bool IsDirectory(this IFileSystemEntry entry)
            => entry is IFileSystemEntryDirectory dir ? dir.IsDirectory : false;

        /// <summary>
        /// Get options
        /// </summary>
        /// <param name="entry"></param>
        /// <returns>options or null.</returns>
        public static IFileSystemOption Options(this IFileSystemEntry entry)
            => entry is IFileSystemEntryOptions options ? options.Options : null;

        /// <summary>
        /// Get effective options
        /// </summary>
        /// <param name="entry"></param>
        /// <returns>options.</returns>
        public static IFileSystemOption EffectiveOptions(this IFileSystemEntry entry)
            => entry is IFileSystemEntryOptions options ? (options.Options??entry.FileSystem) : entry.FileSystem;

        /// <summary>
        /// Tests if <paramref name="entry"/> represents a drive.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public static bool IsDrive(this IFileSystemEntry entry)
            => entry is IFileSystemEntryDrive drive ? drive.IsDrive : false;

        /// <summary>
        /// Drive type.
        /// </summary>
        public static DriveType DriveType(this IFileSystemEntry entry)
            => entry is IFileSystemEntryDrive drive ? drive.DriveType : System.IO.DriveType.Unknown;

        /// <summary>
        /// Free space, -1L if unknown.
        /// </summary>
        public static long DriveFreeSpace(this IFileSystemEntry entry)
            => entry is IFileSystemEntryDrive drive ? drive.DriveFreeSpace: -1L;

        /// <summary>
        /// Total size of drive or volume. -1L if unkown.
        /// </summary>
        public static long DriveSize(this IFileSystemEntry entry)
            => entry is IFileSystemEntryDrive drive ? drive.DriveSize : -1L;

        /// <summary>
        /// Label, or null if unknown.
        /// </summary>
        public static String DriveLabel(this IFileSystemEntry entry)
            => entry is IFileSystemEntryDrive drive ? drive.DriveLabel : null;

        /// <summary>
        /// File system format.
        /// 
        /// Examples:
        /// <list type="bullet">
        ///     <item>NTFS</item>
        ///     <item>FAT32</item>
        /// </list>
        /// </summary>
        public static String DriveFormat(this IFileSystemEntry entry)
            => entry is IFileSystemEntryDrive drive ? drive.DriveFormat : null;

        /// <summary>
        /// Tests if <paramref name="entry"/> represents a mount root.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public static bool IsMountPoint(this IFileSystemEntry entry)
            => entry is IFileSystemEntryMount mount ? mount.IsMountPoint : false;

        /// <summary>
        /// (optional) Mounted filesystem.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns>mounted filesystem or null</returns>
        public static FileSystemAssignment[] Mounts(this IFileSystemEntry entry)
            => entry is IFileSystemEntryMount mount ? mount.Mounts : null;

        /// <summary>
        /// Tests if <paramref name="entry"/> is a directory that is automatically mounted.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns>true if automatically mounted</returns>
        public static bool IsAutoMounted(this IFileSystemEntry entry)
        {
            if (entry is IFileSystemEntryMount mountEntry && mountEntry.IsMountPoint())
            {
                foreach(var mount in mountEntry.Mounts)
                {
                    if (mount.AutoMount) return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Tests if <paramref name="entry"/> has <see cref="FileAttributes"/>.
        /// </summary>
        /// <returns>true if it has file attributes</returns>
        public static bool HasFileAttributes(this IFileSystemEntry entry)
            => entry is IFileSystemEntryFileAttributes attributes ? attributes.HasFileAttributes : false;

        /// <summary>
        /// Gets file attributes of <paramref name="entry"/>.
        /// 
        /// The caller should first test if attributes exist with <see cref="HasFileAttributes(IFileSystemEntry)"/>.
        /// </summary>
        /// <returns>file attributes</returns>
        public static FileAttributes FileAttributes(this IFileSystemEntry entry)
            => entry is IFileSystemEntryFileAttributes attributes ? attributes.FileAttributes : 0;

        /// <summary>
        /// Gets physical (OS) file or directory path of <paramref name="entry"/>.
        /// </summary>
        /// <returns>(optional) file path</returns>
        public static string PhysicalPath(this IFileSystemEntry entry)
            => entry is IFileSystemEntryPhysicalPath physicalPath ? physicalPath.PhysicalPath : null;

    }

}
