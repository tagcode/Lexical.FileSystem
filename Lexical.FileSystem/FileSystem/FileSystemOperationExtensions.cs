﻿// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Operation;
using System;
using System.IO;
using System.Security;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Extension methods for <see cref="IFileSystem"/>.
    /// </summary>
    public static partial class FileSystemOperationExtensions
    {
        /// <summary>
        /// Transfer a file or directory by copy and delete.
        /// 
        /// If <paramref name="srcPath"/> and <paramref name="dstPath"/> refers to a directory, then the path names 
        /// should end with directory separator character '/'.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="srcPath">old path of a file or directory</param>
        /// <param name="dstFileSystem">filesystem to copy to</param>
        /// <param name="dstPath">new path of a file or directory</param>
        /// <param name="srcOption">(optional)</param>
        /// <param name="dstOption">(optional)</param>
        /// <exception cref="FileNotFoundException">The specified <paramref name="srcPath"/> is invalid.</exception>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="FileNotFoundException">The specified path is invalid.</exception>
        /// <exception cref="ArgumentNullException">path is null</exception>
        /// <exception cref="ArgumentException">path is an empty string (""), contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support copy and delete of files</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">path refers to non-file device, or an entry already exists at <paramref name="dstPath"/></exception>
        /// <exception cref="ObjectDisposedException"/>
        public static void Transfer(this IFileSystem filesystem, string srcPath, IFileSystem dstFileSystem, string dstPath, IOption srcOption = null, IOption dstOption = null)
        {
            using (var s = new OperationSession())
            {
                OperationBase op = new TransferTree(s, filesystem, srcPath, dstFileSystem, dstPath, 
                    srcOption, dstOption,
                    policy: OperationPolicy.SrcThrow | OperationPolicy.DstThrow | OperationPolicy.OmitMountedPackages | OperationPolicy.CancelOnError
                );
                op.Estimate();
                op.Run(rollbackOnError: true);
                op.AssertSuccessful();
            }
        }

        /// <summary>
        /// Copy a file.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="srcPath">source path of a file or directory</param>
        /// <param name="dstPath">target path of a file or directory</param>
        /// <param name="option">(optional) token to authorize or facilitate operation</param>
        /// <exception cref="FileNotFoundException">The specified <paramref name="srcPath"/> is invalid.</exception>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="FileNotFoundException">The specified path is invalid.</exception>
        /// <exception cref="ArgumentNullException">path is null</exception>
        /// <exception cref="ArgumentException">path is an empty string (""), contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support copy and delete of files</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">path refers to non-file device, or an entry already exists at <paramref name="dstPath"/></exception>
        /// <exception cref="ObjectDisposedException"/>
        public static void CopyFile(this IFileSystem filesystem, string srcPath, string dstPath, IOption option = null)
        {
            using (var s = new OperationSession())
            {
                OperationBase op = new CopyFile(s, filesystem, srcPath, filesystem, dstPath, option, option,
                    policy: OperationPolicy.SrcThrow | OperationPolicy.DstThrow | OperationPolicy.OmitMountedPackages | OperationPolicy.CancelOnError
                );
                op.Estimate();
                op.Run(rollbackOnError: true);
                op.AssertSuccessful();
            }
        }

        /// <summary>
        /// Copy a file.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="srcPath">source path of a file or directory</param>
        /// <param name="dstFileSystem">filesystem to copy to</param>
        /// <param name="dstPath">target path of a file or directory</param>
        /// <param name="srcOption">(optional)</param>
        /// <param name="dstOption">(optional)</param>
        /// <exception cref="FileNotFoundException">The specified <paramref name="srcPath"/> is invalid.</exception>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="FileNotFoundException">The specified path is invalid.</exception>
        /// <exception cref="ArgumentNullException">path is null</exception>
        /// <exception cref="ArgumentException">path is an empty string (""), contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support copy and delete of files</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">path refers to non-file device, or an entry already exists at <paramref name="dstPath"/></exception>
        /// <exception cref="ObjectDisposedException"/>
        public static void CopyFile(this IFileSystem filesystem, string srcPath, IFileSystem dstFileSystem, string dstPath, IOption srcOption = null, IOption dstOption = null)
        {
            using (var s = new OperationSession())
            {
                OperationBase op = new CopyFile(s, filesystem, srcPath, dstFileSystem, dstPath, srcOption, dstOption,
                    policy: OperationPolicy.SrcThrow | OperationPolicy.DstThrow | OperationPolicy.OmitMountedPackages | OperationPolicy.CancelOnError
                );
                op.Estimate();
                op.Run(rollbackOnError: true);
                op.AssertSuccessful();
            }
        }

        /// <summary>
        /// Copy a file or directory tree.
        /// 
        /// If <paramref name="srcPath"/> and <paramref name="dstPath"/> refers to a directory, then the path names 
        /// should end with directory separator character '/'.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="srcPath">source path of a file or directory</param>
        /// <param name="dstPath">target path of a file or directory</param>
        /// <param name="option">(optional) token to authorize or facilitate operation</param>
        /// <exception cref="FileNotFoundException">The specified <paramref name="srcPath"/> is invalid.</exception>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="FileNotFoundException">The specified path is invalid.</exception>
        /// <exception cref="ArgumentNullException">path is null</exception>
        /// <exception cref="ArgumentException">path is an empty string (""), contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support copy and delete of files</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">path refers to non-file device, or an entry already exists at <paramref name="dstPath"/></exception>
        /// <exception cref="ObjectDisposedException"/>
        public static void CopyTree(this IFileSystem filesystem, string srcPath, string dstPath, IOption option = null)
        {
            using (var s = new OperationSession())
            {
                OperationBase op = new CopyTree(s, filesystem, srcPath, filesystem, dstPath, option, option,
                    policy: OperationPolicy.SrcThrow | OperationPolicy.DstThrow | OperationPolicy.OmitMountedPackages | OperationPolicy.CancelOnError
                );
                op.Estimate();
                op.Run(rollbackOnError: true);
                op.AssertSuccessful();
            }
        }

        /// <summary>
        /// Copy a file or directory tree.
        /// 
        /// If <paramref name="srcPath"/> and <paramref name="dstPath"/> refers to a directory, then the path names 
        /// should end with directory separator character '/'.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="srcPath">source path of a file or directory</param>
        /// <param name="dstFileSystem">filesystem to copy to</param>
        /// <param name="dstPath">target path of a file or directory</param>
        /// <param name="srcOption">(optional)</param>
        /// <param name="dstOption">(optional)</param>
        /// <exception cref="FileNotFoundException">The specified <paramref name="srcPath"/> is invalid.</exception>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="FileNotFoundException">The specified path is invalid.</exception>
        /// <exception cref="ArgumentNullException">path is null</exception>
        /// <exception cref="ArgumentException">path is an empty string (""), contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support copy and delete of files</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">path refers to non-file device, or an entry already exists at <paramref name="dstPath"/></exception>
        /// <exception cref="ObjectDisposedException"/>
        public static void CopyTree(this IFileSystem filesystem, string srcPath, IFileSystem dstFileSystem, string dstPath, IOption srcOption = null, IOption dstOption = null)
        {
            using (var s = new OperationSession())
            {
                OperationBase op = new CopyTree(s, filesystem, srcPath, dstFileSystem, dstPath, srcOption, dstOption,
                    policy: OperationPolicy.SrcThrow | OperationPolicy.DstThrow | OperationPolicy.OmitMountedPackages | OperationPolicy.CancelOnError
                );
                op.Estimate();
                op.Run(rollbackOnError: true);
                op.AssertSuccessful();
            }
        }

    }
}
