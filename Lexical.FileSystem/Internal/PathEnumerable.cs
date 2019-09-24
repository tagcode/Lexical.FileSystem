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
    public struct PathEnumerable : IEnumerable<StringSegment>
    {
        /// <summary>
        /// Path string.
        /// </summary>
        public readonly StringSegment Path;

        /// <summary>
        /// Create path string separator.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="ignoreTrailingSlash">If set, trailing slash of <paramref name="path"/> is ignored. for example "/mnt/dir/" registers as "/mnt/dir"</param>
        public PathEnumerable(string path, bool ignoreTrailingSlash = false)
        {
            if (path == null) Path = new StringSegment("", 0, 0);
            else if (ignoreTrailingSlash && path.Length>0 && path[path.Length-1] == '/')
            {                
                Path = new StringSegment(path, 0, path.Length-1);
            }
            else
            {
                Path = new StringSegment(path);
            }
        }

        /// <summary>
        /// Create path string separator.
        /// </summary>
        /// <param name="path"></param>
        public PathEnumerable(StringSegment path)
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
        IEnumerator<StringSegment> IEnumerable<StringSegment>.GetEnumerator()
            => new PathEnumerator(Path);
    }

    /// <summary>
    /// Enumerator of <see cref="PathEnumerable"/>.
    /// </summary>
    public struct PathEnumerator : IEnumerator<StringSegment>
    {
        /// <summary>
        /// Path string.
        /// </summary>
        public readonly StringSegment Path;

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
        /// <param name="ignoreTrailingSlash">If set, trailing slash of <paramref name="path"/> is ignored. for example "/mnt/dir/" registers as "/mnt/dir"</param>
        public PathEnumerator(string path, bool ignoreTrailingSlash = false)
        {
            if (path == null) Path = new StringSegment("", 0, 0);
            else if (ignoreTrailingSlash && path.Length > 0 && path[path.Length - 1] == '/')
            {
                Path = new StringSegment(path, 0, path.Length - 1);
            }
            else
            {
                Path = new StringSegment(path);
            }
            endIx = -1;
            startIx = -1;
        }

        /// <summary>
        /// Create path string separator.
        /// </summary>
        /// <param name="path"></param>
        public PathEnumerator(StringSegment path)
        {
            Path = path;
            endIx = -1;
            startIx = -1;
        }

        /// <inheritdoc/>
        public StringSegment Current
            => startIx < Path.Start || endIx < Path.Start || startIx > Path.Start+Path.Length || endIx > Path.Start + Path.Length  ?
               new StringSegment(Path.String, Path.Start, 0) :
               new StringSegment(Path.String, Path.Start + startIx, endIx - startIx);

        /// <inheritdoc/>
        object IEnumerator.Current
            => startIx < Path.Start || endIx < Path.Start || startIx > Path.Start + Path.Length || endIx > Path.Start + Path.Length ?
               new StringSegment(Path.String, Path.Start, 0) :
               new StringSegment(Path.String, Path.Start + startIx, endIx - startIx);

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

    /// <summary>
    /// Enumerator of <see cref="PathEnumerable"/> with special case handling.
    /// 
    /// Special case handling: Two of first "", "" are considered one "".
    /// </summary>
    public struct PathEnumerator2 : IEnumerator<StringSegment>
    {
        /// <summary>
        /// Path string.
        /// </summary>
        public readonly StringSegment Path;

        /// <summary>
        /// End index.
        /// </summary>
        int endIx;

        /// <summary>
        /// Start index.
        /// </summary>
        int startIx;

        /// <summary>
        /// Name index
        /// </summary>
        int nameIndex;

        /// <summary>
        /// Current name
        /// </summary>
        StringSegment name;

        /// <summary>
        /// Create path string separator.
        /// </summary>
        /// <param name="path"></param>
        public PathEnumerator2(String path)
        {
            Path = new StringSegment(path);
            endIx = -1;
            startIx = -1;
            nameIndex = -1;
            name = StringSegment.Empty;
        }

        /// <summary>
        /// Create path string separator.
        /// </summary>
        /// <param name="path"></param>
        public PathEnumerator2(StringSegment path)
        {
            Path = path;
            endIx = -1;
            startIx = -1;
            nameIndex = -1;
            name = StringSegment.Empty;
        }

        /// <inheritdoc/>
        public StringSegment Current
            => name;

        /// <inheritdoc/>
        object IEnumerator.Current
            => name;

        /// <inheritdoc/>
        public void Reset()
        {
            endIx = -1;
            startIx = -1;
            nameIndex = -1;
            name = StringSegment.Empty;
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
            while (true)
            {
                endIx++;
                // End
                if (endIx > eol) return false;
                // End of line
                if ((endIx == eol) || /*Break at '/'*/(endIx < eol && Path.String[endIx] == '/'))
                {
                    StringSegment previousName = name;
                    name = startIx < Path.Start || endIx < Path.Start || startIx > Path.Start + Path.Length || endIx > Path.Start + Path.Length ?
                           new StringSegment(Path.String, Path.Start, 0) :
                           new StringSegment(Path.String, Path.Start + startIx, endIx - startIx);
                    nameIndex++;

                    // Special case handling: Two of first "", "" are considered one "".
                    if (nameIndex == 1 && name.Equals(StringSegment.Empty) && previousName.Equals(StringSegment.Empty))
                    {
                        startIx = endIx+1;
                        continue;
                    }

                    return true;
                }
            }
        }
    }

}
