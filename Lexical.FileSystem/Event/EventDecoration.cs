// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           13.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;

namespace Lexical.FileSystem.Decoration
{
    /// <summary>
    /// Abstract base class for decorated events.
    /// 
    /// See sub-classes:
    /// <list type="bullet">
    ///     <item><see cref="RenameEventDecoration"/></item>
    ///     <item><see cref="CreateEventDecoration"/></item>
    ///     <item><see cref="ChangeEventDecoration"/></item>
    ///     <item><see cref="DeleteEventDecoration"/></item>
    ///     <item><see cref="ErrorEventDecoration"/></item>
    /// </list>
    /// </summary>
    public class EventDecoration : IEventDecoration
    {
        /// <summary>
        /// Decorate <paramref name="event"/> with <paramref name="newObserver"/>.
        /// </summary>
        /// <param name="event"></param>
        /// <param name="newObserver">overriding observer</param>
        /// <param name="throwIfUnknown">
        ///     If true, throws exception if <paramref name="event"/> is not recognized. 
        ///     If false, returns <see cref="EventDecoration"/> that has reference to undecorated event, but does not pass the interfaces from the source.
        /// </param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">If the interface of <paramref name="event"/> is not supported.</exception>
        public static IEvent DecorateObserver(IEvent @event, IFileSystemObserver newObserver, bool throwIfUnknown = true)
        {
            switch (@event)
            {
                case IStartEvent se: return new StartEventDecoration.NewObserver(@event, newObserver);
                case ICreateEvent ce: return new CreateEventDecoration.NewObserver(@event, newObserver);
                case IDeleteEvent de: return new DeleteEventDecoration.NewObserver(@event, newObserver);
                case IChangeEvent cc: return new ChangeEventDecoration.NewObserver(@event, newObserver);
                case IRenameEvent re: return new RenameEventDecoration.NewObserver(@event, newObserver);
                case IErrorEvent  ee: return new ErrorEventDecoration.NewObserver(@event, newObserver);
                default:
                    if (throwIfUnknown) throw new NotSupportedException(@event.GetType().FullName);
                    else return new EventDecoration.NewObserver(@event, newObserver);
            }
        }

        /// <summary>
        /// Decorate <paramref name="event"/> with <paramref name="newObserver"/> and <paramref name="newPath"/>.
        /// </summary>
        /// <param name="event"></param>
        /// <param name="newObserver">overriding observer</param>
        /// <param name="newPath">overriding path</param>
        /// <param name="newNewPath">overriding NewPath for <see cref="IRenameEvent"/> events</param>
        /// <param name="throwIfUnknown">
        ///     If true, throws exception if <paramref name="event"/> is not recognized. 
        ///     If false, returns <see cref="EventDecoration"/> that has reference to undecorated event, but does not pass the interfaces from the source.
        /// </param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">If the interface of <paramref name="event"/> is not supported.</exception>
        public static IEvent DecorateObserverAndPath(IEvent @event, IFileSystemObserver newObserver, string newPath, string newNewPath = null, bool throwIfUnknown = true)
        {
            switch (@event)
            {
                case IStartEvent se: return new StartEventDecoration.NewObserverAndPath(@event, newObserver, newPath);
                case ICreateEvent ce: return new CreateEventDecoration.NewObserverAndPath(@event, newObserver, newPath);
                case IDeleteEvent de: return new DeleteEventDecoration.NewObserverAndPath(@event, newObserver, newPath);
                case IChangeEvent cc: return new ChangeEventDecoration.NewObserverAndPath(@event, newObserver, newPath);
                case IRenameEvent re: return new RenameEventDecoration.NewObserverAndPath(@event, newObserver, newPath, newNewPath);
                case IErrorEvent ee: return new ErrorEventDecoration.NewObserverAndPath(@event, newObserver, newPath);
                default:
                    if (throwIfUnknown) throw new NotSupportedException(@event.GetType().FullName);
                    else return new EventDecoration.NewObserverAndPath(@event, newObserver, newPath);
            }
        }

        /// <summary>
        /// original undecorated event.
        /// </summary>
        public virtual IEvent Original { get; protected set; }

