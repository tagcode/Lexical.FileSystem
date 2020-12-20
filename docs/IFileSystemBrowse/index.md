### IFileSystemBrowse
<details>
 <summary><b>IFileSystemBrowse</b> is interface for browsing directories. (<u>Click here</u>)</summary>
[!code-csharp[Snippet](../../../FileSystem.GitHub/Lexical.FileSystem.Abstractions/IFileSystemBrowse.cs#IFileSystemBrowse)]
</details>
<details>
 <summary><b>IFileSystemBrowseAsync</b> is async interface for browsing directories. (<u>Click here</u>)</summary>
[!code-csharp[Snippet](../../../FileSystem.GitHub/Lexical.FileSystem.Abstractions/IFileSystemBrowse.cs#IFileSystemBrowseAsync)]
</details>
<details>
 <summary><b>IDirectoryContent</b> is interface for browse result. (<u>Click here</u>)</summary>
[!code-csharp[Snippet](../../../FileSystem.GitHub/Lexical.FileSystem.Abstractions/IFileSystemBrowse.cs#IDirectoryContent)]
</details>
<p/><p/>

<i>IFileSystem</i>**.Browse()** browses for files and directories in a path.
[!code-csharp[Snippet](Examples.cs#IFileSystemBrowse_1)]

<i>IFileSystem</i>**.Exists()** tests whether file or directory exists in a path.
[!code-csharp[Snippet](Examples.cs#IFileSystemBrowse_2)]
