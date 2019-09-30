// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           29.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Internal;
using System;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Implementations to <see cref="IFileSystemOption"/>.
    /// 
    /// See classes:
    /// <list type="bullet">
    ///     <item><see cref="FileSystemOptionBrowse"/></item>
    ///     <item><see cref="FileSystemOptionCreateDirectory"/></item>
    ///     <item><see cref="FileSystemOptionDelete"/></item>
    ///     <item><see cref="FileSystemOptionMount"/></item>
    ///     <item><see cref="FileSystemOptionMove"/></item>
    ///     <item><see cref="FileSystemOptionObserve"/></item>
    ///     <item><see cref="FileSystemOptionOpen"/></item>
    ///     <item><see cref="FileSystemOptionMountPath"/></item>
    ///     <item><see cref="FileSystemOptionPath"/></item>
    /// </list>
    /// </summary>
    public partial class FileSystemOption : IFileSystemOption
    {
        internal static IFileSystemOption noOptions = new FileSystemOption();
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
        internal static IFileSystemOptionMountPath noMountPath = new FileSystemOptionMountPath(null);

        /// <summary>No options</summary>
        public static IFileSystemOption NoOptions => noOptions;

        // Path-level options //

        /// <summary>Path options.</summary>
        public static IFileSystemOption Path(FileSystemCaseSensitivity caseSensitivity, bool emptyDirectoryName) => new FileSystemOptionPath(caseSensitivity, emptyDirectoryName);
        /// <summary>Observe is allowed.</summary>
        public static IFileSystemOption Observe => observe;
        /// <summary>Observe is allowed, but cannt set event dispatch.</summary>
        public static IFileSystemOption ObserveCannotSetEventDispatch => observeCannotSetEventDispatch;
        /// <summary>Observe is not allowed</summary>
        public static IFileSystemOption NoObserve => noObserve;
        /// <summary>Open, Read, Write, Create</summary>
        public static IFileSystemOption OpenReadWriteCreate => openReadWriteCreate;
        /// <summary>Open, Read, Write</summary>
        public static IFileSystemOption OpenReadWrite => openReadWrite;
        /// <summary>Open, Read</summary>
        public static IFileSystemOption OpenRead => openRead;
        /// <summary>No access</summary>
        public static IFileSystemOption NoOpen => noOpen;
        /// <summary>Open options</summary>
        public static IFileSystemOption Open(bool canOpen, bool canRead, bool canWrite, bool canCreateFile) => new FileSystemOptionOpen(canOpen, canRead, canWrite, canCreateFile);
        /// <summary>Mount is allowed.</summary>
        public static IFileSystemOption Mount => mount;
        /// <summary>Mount is not allowed</summary>
        public static IFileSystemOption NoMount => noMount;
        /// <summary>Setting event dispatch is allowed.</summary>
        public static IFileSystemOption Move => move;
        /// <summary>Setting event dispatch not allowed</summary>
        public static IFileSystemOption NoMove => noMove;
        /// <summary>Browse allowed.</summary>
        public static IFileSystemOption Delete => delete;
        /// <summary>Browse not allowed.</summary>
        public static IFileSystemOption NoDelete => noDelete;
        /// <summary>CreateDirectory allowed.</summary>
        public static IFileSystemOption CreateDirectory => createDirectory;
        /// <summary>CreateDirectory not allowed.</summary>
        public static IFileSystemOption NoCreateDirectory => noCreateDirectory;
        /// <summary>Browse allowed.</summary>
        public static IFileSystemOption Browse => browse;
        /// <summary>Browse not allowed.</summary>
        public static IFileSystemOption NoBrowse => noBrowse;

        // FileSystem-level options //

        /// <summary>Create option for mount path. Used with <see cref="IFileSystemMountHandle"/></summary>
        public static IFileSystemOption MountPath(string mountPath) => new FileSystemOptionMountPath(mountPath);
        /// <summary>No mount path.</summary>
        public static IFileSystemOption NoMountPath => noMountPath;

        /// <summary>
        /// Create union of <paramref name="options"/>.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IFileSystemOption Union(params IFileSystemOption[] options) => FileSystemOptions.Union(options);

        /// <summary>
        /// Create intersection of <paramref name="options"/>.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IFileSystemOption Intersection(params IFileSystemOption[] options) => FileSystemOptions.Intersection(options);
    }

    /// <summary>Option for mount path. Used with <see cref="IFileSystemMountHandle"/></summary>
    public class FileSystemOptionMountPath : FileSystemOption, IFileSystemOptionMountPath
    {
        /// <summary>Mount path.</summary>
        public new String MountPath { get; protected set; }

        /// <summary>Create option for mount path. Used with <see cref="IFileSystemMountHandle"/></summary>
        public FileSystemOptionMountPath(string mountPath)
        {
            MountPath = mountPath;;
        }
    }

    /// <summary>Path related options</summary>
    public class FileSystemOptionPath : FileSystemOption, IFileSystemOptionPath
    {
        /// <summary>Case sensitivity</summary>
        public FileSystemCaseSensitivity CaseSensitivity { get; protected set; }
        /// <summary>Filesystem allows empty string "" directory names.</summary>
        public bool EmptyDirectoryName { get; protected set; }

        /// <summary>Create path related options</summary>
        public FileSystemOptionPath(FileSystemCaseSensitivity caseSensitivity, bool emptyDirectoryName)
        {
            this.CaseSensitivity = caseSensitivity;
            this.EmptyDirectoryName = emptyDirectoryName;
        }
    }

    /// <summary>File system options for browse.</summary>
    public class FileSystemOptionBrowse : FileSystemOption, IFileSystemOptionBrowse
    {
        /// <summary>Has Browse capability.</summary>
        public bool CanBrowse { get; protected set; }
        /// <summary>Has GetEntry capability.</summary>
        public bool CanGetEntry { get; protected set; }

        /// <summary>Create file system options for browse.</summary>
        public FileSystemOptionBrowse(bool canBrowse, bool canGetEntry)
        {
            CanBrowse = canBrowse;
            CanGetEntry = canGetEntry;
        }
    }

    /// <summary>File system option for creating directories.</summary>
    public class FileSystemOptionCreateDirectory : FileSystemOption, IFileSystemOptionCreateDirectory
    {
        /// <summary>Has CreateDirectory capability.</summary>
        public bool CanCreateDirectory { get; protected set; }

        /// <summary>Create file system option for creating directories.</summary>
        public FileSystemOptionCreateDirectory(bool canCreateDirectory)
        {
            CanCreateDirectory = canCreateDirectory;
        }
    }

    /// <summary>File system option for deleting files and directories.</summary>
    public class FileSystemOptionDelete : FileSystemOption, IFileSystemOptionDelete
    {
        /// <summary>Has Delete capability.</summary>
        public bool CanDelete { get; protected set; }

        /// <summary>Create file system option for deleting files and directories.</summary>
        public FileSystemOptionDelete(bool canDelete)
        {
            CanDelete = canDelete;
        }
    }

    /// <summary>File system option for move/rename.</summary>
    public class FileSystemOptionMove : FileSystemOption, IFileSystemOptionMove
    {
        /// <summary>Has Move capability.</summary>
        public bool CanMove { get; protected set; }

        /// <summary>Create file system option for move/rename.</summary>
        public FileSystemOptionMove(bool canMove)
        {
            CanMove = canMove;
        }
    }

    /// <summary>File system option for mount capabilities.</summary>
    public class FileSystemOptionMount : FileSystemOption, IFileSystemOptionMount
    {
        /// <summary>Is filesystem capable of creating mountpoints.</summary>
        public bool CanCreateMountPoint { get; protected set; }
        /// <summary>Is filesystem capable of listing mountpoints.</summary>
        public bool CanListMountPoints { get; protected set; }
        /// <summary>Is filesystem capable of getting mountpoint entry.</summary>
        public bool CanGetMountPoint { get; protected set; }

        /// <summary>Create file system option for mount capabilities.</summary>
        public FileSystemOptionMount(bool canCreateMountpoint, bool canListMountpoints, bool canGetMountpoint)
        {
            CanCreateMountPoint = canCreateMountpoint;
            CanListMountPoints = canListMountpoints;
            CanGetMountPoint = canGetMountpoint;
        }
    }


    /// <summary>File system options for open, create, read and write files.</summary>
    public class FileSystemOptionOpen : FileSystemOption, IFileSystemOptionOpen
    {
        /// <summary>Can open file</summary>
        public bool CanOpen { get; protected set; }
        /// <summary>Can open file for reading(</summary>
        public bool CanRead { get; protected set; }
        /// <summary>Can open file for writing.</summary>
        public bool CanWrite { get; protected set; }
        /// <summary>Can open and create file.</summary>
        public bool CanCreateFile { get; protected set; }

        /// <summary>Create file system options for open, create, read and write files.</summary>
        public FileSystemOptionOpen(bool canOpen, bool canRead, bool canWrite, bool canCreateFile)
        {
            CanOpen = canOpen;
            CanRead = canRead;
            CanWrite = canWrite;
            CanCreateFile = canCreateFile;
        }
    }

    /// <summary>File system option for observe.</summary>
    public class FileSystemOptionObserve : FileSystemOption, IFileSystemOptionObserve
    {
        /// <summary>Has Observe capability.</summary>
        public bool CanObserve { get; protected set; }
        /// <summary>Has SetEventDispatcher capability.</summary>
        public bool CanSetEventDispatcher { get; protected set; }

        /// <summary>Create file system option for observe.</summary>
        public FileSystemOptionObserve(bool canObserve, bool canSetEventDispatcher)
        {
            CanObserve = canObserve;
            CanSetEventDispatcher = canSetEventDispatcher;
        }
    }

    /// <summary>
    /// Implementations to <see cref="IFileSystemOption"/>.
    /// </summary>
    public class FileSystemOptions : FileSystemOption, IFileSystemOptionOpen, IFileSystemOptionBrowse, IFileSystemOptionCreateDirectory,
        IFileSystemOptionDelete, IFileSystemOptionMount, IFileSystemOptionMove,
        IFileSystemOptionObserve, IFileSystemOptionMountPath, IFileSystemOptionPath
    {
        /// <inheritdoc/>
        public bool CanBrowse { get; protected set; }
        /// <inheritdoc/>
        public bool CanGetEntry { get; protected set; }
        /// <inheritdoc/>
        public bool CanCreateDirectory { get; protected set; }
        /// <inheritdoc/>
        public bool CanMove { get; protected set; }
        /// <inheritdoc/>
        public bool CanDelete { get; protected set; }
        /// <inheritdoc/>
        public bool CanSetEventDispatcher { get; protected set; }
        /// <inheritdoc/>
        public bool CanCreateMountPoint { get; protected set; }
        /// <inheritdoc/>
        public bool CanListMountPoints { get; protected set; }
        /// <inheritdoc/>
        public bool CanGetMountPoint { get; protected set; }
        /// <inheritdoc/>
        public FileSystemCaseSensitivity CaseSensitivity { get; protected set; }
        /// <inheritdoc/>
        public bool EmptyDirectoryName { get; protected set; }
        /// <inheritdoc/>
        public bool CanObserve { get; protected set; }
        /// <inheritdoc/>
        public new string MountPath { get; protected set; }
        /// <inheritdoc/>
        public bool CanOpen { get; protected set; }
        /// <inheritdoc/>
        public bool CanRead { get; protected set; }
        /// <inheritdoc/>
        public bool CanWrite { get; protected set; }
        /// <inheritdoc/>
        public bool CanCreateFile { get; protected set; }

        /// <summary>
        /// Create union of <paramref name="options"/>.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public new static IFileSystemOption Union(params IFileSystemOption[] options)
        {
            FileSystemOptions r = new FileSystemOptions();
            foreach (IFileSystemOption option in options)
            {
                if (option is IFileSystemOptionBrowse browse) { r.CanBrowse |= browse.CanBrowse; r.CanGetEntry |= browse.CanGetEntry; }
                if (option is IFileSystemOptionCreateDirectory cd) { r.CanCreateDirectory |= cd.CanCreateDirectory; }
                if (option is IFileSystemOptionDelete del) { r.CanDelete |= del.CanDelete; }
                if (option is IFileSystemOptionMount mt) { r.CanCreateMountPoint |= mt.CanCreateMountPoint; r.CanGetMountPoint |= mt.CanGetMountPoint; r.CanListMountPoints |= mt.CanListMountPoints; }
                if (option is IFileSystemOptionMove mv) { r.CanMove |= mv.CanMove; }
                if (option is IFileSystemOptionOpen op) { r.CanOpen |= op.CanOpen; r.CanRead |= op.CanRead; r.CanWrite |= op.CanWrite; r.CanCreateFile |= op.CanCreateFile; }
                if (option is IFileSystemOptionObserve ob) { r.CanObserve |= ob.CanObserve; r.CanSetEventDispatcher |= ob.CanSetEventDispatcher; }
                if (option is IFileSystemOptionMountPath mp) { if (String.IsNullOrEmpty(r.MountPath) && mp.MountPath != null) r.MountPath = mp.MountPath; }
                if (option is IFileSystemOptionPath pa) { r.CaseSensitivity |= pa.CaseSensitivity; r.EmptyDirectoryName |= pa.EmptyDirectoryName; }
            }
            return r;
        }

        /// <summary>
        /// Create intersection of <paramref name="options"/>.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public new static IFileSystemOption Intersection(params IFileSystemOption[] options)
        {
            FileSystemOptions r = new FileSystemOptions();
            int iBrowse = 0, iCreateDirectory = 0, iDelete = 0, iMount = 0, iMove = 0, iObserve = 0, iRoot = 0, iPath = 0, iOpen = 0;
            foreach (IFileSystemOption option in options)
            {
                if (option is IFileSystemOptionBrowse browse)
                {
                    if (iBrowse++ == 0) { r.CanBrowse = browse.CanBrowse; r.CanGetEntry = browse.CanGetEntry; }
                    else { r.CanBrowse &= browse.CanBrowse; r.CanGetEntry &= browse.CanGetEntry; }
                }
                if (option is IFileSystemOptionCreateDirectory cd)
                {
                    if (iCreateDirectory++ == 0) r.CanCreateDirectory = cd.CanCreateDirectory;
                    else r.CanCreateDirectory &= cd.CanCreateDirectory;
                }
                if (option is IFileSystemOptionDelete del)
                {
                    if (iDelete++ == 0) r.CanDelete = del.CanDelete;
                    else r.CanDelete &= del.CanDelete;
                }
                if (option is IFileSystemOptionMount mt)
                {
                    if (iMount++ == 0) { r.CanCreateMountPoint = mt.CanCreateMountPoint; r.CanGetMountPoint = mt.CanGetMountPoint; r.CanListMountPoints = mt.CanListMountPoints; }
                    else { r.CanCreateMountPoint &= mt.CanCreateMountPoint; r.CanGetMountPoint &= mt.CanGetMountPoint; r.CanListMountPoints &= mt.CanListMountPoints; }
                }
                if (option is IFileSystemOptionMove mv)
                {
                    if (iMove++ == 0) r.CanMove = mv.CanMove;
                    else r.CanMove &= mv.CanMove;
                }
                if (option is IFileSystemOptionOpen op)
                {
                    if (iOpen++ == 0) { r.CanOpen = op.CanOpen; r.CanRead = op.CanRead; r.CanWrite = op.CanWrite; r.CanCreateFile = op.CanCreateFile; }
                    else { r.CanOpen &= op.CanOpen; r.CanRead &= op.CanRead; r.CanWrite &= op.CanWrite; r.CanCreateFile &= op.CanCreateFile; }
                }
                if (option is IFileSystemOptionObserve ob)
                {
                    if (iObserve++ == 0) { r.CanObserve = ob.CanObserve; r.CanSetEventDispatcher = ob.CanSetEventDispatcher; }
                    else { r.CanObserve &= ob.CanObserve; r.CanSetEventDispatcher &= ob.CanSetEventDispatcher; }
                    }
                if (option is IFileSystemOptionMountPath mp)
                {
                    if (iRoot++ == 0) r.MountPath = mp.MountPath;
                }
                if (option is IFileSystemOptionPath pa)
                {
                    if (iPath++ == 0) { r.CaseSensitivity = pa.CaseSensitivity; r.EmptyDirectoryName = pa.EmptyDirectoryName; }
                    else { r.CaseSensitivity &= pa.CaseSensitivity; r.EmptyDirectoryName &= pa.EmptyDirectoryName; }
                }
            }
            return r;
        }

    }


}
