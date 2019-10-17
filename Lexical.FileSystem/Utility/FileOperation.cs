// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           17.10.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Lexical.FileSystem.Utility
{
    /// <summary>
    /// File operation.
    /// </summary>
    public abstract class FileOperation 
    {
        /// <summary>
        /// FileSystem where the operation is ran.
        /// </summary>
        public readonly IFileSystem FileSystem;

        /// <summary>
        /// Set policy whether to omit directories that are automatically mounted.
        /// These are typically package files, such as .zip, exposed as part of the filesystem.
        /// </summary>
        public bool OmitAutoMounts { get; set; } = true;

        /// <summary>
        /// Cancel token
        /// </summary>
        public CancellationToken CancelToken;

        /// <summary>
        /// Operation State
        /// </summary>
        public enum State : int
        {
            /// <summary>Operation has been initialized</summary>
            Initialized = 0,
            /// <summary>Operation size and viability are being estimated</summary>
            Estimating = 1,
            /// <summary>Operation size and viability have been estimated</summary>
            Estimated = 2,
            /// <summary>Started and running</summary>
            Running = 1,
            /// <summary>Run completed ok</summary>
            Completed = 2,
            /// <summary>Run interrupted with cancellation token</summary>
            Cancelled = 3,
            /// <summary>Run failed</summary>
            Error = 4,
        }

        /// <summary>Current state of the operation</summary>
        protected int currentState = (int) State.Initialized;

        /// <summary>Current state of the operation</summary>
        public State CurrentState => (State) currentState;

        /// <summary>Errors that occured</summary>
        protected Exception[] errors;

        /// <summary>Errors that occured</summary>
        public virtual Exception[] Errors => errors;

        /// <summary>Child operations</summary>
        public virtual FileOperation[] Children => null;

        /// <summary>
        /// Create filesystem operation
        /// </summary>
        /// <param name="filesystem"></param>
        public FileOperation(IFileSystem filesystem)
        {
            this.FileSystem = filesystem ?? throw new ArgumentNullException(nameof(filesystem));
            this.CancelToken = new CancellationToken(false);
        }

        /// <summary>
        /// Create filesystem operation
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="cancelToken"></param>
        public FileOperation(IFileSystem filesystem, CancellationToken cancelToken)
        {
            this.FileSystem = filesystem ?? throw new ArgumentNullException(nameof(filesystem));
            this.CancelToken = cancelToken;
        }

        /// <summary>
        /// Estimate viability and size of the operation.
        /// Creates an action plan, and adds them to <see cref="Children"/>.
        /// </summary>
        /// <returns></returns>
        public virtual bool Estimate() => true;

        /// <summary>
        /// Run the operation
        /// </summary>
        /// <exception cref="IOException"></exception>
        /// <exception cref="Exception"></exception>
        public abstract void Run();

        /// <summary>
        /// Captures error, sets state to Error. 
        /// </summary>
        /// <param name="e"></param>
        /// <returns>false</returns>
        protected bool CaptureError(Exception e)
        {
            this.errors = e is AggregateException ae ? ae.InnerExceptions.ToArray() : new Exception[] { e };
            currentState = (int)State.Error;
            return false;
        }

        /// <summary>
        /// Create rollback operation that reverts already executed operations.
        /// </summary>
        /// <returns></returns>
        public virtual FileOperation CreateRollback()
        {
            throw new NotSupportedException();
        }

        /// <summary>Delete directory recursively</summary>
        public class DeleteDirectory : FileOperation
        {
            /// <summary>
            /// Create delete directory action
            /// </summary>
            /// <param name="filesystem"></param>
            public DeleteDirectory(IFileSystem filesystem) : base(filesystem)
            {
            }

            /// <summary>
            /// Create delete directory action.
            /// </summary>
            /// <param name="filesystem"></param>
            /// <param name="cancelToken"></param>
            public DeleteDirectory(IFileSystem filesystem, CancellationToken cancelToken) : base(filesystem, cancelToken)
            {
            }

            public override void Run()
            {
                Interlocked.CompareExchange(ref currentState, (int)State.Running, (int)State.Initialized);
            }
        }

        /// <summary>Move directory recursively as copy and delete operation</summary>
        public class MoveDirectory : FileOperation
        {
            public MoveDirectory(IFileSystem filesystem) : base(filesystem)
            {
            }

            public MoveDirectory(IFileSystem filesystem, CancellationToken cancelToken) : base(filesystem, cancelToken)
            {
            }
 
            public override void Run()
            {
            }
        }

        /// <summary>Copy file</summary>
        public class CopyFile : FileOperation
        {
            /// <summary>Source file path</summary>
            public readonly string SrcPath;
            /// <summary>Destination file path file path</summary>
            public readonly string DstPath;

            /// <summary>
            /// Create copy file operation.
            /// </summary>
            /// <param name="filesystem"></param>
            /// <param name="srcPath"></param>
            /// <param name="dstPath"></param>
            public CopyFile(IFileSystem filesystem, string srcPath, string dstPath) : base(filesystem)
            {
            }

            /// <summary>
            /// Create copy file operation.
            /// </summary>
            /// <param name="filesystem"></param>
            /// <param name="srcPath"></param>
            /// <param name="dstPath"></param>
            /// <param name="cancelToken"></param>
            public CopyFile(IFileSystem filesystem, string srcPath, string dstPath, CancellationToken cancelToken) : base(filesystem, cancelToken)
            {
            }

            /// <summary>Run operation</summary>
            /// <exception cref="IOException"></exception>
            /// <exception cref="Exception">Unexpected error</exception>
            public override void Run()
            {
                try
                {
                    // Set state
                    State prevState = (State) Interlocked.CompareExchange(ref currentState, (int)State.Running, (int)State.Initialized);
                    // Only one thread may run, and only once
                    if (prevState != State.Initialized) throw new Exception("Operation has already executed, or is being executed.");

                    using (Stream si = FileSystem.Open(SrcPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (Stream so = FileSystem.Open(DstPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                    {
                        //int bufsize = si.L
                    }
                }
                // Capture error and set state
                catch (Exception e) when (CaptureError(e)) { }
            }

            /// <summary>Print info</summary>
            public override string ToString()
                => $"CopyFile(Src={SrcPath}, Dst={DstPath})";
        }

        /// <summary>Delete file</summary>
        public class DeleteFile : FileOperation
        {
            public DeleteFile(IFileSystem filesystem) : base(filesystem)
            {
            }

            public DeleteFile(IFileSystem filesystem, CancellationToken cancelToken) : base(filesystem, cancelToken)
            {
            }

            public override void Run()
            {
            }
        }
    }



}
