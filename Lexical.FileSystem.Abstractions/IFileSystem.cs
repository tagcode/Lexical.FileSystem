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
    ///     <item><see cref="IFileSystemReference"/></item>
    /// </list>
    /// </summary>
    public interface IFileSystem
    {
        /// <summary>
        /// Features of the file-system.
        /// </summary>
        FileSystemFeatures Features { get; }
    }

    /// <summary>
    /// File system operation capabilities
    /// </summary>
    [Flags]
    public enum FileSystemFeatures : UInt64
    {
        /// <summary></summary>
        None = 0UL,

        /// <summary>Some or all files use case-sensitive filenames. Note, if neither <see cref="CaseSensitive"/> or <see cref="CaseInsensitive"/> then sensitivity is not consistent or is unknown. If both are set, then sensitivity is inconsistent.</summary>
        CaseSensitive = 1UL << 48,
        /// <summary>Some or all files use case-insensitive filenames. Note, if neither <see cref="CaseSensitive"/> or <see cref="CaseInsensitive"/> then sensitivity is not consistent or is unknown. If both are set, then sensitivity is inconsistent.</summary>
        CaseInsensitive = 1UL << 49,

        /// <summary>Reserved for implementing classes to use for any purpose.</summary>
        Reserved0 = 1UL << 56,
        /// <summary>Reserved for implementing classes to use for any purpose.</summary>
        Reserved1 = 1UL << 57,
        /// <summary>Reserved for implementing classes to use for any purpose.</summary>
        Reserved2 = 1UL << 58,
        /// <summary>Reserved for implementing classes to use for any purpose.</summary>
        Reserved3 = 1UL << 59,
        /// <summary>Reserved for implementing classes to use for any purpose.</summary>
        Reserved4 = 1UL << 60,
        /// <summary>Reserved for implementing classes to use for any purpose.</summary>
        Reserved5 = 1UL << 61,
        /// <summary>Reserved for implementing classes to use for any purpose.</summary>
        Reserved6 = 1UL << 62,
        /// <summary>Reserved for implementing classes to use for any purpose.</summary>
        Reserved7 = 1UL << 63
    }

    /// <summary>
    /// Signals that the file-system is also disposable.
    /// 
    /// Used when returning file-system from methods to signal disposability.
    /// </summary>
    public interface IDisposableFileSystem : IFileSystem, IDisposable { }
    // </doc>

}
