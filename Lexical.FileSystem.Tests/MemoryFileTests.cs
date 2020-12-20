using Lexical.FileSystem.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lexical.FileSystem.Tests
{
    [TestClass]
    public class MemoryFileTests
    {
        public TestContext TestContext { get; set; }

        public static byte[] HelloWorld = Encoding.UTF8.GetBytes("Hello World!\r\n");
        public static byte[] HelloWorld_x100 = Encoding.UTF8.GetBytes(Repeat("Hello World!\r\n", 100));
        public static byte[] HelloWorld_x10000 = Encoding.UTF8.GetBytes(Repeat("Hello World!\r\n", 10000));

        /// <summary>
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
        }

        /// <summary>
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
        }

        /// <summary>
        /// Test Observe changes
        /// </summary>
        [TestMethod]
        public void TestObserve()
        {
            MemoryFile file = new MemoryFile();
            Observer observer = new Observer();
            IDisposable disposable = file.Subscribe(observer);
            using (Stream s = file.Open(FileAccess.ReadWrite, FileShare.None))
            {
                byte[] data = HelloWorld_x100;
                s.Write(data, 0, data.Length);
                Assert.AreEqual(data.Length, s.Position);
                Assert.AreEqual(data.Length, s.Length);
                Assert.AreEqual(1, observer.events.Count);
                Assert.AreSame(file, observer.events[0].File);

                Thread.Sleep(1500);
                s.SetLength(100L);
                Assert.AreEqual(2, observer.events.Count);
                Assert.AreSame(file, observer.events[1].File);
                Assert.AreEqual(s.Length, 100L);
                Assert.AreEqual(file.Length, 100L);
            }
            Assert.IsFalse(observer.closed);
            disposable.Dispose();
            Assert.IsTrue(observer.closed);
        }

        /// <summary>
        /// Test file access
        /// </summary>
        [TestMethod]
        public void TestFileAccess()
        {
            MemoryFile file = new MemoryFile();
            using (var ss = file.Open(FileAccess.ReadWrite, FileShare.None)) ss.Write(HelloWorld_x10000);

            // Read access, try to write
            using (Stream s = file.Open(FileAccess.Read, FileShare.None))
            {
                // Write byte
                try
                {
                    s.WriteByte(4);
                    Assert.Fail();
                }
                catch (IOException) { }

                // Write bytes
                try
                {
                    s.Write(HelloWorld, 0, HelloWorld.Length);
                    Assert.Fail();
                }
                catch (IOException) { }
            }

            // Write access, try to read
            using (Stream s = file.Open(FileAccess.Write, FileShare.None))
            {
                try
                {
                    int data = s.ReadByte();
                    Assert.Fail();
                }
                catch (IOException) { }

                try
                {
                    byte[] data = new byte[4096];
                    int count = s.Read(data, 0, data.Length);
                    Assert.Fail();
                }
                catch (IOException) { }
            }
        }

        /// <summary>
        /// Test file share
        /// </summary>
        [TestMethod]
        public void TestFileShare()
        {
            MemoryFile file = new MemoryFile();
            using (var ss = file.Open(FileAccess.ReadWrite, FileShare.None)) ss.Write(HelloWorld_x10000);

            // Allow to share read, not write
            {
                using (Stream s1 = file.Open(FileAccess.ReadWrite, FileShare.Read))
                {
                    // Write not allowed
                    try
                    {
                        Stream s2 = file.Open(FileAccess.Write, FileShare.ReadWrite);
                        Assert.Fail();
                    }
                    catch (IOException) { }

                    // Read is allowed
                    using (Stream s3 = file.Open(FileAccess.Read, FileShare.ReadWrite)) { }
                }

                // Write is allowed now
                using (Stream s4 = file.Open(FileAccess.Write, FileShare.ReadWrite)) { }

            }

            // Allow to share read, not write
            {
                using (Stream s1 = file.Open(FileAccess.ReadWrite, FileShare.Write))
                {
                    // Read not allowed
                    try
                    {
                        Stream s2 = file.Open(FileAccess.ReadWrite, FileShare.ReadWrite);
                        Assert.Fail();
                    }
                    catch (IOException) { }

                    // Write is allowed
                    using (Stream s3 = file.Open(FileAccess.Write, FileShare.ReadWrite)) { }
                }

                // Read is allowed now
                using (Stream s4 = file.Open(FileAccess.Read, FileShare.ReadWrite)) { }
            }

        }

        /// <summary>
        /// Test Reads, Writes
        /// </summary>
        [TestMethod]
        public void TestReadWrite()
        {
            // Reads and writes
            {
                MemoryFile file = new MemoryFile();
                using (var ss = file.Open(FileAccess.ReadWrite, FileShare.None))
                {
                    // Append HelloWorld 10000x
                    ss.Write(HelloWorld_x10000);
                    Assert.AreEqual(HelloWorld_x10000.Length, ss.Position);
                    Assert.AreEqual(HelloWorld_x10000.Length, ss.Length);

                    // Overwrite HelloWorld 10000x
                    ss.Position = 0L;
                    ss.Write(HelloWorld_x10000);
                    Assert.AreEqual(HelloWorld_x10000.Length, ss.Position);
                    Assert.AreEqual(HelloWorld_x10000.Length, ss.Length);
                }

                // Read hello world 10000 times
                using (var s = file.Open(FileAccess.Read, FileShare.None))
                {
                    byte[] data = new byte[HelloWorld.Length];
                    for (int i = 0; i < 10000; i++)
                    {
                        int c = s.Read(data, 0, data.Length);
                        Assert.AreEqual(c, data.Length);
                        for (int j = 0; j < data.Length; j++)
                            Assert.AreEqual(data[j], HelloWorld[j]);
                    }
                    // Check position
                    Assert.AreEqual(HelloWorld_x10000.Length, s.Position);
                    Assert.AreEqual(HelloWorld_x10000.Length, file.Length);

                    // Try to read one more byte
                    {
                        int value = s.ReadByte();
                        Assert.AreEqual(-1, value);
                    }

                    // Try to read block
                    try
                    {
                        int c = s.Read(data, 0, data.Length);
                        Assert.AreEqual(0, c);
                    }
                    catch (IOException) { }
                }
            }

            // Reads and writes, one byte at time
            {
                MemoryFile file = new MemoryFile();

                using (var ss = file.Open(FileAccess.ReadWrite, FileShare.None))
                {
                    // Append HelloWorld 10000x, one byte at time
                    for (int i = 0; i < HelloWorld_x10000.Length; i++) ss.WriteByte(HelloWorld_x10000[i]);
                    Assert.AreEqual(HelloWorld_x10000.Length, ss.Position);
                    Assert.AreEqual(HelloWorld_x10000.Length, ss.Length);

                    // Overwrite HelloWorld 10000x, one byte at time
                    ss.Position = 0L;
                    for (int i = 0; i < HelloWorld_x10000.Length; i++) ss.WriteByte(HelloWorld_x10000[i]);
                    Assert.AreEqual(HelloWorld_x10000.Length, ss.Position);
                    Assert.AreEqual(HelloWorld_x10000.Length, ss.Length);
                }

                // Check position
                Assert.AreEqual(HelloWorld_x10000.Length, file.Length);

                // Read hello world 10000 times
                using (var s = file.Open(FileAccess.Read, FileShare.None))
                {
                    byte[] data = new byte[HelloWorld.Length];
                    for (int i = 0; i < 10000; i++)
                    {
                        int c = s.Read(data, 0, data.Length);
                        Assert.AreEqual(c, data.Length);
                        for (int j = 0; j < data.Length; j++)
                            Assert.AreEqual(data[j], HelloWorld[j]);
                    }
                    // Check position
                    Assert.AreEqual(HelloWorld_x10000.Length, s.Position);
                    Assert.AreEqual(HelloWorld_x10000.Length, file.Length);
                }

            }


        }

        /// <summary>
        /// Test SetLength
        /// </summary>
        [TestMethod]
        public void TestSetLength()
        {
            MemoryFile file = new MemoryFile();
            // SetLength()
            using (var s = file.Open(FileAccess.ReadWrite, FileShare.None))
            {
                // Append HelloWorld 10000x
                s.Write(HelloWorld_x10000);
                Assert.AreEqual(HelloWorld_x10000.Length, s.Position);
                Assert.AreEqual(HelloWorld_x10000.Length, s.Length);

                // SetLength() - Shorten
                long newLength = 10000L;
                s.SetLength(newLength);
                Assert.AreEqual(newLength, s.Position);
                Assert.AreEqual(newLength, file.Length);
                s.Position = 0L;
                for (long i = 0L; i < newLength; i++)
                    Assert.AreEqual(HelloWorld_x10000[i], s.ReadByte());
                Assert.AreEqual(-1, s.ReadByte());

                // SetLength() - Clear
                s.SetLength(0L);
                Assert.AreEqual(0L, s.Position);
                Assert.AreEqual(0L, file.Length);

                // SetLength() - Grow
                s.SetLength(newLength);
                Assert.AreEqual(newLength, file.Length);
                s.Position = 0L;
                // Assert all new bytes are zero
                for (long i = 0L; i < newLength; i++)
                    Assert.AreEqual(0, s.ReadByte());
                Assert.AreEqual(-1, s.ReadByte());

                // SetLength() - Clear, Append, Grow
                s.SetLength(0L);
                s.Write(HelloWorld_x10000);
                long oldLength = s.Length;
                newLength = s.Length + 10000L;
                s.SetLength(newLength);
                // Assert all new bytes are zero
                s.Position = oldLength;
                for (int i = 0; i < 10000; i++)
                    Assert.AreEqual(0, s.ReadByte());
            }
        }

        /// <summary>
        /// Test concurrent writes
        /// </summary>
        [TestMethod]
        public void TestConcurrentWrite()
        {
            MemoryFile file = new MemoryFile();
            // SetLength()
            using (var s = file.Open(FileAccess.ReadWrite, FileShare.None))
            {
                // Write "HelloWorld" with 10000 tasks concurrently. 
                Parallel.For(0, 10000, (int x) => s.Write(HelloWorld));

                s.Position = 0L;

                byte[] data = new byte[HelloWorld.Length];
                for (int i = 0; i < 10000; i++)
                {
                    int c = s.Read(data, 0, data.Length);
                    Assert.AreEqual(c, data.Length);
                    for (int j = 0; j < data.Length; j++)
                        Assert.AreEqual(data[j], HelloWorld[j]);
                }
                Assert.AreEqual(-1, s.ReadByte());

                // Check position
                Assert.AreEqual(HelloWorld_x10000.Length, s.Position);
                Assert.AreEqual(HelloWorld_x10000.Length, file.Length);
            }
        }

        /// <summary>
        /// Test concurrent reads
        /// </summary>
        [TestMethod]
        public void TestConcurrentRead()
        {
            MemoryFile file = new MemoryFile();
            // Write data
            using (var s = file.Open(FileAccess.Write, FileShare.None)) s.Write(HelloWorld_x10000);

            // Read data with 100 tasks concurrently
            Parallel.For(0, 100, (int x) =>
            {
                using (var s = file.Open(FileAccess.Read, FileShare.Read))
                {
                    byte[] data = new byte[HelloWorld.Length];
                    for (int i = 0; i < 10000; i++)
                    {
                        int c = s.Read(data, 0, data.Length);
                        Assert.AreEqual(c, data.Length);
                        for (int j = 0; j < data.Length; j++)
                            Assert.AreEqual(data[j], HelloWorld[j]);
                    }
                    Assert.AreEqual(-1, s.ReadByte());
                }
            });
        }

        /// <summary>Test quota</summary>
        [TestMethod]
        public void Quota()
        {
            BlockPool pool = new BlockPool(1024, 3, 3, true);
            MemoryFile file = new MemoryFile(pool);
            using (var s = file.Open(FileAccess.ReadWrite, FileShare.None))
            {
                Assert.AreEqual(0L, pool.BytesAllocated);

                s.Write(new byte[1024]);
                Assert.AreEqual(1024L, pool.BytesAllocated);
                Assert.AreEqual(2048L, pool.BytesAvailable);
                Assert.AreEqual(1024L, s.Length);

                s.Write(new byte[1024]);
                Assert.AreEqual(2048L, pool.BytesAllocated);
                Assert.AreEqual(1024L, pool.BytesAvailable);
                Assert.AreEqual(2048L, s.Length);

                s.Write(new byte[1024]);
                Assert.AreEqual(3072L, s.Length);
                Assert.AreEqual(3072L, pool.BytesAllocated);
                Assert.AreEqual(0L, pool.BytesAvailable);

                try
                {
                    s.Write(new byte[1024]);
                    Assert.Fail();
                } catch (FileSystemExceptionOutOfDiskSpace)
                {
                }
                Assert.AreEqual(3072, s.Length);

                try
                {
                    s.WriteByte(3);
                    Assert.Fail();
                }
                catch (FileSystemExceptionOutOfDiskSpace)
                {
                }
                Assert.AreEqual(3072, s.Length);

                s.SetLength(3071);
                Assert.AreEqual(3071, s.Length);

                s.WriteByte(3);
                Assert.AreEqual(3072, s.Length);

                s.SetLength(0L);
                Assert.AreEqual(0, s.Length);
                Assert.AreEqual(0L, pool.BytesAllocated);
                Assert.AreEqual(3072L, pool.BytesAvailable);

                s.Write(new byte[1024]);
                s.Write(new byte[1024]);
                s.Write(new byte[1024]);
                Assert.AreEqual(3072L, s.Length);
                Assert.AreEqual(3072L, pool.BytesAllocated);
                Assert.AreEqual(0L, pool.BytesAvailable);
            }

            Assert.AreEqual(3072L, pool.BytesAllocated);
            Assert.AreEqual(0L, pool.BytesAvailable);
            Stream ss = file.Open(FileAccess.ReadWrite, FileShare.ReadWrite);
            file.Dispose();
            Assert.AreEqual(3072L, pool.BytesAllocated);
            Assert.AreEqual(0L, pool.BytesAvailable);
            ss.Dispose();
            Assert.AreEqual(0L, pool.BytesAllocated);
            Assert.AreEqual(3072L, pool.BytesAvailable);
        }

        class Observer : IObserver<MemoryFile.ModifiedEvent>
        {
            public List<MemoryFile.ModifiedEvent> events = new List<MemoryFile.ModifiedEvent>();
            public bool closed;
            public Exception error;

            public void OnCompleted() => closed = true;
            public void OnError(Exception error) => this.error = error;
            public void OnNext(MemoryFile.ModifiedEvent value) => events.Add(value);
        }

        static string Repeat(string x, int c)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < c; i++) sb.Append(x);
            return sb.ToString();
        }

    }

}