        /// <summary>
        /// The filesystem observer that sent the event.
        /// </summary>
        public virtual IFileSystemObserver Observer => Original.Observer;

        /// <summary>
        /// The time the event occured, or approximation if not exactly known.
        /// </summary>
        public virtual DateTimeOffset EventTime => Original.EventTime;

        /// <summary>
        /// (optional) Affected entry if applicable.
        /// </summary>
        public virtual string Path => Original.Path;

        /// <summary>
        /// Create event.
        /// </summary>
        /// <param name="original">original event to be decorated</param>
        public EventDecoration(IEvent original)
        {
            this.Original = original ?? throw new ArgumentNullException(nameof(original));
        }

        /// <summary>
        /// Print info
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => Path == null ? $"Event({Observer?.FileSystem}, {EventTime})" : $"{GetType().Name}({Observer?.FileSystem}, {EventTime}, {Path})";

        /// <summary>Decoration with observer override.</summary>
        public class NewObserver : EventDecoration
        {
            /// <summary>New observer value.</summary>
            protected IFileSystemObserver newObserver;
            /// <summary>New observer value.</summary>
            public override IFileSystemObserver Observer => newObserver;
            /// <summary>Create decoration with observer override.</summary>
            /// <param name="original">original event</param>
            /// <param name="newObserver">observer override</param>
            public NewObserver(IEvent original, IFileSystemObserver newObserver) : base(original)
            {
                this.newObserver = newObserver;
            }
        }

        /// <summary>Override observer and path.</summary>
        public class NewObserverAndPath : NewObserver
        {
            /// <summary>New observer value.</summary>
            protected string newPath;
            /// <summary>New observer value.</summary>
            public override string Path => newPath;
            /// <summary>Create observer and path decoration.</summary>
            public NewObserverAndPath(IEvent original, IFileSystemObserver newObserver, string newPath) : base(original, newObserver) { this.newPath = newPath; }
        }
    }

    /// <summary>
    /// Abstract base class for decorated file renamed event.
    /// </summary>
    public abstract class RenameEventDecoration : EventDecoration, IRenameEvent
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
        public virtual String OldPath => ((IRenameEvent)Original).OldPath;

        /// <summary>
        /// The new file or directory path.
        /// </summary>
        public virtual String NewPath => ((IRenameEvent)Original).NewPath;

        /// <summary>
        /// Create rename event.
        /// </summary>
        /// <param name="original">original event to be decorated</param>
        public RenameEventDecoration(IEvent original) : base((IRenameEvent)original)
        {
        }

        /// <summary>
        /// Print info
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => $"Rename({this.Observer?.FileSystem}, {EventTime}, OldPath={OldPath}, NewPath={NewPath})";

        /// <summary>Decoration with observer override.</summary>
        public new class NewObserver : RenameEventDecoration
        {
            /// <summary>New observer value.</summary>
            protected IFileSystemObserver newObserver;
            /// <summary>New observer value.</summary>
            public override IFileSystemObserver Observer => newObserver;
            /// <summary>Create decoration with observer override.</summary>
            /// <param name="original">original event</param>
            /// <param name="newObserver">observer override</param>
            public NewObserver(IEvent original, IFileSystemObserver newObserver) : base(original)
            {
                this.newObserver = newObserver;
            }
        }

