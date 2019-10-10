// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           9.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Text;

namespace Lexical.FileSystem.Internal
{
    /// <summary>
    /// Extraction of features from glob pattern string.
    /// Used for estimating the location and size of subtree a pattern represents.
    /// 
    /// Splits a wildcard path string into two parts.
    /// First part contains all paths without any wildcards, and second part all paths with wildcards and filename.
    /// 
    /// For example "dir/**/file.txt" is split into "dir" and "**/file.txt".
    /// 
    /// Examples:
    ///   Pattern=dir/dir/file.txt, Prefix=dir/dir/file.txt, Suffix=, SuffixDepth=0
    ///   Pattern=*.txt, Prefix=, Suffix=*.txt, SuffixDepth=1
    ///   Pattern=**.txt, Prefix=, Suffix=**.txt, SuffixDepth=2147483647
    ///   Pattern=/*.txt, Prefix=/, Suffix=*.txt, SuffixDepth=1
    ///   Pattern=*/*.txt, Prefix=, Suffix=*/*.txt, SuffixDepth=2
    ///   Pattern=/**.txt, Prefix=/, Suffix=**.txt, SuffixDepth=2147483647
    ///   Pattern=dir/dir/*/*.txt, Prefix=dir/dir/, Suffix=*/*.txt, SuffixDepth=2
    ///   Pattern=dir/dir?/*/*.txt, Prefix=dir/, Suffix=dir?/*/*.txt, SuffixDepth=3
    ///   Pattern=dir/dir/dir/*/*.txt, Prefix=dir/dir/dir/, Suffix=*/*.txt, SuffixDepth=2
    ///   Pattern=dir/dir/dir?/*/*.txt, Prefix=dir/dir/, Suffix=dir?/*/*.txt, SuffixDepth=3
    ///   Pattern=dir/dir/dir?/*/**.txt, Prefix=dir/dir/, Suffix=dir?/*/**.txt, SuffixDepth=2147483647
    ///   Pattern=dir/*/dir/dir/dir/file.txt, Prefix=dir/, Suffix=*/dir/dir/dir/file.txt, SuffixDepth=5
    ///   
    /// </summary>
    public struct GlobPatternInfo
    {
        /// <summary>Source pattern string.</summary>
        public readonly String Pattern;
        /// <summary>The part from <see cref="Pattern"/> before first directory or file name with wildcard */**/?.</summary>
        public readonly String Prefix;
        /// <summary>The part from <see cref="Pattern"/> that contains first wildcard, starting from directory separator '/'.</summary>
        public readonly String Suffix;
        /// <summary>
        /// Number of directory levels in <see cref="Suffix"/>.
        /// 
        /// Depth is 0 if there are no wildcard characters in <see cref="Pattern"/>.
        /// Depth is 1 if there are wildcard characters '?'/'*' in one directory.
        /// Depth is n if there are wildcard characters '?'/'*' in n directories after <see cref="Prefix"/> part.
        /// Depth is <see cref="int.MaxValue"/>, if there is **' wildcards anywhere.
        /// 
        /// Examples:
        ///     Pattern="dir/dir/file.txt", Prefix="dir/dir/file.txt", Suffix="", depth = 0
        ///     Pattern="dir/dir/*.txt", Prefix="dir/dir/", Suffix="*.txt", depth = 1
        ///     Pattern="dir/dir/*/*.txt", Prefix="dir/dir/", Suffix="*/*.txt", depth = 2
        ///     Pattern="dir/dir/dir?/*/*.txt", Prefix="dir/dir/", Suffix="dir?/*/*.txt", depth = 3
        ///     Pattern="dir/*/dir/dir/dir/file.txt", Prefix="dir/", Suffix="*/dir/dir/dir/file.txt", depth = 5
        ///     Pattern="dir/dir/**.txt", Prefix="dir/dir/", Suffix="**", depth = int.MaxValue
        ///     
        /// </summary>
        public readonly int SuffixDepth;

        /// <summary>Implicit converter</summary>
        public static implicit operator String(GlobPatternInfo str) => str.Pattern;
        /// <summary>Implicit converter</summary>
        public static implicit operator GlobPatternInfo(String str) => new GlobPatternInfo(str);

        /// <summary>
        /// Create filter info.
        /// 
        /// If <paramref name="pattern"/> is null, then monitors any file in the path, but not subdirectories.
        /// <paramref name="pattern"/> is "**" then monitors any file in subdirectories.
        /// 
        /// </summary>
        /// <param name="pattern">glob pattern, e.g. "dir/**.txt"</param>
        public GlobPatternInfo(string pattern)
        {
            this.Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));

            // Last separator (before wildcard) and first wildcard indices
            int ix_separator = -1, ix_wildcard = -1;
            long suffixDepth = 0; 
            // Find last separator char before first pattern character
            char prevch = (char)0;
            for (int i = 0; i < pattern.Length; i++)
            {
                char ch = pattern[i];
                // Wildcard
                if (ch == '?' || ch == '*')
                {
                    // First wild card
                    if (ix_wildcard < 0)
                    {
                        ix_wildcard = i;
                        suffixDepth++;
                    }
                }
                // Separator
                if (ch == '/')
                {
                    // First separator before wildcard
                    if (ix_wildcard < 0) ix_separator = i+1;
                    // Separator after wildcard
                    else suffixDepth++;
                }
                // "**"
                if (prevch == '*' && ch == '*') suffixDepth = int.MaxValue;
                prevch = ch;
            }

