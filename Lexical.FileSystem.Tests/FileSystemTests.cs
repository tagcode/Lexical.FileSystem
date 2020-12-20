using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Lexical.FileSystem;
using Microsoft.Extensions.FileProviders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Lexical.Utils.Permutation;
using Lexical.FileSystem.Decoration;
using Lexical.FileSystem.Internal;

namespace Lexical.FileSystem.Tests
{
    [TestClass]
    public class FileSystemTests
    {
        internal protected static bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows), isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux), isOsx = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        static Case classFileSystem = new Case("Class", nameof(FileSystem), null, run => new FileSystem(run["Root"] as string));
        static Case classFileProvider = new Case("Class", nameof(FileProviderSystem), null, run => new FileProviderSystem(new PhysicalFileProvider(run["Root"] as string)).AddSourceToBeDisposed());
        static Case classFileSystemDecoration = new Case("Class", nameof(FileSystemDecoration) + "(" + nameof(FileSystem) + ")", null, run => new FileSystem(run["Root"] as string).Decorate(null).AddSourceToBeDisposed());
        static Case classFileSystemDecorationMulti = new Case("Class", nameof(FileSystemDecoration) + "(" + nameof(FileSystem) + ")", null, run => new FileSystem(run["Root"] as string).Concat(new MemoryFileSystem()).AddSourceToBeDisposed());
        static Case classFileProviderComposition = new Case("Class", nameof(FileSystemDecoration) + "(" + nameof(FileProviderSystem) + ")", null, run => new PhysicalFileProvider(run["Root"] as string).ToFileSystem().Decorate(null).AddSourceToBeDisposed());
        static Case memoryFileSystem = new Case("Class", nameof(MemoryFileSystem), null, run => new MemoryFileSystem());

        static Case vfs_fs = new Case("Class", nameof(VirtualFileSystem) + "fs", null, run => new VirtualFileSystem().Mount("", new FileSystem(run["Root"] as string)));
        static Case vfs_ms = new Case("Class", nameof(VirtualFileSystem) + "fs", null, run => new VirtualFileSystem().Mount("", new MemoryFileSystem()));
        static Case vfs_fs_ms = new Case("Class", nameof(VirtualFileSystem) + "fs", null, run => new VirtualFileSystem().Mount("", new FileSystem(run["Root"] as string), new MemoryFileSystem()));
        static Case vfs_fs_ms2 = new Case("Class", nameof(VirtualFileSystem) + "fs", null, run => new VirtualFileSystem().Mount("", new FileSystem(run["Root"] as string)).Mount("/ram", new MemoryFileSystem()));

        static Case dispatcher1 = new Case("Dispatcher", "CurrentThread", null, run => EventDispatcher.Instance);
        static Case dispatcher2 = new Case("Dispatcher", "TaskFactory", null, run => EventTaskDispatcher.Instance);

        public TestContext TestContext { get; set; }

        /// <summary>
        /// Path where test is ran.
        /// </summary>
        string Root;

        /// <summary>
        /// FileSystem of the test.
        /// </summary>
        IFileSystem filesystem;

        /// <summary>
        /// Create <see cref="Root"/>.
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            string root = AppDomain.CurrentDomain.BaseDirectory; // Directory.GetCurrentDirectory()
            Root = Path.Combine(root, TestContext.TestName);
            Directory.CreateDirectory(Root);
        }

        /// <summary>
        /// Delete <see cref="Root"/>.
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
            Directory.Delete(Root, true);
            Root = null;
        }

        /// <summary>
        /// Interface: <see cref="IFileSystemReader"/> 
        /// 
        /// Tests:
        /// <list type="number">
        ///     <item>1. Read file that exists</item>
        ///     <item>2. Read file that doesn't exist</item>
        ///     <item>3. Read file outside root "../nn"</item>
        ///     <item>4. Read directory</item>
        /// </list>
        /// 
        /// </summary>
        [TestMethod]
        public void Read()
        {
            // Arrange
            PermutationSetup permutation = new PermutationSetup();
            permutation.initialParameters["Root"] = Root;
            permutation.Add(classFileSystem);
            permutation.Add(classFileProvider);
            permutation.Add(classFileSystemDecoration);
            permutation.Add(classFileSystemDecorationMulti);
            permutation.Add(classFileProviderComposition);
            permutation.Add(memoryFileSystem);
            permutation.Add(vfs_fs);
            permutation.Add(vfs_ms);
            permutation.Add(vfs_fs_ms);
            permutation.Add(vfs_fs_ms2);

            // Act & Assert
            foreach (Scenario scenario in permutation.Scenarios)
            {
                using (Run run = scenario.Run().Initialize())
                {
                    filesystem = run.Parameters["Class"] as IFileSystem;

                    // Test 1: Read file that exists
                    {
                        string path = "dir/file.txt";
                        CreateFile(path);
                        Stream s = filesystem.Open(path, FileMode.Open, FileAccess.Read, FileShare.None);
                        byte[] data = ReadFully(s);
                        s.Dispose();
                        Assert.IsTrue(data.Length > 1);
                        DeleteFile(path);
                    }

                    // Test 2: Read file that doesn't exist
                    {
                        string path = "dir/fileXX.txt";
                        DeleteFile(path);
                        try
                        {
                            filesystem.Open(path, FileMode.Open, FileAccess.Read, FileShare.None);
                            Assert.Fail();
                        }
                        catch (FileNotFoundException)
                        {
                            // Expected
                        }
                    }

                    // Test 3: Read a file that exists outside root "../file.txt"
                    if (filesystem is MemoryFileSystem == false && filesystem is VirtualFileSystem == false)
                    {
                        string path = "../file.txt";
                        CreateFile(path);
                        try
                        {
                            filesystem.Open(path, FileMode.Open, FileAccess.Read, FileShare.None);
                            Assert.Fail();
                        }
                        catch (InvalidOperationException) { }
                        catch (FileNotFoundException) { }
                        finally
                        {
                            DeleteFile(path);
                        }
                    }

                    // Test 4: Read directory
                    {
                        string path = "dir2";
                        CreateDir(path);
                        try
                        {
                            filesystem.Open(path, FileMode.Open, FileAccess.Read, FileShare.None);
                            Assert.Fail();
                        }
                        catch (Exception e) when (e is AssertFailedException == false) { }
                        finally
                        {
                            DeleteDir(path);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Interface: <see cref="IFileSystemWriter"/> 
        /// 
        /// Tests:
        /// <list type="number">
        ///     <item>1. Create to non-existing path</item>
        ///     <item>2. Create new file</item>
        ///     <item>3. Open existing file for writing</item>
        ///     <item>4. Create invalid file (outside root)</item>
        ///     <item>5. Create invalid file (bad characters)</item>
        /// </list>
        /// 
        /// </summary>
        [TestMethod]
        public void Write()
        {
            // Arrange
            PermutationSetup permutation = new PermutationSetup();
            permutation.initialParameters["Root"] = Root;
            permutation.Add(classFileSystem);
            permutation.Add(classFileSystemDecoration);
            permutation.Add(classFileSystemDecorationMulti);
            permutation.Add(memoryFileSystem);
            permutation.Add(vfs_fs);
            permutation.Add(vfs_ms);
            permutation.Add(vfs_fs_ms);
            permutation.Add(vfs_fs_ms2);
            permutation.Add(dispatcher1);
            permutation.Add(dispatcher2);

            // Act
            // Assert
            foreach (Scenario scenario in permutation.Scenarios)
            {
                using (Run run = scenario.Run().Initialize())
                {
                    filesystem = run.Parameters["Class"] as IFileSystem;
                    var dispatcher = run.Parameters["Dispatcher"] as IEventDispatcher;

                    // Test 1: Create to non-existing path "../file2.txt"
                    if (filesystem is MemoryFileSystem == false && filesystem is VirtualFileSystem == false)
                    {
                        string path = "../file2.txt";
                        DeleteFile(path);
                        try
                        {
                            filesystem.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite).Dispose();
                            Assert.Fail();
                        }
                        catch (InvalidOperationException) { }
                    }

                    // Test 2: Create new file
                    {
                        string filepath = "dir/temp.txt";

                        // Observe 
                        Observer fileObserver = new Observer(), dirObserver = new Observer();
                        DeleteDir("dir");
                        CreateDir("dir");
                        IDisposable handle1 = filesystem.Observe(filepath, fileObserver, null, dispatcher), handle2 = filesystem.Observe("**", dirObserver, null, dispatcher);

                        // Create new file
                        filesystem.Open(filepath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite).Dispose();
                        fileObserver.Wait();
                        dirObserver.Wait();
                        handle1.Dispose(); handle2.Dispose();

                        Assert.IsNull(fileObserver.Error);
                        Assert.IsNotNull(fileObserver.Event);
                        Assert.IsTrue(fileObserver.Event is ICreateEvent ce && ce.Path == filepath && ce.Observer.FileSystem == filesystem);

                        Assert.IsNull(dirObserver.Error);
                        Assert.IsNotNull(dirObserver.Event);
                        Assert.IsTrue(dirObserver.Event is ICreateEvent ee && ee.Path == filepath && ee.Observer.FileSystem == filesystem);

                        // Cleanup
                        DeleteDir("dir");
                    }

                    // Test 3: Open existing file for writing
                    {
                        string path = "file.txt";
                        CreateFile(path);
                        Stream s = filesystem.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                        s.Position = s.Length;
                        byte[] data = UTF8Encoding.UTF8.GetBytes("Added text");
                        s.Write(data, 0, data.Length);
                        s.Flush();
                        s.Dispose();
                    }

                    // Test 4: Create invalid file (outside root)
                    {
                        string path = "../file.txt";
                        try
                        {
                            filesystem.Open(path, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None);
                            Assert.Fail();
                        }
                        catch (InvalidOperationException) { }
                        catch (IOException) { }
                        finally
                        {
                            DeleteFile(path);
                        }
                    }

                    // 5. Create invalid file (bad characters)
                    if (filesystem is MemoryFileSystem == false && filesystem is VirtualFileSystem == false)
                    {
                        string path = "::::";
                        try
                        {
                            filesystem.Open(path, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None);
                            Assert.Fail();
                        }
                        catch (InvalidOperationException) { }
                        catch (IOException) { }
                    }

                }
            }
        }

        /// <summary>
        /// Interface: <see cref="IFileSystemBrowser"/> 
        /// 
        /// Tests:
        /// <list type="number">
        ///     <item>1. List files in existing path (return file and directory)</item>
        ///     <item>2. List files in non-existing path</item>
        ///     <item>3. List files outside root</item>
        ///     <item>4. Browse a single file</item>
        /// </list>
        /// 
        /// </summary>
        [TestMethod]
        public void Browse()
        {
            // Arrange
            PermutationSetup permutation = new PermutationSetup();
            permutation.initialParameters["Root"] = Root;
            permutation.Add(classFileSystem);
            permutation.Add(classFileProvider);
            permutation.Add(classFileSystemDecoration);
            permutation.Add(classFileSystemDecorationMulti);
            permutation.Add(classFileProviderComposition);
            permutation.Add(memoryFileSystem);
            permutation.Add(vfs_fs);
            permutation.Add(vfs_ms);
            permutation.Add(vfs_fs_ms);
            permutation.Add(vfs_fs_ms2);
            permutation.Add(dispatcher1);
            permutation.Add(dispatcher2);

            // Act
            // Assert
            foreach (Scenario scenario in permutation.Scenarios)
            {
                using (Run run = scenario.Run().Initialize())
                {
                    filesystem = run.Parameters["Class"] as IFileSystem;

                    // Test 1: List files in existing path (Return file and directory)
                    {
                        string path = "dir", dirpath = "dir/dir1/", filepath = "dir/file.txt";
                        DeleteDir(path);
                        CreateDir(path);
                        CreateDir(dirpath);
                        CreateFile(filepath);
                        IDirectoryContent content = filesystem.Browse(path);
                        Assert.IsTrue(content.Count == 2);
                        Assert.IsTrue(content[0].Path == dirpath || content[0].Path == filepath);
                        Assert.IsTrue(content[1].Path == dirpath || content[1].Path == filepath);
                        DeleteDir(path);
                    }
                    // Test 2: List files in non-existing path
                    {
                        string path = "non-existing";
                        DeleteDir(path);
                        IDirectoryContent content = filesystem.Browse(path);
                        if (content.Exists) filesystem.Browse(path);
                        Assert.AreEqual(false, content.Exists);
                        Assert.AreEqual(0, content.Count);
                    }

                    // Test 3. List files outside root
                    {
                        string path = "..";
                        IDirectoryContent content = filesystem.Browse(path);
                        Assert.AreEqual(false, content.Exists);
                        Assert.AreEqual(0, content.Count);
                    }

                    // Test 4. Browse a single file
                    {
                        string path = "test.txt";
                        CreateFile(path);
                        IDirectoryContent content = filesystem.Browse(path);
                        Assert.AreEqual(false, content.Exists);
                        Assert.AreEqual(0, content.Count);
                        DeleteFile(path);
                    }
                }
            }
        }
        /// <summary>
        /// Interface: <see cref="IFileSystemBrowser"/> 
        /// 
        /// Tests:
        /// <list type="number">
        ///     <item>1. Test root</item>
        ///     <item>2. Test directory and file in existing path</item>
        ///     <item>3. Test non-existing path</item>
        ///     <item>4. Test path outside root</item>
        /// </list>
        /// 
        /// </summary>
        [TestMethod]
        public void Exists()
        {
            // Arrange
            PermutationSetup permutation = new PermutationSetup();
            permutation.initialParameters["Root"] = Root;
            permutation.Add(classFileSystem);
            permutation.Add(classFileProvider);
            permutation.Add(classFileSystemDecoration);
            permutation.Add(classFileSystemDecorationMulti);
            permutation.Add(classFileProviderComposition);
            permutation.Add(memoryFileSystem);
            permutation.Add(vfs_fs);
            permutation.Add(vfs_ms);
            permutation.Add(vfs_fs_ms);
            permutation.Add(vfs_fs_ms2);
            permutation.Add(dispatcher1);
            permutation.Add(dispatcher2);

            // Act
            // Assert
            foreach (Scenario scenario in permutation.Scenarios)
            {
                using (Run run = scenario.Run().Initialize())
                {
                    filesystem = run.Parameters["Class"] as IFileSystem;

                    // Test 1: Test root
                    {
                        Assert.IsNotNull(filesystem.GetEntry(""));
                    }
                    // Test 2: Test directory and file in existing path
                    {
                        string path = "dir", filepath = "dir/file.txt";
                        DeleteDir(path);
                        CreateDir(path);
                        CreateFile(filepath);
                        Assert.IsNotNull(filesystem.GetEntry(path));
                        Assert.IsNotNull(filesystem.GetEntry(filepath));
                        DeleteDir(path);
                    }
                    // Test 3: Test non-existing path
                    {
                        string path = "non-existing";
                        Assert.IsNull(filesystem.GetEntry(path));
                    }

                    // Test 4. Test path outside root
                    {
                        string path = "..";
                        try
                        {
                            Assert.IsNull(filesystem.GetEntry(path));
                        }
                        catch (InvalidOperationException) { }  // FileSystem throws on ".." todo, make unifying exception contract
                        catch (DirectoryNotFoundException) { } // VFS throws on ".."
                    }
                }
            }
        }

        /// <summary>
        /// Dispose tests
        /// 
        /// Tests:
        /// <list type="number">
        ///     <item>1. Delete fs, test that observers are closed</item>
        /// </list>
        /// 
        /// </summary>
        [TestMethod]
        public void DisposeTest()
        {
            // Arrange
            PermutationSetup permutation = new PermutationSetup();
            permutation.initialParameters["Root"] = Root;
            permutation.Add(classFileSystemDecoration);
            permutation.Add(classFileProvider);
            permutation.Add(classFileSystem);
            permutation.Add(classFileSystemDecorationMulti);
            permutation.Add(classFileProviderComposition);
            permutation.Add(memoryFileSystem);
            permutation.Add(vfs_fs);
            permutation.Add(vfs_ms);
            permutation.Add(vfs_fs_ms);
            permutation.Add(vfs_fs_ms2);

            permutation.Add(dispatcher1);
            permutation.Add(dispatcher2);

            // Act
            // Assert
            foreach (Scenario scenario in permutation.Scenarios)
            {
                using (Run run = scenario.Run().Initialize())
                {
                    filesystem = run.Parameters["Class"] as IFileSystem;
                    var dispatcher = run.Parameters["Dispatcher"] as IEventDispatcher;

                    // Test 1: Dispose fs
                    {
                        string dirpath = "dir2";
                        string filepath = dirpath + "/temp.txt";
                        CreateDir(dirpath);
                        CreateFile(filepath);

                        // Observe file, observe directory
                        Observer fileObserver = new Observer(), dirObserver = new Observer();
                        IDisposable handle1 = filesystem.Observe(filepath, fileObserver, null, dispatcher), handle2 = filesystem.Observe(dirpath + "/**", dirObserver, null, dispatcher);

                        // Dispose
                        if (filesystem is IDisposable disp)
                        {
                            disp.Dispose();

                            fileObserver.Wait(); dirObserver.Wait();
                            Assert.IsTrue(fileObserver.Completed);
                            Assert.IsTrue(dirObserver.Completed);
                        }
                    }

                }
            }
        }
        /// <summary>
        /// Interface: <see cref="IFileSystemDeleter"/> 
        /// 
        /// Tests:
        /// <list type="number">
        ///     <item>1. Delete non-existing file</item>
        ///     <item>2. Delete existing file</item>
        ///     <item>3. Delete non-existing directory</item>
        ///     <item>4. Delete outside root path</item>
        ///     <item>5. Delete non-empty directory with recurse</item>
        ///     <item>6. Delete non-empty directory without recurse</item>
        ///     <item>7. Delete empty directory without recurse</item>
        /// </list>
        /// 
        /// </summary>
        [TestMethod]
        public void Delete()
        {
            // Arrange
            PermutationSetup permutation = new PermutationSetup();
            permutation.initialParameters["Root"] = Root;
            permutation.Add(classFileSystem);
            permutation.Add(classFileSystemDecoration);
            permutation.Add(classFileSystemDecorationMulti);
            permutation.Add(memoryFileSystem);
            permutation.Add(vfs_fs);
            permutation.Add(vfs_ms);
            permutation.Add(vfs_fs_ms);
            permutation.Add(vfs_fs_ms2);
            permutation.Add(dispatcher1);
            permutation.Add(dispatcher2);

            // Act
            // Assert
            foreach (Scenario scenario in permutation.Scenarios)
            {
                using (Run run = scenario.Run().Initialize())
                {
                    filesystem = run.Parameters["Class"] as IFileSystem;
                    var dispatcher = run.Parameters["Dispatcher"] as IEventDispatcher;

                    // Test 1: Delete non-existing file
                    {
                        string path = "../file2.txt";
                        try
                        {
                            filesystem.Delete(path);
                            Assert.Fail();
                        }
                        catch (InvalidOperationException) { }  // FileSystem throws on ".." todo, make unifying exception contract
                        catch (DirectoryNotFoundException) { } // VFS throws on ".."
                        catch (IOException) { }
                    }

                    // Test 2: Delete existing file
                    {
                        // create 
                        string dirpath = "dir2";
                        string filepath = dirpath + "/temp.txt";
                        CreateDir(dirpath);
                        CreateFile(filepath);

                        // Observe file, observe directory
                        Observer fileObserver = new Observer(), dirObserver = new Observer();
                        IDisposable handle1 = filesystem.Observe(filepath, fileObserver, null, dispatcher), handle2 = filesystem.Observe(dirpath+"/**", dirObserver, null, dispatcher);

                        // Delete file
                        filesystem.Delete(filepath);
                        fileObserver.Wait(); dirObserver.Wait();
                        handle1.Dispose(); handle2.Dispose();

                        Assert.IsNull(fileObserver.Error);
                        Assert.IsNotNull(fileObserver.Event);
                        Assert.IsNotNull(fileObserver.Event);
                        Assert.IsTrue(fileObserver.Event is IDeleteEvent de && de.Path == filepath && de.Observer.FileSystem == filesystem);
                        if (dispatcher is EventTaskDispatcher) Assert.AreNotEqual(Thread.CurrentThread, fileObserver.thread);

                        Assert.IsNull(dirObserver.Error);
                        Assert.IsNotNull(dirObserver.Event);
                        Assert.IsTrue(dirObserver.Event is IDeleteEvent _de && _de.Path == filepath && _de.Observer.FileSystem == filesystem);
                        if (dispatcher is EventTaskDispatcher) Assert.AreNotEqual(Thread.CurrentThread, dirObserver.thread);

                        // Cleanup
                        DeleteFile(filepath);
                        DeleteDir(dirpath);
                    }

                    // Test 3: Delete non-existing directory
                    {
                        string path = "nonexisting";
                        DeleteDir(path);
                        // Delete file
                        try
                        {
                            filesystem.Delete(path, true);
                            Assert.Fail();
                        }
                        catch (FileNotFoundException)
                        {
                        }
                    }

                    // Test 4: Delete outside root
                    {
                        string path = "../dir";
                        DeleteDir(path);
                        // Delete file
                        try
                        {
                            filesystem.Delete(path, true);
                            Assert.Fail();
                        }
                        catch (InvalidOperationException) { }  // FileSystem throws on ".." todo, make unifying exception contract
                        catch (DirectoryNotFoundException) { } // VFS throws on ".."
                        catch (IOException) { }
                    }

                    // Test 5: Delete non-empty directory with recurse
                    {
                        // create 
                        string dirpath = "dir/";
                        string filepath = "dir/file";
                        CreateDir(dirpath);
                        CreateFile(filepath);

                        // Observe
                        Observer fileObserver = new Observer(), dirObserver = new Observer();
                        IDisposable handle1 = filesystem.Observe(filepath, fileObserver, null, dispatcher), handle2 = filesystem.Observe("**", dirObserver, null, dispatcher);

                        // Delete file
                        filesystem.Delete(dirpath, true);

                        Thread.Sleep(1000);
                        fileObserver.Wait(); dirObserver.Wait();
                        handle1.Dispose(); handle2.Dispose();

                        Assert.IsFalse(Directory.Exists(dirpath));
                        Assert.IsFalse(File.Exists(filepath));

                        //Assert.IsNull(fileObserver.Error); // <-- AccessDeniedException, monitored directory cannot be deleted
                        Assert.IsNotNull(fileObserver.Event);
                        Assert.IsTrue(fileObserver.Event is IDeleteEvent de && de.Path == filepath && de.Observer.FileSystem == filesystem);

                        //Assert.IsNull(dirObserver.Error); // <-- AccessDeniedException, monitored directory cannot be deleted
                        Assert.IsNotNull(dirObserver.Event);
                        Assert.IsTrue(dirObserver.Event is IDeleteEvent _de && (_de.Path == dirpath||_de.Path == filepath) && _de.Observer.FileSystem == filesystem);

                        // Cleanup
                        DeleteDir(dirpath);
                    }

                    // Test 6: Delete non-empty directory without recurse
                    {
                        // create 
                        string dirpath = "dir";
                        string filepath = "dir/file";
                        CreateDir(dirpath);
                        CreateFile(filepath);

                        // Delete file
                        try
                        {
                            filesystem.Delete(dirpath, false);
                            Assert.Fail();
                        }
                        catch (IOException) { }
                        Assert.IsTrue(PathExists(dirpath));
                        Assert.IsTrue(PathExists(filepath));

                        // Cleanup
                        DeleteDir(dirpath);
                    }

                    // Test 7: Delete empty directory without recurse
                    {
                        // create 
                        string dirpath = "dir/";
                        DeleteDir(dirpath);
                        CreateDir(dirpath);

                        // Observe
                        Observer dirObserver = new Observer();
                        IDisposable handle2 = filesystem.Observe("**", dirObserver, null, dispatcher);

                        // Delete file
                        filesystem.Delete(dirpath, false);
                        Thread.Sleep(100);

                        dirObserver.Wait();
                        handle2.Dispose();

                        Assert.IsFalse(PathExists(dirpath));

                        //Assert.IsNull(dirObserver.Error); // <-- AccessDeniedException is put here when directory is deleted with recurse. The directory goes into some kind of limbo and the Watcher cannot access it
                        Assert.IsNotNull(dirObserver.Event);
                        Assert.IsTrue(dirObserver.Event is IDeleteEvent _de && _de.Path == dirpath && _de.Observer.FileSystem == filesystem);

                        // Cleanup
                        DeleteDir(dirpath);
                    }

                }
            }
        }

        /// <summary>
        /// Interface: <see cref="IFileSystemFileAttribute"/> 
        /// 
        /// Tests:
        /// <list type="number">
        ///     <item>1. SetFileAttribute non-existing file</item>
        ///     <item>2. SetFileAttribute existing file</item>
        ///     <item>3. SetFileAttribute non-existing directory</item>
        ///     <item>4. SetFileAttribute outside root path</item>
        ///     <item>6. SetFileAttribute on directory</item>
        /// </list>
        /// 
        /// </summary>
        [TestMethod]
        public void SetFileAttributes()
        {
            // Arrange
            PermutationSetup permutation = new PermutationSetup();
            permutation.initialParameters["Root"] = Root;
            permutation.Add(classFileSystem);
            permutation.Add(classFileSystemDecoration);
            permutation.Add(classFileSystemDecorationMulti);
            permutation.Add(vfs_fs);
            permutation.Add(vfs_fs_ms);
            permutation.Add(vfs_fs_ms2);

            // Act
            // Assert
            foreach (Scenario scenario in permutation.Scenarios)
            {
                using (Run run = scenario.Run().Initialize())
                {
                    filesystem = run.Parameters["Class"] as IFileSystem;

                    // Test 1: SetFileAttributes non-existing file
                    {
                        string path = "../file2.txt";
                        try
                        {
                            filesystem.SetFileAttribute(path, FileAttributes.Normal);
                            Assert.Fail();
                        }
                        catch (InvalidOperationException) { }  // FileSystem throws on ".." todo, make unifying exception contract
                        catch (DirectoryNotFoundException) { } // VFS throws on ".."
                        catch (IOException) { }
                    }

                    // Test 2: SetFileAttributes existing file
                    {
                        // create 
                        string dirpath = "dir2";
                        string filepath = dirpath + "/temp.txt";
                        CreateDir(dirpath);
                        CreateFile(filepath);

                        // Delete file
                        filesystem.SetFileAttribute(filepath, FileAttributes.Hidden);

                        // Assert
                        Assert.AreEqual(FileAttributes.Hidden, filesystem.GetEntry(filepath).FileAttributes());

                        // Cleanup
                        DeleteFile(filepath);
                        DeleteDir(dirpath);
                    }

                    // Test 3: SetFileAttributes non-existing directory
                    {
                        string path = "nonexisting";
                        DeleteDir(path);
                        // Delete file
                        try
                        {
                            filesystem.SetFileAttribute(path, FileAttributes.Hidden|FileAttributes.Directory);
                            Assert.Fail();
                        }
                        catch (FileNotFoundException)
                        {
                        }
                    }

                    // Test 4: SetFileAttributes outside root
                    {
                        string path = "../dir";
                        DeleteDir(path);
                        // Delete file
                        try
                        {
                            filesystem.SetFileAttribute(path, FileAttributes.Hidden|FileAttributes.Directory);
                            Assert.Fail();
                        }
                        catch (InvalidOperationException) { }  // FileSystem throws on ".." todo, make unifying exception contract
                        catch (DirectoryNotFoundException) { } // VFS throws on ".."
                        catch (IOException) { }
                    }


                    // Test 5: SetAttribute on directory
                    {
                        // create 
                        string dirpath = "dir/";
                        DeleteDir(dirpath);
                        CreateDir(dirpath);

                        // Delete file
                        filesystem.SetFileAttribute(dirpath, FileAttributes.Hidden|FileAttributes.Directory);
                        Thread.Sleep(100);

                        // Assert
                        Assert.AreEqual( FileAttributes.Hidden|FileAttributes.Directory, filesystem.GetEntry(dirpath).FileAttributes() );

                        // Cleanup
                        DeleteDir(dirpath);
                    }

                }
            }
        }

        /// <summary>
        /// Interface: <see cref="IFileSystemMover"/> 
        /// 
        /// Tests:
        /// <list type="number">
        ///     <item>1. Move&rename existing file</item>
        ///     <item>2. Move&rename existing directory</item>
        ///     <item>3. Move&rename non-existing file</item>
        ///     <item>4. Move&rename non-existing directory</item>
        ///     <item>5. Move&rename existing file over to existing path (to fail)</item>
        ///     <item>6. Move&rename existing directory over to existing path (to fail)</item>
        ///     <item>7. Move file from outside root</item>
        ///     <item>8. Move dir from outside root</item>
        ///     <item>9. Move file to outside root</item>
        ///     <item>10. Move dir to outside root</item>
        /// </list>
        /// 
        /// </summary>
        [TestMethod]
        public void Move()
        {
            // Arrange
            PermutationSetup permutation = new PermutationSetup();
            permutation.initialParameters["Root"] = Root;
            permutation.Add(classFileSystem);
            permutation.Add(classFileSystemDecoration);
            permutation.Add(classFileSystemDecorationMulti);
            permutation.Add(memoryFileSystem);
            permutation.Add(vfs_fs);
            permutation.Add(vfs_ms);
            permutation.Add(vfs_fs_ms);
            permutation.Add(vfs_fs_ms2);
            permutation.Add(dispatcher1);
            permutation.Add(dispatcher2);

            foreach (Scenario scenario in permutation.Scenarios)
            {
                using (Run run = scenario.Run().Initialize())
                {
                    filesystem = run.Parameters["Class"] as IFileSystem;
                    var dispatcher = run.Parameters["Dispatcher"] as IEventDispatcher;

                    // Test: 1. Move&rename existing file
                    {
                        // Arrange
                        string srcPath = "file-to-move.txt", dstPath = "file-moved.txt";
                        CreateFile(srcPath);
                        DeleteFile(dstPath);

                        // Observe
                        Observer dirObserver = new Observer(), fileObserver = new Observer();
                        IDisposable handle1 = filesystem.Observe(srcPath, fileObserver, null, dispatcher);
                        IDisposable handle2 = filesystem.Observe("**", dirObserver, null, dispatcher);

                        // Act
                        filesystem.Move( srcPath, dstPath);
                        Thread.Sleep(300);
                        handle1.Dispose();
                        handle2.Dispose();

                        // Assert
                        Assert.IsNotNull(dirObserver.Event);
                        if (isWindows)
                        {
                            Assert.IsTrue(fileObserver.Event is IRenameEvent re && re.OldPath == srcPath && re.Observer.FileSystem == filesystem && re.NewPath == dstPath);
                            Assert.IsTrue(dirObserver.Event is IRenameEvent _re && _re.OldPath == srcPath && _re.Observer.FileSystem == filesystem && _re.NewPath == dstPath);
                        }
                        else
                        {
                            // Linux sends Deleted and Created events but no Renamed.
                        }

                        // Cleanup
                        DeleteFile(dstPath);
                    }

                    // Test: 2. Move&rename existing directory
                    {
                        // Arrange
                        string srcPath = "dir-to-move", dstPath = "dir-moved";
                        CreateDir(srcPath);
                        DeleteDir(dstPath);

                        // Observe
                        Observer dirObserver = new Observer();
                        IDisposable handle1 = filesystem.Observe("**", dirObserver, null, dispatcher);

                        // Act
                        filesystem.Move( srcPath, dstPath);
                        Thread.Sleep(300);
                        handle1.Dispose();

                        // Assert
                        Assert.IsNotNull(dirObserver.Event);
                        Assert.IsTrue(dirObserver.Event is IRenameEvent _re && _re.OldPath == srcPath+"/" && _re.Observer.FileSystem == filesystem && _re.NewPath == dstPath+"/");

                        // Cleanup
                        DeleteDir(dstPath);
                    }

                    // Test: 3. Move&rename non-existing file
                    {
                        // Arrange
                        string srcPath = "file-to-move.txt", dstPath = "file-moved.txt";
                        DeleteFile(srcPath);
                        DeleteFile(dstPath);

                        // Act
                        try
                        {
                            filesystem.Move( srcPath, dstPath);
                            Assert.Fail();
                        }
                        catch (IOException)
                        {
                        }
                    }

                    // Test: 4. Move&rename non-existing directory
                    {
                        // Arrange
                        string srcPath = "dir-to-move", dstPath = "dir-moved";
                        DeleteDir(srcPath);
                        DeleteDir(dstPath);

                        // Act
                        try
                        {
                            filesystem.Move( srcPath, dstPath);
                            Assert.Fail();
                        }
                        catch (IOException)
                        {
                        }
                    }

                    // Test: 5. Move&rename existing file over to existing path (to fail)
                    {
                        // Arrange
                        string srcPath = "file-to-move.txt", dstPath = "already-existing-file.txt";
                        CreateFile(srcPath);
                        CreateFile(dstPath);

                        // Act
                        try
                        {
                            filesystem.Move( srcPath, dstPath);
                            Assert.Fail();
                        }
                        catch (IOException)
                        {
                        }

                        // Assert
                        Assert.IsTrue(PathExists(srcPath));
                        Assert.IsTrue(PathExists(dstPath));

                        // Cleanup
                        DeleteFile(srcPath);
                        DeleteFile(dstPath);
                    }

                    // Test: 6. Move&rename existing directory over to existing path (to fail)
                    {
                        // Arrange
                        string srcPath = "dir-to-move", dstPath = "already-existing-dir";
                        CreateDir(srcPath);
                        CreateDir(dstPath);

                        // Act
                        try
                        {
                            filesystem.Move( srcPath, dstPath);
                            Assert.Fail();
                        }
                        catch (IOException)
                        {
                        }

                        // Assert
                        Assert.IsTrue(PathExists(srcPath));
                        Assert.IsTrue(PathExists(dstPath));

                        // Cleanup
                        DeleteDir(srcPath);
                        DeleteDir(dstPath);
                    }

                    /// Test: 7. Move file from outside root
                    if (filesystem is MemoryFileSystem == false && filesystem is VirtualFileSystem == false)
                    {
                        // Arrange
                        string srcPath = "../file.txt", dstPath = "file.txt";
                        CreateFile(srcPath);
                        DeleteFile(dstPath);

                        // Act
                        try
                        {
                            filesystem.Move( srcPath, dstPath);
                            Assert.Fail();
                        }
                        catch (InvalidOperationException) { }  // FileSystem throws on ".." todo, make unifying exception contract
                        catch (DirectoryNotFoundException) { } // VFS throws on ".."
                        catch (FileNotFoundException) { }

                        // Assert
                        Assert.IsTrue(PathExists(srcPath));
                        Assert.IsFalse(PathExists(dstPath));

                        // Cleanup
                        DeleteFile(srcPath);
                    }

                    /// Test: 8. Move dir from outside root
                    if (filesystem is MemoryFileSystem == false && filesystem is VirtualFileSystem == false)
                    {
                        // Arrange
                        string srcPath = "../dir-to-move", dstPath = "dst-dir";
                        CreateDir(srcPath);
                        DeleteDir(dstPath);

                        // Act
                        try
                        {
                            filesystem.Move( srcPath, dstPath);
                            Assert.Fail();
                        }
                        catch (FileNotFoundException) { }
                        catch (InvalidOperationException) { }  // FileSystem throws on ".." todo, make unifying exception contract
                        catch (DirectoryNotFoundException) { } // VFS throws on ".."

                        // Assert
                        Assert.IsTrue(PathExists(srcPath));
                        Assert.IsFalse(PathExists(dstPath));

                        // Cleanup
                        DeleteDir(srcPath);
                    }

                    /// Test: 9. Move file to outside root
                    {
                        // Arrange
                        string srcPath = "file.txt", dstPath = "../file.txt";
                        CreateFile(srcPath);
                        DeleteFile(dstPath);

                        // Act
                        try
                        {
                            filesystem.Move( srcPath, dstPath);
                            Assert.Fail();
                        }
                        catch (InvalidOperationException) { }
                        catch (IOException) { }

                        // Assert
                        Assert.IsTrue(PathExists(srcPath));
                        Assert.IsFalse(PathExists(dstPath));

                        // Cleanup
                        DeleteFile(srcPath);
                    }

                    /// Test: 10. Move dir to outside root
                    if (filesystem is MemoryFileSystem == false)
                    {
                        // Arrange
                        string srcPath = "src-dir", dstPath = "../dst-dir";
                        CreateDir(srcPath);
                        DeleteDir(dstPath);

                        // Act
                        try
                        {
                            filesystem.Move( srcPath, dstPath);
                            Assert.Fail();
                        }
                        catch (InvalidOperationException) { }  // FileSystem throws on ".." todo, make unifying exception contract
                        catch (DirectoryNotFoundException) { } // VFS throws on ".."

                        // Assert
                        Assert.IsTrue(PathExists(srcPath));
                        Assert.IsFalse(PathExists(dstPath));

                        // Cleanup
                        DeleteDir(srcPath);
                    }

                }
            }

        }

        /// <summary>
        /// Read bytes from <paramref name="s"/>.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static byte[] ReadFully(Stream s)
        {
            if (s == null) return null;

            // Get length
            long length;
            try
            {
                length = s.Length;
            }
            catch (NotSupportedException)
            {
                // Cannot get length
                MemoryStream ms = new MemoryStream();
                s.CopyTo(ms);
                return ms.ToArray();
            }

            if (length > int.MaxValue) throw new IOException("File size over 2GB");

            int _len = (int)length;
            byte[] data = new byte[_len];

            // Read chunks
            int ix = 0;
            while (ix < _len)
            {
                int count = s.Read(data, ix, _len - ix);

                // "returns zero (0) if the end of the stream has been reached."
                if (count == 0) break;

                ix += count;
            }
            if (ix == _len) return data;
            throw new IOException("Failed to read stream fully");
        }

        /// <summary>
        /// Create file with some content.
        /// Created directory if it doesn't exist.
        /// For example "dir1/dir2/somefile.txt".
        /// </summary>
        /// <param name="path">path with '/' as separator</param>
        public void CreateFile(string path)
        {
            string content = "This is a test file for IFileSystem.";

            if (filesystem is FileSystem || filesystem is FileProviderSystem || 
                (filesystem is FileSystemDecoration comp && comp.Any(fs => fs is FileSystem || fs is FileProviderSystem)) ||
                (filesystem is VirtualFileSystem vfs_ && vfs_.ListMountPoints().Any(mp => mp.FileSystem is FileSystem || mp.FileSystem is FileProviderSystem))
                )
            {
                string fullPath = Path.Combine(Root, path.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(fullPath)) return;
                string dirPath = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);
                File.WriteAllText(fullPath, content);
            }
            else
            // Special case for MemoryFileSystem and EmbeddedFileSystem
            // Not really a good test, but asserts somethings still
            {
                string parent = Path.GetDirectoryName(path);
                if (parent != "" && !filesystem.Exists(parent)) filesystem.CreateDirectory(parent);
                using (Stream s = filesystem.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    byte[] data = Encoding.UTF8.GetBytes(content);
                    s.Write(data, 0, data.Length);
                }
            }
        }

        /// <summary>
        /// Delete file from test directory.
        /// </summary>
        /// <param name="path"></param>
        public void DeleteFile(string path)
        {
            if (filesystem is FileSystem || filesystem is FileProviderSystem || 
                (filesystem is FileSystemDecoration comp && comp.Any(fs => fs is FileSystem || fs is FileProviderSystem)) ||
                (filesystem is VirtualFileSystem vfs_ && vfs_.ListMountPoints().Any(mp => mp.FileSystem is FileSystem || mp.FileSystem is FileProviderSystem))
                )
            {
                string fullPath = Path.Combine(Root, path.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(fullPath)) File.Delete(fullPath);
            } else
            {
                if (filesystem.Exists(path)) filesystem.Delete(path, true);
            }
        }

        /// <summary>
        /// Create dir in test dir. The path separator is '/'. 
        /// For example "dir1/dir2".
        /// </summary>
        /// <param name="path"></param>
        public void CreateDir(string path)
        {
            if (filesystem is FileSystem || filesystem is FileProviderSystem || 
                (filesystem is FileSystemDecoration comp && comp.Any(fs=>fs is FileSystem || fs is FileProviderSystem)) ||
                (filesystem is VirtualFileSystem vfs_ && vfs_.ListMountPoints().Any(mp => mp.FileSystem is FileSystem || mp.FileSystem is FileProviderSystem))
                )
            {
                string fullPath = Path.Combine(Root, path.Replace('/', Path.DirectorySeparatorChar));
                if (!Directory.Exists(fullPath)) Directory.CreateDirectory(fullPath);
            } else
            {
                filesystem.CreateDirectory(path);
            }
        }

        public bool PathExists(string path)
        {
            if (filesystem is FileSystem || filesystem is FileProviderSystem || 
                (filesystem is FileSystemDecoration comp && comp.Any(fs => fs is FileSystem || fs is FileProviderSystem)) ||
                (filesystem is VirtualFileSystem vfs_ && vfs_.ListMountPoints().Any(mp => mp.FileSystem is FileSystem || mp.FileSystem is FileProviderSystem))
                )
            {
                string fullPath = Path.Combine(Root, path.Replace('/', Path.DirectorySeparatorChar));
                return Directory.Exists(fullPath) || File.Exists(fullPath);
            }
            else
            {
                return filesystem.Exists(path);
            }
        }

        public void DeleteDir(string path)
        {
            if (filesystem is FileSystem || filesystem is FileProviderSystem || 
                (filesystem is FileSystemDecoration comp && comp.Any(fs => fs is FileSystem || fs is FileProviderSystem)) ||
                (filesystem is VirtualFileSystem vfs_ && vfs_.ListMountPoints().Any(mp => mp.FileSystem is FileSystem || mp.FileSystem is FileProviderSystem))
                )
            {
                string fullPath = Path.Combine(Root, path.Replace('/', Path.DirectorySeparatorChar));
                if (Directory.Exists(fullPath)) Directory.Delete(fullPath, true);
            } else
            {
                if (filesystem.Exists(path)) filesystem.Delete(path, true);
            }
        }

        class Observer : IObserver<IEvent>
        {
            public ArrayList<IEvent> events = new ArrayList<IEvent>();
            public Semaphore semaphore = new Semaphore(0, int.MaxValue);
            public IEvent Event => events.Count == 0 ? null : events[0/*events.Count - 1*/];
            public bool Completed = false;
            public Exception Error;
            public Thread thread;
            public void OnCompleted()
            {
                Completed = true;
                semaphore.Release(10);
            }
            public void OnError(Exception error)
            {
                Error = error;
                //semaphore.Release(10);
            }
            public void OnNext(IEvent value)
            {
                if (value is IStartEvent) return;
                // Capture event
                events.Add(value);
                semaphore.Release(10);
                this.thread = Thread.CurrentThread;
            }

            public void Wait()
            {
                Assert.IsTrue(semaphore.WaitOne(10000));
            }
        }

    }
}
