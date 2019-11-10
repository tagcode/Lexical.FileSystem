// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           29.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System.Collections.Generic;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Facade for <see cref="IOption"/> static values.
    /// </summary>
    public static class Option
    {
        // Operations //
        /// <summary>Join <paramref name="option1"/> and <paramref name="option2"/>. Takes first instance of each option.</summary>
        public static IOption Join(IOption option1, IOption option2) => option1 == null ? option2 : option2 == null ? option1 : new OptionComposition(OptionComposition.Op.First, option1, option2);
        /// <summary>Join <paramref name="options"/>, takes first instance of each option.</summary>
        public static IOption Join(params IOption[] options) => new OptionComposition(OptionComposition.Op.First, options);
        /// <summary>Join <paramref name="options"/>, takes first instance of each option.</summary>
        public static IOption Join(IEnumerable<IOption> options) => new OptionComposition(OptionComposition.Op.First, options);
        /// <summary>Join <paramref name="option1"/> and <paramref name="option2"/>. Takes first instance of each option.</summary>
        public static IOption OptionJoin(this IOption option1, IOption option2) => option1 == null ? option2 : option2 == null ? option1 : new OptionComposition(OptionComposition.Op.First, option1, option2);
        /// <summary>Union of <paramref name="option1"/> and <paramref name="option2"/>. Takes first instance of each option.</summary>
        public static IOption Union(IOption option1, IOption option2) => option1 == null ? option2 : option2 == null ? option1 : new OptionComposition(OptionComposition.Op.Union, option1, option2);
        /// <summary>Union of <paramref name="options"/>.</summary>
        public static IOption Union(params IOption[] options) => new OptionComposition(OptionComposition.Op.Union, options);
        /// <summary>Union of <paramref name="options"/>.</summary>
        public static IOption Union(IEnumerable<IOption> options) => new OptionComposition(OptionComposition.Op.Union, options);
        /// <summary>Union of <paramref name="option1"/> and <paramref name="option2"/>. Takes first instance of each option.</summary>
        public static IOption OptionUnion(this IOption option1, IOption option2) => option1 == null ? option2 : option2 == null ? option1 : new OptionComposition(OptionComposition.Op.Union, option1, option2);
        /// <summary>Intersection of <paramref name="option1"/> and <paramref name="option2"/>. Takes first instance of each option.</summary>
        public static IOption Intersection(IOption option1, IOption option2) => option1 == null ? option2 : option2 == null ? option1 : new OptionComposition(OptionComposition.Op.Intersection, option1, option2);
        /// <summary>Intersection of <paramref name="options"/>.</summary>
        public static IOption Intersection(params IOption[] options) => new OptionComposition(OptionComposition.Op.Intersection, options);
        /// <summary>Intersection of <paramref name="options"/>.</summary>
        public static IOption Intersection(IEnumerable<IOption> options) => new OptionComposition(OptionComposition.Op.Intersection, options);
        /// <summary>Intersection of <paramref name="option1"/> and <paramref name="option2"/>. Takes first instance of each option.</summary>
        public static IOption OptionIntersection(this IOption option1, IOption option2) => option1 == null ? option2 : option2 == null ? option1 : new OptionComposition(OptionComposition.Op.Intersection, option1, option2);

        // Path-level options //
        /// <summary>Read-only operations allowed, deny modification and write operations</summary>
        public static IOption ReadOnly => ReadOnlyOption.Instance;
        /// <summary>No options</summary>
        public static IOption NoOptions => NoOption.Instance;

        /// <summary>Path options.</summary>
        public static IOption Path(FileSystemCaseSensitivity caseSensitivity, bool emptyDirectoryName) => new FileSystemOptionPath(caseSensitivity, emptyDirectoryName);

        /// <summary>Observe is allowed.</summary>
        public static IOption Observe => ObserveOption.observe;
        /// <summary>Observe is not allowed</summary>
        public static IOption NoObserve => ObserveOption.noObserve;

        /// <summary>Open options</summary>
        public static IOption Open(bool canOpen, bool canRead, bool canWrite, bool canCreateFile) => new OpenOption(canOpen, canRead, canWrite, canCreateFile);
        /// <summary>Open, Read, Write, Create</summary>
        public static IOption OpenReadWriteCreate => OpenOption.OpenReadWriteCreate;
        /// <summary>Open, Read, Write</summary>
        public static IOption OpenReadWrite => OpenOption.OpenReadWrite;
        /// <summary>Open, Read</summary>
        public static IOption OpenRead => OpenOption.OpenRead;
        /// <summary>No access</summary>
        public static IOption NoOpen => OpenOption.NoOpen;

        /// <summary>Mount is allowed.</summary>
        public static IOption Mount => MountOption.Mount;
        /// <summary>Mount is not allowed</summary>
        public static IOption NoMount => MountOption.NoMount;

        /// <summary>Move and rename is allowed.</summary>
        public static IOption Move => MoveOption.Move;
        /// <summary>Move and rename not allowed.</summary>
        public static IOption NoMove => MoveOption.NoMove;

        /// <summary>Delete allowed.</summary>
        public static IOption Delete => DeleteOption.Delete;
        /// <summary>Delete not allowed.</summary>
        public static IOption NoDelete => DeleteOption.NoDelete;

        /// <summary>CreateDirectory allowed.</summary>
        public static IOption CreateDirectory => CreateDirectoryOption.CreateDirectory;
        /// <summary>CreateDirectory not allowed.</summary>
        public static IOption NoCreateDirectory => CreateDirectoryOption.NoCreateDirectory;

        /// <summary>Browse allowed.</summary>
        public static IOption Browse => BrowseOption.Browse;
        /// <summary>Browse not allowed.</summary>
        public static IOption NoBrowse => BrowseOption.NoBrowse;

        // FileSystem-level options //
        /// <summary>Create option for sub-path. Used with decorator and virtual filesystem mount option.</summary>
        public static IOption SubPath(string subPath) => new SubPathOption(subPath);
        /// <summary>No mount path.</summary>
        public static IOption NoSubPath => SubPathOption.noSubPath;

    }
}
