// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.IO;
using System.Reflection;

namespace Lexical.FileSystem
{
    /// <summary>
    /// File System that represents embedded resources of an <see cref="System.Reflection.Assembly"/>.
    /// </summary>
    public class EmbeddedFileSystem : FileSystemBase, IFileSystemBrowse, IFileSystemOpen
    {
        /// <summary>
        /// Associated Assembly
        /// </summary>
        public readonly Assembly Assembly;

        /// <summary>
        /// Get capabilities.
        /// </summary>
        public override FileSystemCapabilities Capabilities => FileSystemCapabilities.Browse | FileSystemCapabilities.Open | FileSystemCapabilities.Read;

        /// <summary>
        /// Snapshot of entries.
        /// </summary>
        protected FileSystemEntry[] entries;

        /// <summary>
        /// Create embedded 
        /// </summary>
        /// <param name="assembly"></param>
        public EmbeddedFileSystem(Assembly assembly)
        {
            this.Assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        }

        /// <summary>
        /// Create a snapshot of entries.
        /// </summary>
        /// <returns></returns>
        protected FileSystemEntry[] CreateEntries()
        {
            string[] names = Assembly.GetManifestResourceNames();

            // Get file time, or use Unix time 0.
            DateTimeOffset time;
            if (Assembly.Location != null && File.Exists(Assembly.Location))
                time = new FileInfo(Assembly.Location).LastWriteTimeUtc;
            else
                time = DateTimeOffset.FromUnixTimeSeconds(0L);

            FileSystemEntry[] result = new FileSystemEntry[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                result[i] = new FileSystemEntry
                {
                    FileSystem = this,
                    LastModified = time,
                    Length = -1L,
                    Name = names[i],
                    Path = names[i],
                    Type = FileSystemEntryType.File
                };
            }
            return result;
        }

        /// <summary>
        /// Browse a list of embedded resources.
        /// 
        /// For example:
        ///     "assembly.res1"
        ///     "assembly.res2"
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public FileSystemEntry[] Browse(string path)
            => entries ?? (entries = CreateEntries());

        /// <summary>
        /// Open embedded resource for reading.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="fileMode"></param>
        /// <param name="fileAccess"></param>
        /// <param name="fileShare"></param>
        /// <returns></returns>
        public Stream Open(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (fileMode != FileMode.Open) throw new IOException($"Cannot open embedded resouce in FileMode={fileMode}");
            if (fileAccess != FileAccess.Read) throw new IOException($"Cannot open embedded resouce in FileAccess={fileAccess}");
            Stream s = Assembly.GetManifestResourceStream(path);
            if (s == null) throw new FileNotFoundException(path);
            return s;
        }

        /// <summary>
        /// Add <paramref name="disposable"/> to list of objects to be disposed along with the system.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns>filesystem</returns>
        public EmbeddedFileSystem AddDisposable(object disposable) => AddDisposableBase(disposable) as EmbeddedFileSystem;

        /// <summary>
        /// Remove disposable from dispose list.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns></returns>
        public EmbeddedFileSystem RemoveDisposable(object disposable) => RemoveDisposableBase(disposable) as EmbeddedFileSystem;

        /// <summary>
        /// Print info
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => Assembly.FullName;
    }
}