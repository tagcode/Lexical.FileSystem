using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lexical.FileSystem.Decoration
{
    /// <summary>Class that implements all options except <see cref="IToken"/>.</summary>
    public class FileSystemOptionsAll : IBrowseOption, IObserveOption, IOpenOption, IDeleteOption, IFileAttributeOption, IMoveOption, ICreateDirectoryOption, IMountOption, IPathInfo, ISubPathOption
    {
        /// <summary>Contained interface types.</summary>
        public static Type[] Types = new Type[]
        {
            typeof(IBrowseOption),
            typeof(IObserveOption),
            typeof(IOpenOption),
            typeof(IDeleteOption),
            typeof(IFileAttributeOption),
            typeof(IMoveOption),
            typeof(ICreateDirectoryOption),
            typeof(IMountOption),
            typeof(IPathInfo),
            typeof(ISubPathOption)
        };

        // TODO Implement Hash-Equals //
        // TODO Implement Union & Intersection //

        /// <inheritdoc/>
        public bool CanBrowse { get; set; }
        /// <inheritdoc/>
        public bool CanGetEntry { get; set; }
        /// <inheritdoc/>
        public bool CanObserve { get; set; }
        /// <inheritdoc/>
        public bool CanOpen { get; set; }
        /// <inheritdoc/>
        public bool CanRead { get; set; }
        /// <inheritdoc/>
        public bool CanWrite { get; set; }
        /// <inheritdoc/>
        public bool CanCreateFile { get; set; }
        /// <inheritdoc/>
        public bool CanDelete { get; set; }
        /// <inheritdoc/>
        public bool CanMove { get; set; }
        /// <inheritdoc/>
        public bool CanCreateDirectory { get; set; }
        /// <inheritdoc/>
        public bool CanMount { get; set; }
        /// <inheritdoc/>
        public bool CanUnmount { get; set; }
        /// <inheritdoc/>
        public bool CanListMountPoints { get; set; }
        /// <inheritdoc/>
        public FileSystemCaseSensitivity CaseSensitivity { get; set; }
        /// <inheritdoc/>
        public bool EmptyDirectoryName { get; set; }
        /// <inheritdoc/>
        public string SubPath { get; set; }
        /// <inheritdoc/>
        public bool CanSetFileAttribute { get; set; }

        /// <summary>
        /// Read options from <paramref name="option"/> and return flattened object.
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        public static FileSystemOptionsAll Read(IOption option)
        {
            FileSystemOptionsAll result = new FileSystemOptionsAll();
            result.ReadFrom(option);
            return result;
        }

        /// <summary>
        /// Read options from <paramref name="option"/> and return flattened object.
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        public virtual void ReadFrom(IOption option)
        {
            this.CanBrowse = option.CanBrowse();
            this.CanGetEntry = option.CanGetEntry();
            this.CanObserve = option.CanObserve();
            this.CanOpen = option.CanOpen();
            this.CanRead = option.CanRead();
            this.CanWrite = option.CanWrite();
            this.CanCreateFile = option.CanCreateFile();
            this.CanDelete = option.CanDelete();
            this.CanMove = option.CanMove();
            this.CanCreateDirectory = option.CanCreateDirectory();
            this.CanMount = option.CanMount();
            this.CanUnmount = option.CanUnmount();
            this.CanListMountPoints = option.CanListMountPoints();
            this.SubPath = option.SubPath();
            this.CanSetFileAttribute = option.CanSetFileAttribute();
        }

        /// <summary>
        /// Create intersection with another option
        /// </summary>
        /// <param name="option"></param>
        /// <returns>this if <paramref name="option"/> is null or new instance with intersection</returns>
        public virtual FileSystemOptionsAll Intersection(IOption option)
        {
            if (option == null) return this;
            FileSystemOptionsAll result = new FileSystemOptionsAll();
            result.CanBrowse = this.CanBrowse | option.CanBrowse();
            result.CanGetEntry = this.CanGetEntry | option.CanGetEntry();
            result.CanObserve = this.CanObserve | option.CanObserve();
            result.CanOpen = this.CanOpen | option.CanOpen();
            result.CanRead = this.CanRead | option.CanRead();
            result.CanWrite = this.CanWrite | option.CanWrite();
            result.CanCreateFile = this.CanCreateFile | option.CanCreateFile();
            result.CanDelete = this.CanDelete | option.CanDelete();
            result.CanSetFileAttribute = this.CanSetFileAttribute | option.CanSetFileAttribute();
            result.CanMount = this.CanMount | option.CanMount();
            result.CanCreateFile = this.CanCreateFile | option.CanCreateFile();
            result.CanDelete = this.CanDelete | option.CanDelete();
            result.CanMove = this.CanMove | option.CanMove();
            result.CanCreateDirectory = this.CanCreateDirectory | option.CanCreateDirectory();
            result.CanMount = this.CanMount | option.CanMount();
            result.CanUnmount = this.CanUnmount | option.CanUnmount();
            result.CanListMountPoints = this.CanListMountPoints | option.CanListMountPoints();
            result.SubPath = this.SubPath ?? option.SubPath();
            return result;
        }
    }

    /// <summary>
    /// Options with tokens
    /// </summary>
    public class FileSystemOptionsAllWithTokens : FileSystemOptionsAll, ITokenEnumerable
    {
        static IToken[] no_tokens = new IToken[0];

        /// <summary>Tokens</summary>
        protected IToken[] tokens = no_tokens;

        /// <summary>
        /// Read options from <paramref name="option"/> and return flattened object.
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        public static new FileSystemOptionsAllWithTokens Read(IOption option)
        {
            FileSystemOptionsAllWithTokens result = new FileSystemOptionsAllWithTokens();
            result.ReadFrom(option);
            return result;
        }

        /// <summary>
        /// Read options from <paramref name="option"/> and return flattened object.
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        public override void ReadFrom(IOption option)
        {
            base.ReadFrom(option);
            var enumr = option.ListTokens(false);
            this.tokens = enumr is IToken[] arr ? arr : enumr.ToArray();
        }

        /// <summary>Get enumerator</summary>
        public IEnumerator<IToken> GetEnumerator()
            => ((IEnumerable<IToken>)tokens).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => ((IEnumerable)tokens).GetEnumerator();

    }

}
