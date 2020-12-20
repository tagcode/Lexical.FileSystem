using Lexical.FileSystem;
using Lexical.FileSystem.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace docs
{
    public class MemoryFileSystem_Examples
    {
        public static void Main(string[] args)
        {
            {
                #region Snippet_1
                IFileSystem filesystem = new MemoryFileSystem();
                #endregion Snippet_1
            }
            {
                #region Snippet_1b
                IFileSystem filesystem = new MemoryFileSystem(blockSize: 4096);
                #endregion Snippet_1b
            }
            {
                IFileSystem filesystem = new MemoryFileSystem();
                #region Snippet_2
                foreach (var entry in filesystem.Browse(""))
                    Console.WriteLine(entry.Path);
                #endregion Snippet_2
            }
            {
                IFileSystem filesystem = new MemoryFileSystem();
                filesystem.CreateFile("file.txt");
                #region Snippet_3a
                using (Stream s = filesystem.Open("file.txt", FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    Console.WriteLine(s.Length);
                }
                #endregion Snippet_3a
            }
            {
                IFileSystem filesystem = new MemoryFileSystem();
                filesystem.CreateFile("file.txt");
                #region Snippet_3b
                using (Stream s = filesystem.Open("file.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    s.WriteByte(32);
                }
                #endregion Snippet_3b
            }
            {
                IFileSystem filesystem = new MemoryFileSystem();
                #region Snippet_4
                IObserver<IEvent> observer = new Observer();
                using (IDisposable handle = filesystem.Observe("**", observer))
                {
                }
                #endregion Snippet_4
            }
            {
                IFileSystem filesystem = new MemoryFileSystem();
                #region Snippet_5
                filesystem.CreateDirectory("dir/");
                #endregion Snippet_5
            }

            {
                IFileSystem filesystem = new MemoryFileSystem();
                #region Snippet_5a
                filesystem.CreateDirectory("dir1/dir2/dir3/");
                filesystem.PrintTo(Console.Out);
                #endregion Snippet_5a
            }
            {
                IFileSystem filesystem = new MemoryFileSystem();
                #region Snippet_5b
                filesystem.CreateDirectory("/tmp/dir/");
                #endregion Snippet_5b
                filesystem.PrintTo(Console.Out);
            }
            {
                IFileSystem filesystem = new MemoryFileSystem();
                #region Snippet_5c
                filesystem.CreateDirectory("/tmp/dir/");
                #endregion Snippet_5c
                filesystem.PrintTo(Console.Out);
            }
            {
                IFileSystem filesystem = new MemoryFileSystem();
                #region Snippet_5d
                filesystem.CreateDirectory("file://");
                #endregion Snippet_5d
                filesystem.PrintTo(Console.Out);
            }

            {
                IFileSystem filesystem = new MemoryFileSystem();
                filesystem.CreateDirectory("dir/");
                #region Snippet_6
                filesystem.Delete("dir/", recurse: true);
                #endregion Snippet_6
            }
            {
                IFileSystem filesystem = new MemoryFileSystem();
                filesystem.CreateDirectory("dir/");
                #region Snippet_7
                filesystem.CreateDirectory("dir/");
                filesystem.Move("dir/", "new-name/");
                #endregion Snippet_7
                filesystem.Delete("new-name/");
            }

            {
                #region Snippet_8a
                IFileSystem filesystem = new MemoryFileSystem();
                #endregion Snippet_8a
                foreach (var entry in filesystem.Browse(""))
                    Console.WriteLine(entry.Path);
            }
            {
                #region Snippet_8b
                #endregion Snippet_8b
            }
            {
                #region Snippet_10a
                // Init
                object obj = new ReaderWriterLockSlim();
                IFileSystemDisposable filesystem = new FileSystem("").AddDisposable(obj);

                // ... do work ...

                // Dispose both
                filesystem.Dispose();
                #endregion Snippet_10a
            }
            {
                #region Snippet_10b
                IFileSystemDisposable filesystem = new FileSystem("")
                    .AddDisposeAction(f => Console.WriteLine("Disposed"));
                #endregion Snippet_10b
            }
            {
                #region Snippet_10c
                MemoryFileSystem filesystem = new MemoryFileSystem();
                filesystem.CreateDirectory("/tmp/dir/");

                // Postpone dispose
                IDisposable belateDisposeHandle = filesystem.BelateDispose();
                // Start concurrent work
                Task.Run(() =>
                {
                    // Do work
                    Thread.Sleep(1000);
                    filesystem.GetEntry("");
                    // Release belate handle. Disposes here or below, depending which thread runs last.
                    belateDisposeHandle.Dispose();
                });

                // Start dispose, but postpone it until belatehandle is disposed in another thread.
                filesystem.Dispose();
                #endregion Snippet_10c
            }

            {
                // <Snippet_20a>
                IFileSystem ms = new MemoryFileSystem(blockSize: 1024, maxSpace: 1L << 34);
                // </Snippet_20a>
            }
            { 
                // <Snippet_20b>
                IFileSystem ms = new MemoryFileSystem(blockSize: 1024, maxSpace: 1L << 34);
                ms.CreateFile("file", new byte[1 << 30]);
                ms.PrintTo(Console.Out, format: PrintTree.Format.AllWithName);
                // </Snippet_20b>
            }
            {
                try
                {
                    // <Snippet_20c>
                    IFileSystem ms = new MemoryFileSystem(blockSize: 1024, maxSpace: 2048);
                    ms.CreateFile("file1", new byte[1024]);
                    ms.CreateFile("file2", new byte[1024]);

                    // throws FileSystemExceptionOutOfDiskSpace
                    ms.CreateFile("file3", new byte[1024]);
                    // </Snippet_20c>
                }
                catch (FileSystemExceptionOutOfDiskSpace) { }
            }
            {
                try
                {
                    // <Snippet_20d>
                    IBlockPool pool = new BlockPool(blockSize: 1024, maxBlockCount: 3, maxRecycleQueue: 3);
                    IFileSystem ms1 = new MemoryFileSystem(pool);
                    IFileSystem ms2 = new MemoryFileSystem(pool);

                    // Reserve 2048 from shared pool
                    ms1.CreateFile("file1", new byte[2048]);

                    // Not enough for another 3072, throws FileSystemExceptionOutOfDiskSpace
                    ms2.CreateFile("file2", new byte[2048]);
                    // </Snippet_20d>
                }
                catch (FileSystemExceptionOutOfDiskSpace) { }

            }
            {
                try
                {
                    // <Snippet_20e>
                    IBlockPool pool = new BlockPool(blockSize: 1024, maxBlockCount: 3, maxRecycleQueue: 3);
                    IFileSystem ms = new MemoryFileSystem(pool);
                    Stream s = ms.Open("file", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                    s.Write(new byte[3072], 0, 3072);
                    ms.Delete("file");

                    Console.WriteLine(pool.BytesAvailable); // Prints 0
                    s.Dispose();
                    Console.WriteLine(pool.BytesAvailable); // Prints 3072
                    // </Snippet_20e>
                }
                catch (FileSystemExceptionOutOfDiskSpace) { }

            }

        }

    }
}
