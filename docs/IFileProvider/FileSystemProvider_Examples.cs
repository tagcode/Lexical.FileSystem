using Lexical.FileSystem;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace docs
{
    public class FileSystemProvider_Examples
    {
        public static void Main(string[] args)
        {
            {
                #region Snippet_1
                IFileProvider fp = FileSystem.OS.ToFileProvider();
                #endregion Snippet_1
            }
            {
                #region Snippet_3
                IFileSystem fs = new FileSystem("");
                IFileProviderDisposable fp = fs.ToFileProvider().AddDisposable(fs);
                #endregion Snippet_3
            }
            {
                #region Snippet_4
                IFileProviderDisposable fp = new FileSystem("")
                    .ToFileProvider()
                    .AddDisposeAction(fs => fs.FileSystemDisposable?.Dispose());
                #endregion Snippet_4
            }
            {
                #region Snippet_5
                using (var fp = new FileSystem("").ToFileProvider()
                        .AddDisposeAction(fs => fs.FileSystemDisposable?.Dispose()))
                {
                    fp.GetDirectoryContents("");

                    // Post pone dispose at end of using()
                    IDisposable belateDisposeHandle = fp.BelateDispose();
                    // Start concurrent work
                    Task.Run(() =>
                    {
                        // Do work
                        Thread.Sleep(100);
                        fp.GetDirectoryContents("");

                        // Release the belate dispose handle
                        // FileSystem is actually disposed here
                        // provided that the using block has exited
                        // in the main thread.
                        belateDisposeHandle.Dispose();
                    });

                    // using() exists here and starts the dispose fs
                }
                #endregion Snippet_5
            }

            {
                #region Snippet_6
                IFileProvider fp = FileSystem.OS.ToFileProvider();
                foreach (var fi in fp.GetDirectoryContents(""))
                    Console.WriteLine(fi.Name);
                #endregion Snippet_6
            }

            {
                #region Snippet_7
                IChangeToken token = new FileSystem(@"c:").ToFileProvider().Watch("**");
                token.RegisterChangeCallback(o => Console.WriteLine("Changed"), null);
                #endregion Snippet_7
                Console.WriteLine(token.HasChanged);
            }
        }
    }
}
