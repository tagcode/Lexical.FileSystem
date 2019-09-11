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
    /// </summary>
    public abstract class FileSystemEvent : IFileSystemEvent
    {
        /// <summary>
        /// The file-system observer that sent the event.
        /// </summary>
        public IFileSystemObserver Observer { get; protected set; }

        /// <summary>
        /// The time the event occured, or approximation if not exactly known.
        /// </summary>
        public DateTimeOffset EventTime { get; protected set; }

        /// <summary>
        /// (optional) Affected entry if applicable.
        /// </summary>
        public string Path { get; protected set; }

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

        /// <summary>
        /// Print info
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => Path == null ? $"{GetType().Name}({Observer}, {EventTime})" : $"{GetType().Name}({Observer}, {EventTime}, {Path})";
    }

    /// <summary>
    /// File renamed event.
    /// </summary>
    public class FileSystemRenameEvent : FileSystemEvent, IFileSystemRenameEvent
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
        public String OldPath => base.Path;

        /// <summary>
        /// The new file or directory path.
        /// </summary>
        public String NewPath { get; protected set; }

        /// <summary>
        /// Create rename event.
        /// </summary>
        /// <param name="observer"></param>
        /// <param name="eventTime"></param>
        /// <param name="oldPath"></param>
        /// <param name="newPath"></param>
        public FileSystemRenameEvent(IFileSystemObserver observer, DateTimeOffset eventTime, string oldPath, string newPath) : base(observer, eventTime, oldPath)
        {
            NewPath = newPath;
        }

        /// <summary>
        /// Print info
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => $"{GetType().Name}({Observer}, {EventTime}, OldPath={OldPath}, NewPath={NewPath})";
    }

    /// <summary>
    /// File created event
    /// </summary>
    public class FileSystemCreateEvent : FileSystemEvent, IFileSystemCreateEvent
    {
        /// <summary>
        /// Create rename event.
        /// </summary>
        /// <param name="observer"></param>
        /// <param name="eventTime"></param>
        /// <param name="path"></param>
        public FileSystemCreateEvent(IFileSystemObserver observer, DateTimeOffset eventTime, string path) : base(observer, eventTime, path)
        {
        }
    }

    /// <summary>
    /// File contents changed event
    /// </summary>
    public class FileSystemChangeEvent : FileSystemEvent, IFileSystemChangeEvent
    {
        /// <summary>
        /// Create file contents changed event.
        /// </summary>
        /// <param name="observer"></param>
        /// <param name="eventTime"></param>
        /// <param name="path"></param>
        public FileSystemChangeEvent(IFileSystemObserver observer, DateTimeOffset eventTime, string path) : base(observer, eventTime, path)
        {
        }
    }

    /// <summary>
    /// File deleted event
    /// </summary>
    public class FileSystemDeleteEvent : FileSystemEvent, IFileSystemDeleteEvent
    {
        /// <summary>
        /// Create file deleted event.
        /// </summary>
        /// <param name="observer"></param>
        /// <param name="eventTime"></param>
        /// <param name="path"></param>
        public FileSystemDeleteEvent(IFileSystemObserver observer, DateTimeOffset eventTime, string path) : base(observer, eventTime, path)
        {
        }
    }

    /// <summary>
    /// File-system error event
    /// </summary>
    public class FileSystemErrorEvent : FileSystemEvent, IFileSystemErrorEvent
    {
        /// <summary>
        /// Error
        /// </summary>
        public Exception Error { get; protected set; }

        /// <summary>
        /// Create Error event.
        /// </summary>
        /// <param name="observer"></param>
        /// <param name="eventTime"></param>
        /// <param name="error"></param>
        /// <param name="path">(optional)</param>
        public FileSystemErrorEvent(IFileSystemObserver observer, DateTimeOffset eventTime, Exception error, string path) : base(observer, eventTime, path)
        {
            this.Error = error;
        }

        /// <summary>
        /// Print info
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => Path == null ? $"{GetType().Name}({Observer}, {EventTime}, {Error})" : $"{GetType().Name}({Observer}, {EventTime}, {Error}, {Path})";
    }

}
