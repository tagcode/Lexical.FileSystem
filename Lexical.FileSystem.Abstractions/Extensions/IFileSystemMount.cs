// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           12.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Package;
using System;
using System.Linq;

namespace Lexical.FileSystem
{
    public partial struct FileSystemAssignment : IEquatable<FileSystemAssignment>
    {
        /// <summary>Implicit conversion</summary>
        public static implicit operator (IFileSystem, IFileSystemOption)(FileSystemAssignment info) => (info.FileSystem, info.Option);
        /// <summary>Implicit conversion</summary>
        public static implicit operator FileSystemAssignment((IFileSystem, IFileSystemOption) info) => new FileSystemAssignment(info.Item1, info.Item2);
        /// <summary>Compare infos</summary>
        public static bool operator ==(FileSystemAssignment left, FileSystemAssignment right)
            => right.FileSystem.Equals(left.FileSystem) && ((left.Option == null) == (right.Option == null) || (left.Option != null && left.Option.Equals(right.Option)));
        /// <summary>Compare infos</summary>
        public static bool operator !=(FileSystemAssignment left, FileSystemAssignment right)
            => !right.FileSystem.Equals(left.FileSystem) || ((left.Option == null) != (right.Option == null)) || (left.Option != null && !left.Option.Equals(right.Option));
        /// <summary>Compare infos</summary>
        public bool Equals(FileSystemAssignment other)
            => other.FileSystem.Equals(FileSystem) && ((Option == null) == (other.Option == null) || (Option != null && Option.Equals(other.Option)));
        /// <summary>Compare infos</summary>
        public override bool Equals(object obj)
            => obj is FileSystemAssignment other ? other.FileSystem.Equals(FileSystem) && ((Option == null) == (other.Option == null) || (Option != null && Option.Equals(other.Option))) : false;
        /// <summary>Info hashcode</summary>
        public override int GetHashCode()
            => 3 * FileSystem.GetHashCode() + (Option == null ? 0 : 7 * Option.GetHashCode());
        /// <summary>Print info</summary>
        public override string ToString()
            => Option == null ? FileSystem.ToString() : $"{FileSystem}({Option})";
    }

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
            => filesystemOption.AsOption<IFileSystemOptionMount>() is IFileSystemOptionMount mountable ? mountable.CanMount : false;

        /// <summary>
        /// Is filesystem allowed to list mountpoints.
        /// </summary>
        /// <returns></returns>
        public static bool CanListMountPoints(this IFileSystemOption filesystemOption)
            => filesystemOption.AsOption<IFileSystemOptionMount>() is IFileSystemOptionMount mountable ? mountable.CanListMountPoints : false;

        /// <summary>
        /// Is filesystem allowed to unmount a mount.
        /// </summary>
        /// <returns></returns>
        public static bool CanUnmount(this IFileSystemOption filesystemOption)
            => filesystemOption.AsOption<IFileSystemOptionMount>() is IFileSystemOptionMount mountable ? mountable.CanUnmount : false;

        /// <summary>
        /// Get automounters.
        /// </summary>
        /// <param name="filesystemOption"></param>
        /// <returns></returns>
        public static IPackageLoader[] AutoMounters(this IFileSystemOption filesystemOption)
            => filesystemOption.AsOption<IFileSystemOptionAutoMount>() is IFileSystemOptionAutoMount automount ? automount.AutoMounters : null;

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
        /// <param name="parentFileSystem"></param>
        /// <param name="path">path to mount point</param>
        /// <param name="filesystem">filesystem</param>
        /// <param name="mountOption">(optional) options</param>
        /// <param name="option">(optional) filesystem implementation specific token, such as session, security token or credential. Used for authorizing or facilitating the action.</param>
        /// <exception cref="NotSupportedException">If operation is not supported</exception>
        public static IFileSystem Mount(this IFileSystem parentFileSystem, string path, IFileSystem filesystem, IFileSystemOption mountOption = null, IFileSystemToken option = null)
        {
            if (parentFileSystem is IFileSystemMount mountable) return mountable.Mount(path, new FileSystemAssignment[] { new FileSystemAssignment(filesystem, mountOption) }, option);
            throw new NotSupportedException(nameof(Mount));
        }

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
        /// <param name="parentFileSystem"></param>
        /// <param name="path"></param>
        /// <param name="filesystems">filesystem</param>
        /// <exception cref="NotSupportedException">If operation is not supported</exception>
        public static IFileSystem Mount(this IFileSystem parentFileSystem, string path, params IFileSystem[] filesystems)
        {
            if (parentFileSystem is IFileSystemMount mountable) return mountable.Mount(path, filesystems.Select(fs=> new FileSystemAssignment(fs, null)).ToArray(), option: null);
            throw new NotSupportedException(nameof(Mount));
        }

