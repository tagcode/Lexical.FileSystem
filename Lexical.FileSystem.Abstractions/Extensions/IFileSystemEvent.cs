// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Extension methods for <see cref="IFileSystem"/>.
    /// </summary>
    public static partial class IFileSystemEventExtensions
    {
        /// <summary>
        /// Get NewPath value of <paramref name="event"/> if it's <see cref="IFileSystemEventRename"/>.
        /// </summary>
        /// <param name="event"></param>
        /// <returns>new path</returns>
        /// <exception cref="NotSupportedException">If <paramref name="event"/> is not <see cref="IFileSystemEventRename"/></exception>
        public static String NewPath(this IFileSystemEvent @event)
            => @event is IFileSystemEventRename rename ? rename.NewPath : throw new NotSupportedException(nameof(NewPath));
    }

}
