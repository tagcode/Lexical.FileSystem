// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Internal;
using Lexical.FileSystem.Utility;
using System;

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
    public abstract class FileSystemBase : DisposeList, IFileSystemDisposable
    {
        /// <summary>
        /// Create new filesystem.
        /// </summary>
        public FileSystemBase() : base()
        {
        }

        /// <summary>
        /// Send <paramref name="events"/> to observers.
        /// </summary>
        /// <param name="events"></param>
        public void DispatchEvents(ref StructList12<IEvent> events)
        {
            // Don't send events anymore
            if (IsDisposing) return;
            // Nothing to do
            if (events.Count == 0) return;
            // Get first dispatcher
            var _dispatcher = events[0].Observer.Dispatcher ?? EventDispatcher.Instance;
            // Dispatch one event
            if (events.Count == 1) { _dispatcher.DispatchEvent(events[0]); return; }
            // All same dispatcher?
            bool allSameDispatcher = true;
            for (int i = 1; i < events.Count; i++)
            {
                var __dispatcher = events[i].Observer.Dispatcher ?? EventDispatcher.Instance;
                if (__dispatcher != _dispatcher) { allSameDispatcher = false; break; }
            }

            // All events use same dispatcher
            if (allSameDispatcher)
            {
                // Send with struct list
                if (_dispatcher is IEventDispatcherExtended ext) { ext.DispatchEvents(ref events); return; }
                // Convert to array
                _dispatcher.DispatchEvents(events.ToArray());
            }
            else
            // Events use different dispatchers
            {
                // Dispatch each separately with different dispatchers
                for (int i = 0; i < events.Count; i++)
                    (events[i].Observer.Dispatcher ?? EventDispatcher.Instance).DispatchEvent(events[i]);
            }
        }
    }
}
