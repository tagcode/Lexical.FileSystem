// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.IO;
using System.Security;
using System.Threading.Tasks;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Extension methods for <see cref="IFileSystemCreateDirectory"/>.
    /// </summary>
    public static partial class FileSystemCreateDirectoryExtensions
    {
        /// <summary>
        /// Test if <paramref name="filesystemOption"/> has CreateDirectory capability.
        /// </summary>
        /// <param name="filesystemOption"></param>
        /// <param name="defaultValue">Returned value if option is unspecified</param>
        /// <returns>true, if has CreateDirectory capability</returns>
        public static bool CanCreateDirectory(this IOption filesystemOption, bool defaultValue = false)
            => filesystemOption.AsOption<ICreateDirectoryOption>() is ICreateDirectoryOption directoryConstructor ? directoryConstructor.CanCreateDirectory : defaultValue;

        /// <summary>
        /// Create a directory, or multiple cascading directories.
        /// 
        /// If directory at <paramref name="path"/> already exists, then returns without exception.
        /// 
        /// <paramref name="path"/> should end with directory separator character '/'.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path">Relative path to file. Directory separator is "/". The root is without preceding slash "", e.g. "dir/dir2"</param>
        /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
        /// <returns>true if directory exists after the method, false if directory doesn't exist</returns>
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
        public static void CreateDirectory(this IFileSystem filesystem, string path, IOption option = null)
        {
            if (filesystem is IFileSystemCreateDirectory directoryConstructor) directoryConstructor.CreateDirectory(path, option);
            else if (filesystem is IFileSystemCreateDirectoryAsync directoryConstructorAsync) directoryConstructorAsync.CreateDirectoryAsync(path, option).Wait();
            else throw new NotSupportedException(nameof(CreateDirectory));
        }

        /// <summary>
        /// Create a directory, or multiple cascading directories.
        /// 
        /// If directory at <paramref name="path"/> already exists, then returns without exception.
        /// 
        /// <paramref name="path"/> should end with directory separator character '/'.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path">Relative path to file. Directory separator is "/". The root is without preceding slash "", e.g. "dir/dir2"</param>
        /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
        /// <returns>true if directory exists after the method, false if directory doesn't exist</returns>
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
        public static Task CreateDirectoryTask(this IFileSystem filesystem, string path, IOption option = null)
        {
            if (filesystem is IFileSystemCreateDirectoryAsync directoryConstructorAsync) return directoryConstructorAsync.CreateDirectoryAsync(path, option);
            else if (filesystem is IFileSystemCreateDirectory directoryConstructor) return Task.Run(()=>directoryConstructor.CreateDirectory(path, option));
            else throw new NotSupportedException(nameof(CreateDirectory));
        }

    }

    /// <summary><see cref="ICreateDirectoryOption"/> operations.</summary>
    public class CreateDirectoryOptionOperations : IOptionFlattenOperation, IOptionIntersectionOperation, IOptionUnionOperation
    {
        /// <summary>The option type that this class has operations for.</summary>
        public Type OptionType => typeof(ICreateDirectoryOption);
        /// <summary>Flatten to simpler instance.</summary>
        public IOption Flatten(IOption o) => o is ICreateDirectoryOption c ? o is CreateDirectoryOption ? /*already flattened*/o : /*new instance*/new CreateDirectoryOption(c.CanCreateDirectory) : throw new InvalidCastException($"{typeof(ICreateDirectoryOption)} expected.");
        /// <summary>Intersection of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IOption Intersection(IOption o1, IOption o2) => o1 is ICreateDirectoryOption c1 && o2 is ICreateDirectoryOption c2 ? new CreateDirectoryOption(c1.CanCreateDirectory && c2.CanCreateDirectory) : throw new InvalidCastException($"{typeof(ICreateDirectoryOption)} expected.");
        /// <summary>Union of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IOption Union(IOption o1, IOption o2) => o1 is ICreateDirectoryOption c1 && o2 is ICreateDirectoryOption c2 ? new CreateDirectoryOption(c1.CanCreateDirectory || c2.CanCreateDirectory) : throw new InvalidCastException($"{typeof(ICreateDirectoryOption)} expected.");
    }

    /// <summary>File system option for creating directories.</summary>
    public class CreateDirectoryOption : ICreateDirectoryOption
    {
        internal static ICreateDirectoryOption createDirectory = new CreateDirectoryOption(true);
        internal static ICreateDirectoryOption noCreateDirectory = new CreateDirectoryOption(false);

        /// <summary>CreateDirectory allowed.</summary>
        public static IOption CreateDirectory => createDirectory;
        /// <summary>CreateDirectory not allowed.</summary>
        public static IOption NoCreateDirectory => noCreateDirectory;

        /// <summary>Has CreateDirectory capability.</summary>
        public bool CanCreateDirectory { get; protected set; }

        /// <summary>Create file system option for creating directories.</summary>
        public CreateDirectoryOption(bool canCreateDirectory)
        {
            CanCreateDirectory = canCreateDirectory;
        }

        /// <inheritdoc/>
        public override string ToString() => CanCreateDirectory ? "CanCreateDirectory" : "NoCreateDirectory";
    }
}
