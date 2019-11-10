// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Decoration;
using System;
using System.IO;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Base implementation for <see cref="IEvent"/> classes. Entry is a snapshot at the time of creation.
    /// 
    /// See sub-classes:
    /// <list type="bullet">
    ///     <item><see cref="FileEntry"/></item>
    ///     <item><see cref="DirectoryEntry"/></item>
    ///     <item><see cref="DriveEntry"/></item>
    ///     <item><see cref="EntryDecoration"/></item>
    /// </list>
    /// </summary>
    public abstract class EntryBase : IEntry
    {
        /// <summary>
        /// (optional) Associated file system.
        /// </summary>
        public IFileSystem FileSystem { get; protected set; }

        /// <summary>
        /// Path that is relative to the <see cref="IFileSystem"/>.
        /// Separator is "/".
        /// </summary>
        public string Path { get; protected set; }

        /// <summary>
        /// Entry name in its parent context.
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Date time of last modification.
        /// </summary>
        public DateTimeOffset LastModified { get; protected set; }

        /// <summary>
        /// Last access time of entry. If Unknown returns <see cref="DateTimeOffset.MinValue"/>.
        /// </summary>
        public DateTimeOffset LastAccess { get; protected set; }

        /// <summary>
        /// Create entry
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="lastModified"></param>
        /// <param name="lastAccess"></param>
        public EntryBase(IFileSystem filesystem, string path, string name, DateTimeOffset lastModified, DateTimeOffset lastAccess)
        {
            FileSystem = filesystem;
            Path = path;
            Name = name;
            LastModified = lastModified;
            LastAccess = lastAccess;
        }

        /// <summary>
        /// Print info.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => Path;
    }

}

