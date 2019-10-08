// -----------------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           20.8.2018
// Url:            http://lexical.fi
// -----------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

namespace Lexical.FileSystem.Internal
{
    /// <summary>
    /// Pair (2-tuple). Hashcode is cached. Elements are immutable. Type is stack allocated. 
    /// </summary>
    /// <typeparam name="A"></typeparam>
    /// <typeparam name="B"></typeparam>
    public struct Pair<A, B> : IEquatable<Pair<A, B>>, IComparable<Pair<A, B>>
    {
        /// <summary>A</summary>
        public readonly A a;
        /// <summary>B</summary>
        public readonly B b;
        /// <summary>Precalculated hashcode</summary>
        public readonly int hashcode;


        /// <summary>
        /// Create Pair (2-tuple).
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public Pair(A a, B b) { this.a = a; this.b = b; hashcode = (a == null ? 0 : 11 * a.GetHashCode()) + (b == null ? 0 : 13 * b.GetHashCode()); }

        /// <summary>
        /// Get Hash-Code
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => hashcode;

        /// <summary>
        /// Test equality with default element comparer.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) => obj is Pair<A, B> other ? EqualityComparer.Default.Equals(this, other) : false;

        /// <summary>
        /// Test equality with default element comparer.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(Pair<A, B> other) => EqualityComparer.Default.Equals(this, other);

        /// <summary>
        /// Compare order with default element comparer.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(Pair<A, B> other) => Comparer.Default.Compare(this, other);

        /// <summary>
        /// Equality comparer
        /// </summary>
        public class EqualityComparer : IEqualityComparer<Pair<A, B>>
        {
            private static EqualityComparer singleton;

            /// <summary>
            /// Default instance
            /// </summary>
            public static EqualityComparer Default => singleton ?? (singleton = new EqualityComparer());

            /// <summary>
            /// Element A comparer.
            /// </summary>
            public readonly IEqualityComparer<A> aComparer;

            /// <summary>
            /// Element B comparer.
            /// </summary>
            public readonly IEqualityComparer<B> bComparer;


            /// <summary>
            /// Create equality comparer.
            /// </summary>
            /// <param name="aComparer">(optional) element comparer</param>
            /// <param name="bComparer">(optional) element comparer</param>
            public EqualityComparer(IEqualityComparer<A> aComparer = default, IEqualityComparer<B> bComparer = default)
            {
                this.aComparer = aComparer ?? EqualityComparer<A>.Default;
                this.bComparer = bComparer ?? EqualityComparer<B>.Default;
            }

            /// <summary>
            /// Test equality
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <returns></returns>
            public bool Equals(Pair<A, B> x, Pair<A, B> y)
            {
                if (!aComparer.Equals(x.a, y.a)) return false;
                if (!bComparer.Equals(x.b, y.b)) return false;
                return true;
            }

            /// <summary>
            /// Calculate hash-code
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public int GetHashCode(Pair<A, B> obj)
                => (obj.a == null ? 0 : 11 * obj.a.GetHashCode()) + (obj.b == null ? 0 : 13 * obj.b.GetHashCode());
        }

        /// <summary>
        /// Order comparer
        /// </summary>
        public class Comparer : IComparer<Pair<A, B>>
        {
            private static Comparer singleton;

            /// <summary>
            /// Default instance
            /// </summary>
            public static Comparer Default => singleton ?? (singleton = new Comparer());

            /// <summary>
            /// Element A comparer
            /// </summary>
            public readonly IComparer<A> aComparer;

            /// <summary>
            /// Element B comparer
            /// </summary>
            public readonly IComparer<B> bComparer;


            /// <summary>
            /// Create comparer
            /// </summary>
            /// <param name="aComparer">(optional) element comparer</param>
            /// <param name="bComparer">(optional) element comparer</param>
            public Comparer(IComparer<A> aComparer = default, IComparer<B> bComparer = default)
            {
                this.aComparer = aComparer ?? System.Collections.Generic.Comparer<A>.Default;
                this.bComparer = bComparer ?? System.Collections.Generic.Comparer<B>.Default;
            }

            /// <summary>
            /// Compare for order
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <returns></returns>
            public int Compare(Pair<A, B> x, Pair<A, B> y)
            {

                int compare = 0;
                compare = aComparer.Compare(x.a, y.a);
                if (compare != 0) return compare;
                compare = bComparer.Compare(x.b, y.b);
                if (compare != 0) return compare;
                return 0;
            }
        }

        /// <summary>
        /// Print info
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            AppendTo(sb);
            return sb.ToString();
        }

