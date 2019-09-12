// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;

namespace Lexical.FileSystem
{
    // <doc>
    /// <summary>
    /// File entry event.
    /// </summary>
    public interface IFileSystemEvent
    {
        /// <summary>
        /// The observer object that monitors the file-system.
        /// </summary>
        IFileSystemObserveHandle Observer { get; }

        /// <summary>
        /// The time the event occured, or approximation if not exactly known.
        /// </summary>
        DateTimeOffset EventTime { get; }

        /// <summary>
        /// (optional) Affected file or directory entry if applicable. 
        /// 
        /// Path is relative to the file-systems's root.
        /// Directory separator is "/". Root path doesn't use separator.
        /// Example: "dir/file.ext"
        /// 
        /// If event is rename, refers to old path.
        /// </summary>
        String Path { get; }
    }

    /// <summary>
    /// File renamed event.
    /// </summary>
    public interface IFileSystemRenameEvent : IFileSystemEvent
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
        String OldPath { get; }

        /// <summary>
        /// The new file or directory path.
        /// </summary>
        String NewPath { get; }
    }

    /// <summary>
    /// File created event
    /// </summary>
    public interface IFileSystemCreateEvent : IFileSystemEvent { }

    /// <summary>
    /// File delete event
    /// </summary>
    public interface IFileSystemDeleteEvent : IFileSystemEvent { }

    /// <summary>
    /// File contents changed event.
    /// </summary>
    public interface IFileSystemChangeEvent : IFileSystemEvent { }

    /// <summary>
    /// Information about file-system that produced the event.
    /// </summary>
    public interface IFileSystemCompositionEvent : IFileSystemEvent
    {
        /// <summary>
        /// The underlying file system that produced the event.
        /// </summary>
        IFileSystem OriginalFileSystem { get; }
    }

    /// <summary>
    /// Event for error with <see cref="IFileSystem"/> or a file entry.
    /// </summary>
    public interface IFileSystemErrorEvent : IFileSystemEvent
    {
        /// <summary>
        /// Error as exception.
        /// </summary>
        Exception Error { get; }
    }
    // </doc>

    /// <summary>
    /// Extension methods for <see cref="IFileSystem"/>.
    /// </summary>
    public static partial class IFileSystemEventExtensions
    {
    }

}
