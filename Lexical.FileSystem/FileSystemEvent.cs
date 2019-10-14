// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           9.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Text;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Base implementation to <see cref="IFileSystemEvent"/> classes.
    /// 
    /// See sub-classes:
    /// <list type="bullet">
    ///     <item><see cref="FileSystemEventRename"/></item>
    ///     <item><see cref="FileSystemEventCreate"/></item>
    ///     <item><see cref="FileSystemEventChange"/></item>
    ///     <item><see cref="FileSystemEventDelete"/></item>
    ///     <item><see cref="FileSystemEventError"/></item>
    ///     <item><see cref="FileSystemEventStart"/></item>
    /// </list>
    /// </summary>
    public abstract class FileSystemEventBase : IFileSystemEvent
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
        protected FileSystemEventBase(IFileSystemObserver observer, DateTimeOffset eventTime, string path)
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

    /// <summary>
    /// File renamed event.
    /// </summary>
    public class FileSystemEventRename : FileSystemEventBase, IFileSystemEventRename
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
            => $"Rename({Observer?.FileSystem}, {EventTime}, OldPath={OldPath}, NewPath={NewPath})";
    }

    /// <summary>
    /// File created event
    /// </summary>
    public class FileSystemEventCreate : FileSystemEventBase, IFileSystemEventCreate
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
            => Path == null ? $"Create({Observer?.FileSystem}, {EventTime})" : $"Create({Observer?.FileSystem}, {EventTime}, {Path})";
    }

    /// <summary>
    /// File contents changed event
    /// </summary>
    public class FileSystemEventChange : FileSystemEventBase, IFileSystemEventChange
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
            => Path == null ? $"Change({Observer?.FileSystem}, {EventTime})" : $"Change({Observer?.FileSystem}, {EventTime}, {Path})";
    }

    /// <summary>
    /// File deleted event
    /// </summary>
    public class FileSystemEventDelete : FileSystemEventBase, IFileSystemEventDelete
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
            => Path == null ? $"Delete({Observer?.FileSystem}, {EventTime})" : $"Delete({Observer?.FileSystem}, {EventTime}, {Path})";
    }

    /// <summary>
    /// Filesystem error event
    /// </summary>
    public class FileSystemEventError : FileSystemEventBase, IFileSystemEventError
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
            => Path == null ? $"Error({Observer?.FileSystem}, {EventTime})" : $"Error({Observer?.FileSystem}, {EventTime}, {Path})";
    }

    /// <summary>
    /// Filesystem observe started event.
    /// </summary>
    public class FileSystemEventStart : FileSystemEventBase, IFileSystemEventStart
    {
        /// <summary>
        /// Create Error event.
        /// </summary>
        /// <param name="observer"></param>
        /// <param name="eventTime"></param>
        public FileSystemEventStart(IFileSystemObserver observer, DateTimeOffset eventTime) : base(observer, eventTime, null)
        {
        }

        /// <summary>Print info</summary>
        /// <returns>Info</returns>
        public override string ToString()
            => $"Start({Observer?.FileSystem}, {EventTime})";
    }

}
