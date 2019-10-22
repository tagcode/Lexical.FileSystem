// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Internal;
using Lexical.FileSystem.Utility;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Base implementation for <see cref="IFileSystem"/>. 
    /// 
    /// Disposables can be attached to be disposed along with <see cref="IFileSystem"/>.
    /// Watchers can be attached as disposables, so that they forward <see cref="IObserver{T}.OnCompleted"/> event upon IFileSystem dispose.
    /// 
    /// Can send events to observers.
    /// </summary>
    public abstract class FileSystemBase : DisposeList, IFileSystemDisposable, IFileSystemObserve
    {
        /// <summary>
        /// Has SetEventDispatcher() capability.
        /// </summary>
        public virtual bool CanSetEventDispatcher { get; protected set; } = true;

        /// <summary>
        /// Can observe
        /// </summary>
        public abstract bool CanObserve { get; }

        /// <summary>
        /// Evetnt dispatcher.
        /// </summary>
        protected internal IFileSystemEventDispatcher eventDispatcher = FileSystemEventDispatcher.Instance;

        /// <summary>
        /// Create new filesystem.
        /// </summary>
        public FileSystemBase() : base()
        {
        }

        /// <summary>
        /// Set a <see cref="IFileSystemEventDispatcher"/> that dispatches the events. If set to null, then filesystem doesn't dispatch events.
        /// </summary>
        /// <param name="eventDispatcher">(optional) that dispatches events to observers. If null, doesn't dispatch events..</param>
        /// <returns>this</returns>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support setting event handler.</exception>
        IFileSystem IFileSystemObserve.SetEventDispatcher(IFileSystemEventDispatcher eventDispatcher)
        {
            setEventDispatcher(eventDispatcher);
            return this;
        }

        /// <summary>Override this to change behaviour.</summary>
        /// <param name="eventDispatcher"></param>
        protected virtual void setEventDispatcher(IFileSystemEventDispatcher eventDispatcher) => this.eventDispatcher = eventDispatcher;

        /// <summary>
        /// Send <paramref name="events"/> to observers.
        /// </summary>
        /// <param name="events"></param>
        public void DispatchEvents(ref StructList12<IFileSystemEvent> events)
        {
            // Don't send events anymore
            if (IsDisposing) return;
            // Nothing to do
            if (events.Count == 0) return;
            // Get reference
            var _dispatcher = eventDispatcher;
            // No dispatcher
            if (_dispatcher == null) return;
            // Dispatch one event
            if (events.Count == 1) { _dispatcher.DispatchEvent(events[0]); return; }
            // Send with struct list
            if (_dispatcher is IFileSystemEventDispatcherExtended ext) { ext.DispatchEvents(ref events); return; }
            // Convert to array
            _dispatcher.DispatchEvents(events.ToArray());
        }

        /// <inheritdoc/>
        public abstract IFileSystemObserver Observe(string filter, IObserver<IFileSystemEvent> observer, object state = null);
    }
}
