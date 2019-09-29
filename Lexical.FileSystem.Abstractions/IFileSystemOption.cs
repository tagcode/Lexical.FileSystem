using System;
using System.Collections.Generic;
using System.Text;

namespace Lexical.FileSystem
{
    // <doc>
    /// <summary>
    /// Base interface for filesystem options.
    /// 
    /// See sub-interfaces:
    /// <list type="bullet">
    ///     <item><see cref="IFileSystemOptionRoot"/></item>
    ///     <item><see cref="IFileSystemOptionPath"/></item>
    ///     <item><see cref="IFileSystemOptionBrowse"/></item>
    ///     <item><see cref="IFileSystemOptionCreateDirectory"/></item>
    ///     <item><see cref="IFileSystemOptionDelete"/></item>
    ///     <item><see cref="IFileSystemOptionEventDispatch"/></item>
    ///     <item><see cref="IFileSystemOptionMount"/></item>
    ///     <item><see cref="IFileSystemOptionMove"/></item>
    ///     <item><see cref="IFileSystemOptionObserve"/></item>
    ///     <item><see cref="IFileSystemOptionOpen"/></item>
    /// </list>
    /// </summary>
    public interface IFileSystemOption
    {
    }

    /// <summary>Option for root path.</summary>
    public interface IFileSystemOptionRoot : IFileSystemOption
    {
        /// <summary>Root path within filesystem.</summary>
        String Root { get; }
    }
    // </doc>
}
