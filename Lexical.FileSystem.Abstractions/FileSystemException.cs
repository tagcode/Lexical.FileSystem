// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           20.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace Lexical.FileSystem
{
    /// <summary>
    /// FileSystem specific exception. 
    /// 
    /// Addresses more specific errors situations that generic <see cref="IOException"/> doesn't cover.
    /// </summary>
    public abstract class FileSystemException : IOException
    {
        /// <summary>
        /// (optional) Error related filesystem.
        /// </summary>
        protected internal IFileSystem filesystem;

        /// <summary>
        /// (optional) Error related file path.
        /// </summary>
        protected internal string path;

        /// <summary>
        /// (optional) Error related filesystem.
        /// </summary>
        public virtual IFileSystem FileSystem => filesystem;

        /// <summary>
        /// (optional) Error related file path.
        /// </summary>
        public virtual string Path => path;

        /// <summary>Error message</summary>
        public override string Message
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(String.IsNullOrEmpty(base.Message) ? base.Message : GetType().Name);
                if (Path != null)
                {
                    sb.Append(": ");
                    sb.Append(Path);
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Create filesystem exception.
        /// </summary>
        /// <param name="filesystem">(optional) error related filesystem</param>
        /// <param name="path">(optional) error related file path</param>
        /// <param name="message">Message</param>
        /// <param name="innerException">(optional) inner exception</param>
        public FileSystemException(IFileSystem filesystem = null, string path = null, string message = "", Exception innerException = null) : base(message, innerException)
        {
            this.filesystem = filesystem;
            this.path = path;
        }

        /// <summary>Deserialize exception.</summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected FileSystemException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// No read access to a file.
    /// </summary>
    public class FileSystemExceptionNoReadAccess : FileSystemException
    {
        /// <summary>
        /// Create no read access error.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path"></param>
        public FileSystemExceptionNoReadAccess(IFileSystem filesystem = null, string path = null) : base(filesystem, path, "No read access") { }

        /// <inheritdoc/>
        protected FileSystemExceptionNoReadAccess(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// No write access to a file.
    /// </summary>
    public class FileSystemExceptionNoWriteAccess : FileSystemException
    {
        /// <summary>
        /// Create no write access error.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path"></param>
        public FileSystemExceptionNoWriteAccess(IFileSystem filesystem = null, string path = null) : base(filesystem, path, "No write access") { }

        /// <inheritdoc/>
        protected FileSystemExceptionNoWriteAccess(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// File exists when not expected
    /// </summary>
    public class FileSystemExceptionFileExists : FileSystemException
    {
        /// <summary>
        /// Create exception
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path"></param>
        public FileSystemExceptionFileExists(IFileSystem filesystem = null, string path = null) : base(filesystem, path, "File exists") { }

        /// <inheritdoc/>
        protected FileSystemExceptionFileExists(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// Invalid name for file or directory
    /// </summary>
    public class FileSystemExceptionInvalidName : FileSystemException
    {
        /// <summary>
        /// Create exception
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path"></param>
        public FileSystemExceptionInvalidName(IFileSystem filesystem = null, string path = null) : base(filesystem, path, "File exists") { }

        /// <inheritdoc/>
        protected FileSystemExceptionInvalidName(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// Directory exists when not expected
    /// </summary>
    public class FileSystemExceptionDirectoryExists : FileSystemException
    {
        /// <summary>
        /// Create exception
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path"></param>
        public FileSystemExceptionDirectoryExists(IFileSystem filesystem = null, string path = null) : base(filesystem, path, "Directory exists") { }

        /// <inheritdoc/>
        protected FileSystemExceptionDirectoryExists(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// Utilities for <see cref="FileSystemException"/>.
    /// </summary>
    public static class FileSystemExceptionUtil
    {
        /// <summary>
        /// Set the filesystem reference of <paramref name="exception"/>.
        /// 
        /// This allows filesystem implementations that compose other implementations to update references.
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="filesystem"></param>
        /// <param name="path"></param>
        /// <returns>false</returns>
        public static bool Set(this FileSystemException exception, IFileSystem filesystem, string path)
        {
            exception.filesystem = filesystem;
            exception.path = path;
            return false;
        }

    }

}
