// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           1.10.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;

namespace Lexical.FileSystem.Option
{
    /// <summary>
    /// Contains operations to <see cref="IFileSystemOptionBrowse"/> instances.
    /// </summary>
    public class FileSystemOptionOperationBrowse : IFileSystemOptionOperationFlatten, IFileSystemOptionOperationIntersection, IFileSystemOptionOperationUnion
    {
        /// <summary>The option type that is managed.</summary>
        public Type OptionType => typeof(IFileSystemOptionBrowse);

        /// <summary>
        /// Flatten to simpler instance.
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public IFileSystemOption Flatten(IFileSystemOption o)
        {
            if (o is IFileSystemOptionBrowse b) return o is FileSystemOptionBrowse ? /*already flattened*/o : /*new instance*/new FileSystemOptionBrowse(b.CanBrowse, b.CanGetEntry);
            throw new ArgumentException($"{typeof(IFileSystemOptionBrowse)} expected.");
        }

        /// <summary>
        /// Intersection of <paramref name="o1"/> and <paramref name="o2"/>.
        /// </summary>
        /// <param name="o1"></param>
        /// <param name="o2"></param>
        /// <returns></returns>
        public IFileSystemOption Intersection(IFileSystemOption o1, IFileSystemOption o2)
        {
            IFileSystemOptionBrowse b1 = (IFileSystemOptionBrowse)o1, b2 = (IFileSystemOptionBrowse)o2;
            return new FileSystemOptionBrowse(b1.CanBrowse && b2.CanBrowse, b1.CanGetEntry && b2.CanGetEntry);
        }

        /// <summary>
        /// Union of <paramref name="o1"/> and <paramref name="o2"/>.
        /// </summary>
        /// <param name="o1"></param>
        /// <param name="o2"></param>
        /// <returns></returns>
        public IFileSystemOption Union(IFileSystemOption o1, IFileSystemOption o2)
        {
            IFileSystemOptionBrowse b1 = (IFileSystemOptionBrowse)o1, b2 = (IFileSystemOptionBrowse)o2;
            return new FileSystemOptionBrowse(b1.CanBrowse || b2.CanBrowse, b1.CanGetEntry || b2.CanGetEntry);
        }
    }
}
