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
    public class EmbeddedFileSystem : FileSystemBase, IFileSystemBrowse, IFileSystemOpen, IFileSystemReference
    {
        /// <summary>
        /// Zero entries.
        /// </summary>
        protected internal static IFileSystemEntry[] NoEntries = new IFileSystemEntry[0];

        /// <summary>
        /// Associated Assembly
        /// </summary>
        public readonly Assembly Assembly;

        /// <summary>
        /// Snapshot of entries.
        /// </summary>
        protected IFileSystemEntry[] entries;

        /// <summary>
        /// Reference to file-system.
        /// </summary>
        public String Reference { get; protected set; }

        /// <summary>
        /// File-system features.
        /// </summary>
        public override FileSystemFeatures Features => FileSystemFeatures.CaseSensitive;

        /// <inheritdoc/>
        public virtual bool CanBrowse => true;
        /// <inheritdoc/>
        public virtual bool CanTestExists => true;
        /// <inheritdoc/>
        public virtual bool CanOpen => true;
        /// <inheritdoc/>
        public virtual bool CanRead => true;
        /// <inheritdoc/>
        public virtual bool CanWrite => false;
        /// <inheritdoc/>
        public virtual bool CanCreateFile => false;
        /// <inheritdoc/>
        public virtual bool CanReference => true;

        /// <summary>
        /// Create embedded 
        /// </summary>
        /// <param name="assembly"></param>
        public EmbeddedFileSystem(Assembly assembly)
        {
            this.Assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
            this.Reference = $"assembly://[{assembly.FullName}]/";
        }

        /// <summary>
        /// Create a snapshot of entries.
        /// </summary>
        /// <returns></returns>
        protected IFileSystemEntry[] CreateEntries()
        {
            string[] names = Assembly.GetManifestResourceNames();

            // Get file time, or use Unix time 0.
            DateTimeOffset time;
            if (Assembly.Location != null && File.Exists(Assembly.Location))
                time = new FileInfo(Assembly.Location).LastWriteTimeUtc;
            else
                time = DateTimeOffset.FromUnixTimeSeconds(0L);

            IFileSystemEntry[] result = new IFileSystemEntry[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                result[i] = new FileSystemEntryFile(this, names[i], names[i], time, -1L);
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
        public IFileSystemEntry[] Browse(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (path == "") return entries ?? (entries = CreateEntries());
            return NoEntries;
        }

        /// <summary>
        /// Tests whether a file or directory exists.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support exists</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        public bool Exists(string path)
        {
            IFileSystemEntry[] _entries = entries ?? (entries = CreateEntries());
            foreach (IFileSystemEntry e in _entries)
                if (e.Path == path) return true;
            return false;
        }

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