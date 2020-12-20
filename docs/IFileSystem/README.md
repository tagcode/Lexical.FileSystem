### IFileSystem
<details>
  <summary><b>IFileSystem</b> is the root interface for virtual file-system abstractions. (<u>Click here</u>)</summary>

```csharp
/// <summary>
/// Root interface for file system interfaces. 
/// 
/// See sub-interfaces:
/// <list type="bullet">
///     <item><see cref="IFileSystemOpen"/></item>
///     <item><see cref="IFileSystemCreateDirectory"/></item>
///     <item><see cref="IFileSystemBrowse"/></item>
///     <item><see cref="IFileSystemDelete"/></item>
///     <item><see cref="IFileSystemMove"/></item>
///     <item><see cref="IFileSystemObserve"/></item>
///     <item><see cref="IFileSystemMount"/></item>
///     <item><see cref="IFileSystemFileAttribute"/></item>
///     <item><see cref="IFileSystemDisposable"/></item>
/// </list>
/// </summary>
public interface IFileSystem : IOption
{
}
```
</details>
<p/><p/>

**IFileSystem** is an abstraction to access filesystem functionality.

```csharp
string path = AppDomain.CurrentDomain.BaseDirectory;
IFileSystem filesystem = new FileSystem(path);
```
 
**IFileSystem.Browse()** browses for files and directories in a path.

```csharp
foreach (var entry in filesystem.Browse(""))
    Console.WriteLine(entry.Path);
```

**IFileSystem.Exists()** tests whether file or directory exists in a path.

```csharp
bool exists = filesystem.Exists("dir/");
```

**IFileSystem.Open()** open files for reading.

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

**IFileSystem.Observe()** can observe directories and files for modifications.

```csharp
IObserver<IEvent> observer = new Observer();
using (IDisposable handle = filesystem.Observe("**", observer))
{
}
```

**IFileSystem.Move()** can move and rename files and directories.

```csharp
filesystem.Move( "dir/", "new-name/");
```

**IFileSystem.CreateDirectory()** makes a directory.

```csharp
filesystem.CreateDirectory("dir/");
```

**IFileSystem.Delete()** deletes a file.

```csharp
filesystem.Delete("file.txt");
```

And a directory.

```csharp
filesystem.Delete("dir/", recurse: true);
```

**IFileSystem** is very simple to implement.

```csharp
class ExampleFileSystem : IFileSystem, IFileSystemBrowse, IFileSystemOpen, IPathInfo
{
    public bool CanOpen => true;
    public bool CanRead => true;
    public bool CanWrite => true;
    public bool CanCreateFile => true;
    public bool CanBrowse => true;
    public bool CanGetEntry => true;
    public FileSystemCaseSensitivity CaseSensitivity => FileSystemCaseSensitivity.CaseSensitive;
    public bool EmptyDirectoryName => false;

    DirectoryEntry rootEntry;
    FileEntry fileEntry;

    public ExampleFileSystem()
    {
        DateTimeOffset time = DateTimeOffset.MinValue;
        rootEntry = new DirectoryEntry(this, "", "", time, time, null);
        fileEntry = new FileEntry(this, "example.txt", "example.txt", time, time, 11L, null);
    }

    public IDirectoryContent Browse(string path, IOption option = null)
    {
        // Browse root
        if (path == rootEntry.Path) return new DirectoryContent(this, path, new IEntry[] { fileEntry });
        // Not found
        return new DirectoryNotFound(this, path);
    }

    public IEntry GetEntry(string path, IOption option = null)
    {
        // Root entry
        if (path == rootEntry.Path) return rootEntry;
        // File entry
        if (path == fileEntry.Path) return fileEntry;
        // Entry not found
        return null;
    }

    public Stream Open(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare, IOption option = null)
    {
        if (path != fileEntry.Path) throw new FileNotFoundException(path);
        if (fileMode != FileMode.Open) throw new NotSupportedException();
        if (fileAccess != FileAccess.Read) throw new NotSupportedException();
        byte[] data = UTF8Encoding.UTF8.GetBytes("Hello World");
        return new MemoryStream(data);
    }
}
```

And to run.

```csharp
IFileSystem filesystem = new ExampleFileSystem();
Console.WriteLine(filesystem.Print());
```


