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
    /// Extension methods for <see cref="IFileSystemDelete"/>.
    /// </summary>
    public static partial class FileSystemDeleteExtensions
    {
        /// <summary>
        /// Test if <paramref name="filesystemOption"/> has Delete capability.
        /// </summary>
        /// <param name="filesystemOption"></param>
        /// <param name="defaultValue">Returned value if option is unspecified</param>
        /// <returns>true, if has Delete capability</returns>
        public static bool CanDelete(this IOption filesystemOption, bool defaultValue = false)
            => filesystemOption.AsOption<IDeleteOption>() is IDeleteOption deleter ? deleter.CanDelete : defaultValue;

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
        /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
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
        public static void Delete(this IFileSystem filesystem, string path, bool recurse = false, IOption option = null)
        {
            if (filesystem is IFileSystemDelete deleter) deleter.Delete(path, recurse, option);
            else throw new NotSupportedException(nameof(Delete));
        }
    }

    /// <summary><see cref="IDeleteOption"/> operations.</summary>
    public class DeleteOptionOperations : IOptionFlattenOperation, IOptionIntersectionOperation, IOptionUnionOperation
    {
        /// <summary>The option type that this class has operations for.</summary>
        public Type OptionType => typeof(IDeleteOption);
        /// <summary>Flatten to simpler instance.</summary>
        public IOption Flatten(IOption o) => o is IDeleteOption c ? o is DeleteOption ? /*already flattened*/o : /*new instance*/new DeleteOption(c.CanDelete) : throw new InvalidCastException($"{typeof(IDeleteOption)} expected.");
        /// <summary>Intersection of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IOption Intersection(IOption o1, IOption o2) => o1 is IDeleteOption c1 && o2 is IDeleteOption c2 ? new DeleteOption(c1.CanDelete && c2.CanDelete) : throw new InvalidCastException($"{typeof(IDeleteOption)} expected.");
        /// <summary>Union of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IOption Union(IOption o1, IOption o2) => o1 is IDeleteOption c1 && o2 is IDeleteOption c2 ? new DeleteOption(c1.CanDelete || c2.CanDelete) : throw new InvalidCastException($"{typeof(IDeleteOption)} expected.");
    }

    /// <summary>File system option for deleting files and directories.</summary>
    public class DeleteOption : IDeleteOption
    {
        internal static IDeleteOption delete = new DeleteOption(true);
        internal static IDeleteOption noDelete = new DeleteOption(false);

        /// <summary>Delete allowed.</summary>
        public static IOption Delete => delete;
        /// <summary>Delete not allowed.</summary>
        public static IOption NoDelete => noDelete;

        /// <summary>Has Delete capability.</summary>
        public bool CanDelete { get; protected set; }

        /// <summary>Create file system option for deleting files and directories.</summary>
        public DeleteOption(bool canDelete)
        {
            CanDelete = canDelete;
        }

        /// <inheritdoc/>
        public override string ToString() => CanDelete ? "CanDelete" : "NoDelete";
    }
}
