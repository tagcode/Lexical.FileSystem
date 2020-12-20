### IFileSystemOpen
<details>
  <summary><b>IFileSystemOpen</b> is interface for opening and creating files for reading and writing. (<u>Click here</u>)</summary>

```csharp
/// <summary>
/// File system that can open files for reading and writing. 
/// </summary>
public interface IFileSystemOpen : IFileSystem, IOpenOption
{
    /// <summary>
    /// Open a file for reading and/or writing. File can be created when <paramref name="fileMode"/> is <see cref="FileMode.Create"/> or <see cref="FileMode.CreateNew"/>.
    /// </summary>
    /// <param name="path">Relative path to file. Directory separator is "/". Root is without preceding "/", e.g. "dir/file.xml"</param>
    /// <param name="fileMode">determines whether to open or to create the file</param>
    /// <param name="fileAccess">how to access the file, read, write or read and write</param>
    /// <param name="fileShare">how the file will be shared by processes</param>
    /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
    /// <returns>open file stream</returns>
    /// <exception cref="IOException">On unexpected IO error</exception>
    /// <exception cref="SecurityException">If caller did not have permission</exception>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is an empty string (""), contains only white space, or contains one or more invalid characters</exception>
    /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support opening files</exception>
    /// <exception cref="FileNotFoundException">The file cannot be found, such as when mode is FileMode.Truncate or FileMode.Open, and and the file specified by path does not exist. The file must already exist in these modes.</exception>
    /// <exception cref="DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
    /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
    /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="fileMode"/>, <paramref name="fileAccess"/> or <paramref name="fileShare"/> contains an invalid value.</exception>
    /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
    /// <exception cref="ObjectDisposedException"/>
    /// <exception cref="FileSystemExceptionNoReadAccess">No read access</exception>
    /// <exception cref="FileSystemExceptionNoWriteAccess">No write access</exception>
    Stream Open(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare, IOption option = null);
}
```
</details>
<details>
  <summary><b>IFileSystemOpenAsync</b> is async interface for opening and creating files for reading and writing. (<u>Click here</u>)</summary>

```csharp
/// <summary>
/// File system that can open files for reading and writing. 
/// </summary>
public interface IFileSystemOpenAsync : IFileSystem, IOpenOption
{
    /// <summary>
    /// Open a file for reading and/or writing. File can be created when <paramref name="fileMode"/> is <see cref="FileMode.Create"/> or <see cref="FileMode.CreateNew"/>.
    /// </summary>
    /// <param name="path">Relative path to file. Directory separator is "/". Root is without preceding "/", e.g. "dir/file.xml"</param>
    /// <param name="fileMode">determines whether to open or to create the file</param>
    /// <param name="fileAccess">how to access the file, read, write or read and write</param>
    /// <param name="fileShare">how the file will be shared by processes</param>
    /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
    /// <returns>open file stream</returns>
    /// <exception cref="IOException">On unexpected IO error</exception>
    /// <exception cref="SecurityException">If caller did not have permission</exception>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is an empty string (""), contains only white space, or contains one or more invalid characters</exception>
    /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support opening files</exception>
    /// <exception cref="FileNotFoundException">The file cannot be found, such as when mode is FileMode.Truncate or FileMode.Open, and and the file specified by path does not exist. The file must already exist in these modes.</exception>
    /// <exception cref="DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
    /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
    /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="fileMode"/>, <paramref name="fileAccess"/> or <paramref name="fileShare"/> contains an invalid value.</exception>
    /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
    /// <exception cref="ObjectDisposedException"/>
    /// <exception cref="FileSystemExceptionNoReadAccess">No read access</exception>
    /// <exception cref="FileSystemExceptionNoWriteAccess">No write access</exception>
    Task<Stream> OpenAsync(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare, IOption option = null);
}
```
</details>
<p/><p/>

<i>IFileSystem</i>**.Open()** open files for reading.

```csharp
using (Stream s = filesystem.Open("file.txt", FileMode.Open, FileAccess.Read, FileShare.Read))
{
    Console.WriteLine(s.Length);
}
```

And for writing.

```csharp
using (Stream s = filesystem.Open("somefile.txt", 
       FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
{
    s.WriteByte(32);
}
```