        /// <summary>
        /// Mount <paramref name="filesystems"/> at <paramref name="path"/> in the parent filesystem.
        /// 
        /// If <paramref name="path"/> is already mounted, then replaces previous mount.
        /// If there is an open stream to previously mounted filesystem, that stream is unlinked from the filesystem.
        /// </summary>
        /// <param name="parentFileSystem"></param>
        /// <param name="path">path to mount point</param>
        /// <param name="filesystems"></param>
        /// <returns>this (parent filesystem)</returns>
        /// <exception cref="NotSupportedException">If operation is not supported</exception>
        public static IFileSystem Mount(this IFileSystem parentFileSystem, string path, params (IFileSystem filesystem, IFileSystemOption mountOption)[] filesystems)
        {
            if (parentFileSystem is IFileSystemMount mountable) return mountable.Mount(path, filesystems.Select(fs => new FileSystemAssignment(fs.filesystem, fs.mountOption)).ToArray(), option: null);
            throw new NotSupportedException(nameof(Mount));
        }

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
        /// <param name="parentFileSystem"></param>
        /// <param name="path">path to mount point</param>
        /// <param name="mounts">(optional) filesystem and option infos</param>
        /// <param name="option">(optional) filesystem implementation specific token, such as session, security token or credential. Used for authorizing or facilitating the action.</param>
        /// <exception cref="NotSupportedException">If operation is not supported</exception>
        public static IFileSystem Mount(this IFileSystem parentFileSystem, string path, FileSystemAssignment[] mounts, IFileSystemToken option = null)
        {
            if (parentFileSystem is IFileSystemMount mountable) return mountable.Mount(path, mounts, option);
            throw new NotSupportedException(nameof(Mount));
        }

        /// <summary>
        /// Unmount a filesystem at <paramref name="path"/>.
        /// 
        /// If there is no mount at <paramref name="path"/>, then does nothing.
        /// </summary>
        /// <param name="parentFileSystem"></param>
        /// <param name="path">path to mount point</param>
        /// <param name="option">(optional) filesystem implementation specific token, such as session, security token or credential. Used for authorizing or facilitating the action.</param>
        /// <returns>this (parent filesystem)</returns>
        /// <exception cref="NotSupportedException">If operation is not supported</exception>
        public static IFileSystem Unmount(this IFileSystem parentFileSystem, string path, IFileSystemToken option = null)
        {
            if (parentFileSystem is IFileSystemMount mountable) return mountable.Unmount(path, option);
            throw new NotSupportedException(nameof(Unmount));
        }

