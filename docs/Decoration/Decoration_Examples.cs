using Lexical.FileSystem;
using Lexical.FileSystem.Decoration;
using Microsoft.Extensions.FileProviders;
using System;
using System.IO;

namespace docs
{
    public class Decoration_Examples
    {
        public static void Main(string[] args)
        {
            {
                #region Snippet_1a
                IFileSystem ram = new MemoryFileSystem();
                IFileSystem rom = ram.Decorate(Option.ReadOnly);
                #endregion Snippet_1a
            }
            {
                IFileSystem ram = new MemoryFileSystem();
                #region Snippet_1b
                IFileSystem rom = ram.AsReadOnly();
                #endregion Snippet_1b
            }
            {
                IFileSystem ram = new MemoryFileSystem();
                #region Snippet_2a
                IFileSystem invisible = ram.Decorate(Option.NoOpen);
                #endregion Snippet_2a
            }
            {
                IFileSystem ram = new MemoryFileSystem();
                #region Snippet_2b
                IFileSystem invisible = ram.Decorate(Option.NoBrowse);
                #endregion Snippet_2b
            }
            {
                #region Snippet_3
                IFileSystem ram = new MemoryFileSystem();
                ram.CreateDirectory("tmp/dir/");
                ram.CreateFile("tmp/dir/file.txt", new byte[] { 32,32,32,32,32,32,32,32,32 });

                IFileSystem tmp = ram.Decorate(Option.SubPath("tmp/"));
                tmp.PrintTo(Console.Out, format: PrintTree.Format.DefaultPath);
                #endregion Snippet_3
            }

            {
                #region Snippet_4a
                MemoryFileSystem ram = new MemoryFileSystem();
                IFileSystemDisposable rom = ram.Decorate(Option.ReadOnly).AddSourceToBeDisposed();
                // Do work ...
                rom.Dispose();
                #endregion Snippet_4a
            }

            {
                #region Snippet_4b
                MemoryFileSystem ram = new MemoryFileSystem();
                ram.CreateDirectory("tmp/dir/");
                ram.CreateFile("tmp/dir/file.txt", new byte[] { 32, 32, 32, 32, 32, 32, 32, 32, 32 });
                IFileSystemDisposable rom = ram.Decorate(Option.ReadOnly).AddDisposable(ram);
                // Do work ...
                rom.Dispose();
                #endregion Snippet_4b
            }


            {
                #region Snippet_4c
                // Create ram filesystem
                MemoryFileSystem ram = new MemoryFileSystem();
                ram.CreateDirectory("tmp/dir/");
                ram.CreateFile("tmp/dir/file.txt", new byte[] { 32, 32, 32, 32, 32, 32, 32, 32, 32 });

                // Create decorations
                IFileSystemDisposable rom = ram.Decorate(Option.ReadOnly).AddDisposable(ram.BelateDispose());
                IFileSystemDisposable tmp = ram.Decorate(Option.SubPath("tmp/")).AddDisposable(ram.BelateDispose());
                ram.Dispose(); // <- is actually postponed

                // Do work ...

                // Dispose rom1 and tmp, disposes ram as well
                rom.Dispose();
                tmp.Dispose();
                #endregion Snippet_4c
            }

        }
    }
}
