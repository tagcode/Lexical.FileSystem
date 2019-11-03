using System;
using System.Collections.Generic;
using System.Text;

namespace Lexical.FileSystem.Decoration
{
    /// <summary>Class that implements all options except <see cref="IFileSystemToken"/>.</summary>
    public class FileSystemOptionsAll : IFileSystemOptionBrowse, IFileSystemOptionObserve, IFileSystemOptionOpen, IFileSystemOptionDelete, IFileSystemOptionFileAttribute, IFileSystemOptionMove, IFileSystemOptionCreateDirectory, IFileSystemOptionMount, IFileSystemOptionPath, IFileSystemOptionSubPath
    {
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
        public static FileSystemOptionsAll Read(IFileSystemOption option)
        {
            FileSystemOptionsAll result = new FileSystemOptionsAll();
            result.CanBrowse = option.CanBrowse();
            result.CanGetEntry = option.CanGetEntry();
            result.CanObserve = option.CanObserve();
            result.CanOpen = option.CanOpen();
            result.CanRead = option.CanRead();
            result.CanWrite = option.CanWrite();
            result.CanCreateFile = option.CanCreateFile();
            result.CanDelete = option.CanDelete();
            result.CanMove = option.CanMove();
            result.CanCreateDirectory = option.CanCreateDirectory();
            result.CanMount = option.CanMount();
            result.CanUnmount = option.CanUnmount();
            result.CanListMountPoints = option.CanListMountPoints();
            result.SubPath = option.SubPath();
            result.CanSetFileAttribute = option.CanSetFileAttribute();
            return result;
        }

        /// <summary>
        /// Create intersection with another option
        /// </summary>
        /// <param name="option"></param>
        /// <returns>this if <paramref name="option"/> is null or new instance with intersection</returns>
        public FileSystemOptionsAll Intersection(IFileSystemOption option)
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
}
