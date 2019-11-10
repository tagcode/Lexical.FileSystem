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
            => $"RenameEvent({Observer?.FileSystem}, {EventTime}, OldPath={OldPath}, NewPath={NewPath})";
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
            => Path == null ? $"CreateEvent({Observer?.FileSystem}, {EventTime})" : $"CreateEvent({Observer?.FileSystem}, {EventTime}, {Path})";
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
            => Path == null ? $"ChangeEvent({Observer?.FileSystem}, {EventTime})" : $"ChangeEvent({Observer?.FileSystem}, {EventTime}, {Path})";
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
            => Path == null ? $"DeleteEvent({Observer?.FileSystem}, {EventTime})" : $"DeleteEvent({Observer?.FileSystem}, {EventTime}, {Path})";
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
            => Path == null ? $"ErrorEvent({Observer?.FileSystem}, {EventTime})" : $"ErrorEvent({Observer?.FileSystem}, {EventTime}, {Path})";
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
            => $"StartEvent({Observer?.FileSystem}, {EventTime})";
    }

    /// <summary>
    /// The event when mountpoint is created or when assignments are changed when <see cref="IFileSystemMount.Mount"/> is called.
    /// </summary>
    public class MountEvent : EventBase, IMountEvent
    {
        /// <summary>(new) Assignment configuration at mountpoint</summary>
        public FileSystemAssignment[] Assignments { get; protected set; }

        /// <summary>Mount option</summary>
        public IOption Option { get; protected set; }

        /// <summary>
        /// Create Error event.
        /// </summary>
        /// <param name="observer"></param>
        /// <param name="eventTime"></param>
        /// <param name="mountpoint"></param>
        /// <param name="assignments"></param>
        /// <param name="option"></param>
        public MountEvent(IFileSystemObserver observer, DateTimeOffset eventTime, string mountpoint, FileSystemAssignment[] assignments, IOption option) : base(observer, eventTime, mountpoint)
        {
            this.Assignments = assignments;
            this.Option = option;
        }

        /// <summary>Print info</summary>
        /// <returns>Info</returns>
        public override string ToString()
        {
            string ass = Assignments == null ? "" : String.Join(", ", Assignments);
            return $"MountEvent({Observer?.FileSystem}, {EventTime}, {Path}, {ass}, {Option})";
        }
    }

    /// <summary>
    /// The event when whole mountpoint is unmounted.
    /// </summary>
    public class UnmountEvent : EventBase, IUnmountEvent
    {
        /// <summary>
        /// Create Error event.
        /// </summary>
        /// <param name="observer"></param>
        /// <param name="eventTime"></param>
        /// <param name="mountpoint"></param>
        public UnmountEvent(IFileSystemObserver observer, DateTimeOffset eventTime, string mountpoint) : base(observer, eventTime, mountpoint)
        {
        }

        /// <summary>Print info</summary>
        /// <returns>Info</returns>
        public override string ToString()
        {
            return $"UnmountEvent({Observer?.FileSystem}, {EventTime}, {Path})";
        }
    }

}
