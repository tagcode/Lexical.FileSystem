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
        /// </summary>
        /// <param name="filesystemOption"></param>
        /// <param name="defaultValue">Returned value if option is unspecified</param>
        /// <returns>true, if has Open capability. If unspecified, the default value is false.</returns>
        public static bool CanOpen(this IOption filesystemOption, bool defaultValue = false)
            => filesystemOption.AsOption<IOpenOption>() is IOpenOption opener ? opener.CanOpen : defaultValue;

        /// <summary>
        /// Test if <paramref name="filesystemOption"/> has Read capability.
        /// </summary>
        /// <param name="filesystemOption"></param>
        /// <param name="defaultValue">Returned value if option is unspecified</param>
        /// <returns>true, if has Read capability</returns>
        public static bool CanRead(this IOption filesystemOption, bool defaultValue = false)
            => filesystemOption.AsOption<IOpenOption>() is IOpenOption opener ? opener.CanRead : defaultValue;

        /// <summary>
        /// Test if <paramref name="filesystemOption"/> has Write capability.
        /// </summary>
        /// <param name="filesystemOption"></param>
        /// <param name="defaultValue">Returned value if option is unspecified</param>
        /// <returns>true, if has Write capability</returns>
        public static bool CanWrite(this IOption filesystemOption, bool defaultValue = false)
            => filesystemOption.AsOption<IOpenOption>() is IOpenOption opener ? opener.CanWrite : defaultValue;

        /// <summary>
        /// Test if <paramref name="filesystemOption"/> has CreateFile capability.
        /// </summary>
        /// <param name="filesystemOption"></param>
        /// <param name="defaultValue">Returned value if option is unspecified</param>
        /// <returns>true, if has CreateFile capability</returns>
        public static bool CanCreateFile(this IOption filesystemOption, bool defaultValue = false)
            => filesystemOption.AsOption<IOpenOption>() is IOpenOption opener ? opener.CanCreateFile : defaultValue;

        /// <summary>
        /// Create a new file. If file exists, does nothing.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path">Relative path to file. Directory separator is "/". The root is without preceding slash "", e.g. "dir/file"</param>
        /// <param name="initialData">(optional) initial data to write</param>
        /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
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
        public static void CreateFile(this IFileSystem filesystem, string path, byte[] initialData = null, IOption option = null)
        {
            if (filesystem is IFileSystemOpen opener)
            {
                using (Stream s = opener.Open(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite, option))
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
        /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
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
        public static Stream Open(this IFileSystem filesystem, string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare, IOption option = null)
        {
            if (filesystem is IFileSystemOpen opener) return opener.Open(path, fileMode, fileAccess, fileShare, option);
            throw new NotSupportedException(nameof(Open));
        }
    }

    /// <summary><see cref="IOpenOption"/> operations.</summary>
    public class OpenOptionOperations : IOptionFlattenOperation, IOptionIntersectionOperation, IOptionUnionOperation
    {
        /// <summary>The option type that this class has operations for.</summary>
        public Type OptionType => typeof(IOpenOption);
        /// <summary>Flatten to simpler instance.</summary>
        public IOption Flatten(IOption o) => o is IOpenOption c ? o is OpenOption ? /*already flattened*/o : /*new instance*/new OpenOption(c.CanOpen, c.CanRead, c.CanWrite, c.CanCreateFile) : throw new InvalidCastException($"{typeof(IOpenOption)} expected.");
        /// <summary>Intersection of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IOption Intersection(IOption o1, IOption o2) => o1 is IOpenOption c1 && o2 is IOpenOption c2 ? new OpenOption(c1.CanOpen && c2.CanOpen, c1.CanRead && c2.CanRead, c1.CanWrite && c2.CanWrite, c1.CanCreateFile && c2.CanCreateFile) : throw new InvalidCastException($"{typeof(IOpenOption)} expected.");
        /// <summary>Union of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IOption Union(IOption o1, IOption o2) => o1 is IOpenOption c1 && o2 is IOpenOption c2 ? new OpenOption(c1.CanOpen || c2.CanOpen, c1.CanRead && c2.CanRead, c1.CanWrite && c2.CanWrite, c1.CanCreateFile && c2.CanCreateFile) : throw new InvalidCastException($"{typeof(IOpenOption)} expected.");
    }

    /// <summary>File system options for open, create, read and write files.</summary>
    public class OpenOption : IOpenOption
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
        public OpenOption(bool canOpen, bool canRead, bool canWrite, bool canCreateFile)
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
