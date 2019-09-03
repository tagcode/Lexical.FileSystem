// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text.RegularExpressions;
using System.Threading;

namespace Lexical.FileSystem
{
    /// <summary>
    /// File system based <see cref="IFileSystem"/> that loads local file-system files.
    /// 
    /// If file watchers have been created, and file system is disposed, then watchers will be disposed also. 
    /// <see cref="IObserver{T}.OnCompleted"/> event is forwarded to watchers.
    /// </summary>
    public class FileSystem : FileSystemBase, IFileSystem, IFileSystemBrowse, IFileSystemOpen, IFileSystemDelete, IFileSystemMove, IFileSystemCreateDirectory, IFileSystemObserve
    {
        /// <summary>
        /// Regex pattern that extracts features and classifies paths.
        /// </summary>
        internal protected static Regex PathPattern = new Regex("(^(?<windows_driveletter>[a-zA-Z]\\:)((\\\\|\\/)(?<windows_path>.*))?$)|(^\\\\\\\\(?<share_server>[^\\\\]+)\\\\(?<share_name>[^\\\\]+)((\\\\|\\/)(?<share_path>.*))?$)|((?<unix_rooted_path>\\/.*)$)|(?<relativepath>^.*$)", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

        /// <summary>
        /// Native separator character in the running OS.
        /// </summary>
        internal protected static string osSeparator = Path.DirectorySeparatorChar + "";

        /// <summary>
        /// Is OS Windows, Linux, or OSX.
        /// </summary>
        internal protected static bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows), isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux), isOsx = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        static FileSystem osRoot = new FileSystem("");

        static Lazy<FileSystem> applicationRoot = new Lazy<FileSystem>(() => new FileSystem(AppDomain.CurrentDomain.BaseDirectory));

        /// <summary>
        /// File system system that reads from application base directory (application resources).
        /// </summary>
        public static FileSystem ApplicationRoot => applicationRoot.Value;

        /// <summary>
        /// File system system that reads from operating system root.
        /// </summary>
        public static FileSystem OSRoot => osRoot;

        /// <summary>
        /// The root path as provided with constructor.
        /// </summary>
        public readonly string RootPath;

        /// <summary>
        /// Full absolute root path.
        /// <see cref="RootPath"/> ran with <see cref="System.IO.Path.GetFullPath(string)"/>.
        /// </summary>
        public readonly string AbsoluteRootPath;

        /// <summary>
        /// Constructed to file-system root.
        /// </summary>
        public readonly bool FileSystemRoot;

        /// <summary>
        /// Get capabilities.
        /// </summary>
        public override FileSystemCapabilities Capabilities =>
            FileSystemCapabilities.Browse | FileSystemCapabilities.Exists |
            FileSystemCapabilities.CreateDirectory | FileSystemCapabilities.Delete | FileSystemCapabilities.Move |
            FileSystemCapabilities.Observe |
            FileSystemCapabilities.Open | FileSystemCapabilities.Write | FileSystemCapabilities.Read | FileSystemCapabilities.CreateFile;

        /// <summary>
        /// Create an access to local file-system.
        /// 
        /// If <paramref name="rootPath"/> is "", then FileSystem returns drive letters on Windows "C:" and "/" on Linux.
        /// 
        /// If FileSystem is constructed with relative drive letter "C:", then the instance refers to the absolute path at time of the construction.
        /// If working directory is modified later on, the FileSystem instance is not affected.
        /// </summary>
        /// <param name="rootPath">Path to root directory, or "" for OS root which returns drive letters.</param>
        public FileSystem(string rootPath) : base()
        {
            RootPath = rootPath ?? throw new ArgumentNullException(nameof(rootPath));
            AbsoluteRootPath = rootPath == "" ? "" : System.IO.Path.GetFullPath(rootPath);
            FileSystemRoot = rootPath == "" || ((isLinux || isOsx) && (rootPath == "/"));
        }

