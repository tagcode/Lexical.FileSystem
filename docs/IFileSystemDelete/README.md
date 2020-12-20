### IFileSystemDelete
<details>
  <summary><b>IFileSystemDelete</b> is interface for deleting files. (<u>Click here</u>)</summary>

```csharp
/// <summary>
/// File system that can delete files and directories.
/// </summary>
public interface IFileSystemDelete : IFileSystem, IDeleteOption
{
    /// <summary>
    /// Delete a file or directory.
    /// 
    /// If <paramref name="path"/> is directory, then it should end with directory separator character '/', for example "dir/".
    /// 
    /// If <paramref name="recurse"/> is false and <paramref name="path"/> is a directory that is not empty, then <see cref="IOException"/> is thrown.
    /// If <paramref name="recurse"/> is true, then any file or directory in <paramref name="path"/> is deleted as well.
    /// </summary>
    /// <param name="path">path to a file or directory</param>
    /// <param name="recurse">if path refers to directory, recurse into sub directories</param>
    /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
    /// <exception cref="FileNotFoundException">The specified path is invalid.</exception>
    /// <exception cref="IOException">On unexpected IO error, or if <paramref name="path"/> refered to a directory that wasn't empty and <paramref name="recurse"/> is false, or trying to delete root when not allowed</exception>
    /// <exception cref="SecurityException">If caller did not have permission</exception>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> contains invalid characters</exception>
    /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support deleting files</exception>
    /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
    /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="path"/> refers to non-file device</exception>
    /// <exception cref="ObjectDisposedException"/>
    void Delete(string path, bool recurse = false, IOption option = null);
}
```
</details>
<details>
  <summary><b>IFileSystemDeleteAsync</b> is async interface for deleting files. (<u>Click here</u>)</summary>

```csharp
/// <summary>
/// File system that can delete files and directories.
/// </summary>
public interface IFileSystemDeleteAsync : IFileSystem, IDeleteOption
{
    /// <summary>
    /// Delete a file or directory.
    /// 
    /// If <paramref name="path"/> is directory, then it should end with directory separator character '/', for example "dir/".
    /// 
    /// If <paramref name="recurse"/> is false and <paramref name="path"/> is a directory that is not empty, then <see cref="IOException"/> is thrown.
    /// If <paramref name="recurse"/> is true, then any file or directory in <paramref name="path"/> is deleted as well.
    /// </summary>
    /// <param name="path">path to a file or directory</param>
    /// <param name="recurse">if path refers to directory, recurse into sub directories</param>
    /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
    /// <exception cref="FileNotFoundException">The specified path is invalid.</exception>
    /// <exception cref="IOException">On unexpected IO error, or if <paramref name="path"/> refered to a directory that wasn't empty and <paramref name="recurse"/> is false, or trying to delete root when not allowed</exception>
    /// <exception cref="SecurityException">If caller did not have permission</exception>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> contains invalid characters</exception>
    /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support deleting files</exception>
    /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
    /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="path"/> refers to non-file device</exception>
    /// <exception cref="ObjectDisposedException"/>
    Task DeleteAsync(string path, bool recurse = false, IOption option = null);
}    
```
</details>
<p/><p/>

<i>IFileSystem</i>**.Delete()** deletes a file.

```csharp
filesystem.Delete("file.txt");
```
