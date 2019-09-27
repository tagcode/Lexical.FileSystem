// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Generic;

namespace Lexical.FileSystem.Internal
{
    /// <summary>
    /// Span of characters of a <see cref="String"/>.
    /// 
    /// Used as workaround for missing Span class in .NET Standard.
    /// </summary>
    public struct StringSegment : IEquatable<StringSegment>
    {
        /// <summary>
        /// Empty string "".
        /// </summary>
        public static StringSegment Empty = new StringSegment("");

        /// <summary>
        /// String ".".
        /// </summary>
        public static StringSegment Dot = new StringSegment(".");

        /// <summary>
        /// String "..".
        /// </summary>
        public static StringSegment DotDot = new StringSegment("..");

        /// <summary>
        /// Start index
        /// </summary>
        public readonly int Start;

        /// <summary>
        /// Length
        /// </summary>
        public readonly int Length;

        /// <summary>
        /// String
        /// </summary>
        public readonly string String;

        /// <summary>
        /// Hashcode
        /// </summary>
        int hashcode;

        /// <summary>
        /// Implicit converter
        /// </summary>
        /// <param name="str"></param>
        public static implicit operator String(StringSegment str)
            => str.ToString();

        /// <summary>
        /// Implicit converter
        /// </summary>
        /// <param name="str"></param>
        public static implicit operator StringSegment(String str)
            => new StringSegment(str);

        /// <summary>
        /// Create span of characters.
        /// </summary>
        /// <param name="str"></param>
        public StringSegment(string str)
        {
            this.String = str ?? throw new ArgumentNullException(nameof(str));
            this.Start = 0;
            this.Length = str.Length;
            this.hashcode = str.GetHashCode();

            // Hash
            int hash = unchecked((int)2166136261);
            for (int i = 0; i < Length; i++)
            {
                hash ^= (int)String[i + Start];
                hash *= 16777619;
            }
            this.hashcode = hash;
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

            // Hash
            int hash = unchecked((int)2166136261);
            for (int i = 0; i < Length; i++)
            {
                hash ^= (int)String[i + Start];
                hash *= 16777619;
            }
            this.hashcode = hash;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is StringSegment ss)
            {
                if (ss.hashcode == hashcode && ss.String == String && ss.Start == Start && ss.Length == Length) return true;
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

        /// <inheritdoc/>
        public override int GetHashCode()
            => hashcode;

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
                if (x.hashcode == y.hashcode && x.String == y.String && x.Start == y.Start && x.Length == y.Length) return true;
                if (x.Length != y.Length) return false;
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
                => obj.hashcode;
        }
    }
}
