// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Internal;
using Lexical.FileSystem.Utils;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Base implementation for <see cref="IFileSystem"/>. 
    /// 
    /// Disposables can be attached to be disposed along with <see cref="IFileSystem"/>.
    /// Watchers can be attached as disposables, so that they forward <see cref="IObserver{T}.OnCompleted"/> event upon IFileSystem dispose.
    /// </summary>
    public abstract class FileSystemBase : DisposeList, IFileSystemDisposable
    {
        /// <summary>
        /// Get capabilities.
        /// </summary>
        public virtual FileSystemFeatures Features { get; protected set; } = FileSystemFeatures.None;
    }
}
