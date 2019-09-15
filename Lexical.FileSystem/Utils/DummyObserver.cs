// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           11.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

namespace Lexical.FileSystem.Utils
{
    /// <summary>
    /// Dummy observer that returns no events.
    /// </summary>
    public class DummyObserver : FileSystemObserverHandleBase
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileSystem"></param>
        /// <param name="filter"></param>
        /// <param name="observer"></param>
        /// <param name="state"></param>
        public DummyObserver(IFileSystem fileSystem, string filter, IObserver<IFileSystemEvent> observer, object state) : base(fileSystem, filter, observer, state)
        {
        }
    }
}
