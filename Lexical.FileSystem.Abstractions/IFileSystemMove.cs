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
    /// <summary>File system option for move/rename.</summary>
    [Operations(typeof(FileSystemOptionOperationMove))]
    // <IFileSystemOptionMove>
    public interface IFileSystemOptionMove : IFileSystemOption
    {
        /// <summary>Has Move capability.</summary>
        bool CanMove { get; }
    }
    // </IFileSystemOptionMove>

    // <doc>
    /// <summary>
    /// File system that can move/rename files and directories.
    /// </summary>
    public interface IFileSystemMove : IFileSystem, IFileSystemOptionMove
    {
        /// <summary>
        /// Try to move/rename a file or directory.
        /// 
        /// If <paramref name="oldPath"/> and <paramref name="newPath"/> refers to a directory, then the path names 
        /// should end with directory separator character '/'.
        /// </summary>
        /// <param name="oldPath">old path of a file or directory</param>
        /// <param name="newPath">new path of a file or directory</param>
        /// <exception cref="FileNotFoundException">The specified <paramref name="oldPath"/> is invalid.</exception>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException">path is null</exception>
        /// <exception cref="ArgumentException">path is an empty string (""), contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support renaming/moving files</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">path refers to non-file device, or an entry already exists at <paramref name="newPath"/></exception>
        /// <exception cref="ObjectDisposedException"/>
        void Move(string oldPath, string newPath);
    }
    // </doc>
}
