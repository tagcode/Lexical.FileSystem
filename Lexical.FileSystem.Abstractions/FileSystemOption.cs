// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           29.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------


using System.Collections.Generic;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Facade for <see cref="IFileSystemOption"/> static values.
    /// </summary>
    public static partial class FileSystemOption
    {
        // Operations //
        /// <summary>Join <paramref name="option1"/> and <paramref name="option2"/>. Takes first instance of each option.</summary>
        public static IFileSystemOption Join(IFileSystemOption option1, IFileSystemOption option2) => option1 == null ? option2 : option2 == null ? option1 : new FileSystemOptionComposition(FileSystemOptionComposition.Op.First, option1, option2);
        /// <summary>Join <paramref name="options"/>, takes first instance of each option.</summary>
        public static IFileSystemOption Join(params IFileSystemOption[] options) => new FileSystemOptionComposition(FileSystemOptionComposition.Op.First, options);
        /// <summary>Join <paramref name="options"/>, takes first instance of each option.</summary>
        public static IFileSystemOption Join(IEnumerable<IFileSystemOption> options) => new FileSystemOptionComposition(FileSystemOptionComposition.Op.First, options);
        /// <summary>Join <paramref name="option1"/> and <paramref name="option2"/>. Takes first instance of each option.</summary>
        public static IFileSystemOption OptionJoin(this IFileSystemOption option1, IFileSystemOption option2) => option1 == null ? option2 : option2 == null ? option1 : new FileSystemOptionComposition(FileSystemOptionComposition.Op.First, option1, option2);
        /// <summary>Union of <paramref name="option1"/> and <paramref name="option2"/>. Takes first instance of each option.</summary>
        public static IFileSystemOption Union(IFileSystemOption option1, IFileSystemOption option2) => option1 == null ? option2 : option2 == null ? option1 : new FileSystemOptionComposition(FileSystemOptionComposition.Op.Union, option1, option2);
        /// <summary>Union of <paramref name="options"/>.</summary>
        public static IFileSystemOption Union(params IFileSystemOption[] options) => new FileSystemOptionComposition(FileSystemOptionComposition.Op.Union, options);
        /// <summary>Union of <paramref name="options"/>.</summary>
        public static IFileSystemOption Union(IEnumerable<IFileSystemOption> options) => new FileSystemOptionComposition(FileSystemOptionComposition.Op.Union, options);
        /// <summary>Union of <paramref name="option1"/> and <paramref name="option2"/>. Takes first instance of each option.</summary>
        public static IFileSystemOption OptionUnion(this IFileSystemOption option1, IFileSystemOption option2) => option1 == null ? option2 : option2 == null ? option1 : new FileSystemOptionComposition(FileSystemOptionComposition.Op.Union, option1, option2);
        /// <summary>Intersection of <paramref name="option1"/> and <paramref name="option2"/>. Takes first instance of each option.</summary>
        public static IFileSystemOption Intersection(IFileSystemOption option1, IFileSystemOption option2) => option1 == null ? option2 : option2 == null ? option1 : new FileSystemOptionComposition(FileSystemOptionComposition.Op.Intersection, option1, option2);
        /// <summary>Intersection of <paramref name="options"/>.</summary>
        public static IFileSystemOption Intersection(params IFileSystemOption[] options) => new FileSystemOptionComposition(FileSystemOptionComposition.Op.Intersection, options);
        /// <summary>Intersection of <paramref name="options"/>.</summary>
        public static IFileSystemOption Intersection(IEnumerable<IFileSystemOption> options) => new FileSystemOptionComposition(FileSystemOptionComposition.Op.Intersection, options);
        /// <summary>Intersection of <paramref name="option1"/> and <paramref name="option2"/>. Takes first instance of each option.</summary>
        public static IFileSystemOption OptionIntersection(this IFileSystemOption option1, IFileSystemOption option2) => option1 == null ? option2 : option2 == null ? option1 : new FileSystemOptionComposition(FileSystemOptionComposition.Op.Intersection, option1, option2);

        // Path-level options //
        /// <summary>Read-only operations allowed, deny modification and write operations</summary>
        public static IFileSystemOption ReadOnly => _readonly;
        /// <summary>No options</summary>
        public static IFileSystemOption NoOptions => noOptions;

        /// <summary>Path options.</summary>
        public static IFileSystemOption Path(FileSystemCaseSensitivity caseSensitivity, bool emptyDirectoryName) => new FileSystemOptionPath(caseSensitivity, emptyDirectoryName);

        /// <summary>Observe is allowed.</summary>
        public static IFileSystemOption Observe => observe;
        /// <summary>Observe is not allowed</summary>
        public static IFileSystemOption NoObserve => noObserve;

        /// <summary>Open options</summary>
        public static IFileSystemOption Open(bool canOpen, bool canRead, bool canWrite, bool canCreateFile) => new FileSystemOptionOpen(canOpen, canRead, canWrite, canCreateFile);
        /// <summary>Open, Read, Write, Create</summary>
        public static IFileSystemOption OpenReadWriteCreate => openReadWriteCreate;
        /// <summary>Open, Read, Write</summary>
        public static IFileSystemOption OpenReadWrite => openReadWrite;
        /// <summary>Open, Read</summary>
        public static IFileSystemOption OpenRead => openRead;
        /// <summary>No access</summary>
        public static IFileSystemOption NoOpen => noOpen;

        /// <summary>Mount is allowed.</summary>
        public static IFileSystemOption Mount => mount;
        /// <summary>Mount is not allowed</summary>
        public static IFileSystemOption NoMount => noMount;

        /// <summary>Move and rename is allowed.</summary>
        public static IFileSystemOption Move => move;
        /// <summary>Move and rename not allowed.</summary>
        public static IFileSystemOption NoMove => noMove;

        /// <summary>Delete allowed.</summary>
        public static IFileSystemOption Delete => delete;
        /// <summary>Delete not allowed.</summary>
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
        /// <summary>Create option for sub-path. Used with decorator and virtual filesystem mount option.</summary>
        public static IFileSystemOption SubPath(string subPath) => new FileSystemOptionSubPath(subPath);
        /// <summary>No mount path.</summary>
        public static IFileSystemOption NoSubPath => noSubPath;

    }
}
