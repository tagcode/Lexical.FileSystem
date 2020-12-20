using Lexical.FileSystem;
using Lexical.FileSystem.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace docs
{
    public class IFileSystemDelete_Examples
    {
        public static void Main(string[] args)
        {
            {
                #region IFileSystem_1
                string path = AppDomain.CurrentDomain.BaseDirectory;
                IFileSystem filesystem = new FileSystem(path);
                #endregion IFileSystem_1

                #region IFileSystem_2
                #endregion IFileSystem_2
            }

            {
                IFileSystem filesystem = new FileSystem(AppDomain.CurrentDomain.BaseDirectory);
                #region IFileSystemBrowse_1
                foreach (var entry in filesystem.Browse(""))
                    Console.WriteLine(entry.Path);
                #endregion IFileSystemBrowse_1

                #region IFileSystemBrowse_2
                bool exists = filesystem.GetEntry("dir") != null;
                #endregion IFileSystemBrowse_2
            }
            {
                IFileSystem filesystem = new FileSystem(AppDomain.CurrentDomain.BaseDirectory);
                #region IFileSystemOpen_1
                using (Stream s = filesystem.Open("file.txt", FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    Console.WriteLine(s.Length);
                }
                #endregion IFileSystemOpen_1

                #region IFileSystemOpen_2
                using (Stream s = filesystem.Open("somefile.txt", 
                       FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    s.WriteByte(32);
                }
                #endregion IFileSystemOpen_2
            }

            {
                IFileSystem filesystem = new FileSystem(AppDomain.CurrentDomain.BaseDirectory);
                #region IFileSystemObserve_1
                IObserver<IEvent> observer = new Observer();
                using (IDisposable handle = filesystem.Observe("**", observer))
                {
                }
                #endregion IFileSystemObserve_1

                #region IFileSystemObserve_2
                #endregion IFileSystemObserve_2
            }

            {
                IFileSystem filesystem = new FileSystem(AppDomain.CurrentDomain.BaseDirectory);
                #region IFileSystemCreateDirectory_1
                filesystem.CreateDirectory("dir");
                #endregion IFileSystemCreateDirectory_1
                filesystem.Delete("dir");

                #region IFileSystemCreateDirectory_2
                #endregion IFileSystemCreateDirectory_2
            }

            {
                IFileSystem filesystem = new FileSystem(AppDomain.CurrentDomain.BaseDirectory);
                filesystem.CreateDirectory("dir");
                #region IFileSystemMove_1
                filesystem.Move( "dir", "new-name");
                #endregion IFileSystemMove_1
                filesystem.Delete("new-name");

                #region IFileSystemMove_2
                #endregion IFileSystemMove_2
            }

            {
                IFileSystem filesystem = new FileSystem(AppDomain.CurrentDomain.BaseDirectory);
                #region IFileSystemDelete_1
                filesystem.Delete("file.txt");
                #endregion IFileSystemDelete_1
                using (Stream s = filesystem.Open("file.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite)) { }

                filesystem.CreateDirectory("dir");
                #region IFileSystemDelete_2
                filesystem.Delete("dir", recurse: true);
                #endregion IFileSystemDelete_2
            }

            {
                #region IFileSystem_ExampleRun
                IFileSystem filesystem = new ExampleFileSystem();
                foreach (IEntry e in new FileScanner(filesystem).AddGlobPattern("**"))
                    Console.WriteLine(e.Path);
                #endregion IFileSystem_ExampleRun
            }

        }
    }

}
