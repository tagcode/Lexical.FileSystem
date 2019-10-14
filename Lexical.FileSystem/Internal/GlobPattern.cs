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
    public struct GlobPattern : IEnumerable<GlobPattern.Token>
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


        /// <summary>Get enumerator</summary>
        /// <returns></returns>
        public Enumerator GetEnumerator()
            => new Enumerator(Pattern);
        IEnumerator IEnumerable.GetEnumerator()
            => new Enumerator(Pattern);
        IEnumerator<Token> IEnumerable<Token>.GetEnumerator()
            => new Enumerator(Pattern);

        /// <summary>
        /// Enumerates glob pattern literals
        /// </summary>
        public struct Enumerator : IEnumerator<Token>
        {
            /// <summary>Glob pattern string</summary>
            public readonly string Pattern;

            /// <summary>Character index.</summary>
            int index;

            /// <summary>Current literal.</summary>
            Token current;

            /// <summary>Create glob pattern enumerable.</summary>
            /// <param name="pattern"></param>
            public Enumerator(string pattern)
            {
                Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
                index = 0;
                current = Token.None;
            }

            /// <summary>Current literal.</summary>
            public Token Current => current;
            object IEnumerator.Current => current;

            /// <summary></summary>
            public void Dispose() { }

            /// <summary>Move to next literal</summary>
            public bool MoveNext()
            {
                // Out of index
                if (index >= Pattern.Length) { current = Token.None; return false; }

                // Read char
                char ch = Pattern[index++];

                // Choose literal
                switch (ch)
                {
                    case '/': current = Token.Type.Slash; break;
                    case '?': current = Token.Type.QuestionMark; break;
                    case '*': if (index < Pattern.Length && Pattern[index] == '*') { current = Token.Type.StarStar; index++; } else current = Token.Type.Star; break;
                    default: current = ch; break;
                }
                return true;
            }

            /// <summary>Start from beginning.</summary>
            public void Reset()
            {
                index = 0;
                current = Token.None;
            }
        }

        /// <summary>
        /// Glob pattern literal
        /// </summary>
        public struct Token : IEquatable<Token>
        {
            /// <summary>Non-content literal</summary>
            public static readonly Token None = new Token(Type.None);

            /// <summary>Implicit conversion</summary>
            public static implicit operator Type(Token l) => l.Kind;
            /// <summary>Implicit conversion</summary>
            public static implicit operator char(Token l) => l.Char;
            /// <summary>Implicit conversion</summary>
            public static implicit operator Token(Type k) => new Token(k);
            /// <summary>Implicit conversion</summary>
            public static implicit operator Token(Char c) => new Token(c);

            /// <summary>Literal used by <see cref="GlobPattern.Enumerator"/></summary>
            public enum Type
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
            public Type Kind;
            /// <summary>Characters</summary>
            public char Char;
            /// <summary>Create record of literal</summary>
            public Token(Type type)
            {
                Kind = type;
                switch(type)
                {
                    case Type.QuestionMark: Char = '?'; break;
                    case Type.Slash: Char = '/'; break;
                    case Type.Star: Char = '*'; break;
                    case Type.StarStar: Char = '*'; break;
                    default: Char = '\0'; break;
                }
            }
            /// <summary>Create record of literal</summary>
            public Token(char _char)
            {
                Kind = Type.Char;
                Char = _char;
            }

            /// <summary>Compare equality.</summary>
            public static bool operator ==(Token l, Token r) => l.Kind == r.Kind && (l.Kind != Type.Char || l.Char == r.Char);
            /// <summary>Compare inequality.</summary>
            public static bool operator !=(Token l, Token r) => l.Kind != r.Kind || l.Char != r.Char;
            /// <summary>Compare equality.</summary>
            public bool Equals(Token other) => Kind == other.Kind && (Kind != Type.Char || Char == other.Char);
            /// <summary>Compare equality.</summary>
            public override bool Equals(Object other_) => other_ is Token other ? Kind == other.Kind && (Kind != Type.Char || Char == other.Char) : false;
            /// <summary>Get hashcode.</summary>
            public override int GetHashCode() => Kind == Type.Char ? Char.GetHashCode() : Kind.GetHashCode();
            /// <summary>Append to string builder</summary>
            public void AppendTo(StringBuilder sb)
            {
                switch(Kind)
                {
                    case Type.None: break;
                    case Type.Slash: sb.Append('/'); break;
                    case Type.QuestionMark: sb.Append('?'); break;
                    case Type.Star: sb.Append('*'); break;
                    case Type.StarStar: sb.Append('*'); sb.Append('*'); break;
                    case Type.Char: sb.Append(Char); break;
                }
            }

            /// <summary>Print info</summary>
            public override string ToString() => Kind == Type.StarStar ? "**" : Char.ToString();
        }


    }

}
