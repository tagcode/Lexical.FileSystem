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
        /// Test if <paramref name="filesystemOption"/> has Open capability.
        /// <param name="filesystemOption"></param>
        /// </summary>
        /// <returns>true, if has Open capability</returns>
        public static bool CanOpen(this IFileSystemOption filesystemOption)
            => filesystemOption.AsOption<IFileSystemOptionOpen>() is IFileSystemOptionOpen opener ? opener.CanOpen : false;

        /// <summary>
        /// Test if <paramref name="filesystemOption"/> has Read capability.
        /// <param name="filesystemOption"></param>
        /// </summary>
        /// <returns>true, if has Read capability</returns>
        public static bool CanRead(this IFileSystemOption filesystemOption)
            => filesystemOption.AsOption<IFileSystemOptionOpen>() is IFileSystemOptionOpen opener ? opener.CanRead : false;

        /// <summary>
        /// Test if <paramref name="filesystemOption"/> has Write capability.
        /// <param name="filesystemOption"></param>
        /// </summary>
        /// <returns>true, if has Write capability</returns>
        public static bool CanWrite(this IFileSystemOption filesystemOption)
            => filesystemOption.AsOption<IFileSystemOptionOpen>() is IFileSystemOptionOpen opener ? opener.CanWrite : false;

        /// <summary>
        /// Test if <paramref name="filesystemOption"/> has CreateFile capability.
        /// <param name="filesystemOption"></param>
        /// </summary>
        /// <returns>true, if has CreateFile capability</returns>
        public static bool CanCreateFile(this IFileSystemOption filesystemOption)
            => filesystemOption.AsOption<IFileSystemOptionOpen>() is IFileSystemOptionOpen opener ? opener.CanCreateFile : false;

        /// <summary>
        /// Create a new file. If file exists, does nothing.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path">Relative path to file. Directory separator is "/". The root is without preceding slash "", e.g. "dir/file"</param>
        /// <param name="initialData">(optional) initial data to write</param>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is an empty string (""), contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support create directory</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        /// <exception cref="FileSystemExceptionNoReadAccess">No read access</exception>
        /// <exception cref="FileSystemExceptionNoWriteAccess">No write access</exception>
        public static void CreateFile(this IFileSystem filesystem, string path, byte[] initialData = null)
        {
            if (filesystem is IFileSystemOpen opener)
            {
                using (Stream s = opener.Open(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
                {
                    if (initialData != null) s.Write(initialData, 0, initialData.Length);
                }
            }
            else throw new NotSupportedException(nameof(CreateDirectory));
        }

        /// <summary>
        /// Open a file for reading and/or writing. File can be created when <paramref name="fileMode"/> is <see cref="FileMode.Create"/> or <see cref="FileMode.CreateNew"/>.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path">Relative path to file. Directory separator is "/". Root is without preceding "/", e.g. "dir/file.xml"</param>
        /// <param name="fileMode">determines whether to open or to create the file</param>
        /// <param name="fileAccess">how to access the file, read, write or read and write</param>
        /// <param name="fileShare">how the file will be shared by processes</param>
        /// <returns>open file stream</returns>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is an empty string (""), contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support opening files</exception>
        /// <exception cref="FileNotFoundException">The file cannot be found, such as when mode is FileMode.Truncate or FileMode.Open, and and the file specified by path does not exist. The file must already exist in these modes.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="fileMode"/>, <paramref name="fileAccess"/> or <paramref name="fileShare"/> contains an invalid value.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        /// <exception cref="FileSystemExceptionNoReadAccess">No read access</exception>
        /// <exception cref="FileSystemExceptionNoWriteAccess">No write access</exception>
        public static Stream Open(this IFileSystem filesystem, string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
        {
            if (filesystem is IFileSystemOpen opener) return opener.Open(path, fileMode, fileAccess, fileShare);
            throw new NotSupportedException(nameof(Open));
        }
    }

    /// <summary><see cref="IFileSystemOptionOpen"/> operations.</summary>
    public class FileSystemOptionOperationOpen : IFileSystemOptionOperationFlatten, IFileSystemOptionOperationIntersection, IFileSystemOptionOperationUnion
    {
        /// <summary>The option type that this class has operations for.</summary>
        public Type OptionType => typeof(IFileSystemOptionOpen);
        /// <summary>Flatten to simpler instance.</summary>
        public IFileSystemOption Flatten(IFileSystemOption o) => o is IFileSystemOptionOpen c ? o is FileSystemOptionOpen ? /*already flattened*/o : /*new instance*/new FileSystemOptionOpen(c.CanOpen, c.CanRead, c.CanWrite, c.CanCreateFile) : throw new InvalidCastException($"{typeof(IFileSystemOptionOpen)} expected.");
        /// <summary>Intersection of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IFileSystemOption Intersection(IFileSystemOption o1, IFileSystemOption o2) => o1 is IFileSystemOptionOpen c1 && o2 is IFileSystemOptionOpen c2 ? new FileSystemOptionOpen(c1.CanOpen && c2.CanOpen, c1.CanRead && c2.CanRead, c1.CanWrite && c2.CanWrite, c1.CanCreateFile && c2.CanCreateFile) : throw new InvalidCastException($"{typeof(IFileSystemOptionOpen)} expected.");
        /// <summary>Union of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IFileSystemOption Union(IFileSystemOption o1, IFileSystemOption o2) => o1 is IFileSystemOptionOpen c1 && o2 is IFileSystemOptionOpen c2 ? new FileSystemOptionOpen(c1.CanOpen || c2.CanOpen, c1.CanRead && c2.CanRead, c1.CanWrite && c2.CanWrite, c1.CanCreateFile && c2.CanCreateFile) : throw new InvalidCastException($"{typeof(IFileSystemOptionOpen)} expected.");
    }

    /// <summary>File system options for open, create, read and write files.</summary>
    public class FileSystemOptionOpen : IFileSystemOptionOpen
    {
        /// <summary>Can open file</summary>
        public bool CanOpen { get; protected set; }
        /// <summary>Can open file for reading(</summary>
        public bool CanRead { get; protected set; }
        /// <summary>Can open file for writing.</summary>
        public bool CanWrite { get; protected set; }
        /// <summary>Can open and create file.</summary>
        public bool CanCreateFile { get; protected set; }

        /// <summary>Create file system options for open, create, read and write files.</summary>
        public FileSystemOptionOpen(bool canOpen, bool canRead, bool canWrite, bool canCreateFile)
        {
            CanOpen = canOpen;
            CanRead = canRead;
            CanWrite = canWrite;
            CanCreateFile = canCreateFile;
        }

        /// <inheritdoc/>
        public override string ToString() => (CanOpen ? "CanOpen" : "NoOpen") + "," + (CanRead ? "CanRead" : "NoRead") + "," + (CanWrite ? "CanWrite" : "NoWrite") + "," + (CanCreateFile ? "CanCreateFile" : "NoCreateFile");
    }
}
