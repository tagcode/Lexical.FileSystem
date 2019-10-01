// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           28.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Lexical.FileSystem
{
    // <doc>
    /// <summary>
    /// Interface for filesystem options. 
    /// 
    /// See sub-interfaces:
    /// <list type="bullet">
    ///     <item><see cref="IFileSystemOptionOpen"/></item>
    ///     <item><see cref="IFileSystemOptionObserve"/></item>
    ///     <item><see cref="IFileSystemOptionMove"/></item>
    ///     <item><see cref="IFileSystemOptionBrowse"/></item>
    ///     <item><see cref="IFileSystemOptionCreateDirectory"/></item>
    ///     <item><see cref="IFileSystemOptionDelete"/></item>
    ///     <item><see cref="IFileSystemOptionMount"/></item>
    ///     <item><see cref="IFileSystemOptionMountPath"/></item>
    ///     <item><see cref="IFileSystemOptionPath"/></item>
    ///     <item><see cref="IFileSystemOptionAdaptable"/></item>
    /// </list>
    /// 
    /// The options properties must be immutable in the implementing classes.
    /// </summary>
    public interface IFileSystemOption
    {
    }

    /// <summary>
    /// Interface for option classes that adapt to option types at runtime.
    /// Also enumerates supported <see cref="IFileSystemOption"/> option type interfaces.
    /// </summary>
    public interface IFileSystemOptionAdaptable : IFileSystemOption, IEnumerable<KeyValuePair<Type, IFileSystemOption>>
    {
        /// <summary>
        /// Get option with type interface.
        /// </summary>
        /// <param name="optionInterfaceType">Subtype of <see cref="IFileSystemOption"/></param>
        /// <returns>Option or null</returns>
        IFileSystemOption GetOption(Type optionInterfaceType);
    }

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
    // </doc>

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

        /// <summary>
        /// Get option as <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="option"></param>
        /// <returns>Option casted as <typeparamref name="T"/> or null</returns>
        public static T As<T>(this IFileSystemOption option) where T : IFileSystemOption
        {
            if (option is T casted) return casted;
            if (option is IFileSystemOptionAdaptable adaptable && adaptable.GetOption(typeof(T)) is T casted_) return casted_;
            return default;
        }
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
}
