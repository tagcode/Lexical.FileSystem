// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           23.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Decoration;
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
        /// <returns><see cref="IFileSystem"/></returns>
        public static FileProviderSystem ToFileSystem(this IFileProvider fileProvider)
            => new FileProviderSystem(fileProvider, FileProviderSystem.Options.AllEnabled);

        /// <summary>
        /// Adapt <paramref name="fileProvider"/> to <see cref="IFileSystem"/>.
        /// 
        /// WARNING: The Observe implementation browses the subtree of the watched directory path in order to create delta of changes.
        /// </summary>
        /// <param name="fileProvider"></param>
        /// <param name="canBrowse"></param>
        /// <param name="canObserve"></param>
        /// <param name="canOpen"></param>
        /// <returns><see cref="IFileSystem"/></returns>
        public static FileProviderSystem ToFileSystem(this IFileProvider fileProvider, bool canBrowse = true, bool canObserve = true, bool canOpen = true)
            => new FileProviderSystem(fileProvider, new FileProviderSystem.Options(canBrowse, canBrowse, canOpen, canOpen, false, false, canObserve));

        /// <summary>
        /// Adapt <paramref name="fileProvider"/> to <see cref="IFileSystem"/>.
        /// 
        /// WARNING: The Observe implementation browses the subtree of the watched directory path in order to create delta of changes.
        /// </summary>
        /// <param name="fileProvider"></param>
        /// <param name="option"></param>
        /// <returns><see cref="IFileSystem"/></returns>
        public static FileProviderSystem ToFileSystem(this IFileProvider fileProvider, IFileSystemOption option)
            => new FileProviderSystem(fileProvider, option);

        /// <summary>
        /// Adapt <paramref name="filesystem"/> to <see cref="IFileProvider"/>.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <returns><see cref="IFileProvider"/></returns>
        public static FileSystemProvider ToFileProvider(this IFileSystem filesystem)
            => new FileSystemProvider(filesystem);
    }

}
