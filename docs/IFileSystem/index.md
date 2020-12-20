### IFileSystem
<details>
  <summary><b>IFileSystem</b> is the root interface for virtual file-system abstractions. (<u>Click here</u>)</summary>
[!code-csharp[Snippet](../../../FileSystem.GitHub/Lexical.FileSystem.Abstractions/IFileSystem.cs#doc)]
</details>
<p/><p/>

**IFileSystem** is an abstraction to access filesystem functionality.
[!code-csharp[Snippet](Examples.cs#IFileSystem_1)]
 
**IFileSystem.Browse()** browses for files and directories in a path.
[!code-csharp[Snippet](Examples.cs#IFileSystemBrowse_1)]

**IFileSystem.Exists()** tests whether file or directory exists in a path.
[!code-csharp[Snippet](Examples.cs#IFileSystemBrowse_2)]

**IFileSystem.Open()** open files for reading.
[!code-csharp[Snippet](Examples.cs#IFileSystemOpen_1)]

And for writing.
[!code-csharp[Snippet](Examples.cs#IFileSystemOpen_2)]

**IFileSystem.Observe()** can observe directories and files for modifications.
[!code-csharp[Snippet](Examples.cs#IFileSystemObserve_1)]

**IFileSystem.Move()** can move and rename files and directories.
[!code-csharp[Snippet](Examples.cs#IFileSystemMove_1)]

**IFileSystem.CreateDirectory()** makes a directory.
[!code-csharp[Snippet](Examples.cs#IFileSystemCreateDirectory_1)]

**IFileSystem.Delete()** deletes a file.
[!code-csharp[Snippet](Examples.cs#IFileSystemDelete_1)]

And a directory.
[!code-csharp[Snippet](Examples.cs#IFileSystemDelete_2)]

**IFileSystem** is very simple to implement.
[!code-csharp[Snippet](ExampleFileSystem.cs#docs)]

And to run.
[!code-csharp[Snippet](Examples.cs#IFileSystem_ExampleRun)]


