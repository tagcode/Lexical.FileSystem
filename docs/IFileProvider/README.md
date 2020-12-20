# IFileProvider 

There are decorating adapters to and from **IFileProvider** instances.

To use *IFileProvider* decorations, the calling assembly must import the **Microsoft.Extensions.FileProviders.Abstractions** assembly.

## To IFileSystem
The extension method <i>IFileProvider</i><b>.ToFileSystem()</b> adapts *IFileProvider* into *IFileSystem*.

```csharp
IFileSystem fs = new PhysicalFileProvider(@"C:\Users").ToFileSystem();
```

Parameters <b>.ToFileSystem(*bool canBrowse, bool canObserve, bool canOpen*)</b> can be used for limiting the capabilities of the adapted *IFileSystem*.

```csharp
IFileProvider fp = new PhysicalFileProvider(@"C:\");
IFileSystem fs = fp.ToFileSystem(
    canBrowse: true,
    canObserve: true,
    canOpen: true);
```

**.AddDisposable(<i>object</i>)** attaches a disposable to be disposed along with the *IFileSystem* adapter.

```csharp
IFileProvider fp = new PhysicalFileProvider(@"C:\Users");
IFileSystemDisposable filesystem = fp.ToFileSystem().AddDisposable(fp);
```

**.AddDisposeAction()** attaches a delegate to be ran at dispose. It can be used for disposing the source *IFileProvider*.

```csharp
IFileSystemDisposable filesystem = new PhysicalFileProvider(@"C:\Users")
    .ToFileSystem()
    .AddDisposeAction(fs => fs.FileProviderDisposable?.Dispose());
```

**.BelateDispose()** creates a handle that postpones dispose on *.Dispose()*. Actual dispose will proceed once *.Dispose()* is called and
all belate handles are disposed. This can be used for passing the *IFileSystem* to a worker thread. 

```csharp
using (var fs = new PhysicalFileProvider(@"C:\Users")
    .ToFileSystem()
    .AddDisposeAction(f => f.FileProviderDisposable?.Dispose()))
{
    fs.Browse("");

    // Post pone dispose at end of using()
    IDisposable belateDisposeHandle = fs.BelateDispose();
    // Start concurrent work
    Task.Run(() =>
    {
        // Do work
        Thread.Sleep(100);
        fs.GetEntry("");

        // Release the belate dispose handle
        // FileSystem is actually disposed here
        // provided that the using block has exited
        // in the main thread.
        belateDisposeHandle.Dispose();
    });

    // using() exists here and starts the dispose fs
}
```

The adapted *IFileSystem* can be used as any filesystem that has Open(), Browse() and Observe() features.

```csharp
IFileSystem fs = new PhysicalFileProvider(@"C:\Users").ToFileSystem();
foreach (var line in fs.VisitTree(depth: 2))
    Console.WriteLine(line);
```

<pre style="line-height:1.2;">
""
├──"Public"
│  ├──"Shared Files"
│  ├──"Documents"
│  ├──"Downloads"
│  ├──"Music"
│  ├──"Pictures"
│  ├──"Roaming"
│  └──"Videos"
└──"user"
   ├──"Contacts"
   ├──"Desktop"
   ├──"Documents"
   ├──"Downloads"
   ├──"Favorites"
   ├──"Links"
   ├──"Music"
   ├──"OneDrive"
   ├──"Pictures"
   └──"Videos"
</pre>

**.Observe()** attaches a watcher to the source *IFileProvider* and adapts incoming events.

```csharp
IFileSystem fs = new PhysicalFileProvider(@"C:\Users").ToFileSystem();
IObserver<IEvent> observer = new Observer();
using (IDisposable handle = fs.Observe("**", observer))
{
}
```

> [!WARNING]
> Note that, observing a IFileProvider through IFileSystem adapter browses
> the subtree of the source IFileProvider and compares snapshots
> in order to produce change events. If observer uses "**" pattern, it will
> browse through the whole IFileProvider.

## To IFileProvider
<i>*IFileSystem*</i><b>.ToFileProvider()</b> adapts *IFileProvider* into *IFileSystem*.

```csharp
IFileProvider fp = FileSystem.OS.ToFileProvider();
```

**.AddDisposable(<i>object</i>)** attaches a disposable to be disposed along with the *IFileProvider* adapter.

```csharp
IFileSystem fs = new FileSystem("");
IFileProviderDisposable fp = fs.ToFileProvider().AddDisposable(fs);
```

**.AddDisposeAction()** attaches a delegate to be ran at dispose. It can be used for disposing the source *IFileSystem*.

```csharp
IFileProviderDisposable fp = new FileSystem("")
    .ToFileProvider()
    .AddDisposeAction(fs => fs.FileSystemDisposable?.Dispose());
```

**.BelateDispose()** creates a handle that postpones dispose on *.Dispose()*. Actual dispose will proceed once *.Dispose()* is called and
all belate handles are disposed. This can be used for passing the *IFileProvider* to a worker thread. 

```csharp
using (var fp = new FileSystem("").ToFileProvider()
        .AddDisposeAction(fs => fs.FileSystemDisposable?.Dispose()))
{
    fp.GetDirectoryContents("");

    // Post pone dispose at end of using()
    IDisposable belateDisposeHandle = fp.BelateDispose();
    // Start concurrent work
    Task.Run(() =>
    {
        // Do work
        Thread.Sleep(100);
        fp.GetDirectoryContents("");

        // Release the belate dispose handle
        // FileSystem is actually disposed here
        // provided that the using block has exited
        // in the main thread.
        belateDisposeHandle.Dispose();
    });

    // using() exists here and starts the dispose fs
}
```

The adapted *IFileProvider* can be used as any fileprovider that can *GetDirectoryContents()*, *GetFileInfo()*, and *Watch()*.

```csharp
IFileProvider fp = FileSystem.OS.ToFileProvider();
foreach (var fi in fp.GetDirectoryContents(""))
    Console.WriteLine(fi.Name);
```

<pre style="line-height:1.2;">
C:
D:
E:
</pre>

**.Watch()** attaches a watcher.

```csharp
IChangeToken token = new FileSystem(@"c:").ToFileProvider().Watch("**");
token.RegisterChangeCallback(o => Console.WriteLine("Changed"), null);
```


