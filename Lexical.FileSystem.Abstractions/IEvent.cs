// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Generic;

namespace Lexical.FileSystem
{
    // <IEvent>
    /// <summary>
    /// File entry event.
    /// 
    /// See sub-interfaces:
    /// <list type="bullet">
    ///     <item><see cref="ICreateEvent"/></item>
    ///     <item><see cref="IDeleteEvent"/></item>
    ///     <item><see cref="IChangeEvent"/></item>
    ///     <item><see cref="IRenameEvent"/></item>
    ///     <item><see cref="IErrorEvent"/></item>
    ///     <item><see cref="IStartEvent"/></item>
    ///     <item><see cref="IMountEvent"/></item>
    ///     <item><see cref="IUnmountEvent"/></item>
    ///     <item><see cref="IEventDecoration"/></item>
    /// </list>
    /// </summary>
    public interface IEvent
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
        /// If event is <see cref="IRenameEvent"/> the value is same as <see cref="IRenameEvent.OldPath"/>.
        /// </summary>
        String Path { get; }
    }
    // </IEvent>

    // <IRenameEvent>
    /// <summary>
    /// File renamed event.
    /// </summary>
    public interface IRenameEvent : IEvent
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
        /// This value is same as inherited <see cref="IEvent.Path"/>.
        /// </summary>
        String OldPath { get; }

        /// <summary>
        /// The new file or directory path.
        /// </summary>
        String NewPath { get; }
    }
    // </IRenameEvent>

    // <ICreateEvent>
    /// <summary>
    /// File created event
    /// </summary>
    public interface ICreateEvent : IEvent { }
    // </ICreateEvent>

    // <IDeleteEvent>
    /// <summary>
    /// File delete event
    /// </summary>
    public interface IDeleteEvent : IEvent { }
    // </IDeleteEvent>

    // <IChangeEvent>
    /// <summary>
    /// File contents changed event.
    /// </summary>
    public interface IChangeEvent : IEvent { }
    // </IChangeEvent>

    // <IErrorEvent>
    /// <summary>
    /// Event for error with <see cref="IFileSystem"/> or a file entry.
    /// </summary>
    public interface IErrorEvent : IEvent
    {
        /// <summary>
        /// Error as exception.
        /// </summary>
        Exception Error { get; }
    }
    // </IErrorEvent>

    // <IStartEvent>
    /// <summary>
    /// The very first event when <see cref="IFileSystemObserve.Observe"/> is called.
    /// 
    /// <see cref="IStartEvent"/> must be handled in the thread that calls .Observe() 
    /// in the implementation of Observe() and before Observe() returns. It ignores .EventDispatcher property.
    /// </summary>
    public interface IStartEvent : IEvent
    {
    }
    // </IStartEvent>

    // <IMountEvent>
    /// <summary>
    /// The event when mountpoint is created or when assignments are changed when <see cref="IFileSystemMount.Mount"/> is called.
    /// 
    /// Possible mounting <see cref="ICreateEvent"/>s are sent afterwards.
    /// </summary>
    public interface IMountEvent : IEvent
    {
        /// <summary>(new) Assignment configuration at mountpoint</summary>
        FileSystemAssignment[] Assignments { get; }

        /// <summary>Mount option</summary>
        IOption Option { get; }
    }
    // </IMountEvent>

    // <IUnmountEvent>
    /// <summary>
    /// The event when whole mountpoint is unmounted.
    /// 
    /// Possible unmounting <see cref="IDeleteEvent"/>s are sent afterwards.
    /// </summary>
    public interface IUnmountEvent : IEvent
    {
    }
    // </IUnmountEvent>

    // <IEventDecoration>
    /// <summary>
    /// Signals that the event object decorates another event object.
    /// </summary>
    public interface IEventDecoration : IEvent
    {
        /// <summary>
        /// (optional) Original event that is decorated.
        /// </summary>
        IEvent Original { get; }
    }
    // </IEventDecoration>

    // <IEventDispatcher>
    /// <summary>
    /// Dispatches <see cref="IEvent"/>s.
    /// 
    /// Dispatcher implementation may capture unexpected exceptions from event handlers, or
    /// it may let them fly to the caller. 
    /// </summary>
    public interface IEventDispatcher
    {
        /// <summary>
        /// Dispatch <paramref name="events"/> to observers.
        /// 
        /// If it recommended that the implementation enumerates <paramref name="events"/>, as this allows the caller to pass on heavy enumeration operations.
        /// </summary>
        /// <param name="events">(optional) Events</param>
        /// <exception cref="Exception">Any exception from observer may be captured or passed to caller</exception>
        void DispatchEvents(IEnumerable<IEvent> events);

        /// <summary>
        /// Dispatch single <paramref name="event"/> to observers.
        /// </summary>
        /// <param name="event">(optional) events</param>
        /// <exception cref="Exception">Any exception from observer may be captured or passed to caller</exception>
        void DispatchEvent(IEvent @event);
    }
    // </IEventDispatcher>

}
