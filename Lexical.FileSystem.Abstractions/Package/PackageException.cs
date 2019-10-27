// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           27.10.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Runtime.Serialization;

namespace Lexical.FileSystem.Package
{
    /// <summary>
    /// Generic <see cref="IPackageLoader" /> related exception.
    /// </summary>
    public class PackageException : FileSystemException
    {
        /// <summary>
        /// (optional) Associated package loader.
        /// </summary>
        protected internal IPackageLoader packageLoader;

        /// <summary>
        /// (optional) Associated package loader.
        /// </summary>
        public virtual IPackageLoader PackageLoader => packageLoader;

        /// <summary>
        /// Create exception.
        /// </summary>
        /// <param name="filesystem">(optional) associated filesystem</param>
        /// <param name="path">(optional) associated path</param>
        /// <param name="message">(optional) message</param>
        /// <param name="innerException">(optional) inner exception</param>
        /// <param name="packageLoader">(optional) associated package loader</param>
        public PackageException(IFileSystem filesystem = null, string path = null, string message = "", Exception innerException = null, IPackageLoader packageLoader = null) : base(filesystem, path, message, innerException) { this.packageLoader = packageLoader; }

        /// <summary>
        /// Create exception.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected PackageException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <summary>
        /// Generic file related error.
        /// </summary>
        public abstract class FileError : PackageException
        {
            /// <summary>
            /// (Optional) File path that is associated to this error.
            /// </summary>
            public readonly string FilePath;

            /// <summary>
            /// Create file related error.
            /// </summary>
            /// <param name="filesystem">(optional) associated filesystem</param>
            /// <param name="path">(optional) associated path</param>
            /// <param name="message">(optional) message</param>
            /// <param name="innerException">(optional) inner exception</param>
            /// <param name="packageLoader">(optional) associated package loader</param>
            public FileError(IFileSystem filesystem = null, string path = null, string message = "", Exception innerException = null, IPackageLoader packageLoader = null) : base(filesystem, path, message, innerException) { this.packageLoader = packageLoader; }

            /// <summary>
            /// Derialize exception from <paramref name="context"/>.
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            protected FileError(SerializationInfo info, StreamingContext context) : base(info, context) { this.FilePath = info.GetString(nameof(FilePath)); }

            /// <summary>
            /// Serialize object data to <paramref name="context"/>.
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            public override void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue(nameof(FilePath), FilePath);
                base.GetObjectData(info, context);
            }
        }

        /// <summary>
        /// Could not find suitable load capability in <see cref="IPackageLoader"/>.
        /// </summary>
        public class NoSuitableLoadCapability : FileError
        {
            /// <summary>
            /// Create load capability error.
            /// </summary>
            /// <param name="filesystem">(optional) associated filesystem</param>
            /// <param name="path">(optional) associated path</param>
            /// <param name="message">(optional) message</param>
            /// <param name="innerException">(optional) inner exception</param>
            /// <param name="packageLoader">(optional) associated package loader</param>
            public NoSuitableLoadCapability(IFileSystem filesystem = null, string path = null, string message = "", Exception innerException = null, IPackageLoader packageLoader = null) : base(filesystem, path, message, innerException) { this.packageLoader = packageLoader; }

            /// <summary>
            /// Derialize exception from <paramref name="context"/>.
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            protected NoSuitableLoadCapability(SerializationInfo info, StreamingContext context) : base(info, context) { }
        }

        /// <summary>
        /// Package load failed error.
        /// </summary>
        public class LoadError : FileError
        {
            /// <summary>
            /// Create load capability error.
            /// </summary>
            /// <param name="filesystem">(optional) associated filesystem</param>
            /// <param name="path">(optional) associated path</param>
            /// <param name="message">(optional) message</param>
            /// <param name="innerException">(optional) inner exception</param>
            /// <param name="packageLoader">(optional) associated package loader</param>
            public LoadError(IFileSystem filesystem = null, string path = null, string message = "", Exception innerException = null, IPackageLoader packageLoader = null) : base(filesystem, path, message, innerException) { this.packageLoader = packageLoader; }

            /// <summary>
            /// Derialize exception from <paramref name="context"/>.
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            protected LoadError(SerializationInfo info, StreamingContext context) : base(info, context) { }
        }
    }
}
