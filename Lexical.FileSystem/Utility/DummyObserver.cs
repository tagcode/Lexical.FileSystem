// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           11.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

namespace Lexical.FileSystem.Utility
{
    /// <summary>
    /// Dummy observer that returns no events.
    /// </summary>
    public class DummyObserver : FileSystemObserverBase
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="filter"></param>
        /// <param name="observer"></param>
        /// <param name="state"></param>
        public DummyObserver(IFileSystem filesystem, string filter, IObserver<IFileSystemEvent> observer, object state, IFileSystemEventDispatcher eventDispatcher) : base(filesystem, filter, observer, state, eventDispatcher)
        {
        }
    }
}
