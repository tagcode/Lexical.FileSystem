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
    // <IFileSystemPackageLoader>
    /// <summary>
    /// Interace for loaders that read package files, such as .zip, as <see cref="IFileSystem"/>.
    /// 
    /// The implementing class must implement one or more of the following sub-interfaces:
    /// <list type="bullet">
    ///    <item><see cref="IFileSystemPackageLoaderOpenFile"/></item>
    ///    <item><see cref="IFileSystemPackageLoaderLoadFile"/></item>
    ///    <item><see cref="IFileSystemPackageLoaderUseStream"/></item>
    ///    <item><see cref="IFileSystemPackageLoaderLoadFromStream"/></item>
    ///    <item><see cref="IFileSystemPackageLoaderUseBytes"/></item>
    /// </list>
    /// </summary>
    public interface IFileSystemPackageLoader
    {
        /// <summary>
        /// The file extension(s) this format can open.
        /// 
        /// The string is a regular expression. 
        /// For example "\.zip" or "\.zip|\.7z|\.tar\.gz"
        /// 
        /// Pattern will be used as case insensitive, so the case doesn't matter, but lower is preferred.
        /// 
        /// Do not add named groups. For example "(?&lt;name&gt;..)".
        /// 
        /// Unnamed groups are, however, allowed. For example: "\.zip(\.tmp)?"
        /// </summary>
        String FileExtensionPattern { get; }
    }
    // </IFileSystemPackageLoader>

    // <IFileSystemPackageLoaderOpenFile>
    /// <summary>
    /// Package loader that can open a package file as <see cref="IFileSystem"/>.
    /// </summary>
    public interface IFileSystemPackageLoaderOpenFile : IFileSystemPackageLoader
    {
        /// <summary>
        /// Open a package file and keep it open until the file system is disposed. 
        /// Return <see cref="IFileSystem"/> that represents the contents of the open file.
        /// 
        /// The caller is responsible for disposing the returned file system if it implements <see cref="IDisposable"/>.
        /// </summary>
        /// <param name="filepath">data to read from</param>
        /// <param name="packageInfo">(optional) Information about packge that is being opened</param>
        /// <returns>file system</returns>
        /// <exception cref="Exception">If there was unexpected error, such as IOException</exception>
        /// <exception cref="InvalidOperationException">If this load method is not supported.</exception>
        /// <exception cref="IOException">Problem with io stream</exception>
        /// <exception cref="FileSystemExceptionPackaageLoadError">The when file format is erronous, package will not be opened as directory.</exception>
        IFileSystem OpenFile(string filepath, IPackageLoadInfo packageInfo = null);
    }
    // </IFileSystemPackageLoaderOpenFile>

    // <IFileSystemPackageLoaderLoadFile>
    /// <summary>
    /// Package loader that cab load a package file completely.
    /// </summary>
    public interface IFileSystemPackageLoaderLoadFile : IFileSystemPackageLoader
    {
        /// <summary>
        /// Load a package file completely. The implementation must close the file before the call returns.
        /// Return <see cref="IFileSystem"/> that represents the contents of the open file.
        /// 
        /// The caller is responsible for disposing the returned file system if it implements <see cref="IDisposable"/>.
        /// </summary>
        /// <param name="filepath">data to read from</param>
        /// <param name="packageInfo">(optional) Information about packge that is being opened</param>
        /// <returns>file system</returns>
        /// <exception cref="Exception">If there was unexpected error, such as IOException</exception>
        /// <exception cref="InvalidOperationException">If this load method is not supported.</exception>
        /// <exception cref="IOException">Problem with io stream</exception>
        /// <exception cref="FileSystemExceptionPackaageLoadError">The when file format is erronous, package will not be opened as directory.</exception>
        IFileSystem LoadFile(string filepath, IPackageLoadInfo packageInfo = null);
    }
    // </IFileSystemPackageLoaderLoadFile>

    // <IFileSystemPackageLoaderUseStream>
    /// <summary>
    /// Package loader that can open <see cref="Stream"/> to access contents of a package file.
    /// </summary>
    public interface IFileSystemPackageLoaderUseStream : IFileSystemPackageLoader
    {
        /// <summary>
        /// Use an open <paramref name="stream"/> to read contents from a package file.
        /// Return a <see cref="IFileSystem"/> that represent the contents.
        /// 
        /// The returned file system takes ownership of the stream, and must close the <paramref name="stream"/> along with the system.
        /// 
        /// <paramref name="stream"/> must be readable and seekable, <see cref="Stream.CanSeek"/> must be true.
        /// 
        /// The caller is responsible for disposing the returned file system if it implements <see cref="IDisposable"/>.
        /// 
        /// Note, open stream cannot be read concurrently from two threads and must be locked with mutually exclusive lock if two reads attempted.
        /// </summary>
        /// <param name="stream">stream to read data from. Stream must be disposed along with the returned file system.</param>
        /// <param name="packageInfo">(optional) Information about packge that is being opened</param>
        /// <returns>file system that can be disposable</returns>
        /// <exception cref="Exception">If there was unexpected error, such as IOException</exception>
        /// <exception cref="InvalidOperationException">If this load method is not supported.</exception>
        /// <exception cref="IOException">Problem with io stream</exception>
        /// <exception cref="FileSystemExceptionPackaageLoadError">The when file format is erronous, package will not be opened as directory.</exception>
        IFileSystem UseStream(Stream stream, IPackageLoadInfo packageInfo = null);
    }
    // </IFileSystemPackageLoaderUseStream>

    // <IFileSystemPackageLoaderLoadFromStream>
    /// <summary>
    /// Package loader that can load a package completely from an open <see cref="Stream"/>.
    /// </summary>
    public interface IFileSystemPackageLoaderLoadFromStream : IFileSystemPackageLoader
    {
        /// <summary>
        /// Read package completely from <paramref name="stream"/> and return representation of contents as <see cref="IFileSystem"/>.
        /// The implementation and the returned <see cref="IFileSystem"/> does not take ownership of the stream. 
        /// 
        /// The returned file system can be left to be garbage collected and doesn't need to be disposed.
        /// </summary>
        /// <param name="stream">stream to read data from. Stream doesn't need to be closed by callee, but is allowed to do so.</param>
        /// <param name="packageInfo">(optional) Information about packge that is being opened</param>
        /// <returns>file system</returns>
        /// <exception cref="Exception">If there was unexpected error, such as IOException</exception>
        /// <exception cref="InvalidOperationException">If this load method is not supported.</exception>
        /// <exception cref="IOException">Problem with io stream</exception>
        /// <exception cref="FileSystemExceptionPackaageLoadError">The when file format is erronous, package will not be opened as directory.</exception>
        IFileSystem LoadFromStream(Stream stream, IPackageLoadInfo packageInfo = null);
    }
    // </IFileSystemPackageLoaderLoadFromStream>

    // <IFileSystemPackageLoaderUseBytes>
    /// <summary>
    /// Package loader that can load a package completely from an bytes.
    /// </summary>
    public interface IFileSystemPackageLoaderUseBytes : IFileSystemPackageLoader
    {
        /// <summary>
        /// Load file system from bytes.
        /// 
        /// The caller is responsible for disposing the returned file system if it implements <see cref="IDisposable"/>.
        /// </summary>
        /// <param name="data">data to read from</param>
        /// <param name="packageInfo">(optional) Information about packge that is being opened</param>
        /// <returns>file system</returns>
        /// <exception cref="Exception">If there was unexpected error, such as IOException</exception>
        /// <exception cref="InvalidOperationException">If this load method is not supported.</exception>
        /// <exception cref="IOException">Problem with io stream</exception>
        /// <exception cref="FileSystemExceptionPackaageLoadError">The when file format is erronous, package will not be opened as directory.</exception>
        IFileSystem UseBytes(byte[] data, IPackageLoadInfo packageInfo = null);
    }
    // </IFileSystemPackageLoaderUseBytes>

    // <IPackageLoadInfo>
    /// <summary>
    /// Optional hints about the package that is being loaded.
    /// </summary>
    public interface IPackageLoadInfo
    {
        /// <summary>
        /// (optional) Path within parent file system.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// (Optional) Last modified UTC date time.
        /// </summary>
        DateTimeOffset? LastModified { get; }

        /// <summary>
        /// File length, or -1 if unknown
        /// </summary>
        long Length { get; }
    }
    // </IPackageLoadInfo>

    // <IFileSystemPackageLoader>
    /// <summary>Option for auto-mounted packages.</summary>
    [Operations(typeof(FileSystemOptionOperationPackageLoader))]
    public interface IFileSystemOptionPackageLoader : IFileSystemOption
    {
        /// <summary>Package loaders that can mount package files, such as .zip.</summary>
        IFileSystemPackageLoader[] PackageLoaders { get; }
    }
    // </IFileSystemPackageLoader>

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
