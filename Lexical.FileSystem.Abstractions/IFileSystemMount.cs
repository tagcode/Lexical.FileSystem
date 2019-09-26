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
        /// Is filesystem capable of creating mountpoints.
        /// </summary>
        bool CanCreateMountpoint { get; }

        /// <summary>
        /// Is filesystem capable of listing mountpoints.
        /// </summary>
        bool CanListMountpoints { get; }

        /// <summary>
        /// Is filesystem capable of getting mountpoint entry.
        /// </summary>
        bool CanGetMountpoint { get; }

        /// <summary>
        /// Create a mountpoint where other file systems can be mounted.
        /// 
        /// If mountpoint already exists, returns another handle to same mountpoint. Both handles must be disposed separately.
        /// </summary>
        /// <param name="path">path to the mountpoint</param>
        /// <returns>a handle to mountpoint</returns>
        /// <exception cref="NotSupportedException">If mountpoint creation is not supported</exception>
        /// <exception cref="IOException">If creation failed.</exception>
        IFileSystemMountpoint CreateMountpoint(string path);

        /// <summary>
        /// List all mountpoints.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">If mountpoint creation is not supported</exception>
        IFileSystemMountpoint[] ListMountpoints();

        /// <summary>
        /// Get handle mountpoint at <paramref name="path"/>.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>mountpoint or null if doesn't exist</returns>
        IFileSystemMountpoint GetMountpoint(string path);
    }

    /// <summary>
    /// Mount point
    /// </summary>
    public interface IFileSystemMountpoint : IDisposable
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
        /// Mount <paramref name="filesystem"/> at <paramref name="subpath"/> to the mountpoint.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="subpath"></param>
        /// <returns></returns>
        IFileSystemMountHandle Mount(IFileSystem filesystem, string subpath);
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
        /// Is filesystem capable of creating mountpoints.
        /// </summary>
        /// <returns></returns>
        public static bool CanCreateMountpoint(this IFileSystem filesystem)
            => filesystem is IFileSystemMount mountable ? mountable.CanCreateMountpoint : false;

        /// <summary>
        /// Is filesystem capable of listing mountpoints.
        /// </summary>
        /// <returns></returns>
        public static bool CanListMountpoints(this IFileSystem filesystem)
            => filesystem is IFileSystemMount mountable ? mountable.CanListMountpoints : false;

        /// <summary>
        /// Is filesystem capable of getting mountpoint entry.
        /// </summary>
        /// <returns></returns>
        public static bool CanGetMountpoint(this IFileSystem filesystem)
            => filesystem is IFileSystemMount mountable ? mountable.CanGetMountpoint : false;

        /// <summary>
        /// Create a mountpoint where other file systems can be mounted.
        /// 
        /// If mountpoint already exists, returns another handle to same mountpoint. Both handles must be disposed separately.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path">path to the mountpoint</param>
        /// <returns>a handle to mountpoint</returns>
        /// <exception cref="NotSupportedException">If mountpoint creation is not supported</exception>
        /// <exception cref="IOException">If creation failed.</exception>
        public static IFileSystemMountpoint CreateMountpoint(this IFileSystem filesystem, string path)
        {
            if (filesystem is IFileSystemMount mountable) return mountable.CreateMountpoint(path);
            throw new NotSupportedException();
        }

        /// <summary>
        /// List all mountpoints.
        /// </summary>
        /// <returns></returns>
        /// <param name="filesystem"></param>
        /// <exception cref="NotSupportedException">If mountpoint creation is not supported</exception>
        public static IFileSystemMountpoint[] ListMountpoints(this IFileSystem filesystem)
        {
            if (filesystem is IFileSystemMount mountable) return mountable.ListMountpoints();
            throw new NotSupportedException();
        }

        /// <summary>
        /// Get handle mountpoint at <paramref name="path"/>.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path"></param>
        /// <returns>mountpoint or null if doesn't exist</returns>
        public static IFileSystemMountpoint GetMountpoint(this IFileSystem filesystem, string path)
        {
            if (filesystem is IFileSystemMount mountable) return mountable.GetMountpoint(path);
            throw new NotSupportedException();
        }
    }

}
