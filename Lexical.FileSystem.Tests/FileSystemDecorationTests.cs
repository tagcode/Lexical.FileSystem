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
using Lexical.FileSystem.Decoration;
using Lexical.Utils.Permutation;

namespace Lexical.FileSystem.Tests
{
    // TODO 
    //   Only Move2() is designed to test FileSystemDecoration features.
    //   Other methods are a copy from MemoryFileSystemTests and aren't designed to test FileSystemDecoration and should be updated with better tests..

    [TestClass]
    public class FileSystemDecorationTests
    {
        public static byte[] HelloWorld = Encoding.UTF8.GetBytes("Hello World!\r\n");
        public static byte[] HelloWorld_x100 = Encoding.UTF8.GetBytes(Repeat("Hello World!\r\n", 100));
        public static byte[] HelloWorld_x10000 = Encoding.UTF8.GetBytes(Repeat("Hello World!\r\n", 10000));

        public TestContext TestContext { get; set; }

        MemoryFileSystem ram, ram2;
        FileSystemDecoration fs;
        Observer observer;

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

            ram2 = new MemoryFileSystem();
            ram2.CreateDirectory("/ram2");
            ram2.CreateDirectory("/ram2/dir");
            ram2.CreateFile("/ram2/dir/helloworld.txt", HelloWorld);
            ram2.CreateFile("/ram2/dir/helloworld_100.txt", HelloWorld_x100);
            ram2.CreateFile("/ram2/dir/helloworld_10000.txt", HelloWorld_x10000);

            fs = ram.Concat(ram2);
            fs.Observe("**", observer = new Observer());
            observer.events.Clear();
        }

        [TestCleanup]
        public void Cleanup()
        {
            fs.Dispose();
            fs = null;
        }


        /// <summary>
        /// Move between filesystem volumes
        /// </summary>
        [TestMethod]
        public void Move2()
        {
            // Move file
            Assert.IsTrue(fs.Exists("/tmp/helloworld.txt"));
            Assert.IsTrue(ram.Exists("/tmp/helloworld.txt"));
            fs.Move("/tmp/helloworld.txt", "/ram2/moved.txt");
            Assert.IsFalse(fs.Exists("/tmp/helloworld.txt"));
            Assert.IsFalse(ram.Exists("/tmp/helloworld.txt"));
            Assert.IsTrue(fs.Exists("/ram2/moved.txt"));
            Assert.IsTrue(ram2.Exists("/ram2/moved.txt"));

            // Move dir
            Assert.IsTrue(fs.Exists("/ram2/dir/"));
            Assert.IsTrue(fs.Exists("/ram2/dir/helloworld.txt"));
            Assert.IsTrue(ram2.Exists("/ram2/dir/"));
            Assert.IsTrue(ram2.Exists("/ram2/dir/helloworld.txt"));
            fs.Move("/ram2/dir/", "/tmp/moved-dir/");
            Assert.IsFalse(fs.Exists("/ram2/dir/"));
            Assert.IsFalse(ram2.Exists("/ram2/dir/"));
            Assert.IsFalse(fs.Exists("/ram2/dir/helloworld.txt"));
            Assert.IsFalse(ram2.Exists("/ram2/dir/helloworld.txt"));
            Assert.IsTrue(fs.Exists("/tmp/moved-dir/"));
            Assert.IsTrue(ram.Exists("/tmp/moved-dir/"));
            Assert.IsTrue(fs.Exists("/tmp/moved-dir/helloworld.txt"));
            Assert.IsTrue(ram.Exists("/tmp/moved-dir/helloworld.txt"));
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