        /// <summary>
        /// Open a file for reading and/or writing. File can be created when <paramref name="fileMode"/> is <see cref="FileMode.Create"/> or <see cref="FileMode.CreateNew"/>.
        /// </summary>
        /// <param name="path">Relative path to file. Directory separator is "/". Root is without preceding "/", e.g. "dir/file.xml"</param>
        /// <param name="fileMode">determines whether to open or to create the file</param>
        /// <param name="fileAccess">how to access the file, read, write or read and write</param>
        /// <param name="fileShare">how the file will be shared by processes</param>
        /// <returns>open file stream</returns>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support opening files</exception>
        /// <exception cref="FileNotFoundException">The file cannot be found, such as when mode is FileMode.Truncate or FileMode.Open, and and the file specified by path does not exist. The file must already exist in these modes.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="fileMode"/>, <paramref name="fileAccess"/> or <paramref name="fileShare"/> contains an invalid value.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        public Stream Open(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
        {
            string concatenatedPath = Path.Combine(AbsoluteRootPath, path);
            string absolutePath = Path.GetFullPath(concatenatedPath);
            if (!absolutePath.StartsWith(AbsoluteRootPath, StringComparison.InvariantCulture)) throw new InvalidOperationException("Path cannot refer outside IFileSystem root");
            return new FileStream(absolutePath, fileMode, fileAccess, fileShare);
        }

        /// <summary>
        /// Create a directory, or multiple cascading directories.
        /// 
        /// If directory at <paramref name="path"/> already exists, then returns without exception.
        /// </summary>
        /// <param name="path">Relative path to file. Directory separator is "/". The root is without preceding slash "", e.g. "dir/dir2"</param>
        /// <returns>true if directory exists after the method, false if directory doesn't exist</returns>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is an empty string (""), contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support create directory</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        public void CreateDirectory(string path)
        {
            string concatenatedPath = Path.Combine(AbsoluteRootPath, path);
            string absolutePath = Path.GetFullPath(concatenatedPath);
            if (!absolutePath.StartsWith(AbsoluteRootPath, StringComparison.InvariantCulture)) throw new InvalidOperationException("Path cannot refer outside IFileSystem root");
            Directory.CreateDirectory(absolutePath);
        }

