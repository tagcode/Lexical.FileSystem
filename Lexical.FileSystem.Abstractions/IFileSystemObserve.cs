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
    /// <summary>File system option for observe.</summary>
    [Operations(typeof(FileSystemOptionOperationObserve))]
    // <IFileSystemOptionObserve>
    public interface IFileSystemOptionObserve : IFileSystemOption
    {
        /// <summary>Has Observe capability.</summary>
        bool CanObserve { get; }
        /// <summary>Has SetEventDispatcher capability.</summary>
        bool CanSetEventDispatcher { get; }
    }
    // </IFileSystemOptionObserve>

    // <doc>
    /// <summary>
    /// File system that observe file and directory changes.
    /// </summary>
    public interface IFileSystemObserve : IFileSystem, IFileSystemOptionObserve
    {
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
        ///     <item>"**/file.txt", to monitor "file.txt" in any subdirectory (excluding root due to missing "/")</item>
        ///     <item>"*" is any set of characters file in one directory. For example "mydir/somefile*.txt"</item>
        ///     <item>"", to monitor changes to the root directory itself, but not its files</item>
        ///     <item>"dir", to monitor the dir itself, but not its files</item>
        ///     <item>"dir/", to monitor the dir itself, but not its files</item>
        ///     <item>"dir/file", to monitor one file "file"</item>
        ///     <item>"dir/*", to monitor files in a dir but not subdirectories</item>
        ///     <item>"dir/**", to monitor files in a dir and its subdirectories</item>
        ///   </list>
        /// 
        /// The implementation sends the reference to the observer handle in a <see cref="IFileSystemEventStart"/> event before this method returns to caller.
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
        IFileSystemObserver Observe(string filter, IObserver<IFileSystemEvent> observer, object state = null);

        /// <summary>
        /// Set a <see cref="TaskFactory"/> that dispatches the events. If set to null, runs in running thread.
        /// </summary>
        /// <param name="eventDispatcher">(optional) Set a <see cref="TaskFactory"/> that processes events. If set to null, runs in running thread.</param>
        /// <returns>this</returns>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support setting event handler.</exception>
        IFileSystem SetEventDispatcher(TaskFactory eventDispatcher);
    }

    /// <summary>
    /// Observer information.
    /// </summary>
    public interface IFileSystemObserver : IDisposable
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
}
