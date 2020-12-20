### IToken
<details>
  <summary><b>IToken</b> is root interface for filesystem tokens. (<u>Click here</u>)</summary>
[!code-csharp[Snippet](../../../FileSystem.GitHub/Lexical.FileSystem.Abstractions/IToken.cs#IToken)]
</details>
<details>
  <summary><b>ITokenObject</b> is interface for a single token. (<u>Click here</u>)</summary>
[!code-csharp[Snippet](../../../FileSystem.GitHub/Lexical.FileSystem.Abstractions/IToken.cs#ITokenObject)]
</details>
<details>
  <summary><b>ITokenProvider</b> is interface token provider. (<u>Click here</u>)</summary>
[!code-csharp[Snippet](../../../FileSystem.GitHub/Lexical.FileSystem.Abstractions/IToken.cs#ITokenProvider)]
</details>
<details>
  <summary><b>ITokenEnumerable</b> is interface for collection of tokens. (<u>Click here</u>)</summary>
[!code-csharp[Snippet](../../../FileSystem.GitHub/Lexical.FileSystem.Abstractions/IToken.cs#ITokenEnumerable)]
</details>
<p/><p/>

**new Token(<i>path, key, value</i>)** wraps a single object into a *IToken*.
[!code-csharp[Snippet](Examples.cs#Snippet_1)]

The extension method **.Concat(<i>IToken</i>)** creates **TokenList** which wraps multiple tokens into one queryable and enumerable token.
[!code-csharp[Snippet](Examples.cs#Snippet_2)]

Token can be queried for object with **.TryGetToken(<i>path, key, output</i>)**. 
[!code-csharp[Snippet](Examples.cs#Snippet_3)]

If *key* parameter is omited, then key is derived from generic type T.
[!code-csharp[Snippet](Examples.cs#Snippet_4)]
