### IToken
<details>
  <summary><b>IToken</b> is root interface for filesystem tokens. (<u>Click here</u>)</summary>

```csharp
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
```
</details>
<details>
  <summary><b>ITokenObject</b> is interface for a single token. (<u>Click here</u>)</summary>

```csharp
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
```
</details>
<details>
  <summary><b>ITokenProvider</b> is interface token provider. (<u>Click here</u>)</summary>

```csharp
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
```
</details>
<details>
  <summary><b>ITokenEnumerable</b> is interface for collection of tokens. (<u>Click here</u>)</summary>

```csharp
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
```
</details>
<p/><p/>

**new Token(<i>path, key, value</i>)** wraps a single object into a *IToken*.

```csharp
// Authorization header
AuthenticationHeaderValue authorization = new AuthenticationHeaderValue(
    "Basic", Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes("webuser:webpassword")));
// Tokenize
IToken token = new Token(authorization, typeof(AuthenticationHeaderValue).FullName);
```

The extension method **.Concat(<i>IToken</i>)** creates **TokenList** which wraps multiple tokens into one queryable and enumerable token.

```csharp
// Token 1
AuthenticationHeaderValue authorization = new AuthenticationHeaderValue(
    "Basic", Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes("webuser:webpassword")));
IToken token1 = new Token(authorization, typeof(AuthenticationHeaderValue).FullName);

// Token 2
List<KeyValuePair<string, IEnumerable<string>>> headers = new List<KeyValuePair<string, IEnumerable<string>>>();
headers.Add(new KeyValuePair<string, IEnumerable<string>>("User-Agent", new String[] { "MyUserAgent" }));
IToken token2 = new Token(headers, typeof(HttpHeaders).FullName);

// Token 3
CancellationTokenSource cancelSrc = new CancellationTokenSource();
IToken token3 = new Token(cancelSrc.Token, typeof(CancellationToken).FullName);

// Unify tokens
IToken token = token1.Concat(token2, token3);
```

Token can be queried for object with **.TryGetToken(<i>path, key, output</i>)**. 

```csharp
IEnumerable<KeyValuePair<string, IEnumerable<string>>> _headers;
token.TryGetToken(null, key: "System.Net.Http.Headers.HttpHeaders", out _headers);
```

If *key* parameter is omited, then key is derived from generic type T.

```csharp
AuthenticationHeaderValue _authorization;
token.TryGetToken<AuthenticationHeaderValue>(path: null, out _authorization);
```
