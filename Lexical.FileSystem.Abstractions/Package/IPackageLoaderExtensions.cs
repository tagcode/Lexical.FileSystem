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
        /// Can open file.
        /// </summary>
        /// <param name="packageLoader"></param>
        public static bool CanOpenFile(this IPackageLoader packageLoader)
            => packageLoader is IPackageLoaderOpenFile pl ? pl.CanOpenFile : false;

        /// <summary>
        /// Can load file.
        /// </summary>
        /// <param name="packageLoader"></param>
        public static bool CanLoadFile(this IPackageLoader packageLoader)
            => packageLoader is IPackageLoaderLoadFile pl ? pl.CanLoadFile : false;

        /// <summary>
        /// Can use package from stream.
        /// </summary>
        /// <param name="packageLoader"></param>
        public static bool IPackageLoaderUseStream(this IPackageLoader packageLoader)
            => packageLoader is IPackageLoaderUseStream pl ? pl.CanUseStream : false;

        /// <summary>
        /// Can load from stream.
        /// </summary>
        /// <param name="packageLoader"></param>
        public static bool CanLoadFromStream(this IPackageLoader packageLoader)
            => packageLoader is IPackageLoaderLoadFromStream pl ? pl.CanLoadFromStream : false;

        /// <summary>
        /// Can create package.
        /// </summary>
        /// <param name="packageLoader"></param>
        public static bool CanCreate(this IPackageLoader packageLoader)
            => packageLoader is IPackageLoaderCreate pl ? pl.CanCreate : false;

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

    /// <summary><see cref="IOpenOption"/> operations.</summary>
    public class FileSystemOptionOperationAutoMount : IOptionFlattenOperation, IOptionIntersectionOperation, IOptionUnionOperation
    {
        /// <summary>The option type that this class has operations for.</summary>
        public Type OptionType => typeof(IAutoMountOption);
        /// <summary>Flatten to simpler instance.</summary>
        public IOption Flatten(IOption o) => o is IAutoMountOption c ? o is FileSystemOptionPackageLoader ? /*already flattened*/o : /*new instance*/new FileSystemOptionPackageLoader(c.AutoMounters) : throw new InvalidCastException($"{typeof(IAutoMountOption)} expected.");
        /// <summary>Intersection of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IOption Intersection(IOption o1, IOption o2)
        {
            IAutoMountOption p1 = (IAutoMountOption)o1, p2 = (IAutoMountOption)o2;
            if (p1.AutoMounters == null) return p2;
            if (p2.AutoMounters == null) return p1;

            IPackageLoader[] list = p1.AutoMounters.Where(pl => p2.AutoMounters.Contains(pl)).ToArray();
            return new FileSystemOptionPackageLoader(list);
        }
        /// <summary>Union of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IOption Union(IOption o1, IOption o2)
        {
            IAutoMountOption p1 = (IAutoMountOption)o1, p2 = (IAutoMountOption)o2;
            if (p1.AutoMounters == null) return p2;
            if (p2.AutoMounters == null) return p1;
            if (p1.AutoMounters.Length == 0) return p2;
            if (p2.AutoMounters.Length == 0) return p1;
            Dictionary<string, IPackageLoader> byExtension = new Dictionary<string, IPackageLoader>(StringComparer.OrdinalIgnoreCase);
            foreach (var pl in p1.AutoMounters.Concat(p2.AutoMounters))
            {
                foreach (string extension in pl.GetExtensions())
                {
                    if (byExtension.ContainsKey(extension)) throw new FileSystemExceptionOptionOperationNotSupported(null, null, o2, typeof(IAutoMountOption), typeof(IOptionUnionOperation));
                    byExtension[extension] = pl;
                }
            }
            IPackageLoader[] array = byExtension.Values.Distinct().ToArray();
            return new FileSystemOptionPackageLoader(array);
        }
    }

    /// <summary>Option for auto-mounted packages.</summary>
    public class FileSystemOptionPackageLoader : IAutoMountOption
    {
        /// <summary>Package loaders that can mount package files, such as .zip.</summary>
        public IPackageLoader[] AutoMounters { get; protected set; }
        /// <summary>Create option for auto-mounted packages.</summary>
        public FileSystemOptionPackageLoader(IPackageLoader[] packageLoaders) { AutoMounters = packageLoaders; }
    }

}
