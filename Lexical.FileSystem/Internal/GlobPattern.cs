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
    /// Glob pattern string info
    /// </summary>
    public struct GlobPattern : IEnumerable<GlobPattern.Literal>
    {
        /// <summary>Implicit conversion</summary>
        public static implicit operator String(GlobPattern str) => str.Pattern;
        /// <summary>Implicit conversion</summary>
        public static implicit operator GlobPattern(String str) => new GlobPattern(str);

        /// <summary>Glob pattern string</summary>
        public readonly string Pattern;

        /// <summary>Create glob pattern enumerable.</summary>
        /// <param name="pattern"></param>
        public GlobPattern(string pattern)
        {
            Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        }

        /// <summary>Separate non-wildcard and wildcard parts.</summary>
        public Info GetInfo() => new Info(Pattern);

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
        public struct Info
        {
            /// <summary>Full pattern string.</summary>
            public readonly String Pattern;
            /// <summary>The stem part of <see cref="Pattern"/> that doesn't have wild cards. The directories and filename before first entry with a wildcard character '*'/'**'/'?'.</summary>
            public readonly String Prefix;
            /// <summary>The latter part of <see cref="Pattern"/> after first directory or filename that has wildcards, starting from preceding directory separator '/'.</summary>
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
            ///     Pattern="dir/dir/file.txt", Constant="dir/dir/file.txt", Variable="", depth = 0
            ///     Pattern="dir/dir/*.txt", Constant="dir/dir/", Variable="*.txt", depth = 1
            ///     Pattern="dir/dir/*/*.txt", Constant="dir/dir/", Variable="*/*.txt", depth = 2
            ///     Pattern="dir/dir/dir?/*/*.txt", Constant="dir/dir/", Variable="dir?/*/*.txt", depth = 3
            ///     Pattern="dir/*/dir/dir/dir/file.txt", Constant="dir/", Variable="*/dir/dir/dir/file.txt", depth = 5
            ///     Pattern="dir/dir/**.txt", Constant="dir/dir/", Variable="**", depth = int.MaxValue
            ///     
            /// </summary>
            public readonly int SuffixDepth;

            /// <summary>Implicit conversion</summary>
            public static implicit operator String(Info str) => str.Pattern;
            /// <summary>Implicit conversion</summary>
            public static implicit operator Info(String str) => new Info(str);

            /// <summary>
            /// Create filter info.
            /// 
            /// If <paramref name="pattern"/> is null, then monitors any file in the path, but not subdirectories.
            /// <paramref name="pattern"/> is "**" then monitors any file in subdirectories.
            /// 
            /// </summary>
            /// <param name="pattern">glob pattern, e.g. "dir/**.txt"</param>
            public Info(string pattern)
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
            /// Info
            /// </summary>
            /// <returns></returns>
            public override string ToString()
                => $"{nameof(Pattern)}={Pattern}, {nameof(Prefix)}={Prefix}, {nameof(Suffix)}={Suffix}, {nameof(SuffixDepth)}={SuffixDepth}";
        }


        /// <summary>Get enumerator</summary>
        /// <returns></returns>
        public Enumerator GetEnumerator()
            => new Enumerator(Pattern);
        IEnumerator IEnumerable.GetEnumerator()
            => new Enumerator(Pattern);
        IEnumerator<Literal> IEnumerable<Literal>.GetEnumerator()
            => new Enumerator(Pattern);

        /// <summary>
        /// Enumerates glob pattern literals
        /// </summary>
        public struct Enumerator : IEnumerator<Literal>
        {
            /// <summary>Glob pattern string</summary>
            public readonly string Pattern;

            /// <summary>Character index.</summary>
            int index;

            /// <summary>Current literal.</summary>
            Literal current;

            /// <summary>Create glob pattern enumerable.</summary>
            /// <param name="pattern"></param>
            public Enumerator(string pattern)
            {
                Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
                index = 0;
                current = Literal.None;
            }

            /// <summary>Current literal.</summary>
            public Literal Current => current;
            object IEnumerator.Current => current;

            /// <summary></summary>
            public void Dispose() { }

            /// <summary>Move to next literal</summary>
            public bool MoveNext()
            {
                // Out of index
                if (index >= Pattern.Length) { current = Literal.None; return false; }

                // Read char
                char ch = Pattern[index++];

                // Choose literal
                switch (ch)
                {
                    case '/': current = new Literal(Literal.Kind.Slash, '/'); break;
                    case '?': current = new Literal(Literal.Kind.QuestionMark, '?'); break;
                    case '*': if (index < Pattern.Length && Pattern[index] == '*') { current = new Literal(Literal.Kind.StarStar, '*'); index++; } else current = new Literal(Literal.Kind.Star, '*'); break;
                    default: current = new Literal(Literal.Kind.Char, ch); break;
                }
                return true;
            }

            /// <summary>Start from beginning.</summary>
            public void Reset()
            {
                index = 0;
                current = Literal.None;
            }
        }

        /// <summary>
        /// Glob pattern literal
        /// </summary>
        public struct Literal : IEquatable<Literal>
        {
            /// <summary>Non-content literal</summary>
            public static readonly Literal None = new Literal(Kind.None, '\0');

            /// <summary>Literal used by <see cref="GlobPattern.Enumerator"/></summary>
            public enum Kind
            {
                /// <summary>not initialized</summary>
                None,
                /// <summary>"/"</summary>
                Slash,
                /// <summary>"?"</summary>
                QuestionMark,
                /// <summary>"*"</summary>
                Star,
                /// <summary>"**"</summary>
                StarStar,
                /// <summary>any other character</summary>
                Char
            }

            /// <summary>Literal type</summary>
            public Kind Type;
            /// <summary>Characters</summary>
            public char Char;
            /// <summary>Create record of literal</summary>
            public Literal(Kind type, char _char)
            {
                Type = type;
                Char = _char;
            }

            /// <summary>Compare equality.</summary>
            public static bool operator ==(Literal l, Literal r) => l.Type == r.Type && (l.Type != Kind.Char || l.Char == r.Char);
            /// <summary>Compare inequality.</summary>
            public static bool operator !=(Literal l, Literal r) => l.Type != r.Type || l.Char != r.Char;
            /// <summary>Compare equality.</summary>
            public bool Equals(Literal other) => Type == other.Type && (Type != Kind.Char || Char == other.Char);
            /// <summary>Compare equality.</summary>
            public override bool Equals(Object other_) => other_ is Literal other ? Type == other.Type && (Type != Kind.Char || Char == other.Char) : false;
            /// <summary>Get hashcode.</summary>
            public override int GetHashCode() => Type == Kind.Char ? Char.GetHashCode() : Type.GetHashCode();
            /// <summary>Append to string builder</summary>
            public void AppendTo(StringBuilder sb)
            {
                switch(Type)
                {
                    case Kind.None: break;
                    case Kind.Slash: sb.Append('/'); break;
                    case Kind.QuestionMark: sb.Append('?'); break;
                    case Kind.Star: sb.Append('*'); break;
                    case Kind.StarStar: sb.Append('*'); sb.Append('*'); break;
                    case Kind.Char: sb.Append(Char); break;
                }
            }

            /// <summary>Print info</summary>
            public override string ToString() => Type == Kind.StarStar ? "**" : Char.ToString();
        }


    }

}
