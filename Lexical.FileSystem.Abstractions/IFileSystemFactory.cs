// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           7.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;

namespace Lexical.FileSystem
{
    // <doc>
    /// <summary>
    /// Root interface for file-system factory interfaces. 
    /// 
    /// See sub-interfaces:
    /// <list type="bullet">
    /// <see cref="IFileSystemFactoryReference"/>
    /// </list>
    /// </summary>
    public interface IFileSystemFactory
    {
    }

    /// <summary>
    /// Constructs <see cref="IFileSystem"/> from reference.
    /// </summary>
    public interface IFileSystemFactoryReference
    {
        /// <summary>
        /// Create file-system using <paramref name="fileSystemReference"/>.
        /// </summary>
        /// <param name="fileSystemReference">File-system reference, for example "file://c:/Temp/"</param>
        /// <returns>File-system</returns>
        /// <exception cref="NotSupportedException">If not supported </exception>
        IFileSystem CreateFileSystem(string fileSystemReference);

        /// <summary>
        /// Extract <paramref name="fileReference"/> into two parts, file-system reference and file-reference.
        /// 
        /// Local OS file references is typically broken into the the last directory.
        /// For example "file://c:/temp/file.txt" -> "file://c:/temp/" and "file.txt".
        /// </summary>
        /// <param name="fileReference">Reference to file, for example "file://c:/temp/file.txt"</param>
        /// <returns>Tuple with file-system reference and file-reference</returns>
        /// <exception cref="NotSupportedException">If not supported </exception>
        (string, string) ExtractReferences(string fileReference);
    }
    // </doc>

}
