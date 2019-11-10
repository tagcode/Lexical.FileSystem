// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           17.10.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;

namespace Lexical.FileSystem.Operation
{
    /// <summary>File operation event</summary>
    public class OperationEvent : IOperationEvent
    {
        /// <summary>(optional) Involved operation</summary>
        public IOperation Op { get; protected set; }
        /// <summary>Create event</summary>
        public OperationEvent(IOperation op) { this.Op = op; }

        /// <summary>State changed event</summary>
        public class State : OperationEvent, IOperationStateEvent
        {
            /// <summary>New state</summary>
            public OperationState OpState { get; protected set; }
            /// <summary>Create error event</summary>
            public State(IOperation op, OperationState opState) : base(op) { this.OpState = opState; }
            /// <summary>Print info</summary>
            public override string ToString() => Op + " = " + OpState;
        }

        /// <summary>Error state event</summary>
        public class Error : State, IOperationErrorEvent
        {
            /// <summary>Error</summary>
            public Exception Exception { get; protected set; }
            /// <summary>Create error event</summary>
            public Error(IOperation op, Exception exception) : base(op, OperationState.Error) { Exception = exception; }
            /// <summary>Print info</summary>
            public override string ToString() => Op + " = " + Exception;
        }

        /// <summary>Progress event</summary>
        public class Progress : OperationEvent, IOperationProgressEvent
        {
            /// <summary>Current position of operation</summary>
            public long Length { get; protected set; }
            /// <summary>Total length of operation</summary>
            public long TotalLength { get; protected set; }
            /// <summary>Create IOperation event</summary>
            public Progress(IOperation op, long length, long totalLength) : base(op) { this.Length = length; this.TotalLength = totalLength; }
            /// <summary>Print info</summary>
            public override string ToString() =>
                TotalLength > 0 ?
                "Progress(" + Op + ", " + (int)((Length * 100L) / TotalLength) + "%)" :
                "Progress(" + Op + ")";
        }
    }

}
