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
using Lexical.Utils.Permutation;
using Lexical.FileSystem.Decoration;
using Lexical.FileSystem.Utility;

namespace Lexical.FileSystem.Tests
{
    [TestClass]
    public class FileSystemTests2
    {
        public static byte[] HelloWorld = Encoding.UTF8.GetBytes("Hello World!\r\n");
        public static byte[] HelloWorld_x100 = Encoding.UTF8.GetBytes(Repeat("Hello World!\r\n", 100));
        public static byte[] HelloWorld_x10000 = Encoding.UTF8.GetBytes(Repeat("Hello World!\r\n", 10000));

        static Case case_ms = new Case("Class", nameof(MemoryFileSystem), dep_blockpool, run => new MemoryFileSystem( run["BlockPool"] as IBlockPool ));
        static Case case_decor_fs = new Case("Class", nameof(FileSystemDecoration) + "(" + nameof(FileSystem) + ")", null, run => FileSystem.Temp.Decorate(null).AddSourceToBeDisposed());
        static Case case_decor_ms = new Case("Class", nameof(FileSystemDecoration) + "(" + nameof(MemoryFileSystem) + ")", dep_blockpool, run => new MemoryFileSystem(run["BlockPool"] as IBlockPool).Decorate(null).AddSourceToBeDisposed());
        static Case case_vfs_ms = new Case("Class", nameof(VirtualFileSystem) + "ms", dep_blockpool, run => new VirtualFileSystem().Mount("", new MemoryFileSystem(run["BlockPool"] as IBlockPool)).AddMountsToBeDisposed());
        static Case case_vfs_2ms = new Case("Class", nameof(VirtualFileSystem) + "2ms", dep_blockpool, run => new VirtualFileSystem().Mount("", new MemoryFileSystem(run["BlockPool"] as IBlockPool), new MemoryFileSystem(run["BlockPool"] as IBlockPool)).AddMountsToBeDisposed());
        static Case case_vfs_fsms = new Case("Class", nameof(VirtualFileSystem) + "fsms", dep_blockpool, run => new VirtualFileSystem().Mount("", FileSystem.Temp, new MemoryFileSystem(run["BlockPool"] as IBlockPool)).AddMountsToBeDisposed());

        static string[] dep_blockpool = new string[] { "BlockPool" };
        static Case blockpool_3KB = new Case("BlockPool", "3KB", null, run => new BlockPool(1024, 3, 3, true));
        static Case blockpool_1TB = new Case("BlockPool", "1TB", null, run => new BlockPool(1024, 1 << 30, 2, true));
        static Case blockpool_Pseudo = new Case("BlockPool", "Pseudo", null, run => BlockPoolPseudo.Instance);

        static Case init = new Case("Init", "", new string[] { "Class" }, run => Init((IFileSystem)run["Class"]));

        public TestContext TestContext { get; set; }

        Observer observer;

        static IFileSystem Init(IFileSystem fs)
        {
            fs.CreateDirectory("/tmp");
            fs.CreateDirectory("/tmp/dir");
            fs.CreateDirectory("/mnt");
            fs.CreateDirectory("/usr/lex");
            fs.CreateDirectory("c:/dir/dir");
            fs.CreateFile("/tmp/helloworld.txt", HelloWorld);
            fs.CreateFile("/tmp/helloworld_100.txt", HelloWorld_x100);
            fs.CreateFile("/tmp/helloworld_10000.txt", HelloWorld_x10000);
            return fs;
        }

