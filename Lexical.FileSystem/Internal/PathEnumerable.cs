// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;

namespace Lexical.FileSystem.Internal
{
    /// <summary>
    /// Separates directories and filename from path strings.
    /// Directory separator is slash '/'. 
    /// 
    /// Path is not expected to start with separator. 
    /// 
    /// Examples:
    /// <list type="bullet">
    ///     <item>"" -> [""]</item>
    ///     <item>"dir/dir/file" -> ["dir", "dir", "file"]</item>
    ///     <item>"dir/dir/path/" -> ["dir", "dir", "path", ""]</item>
    ///     <item>"/mnt/shared/" -> ["", "mnt", "shared", ""]</item>
    ///     <item>"/" -> ["", ""]</item>
    ///     <item>"//" -> ["", "", ""]</item>
    /// </list>
    /// </summary>
    public struct PathEnumerable : IEnumerable<StringSpan>
    {
        /// <summary>
        /// Path string.
        /// </summary>
        public readonly StringSpan Path;

        /// <summary>
        /// Create path string separator.
        /// </summary>
        /// <param name="path"></param>
        public PathEnumerable(string path)
        {
            Path = new StringSpan(path ?? throw new ArgumentNullException(nameof(path)));
        }

        /// <summary>
        /// Create path string separator.
        /// </summary>
        /// <param name="path"></param>
        public PathEnumerable(StringSpan path)
        {
            Path = path;
        }

        /// <summary>
        /// Get enumerator
        /// </summary>
        /// <returns></returns>
        public PathEnumerator GetEnumerator()
            => new PathEnumerator(Path);
        IEnumerator IEnumerable.GetEnumerator()
            => new PathEnumerator(Path);
        IEnumerator<StringSpan> IEnumerable<StringSpan>.GetEnumerator()
            => new PathEnumerator(Path);
    }

    /// <summary>
    /// Enumerator of <see cref="PathEnumerable"/>.
    /// </summary>
    public struct PathEnumerator : IEnumerator<StringSpan>
    {
        /// <summary>
        /// Path string.
        /// </summary>
        public readonly StringSpan Path;

        /// <summary>
        /// End index.
        /// </summary>
        int endIx;

        /// <summary>
        /// Start index.
        /// </summary>
        int startIx;

        /// <summary>
        /// Create path string separator.
        /// </summary>
        /// <param name="path"></param>
        public PathEnumerator(String path)
        {
            Path = new StringSpan(path ?? throw new ArgumentNullException(nameof(path)));
            endIx = -1;
            startIx = -1;
        }

        /// <summary>
        /// Create path string separator.
        /// </summary>
        /// <param name="path"></param>
        public PathEnumerator(StringSpan path)
        {
            Path = path;
            endIx = -1;
            startIx = -1;
        }

        /// <inheritdoc/>
        public StringSpan Current
            => startIx < Path.Start || endIx < Path.Start || startIx > Path.Start+Path.Length || endIx > Path.Start + Path.Length  ?
               new StringSpan(Path.String, Path.Start, 0) :
               new StringSpan(Path.String, Path.Start + startIx, endIx - startIx);

        /// <inheritdoc/>
        object IEnumerator.Current
            => startIx < Path.Start || endIx < Path.Start || startIx > Path.Start + Path.Length || endIx > Path.Start + Path.Length ?
               new StringSpan(Path.String, Path.Start, 0) :
               new StringSpan(Path.String, Path.Start + startIx, endIx - startIx);

        /// <inheritdoc/>
        public void Reset()
        {
            endIx = -1;
            startIx = -1;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public bool MoveNext()
        {
            int eol = Path.Start + Path.Length;

            // Move start index
            startIx = endIx;
            // First Move
            startIx++;

            // Move end index
            while (true) {
                endIx++;
                // End
                if (endIx > eol) return false;
                // End of line
                if (endIx == eol) return true;
                // Break at '/'
                if (endIx < eol && Path.String[endIx] == '/') return true;
            }
        }
    }
}
