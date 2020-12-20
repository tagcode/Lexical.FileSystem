### IFileSystemCreateDirectory
<details>
  <summary><b>IFileSystemCreateDirectory</b> is interface for creating directories. (<u>Click here</u>)</summary>

```csharp
/// <summary>
/// File system that can create directories.
/// </summary>
public interface IFileSystemCreateDirectory : IFileSystem, ICreateDirectoryOption
{
    /// <summary>
    /// Create a directory, or multiple cascading directories.
    /// 
    /// If directory at <paramref name="path"/> already exists, then returns without exception.
    /// <paramref name="path"/> should end with directory separator character '/'.
    /// </summary>
    /// <param name="path">Relative path to file. Directory separator is "/". The root is without preceding slash "", e.g. "dir/dir2"</param>
    /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
    /// <returns>true if directory exists after the method, false if directory doesn't exist</returns>
    /// <exception cref="DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
    /// <exception cref="IOException">On unexpected IO error</exception>
    /// <exception cref="SecurityException">If caller did not have permission</exception>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is an empty string (""), contains only white space, or contains one or more invalid characters</exception>
    /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support create directory</exception>
    /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
    /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
    /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
    /// <exception cref="ObjectDisposedException"/>
    void CreateDirectory(string path, IOption option = null);
}
```
</details>
<details>
  <summary><b>IFileSystemCreateDirectoryAsync</b> is async interface for creating directories. (<u>Click here</u>)</summary>

```csharp
/// <summary>
/// File system that can create directories.
/// </summary>
public interface IFileSystemCreateDirectoryAsync : IFileSystem, ICreateDirectoryOption
{
    /// <summary>
    /// Create a directory, or multiple cascading directories.
    /// 
    /// If directory at <paramref name="path"/> already exists, then returns without exception.
    /// <paramref name="path"/> should end with directory separator character '/'.
    /// </summary>
    /// <param name="path">Relative path to file. Directory separator is "/". The root is without preceding slash "", e.g. "dir/dir2"</param>
    /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
    /// <returns>true if directory exists after the method, false if directory doesn't exist</returns>
    /// <exception cref="DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
    /// <exception cref="IOException">On unexpected IO error</exception>
    /// <exception cref="SecurityException">If caller did not have permission</exception>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is an empty string (""), contains only white space, or contains one or more invalid characters</exception>
    /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support create directory</exception>
    /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
    /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
    /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
    /// <exception cref="ObjectDisposedException"/>
    Task CreateDirectoryAsync(string path, IOption option = null);
}
```
</details>
<p/><p/>

<i>IFileSystem</i>**.CreateDirectory()** makes a directory.

```csharp
filesystem.CreateDirectory("dir/");
```
