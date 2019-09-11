# Introduction
Lexical.FileSystem is a virtual filesystem class libraries for .NET.

NuGet Packages:
* Lexical.FileSystem ([Website](http://lexical.fi/FileSystem/index.html), [Github](https://github.com/tagcode/Lexical.FileSystem), [Nuget](https://www.nuget.org/packages/Lexical.FileSystem/))
* Lexical.FileSystem.Abstractions ([Github](https://github.com/tagcode/Lexical.FileSystem/tree/master/Lexical.FileSystem.Abstractions), [Nuget](https://www.nuget.org/packages/Lexical.FileSystem.Abstractions/))

# FileSystem

**new FileSystem(<i>path</i>)** creates an instance to a path in local directory.

```csharp
string path = AppDomain.CurrentDomain.BaseDirectory;
IFileSystem filesystem = new FileSystem(path);
```

**new FileSystem("")** creates OS file-system root, which returns drive letters.

```csharp
IFileSystem filesystem = new FileSystem("");
```

```none
C:
D:
/
```

Singleton instance **FileSystem.OS** refers to the same OS root.

```csharp
IFileSystem filesystem = FileSystem.OS;
```

Files can be browsed.

```csharp
foreach (var entry in filesystem.Browse(""))
    Console.WriteLine(entry.Path);
```

Files can be opened for reading.

```csharp
using (Stream s = filesystem.Open("file.txt", FileMode.Open, FileAccess.Read, FileShare.Read))
{
    Console.WriteLine(s.Length);
}
```

And for for writing.

```csharp
using (Stream s = filesystem.Open("somefile.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
{
    s.WriteByte(32);
}
```

Files and directories can be observed for changes.

```csharp
IObserver<IFileSystemEvent> observer = new Observer();
using (IDisposable handle = filesystem.Observe("**", observer))
{
}
```

Directories can be created.

```csharp
filesystem.CreateDirectory("dir");
```

Directories can be deleted.

```csharp
filesystem.Delete("dir", recursive: true);
```

Files and directories can be renamed and moved.

```csharp
filesystem.CreateDirectory("dir");
filesystem.Move("dir", "new-name");
```

If FileSystem is constructed with relative drive letter "C:", then the instance refers to the absolute path at time of the construction.
If working directory is modified later on, the FileSystem instance is not affected.

```csharp
IFileSystem filesystem = new FileSystem("c:");
foreach (var entry in filesystem.Browse(""))
    Console.WriteLine(entry.Path);
```
