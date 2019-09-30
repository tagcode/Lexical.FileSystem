using System;
using System.Collections.Generic;
using System.Text;

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
    /// </list>
    /// 
    /// The options properties must be immutable in the implementing classes.
    /// </summary>
    public interface IFileSystemOption
    {
    }

    /// <summary>Knolwedge about path name case sensitivity</summary>
    [Flags]
    public enum FileSystemCaseSensitivity
    {
        /// <summary>Unknown.</summary>
        Unknown = 0,
        /// <summary>Path names are case-sensitive</summary>
        CaseSensitive = 1,
        /// <summary>Path names are case-insensitive</summary>
        CaseInsensitive = 2,
        /// <summary>Some parts are sensitive, some insensitive</summary>
        Inconsistent = 3
    }

    /// <summary>Path related options</summary>
    public interface IFileSystemOptionPath
    {
        /// <summary>Case sensitivity</summary>
        FileSystemCaseSensitivity CaseSensitivity { get; }
        /// <summary>Filesystem allows empty string "" directory names. The value of this property excludes the default empty "" root path.</summary>
        bool EmptyDirectoryName { get; }
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
            => filesystemOption is IFileSystemOptionPath op ? op.CaseSensitivity : FileSystemCaseSensitivity.Unknown;

        /// <summary>
        /// Get option for Filesystem allows empty string "" directory names.
        /// <param name="filesystemOption"></param>
        /// </summary>
        /// <returns>mount path or null</returns>
        public static bool EmptyDirectoryName(this IFileSystemOption filesystemOption)
            => filesystemOption is IFileSystemOptionPath op ? op.EmptyDirectoryName : false;

    }
}
