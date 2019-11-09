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
    public class ReadOnlyOption : ICreateDirectoryOption, IDeleteOption, IMoveOption, IOpenOption, IMountOption, IBrowseOption, IObserveOption
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
        public bool CanListMountPoints => true;
        /// <inheritdoc/>
        public bool CanBrowse => true;
        /// <inheritdoc/>
        public bool CanGetEntry => true;
        /// <inheritdoc/>
        public bool CanObserve => true;
        /// <inheritdoc/>
        public override string ToString() => "ReadOnly";
    }

    /// <summary>No options of <see cref="IOption"/>.</summary>
    public class NoneOption : IOption
    {
        /// <summary>No options</summary>
        static IOption noOptions = new NoneOption();
        /// <summary>No options</summary>
        public static IOption NoOptions => noOptions;
        /// <inheritdoc/>
        public override string ToString() => "None";
    }

}

namespace Lexical.FileSystem
{
    public static partial class Option
    {
        internal static IOption noOptions = new NoneOption();
        internal static IBrowseOption browse = new BrowseOption(true, true);
        internal static IBrowseOption noBrowse = new BrowseOption(false, false);
        internal static ICreateDirectoryOption createDirectory = new CreateDirectoryOption(true);
        internal static ICreateDirectoryOption noCreateDirectory = new CreateDirectoryOption(false);
        internal static IDeleteOption delete = new DeleteOption(true);
        internal static IDeleteOption noDelete = new DeleteOption(false);
        internal static IMoveOption move = new MoveOption(true);
        internal static IMoveOption noMove = new MoveOption(false);
        internal static IMountOption mount = new MountOption(true, true, true);
        internal static IMountOption noMount = new MountOption(false, false, false);
        internal static IOpenOption openReadWriteCreate = new OpenOption(true, true, true, true);
        internal static IOpenOption openReadWrite = new OpenOption(true, true, true, false);
        internal static IOpenOption openRead = new OpenOption(true, true, false, false);
        internal static IOpenOption noOpen = new OpenOption(false, false, false, false);
        internal static IObserveOption observe = new ObserveOption(true);
        internal static IObserveOption noObserve = new ObserveOption(false);
        internal static ISubPathOption noSubPath = new SubPathOption(null);
        internal static IOption _readonly = new ReadOnlyOption();
    }
}
