// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           13.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Abstract base class for decorated events.
    /// 
    /// See sub-classes:
    /// <list type="bullet">
    ///     <item><see cref="FileSystemEventDecorationRename"/></item>
    ///     <item><see cref="FileSystemEventDecorationCreate"/></item>
    ///     <item><see cref="FileSystemEventDecorationChange"/></item>
    ///     <item><see cref="FileSystemEventDecorationDelete"/></item>
    ///     <item><see cref="FileSystemEventDecorationError"/></item>
    /// </list>
    /// </summary>
    public abstract class FileSystemEventDecoration : IFileSystemEventDecoration
    {
        /// <summary>
        /// Convert <paramref name="event"/> to implement <see cref="IFileSystemEventDecoration"/> when possible.
        /// </summary>
        /// <param name="event"></param>
        /// <param name="newObserver">overriding observer</param>
        /// <returns></returns>
        public static IFileSystemEvent DecorateObserver(IFileSystemEvent @event, IFileSystemObserver newObserver)
        {
            switch (@event)
            {
                case IFileSystemEventCreate ce: return new FileSystemEventDecorationCreate.NewObserver(@event, newObserver);
                case IFileSystemEventDelete de: return new FileSystemEventDecorationDelete.NewObserver(@event, newObserver);
                case IFileSystemEventChange cc: return new FileSystemEventDecorationChange.NewObserver(@event, newObserver);
                case IFileSystemEventRename re: return new FileSystemEventDecorationRename.NewObserver(@event, newObserver);
                case IFileSystemEventError  ee: return new FileSystemEventDecorationError.NewObserver(@event, newObserver);
                default: return new FileSystemEventDecoration.NewObserver(@event, newObserver);
            }
        }

        /// <summary>
        /// original undecorated event.
        /// </summary>
        public virtual IFileSystemEvent Original { get; protected set; }

        /// <summary>
        /// The file-system observer that sent the event.
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
        protected FileSystemEventDecoration(IFileSystemEvent original)
        {
            this.Original = original ?? throw new ArgumentNullException(nameof(original));
        }

        /// <summary>
        /// Print info
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => Path == null ? $"{GetType().Name}({Observer}, {EventTime})" : $"{GetType().Name}({Observer}, {EventTime}, {Path})";

        /// <summary>Decoration with observer override.</summary>
        public class NewObserver : FileSystemEventDecoration
        {
            /// <summary>New observer value.</summary>
            protected IFileSystemObserver newObserver;
            /// <summary>New observer value.</summary>
            public override IFileSystemObserver Observer => newObserver;
            /// <summary>Create decoration with observer override.</summary>
            /// <param name="original">original event</param>
            /// <param name="newObserver">observer override</param>
            public NewObserver(IFileSystemEvent original, IFileSystemObserver newObserver) : base(original)
            {
                this.newObserver = newObserver;
            }
        }
    }

    /// <summary>
    /// Abstract base class for decorated file renamed event.
    /// </summary>
    public abstract class FileSystemEventDecorationRename : FileSystemEventDecoration, IFileSystemEventRename
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
        public virtual String OldPath => ((IFileSystemEventRename)Original).OldPath;

        /// <summary>
        /// The new file or directory path.
        /// </summary>
        public virtual String NewPath => ((IFileSystemEventRename)Original).NewPath;

        /// <summary>
        /// Create rename event.
        /// </summary>
        /// <param name="original">original event to be decorated</param>
        public FileSystemEventDecorationRename(IFileSystemEvent original) : base((IFileSystemEventRename)original)
        {
        }

        /// <summary>
        /// Print info
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => $"Rename({this.Observer}, {EventTime}, OldPath={OldPath}, NewPath={NewPath})";

        /// <summary>Decoration with observer override.</summary>
        public new class NewObserver : FileSystemEventDecorationRename
        {
            /// <summary>New observer value.</summary>
            protected IFileSystemObserver newObserver;
            /// <summary>New observer value.</summary>
            public override IFileSystemObserver Observer => newObserver;
            /// <summary>Create decoration with observer override.</summary>
            /// <param name="original">original event</param>
            /// <param name="newObserver">observer override</param>
            public NewObserver(IFileSystemEvent original, IFileSystemObserver newObserver) : base(original)
            {
                this.newObserver = newObserver;
            }
        }
    }

    /// <summary>
    /// Abstract base class for decorated file created event.
    /// </summary>
    public class FileSystemEventDecorationCreate : FileSystemEventDecoration, IFileSystemEventCreate
    {
        /// <summary>
        /// Create create event.
        /// </summary>
        /// <param name="original"></param>
        public FileSystemEventDecorationCreate(IFileSystemEvent original) : base((IFileSystemEventCreate)original)
        {
        }

        /// <summary>Print info</summary>
        /// <returns>Info</returns>
        public override string ToString()
            => Path == null ? $"Create({Observer}, {EventTime})" : $"Create({Observer}, {EventTime}, {Path})";

        /// <summary>Decoration with observer override.</summary>
        public new class NewObserver : FileSystemEventDecorationCreate
        {
            /// <summary>New observer value.</summary>
            protected IFileSystemObserver newObserver;
            /// <summary>New observer value.</summary>
            public override IFileSystemObserver Observer => newObserver;
            /// <summary>Create decoration with observer override.</summary>
            /// <param name="original">original event</param>
            /// <param name="newObserver">observer override</param>
            public NewObserver(IFileSystemEvent original, IFileSystemObserver newObserver) : base(original)
            {
                this.newObserver = newObserver;
            }
        }
    }

    /// <summary>
    /// File contents changed event
    /// </summary>
    public class FileSystemEventDecorationChange : FileSystemEventDecoration, IFileSystemEventChange
    {
        /// <summary>
        /// Create change event.
        /// </summary>
        /// <param name="original"></param>
        public FileSystemEventDecorationChange(IFileSystemEvent original) : base((IFileSystemEventChange)original)
        {
        }

        /// <summary>Print info</summary>
        /// <returns>Info</returns>
        public override string ToString()
            => Path == null ? $"Change({Observer}, {EventTime})" : $"Change({Observer}, {EventTime}, {Path})";

        /// <summary>Decoration with observer override.</summary>
        public new class NewObserver : FileSystemEventDecorationChange
        {
            /// <summary>New observer value.</summary>
            protected IFileSystemObserver newObserver;
            /// <summary>New observer value.</summary>
            public override IFileSystemObserver Observer => newObserver;
            /// <summary>Create decoration with observer override.</summary>
            /// <param name="original">original event</param>
            /// <param name="newObserver">observer override</param>
            public NewObserver(IFileSystemEvent original, IFileSystemObserver newObserver) : base(original)
            {
                this.newObserver = newObserver;
            }
        }
    }

    /// <summary>
    /// Abstract base class for decorated file deleted event.
    /// </summary>
    public class FileSystemEventDecorationDelete : FileSystemEventDecoration, IFileSystemEventDelete
    {
        /// <summary>
        /// Create delete event.
        /// </summary>
        /// <param name="original"></param>
        public FileSystemEventDecorationDelete(IFileSystemEvent original) : base((IFileSystemEventDelete)original)
        {
        }

        /// <summary>Print info</summary>
        /// <returns>Info</returns>
        public override string ToString()
            => Path == null ? $"Delete({Observer}, {EventTime})" : $"Delete({Observer}, {EventTime}, {Path})";

        /// <summary>Decoration with observer override.</summary>
        public new class NewObserver : FileSystemEventDecorationDelete
        {
            /// <summary>New observer value.</summary>
            protected IFileSystemObserver newObserver;
            /// <summary>New observer value.</summary>
            public override IFileSystemObserver Observer => newObserver;
            /// <summary>Create decoration with observer override.</summary>
            /// <param name="original">original event</param>
            /// <param name="newObserver">observer override</param>
            public NewObserver(IFileSystemEvent original, IFileSystemObserver newObserver) : base(original)
            {
                this.newObserver = newObserver;
            }
        }
    }

    /// <summary>
    /// Abstract base class for decorated error event.
    /// </summary>
    public class FileSystemEventDecorationError : FileSystemEventDecoration, IFileSystemEventError
    {
        /// <summary>
        /// Error
        /// </summary>
        public virtual Exception Error => ((IFileSystemEventError)Original).Error;

        /// <summary>
        /// Create delete event.
        /// </summary>
        /// <param name="original"></param>
        public FileSystemEventDecorationError(IFileSystemEvent original) : base((IFileSystemEventError)original)
        {
        }

        /// <summary>Print info</summary>
        /// <returns>Info</returns>
        public override string ToString()
            => Path == null ? $"Error({Observer}, {EventTime})" : $"Error({Observer}, {EventTime}, {Path})";

        /// <summary>Decoration with observer override.</summary>
        public new class NewObserver : FileSystemEventDecorationError
        {
            /// <summary>New observer value.</summary>
            protected IFileSystemObserver newObserver;
            /// <summary>New observer value.</summary>
            public override IFileSystemObserver Observer => newObserver;
            /// <summary>Create decoration with observer override.</summary>
            /// <param name="original">original event</param>
            /// <param name="newObserver">observer override</param>
            public NewObserver(IFileSystemEvent original, IFileSystemObserver newObserver) : base(original)
            {
                this.newObserver = newObserver;
            }
        }
    }

}
