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
    public class DummyObserver : ObserverBase
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="filter"></param>
        /// <param name="observer"></param>
        /// <param name="state"></param>
        /// <param name="eventDispatcher"></param>
        public DummyObserver(IFileSystem filesystem, string filter, IObserver<IEvent> observer, object state, IEventDispatcher eventDispatcher) : base(filesystem, filter, observer, state, eventDispatcher)
        {
        }
    }
}
