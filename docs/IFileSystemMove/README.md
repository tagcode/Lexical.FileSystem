### IFileSystemMove
<details>
  <summary><b>IFileSystemMove</b> is interface for moving and renaming files. (<u>Click here</u>)</summary>

```csharp
/// <summary>
/// File system that can move/rename files and directories.
/// </summary>
public interface IFileSystemMove : IFileSystem, IMoveOption
{
    /// <summary>
    /// Move/rename a file or directory. 
    /// 
    /// If <paramref name="srcPath"/> and <paramref name="dstPath"/> refers to a directory, then the path names 
    /// should end with directory separator character '/'.
    /// </summary>
    /// <param name="srcPath">old path of a file or directory</param>
    /// <param name="dstPath">new path of a file or directory</param>
    /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
    /// <exception cref="FileNotFoundException">The specified <paramref name="srcPath"/> is invalid.</exception>
    /// <exception cref="IOException">On unexpected IO error</exception>
    /// <exception cref="SecurityException">If caller did not have permission</exception>
    /// <exception cref="ArgumentNullException">path is null</exception>
    /// <exception cref="ArgumentException">path is an empty string (""), contains only white space, or contains one or more invalid characters</exception>
    /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support renaming/moving files</exception>
    /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
    /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
    /// <exception cref="InvalidOperationException">path refers to non-file device, or an entry already exists at <paramref name="dstPath"/></exception>
    /// <exception cref="ObjectDisposedException"/>
    void Move(string srcPath, string dstPath, IOption option = null);
}
```
</details>
<details>
  <summary><b>IFileSystemMoveAsync</b> is async interface for moving and renaming files. (<u>Click here</u>)</summary>

```csharp
/// <summary>
/// File system that can move/rename files and directories.
/// </summary>
public interface IFileSystemMoveAsync : IFileSystem, IMoveOption
{
    /// <summary>
    /// Move/rename a file or directory. 
    /// 
    /// If <paramref name="srcPath"/> and <paramref name="dstPath"/> refers to a directory, then the path names 
    /// should end with directory separator character '/'.
    /// </summary>
    /// <param name="srcPath">old path of a file or directory</param>
    /// <param name="dstPath">new path of a file or directory</param>
    /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
    /// <exception cref="FileNotFoundException">The specified <paramref name="srcPath"/> is invalid.</exception>
    /// <exception cref="IOException">On unexpected IO error</exception>
    /// <exception cref="SecurityException">If caller did not have permission</exception>
    /// <exception cref="ArgumentNullException">path is null</exception>
    /// <exception cref="ArgumentException">path is an empty string (""), contains only white space, or contains one or more invalid characters</exception>
    /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support renaming/moving files</exception>
    /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
    /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
    /// <exception cref="InvalidOperationException">path refers to non-file device, or an entry already exists at <paramref name="dstPath"/></exception>
    /// <exception cref="ObjectDisposedException"/>
    Task MoveAsync(string srcPath, string dstPath, IOption option = null);
}
```
</details>
<p/><p/>

<i>IFileSystem</i>**.Move()** can move and rename files and directories.

```csharp
filesystem.Move( "dir", "new-name");
```
