// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           11.10.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using static Lexical.FileSystem.Internal.GlobPattern;

namespace Lexical.FileSystem.Internal
{
    /// <summary>
    /// Glob pattern set theory operations
    /// </summary>
    public struct GlobPatternSet
    {
        /// <summary>
        /// Create intersection of <paramref name="pattern1"/> and <paramref name="pattern2"/>.
        /// </summary>
        /// <param name="pattern1"></param>
        /// <param name="pattern2"></param>
        /// <returns>intersection or null if patterns do not intersect.</returns>
        public static string Intersection(string pattern1, string pattern2)
        {
            StructList12<string> intersections = new StructList12<string>();
            
            foreach (var intersection in Intersections(pattern1, pattern2)) 
                intersections.Add(intersection);

            if (intersections.Count == 0) return null;
            if (intersections.Count == 1) return intersections[0];
            // TODO create union 
            //throw new NotImplementedException();
            // for debugging
            return string.Join(", ", intersections.ToArray());
        }

        /// <summary>
        /// Create intersections of <paramref name="leftPattern"/> and <paramref name="rightPattern"/>.
        /// </summary>
        /// <param name="leftPattern"></param>
        /// <param name="rightPattern"></param>
        /// <returns>intersections</returns>
        public static IEnumerable<String> Intersections(string leftPattern, string rightPattern)
        {
            // Read in the literals
            StructList24<Literal> leftLiterals = new StructList24<Literal>(), rightLiterals = new StructList24<Literal>();            
            Enumerator leftEnumerator = new Enumerator(leftPattern), rightEnumerator = new Enumerator(rightPattern);
            while (leftEnumerator.MoveNext()) leftLiterals.Add(leftEnumerator.Current);
            while (rightEnumerator.MoveNext()) rightLiterals.Add(rightEnumerator.Current);

            // Queue of scenarios as index pairs and result so far.
            List<Line> queue = new List<Line>();
            // List of already yielded strings
            HashSet<Line> visited = new HashSet<Line>();
            // Queue the starting position
            Queue(queue, visited, 0, 0, null);
            // Process queue
            while (queue.Count>0)
            {
                // Take indices from queue
                Line line = queue[queue.Count - 1];
                int li = line.li, ri = line.ri; List<Literal> result = line.literals;
                queue.RemoveAt(queue.Count - 1);
                // Has more literals
                bool leftHasMore = li < leftLiterals.Count, rightHasMore = ri < rightLiterals.Count;

                // End of literals on both streams
                if (!leftHasMore && !rightHasMore)
                {
                    // Yield possible result
                    if (result != null)
                    {
                        // Print to string
                        string pattern = Print(result);
                        // yield result
                        yield return pattern; 
                    }
                }

                // left still has literals, right is at end
                else if (leftHasMore && !rightHasMore)
                {
                    // Take literal
                    Literal left = leftLiterals[li]; 
                    // "*" and "**" matches against ""
                    if (left.Type == Literal.Kind.Star || left.Type == Literal.Kind.StarStar) Queue(queue, visited, li + 1, ri, result);
                }

                // left is at end, right still has literals
                else if (leftHasMore && !rightHasMore)
                {
                    // Take literal and type
                    Literal right = rightLiterals[ri];
                    // "" matches against "*" and "**"
                    if (right.Type == Literal.Kind.Star || right.Type == Literal.Kind.StarStar) Queue(queue, visited, li, ri + 1, result);
                }

                // left and right have literals
                if (leftHasMore && rightHasMore)
                {
                    // Take literals 
                    Literal l = leftLiterals[li], r = rightLiterals[ri];

                    // l == "**" matches against anything on the right
                    if (l.Type == Literal.Kind.StarStar)
                    {
                        // Branch into scenarios 
                        // B) both indices proceed - right is equally or more restrictive than left, append it
                        CloneAndAppend(queue, visited, li + 1, ri + 1, result, r);
                        // C) left remains on "**" literal, right proceeds. Right restricts more
                        Append(queue, visited, li, ri + 1, result, r);
                        // Next scenario
                        continue;
                    }

                    // any literal on left matches aginst "**" on the right
                    if (r.Type == Literal.Kind.StarStar)
                    {
                        // Branch into scenarios 
                        // B) both indices proceed
                        CloneAndAppend(queue, visited, li + 1, ri + 1, result, l);
                        // C) left proceeds and right remains on "**" literal
                        Append(queue, visited, li +1, ri, result, l);
                        // Next scenario
                        continue;
                    }

                    // l == "*" matches against anything on the right except "/"
                    if (l.Type == Literal.Kind.Star && r.Type != Literal.Kind.Slash)
                    {
                        // Branch into scenarios 
                        // B) both indices proceed
                        CloneAndAppend(queue, visited, li + 1, ri + 1, result, r);
                        // C) left remains on "*" literal and right proceeds
                        Append(queue, visited, li, ri + 1, result, r);
                        // Next scenario
                        continue;
                    }

                    // anything but "/" on the left matches to "*" on the right
                    if (l.Type != Literal.Kind.Slash && r.Type == Literal.Kind.Star)
                    {
                        // Branch into scenarios 
                        // B) both indices proceed as-is
                        CloneAndAppend(queue, visited, li + 1, ri + 1, result, l);
                        // C) left proceeds and right remains on "*" literal
                        Append(queue, visited, li +1, ri, result, l);
                        // Next scenario
                        continue;
                    }

                    // l == "?" and r !='*' and r != '**'
                    if (l.Type == Literal.Kind.QuestionMark && (r.Type != Literal.Kind.Star && r.Type != Literal.Kind.StarStar))
                    {
                        // Append what ever is in right literal
                        Append(queue, visited, li + 1, ri + 1, result, r);
                        // Next scenario
                        continue;
                    }

                    // l !='*' and l != '**' and r == "?"
                    if ((l.Type != Literal.Kind.Star && l.Type != Literal.Kind.StarStar) && r.Type == Literal.Kind.QuestionMark)
                    {
                        // Append what ever is in left literal
                        Append(queue, visited, li + 1, ri + 1, result, l);
                        // Next scenario
                        continue;
                    }

                    // l1 == l2 (except "*" and "**")
                    if (l == r && (l.Type != Literal.Kind.Star && l.Type != Literal.Kind.StarStar))
                    {
                        // Append, move indices and queue.
                        Append(queue, visited, li + 1, ri + 1, result, l);
                        // Next scenario
                        continue;
                    }

                }
            }
        }

