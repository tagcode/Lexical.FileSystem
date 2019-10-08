// --------------------------------------------------------
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
    public class ObserverDecorator : DisposeList, IFileSystemObserver, IObserver<IFileSystemEvent>, IFileSystemEventStart
    {
        /// <summary>Parent filesystem to use in decorated events.</summary>
        public IFileSystem FileSystem { get; protected set; }

        /// <summary><see cref="IFileSystemOptionMountPath"/> adapted path filter string.</summary>
        public string Filter { get; protected set; }

        /// <summary>The observer were decorated events are forwarded to.</summary>
        public IObserver<IFileSystemEvent> Observer { get; protected set; }

        /// <summary>The state object the Observe() caller provided.</summary>
        public object State { get; protected set; }

        /// <summary>Time when observe started.</summary>
        DateTimeOffset startTime = DateTimeOffset.UtcNow;

        /// <summary>
        /// Number of feeding observers. 
        /// This count is incremented when <see cref="IFileSystemEventStart"/> event is sent, and decremented when <see cref="IFileSystemEvent.Observer"/> is sent.
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
        /// <param name="sourceFileSystem">File system to show as the source of forwarded events (in <see cref="IFileSystemEvent.Observer"/>)</param>
        /// <param name="filter"></param>
        /// <param name="observer">The observer were decorated events are forwarded to</param>
        /// <param name="state"></param>
        /// <param name="disposeWhenLastCompletes">if true, when last attached observer sends <see cref="IObserver{T}.OnCompleted"/> event, 
        /// then diposes this object and sends <see cref="IObserver{T}.OnCompleted"/> to <see cref="Observer"/>.</param>
        public ObserverDecorator(IFileSystem sourceFileSystem, string filter, IObserver<IFileSystemEvent> observer, object state, bool disposeWhenLastCompletes)
        {
            if (sourceFileSystem is FileSystemBase == false) throw new ArgumentException($"This class is intended to be used with subclasses of {nameof(FileSystemBase)}.");
            this.FileSystem = sourceFileSystem;
            this.Filter = filter;
            this.Observer = observer;
            this.State = state;
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
        public void OnNext(IFileSystemEvent @event)
        {
            // A component observer registers in.
            if (@event is IFileSystemEventStart) { Interlocked.Increment(ref feedingObserverCount); return; }

            // If observer dispose has completed, don't forward events.
            if (this.IsDisposing) return;

            // Get child's state object
            StateInfo state = @event.Observer.State as StateInfo;
            PathDecoration pathDecorator = state?.pathDecoration;
            // Convert paths
            string newOldPath, newNewPath = null;
            if (pathDecorator == null)
            {
                newOldPath = @event.Path;
                if (@event is IFileSystemEventRename re) newNewPath = re.NewPath;
            }
            else
            {
                if (!pathDecorator.ChildToParent(@event.Path, out newOldPath)) return;
                if (@event is IFileSystemEventRename re) if (!pathDecorator.ChildToParent(re.NewPath, out newNewPath)) return;
            }
            // Try to decorate event
            @event = FileSystemEventDecoration.DecorateObserverAndPath(@event, this, newOldPath, newNewPath, false);
            // Send event forward
            ((FileSystemBase)this.FileSystem).SendEvent(@event);
        }

        // Used when pushing self as IFileSystemEventStart
        IFileSystemObserver IFileSystemEvent.Observer => this;
        DateTimeOffset IFileSystemEvent.EventTime => startTime;
        string IFileSystemEvent.Path => null;
        //

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
            public PathDecoration pathDecoration;

            /// <summary>3rd party object</summary>
            public Object state;

            /// <summary>
            /// Create state info.
            /// </summary>
            /// <param name="pathDecoration"></param>
            /// <param name="state"></param>
            public StateInfo(PathDecoration pathDecoration, object state)
            {
                this.pathDecoration = pathDecoration;
                this.state = state;
            }
        }
    }

}