        [TestMethod]
        public void Browse()
        {
            // Arrange
            PermutationSetup permutation = new PermutationSetup();
            permutation.Add(blockpool_1TB);
            permutation.Add(blockpool_Pseudo);

            permutation.Add(case_ms);
            //permutation.Add(decor_fs);
            permutation.Add(case_decor_ms);
            permutation.Add(case_vfs_ms);
            permutation.Add(case_vfs_2ms);
            //permutation.Add(vfs_fsms);
            permutation.Add(init);

            // Act & Assert
            foreach (Scenario scenario in permutation.Scenarios)
            {
                using (Run run = scenario.Run().Initialize())
                {
                    IFileSystem ram = run.Parameters["Class"] as IFileSystem;
                    Assert.IsTrue(ram.Browse("/tmp/").Count >= 3);
                    Assert.IsTrue(ram.Browse("c:").Any(e => e.Path == "c:/dir/"));
                    Assert.IsTrue(ram.Browse("c:/dir").Any(e => e.Path == "c:/dir/dir/"));
                    Assert.IsTrue(ram.Browse("./c:/dir").Any(e => e.Path == "c:/dir/dir/"));
                    Assert.IsTrue(ram.Browse("./c:/../c:/dir").Any(e => e.Path == "c:/dir/dir/"));
                    Assert.IsTrue(ram.Browse("/tmp").Any(e => e.Path == "/tmp/helloworld.txt"));
                    Assert.IsTrue(ram.Browse(".//tmp").Any(e => e.Path == "/tmp/helloworld.txt"));
                    Assert.IsTrue(ram.Browse(".//tmp/.").Any(e => e.Path == "/tmp/helloworld.txt"));
                    Assert.IsTrue(ram.Browse(".//tmp/../tmp").Any(e => e.Path == "/tmp/helloworld.txt"));
                }
            }
        }

        [TestMethod]
        public void GetEntry()
        {
            // Arrange
            PermutationSetup permutation = new PermutationSetup();
            permutation.Add(blockpool_1TB);
            permutation.Add(blockpool_Pseudo);

            permutation.Add(case_ms);
            //permutation.Add(decor_fs);
            permutation.Add(case_decor_ms);
            permutation.Add(case_vfs_ms);
            permutation.Add(case_vfs_2ms);
            //permutation.Add(vfs_fsms);
            permutation.Add(init);


            // Act & Assert
            foreach (Scenario scenario in permutation.Scenarios)
            {
                using (Run run = scenario.Run().Initialize())
                {
                    IFileSystem fs = run.Parameters["Class"] as IFileSystem;
                    fs.Observe("**", observer = new Observer());
                    observer.events.Clear();

                    // root
                    Assert.IsNotNull(fs.GetEntry(""));
                    // "/"
                    Assert.IsNotNull(fs.GetEntry("/"));

                    // "//"
                    Assert.IsNull(fs.GetEntry("//"));
                    fs.CreateDirectory("//");
                    Assert.IsNotNull(fs.GetEntry("//"));
                    Assert.AreEqual("//", observer.Last.Path);
                    Assert.IsTrue(observer.Last is ICreateEvent);

                    // "///"
                    Assert.IsNull(fs.GetEntry("///"));
                    fs.CreateDirectory("///");
                    Assert.IsNotNull(fs.GetEntry("///"));
                    Assert.AreEqual("///", observer.Last.Path);
                    Assert.IsTrue(observer.Last is ICreateEvent);

                    // "/mnt/"
                    Assert.IsNotNull(fs.GetEntry("/mnt"));
                    Assert.IsNotNull(fs.GetEntry("/mnt/"));
                    Assert.IsNull(fs.GetEntry("/mnt//"));
                    fs.CreateDirectory("/mnt//");
                    Assert.IsNotNull(fs.GetEntry("/mnt//"));
                    Assert.AreEqual("/mnt//", observer.Last.Path);
                    Assert.IsTrue(observer.Last is ICreateEvent);

                    // "c:"
                    Assert.IsNotNull(fs.GetEntry("c:"));
                    Assert.IsNotNull(fs.GetEntry("c:/"));
                    Assert.IsNull(fs.GetEntry("c://"));
                    fs.CreateDirectory("c://");
                    Assert.IsNotNull(fs.GetEntry("c://"));
                    Assert.AreEqual("c://", observer.Last.Path);
                    Assert.IsTrue(observer.Last is ICreateEvent);

                    //
                    Assert.AreEqual("", fs.GetEntry(".").Path);
                    Assert.AreEqual("", fs.GetEntry("/..").Path);
                    Assert.AreEqual("", fs.GetEntry("c:/..").Path);
                    Assert.AreEqual("", fs.GetEntry("./c:/..").Path);
                    // test . and .. in paths
                    // test create empty name

                    try
                    {
                        fs.Open("..", FileMode.Open, FileAccess.Read, FileShare.None);
                        Assert.Fail();
                    }
                    catch (InvalidOperationException) { }
                    catch (FileNotFoundException) { }
                    catch (DirectoryNotFoundException) { }
                    fs.Observe("**", observer = new Observer());
                    observer.events.Clear();
                }
            }
        }

