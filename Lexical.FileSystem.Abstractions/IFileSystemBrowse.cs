// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.IO;
using System.Security;

namespace Lexical.FileSystem
{
    // <doc>
    /// <summary>
    /// File system that can browse directories.
    /// </summary>
    public interface IFileSystemBrowse : IFileSystem
    {
        /// <summary>
        /// Has Browse capability.
        /// </summary>
        bool CanBrowse { get; }

        /// <summary>
        /// Has Exists capability.
        /// </summary>
        bool CanTestExists { get; }

        /// <summary>
        /// Browse a directory for file and subdirectory entries.
        /// </summary>
        /// <param name="path">path to a directory or to a single file, "" is root, separator is "/"</param>
        /// <returns>a snapshot of file and directory entries</returns>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support browse</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        FileSystemEntry[] Browse(string path);

        /// <summary>
        /// Tests whether a file or directory exists.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support exists</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        bool Exists(string path);
    }

    /// <summary>
    /// File entry used by <see cref="IFileSystem"/>.
    /// The entry represents the snapshot state at the time of creation.
    /// </summary>
    public struct FileSystemEntry
    {
        /// <summary>
        /// (optional) Associated file system.
        /// </summary>
        public IFileSystem FileSystem;

        /// <summary>
        /// File entry type.
        /// </summary>
        public FileSystemEntryType Type;

        /// <summary>
        /// Path that is relative to the <see cref="IFileSystem"/>.
        /// Separator is "/".
        /// </summary>
        public string Path;

        /// <summary>
        /// File length. -1 if is folder or length is unknown.
        /// </summary>
        public long Length;

        /// <summary>
        /// Entry name without path.
        /// </summary>
        public string Name;

        /// <summary>
        /// Date time of last modification.
        /// </summary>
        public DateTimeOffset LastModified;

        /// <summary>
        /// Print info (path).
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => Path;
    }

    /// <summary>
    /// <see cref="FileSystemEntry"/> type.
    /// </summary>
    public enum FileSystemEntryType : Int32
    {
        /// <summary>Entry is file</summary>
        File = 1,
        /// <summary>Entry is directory</summary>
        Directory = 2
    }    
    // </doc>

    /// <summary>
    /// Extension methods for <see cref="IFileSystem"/>.
    /// </summary>
    public static partial class IFileSystemExtensions
    {
        /// <summary>
        /// Test if <paramref name="fileSystem"/> has Browse capability.
        /// <param name="fileSystem"></param>
        /// </summary>
        /// <returns>true if has Browse capability</returns>
        public static bool CanBrowse(this IFileSystem fileSystem)
            => fileSystem is IFileSystemBrowse browser ? browser.CanBrowse : false;

        /// <summary>
        /// Test if <paramref name="fileSystem"/> has Exists capability.
        /// <param name="fileSystem"></param>
        /// </summary>
        /// <returns>true if has Exists capability</returns>
        public static bool CanTestExists(this IFileSystem fileSystem)
            => fileSystem is IFileSystemBrowse browser ? browser.CanTestExists : false;

        /// <summary>
        /// Browse a directory for file and subdirectory entries.
        /// </summary>
        /// <param name="fileSystem"></param>
        /// <param name="path">path to a directory or to a single file, "" is root, separator is "/"</param>
        /// <returns>a snapshot of file and directory entries</returns>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support browse</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        public static FileSystemEntry[] Browse(this IFileSystem fileSystem, string path)
        {
            if (fileSystem is IFileSystemBrowse browser) return browser.Browse(path);
            else throw new NotSupportedException(nameof(Browse));
        }


        /// <summary>
        /// Tests whether a file or directory exists.
        /// </summary>
        /// <param name="fileSystem"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support exists</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        public static bool Exists(this IFileSystem fileSystem, string path)
        {
            if (fileSystem is IFileSystemBrowse browser) return browser.Exists(path);
            else throw new NotSupportedException(nameof(Browse));
        }

    }

}