        /// <summary>Override observer and path.</summary>
        public new class NewObserverAndPath : NewObserver
        {
            /// <summary>New observer value.</summary>
            protected string newOldPath, newNewPath;
            /// <summary>New observer value.</summary>
            public override string Path => newOldPath;
            /// <summary>New observer value.</summary>
            public override string OldPath => newOldPath;
            /// <summary>New observer value.</summary>
            public override string NewPath => newNewPath;
            /// <summary>Create observer and path decoration.</summary>
            public NewObserverAndPath(IEvent original, IFileSystemObserver newObserver, string newOldPath, string newNewPath) : base(original, newObserver) { this.newOldPath = newOldPath; this.newNewPath = newNewPath; }
        }
    }

    /// <summary>
    /// Abstract base class for decorated file created event.
    /// </summary>
    public class CreateEventDecoration : EventDecoration, ICreateEvent
    {
        /// <summary>
        /// Create create event.
        /// </summary>
        /// <param name="original"></param>
        public CreateEventDecoration(IEvent original) : base((ICreateEvent)original)
        {
        }

        /// <summary>Print info</summary>
        /// <returns>Info</returns>
        public override string ToString()
            => Path == null ? $"Create({Observer?.FileSystem}, {EventTime})" : $"Create({Observer?.FileSystem}, {EventTime}, {Path})";

        /// <summary>Decoration with observer override.</summary>
        public new class NewObserver : CreateEventDecoration
        {
            /// <summary>New observer value.</summary>
            protected IFileSystemObserver newObserver;
            /// <summary>New observer value.</summary>
            public override IFileSystemObserver Observer => newObserver;
            /// <summary>Create decoration with observer override.</summary>
            /// <param name="original">original event</param>
            /// <param name="newObserver">observer override</param>
            public NewObserver(IEvent original, IFileSystemObserver newObserver) : base(original)
            {
                this.newObserver = newObserver;
            }
        }

        /// <summary>Override observer and path.</summary>
        public new class NewObserverAndPath : NewObserver
        {
            /// <summary>New observer value.</summary>
            protected string newPath;
            /// <summary>New observer value.</summary>
            public override string Path => newPath;
            /// <summary>Create observer and path decoration.</summary>
            public NewObserverAndPath(IEvent original, IFileSystemObserver newObserver, string newPath) : base(original, newObserver) { this.newPath = newPath; }
        }
    }

    /// <summary>
    /// File contents changed event
    /// </summary>
    public class ChangeEventDecoration : EventDecoration, IChangeEvent
    {
        /// <summary>
        /// Create change event.
        /// </summary>
        /// <param name="original"></param>
        public ChangeEventDecoration(IEvent original) : base((IChangeEvent)original)
        {
        }

        /// <summary>Print info</summary>
        /// <returns>Info</returns>
        public override string ToString()
            => Path == null ? $"Change({Observer?.FileSystem}, {EventTime})" : $"Change({Observer?.FileSystem}, {EventTime}, {Path})";

        /// <summary>Decoration with observer override.</summary>
        public new class NewObserver : ChangeEventDecoration
        {
            /// <summary>New observer value.</summary>
            protected IFileSystemObserver newObserver;
            /// <summary>New observer value.</summary>
            public override IFileSystemObserver Observer => newObserver;
            /// <summary>Create decoration with observer override.</summary>
            /// <param name="original">original event</param>
            /// <param name="newObserver">observer override</param>
            public NewObserver(IEvent original, IFileSystemObserver newObserver) : base(original)
            {
                this.newObserver = newObserver;
            }
        }

        /// <summary>Override observer and path.</summary>
        public new class NewObserverAndPath : NewObserver
        {
            /// <summary>New observer value.</summary>
            protected string newPath;
            /// <summary>New observer value.</summary>
            public override string Path => newPath;
            /// <summary>Create observer and path decoration.</summary>
            public NewObserverAndPath(IEvent original, IFileSystemObserver newObserver, string newPath) : base(original, newObserver) { this.newPath = newPath; }
        }
    }

    /// <summary>
    /// Abstract base class for decorated file deleted event.
    /// </summary>
    public class DeleteEventDecoration : EventDecoration, IDeleteEvent
    {
        /// <summary>
        /// Create delete event.
        /// </summary>
        /// <param name="original"></param>
        public DeleteEventDecoration(IEvent original) : base((IDeleteEvent)original)
        {
        }

        /// <summary>Print info</summary>
        /// <returns>Info</returns>
        public override string ToString()
            => Path == null ? $"Delete({Observer?.FileSystem}, {EventTime})" : $"Delete({Observer?.FileSystem}, {EventTime}, {Path})";

        /// <summary>Decoration with observer override.</summary>
        public new class NewObserver : DeleteEventDecoration
        {
            /// <summary>New observer value.</summary>
            protected IFileSystemObserver newObserver;
            /// <summary>New observer value.</summary>
            public override IFileSystemObserver Observer => newObserver;
            /// <summary>Create decoration with observer override.</summary>
            /// <param name="original">original event</param>
            /// <param name="newObserver">observer override</param>
            public NewObserver(IEvent original, IFileSystemObserver newObserver) : base(original)
            {
                this.newObserver = newObserver;
            }
        }

        /// <summary>Override observer and path.</summary>
        public new class NewObserverAndPath : NewObserver
        {
            /// <summary>New observer value.</summary>
            protected string newPath;
            /// <summary>New observer value.</summary>
            public override string Path => newPath;
            /// <summary>Create observer and path decoration.</summary>
            public NewObserverAndPath(IEvent original, IFileSystemObserver newObserver, string newPath) : base(original, newObserver) { this.newPath = newPath; }
        }
    }

    /// <summary>
    /// Abstract base class for decorated error event.
    /// </summary>
    public class ErrorEventDecoration : EventDecoration, IErrorEvent
    {
        /// <summary>
        /// Error
        /// </summary>
        public virtual Exception Error => ((IErrorEvent)Original).Error;

        /// <summary>
        /// Create delete event.
        /// </summary>
        /// <param name="original"></param>
        public ErrorEventDecoration(IEvent original) : base((IErrorEvent)original)
        {
        }

        /// <summary>Print info</summary>
        /// <returns>Info</returns>
        public override string ToString()
            => Path == null ? $"Error({Observer?.FileSystem}, {EventTime})" : $"Error({Observer?.FileSystem}, {EventTime}, {Path})";

        /// <summary>Decoration with observer override.</summary>
        public new class NewObserver : ErrorEventDecoration
        {
            /// <summary>New observer value.</summary>
            protected IFileSystemObserver newObserver;
            /// <summary>New observer value.</summary>
            public override IFileSystemObserver Observer => newObserver;
            /// <summary>Create decoration with observer override.</summary>
            /// <param name="original">original event</param>
            /// <param name="newObserver">observer override</param>
            public NewObserver(IEvent original, IFileSystemObserver newObserver) : base(original)
            {
                this.newObserver = newObserver;
            }
        }

        /// <summary>Override observer and path.</summary>
        public new class NewObserverAndPath : NewObserver
        {
            /// <summary>New observer value.</summary>
            protected string newPath;
            /// <summary>New observer value.</summary>
            public override string Path => newPath;
            /// <summary>Create observer and path decoration.</summary>
            public NewObserverAndPath(IEvent original, IFileSystemObserver newObserver, string newPath) : base(original, newObserver) { this.newPath = newPath; }
        }
    }

    /// <summary>
    /// Abstract base class for decorated observe started event.
    /// </summary>
    public class StartEventDecoration : EventDecoration, IStartEvent
    {
        /// <summary>
        /// Start create event.
        /// </summary>
        /// <param name="original"></param>
        public StartEventDecoration(IEvent original) : base((IStartEvent)original)
        {
        }

        /// <summary>Print info</summary>
        /// <returns>Info</returns>
        public override string ToString()
            => $"Start({Observer?.FileSystem}, {EventTime})";

        /// <summary>Decoration with observer override.</summary>
        public new class NewObserver : StartEventDecoration
        {
            /// <summary>New observer value.</summary>
            protected IFileSystemObserver newObserver;
            /// <summary>New observer value.</summary>
            public override IFileSystemObserver Observer => newObserver;
            /// <summary>Create observer decoration.</summary>
            public NewObserver(IEvent original, IFileSystemObserver newObserver) : base(original)
            {
                this.newObserver = newObserver;
            }
        }

        /// <summary>Override observer and path.</summary>
        public new class NewObserverAndPath : NewObserver
        {
            /// <summary>New observer value.</summary>
            protected string newPath;
            /// <summary>New observer value.</summary>
            public override string Path => newPath;
            /// <summary>Create observer and path decoration.</summary>
            public NewObserverAndPath(IEvent original, IFileSystemObserver newObserver, string newPath) : base(original, newObserver) { this.newPath = newPath; }
        }

    }


}
