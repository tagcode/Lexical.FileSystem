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
    public class DisposeList_Examples
    {
        public static void Main(string[] args)
        {
            {
                #region Snippet_10a
                IDisposable disposable = new ReaderWriterLockSlim();
                IDisposeList disposeList = new DisposeList();
                disposeList.AddDisposable(disposable);
                // ... do work ... and dispose both.
                disposeList.Dispose();
                #endregion Snippet_10a
            }
            {
                #region Snippet_10b
                IDisposeList disposeList = new DisposeList();
                disposeList.AddDisposeAction(_=>Console.WriteLine("Disposed"), null);
                // ... do work ...
                disposeList.Dispose();
                #endregion Snippet_10b
            }
            {
                #region Snippet_10c
                IBelatableDispose disposeList = new DisposeList();

                // Postpone dispose
                IDisposable belateDisposeHandle = disposeList.BelateDispose();
                // Start concurrent work
                Task.Run(() =>
                {
                    // Do work
                    Thread.Sleep(100);
                    // Release belate handle. Disposes here or below, depending which thread runs last.
                    belateDisposeHandle.Dispose();
                });

                // Start dispose, but postpone it until belatehandle is disposed in another thread.
                disposeList.Dispose();
                #endregion Snippet_10c
            }
        }

    }
}
