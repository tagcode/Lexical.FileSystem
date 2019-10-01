// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           29.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Facade class for <see cref="IFileSystemOption"/> implementations.
    /// </summary>
    public partial class FileSystemOption : IFileSystemOption
    {
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

        /// <summary>Create option for mount path. Used with <see cref="IFileSystemMountAssignment"/></summary>
        public static IFileSystemOption MountPath(string mountPath) => new FileSystemOptionMountPath(mountPath);
        /// <summary>No mount path.</summary>
        public static IFileSystemOption NoMountPath => noMountPath;

        /// <summary>
        /// Create union of <paramref name="options"/>.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IFileSystemOption Union(params IFileSystemOption[] options) 
            => new FileSystemOptionComposition(FileSystemOptionComposition.Op.Union, options);

        /// <summary>
        /// Create intersection of <paramref name="options"/>.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IFileSystemOption Intersection(params IFileSystemOption[] options)
            => new FileSystemOptionComposition(FileSystemOptionComposition.Op.Intersection, options);
    }

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
    }

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
        }

        /// <summary>
        /// Create composition of filesystem options.
        /// </summary>
        /// <param name="op">Join operation of same option intefaces</param>
        /// <param name="options">options to compose</param>
        public FileSystemOptionComposition(Op op, params IFileSystemOption[] options)
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
        }

        /// <summary>
        /// Add option to the composition.
        /// </summary>
        /// <param name="op">Join method</param>
        /// <param name="type">Interface type to add as</param>
        /// <param name="option">Option instance to add to composition</param>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="ArgumentException"></exception>
        protected virtual void Add(Op op, Type type, IFileSystemOption option)
        {
            IFileSystemOption prev;
            // Combine previousOption and option
            if (byType.TryGetValue(type, out prev))
            {
                if (op == Op.First) return;
                if (op == Op.Last) { byType[type] = option; return; }
                if (op == Op.Union) { byType[type] = Union(type, prev, option); return; };
                if (op == Op.Intersection) { byType[type] = Intersection(type, prev, option); return; };
                throw new ArgumentException(nameof(op));
            }
            else
            {
                // Add new entry
                byType[type] = option;
            }
        }

        /// <summary>
        /// Method that takes union of two option instances.
        /// 
        /// Inherit this to add mode supported types.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="o1"></param>
        /// <param name="o2"></param>
        /// <returns></returns>
        protected virtual IFileSystemOption Union(Type type, IFileSystemOption o1, IFileSystemOption o2)
        {
            if (type == typeof(IFileSystemOptionBrowse) && o1 is IFileSystemOptionBrowse b1 && o2 is IFileSystemOptionBrowse b2) return new FileSystemOptionBrowse(b1.CanBrowse || b2.CanBrowse, b1.CanGetEntry || b2.CanGetEntry);
            if (type == typeof(IFileSystemOptionCreateDirectory) && o1 is IFileSystemOptionCreateDirectory cd1 && o2 is IFileSystemOptionCreateDirectory cd2) return new FileSystemOptionCreateDirectory(cd1.CanCreateDirectory || cd2.CanCreateDirectory);
            if (type == typeof(IFileSystemOptionDelete) && o1 is IFileSystemOptionDelete d1 && o2 is IFileSystemOptionDelete d2) return new FileSystemOptionDelete(d1.CanDelete || d2.CanDelete);
            if (type == typeof(IFileSystemOptionMount) && o1 is IFileSystemOptionMount mt1 && o2 is IFileSystemOptionMount mt2) return new FileSystemOptionMount(mt1.CanCreateMountPoint||mt2.CanCreateMountPoint, mt1.CanListMountPoints || mt2.CanListMountPoints, mt1.CanGetMountPoint || mt2.CanGetMountPoint);
            if (type == typeof(IFileSystemOptionMove) && o1 is IFileSystemOptionMove mv1 && o2 is IFileSystemOptionMove mv2) return new FileSystemOptionMove(mv1.CanMove||mv2.CanMove);
            if (type == typeof(IFileSystemOptionOpen) && o1 is IFileSystemOptionOpen op1 && o2 is IFileSystemOptionOpen op2) return new FileSystemOptionOpen(op1.CanOpen||op2.CanOpen, op1.CanRead||op2.CanRead, op1.CanWrite||op2.CanWrite, op1.CanCreateFile||op2.CanCreateFile);
            if (type == typeof(IFileSystemOptionObserve) && o1 is IFileSystemOptionObserve ob1 && o2 is IFileSystemOptionObserve ob2) return new FileSystemOptionObserve(ob1.CanObserve||ob2.CanObserve, ob1.CanSetEventDispatcher||ob2.CanSetEventDispatcher);
            if (type == typeof(IFileSystemOptionPath) && o1 is IFileSystemOptionPath pa1 && o2 is IFileSystemOptionPath pa2) return new FileSystemOptionPath(pa1.CaseSensitivity|pa2.CaseSensitivity, pa1.EmptyDirectoryName||pa2.EmptyDirectoryName);
            throw new InvalidOperationException($"Cannot make union of {o1.GetType().Name} and {o2.GetType()} as {type.Name}");
        }

        /// <summary>
        /// Method that takes intersection of two option instances.
        /// 
        /// Inherit this to add mode supported types.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="o1"></param>
        /// <param name="o2"></param>
        /// <returns></returns>
        protected virtual IFileSystemOption Intersection(Type type, IFileSystemOption o1, IFileSystemOption o2)
        {
            if (type == typeof(IFileSystemOptionBrowse) && o1 is IFileSystemOptionBrowse b1 && o2 is IFileSystemOptionBrowse b2) return new FileSystemOptionBrowse(b1.CanBrowse && b2.CanBrowse, b1.CanGetEntry && b2.CanGetEntry);
            if (type == typeof(IFileSystemOptionCreateDirectory) && o1 is IFileSystemOptionCreateDirectory cd1 && o2 is IFileSystemOptionCreateDirectory cd2) return new FileSystemOptionCreateDirectory(cd1.CanCreateDirectory && cd2.CanCreateDirectory);
            if (type == typeof(IFileSystemOptionDelete) && o1 is IFileSystemOptionDelete d1 && o2 is IFileSystemOptionDelete d2) return new FileSystemOptionDelete(d1.CanDelete && d2.CanDelete);
            if (type == typeof(IFileSystemOptionMount) && o1 is IFileSystemOptionMount mt1 && o2 is IFileSystemOptionMount mt2) return new FileSystemOptionMount(mt1.CanCreateMountPoint && mt2.CanCreateMountPoint, mt1.CanListMountPoints && mt2.CanListMountPoints, mt1.CanGetMountPoint && mt2.CanGetMountPoint);
            if (type == typeof(IFileSystemOptionMove) && o1 is IFileSystemOptionMove mv1 && o2 is IFileSystemOptionMove mv2) return new FileSystemOptionMove(mv1.CanMove && mv2.CanMove);
            if (type == typeof(IFileSystemOptionOpen) && o1 is IFileSystemOptionOpen op1 && o2 is IFileSystemOptionOpen op2) return new FileSystemOptionOpen(op1.CanOpen && op2.CanOpen, op1.CanRead && op2.CanRead, op1.CanWrite && op2.CanWrite, op1.CanCreateFile && op2.CanCreateFile);
            if (type == typeof(IFileSystemOptionObserve) && o1 is IFileSystemOptionObserve ob1 && o2 is IFileSystemOptionObserve ob2) return new FileSystemOptionObserve(ob1.CanObserve && ob2.CanObserve, ob1.CanSetEventDispatcher && ob2.CanSetEventDispatcher);
            if (type == typeof(IFileSystemOptionPath) && o1 is IFileSystemOptionPath pa1 && o2 is IFileSystemOptionPath pa2) return new FileSystemOptionPath(pa1.CaseSensitivity | pa2.CaseSensitivity, pa1.EmptyDirectoryName && pa2.EmptyDirectoryName);
            throw new InvalidOperationException($"Cannot make intersection of {o1.GetType().Name} and {o2.GetType()} as {type.Name}");
        }


        /// <summary>
        /// Get option by type.
        /// </summary>
        /// <param name="optionInterfaceType"></param>
        /// <returns></returns>
        public IFileSystemOption GetOption(Type optionInterfaceType)
        {
            IFileSystemOption result;
            if (byType.TryGetValue(optionInterfaceType, out result)) return result;
            return default;
        }

        IEnumerator<KeyValuePair<Type, IFileSystemOption>> IEnumerable<KeyValuePair<Type, IFileSystemOption>>.GetEnumerator() => byType.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => byType.GetEnumerator();

    }


}
