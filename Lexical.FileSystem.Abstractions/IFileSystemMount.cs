// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           12.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------

using Lexical.FileSystem.Utility;
using System;
using System.IO;

namespace Lexical.FileSystem
{
    // <doc>
    /// <summary>File system option for mount capabilities. Used with <see cref="IFileSystemMount"/>.</summary>
    [Operations(typeof(FileSystemOptionOperationMount))]
    public interface IFileSystemOptionMount : IFileSystemOption
    {
        /// <summary>CAn filesystem create mountpoints.</summary>
        bool CanCreateMountPoint { get; }
        /// <summary>Is filesystem allowed to list mountpoints handles.</summary>
        bool CanListMountPoints { get; }
        /// <summary>Can filesystem capable get mountpoint entry by path.</summary>
        bool CanGetMountPoint { get; }
        /// <summary>Is filesystem allowed to close mount point.</summary>
        bool CanCloseMountPoint { get; }
        /// <summary>Can filesystem assign mount to mountpoint.</summary>
        bool CanMount { get; }
        /// <summary>Is filesystem allowed to get mount assignment handles.</summary>
        bool CanListMounts { get; }
        /// <summary>Is filesystem allowed to close mount assignment.</summary>
        bool CanCloseMount { get; }
    }

    /// <summary>
    /// FileSystem that can mount other filesystems into its directory tree.
    /// </summary>
    public interface IFileSystemMount : IFileSystem, IFileSystemOptionMount
    {
        /// <summary>
        /// Create a mountpoint where other filesystems can be mounted.
        /// 
        /// If mountpoint already exists, returns another handle to same mountpoint. All handles must be disposed separately.
        /// </summary>
        /// <param name="path">path to the mountpoint</param>
        /// <returns>a handle to mountpoint</returns>
        /// <exception cref="NotSupportedException">If mountpoint creation is not supported</exception>
        /// <exception cref="IOException">If creation failed.</exception>
        IFileSystemMountPoint CreateMountPoint(string path);

        /// <summary>
        /// List all mountpoints.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">If mountpoint creation is not supported</exception>
        IFileSystemMountPoint[] ListMountPoints();

        /// <summary>
        /// Get handle mountpoint at <paramref name="path"/>.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>mountpoint or null if doesn't exist</returns>
        IFileSystemMountPoint GetMountPoint(string path);
    }

    /// <summary>
    /// Mount point is a virtual directory where other filesystems can be assigned by mounting.
    /// 
    /// All mount assignments are disposed when mountpoint is disposed <see cref="IFileSystemMountAssignment"/>.
    /// </summary>
    public interface IFileSystemMountPoint : IDisposeList
    {
        /// <summary>
        /// The mountable parent file system.
        /// </summary>
        IFileSystem FileSystem { get; }

        /// <summary>
        /// Path of the mount point in the parent <see cref="FileSystem"/>.
        /// </summary>
        String Path { get; }

        /// <summary>
        /// Mount <paramref name="filesystem"/> at the mountpoint.
        /// 
        /// The <paramref name="options"/> parameter determines the mounting options.
        /// The mounted directory stucture will have intersection of options in <paramref name="filesystem"/> and <paramref name="options"/>.
        /// 
        /// For example if <paramref name="filesystem"/> has read and write permissions, but <paramref name="options"/> uses <see cref="FileSystemOption.ReadOnly"/>, then the mounted directory can be read but not written to.
        /// 
        /// To mount a subpath of <paramref name="filesystem"/> use <see cref="IFileSystemOptionMountPath"/> as an option to <paramref name="options"/>.
        /// For example <see cref="FileSystemOption.MountPath(string)"/>.
        /// 
        /// To combine multiple mounting options use <see cref="FileSystemOption.Union(IFileSystemOption[])"/>.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="options">(optional) mounting options.</param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">If the implementing class does not support mounting at all, or one of the mount <paramref name="options"/></exception>
        IFileSystemMountAssignment Mount(IFileSystem filesystem, IFileSystemOption options);

        /// <summary>
        /// Get mounting assignment handles.
        /// </summary>
        /// <returns></returns>
        IFileSystemMountAssignment[] GetMounts();
    }

    /// <summary>
    /// Mount handle. Dispose the handle to unmount.
    /// 
    /// All disposables are disposed when mountpoint is disposed <see cref="IFileSystemMountAssignment"/>.
    /// </summary>
    public interface IFileSystemMountAssignment : IDisposeList
    {
        /// <summary>Mounted filesystem</summary>
        IFileSystem MountedFileSystem { get; }

        /// <summary>The effective mount options</summary>
        IFileSystemOption Options { get; }
    }

    /// <summary>Option for mount path. Used as mounting option with <see cref="IFileSystemMountPoint"/> Mount method.</summary>
    [Operations(typeof(FileSystemOptionOperationMountPath))]
    public interface IFileSystemOptionMountPath : IFileSystemOption
    {
        /// <summary>Mount path.</summary>
        String MountPath { get; }
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
        public static bool CanCreateMountPoint(this IFileSystemOption filesystemOption)
            => filesystemOption.As<IFileSystemMount>() is IFileSystemMount mountable ? mountable.CanCreateMountPoint : false;

