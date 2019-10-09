// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           9.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;

namespace Lexical.FileSystem.Internal
{
    /// <summary>
    /// Extracts information about glob pattern string.
    /// 
    /// Splits a wildcard path string into two parts.
    /// First part contains all paths without any wildcards, and second part all paths with wildcards and filename.
    /// 
    /// For example "dir/**/file.txt" is split into "dir" and "**/file.txt".
    /// 
    /// Examples:
    ///   Pattern=*.txt, Prefix=, Suffix=*.txt, SuffixDepth=0, HasWildcards=True, SuffixDepth=0
    ///   Pattern=**.txt, Prefix=, Suffix=**.txt, SuffixDepth=2147483647, HasWildcards=True, SuffixDepth=2147483647
    ///   Pattern=/*.txt, Prefix=/, Suffix=*.txt, SuffixDepth=0, HasWildcards=True, SuffixDepth=0
    ///   Pattern=*/*.txt, Prefix=, Suffix=*/*.txt, SuffixDepth=1, HasWildcards=True, SuffixDepth=1
    ///   Pattern=/**.txt, Prefix=/, Suffix=**.txt, SuffixDepth=2147483647, HasWildcards=True, SuffixDepth=2147483647
    ///   Pattern=dir/dir/*/*.txt, Prefix=dir/dir/, Suffix=*/*.txt, SuffixDepth=1, HasWildcards=True, SuffixDepth=1
    ///   Pattern=dir/dir?/*/*.txt, Prefix=dir/, Suffix=dir?/*/*.txt, SuffixDepth=2, HasWildcards=True, SuffixDepth=2
    ///   Pattern=dir/dir/dir/*/*.txt, Prefix=dir/dir/dir/, Suffix=*/*.txt, SuffixDepth=1, HasWildcards=True, SuffixDepth=1
    ///   Pattern=dir/dir/dir?/*/*.txt, Prefix=dir/dir/, Suffix=dir?/*/*.txt, SuffixDepth=2, HasWildcards=True, SuffixDepth=2
    ///   Pattern=dir/dir/dir?/*/**.txt, Prefix=dir/dir/, Suffix=dir?/*/**.txt, SuffixDepth=2147483647, HasWildcards=True, SuffixDepth=2147483647
    ///   
    /// </summary>
    public struct GlobPatternInfo
    {
        /// <summary>
        /// Source pattern string.
        /// </summary>
        public readonly String Pattern;

        /// <summary>
        /// Path stem without any wildcards, before first directory/file name part with wildcards "*", "**", or "?". 
        /// Separator character is "/". 
        /// </summary>
        public readonly String Prefix;

        /// <summary>
        /// Pattern part for file, or for directories wild cards and file.
        /// </summary>
        public readonly String Suffix;

        /// <summary>
        /// Number of directory levels in <see cref="Suffix"/>.
        /// 
        /// Examples:
        ///     Pattern="dir/dir/*.txt", Prefix="dir/dir/", Suffix="*.txt", depth = 0
        ///     Pattern="dir/dir/*/*.txt", Prefix="dir/dir/", Suffix="*/*.txt", depth = 1
        ///     Pattern="dir/dir/dir?/*/*.txt", Prefix="dir/dir/", Suffix="dir?/*/*.txt", depth = 2
        ///     Pattern="dir/dir/**.txt", Prefix="dir/dir/", Suffix="**", depth = int.MaxValue
        ///     
        /// </summary>
        public readonly int SuffixDepth;

        /// <summary>
        /// Does <see cref="Pattern"/> have any wildcard '?', '*', '**' characters.
        /// </summary>
        public readonly bool HasWildcards;

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
                    if (ix_wildcard < 0) ix_wildcard = i;
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

            // There is no separator
            if (ix_separator < 0)
            {
                Prefix = "";
                Suffix = pattern;
                HasWildcards = ix_wildcard >= 0;
            }
            // 'xxx/zzz'
            else
            {
                Prefix = pattern.Substring(0, ix_separator);
                Suffix = pattern.Substring(ix_separator);
            }

            HasWildcards = ix_wildcard >= 0;
            this.SuffixDepth = (int)Math.Min(suffixDepth, int.MaxValue);
        }

        /// <summary>
        /// Info
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => $"{nameof(Pattern)}={Pattern}, {nameof(Prefix)}={Prefix}, {nameof(Suffix)}={Suffix}, {nameof(SuffixDepth)}={SuffixDepth}, {nameof(HasWildcards)}={HasWildcards}, {nameof(SuffixDepth)}={SuffixDepth}";
    }
}
