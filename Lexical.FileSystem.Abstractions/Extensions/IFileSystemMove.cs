// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------

using System;
using System.IO;
using System.Security;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Extension methods for <see cref="IFileSystem"/>.
    /// </summary>
    public static partial class IFileSystemExtensions
    {
        /// <summary>
        /// Test if <paramref name="filesystemOption"/> has Move capability.
        /// <param name="filesystemOption"></param>
        /// </summary>
        /// <returns>true, if has Move capability</returns>
        public static bool CanMove(this IFileSystemOption filesystemOption)
            => filesystemOption.As<IFileSystemOptionMove>() is IFileSystemOptionMove mover ? mover.CanMove : false;

        /// <summary>
        /// Try to move/rename a file or directory.
        /// 
        /// If <paramref name="oldPath"/> and <paramref name="newPath"/> refers to a directory, then the path names 
        /// should end with directory separator character '/'.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="oldPath">old path of a file or directory</param>
        /// <param name="newPath">new path of a file or directory</param>
        /// <exception cref="FileNotFoundException">The specified <paramref name="oldPath"/> is invalid.</exception>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="FileNotFoundException">The specified path is invalid.</exception>
        /// <exception cref="ArgumentNullException">path is null</exception>
        /// <exception cref="ArgumentException">path is an empty string (""), contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support renaming/moving files</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">path refers to non-file device, or an entry already exists at <paramref name="newPath"/></exception>
        /// <exception cref="ObjectDisposedException"/>
        public static void Move(this IFileSystem filesystem, string oldPath, string newPath)
        {
            if (filesystem is IFileSystemMove mover) mover.Move(oldPath, newPath);
            else throw new NotSupportedException(nameof(Move));
        }
    }

    /// <summary><see cref="IFileSystemOptionMove"/> operations.</summary>
    public class FileSystemOptionOperationMove : IFileSystemOptionOperationFlatten, IFileSystemOptionOperationIntersection, IFileSystemOptionOperationUnion
    {
        /// <summary>The option type that this class has operations for.</summary>
        public Type OptionType => typeof(IFileSystemOptionMove);
        /// <summary>Flatten to simpler instance.</summary>
        public IFileSystemOption Flatten(IFileSystemOption o) => o is IFileSystemOptionMove c ? o is FileSystemOptionMove ? /*already flattened*/o : /*new instance*/new FileSystemOptionMove(c.CanMove) : throw new InvalidCastException($"{typeof(IFileSystemOptionMove)} expected.");
        /// <summary>Intersection of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IFileSystemOption Intersection(IFileSystemOption o1, IFileSystemOption o2) => o1 is IFileSystemOptionMove c1 && o2 is IFileSystemOptionMove c2 ? new FileSystemOptionMove(c1.CanMove && c2.CanMove) : throw new InvalidCastException($"{typeof(IFileSystemOptionMove)} expected.");
        /// <summary>Union of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IFileSystemOption Union(IFileSystemOption o1, IFileSystemOption o2) => o1 is IFileSystemOptionMove c1 && o2 is IFileSystemOptionMove c2 ? new FileSystemOptionMove(c1.CanMove || c2.CanMove) : throw new InvalidCastException($"{typeof(IFileSystemOptionMove)} expected.");
    }

    /// <summary>File system option for move/rename.</summary>
    public class FileSystemOptionMove : IFileSystemOptionMove
    {
        /// <summary>Has Move capability.</summary>
        public bool CanMove { get; protected set; }

        /// <summary>Create file system option for move/rename.</summary>
        public FileSystemOptionMove(bool canMove)
        {
            CanMove = canMove;
        }

        /// <inheritdoc/>
        public override string ToString() => CanMove ? "CanMove" : "NoMove";
    }
}
