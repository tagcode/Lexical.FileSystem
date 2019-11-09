// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           17.10.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lexical.FileSystem.Decoration;
using Lexical.FileSystem.Internal;

namespace Lexical.FileSystem.Utility
{
    /// <summary>
    /// File operation utility class.
    /// </summary>
    public abstract class FileOperation
    {
        /// <summary>Operation State</summary>
        public enum State : int
        {
            /// <summary>Operation has been initialized</summary>
            Initialized = 0,
            /// <summary>Operation size and viability are being estimated</summary>
            Estimating = 1,
            /// <summary>Operation size and viability have been estimated</summary>
            Estimated = 2,
            /// <summary>Started and running</summary>
            Running = 3,
            /// <summary>Action skipped</summary>
            Skipped = 4,
            /// <summary>Run completed ok</summary>
            Completed = 5,
            /// <summary>Run interrupted with cancellation token</summary>
            Cancelled = 6,
            /// <summary>Run failed</summary>
            Error = 7,
        }

        /// <summary>File operation policy</summary>
        [Flags]
        public enum Policy : ulong
        {
            /// <summary>Policy is not set. If used in FileOperation, then inherits policy from its session.</summary>
            Unset = 0UL,

            // Policy on source files (choose one)
            /// <summary>Source policy is not set.</summary>
            SrcUnset = 0x00UL,
            /// <summary>Throw <see cref="FileNotFoundException"/> or <see cref="DirectoryNotFoundException"/> if source files or directories are not found.</summary>
            SrcThrow = 0x01UL,
            /// <summary>If source files or directories are not found, then operation is skipped</summary>
            SrcSkip = 0x02UL,
            /// <summary>Source policy mask</summary>
            SrcMask = 0xffUL,

            // Policy on destination files (choose one)
            /// <summary>Destination policy is not set.</summary>
            DstUnset = 0x00UL << 8,
            /// <summary>If destination file already exists (or doesn't exist on delete), throw <see cref="FileSystemExceptionFileExists"/> or <see cref="FileSystemExceptionDirectoryExists"/>.</summary>
            DstThrow = 0x01UL << 8,
            /// <summary>If destination file already exists, skip the operation on them.</summary>
            DstSkip = 0x02UL << 8,
            /// <summary>If destination file already exists, overwrite it.</summary>
            DstOverwrite = 0x03UL << 8,
            /// <summary>Destination policy mask</summary>
            DstMask = 0xffUL << 8,

            // Estimate policies
            /// <summary>No estimate flags</summary>
            EstimateUnset = 0x00UL << 16,
            /// <summary>Estimate on Run(). Public method Estimate() does nothing.</summary>
            EstimateOnRun = 0x01UL << 16,
            /// <summary>Re-estimate on Run(). Runs on Estimate() and Run().</summary>
            ReEstimateOnRun = 0x01UL << 16,
            /// <summary>Estimate flags mask</summary>
            EstimateMask = 0xffUL << 16,

            // Rollback policies
            /// <summary>No rollback flags</summary>
            RollbackUnset = 0x00UL << 24,
            /// <summary>Rollback flags mask</summary>
            RollbackMask = 0xffUL << 24,

            // Other flags
            /// <summary>If one operation fails, signals cancel on <see cref="Session.CancelSrc"/> cancel token source.</summary>
            CancelOnError = 0x0001UL << 32,
            /// <summary>Policy whether to omit directories that are mounted packages, such as .zip.</summary>
            OmitMountedPackages = 0x0002UL << 32,
            /// <summary>Batch operation continues on child op error. Throws <see cref="AggregateException"/> on errors, but only after all child ops have been ran.</summary>
            BatchContinueOnError = 0x0004UL << 32,
            /// <summary>Suppress exception in Estimate() and Run().</summary>
            SuppressException = 0x0008UL << 32,
            /// <summary>Log events to session.</summary>
            LogEvents = 0x0010UL << 32,
            /// <summary>Dispatch events to subscribers.</summary>
            DispatchEvents = 0x0020UL << 32,
            /// <summary>Mask for flags</summary>
            FlagsMask = 0xffffUL << 32,

            /// <summary>Default policy</summary>
            Default = SrcSkip | DstThrow | OmitMountedPackages | LogEvents | DispatchEvents
        }

        /// <summary>The session where the op is ran in.</summary>
        protected Session session;
        /// <summary>The session where the op is ran in.</summary>
        public Session OpSession => session;

        /// <summary>Current state of the operation</summary>
        protected int currentState = (int)State.Initialized;
        /// <summary>Current state of the operation</summary>
        public State CurrentState => (State)currentState;

        /// <summary>Error events that occured involving this op</summary>
        public IEnumerable<Exception> Errors => session.Events.Where(e => e is Event.Error err && err.Op == this).Select(e => ((Event.Error)e).Exception);

        /// <summary>Child operations</summary>
        public virtual FileOperation[] Children => null;

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

        /// <summary>Operation overriding policy. If set to <see cref="Policy.Unset"/>, then uses policy from session.</summary>
        public Policy OpPolicy { get; protected set; }

