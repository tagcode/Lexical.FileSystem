using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using System.Threading;
using Lexical.FileSystem;
using Microsoft.Extensions.FileProviders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Lexical.FileSystem.Internal;
using Lexical.FileSystem.Utility;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace Lexical.FileSystem.Tests
{
    [TestClass]
    public class FileSystemProviderTests
    {
        public static byte[] HelloWorld = Encoding.UTF8.GetBytes("Hello World!\r\n");
        public static byte[] HelloWorld_x100 = Encoding.UTF8.GetBytes(Repeat("Hello World!\r\n", 100));
        public static byte[] HelloWorld_x10000 = Encoding.UTF8.GetBytes(Repeat("Hello World!\r\n", 10000));

        public TestContext TestContext { get; set; }

        MemoryFileSystem ram;
        IFileProvider fp;

        /// <summary>
        /// </summary>
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
            ram.CreateDirectory("file://");
            fp = ram.ToFileProvider();
        }

        /// <summary>
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
        }

        /// <summary>
        /// </summary>
        [TestMethod]
        public void Test()
        {
            Assert.IsFalse(fp.GetDirectoryContents("/nonexisting").Exists);
            Assert.IsTrue(fp.GetDirectoryContents("/tmp").Exists);
            Assert.IsTrue(fp.GetDirectoryContents("/tmp/dir").Exists);
            Assert.IsTrue(fp.GetDirectoryContents("c:").Exists);
            Assert.IsFalse(fp.GetDirectoryContents("/tmp/helloworld.txt").Exists);
            Assert.IsTrue(fp.GetFileInfo("/tmp/helloworld.txt").Exists);
            Assert.IsFalse(fp.GetFileInfo("/tmp").Exists);

            // Read file
            using(Stream s = fp.GetFileInfo("/tmp/helloworld.txt").CreateReadStream())
            {
                byte[] data = new byte[100];
                int c = s.Read(data, 0, 100);
                Assert.AreEqual(c, HelloWorld.Length);
                for (int i = 0; i < c; i++) Assert.AreEqual(data[i], HelloWorld[i]);
            }

            // Observe
            IChangeToken token = fp.Watch("/tmp/**");
            Semaphore semaphore = new Semaphore(0, int.MaxValue);
            IDisposable disposable = token.RegisterChangeCallback(o => semaphore.Release(), null);

            // Test, not activated
            Assert.IsFalse(semaphore.WaitOne(300));
            Assert.IsFalse(token.HasChanged);

            // Test, not activated
            ram.CreateFile("/not-monited-path.txt");
            Assert.IsFalse(semaphore.WaitOne(300));
            Assert.IsFalse(token.HasChanged);

            // Test, not activated
            disposable.Dispose();
            ram.CreateFile("/tmp/monited-path.txt");
            Assert.IsFalse(semaphore.WaitOne(300));
            Assert.IsTrue(token.HasChanged);

            // Observe again
            token = fp.Watch("/tmp/**");
            semaphore = new Semaphore(0, int.MaxValue);
            disposable = token.RegisterChangeCallback(o => semaphore.Release(), null);
            ram.CreateFile("/tmp/monited-path-again.txt");
            Assert.IsTrue(semaphore.WaitOne(300));
            Assert.IsTrue(token.HasChanged);

            // Shouldn't activate any more
            ram.CreateFile("/tmp/monited-path-again-one-more-time.txt");
            Assert.IsFalse(semaphore.WaitOne(300));

        }

        static string Repeat(string x, int c) { StringBuilder sb = new StringBuilder(); for (int i = 0; i < c; i++) sb.Append(x); return sb.ToString(); }

    }
}