        /// <summary>
        /// Concatenates <see cref="RootPath"/> to <paramref name="path"/> argument.
        /// 
        /// Asserts that <paramref name="path"/> doesn't refer over the constructed root, e.g. ".".
        /// 
        /// If <paramref name="path"/> ends with directory separator, it is reduced.
        /// If <paramref name="path"/> ends with ":" on windows and root is "", then "\\" is appended to the path so that
        /// relative path is not used.
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="concatenatedPath"></param>
        /// <param name="absolutePath"></param>
        /// <return>path without trailing separator</return>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers over constructed root, e.g. ".."</exception>
        string ConcatenateAndAssertPath(string path, out string concatenatedPath, out string absolutePath)
        {
            // Assert not null
            if (path == null) throw new ArgumentNullException(nameof(path));
            // Check settings for windows
            if (isWindows)
            {
                // "dir\" remove OS directory separator from end.
                if (path.EndsWith(osSeparator, StringComparison.InvariantCulture) || path.EndsWith("/", StringComparison.InvariantCulture)) path = path.Substring(0, path.Length - 1);
                // Concatenate root path from construction to argumen path
                concatenatedPath = RootPath == "" ? path : (RootPath.EndsWith("/", StringComparison.InvariantCulture) || RootPath.EndsWith("\\", StringComparison.InvariantCulture) || RootPath.EndsWith(":", StringComparison.InvariantCulture)) ? RootPath + path : RootPath + "/" + path;
                // Convert to absolute path. If path is drive-letter and root is "", add separator "\\".
                absolutePath = Path.GetFullPath(isWindows && RootPath == "" && path.EndsWith(":", StringComparison.InvariantCulture) ? concatenatedPath + osSeparator : concatenatedPath);
                // Assert that we are not browsing the parent of constructed path
                if (!absolutePath.StartsWith(AbsoluteRootPath, StringComparison.InvariantCulture)) throw new InvalidOperationException("Path cannot refer outside IFileSystem root");
                // Return path without trailing separator
                return path;
            }
            else if (isLinux || isOsx || osSeparator == "/")
            {
                // Constructed to OS-Root
                if (FileSystemRoot)
                {
                    // Requests root files
                    if (path == "" || path == "/")
                    {
                        concatenatedPath = "/";
                        absolutePath = "/";
                        return "";
                    }
                    else
                    // Requests a specific dir
                    {
                        // Add preceding "/" on linux.
                        if (!path.StartsWith("/", StringComparison.InvariantCulture)) path = "/" + path;
                        // Remove trailing from "/dir/" -> "/dir"
                        if (path.EndsWith(osSeparator, StringComparison.InvariantCulture) || path.EndsWith("/", StringComparison.InvariantCulture)) path = path.Substring(0, path.Length - 1);
                        // Concatenate root path from construction to argument path
                        concatenatedPath = path;
                        // Convert to absolute path. If path is drive-letter and root is "", add separator "\\".
                        absolutePath = Path.GetFullPath(concatenatedPath);
                        // Assert that we are not browsing the parent of constructed path
                        if (!absolutePath.StartsWith(AbsoluteRootPath, StringComparison.InvariantCulture)) throw new InvalidOperationException("Path cannot refer outside IFileSystem root");
                        // Return path without trailing separator
                        return path;
                    }
                }
                // Constructed to specific directory
                else
                {
                    // Remove preceding "/" from "/dir"
                    if (path.StartsWith(osSeparator, StringComparison.InvariantCulture)) path = path.Substring(1, path.Length - 1);
                    // Remove trailing "/" from "dir/"
                    if (path.EndsWith(osSeparator, StringComparison.InvariantCulture)) path = path.Substring(0, path.Length - 1);
                    // Concatenate paths
                    concatenatedPath = path == "" || path == "/" ? RootPath : RootPath + (RootPath.EndsWith("/", StringComparison.InvariantCulture) ? "" : "/") + path;
                    // Convert to absolute path. If path is drive-letter and root is "", add separator "\\".
                    absolutePath = Path.GetFullPath(concatenatedPath);
                    // Assert that we are not browsing the parent of constructed path
                    if (!absolutePath.StartsWith(AbsoluteRootPath, StringComparison.InvariantCulture)) throw new InvalidOperationException("Path cannot refer outside IFileSystem root");
                    // Return path without trailing separator
                    return path;
                }
            }
            else
            {
                throw new NotSupportedException("This OS is not supported.");
            }

        }

        /// <summary>
        /// Browse a directory for file and subdirectory entries.
        /// </summary>
        /// <param name="path">path to directory, "" is root, separator is "/"</param>
        /// <returns>a snapshot of file and directory entries</returns>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support browse</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        public FileSystemEntry[] Browse(string path)
        {
            // Return OS-root, return drive letters.
            if (path == "" && RootPath == "") return BrowseRoot();
            // Concatenate paths and assert that path doesn't refer to parent of the constructed path
            string concatenatedPath, absolutePath;
            path = ConcatenateAndAssertPath(path, out concatenatedPath, out absolutePath);

            DirectoryInfo dir = new DirectoryInfo(absolutePath);
            if (dir.Exists)
            {
                string prefix = path.Length > 0 ? (path.EndsWith("/", StringComparison.InvariantCulture) ? path : path + "/") : null;
                if (FileSystemRoot && path == "" || path == "/") prefix = "/";
                StructList24<FileSystemEntry> list = new StructList24<FileSystemEntry>();
                foreach (DirectoryInfo di in dir.GetDirectories())
                {
                    FileSystemEntry e = new FileSystemEntry
                    {
                        FileSystem = this,
                        LastModified = di.LastWriteTimeUtc,
                        Name = di.Name,
                        Path = prefix == null ? di.Name : prefix + di.Name,
                        Length = -1L,
                        Type = FileSystemEntryType.Directory
                    };
                    list.Add(e);
                }
                foreach (FileInfo _fi in dir.GetFiles())
                {
                    FileSystemEntry e = new FileSystemEntry
                    {
                        FileSystem = this,
                        LastModified = _fi.LastWriteTimeUtc,
                        Name = _fi.Name,
                        Path = prefix == null ? _fi.Name : prefix + _fi.Name,
                        Length = _fi.Length,
                        Type = FileSystemEntryType.File
                    };
                    list.Add(e);
                }
                return list.ToArray();
            }

            FileInfo fi = new FileInfo(absolutePath);
            if (fi.Exists)
            {
                FileSystemEntry e = new FileSystemEntry
                {
                    FileSystem = this,
                    LastModified = fi.LastWriteTimeUtc,
                    Name = fi.Name,
                    Path = path,
                    Length = fi.Length,
                    Type = FileSystemEntryType.File
                };
                return new FileSystemEntry[] { e };
            }

            throw new DirectoryNotFoundException(path);
        }

