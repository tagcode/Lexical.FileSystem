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
    public class VirtualFileSystem_Examples
    {
        public static void Main(string[] args)
        {
            {
                #region Snippet_1
                IFileSystem vfs = new VirtualFileSystem();
                #endregion Snippet_1
            }

            {
                #region Snippet_2a
                IFileSystem vfs = new VirtualFileSystem()
                    .Mount("", FileSystem.OS);
                #endregion Snippet_2a
            }
            {
                #region Snippet_2b
                IFileSystem urls = new VirtualFileSystem()
                    .Mount("tmp/", FileSystem.Temp)
                    .Mount("ram/", MemoryFileSystem.Instance);
                #endregion Snippet_2b
            }
            {
                #region Snippet_2c
                IFileSystem vfs = new VirtualFileSystem()
                    .Mount("", FileSystem.OS)
                    .Mount("/tmp/", new MemoryFileSystem());

                vfs.Browse("/tmp/");
                #endregion Snippet_2c
            }
            {
                #region Snippet_3a
                IFileSystem vfs = new VirtualFileSystem();
                vfs.Mount("/tmp/", FileSystem.Temp);
                vfs.Unmount("/tmp/");
                #endregion Snippet_3a
            }
            {
                #region Snippet_3b
                IFileSystem vfs = new VirtualFileSystem();
                vfs.Mount("/tmp/", FileSystem.Temp);
                vfs.Mount("/tmp/", new MemoryFileSystem());
                #endregion Snippet_3b
            }

            {
                #region Snippet_4a
                IFileSystem vfs = new VirtualFileSystem();
                vfs.Mount("/app/", FileSystem.Application, Option.ReadOnly);
                #endregion Snippet_4a
            }
            {
                #region Snippet_4b
                IFileSystem vfs = new VirtualFileSystem();
                IFileSystem overrides = new MemoryFileSystem();
                overrides.CreateFile("important.dat", new byte[] { 12, 23, 45, 67, 89 });
                vfs.Mount("/app/", overrides, FileSystem.Application);
                #endregion Snippet_4b
            }
            {
                #region Snippet_4c
                IFileSystem overrides = new MemoryFileSystem();
                IFileSystem vfs = new VirtualFileSystem();

                vfs.Mount("/app/", 
                    (overrides, Option.ReadOnly), 
                    (FileSystem.Application, Option.ReadOnly)
                );
                #endregion Snippet_4c
            }
            {
                #region Snippet_4d
                IFileSystem vfs = new VirtualFileSystem();
                string appDir = AppDomain.CurrentDomain.BaseDirectory.Replace('\\', '/');
                vfs.Mount("/app/", FileSystem.OS, Option.SubPath(appDir));
                #endregion Snippet_4d
            }
            {
                #region Snippet_5a
                IFileSystem vfs = new VirtualFileSystem();
                vfs.Mount("/tmp/", filesystem: null);
                #endregion Snippet_5a
                vfs.PrintTo(Console.Out);
            }

            // Observing
            {
                #region Snippet_6a
                IFileSystem vfs = new VirtualFileSystem();
                vfs.Observe("**", new PrintObserver());

                IFileSystem ram = new MemoryFileSystem();
                ram.CreateDirectory("/dir/");
                ram.CreateFile("/dir/file.txt", new byte[] { 32, 65, 66 });

                vfs.Mount("", ram);
                #endregion Snippet_6a
            }

            {
                #region Snippet_6b
                IFileSystem vfs = new VirtualFileSystem();
                vfs.Observe("/dir/*.txt", new PrintObserver());

                IFileSystem ram = new MemoryFileSystem();
                ram.CreateDirectory("/dir/");
                ram.CreateFile("/dir/file.txt", new byte[] { 32, 65, 66 });
                ram.CreateFile("/dir/file.dat", new byte[] { 255, 255, 255 });

                vfs.Mount("", ram);
                #endregion Snippet_6b

                #region Snippet_6c
                vfs.Unmount("");
                #endregion Snippet_6c
            }

            {
                IFileSystem vfs = new VirtualFileSystem();
                vfs.Observe("**", new PrintObserver());

                IFileSystem ram = new MemoryFileSystem();
                ram.CreateDirectory("/dir/");
                ram.CreateFile("/dir/file.txt", new byte[] { 32, 65, 66 });
                #region Snippet_6d
                vfs.Mount("", ram, Option.NoObserve);
                #endregion Snippet_6d
            }

            {
                VirtualFileSystem vfs = new VirtualFileSystem();
                #region Snippet_6e
                IDisposable observerHandle = vfs.Observe("**", new PrintObserver());
                observerHandle.Dispose();
                #endregion Snippet_6e
                vfs.Dispose();
            }

            {
                #region Snippet_6f
                VirtualFileSystem vfs = new VirtualFileSystem();
                IDisposable observerHandle = vfs.Observe("**", new PrintObserver());
                vfs.Dispose();
                #endregion Snippet_6f
            }

            {
                #region Snippet_10a
                // Init
                object obj = new ReaderWriterLockSlim();
                IFileSystemDisposable vfs = new VirtualFileSystem().AddDisposable(obj);

                // ... do work ...

                // Dispose both
                vfs.Dispose();
                #endregion Snippet_10a
            }
            {
                #region Snippet_10b
                IFileSystemDisposable vfs = new VirtualFileSystem()
                    .AddDisposeAction(f => Console.WriteLine("Disposed"));
                #endregion Snippet_10b
            }
            {
                #region Snippet_10c
                VirtualFileSystem vfs = new VirtualFileSystem().Mount("", FileSystem.OS);
                vfs.Browse("");

                // Postpone dispose
                IDisposable belateDisposeHandle = vfs.BelateDispose();
                // Start concurrent work
                Task.Run(() =>
                {
                    // Do work
                    Thread.Sleep(1000);
                    vfs.GetEntry("");
                    // Release belate handle. Disposes here or below, depending which thread runs last.
                    belateDisposeHandle.Dispose();
                });

                // Start dispose, but postpone it until belatehandle is disposed in another thread.
                vfs.Dispose();
                #endregion Snippet_10c
            }

            {
                #region Snippet_10d
                IFileSystemDisposable vfs =
                    new VirtualFileSystem()
                    .Mount("", new FileSystem(""))
                    .Mount("/tmp/", new MemoryFileSystem())
                    .AddMountsToBeDisposed();
                #endregion Snippet_10d
                vfs.Dispose();
            }


            {
                #region Snippet_12a
                #endregion Snippet_12a

                #region Snippet_12b
                VirtualFileSystem.Url.PrintTo(Console.Out, "config://", 2, PrintTree.Format.DefaultPath);
                VirtualFileSystem.Url.PrintTo(Console.Out, "data://", 1, PrintTree.Format.DefaultPath);
                VirtualFileSystem.Url.PrintTo(Console.Out, "program-data://", 1, PrintTree.Format.DefaultPath);
                VirtualFileSystem.Url.PrintTo(Console.Out, "home://", 1, PrintTree.Format.DefaultPath);
                VirtualFileSystem.Url.PrintTo(Console.Out, "https://github.com/tagcode/Lexical.FileSystem/tree/master/");
                #endregion Snippet_12b

                #region Snippet_12c
                string config = "[Config]\nUser=ExampleUser\n";
                VirtualFileSystem.Url.CreateDirectory("config://ApplicationName/");
                VirtualFileSystem.Url.CreateFile("config://ApplicationName/config.ini", UTF8Encoding.UTF8.GetBytes(config));
                #endregion Snippet_12c

                #region Snippet_12d
                byte[] cacheData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
                VirtualFileSystem.Url.CreateDirectory("data://ApplicationName/");
                VirtualFileSystem.Url.CreateFile("data://ApplicationName/cache.db", cacheData);
                #endregion Snippet_12d

                #region Snippet_12e
                string saveGame = "[Save]\nLocation=12.32N 43.43W\n";
                VirtualFileSystem.Url.CreateDirectory("document://ApplicationName/");
                VirtualFileSystem.Url.CreateFile("document://ApplicationName/save1.txt", UTF8Encoding.UTF8.GetBytes(saveGame));
                #endregion Snippet_12e

                #region Snippet_12f
                byte[] programData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
                VirtualFileSystem.Url.CreateDirectory("program-data://ApplicationName/");
                VirtualFileSystem.Url.CreateFile("program-data://ApplicationName/index.db", programData);
                #endregion Snippet_12f

                #region Snippet_12g
                VirtualFileSystem.Url.PrintTo(Console.Out, "application://", format: PrintTree.Format.DefaultPath);
                #endregion Snippet_12g
            }
        }
    }

    // <PrintObserver>
    class PrintObserver : IObserver<IEvent>
    {
        public void OnCompleted() => Console.WriteLine("OnCompleted");
        public void OnError(Exception error) => Console.WriteLine(error);
        public void OnNext(IEvent @event) => Console.WriteLine(@event);
    }
    // </PrintObserver>

}
