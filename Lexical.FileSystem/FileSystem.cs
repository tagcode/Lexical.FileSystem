// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Internal;
using Lexical.FileSystem.Utility;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Operating System based <see cref="IFileSystem"/> implementation that uses normal files and directories.
    /// 
    /// If file watchers have been created, and file system is disposed, then watchers will be disposed also. 
    /// <see cref="IObserver{T}.OnCompleted"/> event is forwarded to watchers.
    /// </summary>
    public class FileSystem : FileSystemBase, IFileSystem, IFileSystemBrowse, IFileSystemOpen, IFileSystemDelete, IFileSystemFileAttribute, IFileSystemMove, IFileSystemCreateDirectory, IFileSystemObserve, IFileSystemOptionPath
    {
        /// <summary>
        /// Regex pattern that extracts features and classifies paths.
        /// </summary>
        internal protected static Regex PathPattern = new Regex("(^(?<windows_driveletter>[a-zA-Z]\\:)((\\\\|\\/)(?<windows_path>.*))?$)|(^\\\\\\\\(?<share_server>[^\\\\]+)\\\\(?<share_name>[^\\\\]+)((\\\\|\\/)(?<share_path>.*))?$)|((?<unix_rooted_path>\\/.*)$)|(?<relativepath>^.*$)", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

        /// <summary>
        /// Native separator character in the running OS.
        /// </summary>
        internal protected static string osSeparator = System.IO.Path.DirectorySeparatorChar + "";

        /// <summary>
        /// Is OS Windows, Linux, or OSX.
        /// </summary>
        internal protected static bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows), isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux), isOsx = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        /// <summary>Operating system root.</summary>
        static FileSystem os = new FileSystem.NonDisposable("");
        /// <summary>Running application's base directory.</summary>
        static Lazy<FileSystem> application = new Lazy<FileSystem>(() => new FileSystem.NonDisposable(AppDomain.CurrentDomain.BaseDirectory));
        /// <summary>Running user's temp directory. "C:\Users\user\AppData\Local\Temp"</summary>
        static Lazy<FileSystem> temp = new Lazy<FileSystem>(() => new FileSystem.NonDisposable(System.IO.Path.GetTempPath()));
        /// <summary>The My Documents folder. "C:\Users\user\Documents"</summary>
        static Lazy<FileSystem> myDocuments = new Lazy<FileSystem>(() => new FileSystem.NonDisposable(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)));
        /// <summary>Documents that are common to all users. "C:\Users\Public\Documents", linux = null</summary>
        static Lazy<FileSystem> commonDocuments = new Lazy<FileSystem>(() => new FileSystem.NonDisposable(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments)));
        /// <summary>A common repository for documents. "C:\Users\user\Documents"</summary>
        static Lazy<FileSystem> personal = new Lazy<FileSystem>(() => new FileSystem.NonDisposable(Environment.GetFolderPath(Environment.SpecialFolder.Personal)));
        /// <summary>The user's profile folder. Applications should not create files or folders at this level; they should put their data under the locations referred to by <see cref="applicationData"/>. "C:\Users\user"</summary>
        static Lazy<FileSystem> userProfile = new Lazy<FileSystem>(() => new FileSystem.NonDisposable(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)));
        /// <summary>A common repository for application-specific data for the current roaming user. "C:\Users\user\AppData\Roaming"</summary>
        static Lazy<FileSystem> applicationData = new Lazy<FileSystem>(() => new FileSystem.NonDisposable(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)));
        /// <summary>A common repository for application-specific data that is used by the current, non-roaming user. "C:\Users\user\AppData\Local"</summary>
        static Lazy<FileSystem> localApplicationData = new Lazy<FileSystem>(() => new FileSystem.NonDisposable(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)));
        /// <summary>A common repository for application-specific data that is used by all users. "C:\ProgramData"</summary>
        static Lazy<FileSystem> commonApplicationData = new Lazy<FileSystem>(() => new FileSystem.NonDisposable(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)));
        /// <summary>Desktop. "C:\Users\user\Desktop" "/home/user/Desktop"</summary>
        static Lazy<FileSystem> desktop = new Lazy<FileSystem>(() => new FileSystem.NonDisposable(Environment.GetFolderPath(Environment.SpecialFolder.Desktop)));
        /// <summary>MyPictures. "C:\Users\user\Pictures" "/home/user/Pictures"</summary>
        static Lazy<FileSystem> myPictures = new Lazy<FileSystem>(() => new FileSystem.NonDisposable(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)));
        /// <summary>MyVideos. "C:\Users\user\Videos" "/home/user/Videos"</summary>
        static Lazy<FileSystem> myVideos = new Lazy<FileSystem>(() => new FileSystem.NonDisposable(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)));
        /// <summary>MyMusic. "C:\Users\user\Music" "/home/user/Music"</summary>
        static Lazy<FileSystem> myMusic = new Lazy<FileSystem>(() => new FileSystem.NonDisposable(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)));
        /// <summary>Templates. "C:\Users\user\AppData\Roaming\Microsoft\Windows\Templates" "/home/user/Templates"</summary>
        static Lazy<FileSystem> templates = new Lazy<FileSystem>(() => new FileSystem.NonDisposable(Environment.GetFolderPath(Environment.SpecialFolder.Templates)));

        /// <summary>Operating system root.</summary>
        public static FileSystem OS => os;
        /// <summary>Running application's base directory.</summary>
        public static FileSystem Application => application.Value;
        /// <summary>Running user's temp directory. "C:\Users\user\AppData\Local\Temp"</summary>
        public static FileSystem Temp = temp.Value;
        /// <summary>The My Documents folder. "C:\Users\user\Documents"</summary>
        public static FileSystem MyDocuments = myDocuments.Value;
        /// <summary>Documents that are common to all users. "C:\Users\Public\Documents", linux = null</summary>
        public static FileSystem CommonDocuments = commonDocuments.Value;
        /// <summary>A common repository for documents. "C:\Users\user\Documents"</summary>
        public static FileSystem Personal = personal.Value;
        /// <summary>The user's profile folder. Applications should not create files or folders at this level; they should put their data under the locations referred to by <see cref="applicationData"/>. "C:\Users\user"</summary>
        public static FileSystem UserProfile = userProfile.Value;

        /// <summary>Desktop. "C:\Users\user\Desktop" "/home/user/Desktop"</summary>
        public static FileSystem Desktop => desktop.Value;
        /// <summary>MyPictures. "C:\Users\user\Pictures" "/home/user/Pictures"</summary>
        public static FileSystem MyPictures => myPictures.Value;
        /// <summary>MyVideos. "C:\Users\user\Videos" "/home/user/Videos"</summary>
        public static FileSystem MyVideos => myVideos.Value;
        /// <summary>MyMusic. "C:\Users\user\Music" "/home/user/Music"</summary>
        public static FileSystem MyMusic => myMusic.Value;
        /// <summary>Templates. "C:\Users\user\AppData\Roaming\Microsoft\Windows\Templates" "/home/user/Templates"</summary>
        public static FileSystem Templates => templates.Value;

        /// <summary>User's cloud-sync program configuration (roaming data). "C:\Users\user\AppData\Roaming\" "/home/user/.config/"</summary>
        public static FileSystem Config = applicationData.Value;
        /// <summary>User's local program data. "C:\Users\user\AppData\Local\" "/home/user/.local/share/"</summary>
        public static FileSystem Data = localApplicationData.Value;
        /// <summary>Program data that is shared with every user. "C:\ProgramData\" "/usr/share/"</summary>
        public static FileSystem ProgramData = commonApplicationData.Value;

        /// <summary>
        /// The root path as provided with constructor.
        /// </summary>
        public readonly string Path;

        /// <summary>
        /// Full absolute root path.
        /// <see cref="Path"/> ran with <see cref="System.IO.Path.GetFullPath(string)"/>.
        /// </summary>
        public readonly string AbsolutePath;

        /// <summary>
        /// Constructed to filesystem root.
        /// </summary>
        public readonly bool IsOsRoot;

        /// <inheritdoc/>
        public FileSystemCaseSensitivity CaseSensitivity { get; protected set; }
        /// <inheritdoc/>
        public bool EmptyDirectoryName => false;
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
        /// <inheritdoc/>
        public bool CanSetFileAttribute => true;

        /// <summary>Root "" entry</summary>
        protected IFileSystemEntry rootEntry;

        /// <summary>
        /// Create an access to local filesystem.
        /// 
        /// If <paramref name="path"/> is "", then FileSystem returns drive letters on Windows "C:" and "/" on Linux.
        /// 
        /// If FileSystem is constructed with relative drive letter "C:", then the instance refers to the absolute path at time of the construction.
        /// If working directory is modified later on, the FileSystem instance is not affected.
        /// </summary>
        /// <param name="path">Path to root directory, or "" for OS root which returns drive letters.</param>
        public FileSystem(string path) : base()
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            AbsolutePath = path == "" ? "" : System.IO.Path.GetFullPath(/*c: ?*/isWindows && path.EndsWith(":", StringComparison.InvariantCulture) ? /*c:\*/path + osSeparator : /*c:\nn*/path);

            IsOsRoot = path == "";
            // Canonized relative path uses "/" as separator. Ends with "/".
            string canonizedRelativePath = IsOsRoot ? "" : osSeparator == "/" ? path : path.Replace(osSeparator, " / ");
            if (!canonizedRelativePath.EndsWith("/")) canonizedRelativePath += "/";

            if (isWindows) this.CaseSensitivity = FileSystemCaseSensitivity.Inconsistent; /*Smb drives may have sensitive names*/
            if (isLinux || isOsx) this.CaseSensitivity = FileSystemCaseSensitivity.Inconsistent; /*Smb drives may have insensitive names*/

            rootEntry = new FileSystemEntryDirectory(this, "", "", DateTimeOffset.MinValue, DateTimeOffset.MinValue, AbsolutePath);
        }

        /// <summary>
        /// Non-disposable <see cref="FileSystem"/> disposes and cleans all attached <see cref="IDisposable"/> on dispose, but doesn't go into disposed state.
        /// </summary>
        public class NonDisposable : FileSystem
        {
            /// <summary>Create non-disposable filesystem.</summary>
            /// <param name="path">Path to root directory, or "" for OS root which returns drive letters.</param>
            public NonDisposable(string path) : base(path) { SetToNonDisposable(); }
        }

        /// <summary>
        /// Open a file for reading and/or writing. File can be created when <paramref name="fileMode"/> is <see cref="FileMode.Create"/> or <see cref="FileMode.CreateNew"/>.
        /// </summary>
        /// <param name="path">Relative path to file. Directory separator is "/". Root is without preceding "/", e.g. "dir/file.xml"</param>
        /// <param name="fileMode">determines whether to open or to create the file</param>
        /// <param name="fileAccess">how to access the file, read, write or read and write</param>
        /// <param name="fileShare">how the file will be shared by processes</param>
        /// <param name="option">(optional) filesystem implementation specific token, such as session, security token or credential. Used for authorizing or facilitating the action.</param>
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
        public Stream Open(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare, IFileSystemToken option = null)
        {
            string concatenatedPath = System.IO.Path.Combine(AbsolutePath, path);
            string absolutePath = System.IO.Path.GetFullPath(concatenatedPath);
            if (!absolutePath.StartsWith(AbsolutePath, StringComparison.InvariantCulture)) throw new InvalidOperationException("Path cannot refer outside IFileSystem root");
            return new FileStream(absolutePath, fileMode, fileAccess, fileShare);
        }

        /// <summary>
        /// Create a directory, or multiple cascading directories.
        /// 
        /// If directory at <paramref name="path"/> already exists, then returns without exception.
        /// </summary>
        /// <param name="path">Relative path to file. Directory separator is "/". The root is without preceding slash "", e.g. "dir/dir2"</param>
        /// <param name="option">(optional) filesystem implementation specific token, such as session, security token or credential. Used for authorizing or facilitating the action.</param>
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
        public void CreateDirectory(string path, IFileSystemToken option = null)
        {
            string concatenatedPath = System.IO.Path.Combine(AbsolutePath, path);
            string absolutePath = System.IO.Path.GetFullPath(concatenatedPath);
            if (!absolutePath.StartsWith(AbsolutePath, StringComparison.InvariantCulture)) throw new InvalidOperationException("Path cannot refer outside IFileSystem root");
            Directory.CreateDirectory(absolutePath);
        }

        /// <summary>
        /// Concatenates <see cref="Path"/> to <paramref name="path"/> argument.
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
        /// <param name="throwOnError">if true and <paramref name="path"/> refers beyond root, throws <see cref="DirectoryNotFoundException"/></param>
        /// <return>path without trailing separator, or null if <paramref name="throwOnError"/> is true and refers beyond root</return>
        /// <exception cref="DirectoryNotFoundException">If <paramref name="path"/> refers over constructed root, e.g. ".." and <paramref name="throwOnError"/> is true</exception>
        string ConcatenateAndAssertPath(string path, out string concatenatedPath, out string absolutePath, bool throwOnError)
        {
            // Assert not null
            if (path == null) throw new ArgumentNullException(nameof(path));
            // Check settings for windows
            if (isWindows)
            {
                // "dir\" remove OS directory separator from end.
                if (path.EndsWith(osSeparator, StringComparison.InvariantCulture) || path.EndsWith("/", StringComparison.InvariantCulture)) path = path.Substring(0, path.Length - 1);
                // Concatenate root path from construction to argumen path
                concatenatedPath = Path == "" ? path : (Path.EndsWith("/", StringComparison.InvariantCulture) || Path.EndsWith("\\", StringComparison.InvariantCulture) || Path.EndsWith(":", StringComparison.InvariantCulture)) ? Path + path : Path + "/" + path;
                // Convert to absolute path. If path is drive-letter and root is "", add separator "\\".
                absolutePath = System.IO.Path.GetFullPath(/*c: ?*/isWindows && concatenatedPath.EndsWith(":", StringComparison.InvariantCulture) ? /*c:\*/concatenatedPath + osSeparator : /*c:\nnn*/concatenatedPath);
                // Assert that we are not browsing the parent of constructed path
                if (!absolutePath.StartsWith(AbsolutePath, StringComparison.InvariantCulture))
                {
                    if (throwOnError) throw new DirectoryNotFoundException("Path cannot refer outside IFileSystem root");
                    return null;
                }
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
                        absolutePath = System.IO.Path.GetFullPath(concatenatedPath);
                        // Assert that we are not browsing the parent of constructed path
                        if (!absolutePath.StartsWith(AbsolutePath, StringComparison.InvariantCulture)) throw new InvalidOperationException("Path cannot refer outside IFileSystem root");
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
                    concatenatedPath = path == "" || path == "/" ? Path : Path + (Path.EndsWith("/", StringComparison.InvariantCulture) ? "" : "/") + path;
                    // Convert to absolute path. If path is drive-letter and root is "", add separator "\\".
                    absolutePath = System.IO.Path.GetFullPath(concatenatedPath);
                    // Assert that we are not browsing the parent of constructed path
                    if (!absolutePath.StartsWith(AbsolutePath, StringComparison.InvariantCulture)) throw new InvalidOperationException("Path cannot refer outside IFileSystem root");
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
        /// Browse a directory for child entries.
        /// </summary>
        /// <param name="path">path to directory, "" is root, separator is "/"</param>
        /// <param name="option">(optional) filesystem implementation specific token, such as session, security token or credential. Used for authorizing or facilitating the action.</param>
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
        public IFileSystemEntry[] Browse(string path, IFileSystemToken option = null)
        {
            // Return OS-root, return drive letters.
            if (path == "" && Path == "") return BrowseRoot();
            //
            if (isWindows && path.StartsWith("/")) throw new DirectoryNotFoundException(path);

            // Concatenate paths and assert that path doesn't refer to parent of the constructed path
            string concatenatedPath, absolutePath;
            path = ConcatenateAndAssertPath(path, out concatenatedPath, out absolutePath, true);

            DirectoryInfo dir = new DirectoryInfo(absolutePath);
            if (dir.Exists)
            {
                string prefix = path.Length > 0 ? (path.EndsWith("/", StringComparison.InvariantCulture) ? path : path + "/") : null;
                if (IsOsRoot && path == "" || path == "/") prefix = "/";
                StructList24<IFileSystemEntry> list = new StructList24<IFileSystemEntry>();
                foreach (FileSystemInfo fsi in dir.EnumerateFileSystemInfos())
                {
                    if (fsi is DirectoryInfo di)
                    {
                        IFileSystemEntry e = new FileSystemEntryDirectory.WithAttributes(this, prefix + di.Name + "/", di.Name, di.LastWriteTimeUtcUnchecked(), di.LastAccessTimeUtcUnchecked(), di.Attributes, di.FullName);
                        list.Add(e);
                    }
                    else if (fsi is FileInfo _fi)
                    {
                        IFileSystemEntry e = new FileSystemEntryFile.WithAttributes(this, String.IsNullOrEmpty(prefix) ? _fi.Name : prefix + _fi.Name, _fi.Name, _fi.LastWriteTimeUtcUnchecked(), _fi.LastAccessTimeUtcUnchecked(), _fi.Length, _fi.Attributes, _fi.FullName);
                        list.Add(e);
                    }
                }
                return list.ToArray();
            }

            throw new DirectoryNotFoundException(path);
        }

        /// <summary>
        /// Get entry of a single file or directory.
        /// </summary>
        /// <param name="path">path to a directory or to a single file, "" is root, separator is "/"</param>
        /// <param name="option">(optional) filesystem implementation specific token, such as session, security token or credential. Used for authorizing or facilitating the action.</param>
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
        public IFileSystemEntry GetEntry(string path, IFileSystemToken option = null)
        {
            // Return OS-root, return drive letters.
            if (path == "") return rootEntry;

            // Concatenate paths and assert that path doesn't refer to parent of the constructed path
            string concatenatedPath, absolutePath;
            path = ConcatenateAndAssertPath(path, out concatenatedPath, out absolutePath, false);
            if (path == null) return null;

            DirectoryInfo dir = new DirectoryInfo(absolutePath);
            if (dir.Exists) return new FileSystemEntryDirectory.WithAttributes(this, path + "/", dir.Name, dir.LastWriteTimeUtcUnchecked(), dir.LastAccessTimeUtcUnchecked(), dir.Attributes, dir.FullName);

            FileInfo fi = new FileInfo(absolutePath);
            if (fi.Exists) return new FileSystemEntryFile.WithAttributes(this, path, fi.Name, fi.LastWriteTimeUtcUnchecked(), fi.LastAccessTimeUtcUnchecked(), fi.Length, fi.Attributes, fi.FullName);

            return null;
        }

        /// <summary>
        /// Browse root drive letters
        /// </summary>
        /// <returns></returns>
        protected IFileSystemEntry[] BrowseRoot()
        {
            DriveInfo[] driveInfos = DriveInfo.GetDrives();

            (DriveInfo, Match)[] matches = driveInfos.Select(di => (di, PathPattern.Match(di.Name))).ToArray();
            int windows = matches.Where(m => m.Item2.Groups["windows_driveletter"].Success).Count();
            int unix = matches.Where(m => m.Item2.Groups["unix_rooted_path"].Success).Count();

            // Reduce all "/mnt/xx" into single "/" entry.
            if (unix > 0)
            {
                DriveInfo rootInfo = null;
                foreach (DriveInfo _di in driveInfos) if (_di.Name == "/") { rootInfo = _di; break; }
                DirectoryInfo di = new DirectoryInfo("/");
                IFileSystemEntry e = new FileSystemEntryDrive(this, "/", "", di.LastWriteTimeUtcUnchecked(), DateTimeOffset.MinValue, DriveType.Fixed, rootInfo.AvailableFreeSpace, rootInfo.TotalSize, rootInfo.VolumeLabel, rootInfo.DriveFormat, true, di.FullName);
                return new IFileSystemEntry[] { e };
            }

            List<IFileSystemEntry> list = new List<IFileSystemEntry>(matches.Length);

            foreach ((DriveInfo driveInfo, Match m) in matches)
            {
                // Reduce all "/mnt/xx" into one "/" root.
                if (m.Groups["unix_rooted_path"].Success) continue;

                string path = m.Value;
                if (path.EndsWith(osSeparator)) path = path.Substring(0, path.Length - 1);
                string name = path;
                DirectoryInfo di = driveInfo.RootDirectory;
                path = path + "/";

                IFileSystemEntry e = new FileSystemEntryDrive(this, path, name, di.LastWriteTimeUtcUnchecked(), di.LastAccessTimeUtcUnchecked(), driveInfo.DriveType, driveInfo.AvailableFreeSpace, driveInfo.TotalSize, driveInfo.VolumeLabel, driveInfo.DriveFormat, driveInfo.IsReady, di.FullName);
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
        /// <param name="option">(optional) filesystem implementation specific token, such as session, security token or credential. Used for authorizing or facilitating the action.</param>
        /// <exception cref="FileNotFoundException">The specified path is invalid.</exception>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> contains invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support deleting files</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refered to a directory that wasn't empty and <paramref name="recursive"/> is false, or <paramref name="path"/> refers to non-file device</exception>
        public void Delete(string path, bool recursive = false, IFileSystemToken option = null)
        {
            // Concatenate paths and assert that path doesn't refer to parent of the constructed path
            string concatenatedPath, absolutePath;
            ConcatenateAndAssertPath(path, out concatenatedPath, out absolutePath, true);

            FileInfo fi = new FileInfo(absolutePath);
            if (fi.Exists) { fi.Delete(); return; }

            DirectoryInfo di = new DirectoryInfo(absolutePath);
            if (di.Exists) { di.Delete(recursive); return; }

            throw new FileNotFoundException(path);
        }

        /// <summary>
        /// Set <paramref name="fileAttribute"/> on <paramref name="path"/>.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="fileAttribute"></param>
        /// <param name="option">(optional) filesystem implementation specific token, such as session, security token or credential. Used for authorizing or facilitating the action.</param>
        /// <exception cref="FileNotFoundException"><paramref name="path"/> is not found</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="path"/> is invalid. For example, it's on an unmapped drive. Only thrown when setting the property value.</exception>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support browse</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"></exception>
        public void SetFileAttribute(string path, FileAttributes fileAttribute, IFileSystemToken option = null)
        {
            // Concatenate paths and assert that path doesn't refer to parent of the constructed path
            string concatenatedPath, absolutePath;
            ConcatenateAndAssertPath(path, out concatenatedPath, out absolutePath, true);

            FileInfo fi = new FileInfo(absolutePath);
            if (fi.Exists) { fi.Attributes = fileAttribute; return; }

            DirectoryInfo di = new DirectoryInfo(absolutePath);
            if (di.Exists) { di.Attributes = fileAttribute; return; }

            throw new FileNotFoundException(path);
        }

        /// <summary>
        /// Try to move/rename a file or directory.
        /// </summary>
        /// <param name="oldPath">old path of a file or directory</param>
        /// <param name="newPath">new path of a file or directory</param>
        /// <param name="option">(optional) filesystem implementation specific token, such as session, security token or credential. Used for authorizing or facilitating the action.</param>
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
        public void Move(string oldPath, string newPath, IFileSystemToken option = null)
        {
            // Concatenate paths and assert that path doesn't refer to parent of the constructed path
            string oldConcatenatedPath, oldAbsolutePath, newConcatenatedPath, newAbsolutePath;
            ConcatenateAndAssertPath(oldPath, out oldConcatenatedPath, out oldAbsolutePath, true);
            ConcatenateAndAssertPath(newPath, out newConcatenatedPath, out newAbsolutePath, true);

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
        /// <param name="eventDispatcher">(optional) </param>
        /// <param name="option">(optional) filesystem implementation specific token, such as session, security token or credential. Used for authorizing or facilitating the action.</param>
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
        public virtual IFileSystemObserver Observe(string filter, IObserver<IFileSystemEvent> observer, object state, IFileSystemEventDispatcher eventDispatcher = default, IFileSystemToken option = null)
        {
            // Parse filter
            GlobPatternInfo patternInfo = new GlobPatternInfo(filter);

            // Monitor drive letters
            if (patternInfo.Stem == "" && Path == "")
            {
                throw new NotImplementedException();
            }

            // Monitor single file (or dir, we don't know "dir")
            if (patternInfo.SuffixDepth == 0)
            {
                string concatenatedPath, absolutePath;
                string path = ConcatenateAndAssertPath(filter, out concatenatedPath, out absolutePath, true);

                // Create observer object
                FileObserver handle = new FileObserver(this, path, observer, state, eventDispatcher, AbsolutePath, absolutePath);
                // Send IFileSystemEventStart
                observer.OnNext( new FileSystemEventStart(handle, DateTimeOffset.UtcNow) );
                // Return handle
                return handle;
            }
            else
            // Has wildcards, e.g. "**/file.txt"
            {
                // Concatenate paths and assert that path doesn't refer to parent of the constructed path
                string concatenatedPath, absolutePathToPrefixPart;

                string relativePathToPrefixPartWithoutTrailingSeparator = ConcatenateAndAssertPath(patternInfo.Stem, out concatenatedPath, out absolutePathToPrefixPart, true);

                // Create observer object
                PatternObserver handle = new PatternObserver(this, observer, state, eventDispatcher, filter, AbsolutePath, relativePathToPrefixPartWithoutTrailingSeparator, absolutePathToPrefixPart, patternInfo.Suffix);
                // Send IFileSystemEventStart
                observer.OnNext(new FileSystemEventStart(handle, DateTimeOffset.UtcNow));
                // Return handle
                return handle;
            }

        }

        /// <summary>
        /// Single file observer.
        /// </summary>
        protected internal class FileObserver : FileSystemObserverBase
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

            /// <summary>Time when observing started.</summary>
            protected DateTimeOffset startTime = DateTimeOffset.UtcNow;

            /// <summary>
            /// Create observer for one file (or directory).
            /// </summary>
            /// <param name="filesystem">associated file system</param>
            /// <param name="relativePath">path to file as <see cref="IFileSystem"/> path</param>
            /// <param name="observer">observer for callbacks</param>
            /// <param name="state"></param>
            /// <param name="eventDispatcher"></param>
            /// <param name="filesystemRootAbsolutePath">Absolute path to filesystem root.</param>
            /// <param name="absolutePath">Absolute path to the file</param>
            /// <exception cref="DirectoryNotFoundException">If directory in <paramref name="filesystemRootAbsolutePath"/> is not found.</exception>
            public FileObserver(IFileSystem filesystem, string relativePath, IObserver<IFileSystemEvent> observer, object state, IFileSystemEventDispatcher eventDispatcher, string filesystemRootAbsolutePath, string absolutePath) : base(filesystem, relativePath, observer, state, eventDispatcher)
            {
                this.FileSystemRootAbsolutePath = filesystemRootAbsolutePath ?? throw new ArgumentNullException(nameof(filesystemRootAbsolutePath));
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
                // Get observer
                var _observer = Observer;
                // No observer
                if (_observer == null) return;
                // Get dispatcher
                var _dispatcher = Dispatcher ?? FileSystemEventDispatcher.Instance;

                // Disposed
                IFileSystem _filesystem = FileSystem;
                if (_filesystem == null) return;

                // Create event
                IFileSystemEvent @event = new FileSystemEventError(this, DateTimeOffset.UtcNow, e.GetException(), RelativePath);
                // Forward error as event object.
                _dispatcher.DispatchEvent(@event);
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
                IFileSystem _filesystem = FileSystem;
                if (_filesystem == null) return;

                // Forward event(s)
                DateTimeOffset time = DateTimeOffset.UtcNow;
                string path = ConvertPath(e.FullPath);

                // Event type
                WatcherChangeTypes type = e.ChangeType;
                StructList12<IFileSystemEvent> events = new StructList12<IFileSystemEvent>();
                // HasFlag has been optimized since .Net core 2.1 and does not box any more
                if (type.HasFlag(WatcherChangeTypes.Created) && path != null) events.Add(new FileSystemEventCreate(this, time, path));
                if (type.HasFlag(WatcherChangeTypes.Changed) && path != null) events.Add(new FileSystemEventChange(this, time, path));
                if (type.HasFlag(WatcherChangeTypes.Deleted) && path != null) events.Add(new FileSystemEventDelete(this, time, path));
                if (type.HasFlag(WatcherChangeTypes.Renamed) && e is RenamedEventArgs re)
                {
                    string oldPath = ConvertPath(re.OldFullPath);
                    // One path must be within watcher's interest
                    if (oldPath != null || path != null)
                    {
                        // Send event
                        events.Add(new FileSystemEventRename(this, time, oldPath, path));
                    }
                }

                // Dispatch events.
                ((FileSystemBase)this.FileSystem).DispatchEvents(ref events);
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
                    int length = absolutePath.Length > FileSystemRootAbsolutePath.Length && absolutePath[FileSystemRootAbsolutePath.Length] == System.IO.Path.DirectorySeparatorChar ? absolutePath.Length - FileSystemRootAbsolutePath.Length - 1 : absolutePath.Length - FileSystemRootAbsolutePath.Length;
                    string _relativePath = absolutePath.Substring(absolutePath.Length - length, length);
                    // Convert separator back-slash '\' into slash '/'.
                    if (System.IO.Path.DirectorySeparatorChar != '/') _relativePath = _relativePath.Replace(System.IO.Path.DirectorySeparatorChar, '/');
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
        protected internal class PatternObserver : FileSystemObserverBase
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
            protected FileSystemWatcher fileWatcher, directoryWatcher;

            /// <summary>
            /// Filter glob pattern
            /// </summary>
            public readonly Regex Pattern;

            /// <summary>Time when observing started.</summary>
            protected DateTimeOffset startTime = DateTimeOffset.UtcNow;

            /// <summary>
            /// Create observer for one file.
            /// </summary>
            /// <param name="filesystem">associated file system</param>
            /// <param name="observer">observer for callbacks</param>
            /// <param name="state"></param>
            /// <param name="eventDispatcher"></param>
            /// <param name="filterString">original filter string</param>
            /// <param name="filesystemRootAbsolutePath">Absolute path to <see cref="FileSystem"/> root.</param>
            /// <param name="relativePathToPrefixPartWithoutTrailingSeparator">prefix part of <paramref name="filterString"/>, for example "dir" if filter string is "dir/**"</param>
            /// <param name="absolutePathToPrefixPart">absolute path to prefix part of <paramref name="filterString"/>, for example "C:\Temp\Dir", if filter string is "dir/**" and <paramref name="filesystemRootAbsolutePath"/> is "C:\temp"</param>
            /// <param name="suffixPart">Suffix part of <paramref name="filterString"/>, for example "**" if filter string is "dir/**"</param>
            public PatternObserver(
                IFileSystem filesystem,
                IObserver<IFileSystemEvent> observer,
                object state,
                IFileSystemEventDispatcher eventDispatcher,
                string filterString,
                string filesystemRootAbsolutePath,
                string relativePathToPrefixPartWithoutTrailingSeparator,
                string absolutePathToPrefixPart,
                string suffixPart)
            : base(filesystem, filterString, observer, state, eventDispatcher)
            {
                this.FileSystemRootAbsolutePath = filesystemRootAbsolutePath ?? throw new ArgumentNullException(nameof(filesystemRootAbsolutePath));
                this.AbsolutePathToPrefixPart = absolutePathToPrefixPart ?? throw new ArgumentNullException(nameof(absolutePathToPrefixPart));
                this.RelativePathToPrefixPartWithoutTrailingSeparatorRelativePath = relativePathToPrefixPartWithoutTrailingSeparator ?? throw new ArgumentNullException(nameof(relativePathToPrefixPartWithoutTrailingSeparator));
                this.SuffixPart = suffixPart ?? throw new ArgumentNullException(nameof(suffixPart));
                this.Pattern = GlobPatternRegexFactory.Slash.CreateRegex(filterString ?? throw new ArgumentNullException(nameof(filterString)));
                fileWatcher = new FileSystemWatcher(AbsolutePathToPrefixPart);
                fileWatcher.IncludeSubdirectories = true;
                fileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size;
                fileWatcher.Error += OnError;
                fileWatcher.Changed += OnEvent;
                fileWatcher.Created += OnEvent;
                fileWatcher.Deleted += OnEvent;
                fileWatcher.Renamed += OnEvent;
                fileWatcher.EnableRaisingEvents = true;
                directoryWatcher = new FileSystemWatcher(AbsolutePathToPrefixPart);
                directoryWatcher.IncludeSubdirectories = true;
                directoryWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.DirectoryName | NotifyFilters.Size;
                directoryWatcher.Error += OnError;
                //directoryWatcher.Changed += OnEvent;
                directoryWatcher.Created += OnEvent;
                directoryWatcher.Deleted += OnEvent;
                directoryWatcher.Renamed += OnEvent;
                directoryWatcher.EnableRaisingEvents = true;
            }

            /// <summary>
            /// Handle (Forward) error event.
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            void OnError(object sender, ErrorEventArgs e)
            {
                // Get observer
                var _observer = Observer;
                // No observer
                if (_observer == null) return;
                // Get dispatcher
                var _dispatcher = Dispatcher ?? FileSystemEventDispatcher.Instance;

                // Disposed
                IFileSystem _filesystem = FileSystem;
                if (_filesystem == null) return;

                // Forward error as event object.
                IFileSystemEvent @event = new FileSystemEventError(this, DateTimeOffset.UtcNow, e.GetException(), null);
                // Forward error as event object.
                _dispatcher.DispatchEvent(@event);
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
                IFileSystem _filesystem = FileSystem;
                if (_filesystem == null) return;

                // Is file or directory
                bool isDirectory = sender == directoryWatcher || Directory.Exists(e.FullPath);

                // Forward event(s)
                DateTimeOffset time = DateTimeOffset.UtcNow;
                string path = ConvertPath(e.FullPath);
                if (isDirectory && path != "") path = path + "/";

                // Event type
                WatcherChangeTypes type = e.ChangeType;
                // HasFlag has been optimized since .Net core 2.1
                StructList12<IFileSystemEvent> events = new StructList12<IFileSystemEvent>();
                if (type.HasFlag(WatcherChangeTypes.Created) && path != null && (Pattern.IsMatch(path)/* || Pattern.IsMatch("/"+path)*/)) events.Add(new FileSystemEventCreate(this, time, path));
                if (type.HasFlag(WatcherChangeTypes.Changed) && path != null && (Pattern.IsMatch(path)/* || Pattern.IsMatch("/" + path)*/)) events.Add(new FileSystemEventChange(this, time, path));
                if (type.HasFlag(WatcherChangeTypes.Deleted) && path != null && (Pattern.IsMatch(path)/* || Pattern.IsMatch("/" + path)*/)) events.Add(new FileSystemEventDelete(this, time, path));
                if (type.HasFlag(WatcherChangeTypes.Renamed) && e is RenamedEventArgs re)
                {
                    string oldPath = ConvertPath(re.OldFullPath);
                    if (isDirectory && oldPath != "") oldPath = oldPath + "/";
                    // One path match match glob pattenr
                    if ((oldPath != null && (Pattern.IsMatch(oldPath)/*||Pattern.IsMatch("/"+oldPath)*/)) || (path != null && (Pattern.IsMatch(path)/*||Pattern.IsMatch("/"+path)*/)))
                    {
                        // Send event
                        events.Add(new FileSystemEventRename(this, time, oldPath, path));
                    }
                }
                // Dispatch events.
                ((FileSystemBase)this.FileSystem).DispatchEvents(ref events);
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
                    int length = absolutePath.Length > FileSystemRootAbsolutePath.Length && absolutePath[FileSystemRootAbsolutePath.Length] == System.IO.Path.DirectorySeparatorChar ? absolutePath.Length - FileSystemRootAbsolutePath.Length - 1 : absolutePath.Length - FileSystemRootAbsolutePath.Length;
                    string _relativePath = absolutePath.Substring(absolutePath.Length - length, length);
                    // Convert separator back-slash '\' into slash '/'.
                    if (System.IO.Path.DirectorySeparatorChar != '/') _relativePath = _relativePath.Replace(System.IO.Path.DirectorySeparatorChar, '/');
                    // Return
                    return _relativePath;
                }
                else
                {
                    return null;
                }
            }

            /// <summary>
            /// Dispose unmanaged resources
            /// </summary>
            /// <exception cref="AggregateException"></exception>
            protected override void InnerDispose/*Unmanaged*/(ref StructList4<Exception> errors)
            {
                base.InnerDispose/*Unmanaged*/(ref errors);

                var _fileWatcher = fileWatcher;
                if (_fileWatcher != null)
                {
                    fileWatcher = null;
                    try
                    {
                        _fileWatcher.Dispose();
                    }
                    catch (Exception e)
                    {
                        errors.Add(e);
                    }
                }

                var _directoryWatcher = directoryWatcher;
                if (_directoryWatcher != null)
                {
                    directoryWatcher = null;
                    try
                    {
                        _directoryWatcher.Dispose();
                    }
                    catch (Exception e)
                    {
                        errors.Add(e);
                    }
                }
            }
        }

        /// <summary>
        /// Invoke <paramref name="disposeAction"/> on the dispose of the object.
        /// 
        /// If parent object is disposed or being disposed, the disposable will be disposed immedialy.
        /// </summary>
        /// <param name="disposeAction"></param>
        /// <returns>self</returns>
        public FileSystem AddDisposeAction(Action<FileSystem> disposeAction)
        {
            // Argument error
            if (disposeAction == null) throw new ArgumentNullException(nameof(disposeAction));
            // Parent is disposed/ing
            if (IsDisposing) { disposeAction(this); return this; }
            // Adapt to IDisposable
            IDisposable disposable = new DisposeAction<FileSystem>(disposeAction, this);
            // Add to list
            lock (m_disposelist_lock) disposeList.Add(disposable);
            // Check parent again
            if (IsDisposing) { lock (m_disposelist_lock) disposeList.Remove(disposable); disposable.Dispose(); return this; }
            // OK
            return this;
        }

        /// <summary>
        /// Invoke <paramref name="disposeAction"/> on the dispose of the object.
        /// 
        /// If parent object is disposed or being disposed, the disposable will be disposed immedialy.
        /// </summary>
        /// <param name="disposeAction"></param>
        /// <param name="state"></param>
        /// <returns>self</returns>
        public FileSystem AddDisposeAction(Action<object> disposeAction, object state)
        {
            ((IDisposeList)this).AddDisposeAction(disposeAction, state);
            return this;
        }

        /// <summary>
        /// Add <paramref name="disposable"/> to list of objects to be disposed along with the system.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns>filesystem</returns>
        public FileSystem AddDisposable(object disposable)
        {
            ((IDisposeList)this).AddDisposable(disposable);
            return this;
        }

        /// <summary>
        /// Add <paramref name="disposables"/> to list of objects to be disposed along with the system.
        /// </summary>
        /// <param name="disposables"></param>
        /// <returns>filesystem</returns>
        public FileSystem AddDisposables(IEnumerable disposables)
        {
            ((IDisposeList)this).AddDisposables(disposables);
            return this;
        }

        /// <summary>
        /// Remove <paramref name="disposable"/> from dispose list.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns></returns>
        public FileSystem RemoveDisposable(object disposable)
        {
            ((IDisposeList)this).RemoveDisposable(disposable);
            return this;
        }

        /// <summary>
        /// Remove <paramref name="disposables"/> from dispose list.
        /// </summary>
        /// <param name="disposables"></param>
        /// <returns></returns>
        public FileSystem RemoveDisposables(IEnumerable disposables)
        {
            ((IDisposeList)this).RemoveDisposables(disposables);
            return this;
        }

        /// <summary>
        /// Print info
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => Path == "" ? "OS" : Path;
    }

    internal static class FileSystemInfoExtensions
    {
        /// <summary>
        /// Gets LastAccessTime, but captures exceptions and returns default value.
        /// </summary>
        /// <param name="fi"></param>
        /// <returns></returns>
        public static DateTimeOffset LastAccessTimeUtcUnchecked(this FileSystemInfo fi)
        {
            try
            {
                return fi.LastAccessTimeUtc;
            }
            catch (Exception)
            {
                return DateTimeOffset.MinValue;
            }
        }

        /// <summary>
        /// Gets LastWriteTime, but captures exceptions and returns default value.
        /// </summary>
        /// <param name="fi"></param>
        /// <returns></returns>
        public static DateTimeOffset LastWriteTimeUtcUnchecked(this FileSystemInfo fi)
        {
            try
            {
                return fi.LastWriteTimeUtc;
            }
            catch (Exception)
            {
                return DateTimeOffset.MinValue;
            }
        }

    }
}
