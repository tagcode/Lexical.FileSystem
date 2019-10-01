// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           1.10.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;

namespace Lexical.FileSystem.Option
{
    /// <summary><see cref="IFileSystemOptionBrowse"/> operations.</summary>
    public class FileSystemOptionOperationBrowse : IFileSystemOptionOperationFlatten, IFileSystemOptionOperationIntersection, IFileSystemOptionOperationUnion
    {
        /// <summary>The option type that is managed.</summary>
        public Type OptionType => typeof(IFileSystemOptionBrowse);

        /// <summary>Flatten to simpler instance.</summary>
        public IFileSystemOption Flatten(IFileSystemOption o)
            => o is IFileSystemOptionBrowse b ? o is FileSystemOptionBrowse ? /*already flattened*/o : /*new instance*/new FileSystemOptionBrowse(b.CanBrowse, b.CanGetEntry) : throw new InvalidCastException($"{typeof(IFileSystemOptionBrowse)} expected.");

        /// <summary>Intersection of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IFileSystemOption Intersection(IFileSystemOption o1, IFileSystemOption o2)
            => o1 is IFileSystemOptionBrowse b1 && o2 is IFileSystemOptionBrowse b2 ? new FileSystemOptionBrowse(b1.CanBrowse && b2.CanBrowse, b1.CanGetEntry && b2.CanGetEntry) : throw new InvalidCastException($"{typeof(IFileSystemOptionBrowse)} expected.");

        /// <summary>Union of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IFileSystemOption Union(IFileSystemOption o1, IFileSystemOption o2)
            => o1 is IFileSystemOptionBrowse b1 && o2 is IFileSystemOptionBrowse b2 ? new FileSystemOptionBrowse(b1.CanBrowse || b2.CanBrowse, b1.CanGetEntry || b2.CanGetEntry) : throw new InvalidCastException($"{typeof(IFileSystemOptionBrowse)} expected.");
    }
}
