// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           12.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Internal;
using Lexical.FileSystem.Utility;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Lexical.FileSystem.Adapter
{
    /// <summary>
    /// Adapts <see cref="IFileSystem"/> into <see cref="IFileProvider"/>.
    /// </summary>
    public class FileSystemProvider : DisposeList, IFileProvider
    {
        /// <summary>
        /// Source filesystem
        /// </summary>
        public IFileSystem FileSystem { get; protected set; }

        /// <summary>
        /// Create adapter that adapts <paramref name="filesystem"/> into <see cref="IFileProvider"/>.
        /// </summary>
        /// <param name="filesystem"></param>
        public FileSystemProvider(IFileSystem filesystem) : base()
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
            IFileSystemEntry[] entries = FileSystem.Browse(subpath);
            
            throw new NotImplementedException();
        }

        class DirectoryContents : IDirectoryContents
        {
            public bool Exists => throw new NotImplementedException();

            public IEnumerator<IFileInfo> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
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
        /// Invoke <paramref name="disposeAction"/> on the dispose of the object.
        /// 
        /// If parent object is disposed or being disposed, the disposable will be disposed immedialy.
        /// </summary>
        /// <param name="disposeAction"></param>
        /// <returns>true if was added to list, false if was disposed right away</returns>
        public new FileSystemProvider AddDisposeAction(Action<object> disposeAction)
        {
            base.AddDisposeAction(disposeAction);
            return this;
        }

        /// <summary>
        /// Add <paramref name="disposable"/> to list of objects to be disposed along with the system.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns>filesystem</returns>
        public FileSystemProvider AddDisposable(object disposable)
        {
            ((IDisposeList)this).AddDisposable(disposable);
            return this;
        }

        /// <summary>
        /// Add <paramref name="disposables"/> to list of objects to be disposed along with the system.
        /// </summary>
        /// <param name="disposables"></param>
        /// <returns>filesystem</returns>
        public FileSystemProvider AddDisposables(IEnumerable<object> disposables)
        {
            ((IDisposeList)this).AddDisposables(disposables);
            return this;
        }

        /// <summary>
        /// Remove <paramref name="disposable"/> from dispose list.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns></returns>
        public FileSystemProvider RemoveDisposable(object disposable)
        {
            ((IDisposeList)this).RemoveDisposable(disposable);
            return this;
        }

        /// <summary>
        /// Remove <paramref name="disposables"/> from dispose list.
        /// </summary>
        /// <param name="disposables"></param>
        /// <returns></returns>
        public FileSystemProvider RemoveDisposables(IEnumerable<object> disposables)
        {
            ((IDisposeList)this).RemoveDisposables(disposables);
            return this;
        }

        /// <summary>
        /// Print info
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => FileSystem.ToString();
    }
}
