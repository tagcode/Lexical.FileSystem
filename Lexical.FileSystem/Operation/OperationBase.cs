// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           17.10.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Lexical.FileSystem.Operation
{
    /// <summary>
    /// File operation base class.
    /// </summary>
    public abstract class OperationBase : IOperation
    {
        /// <summary>The session where the op is ran in.</summary>
        protected IOperationSession session;
        /// <summary>The session where the op is ran in.</summary>
        public IOperationSession Session => session;

        /// <summary>Current state of the operation</summary>
        protected int currentState = (int)OperationState.Initialized;
        /// <summary>Current state of the operation</summary>
        public OperationState CurrentState => (OperationState)currentState;

        /// <summary>Error events that occured involving this op</summary>
        public IEnumerable<Exception> Errors => session.Events.Where(e => e is OperationEvent.Error err && err.Op == this).Select(e => ((OperationEvent.Error)e).Exception);

        /// <summary>Child operations</summary>
        public virtual IOperation[] Children => null;

        /// <summary>Target filesystem, if applicable for the operation</summary>
        public virtual IFileSystem FileSystem => null;
        /// <summary>Target path, if applicable for the operation.</summary>
        public virtual string Path => null;
        /// <summary>Source filesystem, if applicable for the operation. Copy, move and transfer operation use this.</summary>
        public virtual IFileSystem SrcFileSystem => null;
        /// <summary>Source path, if applicable for the operation. Copy, move and transfer operations use this.</summary>
        public virtual string SrcPath => null;

        /// <summary>Current progress of operation in bytes. -1 if unknown.</summary>
        public long Progress { get; protected set; }
        /// <summary>Total length of operation in bytes. -1 if unknown.</summary>
        public long TotalLength { get; protected set; }

        /// <summary>Operation overriding policy. If set to <see cref="OperationPolicy.Unset"/>, then uses policy from session.</summary>
        public OperationPolicy OpPolicy { get; protected set; }

        /// <summary>
        /// Effective policy for the operation. 
        /// 
        /// For source and destination file policies prioritizes the policy on the operation, then fallback to policy on session.
        /// For other flags uses union of the policy in operation and the policy on session.
        /// </summary>
        public OperationPolicy EffectivePolicy
        {
            get
            {
                OperationPolicy result = OperationPolicy.Unset;
                // Add src policy
                result |= ((OpPolicy & OperationPolicy.SrcMask) == OperationPolicy.SrcUnset ? Session.Policy : OpPolicy) & OperationPolicy.SrcMask;
                // Add dst policy
                result |= ((OpPolicy & OperationPolicy.DstMask) == OperationPolicy.DstUnset ? Session.Policy : OpPolicy) & OperationPolicy.DstMask;
                // Add rollback policy
                result |= ((OpPolicy & OperationPolicy.RollbackMask) == OperationPolicy.RollbackUnset ? Session.Policy : OpPolicy) & OperationPolicy.RollbackMask;
                // Add estimate policy
                result |= ((OpPolicy & OperationPolicy.EstimateMask) == OperationPolicy.EstimateUnset ? Session.Policy : OpPolicy) & OperationPolicy.EstimateMask;
                // Union of flags
                result |= (OpPolicy & OperationPolicy.FlagsMask) | (Session.Policy & OperationPolicy.FlagsMask);
                // Return the effective policy
                return result;
            }
        }

        /// <summary>
        /// Is operation capable of rollback. Value may change after <see cref="Estimate()"/> and <see cref="Run"/>.
        /// </summary>
        public bool CanRollback { get; protected set; } = false;

        /// <summary>
        /// Create filesystem operation
        /// </summary>
        /// <param name="session">operation session</param>
        /// <param name="policy">operation specific policy</param>
        public OperationBase(IOperationSession session, OperationPolicy policy)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.OpPolicy = policy;
        }

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
        public IOperation Estimate()
        {
            // Assert session is not cancelled
            if (session.CancelSrc.IsCancellationRequested) { SetState(OperationState.Cancelled); return this; }
            // Estimate is post-poned
            if (EffectivePolicy.HasFlag(OperationPolicy.EstimateOnRun)) return this;

            // Start estimating
            if (TrySetState(OperationState.Estimating, OperationState.Initialized))
                try
                {
                    // Do
                    InnerEstimate();
                    // Estimated
                    TrySetState(OperationState.Estimated, OperationState.Estimating);
                }
                // Capture error and set state to error
                catch (Exception e) when (SetError(e)) { }

            // Self
            return this;
        }

        /// <summary>
        /// Estimate viability and size of the operation.
        /// 
        /// Creates an action plan, and adds them to <see cref="Children"/>.
        /// 
        /// May change <see cref="CanRollback"/> value to true from default false.
        /// 
        /// If caller needs rollback capability, the caller may call <see cref="OperationExtensions.AssertCanRollback"/> right after estimate.
        /// </summary>
        /// <exception cref="Exception">On any error, captured and processed by calling Run()</exception>
        protected abstract void InnerEstimate();

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
        public IOperation Run(bool rollbackOnError = false)
        {
            // Assert session is not cancelled
            if (session.CancelSrc.IsCancellationRequested) { SetState(OperationState.Cancelled); return this; }

            try
            {
                // Has estimate been ran on this Run() method
                try
                {
                    // Start estimating
                    if (EffectivePolicy.HasFlag(OperationPolicy.ReEstimateOnRun) || CurrentState == OperationState.Initialized)
                    {
                        TrySetState(OperationState.Estimating, OperationState.Initialized);
                        // Estimate
                        InnerEstimate();
                        // Estimated
                        TrySetState(OperationState.Estimated, OperationState.Estimating);
                    }

                    // Start run
                    if (TrySetState(OperationState.Running, OperationState.Estimated))
                    {
                        // Run
                        InnerRun();
                        // Completed
                        if (!TrySetState(OperationState.Completed, OperationState.Running)) return this;
                    }
                }
                // Capture error and set state to error
                catch (Exception e) when (SetError(e)) { }
            }
            // Rollback
            catch (Exception) when (rollbackOnError && tryRollback()) { } // never goes here

            // Return
            return this;

            // TryRollback
            bool tryRollback() { CreateRollback()?.Run(); return false; }
        }

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
        /// <exception cref="Exception">On any error, captured and processed by calling Run()</exception>
        protected abstract void InnerRun();

        /// <summary>
        /// Create rollback operation that reverts already executed operations.
        /// </summary>
        /// <returns>null, if rollback could not be created</returns>
        public virtual IOperation CreateRollback() => null;

        /// <summary>
        /// Captures and handles exception.
        /// 
        /// Sets state to <see cref="OperationState.Error"/>.
        /// Adds event to event log.
        /// If <see cref="OperationPolicy.CancelOnError"/> is set, then cancels the cancel token.
        /// </summary>
        /// <param name="error"></param>
        /// <returns>value of <see cref="OperationPolicy.SuppressException"/></returns>
        protected bool SetError(Exception error)
        {
            // Set state to error
            currentState = (int)OperationState.Error;
            // Cancel token
            if (session.Policy.HasFlag(OperationPolicy.CancelOnError)) session.CancelSrc.Cancel();
            // Change state
            if ((OperationState)Interlocked.Exchange(ref currentState, (int)OperationState.Error) != OperationState.Error)
            {
                if (((EffectivePolicy & (OperationPolicy.LogEvents | OperationPolicy.DispatchEvents)) == (OperationPolicy.LogEvents | OperationPolicy.DispatchEvents))) session.LogAndDispatchEvent(new OperationEvent.Error(this, error));
                else if ((EffectivePolicy & OperationPolicy.LogEvents) == OperationPolicy.LogEvents) session.Events.TryAdd(new OperationEvent.Error(this, error));
                else if ((EffectivePolicy & OperationPolicy.DispatchEvents) == OperationPolicy.DispatchEvents) session.DispatchEvent(new OperationEvent.Error(this, error));
            }
            // Return true if has SuppressException
            return EffectivePolicy.HasFlag(OperationPolicy.SuppressException);
        }

        /// <summary>
        /// Try to set state to <paramref name="newState"/>, but only if previous state was <paramref name="expectedState"/>.
        /// 
        /// Add event of state change to session.
        /// </summary>
        /// <param name="newState"></param>
        /// <param name="expectedState"></param>
        /// <returns>true if state was <paramref name="expectedState"/> and is now <paramref name="newState"/></returns>
        protected bool TrySetState(OperationState newState, OperationState expectedState)
        {
            // Try to change state
            if ((OperationState)Interlocked.CompareExchange(ref currentState, (int)newState, (int)expectedState) != expectedState) return false;

            // Send event of new state
            if (((EffectivePolicy & (OperationPolicy.LogEvents | OperationPolicy.DispatchEvents)) == (OperationPolicy.LogEvents | OperationPolicy.DispatchEvents))) session.LogAndDispatchEvent(new OperationEvent.State(this, newState));
            else if ((EffectivePolicy & OperationPolicy.LogEvents) == OperationPolicy.LogEvents) session.Events.TryAdd(new OperationEvent.State(this, newState));
            else if ((EffectivePolicy & OperationPolicy.DispatchEvents) == OperationPolicy.DispatchEvents) session.DispatchEvent(new OperationEvent.State(this, newState));
            return true;
        }

        /// <summary>
        /// Set state to <paramref name="newState"/> if previous state wasn't that already.
        /// 
        /// Add event of state change to session.
        /// </summary>
        /// <param name="newState">true if previous state was other than <paramref name="newState"/></param>
        protected bool SetState(OperationState newState)
        {
            // Try to change state
            if ((OperationState)Interlocked.Exchange(ref currentState, (int)newState) == newState) return false;

            // Send event of new state
            if (((EffectivePolicy & (OperationPolicy.LogEvents | OperationPolicy.DispatchEvents)) == (OperationPolicy.LogEvents | OperationPolicy.DispatchEvents))) session.LogAndDispatchEvent(new OperationEvent.State(this, newState));
            else if ((EffectivePolicy & OperationPolicy.LogEvents) == OperationPolicy.LogEvents) session.Events.TryAdd(new OperationEvent.State(this, newState));
            else if ((EffectivePolicy & OperationPolicy.DispatchEvents) == OperationPolicy.DispatchEvents) session.DispatchEvent(new OperationEvent.State(this, newState));
            return true;
        }
    }

}
