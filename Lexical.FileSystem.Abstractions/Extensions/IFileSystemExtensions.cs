// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
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
        /// Get case sensitivity.
        /// <param name="filesystemOption"></param>
        /// </summary>
        /// <returns>mount path or null</returns>
        public static FileSystemCaseSensitivity CaseSensitivity(this IOption filesystemOption)
            => filesystemOption.AsOption<IPathInfo>() is IPathInfo op ? op.CaseSensitivity : FileSystemCaseSensitivity.Unknown;

        /// <summary>
        /// Get option for Filesystem allows empty string "" directory names.
        /// <param name="filesystemOption"></param>
        /// </summary>
        /// <returns>mount path or null</returns>
        public static bool EmptyDirectoryName(this IOption filesystemOption)
            => filesystemOption.AsOption<IPathInfo>() is IPathInfo op ? op.EmptyDirectoryName : false;

    }

    /// <summary><see cref="IPathInfo"/> operations.</summary>
    public class FileSystemOptionOperationPath : IOptionFlattenOperation, IOptionIntersectionOperation, IOptionUnionOperation
    {
        /// <summary>The option type that this class has operations for.</summary>
        public Type OptionType => typeof(IPathInfo);
        /// <summary>Flatten to simpler instance.</summary>
        public IOption Flatten(IOption o) => o is IPathInfo b ? o is FileSystemOptionPath ? /*already flattened*/o : /*new instance*/new FileSystemOptionPath(b.CaseSensitivity, b.EmptyDirectoryName) : throw new InvalidCastException($"{typeof(IPathInfo)} expected.");
        /// <summary>Intersection of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IOption Intersection(IOption o1, IOption o2) => o1 is IPathInfo b1 && o2 is IPathInfo b2 ? new FileSystemOptionPath(b1.CaseSensitivity & b2.CaseSensitivity, b1.EmptyDirectoryName && b2.EmptyDirectoryName) : throw new InvalidCastException($"{typeof(IPathInfo)} expected.");
        /// <summary>Union of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IOption Union(IOption o1, IOption o2) => o1 is IPathInfo b1 && o2 is IPathInfo b2 ? new FileSystemOptionPath(b1.CaseSensitivity | b2.CaseSensitivity, b1.EmptyDirectoryName || b2.EmptyDirectoryName) : throw new InvalidCastException($"{typeof(IPathInfo)} expected.");
    }

    /// <summary>Path related options</summary>
    public class FileSystemOptionPath : IPathInfo
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
