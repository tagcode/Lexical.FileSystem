// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           12.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Internal;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Lexical.FileSystem.Adapter
{
    /// <summary>
    /// Adapts <see cref="IFileSystem"/> into <see cref="IFileProvider"/>.
    /// </summary>
    public class FileSystemProvider : IFileProvider
    {
        /// <summary>
        /// Source filesystem
        /// </summary>
        public IFileSystem FileSystem { get; protected set; }

        /// <summary>
        /// Lock for modifying dispose list.
        /// </summary>
        protected object m_lock = new object();

        /// <summary>
        /// Attached disposables.
        /// </summary>
        protected List<IDisposable> disposeList;

        /// <summary>
        /// Create adapter that adapts <paramref name="filesystem"/> into <see cref="IFileProvider"/>.
        /// </summary>
        /// <param name="filesystem"></param>
        public FileSystemProvider(IFileSystem filesystem)
        {
            FileSystem = filesystem ?? throw new ArgumentNullException(nameof(filesystem));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="subpath"></param>
        /// <returns></returns>
        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Locate a file at the given path.
        /// </summary>
        /// <param name="subpath">Relative path that identifies the file.</param>
        /// <returns>The file information. Caller must check Exists property.</returns>
        public IFileInfo GetFileInfo(string subpath)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Enumerate a directory at the given path, if any.
        /// </summary>
        /// <param name="filter">Relative path that identifies the directory.</param>
        /// <returns>Returns the contents of the directory.</returns>
        public IChangeToken Watch(string filter)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Add <paramref name="disposable"/> to list of objects to be disposed along with the system.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns>filesystem</returns>
        public FileSystemProvider AddDisposable(object disposable)
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
        public FileSystemProvider RemoveDisposable(object disposable)
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
