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
        /// <summary>Is filesystem allowed to list mounts.</summary>
        bool CanListMounts { get; }
    }
    // </IFileSystemOptionMount>

    // <doc>
    /// <summary>
    /// FileSystem that can mount other filesystems into its directory tree.
    /// </summary>
    public interface IFileSystemMount : IFileSystem, IFileSystemOptionMount
    {
        /// <summary>
        /// Mount <paramref name="filesystem"/> at <paramref name="path"/> in the parent filesystem.
        /// 
        /// The <paramref name="path"/> should end at directory separator character '/', unless root directory "" is mounted.
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

    /// <summary>The mount parameters.</summary>
    public partial struct FileSystemMountInfo
    {
        /// <summary>The mounted filesystem.</summary>
        public readonly IFileSystem FileSystem;
        /// <summary>The mounte options.</summary>
        public readonly IFileSystemOption MountOption;

        /// <summary>Create mount info.</summary>
        /// <param name="fileSystem">file system to mount</param>
        /// <param name="mountOption">(optional) mount options</param>
        public FileSystemMountInfo(IFileSystem fileSystem, IFileSystemOption mountOption)
        {
            FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            MountOption = mountOption;
        }
    }
    // </doc>


    /// <summary>Option for auto-mounted packages.</summary>
    [Operations(typeof(FileSystemOptionOperationAutoMount))]
    // <IFileSystemOptionAutoMount>
    public interface IFileSystemOptionAutoMount : IFileSystemOption
    {
        /// <summary>Package loaders that can mount package files, such as .zip.</summary>
        IFileSystemPackageLoader[] PackageLoaders { get; }
    }
    // </IFileSystemOptionAutoMount>

    /// <summary>The mount parameters.</summary>
    public partial struct FileSystemMountInfo : IEquatable<FileSystemMountInfo>
    {
        /// <summary>Implicit conversion</summary>
        public static implicit operator (IFileSystem, IFileSystemOption)(FileSystemMountInfo info) => (info.FileSystem, info.MountOption);
        /// <summary>Implicit conversion</summary>
        public static implicit operator FileSystemMountInfo((IFileSystem, IFileSystemOption) info) => new FileSystemMountInfo(info.Item1, info.Item2);
        /// <summary>Compare infos</summary>
        public static bool operator ==(FileSystemMountInfo left, FileSystemMountInfo right)
            => right.FileSystem.Equals(left.FileSystem) && ((left.MountOption == null) == (right.MountOption == null) || (left.MountOption != null && left.MountOption.Equals(right.MountOption)));
        /// <summary>Compare infos</summary>
        public static bool operator !=(FileSystemMountInfo left, FileSystemMountInfo right)
            => !right.FileSystem.Equals(left.FileSystem) || ((left.MountOption == null) != (right.MountOption == null)) || (left.MountOption != null && !left.MountOption.Equals(right.MountOption));
        /// <summary>Compare infos</summary>
        public bool Equals(FileSystemMountInfo other)
            => other.FileSystem.Equals(FileSystem) && ((MountOption == null) == (other.MountOption == null) || (MountOption != null && MountOption.Equals(other.MountOption)));
        /// <summary>Compare infos</summary>
        public override bool Equals(object obj)
            => obj is FileSystemMountInfo other ? other.FileSystem.Equals(FileSystem) && ((MountOption == null) == (other.MountOption == null) || (MountOption != null && MountOption.Equals(other.MountOption))) : false;
        /// <summary>Info hashcode</summary>
        public override int GetHashCode()
            => 3 * FileSystem.GetHashCode() + (MountOption == null ? 0 : 7 * MountOption.GetHashCode());
        /// <summary>Print info</summary>
        public override string ToString()
            => MountOption == null ? FileSystem.ToString() : $"{FileSystem}({MountOption})";
    }

}
