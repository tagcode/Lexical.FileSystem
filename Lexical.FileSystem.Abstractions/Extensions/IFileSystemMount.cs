// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           12.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Extension methods for <see cref="IFileSystem"/>.
    /// </summary>
    public static partial class IFileSystemExtensions
    {
        /// <summary>
        /// Is filesystem capable of creating mountpoints.
        /// </summary>
        /// <returns></returns>
        public static bool CanMount(this IFileSystemOption filesystemOption)
            => filesystemOption.As<IFileSystemOptionMount>() is IFileSystemOptionMount mountable ? mountable.CanMount : false;

        /// <summary>
        /// Is filesystem allowed to list mounts.
        /// </summary>
        /// <returns></returns>
        public static bool CanListMounts(this IFileSystemOption filesystemOption)
            => filesystemOption.As<IFileSystemOptionMount>() is IFileSystemOptionMount mountable ? mountable.CanListMounts : false;

        /// <summary>
        /// Is filesystem allowed to unmount a mount.
        /// </summary>
        /// <returns></returns>
        public static bool CanUnmount(this IFileSystemOption filesystemOption)
            => filesystemOption.As<IFileSystemOptionMount>() is IFileSystemOptionMount mountable ? mountable.CanUnmount : false;

        /// <summary>
        /// Get mount path option.
        /// <param name="filesystemOption"></param>
        /// </summary>
        /// <returns>mount path or null</returns>
        public static String MountPath(this IFileSystemOption filesystemOption)
            => filesystemOption.As<IFileSystemOptionMountPath>() is IFileSystemOptionMountPath mp ? mp.MountPath : null;

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
        /// <param name="parentFileSystem"></param>
        /// <param name="path"></param>
        /// <param name="filesystem"></param>
        /// <param name="mountOption">(Optional) mount options</param>
        /// <returns>this (parent filesystem)</returns>
        /// <exception cref="NotSupportedException">If operation is not supported</exception>
        public static IFileSystem Mount(this IFileSystem parentFileSystem, string path, IFileSystem filesystem, IFileSystemOption mountOption = null)
        {
            if (parentFileSystem is IFileSystemMount mountable) return mountable.Mount(path, filesystem, mountOption);
            throw new NotSupportedException(nameof(Mount));
        }

        /// <summary>
        /// Unmount a filesystem at <paramref name="path"/>.
        /// 
        /// If there is no mount at <paramref name="path"/>, then does nothing.
        /// </summary>
        /// <param name="parentFileSystem"></param>
        /// <param name="path"></param>
        /// <returns>this (parent filesystem)</returns>
        /// <exception cref="NotSupportedException">If operation is not supported</exception>
        public static IFileSystem Unmount(this IFileSystem parentFileSystem, string path)
        {
            if (parentFileSystem is IFileSystemMount mountable) return mountable.Unmount(path);
            throw new NotSupportedException(nameof(Unmount));
        }

        /// <summary>
        /// List all mounts.
        /// </summary>
        /// <param name="parentFileSystem"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">If operation is not supported</exception>
        public static IFileSystemEntryMount[] ListMounts(this IFileSystem parentFileSystem)
        {
            if (parentFileSystem is IFileSystemMount mountable) return mountable.ListMounts();
            throw new NotSupportedException(nameof(ListMounts));
        }

    }

    /// <summary><see cref="IFileSystemOptionMount"/> operations.</summary>
    public class FileSystemOptionOperationMount : IFileSystemOptionOperationFlatten, IFileSystemOptionOperationIntersection, IFileSystemOptionOperationUnion
    {
        /// <summary>The option type that this class has operations for.</summary>
        public Type OptionType => typeof(IFileSystemOptionMount);
        /// <summary>Flatten to simpler instance.</summary>
        public IFileSystemOption Flatten(IFileSystemOption o) => o is IFileSystemOptionMount c ? o is FileSystemOptionMount ? /*already flattened*/o : /*new instance*/new FileSystemOptionMount(c.CanMount, c.CanUnmount, c.CanListMounts) : throw new InvalidCastException($"{typeof(IFileSystemOptionMount)} expected.");
        /// <summary>Intersection of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IFileSystemOption Intersection(IFileSystemOption o1, IFileSystemOption o2) => o1 is IFileSystemOptionMount c1 && o2 is IFileSystemOptionMount c2 ? new FileSystemOptionMount(c1.CanMount || c1.CanMount, c1.CanUnmount || c2.CanUnmount, c1.CanListMounts || c2.CanListMounts) : throw new InvalidCastException($"{typeof(IFileSystemOptionMount)} expected.");
        /// <summary>Union of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IFileSystemOption Union(IFileSystemOption o1, IFileSystemOption o2) => o1 is IFileSystemOptionMount c1 && o2 is IFileSystemOptionMount c2 ? new FileSystemOptionMount(c1.CanMount && c1.CanMount, c1.CanUnmount && c2.CanUnmount, c1.CanListMounts && c2.CanListMounts) : throw new InvalidCastException($"{typeof(IFileSystemOptionMount)} expected.");
    }

    /// <summary><see cref="IFileSystemOptionMountPath"/> operations.</summary>
    public class FileSystemOptionOperationMountPath : IFileSystemOptionOperationFlatten, IFileSystemOptionOperationIntersection, IFileSystemOptionOperationUnion
    {
        /// <summary>The option type that this class has operations for.</summary>
        public Type OptionType => typeof(IFileSystemOptionMountPath);
        /// <summary>Flatten to simpler instance.</summary>
        public IFileSystemOption Flatten(IFileSystemOption o) => o is IFileSystemOptionMountPath b ? o is FileSystemOptionMountPath ? /*already flattened*/o : /*new instance*/new FileSystemOptionMountPath(b.MountPath) : throw new InvalidCastException($"{typeof(IFileSystemOptionMountPath)} expected.");
        /// <summary>Intersection of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IFileSystemOption Intersection(IFileSystemOption o1, IFileSystemOption o2) => o1 is IFileSystemOptionMountPath c1 && o2 is IFileSystemOptionMountPath c2 ?
            (c1 != null && c2 == null ? new FileSystemOptionMountPath(c1.MountPath) :
             c1 == null && c2 != null ? new FileSystemOptionMountPath(c2.MountPath) :
             c1 != null && c2 != null && c1.MountPath == c1.MountPath ? new FileSystemOptionMountPath(c1.MountPath) :
             c1 == null && c2 == null ? (IFileSystemOption) null: 
             throw new FileSystemExceptionOptionOperationNotSupported(null, null, o1, typeof(IFileSystemOptionMountPath), typeof(IFileSystemOptionOperationIntersection))) : 
            throw new InvalidCastException($"{typeof(IFileSystemOptionMount)} expected.");
        /// <summary>Union of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IFileSystemOption Union(IFileSystemOption o1, IFileSystemOption o2) => o1 is IFileSystemOptionMountPath c1 && o2 is IFileSystemOptionMountPath c2 ?
            (c1 != null && c2 == null ? new FileSystemOptionMountPath(c1.MountPath) :
             c1 == null && c2 != null ? new FileSystemOptionMountPath(c2.MountPath) :
             c1 != null && c2 != null && c1.MountPath == c1.MountPath ? new FileSystemOptionMountPath(c1.MountPath) :
             c1 == null && c2 == null ? (IFileSystemOption)null :
             throw new FileSystemExceptionOptionOperationNotSupported(null, null, o1, typeof(IFileSystemOptionMountPath), typeof(IFileSystemOptionOperationIntersection))) :
            throw new InvalidCastException($"{typeof(IFileSystemOptionMount)} expected.");
    }

    /// <summary>Option for mount path. Use with decorator.</summary>
    public class FileSystemOptionMountPath : IFileSystemOptionMountPath
    {
        /// <summary>Mount path.</summary>
        public String MountPath { get; protected set; }

        /// <summary>Create option for mount path.</summary>
        public FileSystemOptionMountPath(string mountPath)
        {
            MountPath = mountPath;
        }
    }

    /// <summary>File system option for mount capabilities.</summary>
    public class FileSystemOptionMount : IFileSystemOptionMount
    {
        /// <summary>Can filesystem mount other filesystems.</summary>
        public bool CanMount { get; protected set; }
        /// <summary>Is filesystem allowed to unmount a mount.</summary>
        public bool CanUnmount { get; protected set; }
        /// <summary>Is filesystem allowed to list mounts.</summary>
        public bool CanListMounts { get; protected set; }

        /// <summary>Create file system option for mount capabilities.</summary>
        public FileSystemOptionMount(bool canMount, bool canUnmount, bool canListMounts)
        {
            CanMount = canMount;
            CanUnmount = canUnmount;
            CanListMounts = canListMounts;
        }
    }


}
