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
    /// 
    /// The options properties must be immutable in the implementing classes.
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

    /// <summary>Option for block size.</summary>
    public interface IFileSystemOptionBlockSize : IFileSystemOption
    {
        /// <summary>Block size of files.</summary>
        long BlockSize { get; }
    }

    // </doc>
}
