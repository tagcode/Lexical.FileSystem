// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           17.10.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;

namespace Lexical.FileSystem.Operation
{
    /// <summary>
    /// File operation event.
    /// 
    /// See sub-interfaces:
    /// <list type="bullet">
    ///     <item><see cref="IOperationStateEvent"/></item>
    ///     <item><see cref="IOperationErrorEvent"/></item>
    ///     <item><see cref="IOperationProgressEvent"/></item>
    /// </list>
    /// </summary>
    public interface IOperationEvent
    {
        /// <summary>(optional) Involved operation</summary>
        IOperation Op { get; }
    }

    /// <summary>State changed event</summary>
    public interface IOperationStateEvent : IOperationEvent
    {
        /// <summary>New state</summary>
        OperationState OpState { get; }
    }

    /// <summary>Error state event</summary>
    public interface IOperationErrorEvent : IOperationStateEvent
    {
        /// <summary>Error</summary>
        Exception Exception { get; }
    }

    /// <summary>Progress event</summary>
    public interface IOperationProgressEvent : IOperationEvent
    { 
        /// <summary>Current position of operation</summary>
        long Length { get; }
        /// <summary>Total length of operation</summary>
        long TotalLength { get; }
    }

}