        [TestMethod]
        public void CreateDirectory()
        {
            // Arrange
            PermutationSetup permutation = new PermutationSetup();

            permutation.Add(blockpool_1TB);
            permutation.Add(blockpool_Pseudo);

            permutation.Add(case_ms);
            //permutation.Add(decor_fs);
            permutation.Add(case_decor_ms);
            permutation.Add(case_vfs_ms);
            permutation.Add(case_vfs_2ms);
            //permutation.Add(vfs_fsms);
            permutation.Add(init);

            // Act & Assert
            foreach (Scenario scenario in permutation.Scenarios)
            {
                using (Run run = scenario.Run().Initialize())
                {
                    IFileSystem fs = run.Parameters["Class"] as IFileSystem;
                    fs.Observe("**", observer = new Observer());
                    observer.events.Clear();

                    // root
                    Assert.IsNotNull(fs.GetEntry(""));
                    Assert.IsFalse(fs.Browse("").Any(e => e.Path == "dir1/"));
                    fs.CreateDirectory("dir1/../dir2/../dir3/../dir4");
                    Assert.IsTrue(fs.Browse("").Any(e => e.Path == "dir1/"));
                    Assert.IsTrue(fs.Exists("dir1/"));
                    Assert.IsTrue(fs.Exists("dir2/"));
                    Assert.IsTrue(fs.Exists("dir3"));
                    Assert.IsTrue(fs.Exists("dir4"));

                    try
                    {
                        fs.CreateDirectory("../dir");
                        Assert.Fail();
                    }
                    catch (IOException) { }
                    fs.Observe("**", observer = new Observer());
                    observer.events.Clear();
                }
            }
        }

