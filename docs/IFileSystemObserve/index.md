### IFileSystemObserve
<details>
  <summary><b>IFileSystemObserve</b> is interface for observing directory and file changes. (<u>Click here</u>)</summary>
[!code-csharp[Snippet](../../../FileSystem.GitHub/Lexical.FileSystem.Abstractions/IFileSystemObserve.cs#IFileSystemObserve)]
</details>
<details>
  <summary><b>IFileSystemObserveAsync</b> is async interface for observing directory and file changes. (<u>Click here</u>)</summary>
[!code-csharp[Snippet](../../../FileSystem.GitHub/Lexical.FileSystem.Abstractions/IFileSystemObserve.cs#IFileSystemObserveAsync)]
</details>
<p/><p/>

<i>IFileSystem</i>**.Observe(<i>filter, observer, state, eventDispatcher</i>)** observe directories and files for modifications.
Filter uses glob pattern. 
[!code-csharp[Snippet](Examples.cs#IFileSystemObserve_1)]

Optionally *eventDispatcher* parameter determines how events are dispatched.
[!code-csharp[Snippet](Examples.cs#IFileSystemObserve_2)]

**EventTaskDispatcher** dispatches events with task factory.
[!code-csharp[Snippet](Examples.cs#IFileSystemObserve_3)]


*IEventDispatcher* singleton instances:

| Name                                  | Description                                                                                      |
|:--------------------------------------|:-------------------------------------------------------------------------------------------------|
| EventDispatcher.Instance    | Dispatches events in the API caller's thread.                                                    |
| EventTaskDispatcher.Instance| Dispatches events concurrently in a TaskFactory.                                                 |
