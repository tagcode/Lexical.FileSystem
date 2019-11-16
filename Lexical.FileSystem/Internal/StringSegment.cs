// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

namespace Lexical.FileSystem.Internal
{
    /// <summary>
    /// Span of characters.
    /// 
    /// Used due to lack of Span in .NET Standard 2.0.
    /// </summary>
    public struct StringSegment : IEquatable<StringSegment>
    {
        /// <summary>
        /// Character at <paramref name="ix"/>
        /// </summary>
        /// <param name="ix"></param>
        /// <returns>character</returns>
        public char this[int ix] => String[Start+ix];

        /// <summary>Empty string ""</summary>
        public static StringSegment Empty = new StringSegment("");
        /// <summary>String "."</summary>
        public static StringSegment Dot = new StringSegment(".");
        /// <summary>String ".."</summary>
        public static StringSegment DotDot = new StringSegment("..");

        /// <summary>Start index</summary>
        public readonly int Start;
        /// <summary>Length</summary>
        public readonly int Length;
        /// <summary>String</summary>
        public readonly string String;

        /// <summary>Implicit converter</summary>
        public static implicit operator String(StringSegment str) => str.String.Substring(str.Start, str.Length);
        /// <summary>Implicit converter</summary>
        public static implicit operator StringSegment(String str) => new StringSegment(str);

        /// <summary>
        /// Create span of characters.
        /// </summary>
        /// <param name="str"></param>
        public StringSegment(string str)
        {
            this.String = str ?? throw new ArgumentNullException(nameof(str));
            this.Start = 0;
            this.Length = str.Length;
        }

        /// <summary>
        /// Create span of characters.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="start"></param>
        /// <param name="length"></param>
        public StringSegment(string str, int start, int length)
        {
            this.String = str ?? throw new ArgumentNullException(nameof(str));
            if (start < 0 || start > str.Length) throw new ArgumentOutOfRangeException(nameof(start));
            if (length < 0 || start + length > str.Length) throw new ArgumentOutOfRangeException(nameof(length));
            this.Start = start;
            this.Length = length;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is StringSegment ss)
            {
                if (ss.String == String && ss.Start == Start && ss.Length == Length) return true;
                //if (ss.hashcode != hashcode) return false;
                if (ss.Length != Length) return false;
                for (int i = 0; i < Length; i++)
                {
                    char c1 = String[Start + i], c2 = ss.String[ss.Start + i];
                    if (c1 != c2) return false;
                }
                return true;
            }
            return false;
        }

        /// <summary></summary>
        public static bool operator ==(StringSegment a, StringSegment b)
        {
            if (a.String == b.String && a.Start == b.Start && a.Length == b.Length) return true;
            //if (ss.hashcode != hashcode) return false;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                char c1 = a.String[a.Start + i], c2 = b.String[b.Start + i];
                if (c1 != c2) return false;
            }
            return true;
        }

        /// <summary></summary>
        public static bool operator !=(StringSegment a, StringSegment b)
        {
            if (a.String == b.String && a.Start == b.Start && a.Length == b.Length) return false;
            //if (ss.hashcode != hashcode) return false;
            if (a.Length != b.Length) return true;
            for (int i = 0; i < a.Length; i++)
            {
                char c1 = a.String[a.Start + i], c2 = b.String[b.Start + i];
                if (c1 != c2) return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            // Calculate hashcode from content
            int hash = unchecked((int)2166136261);
            for (int i = 0; i < Length; i++)
            {
                hash ^= (int)String[i + Start];
                hash *= 16777619;
            }
            return hash;
        }

        /// <summary>Append to string builder</summary>
        public void AppendTo(StringBuilder sb) 
            => sb.Append(String, Start, Length);

        /// <inheritdoc/>
        public override string ToString()
            => String.Substring(Start, Length);

        /// <inheritdoc/>
        public bool Equals(StringSegment other)
            => Comparer.Instance.Equals(this, other);

        /// <summary>
        /// EqualityComparer
        /// </summary>
        public class Comparer : IEqualityComparer<StringSegment>
        {
            private static Comparer instance => new Comparer();

            /// <summary>
            /// Singleton instance
            /// </summary>
            public static Comparer Instance => instance;

            /// <summary>
            /// Compare equal content
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <returns></returns>
            public bool Equals(StringSegment x, StringSegment y)
            {
                // Check for content equality
                if (x.String == y.String && x.Start == y.Start && x.Length == y.Length) return true;

                // Compare hashcode
                //if (x.hashcode != y.hashcode) return false;

                // Compare lengths
                if (x.Length != y.Length) return false;

                // Compare characters
                for (int i = 0; i < x.Length; i++)
                {
                    char c1 = x.String[x.Start + i], c2 = y.String[y.Start + i];
                    if (c1 != c2) return false;
                }

                return true;
            }

            /// <summary>
            /// Calculate hashcode for 
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public int GetHashCode(StringSegment obj)
                => obj.GetHashCode();
        }
    }
}