        /// <summary>
        /// Tests whether a file or directory exists.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support exists</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        public bool Exists(string path)
        {
            // Return OS-root, return drive letters.
            if (path == "" && RootPath == "") return true;
            // Concatenate paths and assert that path doesn't refer to parent of the constructed path
            string concatenatedPath, absolutePath;
            path = ConcatenateAndAssertPath(path, out concatenatedPath, out absolutePath);

            DirectoryInfo dir = new DirectoryInfo(absolutePath);
            if (dir.Exists) return true;
            FileInfo fi = new FileInfo(absolutePath);
            if (fi.Exists) return true;
            return false;
        }

        /// <summary>
        /// Browse root drive letters
        /// </summary>
        /// <returns></returns>
        protected FileSystemEntry[] BrowseRoot()
        {
            IEnumerable<DriveInfo> driveInfos = DriveInfo.GetDrives().Where(di => di.IsReady);

            Match[] matches = driveInfos.Select(di => PathPattern.Match(di.Name)).ToArray();
            int windows = matches.Where(m => m.Groups["windows_driveletter"].Success).Count();
            int unix = matches.Where(m => m.Groups["unix_rooted_path"].Success).Count();

            // Reduce all "/mnt/xx" into single "/" entry.
            if (unix > 0)
            {
                FileSystemEntry e = new FileSystemEntry
                {
                    FileSystem = this,
                    LastModified = DateTimeOffset.MinValue,
                    Length = -1L,
                    Name = "",
                    Path = "/",
                    Type = FileSystemEntryType.Directory
                };
                return new FileSystemEntry[] { e };
            }

            List<FileSystemEntry> list = new List<FileSystemEntry>(matches.Length);

            foreach (Match m in matches)
            {
                // Reduce all "/mnt/xx" into one "/" root.
                if (m.Groups["unix_rooted_path"].Success) continue;

                string path = m.Value;
                DirectoryInfo directoryInfo = new DirectoryInfo(path);
                if (path.EndsWith(osSeparator)) path = path.Substring(0, path.Length - 1);

                FileSystemEntry e = new FileSystemEntry
                {
                    FileSystem = this,
                    LastModified = DateTimeOffset.MinValue,
                    Length = -1L,
                    Name = path,
                    Path = path,
                    Type = FileSystemEntryType.Directory
                };
                list.Add(e);
            }

            return list.ToArray();
        }

        /// <summary>
        /// Delete a file or directory.
        /// 
        /// If <paramref name="recursive"/> is false and <paramref name="path"/> is a directory that is not empty, then <see cref="InvalidOperationException"/> is thrown.
        /// If <paramref name="recursive"/> is true, then any file or directory within <paramref name="path"/> is deleted as well.
        /// </summary>
        /// <param name="path">path to a file or directory</param>
        /// <param name="recursive">if path refers to directory, recurse into sub directories</param>
        /// <exception cref="FileNotFoundException">The specified path is invalid.</exception>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is an empty string (""), contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support deleting files</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refered to a directory that wasn't empty and <paramref name="recursive"/> is false, or <paramref name="path"/> refers to non-file device</exception>
        public void Delete(string path, bool recursive = false)
        {
            // Concatenate paths and assert that path doesn't refer to parent of the constructed path
            string concatenatedPath, absolutePath;
            ConcatenateAndAssertPath(path, out concatenatedPath, out absolutePath);

            FileInfo fi = new FileInfo(absolutePath);
            if (fi.Exists) { fi.Delete(); return; }

            DirectoryInfo di = new DirectoryInfo(absolutePath);
            if (di.Exists) { di.Delete(recursive); return; }

            throw new FileNotFoundException(path);
        }

