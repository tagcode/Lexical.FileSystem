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
    // <IFileSystemOptionPath>
    public interface IFileSystemOptionPath : IFileSystemOption
    {
        /// <summary>Case sensitivity</summary>
        FileSystemCaseSensitivity CaseSensitivity { get; }
        /// <summary>Filesystem allows empty string "" directory names. The value of this property excludes the default empty "" root path.</summary>
        bool EmptyDirectoryName { get; }
    }
    // </IFileSystemOptionPath>

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
}
