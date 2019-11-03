// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           20.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
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
    public class FileSystemException : IOException
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
                sb.Append(base.Message);
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

        /// <summary>
        /// Create filesystem exception.
        /// </summary>
        /// <param name="filesystem">(optional) error related filesystem</param>
        /// <param name="path">(optional) error related file path</param>
        /// <param name="message">Message</param>
        /// <param name="hresult"></param>
        public FileSystemException(IFileSystem filesystem, string path, string message, int hresult) : base(message, hresult)
        {
            this.filesystem = filesystem;
            this.path = path;
        }

        /// <summary>Deserialize exception.</summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected FileSystemException(SerializationInfo info, StreamingContext context) : base(info, context) { this.path = info.GetString(nameof(Path)); }

        /// <summary>
        /// Serialize object data to <paramref name="context"/>.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Path), path);
            base.GetObjectData(info, context);
        }

        // <summary>Print info</summary>
        //public override string ToString() => $"{GetType()}(FileSystem={FileSystem}, Path={Path}, Message={base.Message}, InnerException={InnerException})";

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
    /// Entry already exists when not expected
    /// </summary>
    public class FileSystemExceptionEntryExists : FileSystemException
    {
        /// <summary>
        /// Create exception
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path"></param>
        /// <param name="message"></param>
        public FileSystemExceptionEntryExists(IFileSystem filesystem = null, string path = null, string message = null) : base(filesystem, path, message ?? "Entry exists") { }

        /// <inheritdoc/>
        protected FileSystemExceptionEntryExists(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// File exists when not expected
    /// </summary>
    public class FileSystemExceptionFileExists : FileSystemExceptionEntryExists
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
    public class FileSystemExceptionInvalidName : FileSystemExceptionEntryExists
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

    /// <summary>
    /// Requested <see cref="IFileSystemOption"/> is not supported.
    /// </summary>
    public class FileSystemExceptionOptionNotSupported : FileSystemException
    {
        /// <summary>
        /// 
        /// </summary>
        public IFileSystemOption Option { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public Type OptionType { get; protected set; }

        /// <summary>
        /// Create file system option not supported error.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path">(optional) a path where the option was applied</param>
        /// <param name="option">(optional) option instance</param>
        /// <param name="optionType">The <see cref="IFileSystemOption"/> interface that was not supported</param>
        public FileSystemExceptionOptionNotSupported(IFileSystem filesystem = null, string path = null, IFileSystemOption option = null, Type optionType = null) : base(filesystem, path, "Option not supported")
        {
            Option = option;
            OptionType = optionType;
        }

        /// <inheritdoc/>
        protected FileSystemExceptionOptionNotSupported(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// Requested <see cref="IFileSystemOptionOperation"/> is not supported.
    /// </summary>
    public class FileSystemExceptionOptionOperationNotSupported : FileSystemException
    {
        /// <summary>
        /// 
        /// </summary>
        public IFileSystemOption Option { get; protected set; }

        /// <summary>
        /// Subinterface of <see cref="IFileSystemOption"/>.
        /// </summary>
        public Type OptionType { get; protected set; }

        /// <summary>
        /// Subinterface of <see cref="IFileSystemOptionOperation"/>.
        /// </summary>
        public Type OptionOperationType { get; protected set; }

        /// <summary>
        /// Create file system option
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path">(optional) a path where the option was applied</param>
        /// <param name="option">(optional) option instance</param>
        /// <param name="optionType">The <see cref="IFileSystemOption"/> interface type that was not supported</param>
        /// <param name="optionOperationType">The <see cref="IFileSystemOptionOperation"/> interface type that was not supported</param>
        public FileSystemExceptionOptionOperationNotSupported(IFileSystem filesystem = null, string path = null, IFileSystemOption option = null, Type optionType = null, Type optionOperationType = null) : base(filesystem, path, "Option operation not supported")
        {
            Option = option;
            OptionType = optionType;
            OptionOperationType = optionOperationType;
        }

        /// <inheritdoc/>
        protected FileSystemExceptionOptionOperationNotSupported(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// Out of disk space exception
    /// </summary>
    public class FileSystemExceptionOutOfDiskSpace : FileSystemException
    {
        /// <summary>
        /// Create exception
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path"></param>
        public FileSystemExceptionOutOfDiskSpace(IFileSystem filesystem = null, string path = null) : base(filesystem, path, "Out of disk space", 0x00000070 /*ERROR_DISK_FULL*/) { }

        /// <inheritdoc/>
        protected FileSystemExceptionOutOfDiskSpace(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

}
