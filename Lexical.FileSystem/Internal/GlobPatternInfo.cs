// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           11.10.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Lexical.FileSystem.Internal
{
    /// <summary>
    /// Separates wildcard pattern string into two parts.
    /// 
    /// First part contains directory levels without any wilcards.
    /// Second parts contains all the directory levels after first wildcard.
    /// 
    /// Used for estimating the location and size of subtree a pattern represents.
    /// 
    /// For example "dir/**/file.txt" is split into "dir/" and "**/file.txt".
    /// 
    /// Examples:
    ///   Pattern=dir/dir/file.txt, Stem=dir/dir/file.txt, Suffix=, SuffixDepth=0
    ///   Pattern=*.txt, Stem=, Suffix=*.txt, SuffixDepth=1
    ///   Pattern=**.txt, Stem=, Suffix=**.txt, SuffixDepth=2147483647
    ///   Pattern=/*.txt, Stem=/, Suffix=*.txt, SuffixDepth=1
    ///   Pattern=*/*.txt, Stem=, Suffix=*/*.txt, SuffixDepth=2
    ///   Pattern=/**.txt, Stem=/, Suffix=**.txt, SuffixDepth=2147483647
    ///   Pattern=dir/dir/*/*.txt, Stem=dir/dir/, Suffix=*/*.txt, SuffixDepth=2
    ///   Pattern=dir/dir?/*/*.txt, Stem=dir/, Suffix=dir?/*/*.txt, SuffixDepth=3
    ///   Pattern=dir/dir/dir/*/*.txt, Stem=dir/dir/dir/, Suffix=*/*.txt, SuffixDepth=2
    ///   Pattern=dir/dir/dir?/*/*.txt, Stem=dir/dir/, Suffix=dir?/*/*.txt, SuffixDepth=3
    ///   Pattern=dir/dir/dir?/*/**.txt, Stem=dir/dir/, Suffix=dir?/*/**.txt, SuffixDepth=2147483647
    ///   Pattern=dir/*/dir/dir/dir/file.txt, Stem=dir/, Suffix=*/dir/dir/dir/file.txt, SuffixDepth=5
    ///   
    /// </summary>
    public struct GlobPatternInfo
    {
        /// <summary>Full pattern string.</summary>
        public readonly String Pattern;
        /// <summary>The stem part of <see cref="Pattern"/> that doesn't have wild cards. The directories and filename before first entry with a wildcard character '*'/'**'/'?'.</summary>
        public readonly String Stem;
        /// <summary>The latter part of <see cref="Pattern"/>, the string after first directory that has wildcards.</summary>
        public readonly String Suffix;

        /// <summary>
        /// Number of directory levels in <see cref="Suffix"/>.
        /// 
        /// Depth is 0 if there are no wildcard characters in <see cref="Pattern"/>.
        /// Depth is 1 if there are wildcard characters '?'/'*' in one directory.
        /// Depth is n if there are wildcard characters '?'/'*' in n directories after <see cref="Stem"/> part.
        /// Depth is <see cref="int.MaxValue"/>, if there is **' wildcards anywhere.
        /// 
        /// Examples:
        ///     Pattern="dir/dir/file.txt", Stem="dir/dir/file.txt", Suffix="", depth = 0
        ///     Pattern="dir/dir/*.txt", Stem="dir/dir/", Suffix="*.txt", depth = 1
        ///     Pattern="dir/dir/*/*.txt", Stem="dir/dir/", Suffix="*/*.txt", depth = 2
        ///     Pattern="dir/dir/dir?/*/*.txt", Stem="dir/dir/", Suffix="dir?/*/*.txt", depth = 3
        ///     Pattern="dir/*/dir/dir/dir/file.txt", Stem="dir/", Suffix="*/dir/dir/dir/file.txt", depth = 5
        ///     Pattern="dir/dir/**.txt", Stem="dir/dir/", Suffix="**", depth = int.MaxValue
        ///     
        /// </summary>
        public readonly int SuffixDepth;

        /// <summary>Implicit conversion</summary>
        public static implicit operator String(GlobPatternInfo str) => str.Pattern;
        /// <summary>Implicit conversion</summary>
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
                    if (ix_wildcard < 0) ix_separator = i + 1;
                    // Separator after wildcard
                    else suffixDepth++;
                }
                // "**"
                if (prevch == '*' && ch == '*') suffixDepth = int.MaxValue;
                prevch = ch;
            }

            // There are no wildcard
            if (ix_wildcard < 0)
            {
                Stem = pattern;
                Suffix = "";
            }
            // There is no separator, but are wildcards
            else if (ix_separator < 0)
            {
                Stem = "";
                Suffix = pattern;
            }
            // 'xxx/zzz'
            else
            {
                Stem = pattern.Substring(0, ix_separator);
                Suffix = pattern.Substring(ix_separator);
            }

            this.SuffixDepth = (int)Math.Min(suffixDepth, int.MaxValue);
        }

        /// <summary>
        /// Info
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => $"{nameof(Pattern)}={Pattern}, {nameof(Stem)}={Stem}, {nameof(Suffix)}={Suffix}, {nameof(SuffixDepth)}={SuffixDepth}";
    }
}
