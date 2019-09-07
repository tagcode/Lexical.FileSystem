// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           7.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.IO;
using System.Reflection;
using System.Security;

namespace Lexical.FileSystem
{
    // <doc>
    /// <summary>
    /// File system that has a file-system reference.
    /// </summary>
    public interface IFileSystemReference : IFileSystem
    {
        /// <summary>
        /// Has Reference capability.
        /// </summary>
        bool CanReference { get; }

        /// <summary>
        /// Reference to file-system. Note, this doesn't include path within the file-system.
        /// 
        /// File based reference uses "file://" URL schema. The separator is "/". The reference always ends with "/", for example "file://C:/Temp/".
        /// Reference can be relative path or absolute path. Linux root starts with three slashes "file:///tmp/".
        /// OS-Root uses reference "file://".
        /// 
        /// <see cref="Assembly"/> uses reference "assembly://[full assembly name]/". For example "assembly://[ConsoleApp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]/".
        /// 
        /// File-system reference scheme should be designed so that the reference can be concatenated with file path.
        /// </summary>
        /// <exception cref="NotSupportedException">If file-system cannot provide reference</exception>
        string Reference { get; }
    }
    // </doc>

    /// <summary>
    /// Extension methods for <see cref="IFileSystem"/>.
    /// </summary>
    public static partial class IFileSystemExtensions
    {
        /// <summary>
        /// Test if <paramref name="fileSystem"/> has Reference capability.
        /// <param name="fileSystem"></param>
        /// </summary>
        /// <returns>true, if has Reference capability</returns>
        public static bool CanReference(this IFileSystem fileSystem)
            => fileSystem is IFileSystemReference referable ? referable.CanReference : false;

        /// <summary>
        /// Reference to file-system. Note, this doesn't include path within the file-system.
        /// 
        /// File based reference uses "file://" URL schema. The separator is "/". The reference always ends with "/", for example "file://C:/Temp/".
        /// Reference can be relative path or absolute path. Linux root starts with three slashes "file:///tmp/".
        /// OS-Root uses reference "file://".
        /// 
        /// <see cref="Assembly"/> uses reference "assembly://[full assembly name]/".
        /// </summary>
        /// <exception cref="NotSupportedException">If file-system cannot provide reference</exception>
        public static String Reference(this IFileSystem fileSystem)
        {
            if (fileSystem is IFileSystemReference referable) return referable.Reference();
            else throw new NotSupportedException(nameof(Reference));
        }
    }

}
