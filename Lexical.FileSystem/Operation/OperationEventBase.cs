// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           17.10.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;

namespace Lexical.FileSystem.Operation
{
    /// <summary>File operation event</summary>
    public class OperationEventBase : IOperationEvent
    {
        /// <summary>(optional) Involved operation</summary>
        public IOperation Op { get; protected set; }
        /// <summary>Create event</summary>
        public OperationEventBase(IOperation op) { this.Op = op; }
    }

    /// <summary>State changed event</summary>
    public class OperationStateEvent : OperationEventBase, IOperationStateEvent
    {
        /// <summary>New state</summary>
        public OperationState OpState { get; protected set; }
        /// <summary>Create error event</summary>
        public OperationStateEvent(IOperation op, OperationState opState) : base(op) { this.OpState = opState; }
        /// <summary>Print info</summary>
        public override string ToString() => Op + " = " + OpState;
    }

    /// <summary>Error state event</summary>
    public class OperationErrorEvent : OperationStateEvent, IOperationErrorEvent
    {
        /// <summary>Error</summary>
        public Exception Exception { get; protected set; }
        /// <summary>Create error event</summary>
        public OperationErrorEvent(IOperation op, Exception exception) : base(op, OperationState.Error) { Exception = exception; }
        /// <summary>Print info</summary>
        public override string ToString() => Op + " = " + Exception;
    }

    /// <summary>Progress event</summary>
    public class OperationProgressEvent : OperationEventBase, IOperationProgressEvent
    {
        /// <summary>Current position of operation</summary>
        public long Length { get; protected set; }
        /// <summary>Total length of operation</summary>
        public long TotalLength { get; protected set; }
        /// <summary>Create IOperation event</summary>
        public OperationProgressEvent(IOperation op, long length, long totalLength) : base(op) { this.Length = length; this.TotalLength = totalLength; }
        /// <summary>Print info</summary>
        public override string ToString() => TotalLength > 0 ? "Progress(" + Op + ", " + (int)((Length * 100L) / TotalLength) + "%)" : "Progress(" + Op + ")";
    }

}
