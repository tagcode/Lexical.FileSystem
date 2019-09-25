// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           23.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Adapter;
using Lexical.FileSystem.Internal;
using Microsoft.Extensions.FileProviders;
using System.Collections.Generic;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Facade for services and extension methods.
    /// </summary>
    public static partial class FileSystems
    {
        /// <summary>
        /// Join <paramref name="fileSystem"/> and <paramref name="anotherFileSystem"/> into composition filesystem.
        /// </summary>
        /// <param name="fileSystem"></param>
        /// <param name="anotherFileSystem"></param>
        /// <returns></returns>
        public static IFileSystem Join(this IFileSystem fileSystem, IFileSystem anotherFileSystem)
        {
            StructList12<IFileSystem> fileSystems = new StructList12<IFileSystem>();
            if (fileSystem is IEnumerable<IFileSystem> composition) foreach (IFileSystem fs in composition) fileSystems.AddIfNew(fs);
            else if (fileSystem != null) fileSystems.AddIfNew(fileSystem);

            if (anotherFileSystem is IEnumerable<IFileSystem> composition_) foreach (IFileSystem fs in composition_) fileSystems.AddIfNew(fs);
            else if (anotherFileSystem != null) fileSystems.AddIfNew(anotherFileSystem);
            return new FileSystemComposition(fileSystems.ToArray());
        }

        /// <summary>
        /// Join <paramref name="fileSystem"/> and <paramref name="otherFileSystems"/> into composition filesystem.
        /// </summary>
        /// <param name="fileSystem"></param>
        /// <param name="otherFileSystems"></param>
        /// <returns></returns>
        public static IFileSystem Join(this IFileSystem fileSystem, params IFileSystem[] otherFileSystems)
        {
            StructList12<IFileSystem> fileSystems = new StructList12<IFileSystem>();
            if (fileSystem is IEnumerable<IFileSystem> composition) foreach (IFileSystem fs in composition) fileSystems.AddIfNew(fs);
            else if (fileSystem != null) fileSystems.AddIfNew(fileSystem);

            foreach (IFileSystem otherFileSystem in otherFileSystems)
                if (otherFileSystem is IEnumerable<IFileSystem> composition_) foreach (IFileSystem fs in composition_) fileSystems.AddIfNew(fs);
                else if (otherFileSystem != null) fileSystems.AddIfNew(otherFileSystem);

            return new FileSystemComposition(fileSystems.ToArray());
        }

        /// <summary>
        /// Join <paramref name="fileSystem"/> and <paramref name="otherFileSystems"/> into composition filesystem.
        /// </summary>
        /// <param name="fileSystem"></param>
        /// <param name="otherFileSystems"></param>
        /// <returns></returns>
        public static IFileSystem Join(this IFileSystem fileSystem, IEnumerable<IFileSystem> otherFileSystems)
        {
            StructList12<IFileSystem> fileSystems = new StructList12<IFileSystem>();
            if (fileSystem is IEnumerable<IFileSystem> composition) foreach (IFileSystem fs in composition) fileSystems.AddIfNew(fs);
            else if (fileSystem != null) fileSystems.AddIfNew(fileSystem);

            foreach (IFileSystem otherFileSystem in otherFileSystems)
                if (otherFileSystem is IEnumerable<IFileSystem> composition_) foreach (IFileSystem fs in composition_) fileSystems.AddIfNew(fs);
                else if (otherFileSystem != null) fileSystems.AddIfNew(otherFileSystem);

            return new FileSystemComposition(fileSystems.ToArray());
        }

    }

    /// <summary>
    /// Extension methods for <see cref="IFileProvider"/>.
    /// </summary>
    public static partial class FileSystemExtensions
    {
        /// <summary>
        /// Adapt <paramref name="fileProvider"/> to <see cref="IFileSystem"/>.
        /// 
        /// WARNING: The Observe implementation browses the subtree of the watched directory path in order to create delta of changes.
        /// </summary>
        /// <param name="fileProvider"></param>
        /// <param name="subpath">(optional)</param>
        /// <param name="canBrowse"></param>
        /// <param name="canObserve"></param>
        /// <param name="canOpen"></param>
        /// <returns><see cref="IFileSystem"/></returns>
        public static IFileSystem ToFileSystem(this IFileProvider fileProvider, string subpath = null, bool canBrowse = true, bool canObserve = true, bool canOpen = true)
            => new FileProviderSystem(fileProvider, subpath, canBrowse, canObserve, canOpen);

        /// <summary>
        /// Adapt <paramref name="fileSystem"/> to <see cref="IFileProvider"/>.
        /// </summary>
        /// <param name="fileSystem"></param>
        /// <returns><see cref="IFileProvider"/></returns>
        public static IFileProvider ToFileProvider(this IFileSystem fileSystem)
            => new FileSystemProvider(fileSystem);
    }

}
