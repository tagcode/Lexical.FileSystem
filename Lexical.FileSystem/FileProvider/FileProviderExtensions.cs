// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           23.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.FileProvider;
using Microsoft.Extensions.FileProviders;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Extension methods for <see cref="IFileProvider"/>.
    /// </summary>
    public static partial class FileProviderExtensions
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
