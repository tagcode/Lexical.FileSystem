### IOption
<details>
  <summary><b>IOption</b> is root interface filesystem options. (<u>Click here</u>)</summary>
[!code-csharp[Snippet](../../../FileSystem.GitHub/Lexical.FileSystem.Abstractions/IOption.cs#IOption)]
</details>
<details>
  <summary><b>IOpenOption</b> is capabilities of <i>IFileSystemOpen</i> interface. (<u>Click here</u>)</summary>
[!code-csharp[Snippet](../../../FileSystem.GitHub/Lexical.FileSystem.Abstractions/IFileSystemOpen.cs#IOpenOption)]
</details>
<details>
  <summary><b>IObserveOption</b> is capabilities of <i>IFileSystemObserve</i> interface. (<u>Click here</u>)</summary>
[!code-csharp[Snippet](../../../FileSystem.GitHub/Lexical.FileSystem.Abstractions/IFileSystemObserve.cs#IObserveOption)]
</details>
<details>
  <summary><b>IMoveOption</b> is capabilities of <i>IFileSystemMove</i> interface. (<u>Click here</u>)</summary>
[!code-csharp[Snippet](../../../FileSystem.GitHub/Lexical.FileSystem.Abstractions/IFileSystemMove.cs#IMoveOption)]
</details>
<details>
  <summary><b>IBrowseOption</b> is capabilities of <i>IFileSystemBrowse</i> interface. (<u>Click here</u>)</summary>
[!code-csharp[Snippet](../../../FileSystem.GitHub/Lexical.FileSystem.Abstractions/IFileSystemBrowse.cs#IBrowseOption)]
</details>
<details>
  <summary><b>ICreateDirectoryOption</b> is capabilities of <i>IFileSystemCreateDirectory</i> interface. (<u>Click here</u>)</summary>
[!code-csharp[Snippet](../../../FileSystem.GitHub/Lexical.FileSystem.Abstractions/IFileSystemCreateDirectory.cs#ICreateDirectoryOption)]
</details>
<details>
  <summary><b>IDeleteOption</b> is capabilities of <i>IFileSystemDelete</i> interface. (<u>Click here</u>)</summary>
[!code-csharp[Snippet](../../../FileSystem.GitHub/Lexical.FileSystem.Abstractions/IFileSystemDelete.cs#IDeleteOption)]
</details>
<details>
  <summary><b>IMountOption</b> is capabilities of <i>IFileSystemMount</i> interface. (<u>Click here</u>)</summary>
[!code-csharp[Snippet](../../../FileSystem.GitHub/Lexical.FileSystem.Abstractions/IFileSystemMount.cs#IMountOption)]
</details>
<details>
  <summary><b>IFileAttributeOption</b> is capabilities of <i>IFileSystemFileAttribute</i> interface. (<u>Click here</u>)</summary>
[!code-csharp[Snippet](../../../FileSystem.GitHub/Lexical.FileSystem.Abstractions/IFileSystemFileAttribute.cs#IFileAttributeOption)]
</details>
<details>
  <summary><b>ISubPathOption</b> is interface for subpath option. (<u>Click here</u>)</summary>
[!code-csharp[Snippet](../../../FileSystem.GitHub/Lexical.FileSystem.Abstractions/IOption.cs#ISubPathOption)]
</details>
<details>
  <summary><b>IAutoMountOption</b> is interface for automatic mounting of package files. (<u>Click here</u>)</summary>
[!code-csharp[Snippet](../../../FileSystem.GitHub/Lexical.FileSystem.Abstractions/IFileSystemMount.cs#IAutoMountOption)]
</details>
<details>
  <summary><b>IPathInfo</b> is interface for filesystem's path info options. (<u>Click here</u>)</summary>
[!code-csharp[Snippet](../../../FileSystem.GitHub/Lexical.FileSystem.Abstractions/IOption.cs#IPathInfo)]
</details>
<details>
  <summary><b>IAdaptableOption</b> is interface for runtime option classes. (<u>Click here</u>)</summary>
[!code-csharp[Snippet](../../../FileSystem.GitHub/Lexical.FileSystem.Abstractions/IOption.cs#IAdaptableOption)]
</details>
<p/><p/>

<br/>
*FileSystemOption* singletons:

| Flag     | Description |
|:---------|:------------|
| <i>Option.</i>Join() | Takes first instance of each option. |
| <i>Option.</i>Union() | Take union of options. |
| <i>Option.</i>Intersection() | Take intersection of options. |
| <i>Option.</i>ReadOnly | Read-only operations allowed, deny modification and write operations |
| <i>Option.</i>NoOptions | No options |
| <i>Option.</i>Path(caseSensitivity, emptyDirectoryName) | Path options |
| <i>Option.</i>Observe | Observe is allowed. |
| <i>Option.</i>NoObserve | Observe is not allowed |
| <i>Option.</i>Open(canOpen, canRead, canWrite, canCreateFile) | Open options |
| <i>Option.</i>OpenReadWriteCreate | Open, Read, Write, Create |
| <i>Option.</i>OpenReadWrite | Open, Read, Write |
| <i>Option.</i>OpenRead | Open, Read |
| <i>Option.</i>NoOpen | No access |
| <i>Option.</i>Mount | Mount is allowed. |
| <i>Option.</i>NoMount | Mount is not allowed |
| <i>Option.</i>Move | Move and rename is allowed. |
| <i>Option.</i>NoMove | Move and rename not allowed.  |
| <i>Option.</i>Delete | Delete allowed. |
| <i>Option.</i>NoDelete | Delete not allowed. |
| <i>Option.</i>CreateDirectory | CreateDirectory allowed. |
| <i>Option.</i>NoCreateDirectory | CreateDirectory not allowed. |
| <i>Option.</i>Browse | Browse allowed. |
| <i>Option.</i>NoBrowse | Browse not allowed. |
| <i>Option.</i>SubPath(subpath) | Create option for sub-path. Used with decorator and VirtualFileSystem. |
| <i>Option.</i>NoSubPath | No mount path. |
