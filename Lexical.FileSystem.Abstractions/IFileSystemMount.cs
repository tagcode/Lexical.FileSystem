// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           12.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;

namespace Lexical.FileSystem
{
    /// <summary>File system option for mount capabilities. Used with <see cref="IFileSystemMount"/>.</summary>
    [Operations(typeof(FileSystemOptionOperationMount))]
    // <IFileSystemOptionMount>
    public interface IFileSystemOptionMount : IFileSystemOption
    {
        /// <summary>Can filesystem mount other filesystems.</summary>
        bool CanMount { get; }
        /// <summary>Is filesystem allowed to unmount a mount.</summary>
        bool CanUnmount { get; }
        /// <summary>Is filesystem allowed to list mountpoints.</summary>
        bool CanListMountPoints { get; }
    }
    // </IFileSystemOptionMount>

    // <doc>
    /// <summary>
    /// FileSystem that can mount other filesystems into its directory tree.
    /// </summary>
    public interface IFileSystemMount : IFileSystem, IFileSystemOptionMount
    {
        /// <summary>
        /// Mounts zero, one or many <see cref="IFileSystem"/> with optional <see cref="IFileSystemOption"/> in the parent filesystem.
        /// 
        /// If no mounts are provided, then creates empty virtual directory.
        /// If one mount is provided, then mounts that to parent filesystem, with possible mount option.
        /// If multiple mounts are provided, then mounts a composition of all the filesystem, with the precedence of the order in the provided array.
        /// 
        /// If previous mounts exist at the <paramref name="path"/>, then replaces them with new configuration.
        /// 
        /// If parent filesystem had observers monitoring the <paramref name="path"/>, then observers are notified with new emerged files from the mounted filesystems.
        /// 
        /// The <paramref name="path"/> parameter must end with directory separator character '/', unless root directory "" is mounted.
        /// 
        /// If there is an open stream to a mounted filesystem, then the file is unlinked from the parent filesystem, but stream maintains open.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="mounts">(optional) filesystem and option infos</param>
        /// <returns>this (parent filesystem)</returns>
        /// <exception cref="NotSupportedException">If operation is not supported</exception>
        IFileSystem Mount(string path, params FileSystemAssignment[] mounts);

        /// <summary>
        /// Unmount a filesystem at <paramref name="path"/>.
        /// 
        /// If there is no mount at <paramref name="path"/>, then does nothing.
        /// If there is an open stream to previously mounted filesystem, that the file is unlinked, but stream remains open.
        /// If there are observers monitoring <paramref name="path"/> in the parent filesystem, then the unmounted files are notified as being deleted.
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
        IFileSystemEntryMount[] ListMountPoints();
    }

    /// <summary>The filesystem and option assignment.</summary>
    public partial struct FileSystemAssignment
    {
        /// <summary>Filesystem.</summary>
        public readonly IFileSystem FileSystem;
        /// <summary>Overriding option assignment.</summary>
        public readonly IFileSystemOption Option;

        /// <summary>Create filesystem and option assignment.</summary>
        /// <param name="fileSystem">file system</param>
        /// <param name="option">(optional) overriding option assignment</param>
        public FileSystemAssignment(IFileSystem fileSystem, IFileSystemOption option)
        {
            FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            Option = option;
            AutoMount = false;
        }
    }
    // </doc>


    /// <summary>Option for auto-mounted packages.</summary>
    [Operations(typeof(FileSystemOptionOperationAutoMount))]
    // <IFileSystemOptionAutoMount>
    public interface IFileSystemOptionAutoMount : IFileSystemOption
    {
        /// <summary>Package loaders that can mount package files, such as .zip.</summary>
        IFileSystemPackageLoader[] AutoMounters { get; }
    }
    // </IFileSystemOptionAutoMount>

}
