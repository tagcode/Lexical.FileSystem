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
        public static implicit operator (IFileSystem, IOption)(FileSystemAssignment info) => (info.FileSystem, info.Option);
        /// <summary>Implicit conversion</summary>
        public static implicit operator FileSystemAssignment((IFileSystem, IOption) info) => new FileSystemAssignment(info.Item1, info.Item2);
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
    public static partial class FileSystemMountExtensions
    {
        /// <summary>
        /// Is filesystem capable of creating mountpoints.
        /// </summary>
        /// <param name="filesystemOption"></param>
        /// <param name="defaultValue">Returned value if option is unspecified</param>
        /// <returns></returns>
        public static bool CanMount(this IOption filesystemOption, bool defaultValue = false)
            => filesystemOption.AsOption<IMountOption>() is IMountOption mountable ? mountable.CanMount : defaultValue;

        /// <summary>
        /// Is filesystem allowed to list mountpoints.
        /// </summary>
        /// <param name="filesystemOption"></param>
        /// <param name="defaultValue">Returned value if option is unspecified</param>
        /// <returns></returns>
        public static bool CanListMountPoints(this IOption filesystemOption, bool defaultValue = false)
            => filesystemOption.AsOption<IMountOption>() is IMountOption mountable ? mountable.CanListMountPoints : defaultValue;

        /// <summary>
        /// Is filesystem allowed to unmount a mount.
        /// </summary>
        /// <param name="filesystemOption"></param>
        /// <param name="defaultValue">Returned value if option is unspecified</param>
        /// <returns></returns>
        public static bool CanUnmount(this IOption filesystemOption, bool defaultValue = false)
            => filesystemOption.AsOption<IMountOption>() is IMountOption mountable ? mountable.CanUnmount : defaultValue;

        /// <summary>
        /// Get automounters.
        /// </summary>
        /// <param name="filesystemOption"></param>
        /// <returns></returns>
        public static IPackageLoader[] AutoMounters(this IOption filesystemOption)
            => filesystemOption.AsOption<IAutoMountOption>() is IAutoMountOption automount ? automount.AutoMounters : null;

        /// <summary>
        /// Mounts zero, one or many <see cref="IFileSystem"/> with optional <see cref="IOption"/> in the parent filesystem.
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
        /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
        /// <exception cref="NotSupportedException">If operation is not supported</exception>
        public static IFileSystem Mount(this IFileSystem parentFileSystem, string path, IFileSystem filesystem, IOption mountOption = null, IOption option = null)
        {
            if (parentFileSystem is IFileSystemMount mountable) return mountable.Mount(path, new FileSystemAssignment[] { new FileSystemAssignment(filesystem, mountOption) }, option);
            throw new NotSupportedException(nameof(Mount));
        }

        /// <summary>
        /// Mounts zero, one or many <see cref="IFileSystem"/> with optional <see cref="IOption"/> in the parent filesystem.
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
        public static IFileSystem Mount(this IFileSystem parentFileSystem, string path, params (IFileSystem filesystem, IOption mountOption)[] filesystems)
        {
            if (parentFileSystem is IFileSystemMount mountable) return mountable.Mount(path, filesystems.Select(fs => new FileSystemAssignment(fs.filesystem, fs.mountOption)).ToArray(), option: null);
            throw new NotSupportedException(nameof(Mount));
        }

        /// <summary>
        /// Mounts zero, one or many <see cref="IFileSystem"/> with optional <see cref="IOption"/> in the parent filesystem.
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
        /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
        /// <exception cref="NotSupportedException">If operation is not supported</exception>
        public static IFileSystem Mount(this IFileSystem parentFileSystem, string path, FileSystemAssignment[] mounts, IOption option = null)
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
        /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
        /// <returns>this (parent filesystem)</returns>
        /// <exception cref="NotSupportedException">If operation is not supported</exception>
        public static IFileSystem Unmount(this IFileSystem parentFileSystem, string path, IOption option = null)
        {
            if (parentFileSystem is IFileSystemMount mountable) return mountable.Unmount(path, option);
            throw new NotSupportedException(nameof(Unmount));
        }

        /// <summary>
        /// List all mounts.
        /// </summary>
        /// <param name="parentFileSystem"></param>
        /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">If operation is not supported</exception>
        public static IMountEntry[] ListMountPoints(this IFileSystem parentFileSystem, IOption option = null)
        {
            if (parentFileSystem is IFileSystemMount mountable) return mountable.ListMountPoints(option);
            throw new NotSupportedException(nameof(ListMountPoints));
        }

    }

    /// <summary><see cref="IMountOption"/> operations.</summary>
    public class MountOptionOperations : IOptionFlattenOperation, IOptionIntersectionOperation, IOptionUnionOperation
    {
        /// <summary>The option type that this class has operations for.</summary>
        public Type OptionType => typeof(IMountOption);
        /// <summary>Flatten to simpler instance.</summary>
        public IOption Flatten(IOption o) => o is IMountOption c ? o is MountOption ? /*already flattened*/o : /*new instance*/new MountOption(c.CanMount, c.CanUnmount, c.CanListMountPoints) : throw new InvalidCastException($"{typeof(IMountOption)} expected.");
        /// <summary>Intersection of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IOption Intersection(IOption o1, IOption o2) => o1 is IMountOption c1 && o2 is IMountOption c2 ? new MountOption(c1.CanMount || c1.CanMount, c1.CanUnmount || c2.CanUnmount, c1.CanListMountPoints || c2.CanListMountPoints) : throw new InvalidCastException($"{typeof(IMountOption)} expected.");
        /// <summary>Union of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IOption Union(IOption o1, IOption o2) => o1 is IMountOption c1 && o2 is IMountOption c2 ? new MountOption(c1.CanMount && c1.CanMount, c1.CanUnmount && c2.CanUnmount, c1.CanListMountPoints && c2.CanListMountPoints) : throw new InvalidCastException($"{typeof(IMountOption)} expected.");
    }

    /// <summary><see cref="ISubPathOption"/> operations.</summary>
    public class SubPathOptionOperations : IOptionFlattenOperation, IOptionIntersectionOperation, IOptionUnionOperation
    {
        /// <summary>The option type that this class has operations for.</summary>
        public Type OptionType => typeof(ISubPathOption);
        /// <summary>Flatten to simpler instance.</summary>
        public IOption Flatten(IOption o) => o is ISubPathOption b ? o is SubPathOption ? /*already flattened*/o : /*new instance*/new SubPathOption(b.SubPath) : throw new InvalidCastException($"{typeof(ISubPathOption)} expected.");
        /// <summary>Intersection of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IOption Intersection(IOption o1, IOption o2) => o1 is ISubPathOption c1 && o2 is ISubPathOption c2 ?
            (c1 != null && c2 == null ? new SubPathOption(c1.SubPath) :
             c1 == null && c2 != null ? new SubPathOption(c2.SubPath) :
             c1 != null && c2 != null && c1.SubPath == c1.SubPath ? new SubPathOption(c1.SubPath) :
             c1 == null && c2 == null ? (IOption) null: 
             throw new FileSystemExceptionOptionOperationNotSupported(null, null, o1, typeof(ISubPathOption), typeof(IOptionIntersectionOperation))) : 
            throw new InvalidCastException($"{typeof(IMountOption)} expected.");
        /// <summary>Union of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IOption Union(IOption o1, IOption o2) => o1 is ISubPathOption c1 && o2 is ISubPathOption c2 ?
            (c1 != null && c2 == null ? new SubPathOption(c1.SubPath) :
             c1 == null && c2 != null ? new SubPathOption(c2.SubPath) :
             c1 != null && c2 != null && c1.SubPath == c1.SubPath ? new SubPathOption(c1.SubPath) :
             c1 == null && c2 == null ? (IOption)null :
             throw new FileSystemExceptionOptionOperationNotSupported(null, null, o1, typeof(ISubPathOption), typeof(IOptionIntersectionOperation))) :
            throw new InvalidCastException($"{typeof(IMountOption)} expected.");
    }

    /// <summary>Option for mount path. Use with decorator.</summary>
    public class SubPathOption : ISubPathOption
    {
        internal static ISubPathOption noSubPath = new SubPathOption(null);

        /// <summary>No mount path.</summary>
        public static IOption NoSubPath => noSubPath;

        /// <summary>Mount path.</summary>
        public String SubPath { get; protected set; }

        /// <summary>Create option for mount path.</summary>
        public SubPathOption(string mountPath)
        {
            SubPath = mountPath;
        }

        /// <inheritdoc/>
        public override string ToString() => String.IsNullOrEmpty(SubPath) ? "" : "SubPath="+SubPath;
    }

    /// <summary>File system option for mount capabilities.</summary>
    public class MountOption : IMountOption
    {
        internal static IMountOption mount = new MountOption(true, true, true);
        internal static IMountOption noMount = new MountOption(false, false, false);

        /// <summary>Mount is allowed.</summary>
        public static IOption Mount => mount;
        /// <summary>Mount is not allowed</summary>
        public static IOption NoMount => noMount;

        /// <summary>Can filesystem mount other filesystems.</summary>
        public bool CanMount { get; protected set; }
        /// <summary>Is filesystem allowed to unmount a mount.</summary>
        public bool CanUnmount { get; protected set; }
        /// <summary>Is filesystem allowed to list mountpoints.</summary>
        public bool CanListMountPoints { get; protected set; }

        /// <summary>Create file system option for mount capabilities.</summary>
        public MountOption(bool canMount, bool canUnmount, bool canListMountPoints)
        {
            CanMount = canMount;
            CanUnmount = canUnmount;
            CanListMountPoints = canListMountPoints;
        }

        /// <inheritdoc/>
        public override string ToString() => (CanMount ? "CanMount" : "NoMount") + "," + (CanUnmount ? "CanUnmount" : "NoUnmount") + "," + (CanListMountPoints ? "CanListMountPoints" : "NoListMountPoints");
    }


}