        /// <summary>
        /// Effective policy for the operation. 
        /// 
        /// For source and destination file policies prioritizes the policy on the operation, then fallback to policy on session.
        /// For other flags uses union of the policy in operation and the policy on session.
        /// </summary>
        public Policy EffectivePolicy
        {
            get
            {
                Policy result = Policy.Unset;
                // Add src policy
                result |= ((OpPolicy & Policy.SrcMask) == Policy.SrcUnset ? OpSession.Policy : OpPolicy) & Policy.SrcMask;
                // Add dst policy
                result |= ((OpPolicy & Policy.DstMask) == Policy.DstUnset ? OpSession.Policy : OpPolicy) & Policy.DstMask;
                // Add rollback policy
                result |= ((OpPolicy & Policy.RollbackMask) == Policy.RollbackUnset ? OpSession.Policy : OpPolicy) & Policy.RollbackMask;
                // Add estimate policy
                result |= ((OpPolicy & Policy.EstimateMask) == Policy.EstimateUnset ? OpSession.Policy : OpPolicy) & Policy.EstimateMask;
                // Union of flags
                result |= (OpPolicy & Policy.FlagsMask) | (OpSession.Policy & Policy.FlagsMask);
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
        public FileOperation(Session session, Policy policy)
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
        /// If caller needs rollback capability, the caller may call <see cref="AssertCanRollback"/> right after estimate.
        /// </summary>
        /// <exception cref="Exception">If operation is not viable</exception>
        public FileOperation Estimate()
        {
            // Assert session is not cancelled
            if (session.CancelSrc.IsCancellationRequested) { SetState(State.Cancelled); return this; }
            // Estimate is post-poned
            if (EffectivePolicy.HasFlag(Policy.EstimateOnRun)) return this;

            // Start estimating
            if (TrySetState(State.Estimating, State.Initialized)) 
            try
            {
                // Do
                InnerEstimate();
                // Estimated
                TrySetState(State.Estimated, State.Estimating);
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
        /// If caller needs rollback capability, the caller may call <see cref="AssertCanRollback"/> right after estimate.
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
        ///     <item>If canceltoken was canelled then state is set to <see cref="State.Cancelled"/>.</item>
        ///     <item>If file already existed and policy has <see cref="Policy.SrcSkip"/> or <see cref="Policy.DstSkip"/>, then state is set to <see cref="State.Skipped"/>.</item>
        ///     <item>If unexpected error was thrown, then state is set to <see cref="State.Error"/>.</item>
        ///     <item>If operation was ran to end, then state is set to <see cref="State.Completed"/>.</item>
        /// </list>
        /// Or the caller may call <see cref="AssertSuccessful"/> to assert that operation ran into successful state.
        /// </summary>
        /// <param name="rollbackOnError"></param>
        /// <exception cref="IOException"></exception>
        /// <exception cref="Exception"></exception>
        public FileOperation Run(bool rollbackOnError = false)
        {
            // Assert session is not cancelled
            if (session.CancelSrc.IsCancellationRequested) { SetState(State.Cancelled); return this; }

            try
            {
                // Has estimate been ran on this Run() method
                try
                {
                    // Start estimating
                    if (EffectivePolicy.HasFlag(Policy.ReEstimateOnRun) || CurrentState == State.Initialized)
                    {
                        TrySetState(State.Estimating, State.Initialized);
                        // Estimate
                        InnerEstimate();
                        // Estimated
                        TrySetState(State.Estimated, State.Estimating);
                    }

                    // Start run
                    if (TrySetState(State.Running, State.Estimated))
                    {
                        // Run
                        InnerRun();
                        // Completed
                        if (!TrySetState(State.Completed, State.Running)) return this;
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
        ///     <item>If canceltoken was canelled then state is set to <see cref="State.Cancelled"/>.</item>
        ///     <item>If file already existed and policy has <see cref="Policy.SrcSkip"/> or <see cref="Policy.DstSkip"/>, then state is set to <see cref="State.Skipped"/>.</item>
        ///     <item>If unexpected error was thrown, then state is set to <see cref="State.Error"/>.</item>
        ///     <item>If operation was ran to end, then state is set to <see cref="State.Completed"/>.</item>
        /// </list>
        /// Or the caller may call <see cref="AssertSuccessful"/> to assert that operation ran into successful state.
        /// </summary>
        /// <exception cref="Exception">On any error, captured and processed by calling Run()</exception>
        protected abstract void InnerRun();

        /// <summary>
        /// Asserts that <see cref="CanRollback"/> is true..
        /// </summary>
        /// <returns>this</returns>
        /// <exception cref="Exception">If <see cref="CanRollback"/> is false.</exception>
        public FileOperation AssertCanRollback()
        {
            // Not Ok
            if (!CanRollback) throw new Exception("Cannot rollback " + this);
            // Ok
            return this;
        }

        /// <summary>
        /// Asserts that <see cref="CurrentState"/> is either <see cref="State.Completed"/> or <see cref="State.Skipped"/>.
        /// </summary>
        /// <returns>this</returns>
        /// <exception cref="OperationCanceledException">If state is <see cref="State.Cancelled"/></exception>
        /// <exception cref="AggregateException">If state is <see cref="State.Error"/></exception>
        /// <exception cref="Exception">If state is unexpected</exception>
        public FileOperation AssertSuccessful()
        {
            // Get snapshot
            var _state = CurrentState;
            // Ok
            if (_state == State.Completed || CurrentState == State.Skipped) return this;
            // Cancelled
            if (_state == State.Cancelled) throw new OperationCanceledException();
            // Error
            if (_state == State.Error) throw new AggregateException(Errors.ToArray());
            // Unexpected
            throw new Exception("Unexpected state: " + _state);
        }

        /// <summary>
        /// Create rollback operation that reverts already executed operations.
        /// </summary>
        /// <returns>null, if rollback could not be created</returns>
        public virtual FileOperation CreateRollback() => null;

        /// <summary>
        /// Captures and handles exception.
        /// 
        /// Sets state to <see cref="State.Error"/>.
        /// Adds event to event log.
        /// If <see cref="Policy.CancelOnError"/> is set, then cancels the cancel token.
        /// </summary>
        /// <param name="error"></param>
        /// <returns>value of <see cref="Policy.SuppressException"/></returns>
        protected bool SetError(Exception error)
        {
            // Set state to error
            currentState = (int)State.Error;
            // Cancel token
            if (session.Policy.HasFlag(Policy.CancelOnError)) session.CancelSrc.Cancel();
            // Change state
            if ((State)Interlocked.Exchange(ref currentState, (int)State.Error) != State.Error)
            {
                if (((EffectivePolicy & (Policy.LogEvents | Policy.DispatchEvents)) == (Policy.LogEvents | Policy.DispatchEvents))) session.LogAndDispatchEvent(new Event.Error(this, error));
                else if ((EffectivePolicy & Policy.LogEvents) == Policy.LogEvents) session.Events.TryAdd(new Event.Error(this, error));
                else if ((EffectivePolicy & Policy.DispatchEvents) == Policy.DispatchEvents) session.DispatchEvent(new Event.Error(this, error));
            }
            // Return true if has SuppressException
            return EffectivePolicy.HasFlag(Policy.SuppressException);
        }

        /// <summary>
        /// Try to set state to <paramref name="newState"/>, but only if previous state was <paramref name="expectedState"/>.
        /// 
        /// Add event of state change to session.
        /// </summary>
        /// <param name="newState"></param>
        /// <param name="expectedState"></param>
        /// <returns>true if state was <paramref name="expectedState"/> and is now <paramref name="newState"/></returns>
        protected bool TrySetState(State newState, State expectedState)
        {
            // Try to change state
            if ((State)Interlocked.CompareExchange(ref currentState, (int)newState, (int)expectedState) != expectedState) return false;

            // Send event of new state
            if (((EffectivePolicy & (Policy.LogEvents | Policy.DispatchEvents)) == (Policy.LogEvents | Policy.DispatchEvents))) session.LogAndDispatchEvent(new Event.State(this, newState));
            else if ((EffectivePolicy & Policy.LogEvents) == Policy.LogEvents) session.Events.TryAdd(new Event.State(this, newState));
            else if ((EffectivePolicy & Policy.DispatchEvents) == Policy.DispatchEvents) session.DispatchEvent(new Event.State(this, newState));
            return true;
        }

        /// <summary>
        /// Set state to <paramref name="newState"/> if previous state wasn't that already.
        /// 
        /// Add event of state change to session.
        /// </summary>
        /// <param name="newState">true if previous state was other than <paramref name="newState"/></param>
        protected bool SetState(State newState)
        {
            // Try to change state
            if ((State)Interlocked.Exchange(ref currentState, (int)newState) == newState) return false;

            // Send event of new state
            if (((EffectivePolicy & (Policy.LogEvents | Policy.DispatchEvents)) == (Policy.LogEvents | Policy.DispatchEvents))) session.LogAndDispatchEvent(new Event.State(this, newState));
            else if ((EffectivePolicy & Policy.LogEvents) == Policy.LogEvents) session.Events.TryAdd(new Event.State(this, newState));
            else if ((EffectivePolicy & Policy.DispatchEvents) == Policy.DispatchEvents) session.DispatchEvent(new Event.State(this, newState));
            return true;
        }

        /// <summary>Batch operation</summary>
        public class Batch : FileOperation
        {
            /// <summary>Ops.</summary>
            public ArrayList<FileOperation> Ops = new ArrayList<FileOperation>();
            /// <summary>Child operations</summary>
            public override FileOperation[] Children => Ops.Array;
            /// <summary>Target filesystem if same for all ops, otherwise null</summary>
            public override IFileSystem FileSystem => Ops.Count == 0 ? null : Ops.Count == 1 ? Ops[0].FileSystem : (Ops.All(fo => fo.FileSystem == Ops[0].FileSystem) ? Ops[0].FileSystem : null);
            /// <summary>Target path if same for all ops, otherwise null</summary>
            public override String Path => Ops.Count == 0 ? null : Ops.Count == 1 ? Ops[0].Path : (Ops.All(fo => fo.Path == Ops[0].Path) ? Ops[0].Path : null);
            /// <summary>Source filesystem if same for all ops, otherwise null</summary>
            public override IFileSystem SrcFileSystem => Ops.Count == 0 ? null : Ops.Count == 1 ? Ops[0].SrcFileSystem : (Ops.All(fo => fo.SrcFileSystem == Ops[0].SrcFileSystem) ? Ops[0].SrcFileSystem : null);
            /// <summary>Source path if same for all ops, otherwise null</summary>
            public override String SrcPath => Ops.Count == 0 ? null : Ops.Count == 1 ? Ops[0].SrcPath : (Ops.All(fo => fo.SrcPath == Ops[0].SrcPath) ? Ops[0].SrcPath : null);

            /// <summary>Create batch op.</summary>
            public Batch(Session session, Policy policy, IEnumerable<FileOperation> ops) : base(session, policy)
            {
                if (ops != null) this.Ops.AddRange(ops);
                this.CanRollback = true;
            }

            /// <summary>Create batch op.</summary>
            public Batch(Session session, Policy policy, params FileOperation[] ops) : base(session, policy)
            {
                if (ops != null) this.Ops.AddRange(ops);
                this.CanRollback = true;
            }

            /// <summary>Estimate child ops</summary>
            /// <exception cref="Exception">On error</exception>
            protected override void InnerEstimate()
            {
                StructList2<Exception> errors = new StructList2<Exception>();
                foreach (FileOperation op in Ops)
                {
                    try
                    {
                        // Assert session is not cancelled
                        if (session.CancelSrc.IsCancellationRequested) { SetState(State.Cancelled); return; }
                        op.Estimate();
                        if (op.TotalLength > 0L) this.TotalLength += op.TotalLength;
                        if (op.Progress > 0L) this.Progress += op.Progress;
                        this.CanRollback &= op.CanRollback | op.EffectivePolicy.HasFlag(Policy.EstimateOnRun);
                    }
                    catch (Exception e) when (session.Policy.HasFlag(Policy.BatchContinueOnError))
                    {
                        errors.Add(e);
                    }
                }
                // Throw captured exceptions
                if (errors.Count > 0) throw new AggregateException(errors.ToArray());
            }

            /// <summary>Run child ops</summary>
            protected override void InnerRun()
            {
                StructList2<Exception> errors = new StructList2<Exception>();
                long progressReminder = this.Progress;
                foreach (FileOperation op in Ops)
                {
                    if (op.CurrentState == State.Completed) continue;
                    if (op.CurrentState == State.Skipped) continue;
                    try
                    {
                        // Assert session is not cancelled
                        if (session.CancelSrc.IsCancellationRequested) { SetState(State.Cancelled); return; }
                        // Run op
                        op.Run();
                        // 
                        if (!EffectivePolicy.HasFlag(Policy.BatchContinueOnError) && op.CurrentState == State.Error) throw new AggregateException(op.Errors);
                        // Move progress
                        if (op.Progress > 0L) this.Progress += op.Progress;
                        if (op.TotalLength > 0L) this.TotalLength += op.TotalLength;

                        if (op.Progress > 0L || op.TotalLength > 0L)
                        {
                            // Update progress position
                            progressReminder += op.Progress;
                            // Time to send progress event
                            if (session.ProgressInterval > 0L && progressReminder > session.ProgressInterval && session.HasObservers)
                            {
                                progressReminder %= session.ProgressInterval;
                                session.DispatchEvent(new Event.Progress(this, Progress, TotalLength));
                            }
                        }
                    }
                    catch (Exception e) when (session.Policy.HasFlag(Policy.BatchContinueOnError))
                    {
                        errors.Add(e);
                    }
                }
                // Throw captured exceptions
                if (errors.Count > 0) throw new AggregateException(errors.ToArray());
            }

            /// <summary>Create rollback operation.</summary>
            /// <returns>rollback or null</returns>
            public override FileOperation CreateRollback()
            {
                if (CanRollback)
                {
                    return new Batch(session, OpPolicy, Ops.Select(op => op.CreateRollback()).Where(rb => rb != null).Reverse());
                }
                return null;
            }

            /// <summary>Print info</summary>
            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("Batch(State=");
                sb.Append(CurrentState);
                sb.Append(", Ops=");
                foreach (var op in Ops)
                {
                    sb.AppendLine();
                    sb.Append("    ");
                    sb.Append(op.ToString());
                }
                sb.AppendLine(")");
                return sb.ToString();
            }
        }

        /// <summary>Delete file or directory</summary>
        public class Delete : FileOperation
        {
            /// <summary>Target filesystem</summary>
            protected IFileSystem fileSystem;
            /// <summary>Target path</summary>
            protected string path;
            /// <summary>Target filesystem</summary>
            public override IFileSystem FileSystem => fileSystem;
            /// <summary>Target path</summary>
            public override String Path => path;
            /// <summary>Delete tree recursively</summary>
            public bool Recurse { get; protected set; }
            /// <summary>Rollback operation.</summary>
            protected FileOperation rollback;
            /// <summary>Target filesystem option or token</summary>
            protected IOption Option;


            /// <summary>
            /// Create delete directory op.
            /// </summary>
            /// <param name="session"></param>
            /// <param name="filesystem"></param>
            /// <param name="path"></param>
            /// <param name="recurse"></param>
            /// <param name="option">(optional) </param>
            /// <param name="policy">(optional) Responds to <see cref="Policy.DstThrow"/> and <see cref="Policy.DstSkip"/> policies.</param>
            /// <param name="rollback">(optional) Rollback operation</param>
            public Delete(Session session, IFileSystem filesystem, string path, bool recurse, IOption option = null, Policy policy = Policy.Unset, FileOperation rollback = null) : base(session, policy)
            {
                this.fileSystem = filesystem ?? throw new ArgumentNullException(nameof(filesystem));
                this.path = path ?? throw new ArgumentNullException(nameof(path));
                this.Recurse = recurse;
                this.rollback = rollback;
                this.CanRollback = rollback != null;
                this.Option = option;
            }

            /// <summary>Estimate viability of operation.</summary>
            /// <exception cref="FileNotFoundException">If <see cref="Path"/> is not found.</exception>
            protected override void InnerEstimate()
            {
                // Assert dest
                if (EffectivePolicy.HasFlag(Policy.DstThrow) && FileSystem.CanGetEntry())
                {
                    try
                    {
                        IEntry e = FileSystem.GetEntry(Path, this.Option.OptionIntersection(session.Option));
                        // Not found
                        if (e == null)
                        {
                            // Skip
                            if (EffectivePolicy.HasFlag(Policy.DstSkip)) { CanRollback = true; SetState(State.Skipped); return; }
                            // Throw
                            if (EffectivePolicy.HasFlag(Policy.DstThrow)) throw new FileNotFoundException(Path);
                        }
                    }
                    catch (NotSupportedException) { }
                }
            }

            /// <summary>Run op</summary>
            /// <exception cref="FileNotFoundException">If <see cref="Path"/> is not found.</exception>
            protected override void InnerRun()
            {
                try
                {
                    // Delete directory
                    FileSystem.Delete(Path, Recurse, this.Option.OptionIntersection(session.Option));
                }
                catch (FileNotFoundException) when (!EffectivePolicy.HasFlag(Policy.DstThrow)) { }
                catch (DirectoryNotFoundException) when (!EffectivePolicy.HasFlag(Policy.DstThrow)) { }
            }

            /// <summary>Create rollback op</summary>
            /// <returns></returns>
            public override FileOperation CreateRollback()
                => CurrentState == State.Completed ? rollback : null;

            /// <summary>Print info</summary>
            public override string ToString() => $"Delete(Path={Path}, Recurse={Recurse}, State={CurrentState})";
        }

        /// <summary>Create directory</summary>
        public class CreateDirectory : FileOperation
        {
            /// <summary>Target filesystem</summary>
            protected IFileSystem fileSystem;
            /// <summary>Target path</summary>
            protected string path;
            /// <summary>Target filesystem</summary>
            public override IFileSystem FileSystem => fileSystem;
            /// <summary>Target path</summary>
            public override String Path => path;
            /// <summary>Directories created in Run().</summary>
            protected List<string> DirectoriesCreated = new List<string>();
            /// <summary>Target filesystem option or token</summary>
            protected IOption Option;

            /// <summary>
            /// Create create directory op.
            /// </summary>
            /// <param name="session"></param>
            /// <param name="filesystem"></param>
            /// <param name="path"></param>
            /// <param name="option"></param>
            /// <param name="policy">(optional) Responds to <see cref="Policy.DstThrow"/>, <see cref="Policy.DstSkip"/> and <see cref="Policy.DstOverwrite"/> policies</param>
            public CreateDirectory(Session session, IFileSystem filesystem, string path, IOption option = null, Policy policy = Policy.Unset) : base(session, policy)
            {
                this.fileSystem = filesystem ?? throw new ArgumentNullException(nameof(filesystem));
                this.path = path ?? throw new ArgumentNullException(nameof(path));
                // Can rollback if can delete
                this.CanRollback = filesystem.CanDelete();
                this.Option = option;
            }

            /// <summary>Estimate viability of operation.</summary>
            /// <exception cref="FileNotFoundException">If <see cref="Path"/> is not found.</exception>
            protected override void InnerEstimate()
            {
                // Cannot create directory
                if (!FileSystem.CanCreateDirectory()) throw new NotSupportedException("CreateDirectory");
                // Test that directory already exists
                if (FileSystem.CanGetEntry())
                {
                    try
                    {
                        IEntry e = FileSystem.GetEntry(Path, this.Option.OptionIntersection(session.Option));
                        // Directory already exists
                        if (e != null)
                        {
                            // Overwrite
                            if (EffectivePolicy.HasFlag(Policy.DstThrow))
                            {
                                // Nothing will be done
                                CanRollback = true;
                                if (e.IsDirectory()) throw new FileSystemExceptionDirectoryExists(FileSystem, Path);
                                else if (e.IsFile()) throw new FileSystemExceptionFileExists(FileSystem, Path);
                                else throw new FileSystemExceptionEntryExists(FileSystem, Path);
                            }
                            // Skip
                            if (EffectivePolicy.HasFlag(Policy.DstSkip)) { CanRollback = true; SetState(State.Skipped); return; }
                            // Delete prev
                            if (EffectivePolicy.HasFlag(Policy.DstOverwrite)) 
                            {
                                // Is going to be deleted
                                if (e.IsFile()) CanRollback = false;
                                // Is going to be skipped
                                else if (e.IsDirectory()) CanRollback = true;
                            }
                        } else
                        {
                            // Directory not found, can rollback
                            CanRollback = true;
                        }
                    }
                    catch (NotSupportedException) { }
                }
            }

            /// <summary>Create direcotry</summary>
            /// <exception cref="FileNotFoundException">If <see cref="Path"/> is not found.</exception>
            /// <exception cref="FileSystemExceptionEntryExists">If file or directory already existed at <see cref="Path"/> and <see cref="Policy.DstThrow"/> is true.</exception>
            protected override void InnerRun()
            {
                // Cannot get entry
                if (!FileSystem.CanGetEntry()) { CreateBlind(); return; }

                try
                {

                    // Test that directory already exists
                    if (FileSystem.CanGetEntry())
                    {
                        try
                        {
                            IEntry e = FileSystem.GetEntry(Path, this.Option.OptionIntersection(session.Option));
                            // Directory already exists
                            if (e != null)
                            {
                                // Throw
                                if (EffectivePolicy.HasFlag(Policy.DstThrow))
                                {
                                    // Nothing is done
                                    CanRollback = true;
                                    if (e.IsDirectory()) throw new FileSystemExceptionDirectoryExists(FileSystem, Path);
                                    else if (e.IsFile()) throw new FileSystemExceptionFileExists(FileSystem, Path);
                                    else throw new FileSystemExceptionEntryExists(FileSystem, Path);
                                }
                                // Skip
                                if (EffectivePolicy.HasFlag(Policy.DstSkip)) { CanRollback = true; SetState(State.Skipped); return; }
                                // Delete prev
                                if (EffectivePolicy.HasFlag(Policy.DstOverwrite))
                                {
                                    // Delete File
                                    if (e.IsFile()) { CanRollback = false; FileSystem.Delete(Path, recurse: false, this.Option.OptionIntersection(session.Option)); }
                                    // Skip
                                    else if (e.IsDirectory()) { CanRollback = true; SetState(State.Skipped); return; }
                                }
                            }
                        }
                        catch (NotSupportedException) { }
                    }

                    // Enumerate paths
                    PathEnumerator etor = new PathEnumerator(Path, true);
                    while (etor.MoveNext())
                    {
                        string path = Path.Substring(0, etor.Current.Length+etor.Current.Start);
                        IEntry e = FileSystem.GetEntry(path, this.Option.OptionIntersection(session.Option));

                        // Entry exists
                        if (e != null) continue;

                        FileSystem.CreateDirectory(path, this.Option.OptionIntersection(session.Option));
                        DirectoriesCreated.Add(path);
                    }
                }
                catch (NotSupportedException) {
                    CreateBlind();
                }
            }

            /// <summary>Create directory blind</summary>
            void CreateBlind()
            {
                try
                {
                    // Create directory
                    FileSystem.CreateDirectory(Path, this.Option.OptionIntersection(session.Option));
                    //
                    DirectoriesCreated.Add(Path);
                }
                catch (FileSystemExceptionFileExists) when (!EffectivePolicy.HasFlag(Policy.DstThrow)) { }
                catch (FileSystemExceptionDirectoryExists) when (!EffectivePolicy.HasFlag(Policy.DstThrow)) { }
            }

            /// <summary>Create rollback</summary>
            /// <returns>op or null</returns>
            public override FileOperation CreateRollback()
            {
                // Nothing to do
                if (DirectoriesCreated.Count == 0) return null;
                // Delete the one directory we created
                if (DirectoriesCreated.Count == 1) return new Delete(session, FileSystem, DirectoriesCreated[0], false, Option, OpPolicy);
                // Delete the directories we created, in reverse order
                return new Batch(session, OpPolicy, DirectoriesCreated.Select(d => new Delete(session, FileSystem, d, false, Option, OpPolicy)).Reverse());
            }

            /// <summary>Print info</summary>
            public override string ToString() => $"CreateDirectory(Path={Path}, CurrentState={CurrentState})";
        }

        /// <summary>Move/rename file or directory</summary>
        public class Move : FileOperation
        {
            /// <summary>Source filesystem</summary>
            protected IFileSystem srcFileSystem;
            /// <summary>Target filesystem</summary>
            protected IFileSystem dstFileSystem;
            /// <summary>Source path</summary>
            protected string srcPath;
            /// <summary>Target path</summary>
            protected string dstPath;

            /// <summary>Target filesystem</summary>
            public override IFileSystem FileSystem => dstFileSystem;
            /// <summary>Target path</summary>
            public override String Path => dstPath;
            /// <summary>Source filesystem</summary>
            public override IFileSystem SrcFileSystem => srcFileSystem;
            /// <summary>Source path</summary>
            public override string SrcPath => srcPath;

            /// <summary>Src filesystem option or token</summary>
            protected IOption srcOption;
            /// <summary>Target filesystem option or token</summary>
            protected IOption Option;

            /// <summary>Set to true if <see cref="Run"/> moved src.</summary>
            bool moved;
            /// <summary>Set to true if <see cref="Run"/> deleted previous dst.</summary>
            bool deletedPrev;

            /// <summary>Create move op.</summary>
            public Move(Session session, IFileSystem srcFilesystem, string srcPath, IFileSystem dstFilesystem, string dstPath, IOption srcOption = null, IOption dstOption = null, Policy policy = Policy.Unset) : base(session, policy)
            {
                this.srcFileSystem = srcFilesystem ?? throw new ArgumentNullException(nameof(srcFilesystem));
                this.dstFileSystem = dstFilesystem ?? throw new ArgumentNullException(nameof(dstFilesystem));
                this.srcPath = srcPath ?? throw new ArgumentNullException(nameof(srcPath));
                this.dstPath = dstPath ?? throw new ArgumentNullException(nameof(dstPath));
                if (srcFileSystem != dstFileSystem) throw new ArgumentException($"Move implementation requires that {nameof(srcFilesystem)} and {nameof(dstFilesystem)} are same. Use MoveTree instead.");
                this.srcOption = srcOption;
                this.Option = dstOption;
            }

            /// <summary>Estimate viability of operation.</summary>
            /// <exception cref="FileNotFoundException">If <see cref="SrcPath"/> is not found.</exception>
            /// <exception cref="FileSystemExceptionEntryExists">If entry at <see cref="Path"/> already exists.</exception>
            protected override void InnerEstimate()
            {
                // Cannot move
                if (!SrcFileSystem.CanMove()) throw new NotSupportedException("Move");

                // Test that source file exists
                if (SrcFileSystem.CanGetEntry())
                {
                    try
                    {
                        IEntry e = SrcFileSystem.GetEntry(SrcPath, this.srcOption.OptionIntersection(session.Option));
                        // Src not found
                        if (e == null)
                        {
                            // Throw
                            if (EffectivePolicy.HasFlag(Policy.SrcThrow)) throw new FileNotFoundException(SrcPath);
                            // Skip
                            if (EffectivePolicy.HasFlag(Policy.SrcSkip)) { SetState(State.Skipped); return; }
                        }
                    }
                    catch (NotSupportedException)
                    {
                        // GetEntry is not supported
                    }
                }

                // Test that dest file doesn't exist
                if (dstFileSystem.CanGetEntry())
                {
                    try
                    {
                        IEntry dstEntry = dstFileSystem.GetEntry(Path, this.Option.OptionIntersection(session.Option));
                        if (dstEntry != null)
                        {
                            // Dst exists
                            if (EffectivePolicy.HasFlag(Policy.DstThrow))
                            {
                                if (dstEntry.IsFile()) throw new FileSystemExceptionFileExists(FileSystem, Path);
                                else if (dstEntry.IsDirectory()) throw new FileSystemExceptionDirectoryExists(FileSystem, Path);
                                else throw new FileSystemExceptionEntryExists(FileSystem, Path);
                            }

                            // Skip op
                            else if (EffectivePolicy.HasFlag(Policy.DstSkip))
                            {
                                SetState(State.Skipped);
                                // Nothing to rollback, essentially ok
                                CanRollback = true;
                            }

                            // Overwrite
                            else if (EffectivePolicy.HasFlag(Policy.DstOverwrite))
                            {
                                // Cannot rollback
                                CanRollback = false;
                            }
                        }
                        else
                        {
                            // Prev file didn't exist, can be rollbacked
                            CanRollback = true;
                        }
                    }
                    catch (NotSupportedException)
                    {
                        // GetEntry is not supported
                    }
                }
            }

            /// <summary>Run operation</summary>
            /// <exception cref="IOException"></exception>
            /// <exception cref="Exception">Unexpected error</exception>
            protected override void InnerRun()
            {
                // Test dst
                try
                {
                    IEntry dstEntry = dstFileSystem.GetEntry(dstPath, this.Option.OptionIntersection(session.Option));
                    
                    // Dst exists
                    if (dstEntry != null)
                    {
                        // Dst exists
                        if (EffectivePolicy.HasFlag(Policy.DstThrow))
                        {
                            if (dstEntry.IsFile()) throw new FileSystemExceptionFileExists(FileSystem, Path);
                            else if (dstEntry.IsDirectory()) throw new FileSystemExceptionDirectoryExists(FileSystem, Path);
                            else throw new FileSystemExceptionEntryExists(FileSystem, Path);
                        }

                        // Skip op
                        if (EffectivePolicy.HasFlag(Policy.DstSkip)) { CanRollback = true; moved = false; deletedPrev = false; return; }

                        // Delete prev
                        if (EffectivePolicy.HasFlag(Policy.DstOverwrite)) { FileSystem.Delete(Path, recurse: false, this.Option.OptionIntersection(session.Option)); deletedPrev = true; CanRollback = false; }
                    } else
                    {
                        CanRollback = true;
                    }

                }
                catch (NotSupportedException)
                {
                    // Dst cannot get entry
                }

                // Move
                SrcFileSystem.Move(SrcPath, dstPath, srcOption);
                moved = true;
            }

            /// <summary>Create rollback</summary>
            /// <returns>rollback or null</returns>
            public override FileOperation CreateRollback()
            {
                if (moved && !deletedPrev) return new Move(session, dstFileSystem, dstPath, SrcFileSystem, SrcPath, Option, srcOption, OpPolicy);
                return null;
            }

            /// <summary>Print info</summary>
            public override string ToString() => $"Move(Src={srcPath}, Dst={dstPath}, State={CurrentState})";
        }

        /// <summary>Copy file</summary>
        public class CopyFile : FileOperation
        {
            /// <summary>Source filesystem</summary>
            protected IFileSystem srcFileSystem;
            /// <summary>Target filesystem</summary>
            protected IFileSystem dstFileSystem;
            /// <summary>Source path</summary>
            protected string srcPath;
            /// <summary>Target path</summary>
            protected string dstPath;

            /// <summary>Target filesystem</summary>
            public override IFileSystem FileSystem => dstFileSystem;
            /// <summary>Target path</summary>
            public override String Path => dstPath;
            /// <summary>Source filesystem</summary>
            public override IFileSystem SrcFileSystem => srcFileSystem;
            /// <summary>Source path</summary>
            public override string SrcPath => srcPath;

            /// <summary>Src filesystem option or token</summary>
            protected IOption srcOption;
            /// <summary>Target filesystem option or token</summary>
            protected IOption Option;

            /// <summary>Was file overwritten</summary>
            public bool Overwritten { get; protected set; }

            /// <summary>Set to true if <see cref="Run"/> created file.</summary>
            protected bool createdFile;
            /// <summary>Set to true if <see cref="Run"/> copied.</summary>
            protected bool copied;
            /// <summary>Set to true if <see cref="Run"/> previous entry existed.</summary>
            protected bool prevExisted;

            /// <summary>Create copy file op.</summary>
            public CopyFile(Session session, IFileSystem srcFilesystem, string srcPath, IFileSystem dstFilesystem, string dstPath, IOption srcOption = null, IOption dstOption = null, Policy policy = Policy.Unset) : base(session, policy)
            {
                this.srcFileSystem = srcFilesystem ?? throw new ArgumentNullException(nameof(srcFilesystem));
                this.dstFileSystem = dstFilesystem ?? throw new ArgumentNullException(nameof(dstFilesystem));
                this.srcPath = srcPath ?? throw new ArgumentNullException(nameof(srcPath));
                this.dstPath = dstPath ?? throw new ArgumentNullException(nameof(dstPath));
                this.srcOption = srcOption;
                this.Option = dstOption;
            }

            /// <summary>Estimate viability of operation.</summary>
            /// <exception cref="FileNotFoundException">If <see cref="SrcPath"/> is not found.</exception>
            /// <exception cref="FileSystemExceptionEntryExists">If <see cref="Path"/> already exists.</exception>
            protected override void InnerEstimate()
            {
                // Cannot create
                if (!FileSystem.CanOpen() || !FileSystem.CanCreateFile()) throw new NotSupportedException("Copy");

                // Test that source file exists
                if (SrcFileSystem.CanGetEntry())
                {
                    try
                    {
                        IEntry e = SrcFileSystem.GetEntry(SrcPath, srcOption.OptionIntersection(session.Option));
                        // Src not found
                        if (e == null)
                        {
                            // Throw
                            if (EffectivePolicy.HasFlag(Policy.SrcThrow)) throw new FileNotFoundException(SrcPath);
                            // Skip
                            if (EffectivePolicy.HasFlag(Policy.SrcSkip)) { SetState(State.Skipped); return; }
                        }
                        // Set length
                        if (e.Length() > 0L) this.TotalLength = e.Length();
                    }
                    catch (NotSupportedException)
                    {
                        // GetEntry is not supported
                    }
                }

                // Test that dest file doesn't exist
                if (FileSystem.CanGetEntry())
                {
                    try
                    {
                        IEntry dstEntry = dstFileSystem.GetEntry(Path, Option.OptionIntersection(session.Option));
                        prevExisted = dstEntry != null;
                        if (dstEntry != null)
                        {
                            // Dst exists
                            if (EffectivePolicy.HasFlag(Policy.DstThrow))
                            {
                                if (dstEntry.IsFile()) throw new FileSystemExceptionFileExists(FileSystem, Path);
                                else if (dstEntry.IsDirectory()) throw new FileSystemExceptionDirectoryExists(FileSystem, Path);
                                else throw new FileSystemExceptionEntryExists(FileSystem, Path);
                            }

                            // Skip op
                            else if (EffectivePolicy.HasFlag(Policy.DstSkip))
                            {
                                SetState(State.Skipped);
                                // Nothing to rollback, essentially ok
                                CanRollback = true;
                            }

                            // Overwrite
                            else if (EffectivePolicy.HasFlag(Policy.DstOverwrite))
                            {
                                // Cannot rollback
                                CanRollback = false;
                            }
                        }
                        else
                        {
                            // Prev file didn't exist, can be rollbacked
                            CanRollback = true;
                        }
                    }
                    catch (NotSupportedException)
                    {
                        // GetEntry is not supported
                    }
                }
            }

            /// <summary>Run operation</summary>
            /// <exception cref="IOException"></exception>
            /// <exception cref="Exception">Unexpected error</exception>
            protected override void InnerRun()
            {
                InnerEstimate();

                // Queue of blocks
                BlockingCollection<Block> queue = new BlockingCollection<Block>();
                try
                {
                    // CancelToken to monitor
                    CancellationToken token = session.CancelSrc.Token;

                    // Create mode
                    FileMode createMode = session.Policy.HasFlag(Policy.DstOverwrite) ? FileMode.Create : FileMode.CreateNew;

                    // Write stream
                    using (Stream s = dstFileSystem.Open(dstPath, createMode, FileAccess.Write, FileShare.ReadWrite, Option.OptionIntersection(session.Option)))
                    {
                        // Created file
                        this.createdFile = !prevExisted;
                        // We have overwritten the prev file
                        this.Overwritten = prevExisted && createMode == FileMode.Create;

                        // Start read thread
                        new Thread(() => ReadFile(queue)).Start();

                        long progressReminder = 0L;

                        while (true)
                        {
                            // Read block
                            Block block = queue.Take(token);
                            try
                            {
                                // Assert session is not cancelled
                                if (session.CancelSrc.IsCancellationRequested && CurrentState != State.Error) { SetState(State.Cancelled); return; }
                                // Read error, abort
                                if (CurrentState != State.Running) break;
                                // We got some data
                                if (block.count > 0)
                                {
                                    // Write data
                                    s.Write(block.data, 0, block.count);
                                    // Update progress position
                                    Progress += block.count;
                                    progressReminder += block.count;
                                    // Time to send progress event
                                    if (session.ProgressInterval > 0L && progressReminder > session.ProgressInterval && session.HasObservers)
                                    {
                                        progressReminder %= session.ProgressInterval;
                                        session.DispatchEvent(new Event.Progress(this, Progress, TotalLength));
                                    }
                                }
                                // EOF or error
                                else if (block.count <= 0) break;
                            }
                            finally
                            {
                                // Return block to pool
                                if (block.data != null) session.BlockPool.Return(block.data);
                            }
                        }

                        copied = true;
                    }
                }
                // Capture error and set state
                catch (Exception e) when (SetError(e)) { }
                finally
                {
                    // Return blocks to pool
                    Block b;
                    while (queue.TryTake(out b)) if (b.data != null) session.BlockPool.Return(b.data);
                }
            }

            /// <summary>
            /// Method that reads source file and places blocks into <paramref name="queue"/>. The receiver must return blocks to blockpool.
            /// 
            /// This method is to be ran in a concurrent thread.
            /// 
            /// If cancel is requested, aborts.
            /// 
            /// If ioexception occurs, then sets op state to error.
            /// </summary>
            /// <param name="queue"></param>
            void ReadFile(BlockingCollection<Block> queue)
            {
                try
                {
                    // CancelToken to monitor
                    CancellationToken token = session.CancelSrc.Token;

                    if (token.IsCancellationRequested) return;

                    // Open file
                    Stream s = SrcFileSystem.Open(SrcPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, srcOption.OptionIntersection(session.Option));
                    try
                    {
                        int len = 0;
                        do
                        {
                            // Cancelled, abort
                            if (token.IsCancellationRequested || CurrentState != State.Running) { queue.Add(new Block { data = null, count = -1 /* cancellation */ }); return; }
                            // Allocate block
                            byte[] data = session.BlockPool.Allocate();
                            // Read data to block
                            len = s.Read(data, 0, data.Length);
                            // Pass block
                            if (len > 0) queue.Add(new Block { data = data, count = len });
                        } while (len > 0);
                        // Notify of eos with 0 length
                        queue.Add(new Block { data = null, count = 0 });
                    }
                    finally
                    {
                        s.Dispose();
                    }
                }
                // Capture error and set state
                catch (Exception e)
                {
                    // Set state of op to error
                    SetError(e);
                    // Notify of error with -2 length
                    queue.Add(new Block { data = null, count = -2 });
                }
            }

            /// <summary>Create rollback operation.</summary>
            /// <returns>null or rollback operation</returns>
            public override FileOperation CreateRollback()
                => createdFile && !Overwritten ? new Delete(session, FileSystem, Path, false, Option, OpPolicy) : null;

            struct Block
            {
                public byte[] data;
                public int count;
            }

            /// <summary>Print info</summary>
            public override string ToString() => $"CopyFile(Src={srcPath}, Dst={dstPath}, State={CurrentState})";
        }

        /// <summary>Copy a file or directory tree</summary>
        public class CopyTree : Batch
        {
            /// <summary>Source filesystem</summary>
            protected IFileSystem srcFileSystem;
            /// <summary>Target filesystem</summary>
            protected IFileSystem dstFileSystem;
            /// <summary>Source path</summary>
            protected string srcPath;
            /// <summary>Target path</summary>
            protected string dstPath;

            /// <summary>Target filesystem</summary>
            public override IFileSystem FileSystem => dstFileSystem;
            /// <summary>Target path</summary>
            public override String Path => dstPath;
            /// <summary>Source filesystem</summary>
            public override IFileSystem SrcFileSystem => srcFileSystem;
            /// <summary>Source path</summary>
            public override string SrcPath => srcPath;

            /// <summary>Src filesystem option or token</summary>
            protected IOption srcOption;
            /// <summary>Target filesystem option or token</summary>
            protected IOption Option;

            /// <summary>Create move op.</summary>
            public CopyTree(Session session, IFileSystem srcFilesystem, string srcPath, IFileSystem dstFilesystem, string dstPath, IOption srcOption = null, IOption dstOption = null, Policy policy = Policy.Unset) : base(session, policy)
            {
                this.srcFileSystem = srcFilesystem ?? throw new ArgumentNullException(nameof(srcFilesystem));
                this.dstFileSystem = dstFilesystem ?? throw new ArgumentNullException(nameof(dstFilesystem));
                this.srcPath = srcPath ?? throw new ArgumentNullException(nameof(srcPath));
                this.dstPath = dstPath ?? throw new ArgumentNullException(nameof(dstPath));
                this.srcOption = srcOption;
                this.Option = dstOption;
            }

            /// <summary>Estimate viability of operation.</summary>
            /// <exception cref="FileNotFoundException">If <see cref="SrcPath"/> is not found.</exception>
            /// <exception cref="FileSystemExceptionFileExists">If <see cref="Path"/> already exists.</exception>
            protected override void InnerEstimate()
            {
                PathConverter pathConverter = new PathConverter(SrcPath, Path);
                List<IEntry> queue = new List<IEntry>();

                // Src
                IEntry e = SrcFileSystem.GetEntry(SrcPath, srcOption.OptionIntersection(session.Option));
                // Src not found
                if (e == null)
                {
                    // Throw
                    if (EffectivePolicy.HasFlag(Policy.SrcThrow)) throw new FileNotFoundException(SrcPath);
                    // Skip
                    if (EffectivePolicy.HasFlag(Policy.SrcSkip)) { SetState(State.Skipped); return; }
                    // Fail anyway
                    throw new FileNotFoundException(SrcPath);
                }

                queue.Add(e);
                while (queue.Count > 0)
                {
                    try
                    {
                        // Next entry
                        int lastIx = queue.Count - 1;
                        IEntry entry = queue[lastIx];
                        queue.RemoveAt(lastIx);

                        // Omit package mounts
                        if (session.Policy.HasFlag(Policy.OmitMountedPackages) && entry.IsPackageMount()) continue;

                        // Process directory
                        if (entry.IsDirectory())
                        {
                            // Browse children
                            IEntry[] children = SrcFileSystem.Browse(entry.Path, srcOption.OptionIntersection(session.Option));
                            // Assert children don't refer to the parent of the parent
                            foreach (IEntry child in children) if (entry.Path.StartsWith(child.Path)) throw new IOException($"{child.Path} cannot be child of {entry.Path}");
                            // Visit child
                            for (int i = children.Length - 1; i >= 0; i--) queue.Add(children[i]);
                            // Convert path
                            string _dstPath;
                            if (!pathConverter.ParentToChild(entry.Path, out _dstPath)) throw new Exception("Failed to convert path");
                            // Add op
                            if (_dstPath != "") Ops.Add(new CreateDirectory(session, FileSystem, _dstPath, Option.OptionIntersection(session.Option), OpPolicy));
                        }

                        // Process file
                        else if (entry.IsFile())
                        {
                            // Convert path
                            string _dstPath;
                            if (!pathConverter.ParentToChild(entry.Path, out _dstPath)) throw new Exception("Failed to convert path");
                            // Add op
                            Ops.Add(new CopyFile(session, SrcFileSystem, entry.Path, FileSystem, _dstPath, srcOption.OptionIntersection(session.Option), Option.OptionIntersection(session.Option), OpPolicy));
                        }
                    }
                    catch (Exception error) when (SetError(error)) { }
                }

                base.InnerEstimate();
            }

            /// <summary>Print info</summary>
            public override string ToString() => $"CopyTree(Src={SrcPath}, Dst={Path})";
        }

        /// <summary>Delete a file or directory tree</summary>
        public class DeleteTree : Batch
        {
            /// <summary>Target filesystem</summary>
            protected IFileSystem fileSystem;
            /// <summary>Target path</summary>
            protected string path;
            /// <summary>Target filesystem</summary>
            public override IFileSystem FileSystem => fileSystem;
            /// <summary>Target path</summary>
            public override String Path => path;

            /// <summary>Src filesystem option or token</summary>
            protected IOption srcOption;
            /// <summary>Target filesystem option or token</summary>
            protected IOption Option;

            /// <summary>Create move op.</summary>
            public DeleteTree(Session session, IFileSystem filesystem, string path, IOption srcOption = null, IOption dstOption = null, Policy policy = Policy.Unset) : base(session, policy)
            {
                this.fileSystem = filesystem ?? throw new ArgumentNullException(nameof(filesystem));
                this.path = path ?? throw new ArgumentNullException(nameof(path));
                this.srcOption = srcOption;
                this.Option = dstOption;
            }

            /// <summary>Estimate viability of operation.</summary>
            /// <exception cref="FileNotFoundException">If <see cref="Path"/> is not found.</exception>
            /// <exception cref="FileSystemExceptionFileExists">If <see cref="Path"/> already exists.</exception>
            protected override void InnerEstimate()
            {
                List<Delete> dirDeletes = new List<Delete>();
                try
                {
                    List<IEntry> queue = new List<IEntry>();
                    IEntry e = FileSystem.GetEntry(Path, Option.OptionIntersection(session.Option));
                    if (e == null) throw new FileNotFoundException(Path);
                    queue.Add(e);
                    while (queue.Count > 0)
                    {
                        try
                        {
                            // Next entry
                            int lastIx = queue.Count - 1;
                            IEntry entry = queue[lastIx];
                            queue.RemoveAt(lastIx);

                            // Omit package mounts
                            if (session.Policy.HasFlag(Policy.OmitMountedPackages) && entry.IsPackageMount()) continue;

                            // Process directory
                            if (entry.IsDirectory())
                            {
                                // Browse children
                                IEntry[] children = FileSystem.Browse(entry.Path, Option.OptionIntersection(session.Option));
                                // Assert children don't refer to the parent of the parent
                                foreach (IEntry child in children) if (entry.Path.StartsWith(child.Path)) throw new IOException($"{child.Path} cannot be child of {entry.Path}");
                                // Visit children
                                for (int i = children.Length - 1; i >= 0; i--) queue.Add(children[i]);
                                // Add op
                                dirDeletes.Add(new Delete(session, FileSystem, entry.Path, false));
                            }

                            // Process file
                            else if (entry.IsFile())
                            {
                                // Add op
                                Ops.Add(new Delete(session, FileSystem, entry.Path, false, Option.OptionIntersection(session.Option), OpPolicy));
                            }
                        }
                        catch (Exception error) when (SetError(error)) { }
                    }
                }
                finally
                {
                    // Add directory deletes
                    for (int i = dirDeletes.Count - 1; i >= 0; i--)
                        Ops.Add(dirDeletes[i]);
                }

                // Estimate added ops
                base.InnerEstimate();
            }

            /// <summary>Print info</summary>
            public override string ToString() => $"DeleteTree({Path})";
        }

        /// <summary>
        /// Move/rename a file or directory tree by copying and deleting files.
        /// </summary>
        public class TransferTree : Batch
        {
            /// <summary>Source filesystem</summary>
            protected IFileSystem srcFileSystem;
            /// <summary>Target filesystem</summary>
            protected IFileSystem dstFileSystem;
            /// <summary>Source path</summary>
            protected string srcPath;
            /// <summary>Target path</summary>
            protected string dstPath;

            /// <summary>Target filesystem</summary>
            public override IFileSystem FileSystem => dstFileSystem;
            /// <summary>Target path</summary>
            public override String Path => dstPath;
            /// <summary>Source filesystem</summary>
            public override IFileSystem SrcFileSystem => srcFileSystem;
            /// <summary>Source path</summary>
            public override string SrcPath => srcPath;

            /// <summary>Src filesystem option or token</summary>
            protected IOption srcOption;
            /// <summary>Target filesystem option or token</summary>
            protected IOption Option;

            /// <summary>Create move op.</summary>
            public TransferTree(Session session, IFileSystem srcFilesystem, string srcPath, IFileSystem dstFilesystem, string dstPath, IOption srcOption = null, IOption dstOption = null, Policy policy = Policy.Unset) : base(session, policy)
            {
                this.srcFileSystem = srcFilesystem ?? throw new ArgumentNullException(nameof(srcFilesystem));
                this.dstFileSystem = dstFilesystem ?? throw new ArgumentNullException(nameof(dstFilesystem));
                this.srcPath = srcPath ?? throw new ArgumentNullException(nameof(srcPath));
                this.dstPath = dstPath ?? throw new ArgumentNullException(nameof(dstPath));
                this.srcOption = srcOption;
                this.Option = dstOption;
            }

            /// <summary>Scan tree, and add ops</summary>
            protected override void InnerEstimate()
            {
                PathConverter pathConverter = new PathConverter(SrcPath, Path);
                List<IEntry> queue = new List<IEntry>();

                // Src
                IEntry e = SrcFileSystem.GetEntry(SrcPath, srcOption);

                // Src not found
                if (e == null)
                {
                    // Throw
                    if (EffectivePolicy.HasFlag(Policy.SrcThrow)) throw new FileNotFoundException(SrcPath);
                    // Skip
                    if (EffectivePolicy.HasFlag(Policy.SrcSkip)) { SetState(State.Skipped); return; }
                    // Fail anyway
                    throw new FileNotFoundException(SrcPath);
                }

                List<Delete> deleteDirs = new List<Delete>();
                queue.Add(e);
                while (queue.Count > 0)
                {
                    try
                    {
                        // Next entry
                        int lastIx = queue.Count - 1;
                        IEntry entry = queue[lastIx];
                        queue.RemoveAt(lastIx);

                        // Omit package mounts
                        if (session.Policy.HasFlag(Policy.OmitMountedPackages) && entry.IsPackageMount()) continue;

                        // Process directory
                        if (entry.IsDirectory())
                        {
                            // Browse children
                            IEntry[] children = SrcFileSystem.Browse(entry.Path, srcOption.OptionIntersection(session.Option));
                            // Assert children don't refer to the parent of the parent
                            foreach (IEntry child in children) if (entry.Path.StartsWith(child.Path)) throw new IOException($"{child.Path} cannot be child of {entry.Path}");
                            // Visit child
                            for (int i = children.Length - 1; i >= 0; i--) queue.Add(children[i]);
                            // Convert path
                            string _dstPath;
                            if (!pathConverter.ParentToChild(entry.Path, out _dstPath)) throw new Exception("Failed to convert path");
                            // Add op
                            if (_dstPath != "")
                            {
                                Ops.Add(new CreateDirectory(session, FileSystem, _dstPath, Option.OptionIntersection(session.Option), OpPolicy));
                                deleteDirs.Add(new Delete(session, SrcFileSystem, entry.Path, false, srcOption.OptionIntersection(session.Option), OpPolicy|Policy.EstimateOnRun,
                                    rollback: new CreateDirectory(session, SrcFileSystem, entry.Path, srcOption.OptionIntersection(session.Option), OpPolicy)));
                            }
                        }

                        // Process file
                        else if (entry.IsFile())
                        {
                            // Convert path
                            string _dstPath;
                            if (!pathConverter.ParentToChild(entry.Path, out _dstPath)) throw new Exception("Failed to convert path");
                            // Add op
                            Ops.Add(new CopyFile(session, SrcFileSystem, entry.Path, FileSystem, _dstPath, srcOption.OptionIntersection(session.Option), Option.OptionIntersection(session.Option), OpPolicy));
                            Ops.Add(new Delete(session, SrcFileSystem, entry.Path, false, srcOption.OptionIntersection(session.Option), OpPolicy, 
                                rollback: new CopyFile(session, FileSystem, _dstPath, SrcFileSystem, entry.Path, Option.OptionIntersection(session.Option), srcOption.OptionIntersection(session.Option), OpPolicy)));
                        }

                    }
                    catch (Exception error) when (SetError(error)) { }
                }

                // Add delete directories
                for (int i=deleteDirs.Count-1; i>=0; i--)
                    Ops.Add(deleteDirs[i]);

                base.InnerEstimate();
            }

            /// <summary>Print info</summary>
            public override string ToString() => $"TransferTree(Src={SrcPath}, Dst={Path})";
        }



        /// <summary>File operation session</summary>
        public class Session : IDisposable, IObservable<Event>
        {
            /// <summary>Observers</summary>
            internal ArrayList<ObserverHandle> observers = new ArrayList<ObserverHandle>();
            /// <summary>Shared cancellation token</summary>
            public CancellationTokenSource CancelSrc { get; protected set; }
            /// <summary>Operation policies</summary>
            public Policy Policy { get; protected set; }
            /// <summary>Accumulated events</summary>
            public IProducerConsumerCollection<Event> Events { get; protected set; } = new ConcurrentQueue<Event>();
            /// <summary>Operations executed in this session</summary>
            public IProducerConsumerCollection<FileOperation> Ops { get; protected set; } = new ConcurrentQueue<FileOperation>();
            /// <summary>Tests if there are observers</summary>
            public bool HasObservers => observers.Count > 0;
            /// <summary>Pool that allocates byte buffers</summary>
            public IBlockPool BlockPool { get; protected set; }
            /// <summary>Interval of bytes interval to report progress on copying files.</summary>
            public long ProgressInterval { get; protected set; } = 524288L;
            /// <summary>(optional) Option or token</summary>
            public IOption Option { get; protected set; }

            /// <summary>Create session</summary>
            public Session(Policy policy = Policy.Default, IBlockPool blockPool = default, CancellationTokenSource cancelSrc = default, IOption option = default)
            {
                this.Policy = policy;
                this.BlockPool = blockPool ?? new BlockPool();
                this.CancelSrc = cancelSrc ?? new CancellationTokenSource();
                this.Option = option;
            }

            /// <summary>Set new policy</summary>
            public Session SetPolicy(Policy newPolicy)
            {
                this.Policy = newPolicy;
                return this;
            }

            /// <summary>Set new policy</summary>
            public Session SetProgressInterval(long progressInterval)
            {
                this.ProgressInterval = progressInterval;
                return this;
            }

            /// <summary>Set new policy</summary>
            public Session SetCancellationSource(CancellationTokenSource cancelSrc)
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
                            Events.TryAdd(new Event.Error(null, e));
                            // Capture
                            errors.Add(e);
                        }
                    }
                    if (errors.Count > 0) throw new AggregateException(errors.ToArray());
                }
            }

            /// <summary>Subscribe</summary>
            /// <returns>Handle to unsubscribe with</returns>
            public IDisposable Subscribe(IObserver<Event> observer)
            {
                ObserverHandle handle = new ObserverHandle(this, observer);
                observers.Add(handle);
                return handle;
            }

            /// <summary>Cancellable observer handle.</summary>
            internal sealed class ObserverHandle : IDisposable
            {
                Session session;
                internal IObserver<Event> observer;

                public ObserverHandle(Session session, IObserver<Event> observer)
                {
                    this.session = session;
                    this.observer = observer ?? throw new ArgumentNullException(nameof(observer));
                }
                public void Dispose() => session.observers.Remove(this);
            }

            /// <summary>Add event to session log and dispatch it. <see cref="Event.Progress"/> events are not added to event log.</summary>
            public void LogAndDispatchEvent(Event @event)
            {
                if (@event is Event.Progress == false) this.Events.TryAdd(@event);
                DispatchEvent(@event);
            }

            /// <summary>Dispatch event to observers (in current thread)</summary>
            public void DispatchEvent(Event @event)
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
                bool CaptureError(Exception e) { Events.TryAdd(new Event.Error(null, e)); return false; }
            }

        }

        /// <summary>File operation event</summary>
        public class Event
        {
            /// <summary>(optional) Involved operation</summary>
            public FileOperation Op { get; protected set; }
            /// <summary>Create event</summary>
            public Event(FileOperation op) { this.Op = op; }

            /// <summary>State changed event</summary>
            public class State : Event
            {
                /// <summary>New state</summary>
                public FileOperation.State OpState { get; protected set; }
                /// <summary>Create error event</summary>
                public State(FileOperation op, FileOperation.State opState) : base(op) { this.OpState = opState; }
                /// <summary>Print info</summary>
                public override string ToString() => Op+" = "+OpState;
            }

            /// <summary>Error state event</summary>
            public class Error : State
            {
                /// <summary>Error</summary>
                public Exception Exception { get; protected set; }
                /// <summary>Create error event</summary>
                public Error(FileOperation op, Exception exception) : base(op, FileOperation.State.Error) { Exception = exception; }
                /// <summary>Print info</summary>
                public override string ToString() => Op + " = " + Exception;
            }

            /// <summary>Progress event</summary>
            public class Progress : Event
            {
                /// <summary>Current position of operation</summary>
                public long Length { get; protected set; }
                /// <summary>Total length of operation</summary>
                public long TotalLength { get; protected set; }
                /// <summary>Create progress event</summary>
                public Progress(FileOperation op, long length, long totalLength) : base(op) { this.Length = length; this.TotalLength = totalLength; }
                /// <summary>Print info</summary>
                public override string ToString() =>
                    TotalLength > 0 ?
                    "Progress(" + Op + ", " + (int)((Length*100L) / TotalLength) + "%)" :
                    "Progress(" + Op + ")";
            }
        }

    }

}
