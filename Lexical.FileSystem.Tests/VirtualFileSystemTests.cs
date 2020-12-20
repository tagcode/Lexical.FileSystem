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

namespace Lexical.FileSystem.Tests
{
    [TestClass]
    public class VirtualFileSystemTests
    {
        public static byte[] HelloWorld = Encoding.UTF8.GetBytes("Hello World!\r\n");
        public static byte[] HelloWorld_x100 = Encoding.UTF8.GetBytes(Repeat("Hello World!\r\n", 100));
        public static byte[] HelloWorld_x10000 = Encoding.UTF8.GetBytes(Repeat("Hello World!\r\n", 10000));

        public TestContext TestContext { get; set; }

        MemoryFileSystem ram;
        VirtualFileSystem vfs;
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
            //ram.CreateDirectory("file://");
        }

        [TestCleanup]
        public void Cleanup()
        {
            ram.Dispose();
            ram = null;
            vfs?.Dispose();
            vfs = null;
        }

        [TestMethod]
        public void Mount()
        {
            // 1. Observe, 2. Mount, 3. Unmount
            {
                vfs = new VirtualFileSystem();
                vfs.Observe("/tmp/helloworld*.txt", observer = new Observer());
                observer.events.Clear();

                vfs.Mount("", ram);
                Assert.IsTrue(observer.events.All(e=>e is ICreateEvent&&e.Path.Contains("helloworld")));

                observer.events.Clear();
                vfs.Unmount("");
                Assert.IsTrue(observer.events.All(e => e is IDeleteEvent && e.Path.Contains("helloworld")));
            }

            // 1. Observe, 2. Mount, 3. Unobserve, 4. Unmount
            {
                vfs = new VirtualFileSystem();
                var handle = vfs.Observe("/tmp/helloworld*.txt", observer = new Observer());
                observer.events.Clear();

                vfs.Mount("", ram);
                Assert.IsTrue(observer.events.All(e => e is ICreateEvent && e.Path.Contains("helloworld")));

                observer.events.Clear();
                handle.Dispose();
                vfs.Unmount("");
                Assert.IsTrue(observer.events.Count == 0);
            }

            // 1. Observe, 2. Mount, 3. Dispose, 4. Unmount
            {
                vfs = new VirtualFileSystem();
                var handle = vfs.Observe("/tmp/helloworld*.txt", observer = new Observer());
                observer.events.Clear();

                vfs.Mount("", ram);
                Assert.IsTrue(observer.events.All(e => e is ICreateEvent && e.Path.Contains("helloworld")));

                observer.events.Clear();
                vfs.Dispose();
                handle.Dispose();
                Assert.IsTrue(observer.events.Count == 0);
            }


        }

        [TestMethod]
        public void Browse()
        {
            // Union of entries
            {
                MemoryFileSystem ram = new MemoryFileSystem();
                MemoryFileSystem zip = new MemoryFileSystem();
                ram.CreateFile("myfile.zip", new byte[1000]);
                zip.CreateFile("content.txt", new byte[10000]);
                VirtualFileSystem vfs = new VirtualFileSystem()
                    .Mount("", ram)
                    .Mount("myfile.zip", zip);
                IEntry e = vfs.Browse("")[0];
                Assert.IsTrue(e.IsFile());
                Assert.IsTrue(e.IsDirectory());
                Assert.IsTrue(e.IsMountPoint());
                Assert.AreEqual("myfile.zip/", e.Path);
            }
        }

        [TestMethod]
        public void GetEntry()
        {
            // Union of entries
            {
                MemoryFileSystem ram = new MemoryFileSystem();
                MemoryFileSystem zip = new MemoryFileSystem();
                ram.CreateFile("myfile.zip", new byte[1000]);
                zip.CreateFile("content.txt", new byte[10000]);
                VirtualFileSystem vfs = new VirtualFileSystem()
                    .Mount("", ram)
                    .Mount("myfile.zip", zip);
                IEntry e = vfs.GetEntry("myfile.zip");
                Assert.IsTrue(e.IsFile());
                Assert.IsTrue(e.IsDirectory());
                Assert.IsTrue(e.IsMountPoint());
                Assert.AreEqual("myfile.zip/", e.Path);
            }
        }

        [TestMethod]
        public void CreateDirectory()
        {
        }

        [TestMethod]
        public void Delete()
        {
        }

        [TestMethod]
        public void Move()
        {
        }

        [TestMethod]
        public void Open()
        {
        }

        [TestMethod]
        public void Dispose()
        {
        }

        [TestMethod]
        public void ListMountPoints()
        {
            MemoryFileSystem ram = new MemoryFileSystem();
            VirtualFileSystem vfs = new VirtualFileSystem()
                .Mount("/tmp/", FileSystem.Temp)
                .Mount("/ram/", ram);

            IMountEntry[] mountpoints = vfs.ListMountPoints();
            Assert.AreEqual(2, mountpoints.Length);
            Assert.IsTrue(mountpoints.Any(mp => mp.Path == "/tmp/"));
            Assert.IsTrue(mountpoints.Any(mp => mp.Path == "/ram/"));
            Assert.IsTrue(mountpoints.Any(mp => mp.Mounts[0].FileSystem == FileSystem.Temp));
            Assert.IsTrue(mountpoints.Any(mp => mp.Mounts[0].FileSystem == ram));

            Assert.AreEqual(ram, vfs.GetEntry("/ram/").Mounts()[0].FileSystem);
            Assert.AreEqual(FileSystem.Temp, vfs.GetEntry("/tmp/").Mounts()[0].FileSystem);
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
