// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Generic;

namespace Lexical.FileSystem
{
    // <IFileSystemEvent>
    /// <summary>
    /// File entry event.
    /// 
    /// See sub-interfaces:
    /// <list type="bullet">
    ///     <item><see cref="IFileSystemEventCreate"/></item>
    ///     <item><see cref="IFileSystemEventDelete"/></item>
    ///     <item><see cref="IFileSystemEventChange"/></item>
    ///     <item><see cref="IFileSystemEventRename"/></item>
    ///     <item><see cref="IFileSystemEventError"/></item>
    ///     <item><see cref="IFileSystemEventDecoration"/></item>
    ///     <item><see cref="IFileSystemEventStart"/></item>
    /// </list>
    /// </summary>
    public interface IFileSystemEvent
    {
        /// <summary>
        /// The observer object that monitors the filesystem.
        /// </summary>
        IFileSystemObserver Observer { get; }

        /// <summary>
        /// The time the event occured, or approximation if not exactly known.
        /// </summary>
        DateTimeOffset EventTime { get; }

        /// <summary>
        /// (optional) Affected file or directory entry if applicable. 
        /// 
        /// Path is relative to the filesystems's root.
        /// Directory separator is "/". Root path doesn't use separator.
        /// Example: "dir/file.ext"
        /// 
        /// If event is <see cref="IFileSystemEventRename"/> the value is same as <see cref="IFileSystemEventRename.OldPath"/>.
        /// </summary>
        String Path { get; }
    }
    // </IFileSystemEvent>

    // <IFileSystemEventRename>
    /// <summary>
    /// File renamed event.
    /// </summary>
    public interface IFileSystemEventRename : IFileSystemEvent
    {
        /// <summary>
        /// The affected file or directory.
        /// 
        /// Path is relative to the <see cref="FileSystem"/>'s root.
        /// 
        /// Directory separator is "/". Root path doesn't use separator.
        /// 
        /// Example: "dir/file.ext"
        /// 
        /// This value is same as inherited <see cref="IFileSystemEvent.Path"/>.
        /// </summary>
        String OldPath { get; }

        /// <summary>
        /// The new file or directory path.
        /// </summary>
        String NewPath { get; }
    }
    // </IFileSystemEventRename>

    // <IFileSystemEventCreate>
    /// <summary>
    /// File created event
    /// </summary>
    public interface IFileSystemEventCreate : IFileSystemEvent { }
    // </IFileSystemEventCreate>

    // <IFileSystemEventDelete>
    /// <summary>
    /// File delete event
    /// </summary>
    public interface IFileSystemEventDelete : IFileSystemEvent { }
    // </IFileSystemEventDelete>

    // <IFileSystemEventChange>
    /// <summary>
    /// File contents changed event.
    /// </summary>
    public interface IFileSystemEventChange : IFileSystemEvent { }
    // </IFileSystemEventChange>

    // <IFileSystemEventError>
    /// <summary>
    /// Event for error with <see cref="IFileSystem"/> or a file entry.
    /// </summary>
    public interface IFileSystemEventError : IFileSystemEvent
    {
        /// <summary>
        /// Error as exception.
        /// </summary>
        Exception Error { get; }
    }
    // </IFileSystemEventError>

    // <IFileSystemEventStart>
    /// <summary>
    /// The very first event when <see cref="IFileSystemObserve.Observe(string, IObserver{IFileSystemEvent}, object)"/> is called.
    /// </summary>
    public interface IFileSystemEventStart : IFileSystemEvent
    {
    }
    // </IFileSystemEventStart>

    // <IFileSystemEventDecoration>
    /// <summary>
    /// Signals that the event object decorates another event object.
    /// </summary>
    public interface IFileSystemEventDecoration : IFileSystemEvent
    {
        /// <summary>
        /// (optional) Original event that is decorated.
        /// </summary>
        IFileSystemEvent Original { get; }
    }
    // </IFileSystemEventDecoration>

    // <IFileSystemEventDispatcher>
    /// <summary>
    /// Dispatches <see cref="IFileSystemEvent"/>s.
    /// 
    /// Dispatcher implementation may capture unexpected exceptions from event handlers, or
    /// it may let them fly to the caller. 
    /// </summary>
    public interface IFileSystemEventDispatcher
    {
        /// <summary>
        /// Dispatch <paramref name="events"/> to observers.
        /// 
        /// If it recommended that the implementation enumerates <paramref name="events"/>, as this allows the caller to pass on heavy enumeration operations.
        /// </summary>
        /// <param name="events">(optional) Events</param>
        /// <exception cref="Exception">Any exception from observer may be captured or passed to caller</exception>
        void DispatchEvents(IEnumerable<IFileSystemEvent> events);

        /// <summary>
        /// Dispatch single <paramref name="event"/> to observers.
        /// </summary>
        /// <param name="event">(optional) events</param>
        /// <exception cref="Exception">Any exception from observer may be captured or passed to caller</exception>
        void DispatchEvent(IFileSystemEvent @event);
    }
    // </IFileSystemEventDispatcher>

}
