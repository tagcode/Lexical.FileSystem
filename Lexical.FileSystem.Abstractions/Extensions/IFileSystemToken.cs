// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           30.10.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Token extension methods.
    /// </summary>
    public static class IFileSystemTokenExtensions
    {
        /// <summary>
        /// (optional) Get token object
        /// </summary>
        /// <param name="tokenContainer"></param>
        /// <returns>object or null</returns>
        public static object Token(this IFileSystemToken tokenContainer)
            => tokenContainer is IFileSystemTokenObject tokenObj ? tokenObj.Token : null;

        /// <summary>
        /// (optional) Get token type
        /// </summary>
        /// <param name="tokenContainer"></param>
        /// <returns>type or null</returns>
        public static string Key(this IFileSystemToken tokenContainer)
            => tokenContainer is IFileSystemTokenObject tokenObj ? tokenObj.Key : null;


        /// <summary>
        /// (optional) Get token path patterns.
        /// </summary>
        /// <param name="tokenContainer"></param>
        /// <returns>type or null</returns>
        public static string[] Patterns(this IFileSystemToken tokenContainer)
            => tokenContainer is IFileSystemTokenObject tokenObj ? tokenObj.Patterns : null;

        /// <summary>
        /// Query for a token object at path <paramref name="path"/> as type <paramref name="key"/>.
        /// </summary>
        /// <param name="tokenContainer"></param>
        /// <param name="path">(optional) path to query token at</param>
        /// <param name="key">(optional) key to query, typically <see cref="Type.FullName"/></param>
        /// <param name="token">array of tokens, or null if failed to find matching tokens</param>
        /// <returns>true if tokens were found for the parameters</returns>
        public static bool TryGet(this IFileSystemToken tokenContainer, string path, string key, out object token)
        {
            if (tokenContainer is IFileSystemTokenProvider tokenProvider && tokenProvider.TryGet(path, key, out token)) return true;
            token = null;
            return false;
        }

        /// <summary>
        /// Query for a token object at path <paramref name="path"/> as type <paramref name="key"/>.
        /// </summary>
        /// <param name="tokenContainer"></param>
        /// <param name="path">(optional) path to query token at</param>
        /// <param name="key">(optional) key to query, typically <see cref="Type.FullName"/></param>
        /// <param name="tokens">array of tokens, or null if failed to find matching tokens</param>
        /// <returns>true if tokens were found for the parameters</returns>
        public static bool TryGetAll(this IFileSystemToken tokenContainer, string path, string key, out object[] tokens)
        {
            if (tokenContainer is IFileSystemTokenProvider tokenProvider && tokenProvider.TryGetAll(path, key, out tokens)) return true;
            tokens = null;
            return false;
        }

        /// <summary>
        /// Query for a token object at path <paramref name="path"/> as type <typeparamref name="T"/>.
        /// 
        /// Uses <typeparamref name="T"/>.FullName as Key.
        /// </summary>
        /// <param name="tokenContainer"></param>
        /// <param name="path">(optional) path to query token at</param>
        /// <param name="token">array of tokens, or null if failed to find matching tokens</param>
        /// <returns>true if tokens were found for the parameters</returns>
        public static bool TryGet<T>(this IFileSystemToken tokenContainer, string path, out T token)
        {
            object obj;
            if (tokenContainer is IFileSystemTokenProvider tokenProvider && tokenProvider.TryGet(path, typeof(T).FullName, out obj) && obj is T casted) { token = casted; return true; }
            token = default;
            return false;
        }

        /// <summary>
        /// Query for a token object at path <paramref name="path"/> as type <typeparamref name="T"/>.
        /// 
        /// Uses <typeparamref name="T"/>.FullName as Key.
        /// </summary>
        /// <param name="tokenContainer"></param>
        /// <param name="path">(optional) path to query token at</param>
        /// <param name="tokens">array of tokens, or null if failed to find matching tokens</param>
        /// <returns>true if tokens were found for the parameters</returns>
        public static bool TryGetAll<T>(this IFileSystemToken tokenContainer, string path, out T[] tokens)
        {
            object[] objs;
            if (tokenContainer is IFileSystemTokenProvider tokenProvider && tokenProvider.TryGetAll(path, typeof(T).FullName, out objs))
            {
                List<T> list = new List<T>(objs.Length);
                for (int i = 0; i < objs.Length; i++)
                {
                    if (objs[i] is T casted) list.Add(casted);
                }
                tokens = list.ToArray();
                return list.Count > 0;
            }
            tokens = null;
            return false;
        }


        /// <summary>
        /// Query for a token object at path <paramref name="path"/> as type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="tokenContainer"></param>
        /// <param name="path">(optional) path to query token at</param>
        /// <param name="key">(optional) key to query, typically <see cref="Type.FullName"/></param>
        /// <param name="token">array of tokens, or null if failed to find matching tokens</param>
        /// <returns>true if tokens were found for the parameters</returns>
        public static bool TryGet<T>(this IFileSystemToken tokenContainer, string path, string key, out T token)
        {
            object obj;
            if (tokenContainer is IFileSystemTokenProvider tokenProvider && tokenProvider.TryGet(path, key, out obj) && obj is T casted) { token = casted; return true; }
            token = default;
            return false;
        }

        /// <summary>
        /// Query for a token object at path <paramref name="path"/> as type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="tokenContainer"></param>
        /// <param name="path">(optional) path to query token at</param>
        /// <param name="key">(optional) key to query, typically <see cref="Type.FullName"/></param>
        /// <param name="tokens">array of tokens, or null if failed to find matching tokens</param>
        /// <returns>true if tokens were found for the parameters</returns>
        public static bool TryGetAll<T>(this IFileSystemToken tokenContainer, string path, string key, out T[] tokens)
        {
            object[] objs;
            if (tokenContainer is IFileSystemTokenProvider tokenProvider && tokenProvider.TryGetAll(path, key, out objs))
            {
                List<T> list = new List<T>(objs.Length);
                for (int i = 0; i < objs.Length; i++)
                {
                    if (objs[i] is T casted) list.Add(casted);
                }
                tokens = list.ToArray();
                return list.Count>0;
            }
            tokens = null;
            return false;
        }

        /// <summary>
        /// If <paramref name="tokenContainer"/> is a single token, then enumerates it.
        /// If <paramref name="tokenContainer"/> is token collection, then enumerates all contained tokens.
        /// If <paramref name="recurse"/> is true, then enumerates tree of collections.
        /// </summary>
        /// <param name="tokenContainer"></param>
        /// <param name="recurse"></param>
        /// <returns>enumerable of tokens</returns>
        public static IEnumerable<IFileSystemToken> ListTokens(this IFileSystemToken tokenContainer, bool recurse = true)
        {
            if (tokenContainer is IFileSystemTokenEnumerable enumr)
            {
                if (!recurse) return enumr;
                return Recurse(enumr);
            }
            return new IFileSystemToken[] { tokenContainer };
            IEnumerable<IFileSystemToken> Recurse(IEnumerable<IFileSystemToken> tokens)
            {
                StructList12<IFileSystemToken> queue = new StructList12<IFileSystemToken>();
                foreach (IFileSystemToken t in tokens) queue.Add(t);
                StructListSorter<StructList12<IFileSystemToken>, IFileSystemToken>.Reverse(ref queue);
                while (queue.Count > 0)
                {
                    int ix = queue.Count - 1;
                    IFileSystemToken t = queue[ix];
                    queue.RemoveAt(ix);

                    if (t is IFileSystemTokenEnumerable enumr_)
                        foreach (IFileSystemToken tt in enumr_) queue.Add(tt);
                    else yield return t;
                }
            }
        }

        /// <summary>
        /// Concatenates two tokens non-recursively.
        /// Tokens may be null valued.
        /// </summary>
        /// <param name="t1">(optional) Token</param>
        /// <param name="t2">(optional) Token</param>
        /// <returns>null, t1, t2, or concatenated token</returns>
        public static IFileSystemToken Concat(this IFileSystemToken t1, IFileSystemToken t2)
        {
            StructList4<IFileSystemToken> tokens = new StructList4<IFileSystemToken>();
            if (t1 != null)
            {
                if (t1 is IFileSystemTokenEnumerable enumr) foreach (IFileSystemToken t in enumr) tokens.AddIfNew(t);
                else tokens.AddIfNew(t1);
            }
            if (t2 != null)
            {
                if (t2 is IFileSystemTokenEnumerable enumr) foreach (IFileSystemToken t in enumr) tokens.AddIfNew(t);
                else tokens.AddIfNew(t2);
            }
            if (tokens.Count == 0) return null;
            if (tokens.Count == 1) return tokens[0];
            return new FileSystemTokenList(tokens.ToArray());
        }

        /// <summary>
        /// Concatenates three tokens non-recursively.
        /// Tokens may be null valued.
        /// </summary>
        /// <param name="t1">(optional) Token</param>
        /// <param name="t2">(optional) Token</param>
        /// <param name="t3">(optional) Token</param>
        /// <returns>null, t1, t2, or concatenated token</returns>
        public static IFileSystemToken Concat(this IFileSystemToken t1, IFileSystemToken t2, IFileSystemToken t3)
        {
            StructList4<IFileSystemToken> tokens = new StructList4<IFileSystemToken>();
            if (t1 != null)
            {
                if (t1 is IFileSystemTokenEnumerable enumr) foreach (IFileSystemToken t in enumr) tokens.AddIfNew(t);
                else tokens.AddIfNew(t1);
            }
            if (t2 != null)
            {
                if (t2 is IFileSystemTokenEnumerable enumr) foreach (IFileSystemToken t in enumr) tokens.AddIfNew(t);
                else tokens.AddIfNew(t2);
            }
            if (t3 != null)
            {
                if (t3 is IFileSystemTokenEnumerable enumr) foreach (IFileSystemToken t in enumr) tokens.AddIfNew(t);
                else tokens.AddIfNew(t3);
            }
            if (tokens.Count == 0) return null;
            if (tokens.Count == 1) return tokens[0];
            return new FileSystemTokenList(tokens.ToArray());
        }

        /// <summary>
        /// Concatenates tokens non-recursively.
        /// Tokens may be null valued.
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="moreTokens">(optional) more tokens</param>
        /// <returns>null, t1, t2, or concatenated token</returns>
        public static IFileSystemToken Concat(this IFileSystemToken t1, params IFileSystemToken[] moreTokens)
        {
            if (moreTokens == null) return t1;
            if (t1 == null)
            {
                if (moreTokens.Length == 0) return null;
                if (moreTokens.Length == 1) return moreTokens[0];
                new FileSystemTokenList(moreTokens);
            }
            IFileSystemToken[] arr = new IFileSystemToken[moreTokens.Length + 1];
            arr[0] = t1;
            Array.Copy(moreTokens, 0, arr, 1, moreTokens.Length);
            return new FileSystemTokenList(arr);
        }
    }

    /// <summary><see cref="IFileSystemToken"/> operations.</summary>
    public class FileSystemOperationToken : IFileSystemOptionOperationFlatten/*, IFileSystemOptionOperationIntersection*/, IFileSystemOptionOperationUnion
    {
        /// <summary>The option type that this class has operations for.</summary>
        public Type OptionType => typeof(IFileSystemToken);
        /// <summary>Flatten to simpler instance.</summary>
        public IFileSystemOption Flatten(IFileSystemOption o)
        {
            StructList4<IFileSystemToken> tokens = new StructList4<IFileSystemToken>();
            if (o is IFileSystemToken t_)
                foreach (IFileSystemToken t in t_.ListTokens(true))
                    tokens.AddIfNew(t);
            if (tokens.Count == 0) return FileSystemTokenList.NoTokens;
            if (tokens.Count == 1) return tokens[0];
            return new FileSystemTokenList(tokens.ToArray());
        }

        // <summary>Intersection of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        //public IFileSystemOption Intersection(IFileSystemOption o1, IFileSystemOption o2) => o1 is IFileSystemOptionToken c1 && o2 is IFileSystemOptionToken c2 ? new FileSystemOptionToken() : throw new InvalidCastException($"{typeof(FileSystemOptionToken)} expected.");
        /// <summary>Union of <paramref name="o1"/> and <paramref name="o2"/>.</summary>
        public IFileSystemOption Union(IFileSystemOption o1, IFileSystemOption o2)
        {
            StructList4<IFileSystemToken> tokens = new StructList4<IFileSystemToken>();
            if (o1 is IFileSystemToken t1)
                foreach (IFileSystemToken t in t1.ListTokens(true))
                    tokens.AddIfNew(t);
            if (o2 is IFileSystemToken t2)
                foreach (IFileSystemToken t in t2.ListTokens(true))
                    tokens.AddIfNew(t);
            if (tokens.Count == 0) return FileSystemTokenList.NoTokens;
            if (tokens.Count == 1) return tokens[0];
            return new FileSystemTokenList(tokens.ToArray());
        }
    }

    /// <summary>
    /// Single token container.
    /// </summary>
    public class FileSystemToken : IFileSystemTokenObject, IFileSystemTokenProvider
    {
        static object[] emptyArray = new object[0];

        /// <summary>(optional) Token object</summary>
        public object Token { get; protected set; }
        /// <summary>(optional) Token type as which object is offered.</summary>
        public String Key { get; protected set; }
        /// <summary>(optional) Path patterns.</summary>
        public string[] Patterns { get; protected set; }
        /// <summary>(optional) Token object</summary>
        public object[] TokenAsArray { get; protected set; }

        /// <summary>
        /// Accept all patterns
        /// </summary>
        bool acceptAllPaths;

        /// <summary>
        /// Function that tests whether patterns match
        /// </summary>
        Func<string, Match> matcher;

        /// <summary>
        /// Create token container.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="key">(optional) The key <paramref name="token"/> is offered as. If null, then matches to every key criteria in TryGet</param>
        public FileSystemToken(object token, String key = null)
        {
            this.Token = token;
            this.Key = key;
            this.Patterns = null;
            this.TokenAsArray = token == null ? emptyArray : new object[] { token };
            acceptAllPaths = true;
        }

        /// <summary>
        /// Create token container.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="key">(optional) The key <paramref name="token"/> is offered as. If null, then matches to every key criteria in TryGet</param>
        /// <param name="patterns">(optional) path globa patterns to accept. If null, then accepts every path, which is equal to "**" pattern.</param>
        public FileSystemToken(object token, String key = null, params string[] patterns) : this(token, key)
        {
            this.Patterns = patterns;
            if (patterns == null)
            {
                acceptAllPaths = true;
            }
            else
            {
                PatternSet patternSet = new PatternSet();
                foreach (string pattern in patterns)
                    patternSet.AddGlobPattern(pattern);
                this.acceptAllPaths = false;
                this.matcher = patternSet.MatcherFunc;
            }
        }

        /// <summary>
        /// Query for first token object at path <paramref name="path"/> as type <paramref name="key"/>.
        /// </summary>
        /// <param name="path">(optional) path to query token at</param>
        /// <param name="key">(optional) key to query</param>
        /// <param name="token">array of tokens, or null if failed to find matching tokens</param>
        /// <returns>true if tokens were found for the parameters</returns>
        public bool TryGet(string path, String key, out object token)
        {
            // There is no token
            if (this.Token == null) { token = null; return false; }

            // Qualify path
            if (!acceptAllPaths)
            {
                // null path doesn't match to matcher
                if (path == null) { token = null; return false; }
                // run matcher
                if (!matcher(path).Success) { token = null; return false; }
            }

            // Qualify type
            if (this.Key != null)
            {
                // Type mismatch
                if (!key.Equals(this.Key)) { token = null; return false; }
            }

            token = this.Token;
            return true;
        }

        /// <summary>
        /// Query for all token objects at path <paramref name="path"/> as type <paramref name="key"/>.
        /// </summary>
        /// <param name="path">(optional) path to query token at</param>
        /// <param name="key">(optional) key to query</param>
        /// <param name="tokens">array of tokens, or null if failed to find matching tokens</param>
        /// <returns>true if tokens were found for the parameters</returns>
        public bool TryGetAll(string path, string key, out object[] tokens)
        {
            // There is no token
            if (this.Token == null) { tokens = null; return false; }

            // Qualify path
            if (!acceptAllPaths)
            {
                // null path doesn't match to matcher
                if (path == null) { tokens = null; return false; }
                // run matcher
                if (!matcher(path).Success) { tokens = null; return false; }
            }

            // Qualify type
            if (this.Key != null)
            {
                // Type mismatch
                if (!key.Equals(this.Key)) { tokens = null; return false; }
            }

            tokens = TokenAsArray;
            return true;
        }

        /// <summary>Print info</summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (Token != null) sb.Append(Token.ToString());
            if (Key != null || Patterns != null)
            {
                sb.Append("FileSystemToken(");
                sb.Append(Token?.ToString());
                if (Key != null) { sb.Append(", Key="); sb.Append(Key); }
                if (Patterns != null)
                {
                    if (Key != null) sb.Append(", ");
                    if (Patterns.Length == 1) { sb.Append("Patters="); sb.Append(Patterns[0]); }
                    else
                    {
                        sb.Append("Patterns=[");
                        for (int i = 0; i < Patterns.Length - 1; i++)
                        {
                            if (i > 0) sb.Append(", ");
                            sb.Append(Patterns[i]);
                        }
                        sb.Append("]");
                    }
                }
                sb.Append(')');
            }
            return sb.ToString();
        }

    }

    /// <summary>
    /// Readonly list of tokens.
    /// </summary>
    public class FileSystemTokenList : IFileSystemTokenEnumerable, IFileSystemTokenProvider, IReadOnlyList<IFileSystemToken>
    {
        static object[] emptyArray = new object[0];
        static IFileSystemToken noTokens = new FileSystemTokenList();

        /// <summary>Singleton instance that contains 0 tokens.</summary>
        public static IFileSystemToken NoTokens => noTokens;

        /// <summary>Token count</summary>
        public int Count => tokens.Length;

        /// <summary>Get token at <paramref name="index"/>.</summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public IFileSystemToken this[int index] => tokens[index];

        /// <summary>Array of tokens, nulls are removed</summary>
        protected IFileSystemToken[] tokens;

        /// <summary>
        /// Create a token that combins a pair of tokens.
        /// Removes null values.
        /// </summary>
        /// <param name="token1"></param>
        /// <param name="token2"></param>
        public FileSystemTokenList(IFileSystemToken token1, IFileSystemToken token2)
        {
            StructList2<IFileSystemToken> list = new StructList2<IFileSystemToken>();
            if (token1 != null) list.Add(token1);
            if (token2 != null) list.Add(token2);
            this.tokens = list.ToArray();
        }

        /// <summary>
        /// Create a token that reads <paramref name="tokens"/> into array of tokens.
        /// Removes null values.
        /// </summary>
        /// <param name="tokens"></param>
        public FileSystemTokenList(IEnumerable<IFileSystemToken> tokens)
        {
            StructList4<IFileSystemToken> list = new StructList4<IFileSystemToken>();
            foreach (var t in tokens) if (t != null) list.Add(t);
            this.tokens = list.ToArray();
        }

        /// <summary>
        /// Create token list.
        /// </summary>
        /// <param name="tokens"></param>
        public FileSystemTokenList(params IFileSystemToken[] tokens)
        {
            int c = 0;
            for (int i = 0; i < tokens.Length; i++)
                if (tokens[i] != null) c++;
            if (tokens.Length == c)
            {
                this.tokens = tokens;
            }
            else
            {
                this.tokens = new IFileSystemToken[c];
                int ix = 0;
                for (int i = 0; i < tokens.Length; i++)
                    if (tokens[i] != null) this.tokens[ix++] = tokens[i];
            }

        }

        /// <summary>
        /// Query for first token object at path <paramref name="path"/> as type <paramref name="key"/>.
        /// </summary>
        /// <param name="path">(optional) path to query token at</param>
        /// <param name="key">(optional) key to query with</param>
        /// <param name="token">array of tokens, or null if failed to find matching tokens</param>
        /// <returns>true if tokens were found</returns>
        public bool TryGet(string path, String key, out object token)
        {
            foreach (var t in this.tokens)
                if (t.TryGet(path, key, out token)) return true;
            token = null;
            return false;
        }

        /// <summary>
        /// Query for all token objects at path <paramref name="path"/> as type <paramref name="key"/>.
        /// </summary>
        /// <param name="path">(optional) path to query token at</param>
        /// <param name="key">(optional) key to query with</param>
        /// <param name="tokens">array of tokens, or null if failed to find matching tokens</param>
        /// <returns>true if tokens were found</returns>
        public bool TryGetAll(string path, String key, out object[] tokens)
        {
            StructList4<object[]> tokenArrays = new StructList4<object[]>();
            int c = 0;
            foreach (var t in this.tokens)
            {
                object[] array;
                if (t.TryGetAll(path, key, out array))
                {
                    c += array.Length;
                    tokenArrays.Add(array);
                }
            }
            if (tokenArrays.Count == 0) { tokens = emptyArray; return false; }
            if (tokenArrays.Count == 1) { tokens = tokenArrays[0]; return true; }
            object[] result = new object[c];
            int ix = 0;
            for (int i = 0; i < tokenArrays.Count; i++)
            {
                object[] arr = tokenArrays[i];
                Array.Copy(arr, 0, result, ix, arr.Length);
                ix += arr.Length;
            }
            tokens = result;
            return true;
        }

        /// <summary>Get enumerator</summary>
        public IEnumerator<IFileSystemToken> GetEnumerator()
            => ((IEnumerable<IFileSystemToken>)tokens).GetEnumerator();

        /// <summary>Get enumerator</summary>
        IEnumerator IEnumerable.GetEnumerator()
            => ((IEnumerable<IFileSystemToken>)tokens).GetEnumerator();

        /// <summary>Print info</summary>
        /// <returns></returns>
        public override string ToString()
            => "["+String.Join(", ", values: (object[]) this.tokens)+"]";

    }
}