        [TestMethod]
        public void Delete()
        {
            // Arrange
            PermutationSetup permutation = new PermutationSetup();
            permutation.Add(blockpool_1TB);
            permutation.Add(blockpool_Pseudo);

            permutation.Add(case_ms);
            //permutation.Add(decor_fs);
            permutation.Add(case_decor_ms);
            permutation.Add(case_vfs_ms);
            permutation.Add(case_vfs_2ms);
            //permutation.Add(vfs_fsms);
            permutation.Add(init);

            // Act & Assert
            foreach (Scenario scenario in permutation.Scenarios)
            {
                using (Run run = scenario.Run().Initialize())
                {
                    IFileSystem fs = run.Parameters["Class"] as IFileSystem;
                    fs.Observe("**", observer = new Observer());
                    observer.events.Clear();

                    try { fs.Delete(""); Assert.Fail(); } catch (IOException) { }
                    try { fs.Delete("."); Assert.Fail(); } catch (IOException) { }
                    try { fs.Delete(".."); Assert.Fail(); } catch (IOException) { }

                    // Delete file
                    fs.Delete("/tmp/../tmp/helloworld.txt");
                    Assert.IsFalse(fs.Exists("/tmp/helloworld.txt"));
                    Assert.IsTrue(DateTimeOffset.UtcNow - fs.GetEntry("/tmp").LastModified < TimeSpan.FromSeconds(2));
                    Assert.IsTrue(observer.Last is IDeleteEvent);
                    Assert.IsTrue(DateTimeOffset.UtcNow - observer.Last.EventTime < TimeSpan.FromSeconds(2));
                    Assert.AreEqual(fs, observer.Last.Observer.FileSystem);
                    Assert.AreEqual("/tmp/helloworld.txt", observer.Last.Path);

                    // Delete dir
                    Assert.IsTrue(fs.Exists("/tmp"));
                    try { fs.Delete("/tmp", false); Assert.Fail(); } catch (IOException) { }

                    // Delete dir tree            
                    Assert.IsTrue(fs.Browse("/").Any(e => e.Path == "/tmp/"));
                    Assert.IsTrue(fs.Exists("/tmp"));
                    Assert.IsTrue(fs.Exists("/tmp/"));
                    fs.Delete("/tmp", true);
                    Assert.IsFalse(fs.Browse("/").Any(e => e.Path == "/tmp/"));
                    Assert.IsFalse(fs.Exists("/tmp"));
                    Assert.IsFalse(fs.Exists("/tmp/"));
                    Assert.IsFalse(fs.Exists("/tmp/helloworld_100.txt"));
                    Assert.IsTrue(observer.events.Count >= 4);
                    Assert.IsTrue(observer.Last is IDeleteEvent);
                    Assert.IsTrue(DateTimeOffset.UtcNow - observer.Last.EventTime < TimeSpan.FromSeconds(2));
                    Assert.AreEqual(fs, observer.Last.Observer.FileSystem);
                    fs.Observe("**", observer = new Observer());
                    observer.events.Clear();
                }
            }
        }

        [TestMethod]
        public void Move()
        {
            // Arrange
            PermutationSetup permutation = new PermutationSetup();
            permutation.Add(blockpool_1TB);
            permutation.Add(blockpool_Pseudo);

            permutation.Add(case_ms);
            //permutation.Add(decor_fs);
            permutation.Add(case_decor_ms);
            permutation.Add(case_vfs_ms);
            permutation.Add(case_vfs_2ms);
            //permutation.Add(vfs_fsms);
            permutation.Add(init);

            // Act & Assert
            foreach (Scenario scenario in permutation.Scenarios)
            {
                using (Run run = scenario.Run().Initialize())
                {
                    IFileSystem fs = run.Parameters["Class"] as IFileSystem;
                    fs.Observe("**", observer = new Observer());
                    observer.events.Clear();

                    // Rename to same file
                    fs.Move("/tmp/helloworld.txt", "/tmp/../tmp/helloworld.txt");

                    Assert.IsTrue(observer.events.Count == 0);
                    // Move to non-existing dir
                    try { fs.Move("/tmp/helloworld.txt", "/nonexisting/helloworld.txt"); Assert.Fail(); } catch (IOException) { }
                    // Move non-existing file
                    try { fs.Move("/tmp/nonexisting.txt", "/tmp/somefile.txt"); Assert.Fail(); } catch (IOException) { }
                    // Move over to directory
                    try { fs.Move("/tmp/helloworld.txt", "c:"); Assert.Fail(); } catch (IOException) { }

                    // Move a file
                    Assert.IsFalse(fs.Browse("c:").Any(e => e.Path == "c:/helloworld.txt"));
                    Assert.IsTrue(fs.Browse("/tmp").Any(e => e.Path == "/tmp/helloworld.txt"));
                    fs.Move("/tmp/helloworld.txt", "c:/helloworld.txt");
                    Assert.IsTrue(fs.Browse("c:").Any(e => e.Path == "c:/helloworld.txt"));
                    Assert.IsFalse(fs.Browse("/tmp").Any(e => e.Path == "/tmp/helloworld.txt"));
                    Assert.IsTrue(observer.Last is IRenameEvent);
                    Assert.IsTrue(DateTimeOffset.UtcNow - observer.Last.EventTime < TimeSpan.FromSeconds(2));
                    Assert.AreEqual(fs, observer.Last.Observer.FileSystem);

                    // Move whole tree
                    Assert.IsFalse(fs.Browse("c:").Any(e => e.Path == "c:/tmp/"));
                    Assert.IsTrue(fs.Browse("/").Any(e => e.Path == "/tmp/"));
                    fs.Move("/tmp/", "c:/tmp/");
                    Assert.IsTrue(fs.Browse("c:").Any(e => e.Path == "c:/tmp/"));
                    Assert.IsFalse(fs.Browse("/").Any(e => e.Path == "/tmp/"));
                    Assert.IsNull(fs.GetEntry("/tmp/"));
                    Assert.IsTrue(fs.Browse("c:/tmp/").Count >= 3);
                    Assert.IsTrue(observer.Last is IRenameEvent);
                    Assert.IsTrue(DateTimeOffset.UtcNow - observer.Last.EventTime < TimeSpan.FromSeconds(2));
                    Assert.AreEqual(fs, observer.Last.Observer.FileSystem);
                    Assert.IsTrue(observer.Last.Path.StartsWith("/tmp/"));
                    Assert.IsTrue(observer.Last.NewPath().StartsWith("c:/tmp/"));
                    fs.Observe("**", observer = new Observer());
                    observer.events.Clear();
                }
            }
        }

