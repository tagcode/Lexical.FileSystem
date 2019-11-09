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
    /// <summary>File system options for browse.</summary>
    [Operations(typeof(FileAttributeOptionOperations))]
    // <IFileAttributeOption>
    public interface IFileAttributeOption : IOption
    {
        /// <summary>Has SetFileAttribute capability.</summary>
        bool CanSetFileAttribute { get; }
    }
    // </IFileAttributeOption>

    // <doc>
    /// <summary>
    /// File system that set file attribute.
    /// </summary>
    public interface IFileSystemFileAttribute : IFileSystem, IFileAttributeOption
    {
        /// <summary>
        /// Set <paramref name="fileAttribute"/> on <paramref name="path"/>.
        /// </summary>
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
        void SetFileAttribute(string path, FileAttributes fileAttribute, IOption option = null);
    }
    // </doc>
}
