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
    /// Extension methods for <see cref="IEntry"/>.
    /// </summary>
    public static partial class IEntryExtensions
    {
        /// <summary>
        /// File length. -1 if is length is unknown.
        /// </summary>
        /// <returns>File length. -1 if is length is unknown.</returns>
        public static long Length(this IEntry entry)
            => entry is IFileEntry file ? file.Length : -1L;

        /// <summary>
        /// Tests if <paramref name="entry"/> represents a file.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public static bool IsFile(this IEntry entry)
            => entry is IFileEntry file ? file.IsFile : false;

        /// <summary>
        /// Tests if <paramref name="entry"/> represents a directory.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public static bool IsDirectory(this IEntry entry)
            => entry is IDirectoryEntry dir ? dir.IsDirectory : false;

        /// <summary>
        /// Get options
        /// </summary>
        /// <param name="entry"></param>
        /// <returns>options or null.</returns>
        public static IOption Options(this IEntry entry)
            => entry is IEntryOptions options ? options.Options : null;

        /// <summary>
        /// Get effective options
        /// </summary>
        /// <param name="entry"></param>
        /// <returns>options.</returns>
        public static IOption EffectiveOptions(this IEntry entry)
            => entry is IEntryOptions options ? (options.Options??entry.FileSystem) : entry.FileSystem;

        /// <summary>
        /// Tests if <paramref name="entry"/> represents a drive.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public static bool IsDrive(this IEntry entry)
            => entry is IDriveEntry drive ? drive.IsDrive : false;

        /// <summary>
        /// Drive type.
        /// </summary>
        public static DriveType DriveType(this IEntry entry)
            => entry is IDriveEntry drive ? drive.DriveType : System.IO.DriveType.Unknown;

        /// <summary>
        /// Free space, -1L if unknown.
        /// </summary>
        public static long DriveFreeSpace(this IEntry entry)
            => entry is IDriveEntry drive ? drive.DriveFreeSpace: -1L;

        /// <summary>
        /// Total size of drive or volume. -1L if unkown.
        /// </summary>
        public static long DriveSize(this IEntry entry)
            => entry is IDriveEntry drive ? drive.DriveSize : -1L;

        /// <summary>
        /// Label, or null if unknown.
        /// </summary>
        public static String DriveLabel(this IEntry entry)
            => entry is IDriveEntry drive ? drive.DriveLabel : null;

        /// <summary>
        /// File system format.
        /// 
        /// Examples:
        /// <list type="bullet">
        ///     <item>NTFS</item>
        ///     <item>FAT32</item>
        /// </list>
        /// </summary>
        public static String DriveFormat(this IEntry entry)
            => entry is IDriveEntry drive ? drive.DriveFormat : null;

        /// <summary>
        /// Tests if <paramref name="entry"/> represents a mount root.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public static bool IsMountPoint(this IEntry entry)
            => entry is IMountEntry mount ? mount.IsMountPoint : false;

        /// <summary>
        /// (optional) Mounted filesystem.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns>mounted filesystem or null</returns>
        public static FileSystemAssignment[] Mounts(this IEntry entry)
            => entry is IMountEntry mount ? mount.Mounts : null;

        /// <summary>
        /// Tests if <paramref name="entry"/> is a directory that is automatically mounted.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns>true if automatically mounted</returns>
        public static bool IsAutoMounted(this IEntry entry)
        {
            if (entry is IMountEntry mountEntry && mountEntry.IsMountPoint())
            {
                foreach(var mount in mountEntry.Mounts)
                {
                    if (mount.Flags.HasFlag(FileSystemAssignmentFlags.AutoMounted)) return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Tests if <paramref name="entry"/> represents the root of a mounted package, such as .zip.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns>true if automatically mounted</returns>
        public static bool IsPackageMount(this IEntry entry)
        {
            if (entry is IMountEntry mountEntry && mountEntry.IsMountPoint())
            {
                foreach (var mount in mountEntry.Mounts)
                {
                    if (mount.Flags.HasFlag(FileSystemAssignmentFlags.Package)) return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Tests if <paramref name="entry"/> has <see cref="FileAttributes"/>.
        /// </summary>
        /// <returns>true if it has file attributes</returns>
        public static bool HasFileAttributes(this IEntry entry)
            => entry is IEntryFileAttributes attributes ? attributes.HasFileAttributes : false;

        /// <summary>
        /// Gets file attributes of <paramref name="entry"/>.
        /// 
        /// The caller should first test if attributes exist with <see cref="HasFileAttributes(IEntry)"/>.
        /// </summary>
        /// <returns>file attributes</returns>
        public static FileAttributes FileAttributes(this IEntry entry)
            => entry is IEntryFileAttributes attributes ? attributes.FileAttributes : 0;

        /// <summary>
        /// Gets physical (OS) file or directory path of <paramref name="entry"/>.
        /// </summary>
        /// <returns>(optional) file path</returns>
        public static string PhysicalPath(this IEntry entry)
            => entry is IEntryPhysicalPath physicalPath ? physicalPath.PhysicalPath : null;

    }

}
