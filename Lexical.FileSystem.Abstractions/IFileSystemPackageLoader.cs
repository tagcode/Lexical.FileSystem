// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           8.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.IO;

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

    /// <summary>Option for auto-mounted packages.</summary>
    [Operations(typeof(FileSystemOptionOperationPackageLoader))]
    // <IFileSystemOptionPackageLoader>
    public interface IFileSystemOptionPackageLoader : IFileSystemOption
    {
        /// <summary>Package loaders that can mount package files, such as .zip.</summary>
        IFileSystemPackageLoader[] PackageLoaders { get; }
    }
    // </IFileSystemOptionPackageLoader>
}
