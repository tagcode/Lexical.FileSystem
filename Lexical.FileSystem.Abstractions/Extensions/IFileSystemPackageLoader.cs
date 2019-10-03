// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           8.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace Lexical.FileSystem
{
    /// <summary>
    /// <see cref="IFileSystemPackageLoader"/> extension methods.
    /// </summary>
    public static class PackageLoaderExtensions
    {
        /// <summary>
        /// Try to read supported file formats from the regular expression pattern.
        /// </summary>
        /// <param name="packageLoader"></param>
        /// <returns>for example "dll"</returns>
        public static IEnumerable<string> GetExtensions(this IFileSystemPackageLoader packageLoader)
            => packageLoader.FileExtensionPattern.Split('|').Select(ext => ext.Replace(@"\", ""));

        /// <summary>
        /// Try to read supported file formats from the regular expression pattern.
        /// </summary>
        /// <param name="packageLoaders"></param>
        /// <returns>for example "dll", "zip", ... </returns>
        public static string[] GetExtensions(this IEnumerable<IFileSystemPackageLoader> packageLoaders)
            => packageLoaders.SelectMany(pl => pl.FileExtensionPattern.Split('|')).Select(ext => ext.Replace(@"\", "")).ToArray();

        /// <summary>
        /// Sort packageloaders by the file extensions they support.
        /// </summary>
        /// <param name="packageLoaders"></param>
        /// <returns>map, e.g. { "dll", Dll.Singleton }</returns>
        public static IReadOnlyDictionary<string, IFileSystemPackageLoader> SortByExtension(this IEnumerable<IFileSystemPackageLoader> packageLoaders)
        {
            Dictionary<string, IFileSystemPackageLoader> result = new Dictionary<string, IFileSystemPackageLoader>();

            // Sort by extension
            foreach (IFileSystemPackageLoader pl in packageLoaders)
                foreach (string extension in pl.GetExtensions())
                    result[extension] = pl;

            return result;
        }
    }

    /// <summary>Package loading failed.</summary>
    public class FileSystemExceptionPackaageLoadError : FileSystemException
    {
        /// <summary>Create Package loading failed exception.</summary>
        public FileSystemExceptionPackaageLoadError(IFileSystem filesystem = null, string path = null) : base(filesystem, path, "Package loading failed") { }
        /// <summary>Create Package loading failed exception.</summary>
        protected FileSystemExceptionPackaageLoadError(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary><see cref="IFileSystemOptionOpen"/> operations.</summary>
    public class FileSystemOptionOperationPackageLoader : IFileSystemOptionOperationFlatten, IFileSystemOptionOperationIntersection, IFileSystemOptionOperationUnion
    {
        /// <summary>The option type that this class has operations for.</summary>
        public Type OptionType => typeof(IFileSystemOptionPackageLoader);
        /// <summary>Flatten to simpler instance.</summary>
        public IFileSystemOption Flatten(IFileSystemOption o) => o is IFileSystemOptionPackageLoader c ? o is FileSystemOptionPackageLoader ? /*already flattened*/o : /*new instance*/new FileSystemOptionPackageLoader(c.PackageLoaders) : throw new InvalidCastException($"{typeof(IFileSystemOptionPackageLoader)} expected.");
        /// <summary>Intersection of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IFileSystemOption Intersection(IFileSystemOption o1, IFileSystemOption o2)
        {
            IFileSystemOptionPackageLoader p1 = (IFileSystemOptionPackageLoader)o1, p2 = (IFileSystemOptionPackageLoader)o2;
            if (p1.PackageLoaders == null) return p2;
            if (p2.PackageLoaders == null) return p1;

            IFileSystemPackageLoader[] list = p1.PackageLoaders.Where(pl => p2.PackageLoaders.Contains(pl)).ToArray();
            return new FileSystemOptionPackageLoader(list);
        }
        /// <summary>Union of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IFileSystemOption Union(IFileSystemOption o1, IFileSystemOption o2)
        {
            IFileSystemOptionPackageLoader p1 = (IFileSystemOptionPackageLoader)o1, p2 = (IFileSystemOptionPackageLoader)o2;
            if (p1.PackageLoaders == null) return p2;
            if (p2.PackageLoaders == null) return p1;
            if (p1.PackageLoaders.Length == 0) return p2;
            if (p2.PackageLoaders.Length == 0) return p1;
            Dictionary<string, IFileSystemPackageLoader> byExtension = new Dictionary<string, IFileSystemPackageLoader>(StringComparer.OrdinalIgnoreCase);
            foreach (var pl in p1.PackageLoaders.Concat(p2.PackageLoaders))
            {
                foreach (string extension in pl.GetExtensions())
                {
                    if (byExtension.ContainsKey(extension)) throw new FileSystemExceptionOptionOperationNotSupported(null, null, o2, typeof(IFileSystemOptionPackageLoader), typeof(IFileSystemOptionOperationUnion));
                    byExtension[extension] = pl;
                }
            }
            IFileSystemPackageLoader[] array = byExtension.Values.Distinct().ToArray();
            return new FileSystemOptionPackageLoader(array);
        }
    }

    /// <summary>Option for auto-mounted packages.</summary>
    public class FileSystemOptionPackageLoader : IFileSystemOptionPackageLoader
    {
        /// <summary>Package loaders that can mount package files, such as .zip.</summary>
        public IFileSystemPackageLoader[] PackageLoaders { get; protected set; }
        /// <summary>Create option for auto-mounted packages.</summary>
        public FileSystemOptionPackageLoader(IFileSystemPackageLoader[] packageLoaders) { PackageLoaders = packageLoaders; }
    }

}
