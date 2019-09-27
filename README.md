# Introduction
Lexical.FileSystem is a virtual filesystem class libraries for .NET.

NuGet Packages:
* Lexical.FileSystem ([Website](http://lexical.fi/FileSystem/index.html), [Github](https://github.com/tagcode/Lexical.FileSystem), [Nuget](https://www.nuget.org/packages/Lexical.FileSystem/))
* Lexical.FileSystem.Abstractions ([Website](http://lexical.fi/docs/IFileSystem/index.html), [Github](https://github.com/tagcode/Lexical.FileSystem/tree/master/Lexical.FileSystem.Abstractions), [Nuget](https://www.nuget.org/packages/Lexical.FileSystem.Abstractions/))
# FileSystem

**new FileSystem(<i>path</i>)** creates an instance to a path in local directory. "" path refers to operating system root.

```csharp
IFileSystem filesystem = new FileSystem(path: "");
```

*FileSystem* can be browsed.

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
using (IDisposable handle = filesystem.Observe("c:/**", observer))
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

Singleton instance **FileSystem.OS** refers to a filesystem at the OS root.

```csharp
IFileSystem filesystem = FileSystem.OS;
```

Extension method **.VisitTree()** visits filesystem. On root path "" *FileSystem.OS* returns drive letters.

```csharp
foreach (var line in FileSystem.OS.VisitTree(depth: 1))
    Console.WriteLine(line);
```

```none
""
├──"C:"
└──"D:"
```

On linux it returns slash '/' root.

```csharp
foreach (var line in FileSystem.OS.VisitTree(depth: 3))
    Console.WriteLine(line.ToString(PrintTree.Format.DefaultPath));
```

```none

└──/
   ├──/bin
   ├──/boot
   ├──/dev
   ├──/etc
   ├──/lib
   ├──/media
   ├──/mnt
   ├──/root
   ├──/sys
   ├──/usr
   └──/var
```


**FileSystem.ApplicationRoot** refers to the application's root directory.

```csharp
IFileSystem filesystem = FileSystem.ApplicationRoot;
foreach (var line in filesystem.VisitTree(depth: 3))
    Console.WriteLine(line);
```

**FileSystem.Tmp** refers to the running user's temp directory.

```csharp
IFileSystem filesystem = FileSystem.Tmp;
foreach (var line in filesystem.VisitTree(depth: 1))
    Console.WriteLine(line);
```

Disposable objects can be attached to be disposed along with *FileSystem*.

```csharp
// Init
object obj = new ReaderWriterLockSlim();
IFileSystemDisposable filesystem = new FileSystem("").AddDisposable(obj);

// ... do work ...

// Dispose both
filesystem.Dispose();
```

Delegates can be attached to be executed at dispose of *FileSystem*.

```csharp
IFileSystemDisposable filesystem = new FileSystem("")
    .AddDisposeAction(f => Console.WriteLine("Disposed"));
```

**.BelateDispose()** creates a handle that postpones dispose on *.Dispose()*. Actual dispose will proceed once *.Dispose()* is called and
all belate handles are disposed. This can be used for passing the *IFileSystem* to a worker thread. 

```csharp
FileSystem filesystem = new FileSystem("");
filesystem.Browse("");

// Postpone dispose
IDisposable belateDisposeHandle = filesystem.BelateDispose();
// Start concurrent work
Task.Run(() =>
{
    // Do work
    Thread.Sleep(1000);
    filesystem.GetEntry("");
    // Release belate handle. Disposes here or below, depending which thread runs last.
    belateDisposeHandle.Dispose();
});

// Start dispose, but postpone it until belatehandle is disposed in another thread.
filesystem.Dispose();
```
