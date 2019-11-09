// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           30.10.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Generic;

namespace Lexical.FileSystem
{
    // <IFileSystemToken>
    /// <summary>
    /// Abstract token that can be passed to filesystem implementations.
    /// Token is typically a session, a security token such as credential
    /// 
    /// Token implementation must be immutable.
    /// 
    /// See more specific subinterfaces:
    /// <list type="bullet">
    ///     <item><see cref="IFileSystemTokenObject"/></item>
    ///     <item><see cref="IFileSystemTokenEnumerable"/></item>
    ///     <item><see cref="IFileSystemTokenProvider"/></item>
    /// </list>
    /// </summary>
    [Operations(typeof(FileSystemOperationToken))]
    public interface IFileSystemToken : IFileSystemOption
    {
    }
    // </IFileSystemToken>

    // <IFileSystemTokenObject>
    /// <summary>
    /// A single token object.
    /// </summary>
    [Operations(typeof(FileSystemOperationToken))]
    public interface IFileSystemTokenObject : IFileSystemToken
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
    // </IFileSystemTokenObject>

    // <IFileSystemTokenProvider>
    /// <summary>
    /// Queryable token.
    /// </summary>
    [Operations(typeof(FileSystemOperationToken))]
    public interface IFileSystemTokenProvider : IFileSystemToken
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
    // </IFileSystemTokenProvider>

    // <IFileSystemTokenEnumerable>
    /// <summary>
    /// Object that contains multiple tokens. 
    /// 
    /// If class that implements <see cref="IFileSystemTokenEnumerable"/>, also implements <see cref="IFileSystemTokenProvider"/>
    /// then it must provide only for the tokens that it can enumerate (either recursively or not).
    /// </summary>
    [Operations(typeof(FileSystemOperationToken))]
    public interface IFileSystemTokenEnumerable : IFileSystemToken, IEnumerable<IFileSystemToken>
    {
    }
    // </IFileSystemTokenEnumerable>
}
