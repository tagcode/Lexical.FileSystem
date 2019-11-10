// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.IO;
using System.Linq;

namespace Lexical.FileSystem.Decoration
{
    /// <summary>
    /// Abstract base class for decorated entry.
    /// </summary>
    public abstract class EntryDecoration : IEntryDecoration, IFileEntry, IDirectoryEntry, IDriveEntry, IMountEntry, IOption, IEntryFileAttributes, IEntryPhysicalPath
    {
        /// <summary>
        /// Decorate filesystem.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="newFileSystem"></param>
        /// <returns>decorated entry</returns>
        public static IEntry DecorateFileSystem(IEntry entry, IFileSystem newFileSystem)
            => new NewFileSystem(entry, newFileSystem);

        /// <summary>
        /// Decorate filesystem and path.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="newFileSystem"></param>
        /// <param name="newPath"></param>
        /// <returns>decorated entry</returns>
        public static IEntry DecorateFileSystemAndPath(IEntry entry, IFileSystem newFileSystem, string newPath)
            => new NewFileSystemAndPath(entry, newFileSystem, newPath);

        /// <summary>
        /// Decorate filesystem and path, and add option modifier.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="newFileSystem"></param>
        /// <param name="newPath"></param>
        /// <param name="optionModifier">(optional) option that will be applied to original option with intersection</param>
        /// <returns>decorated entry</returns>
        public static IEntry DecorateFileSystemPathAndOptionModifier(IEntry entry, IFileSystem newFileSystem, string newPath, IOption optionModifier)
            => new NewFileSystemPathAndOptionModifier(entry, newFileSystem, newPath, optionModifier);        

        /// <summary>
        /// Original entry that is being decorated.
        /// </summary>
        public virtual IEntry Original { get; protected set; }
        /// <inheritdoc/>
        public virtual IFileSystem FileSystem => Original.FileSystem;
        /// <inheritdoc/>
        public virtual string Path => Original.Path;
        /// <inheritdoc/>
        public virtual string Name => Original.Name;
        /// <inheritdoc/>
        public virtual DateTimeOffset LastModified => Original.LastModified;
        /// <inheritdoc/>
        public virtual DateTimeOffset LastAccess => Original.LastAccess;
        /// <inheritdoc/>
        public virtual bool IsFile => Original.IsFile();
        /// <inheritdoc/>
        public virtual long Length => Original.Length();
        /// <inheritdoc/>
        public virtual bool IsDirectory => Original.IsDirectory();
        /// <inheritdoc/>
        public virtual IOption Options => Original.Options();
        /// <inheritdoc/>
        public virtual bool IsDrive => Original.IsDrive();
        /// <inheritdoc/>
        public virtual bool IsMountPoint => Original.IsMountPoint();
        /// <inheritdoc/>
        public virtual FileSystemAssignment[] Mounts => Original.Mounts();
        /// <inheritdoc/>
        public virtual DriveType DriveType => Original.DriveType();
        /// <inheritdoc/>
        public virtual long DriveFreeSpace => Original.DriveFreeSpace();
        /// <inheritdoc/>
        public virtual long DriveSize => Original.DriveSize();
        /// <inheritdoc/>
        public virtual string DriveLabel => Original.DriveLabel();
        /// <inheritdoc/>
        public virtual string DriveFormat => Original.DriveFormat();
        /// <inheritdoc/>
        public virtual bool HasFileAttributes => Original.HasFileAttributes();
        /// <inheritdoc/>
        public virtual FileAttributes FileAttributes => Original.FileAttributes();
        /// <inheritdoc/>
        public string PhysicalPath => Original.PhysicalPath();

        /// <summary>
        /// Create decorated entry.
        /// </summary>
        /// <param name="original"></param>
        public EntryDecoration(IEntry original)
        {
            Original = original ?? throw new ArgumentNullException(nameof(original));
        }

        /// <summary>Print info</summary>
        /// <returns>path</returns>
        public override string ToString() => Path;

        /// <summary>
        /// New overriding filesystem.
        /// </summary>
        public class NewFileSystem : EntryDecoration
        {
            /// <summary>New overriding filesystem.</summary>
            protected IFileSystem newFileSystem;
            /// <summary>New overriding filesystem.</summary>
            public override IFileSystem FileSystem => newFileSystem;
            /// <summary>
            /// Create decoration with <paramref name="newFileSystem"/>.
            /// </summary>
            /// <param name="original"></param>
            /// <param name="newFileSystem"></param>
            public NewFileSystem(IEntry original, IFileSystem newFileSystem) : base(original)
            {
                this.newFileSystem = newFileSystem;
            }
        }

        /// <summary>
        /// New overriding filesystem and Path
        /// </summary>
        public class NewFileSystemAndPath : EntryDecoration
        {
            /// <summary>New overriding filesystem.</summary>
            protected IFileSystem newFileSystem;
            /// <summary>New overriding path.</summary>
            protected string newPath;
            /// <summary>New overriding filesystem.</summary>
            public override IFileSystem FileSystem => newFileSystem;
            /// <summary>New path.</summary>
            public override string Path => newPath;
            /// <summary>
            /// Create decoration with <paramref name="newFileSystem"/>.
            /// </summary>
            /// <param name="original"></param>
            /// <param name="newFileSystem"></param>
            /// <param name="newPath"></param>
            public NewFileSystemAndPath(IEntry original, IFileSystem newFileSystem, string newPath) : base(original)
            {
                this.newFileSystem = newFileSystem;
                this.newPath = newPath;
            }
        }

