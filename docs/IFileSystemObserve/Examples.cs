using Lexical.FileSystem;
using Lexical.FileSystem.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace docs
{
    public class IFileSystemObserve_Examples
    {
        public static void Main(string[] args)
        {
            {
                #region IFileSystem_1
                #endregion IFileSystem_1
            }

            {
                #region IFileSystemObserve_1
                IObserver<IEvent> observer = new Observer();
                using (IDisposable handle = FileSystem.Application.Observe("**", observer))
                {
                }
                #endregion IFileSystemObserve_1
            }

            {
                #region IFileSystemObserve_2
                string path = AppDomain.CurrentDomain.BaseDirectory;
                IFileSystem filesystem = new FileSystem(path);
                IObserver<IEvent> observer = new Observer();
                IDisposable handle = FileSystem.Application.Observe(
                    filter: "**", 
                    observer: observer, 
                    eventDispatcher: EventDispatcher.Instance);
                filesystem.CreateDirectory("dir/");
                #endregion IFileSystemObserve_2
            }
            {
                #region IFileSystemObserve_3
                string path = AppDomain.CurrentDomain.BaseDirectory;
                IFileSystem filesystem = new FileSystem(path);
                IObserver<IEvent> observer = new Observer();
                IDisposable handle = FileSystem.Application.Observe(
                    filter: "**", 
                    observer: observer, 
                    eventDispatcher: EventTaskDispatcher.Instance);
                filesystem.CreateDirectory("dir/");
                #endregion IFileSystemObserve_3
            }


        }
    }

}