        /// <summary>
        /// Try to move/rename a file or directory.
        /// </summary>
        /// <param name="oldPath">old path of a file or directory</param>
        /// <param name="newPath">new path of a file or directory</param>
        /// <exception cref="FileNotFoundException">The specified <paramref name="oldPath"/> is invalid.</exception>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="FileNotFoundException">The specified path is invalid.</exception>
        /// <exception cref="ArgumentNullException">path is null</exception>
        /// <exception cref="ArgumentException">path is an empty string (""), contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support renaming/moving files</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">path refers to non-file device, or an entry already exists at <paramref name="newPath"/></exception>
        public void Move(string oldPath, string newPath)
        {
            // Concatenate paths and assert that path doesn't refer to parent of the constructed path
            string oldConcatenatedPath, oldAbsolutePath, newConcatenatedPath, newAbsolutePath;
            ConcatenateAndAssertPath(oldPath, out oldConcatenatedPath, out oldAbsolutePath);
            ConcatenateAndAssertPath(newPath, out newConcatenatedPath, out newAbsolutePath);

            FileInfo fi = new FileInfo(oldAbsolutePath);
            if (fi.Exists) { fi.MoveTo(newAbsolutePath); return; }

            DirectoryInfo di = new DirectoryInfo(oldAbsolutePath);
            if (di.Exists) { di.MoveTo(newAbsolutePath); return; }

            throw new FileNotFoundException(oldPath);
        }

        /// <summary>
        /// Attach an <paramref name="observer"/> on to a single file or directory. 
        /// Observing a directory will observe the whole subtree.
        /// </summary>
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
        public IDisposable Observe(string path, IObserver<FileSystemEntryEvent> observer)
        {
            // Concatenate paths and assert that path doesn't refer to parent of the constructed path
            string concatenatedPath, absolutePath;
            path = ConcatenateAndAssertPath(path, out concatenatedPath, out absolutePath);

            return new Watcher(this, observer, absolutePath, path);

            // TODO Add watcher that monitors changes to drive letters.
        }

        /// <summary>
        /// File or folder watcher.
        /// </summary>
        public class Watcher : IDisposable
        {
            /// <summary>
            /// Associated system
            /// </summary>
            protected IFileSystem fileSystem;

            /// <summary>
            /// Absolute path as OS path. Separator is '\\' or '/'.
            /// </summary>
            public readonly string AbsolutePath;

            /// <summary>
            /// Relative path. Path is relative to the <see cref="fileSystem"/>'s root.
            /// The directory separator is '/'.
            /// </summary>
            public readonly string RelativePath;

            /// <summary>
            /// Relative path that is passed for FileSystemWatcher.
            /// </summary>
            public readonly string WatcherDirectoryRelativePath;

            /// <summary>
            /// Watcher
            /// </summary>
            protected FileSystemWatcher watcher;

            /// <summary>
            /// Callback object.
            /// </summary>
            protected IObserver<FileSystemEntryEvent> observer;

