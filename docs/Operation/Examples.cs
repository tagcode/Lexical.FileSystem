using Lexical.FileSystem;
using Lexical.FileSystem.Internal;
using Lexical.FileSystem.Operation;
using Lexical.FileSystem.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace docs
{
    public class Operation_Examples
    {
        public static void Main(string[] args)
        {
            {
                // <Snippet_1>
                IFileSystem ms = new MemoryFileSystem();
                ms.CreateFile("file", new byte[1024*1024]);

                using(var s = new OperationSession())
                {
                    new CopyFile(s, ms, "file", ms, "file.copy")
                        .Estimate()
                        .AssertCanRollback()
                        .Run()
                        .AssertSuccessful();
                }
                // </Snippet_1>

                ms.PrintTo(Console.Out, format: PrintTree.Format.Default | PrintTree.Format.Length);
            }

            {
                IFileSystem ms = new MemoryFileSystem();
                ms.CreateFile("file", new byte[1024 * 1024]);
                // <Snippet_1b>
                var s = new OperationSession();
                var op = new CopyFile(s, ms, "file", ms, "file.copy");
                op.Estimate();
                // </Snippet_1b>
                // <Snippet_1b1>
                Console.WriteLine(op.CanRollback);
                // </Snippet_1b1>


                // <Snippet_1b2>
                op.Estimate().AssertCanRollback();
                // </Snippet_1b2>

                // <Snippet_1c>
                op.Run();
                // </Snippet_1c>

                // <Snippet_1c1>
                if (op.CurrentState == OperationState.Completed) 
                    Console.WriteLine("ok");
                // </Snippet_1c1>

                ms.Delete("file.copy");
                op = (CopyFile) new CopyFile(s, ms, "file", ms, "file.copy").Estimate();
                // <Snippet_1c2>
                op.Run().AssertSuccessful();
                // </Snippet_1c2>

                ms.Delete("file.copy");
                op = (CopyFile)new CopyFile(s, ms, "file", ms, "file.copy").Estimate();
                // <Snippet_1c3>
                try
                {
                    op.Run();
                }
                catch (Exception)
                {
                    // Rollback
                    op.CreateRollback()?.Run();
                }
                // </Snippet_1c3>

                // <Snippet_1c4>
                op.Run(rollbackOnError: true);
                // </Snippet_1c4>

            }

            {
                // <Snippet_2>
                IFileSystem ms = new MemoryFileSystem();
                ms.CreateDirectory("dir/dir/dir/");
                ms.CreateFile("dir/dir/dir/file", new byte[1024 * 1024]);

                using (var s = new OperationSession())
                {
                    var op = new CopyTree(s, ms, "dir", ms, "dir.copy");

                    op.Estimate().AssertCanRollback().Run().AssertSuccessful();
                }
                // </Snippet_2>
                ms.PrintTo(Console.Out);
            }
            {
                // <Snippet_3>
                IFileSystem ms = new MemoryFileSystem();                
                using (var s = new OperationSession(policy: OperationPolicy.EstimateOnRun/*important*/))
                {
                    Batch batch = new Batch(s, OperationPolicy.Unset);

                    batch.Ops.Add(new CreateDirectory(s, ms, "dir/dir/dir"));
                    batch.Ops.Add(new CopyTree(s, ms, "dir", ms, "dir.copy"));
                    batch.Ops.Add(new Delete(s, ms, "dir/dir", true, policy: OperationPolicy.EstimateOnRun));
                    batch.Estimate().Run().AssertSuccessful();
                }
                // </Snippet_3>
                ms.PrintTo(Console.Out);
            }
            {
                // <Snippet_4>
                IFileSystem ms_src = new MemoryFileSystem();
                ms_src.CreateDirectory("dir/dir/dir/");
                ms_src.CreateFile("dir/dir/dir/file", new byte[1024 * 1024]);

                IFileSystem ms_dst = new MemoryFileSystem();

                using (var s = new OperationSession())
                {
                    var op = new TransferTree(s, ms_src, "dir", ms_dst, "dir.elsewhere");
                    op.Estimate().AssertCanRollback().Run().AssertSuccessful();
                }
                // </Snippet_4>
                ms_dst.PrintTo(Console.Out);
            }
            {
                // <Snippet_5>
                IFileSystem ms = new MemoryFileSystem();
                ms.CreateFile("file", new byte[1024 * 1024]);

                using (var s = new OperationSession())
                {
                    new CopyFile(s, ms, "file", ms, "file.copy")
                        .Estimate()
                        .AssertCanRollback()
                        .Run()
                        .AssertSuccessful();

                    foreach (var @event in s.Events)
                        Console.WriteLine(@event);
                }
                ms.PrintTo(Console.Out);
                // </Snippet_5>
            }
            {
                // <Snippet_6>
                IFileSystem ms = new MemoryFileSystem();
                ms.CreateFile("file", new byte[1024 * 20]);

                using (var s = new OperationSession().SetProgressInterval(1024))
                {
                    s.Subscribe(new OpEventPrinter());

                    new CopyFile(s, ms, "file", ms, "file.copy")
                        .Estimate()
                        .AssertCanRollback()
                        .Run()
                        .AssertSuccessful();
                }
                ms.PrintTo(Console.Out);
                // </Snippet_6>
            }
            {
                try
                {
                    // <Snippet_7>
                    IFileSystem ms = new MemoryFileSystem();
                    ms.CreateFile("file", new byte[1024 * 10]);
                    CancellationTokenSource cancelSrc = new CancellationTokenSource();

                    using (var s = new OperationSession(cancelSrc: cancelSrc))
                    {
                        cancelSrc.Cancel();
                        new Move(s, ms, "file", ms, "file.moved")
                            .Estimate()
                            .AssertCanRollback()
                            .Run()
                            .AssertSuccessful();
                    }
                    // </Snippet_7>
                } catch (Exception e)
                {

                }
            }
            {
                // <Snippet_8>
                // </Snippet_8>
            }
            {
                // <Snippet_9>
                // </Snippet_9>
            }
            {
                // <Snippet_10>
                // </Snippet_10>
            }
            {
                // <Snippet_11>
                // </Snippet_11>
            }
            {
                // <Snippet_12>
                // </Snippet_12>
            }
            {
                // <Snippet_13>
                // </Snippet_13>
            }
            {
                // <Snippet_14>
                // </Snippet_14>
            }
            {
                // <Snippet_15>
                // </Snippet_15>
            }
            {
                // <Snippet_16>
                // </Snippet_16>
            }
        }
    }

    // <OpEventPrinter>
    class OpEventPrinter : IObserver<IOperationEvent>
    {
        public void OnCompleted() => Console.WriteLine("OnCompleted");
        public void OnError(Exception error) => Console.WriteLine(error);
        public void OnNext(IOperationEvent @event) => Console.WriteLine(@event);
    }
    // </OpEventPrinter>

}
