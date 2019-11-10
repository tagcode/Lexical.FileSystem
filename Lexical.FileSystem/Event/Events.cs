// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           9.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;

namespace Lexical.FileSystem
{
    /// <summary>
    /// File renamed event.
    /// </summary>
    public class RenameEvent : EventBase, IRenameEvent
    {
        /// <summary>
        /// The affected file or directory.
        /// 
        /// Path is relative to the <see cref="FileSystem"/>'s root.
        /// 
        /// Directory separator is "/". Root path doesn't use separator.
        /// 
        /// Example: "dir/file.ext"
        /// </summary>
        public virtual String OldPath => base.Path;

        /// <summary>
        /// The new file or directory path.
        /// </summary>
        public virtual String NewPath { get; protected set; }

        /// <summary>
        /// Create rename event.
        /// </summary>
        /// <param name="observer"></param>
        /// <param name="eventTime"></param>
        /// <param name="oldPath"></param>
        /// <param name="newPath"></param>
        public RenameEvent(IFileSystemObserver observer, DateTimeOffset eventTime, string oldPath, string newPath) : base(observer, eventTime, oldPath)
        {
            NewPath = newPath;
        }

        /// <summary>Print info</summary>
        /// <returns>Info</returns>
        public override string ToString()
            => $"Rename({Observer?.FileSystem}, {EventTime}, OldPath={OldPath}, NewPath={NewPath})";
    }

    /// <summary>
    /// File created event
    /// </summary>
    public class CreateEvent : EventBase, ICreateEvent
    {
        /// <summary>
        /// Create create event.
        /// </summary>
        /// <param name="observer"></param>
        /// <param name="eventTime"></param>
        /// <param name="path"></param>
        public CreateEvent(IFileSystemObserver observer, DateTimeOffset eventTime, string path) : base(observer, eventTime, path)
        {
        }

        /// <summary>Print info</summary>
        /// <returns>Info</returns>
        public override string ToString()
            => Path == null ? $"Create({Observer?.FileSystem}, {EventTime})" : $"Create({Observer?.FileSystem}, {EventTime}, {Path})";
    }

    /// <summary>
    /// File contents changed event
    /// </summary>
    public class ChangeEvent : EventBase, IChangeEvent
    {
        /// <summary>
        /// Create file contents changed event.
        /// </summary>
        /// <param name="observer"></param>
        /// <param name="eventTime"></param>
        /// <param name="path"></param>
        public ChangeEvent(IFileSystemObserver observer, DateTimeOffset eventTime, string path) : base(observer, eventTime, path)
        {
        }

        /// <summary>Print info</summary>
        /// <returns>Info</returns>
        public override string ToString()
            => Path == null ? $"Change({Observer?.FileSystem}, {EventTime})" : $"Change({Observer?.FileSystem}, {EventTime}, {Path})";
    }

    /// <summary>
    /// File deleted event
    /// </summary>
    public class DeleteEvent : EventBase, IDeleteEvent
    {
        /// <summary>
        /// Create file deleted event.
        /// </summary>
        /// <param name="observer"></param>
        /// <param name="eventTime"></param>
        /// <param name="path"></param>
        public DeleteEvent(IFileSystemObserver observer, DateTimeOffset eventTime, string path) : base(observer, eventTime, path)
        {
        }

        /// <summary>Print info</summary>
        /// <returns>Info</returns>
        public override string ToString()
            => Path == null ? $"Delete({Observer?.FileSystem}, {EventTime})" : $"Delete({Observer?.FileSystem}, {EventTime}, {Path})";
    }

    /// <summary>
    /// Filesystem error event
    /// </summary>
    public class ErrorEvent : EventBase, IErrorEvent
    {
        /// <summary>
        /// Error
        /// </summary>
        public virtual Exception Error { get; protected set; }

        /// <summary>
        /// Create Error event.
        /// </summary>
        /// <param name="observer"></param>
        /// <param name="eventTime"></param>
        /// <param name="error"></param>
        /// <param name="path">(optional)</param>
        public ErrorEvent(IFileSystemObserver observer, DateTimeOffset eventTime, Exception error, string path) : base(observer, eventTime, path)
        {
            this.Error = error;
        }

        /// <summary>Print info</summary>
        /// <returns>Info</returns>
        public override string ToString()
            => Path == null ? $"Error({Observer?.FileSystem}, {EventTime})" : $"Error({Observer?.FileSystem}, {EventTime}, {Path})";
    }

    /// <summary>
    /// Filesystem observe started event.
    /// </summary>
    public class StartEvent : EventBase, IStartEvent
    {
        /// <summary>
        /// Create Error event.
        /// </summary>
        /// <param name="observer"></param>
        /// <param name="eventTime"></param>
        public StartEvent(IFileSystemObserver observer, DateTimeOffset eventTime) : base(observer, eventTime, null)
        {
        }

        /// <summary>Print info</summary>
        /// <returns>Info</returns>
        public override string ToString()
            => $"Start({Observer?.FileSystem}, {EventTime})";
    }

}