        [TestMethod]
        public void Open()
        {
            // Arrange
            PermutationSetup permutation = new PermutationSetup();
            permutation.Add(blockpool_1TB);
            permutation.Add(blockpool_Pseudo);

            permutation.Add(case_ms);
            //permutation.Add(decor_fs);
            permutation.Add(case_decor_ms);
            permutation.Add(case_vfs_ms);
            permutation.Add(case_vfs_2ms);
            //permutation.Add(vfs_fsms);
            permutation.Add(init);

            // Act & Assert
            foreach (Scenario scenario in permutation.Scenarios)
            {
                using (Run run = scenario.Run().Initialize())
                {
                    IFileSystem fs = run.Parameters["Class"] as IFileSystem;
                    fs.Observe("**", observer = new Observer());
                    observer.events.Clear();

                    // Open
                    //    Points to directory
                    try { fs.Open("/tmp", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite); Assert.Fail(); } catch (IOException) { }
                    //    File doesn't exist
                    try { fs.Open("/tmp/nonexisting.txt", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite); Assert.Fail(); } catch (IOException) { }
                    //    Access Control tests
                    try
                    {
                        using (Stream s = fs.Open("/tmp/helloworld.txt", FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                        using (Stream s2 = fs.Open("/tmp/helloworld.txt", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        { }
                        Assert.Fail();
                    }
                    catch (IOException) { }
                    try
                    {
                        using (Stream s = fs.Open("/tmp/helloworld.txt", FileMode.Open, FileAccess.Write, FileShare.Write))
                        using (Stream s2 = fs.Open("/tmp/helloworld.txt", FileMode.Open, FileAccess.Read, FileShare.Read))
                        { } // Should not open
                        Assert.Fail();
                    }
                    catch (IOException) { }


                    // OpenOrCreate
                    //    Invalid filename "", ".", ".." -> fail
                    try { fs.Open("/tmp/", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write); Assert.Fail(); } catch (IOException) { }
                    try { fs.Open("/tmp/.", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write); Assert.Fail(); } catch (IOException) { }
                    try { fs.Open("/tmp/..", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write); Assert.Fail(); } catch (IOException) { }
                    //    File already exists (with open stream) -> same file
                    {
                        // Wipe events
                        observer.events.Clear();
                        // Open handle to same file
                        string path = "/tmp/file1.txt";
                        Assert.IsFalse(fs.Browse("/tmp").Any(e => e.Path == path));
                        Stream s1 = fs.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                        Stream s2 = fs.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                        int len = HelloWorld.Length;
                        s1.Write(HelloWorld);
                        byte[] data = new byte[len];
                        s2.Read(data);
                        for (int i = 0; i < len; i++) Assert.AreEqual(HelloWorld[i], data[i]);
                        //    Assert event
                        Assert.IsTrue(observer.events[0] is ICreateEvent);
                        Assert.IsTrue(observer.events[1] is IChangeEvent);
                        Assert.IsTrue(observer.events[0].Path == path);
                        Assert.IsTrue(observer.events[1].Path == path);
                        //    Assert parent cache is flushed and children updated
                        Assert.IsTrue(fs.Browse("/tmp").Any(e => e.Path == path));
                        //    Assert entry is ok
                        Assert.IsTrue(fs.GetEntry(path).Path == path);
                        // Wipe events
                        observer.events.Clear();
                    }

                    // CreateNew - 
                    //    Invalid filename "", ".", ".." -> fail
                    try { fs.Open("/tmp/", FileMode.CreateNew, FileAccess.Write, FileShare.Write); Assert.Fail(); } catch (IOException) { }
                    try { fs.Open("/tmp/.", FileMode.CreateNew, FileAccess.Write, FileShare.Write); Assert.Fail(); } catch (IOException) { }
                    try { fs.Open("/tmp/..", FileMode.CreateNew, FileAccess.Write, FileShare.Write); Assert.Fail(); } catch (IOException) { }
                    {
                        string path = "/tmp/file2.txt";
                        Assert.IsFalse(fs.Browse("/tmp").Any(e => e.Path == path));
                        Stream s1 = fs.Open(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);
                        //    File already exists -> fail
                        try { Stream s2 = fs.Open(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite); Assert.Fail(); } catch (IOException) { }
                        //    Assert event
                        Assert.IsTrue(observer.events[0] is ICreateEvent);
                        Assert.IsTrue(observer.events[0].Path == path);
                        //    Assert parent cache is flushed and children updated
                        Assert.IsTrue(fs.Browse("/tmp").Any(e => e.Path == path));
                        //    Assert entry is ok
                        Assert.IsTrue(fs.GetEntry(path).Path == path);
                        // Wipe events
                        observer.events.Clear();
                    }

                    // Create
                    //    Invalid filename "", ".", ".." -> fail
                    try { fs.Open("/tmp/", FileMode.Create, FileAccess.Write, FileShare.Write); Assert.Fail(); } catch (IOException) { }
                    try { fs.Open("/tmp/.", FileMode.Create, FileAccess.Write, FileShare.Write); Assert.Fail(); } catch (IOException) { }
                    try { fs.Open("/tmp/..", FileMode.Create, FileAccess.Write, FileShare.Write); Assert.Fail(); } catch (IOException) { }
                    //    File already exists (with open stream) -> previous file is unlinked
                    {
                        // Open handle to same file
                        string path = "/tmp/file3.txt";
                        Assert.IsFalse(fs.Browse("/tmp").Any(e => e.Path == path));
                        Stream s1 = fs.Open(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                        s1.Write(HelloWorld);
                        s1.Dispose();
                        int len = HelloWorld.Length;
                        // Replace other file
                        Stream s2 = fs.Open(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                        byte[] data = new byte[(int)s2.Length];
                        s2.Read(data);
                        Assert.AreEqual(0L, s2.Length);
                        //    Assert event
                        Assert.IsTrue(observer.events[0] is ICreateEvent);
                        Assert.IsTrue(observer.events[1] is IChangeEvent);
                        Assert.IsTrue(observer.events[0].Path == path);
                        Assert.IsTrue(observer.events[1].Path == path);
                        //    Assert parent cache is flushed and children updated
                        Assert.IsTrue(fs.Browse("/tmp").Any(e => e.Path == path));
                        //    Assert entry is ok
                        Assert.IsTrue(fs.GetEntry(path).Path == path);
                        // Wipe events
                        observer.events.Clear();
                    }
                    fs.Observe("**", observer = new Observer());
                    observer.events.Clear();
                }
            }
        }

        [TestMethod]
        public void Quota()
        {
            // Arrange
            PermutationSetup permutation = new PermutationSetup();
            permutation.Add(blockpool_3KB);

            permutation.Add(case_ms);
            permutation.Add(case_decor_ms);
            permutation.Add(case_vfs_ms);
            permutation.Add(case_vfs_2ms);


            // Act & Assert
            foreach (Scenario scenario in permutation.Scenarios)
            {
                using (Run run = scenario.Run().Initialize())
                {
                    IFileSystemDisposable fs = run.Parameters["Class"] as IFileSystemDisposable;
                    IBlockPool pool = run.Parameters["BlockPool"] as IBlockPool;
                    fs.CreateDirectory("dir/");

                    using (var s = fs.Open("dir/file", FileMode.Create, FileAccess.ReadWrite, FileShare.None))
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
                        }
                        catch (FileSystemExceptionOutOfDiskSpace)
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
                    fs.Delete("dir/", true);
                    Assert.AreEqual(0L, pool.BytesAllocated);
                    Assert.AreEqual(3072L, pool.BytesAvailable);


                    fs.CreateFile("newfile", new byte[3072]);
                    Assert.AreEqual(3072L, pool.BytesAllocated);
                    Assert.AreEqual(0L, pool.BytesAvailable);
                    Stream ss = fs.Open("newfile", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                    fs.Delete("newfile");
                    Assert.AreEqual(3072L, pool.BytesAllocated);
                    Assert.AreEqual(0L, pool.BytesAvailable);
                    ss.Dispose();
                    Assert.AreEqual(0L, pool.BytesAllocated);
                    Assert.AreEqual(3072L, pool.BytesAvailable);

                    fs.CreateFile("newfile", new byte[3072]);
                    Assert.AreEqual(3072L, pool.BytesAllocated);
                    Assert.AreEqual(0L, pool.BytesAvailable);
                    fs.Dispose();
                    Assert.AreEqual(0L, pool.BytesAllocated);
                    Assert.AreEqual(3072L, pool.BytesAvailable);
                }
            }
        }

        [TestMethod]
        public void Dispose()
        {
            // Arrange
            PermutationSetup permutation = new PermutationSetup();
            permutation.Add(blockpool_1TB);
            permutation.Add(blockpool_Pseudo);

            permutation.Add(case_ms);
            //permutation.Add(decor_fs);
            permutation.Add(case_decor_ms);
            permutation.Add(case_vfs_ms);
            permutation.Add(case_vfs_2ms);
            //permutation.Add(vfs_fsms);
            permutation.Add(init);

            // Act & Assert
            foreach (Scenario scenario in permutation.Scenarios)
            {
                using (Run run = scenario.Run().Initialize())
                {
                    IFileSystem fs = run.Parameters["Class"] as IFileSystem;
                    fs.Observe("**", observer = new Observer());
                    observer.events.Clear();

                    // Test that observer gets OnCompleted
                    {
                        Observer o = new Observer();
                        var handle = fs.Observe("**", o, null);
                        Assert.IsFalse(o.closed);
                        handle.Dispose();
                        Assert.IsTrue(o.closed);
                    }

                    // Test that observer gets OnCompleted
                    {
                        MemoryFileSystem s = new MemoryFileSystem.NonDisposable();
                        s.CreateDirectory("/tmp");
                        s.CreateFile("/tmp/helloworld.txt", HelloWorld);

                        Observer o = new Observer();
                        var handle = s.Observe("**", o, null);
                        Assert.IsFalse(o.closed);
                        Assert.IsTrue(s.Exists("/tmp/helloworld.txt"));
                        s.Dispose();
                        Assert.IsTrue(o.closed);
                        Assert.IsFalse(s.Exists("/tmp/helloworld.txt"));
                    }
                    fs.Observe("**", observer = new Observer());
                    observer.events.Clear();
                }
            }
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
