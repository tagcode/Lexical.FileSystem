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
        /// Has Observe capability.
        /// </summary>
        bool CanObserve { get; }

        /// <summary>
        /// Attach an <paramref name="observer"/> on to a directory. 
        /// 
        /// The <paramref name="filter"/> determines the file pattern to observe.
        ///  "*" Matches to any sequence characters within one folder.
        ///  "**" Matches to any sequence characters including directory levels '/'.
        ///  "?" Matches to one and exactly one character.
        /// 
        /// Examples:
        ///   <list type="bullet">
        ///     <item>"**" is any file in any directory.</item>
        ///     <item>"**/file.txt", to monitor "file.txt" in any subdirectory</item>
        ///     <item>"*" is any set of characters file in one directory. For example "mydir/somefile*.txt"</item>
        ///     <item>"", to monitor changes to the root directory itself, but not its files</item>
        ///     <item>"dir", to monitor the dir itself, but not its files</item>
        ///     <item>"dir/", to monitor the dir itself, but not its files</item>
        ///     <item>"dir/file", to monitor one file "file"</item>
        ///     <item>"dir/*", to monitor files in a dir but not subdirectories</item>
        ///     <item>"dir/**", to monitor files in a dir and its subdirectories</item>
        ///   </list>
        ///   
        /// Note that observing a directory without a pattern observes nothing, for example "dir/" does not return any events.
        /// 
        /// </summary>
        /// <param name="filter">file filter as glob pattern. </param>
        /// <param name="observer"></param>
        /// <param name="state">(optional) </param>
        /// <returns>handle to the observer, dispose to cancel the observe</returns>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="filter"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="filter"/> contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support observe</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="filter"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        IFileSystemObserveHandle Observe(string filter, IObserver<IFileSystemEvent> observer, object state = null);
    }

    /// <summary>
    /// Observer object that must be disposed to end observing
    /// </summary>
    public interface IFileSystemObserveHandle : IDisposable
    {
        /// <summary>
        /// The file system where the observer was attached.
        /// </summary>
        IFileSystem FileSystem { get; }

        /// <summary>
        /// File filter as glob pattern.
        /// </summary>
        String Filter { get; }

        /// <summary>
        /// Callback.
        /// </summary>
        IObserver<IFileSystemEvent> Observer { get; }

        /// <summary>
        /// State object that was attached at construction.
        /// </summary>
        Object State { get; }
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
        public static bool CanObserve(this IFileSystem fileSystem)
            => fileSystem is IFileSystemObserve observer ? observer.CanObserve : false;

        /// <summary>
        /// Attach an <paramref name="observer"/> on to a directory. 
        /// </summary>
        /// <param name="fileSystem"></param>
        /// <param name="filter">glob pattern to filter events. "**" means any directory. For example "mydir/**/somefile.txt", or "**" for <paramref name="filter"/> and sub-directories</param>
        /// <param name="observer"></param>
        /// <param name="state">(optional) </param>
        /// <returns>handle to the observer, dispose to cancel the observe</returns>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="filter"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="filter"/> contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support observe</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="filter"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        public static IFileSystemObserveHandle Observe(this IFileSystem fileSystem, string filter, IObserver<IFileSystemEvent> observer, object state = null)
        {
            if (fileSystem is IFileSystemObserve _observer) return _observer.Observe(filter, observer, state);
            else throw new NotSupportedException(nameof(Observe));
        }
    }

}