        /// <summary>
        /// Append info to <paramref name="sb"/>
        /// </summary>
        /// <param name="sb"></param>
        public void AppendTo(StringBuilder sb)
        {
            sb.Append(GetType().Name);
            sb.Append("(");
            sb.Append(a);
            sb.Append(", ");
            sb.Append(b);
            sb.Append(")");
        }

    }

    /// <summary>
    /// Triple (3-tuple). Hashcode is cached. Elements are immutable. Type is stack allocated. 
    /// </summary>
    /// <typeparam name="A"></typeparam>
    /// <typeparam name="B"></typeparam>
    /// <typeparam name="C"></typeparam>
    public struct Triple<A, B, C> : IEquatable<Triple<A, B, C>>, IComparable<Triple<A, B, C>>
    {
        /// <summary>A</summary>
        public readonly A a;
        /// <summary>B</summary>
        public readonly B b;
        /// <summary>C</summary>
        public readonly C c;
        /// <summary>Precalculated hashcode</summary>
        public readonly int hashcode;


        /// <summary>
        /// Create Triple (3-tuple).
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        public Triple(A a, B b, C c) { this.a = a; this.b = b; this.c = c; hashcode = (a == null ? 0 : 11 * a.GetHashCode()) + (b == null ? 0 : 13 * b.GetHashCode()) + (c == null ? 0 : 17 * c.GetHashCode()); }

        /// <summary>
        /// Get Hash-Code
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => hashcode;

        /// <summary>
        /// Test equality with default element comparer.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) => obj is Triple<A, B, C> other ? EqualityComparer.Default.Equals(this, other) : false;

        /// <summary>
        /// Test equality with default element comparer.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(Triple<A, B, C> other) => EqualityComparer.Default.Equals(this, other);

        /// <summary>
        /// Compare order with default element comparer.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(Triple<A, B, C> other) => Comparer.Default.Compare(this, other);

        /// <summary>
        /// Equality comparer
        /// </summary>
        public class EqualityComparer : IEqualityComparer<Triple<A, B, C>>
        {
            private static EqualityComparer singleton;

            /// <summary>
            /// Default instance
            /// </summary>
            public static EqualityComparer Default => singleton ?? (singleton = new EqualityComparer());

            /// <summary>
            /// Element A comparer.
            /// </summary>
            public readonly IEqualityComparer<A> aComparer;

            /// <summary>
            /// Element B comparer.
            /// </summary>
            public readonly IEqualityComparer<B> bComparer;

            /// <summary>
            /// Element C comparer.
            /// </summary>
            public readonly IEqualityComparer<C> cComparer;


            /// <summary>
            /// Create equality comparer.
            /// </summary>
            /// <param name="aComparer">(optional) element comparer</param>
            /// <param name="bComparer">(optional) element comparer</param>
            /// <param name="cComparer">(optional) element comparer</param>
            public EqualityComparer(IEqualityComparer<A> aComparer = default, IEqualityComparer<B> bComparer = default, IEqualityComparer<C> cComparer = default)
            {
                this.aComparer = aComparer ?? EqualityComparer<A>.Default;
                this.bComparer = bComparer ?? EqualityComparer<B>.Default;
                this.cComparer = cComparer ?? EqualityComparer<C>.Default;
            }

            /// <summary>
            /// Test equality
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <returns></returns>
            public bool Equals(Triple<A, B, C> x, Triple<A, B, C> y)
            {
                if (!aComparer.Equals(x.a, y.a)) return false;
                if (!bComparer.Equals(x.b, y.b)) return false;
                if (!cComparer.Equals(x.c, y.c)) return false;
                return true;
            }

            /// <summary>
            /// Calculate hash-code
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public int GetHashCode(Triple<A, B, C> obj)
                => (obj.a == null ? 0 : 11 * obj.a.GetHashCode()) + (obj.b == null ? 0 : 13 * obj.b.GetHashCode()) + (obj.c == null ? 0 : 17 * obj.c.GetHashCode());
        }

        /// <summary>
        /// Order comparer
        /// </summary>
        public class Comparer : IComparer<Triple<A, B, C>>
        {
            private static Comparer singleton;

            /// <summary>
            /// Default instance
            /// </summary>
            public static Comparer Default => singleton ?? (singleton = new Comparer());

            /// <summary>
            /// Element A comparer
            /// </summary>
            public readonly IComparer<A> aComparer;

            /// <summary>
            /// Element B comparer
            /// </summary>
            public readonly IComparer<B> bComparer;

            /// <summary>
            /// Element C comparer
            /// </summary>
            public readonly IComparer<C> cComparer;


            /// <summary>
            /// Create comparer
            /// </summary>
            /// <param name="aComparer">(optional) element comparer</param>
            /// <param name="bComparer">(optional) element comparer</param>
            /// <param name="cComparer">(optional) element comparer</param>
            public Comparer(IComparer<A> aComparer = default, IComparer<B> bComparer = default, IComparer<C> cComparer = default)
            {
                this.aComparer = aComparer ?? System.Collections.Generic.Comparer<A>.Default;
                this.bComparer = bComparer ?? System.Collections.Generic.Comparer<B>.Default;
                this.cComparer = cComparer ?? System.Collections.Generic.Comparer<C>.Default;
            }

            /// <summary>
            /// Compare for order
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <returns></returns>
            public int Compare(Triple<A, B, C> x, Triple<A, B, C> y)
            {

                int compare = 0;
                compare = aComparer.Compare(x.a, y.a);
                if (compare != 0) return compare;
                compare = bComparer.Compare(x.b, y.b);
                if (compare != 0) return compare;
                compare = cComparer.Compare(x.c, y.c);
                if (compare != 0) return compare;
                return 0;
            }
        }

        /// <summary>
        /// Print info
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            AppendTo(sb);
            return sb.ToString();
        }

        /// <summary>
        /// Append info to <paramref name="sb"/>
        /// </summary>
        /// <param name="sb"></param>
        public void AppendTo(StringBuilder sb)
        {
            sb.Append(GetType().Name);
            sb.Append("(");
            sb.Append(a);
            sb.Append(", ");
            sb.Append(b);
            sb.Append(", ");
            sb.Append(c);
            sb.Append(")");
        }

    }
}
