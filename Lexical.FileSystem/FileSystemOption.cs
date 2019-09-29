// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           29.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Implementations to <see cref="IFileSystemOption"/>.
    /// 
    /// See sub-classes:
    /// <list type="bullet">
    ///     <item><see cref="FileSystemOptionRoot"/></item>
    ///     <item><see cref="FileSystemOptionPath"/></item>
    ///     <item><see cref="FileSystemOptionBrowse"/></item>
    ///     <item><see cref="FileSystemOptionCreateDirectory"/></item>
    ///     <item><see cref="FileSystemOptionDelete"/></item>
    ///     <item><see cref="FileSystemOptionEventDispatch"/></item>
    ///     <item><see cref="FileSystemOptionMount"/></item>
    ///     <item><see cref="FileSystemOptionMove"/></item>
    ///     <item><see cref="FileSystemOptionObserve"/></item>
    ///     <item><see cref="FileSystemOptionOpen"/></item>
    /// </list>
    /// </summary>
    public class FileSystemOption
    {
    }

    /// <summary>Option for root path.</summary>
    public class FileSystemOptionRoot : IFileSystemOption
    {
        /// <summary>Root path within filesystem.</summary>
        public String Root { get; protected set; }

        /// <summary>Create root path option.</summary>
        public FileSystemOptionRoot(string root)
        {
            Root = root ?? throw new ArgumentNullException(nameof(root));
        }
    }

    /// <summary>Path related options</summary>
    public class FileSystemOptionPath : IFileSystemOptionPath
    {
        /// <summary>Some or all files use case-sensitive filenames. Note, if neither <see cref="CaseSensitive"/> or <see cref="CaseInsensitive"/> then sensitivity is not consistent or is unknown. If both are set, then sensitivity is inconsistent.</summary>
        public bool CaseSensitive { get; protected set; }
        /// <summary>Some or all files use case-insensitive filenames. Note, if neither <see cref="CaseSensitive"/> or <see cref="CaseInsensitive"/> then sensitivity is not consistent or is unknown. If both are set, then sensitivity is inconsistent.</summary>
        public bool CaseInsensitive { get; protected set; }
        /// <summary>Filesystem allows empty string "" directory names.</summary>
        public bool EmptyDirectoryName { get; protected set; }

        /// <summary>Create path related options</summary>
        public FileSystemOptionPath(bool caseSensitive, bool caseInsensitive, bool emptyDirectoryName)
        {
            CaseSensitive = caseSensitive;
            CaseInsensitive = caseInsensitive;
            EmptyDirectoryName = emptyDirectoryName;
        }
    }

    /// <summary>File system options for browse.</summary>
    public class FileSystemOptionBrowse : IFileSystemOptionBrowse
    {
        private static IFileSystemOptionBrowse browse = new FileSystemOptionBrowse(true, true);
        private static IFileSystemOptionBrowse noBrowse = new FileSystemOptionBrowse(false, false);
        /// <summary>Browse allowed.</summary>
        public static IFileSystemOptionBrowse Browse => browse;
        /// <summary>Browse not allowed.</summary>
        public static IFileSystemOptionBrowse NoBrowse => noBrowse;

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
    public class FileSystemOptionCreateDirectory : IFileSystemOptionCreateDirectory
    {
        private static IFileSystemOptionCreateDirectory createDirectory = new FileSystemOptionCreateDirectory(true);
        private static IFileSystemOptionCreateDirectory noCreateDirectory = new FileSystemOptionCreateDirectory(false);
        /// <summary>CreateDirectory allowed.</summary>
        public static IFileSystemOptionCreateDirectory CreateDirectory => createDirectory;
        /// <summary>CreateDirectory not allowed.</summary>
        public static IFileSystemOptionCreateDirectory NoCreateDirectory => noCreateDirectory;

        /// <summary>Has CreateDirectory capability.</summary>
        public bool CanCreateDirectory { get; protected set; }

        /// <summary>Create file system option for creating directories.</summary>
        public FileSystemOptionCreateDirectory(bool canCreateDirectory)
        {
            CanCreateDirectory = canCreateDirectory;
        }
    }

    /// <summary>File system option for deleting files and directories.</summary>
    public class FileSystemOptionDelete : IFileSystemOptionDelete
    {
        private static IFileSystemOptionDelete delete = new FileSystemOptionDelete(true);
        private static IFileSystemOptionDelete noDelete = new FileSystemOptionDelete(false);
        /// <summary>Browse allowed.</summary>
        public static IFileSystemOptionDelete Delete => delete;
        /// <summary>Browse not allowed.</summary>
        public static IFileSystemOptionDelete NoDelete => noDelete;

        /// <summary>Has Delete capability.</summary>
        public bool CanDelete { get; protected set; }

        /// <summary>Create file system option for deleting files and directories.</summary>
        public FileSystemOptionDelete(bool canDelete)
        {
            CanDelete = canDelete;
        }
    }

    /// <summary>Filesystem option for SetEventDispatcher capability.</summary>
    public class FileSystemOptionEventDispatch : IFileSystemOptionEventDispatch
    {
        private static IFileSystemOptionEventDispatch eventDispatch = new FileSystemOptionEventDispatch(true);
        private static IFileSystemOptionEventDispatch noEventDispatch = new FileSystemOptionEventDispatch(false);
        /// <summary>Setting event dispatch is allowed.</summary>
        public static IFileSystemOptionEventDispatch EventDispatch => eventDispatch;
        /// <summary>Setting event dispatch not allowed</summary>
        public static IFileSystemOptionEventDispatch NoEventDispatch => noEventDispatch;

        /// <summary>Has SetEventDispatcher capability.</summary>
        public bool CanSetEventDispatcher { get; protected set; }

        /// <summary>Create filesystem option for SetEventDispatcher capability.</summary>
        public FileSystemOptionEventDispatch(bool canSetEventDispatcher)
        {
            CanSetEventDispatcher = canSetEventDispatcher;
        }
    }

    /// <summary>File system option for move/rename.</summary>
    public class FileSystemOptionMove : IFileSystemOptionMove
    {
        private static IFileSystemOptionMove move = new FileSystemOptionMove(true);
        private static IFileSystemOptionMove noMove = new FileSystemOptionMove(false);
        /// <summary>Setting event dispatch is allowed.</summary>
        public static IFileSystemOptionMove Move => move;
        /// <summary>Setting event dispatch not allowed</summary>
        public static IFileSystemOptionMove NoMove => noMove;

        /// <summary>Has Move capability.</summary>
        public bool CanMove { get; protected set; }

        /// <summary>Create file system option for move/rename.</summary>
        public FileSystemOptionMove(bool canMove)
        {
            CanMove = canMove;
        }
    }

    /// <summary>File system option for mount capabilities.</summary>
    public class FileSystemOptionMount : IFileSystemOptionMount
    {
        private static IFileSystemOptionMount mount = new FileSystemOptionMount(true, true, true);
        private static IFileSystemOptionMount noMount = new FileSystemOptionMount(false, false, false);
        /// <summary>Mount is allowed.</summary>
        public static IFileSystemOptionMount Mount => mount;
        /// <summary>Mount is not allowed</summary>
        public static IFileSystemOptionMount NoMount => noMount;

        /// <summary>Is filesystem capable of creating mountpoints.</summary>
        public bool CanCreateMountpoint { get; protected set; }
        /// <summary>Is filesystem capable of listing mountpoints.</summary>
        public bool CanListMountpoints { get; protected set; }
        /// <summary>Is filesystem capable of getting mountpoint entry.</summary>
        public bool CanGetMountpoint { get; protected set; }

        /// <summary>Create file system option for mount capabilities.</summary>
        public FileSystemOptionMount(bool canCreateMountpoint, bool canListMountpoints, bool canGetMountpoint)
        {
            CanCreateMountpoint = canCreateMountpoint;
            CanListMountpoints = canListMountpoints;
            CanGetMountpoint = canGetMountpoint;
        }
    }


    /// <summary>File system options for open, create, read and write files.</summary>
    public class FileSystemOptionOpen : IFileSystemOptionOpen
    {
        private static IFileSystemOptionOpen openReadWriteCreate = new FileSystemOptionOpen(true, true, true, true);
        private static IFileSystemOptionOpen openReadWrite = new FileSystemOptionOpen(true, true, true, false);
        private static IFileSystemOptionOpen openRead = new FileSystemOptionOpen(true, true, false, false);
        private static IFileSystemOptionOpen noOpen = new FileSystemOptionOpen(false, false, false, false);

        /// <summary>Open, Read, Write, Create</summary>
        public static IFileSystemOptionOpen OpenReadWriteCreate => openReadWriteCreate;
        /// <summary>Open, Read, Write</summary>
        public static IFileSystemOptionOpen OpenReadWrite => openReadWrite;
        /// <summary>Open, Read</summary>
        public static IFileSystemOptionOpen OpenRead => openRead;
        /// <summary>No access</summary>
        public static IFileSystemOptionOpen NoOpen => noOpen;

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
    public class FileSystemOptionObserve : IFileSystemOptionObserve
    {
        private static IFileSystemOptionObserve observe = new FileSystemOptionObserve(true);
        private static IFileSystemOptionObserve noObserve = new FileSystemOptionObserve(false);
        /// <summary>Observe is allowed.</summary>
        public static IFileSystemOptionObserve Observe => observe;
        /// <summary>Observe is not allowed</summary>
        public static IFileSystemOptionObserve NoObserve => noObserve;

        /// <summary>Has Observe capability.</summary>
        public bool CanObserve { get; protected set; }

        /// <summary>Create file system option for observe.</summary>
        public FileSystemOptionObserve(bool canObserve)
        {
            CanObserve = canObserve;
        }
    }

}
