// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           11.11.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Directory content
    /// </summary>
    public class DirectoryContent : IDirectoryContent
    {
        /// <summary>The filesystem where the browse was issued.</summary>
        public IFileSystem FileSystem => filesystem;
        /// <summary>The browsed path at <see cref="FileSystem"/>.</summary>
        public string Path => path;
        /// <summary><see cref="Path"/> exists.</summary>
        public bool Exists => true;
        /// <summary>Number of entries</summary>
        public int Count => entries.Count;
        /// <summary>Entry at <paramref name="index"/></summary>
        public IEntry this[int index] => entries[index];

        /// <summary>Entries</summary>
        protected readonly IReadOnlyList<IEntry> entries;
        /// <summary>The filesystem where the browse was issued.</summary>
        protected readonly IFileSystem filesystem;
        /// <summary>The browsed path at <see cref="FileSystem"/>.</summary>
        protected readonly string path;

        /// <summary>
        /// Create directory content.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path"></param>
        /// <param name="entries"></param>
        public DirectoryContent(IFileSystem filesystem, string path, IEnumerable<IEntry> entries)
        {
            this.filesystem = filesystem ?? throw new ArgumentNullException(nameof(filesystem));
            this.path = path ?? throw new ArgumentNullException(nameof(path));
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            this.entries = entries is IReadOnlyList<IEntry> list ? list : entries.ToArray();
        }

        /// <summary>Get enumerator for entries</summary>
        public IEnumerator<IEntry> GetEnumerator() => ((IEnumerable<IEntry>)entries).GetEnumerator();
        /// <summary>Get enumerator for entries</summary>
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)entries).GetEnumerator();

        /// <summary>Print into</summary>
        public override string ToString() => path;
    }

    /// <summary>
    /// Directory content with no entries, but existing directory.
    /// </summary>
    public class DirectoryEmpty : IDirectoryContent
    {
        /// <summary>enumerator</summary>
        internal static Enumerator enumerator = new Enumerator();

        /// <summary>The filesystem where the browse was issued.</summary>
        public IFileSystem FileSystem => filesystem;
        /// <summary>The browsed path at <see cref="FileSystem"/>.</summary>
        public string Path => path;
        /// <summary><see cref="Path"/> exists.</summary>
        public bool Exists => true;
        /// <summary>Number of entries</summary>
        public int Count => 0;
        /// <summary>Entry at <paramref name="index"/></summary>
        public IEntry this[int index] => throw new IndexOutOfRangeException();

        /// <summary>The filesystem where the browse was issued.</summary>
        protected readonly IFileSystem filesystem;
        /// <summary>The browsed path at <see cref="FileSystem"/>.</summary>
        protected readonly string path;

        /// <summary>
        /// Create directory content.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path"></param>
        public DirectoryEmpty(IFileSystem filesystem, string path)
        {
            this.filesystem = filesystem ?? throw new ArgumentNullException(nameof(filesystem));
            this.path = path ?? throw new ArgumentNullException(nameof(path));
        }

        /// <summary>Get enumerator for no entries</summary>
        public Enumerator GetEnumerator() => enumerator;
        /// <summary>Get enumerator for no entries</summary>
        IEnumerator<IEntry> IEnumerable<IEntry>.GetEnumerator() => enumerator;
        /// <summary>Get enumerator for no entries</summary>
        IEnumerator IEnumerable.GetEnumerator() => enumerator;

        /// <summary>Directory enumerator</summary>
        public sealed class Enumerator : IEnumerator<IEntry>
        {
            /// <summary>Current entry at cursor</summary>
            public IEntry Current => null;
            /// <summary>Current entry at cursor</summary>
            object IEnumerator.Current => null;
            /// <summary>Dispose enumerator</summary>
            public void Dispose() { }
            /// <summary>Move cursor</summary>
            public bool MoveNext() => false;
            /// <summary>Reset cursor.</summary>
            public void Reset() { }
        }

        /// <summary>Print into</summary>
        public override string ToString() => path;
    }

    /// <summary>
    /// Directory content for directory that doesn't exist
    /// </summary>
    public class DirectoryNotFound : IDirectoryContent
    {
        /// <summary>enumerator</summary>
        internal static Enumerator enumerator = new Enumerator();

        /// <summary>The filesystem where the browse was issued.</summary>
        public IFileSystem FileSystem => filesystem;
        /// <summary>The browsed path at <see cref="FileSystem"/>.</summary>
        public string Path => path;
        /// <summary><see cref="Path"/> exists.</summary>
        public bool Exists => false;
        /// <summary>Entry count</summary>
        public int Count => 0;
        /// <summary>Entry</summary>
        public IEntry this[int index] => throw new IndexOutOfRangeException();

        /// <summary>The filesystem where the browse was issued.</summary>
        protected readonly IFileSystem filesystem;
        /// <summary>The browsed path at <see cref="FileSystem"/>.</summary>
        protected readonly string path;

        /// <summary>
        /// Create content instance for directory that doesn't exist.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path"></param>
        public DirectoryNotFound(IFileSystem filesystem, string path)
        {
            this.filesystem = filesystem ?? throw new ArgumentNullException(nameof(DirectoryNotFound.filesystem));
            this.path = path ?? throw new ArgumentNullException(nameof(path));
        }

        /// <summary>Get enumerator for no entries</summary>
        public Enumerator GetEnumerator() => enumerator;
        /// <summary>Get enumerator for no entries</summary>
        IEnumerator<IEntry> IEnumerable<IEntry>.GetEnumerator() => enumerator;
        /// <summary>Get enumerator for no entries</summary>
        IEnumerator IEnumerable.GetEnumerator() => enumerator;

        /// <summary>Directory enumerator</summary>
        public sealed class Enumerator : IEnumerator<IEntry>
        {
            /// <summary>Current entry at cursor</summary>
            public IEntry Current => null;
            /// <summary>Current entry at cursor</summary>
            object IEnumerator.Current => null;
            /// <summary>Dispose enumerator</summary>
            public void Dispose() { }
            /// <summary>Move cursor</summary>
            public bool MoveNext() => false;
            /// <summary>Reset cursor.</summary>
            public void Reset() { }
        }

        /// <summary>Print into</summary>
        public override string ToString() => path;
    }
}
