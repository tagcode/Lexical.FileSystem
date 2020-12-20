using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using System.Linq;
using Lexical.FileSystem;
using Microsoft.Extensions.FileProviders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Lexical.FileSystem.Internal;
using Lexical.FileSystem.Utility;
using Lexical.FileSystem.Operation;

namespace Lexical.FileSystem.Tests
{
    [TestClass]
    public class FileSystemOperationTests
    {
        public static byte[] HelloWorld = Encoding.UTF8.GetBytes("Hello World!\r\n");
        public static byte[] HelloWorld_x100 = Encoding.UTF8.GetBytes(Repeat("Hello World!\r\n", 100));
        public static byte[] HelloWorld_x10000 = Encoding.UTF8.GetBytes(Repeat("Hello World!\r\n", 10000));

        public TestContext TestContext { get; set; }

        MemoryFileSystem ram;

        const int blocks = 1024/* * 1024*/;

        [TestInitialize]
        public void Initialize()
        {
            ram = new MemoryFileSystem();
            ram.CreateDirectory("/tmp");
            ram.CreateDirectory("/tmp/dir");
            ram.CreateDirectory("/mnt");
            ram.CreateDirectory("/usr/lex");
            ram.CreateDirectory("c:/dir/dir");
            ram.CreateFile("/tmp/helloworld.txt", HelloWorld);
            ram.CreateFile("/tmp/helloworld_100.txt", HelloWorld_x100);
            ram.CreateFile("/tmp/helloworld_10000.txt", HelloWorld_x10000);
            //ram.CreateDirectory("file://");
        }

        [TestCleanup]
        public void Cleanup()
        {
            ram.Dispose();
            ram = null;
        }

        [TestMethod]
        public void Batch()
        {
        }

        [TestMethod]
        public void CopyFile()
        {
            // 
            {
                // Create 4GB file
                using (var s = ram.Open("bigfile", FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                {
                    byte[] buf = new byte[4096];
                    for (int i = 0; i < 4096; i++) buf[i] = (byte)(i & 0xff);
                    for (int i = 0; i < blocks; i++) s.Write(buf, 0, buf.Length);
                }

                // TODO TEst | FileOperation.Policy.CancelIfError

                // File session
                using (var session = new OperationSession())
                {
                    // Copy file
                    {
                        // Operation
                        OperationBase op = new CopyFile(session, ram, "bigfile", ram, "bigfile.copy");
                        //
                        op.Estimate();
                        //
                        Assert.IsTrue(op.CanRollback);
                        //
                        op.Run();
                        // Assert
                        using (var s = ram.Open("bigfile.copy", FileMode.Open, FileAccess.Read, FileShare.None))
                        {
                            byte[] buf = new byte[4096];
                            for (int i = 0; i < blocks; i++)
                            {
                                int x = s.Read(buf, 0, buf.Length);
                                Assert.AreEqual(buf.Length, x);
                                for (int j = 0; j < buf.Length; j++) 
                                    Assert.AreEqual(buf[j], (byte)(j & 0xff));
                            }
                        }
                    }

                    // Copy to existing file
                    {
                        // Operation
                        OperationBase op = new CopyFile(session, ram, "bigfile", ram, "bigfile.copy");
                        try
                        {
                            // Estimate
                            op.Estimate();
                            //
                            Assert.Fail("Estimate should have failed");
                        } catch (FileSystemException)
                        {
                            // Catched
                        }
                    }

                    // Copy, run out of memory, rollback
                    {
                        IFileSystem dst = new MemoryFileSystem(blockSize: 1024, maxSpace: 2048);
                        // Operation
                        OperationBase op = new CopyFile(session, ram, "bigfile", dst, "bigfile.copy");
                        //
                        op.Estimate();
                        //
                        Assert.IsTrue(op.CanRollback);
                        try
                        {
                            //
                            op.Run(rollbackOnError: true);
                        } catch (FileSystemExceptionOutOfDiskSpace)
                        {
                            Assert.IsFalse(dst.Exists("bigfile.copy"));
                        }
                    }


                }
            }
        }

        [TestMethod]
        public void CopyTree()
        {
        }

        [TestMethod]
        public void CreateDirectory()
        {
        }

        [TestMethod]
        public void DeleteDirectory()
        {
        }

        [TestMethod]
        public void DeleteFile()
        {
        }

        [TestMethod]
        public void DeleteTree()
        {
        }

        [TestMethod]
        public void Move()
        {
        }

        [TestMethod]
        public void MoveTree()
        {
        }

        [TestMethod]
        public void Dispose()
        {
        }

        static string Repeat(string x, int c) { StringBuilder sb = new StringBuilder(); for (int i = 0; i < c; i++) sb.Append(x); return sb.ToString(); }

        class Observer : IObserver<IEvent>
        {
            public ArrayList<IEvent> events = new ArrayList<IEvent>();
            public bool closed;
            public Exception error;
            public IEvent Last => events.Count == 0 ? null : events[events.Count - 1];

            public void OnCompleted() => closed = true;
            public void OnError(Exception error) => this.error = error;
            public void OnNext(IEvent @event) => events.Add(@event);
        }
    }
}
