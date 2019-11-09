// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           30.10.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Generic;

namespace Lexical.FileSystem
{
    // <IToken>
    /// <summary>
    /// Abstract token that can be passed to filesystem implementations.
    /// Token is typically a session, a security token such as credential
    /// 
    /// Token implementation must be immutable.
    /// 
    /// See more specific subinterfaces:
    /// <list type="bullet">
    ///     <item><see cref="ITokenObject"/></item>
    ///     <item><see cref="ITokenEnumerable"/></item>
    ///     <item><see cref="ITokenProvider"/></item>
    /// </list>
    /// </summary>
    [Operations(typeof(TokenOperations))]
    public interface IToken : IOption
    {
    }
    // </IToken>

    // <ITokenObject>
    /// <summary>
    /// A single token object.
    /// </summary>
    [Operations(typeof(TokenOperations))]
    public interface ITokenObject : IToken
    {
        /// <summary>
        /// (optional) Token object
        /// </summary>
        object TokenObject { get; }

        /// <summary>
        /// (optional) Key type to identify token as. This is typically <see cref="Type.FullName"/>.
        /// </summary>
        string Key { get; }

        /// <summary>
        /// (optional) Glob pattern filters for paths this token is offered to.
        /// If null, then this token is offered to every filesystem (same as "**").
        /// If empty array, then the token is not offered to any filesystem.
        /// </summary>
        string[] Patterns { get; }
    }
    // </ITokenObject>

    // <ITokenProvider>
    /// <summary>
    /// Queryable token.
    /// </summary>
    [Operations(typeof(TokenOperations))]
    public interface ITokenProvider : IToken
    {
        /// <summary>
        /// Query for first token object at path <paramref name="path"/> as type <paramref name="key"/>.
        /// </summary>
        /// <param name="path">(optional) path to query token at</param>
        /// <param name="key">(optional) key to query, typically <see cref="Type.FullName"/></param>
        /// <param name="token">array of tokens, or null if failed to find matching tokens</param>
        /// <returns>true if tokens were found for the parameters</returns>
        bool TryGetToken(string path, string key, out object token);

        /// <summary>
        /// Query for all token objects at path <paramref name="path"/> as type <paramref name="key"/>.
        /// </summary>
        /// <param name="path">(optional) path to query token at</param>
        /// <param name="key">(optional) key to query, typically <see cref="Type.FullName"/></param>
        /// <param name="tokens">array of tokens, or null if failed to find matching tokens</param>
        /// <returns>true if tokens were found for the parameters</returns>
        bool TryGetAllTokens(string path, string key, out object[] tokens);
    }
    // </ITokenProvider>

    // <ITokenEnumerable>
    /// <summary>
    /// Object that contains multiple tokens. 
    /// 
    /// If class that implements <see cref="ITokenEnumerable"/>, also implements <see cref="ITokenProvider"/>
    /// then it must provide only for the tokens that it can enumerate (either recursively or not).
    /// </summary>
    [Operations(typeof(TokenOperations))]
    public interface ITokenEnumerable : IToken, IEnumerable<IToken>
    {
    }
    // </ITokenEnumerable>
}
