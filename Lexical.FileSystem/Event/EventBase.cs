// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           9.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Base implementation to <see cref="IEvent"/> classes.
    /// 
    /// See sub-classes:
    /// <list type="bullet">
    ///     <item><see cref="RenameEvent"/></item>
    ///     <item><see cref="CreateEvent"/></item>
    ///     <item><see cref="ChangeEvent"/></item>
    ///     <item><see cref="DeleteEvent"/></item>
    ///     <item><see cref="ErrorEvent"/></item>
    ///     <item><see cref="StartEvent"/></item>
    /// </list>
    /// </summary>
    public abstract class EventBase : IEvent
    {
        /// <summary>
        /// The filesystem observer that sent the event.
        /// </summary>
        public virtual IFileSystemObserver Observer { get; protected set; }

        /// <summary>
        /// The time the event occured, or approximation if not exactly known.
        /// </summary>
        public virtual DateTimeOffset EventTime { get; protected set; }

        /// <summary>
        /// (optional) Affected entry if applicable.
        /// </summary>
        public virtual string Path { get; protected set; }

        /// <summary>
        /// Create event.
        /// </summary>
        /// <param name="observer"></param>
        /// <param name="eventTime"></param>
        /// <param name="path"></param>
        protected EventBase(IFileSystemObserver observer, DateTimeOffset eventTime, string path)
        {
            Observer = observer;
            EventTime = eventTime;
            Path = path;
        }

        /// <summary>Print info</summary>
        /// <returns>Info</returns>
        public override string ToString()
            => Path == null ? $"Event({Observer?.FileSystem}, {EventTime})" : $"{GetType().Name}({Observer?.FileSystem}, {EventTime}, {Path})";
    }
}
