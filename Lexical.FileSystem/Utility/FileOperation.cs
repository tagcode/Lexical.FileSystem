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

        /// <summary>File operation policies</summary>
        [Flags]
        public enum Policy : int
        {
            /// <summary>If file already exists, overwrite it.</summary>
            OverwriteIfExists = 0x01,
            /// <summary>If file already exists, skip it.</summary>
            SkipIfExists = 0x02,
            /// <summary>If file already exists, throw exception.</summary>
            FailIfExists = 0x03,

            /// <summary>If one operation fails, cancel the whole operation. If this is not set, then continues with other files.</summary>
            CancelIfError = 0x0100,
            /// <summary>Operation fails, rollback changes before returning.</summary>
            RollbackOnFail = 0x0200,
            /// <summary>Policy whether to omit directories that are automatically mounted. These are typically package files, such as .zip, exposed as part of the filesystem.</summary>
            OmitAutoMounts = 0x0400,

            /// <summary>Default policy</summary>
            Default = OmitAutoMounts | CancelIfError | RollbackOnFail | FailIfExists
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

        /// <summary>Length of operation in bytes. -1 if unknown.</summary>
        public long Length { get; protected set; }
        /// <summary>Progress of operation in bytes. -1 if unknown.</summary>
        public long Progress { get; protected set; }

        /// <summary>
        /// Is operation capable of rollback. Value may change after <see cref="Estimate"/>.
        /// </summary>
        public bool CanRollback { get; protected set; } = false;

        /// <summary>
        /// Create filesystem operation
        /// </summary>
        /// <param name="session"></param>
        public FileOperation(Session session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        /// <summary>
        /// Estimate viability and size of the operation.
        /// 
        /// Creates an action plan, and adds them to <see cref="Children"/>.
        /// 
        /// May change <see cref="CanRollback"/> value to true from default false.
        /// </summary>
        public virtual void Estimate() { }

        /// <summary>
        /// Run the operation.
        /// </summary>
        /// <exception cref="IOException"></exception>
        /// <exception cref="Exception"></exception>
        public abstract void Run();

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
        /// If <see cref="Policy.CancelIfError"/> is set, then cancels the cancel token.
        /// </summary>
        /// <param name="error"></param>
        /// <returns>false</returns>
        protected bool SetError(Exception error)
        {
            // Set state to error
            currentState = (int)State.Error;
            // Cancel token
            if (session.Policy.HasFlag(Policy.CancelIfError)) session.CancelSrc.Cancel();
            // Change state
            if ((State)Interlocked.Exchange(ref currentState, (int)State.Error) != State.Error) session.AddAndDispatchEvent(new Event.Error(this, error));
            // Return
            return false;
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
            session.AddAndDispatchEvent(new Event.State(this, newState));
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
            session.AddAndDispatchEvent(new Event.State(this, newState));
            return true;
        }

        /// <summary>Batch operation</summary>
        public class Batch : FileOperation
        {
            /// <summary>Ops.</summary>
            public ArrayList<FileOperation> Ops = new ArrayList<FileOperation>();
            /// <summary>Child operations</summary>
            public override FileOperation[] Children => Ops.Array;
            /// <summary></summary>
            public bool ThrowIfError = false;

            /// <summary>Create batch op.</summary>
            public Batch(Session session, IEnumerable<FileOperation> ops) : base(session)
            {
                if (ops != null) this.Ops.AddRange(ops);
                this.CanRollback = true;
            }

            /// <summary>Create batch op.</summary>
            public Batch(Session session, params FileOperation[] ops) : base(session)
            {
                if (ops != null) this.Ops.AddRange(ops);
                this.CanRollback = true;
            }

            /// <summary>Estimate child ops</summary>
            /// <exception cref="Exception">On error</exception>
            public override void Estimate()
            {
                // Assert session is not cancelled
                if (session.CancelSrc.IsCancellationRequested) { SetState(State.Cancelled); return; }
                // Estimating
                if (!TrySetState(State.Estimating, State.Initialized)) return;

                try
                {
                    foreach (FileOperation op in Ops)
                    {
                        try
                        {
                            // Assert session is not cancelled
                            if (session.CancelSrc.IsCancellationRequested) { SetState(State.Cancelled); return; }
                            op.Estimate();
                            if (op.Length > 0L) this.Length += op.Length;
                            if (op.Progress > 0L) this.Progress += op.Progress;
                            this.CanRollback &= op.CanRollback;
                        }
                        catch (Exception) when (!session.Policy.HasFlag(Policy.CancelIfError))
                        {
                            // CancelIfError not set, continue with other ops.
                        }
                        // Completed
                        if (!TrySetState(State.Estimated, State.Estimating)) return;
                    }
                }
                // Capture error and set state
                catch (Exception e) when (SetError(e)) { }
            }

            /// <summary>Run child ops</summary>
            public override void Run()
            {
                // Estimate viability of operation
                Estimate();
                // Assert session is not cancelled
                if (session.CancelSrc.IsCancellationRequested) { SetState(State.Cancelled); return; }
                // Running
                if (!TrySetState(State.Running, State.Estimated)) return;
                try
                {
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
                            if (ThrowIfError && op.CurrentState == State.Error) throw new AggregateException(op.Errors);
                            // Move progress
                            if (op.Progress > 0L)
                            {
                                // Update progress position
                                this.Progress += op.Progress;
                                progressReminder += op.Progress;
                                // Time to send progress event
                                if (session.ProgressInterval > 0L && progressReminder > session.ProgressInterval && session.HasObservers)
                                {
                                    progressReminder %= session.ProgressInterval;
                                    session.DispatchEvent(new Event.Progress(this, Progress, Length));
                                }
                            }
                        }
                        catch (Exception) when (!session.Policy.HasFlag(Policy.CancelIfError))
                        {
                            // CancelIfError not set, continue with other ops.
                        }
                    }
                    // Completed
                    if (!TrySetState(State.Completed, State.Running)) return;
                }
                // Capture error and set state
                catch (Exception e) when (SetError(e)) { }
            }

            /// <summary>Create rollback operation.</summary>
            /// <returns>rollback or null</returns>
            public override FileOperation CreateRollback()
            {
                if (CanRollback)
                {
                    List<FileOperation> rollbacks = new List<FileOperation>();
                    for(int i=Ops.Count-1; i>=0; i--)
                    {
                        FileOperation rollback = Ops[i].CreateRollback();
                        if (rollback == null) return null;
                        rollbacks.Add(rollback);
                    }
                    return new Batch(session, rollbacks);
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

        /// <summary>Delete directory</summary>
        public class DeleteDirectory : FileOperation
        {
            /// <summary></summary>
            public IFileSystem FileSystem { get; protected set; }
            /// <summary></summary>
            public string Path { get; protected set; }
            /// <summary></summary>
            public bool Recurse { get; protected set; }

            /// <summary>
            /// Create delete directory op.
            /// </summary>
            /// <param name="session"></param>
            /// <param name="filesystem"></param>
            /// <param name="path"></param>
            /// <param name="recurse"></param>
            public DeleteDirectory(Session session, IFileSystem filesystem, string path, bool recurse) : base(session)
            {
                this.FileSystem = filesystem ?? throw new ArgumentNullException(nameof(filesystem));
                this.Path = path ?? throw new ArgumentNullException(nameof(path));
                this.Recurse = recurse;
            }

            /// <summary>Estimate viability of operation.</summary>
            /// <exception cref="FileNotFoundException">If <see cref="Path"/> is not found.</exception>
            public override void Estimate()
            {
                // Assert session is not cancelled
                if (session.CancelSrc.IsCancellationRequested) { SetState(State.Cancelled); return; }
                // Estimating
                if (!TrySetState(State.Estimating, State.Initialized)) return;

                try
                {
                    // Test that source file exists, if we can browse
                    if (FileSystem.CanGetEntry())
                    {
                        try
                        {
                            IFileSystemEntry e = FileSystem.GetEntry(Path);
                            if (e == null) throw new DirectoryNotFoundException(Path);
                            if (!e.IsDirectory()) throw new DirectoryNotFoundException(Path);
                        }
                        catch (NotSupportedException) { }
                    }
                    // Estimated
                    if (!TrySetState(State.Estimated, State.Estimating)) return;
                }
                // Capture error and set state to error
                catch (Exception e) when (SetError(e)) { }
            }

            /// <summary>Run op</summary>
            /// <exception cref="FileNotFoundException">If <see cref="Path"/> is not found.</exception>
            public override void Run()
            {
                // Estimate viability of operation
                Estimate();
                // Assert session is not cancelled
                if (session.CancelSrc.IsCancellationRequested) { SetState(State.Cancelled); return; }
                // Running
                if (!TrySetState(State.Running, State.Estimated)) return;
                try
                {
                    // Delete directory
                    FileSystem.Delete(Path, Recurse);
                    // Completed
                    if (!TrySetState(State.Completed, State.Running)) return;
                }
                // Capture error and set state
                catch (Exception e) when (SetError(e)) { }
            }

            /// <summary>Print info</summary>
            public override string ToString() => $"DeleteDirectory(Path={Path}, Recurse={Recurse}, State={CurrentState})";
        }

        /// <summary>Create directory</summary>
        public class CreateDirectory : FileOperation
        {
            /// <summary></summary>
            public IFileSystem FileSystem { get; protected set; }
            /// <summary></summary>
            public string Path { get; protected set; }

            /// <summary>
            /// Create create directory op.
            /// </summary>
            /// <param name="session"></param>
            /// <param name="filesystem"></param>
            /// <param name="path"></param>
            public CreateDirectory(Session session, IFileSystem filesystem, string path) : base(session)
            {
                this.FileSystem = filesystem ?? throw new ArgumentNullException(nameof(filesystem));
                this.Path = path ?? throw new ArgumentNullException(nameof(path));
            }

            /// <summary>Estimate viability of operation.</summary>
            /// <exception cref="FileNotFoundException">If <see cref="Path"/> is not found.</exception>
            public override void Estimate()
            {
                // Assert session is not cancelled
                if (session.CancelSrc.IsCancellationRequested) { SetState(State.Cancelled); return; }
                // Estimating
                if (!TrySetState(State.Estimating, State.Initialized)) return;

                try
                {
                    // Test that source file exists, if we can browse
                    if (FileSystem.CanGetEntry())
                    {
                        try
                        {
                            IFileSystemEntry e = FileSystem.GetEntry(Path);
                            // Directory already exists
                            if (e != null && !e.IsDirectory()) throw new FileSystemExceptionFileExists(FileSystem, Path);
                            // Can rollback if directory didn't exist.
                            CanRollback = e == null;
                        }
                        catch (NotSupportedException) { }
                    }

                    // Cannot create directory
                    if (!FileSystem.CanCreateDirectory()) throw new NotSupportedException("CreateDirectory");

                    // Estimated
                    if (!TrySetState(State.Estimated, State.Estimating)) return;
                }
                // Capture error and set state to error
                catch (Exception e) when (SetError(e)) { }
            }

            /// <summary>Run op</summary>
            /// <exception cref="FileNotFoundException">If <see cref="Path"/> is not found.</exception>
            public override void Run()
            {
                // Estimate viability of operation
                Estimate();
                // Assert session is not cancelled
                if (session.CancelSrc.IsCancellationRequested) { SetState(State.Cancelled); return; }
                // Running
                if (!TrySetState(State.Running, State.Estimated)) return;
                try
                {
                    // Create directory
                    FileSystem.CreateDirectory(Path);
                    // Completed
                    if (!TrySetState(State.Completed, State.Running)) return;
                }
                // Capture error and set state
                catch (Exception e) when (SetError(e)) { }
            }

            /// <summary>Create rollback</summary>
            /// <returns>op or null</returns>
            public override FileOperation CreateRollback()
            {
                if (CanRollback) return new DeleteDirectory(session, FileSystem, Path, false);
                return null;
            }

            /// <summary>Print info</summary>
            public override string ToString() => $"CreateDirectory(Path={Path}, CurrentState={CurrentState})";
        }

        /// <summary>Move/rename file or directory</summary>
        public class Move : FileOperation
        {
            /// <summary></summary>
            public IFileSystem SrcFileSystem { get; protected set; }
            /// <summary></summary>
            public string SrcPath { get; protected set; }
            /// <summary></summary>
            public IFileSystem DstFileSystem { get; protected set; }
            /// <summary></summary>
            public string DstPath { get; protected set; }

            /// <summary>Create move op.</summary>
            public Move(Session session, IFileSystem srcFilesystem, string srcPath, IFileSystem dstFilesystem, string dstPath) : base(session)
            {
                this.SrcFileSystem = srcFilesystem ?? throw new ArgumentNullException(nameof(srcFilesystem));
                this.DstFileSystem = dstFilesystem ?? throw new ArgumentNullException(nameof(dstFilesystem));
                this.SrcPath = srcPath ?? throw new ArgumentNullException(nameof(srcPath));
                this.DstPath = dstPath ?? throw new ArgumentNullException(nameof(dstPath));
                if (SrcFileSystem != DstFileSystem) throw new ArgumentException($"Move implementation requires that {nameof(srcFilesystem)} and {nameof(dstFilesystem)} are same. Use MoveTree instead.");
            }

            /// <summary>Estimate viability of operation.</summary>
            /// <exception cref="FileNotFoundException">If <see cref="SrcPath"/> is not found.</exception>
            /// <exception cref="FileSystemExceptionFileExists">If <see cref="DstPath"/> already exists.</exception>
            public override void Estimate()
            {
                // Assert session is not cancelled
                if (session.CancelSrc.IsCancellationRequested) { SetState(State.Cancelled); return; }
                // Set state to estimating and allow only one thread to estimate
                if (!TrySetState(State.Estimating, State.Initialized)) return;

                try
                {
                    // Test that source file exists
                    if (SrcFileSystem.CanGetEntry())
                    {
                        try
                        {
                            IFileSystemEntry e = SrcFileSystem.GetEntry(SrcPath);
                            if (e == null) throw new FileNotFoundException(SrcPath);
                        }
                        catch (NotSupportedException)
                        {
                            // GetEntry is not supported
                        }
                    }

                    // Test that dest file doesn't exist
                    if (DstFileSystem.CanGetEntry())
                    {
                        try
                        {
                            if (DstFileSystem.Exists(DstPath))
                            {
                                // Fail, prev file exists
                                if (session.Policy.HasFlag(Policy.FailIfExists)) throw new FileSystemExceptionFileExists(DstFileSystem, DstPath);

                                // Skip op
                                else if (session.Policy.HasFlag(Policy.SkipIfExists))
                                {
                                    // Nothing to rollback, essentially ok
                                    CanRollback = true;
                                    // Nothing to do. Set state to completed.
                                    Interlocked.CompareExchange(ref currentState, (int)State.Skipped, (int)State.Estimating);
                                }

                                // Overwrite
                                else if (session.Policy.HasFlag(Policy.OverwriteIfExists))
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

                        if (!SrcFileSystem.CanMove()) throw new NotSupportedException("Move");
                    }

                    // Set state to estimated
                    if (!TrySetState(State.Estimated, State.Estimating)) return;
                }
                // Capture error and set state to error
                catch (Exception e) when (SetError(e)) { }
            }

            /// <summary>Run operation</summary>
            /// <exception cref="IOException"></exception>
            /// <exception cref="Exception">Unexpected error</exception>
            public override void Run()
            {
                // Estimate viability of operation
                Estimate();
                // Assert session is not cancelled
                if (session.CancelSrc.IsCancellationRequested) { SetState(State.Cancelled); return; }
                // Running
                if (!TrySetState(State.Running, State.Estimated)) return;
                try
                {
                    // CancelToken to monitor
                    CancellationToken token = session.CancelSrc.Token;

                    // Skip if exists
                    IFileSystemEntry dstEntry = null;
                    bool dstExists = false;
                    try
                    {
                        dstEntry = DstFileSystem.GetEntry(DstPath);
                        dstExists = dstEntry != null;
                    }
                    catch (NotSupportedException)
                    {
                        // Dst cannot get entry
                    }

                    // Handle dst exists
                    if (dstExists)
                    {
                        if (session.Policy.HasFlag(Policy.FailIfExists)) throw new FileSystemExceptionFileExists(DstFileSystem, DstPath);
                        else if (session.Policy.HasFlag(Policy.OverwriteIfExists)) DstFileSystem.Delete(DstPath, false);
                        else if (session.Policy.HasFlag(Policy.SkipIfExists)) { TrySetState(State.Skipped, State.Running); return; }
                        CanRollback = false;
                    }

                    // Move
                    SrcFileSystem.Move(SrcPath, DstPath);

                    // Completed
                    if (!TrySetState(State.Completed, State.Running)) return;
                }
                // Capture error and set state
                catch (Exception e) when (SetError(e)) { }
            }

            /// <summary>Create rollback</summary>
            /// <returns>rollback or null</returns>
            public override FileOperation CreateRollback()
            {
                if (CanRollback) return new Move(session, DstFileSystem, DstPath, SrcFileSystem, SrcPath);
                return null;
            }

            /// <summary>Print info</summary>
            public override string ToString() => $"Move(Src={SrcPath}, Dst={DstPath}, State={CurrentState})";
        }

        /// <summary>Copy file</summary>
        public class CopyFile : FileOperation
        {
            /// <summary></summary>
            public IFileSystem SrcFileSystem { get; protected set; }
            /// <summary></summary>
            public string SrcPath { get; protected set; }
            /// <summary></summary>
            public IFileSystem DstFileSystem { get; protected set; }
            /// <summary></summary>
            public string DstPath { get; protected set; }
            /// <summary>Was file overwritten</summary>
            public bool Overwritten { get; protected set; }

            /// <summary>Create copy file op.</summary>
            public CopyFile(Session session, IFileSystem srcFilesystem, string srcPath, IFileSystem dstFilesystem, string dstPath) : base(session)
            {
                this.SrcFileSystem = srcFilesystem ?? throw new ArgumentNullException(nameof(srcFilesystem));
                this.DstFileSystem = dstFilesystem ?? throw new ArgumentNullException(nameof(dstFilesystem));
                this.SrcPath = srcPath ?? throw new ArgumentNullException(nameof(srcPath));
                this.DstPath = dstPath ?? throw new ArgumentNullException(nameof(dstPath));
            }

            /// <summary>Estimate viability of operation.</summary>
            /// <exception cref="FileNotFoundException">If <see cref="SrcPath"/> is not found.</exception>
            /// <exception cref="FileSystemExceptionFileExists">If <see cref="DstPath"/> already exists.</exception>
            public override void Estimate()
            {
                // Assert session is not cancelled
                if (session.CancelSrc.IsCancellationRequested) { SetState(State.Cancelled); return; }
                // Set state to estimating and allow only one thread to estimate
                if (!TrySetState(State.Estimating, State.Initialized)) return;

                try
                {
                    // Test that source file exists, if we can browse
                    if (SrcFileSystem.CanGetEntry())
                    {
                        try
                        {
                            IFileSystemEntry e = SrcFileSystem.GetEntry(SrcPath);
                            if (e == null) throw new FileNotFoundException(SrcPath);
                            this.Length = e.Length();
                        } catch (NotSupportedException) 
                        {
                            // GetEntry is not supported
                        }
                    }

                    // Test that dest file doesn't exist, if it matters
                    if (DstFileSystem.CanGetEntry())
                    {
                        try
                        {
                            if (DstFileSystem.Exists(DstPath))
                            {
                                // Fail, prev file exists
                                if (session.Policy.HasFlag(Policy.FailIfExists)) throw new FileSystemExceptionFileExists(DstFileSystem, DstPath);

                                // Skip op
                                else if (session.Policy.HasFlag(Policy.SkipIfExists))
                                {
                                    // Nothing to rollback, essentially ok
                                    CanRollback = true;
                                    // Nothing to do. Set state to completed.
                                    Interlocked.CompareExchange(ref currentState, (int)State.Skipped, (int)State.Estimating);
                                }

                                // Overwrite
                                else if (session.Policy.HasFlag(Policy.OverwriteIfExists))
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
                        catch (NotSupportedException) { 
                            // GetEntry is not supported
                        }
                    }

                    // Set state to estimated
                    if (!TrySetState(State.Estimated, State.Estimating)) return;
                }
                // Capture error and set state to error
                catch (Exception e) when (SetError(e)) { }
            }

            /// <summary>Run operation</summary>
            /// <exception cref="IOException"></exception>
            /// <exception cref="Exception">Unexpected error</exception>
            public override void Run()
            {
                // Estimate viability of operation
                Estimate();
                // Assert session is not cancelled
                if (session.CancelSrc.IsCancellationRequested) { SetState(State.Cancelled); return; }
                // Queue of blocks
                BlockingCollection<Block> queue = new BlockingCollection<Block>();
                // Running
                if (!TrySetState(State.Running, State.Estimated)) return;
                try
                {
                    // CancelToken to monitor
                    CancellationToken token = session.CancelSrc.Token;

                    // Skip if exists
                    IFileSystemEntry dstEntry = null;
                    bool prevExisted = false;
                    try
                    {
                        dstEntry = DstFileSystem.GetEntry(DstPath);
                        prevExisted = dstEntry != null;
                        if (session.Policy.HasFlag(Policy.SkipIfExists) && prevExisted) { SetState(State.Skipped); return; }
                    } catch (NotSupportedException)
                    {
                        // Dst cannot get entry
                    }

                    // Create mode
                    FileMode createMode = session.Policy.HasFlag(Policy.OverwriteIfExists) ? FileMode.Create : FileMode.CreateNew;

                    // Write stream
                    using (Stream s = DstFileSystem.Open(DstPath, createMode, FileAccess.Write, FileShare.ReadWrite))
                    {
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
                                        session.DispatchEvent(new Event.Progress(this, Progress, Length));
                                    }
                                }
                                // EOF or error
                                else if (block.count <= 0) break;
                            } finally
                            {
                                // Return block to pool
                                if (block.data != null) session.BlockPool.Return(block.data);
                            }
                        }
                    }

                    // Completed
                    if (!TrySetState(State.Completed, State.Running)) return;
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
                    Stream s = SrcFileSystem.Open(SrcPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
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

            /// <summary>
            /// Create rollback operation.
            /// </summary>
            /// <returns>null or rollback operation</returns>
            public override FileOperation CreateRollback()
            {
                // Create rollback op
                if (CurrentState == State.Skipped || (!Overwritten && CurrentState == State.Completed)) return new DeleteFile(session, DstFileSystem, DstPath);
                // No rollback
                return null;
            }

            struct Block {
                public byte[] data;
                public int count;
            }

            /// <summary>Print info</summary>
            public override string ToString() => $"CopyFile(Src={SrcPath}, Dst={DstPath}, State={CurrentState})";
        }

        /// <summary>Delete file</summary>
        public class DeleteFile : FileOperation
        {
            /// <summary></summary>
            public IFileSystem FileSystem { get; protected set; }
            /// <summary></summary>
            public string Path { get; protected set; }

            /// <summary>
            /// Create delete file op.
            /// </summary>
            /// <param name="session"></param>
            /// <param name="filesystem"></param>
            /// <param name="path"></param>
            public DeleteFile(Session session, IFileSystem filesystem, string path) : base(session)
            {
                this.FileSystem = filesystem ?? throw new ArgumentNullException(nameof(filesystem));
                this.Path = path ?? throw new ArgumentNullException(nameof(path));
            }

            /// <summary>Estimate viability of operation.</summary>
            /// <exception cref="FileNotFoundException">If <see cref="Path"/> is not found.</exception>
            public override void Estimate()
            {
                // Assert session is not cancelled
                if (session.CancelSrc.IsCancellationRequested) { SetState(State.Cancelled); return; }
                // Estimating
                if (!TrySetState(State.Estimating, State.Initialized)) return;

                try
                {
                    // Test that source file exists, if we can browse
                    if (FileSystem.CanGetEntry())
                    {
                        try
                        {
                            IFileSystemEntry e = FileSystem.GetEntry(Path);
                            if (e == null) throw new FileNotFoundException(Path);
                            if (!e.IsFile()) throw new FileNotFoundException(Path);
                        }
                        catch (NotSupportedException) { }
                    }
                    // Estimated
                    if (!TrySetState(State.Estimated, State.Estimating)) return;
                }
                // Capture error and set state to error
                catch (Exception e) when (SetError(e)) { }
            }

            /// <summary>Run op</summary>
            /// <exception cref="FileNotFoundException">If <see cref="Path"/> is not found.</exception>
            public override void Run()
            {
                // Estimate viability of operation
                Estimate();
                // Assert session is not cancelled
                if (session.CancelSrc.IsCancellationRequested) { SetState(State.Cancelled); return; }
                // Running
                if (!TrySetState(State.Running, State.Estimated)) return;
                try
                {
                    // Delete file
                    FileSystem.Delete(Path);
                    // Completed
                    if (!TrySetState(State.Completed, State.Running)) return;
                }
                // Capture error and set state
                catch (Exception e) when (SetError(e)) { }
            }

            /// <summary>Print info</summary>
            public override string ToString() => $"DeleteFile(Path={Path}, State={CurrentState})";
        }

        /// <summary>Copy a file or directory tree</summary>
        public class CopyTree : Batch
        {
            /// <summary></summary>
            public IFileSystem SrcFileSystem { get; protected set; }
            /// <summary></summary>
            public string SrcPath { get; protected set; }
            /// <summary></summary>
            public IFileSystem DstFileSystem { get; protected set; }
            /// <summary></summary>
            public string DstPath { get; protected set; }

            /// <summary>Create move op.</summary>
            public CopyTree(Session session, IFileSystem srcFilesystem, string srcPath, IFileSystem dstFilesystem, string dstPath) : base(session)
            {
                this.SrcFileSystem = srcFilesystem ?? throw new ArgumentNullException(nameof(srcFilesystem));
                this.DstFileSystem = dstFilesystem ?? throw new ArgumentNullException(nameof(dstFilesystem));
                this.SrcPath = srcPath ?? throw new ArgumentNullException(nameof(srcPath));
                this.DstPath = dstPath ?? throw new ArgumentNullException(nameof(dstPath));
            }

            /// <summary>Estimate viability of operation.</summary>
            /// <exception cref="FileNotFoundException">If <see cref="SrcPath"/> is not found.</exception>
            /// <exception cref="FileSystemExceptionFileExists">If <see cref="DstPath"/> already exists.</exception>
            public override void Estimate()
            {
                if (CurrentState != State.Initialized) return;
                // Assert session is not cancelled
                if (session.CancelSrc.IsCancellationRequested) { SetState(State.Cancelled); return; }

                try
                {
                    PathConverter pathConverter = new PathConverter(SrcPath, DstPath);
                    List<IFileSystemEntry> queue = new List<IFileSystemEntry>();
                    IFileSystemEntry e = SrcFileSystem.GetEntry(SrcPath);
                    if (e == null) throw new FileNotFoundException(SrcPath);
                    queue.Add(e);
                    while (queue.Count > 0)
                    {
                        try
                        {
                            // Next entry
                            int lastIx = queue.Count - 1;
                            IFileSystemEntry entry = queue[lastIx];
                            queue.RemoveAt(lastIx);

                            // Omit automounted entries 
                            if (session.Policy.HasFlag(Policy.OmitAutoMounts) && entry.IsAutoMounted()) continue;

                            // Process directory
                            if (entry.IsDirectory())
                            {
                                // Browse children
                                IFileSystemEntry[] children = SrcFileSystem.Browse(entry.Path);
                                // Assert children don't refer to the parent of the parent
                                foreach (IFileSystemEntry child in children) if (entry.Path.StartsWith(child.Path)) throw new IOException($"{child.Path} cannot be child of {entry.Path}");
                                // Visit child
                                for (int i = children.Length - 1; i >= 0; i--) queue.Add(children[i]);
                                // Convert path
                                string dstPath;
                                if (!pathConverter.ParentToChild(entry.Path, out dstPath)) throw new Exception("Failed to convert path");
                                // Add op
                                if (dstPath != "") Ops.Add(new CreateDirectory(session, DstFileSystem, dstPath));
                            }

                            // Process file
                            else if (entry.IsFile())
                            {
                                // Convert path
                                string dstPath;
                                if (!pathConverter.ParentToChild(entry.Path, out dstPath)) throw new Exception("Failed to convert path");
                                // Add op
                                Ops.Add(new CopyFile(session, SrcFileSystem, entry.Path, DstFileSystem, dstPath));
                            }
                        }
                        catch (Exception error) when (SetError(error)) { }
                    }

                    base.Estimate();
                }
                // Capture error and set state to error
                catch (Exception e) when (SetError(e)) { }
            }

            // /// <summary>Print info</summary>
            //public override string ToString() => $"CopyTree(Src={SrcPath}, Dst={DstPath})";
        }

        /// <summary>Delete a file or directory tree</summary>
        public class DeleteTree : Batch
        {
            /// <summary></summary>
            public IFileSystem FileSystem { get; protected set; }
            /// <summary></summary>
            public string Path { get; protected set; }

            /// <summary>Create move op.</summary>
            public DeleteTree(Session session, IFileSystem filesystem, string path) : base(session)
            {
                this.FileSystem = filesystem ?? throw new ArgumentNullException(nameof(filesystem));
                this.Path = path ?? throw new ArgumentNullException(nameof(path));
            }

            /// <summary>Estimate viability of operation.</summary>
            /// <exception cref="FileNotFoundException">If <see cref="Path"/> is not found.</exception>
            /// <exception cref="FileSystemExceptionFileExists">If <see cref="Path"/> already exists.</exception>
            public override void Estimate()
            {
                if (CurrentState != State.Initialized) return;
                // Assert session is not cancelled
                if (session.CancelSrc.IsCancellationRequested) { SetState(State.Cancelled); return; }

                try
                {
                    List<DeleteDirectory> dirDeletes = new List<DeleteDirectory>();
                    try
                    {
                        List<IFileSystemEntry> queue = new List<IFileSystemEntry>();
                        IFileSystemEntry e = FileSystem.GetEntry(Path);
                        if (e == null) throw new FileNotFoundException(Path);
                        queue.Add(e);
                        while (queue.Count > 0)
                        {
                            try
                            {
                                // Next entry
                                int lastIx = queue.Count - 1;
                                IFileSystemEntry entry = queue[lastIx];
                                queue.RemoveAt(lastIx);

                                // Omit automounted entries 
                                if (session.Policy.HasFlag(Policy.OmitAutoMounts) && entry.IsAutoMounted()) continue;

                                // Process directory
                                if (entry.IsDirectory())
                                {
                                    // Browse children
                                    IFileSystemEntry[] children = FileSystem.Browse(entry.Path);
                                    // Assert children don't refer to the parent of the parent
                                    foreach (IFileSystemEntry child in children) if (entry.Path.StartsWith(child.Path)) throw new IOException($"{child.Path} cannot be child of {entry.Path}");
                                    // Visit children
                                    for (int i = children.Length - 1; i >= 0; i--) queue.Add(children[i]);
                                    // Add op
                                    dirDeletes.Add(new DeleteDirectory(session, FileSystem, entry.Path, false));
                                }

                                // Process file
                                else if (entry.IsFile())
                                {
                                    // Add op
                                    Ops.Add(new DeleteFile(session, FileSystem, entry.Path));
                                }
                            }
                            catch (Exception error) when (SetError(error)) { }
                        }
                    }
                    finally
                    {
                        // Add directory deletes
                        for (int i=dirDeletes.Count-1; i>=0; i--)
                            Ops.Add(dirDeletes[i]);
                    }

                    // Estimate added ops
                    base.Estimate();
                }
                // Capture error and set state to error
                catch (Exception e) when (SetError(e)) { }
            }

            // /// <summary>Print info</summary>
            //public override string ToString() => $"DeleteTree({Path})";
        }

        /// <summary>Move/rename a file or directory tree by copying and deleting files</summary>
        public class MoveTree : Batch
        {
            /// <summary></summary>
            public IFileSystem SrcFileSystem { get; protected set; }
            /// <summary></summary>
            public string SrcPath { get; protected set; }
            /// <summary></summary>
            public IFileSystem DstFileSystem { get; protected set; }
            /// <summary></summary>
            public string DstPath { get; protected set; }

            /// <summary>Create move op.</summary>
            public MoveTree(Session session, IFileSystem srcFilesystem, string srcPath, IFileSystem dstFilesystem, string dstPath) : base(session)
            {
                this.SrcFileSystem = srcFilesystem ?? throw new ArgumentNullException(nameof(srcFilesystem));
                this.DstFileSystem = dstFilesystem ?? throw new ArgumentNullException(nameof(dstFilesystem));
                this.SrcPath = srcPath ?? throw new ArgumentNullException(nameof(srcPath));
                this.DstPath = dstPath ?? throw new ArgumentNullException(nameof(dstPath));

                Batch copy = new CopyTree(session, SrcFileSystem, SrcPath, DstFileSystem, DstPath);
                Batch delete = new DeleteTree(session, SrcFileSystem, SrcPath);
                copy.ThrowIfError = true;
                delete.ThrowIfError = true;
                Ops.Add(copy);
                Ops.Add(delete);
            }

            // /// <summary>Print info</summary>
            //public override string ToString() => $"MoveTree(Src={SrcPath}, Dst={DstPath})";
        }



        /// <summary>File operation session</summary>
        public class Session : IDisposable, IObservable<Event>
        {
            /// <summary>Observers</summary>
            internal ArrayList<ObserverHandle> observers = new ArrayList<ObserverHandle>();
            /// <summary>Shared cancellation token</summary>
            public CancellationTokenSource CancelSrc { get; protected set; } = new CancellationTokenSource();
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
            public long ProgressInterval { get; protected set; }

            /// <summary>Create session</summary>
            public Session(Policy policy = Policy.Default, IBlockPool blockPool = default, long progressInterval = 524288L)
            {
                this.Policy = policy;
                this.BlockPool = blockPool ?? new BlockPool();
                this.ProgressInterval = progressInterval;
            }

            /// <summary>Set new policy</summary>
            public Session SetPolicy(Policy newPolicy)
            {
                this.Policy = newPolicy;
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
                        } catch (Exception e)
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
            public void AddAndDispatchEvent(Event @event)
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
            }

            /// <summary>Error state event</summary>
            public class Error : State
            {
                /// <summary>Error</summary>
                public Exception Exception { get; protected set; }
                /// <summary>Create error event</summary>
                public Error(FileOperation op, Exception exception) : base(op, FileOperation.State.Error) { Exception = exception; }
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
            }
        }

    }

}
