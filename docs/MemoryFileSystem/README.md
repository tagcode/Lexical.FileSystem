# MemoryFileSystem

**MemoryFileSystem** is a memory based filesystem.

```csharp
IFileSystem filesystem = new MemoryFileSystem();
```

Files are based on blocks. Maximum number of blocks is 2^31-1. The <i>blockSize</i> can be set in constructor. The default blocksize is 1024. 

```csharp
IFileSystem filesystem = new MemoryFileSystem(blockSize: 4096);
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

And for writing.

```csharp
using (Stream s = filesystem.Open("file.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
{
    s.WriteByte(32);
}
```

Files and directories can be observed for changes.

```csharp
IObserver<IEvent> observer = new Observer();
using (IDisposable handle = filesystem.Observe("**", observer))
{
}
```

Directories can be created.

```csharp
filesystem.CreateDirectory("dir/");
```

Directories can be created recursively. 

```csharp
filesystem.CreateDirectory("dir1/dir2/dir3/");
filesystem.PrintTo(Console.Out);
```

The root is "".
<pre style="line-height:1.2;">
""
└──"dir1"
   └──"dir2"
      └──"dir3"
</pre>

*MemoryFileSystem* can create empty directory names. For example, a slash '/' at the start of a path refers to an empty directory right under the root.

```csharp
filesystem.CreateDirectory("/tmp/dir/");
```

<pre style="line-height:1.2;">
""
└──""
   └──"tmp"
      └──"dir"
</pre>

Path "file://" refers to three directories; the root, "file:" and a empty-named directory between two slashes "//".

```csharp
filesystem.CreateDirectory("file://");
```

<pre style="line-height:1.2;">
""
└──"file:"
   └──""
</pre>

Directories can be deleted.

```csharp
filesystem.Delete("dir/", recurse: true);
```

Files and directories can be renamed and moved.

```csharp
filesystem.CreateDirectory("dir/");
filesystem.Move("dir/", "new-name/");
```


# Disposing

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

**.BelateDispose()** creates a handle that postpones dispose on *.Dispose()*. Actual dispose proceeds once *.Dispose()* is called and
all belate handles are disposed. This can be used for passing the *IFileSystem* to worker threads. 

```csharp
MemoryFileSystem filesystem = new MemoryFileSystem();
filesystem.CreateDirectory("/tmp/dir/");

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

# Size Limit

Constructor **new MemoryFileSystem(<i>blockSize</i>, <i>maxSpace</i>)** creates size limited filesystem. Memory limitation applies to files only, not to directory structure.

```csharp
IFileSystem ms = new MemoryFileSystem(blockSize: 1024, maxSpace: 1L << 34);
```

Printing with **PrintTree.Format.DriveFreespace | PrintTree.Format.DriveSize** flags show drive size.

```csharp
IFileSystem ms = new MemoryFileSystem(blockSize: 1024, maxSpace: 1L << 34);
ms.CreateFile("file", new byte[1 << 30]);
ms.PrintTo(Console.Out, format: PrintTree.Format.AllWithName);
```

```none
"" [Freespace: 15G, Size: 1G/16G, Ram]
└── "file" [1073741824]
```

If filesystem runs out of space, it throws **FileSystemExceptionOutOfDiskSpace**.

```csharp
IFileSystem ms = new MemoryFileSystem(blockSize: 1024, maxSpace: 2048);
ms.CreateFile("file1", new byte[1024]);
ms.CreateFile("file2", new byte[1024]);

// throws FileSystemExceptionOutOfDiskSpace
ms.CreateFile("file3", new byte[1024]);
```

Available space can be shared between *MemoryFileSystem* instances with **IBlockPool**.

```csharp
IBlockPool pool = new BlockPool(blockSize: 1024, maxBlockCount: 3, maxRecycleQueue: 3);
IFileSystem ms1 = new MemoryFileSystem(pool);
IFileSystem ms2 = new MemoryFileSystem(pool);

// Reserve 2048 from shared pool
ms1.CreateFile("file1", new byte[2048]);

// Not enough for another 3072, throws FileSystemExceptionOutOfDiskSpace
ms2.CreateFile("file2", new byte[2048]);
```

Deleted file is returned back to pool once all open streams are closed.

```csharp
IBlockPool pool = new BlockPool(blockSize: 1024, maxBlockCount: 3, maxRecycleQueue: 3);
IFileSystem ms = new MemoryFileSystem(pool);
Stream s = ms.Open("file", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
s.Write(new byte[3072], 0, 3072);
ms.Delete("file");

Console.WriteLine(pool.BytesAvailable); // Prints 0
s.Dispose();
Console.WriteLine(pool.BytesAvailable); // Prints 3072
```
