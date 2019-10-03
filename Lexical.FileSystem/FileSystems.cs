// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           23.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Decoration;
using Lexical.FileSystem.Internal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Facade for services and extension methods.
    /// </summary>
    public static partial class FileSystems
    {
        /// <summary>
        /// Concatenate <paramref name="filesystem"/> and <paramref name="anotherFileSystem"/> into composition filesystem.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="anotherFileSystem"></param>
        /// <returns></returns>
        public static FileSystemDecoration Concat(this IFileSystem filesystem, IFileSystem anotherFileSystem)
            => new FileSystemDecoration(filesystem, anotherFileSystem);

        /// <summary>
        /// Concatenate <paramref name="filesystem"/> and <paramref name="otherFileSystems"/> into composition filesystem.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="otherFileSystems"></param>
        /// <returns></returns>
        public static FileSystemDecoration Concat(this IFileSystem filesystem, params IFileSystem[] otherFileSystems)
            => new FileSystemDecoration(Enumerable.Repeat(filesystem, 1).Concat(otherFileSystems));

        /// <summary>
        /// Concatenate <paramref name="filesystem"/> and <paramref name="otherFileSystems"/> into composition filesystem.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="otherFileSystems"></param>
        /// <returns></returns>
        public static FileSystemDecoration Concat(this IFileSystem filesystem, IEnumerable<IFileSystem> otherFileSystems)
            => new FileSystemDecoration(Enumerable.Repeat(filesystem, 1).Concat(otherFileSystems));

        /// <summary>
        /// Concatenate <paramref name="filesystems"/> into a composition filesystem.
        /// </summary>
        /// <param name="filesystems"></param>
        /// <returns></returns>
        public static FileSystemDecoration Concat(params IFileSystem[] filesystems)
            => new FileSystemDecoration(filesystems);

        /// <summary>
        /// Concatenate <paramref name="filesystems"/> into a composition filesystem.
        /// </summary>
        /// <param name="filesystems"></param>
        /// <returns></returns>
        public static FileSystemDecoration Concat(IEnumerable<IFileSystem> filesystems)
            => new FileSystemDecoration(filesystems);

        /// <summary>
        /// Creates a new filesystem decoration that reduces the permissions of <paramref name="filesystem"/> by 
        /// intersecting <paramref name="filesystem"/>'s options with <paramref name="option"/>.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public static FileSystemDecoration Decorate(this IFileSystem filesystem, IFileSystemOption option)
            => new FileSystemDecoration(filesystem, option);

        /// <summary>
        /// Creates a new filesystem decoration that reduces the permissions of <paramref name="filesystem"/> to readonly.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <returns></returns>
        public static FileSystemDecoration AsReadOnly(this IFileSystem filesystem)
            => new FileSystemDecoration(filesystem, FileSystemOption.ReadOnly);

    }
}
