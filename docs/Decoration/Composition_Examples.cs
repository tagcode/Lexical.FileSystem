using Lexical.FileSystem;
using Lexical.FileSystem.Decoration;
using Microsoft.Extensions.FileProviders;
using System;
using System.IO;

namespace docs
{
    public class Composition_Examples
    {
        public static void Main(string[] args)
        {
            {
                #region Snippet_1
                IFileSystem ram = new MemoryFileSystem();
                IFileSystem os = FileSystem.OS;
                IFileSystem fp = new PhysicalFileProvider(AppDomain.CurrentDomain.BaseDirectory).ToFileSystem()
                    .AddDisposeAction(fs=>fs.FileProviderDisposable?.Dispose());
                IFileSystem embedded = new EmbeddedFileSystem(typeof(Composition_Examples).Assembly);

                IFileSystem composition = FileSystemExtensions.Concat(ram, os, fp, embedded)
                    .AddDisposable(embedded)
                    .AddDisposable(fp)
                    .AddDisposable(os);
                #endregion Snippet_1

                #region Snippet_2
                foreach (var entry in composition.VisitTree(depth: 1))
                    Console.WriteLine(entry);
                #endregion Snippet_2

                #region Snippet_3
                using (Stream s = composition.Open("docs.example-file.txt", FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    Console.WriteLine(s.Length);
                }
                #endregion Snippet_3
            }

            {
                #region Snippet_4
                IFileSystem ram1 = new MemoryFileSystem();
                IFileSystem ram2 = new MemoryFileSystem();
                IFileSystem composition = FileSystemExtensions.Concat(ram1, ram2);

                // Create file of 1024 bytes
                ram1.CreateFile("file.txt", new byte[1024]);

                // Create file of 10 bytes
                ram2.CreateFile("file.txt", new byte[10]);

                // Get only one entry size of 1024 bytes.
                composition.PrintTo(Console.Out, format: PrintTree.Format.Default | PrintTree.Format.Length);
                #endregion Snippet_4
            }

            {
                #region Snippet_5
                IFileSystem filesystem = FileSystem.Application;
                IFileSystem overrides = new MemoryFileSystem();
                IFileSystem composition = FileSystemExtensions.Concat(
                    (filesystem, null), 
                    (overrides, Option.ReadOnly)
                );
                #endregion Snippet_5
            }
        }
    }
}
