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
    ///     <item><see cref="IFileSystemOptionMountPath"/></item>
    ///     <item><see cref="IFileSystemOptionPath"/></item>
    ///     <item><see cref="IFileSystemOptionBrowse"/></item>
    ///     <item><see cref="IFileSystemOptionCreateDirectory"/></item>
    ///     <item><see cref="IFileSystemOptionDelete"/></item>
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

    /// <summary>Option for mount path. Used with <see cref="IFileSystemMountHandle"/></summary>
    public interface IFileSystemOptionMountPath : IFileSystemOption
    {
        /// <summary>Mount path.</summary>
        String MountPath { get; }
    }
    // </doc>

    /// <summary>
    /// Extension methods for <see cref="IFileSystem"/>.
    /// </summary>
    public static partial class IFileSystemExtensions
    {
        /// <summary>
        /// Get mount path option.
        /// <param name="filesystemOption"></param>
        /// </summary>
        /// <returns>mount path or null</returns>
        public static String MountPath(this IFileSystemOption filesystemOption)
            => filesystemOption is IFileSystemOptionMountPath mp ? mp.MountPath : null;

    }
}
