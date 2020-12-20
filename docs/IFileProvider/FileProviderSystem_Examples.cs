using Lexical.FileSystem;
using Microsoft.Extensions.FileProviders;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace docs
{
    public class FileProviderSystem_Examples
    {
        public static void Main(string[] args)
        {
            {
                #region Snippet_1
                IFileSystem fs = new PhysicalFileProvider(@"C:\Users").ToFileSystem();
                #endregion Snippet_1
            }
            {
                #region Snippet_2
                IFileProvider fp = new PhysicalFileProvider(@"C:\");
                IFileSystem fs = fp.ToFileSystem(
                    canBrowse: true,
                    canObserve: true,
                    canOpen: true);
                #endregion Snippet_2

                foreach (var line in fs.VisitTree(depth: 2))
                    Console.WriteLine(line);
            }
            {
                #region Snippet_3
                IFileProvider fp = new PhysicalFileProvider(@"C:\Users");
                IFileSystemDisposable filesystem = fp.ToFileSystem().AddDisposable(fp);
                #endregion Snippet_3
            }
            {
                #region Snippet_4
                IFileSystemDisposable filesystem = new PhysicalFileProvider(@"C:\Users")
                    .ToFileSystem()
                    .AddDisposeAction(fs => fs.FileProviderDisposable?.Dispose());
                #endregion Snippet_4
            }
            {
                #region Snippet_5
                using (var fs = new PhysicalFileProvider(@"C:\Users")
                    .ToFileSystem()
                    .AddDisposeAction(f => f.FileProviderDisposable?.Dispose()))
                {
                    fs.Browse("");

                    // Post pone dispose at end of using()
                    IDisposable belateDisposeHandle = fs.BelateDispose();
                    // Start concurrent work
                    Task.Run(() =>
                    {
                        // Do work
                        Thread.Sleep(100);
                        fs.GetEntry("");

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
                IFileSystem fs = new PhysicalFileProvider(@"C:\Users").ToFileSystem();
                foreach (var line in fs.VisitTree(depth: 2))
                    Console.WriteLine(line);
                #endregion Snippet_6
            }

            {
                #region Snippet_7
                IFileSystem fs = new PhysicalFileProvider(@"C:\Users").ToFileSystem();
                IObserver<IEvent> observer = new Observer();
                using (IDisposable handle = fs.Observe("**", observer))
                {
                }
                #endregion Snippet_7
            }
        }
    }

}
