// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Threading.Tasks;

namespace Lexical.FileSystem
{
    // <doc>
    /// <summary>Filesystem option for SetEventDispatcher capability.</summary>
    public interface IFileSystemOptionEventDispatch : IFileSystemOption
    {
        /// <summary>Has SetEventDispatcher capability.</summary>
        bool CanSetEventDispatcher { get; }
    }

    /// <summary>
    /// Filesystem that can be configured on how events are managed.
    /// 
    /// Extension to <see cref="IFileSystemObserve"/>.
    /// </summary>
    public interface IFileSystemEventDispatch : IFileSystem, IFileSystemOptionEventDispatch
    {
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
        /// Test if <paramref name="filesystem"/> has Observe capability.
        /// <param name="filesystem"></param>
        /// </summary>
        /// <returns>true, if has Observe capability</returns>
        public static bool CanSetEventDispatcher(this IFileSystemOption filesystem)
            => filesystem is IFileSystemEventDispatch eventDispatchHandler ? eventDispatchHandler.CanSetEventDispatcher : false;

        /// <summary>
        /// Set a <see cref="TaskFactory"/> that processes events. If set to null, runs in running thread.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="eventHandler">(optional) Set a <see cref="TaskFactory"/> that processes events. If set to null, runs in running thread.</param>
        /// <returns><paramref name="filesystem"/></returns>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support setting event handler.</exception>
        public static IFileSystem SetEventDispatcher(IFileSystem filesystem, TaskFactory eventHandler)
        {
            if (filesystem is IFileSystemEventDispatch eventDispatchHandler) return eventDispatchHandler.SetEventDispatcher(eventHandler);
            else throw new NotSupportedException(nameof(SetEventDispatcher));
        }
    }

}
