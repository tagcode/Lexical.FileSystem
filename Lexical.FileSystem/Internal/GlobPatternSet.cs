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
    /// Glob pattern set theory operations
    /// </summary>
    public struct GlobPatternSet
    {
        /// <summary>
        /// Create union of <paramref name="leftPattern"/> and <paramref name="rightPattern"/>.
        /// </summary>
        /// <param name="leftPattern"></param>
        /// <param name="rightPattern"></param>
        /// <returns>union that contains <paramref name="leftPattern"/> and <paramref name="rightPattern"/>. May return broader union that minimal due to lack of expression capability to hold in a single pattern string.</returns>
        public static string Union(string leftPattern, string rightPattern)
        {
            // Same pattern
            if (leftPattern == rightPattern) return leftPattern;

            // Read in the tokens
            StructList24<Token> leftTokens = new StructList24<Token>(), rightTokens = new StructList24<Token>();
            Token.Enumerator leftEnumerator = new Token.Enumerator(leftPattern), rightEnumerator = new Token.Enumerator(rightPattern);
            while (leftEnumerator.MoveNext()) leftTokens.Add(leftEnumerator.Current);
            while (rightEnumerator.MoveNext()) rightTokens.Add(rightEnumerator.Current);

            // Queue of scenarios as index pairs and result so far.
            List<Line> queue = new List<Line>();
            // List of already yielded strings
            HashSet<Line> visited = new HashSet<Line>();
            // Queue the starting position
            Queue(queue, visited, 0, 0, null);
            // Final result
            List<Token> finalresult = null; int finalscore = int.MinValue;
            // Process queue
            while (queue.Count > 0)
            {
                // Take indices from queue
                Line line = queue[queue.Count - 1];
                int li = line.li, ri = line.ri; List<Token> result = line.tokens;
                queue.RemoveAt(queue.Count - 1);
                // Has more tokens
                bool leftHasMore = li < leftTokens.Count, rightHasMore = ri < rightTokens.Count;

                // l = Left input, r = right input, _ = last in result
                Token l = leftHasMore ? leftTokens[li] : (Token)Token.Type.None, r = rightHasMore ? rightTokens[ri] : (Token)Token.Type.None, _ = result == null || result.Count == 0 ? (Token)Token.Type.None : result[result.Count - 1];

                // End of tokens on both streams
                if (!leftHasMore && !rightHasMore)
                {
                    int score = Score(result);
                    if (score > finalscore) { finalresult = result; finalscore = score; }
                    continue;
                }

                // left or right have tokens
                if (leftHasMore || rightHasMore)
                {
                    // l == **
                    if (l == Token.Type.StarStar)
                    {
                        if (_ == Token.Type.StarStar) Queue(queue, visited, li + 1, ri, result);
                        else Append(queue, visited, li + 1, ri, result, l);
                        continue;
                    }

                    // r == **
                    if (r == Token.Type.StarStar)
                    {
                        if (_ == Token.Type.StarStar) Queue(queue, visited, li, ri + 1, result);
                        else Append(queue, visited, li, ri + 1, result, r);
                        continue;
                    }

                    // l == *
                    if (l == Token.Type.Star)
                    {
                        if (_ == Token.Type.Star || _ == Token.Type.StarStar) Queue(queue, visited, li + 1, ri, result);
                        else Append(queue, visited, li + 1, ri, result, l);
                        continue;
                    }

                    // r == *
                    if (r == Token.Type.Star)
                    {
                        if (_ == Token.Type.Star || _ == Token.Type.StarStar) Queue(queue, visited, li, ri + 1, result);
                        else Append(queue, visited, li, ri + 1, result, r);
                        continue;
                    }

                    // l == r
                    if (l == r)
                    {
                        Append(queue, visited, li + 1, ri + 1, result, l);
                        continue;
                    }

                    // l == ? && r == c
                    if (l == Token.Type.QuestionMark && r.Kind == Token.Type.Char)
                    {
                        Append(queue, visited, li + 1, ri + 1, result, l);
                        continue;
                    }

                    // l == c && r == ?
                    if (l.Kind == Token.Type.Char && r == Token.Type.QuestionMark)
                    {
                        Append(queue, visited, li + 1, ri + 1, result, l);
                        continue;
                    }

                    // l == c || r == c
                    if (l.Kind == Token.Type.Char || r.Kind == Token.Type.Char)
                    {
                        // Add ?
                        if (l.Kind == Token.Type.Char && r.Kind == Token.Type.Char) CloneAndAppend(queue, visited, li + 1, ri + 1, result, Token.Type.QuestionMark);
                        // Add *
                        if (_ != Token.Type.Star && _ != Token.Type.StarStar)
                        {
                            if (l.Kind == Token.Type.Char) CloneAndAppend(queue, visited, li + 1, ri, result, Token.Type.Star);
                            if (r.Kind == Token.Type.Char) Append(queue, visited, li, ri + 1, result, Token.Type.Star);
                        }
                        else
                        // Use previous *
                        {
                            if (l.Kind == Token.Type.Char) CloneAndAppend(queue, visited, li + 1, ri, result, Token.Type.None);
                            if (r.Kind == Token.Type.Char) Queue(queue, visited, li, ri + 1, result);
                        }
                        continue;
                    }

                    {
                        // Replace * with **
                        if (_ == Token.Type.Star)
                        {
                            result[result.Count - 1] = Token.Type.StarStar;
                            if (l != Token.Type.None) CloneAndAppend(queue, visited, li + 1, ri, result, Token.Type.None);
                            if (r != Token.Type.None) Queue(queue, visited, li, ri + 1, result);
                        }
                        // Add **
                        else if (_ != Token.Type.StarStar)
                        {
                            if (l != Token.Type.None) CloneAndAppend(queue, visited, li + 1, ri, result, Token.Type.StarStar);
                            if (r != Token.Type.None) Append(queue, visited, li, ri + 1, result, Token.Type.StarStar);
                        }
                        else
                        // Use previous **
                        {
                            if (l != Token.Type.None) CloneAndAppend(queue, visited, li + 1, ri, result, Token.Type.None);
                            if (r != Token.Type.None) Queue(queue, visited, li, ri + 1, result);
                        }
                    }
                }
            }
            //Console.Write($" (visited={visited.Count}) ");

            return Print(finalresult);

            // Heuristic score
            int Score(List<Token> result)
            {
                if (result == null) return int.MinValue;
                int x = 0;
                foreach (Token t in result)
                    if (t.Kind == Token.Type.Char) x += 256;
                    else if (t.Kind == Token.Type.QuestionMark) x += 16;
                    else if (t.Kind == Token.Type.Slash) x += 1024;
                    else if (t.Kind == Token.Type.Star) x -= 4;
                    else if (t.Kind == Token.Type.StarStar) x -= 16;
                return x;
            }
        }

        /// <summary>
        /// Create unions of <paramref name="leftPattern"/> and <paramref name="rightPattern"/>.
        /// </summary>
        /// <param name="leftPattern"></param>
        /// <param name="rightPattern"></param>
        /// <returns>unions</returns>
        public static IEnumerable<String> Unions(string leftPattern, string rightPattern)
        {
            throw new NotImplementedException();
            /*
            // Read in the tokens
            StructList24<Token> leftTokens = new StructList24<Token>(), rightTokens = new StructList24<Token>();
            Token.Enumerator leftEnumerator = new Token.Enumerator(leftPattern), rightEnumerator = new Token.Enumerator(rightPattern);
            while (leftEnumerator.MoveNext()) leftTokens.Add(leftEnumerator.Current);
            while (rightEnumerator.MoveNext()) rightTokens.Add(rightEnumerator.Current);

            // Queue of scenarios as index pairs and result so far.
            List<Line> queue = new List<Line>();
            // List of already yielded strings
            HashSet<Line> visited = new HashSet<Line>();
            // Queue the starting position
            Queue(queue, visited, 0, 0, null);
            // Process queue
            while (queue.Count > 0)
            {
                // Take indices from queue
                Line line = queue[queue.Count - 1];
                int li = line.li, ri = line.ri; List<Token> result = line.tokens;
                queue.RemoveAt(queue.Count - 1);
                // Has more tokens
                bool leftHasMore = li < leftTokens.Count, rightHasMore = ri < rightTokens.Count;

                // End of tokens on both streams
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

                // left still has tokens, right is at end
                else if (leftHasMore && !rightHasMore)
                {
                    // Take token
                    Token l = leftTokens[li];
                }

                // left is at end, right still has tokens
                else if (!leftHasMore && rightHasMore)
                {
                    // Take token and type
                    Token r = rightTokens[ri];
                }

                // left and right have tokens
                if (leftHasMore && rightHasMore)
                {
                    // Take tokens 
                    Token l = leftTokens[li], r = rightTokens[ri];

                    // l == **
                    if (l == Token.Type.StarStar)
                    {
                        // Next scenario
                        continue;
                    }

                    // r == **
                    if (r == Token.Type.StarStar)
                    {
                        // Next scenario
                        continue;
                    }

                    // l == *
                    if (l == Token.Type.Star)
                    {
                        // Next scenario
                        continue;
                    }

                    // r == *
                    if (r == Token.Type.Star)
                    {
                        // Next scenario
                        continue;
                    }

                    // l == ?
                    if (l == Token.Type.QuestionMark)
                    {
                        // Next scenario
                        continue;
                    }

                    // r == ?
                    if (r == Token.Type.QuestionMark)
                    {
                        // Next scenario
                        continue;
                    }

                    // l == r
                    if (l == r)
                    {
                        // Next scenario
                        continue;
                    }

                }
            }

            //Console.Write($" (visited={visited.Count}) ");
            */
        }

        /// <summary>
        /// Create intersection of <paramref name="pattern1"/> and <paramref name="pattern2"/>.
        /// </summary>
        /// <param name="pattern1"></param>
        /// <param name="pattern2"></param>
        /// <returns>intersection or null if patterns do not intersect. May return broader intersection that minimal due to lack of expression capability to hold in a single pattern string.</returns>
        public static string Intersection(string pattern1, string pattern2)
        {
            // Same pattern
            if (pattern1 == pattern2) return pattern1;

            // Iterate results
            string result = null;
            foreach (string intersection in Intersections(pattern1, pattern2))
            {
                if (result == null) result = intersection;
                else result = Union(result, intersection);
            }

            // Return 
            return result;
        }

        /// <summary>
        /// Create intersections of <paramref name="leftPattern"/> and <paramref name="rightPattern"/>.
        /// </summary>
        /// <param name="leftPattern"></param>
        /// <param name="rightPattern"></param>
        /// <returns>intersections</returns>
        public static IEnumerable<String> Intersections(string leftPattern, string rightPattern)
        {
            // Read in the tokens
            StructList24<Token> leftTokens = new StructList24<Token>(), rightTokens = new StructList24<Token>();
            Token.Enumerator leftEnumerator = new Token.Enumerator(leftPattern), rightEnumerator = new Token.Enumerator(rightPattern);
            while (leftEnumerator.MoveNext()) leftTokens.Add(leftEnumerator.Current);
            while (rightEnumerator.MoveNext()) rightTokens.Add(rightEnumerator.Current);

            // Queue of scenarios as index pairs and result so far.
            List<Line> queue = new List<Line>();
            // List of already yielded strings
            HashSet<Line> visited = new HashSet<Line>();
            // Queue the starting position
            Queue(queue, visited, 0, 0, null);
            // Process queue
            while (queue.Count > 0)
            {
                // Take indices from queue
                Line line = queue[queue.Count - 1];
                int li = line.li, ri = line.ri; List<Token> result = line.tokens;
                queue.RemoveAt(queue.Count - 1);
                // Has more tokens
                bool leftHasMore = li < leftTokens.Count, rightHasMore = ri < rightTokens.Count;

                // End of tokens on both streams
                if (!leftHasMore && !rightHasMore)
                {
                    // Print to string
                    string pattern = Print(result);
                    // yield result
                    yield return pattern;
                }

                // left still has tokens, right is at end
                else if (leftHasMore && !rightHasMore)
                {
                    // Take token
                    Token l = leftTokens[li];
                    // "*" and "**" matches against ""
                    if (l == Token.Type.Star || l == Token.Type.StarStar) Queue(queue, visited, li + 1, ri, result);
                }

                // left is at end, right still has tokens
                else if (!leftHasMore && rightHasMore)
                {
                    // Take token and type
                    Token r = rightTokens[ri];
                    // "" matches against "*" and "**"
                    if (r == Token.Type.Star || r == Token.Type.StarStar) Queue(queue, visited, li, ri + 1, result);
                }

                // left and right have tokens
                if (leftHasMore && rightHasMore)
                {
                    // Take tokens 
                    Token l = leftTokens[li], r = rightTokens[ri];

                    // l == **
                    if (l == Token.Type.StarStar)
                    {
                        // Match one and more later
                        CloneAndAppend(queue, visited, li, ri + 1, result, r);
                        // r = *, r = **
                        if (r == Token.Type.Star || r == Token.Type.StarStar)
                        {
                            //
                            Append(queue, visited, li + 1, ri, result, r);
                        }
                        else
                        // r = x
                        {
                            // Match to nothing
                            Queue(queue, visited, li + 1, ri, result);
                        }
                        // Next scenario
                        continue;
                    }

                    // r == **
                    if (r == Token.Type.StarStar)
                    {
                        // Match one and more later
                        CloneAndAppend(queue, visited, li + 1, ri, result, l);
                        // l = *, l = **
                        if (l == Token.Type.Star || l == Token.Type.StarStar)
                        {
                            //
                            Append(queue, visited, li, ri + 1, result, l);
                        }
                        else
                        {
                            // Match to nothing
                            Queue(queue, visited, li, ri + 1, result);
                        }
                        // Next scenario
                        continue;
                    }

                    // l == *
                    if (l == Token.Type.Star)
                    {
                        // * & /
                        if (r == Token.Type.Slash)
                        {
                            // Match / to nothing
                            Queue(queue, visited, li + 1, ri, result);
                            continue;
                        }
                        // Match to one and more later
                        CloneAndAppend(queue, visited, li, ri + 1, result, r);
                        // Match to r = *
                        if (r == Token.Type.Star)
                        {
                            Append(queue, visited, li + 1, ri, result, r);
                        }
                        else
                        {
                            // Match to nothing
                            Queue(queue, visited, li + 1, ri, result);
                        }
                        // Next scenario
                        continue;
                    }

                    // r == *
                    if (r == Token.Type.Star)
                    {
                        // / & *
                        if (l == Token.Type.Slash)
                        {
                            // Match / to nothing
                            Queue(queue, visited, li, ri + 1, result);
                            continue;
                        }

                        // Match one, and more later
                        CloneAndAppend(queue, visited, li + 1, ri, result, l);
                        if (l == Token.Type.Star)
                        {
                            Append(queue, visited, li, ri + 1, result, l);
                        }
                        else
                        {
                            // Match to nothing
                            Queue(queue, visited, li, ri + 1, result);
                        }
                        // Next scenario
                        continue;
                    }

                    // l == ?
                    if (l == Token.Type.QuestionMark)
                    {
                        // ? & /
                        if (r == Token.Type.Slash) continue;
                        // Downgrade '*' and '**' to '?'
                        if (r.Kind == Token.Type.Star || r.Kind == Token.Type.StarStar) r = Token.Type.QuestionMark;
                        // Append what ever is in right token
                        Append(queue, visited, li + 1, ri + 1, result, r);
                        // Next scenario
                        continue;
                    }

                    // r == ?
                    if (r == Token.Type.QuestionMark)
                    {
                        // / & ?
                        if (l == Token.Type.Slash) continue;
                        // Downgrade '*' and '**' to '?'
                        if (l.Kind == Token.Type.Star || l.Kind == Token.Type.StarStar) l = Token.Type.QuestionMark;
                        // Append what ever is in left token
                        Append(queue, visited, li + 1, ri + 1, result, l);
                        // Next scenario
                        continue;
                    }

                    // l == r
                    if (l == r)
                    {
                        // Append, move indices and queue.
                        Append(queue, visited, li + 1, ri + 1, result, l);
                        // Next scenario
                        continue;
                    }

                }
            }

            //Console.Write($" (visited={visited.Count}) ");
        }

        static void Queue(List<Line> queue, HashSet<Line> visited, int li, int ri, List<Token> result)
        {
            Line line = new Line(li, ri, result);
            if (visited.Add(line)) queue.Add(line);
        }

        static void Append(List<Line> queue, HashSet<Line> visited, int li, int ri, List<Token> result, Token token)
        {
            if (result == null) result = new List<Token>(10);
            if (token != Token.Type.None) result.Add(token);
            Line line = new Line(li, ri, result);
            if (visited.Add(line)) queue.Add(line);
        }

        static void CloneAndAppend(List<Line> list, HashSet<Line> visited, int li, int ri, List<Token> result, Token token)
        {
            result = result == null ? new List<Token>(10) : new List<Token>(result);
            if (token != Token.Type.None) result.Add(token);
            Line line = new Line(li, ri, result);
            if (visited.Add(line)) list.Add(line);
        }

        /// <summary>
        /// Print <paramref name="tokens"/> to string.
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        static String Print(List<Token> tokens)
        {
            if (tokens == null) return "";
            int count = 0;
            foreach (var t in tokens) count += t.Length;
            char[] arr = new char[count];
            int ix = 0;
            foreach (var t in tokens)
            {
                switch (t.Kind)
                {
                    case Token.Type.QuestionMark: arr[ix++] = '?'; break;
                    case Token.Type.Slash: arr[ix++] = '/'; break;
                    case Token.Type.Star: arr[ix++] = '*'; break;
                    case Token.Type.StarStar: arr[ix++] = '*'; arr[ix++] = '*'; break;
                    case Token.Type.Char: arr[ix++] = t.Char; break;
                    default: break;
                }
            }
            return new String(arr);
        }

        struct Line : IEquatable<Line>
        {
            /// <summary>Left and right token indices</summary>
            public readonly int li, ri;
            /// <summary>(optional)List of tokens</summary>
            public readonly List<Token> tokens;

            public Line(int li, int ri, List<Token> tokens)
            {
                this.li = li;
                this.ri = ri;
                this.tokens = tokens;
            }

            public bool Equals(Line other) => li == other.li && ri == other.ri && TokenListComparer.Instance.Equals(tokens, other.tokens);
            public override bool Equals(object other_) => other_ is Line other ? li == other.li && ri == other.ri && TokenListComparer.Instance.Equals(tokens, other.tokens) : false;
            public override int GetHashCode() => 3 * li + 5 * ri * 7 * TokenListComparer.Instance.GetHashCode(tokens);
            public override string ToString() => $"{li}, {ri}, {Print(tokens)}";
        }

        /// <summary>
        /// Glob pattern token
        /// </summary>
        public struct Token : IEquatable<Token>
        {
            /// <summary>Non-content token</summary>
            public static readonly Token None = new Token(Type.None);

            /// <summary>Implicit conversion</summary>
            public static implicit operator Type(Token l) => l.Kind;
            /// <summary>Implicit conversion</summary>
            public static implicit operator char(Token l) => l.Char;
            /// <summary>Implicit conversion</summary>
            public static implicit operator Token(Type k) => new Token(k);
            /// <summary>Implicit conversion</summary>
            public static implicit operator Token(Char c) => new Token(c);

            /// <summary>Token type</summary>
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
            /// <summary>Create record of token</summary>
            public Token(Type type)
            {
                Kind = type;
                switch (type)
                {
                    case Type.QuestionMark: Char = '?'; break;
                    case Type.Slash: Char = '/'; break;
                    case Type.Star: Char = '*'; break;
                    case Type.StarStar: Char = '*'; break;
                    default: Char = '\0'; break;
                }
            }
            /// <summary>Create record of token</summary>
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
            /// <summary>Number of characters</summary>
            public int Length => Kind == Type.None ? 0 : Kind == Type.StarStar ? 2 : 1;
            /// <summary>Append to string builder</summary>
            public void AppendTo(StringBuilder sb)
            {
                switch (Kind)
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

            /// <summary>
            /// Glob pattern token enumerable
            /// </summary>
            public struct Enumerable : IEnumerable<Token>
            {
                /// <summary>Glob pattern string</summary>
                public readonly string Pattern;

                /// <summary>Create glob pattern enumerable.</summary>
                /// <param name="pattern"></param>
                public Enumerable(string pattern)
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
            }

            /// <summary>
            /// Glob pattern token enumerator
            /// </summary>
            public struct Enumerator : IEnumerator<Token>
            {
                /// <summary>Glob pattern string</summary>
                public readonly string Pattern;

                /// <summary>Character index.</summary>
                int index;

                /// <summary>Current token.</summary>
                Token current;

                /// <summary>Create glob pattern token enumerable.</summary>
                /// <param name="pattern"></param>
                public Enumerator(string pattern)
                {
                    Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
                    index = 0;
                    current = Token.None;
                }

                /// <summary>Current token.</summary>
                public Token Current => current;
                object IEnumerator.Current => current;

                /// <summary></summary>
                public void Dispose() { }

                /// <summary>Move to next token</summary>
                public bool MoveNext()
                {
                    // Out of index
                    if (index >= Pattern.Length) { current = Token.None; return false; }

                    // Read char
                    char ch = Pattern[index++];

                    // Choose token
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

        }

        class TokenListComparer : IEqualityComparer<List<Token>>
        {
            static TokenListComparer instance = new TokenListComparer();
            public static TokenListComparer Instance => instance;

            public bool Equals(List<Token> x, List<Token> y)
            {
                int xc = x == null ? 0 : x.Count, yc = y == null ? 0 : y.Count;
                if (xc != yc) return false;
                for (int i = 0; i < xc; i++) if (x[i] != y[i]) return false;
                return true;
            }

            public int GetHashCode(List<Token> list)
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


}
