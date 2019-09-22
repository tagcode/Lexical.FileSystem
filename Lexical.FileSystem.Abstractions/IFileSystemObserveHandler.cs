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
    public interface IFileSystemObserveHandler : IFileSystem
    {
        /// <summary>
        /// Has SetEventHandler() capability.
        /// </summary>
        bool CanSetEventHandler { get; }

        /// <summary>
        /// Set a <see cref="TaskFactory"/> that processes events. If set to null, runs in running thread.
        /// </summary>
        /// <param name="eventHandler">(optional) Set a <see cref="TaskFactory"/> that processes events. If set to null, runs in running thread.</param>
        /// <returns>this</returns>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support setting event handler.</exception>
        IFileSystem SetEventHandler(TaskFactory eventHandler);
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
        public static bool CanSetEventHandler(this IFileSystem fileSystem)
            => fileSystem is IFileSystemObserveHandler observer ? observer.CanSetEventHandler : false;

        /// <summary>
        /// Set a <see cref="TaskFactory"/> that processes events. If set to null, runs in running thread.
        /// </summary>
        /// <param name="fileSystem"></param>
        /// <param name="eventHandler">(optional) Set a <see cref="TaskFactory"/> that processes events. If set to null, runs in running thread.</param>
        /// <returns><paramref name="fileSystem"/></returns>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support setting event handler.</exception>
        public static IFileSystem SetEventHandler(IFileSystem fileSystem, TaskFactory eventHandler)
        {
            if (fileSystem is IFileSystemObserveHandler _observer) return _observer.SetEventHandler(eventHandler);
            else throw new NotSupportedException(nameof(SetEventHandler));
        }
    }

}
