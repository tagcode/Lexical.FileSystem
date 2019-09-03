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
    /// Root interface for file system interfaces. See sub-interfaces:
    /// <list type="bullet">
    ///     <item><see cref="IFileSystemOpen"/></item>
    ///     <item><see cref="IFileSystemCreateDirectory"/></item>
    ///     <item><see cref="IFileSystemBrowse"/></item>
    ///     <item><see cref="IFileSystemDelete"/></item>
    ///     <item><see cref="IFileSystemMove"/></item>
    ///     <item><see cref="IFileSystemObserve"/></item>
    /// </list>
    /// </summary>
    public interface IFileSystem
    {
        /// <summary>
        /// The capabilities of the implementing class. 
        /// 
        /// Note, that even if class is capable of certain operation, for example delete,
        /// the operation may be not supported for specific files, and the class may throw
        /// <see cref="NotSupportedException"/>.
        /// </summary>
        FileSystemCapabilities Capabilities { get; }
    }

    /// <summary>
    /// File system operation capabilities
    /// </summary>
    [Flags]
    public enum FileSystemCapabilities : UInt64
    {
        /// <summary>Can open file stream (<see cref="IFileSystemOpen"/>).</summary>
        Open = 1 << 0,
        /// <summary>Can open file for reading(<see cref="IFileSystemOpen"/>).</summary>
        Read = 1 << 1,
        /// <summary>Can open file for writing (<see cref="IFileSystemOpen"/>).</summary>
        Write = 1 << 2,
        /// <summary>Can open and create file (<see cref="IFileSystemOpen"/>).</summary>
        CreateFile = 1 << 3,
        /// <summary>Can create directory (<see cref="IFileSystemCreateDirectory"/>)</summary>
        CreateDirectory = 1 << 6,
        /// <summary>Can browse directories</summary>
        Browse = 1 << 8,
        /// <summary>Can test existance of files and directories</summary>
        Exists = 1 << 9,
        /// <summary>Can delete files and directories</summary>
        Delete = 1 << 10,
        /// <summary>Can move and rename files and directories.</summary>
        Move = 1 << 16,
        /// <summary>Can observe for directories and files</summary>
        Observe = 1 << 32,

        /// <summary>FileSystem uses case-sensitive filenames and paths. Note, if neither <see cref="CaseSensitive"/> or <see cref="CaseInsensitive"/> then sensitivity is not consistent or is unknown. If both are set, then sensitivity is inconsistent.</summary>
        CaseSensitive = 1 << 48,
        /// <summary>FileSystem uses case-insensitive filenames and paths. Note, if neither <see cref="CaseSensitive"/> or <see cref="CaseInsensitive"/> then sensitivity is not consistent or is unknown. If both are set, then sensitivity is inconsistent.</summary>
        CaseInsensitive = 1 << 49,

        /// <summary>Reserved for implementing classes to use for any purpose.</summary>
        Reserved0 = 1 << 56,
        /// <summary>Reserved for implementing classes to use for any purpose.</summary>
        Reserved1 = 1 << 57,
        /// <summary>Reserved for implementing classes to use for any purpose.</summary>
        Reserved2 = 1 << 58,
        /// <summary>Reserved for implementing classes to use for any purpose.</summary>
        Reserved3 = 1 << 59,
        /// <summary>Reserved for implementing classes to use for any purpose.</summary>
        Reserved4 = 1 << 60,
        /// <summary>Reserved for implementing classes to use for any purpose.</summary>
        Reserved5 = 1 << 61,
        /// <summary>Reserved for implementing classes to use for any purpose.</summary>
        Reserved6 = 1 << 62,
        /// <summary>Reserved for implementing classes to use for any purpose.</summary>
        Reserved7 = 1UL << 63
    }
    // </doc>

}
