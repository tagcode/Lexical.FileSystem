// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Extension methods for <see cref="IEvent"/>.
    /// </summary>
    public static partial class EventExtensions
    {
        /// <summary>
        /// Get NewPath value of <paramref name="event"/> if it's <see cref="IRenameEvent"/>.
        /// </summary>
        /// <param name="event"></param>
        /// <returns>new path</returns>
        /// <exception cref="NotSupportedException">If <paramref name="event"/> is not <see cref="IRenameEvent"/></exception>
        public static String NewPath(this IEvent @event)
            => @event is IRenameEvent rename ? rename.NewPath : throw new NotSupportedException(nameof(NewPath));
    }

}
