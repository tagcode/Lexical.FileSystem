// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           12.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;

namespace Lexical.FileSystem
{
    // <doc>
    /// <summary>File system option for mount capabilities. Used with <see cref="IFileSystemMount"/>.</summary>
    [Operations(typeof(FileSystemOptionOperationMount))]
    public interface IFileSystemOptionMount : IFileSystemOption
    {
        /// <summary>Can filesystem mount other filesystems.</summary>
        bool CanMount { get; }
        /// <summary>Is filesystem allowed to unmount a mount.</summary>
        bool CanUnmount { get; }
        /// <summary>Is filesystem allowed to list mounts.</summary>
        bool CanListMounts { get; }
    }

    /// <summary>
    /// FileSystem that can mount other filesystems into its directory tree.
    /// </summary>
    public interface IFileSystemMount : IFileSystem, IFileSystemOptionMount
    {
        /// <summary>
        /// Mount <paramref name="filesystem"/> at <paramref name="path"/> in the parent filesystem.
        /// 
        /// If <paramref name="path"/> is already mounted, then replaces previous mount.
        /// If there is an open stream to previously mounted filesystem, that stream is unlinked from the filesystem.
        /// 
        /// If <paramref name="filesystem"/> is null, then an empty directory is created into the parent filesystem.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="filesystem">(optional) filesystem to be mounted</param>
        /// <param name="mountOption">(optional) mount options</param>
        /// <returns>this (parent filesystem)</returns>
        /// <exception cref="NotSupportedException">If operation is not supported</exception>
        IFileSystem Mount(string path, IFileSystem filesystem, IFileSystemOption mountOption = null);

        /// <summary>
        /// Unmount a filesystem at <paramref name="path"/>.
        /// 
        /// If there is no mount at <paramref name="path"/>, then does nothing.
        /// If there is an open stream to previously mounted filesystem, that stream is unlinked from the filesystem.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>this (parent filesystem)</returns>
        /// <exception cref="NotSupportedException">If operation is not supported</exception>
        IFileSystem Unmount(string path);

        /// <summary>
        /// List all mounts.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">If operation is not supported</exception>
        IFileSystemEntryMount[] ListMounts();
    }

    /// <summary>Option for mount path. Use with decorator.</summary>
    [Operations(typeof(FileSystemOptionOperationMountPath))]
    public interface IFileSystemOptionMountPath : IFileSystemOption
    {
        /// <summary>Mount path.</summary>
        String MountPath { get; }
    }
    // </doc>
}
