// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           17.10.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lexical.FileSystem.Operation
{
    /// <summary>Batch operation</summary>
    public class Batch : OperationBase
    {
        /// <summary>Ops.</summary>
        public ArrayList<IOperation> Ops = new ArrayList<IOperation>();
        /// <summary>Child operations</summary>
        public override IOperation[] Children => Ops.Array;
        /// <summary>Target filesystem if same for all ops, otherwise null</summary>
        public override IFileSystem FileSystem => Ops.Count == 0 ? null : Ops.Count == 1 ? Ops[0].FileSystem : (Ops.All(fo => fo.FileSystem == Ops[0].FileSystem) ? Ops[0].FileSystem : null);
        /// <summary>Target path if same for all ops, otherwise null</summary>
        public override String Path => Ops.Count == 0 ? null : Ops.Count == 1 ? Ops[0].Path : (Ops.All(fo => fo.Path == Ops[0].Path) ? Ops[0].Path : null);
        /// <summary>Source filesystem if same for all ops, otherwise null</summary>
        public override IFileSystem SrcFileSystem => Ops.Count == 0 ? null : Ops.Count == 1 ? Ops[0].SrcFileSystem : (Ops.All(fo => fo.SrcFileSystem == Ops[0].SrcFileSystem) ? Ops[0].SrcFileSystem : null);
        /// <summary>Source path if same for all ops, otherwise null</summary>
        public override String SrcPath => Ops.Count == 0 ? null : Ops.Count == 1 ? Ops[0].SrcPath : (Ops.All(fo => fo.SrcPath == Ops[0].SrcPath) ? Ops[0].SrcPath : null);

        /// <summary>Create batch op.</summary>
        public Batch(IOperationSession session, OperationPolicy policy, IEnumerable<IOperation> ops) : base(session, policy)
        {
            if (ops != null) this.Ops.AddRange(ops);
            this.CanRollback = true;
        }

        /// <summary>Create batch op.</summary>
        public Batch(IOperationSession session, OperationPolicy policy, params IOperation[] ops) : base(session, policy)
        {
            if (ops != null) this.Ops.AddRange(ops);
            this.CanRollback = true;
        }

        /// <summary>Estimate child ops</summary>
        /// <exception cref="Exception">On error</exception>
        protected override void InnerEstimate()
        {
            StructList2<Exception> errors = new StructList2<Exception>();
            foreach (IOperation op in Ops)
            {
                try
                {
                    // Assert session is not cancelled
                    if (session.CancelSrc.IsCancellationRequested) { SetState(OperationState.Cancelled); return; }
                    op.Estimate();
                    if (op.TotalLength > 0L) this.TotalLength += op.TotalLength;
                    if (op.Progress > 0L) this.Progress += op.Progress;
                    this.CanRollback &= op.CanRollback | op.EffectivePolicy.HasFlag(OperationPolicy.EstimateOnRun);
                }
                catch (Exception e) when (session.Policy.HasFlag(OperationPolicy.BatchContinueOnError))
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
            foreach (OperationBase op in Ops)
            {
                if (op.CurrentState == OperationState.Completed) continue;
                if (op.CurrentState == OperationState.Skipped) continue;
                try
                {
                    // Assert session is not cancelled
                    if (session.CancelSrc.IsCancellationRequested) { SetState(OperationState.Cancelled); return; }
                    // Run op
                    op.Run();
                    // 
                    if (!EffectivePolicy.HasFlag(OperationPolicy.BatchContinueOnError) && op.CurrentState == OperationState.Error) throw new AggregateException(op.Errors);
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
                            session.DispatchEvent(new OperationEvent.Progress(this, Progress, TotalLength));
                        }
                    }
                }
                catch (Exception e) when (session.Policy.HasFlag(OperationPolicy.BatchContinueOnError))
                {
                    errors.Add(e);
                }
            }
            // Throw captured exceptions
            if (errors.Count > 0) throw new AggregateException(errors.ToArray());
        }

        /// <summary>Create rollback operation.</summary>
        /// <returns>rollback or null</returns>
        public override IOperation CreateRollback()
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

}
