// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           12.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------

using System;

namespace Lexical.FileSystem
{
    // <doc>
    /// <summary>
    /// File system that is actually a decoration.
    /// </summary>
    public interface IFileSystemMount : IFileSystem
    {
        /// <summary>
        /// Mount another filesystem.
        /// 
        /// Returns a handle. If handle is disposed the mount is unmounted.
        /// </summary>
        /// <param name="parentPath">Path in parent <see cref="IFileSystemMount"/> to mount</param>
        /// <param name="fileSystem">Mounted filesystem</param>
        /// <param name="subpath">(optional) subpath in <paramref name="fileSystem"/></param>
        /// <returns>handle for unmounting</returns>
        /// <exception cref="NotSupportedException">If mount is not supported</exception>
        IFileSystemMountHandle Mount(string parentPath, IFileSystem fileSystem, string subpath = null);
    }

    /// <summary>
    /// Handle for a unmounting. Dispose the handle to unmount.
    /// </summary>
    public interface IFileSystemMountHandle : IDisposable
    {
    }
    // </doc>

    /// <summary>
    /// Extension methods for <see cref="IFileSystem"/>.
    /// </summary>
    public static partial class IFileSystemExtensions
    {
        /// <summary>
        /// Mount another filesystem.
        /// </summary>
        /// <param name="parentFileSystem"></param>
        /// <param name="parentPath">Path in parent <see cref="IFileSystemMount"/> to mount</param>
        /// <param name="fileSystem">Mounted filesystem</param>
        /// <param name="subpath">(optional) subpath in <paramref name="fileSystem"/></param>
        /// <returns>handle that can be disposed to unmount</returns>
        /// <exception cref="NotSupportedException">If mount is not supported</exception>
        public static IFileSystemMountHandle Mount(this IFileSystem parentFileSystem, string parentPath, IFileSystem fileSystem, string subpath = null)
        {
            if (parentFileSystem is IFileSystemMount mountable)
                return mountable.Mount(parentPath, fileSystem, subpath);
            throw new NotSupportedException();
        }
    }

}
