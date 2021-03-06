﻿// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           8.10.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Internal;
using Lexical.FileSystem.Utility;
using System;
using System.Threading;

namespace Lexical.FileSystem.Decoration
{
    /// <summary>
    /// Decorates events of child <see cref="IFileSystem"/> to parent <see cref="IFileSystem"/>.
    /// 
    /// When this object is disposed, it forwards <see cref="IObserver{T}.OnCompleted"/> event.
    /// </summary>
    public class ObserverDecoration : DisposeList, IFileSystemObserver, IObserver<IEvent>
    {
        /// <summary>Parent filesystem to use in decorated events.</summary>
        public IFileSystem FileSystem { get; protected set; }

        /// <summary><see cref="ISubPathOption"/> adapted path filter string.</summary>
        public string Filter { get; protected set; }

        /// <summary>The observer were decorated events are forwarded to.</summary>
        public IObserver<IEvent> Observer { get; protected set; }

        /// <summary>The state object the Observe() caller provided.</summary>
        public object State { get; protected set; }

        /// <summary>Event dispatcher</summary>
        public IEventDispatcher Dispatcher { get; protected set; }

        /// <summary>
        /// Number of feeding observers. 
        /// This count is incremented when <see cref="IStartEvent"/> event is sent, and decremented when <see cref="IEvent.Observer"/> is sent.
        /// </summary>
        long feedingObserverCount;

        /// <summary>
        /// If true, when last attached observer sends <see cref="IObserver{T}.OnCompleted"/> event, then this object is disposed.
        /// This mechanism works only if observer is implemented properly, so that is sends OnCompleted once and only once.
        /// </summary>
        bool disposeWhenLastCompletes;

        /// <summary>
        /// Create adapter observer.
        /// </summary>
        /// <param name="sourceFileSystem">File system to show as the source of forwarded events (in <see cref="IEvent.Observer"/>)</param>
        /// <param name="filter"></param>
        /// <param name="observer">The IObserver from caller were the decorated events are forwarded to</param>
        /// <param name="state"></param>
        /// <param name="eventDispatcher">event dispatcher to show on the interface, doesn't use it</param>
        /// <param name="disposeWhenLastCompletes">if true, when last attached observer sends <see cref="IObserver{T}.OnCompleted"/> event, 
        /// then diposes this object and sends <see cref="IObserver{T}.OnCompleted"/> to <see cref="Observer"/>.</param>
        public ObserverDecoration(IFileSystem sourceFileSystem, string filter, IObserver<IEvent> observer, object state, IEventDispatcher eventDispatcher, bool disposeWhenLastCompletes)
        {
            this.FileSystem = sourceFileSystem;
            this.Filter = filter;
            this.Observer = observer;
            this.State = state;
            this.Dispatcher = eventDispatcher;
            this.disposeWhenLastCompletes = disposeWhenLastCompletes;
        }

        /// <summary>child observer completes</summary>
        public void OnCompleted()
        {
            // Feeding observer is disposed
            long count = Interlocked.Decrement(ref feedingObserverCount);

            // Dispose self
            if (count == 0 && disposeWhenLastCompletes) this.Dispose();
        }

        /// <summary>Child observer has error</summary>
        public void OnError(Exception error) => Observer.OnError(error);

        /// <summary>Forward OnNext</summary>
        public void OnNext(IEvent @event)
        {
            // A component observer registers in.
            if (@event is IStartEvent) { Interlocked.Increment(ref feedingObserverCount); return; }

            // If observer dispose has completed, don't forward events.
            if (this.IsDisposing) return;

            // Get child's state object
            StateInfo state = @event.Observer.State as StateInfo;
            IPathConverter pathConverter = state?.pathConverter;
            // Convert paths
            string newOldPath, newNewPath = null;
            if (pathConverter == null)
            {
                newOldPath = @event.Path;
                if (@event is IRenameEvent re) newNewPath = re.NewPath;
            }
            else
            {
                if (!pathConverter.ChildToParent(@event.Path, out newOldPath)) return;
                if (@event is IRenameEvent re) if (!pathConverter.ChildToParent(re.NewPath, out newNewPath)) return;
            }
            // Try to decorate event
            @event = EventDecoration.DecorateObserverAndPath(@event, this, newOldPath, newNewPath, false);
            // Forward event in this thread, which should be dispatcher's thread
            Observer.OnNext(@event);
        }

        /// <summary>
        /// Handle dispose, forwards OnCompleted event.
        /// </summary>
        /// <param name="disposeErrors"></param>
        protected override void InnerDispose(ref StructList4<Exception> disposeErrors)
        {
            try
            {
                Observer.OnCompleted();
            }
            catch (Exception e)
            {
                disposeErrors.Add(e);
            }
        }

        /// <summary>
        /// State object to supply to child filesystem (decoree) on .Observe() method.
        /// </summary>
        public class StateInfo
        {
            /// <summary>Path converter</summary>
            public IPathConverter pathConverter;

            /// <summary>3rd party object</summary>
            public Object state;

            /// <summary>
            /// Create state info.
            /// </summary>
            /// <param name="pathConverter"></param>
            /// <param name="state"></param>
            public StateInfo(IPathConverter pathConverter, object state)
            {
                this.pathConverter = pathConverter;
                this.state = state;
            }
        }

        /// <summary>Print info</summary>
        public override string ToString() => $"Observer({Filter})";
    }

}