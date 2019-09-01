# Introduction
Lexical.FileSystem is a virtual filesystem class libraries for .NET.

NuGet Packages:
* Lexical.FileSystem.Abstractions
* Lexical.FileSystem

**Links**
* [Website](http://lexical.fi/FileSystem/docs/index.html)
* [Github](https://github.com/tagcode/Lexical.FileSystem)
* [Nuget](https://www.nuget.org/packages/Lexical.FileSystem/)

# FileSystem
FileSystem accesses a directory in local file-system.

```csharp
IFileSystem filesystem = new FileSystem(AppDomain.CurrentDomain.BaseDirectory);
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
IObserver<FileSystemEntryEvent> observer = new Observer();
using (IDisposable handle = filesystem.Observe("", observer))
{
}
```

<details>
  <summary><b>Observer.cs</b> above. (<u>Click here</u>)</summary>

```csharp

```
</details>

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

# FileProviderSystem
FileProviderSystem is adapts IFileProvider into IFileSystem.

```csharp
IFileProvider fp = new PhysicalFileProvider(AppDomain.CurrentDomain.BaseDirectory);
IFileSystem filesystem = new FileProviderSystem(fp).AddDisposable(fp);
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

Files and directories can be observed for changes.

```csharp
IObserver<FileSystemEntryEvent> observer = new Observer();
using (IDisposable handle = filesystem.Observe("", observer))
{                    
}
```

<details>
  <summary><b>Observer</b> of the example above. (<u>Click here</u>)</summary>

```csharp

```
</details>

# EmbeddedFileSystem
EmbeddedFileSystem is a file system to the embedded resources of an assembly.

```csharp
IFileSystem filesystem = new EmbeddedFileSystem(typeof(Program).Assembly);
```

Embedded resources can be browsed.

```csharp
foreach (var entry in filesystem.Browse(""))
    Console.WriteLine(entry.Path);
```

Embedded resources can be read.

```csharp
using(Stream s = filesystem.Open("docs.example-file.txt", FileMode.Open, FileAccess.Read, FileShare.Read))
{
    Console.WriteLine(s.Length);
}
```

# FileSystemComposition
FileSystemComposition is a composition of multiple IFileSystem instances.

```csharp
IFileSystem filesystem1 = new FileSystem("c:\\");
IFileProvider fp = new PhysicalFileProvider(AppDomain.CurrentDomain.BaseDirectory);
IFileSystem filesystem2 = new FileProviderSystem(fp).AddDisposable(fp);
IFileSystem filesystem3 = new EmbeddedFileSystem(typeof(Program).Assembly);

IFileSystem composition = new FileSystemComposition(filesystem1, filesystem2, filesystem3)
    .AddDisposable(filesystem1)
    .AddDisposable(filesystem2)
    .AddDisposable(filesystem3);
```

Embedded resources can be browsed.

```csharp
foreach (var entry in composition.Browse(""))
    Console.WriteLine(entry.Path);
```

Embedded resources can be read.

```csharp
using (Stream s = composition.Open("docs.example-file.txt", FileMode.Open, FileAccess.Read, FileShare.Read))
{
    Console.WriteLine(s.Length);
}
```


# Utils
FileScanner scans file-system tree structure for files that match configured criteria.

```csharp
IFileSystem filesystem = new FileSystem(@"C:\");

// Scan every file
foreach (var entry in new FileScanner(filesystem).AddWildcard("*"))
{
    Console.WriteLine(entry);
}
```

