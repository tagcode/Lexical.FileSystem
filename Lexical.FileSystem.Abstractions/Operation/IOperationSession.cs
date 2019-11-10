// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           17.10.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Utility;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Lexical.FileSystem.Operation
{
    /// <summary>File operation session</summary>
    public interface IOperationSession : IDisposable, IObservable<IOperationEvent>
    {
        /// <summary>Shared cancellation token</summary>
        CancellationTokenSource CancelSrc { get; set; }
        /// <summary>Operation policies</summary>
        OperationPolicy Policy { get; set; }
        /// <summary>Accumulated events</summary>
        IProducerConsumerCollection<IOperationEvent> Events { get; set; }
        /// <summary>Operations executed in this session</summary>
        IProducerConsumerCollection<IOperation> Ops { get; set; }
        /// <summary>Tests if there are observers</summary>
        bool HasObservers { get; }
        /// <summary>Pool that allocates byte buffers</summary>
        IBlockPool BlockPool { get; set; }
        /// <summary>Interval of bytes interval to report progress on copying files.</summary>
        long ProgressInterval { get; set; }
        /// <summary>(optional) Option or token</summary>
        IOption Option { get; set; }

        /// <summary>Add event to session log and dispatch it. <see cref="IOperationProgressEvent"/> events are not added to event log.</summary>
        void LogAndDispatchEvent(IOperationEvent @event);

        /// <summary>Dispatch event to observers (in current thread)</summary>
        void DispatchEvent(IOperationEvent @event);
    }

    /// <summary>Operation sesssion extensions</summary>
    public static class OperationSessionExtensions
    {
        /// <summary>Set new policy</summary>
        public static IOperationSession SetPolicy(this IOperationSession session, OperationPolicy newPolicy)
        {
            session.Policy = newPolicy;
            return session;
        }

        /// <summary>Set new policy</summary>
        public static IOperationSession SetProgressInterval(this IOperationSession session, long progressInterval)
        {
            session.ProgressInterval = progressInterval;
            return session;
        }

        /// <summary>Set new policy</summary>
        public static IOperationSession SetCancellationSource(this IOperationSession session, CancellationTokenSource cancelSrc)
        {
            session.CancelSrc = cancelSrc;
            return session;
        }

    }
}
