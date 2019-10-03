// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           23.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Decoration;
using Lexical.FileSystem.Internal;
using System.Collections.Generic;

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
        {
            StructList12<IFileSystem> filesystems = new StructList12<IFileSystem>();
            if (filesystem is IEnumerable<IFileSystem> composition) foreach (IFileSystem fs in composition) filesystems.AddIfNew(fs);
            else if (filesystem != null) filesystems.AddIfNew(filesystem);

            if (anotherFileSystem is IEnumerable<IFileSystem> composition_) foreach (IFileSystem fs in composition_) filesystems.AddIfNew(fs);
            else if (anotherFileSystem != null) filesystems.AddIfNew(anotherFileSystem);
            return new FileSystemDecoration(filesystems.ToArray());
        }

        /// <summary>
        /// Concatenate <paramref name="filesystem"/> and <paramref name="otherFileSystems"/> into composition filesystem.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="otherFileSystems"></param>
        /// <returns></returns>
        public static FileSystemDecoration Concat(this IFileSystem filesystem, params IFileSystem[] otherFileSystems)
        {
            StructList12<IFileSystem> filesystems = new StructList12<IFileSystem>();
            if (filesystem is IEnumerable<IFileSystem> composition) foreach (IFileSystem fs in composition) filesystems.AddIfNew(fs);
            else if (filesystem != null) filesystems.AddIfNew(filesystem);

            foreach (IFileSystem otherFileSystem in filesystems)
                if (otherFileSystem is IEnumerable<IFileSystem> composition_) foreach (IFileSystem fs in composition_) filesystems.AddIfNew(fs);
                else if (otherFileSystem != null) filesystems.AddIfNew(otherFileSystem);

            return new FileSystemDecoration(filesystems.ToArray());
        }

        /// <summary>
        /// Concatenate <paramref name="filesystem"/> and <paramref name="otherFileSystems"/> into composition filesystem.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="otherFileSystems"></param>
        /// <returns></returns>
        public static FileSystemDecoration Concat(this IFileSystem filesystem, IEnumerable<IFileSystem> otherFileSystems)
        {
            StructList12<IFileSystem> filesystems = new StructList12<IFileSystem>();
            if (filesystem is IEnumerable<IFileSystem> composition) foreach (IFileSystem fs in composition) filesystems.AddIfNew(fs);
            else if (filesystem != null) filesystems.AddIfNew(filesystem);

            foreach (IFileSystem otherFileSystem in otherFileSystems)
                if (otherFileSystem is IEnumerable<IFileSystem> composition_) foreach (IFileSystem fs in composition_) filesystems.AddIfNew(fs);
                else if (otherFileSystem != null) filesystems.AddIfNew(otherFileSystem);

            return new FileSystemDecoration(filesystems.ToArray());
        }

        /// <summary>
        /// Concatenate <paramref name="filesystems"/> into a composition filesystem.
        /// </summary>
        /// <param name="filesystems"></param>
        /// <returns></returns>
        public static FileSystemDecoration Concat(params IFileSystem[] filesystems)
        {
            StructList12<IFileSystem> filesystemList = new StructList12<IFileSystem>();

            foreach (IFileSystem otherFileSystem in filesystems)
                if (otherFileSystem is IEnumerable<IFileSystem> composition_) foreach (IFileSystem fs in composition_) filesystemList.AddIfNew(fs);
                else if (otherFileSystem != null) filesystemList.AddIfNew(otherFileSystem);

            return new FileSystemDecoration(filesystemList.ToArray());
        }

        /// <summary>
        /// Concatenate <paramref name="filesystems"/> into a composition filesystem.
        /// </summary>
        /// <param name="filesystems"></param>
        /// <returns></returns>
        public static FileSystemDecoration Concat(IEnumerable<IFileSystem> filesystems)
        {
            StructList12<IFileSystem> filesystemList = new StructList12<IFileSystem>();

            foreach (IFileSystem otherFileSystem in filesystems)
                if (otherFileSystem is IEnumerable<IFileSystem> composition_) foreach (IFileSystem fs in composition_) filesystemList.AddIfNew(fs);
                else if (otherFileSystem != null) filesystemList.AddIfNew(otherFileSystem);

            return new FileSystemDecoration(filesystemList.ToArray());
        }

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