            /// <summary>
            /// Create observer for one file.
            /// </summary>
            /// <param name="fileSystem">associated file system</param>
            /// <param name="observer">observer for callbacks</param>
            /// <param name="absolutePath">Absolute path</param>
            /// <param name="relativePath">Relative path (separator is '/')</param>
            public Watcher(IFileSystem fileSystem, IObserver<FileSystemEntryEvent> observer, string absolutePath, string relativePath)
            {
                this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
                this.AbsolutePath = absolutePath ?? throw new ArgumentNullException(nameof(absolutePath));
                this.RelativePath = relativePath ?? throw new ArgumentNullException(nameof(relativePath));
                this.observer = observer ?? throw new ArgumentNullException(nameof(observer));
                relativePath = relativePath ?? throw new ArgumentNullException(nameof(relativePath));
                FileInfo fi = new FileInfo(absolutePath);
                DirectoryInfo di = new DirectoryInfo(absolutePath);
                // Watch directory
                if (di.Exists)
                {
                    watcher = new FileSystemWatcher(absolutePath);
                    watcher.IncludeSubdirectories = true;
                    watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size;
                    WatcherDirectoryRelativePath = RelativePath;
                }
                // Watch file
                else //if (fi.Exists)
                {
                    watcher = new FileSystemWatcher(fi.Directory.FullName, fi.Name);
                    watcher.IncludeSubdirectories = false;
                    watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size;
                    int ix = RelativePath.LastIndexOf('/');
                    WatcherDirectoryRelativePath = ix < 0 ? "" : RelativePath.Substring(0, ix);
                }

                watcher.Error += OnError;
                watcher.Changed += OnEvent;
                watcher.Created += OnEvent;
                watcher.Deleted += OnEvent;
                watcher.Renamed += OnEvent;
                watcher.EnableRaisingEvents = true;
            }

            /// <summary>
            /// Handle (Forward) error event.
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            void OnError(object sender, ErrorEventArgs e)
            {
                var _observer = observer;
                if (_observer == null) return;

                // Disposed
                IFileSystem _fileSystem = fileSystem;
                if (_fileSystem == null) return;

                // Forward event.
                observer.OnError(e.GetException());
            }

            /// <summary>
            /// Forward event
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            void OnEvent(object sender, FileSystemEventArgs e)
            {
                var _observer = observer;
                if (_observer == null) return;

                // Disposed
                IFileSystem _fileSystem = fileSystem;
                if (_fileSystem == null) return;

                // Forward event.
                FileSystemEntryEvent ae = new FileSystemEntryEvent { FileSystem = _fileSystem, ChangeEvents = e.ChangeType, Path = WatcherDirectoryRelativePath == "" ? e.Name : WatcherDirectoryRelativePath + "/" + e.Name };
                if (Path.DirectorySeparatorChar != '/') ae.Path = ae.Path.Replace(Path.DirectorySeparatorChar, '/');
                observer.OnNext(ae);
            }

            /// <summary>
            /// Dispose observer
            /// </summary>
            /// <exception cref="AggregateException"></exception>
            public void Dispose()
            {
                var _watcher = watcher;
                var _observer = observer;

                // Clear file system reference, and remove watcher from dispose list.
                IFileSystem _fileSystem = Interlocked.Exchange(ref fileSystem, null);
                if (_fileSystem is FileSystemBase __fileSystem) __fileSystem.RemoveDisposableBase(this);

                StructList2<Exception> errors = new StructList2<Exception>();
                if (_observer != null)
                {
                    observer = null;
                    try
                    {
                        _observer.OnCompleted();
                    }
                    catch (Exception e)
                    {
                        errors.Add(e);
                    }
                }

                if (_watcher != null)
                {
                    watcher = null;
                    try
                    {
                        _watcher.Dispose();
                    }
                    catch (Exception e)
                    {
                        errors.Add(e);
                    }
                }

                // Throw exceptions
                if (errors.Count > 0) throw new AggregateException(errors);
            }
        }

        /// <summary>
        /// Add <paramref name="disposable"/> to list of objects to be disposed along with the system.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns>filesystem</returns>
        public FileSystem AddDisposable(object disposable) => AddDisposableBase(disposable) as FileSystem;

        /// <summary>
        /// Remove disposable from dispose list.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns></returns>
        public FileSystem RemoveDisposable(object disposable) => RemoveDisposableBase(disposable) as FileSystem;

        /// <summary>
        /// Print info
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => RootPath;
    }
}
