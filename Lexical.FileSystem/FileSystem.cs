// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Internal;
using Lexical.FileSystem.Utils;
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

        static FileSystem os = new FileSystem("");

        static Lazy<FileSystem> applicationRoot = new Lazy<FileSystem>(() => new FileSystem(AppDomain.CurrentDomain.BaseDirectory));

        /// <summary>
        /// File system system that reads from application base directory (application resources).
        /// </summary>
        public static FileSystem ApplicationRoot => applicationRoot.Value;

        /// <summary>
        /// File system system that represents the filesystem of the running operating system.
        /// 
        /// For instance allows drives on windows "C://Windows" and slashed root on linux "/mnt".
        /// </summary>
        public static FileSystem OS => os;

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
        public readonly bool IsOsRoot;

        /// <inheritdoc/>
        public virtual bool CanBrowse => true;
        /// <inheritdoc/>
        public virtual bool CanGetEntry => true;
        /// <inheritdoc/>
        public virtual bool CanObserve => true;
        /// <inheritdoc/>
        public virtual bool CanOpen => true;
        /// <inheritdoc/>
        public virtual bool CanRead => true;
        /// <inheritdoc/>
        public virtual bool CanWrite => true;
        /// <inheritdoc/>
        public virtual bool CanCreateFile => true;
        /// <inheritdoc/>
        public virtual bool CanDelete => true;
        /// <inheritdoc/>
        public virtual bool CanMove => true;
        /// <inheritdoc/>
        public virtual bool CanCreateDirectory => true;

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
            IsOsRoot = rootPath == "";
            // Canonized relative path uses "/" as separator. Ends with "/".
            string canonizedRelativePath = IsOsRoot ? "" : osSeparator == "/" ? rootPath : rootPath.Replace(osSeparator, " / ");
            if (!canonizedRelativePath.EndsWith("/")) canonizedRelativePath += "/";

            if (isWindows) this.Features |= FileSystemFeatures.CaseInsensitive;
            if (isLinux || isOsx) this.Features |= FileSystemFeatures.CaseSensitive;
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
                if (IsOsRoot)
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
                throw new PlatformNotSupportedException("This OS is not supported.");
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
        public IFileSystemEntry[] Browse(string path)
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
                if (IsOsRoot && path == "" || path == "/") prefix = "/";
                StructList24<IFileSystemEntry> list = new StructList24<IFileSystemEntry>();
                foreach (DirectoryInfo di in dir.GetDirectories())
                {
                    IFileSystemEntry e = new FileSystemEntryDirectory(this, String.IsNullOrEmpty(prefix) ? di.Name : prefix + di.Name, di.Name, di.LastWriteTimeUtc);
                    list.Add(e);
                }
                foreach (FileInfo _fi in dir.GetFiles())
                {
                    IFileSystemEntry e = new FileSystemEntryFile(this, String.IsNullOrEmpty(prefix) ? _fi.Name : prefix + _fi.Name, _fi.Name, _fi.LastWriteTimeUtc, _fi.Length);
                    list.Add(e);
                }
                return list.ToArray();
            }

            FileInfo fi = new FileInfo(absolutePath);
            if (fi.Exists)
            {
                IFileSystemEntry e = new FileSystemEntryFile(this, path, fi.Name, fi.LastWriteTimeUtc, fi.Length);
                return new IFileSystemEntry[] { e };
            }

            throw new DirectoryNotFoundException(path);
        }

        /// <summary>
        /// Get entry of a single file or directory.
        /// </summary>
        /// <param name="path">path to a directory or to a single file, "" is root, separator is "/"</param>
        /// <returns>entry, or null if entry is not found</returns>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support exists</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        public IFileSystemEntry GetEntry(string path)
        {
            // Return OS-root, return drive letters.
            if (path == "") return new FileSystemEntryDirectory(this, "", "", DateTimeOffset.MinValue);

            // Concatenate paths and assert that path doesn't refer to parent of the constructed path
            string concatenatedPath, absolutePath;
            path = ConcatenateAndAssertPath(path, out concatenatedPath, out absolutePath);

            DirectoryInfo dir = new DirectoryInfo(absolutePath);
            if (dir.Exists) return new FileSystemEntryDirectory(this, path, dir.Name, dir.LastWriteTimeUtc);

            FileInfo fi = new FileInfo(absolutePath);
            if (fi.Exists) return new FileSystemEntryFile(this, path, fi.Name, fi.LastWriteTimeUtc, fi.Length);

            return null;
        }

        /// <summary>
        /// Browse root drive letters
        /// </summary>
        /// <returns></returns>
        protected IFileSystemEntry[] BrowseRoot()
        {
            IEnumerable<DriveInfo> driveInfos = DriveInfo.GetDrives();

            (DriveInfo, Match)[] matches = driveInfos.Select(di => (di, PathPattern.Match(di.Name))).ToArray();
            int windows = matches.Where(m => m.Item2.Groups["windows_driveletter"].Success).Count();
            int unix = matches.Where(m => m.Item2.Groups["unix_rooted_path"].Success).Count();

            // Reduce all "/mnt/xx" into single "/" entry.
            if (unix > 0)
            {
                IFileSystemEntry e = new FileSystemEntryDriveDirectory(this, "/", "", DateTimeOffset.MinValue);
                return new IFileSystemEntry[] { e };
            }

            List<IFileSystemEntry> list = new List<IFileSystemEntry>(matches.Length);

            foreach ((DriveInfo di, Match m) in matches)
            {
                // Reduce all "/mnt/xx" into one "/" root.
                if (m.Groups["unix_rooted_path"].Success) continue;

                string path = m.Value;
                DirectoryInfo directoryInfo = new DirectoryInfo(path);
                if (path.EndsWith(osSeparator)) path = path.Substring(0, path.Length - 1);

                IFileSystemEntry e =
                    di.IsReady ?
                    new FileSystemEntryDriveDirectory(this, path, path, DateTimeOffset.MinValue) :
                    new FileSystemEntryDrive(this, path, path, DateTimeOffset.MinValue);
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
        /// Attach an <paramref name="observer"/> on to a directory. 
        /// </summary>
        /// <param name="filter">glob pattern to filter events. "**" means any directory. For example "mydir/**/somefile.txt", or "**" for <paramref name="filter"/> and sub-directories</param>
        /// <param name="observer"></param>
        /// <param name="state">(optional) </param>
        /// <returns>disposable handle</returns>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="filter"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="filter"/> contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support observe</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="filter"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        public IFileSystemObserverHandle Observe(string filter, IObserver<IFileSystemEvent> observer, object state)
        {
            // Parse filter
            GlobPatternInfo info = new GlobPatternInfo(filter);

            // Monitor single file (or dir, we don't know "dir")
            if (!info.HasWildcards)
            {
                // "dir/" observes nothing
                if (filter.EndsWith("/")) return new DummyObserver(this, filter, observer, state);

                string concatenatedPath, absolutePath;
                string path = ConcatenateAndAssertPath(filter, out concatenatedPath, out absolutePath);
                return new FileObserver(this, path, observer, state, AbsoluteRootPath, absolutePath);
            }
            else
            // Has wildcards, e.g. "**/file.txt"
            {
                // Concatenate paths and assert that path doesn't refer to parent of the constructed path
                string concatenatedPath, absolutePathToPrefixPart;

                string relativePathToPrefixPartWithoutTrailingSeparator = ConcatenateAndAssertPath(info.Prefix, out concatenatedPath, out absolutePathToPrefixPart);

                return new PatternObserver(this, observer, state, filter, AbsoluteRootPath, relativePathToPrefixPartWithoutTrailingSeparator, absolutePathToPrefixPart, info.Suffix);
            }
            
            // TODO Add watcher that monitors changes to drive letters.
            // What kind of filter monitors root contents "*" and "**"
        }

        /// <summary>
        /// Single file observer.
        /// </summary>
        protected internal class FileObserver : FileSystemObserverHandleBase
        {
            /// <summary>
            /// Absolute path as <see cref="FileSystem"/> root. Separator is '\\' or '/' depending on operating system.
            /// </summary>
            public readonly string FileSystemRootAbsolutePath;

            /// <summary>
            /// Absolute path to file. Separator is '\\' or '/' depending on operating system.
            /// </summary>
            public readonly string AbsolutePath;

            /// <summary>
            /// Relative path (<see cref="FileSystem"/> path). The directory separator is '/'.
            /// </summary>
            public readonly string RelativePath;

            /// <summary>
            /// Watcher
            /// </summary>
            protected FileSystemWatcher watcher;

            /// <summary>
            /// Create observer for one file.
            /// </summary>
            /// <param name="fileSystem">associated file system</param>
            /// <param name="relativePath">path to file as <see cref="IFileSystem"/> path</param>
            /// <param name="observer">observer for callbacks</param>
            /// <param name="state"></param>
            /// <param name="fileSystemRootAbsolutePath">Absolute path to filesystem root.</param>
            /// <param name="absolutePath">Absolute path to the file</param>
            /// <exception cref="DirectoryNotFoundException">If directory in <paramref name="fileSystemRootAbsolutePath"/> is not found.</exception>
            public FileObserver(IFileSystem fileSystem, string relativePath, IObserver<IFileSystemEvent> observer, object state, string fileSystemRootAbsolutePath, string absolutePath) : base(fileSystem, relativePath, observer, state)
            {
                this.FileSystemRootAbsolutePath = fileSystemRootAbsolutePath ?? throw new ArgumentNullException(nameof(fileSystemRootAbsolutePath));
                this.AbsolutePath = absolutePath ?? throw new ArgumentNullException(nameof(absolutePath));
                this.RelativePath = relativePath ?? throw new ArgumentNullException(nameof(relativePath));
                FileInfo fi = new FileInfo(absolutePath);
                watcher = new FileSystemWatcher(fi.Directory.FullName, fi.Name);
                watcher.IncludeSubdirectories = false;
                watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size;
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
                var _observer = Observer;
                if (_observer == null) return;

                // Disposed
                IFileSystem _fileSystem = FileSystem;
                if (_fileSystem == null) return;

                // Forward error as event object.
                Observer.OnNext(new FileSystemEventError(this, DateTimeOffset.UtcNow, e.GetException(), RelativePath));
                // Forward exception
                //observer.OnError(e.GetException());
            }

            /// <summary>
            /// Forward event
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            void OnEvent(object sender, FileSystemEventArgs e)
            {
                var _observer = Observer;
                if (_observer == null) return;

                // Disposed
                IFileSystem _fileSystem = FileSystem;
                if (_fileSystem == null) return;

                // Forward event(s)
                DateTimeOffset time = DateTimeOffset.UtcNow;
                string path = ConvertPath(e.FullPath);

                // Event type
                WatcherChangeTypes type = e.ChangeType;
                // HasFlag has been optimized since .Net core 2.1 and does not box any more
                if (type.HasFlag(WatcherChangeTypes.Created) && path != null) _observer.OnNext(new FileSystemEventCreate(this, time, path));
                if (type.HasFlag(WatcherChangeTypes.Changed) && path != null) _observer.OnNext(new FileSystemEventChange(this, time, path));
                if (type.HasFlag(WatcherChangeTypes.Deleted) && path != null) _observer.OnNext(new FileSystemEventDelete(this, time, path));
                if (type.HasFlag(WatcherChangeTypes.Renamed) && e is RenamedEventArgs re)
                {
                    string oldPath = ConvertPath(re.OldFullPath);
                    // One path must be within watcher's interest
                    if (oldPath != null || path != null)
                    {
                        // Send event
                        _observer.OnNext(new FileSystemEventRename(this, time, oldPath, path));
                    }
                }
            }

            /// <summary>
            /// Convert path from <see cref="FileSystemEventArgs"/> into relative path of <see cref="IFileSystem"/>.
            /// </summary>
            /// <param name="absolutePath">absolute file path to file that is to be converted to relative path</param>
            /// <returns>relative path, or null if failed</returns>
            String ConvertPath(string absolutePath)
            {
                if (absolutePath == AbsolutePath) return RelativePath;
                // 
                if (absolutePath.StartsWith(FileSystemRootAbsolutePath))
                {
                    // Cut the relative path
                    int length = absolutePath.Length > FileSystemRootAbsolutePath.Length && absolutePath[FileSystemRootAbsolutePath.Length] == Path.DirectorySeparatorChar ? absolutePath.Length - FileSystemRootAbsolutePath.Length - 1 : absolutePath.Length - FileSystemRootAbsolutePath.Length;
                    string _relativePath = absolutePath.Substring(absolutePath.Length-length, length);
                    // Convert separator back-slash '\' into slash '/'.
                    if (Path.DirectorySeparatorChar != '/') _relativePath = _relativePath.Replace(Path.DirectorySeparatorChar, '/');
                    // Return
                    return _relativePath;
                }
                else
                {
                    return null;
                }
            }

            /// <summary>
            /// Dispose observer
            /// </summary>
            /// <exception cref="AggregateException"></exception>
            protected override void InnerDispose(ref StructList4<Exception> errors)
            {
                base.InnerDispose(ref errors);
                var _watcher = watcher;
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
            }
        }


        /// <summary>
        /// Watches a group of files using a pattern.
        /// </summary>
        protected internal class PatternObserver : FileSystemObserverHandleBase
        {
            /// <summary>
            /// Absolute path as <see cref="FileSystem"/> root. Separator is '\\' or '/' depending on operating system.
            /// </summary>
            public readonly string FileSystemRootAbsolutePath;

            /// <summary>
            /// Absolute path to file. Separator is '\\' or '/' depending on operating system.
            /// 
            /// For example, if filter string is "dir/**" then this is "C:\temp\dir". 
            /// </summary>
            public readonly string AbsolutePathToPrefixPart;

            /// <summary>
            /// Relative path (<see cref="FileSystem"/> path). The directory separator is '/'.
            /// 
            /// For example, if filter string is "dir/**" then this is "dir".
            /// </summary>
            public readonly string RelativePathToPrefixPartWithoutTrailingSeparatorRelativePath;

            /// <summary>
            /// Suffix part of filter string that contains wildcards and filenames.
            /// 
            /// For example, if filter string is "dir/**", then this is "**".
            /// </summary>
            public readonly string SuffixPart;

            /// <summary>
            /// Filter info.
            /// </summary>
            protected GlobPatternInfo filterInfo;

            /// <summary>
            /// Watcher
            /// </summary>
            protected FileSystemWatcher watcher;

            /// <summary>
            /// Filter glob pattern
            /// </summary>
            public readonly Regex Pattern;

            /// <summary>
            /// Create observer for one file.
            /// </summary>
            /// <param name="fileSystem">associated file system</param>
            /// <param name="observer">observer for callbacks</param>
            /// <param name="state"></param>
            /// <param name="filterString">original filter string</param>
            /// <param name="fileSystemRootAbsolutePath">Absolute path to <see cref="FileSystem"/> root.</param>
            /// <param name="relativePathToPrefixPartWithoutTrailingSeparator">prefix part of <paramref name="filterString"/>, for example "dir" if filter string is "dir/**"</param>
            /// <param name="absolutePathToPrefixPart">absolute path to prefix part of <paramref name="filterString"/>, for example "C:\Temp\Dir", if filter string is "dir/**" and <paramref name="fileSystemRootAbsolutePath"/> is "C:\temp"</param>
            /// <param name="suffixPart">Suffix part of <paramref name="filterString"/>, for example "**" if filter string is "dir/**"</param>
            public PatternObserver(
                IFileSystem fileSystem, 
                IObserver<IFileSystemEvent> observer, 
                object state,
                string filterString,
                string fileSystemRootAbsolutePath, 
                string relativePathToPrefixPartWithoutTrailingSeparator,
                string absolutePathToPrefixPart,
                string suffixPart)
            : base(fileSystem, filterString, observer, state)
            {
                this.FileSystemRootAbsolutePath = fileSystemRootAbsolutePath ?? throw new ArgumentNullException(nameof(fileSystemRootAbsolutePath));
                this.AbsolutePathToPrefixPart = absolutePathToPrefixPart ?? throw new ArgumentNullException(nameof(absolutePathToPrefixPart));
                this.RelativePathToPrefixPartWithoutTrailingSeparatorRelativePath = relativePathToPrefixPartWithoutTrailingSeparator ?? throw new ArgumentNullException(nameof(relativePathToPrefixPartWithoutTrailingSeparator));
                this.SuffixPart = suffixPart ?? throw new ArgumentNullException(nameof(suffixPart));
                this.Pattern = GlobPatternFactory.Slash.CreateRegex(filterString ?? throw new ArgumentNullException(nameof(filterString)));
                watcher = new FileSystemWatcher(AbsolutePathToPrefixPart);
                watcher.IncludeSubdirectories = true;
                watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size;
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
                var _observer = Observer;
                if (_observer == null) return;

                // Disposed
                IFileSystem _fileSystem = FileSystem;
                if (_fileSystem == null) return;

                // Forward error as event object.
                Observer.OnNext(new FileSystemEventError(this, DateTimeOffset.UtcNow, e.GetException(), null));
                // Forward exception
                //observer.OnError(e.GetException());
            }

            /// <summary>
            /// Forward event
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            void OnEvent(object sender, FileSystemEventArgs e)
            {
                var _observer = Observer;
                if (_observer == null) return;

                // Disposed
                IFileSystem _fileSystem = FileSystem;
                if (_fileSystem == null) return;

                // Forward event(s)
                DateTimeOffset time = DateTimeOffset.UtcNow;
                string path = ConvertPath(e.FullPath);

                // Event type
                WatcherChangeTypes type = e.ChangeType;
                // HasFlag has been optimized since .Net core 2.1
                if (type.HasFlag(WatcherChangeTypes.Created) && path != null && (Pattern.IsMatch(path)||Pattern.IsMatch("/"+path))) _observer.OnNext(new FileSystemEventCreate(this, time, path));
                if (type.HasFlag(WatcherChangeTypes.Changed) && path != null && (Pattern.IsMatch(path) || Pattern.IsMatch("/" + path))) _observer.OnNext(new FileSystemEventChange(this, time, path));
                if (type.HasFlag(WatcherChangeTypes.Deleted) && path != null && (Pattern.IsMatch(path) || Pattern.IsMatch("/" + path))) _observer.OnNext(new FileSystemEventDelete(this, time, path));
                if (type.HasFlag(WatcherChangeTypes.Renamed) && e is RenamedEventArgs re) 
                {
                    string oldPath = ConvertPath(re.OldFullPath);
                    // One path match match glob pattenr
                    if ((oldPath != null && (Pattern.IsMatch(oldPath)||Pattern.IsMatch("/"+oldPath))) || (path != null && (Pattern.IsMatch(path)||Pattern.IsMatch("/"+path))))
                    {
                        // Send event
                        _observer.OnNext(new FileSystemEventRename(this, time, oldPath, path));
                    }
                }
            }

            /// <summary>
            /// Convert path from <see cref="FileSystemEventArgs"/> into relative path of <see cref="IFileSystem"/>.
            /// </summary>
            /// <param name="absolutePath">absolute file path to file that is to be converted to relative path</param>
            /// <returns>relative path, or null if failed</returns>
            String ConvertPath(string absolutePath)
            {
                // 
                if (absolutePath.StartsWith(FileSystemRootAbsolutePath))
                {
                    // Cut the relative path
                    int length = absolutePath.Length > FileSystemRootAbsolutePath.Length && absolutePath[FileSystemRootAbsolutePath.Length] == Path.DirectorySeparatorChar ? absolutePath.Length - FileSystemRootAbsolutePath.Length - 1 : absolutePath.Length - FileSystemRootAbsolutePath.Length;
                    string _relativePath = absolutePath.Substring(absolutePath.Length - length, length);
                    // Convert separator back-slash '\' into slash '/'.
                    if (Path.DirectorySeparatorChar != '/') _relativePath = _relativePath.Replace(Path.DirectorySeparatorChar, '/');
                    // Return
                    return _relativePath;
                }
                else
                {
                    return null;
                }
            }

            /// <summary>
            /// Dispose observer
            /// </summary>
            /// <exception cref="AggregateException"></exception>
            protected override void InnerDispose(ref StructList4<Exception> errors)
            {
                base.InnerDispose(ref errors);
                var _watcher = watcher;
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
            }
        }

        /// <summary>
        /// Add <paramref name="disposable"/> to list of objects to be disposed along with the system.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns>filesystem</returns>
        public FileSystem AddDisposable(object disposable)
        {
            base.AddDisposableBase(disposable);
            return this;
        }

        /// <summary>
        /// Add <paramref name="disposables"/> to list of objects to be disposed along with the system.
        /// </summary>
        /// <param name="disposables"></param>
        /// <returns>filesystem</returns>
        public FileSystem AddDisposables(IEnumerable<object> disposables)
        {
            base.AddDisposablesBase(disposables);
            return this;
        }

        /// <summary>
        /// Remove <paramref name="disposable"/> from dispose list.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns></returns>
        public FileSystem RemoveDisposable(object disposable)
        {
            base.RemoveDisposableBase(disposable);
            return this;
        }

        /// <summary>
        /// Remove <paramref name="disposables"/> from dispose list.
        /// </summary>
        /// <param name="disposables"></param>
        /// <returns></returns>
        public FileSystem RemoveDisposables(IEnumerable<object> disposables)
        {
            base.RemoveDisposablesBase(disposables);
            return this;
        }

        /// <summary>
        /// Print info
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => RootPath;
    }
}