        static void Queue(List<Line> queue, HashSet<Line> visited, int li, int ri, List<Literal> result)
        {
            Line line = new Line(li, ri, result);
            if (visited.Add(line)) queue.Add(line);
        }

        static void Append(List<Line> queue, HashSet<Line> visited, int li, int ri, List<Literal> result, Literal literal)
        {
            if (result == null) result = new List<Literal>(10);
            result.Add(literal);
            Line line = new Line(li, ri, result);
            if (visited.Add(line)) queue.Add(line);
        }

        static void CloneAndAppend(List<Line> list, HashSet<Line> visited, int li, int ri, List<Literal> result, Literal literal)
        {
            result = result == null ? new List<Literal>(10) : new List<Literal>(result);
            result.Add(literal);
            Line line = new Line(li, ri, result);
            if (visited.Add(line)) list.Add(line);
        }

        /// <summary>
        /// Print <paramref name="literals"/> to string.
        /// </summary>
        /// <param name="literals"></param>
        /// <returns></returns>
        static String Print(List<Literal> literals)
        {
            StringBuilder sb = new StringBuilder(literals.Count + 4);
            foreach (var l in literals) l.AppendTo(sb);
            return sb.ToString();
        }

        struct Line : IEquatable<Line>
        {
            /// <summary>Left and right literal indices</summary>
            public readonly int li, ri;
            /// <summary>(optional)List of literals</summary>
            public readonly List<Literal> literals;

            public Line(int li, int ri, List<Literal> literals)
            {
                this.li = li;
                this.ri = ri;
                this.literals = literals;
            }

            public bool Equals(Line other) => li == other.li && ri == other.ri && LiteralListComparer.Instance.Equals(literals, other.literals);
            public override bool Equals(object other_) => other_ is Line other ? li == other.li && ri == other.ri && LiteralListComparer.Instance.Equals(literals, other.literals) : false;
            public override int GetHashCode() => 3 * li + 5 * ri * 7 * LiteralListComparer.Instance.GetHashCode(literals);
            public override string ToString() => $"{li}, {ri}, {Print(literals)}";
        }
    }

    class LiteralListComparer : IEqualityComparer<List<Literal>>
    {
        static LiteralListComparer instance = new LiteralListComparer();
        public static LiteralListComparer Instance => instance;

        public bool Equals(List<Literal> x, List<Literal> y)
        {
            int xc = x == null ? 0 : x.Count, yc = y == null ? 0 : y.Count;
            if (xc != yc) return false;
            for (int i = 0; i<xc; i++) if (x[i] != y[i]) return false;
            return true;
        }

        public int GetHashCode(List<Literal> list)
        {
            int hash = unchecked((int)2166136261);
            if (list != null)
            {
                foreach (var l in list)
                {
                    hash ^= l.GetHashCode();
                    hash *= 16777619;
                }
            }
            return hash;
        }
    }

}
