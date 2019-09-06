// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.IO;
using System.Security;

namespace Lexical.FileSystem
{
    // <doc>
    /// <summary>
    /// File system that observe file and directory changes.
    /// </summary>
    public interface IFileSystemObserve : IFileSystem
    {
        /// <summary>
        /// Attach an <paramref name="observer"/> on to a single file or directory. 
        /// Observing a directory will observe the whole subtree.
        /// </summary>
        /// <param name="path">path to file or directory. The directory separator is "/". The root is without preceding slash "", e.g. "dir/dir2"</param>
        /// <param name="observer"></param>
        /// <returns>disposable handle</returns>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support observe</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        IDisposable Observe(string path, IObserver<FileSystemEntryEvent> observer);
    }
    /// <summary>
    /// File entry event.
    /// </summary>
    public struct FileSystemEntryEvent
    {
        /// <summary>
        /// The file system where the observer was attached.
        /// </summary>
        public IFileSystem FileSystem;

        /// <summary>
        /// The affected file or directory.
        /// 
        /// Path is relative to the <see cref="FileSystem"/>'s root.
        /// 
        /// Directory separator is "/". Root path doesn't use separator.
        /// 
        /// Example: "dir/file.ext"
        /// </summary>
        public string Path;

        /// <summary>
        /// Change events
        /// </summary>
        public WatcherChangeTypes ChangeEvents;

        /// <summary>
        /// Print info
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => Path + " " + ChangeEvents;
    }
    // </doc>

    /// <summary>
    /// Extension methods for <see cref="IFileSystem"/>.
    /// </summary>
    public static partial class IFileSystemExtensions
    {
        /// <summary>
        /// Attach an <paramref name="observer"/> on to a single file or directory. 
        /// Observing a directory will observe the whole subtree.
        /// </summary>
        /// <param name="fileSystem"></param>
        /// <param name="path">path to file or directory. The directory separator is "/". The root is without preceding slash "", e.g. "dir/dir2"</param>
        /// <param name="observer"></param>
        /// <returns>dispose handle</returns>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support observe</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        public static IDisposable Observe(this IFileSystem fileSystem, string path, IObserver<FileSystemEntryEvent> observer)
        {
            if (fileSystem is IFileSystemObserve _observer) return _observer.Observe(path, observer);
            else throw new NotSupportedException(nameof(Observe));
        }
    }

}
