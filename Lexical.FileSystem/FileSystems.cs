// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           23.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
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
        /// Join <paramref name="filesystem"/> and <paramref name="anotherFileSystem"/> into composition filesystem.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="anotherFileSystem"></param>
        /// <returns></returns>
        public static IFileSystem Join(this IFileSystem filesystem, IFileSystem anotherFileSystem)
        {
            StructList12<IFileSystem> filesystems = new StructList12<IFileSystem>();
            if (filesystem is IEnumerable<IFileSystem> composition) foreach (IFileSystem fs in composition) filesystems.AddIfNew(fs);
            else if (filesystem != null) filesystems.AddIfNew(filesystem);

            if (anotherFileSystem is IEnumerable<IFileSystem> composition_) foreach (IFileSystem fs in composition_) filesystems.AddIfNew(fs);
            else if (anotherFileSystem != null) filesystems.AddIfNew(anotherFileSystem);
            return new FileSystemDecoration(filesystems.ToArray());
        }

        /// <summary>
        /// Join <paramref name="filesystem"/> and <paramref name="otherFileSystems"/> into composition filesystem.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="otherFileSystems"></param>
        /// <returns></returns>
        public static IFileSystem Join(this IFileSystem filesystem, params IFileSystem[] otherFileSystems)
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
        /// Join <paramref name="filesystem"/> and <paramref name="otherFileSystems"/> into composition filesystem.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="otherFileSystems"></param>
        /// <returns></returns>
        public static IFileSystem Join(this IFileSystem filesystem, IEnumerable<IFileSystem> otherFileSystems)
        {
            StructList12<IFileSystem> filesystems = new StructList12<IFileSystem>();
            if (filesystem is IEnumerable<IFileSystem> composition) foreach (IFileSystem fs in composition) filesystems.AddIfNew(fs);
            else if (filesystem != null) filesystems.AddIfNew(filesystem);

            foreach (IFileSystem otherFileSystem in otherFileSystems)
                if (otherFileSystem is IEnumerable<IFileSystem> composition_) foreach (IFileSystem fs in composition_) filesystems.AddIfNew(fs);
                else if (otherFileSystem != null) filesystems.AddIfNew(otherFileSystem);

            return new FileSystemDecoration(filesystems.ToArray());
        }

    }
}