        /// <summary>
        /// Is filesystem capable of listing mountpoints.
        /// </summary>
        /// <returns></returns>
        public static bool CanListMountPoints(this IFileSystemOption filesystemOption)
            => filesystemOption.As<IFileSystemMount>() is IFileSystemMount mountable ? mountable.CanListMountPoints : false;

        /// <summary>
        /// Is filesystem capable of getting mountpoint entry.
        /// </summary>
        /// <returns></returns>
        public static bool CanGetMountPoint(this IFileSystemOption filesystemOption)
            => filesystemOption.As<IFileSystemMount>() is IFileSystemMount mountable ? mountable.CanGetMountPoint : false;

        /// <summary>
        /// Get mount path option.
        /// <param name="filesystemOption"></param>
        /// </summary>
        /// <returns>mount path or null</returns>
        public static String MountPath(this IFileSystemOption filesystemOption)
            => filesystemOption is IFileSystemOptionMountPath mp ? mp.MountPath : null;

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
        public static IFileSystemMountPoint CreateMountPoint(this IFileSystem filesystem, string path)
        {
            if (filesystem is IFileSystemMount mountable) return mountable.CreateMountPoint(path);
            throw new NotSupportedException();
        }

        /// <summary>
        /// List all mountpoints.
        /// </summary>
        /// <returns></returns>
        /// <param name="filesystem"></param>
        /// <exception cref="NotSupportedException">If mountpoint creation is not supported</exception>
        public static IFileSystemMountPoint[] ListMountPoints(this IFileSystem filesystem)
        {
            if (filesystem is IFileSystemMount mountable) return mountable.ListMountPoints();
            throw new NotSupportedException();
        }

        /// <summary>
        /// Get handle mountpoint at <paramref name="path"/>.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path"></param>
        /// <returns>mountpoint or null if doesn't exist</returns>
        public static IFileSystemMountPoint GetMountPoint(this IFileSystem filesystem, string path)
        {
            if (filesystem is IFileSystemMount mountable) return mountable.GetMountPoint(path);
            throw new NotSupportedException();
        }
    }

    /// <summary><see cref="IFileSystemOptionMount"/> operations.</summary>
    public class FileSystemOptionOperationMount : IFileSystemOptionOperationFlatten, IFileSystemOptionOperationIntersection, IFileSystemOptionOperationUnion
    {
        /// <summary>The option type that this class has operations for.</summary>
        public Type OptionType => typeof(IFileSystemOptionMount);
        /// <summary>Flatten to simpler instance.</summary>
        public IFileSystemOption Flatten(IFileSystemOption o) => o is IFileSystemOptionMount c ? o is FileSystemOptionMount ? /*already flattened*/o : /*new instance*/new FileSystemOptionMount(c.CanCreateMountPoint, c.CanListMountPoints, c.CanGetMountPoint, c.CanCloseMount, c.CanMount, c.CanListMounts, c.CanCloseMount) : throw new InvalidCastException($"{typeof(IFileSystemOptionMount)} expected.");
        /// <summary>Intersection of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IFileSystemOption Intersection(IFileSystemOption o1, IFileSystemOption o2) => o1 is IFileSystemOptionMount c1 && o2 is IFileSystemOptionMount c2 ? new FileSystemOptionMount(c1.CanCreateMountPoint || c1.CanCreateMountPoint, c1.CanListMountPoints || c2.CanListMountPoints, c1.CanGetMountPoint || c2.CanGetMountPoint, c1.CanCloseMountPoint || c2.CanCloseMountPoint, c1.CanMount||c2.CanMount, c1.CanListMounts||c2.CanListMounts, c1.CanCloseMount||c2.CanCloseMount) : throw new InvalidCastException($"{typeof(IFileSystemOptionMount)} expected.");
        /// <summary>Union of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IFileSystemOption Union(IFileSystemOption o1, IFileSystemOption o2) => o1 is IFileSystemOptionMount c1 && o2 is IFileSystemOptionMount c2 ? new FileSystemOptionMount(c1.CanCreateMountPoint && c1.CanCreateMountPoint, c1.CanListMountPoints && c2.CanListMountPoints, c1.CanGetMountPoint && c2.CanGetMountPoint, c1.CanCloseMountPoint && c2.CanCloseMountPoint, c1.CanMount && c2.CanMount, c1.CanListMounts && c2.CanListMounts, c1.CanCloseMount && c2.CanCloseMount) : throw new InvalidCastException($"{typeof(IFileSystemOptionMount)} expected.");
    }

    /// <summary><see cref="IFileSystemOptionMountPath"/> operations.</summary>
    public class FileSystemOptionOperationMountPath : IFileSystemOptionOperationFlatten
    {
        /// <summary>The option type that this class has operations for.</summary>
        public Type OptionType => typeof(IFileSystemOptionMountPath);
        /// <summary>Flatten to simpler instance.</summary>
        public IFileSystemOption Flatten(IFileSystemOption o) => o is IFileSystemOptionMountPath b ? o is FileSystemOptionMountPath ? /*already flattened*/o : /*new instance*/new FileSystemOptionMountPath(b.MountPath) : throw new InvalidCastException($"{typeof(IFileSystemOptionMountPath)} expected.");
    }


}
