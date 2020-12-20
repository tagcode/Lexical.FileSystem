# DisposeList
<details>
  <summary><b>IDisposeList</b> contains lists of disposables. (<u>Click here</u>)</summary>
[!code-csharp[Snippet](../../../FileSystem.GitHub/Lexical.FileSystem.Abstractions/Utility/IDisposeList.cs#doc)]
</details>
<details>
  <summary><b>IBelatableDispose</b> is interface for objects whose dispose can be postponed. (<u>Click here</u>)</summary>
[!code-csharp[Snippet](../../../FileSystem.GitHub/Lexical.FileSystem.Abstractions/Utility/IBelatableDispose.cs#doc)]
</details>
<p/><p/>

Objects can be attached to be disposed along with the *IDisposeList* object. The attached object doesn't have to implement *IDisposable*, if it doesn't then the object is not added.
[!code-csharp[Snippet](Examples.cs#Snippet_10a)]

Delegate can be attached to be executed on dispose.
[!code-csharp[Snippet](Examples.cs#Snippet_10b)]

Dispose can be belated. This is useful when object is passed to other threads.
[!code-csharp[Snippet](Examples.cs#Snippet_10c)]
