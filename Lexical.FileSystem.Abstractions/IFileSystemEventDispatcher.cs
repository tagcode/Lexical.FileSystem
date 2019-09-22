// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.IO;
using System.Security;
using System.Threading.Tasks;

namespace Lexical.FileSystem
{
    // <doc>
    /// <summary>
    /// File-system that can be configured on how events are managed.
    /// 
    /// Extension to <see cref="IFileSystemObserve"/>.
    /// </summary>
    public interface IFileSystemEventDispatcher : IFileSystem
    {
        /// <summary>
        /// Has SetEventDispatcher() capability.
        /// </summary>
        bool CanSetEventDispatcher { get; }

        /// <summary>
        /// Set a <see cref="TaskFactory"/> that processes events. If set to null, runs in running thread.
        /// </summary>
        /// <param name="eventDispatcher">(optional) Set a <see cref="TaskFactory"/> that processes events. If set to null, runs in running thread.</param>
        /// <returns>this</returns>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support setting event handler.</exception>
        IFileSystem SetEventDispatcher(TaskFactory eventDispatcher);
    }
    // </doc>

    /// <summary>
    /// Extension methods for <see cref="IFileSystem"/>.
    /// </summary>
    public static partial class IFileSystemExtensions
    {
        /// <summary>
        /// Test if <paramref name="fileSystem"/> has Observe capability.
        /// <param name="fileSystem"></param>
        /// </summary>
        /// <returns>true, if has Observe capability</returns>
        public static bool CanSetEventDispatcher(this IFileSystem fileSystem)
            => fileSystem is IFileSystemEventDispatcher observer ? observer.CanSetEventDispatcher : false;

        /// <summary>
        /// Set a <see cref="TaskFactory"/> that processes events. If set to null, runs in running thread.
        /// </summary>
        /// <param name="fileSystem"></param>
        /// <param name="eventHandler">(optional) Set a <see cref="TaskFactory"/> that processes events. If set to null, runs in running thread.</param>
        /// <returns><paramref name="fileSystem"/></returns>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support setting event handler.</exception>
        public static IFileSystem SetEventDispatcher(IFileSystem fileSystem, TaskFactory eventHandler)
        {
            if (fileSystem is IFileSystemEventDispatcher _observer) return _observer.SetEventDispatcher(eventHandler);
            else throw new NotSupportedException(nameof(SetEventDispatcher));
        }
    }

}
