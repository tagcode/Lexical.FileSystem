// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           17.10.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Lexical.FileSystem.Operation
{
    /// <summary>
    /// A file operation.
    /// </summary>
    public interface IOperation
    {
        /// <summary>The session where the op is ran in.</summary>
        IOperationSession Session { get; }

        /// <summary>Current state of the operation</summary>
        OperationState CurrentState { get; }

        /// <summary>Error events that occured involving this op</summary>
        IEnumerable<Exception> Errors { get; }

        /// <summary>Child operations</summary>
        IOperation[] Children { get; }

        /// <summary>Target filesystem, if applicable for the operation</summary>
        IFileSystem FileSystem { get; }
        /// <summary>Target path, if applicable for the operation.</summary>
        string Path { get; }
        /// <summary>Source filesystem, if applicable for the operation. Copy, move and transfer operation use this.</summary>
        IFileSystem SrcFileSystem { get; }
        /// <summary>Source path, if applicable for the operation. Copy, move and transfer operations use this.</summary>
        string SrcPath { get; }

        /// <summary>Current progress of operation in bytes. -1 if unknown.</summary>
        long Progress { get; }
        /// <summary>Total length of operation in bytes. -1 if unknown.</summary>
        long TotalLength { get; }

        /// <summary>Operation overriding policy. If set to <see cref="OperationPolicy.Unset"/>, then uses policy from session.</summary>
        OperationPolicy OpPolicy { get; }

        /// <summary>
        /// Effective policy for the operation. 
        /// 
        /// For source and destination file policies prioritizes the policy on the operation, then fallback to policy on session.
        /// For other flags uses union of the policy in operation and the policy on session.
        /// </summary>
        OperationPolicy EffectivePolicy { get; }

        /// <summary>
        /// Is operation capable of rollback. Value may change after <see cref="Estimate()"/> and <see cref="Run"/>.
        /// </summary>
        bool CanRollback { get; }

        /// <summary>
        /// Estimate viability and size of the operation.
        /// 
        /// Creates an action plan, and adds them to <see cref="Children"/>.
        /// 
        /// May change <see cref="CanRollback"/> value to true from default false.
        /// 
        /// If caller needs rollback capability, the caller may call <see cref="OperationExtensions.AssertCanRollback"/> right after estimate.
        /// </summary>
        /// <exception cref="Exception">If operation is not viable</exception>
        IOperation Estimate();

        /// <summary>
        /// Run the operation.
        /// 
        /// Throws exception on unexpected error.
        /// 
        /// The caller should test <see cref="CurrentState"/> to see how operation completed.
        /// <list type="bullet">
        ///     <item>If canceltoken was canelled then state is set to <see cref="OperationState.Cancelled"/>.</item>
        ///     <item>If file already existed and policy has <see cref="OperationPolicy.SrcSkip"/> or <see cref="OperationPolicy.DstSkip"/>, then state is set to <see cref="OperationState.Skipped"/>.</item>
        ///     <item>If unexpected error was thrown, then state is set to <see cref="OperationState.Error"/>.</item>
        ///     <item>If operation was ran to end, then state is set to <see cref="OperationState.Completed"/>.</item>
        /// </list>
        /// Or the caller may call <see cref="OperationExtensions.AssertSuccessful"/> to assert that operation ran into successful state.
        /// </summary>
        /// <param name="rollbackOnError"></param>
        /// <exception cref="IOException"></exception>
        /// <exception cref="Exception"></exception>
        IOperation Run(bool rollbackOnError = false);

        /// <summary>
        /// Create rollback operation that reverts already executed operations.
        /// </summary>
        /// <returns>null, if rollback could not be created</returns>
        IOperation CreateRollback();
    }

    /// <summary><see cref="IOperation"/> Extension methods.</summary>
    public static class OperationExtensions
    {
        /// <summary>
        /// Asserts that <see cref="IOperation.CanRollback"/> is true.
        /// </summary>
        /// <param name="op"></param>
        /// <returns>this</returns>
        /// <exception cref="Exception">If <see cref="IOperation.CanRollback"/> is false.</exception>
        public static IOperation AssertCanRollback(this IOperation op)
        {
            // Not Ok
            if (!op.CanRollback) throw new Exception("Cannot rollback " + op);
            // Ok
            return op;
        }

        /// <summary>
        /// Asserts that <see cref="IOperation.CurrentState"/> is either <see cref="OperationState.Completed"/> or <see cref="OperationState.Skipped"/>.
        /// </summary>
        /// <param name="op"></param>
        /// <returns>this</returns>
        /// <exception cref="OperationCanceledException">If state is <see cref="OperationState.Cancelled"/></exception>
        /// <exception cref="AggregateException">If state is <see cref="OperationState.Error"/></exception>
        /// <exception cref="Exception">If state is unexpected</exception>
        public static IOperation AssertSuccessful(this IOperation op)
        {
            // Get snapshot
            var _state = op.CurrentState;
            // Ok
            if (_state == OperationState.Completed || op.CurrentState == OperationState.Skipped) return op;
            // Cancelled
            if (_state == OperationState.Cancelled) throw new OperationCanceledException();
            // Error
            if (_state == OperationState.Error) throw new AggregateException(op.Errors.ToArray());
            // Unexpected
            throw new Exception("Unexpected state: " + _state);
        }

    }


}
