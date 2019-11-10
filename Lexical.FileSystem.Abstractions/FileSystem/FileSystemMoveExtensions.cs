// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------

using System;
using System.IO;
using System.Security;
using System.Threading.Tasks;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Extension methods for <see cref="IFileSystem"/>.
    /// </summary>
    public static partial class FileSystemMoveExtensions
    {
        /// <summary>
        /// Test if <paramref name="filesystemOption"/> has local move/rename capability. 
        /// </summary>
        /// <param name="filesystemOption"></param>
        /// <param name="defaultValue">Returned value if option is unspecified</param>
        /// <returns>true, if has Move capability</returns>
        public static bool CanMove(this IOption filesystemOption, bool defaultValue = false)
            => filesystemOption.AsOption<IMoveOption>() is IMoveOption mover ? mover.CanMove : defaultValue;

        /// <summary>
        /// Move/rename a file or directory. 
        /// 
        /// If <paramref name="srcPath"/> and <paramref name="dstPath"/> refers to a directory, then the path names 
        /// should end with directory separator character '/'.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="srcPath">old path of a file or directory</param>
        /// <param name="dstPath">new path of a file or directory</param>
        /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
        /// <exception cref="FileNotFoundException">The specified <paramref name="srcPath"/> is invalid.</exception>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="FileNotFoundException">The specified path is invalid.</exception>
        /// <exception cref="ArgumentNullException">path is null</exception>
        /// <exception cref="ArgumentException">path is an empty string (""), contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support renaming/moving files</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">path refers to non-file device, or an entry already exists at <paramref name="dstPath"/></exception>
        /// <exception cref="ObjectDisposedException"/>
        public static void Move(this IFileSystem filesystem, string srcPath, string dstPath, IOption option = null)
        {
            if (filesystem is IFileSystemMove mover) mover.Move(srcPath, dstPath, option);
            else if (filesystem is IFileSystemMoveAsync moverAsync) moverAsync.MoveAsync(srcPath, dstPath, option).Wait();
            else throw new NotSupportedException(nameof(Move));
        }

        /// <summary>
        /// Move/rename a file or directory. 
        /// 
        /// If <paramref name="srcPath"/> and <paramref name="dstPath"/> refers to a directory, then the path names 
        /// should end with directory separator character '/'.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="srcPath">old path of a file or directory</param>
        /// <param name="dstPath">new path of a file or directory</param>
        /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
        /// <exception cref="FileNotFoundException">The specified <paramref name="srcPath"/> is invalid.</exception>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="FileNotFoundException">The specified path is invalid.</exception>
        /// <exception cref="ArgumentNullException">path is null</exception>
        /// <exception cref="ArgumentException">path is an empty string (""), contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support renaming/moving files</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">path refers to non-file device, or an entry already exists at <paramref name="dstPath"/></exception>
        /// <exception cref="ObjectDisposedException"/>
        public static Task MoveAsync(this IFileSystem filesystem, string srcPath, string dstPath, IOption option = null)
        {
            if (filesystem is IFileSystemMoveAsync moverAsync) return moverAsync.MoveAsync(srcPath, dstPath, option);
            else if (filesystem is IFileSystemMove mover) return Task.Run(()=>mover.Move(srcPath, dstPath, option));
            else throw new NotSupportedException(nameof(Move));
        }
    }

    /// <summary><see cref="IMoveOption"/> operations.</summary>
    public class MoveOptionOperations : IOptionFlattenOperation, IOptionIntersectionOperation, IOptionUnionOperation
    {
        /// <summary>The option type that this class has operations for.</summary>
        public Type OptionType => typeof(IMoveOption);
        /// <summary>Flatten to simpler instance.</summary>
        public IOption Flatten(IOption o) => o is IMoveOption c ? o is MoveOption ? /*already flattened*/o : /*new instance*/new MoveOption(c.CanMove) : throw new InvalidCastException($"{typeof(IMoveOption)} expected.");
        /// <summary>Intersection of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IOption Intersection(IOption o1, IOption o2) => o1 is IMoveOption c1 && o2 is IMoveOption c2 ? new MoveOption(c1.CanMove && c2.CanMove) : throw new InvalidCastException($"{typeof(IMoveOption)} expected.");
        /// <summary>Union of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IOption Union(IOption o1, IOption o2) => o1 is IMoveOption c1 && o2 is IMoveOption c2 ? new MoveOption(c1.CanMove || c2.CanMove) : throw new InvalidCastException($"{typeof(IMoveOption)} expected.");
    }

    /// <summary>File system option for move/rename.</summary>
    public class MoveOption : IMoveOption
    {
        internal static IMoveOption move = new MoveOption(true);
        internal static IMoveOption noMove = new MoveOption(false);

        /// <summary>Move and rename is allowed.</summary>
        public static IOption Move => move;
        /// <summary>Move and rename not allowed.</summary>
        public static IOption NoMove => noMove;

        /// <summary>Has Move capability.</summary>
        public bool CanMove { get; protected set; }

        /// <summary>Create file system option for move/rename.</summary>
        public MoveOption(bool canMove)
        {
            CanMove = canMove;
        }

        /// <inheritdoc/>
        public override string ToString() => CanMove ? "CanMove" : "NoMove";
    }
}
