// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           9.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;

namespace Lexical.FileSystem.Utilities
{
    /// <summary>
    /// Extracts information about glob pattern string.
    /// 
    /// Splits a wildcard path string into two parts.
    /// First part contains all paths without any wildcards, and second part all paths with wildcards and filename.
    /// 
    /// For example "dir/**/file.txt" is split into "dir" and "**/file.txt".
    /// </summary>
    public struct GlobPatternInfo
    {
        /// <summary>
        /// Source pattern string.
        /// </summary>
        public readonly String Source;

        /// <summary>
        /// Path stem without any wildcards, before first "*", "**", or "?". 
        /// Separator character is "/". 
        /// </summary>
        public readonly String Prefix;

        /// <summary>
        /// Pattern part for file, or for directories wild cards and file.
        /// 
        /// For example: 
        ///     "file.txt"
        ///     "dir*/file.txt"
        /// </summary>
        public readonly String Suffix;

        /// <summary>
        /// Does <see cref="Source"/> have any wildcard '?', '*', '**' characters.
        /// </summary>
        public readonly bool HasWildcards;

        /// <summary>
        /// Does <see cref="Suffix"/> refer to subdirectories with wildcards.
        /// 
        /// For example:
        ///     "**/myfile.txt" = true,
        ///     "myfile*.txt" = false,
        ///     "myfile**.txt" = false.
        /// </summary>
        public readonly bool Subdirectories;

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
            this.Source = pattern ?? throw new ArgumentNullException(nameof(pattern));

            // Last separator (before wildcard) and first wildcard indices
            int ix_separator = -1, ix_wildcard = -1;
            bool subdirectories = false;
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
                    if (ix_wildcard < 0) ix_separator = i;
                    // Separator after wildcard
                    else subdirectories = true;
                }
                //
                if (ch == '*' && prevch == '*') subdirectories = true;
                prevch = ch;
            }

            // There is no separator
            if (ix_separator < 0)
            {
                Prefix = "";
                Suffix = pattern;
                HasWildcards = ix_wildcard >= 0;
            }
            else
            // Starts with '/'
            if (ix_separator == 0)
            {
                Prefix = "/";
                Suffix = pattern.Substring(1);
            }
            // 'xxx/zzz'
            else
            {
                Prefix = pattern.Substring(0, ix_separator);
                Suffix = pattern.Substring(ix_separator + 1);
            }

            HasWildcards = ix_wildcard >= 0;
            Subdirectories = subdirectories;
        }

        /// <summary>
        /// Info
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => $"{nameof(Prefix)}={Prefix}, {nameof(Suffix)}={Suffix}, {nameof(HasWildcards)}={HasWildcards}, {nameof(Subdirectories)}={Subdirectories}";
    }
}