        /// <summary>
        /// List all mounts.
        /// </summary>
        /// <param name="parentFileSystem"></param>
        /// <param name="option">(optional) filesystem implementation specific token, such as session, security token or credential. Used for authorizing or facilitating the action.</param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">If operation is not supported</exception>
        public static IFileSystemEntryMount[] ListMountPoints(this IFileSystem parentFileSystem, IFileSystemToken option = null)
        {
            if (parentFileSystem is IFileSystemMount mountable) return mountable.ListMountPoints(option);
            throw new NotSupportedException(nameof(ListMountPoints));
        }

    }

    /// <summary><see cref="IFileSystemOptionMount"/> operations.</summary>
    public class FileSystemOptionOperationMount : IFileSystemOptionOperationFlatten, IFileSystemOptionOperationIntersection, IFileSystemOptionOperationUnion
    {
        /// <summary>The option type that this class has operations for.</summary>
        public Type OptionType => typeof(IFileSystemOptionMount);
        /// <summary>Flatten to simpler instance.</summary>
        public IFileSystemOption Flatten(IFileSystemOption o) => o is IFileSystemOptionMount c ? o is FileSystemOptionMount ? /*already flattened*/o : /*new instance*/new FileSystemOptionMount(c.CanMount, c.CanUnmount, c.CanListMountPoints) : throw new InvalidCastException($"{typeof(IFileSystemOptionMount)} expected.");
        /// <summary>Intersection of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IFileSystemOption Intersection(IFileSystemOption o1, IFileSystemOption o2) => o1 is IFileSystemOptionMount c1 && o2 is IFileSystemOptionMount c2 ? new FileSystemOptionMount(c1.CanMount || c1.CanMount, c1.CanUnmount || c2.CanUnmount, c1.CanListMountPoints || c2.CanListMountPoints) : throw new InvalidCastException($"{typeof(IFileSystemOptionMount)} expected.");
        /// <summary>Union of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IFileSystemOption Union(IFileSystemOption o1, IFileSystemOption o2) => o1 is IFileSystemOptionMount c1 && o2 is IFileSystemOptionMount c2 ? new FileSystemOptionMount(c1.CanMount && c1.CanMount, c1.CanUnmount && c2.CanUnmount, c1.CanListMountPoints && c2.CanListMountPoints) : throw new InvalidCastException($"{typeof(IFileSystemOptionMount)} expected.");
    }

    /// <summary><see cref="IFileSystemOptionSubPath"/> operations.</summary>
    public class FileSystemOptionOperationSubPath : IFileSystemOptionOperationFlatten, IFileSystemOptionOperationIntersection, IFileSystemOptionOperationUnion
    {
        /// <summary>The option type that this class has operations for.</summary>
        public Type OptionType => typeof(IFileSystemOptionSubPath);
        /// <summary>Flatten to simpler instance.</summary>
        public IFileSystemOption Flatten(IFileSystemOption o) => o is IFileSystemOptionSubPath b ? o is FileSystemOptionSubPath ? /*already flattened*/o : /*new instance*/new FileSystemOptionSubPath(b.SubPath) : throw new InvalidCastException($"{typeof(IFileSystemOptionSubPath)} expected.");
        /// <summary>Intersection of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IFileSystemOption Intersection(IFileSystemOption o1, IFileSystemOption o2) => o1 is IFileSystemOptionSubPath c1 && o2 is IFileSystemOptionSubPath c2 ?
            (c1 != null && c2 == null ? new FileSystemOptionSubPath(c1.SubPath) :
             c1 == null && c2 != null ? new FileSystemOptionSubPath(c2.SubPath) :
             c1 != null && c2 != null && c1.SubPath == c1.SubPath ? new FileSystemOptionSubPath(c1.SubPath) :
             c1 == null && c2 == null ? (IFileSystemOption) null: 
             throw new FileSystemExceptionOptionOperationNotSupported(null, null, o1, typeof(IFileSystemOptionSubPath), typeof(IFileSystemOptionOperationIntersection))) : 
            throw new InvalidCastException($"{typeof(IFileSystemOptionMount)} expected.");
        /// <summary>Union of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IFileSystemOption Union(IFileSystemOption o1, IFileSystemOption o2) => o1 is IFileSystemOptionSubPath c1 && o2 is IFileSystemOptionSubPath c2 ?
            (c1 != null && c2 == null ? new FileSystemOptionSubPath(c1.SubPath) :
             c1 == null && c2 != null ? new FileSystemOptionSubPath(c2.SubPath) :
             c1 != null && c2 != null && c1.SubPath == c1.SubPath ? new FileSystemOptionSubPath(c1.SubPath) :
             c1 == null && c2 == null ? (IFileSystemOption)null :
             throw new FileSystemExceptionOptionOperationNotSupported(null, null, o1, typeof(IFileSystemOptionSubPath), typeof(IFileSystemOptionOperationIntersection))) :
            throw new InvalidCastException($"{typeof(IFileSystemOptionMount)} expected.");
    }

    /// <summary>Option for mount path. Use with decorator.</summary>
    public class FileSystemOptionSubPath : IFileSystemOptionSubPath
    {
        /// <summary>Mount path.</summary>
        public String SubPath { get; protected set; }

        /// <summary>Create option for mount path.</summary>
        public FileSystemOptionSubPath(string mountPath)
        {
            SubPath = mountPath;
        }

        /// <inheritdoc/>
        public override string ToString() => String.IsNullOrEmpty(SubPath) ? "" : "SubPath="+SubPath;
    }

    /// <summary>File system option for mount capabilities.</summary>
    public class FileSystemOptionMount : IFileSystemOptionMount
    {
        /// <summary>Can filesystem mount other filesystems.</summary>
        public bool CanMount { get; protected set; }
        /// <summary>Is filesystem allowed to unmount a mount.</summary>
        public bool CanUnmount { get; protected set; }
        /// <summary>Is filesystem allowed to list mountpoints.</summary>
        public bool CanListMountPoints { get; protected set; }

        /// <summary>Create file system option for mount capabilities.</summary>
        public FileSystemOptionMount(bool canMount, bool canUnmount, bool canListMountPoints)
        {
            CanMount = canMount;
            CanUnmount = canUnmount;
            CanListMountPoints = canListMountPoints;
        }

        /// <inheritdoc/>
        public override string ToString() => (CanMount ? "CanMount" : "NoMount") + "," + (CanUnmount ? "CanUnmount" : "NoUnmount") + "," + (CanListMountPoints ? "CanListMountPoints" : "NoListMountPoints");
    }


}