            // There are no wildcard
            if (ix_wildcard<0)
            {
                Prefix = pattern;
                Suffix = "";
            }
            // There is no separator, but are wildcards
            else if (ix_separator < 0)
            {
                Prefix = "";
                Suffix = pattern;
            }
            // 'xxx/zzz'
            else
            {
                Prefix = pattern.Substring(0, ix_separator);
                Suffix = pattern.Substring(ix_separator);
            }

            this.SuffixDepth = (int)Math.Min(suffixDepth, int.MaxValue);
        }

        /// <summary>
        /// Create intersection of the two subtrees that are described by this and <paramref name="other"/> pattern infos.
        /// 
        /// Returns true and <paramref name="intersection"/>, if there is an intersection of trees, otherwise a false.
        /// </summary>
        /// <param name="other"></param>
        /// <param name="intersection"></param>
        /// <returns>true and <paramref name="intersection"/>, if there is an intersection of trees, otherwise a false</returns>
        public bool Intersection(GlobPatternInfo other, out GlobPatternInfo intersection)
        {
            // Equal patterns
            if (this.Pattern == other.Pattern) { intersection = this; return true; }

            // Follow prefixes of both as long as they yield same names
            PathEnumerator p1 = new PathEnumerator(this.Prefix, false), p2 = new PathEnumerator(other.Prefix, false);

            // Index where common stem part ends (common stem = 0..stemEndIx)
            int stemEndIx = 0; bool diverged = false;
            // prefix directory/name depth
            int p1depth = 0, p2depth = 0; bool p1Moved = false, p2Moved = false;
            while (true)
            {
                p1Moved = p1.MoveNext();
                p2Moved = p2.MoveNext();
                if (!p1Moved && !p2Moved) break;
                // Calculate prefix depths
                if (p1Moved) p1depth++;
                if (p2Moved) p2depth++;
                // paths have not yet diverged
                if (p1Moved && p2Moved && !diverged)
                {
                    // directory names are equal
                    if (StringSegment.Comparer.Instance.Equals(p1.Current, p2.Current))
                    {
                        stemEndIx = p1.Current.Start + p1.Current.Length; // prefixes keep having common stem, move index
                        if (this.Prefix.Length >= stemEndIx && other.Prefix.Length >= stemEndIx && this.Prefix[stemEndIx] == '/' && other.Prefix[stemEndIx] == '/') stemEndIx++;
                    }
                    else
                        diverged = true; // prefixes diverge here
                }                
            }

            // How many characters are left in each .Prefix strings after their common stem part.
            int prefix1Left = this.Prefix.Length - stemEndIx, prefix2Left = other.Prefix.Length - stemEndIx;

            // They have equal prefixes
            if (prefix1Left == 0 && prefix2Left == 0)
            {
                if (this.Suffix == other.Suffix) { intersection = this; return true; }

                // Intersection of depth
                int depth = Math.Min(this.SuffixDepth, other.SuffixDepth);
                intersection = new GlobPatternInfo(other.Prefix + Stars(depth));
                return true;
            }
            else 
            // 1 is parent of 2
            if (prefix1Left == 0 && prefix2Left > 0)
            {
                // Dive into prefix2
                int s1depth = this.SuffixDepth-(p2depth-p1depth), s2depth = other.SuffixDepth;

                int depth = Math.Min(s1depth, s2depth);
                if (depth < 0) { intersection = default; return false; }
                if (depth > 100) intersection = new GlobPatternInfo(other.Prefix + "**");
                else intersection = new GlobPatternInfo(other.Prefix + Stars(depth));
                return true;
            }
            else
            // 2 is parent of 1
            if (prefix1Left > 0 && prefix2Left == 0)
            {
                // Dive into prefix2
                int s1depth = this.SuffixDepth, s2depth = other.SuffixDepth - (p1depth - p2depth);

                int depth = Math.Min(s1depth, s2depth);
                if (depth < 0) { intersection = default; return false; }
                if (depth > 100) intersection = new GlobPatternInfo(this.Prefix + "**");
                else intersection = new GlobPatternInfo(this.Prefix + Stars(depth));
                return true;
            }
            else
            // Paths diverge, no intersection
            {
                intersection = default;
                return false;
            }
        }

        static string Stars(int depth)
        {
            StringBuilder sb = new StringBuilder(depth * 2);
            for (int i = 0; i < depth; i++)
            {
                sb.Append("*");
                if (i > 0) sb.Append("/");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Info
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => $"{nameof(Pattern)}={Pattern}, {nameof(Prefix)}={Prefix}, {nameof(Suffix)}={Suffix}, {nameof(SuffixDepth)}={SuffixDepth}";
    }
}
