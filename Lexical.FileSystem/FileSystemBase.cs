// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Internal;
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
    public abstract class FileSystemBase : IFileSystemDisposable
    {
        /// <summary>
        /// Get capabilities.
        /// </summary>
        public virtual FileSystemFeatures Features { get; protected set; } = FileSystemFeatures.None;

        /// <summary>
        /// Lock for modifying dispose list.
        /// </summary>
        protected object m_lock = new object();

        /// <summary>
        /// Attached disposables.
        /// </summary>
        protected List<IDisposable> disposeList;

        /// <summary>
        /// Add <paramref name="disposable"/> to list of objects to be disposed along with the system.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns>filesystem</returns>
        internal protected IFileSystem AddDisposableBase(object disposable)
        {
            if (disposable is IDisposable disp)
            {
                lock (m_lock)
                {
                    if (this.disposeList == null) this.disposeList = new List<IDisposable>();
                    this.disposeList.Add(disp);
                }
            }
            return this;
        }

        /// <summary>
        /// Remove disposable from dispose list.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns></returns>
        internal protected IFileSystem RemoveDisposableBase(object disposable)
        {
            if (disposable is IDisposable disp)
            {
                lock (m_lock)
                {
                    if (this.disposeList != null) this.disposeList.Remove(disp);
                }
            }
            return this;
        }

        /// <summary>
        /// Dispose attached disposables.
        /// </summary>
        /// <exception cref="AggregateException">If dispose exception occurs</exception>
        public void Dispose()
        {
            // Get and clear
            List<IDisposable> list = Interlocked.Exchange(ref this.disposeList, null);
            // Nothing to dispose
            if (list == null) return;

            StructList4<Exception> errors = new StructList4<Exception>();
            foreach (IDisposable d in list)
            {
                try
                {
                    d.Dispose();
                }
                catch (Exception e)
                {
                    errors.Add(e);
                }
            }

            if (errors.Count > 0) throw new AggregateException(errors);
        }

    }
}
