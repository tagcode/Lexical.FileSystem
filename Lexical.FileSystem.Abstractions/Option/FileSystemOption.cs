// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           29.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------

namespace Lexical.FileSystem
{
    /// <summary>
    /// FileSystem option that denies write and modification operations.
    /// </summary>
    public class FileSystemOptionReadOnly : IFileSystemOptionCreateDirectory, IFileSystemOptionDelete, IFileSystemOptionMove, IFileSystemOptionOpen, IFileSystemOptionMount, IFileSystemOptionBrowse, IFileSystemOptionObserve
    {
        /// <inheritdoc/>
        public bool CanOpen => true;
        /// <inheritdoc/>
        public bool CanRead => true;
        /// <inheritdoc/>
        public bool CanWrite => false;
        /// <inheritdoc/>
        public bool CanCreateFile => false;
        /// <inheritdoc/>
        public bool CanMove => false;
        /// <inheritdoc/>
        public bool CanDelete => false;
        /// <inheritdoc/>
        public bool CanCreateDirectory => false;
        /// <inheritdoc/>
        public bool CanMount => false;
        /// <inheritdoc/>
        public bool CanUnmount => false;
        /// <inheritdoc/>
        public bool CanListMounts => true;
        /// <inheritdoc/>
        public bool CanBrowse => true;
        /// <inheritdoc/>
        public bool CanGetEntry => true;
        /// <inheritdoc/>
        public bool CanObserve => true;
        /// <inheritdoc/>
        public bool CanSetEventDispatcher => false;
    }

    /// <summary>No options of <see cref="IFileSystemOption"/>.</summary>
    public class FileSystemOptionNone : IFileSystemOption
    {
        /// <summary>No options</summary>
        static IFileSystemOption noOptions = new FileSystemOptionNone();
        /// <summary>No options</summary>
        public static IFileSystemOption NoOptions => noOptions;
    }

}

namespace Lexical.FileSystem
{
    public partial class FileSystemOption : IFileSystemOption
    {
        internal static IFileSystemOption noOptions = new FileSystemOptionNone();
        internal static IFileSystemOptionBrowse browse = new FileSystemOptionBrowse(true, true);
        internal static IFileSystemOptionBrowse noBrowse = new FileSystemOptionBrowse(false, false);
        internal static IFileSystemOptionCreateDirectory createDirectory = new FileSystemOptionCreateDirectory(true);
        internal static IFileSystemOptionCreateDirectory noCreateDirectory = new FileSystemOptionCreateDirectory(false);
        internal static IFileSystemOptionDelete delete = new FileSystemOptionDelete(true);
        internal static IFileSystemOptionDelete noDelete = new FileSystemOptionDelete(false);
        internal static IFileSystemOptionMove move = new FileSystemOptionMove(true);
        internal static IFileSystemOptionMove noMove = new FileSystemOptionMove(false);
        internal static IFileSystemOptionMount mount = new FileSystemOptionMount(true, true, true);
        internal static IFileSystemOptionMount noMount = new FileSystemOptionMount(false, false, false);
        internal static IFileSystemOptionOpen openReadWriteCreate = new FileSystemOptionOpen(true, true, true, true);
        internal static IFileSystemOptionOpen openReadWrite = new FileSystemOptionOpen(true, true, true, false);
        internal static IFileSystemOptionOpen openRead = new FileSystemOptionOpen(true, true, false, false);
        internal static IFileSystemOptionOpen noOpen = new FileSystemOptionOpen(false, false, false, false);
        internal static IFileSystemOptionObserve observe = new FileSystemOptionObserve(true, true);
        internal static IFileSystemOptionObserve observeCannotSetEventDispatch = new FileSystemOptionObserve(true, false);
        internal static IFileSystemOptionObserve noObserve = new FileSystemOptionObserve(false, false);
        internal static IFileSystemOptionSubPath noSubPath = new FileSystemOptionSubPath(null);
        internal static IFileSystemOption _readonly = new FileSystemOptionReadOnly();
    }
}
