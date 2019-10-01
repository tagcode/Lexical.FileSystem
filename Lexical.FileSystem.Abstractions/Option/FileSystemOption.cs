// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           29.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Option;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Lexical.FileSystem.Option
{
    /// <summary>
    /// Base class for implementations of <see cref="IFileSystemOption"/>.
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
    public partial class FileSystemOptionBase : IFileSystemOption
    {
    }

    /// <summary>Option for mount path. Used with <see cref="IFileSystemMountAssignment"/></summary>
    public class FileSystemOptionMountPath : FileSystemOptionBase, IFileSystemOptionMountPath
    {
        /// <summary>Mount path.</summary>
        public String MountPath { get; protected set; }

        /// <summary>Create option for mount path. Used with <see cref="IFileSystemMountAssignment"/></summary>
        public FileSystemOptionMountPath(string mountPath)
        {
            MountPath = mountPath; ;
        }
    }

    /// <summary>Path related options</summary>
    public class FileSystemOptionPath : FileSystemOptionBase, IFileSystemOptionPath
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
    public class FileSystemOptionBrowse : FileSystemOptionBase, IFileSystemOptionBrowse
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
    public class FileSystemOptionCreateDirectory : FileSystemOptionBase, IFileSystemOptionCreateDirectory
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
    public class FileSystemOptionDelete : FileSystemOptionBase, IFileSystemOptionDelete
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
    public class FileSystemOptionMove : FileSystemOptionBase, IFileSystemOptionMove
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
    public class FileSystemOptionMount : FileSystemOptionBase, IFileSystemOptionMount
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
    public class FileSystemOptionOpen : FileSystemOptionBase, IFileSystemOptionOpen
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
    public class FileSystemOptionObserve : FileSystemOptionBase, IFileSystemOptionObserve
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
    public class FileSystemOptionComposition : FileSystemOptionBase, IFileSystemOptionAdaptable
    {
        /// <summary>
        /// Join operation
        /// </summary>
        public enum Op
        {
            /// <summary>
            /// Uses union of option instances
            /// </summary>
            Union,
            /// <summary>
            /// Uses intersection of option instances
            /// </summary>
            Intersection,
            /// <summary>
            /// Uses first option instance
            /// </summary>
            First,
            /// <summary>
            /// Uses last option instance
            /// </summary>
            Last
        }

        /// <summary>
        /// Options sorted by type.
        /// </summary>
        protected Dictionary<Type, IFileSystemOption> byType = new Dictionary<Type, IFileSystemOption>();

        /// <summary>
        /// Create composition of filesystem options.
        /// </summary>
        /// <param name="op">Join operation of same option intefaces</param>
        /// <param name="options">options to compose</param>
        public FileSystemOptionComposition(Op op, IEnumerable<IFileSystemOption> options)
        {
            foreach (IFileSystemOption option in options)
            {
                // IFileSystemOption
                foreach (Type type in option.GetType().GetInterfaces())
                    if (typeof(IFileSystemOption).IsAssignableFrom(type) && !typeof(IFileSystemOption).Equals(type))
                        Add(op, type, option);
                // IFileSystemOptionAdaptable
                if (option is IFileSystemOptionAdaptable adaptable)
                    foreach (KeyValuePair<Type, IFileSystemOption> line in adaptable)
                        Add(op, line.Key, line.Value);
            }
            Flatten();
        }

        /// <summary>
        /// Create composition of filesystem options.
        /// </summary>
        /// <param name="op">Join operation of same option intefaces</param>
        /// <param name="options">options to compose</param>
        public FileSystemOptionComposition(Op op, params IFileSystemOption[] options) : this(op, (IEnumerable<FileSystemOption>)options) { }

        /// <summary>
        /// Add option to the composition.
        /// </summary>
        /// <param name="op">Join method</param>
        /// <param name="type">Interface type to add as</param>
        /// <param name="option">Option instance to add to composition</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="FileSystemExceptionOptionOperationNotSupported"></exception>
        protected virtual void Add(Op op, Type type, IFileSystemOption option)
        {
            IFileSystemOption prev;
            // Combine previousOption and option
            if (byType.TryGetValue(type, out prev))
            {
                if (op == Op.First) return;
                if (op == Op.Last) { byType[type] = option; return; }
                if (op == Op.Union) { byType[type] = option.UnionAs(prev, type); return; };
                if (op == Op.Intersection) { byType[type] = option.IntersectionAs(prev, type); return; };
                throw new ArgumentException(nameof(op));
            }
            else
            {
                // Add new entry
                byType[type] = option;
            }
        }

        /// <summary>
        /// Remove references of unknown classes.
        /// Sometimes options are instances of <see cref="IFileSystem"/>.
        /// Those are replaced with lighter option instances.
        /// </summary>
        protected virtual void Flatten()
        {
            // List where to add changes to be applied. Dictionary cannot be modified while enumerating it.
            List<(Type, IFileSystemOption)> changes = null;
            // Enumerate lines
            foreach (KeyValuePair<Type, IFileSystemOption> line in byType)
            {
                // Is there flattener
                if (line.Value.GetOperation(line.Key) is IFileSystemOptionOperationFlatten flattener)
                {
                    // Try flattining
                    IFileSystemOption flattened = flattener.Flatten(line.Value);
                    // Nothing was changed
                    if (flattened == line.Value) continue;
                    // Create list of modifications
                    if (changes == null) changes = new List<(Type, IFileSystemOption)>();
                    // Add modification
                    changes.Add((line.Key, flattened));
                }
            }
            // Apply modifications
            if (changes != null) foreach ((Type, IFileSystemOption) change in changes) byType[change.Item1] = change.Item2;
        }

        /// <summary>
        /// Get option by type.
        /// </summary>
        /// <param name="optionInterfaceType"></param>
        /// <returns>option or null</returns>
        public IFileSystemOption GetOption(Type optionInterfaceType)
        {
            IFileSystemOption result;
            if (byType.TryGetValue(optionInterfaceType, out result)) return result;
            return default;
        }

        IEnumerator<KeyValuePair<Type, IFileSystemOption>> IEnumerable<KeyValuePair<Type, IFileSystemOption>>.GetEnumerator() => byType.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => byType.GetEnumerator();

    }

    /// <summary>
    /// FileSystem option that denies write and modification operations.
    /// </summary>
    public class FileSystemOptionReadOnly : FileSystemOptionBase, IFileSystemOptionCreateDirectory, IFileSystemOptionDelete, IFileSystemOptionMove, IFileSystemOptionOpen, IFileSystemOptionMount
    {
        /// <inheritdoc/>
        public bool CanCreateMountPoint => false;
        /// <inheritdoc/>
        public bool CanListMountPoints => true;
        /// <inheritdoc/>
        public bool CanGetMountPoint => true;
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
    }

    /// <summary>
    /// Implementation to <see cref="IFileSystemOption"/>.
    /// </summary>
    public class FileSystemOptionNone : FileSystemOptionBase, IFileSystemOption
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
        internal static IFileSystemOption noOptions = new FileSystemOptionBase();
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
        internal static IFileSystemOption _readonly = new FileSystemOptionReadOnly();
    }
}
