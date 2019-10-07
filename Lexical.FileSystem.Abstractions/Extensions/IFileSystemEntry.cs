// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;

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
        /// <returns></returns>
        public static IFileSystemOption Options(this IFileSystemEntry entry)
            => entry is IFileSystemEntryDirectory dir ? dir.Option : Lexical.FileSystem.FileSystemOptionNone.NoOptions;

        /// <summary>
        /// Tests if <paramref name="entry"/> represents a drive.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public static bool IsDrive(this IFileSystemEntry entry)
            => entry is IFileSystemEntryDrive drive ? drive.IsDrive : false;

        /// <summary>
        /// Tests if <paramref name="entry"/> represents a mount root.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public static bool IsMount(this IFileSystemEntry entry)
            => entry is IFileSystemEntryMount mount ? mount.IsMount : false;

        /// <summary>
        /// (optional) Mounted filesystem.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns>mounted filesystem or null</returns>
        //public static IFileSystem MountedFileSystem(this IFileSystemEntry entry)
            //=> entry is IFileSystemEntryMount mount ? mount.MountedFileSystem : null;

        /// <summary>
        /// (optional) Mount options. The object reference that was passed to Mount() method.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns>mount option or null</returns>
        //public static IFileSystemOption MountOption(this IFileSystemEntry entry)
            //=> entry is IFileSystemEntryMount mount ? mount.MountOption : null;


    }

}
