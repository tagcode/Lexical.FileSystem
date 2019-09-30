// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Internal;
using Lexical.FileSystem.Utility;
using System;
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
        /// Task-factory that is used for sending events.
        /// If factory is set to null, then events are processed in the current thread.
        /// </summary>
        protected TaskFactory eventHandler;

        /// <summary>Delegate that processes events</summary>
        Action<object> processEventsAction;

        /// <summary>
        /// Create new filesystem.
        /// </summary>
        public FileSystemBase() : base()
        {
            this.processEventsAction = processEvents;
        }

        /// <summary>
        /// Set <paramref name="eventHandler"/> to be used for handling observer events.
        /// 
        /// If <paramref name="eventHandler"/> is null, then events are processed in the threads
        /// that make modifications to memory filesytem.
        /// </summary>
        /// <param name="eventHandler">(optional) factory that handles observer events</param>
        /// <returns>memory filesystem</returns>
        IFileSystem IFileSystemObserve.SetEventDispatcher(TaskFactory eventHandler)
        {
            this.eventHandler = eventHandler;
            return this;
        }

        /// <summary>
        /// Send <paramref name="events"/> to observers with <see cref="eventHandler"/>.
        /// If <see cref="eventHandler"/> is null, then sends events in the running thread.
        /// </summary>
        /// <param name="events"></param>
        protected internal void SendEvents(ref StructList12<IFileSystemEvent> events)
        {
            // Don't send events anymore
            if (IsDisposing) return;
            // Nothing to do
            if (events.Count == 0) return;
            // Get taskfactory
            TaskFactory _taskFactory = eventHandler;
            // Send events in this thread
            if (_taskFactory == null)
            {
                // Errors
                StructList4<Exception> errors = new StructList4<Exception>();
                foreach (IFileSystemEvent e in events)
                {
                    try
                    {
                        e.Observer.Observer.OnNext(e);
                    }
                    catch (Exception error)
                    {
                        // Bumerang error
                        try
                        {
                            e.Observer.Observer.OnError(error);
                        }
                        catch (Exception error2)
                        {
                            // 
                            errors.Add(error);
                            errors.Add(error2);
                        }
                    }
                }
                if (errors.Count > 0) throw new AggregateException(errors.ToArray());
            }
            else
            // Create task that processes events.
            {
                _taskFactory.StartNew(processEventsAction, events.ToArray());
            }
        }

        /// <summary>
        /// Send one <paramref name="event"/> to observers with <see cref="eventHandler"/>.
        /// If <see cref="eventHandler"/> is null, then sends events in the running thread.
        /// </summary>
        /// <param name="event"></param>
        protected internal void SendEvent(IFileSystemEvent @event)
        {
            // Don't send events anymore
            if (IsDisposing) return;
            // Nothing to do
            if (@event == null) return;
            // Get taskfactory
            TaskFactory _taskFactory = eventHandler;
            // Send events in this thread
            if (_taskFactory == null)
            {
                // Error
                Exception error = null;
                try
                {
                    @event.Observer.Observer.OnNext(@event);
                }
                catch (Exception error1)
                {
                    // Bumerang error
                    try
                    {
                        @event.Observer.Observer.OnError(error1);
                    }
                    catch (Exception error2)
                    {
                        // Capture
                        error = new AggregateException(error1, error2);
                    }
                }
                if (error != null) throw error;
            }
            else
            // Create task that processes events.
            {
                _taskFactory.StartNew(processEventsAction, @event);
            }
        }

        /// <summary>
        /// Forward events to observers in the running thread.
        /// </summary>
        /// <param name="events">IFileSystemEvent[] or <see cref="IFileSystemEvent"/></param>
        protected void processEvents(object events)
        {
            // Errors
            StructList4<Exception> errors = new StructList4<Exception>();
            if (events is IFileSystemEvent[] eventsArray)
            {
                foreach (IFileSystemEvent e in eventsArray)
                {
                    try
                    {
                        e.Observer.Observer.OnNext(e);
                    }
                    catch (Exception error)
                    {
                        // Bumerang error
                        try
                        {
                            e.Observer.Observer.OnError(error);
                        }
                        catch (Exception error2)
                        {
                            // 
                            errors.Add(error2);
                        }
                    }
                }
                if (errors.Count > 0) throw new AggregateException(errors.ToArray());
            } else if (events is IFileSystemEvent @event)
            {
                // Error
                Exception error = null;
                try
                {
                    @event.Observer.Observer.OnNext(@event);
                }
                catch (Exception error1)
                {
                    // Bumerang error
                    try
                    {
                        @event.Observer.Observer.OnError(error1);
                    }
                    catch (Exception error2)
                    {
                        // Capture
                        error = new AggregateException(error1, error2);
                    }
                }
                if (error != null) throw error;
            }
        }

        /// <inheritdoc/>
        public abstract IFileSystemObserver Observe(string filter, IObserver<IFileSystemEvent> observer, object state = null);
    }
}
