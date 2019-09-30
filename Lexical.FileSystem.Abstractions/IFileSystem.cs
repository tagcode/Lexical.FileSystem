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

}
