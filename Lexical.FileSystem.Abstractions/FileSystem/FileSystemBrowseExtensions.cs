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
    /// <summary>
    /// Extension methods for <see cref="IFileSystemBrowse"/>.
    /// </summary>
    public static partial class FileSystemBrowseExtensions
    {
        /// <summary>
        /// Test if <paramref name="filesystemOption"/> has Browse capability.
        /// </summary>
        /// <param name="filesystemOption"></param>
        /// <param name="defaultValue">Returned value if option is unspecified</param>
        /// <returns>true if has Browse capability</returns>
        public static bool CanBrowse(this IOption filesystemOption, bool defaultValue = false)
            => filesystemOption.AsOption<IBrowseOption>() is IBrowseOption browser ? browser.CanBrowse : defaultValue;

        /// <summary>
        /// Test if <paramref name="filesystemOption"/> has Exists capability.
        /// </summary>
        /// <param name="filesystemOption"></param>
        /// <param name="defaultValue">Returned value if option is unspecified</param>
        /// <returns>true if has Exists capability</returns>
        public static bool CanGetEntry(this IOption filesystemOption, bool defaultValue = false)
            => filesystemOption.AsOption<IBrowseOption>() is IBrowseOption browser ? browser.CanGetEntry : defaultValue;

        /// <summary>
        /// Browse a directory for child entries.
        /// 
        /// <paramref name="path"/> should end with directory separator character '/', for example "mydir/".
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path">path to a directory, "" is root, separator is "/"</param>
        /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
        /// <returns>
        ///     Returns a snapshot of file and directory entries.
        ///     Note, that the returned array be internally cached by the implementation, and therefore the caller must not modify the array.
        /// </returns>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support browse</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        public static IDirectoryContent Browse(this IFileSystem filesystem, string path, IOption option = null)
        {
            if (filesystem is IFileSystemBrowse browser) return browser.Browse(path, option);
            if (filesystem is IFileSystemBrowseAsync browserAsync) return browserAsync.BrowseAsync(path, option).Result;
            throw new NotSupportedException(nameof(Browse));
        }

        /// <summary>
        /// Browse a directory for child entries.
        /// 
        /// <paramref name="path"/> should end with directory separator character '/', for example "mydir/".
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path">path to a directory, "" is root, separator is "/"</param>
        /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
        /// <returns>
        ///     Returns a snapshot of file and directory entries.
        ///     Note, that the returned array be internally cached by the implementation, and therefore the caller must not modify the array.
        /// </returns>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support browse</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        public static Task<IDirectoryContent> BrowseAsync(this IFileSystem filesystem, string path, IOption option = null)
        {
            if (filesystem is IFileSystemBrowseAsync browserAsync) return browserAsync.BrowseAsync(path, option);
            if (filesystem is IFileSystemBrowse browser) return Task.Run(() => browser.Browse(path, option));
            throw new NotSupportedException(nameof(Browse));
        }

        /// <summary>
        /// Get entry of a single file or directory.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path">path to a directory or to a single file, "" is root, separator is "/"</param>
        /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
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
        public static IEntry GetEntry(this IFileSystem filesystem, string path, IOption option = null)
        {
            if (filesystem is IFileSystemBrowse browser) return browser.GetEntry(path, option);
            if (filesystem is IFileSystemBrowseAsync browserAsync) return browserAsync.GetEntryAsync(path, option).Result;
            throw new NotSupportedException(nameof(GetEntry));
        }

        /// <summary>
        /// Get entry of a single file or directory.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path">path to a directory or to a single file, "" is root, separator is "/"</param>
        /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
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
        public static Task<IEntry> GetEntryAsync(this IFileSystem filesystem, string path, IOption option = null)
        {
            if (filesystem is IFileSystemBrowseAsync browserAsync) return browserAsync.GetEntryAsync(path, option);
            if (filesystem is IFileSystemBrowse browser) return Task.Run(()=>browser.GetEntry(path, option));
            throw new NotSupportedException(nameof(GetEntry));
        }

        /// <summary>
        /// Tests if a file or directory exists.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path">path to a directory or to a single file, "" is root, separator is "/"</param>
        /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
        /// <returns>true if exists</returns>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support exists</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        public static bool Exists(this IFileSystem filesystem, string path, IOption option = null)
        {
            if (filesystem is IFileSystemBrowse browser) return browser.GetEntry(path, option) != null;
            if (filesystem is IFileSystemBrowseAsync browserAsync) return browserAsync.GetEntryAsync(path, option).Result != null;
            else throw new NotSupportedException(nameof(GetEntry));
        }

        /// <summary>
        /// Tests if a file or directory exists.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path">path to a directory or to a single file, "" is root, separator is "/"</param>
        /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
        /// <returns>true if exists</returns>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support exists</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        public static Task<bool> ExistsAsync(this IFileSystem filesystem, string path, IOption option = null)
        {
            if (filesystem is IFileSystemBrowseAsync browserAsync) return browserAsync.GetEntryAsync(path, option).ContinueWith(t => t.Result != null);
            if (filesystem is IFileSystemBrowse browser) return Task.Run(() => browser.GetEntry(path, option)).ContinueWith(t => t.Result != null);
            else throw new NotSupportedException(nameof(GetEntry));
        }

        /// <summary>
        /// If <see cref="IDirectoryContent.Exists"/> is false then throws <see cref="DirectoryNotFoundException"/>.
        /// </summary>
        /// <param name="browseResult"></param>
        /// <returns><paramref name="browseResult"/></returns>
        /// <exception cref="DirectoryNotFoundException">If <paramref name="browseResult"/> referes to non-existing path.</exception>
        public static IDirectoryContent AssertExists(this IDirectoryContent browseResult)
        {
            if (!browseResult.Exists) throw new DirectoryNotFoundException(browseResult.Path);
            return browseResult;
        }

        /// <summary>
        /// If <paramref name="entry"/> is null, then throws <see cref="FileNotFoundException"/>.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException">If <paramref name="entry"/> is null.</exception>
        public static IEntry AssertExists(this IEntry entry)
        {
            if (entry == null) throw new FileNotFoundException();
            return entry;
        }
    }

    /// <summary><see cref="IBrowseOption"/> operations.</summary>
    public class BrowseOptionOperations : IOptionFlattenOperation, IOptionIntersectionOperation, IOptionUnionOperation
    {
        /// <summary>The option type that this class has operations for.</summary>
        public Type OptionType => typeof(IBrowseOption);
        /// <summary>Flatten to simpler instance.</summary>
        public IOption Flatten(IOption o) => o is IBrowseOption b ? o is BrowseOption ? /*already flattened*/o : /*new instance*/new BrowseOption(b.CanBrowse, b.CanGetEntry) : throw new InvalidCastException($"{typeof(IBrowseOption)} expected.");
        /// <summary>Intersection of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IOption Intersection(IOption o1, IOption o2) => o1 is IBrowseOption b1 && o2 is IBrowseOption b2 ? new BrowseOption(b1.CanBrowse && b2.CanBrowse, b1.CanGetEntry && b2.CanGetEntry) : throw new InvalidCastException($"{typeof(IBrowseOption)} expected.");
        /// <summary>Union of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IOption Union(IOption o1, IOption o2) => o1 is IBrowseOption b1 && o2 is IBrowseOption b2 ? new BrowseOption(b1.CanBrowse || b2.CanBrowse, b1.CanGetEntry || b2.CanGetEntry) : throw new InvalidCastException($"{typeof(IBrowseOption)} expected.");
    }

    /// <summary>File system options for browse.</summary>
    public class BrowseOption : IBrowseOption
    {
        internal static IBrowseOption browse = new BrowseOption(true, true);
        internal static IBrowseOption noBrowse = new BrowseOption(false, false);
        /// <summary>Browse allowed.</summary>
        public static IOption Browse => browse;
        /// <summary>Browse not allowed.</summary>
        public static IOption NoBrowse => noBrowse;

        /// <summary>Has Browse capability.</summary>
        public bool CanBrowse { get; protected set; }
        /// <summary>Has GetEntry capability.</summary>
        public bool CanGetEntry { get; protected set; }

        /// <summary>Create file system options for browse.</summary>
        public BrowseOption(bool canBrowse, bool canGetEntry)
        {
            CanBrowse = canBrowse;
            CanGetEntry = canGetEntry;
        }

        /// <inheritdoc/>
        public override string ToString() => (CanBrowse ? "CanBrowse" : "NoBrowse") + "," + (CanGetEntry ? "CanGetEntry" : "NoGetEntry");
    }

}
