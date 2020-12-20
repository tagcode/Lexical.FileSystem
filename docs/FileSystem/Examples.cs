using Lexical.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace docs
{
    public class FileSystem_Examples
    {
        public static void Main(string[] args)
        {
            {
                #region Snippet_1a
                IFileSystem fs = new FileSystem(@"C:\Temp\");
                #endregion Snippet_1a
            }
            {
                #region Snippet_1b
                IFileSystem fs = new FileSystem("");
                #endregion Snippet_1b
            }

            {
                IFileSystem fs = new FileSystem("");
                #region Snippet_2a
                IDirectoryContent contents = fs.Browse("C:/Windows/");
                #endregion Snippet_2a

                #region Snippet_2d
                foreach (IEntry entry in fs.Browse("C:/Windows/"))
                    Console.WriteLine(entry.Path);
                #endregion Snippet_2d

                #region Snippet_2e
                foreach (var entry in fs.Browse("C:/Windows/").AssertExists())
                    Console.WriteLine(entry.Path);
                #endregion Snippet_2e

                {
                    #region Snippet_2f
                    IEntry e = FileSystem.OS.GetEntry("C:/Windows/win.ini");
                    Console.WriteLine(e.Path);
                    #endregion Snippet_2f
                }
                {
                    #region Snippet_2g
                    IEntry e = FileSystem.OS.GetEntry("C:/Windows/win.ini").AssertExists();
                    #endregion Snippet_2g
                }

                #region Snippet_3a
                using (Stream s = fs.Open("file.txt", FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    Console.WriteLine(s.Length);
                }
                #endregion Snippet_3a

                #region Snippet_3b
                using (Stream s = fs.Open("somefile.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    s.WriteByte(32);
                }
                #endregion Snippet_3b

                #region Snippet_5
                fs.CreateDirectory("dir/");
                #endregion Snippet_5

                #region Snippet_6
                fs.Delete("dir/", recurse: true);
                #endregion Snippet_6

                #region Snippet_7
                fs.CreateDirectory("dir/");
                fs.Move("dir/", "new-name/");
                #endregion Snippet_7
                fs.Delete("new-name/");

            }
            {
                IFileSystem fs = FileSystem.Temp;
                if (fs.Exists("myfile"))
                {
                    fs.SetFileAttribute("myfile", FileAttributes.Normal);
                    fs.Delete("myfile");
                }

                fs.CreateFile("myfile", new byte[] { 1 });
                #region Snippet_7f
                fs.SetFileAttribute("myfile", FileAttributes.ReadOnly);
                #endregion Snippet_7f
                fs.SetFileAttribute("myfile", FileAttributes.Normal);
                fs.Delete("myfile");

            }

            {
                #region Snippet_7x1
                FileSystem.OS.GetEntry("C:/Windows/win.ini");
                #endregion Snippet_7x1
            }


            {
                #region Snippet_8a
                IFileSystem fs = FileSystem.OS;
                #endregion Snippet_8a
            }

            {
                #region Snippet_8b
                foreach (var line in FileSystem.OS.VisitTree(depth: 2))
                    Console.WriteLine(line);
                #endregion Snippet_8b
            }
            {
                #region Snippet_8b2
                FileSystem.OS.PrintTo(Console.Out, depth: 2, format: PrintTree.Format.DefaultPath);
                #endregion Snippet_8b2
            }
            {
                #region Snippet_8c
                FileSystem.OS.PrintTo(Console.Out, depth: 3, format: PrintTree.Format.DefaultPath);
                #endregion Snippet_8c
            }

            {
                #region Snippet_8d
                FileSystem.Application.PrintTo(Console.Out);
                #endregion Snippet_8d
            }

            {
                #region Snippet_8e
                FileSystem.Temp.PrintTo(Console.Out, depth: 1);
                #endregion Snippet_8e
            }

            {
                // <Snippet_8f>
                foreach(var line in FileSystem.Temp.VisitTree(depth:2))
                    Console.WriteLine(line.Entry.PhysicalPath());
                // </Snippet_8f>
            }
            {
                // <Snippet_8g>
                FileSystem.Temp.PrintTo(
                    output: Console.Out, 
                    depth: 2, 
                    format: PrintTree.Format.Default | PrintTree.Format.PhysicalPath);
                // </Snippet_8g>
            }

            {
                // <Snippet_9a>
                IObserver<IEvent> observer = new Observer();
                IFileSystemObserver handle = FileSystem.OS.Observe("C:/**", observer);
                // </Snippet_9a>
            }
            {
                // <Snippet_9b>
                using (var handle = FileSystem.Temp.Observe("*.dat", new PrintObserver()))
                {
                    FileSystem.Temp.CreateFile("file.dat", new byte[] { 32, 32, 32, 32 });
                    FileSystem.Temp.Delete("file.dat");

                    Thread.Sleep(1000);
                }
                // </Snippet_9b>
            }

            {
                // <Snippet_9c>
                IObserver<IEvent> observer = new Observer();
                FileSystem.OS.Observe("C:/**", observer, eventDispatcher: EventDispatcher.Instance);
                // </Snippet_9c>
            }

            {
                // <Snippet_9d>
                IObserver<IEvent> observer = new Observer();
                FileSystem.OS.Observe("C:/**", observer, eventDispatcher: EventTaskDispatcher.Instance);
                // </Snippet_9d>
            }

            {
                #region Snippet_10a
                // Init
                object obj = new ReaderWriterLockSlim();
                IFileSystemDisposable fs = new FileSystem("").AddDisposable(obj);

                // ... do work ...

                // Dispose both
                fs.Dispose();
                #endregion Snippet_10a
            }
            {
                #region Snippet_10b
                IFileSystemDisposable fs = new FileSystem("")
                    .AddDisposeAction(f => Console.WriteLine("Disposed"));
                #endregion Snippet_10b
            }
            {
                #region Snippet_10c
                FileSystem fs = new FileSystem("");
                fs.Browse("");

                // Postpone dispose
                IDisposable belateDisposeHandle = fs.BelateDispose();
                // Start concurrent work
                Task.Run(() =>
                {
                    // Do work
                    Thread.Sleep(1000);
                    fs.GetEntry("");
                    // Release belate handle. Disposes here or below, depending which thread runs last.
                    belateDisposeHandle.Dispose();
                });

                // Start dispose, but postpone it until belatehandle is disposed in another thread.
                fs.Dispose();
                #endregion Snippet_10c
            }

        }

    }
}
