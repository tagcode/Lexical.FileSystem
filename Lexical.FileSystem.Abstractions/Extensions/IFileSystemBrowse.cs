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
        /// Test if <paramref name="filesystemOption"/> has Browse capability.
        /// <param name="filesystemOption"></param>
        /// </summary>
        /// <returns>true if has Browse capability</returns>
        public static bool CanBrowse(this IFileSystemOption filesystemOption)
            => filesystemOption.AsOption<IFileSystemOptionBrowse>() is IFileSystemOptionBrowse browser ? browser.CanBrowse : false;

        /// <summary>
        /// Test if <paramref name="filesystemOption"/> has Exists capability.
        /// <param name="filesystemOption"></param>
        /// </summary>
        /// <returns>true if has Exists capability</returns>
        public static bool CanGetEntry(this IFileSystemOption filesystemOption)
            => filesystemOption.AsOption<IFileSystemOptionBrowse>() is IFileSystemOptionBrowse browser ? browser.CanGetEntry : false;

        /// <summary>
        /// Browse a directory for child entries.
        /// 
        /// <paramref name="path"/> should end with directory separator character '/', for example "mydir/".
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path">path to a directory, "" is root, separator is "/"</param>
        /// <param name="token">(optional) filesystem implementation specific token, such as session, security token or credential. Used for authorizing or facilitating the action.</param>
        /// <returns>
        ///     Returns a snapshot of file and directory entries.
        ///     Note, that the returned array be internally cached by the implementation, and therefore the caller must not modify the array.
        /// </returns>
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
        public static IFileSystemEntry[] Browse(this IFileSystem filesystem, string path, IFileSystemToken token = null)
        {
            if (filesystem is IFileSystemBrowse browser) return browser.Browse(path, token);
            else throw new NotSupportedException(nameof(Browse));
        }

        /// <summary>
        /// Get entry of a single file or directory.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path">path to a directory or to a single file, "" is root, separator is "/"</param>
        /// <param name="token">(optional) filesystem implementation specific token, such as session, security token or credential. Used for authorizing or facilitating the action.</param>
        /// <returns>entry, or null if entry is not found</returns>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support exists</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        public static IFileSystemEntry GetEntry(this IFileSystem filesystem, string path, IFileSystemToken token = null)
        {
            if (filesystem is IFileSystemBrowse browser) return browser.GetEntry(path, token);
            else throw new NotSupportedException(nameof(GetEntry));
        }

        /// <summary>
        /// Tests if a file or directory exists.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path">path to a directory or to a single file, "" is root, separator is "/"</param>
        /// <param name="token">(optional) filesystem implementation specific token, such as session, security token or credential. Used for authorizing or facilitating the action.</param>
        /// <returns>true if exists</returns>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support exists</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        public static bool Exists(this IFileSystem filesystem, string path, IFileSystemToken token = null)
        {
            if (filesystem is IFileSystemBrowse browser) return browser.GetEntry(path, token) != null;
            else throw new NotSupportedException(nameof(GetEntry));
        }
    }

    /// <summary><see cref="IFileSystemOptionBrowse"/> operations.</summary>
    public class FileSystemOptionOperationBrowse : IFileSystemOptionOperationFlatten, IFileSystemOptionOperationIntersection, IFileSystemOptionOperationUnion
    {
        /// <summary>The option type that this class has operations for.</summary>
        public Type OptionType => typeof(IFileSystemOptionBrowse);
        /// <summary>Flatten to simpler instance.</summary>
        public IFileSystemOption Flatten(IFileSystemOption o) => o is IFileSystemOptionBrowse b ? o is FileSystemOptionBrowse ? /*already flattened*/o : /*new instance*/new FileSystemOptionBrowse(b.CanBrowse, b.CanGetEntry) : throw new InvalidCastException($"{typeof(IFileSystemOptionBrowse)} expected.");
        /// <summary>Intersection of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IFileSystemOption Intersection(IFileSystemOption o1, IFileSystemOption o2) => o1 is IFileSystemOptionBrowse b1 && o2 is IFileSystemOptionBrowse b2 ? new FileSystemOptionBrowse(b1.CanBrowse && b2.CanBrowse, b1.CanGetEntry && b2.CanGetEntry) : throw new InvalidCastException($"{typeof(IFileSystemOptionBrowse)} expected.");
        /// <summary>Union of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IFileSystemOption Union(IFileSystemOption o1, IFileSystemOption o2) => o1 is IFileSystemOptionBrowse b1 && o2 is IFileSystemOptionBrowse b2 ? new FileSystemOptionBrowse(b1.CanBrowse || b2.CanBrowse, b1.CanGetEntry || b2.CanGetEntry) : throw new InvalidCastException($"{typeof(IFileSystemOptionBrowse)} expected.");
    }

    /// <summary>File system options for browse.</summary>
    public class FileSystemOptionBrowse : IFileSystemOptionBrowse
    {
        /// <summary>Has Browse capability.</summary>
        public bool CanBrowse { get; protected set; }
        /// <summary>Has GetEntry capability.</summary>
        public bool CanGetEntry { get; protected set; }

        /// <summary>Create file system options for browse.</summary>
        public FileSystemOptionBrowse(bool canBrowse, bool canGetEntry)
        {
            CanBrowse = canBrowse;
            CanGetEntry = canGetEntry;
        }

        /// <inheritdoc/>
        public override string ToString() => (CanBrowse ? "CanBrowse" : "NoBrowse") + "," + (CanGetEntry ? "CanGetEntry" : "NoGetEntry");
    }

}
