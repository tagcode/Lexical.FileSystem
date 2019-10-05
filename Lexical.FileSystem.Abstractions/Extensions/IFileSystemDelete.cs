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
    /// <summary>
    /// Extension methods for <see cref="IFileSystem"/>.
    /// </summary>
    public static partial class IFileSystemExtensions
    {
        /// <summary>
        /// Test if <paramref name="filesystemOption"/> has Delete capability.
        /// <param name="filesystemOption"></param>
        /// </summary>
        /// <returns>true, if has Delete capability</returns>
        public static bool CanDelete(this IFileSystemOption filesystemOption)
            => filesystemOption.As<IFileSystemOptionDelete>() is IFileSystemOptionDelete deleter ? deleter.CanDelete : false;

        /// <summary>
        /// Delete a file or directory.
        /// 
        /// If <paramref name="path"/> is directory, then it should end with directory separator character '/', for example "dir/".
        /// 
        /// If <paramref name="recurse"/> is false and <paramref name="path"/> is a directory that is not empty, then <see cref="IOException"/> is thrown.
        /// If <paramref name="recurse"/> is true, then any file or directory in <paramref name="path"/> is deleted as well.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path">path to a file or directory</param>
        /// <param name="recurse">if path refers to directory, recurse into sub directories</param>
        /// <exception cref="FileNotFoundException">The specified path is invalid.</exception>
        /// <exception cref="IOException">On unexpected IO error, or if <paramref name="path"/> refered to a directory that wasn't empty and <paramref name="recurse"/> is false, or trying to delete root when not allowed</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> contains invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support deleting files</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="path"/> refers to non-file device</exception>
        /// <exception cref="ObjectDisposedException"/>
        public static void Delete(this IFileSystem filesystem, string path, bool recurse = false)
        {
            if (filesystem is IFileSystemDelete deleter) deleter.Delete(path, recurse);
            else throw new NotSupportedException(nameof(Delete));
        }
    }

    /// <summary><see cref="IFileSystemOptionDelete"/> operations.</summary>
    public class FileSystemOptionOperationDelete : IFileSystemOptionOperationFlatten, IFileSystemOptionOperationIntersection, IFileSystemOptionOperationUnion
    {
        /// <summary>The option type that this class has operations for.</summary>
        public Type OptionType => typeof(IFileSystemOptionDelete);
        /// <summary>Flatten to simpler instance.</summary>
        public IFileSystemOption Flatten(IFileSystemOption o) => o is IFileSystemOptionDelete c ? o is FileSystemOptionDelete ? /*already flattened*/o : /*new instance*/new FileSystemOptionDelete(c.CanDelete) : throw new InvalidCastException($"{typeof(IFileSystemOptionDelete)} expected.");
        /// <summary>Intersection of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IFileSystemOption Intersection(IFileSystemOption o1, IFileSystemOption o2) => o1 is IFileSystemOptionDelete c1 && o2 is IFileSystemOptionDelete c2 ? new FileSystemOptionDelete(c1.CanDelete && c2.CanDelete) : throw new InvalidCastException($"{typeof(IFileSystemOptionDelete)} expected.");
        /// <summary>Union of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IFileSystemOption Union(IFileSystemOption o1, IFileSystemOption o2) => o1 is IFileSystemOptionDelete c1 && o2 is IFileSystemOptionDelete c2 ? new FileSystemOptionDelete(c1.CanDelete || c2.CanDelete) : throw new InvalidCastException($"{typeof(IFileSystemOptionDelete)} expected.");
    }

    /// <summary>File system option for deleting files and directories.</summary>
    public class FileSystemOptionDelete : IFileSystemOptionDelete
    {
        /// <summary>Has Delete capability.</summary>
        public bool CanDelete { get; protected set; }

        /// <summary>Create file system option for deleting files and directories.</summary>
        public FileSystemOptionDelete(bool canDelete)
        {
            CanDelete = canDelete;
        }
    }
}
