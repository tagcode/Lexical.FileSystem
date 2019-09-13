// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           9.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;

namespace Lexical.FileSystem
{
    /// <summary>
    /// File entry event.
    /// 
    /// See sub-classes:
    /// <list type="bullet">
    ///     <item><see cref="FileSystemEventRename"/></item>
    ///     <item><see cref="FileSystemEventCreate"/></item>
    ///     <item><see cref="FileSystemEventChange"/></item>
    ///     <item><see cref="FileSystemEventDelete"/></item>
    ///     <item><see cref="FileSystemEventError"/></item>
    /// </list>
    /// </summary>
    public abstract class FileSystemEvent : IFileSystemEvent
    {
        /// <summary>
        /// The file-system observer that sent the event.
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
        protected FileSystemEvent(IFileSystemObserver observer, DateTimeOffset eventTime, string path)
        {
            Observer = observer;
            EventTime = eventTime;
            Path = path;
        }

        /// <summary>Print info</summary>
        /// <returns>Info</returns>
        public override string ToString()
            => Path == null ? $"{GetType().Name}({Observer}, {EventTime})" : $"{GetType().Name}({Observer}, {EventTime}, {Path})";
    }

    /// <summary>
    /// File renamed event.
    /// </summary>
    public class FileSystemEventRename : FileSystemEvent, IFileSystemEventRename
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
        public FileSystemEventRename(IFileSystemObserver observer, DateTimeOffset eventTime, string oldPath, string newPath) : base(observer, eventTime, oldPath)
        {
            NewPath = newPath;
        }

        /// <summary>Print info</summary>
        /// <returns>Info</returns>
        public override string ToString()
            => $"Rename({Observer}, {EventTime}, OldPath={OldPath}, NewPath={NewPath})";
    }

    /// <summary>
    /// File created event
    /// </summary>
    public class FileSystemEventCreate : FileSystemEvent, IFileSystemEventCreate
    {
        /// <summary>
        /// Create create event.
        /// </summary>
        /// <param name="observer"></param>
        /// <param name="eventTime"></param>
        /// <param name="path"></param>
        public FileSystemEventCreate(IFileSystemObserver observer, DateTimeOffset eventTime, string path) : base(observer, eventTime, path)
        {
        }

        /// <summary>Print info</summary>
        /// <returns>Info</returns>
        public override string ToString()
            => Path == null ? $"Create({Observer}, {EventTime})" : $"Create({Observer}, {EventTime}, {Path})";
    }

    /// <summary>
    /// File contents changed event
    /// </summary>
    public class FileSystemEventChange : FileSystemEvent, IFileSystemEventChange
    {
        /// <summary>
        /// Create file contents changed event.
        /// </summary>
        /// <param name="observer"></param>
        /// <param name="eventTime"></param>
        /// <param name="path"></param>
        public FileSystemEventChange(IFileSystemObserver observer, DateTimeOffset eventTime, string path) : base(observer, eventTime, path)
        {
        }

        /// <summary>Print info</summary>
        /// <returns>Info</returns>
        public override string ToString()
            => Path == null ? $"Change({Observer}, {EventTime})" : $"Change({Observer}, {EventTime}, {Path})";
    }

    /// <summary>
    /// File deleted event
    /// </summary>
    public class FileSystemEventDelete : FileSystemEvent, IFileSystemEventDelete
    {
        /// <summary>
        /// Create file deleted event.
        /// </summary>
        /// <param name="observer"></param>
        /// <param name="eventTime"></param>
        /// <param name="path"></param>
        public FileSystemEventDelete(IFileSystemObserver observer, DateTimeOffset eventTime, string path) : base(observer, eventTime, path)
        {
        }

        /// <summary>Print info</summary>
        /// <returns>Info</returns>
        public override string ToString()
            => Path == null ? $"Delete({Observer}, {EventTime})" : $"Delete({Observer}, {EventTime}, {Path})";
    }

    /// <summary>
    /// File-system error event
    /// </summary>
    public class FileSystemEventError : FileSystemEvent, IFileSystemEventError
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
        public FileSystemEventError(IFileSystemObserver observer, DateTimeOffset eventTime, Exception error, string path) : base(observer, eventTime, path)
        {
            this.Error = error;
        }

        /// <summary>Print info</summary>
        /// <returns>Info</returns>
        public override string ToString()
            => Path == null ? $"Error({Observer}, {EventTime})" : $"Error({Observer}, {EventTime}, {Path})";
    }

}
