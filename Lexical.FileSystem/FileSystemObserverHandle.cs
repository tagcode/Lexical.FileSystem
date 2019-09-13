// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           9.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Internal;
using System;
using System.Threading;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Base implementation file-system observer
    /// </summary>
    public abstract class FileSystemObserverHandle : IFileSystemObserverHandle
    {
        /// <summary>
        /// The file system where the observer was attached.
        /// </summary>
        public IFileSystem FileSystem { get; protected set; }

        /// <summary>
        /// File glob-pattern filter.
        /// </summary>
        public string Filter { get; protected set; }

        /// <summary>
        /// Callback.
        /// </summary>
        public IObserver<IFileSystemEvent> Observer { get; protected set; }

        /// <summary>
        /// State object that was attached at construction.
        /// </summary>
        public object State { get; protected set; }

        /// <summary>
        /// Tests if is disposed
        /// </summary>
        protected bool isDisposed;

        /// <summary>
        /// Tests if is disposed
        /// </summary>
        protected bool IsDisposing;

        /// <summary>
        /// Create observer.
        /// </summary>
        /// <param name="fileSystem"></param>
        /// <param name="filter"></param>
        /// <param name="observer"></param>
        /// <param name="state"></param>
        protected FileSystemObserverHandle(IFileSystem fileSystem, string filter, IObserver<IFileSystemEvent> observer, object state)
        {
            this.FileSystem = fileSystem;
            Filter = filter;
            Observer = observer;
            State = state;

            // Catch dispose of parent file-system
            if (fileSystem is FileSystemBase __fileSystem) __fileSystem.AddDisposableBase(this);
        }

        /// <summary>
        /// Dispose observer
        /// </summary>
        /// <exception cref="AggregateException"></exception>
        public virtual void Dispose()
        {
            IsDisposing = true;
            var _observer = Observer;

            // Remove watcher from dispose list.
            IFileSystem _fileSystem = FileSystem;
            if (_fileSystem is FileSystemBase __fileSystem) __fileSystem.RemoveDisposableBase(this);

            StructList2<Exception> errors = new StructList2<Exception>();

            // Call OnCompleted
            if (_observer != null)
            {
                Observer = null;
                try
                {
                    _observer.OnCompleted();
                }
                catch (Exception e)
                {
                    errors.Add(e);
                }
            }

            // Do other disposes
            try
            {
                InnerDispose(ref errors);
            }
            catch (Exception e)
            {
                errors.Add(e);
            }

            isDisposed = true;

            // Throw exceptions
            if (errors.Count > 0) throw new AggregateException(errors);
        }

        /// <summary>
        /// Handle inner dispose
        /// </summary>
        /// <param name="errors">errors can be placed here</param>
        /// <exception cref="Exception">any exception is captured</exception>
        public virtual void InnerDispose(ref StructList2<Exception> errors) { }
    }

    /// <summary>
    /// Base class for observer handle decoration.
    /// </summary>
    public class FileSystemObserverHandleDecoration : IFileSystemObserverHandle
    {
        /// <summary>
        /// Decorate FileSystem value.
        /// </summary>
        /// <param name="original"></param>
        /// <param name="newFileSystem"></param>
        /// <returns>decoration</returns>
        public static IFileSystemObserverHandle DecorateFileSystem(IFileSystemObserverHandle original, IFileSystem newFileSystem)
            => new NewFileSystem(original, newFileSystem);

        /// <inheritdoc/>
        public virtual IFileSystem FileSystem => original.FileSystem;
        /// <inheritdoc/>
        public virtual string Filter => original.Filter;
        /// <inheritdoc/>
        public virtual IObserver<IFileSystemEvent> Observer => original.Observer;
        /// <inheritdoc/>
        public virtual object State => original.State;
        /// <summary>
        /// Original observer handle
        /// </summary>
        protected IFileSystemObserverHandle original;

        /// <summary>
        /// Create filesystem observer handle decoration.
        /// </summary>
        /// <param name="original"></param>
        public FileSystemObserverHandleDecoration(IFileSystemObserverHandle original)
        {
            this.original = original ?? throw new ArgumentNullException(nameof(original));
        }

        /// <inheritdoc/>
        public virtual void Dispose()
            => original.Dispose();

        /// <summary>Class with overridden filesystem.</summary>
        protected class NewFileSystem : FileSystemObserverHandleDecoration
        {
            /// <summary>Overriding filesystem.</summary>
            protected IFileSystem newFilesystem;
            /// <summary>Return overridden filesystem.</summary>
            public override IFileSystem FileSystem => newFilesystem;
            /// <summary>
            /// Create decoration with new filesystem.
            /// </summary>
            /// <param name="original"></param>
            /// <param name="newFilesystem"></param>
            public NewFileSystem(IFileSystemObserverHandle original, IFileSystem newFilesystem) : base(original)
            {
                this.newFilesystem = newFilesystem;
            }
        }
    }
}
