// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           22.10.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Internal;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lexical.FileSystem
{
    /// <summary>Dispatches <see cref="IEvent"/>s.</summary>
    public interface IEventDispatcherExtended : IEventDispatcher
    {
        /// <summary>
        /// Send <paramref name="events"/> to observers>.
        /// </summary>
        /// <param name="events">events</param>
        /// <exception cref="Exception">Any exception from observer may be captured or passed to caller</exception>
        void DispatchEvents(ref StructList12<IEvent> events);
    }

    /// <summary>
    /// Dispatches events on API caller's current thread.
    /// </summary>
    public class EventDispatcher : IEventDispatcherExtended
    {
        static EventDispatcher instance => new EventDispatcher();

        /// <summary>Singleton instance </summary>
        public static EventDispatcher Instance => instance;

        /// <summary>
        /// (optional) Error handler;
        /// </summary>
        protected Action<IEventDispatcher, IEvent, Exception> errorHandler;

        /// <summary>
        /// Create event dispatcher that dispatches events in the API caller's thread.
        /// </summary>
        /// <param name="errorHandler"></param>
        public EventDispatcher(Action<IEventDispatcher, IEvent, Exception> errorHandler = null)
        {
            this.errorHandler = errorHandler;
        }

        /// <summary>Dispatch <paramref name="event"/></summary>
        /// <param name="event"></param>
        public void DispatchEvent(IEvent @event)
        {
            try
            {
                @event?.Observer?.Observer?.OnNext(@event);
            } catch (Exception error) when (errorHandler != null)
            {
                errorHandler(this, @event, error);
            }
        }

        /// <summary>Dispatch <paramref name="events"/></summary>
        /// <param name="events"></param>
        public void DispatchEvents(ref StructList12<IEvent> events)
        {
            // Errors
            StructList4<Exception> errors = new StructList4<Exception>();
            for (int i = 0; i < events.Count; i++)
            {
                IEvent e = events[i];
                try
                {
                    e?.Observer?.Observer?.OnNext(e);
                }
                catch (Exception error)
                {
                    if (errorHandler!=null) errorHandler(this, e, error);
                    else errors.Add(error);
                }
            }
            if (errors.Count > 0) throw new AggregateException(errors.ToArray());
        }

        /// <summary>Dispatch <paramref name="events"/></summary>
        /// <param name="events"></param>
        public void DispatchEvents(IEnumerable<IEvent> events)
        {
            // Errors
            StructList4<Exception> errors = new StructList4<Exception>();
            if (events != null)
                foreach (IEvent e in events)
                {
                    try
                    {
                        e?.Observer?.Observer?.OnNext(e);
                    }
                    catch (Exception error)
                    {
                        if (errorHandler != null) errorHandler(this, e, error);
                        else errors.Add(error);
                    }
                }
            if (errors.Count > 0) throw new AggregateException(errors.ToArray());
        }
    }

    /// <summary>
    /// Dispatches events in concurrent tasks.
    /// </summary>
    public class EventTaskDispatcher : IEventDispatcherExtended
    {
        static EventTaskDispatcher instance => new EventTaskDispatcher(Task.Factory, null);

        /// <summary>Singleton instance </summary>
        public static EventTaskDispatcher Instance => instance;

        /// <summary>
        /// Task-factory that is used for sending events.
        /// If factory is set to null, then events are processed in the current thread.
        /// </summary>
        protected TaskFactory taskFactory;

        /// <summary>Delegate that processes events</summary>
        Action<object> processEventsAction;

        /// <summary>
        /// (optional) Error handler;
        /// </summary>
        protected Action<IEventDispatcher, IEvent, Exception> errorHandler;

        /// <summary>
        /// Create event dispatcher that uses task factory.
        /// </summary>
        /// <param name="taskFactory">(optional) task factory</param>
        /// <param name="errorHandler">(optional) error handler</param>
        public EventTaskDispatcher(TaskFactory taskFactory = default, Action<IEventDispatcher, IEvent, Exception> errorHandler = null)
        {
            this.taskFactory = taskFactory ?? Task.Factory;
            this.processEventsAction = processEvents;
            this.errorHandler = errorHandler;
        }

        /// <summary>Dispatch <paramref name="event"/></summary>
        /// <param name="event"></param>
        public void DispatchEvent(IEvent @event)
            => taskFactory.StartNew(processEventsAction, @event);

        /// <summary>Dispatch <paramref name="events"/></summary>
        /// <param name="events"></param>
        public void DispatchEvents(ref StructList12<IEvent> events)
        {
            if (events.Count == 1) taskFactory.StartNew(processEventsAction, events[0]);
            else if (events.Count >= 2) taskFactory.StartNew(processEventsAction, events.ToArray());
        }

        /// <summary>Dispatch <paramref name="events"/></summary>
        /// <param name="events"></param>
        public void DispatchEvents(IEnumerable<IEvent> events)
            => taskFactory.StartNew(processEventsAction, events);

        /// <summary>Forward events to observers.</summary>
        /// <param name="events">IEnumerable or <see cref="IEvent"/></param>
        protected void processEvents(object events)
        {
            // Errors
            StructList4<Exception> errors = new StructList4<Exception>();
            if (events is IEnumerable<IEvent> eventsEnumr)
            {
                foreach (IEvent e in eventsEnumr)
                {
                    try
                    {
                        e?.Observer?.Observer?.OnNext(e);
                    }
                    catch (Exception error)
                    {
                        if (errorHandler != null) errorHandler(this, e, error);
                        else errors.Add(error);
                    }
                }
                if (errors.Count > 0) throw new AggregateException(errors.ToArray());
            }
            else if (events is IEvent @event)
            {
                try
                {
                    @event.Observer?.Observer?.OnNext(@event);
                }
                catch (Exception error) when (errorHandler != null)
                {
                    errorHandler(this, @event, error);
                }
            }
        }

    }

}
