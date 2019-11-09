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
        /// Test if <paramref name="filesystemOption"/> has SetFileAttribute capability.
        /// </summary>
        /// <param name="filesystemOption"></param>
        /// <param name="defaultValue">Returned value if option is unspecified</param>
        /// <returns>true if has SetFileAttribute capability</returns>
        public static bool CanSetFileAttribute(this IFileSystemOption filesystemOption, bool defaultValue = false)
            => filesystemOption.AsOption<IFileSystemOptionFileAttribute>() is IFileSystemOptionFileAttribute attributer ? attributer.CanSetFileAttribute : defaultValue;

        /// <summary>
        /// Set <paramref name="fileAttribute"/> on <paramref name="path"/>.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path"></param>
        /// <param name="fileAttribute"></param>
        /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
        /// <exception cref="FileNotFoundException"><paramref name="path"/> is not found</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid. For example, it's on an unmapped drive. Only thrown when setting the property value.</exception>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support browse</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"></exception>
        public static void SetFileAttribute(this IFileSystem filesystem, string path, FileAttributes fileAttribute, IFileSystemOption option = null)
        {
            if (filesystem is IFileSystemFileAttribute attributer) attributer.SetFileAttribute(path, fileAttribute, option);
            else throw new NotSupportedException(nameof(SetFileAttribute));
        }

    }

    /// <summary><see cref="IFileSystemOptionFileAttribute"/> operations.</summary>
    public class FileSystemOptionOperationFileAttribute : IFileSystemOptionOperationFlatten, IFileSystemOptionOperationIntersection, IFileSystemOptionOperationUnion
    {
        /// <summary>The option type that this class has operations for.</summary>
        public Type OptionType => typeof(IFileSystemOptionFileAttribute);
        /// <summary>Flatten to simpler instance.</summary>
        public IFileSystemOption Flatten(IFileSystemOption o) => o is IFileSystemOptionFileAttribute b ? o is FileSystemOptionFileAttribute ? /*already flattened*/o : /*new instance*/new FileSystemOptionFileAttribute(b.CanSetFileAttribute) : throw new InvalidCastException($"{typeof(IFileSystemOptionFileAttribute)} expected.");
        /// <summary>Intersection of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IFileSystemOption Intersection(IFileSystemOption o1, IFileSystemOption o2) => o1 is IFileSystemOptionFileAttribute b1 && o2 is IFileSystemOptionFileAttribute b2 ? new FileSystemOptionFileAttribute(b1.CanSetFileAttribute && b2.CanSetFileAttribute) : throw new InvalidCastException($"{typeof(IFileSystemOptionFileAttribute)} expected.");
        /// <summary>Union of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IFileSystemOption Union(IFileSystemOption o1, IFileSystemOption o2) => o1 is IFileSystemOptionFileAttribute b1 && o2 is IFileSystemOptionFileAttribute b2 ? new FileSystemOptionFileAttribute(b1.CanSetFileAttribute || b2.CanSetFileAttribute) : throw new InvalidCastException($"{typeof(IFileSystemOptionFileAttribute)} expected.");
    }

    /// <summary>File system options for browse.</summary>
    public class FileSystemOptionFileAttribute : IFileSystemOptionFileAttribute
    {
        /// <summary>Has SetFileAttribute capability.</summary>
        public bool CanSetFileAttribute { get; protected set; }

        /// <summary>Create file system options for browse.</summary>
        public FileSystemOptionFileAttribute(bool canSetFileAttribute)
        {
            this.CanSetFileAttribute = canSetFileAttribute;
        }

        /// <inheritdoc/>
        public override string ToString() => CanSetFileAttribute ? "CanSetFileAttribute" : "NoSetFileAttribute";
    }

}
