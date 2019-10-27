// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           8.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Package;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Lexical.FileSystem
{
    /// <summary>
    /// <see cref="IPackageLoader"/> extension methods.
    /// </summary>
    public static class PackageLoaderExtensions
    {
        /// <summary>
        /// Try to read supported file formats from the regular expression pattern.
        /// </summary>
        /// <param name="packageLoader"></param>
        /// <returns>for example "dll"</returns>
        public static IEnumerable<string> GetExtensions(this IPackageLoader packageLoader)
            => packageLoader.FileExtensionPattern.Split('|').Select(ext => ext.Replace(@"\", ""));

        /// <summary>
        /// Try to read supported file formats from the regular expression pattern.
        /// </summary>
        /// <param name="packageLoaders"></param>
        /// <returns>for example "dll", "zip", ... </returns>
        public static string[] GetExtensions(this IEnumerable<IPackageLoader> packageLoaders)
            => packageLoaders.SelectMany(pl => pl.FileExtensionPattern.Split('|')).Select(ext => ext.Replace(@"\", "")).ToArray();

        /// <summary>
        /// Sort packageloaders by the file extensions they support.
        /// </summary>
        /// <param name="packageLoaders"></param>
        /// <returns>map, e.g. { "dll", Dll.Singleton }</returns>
        public static IReadOnlyDictionary<string, IPackageLoader> SortByExtension(this IEnumerable<IPackageLoader> packageLoaders)
        {
            Dictionary<string, IPackageLoader> result = new Dictionary<string, IPackageLoader>();

            // Sort by extension
            foreach (IPackageLoader pl in packageLoaders)
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
    public class FileSystemOptionOperationAutoMount : IFileSystemOptionOperationFlatten, IFileSystemOptionOperationIntersection, IFileSystemOptionOperationUnion
    {
        /// <summary>The option type that this class has operations for.</summary>
        public Type OptionType => typeof(IFileSystemOptionAutoMount);
        /// <summary>Flatten to simpler instance.</summary>
        public IFileSystemOption Flatten(IFileSystemOption o) => o is IFileSystemOptionAutoMount c ? o is FileSystemOptionPackageLoader ? /*already flattened*/o : /*new instance*/new FileSystemOptionPackageLoader(c.AutoMounters) : throw new InvalidCastException($"{typeof(IFileSystemOptionAutoMount)} expected.");
        /// <summary>Intersection of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IFileSystemOption Intersection(IFileSystemOption o1, IFileSystemOption o2)
        {
            IFileSystemOptionAutoMount p1 = (IFileSystemOptionAutoMount)o1, p2 = (IFileSystemOptionAutoMount)o2;
            if (p1.AutoMounters == null) return p2;
            if (p2.AutoMounters == null) return p1;

            IPackageLoader[] list = p1.AutoMounters.Where(pl => p2.AutoMounters.Contains(pl)).ToArray();
            return new FileSystemOptionPackageLoader(list);
        }
        /// <summary>Union of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IFileSystemOption Union(IFileSystemOption o1, IFileSystemOption o2)
        {
            IFileSystemOptionAutoMount p1 = (IFileSystemOptionAutoMount)o1, p2 = (IFileSystemOptionAutoMount)o2;
            if (p1.AutoMounters == null) return p2;
            if (p2.AutoMounters == null) return p1;
            if (p1.AutoMounters.Length == 0) return p2;
            if (p2.AutoMounters.Length == 0) return p1;
            Dictionary<string, IPackageLoader> byExtension = new Dictionary<string, IPackageLoader>(StringComparer.OrdinalIgnoreCase);
            foreach (var pl in p1.AutoMounters.Concat(p2.AutoMounters))
            {
                foreach (string extension in pl.GetExtensions())
                {
                    if (byExtension.ContainsKey(extension)) throw new FileSystemExceptionOptionOperationNotSupported(null, null, o2, typeof(IFileSystemOptionAutoMount), typeof(IFileSystemOptionOperationUnion));
                    byExtension[extension] = pl;
                }
            }
            IPackageLoader[] array = byExtension.Values.Distinct().ToArray();
            return new FileSystemOptionPackageLoader(array);
        }
    }

    /// <summary>Option for auto-mounted packages.</summary>
    public class FileSystemOptionPackageLoader : IFileSystemOptionAutoMount
    {
        /// <summary>Package loaders that can mount package files, such as .zip.</summary>
        public IPackageLoader[] AutoMounters { get; protected set; }
        /// <summary>Create option for auto-mounted packages.</summary>
        public FileSystemOptionPackageLoader(IPackageLoader[] packageLoaders) { AutoMounters = packageLoaders; }
    }

}
