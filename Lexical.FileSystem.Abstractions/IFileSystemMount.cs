// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           12.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------

using System;
using System.IO;

namespace Lexical.FileSystem
{
    // <doc>
    /// <summary>
    /// File system that is actually a decoration.
    /// </summary>
    public interface IFileSystemMount : IFileSystem
    {
        /// <summary>
        /// Create a mount point where other file systems can be mounted.
        /// 
        /// If mount point already exists, returns another handle to same mount point. Both handles must be disposed separately.
        /// </summary>
        /// <param name="path">path to the mount point</param>
        /// <returns>a handle to mount point</returns>
        /// <exception cref="NotSupportedException">If mount point creation is not supported</exception>
        /// <exception cref="IOException">If creation failed.</exception>
        IFileSystemMountPoint CreateMountPoint(string path);
    }

    /// <summary>
    /// Mount point
    /// </summary>
    public interface IFileSystemMountPoint : IDisposable
    {
        /// <summary>
        /// The file system this mountpoint is part of.
        /// </summary>
        IFileSystem FileSystem { get; }

        /// <summary>
        /// Mount point path in <see cref="FileSystem"/>.
        /// </summary>
        String Path { get; }

        /// <summary>
        /// Mount <paramref name="fileSystem"/> at <paramref name="subpath"/> to the mountpoint.
        /// </summary>
        /// <param name="fileSystem"></param>
        /// <param name="subpath"></param>
        /// <returns></returns>
        IFileSystemMountHandle Mount(IFileSystem fileSystem, string subpath);
    }

    /// <summary>
    /// Mount handle. Dispose the handle to unmount.
    /// </summary>
    public interface IFileSystemMountHandle : IDisposable
    {
    }
    // </doc>

    /// <summary>
    /// Extension methods for <see cref="IFileSystem"/>.
    /// </summary>
    public static partial class IFileSystemExtensions
    {
        /// <summary>
        /// Create a mount point where other file systems can be mounted.
        /// 
        /// If mount point already exists, returns another handle to same mount point. Both handles must be disposed separately.
        /// </summary>
        /// <param name="fileSystem"></param>
        /// <param name="path">path to the mount point</param>
        /// <returns>a handle to mount point</returns>
        /// <exception cref="NotSupportedException">If mount point creation is not supported</exception>
        /// <exception cref="IOException">If creation failed.</exception>
        public static IFileSystemMountPoint CreateMountPoint(this IFileSystem fileSystem, string path)
        {
            if (fileSystem is IFileSystemMount mountable) return mountable.CreateMountPoint(path);
            throw new NotSupportedException();
        }
    }

}
