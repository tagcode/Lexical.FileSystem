// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------

using Lexical.FileSystem.Utility;
using System;
using System.IO;
using System.Security;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Extension methods for <see cref="IFileSystem"/>.
    /// </summary>
    public static partial class IFileSystemExtensions_
    {
        /// <summary>
        /// Try to move/rename a file or directory.
        /// 
        /// If <paramref name="oldPath"/> and <paramref name="newPath"/> refers to a directory, then the path names 
        /// should end with directory separator character '/'.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="oldPath">old path of a file or directory</param>
        /// <param name="newPath">new path of a file or directory</param>
        /// <exception cref="FileNotFoundException">The specified <paramref name="oldPath"/> is invalid.</exception>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="FileNotFoundException">The specified path is invalid.</exception>
        /// <exception cref="ArgumentNullException">path is null</exception>
        /// <exception cref="ArgumentException">path is an empty string (""), contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support renaming/moving files</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">path refers to non-file device, or an entry already exists at <paramref name="newPath"/></exception>
        /// <exception cref="ObjectDisposedException"/>
        public static void Move(this IFileSystem filesystem, string oldPath, string newPath)
        {
            // Move local
            if (filesystem is IFileSystemMove mover && mover.CanMoveLocal(oldPath, newPath)) mover.MoveLocal(oldPath, newPath);
            
            using(var s = new FileOperation.Session(FileOperation.Policy.FailIfExists| FileOperation.Policy.OmitAutoMounts))
            {
                FileOperation op = new FileOperation.MoveTree(s, filesystem, oldPath, filesystem, newPath);
                op.Estimate();
                try
                {
                    op.Run();
                } 
                // Rollback but let exception fly
                catch(Exception) when(Rollback(s, op)) { }
            }

            // Rollback
            bool Rollback(FileOperation.Session s, FileOperation op)
            {
                s.SetPolicy(FileOperation.Policy.SkipIfExists | FileOperation.Policy.OmitAutoMounts);
                FileOperation rollback = op.CreateRollback();
                if (rollback != null) rollback.Run();
                return false;
            }
        }
    }
}
