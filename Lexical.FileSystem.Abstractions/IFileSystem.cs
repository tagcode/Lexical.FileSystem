// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;

namespace Lexical.FileSystem
{
    // <doc>
    /// <summary>
    /// Root interface for file system interfaces. 
    /// 
    /// See sub-interfaces:
    /// <list type="bullet">
    ///     <item><see cref="IFileSystemOpen"/></item>
    ///     <item><see cref="IFileSystemCreateDirectory"/></item>
    ///     <item><see cref="IFileSystemBrowse"/></item>
    ///     <item><see cref="IFileSystemDelete"/></item>
    ///     <item><see cref="IFileSystemMove"/></item>
    ///     <item><see cref="IFileSystemObserve"/></item>
    ///     <item><see cref="IFileSystemDisposable"/></item>
    /// </list>
    /// </summary>
    public interface IFileSystem : IFileSystemOption
    {
    }
    // </doc>

    /// <summary>
    /// Signals that the filesystem is also disposable.
    /// 
    /// Used when returning filesystem from methods to signal disposability.
    /// </summary>
    public interface IFileSystemDisposable : IFileSystem, IDisposable { }


    /// <summary>Path related options</summary>
    [Operations(typeof(FileSystemOptionOperationPath))]
    public interface IFileSystemOptionPath : IFileSystemOption
    {
        /// <summary>Case sensitivity</summary>
        FileSystemCaseSensitivity CaseSensitivity { get; }
        /// <summary>Filesystem allows empty string "" directory names. The value of this property excludes the default empty "" root path.</summary>
        bool EmptyDirectoryName { get; }
    }

    /// <summary>Knolwedge about path name case sensitivity</summary>
    [Flags]
    public enum FileSystemCaseSensitivity
    {
        /// <summary>Unknown</summary>
        Unknown = 0,
        /// <summary>Path names are case-sensitive</summary>
        CaseSensitive = 1,
        /// <summary>Path names are case-insensitive</summary>
        CaseInsensitive = 2,
        /// <summary>Some parts are sensitive, some insensitive</summary>
        Inconsistent = 3
    }

    /// <summary>
    /// Extension methods for <see cref="IFileSystem"/>.
    /// </summary>
    public static partial class IFileSystemExtensions
    {
        /// <summary>
        /// Get case sensitivity.
        /// <param name="filesystemOption"></param>
        /// </summary>
        /// <returns>mount path or null</returns>
        public static FileSystemCaseSensitivity CaseSensitivity(this IFileSystemOption filesystemOption)
            => filesystemOption.As<IFileSystemOptionPath>() is IFileSystemOptionPath op ? op.CaseSensitivity : FileSystemCaseSensitivity.Unknown;

        /// <summary>
        /// Get option for Filesystem allows empty string "" directory names.
        /// <param name="filesystemOption"></param>
        /// </summary>
        /// <returns>mount path or null</returns>
        public static bool EmptyDirectoryName(this IFileSystemOption filesystemOption)
            => filesystemOption.As<IFileSystemOptionPath>() is IFileSystemOptionPath op ? op.EmptyDirectoryName : false;

    }

    /// <summary><see cref="IFileSystemOptionPath"/> operations.</summary>
    public class FileSystemOptionOperationPath : IFileSystemOptionOperationFlatten, IFileSystemOptionOperationIntersection, IFileSystemOptionOperationUnion
    {
        /// <summary>The option type that this class has operations for.</summary>
        public Type OptionType => typeof(IFileSystemOptionPath);
        /// <summary>Flatten to simpler instance.</summary>
        public IFileSystemOption Flatten(IFileSystemOption o) => o is IFileSystemOptionPath b ? o is FileSystemOptionPath ? /*already flattened*/o : /*new instance*/new FileSystemOptionPath(b.CaseSensitivity, b.EmptyDirectoryName) : throw new InvalidCastException($"{typeof(IFileSystemOptionPath)} expected.");
        /// <summary>Intersection of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IFileSystemOption Intersection(IFileSystemOption o1, IFileSystemOption o2) => o1 is IFileSystemOptionPath b1 && o2 is IFileSystemOptionPath b2 ? new FileSystemOptionPath(b1.CaseSensitivity & b2.CaseSensitivity, b1.EmptyDirectoryName && b2.EmptyDirectoryName) : throw new InvalidCastException($"{typeof(IFileSystemOptionPath)} expected.");
        /// <summary>Union of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IFileSystemOption Union(IFileSystemOption o1, IFileSystemOption o2) => o1 is IFileSystemOptionPath b1 && o2 is IFileSystemOptionPath b2 ? new FileSystemOptionPath(b1.CaseSensitivity | b2.CaseSensitivity, b1.EmptyDirectoryName || b2.EmptyDirectoryName) : throw new InvalidCastException($"{typeof(IFileSystemOptionPath)} expected.");
    }

    /// <summary>Path related options</summary>
    public class FileSystemOptionPath : IFileSystemOptionPath
    {
        /// <summary>Case sensitivity</summary>
        public FileSystemCaseSensitivity CaseSensitivity { get; protected set; }
        /// <summary>Filesystem allows empty string "" directory names.</summary>
        public bool EmptyDirectoryName { get; protected set; }

        /// <summary>Create path related options</summary>
        public FileSystemOptionPath(FileSystemCaseSensitivity caseSensitivity, bool emptyDirectoryName)
        {
            this.CaseSensitivity = caseSensitivity;
            this.EmptyDirectoryName = emptyDirectoryName;
        }
    }
}