        /// <summary>
        /// New overriding filesystem, Path and Option modifier
        /// </summary>
        public class NewFileSystemPathAndOptionModifier : EntryDecoration
        {
            /// <summary>New overriding filesystem.</summary>
            protected IFileSystem newFileSystem;
            /// <summary>New overriding path.</summary>
            protected string newPath;
            /// <summary>New overriding filesystem.</summary>
            public override IFileSystem FileSystem => newFileSystem;
            /// <summary>New path.</summary>
            public override string Path => newPath;
            /// <summary>(optional) Option that will be intersected lazily with original options.</summary>
            protected IOption optionModifier;
            /// <summary>Lazily construction intersection of <see cref="optionModifier"/> and <see cref="Original"/>.Option()</summary>
            protected IOption optionIntersection;
            /// <summary>Intersection of <see cref="Original"/>.Option() and <see cref="optionModifier"/></summary>
            public override IOption Options => optionIntersection ?? (optionIntersection = Option.Intersection(optionModifier, Original.Options()));
            /// <summary>
            /// Create decoration with <paramref name="newFileSystem"/>.
            /// </summary>
            /// <param name="original"></param>
            /// <param name="newFileSystem"></param>
            /// <param name="newPath"></param>
            /// <param name="optionModifier">(optional) option that will be applied to original option with intersection</param>
            public NewFileSystemPathAndOptionModifier(IEntry original, IFileSystem newFileSystem, string newPath, IOption optionModifier) : base(original)
            {
                this.newFileSystem = newFileSystem;
                this.newPath = newPath;
                this.optionModifier = optionModifier;
            }
        }

    }

    /// <summary>
    /// Decoration that uses two <see cref="IEntry"/> instances: a and b. 
    /// 
    /// Returns values from a if available, and if not then uses a fallback value from b.
    /// </summary>
    public class DoubleEntryDecoration : IEntryDecoration, IFileEntry, IDirectoryEntry, IDriveEntry, IMountEntry, IOption, IEntryFileAttributes, IEntryPhysicalPath
    {
        /// <summary>Entry to decorate.</summary>
        public readonly IEntry A;
        /// <summary>Entry to decorate, fallback values..</summary>
        public readonly IEntry B;

        /// <summary>Original entry that is being decorated.</summary>
        public virtual IEntry Original => A;
        /// <inheritdoc/>
        public virtual IFileSystem FileSystem => A.FileSystem;
        /// <inheritdoc/>
        public virtual string Path => A.Path ?? B.Path;
        /// <inheritdoc/>
        public virtual string Name => A.Name ?? B.Name;
        /// <inheritdoc/>
        public virtual DateTimeOffset LastModified { get { DateTimeOffset _a = A.LastModified; return _a == DateTimeOffset.MinValue ? B.LastModified : _a; } }
        /// <inheritdoc/>
        public virtual DateTimeOffset LastAccess { get { DateTimeOffset _a = A.LastAccess; return _a == DateTimeOffset.MinValue ? B.LastAccess : _a; } }
        /// <inheritdoc/>
        public virtual bool IsFile => A.IsFile() || B.IsFile();
        /// <inheritdoc/>
        public virtual long Length { get { long _a = A.Length(); return _a<0L ? B.Length() : _a; } }
        /// <inheritdoc/>
        public virtual bool IsDirectory => A.IsDirectory() || B.IsDirectory();
        /// <inheritdoc/>
        public virtual IOption Options => Option.Union(A.Options(), B.Options());
        /// <inheritdoc/>
        public virtual bool IsDrive => A.IsDrive() || B.IsDrive();
        /// <inheritdoc/>
        public virtual bool IsMountPoint => A.IsMountPoint() || B.IsMountPoint();
        /// <inheritdoc/>
        public virtual FileSystemAssignment[] Mounts { get { FileSystemAssignment[] _a = A.Mounts(), _b = B.Mounts(); if (_b == null || _b.Length == 0) return _a; if (_a == null || _a.Length == 0) return _b; FileSystemAssignment[] arr = new FileSystemAssignment[_a.Length + _b.Length]; Array.Copy(_a, 0, arr, 0, _a.Length); Array.Copy(_b, 0, arr, _a.Length, _b.Length); return arr; } }
        /// <inheritdoc/>
        public virtual DriveType DriveType { get { DriveType _a = A.DriveType(); return _a == DriveType.Unknown ? B.DriveType() : _a; } }
        /// <inheritdoc/>
        public virtual long DriveFreeSpace { get { long _a = A.DriveFreeSpace(); return _a < 0L ? B.DriveFreeSpace() : _a; } }
        /// <inheritdoc/>
        public virtual long DriveSize { get { long _a = A.DriveSize(); return _a < 0L ? B.DriveSize() : _a; } }
        /// <inheritdoc/>
        public virtual string DriveLabel => A.DriveLabel() ?? B.DriveLabel();
        /// <inheritdoc/>
        public virtual string DriveFormat => A.DriveFormat() ?? B.DriveFormat();
        /// <inheritdoc/>
        public virtual bool HasFileAttributes => A.HasFileAttributes() || B.HasFileAttributes();
        /// <inheritdoc/>
        public virtual FileAttributes FileAttributes => A.FileAttributes() | B.FileAttributes();
        /// <inheritdoc/>
        public string PhysicalPath => A.PhysicalPath() ?? B.PhysicalPath();

        /// <summary>
        /// Create decorated entry.
        /// </summary>
        /// <param name="a">entry to decoate</param>
        /// <param name="b">fallback values</param>
        public DoubleEntryDecoration(IEntry a, IEntry b)
        {
            A = a ?? throw new ArgumentNullException(nameof(a));
            B = b ?? throw new ArgumentNullException(nameof(b));
        }

        /// <summary>Print info</summary>
        /// <returns>path</returns>
        public override string ToString() => A.ToString()??B.ToString()??Path;
    }

}
