// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           17.10.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Internal;
using Lexical.FileSystem.Utility;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Lexical.FileSystem.Operation
{
    /// <summary>File operation session</summary>
    public class OperationSession : IDisposable, IOperationSession
    {
        /// <summary>Observers</summary>
        internal ArrayList<ObserverHandle> observers = new ArrayList<ObserverHandle>();
        /// <summary>Shared cancellation token</summary>
        public CancellationTokenSource CancelSrc { get; set; }
        /// <summary>Operation policies</summary>
        public OperationPolicy Policy { get; set; }
        /// <summary>Accumulated events</summary>
        public IProducerConsumerCollection<IOperationEvent> Events { get; set; } = new ConcurrentQueue<IOperationEvent>();
        /// <summary>Operations executed in this session</summary>
        public IProducerConsumerCollection<IOperation> Ops { get; set; } = new ConcurrentQueue<IOperation>();
        /// <summary>Tests if there are observers</summary>
        public bool HasObservers => observers.Count > 0;
        /// <summary>Pool that allocates byte buffers</summary>
        public IBlockPool BlockPool { get; set; }
        /// <summary>Interval of bytes interval to report progress on copying files.</summary>
        public long ProgressInterval { get; set; } = 524288L;
        /// <summary>(optional) Option or token</summary>
        public IOption Option { get; set; }

        /// <summary>Create session</summary>
        public OperationSession(OperationPolicy policy = OperationPolicy.Default, IBlockPool blockPool = default, CancellationTokenSource cancelSrc = default, IOption option = default)
        {
            this.Policy = policy;
            this.BlockPool = blockPool ?? new BlockPool();
            this.CancelSrc = cancelSrc ?? new CancellationTokenSource();
            this.Option = option;
        }

        /// <summary>Set new policy</summary>
        public OperationSession SetPolicy(OperationPolicy newPolicy)
        {
            this.Policy = newPolicy;
            return this;
        }

        /// <summary>Set new policy</summary>
        public OperationSession SetProgressInterval(long progressInterval)
        {
            this.ProgressInterval = progressInterval;
            return this;
        }

        /// <summary>Set new policy</summary>
        public OperationSession SetCancellationSource(CancellationTokenSource cancelSrc)
        {
            this.CancelSrc = cancelSrc;
            return this;
        }

        /// <summary>Dispose</summary>
        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
        /// <summary>Dispose called by finalizer or consumer (on Dispose())</summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Cancel source
                CancelSrc.Dispose();
                // Array of observer handles
                var handles = observers.Array;
                // Errors
                StructList4<Exception> errors = new StructList4<Exception>();
                foreach (var handle in handles)
                {
                    try
                    {
                        handle.observer.OnCompleted();
                    }
                    catch (Exception e)
                    {
                        // Add observer exception as error event, but don't dispatch it.
                        Events.TryAdd(new OperationErrorEvent(null, e));
                        // Capture
                        errors.Add(e);
                    }
                }
                if (errors.Count > 0) throw new AggregateException(errors.ToArray());
            }
        }

        /// <summary>Subscribe</summary>
        /// <returns>Handle to unsubscribe with</returns>
        public IDisposable Subscribe(IObserver<IOperationEvent> observer)
        {
            ObserverHandle handle = new ObserverHandle(this, observer);
            observers.Add(handle);
            return handle;
        }

        /// <summary>Cancellable observer handle.</summary>
        internal sealed class ObserverHandle : IDisposable
        {
            OperationSession session;
            internal IObserver<IOperationEvent> observer;

            public ObserverHandle(OperationSession session, IObserver<IOperationEvent> observer)
            {
                this.session = session;
                this.observer = observer ?? throw new ArgumentNullException(nameof(observer));
            }
            public void Dispose() => session.observers.Remove(this);
        }

        /// <summary>Add event to session log and dispatch it. <see cref="OperationProgressEvent"/> events are not added to event log.</summary>
        public void LogAndDispatchEvent(IOperationEvent @event)
        {
            if (@event is OperationProgressEvent == false) this.Events.TryAdd(@event);
            DispatchEvent(@event);
        }

        /// <summary>Dispatch event to observers (in current thread)</summary>
        public void DispatchEvent(IOperationEvent @event)
        {
            var handles = observers.Array;
            foreach (var handle in handles)
            {
                try
                {
                    handle.observer.OnNext(@event);
                }
                catch (Exception error) when (CaptureError(error)) { }
            }
            bool CaptureError(Exception e) { Events.TryAdd(new OperationErrorEvent(null, e)); return false; }
        }

    }

}
