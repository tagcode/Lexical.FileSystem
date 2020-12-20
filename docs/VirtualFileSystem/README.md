# VirtualFileSystem

**new VirtualFileSystem()** creates virtual filesystem. Other filesystems can be mounted as part of it.

```csharp
IFileSystem vfs = new VirtualFileSystem();
```

**.Mount(<i>path</i>, <i>filesystem</i>)** assigns a filesystem to a mountpoint.

```csharp
IFileSystem vfs = new VirtualFileSystem()
    .Mount("", FileSystem.OS);
```

File systems can be assigned to multiple points.

```csharp
IFileSystem urls = new VirtualFileSystem()
    .Mount("tmp/", FileSystem.Temp)
    .Mount("ram/", MemoryFileSystem.Instance);
```

File system can be assigned as a child of an earlier assignment. Child assignment has higher evaluation priority than parent. In the following example, "/tmp/" is evaluated from **MemoryFileSystem** first, and then concatenated with potential directory "/tmp/" from the **FileSystem.OS**.

```csharp
IFileSystem vfs = new VirtualFileSystem()
    .Mount("", FileSystem.OS)
    .Mount("/tmp/", new MemoryFileSystem());

vfs.Browse("/tmp/");
```

**.Unmount(<i>path</i>)** removes filesystem assignments.

```csharp
IFileSystem vfs = new VirtualFileSystem();
vfs.Mount("/tmp/", FileSystem.Temp);
vfs.Unmount("/tmp/");
```

Previously assigned filesystem can be replaced.

```csharp
IFileSystem vfs = new VirtualFileSystem();
vfs.Mount("/tmp/", FileSystem.Temp);
vfs.Mount("/tmp/", new MemoryFileSystem());
```

**.Mount(<i>path</i>, params <i>IFilesystem</i>, <i>IOption</i>)** assigns filesystem with mount option.

```csharp
IFileSystem vfs = new VirtualFileSystem();
vfs.Mount("/app/", FileSystem.Application, Option.ReadOnly);
```

Option such as *Option.SubPath()*.

```csharp
IFileSystem vfs = new VirtualFileSystem();
string appDir = AppDomain.CurrentDomain.BaseDirectory.Replace('\\', '/');
vfs.Mount("/app/", FileSystem.OS, Option.SubPath(appDir));
```

**.Mount(<i>path</i>, params <i>IFilesystem[]</i>)** assigns multiple filesystems into one mountpoint.

```csharp
IFileSystem vfs = new VirtualFileSystem();
IFileSystem overrides = new MemoryFileSystem();
overrides.CreateFile("important.dat", new byte[] { 12, 23, 45, 67, 89 });
vfs.Mount("/app/", overrides, FileSystem.Application);
```

**.Mount(<i>path</i>, params (<i>IFilesystem</i>, <i>IOption</i>)[])** assigns multiple filesystems with mount options.

```csharp
IFileSystem overrides = new MemoryFileSystem();
IFileSystem vfs = new VirtualFileSystem();

vfs.Mount("/app/", 
    (overrides, Option.ReadOnly), 
    (FileSystem.Application, Option.ReadOnly)
);
```

If virtual filesystem is assigned with *null* filesystem, then empty mountpoint is created. Mountpoint cannot be deleted with **.Delete()** method, only remounted or unmounted.
*null* assignment doesn't have any interface capabilities, such as *.Browse()*.

```csharp
IFileSystem vfs = new VirtualFileSystem();
vfs.Mount("/tmp/", filesystem: null);
```

<pre style="line-height:1.2;">
└──/
   └── /tmp/ NotSupportedException: Browse
</pre>

# Observing

Observer can be placed before and after mounting. If observer is placed before, then mounting will notify the observer with *ICreateEvent* event for all the added files.

```csharp
IFileSystem vfs = new VirtualFileSystem();
vfs.Observe("**", new PrintObserver());

IFileSystem ram = new MemoryFileSystem();
ram.CreateDirectory("/dir/");
ram.CreateFile("/dir/file.txt", new byte[] { 32, 65, 66 });

vfs.Mount("", ram);
```


```csharp
class PrintObserver : IObserver<IEvent>
{
    public void OnCompleted() => Console.WriteLine("OnCompleted");
    public void OnError(Exception error) => Console.WriteLine(error);
    public void OnNext(IEvent @event) => Console.WriteLine(@event);
}
```

```none
StartEvent(VirtualFileSystem, 19.10.2019 11.34.08 +00:00)
MountEvent(VirtualFileSystem, 19.10.2019 11.34.08 +00:00, , MemoryFileSystem, )
CreateEvent(VirtualFileSystem, 19.10.2019 11.34.08 +00:00, /dir/file.txt)
```

Observer filter can be an intersection of the mounted filesystem's contents.

```csharp
IFileSystem vfs = new VirtualFileSystem();
vfs.Observe("/dir/*.txt", new PrintObserver());

IFileSystem ram = new MemoryFileSystem();
ram.CreateDirectory("/dir/");
ram.CreateFile("/dir/file.txt", new byte[] { 32, 65, 66 });
ram.CreateFile("/dir/file.dat", new byte[] { 255, 255, 255 });

vfs.Mount("", ram);
```

```none
StartEvent(VirtualFileSystem, 19.10.2019 11.34.23 +00:00)
CreateEvent(VirtualFileSystem, 19.10.2019 11.34.23 +00:00, /dir/file.txt)
```

**.Unmount()** dispatches events of unmounted files as if they were deleted.

```csharp
vfs.Unmount("");
```

```none
Delete(VirtualFileSystem, 19.10.2019 11.34.39 +00:00, /dir/file.txt)
```

If filesystem is mounted with **Option.NoObserve**, then the assigned filesystem cannot be observed, and it won't dispatch events of added files on mount.

```csharp
vfs.Mount("", ram, Option.NoObserve);
```

Observer isn't closed by unmounting. It can be closed by disposing its handle.

```csharp
IDisposable observerHandle = vfs.Observe("**", new PrintObserver());
observerHandle.Dispose();
```

```none
OnCompleted
```

... or by disposing the virtual filesystem.

```csharp
VirtualFileSystem vfs = new VirtualFileSystem();
IDisposable observerHandle = vfs.Observe("**", new PrintObserver());
vfs.Dispose();
```

```none
OnCompleted
```

# Singleton

**VirtualFileSystem.Url** is a singleton instance that has the following filesystems mounted as urls.

```csharp
new VirtualFileSystem.NonDisposable()
    .Mount("file://", FileSystem.OS)                  // All files
    .Mount("tmp://", FileSystem.Temp)                 // Temp files
    .Mount("ram://", MemoryFileSystem.Instance)       // Application's internal ram drive
    .Mount("home://", FileSystem.Personal)            // User's home directory
    .Mount("document://", FileSystem.MyDocuments)     // User's documents
    .Mount("desktop://", FileSystem.Desktop)          // User's desktop
    .Mount("picture://", FileSystem.MyPictures)       // User's pictures
    .Mount("video://", FileSystem.MyVideos)           // User's videos
    .Mount("music://", FileSystem.MyMusic)            // User's music
    .Mount("config://", FileSystem.Config)            // User's cloud-sync program configuration (roaming data).
    .Mount("data://", FileSystem.Data)                // User's local program data.
    .Mount("program-data://", FileSystem.ProgramData) // Program data that is shared with every user.
    .Mount("application://", FileSystem.Application)  // Application's install directory
    .Mount("http://", HttpFileSystem.Instance, Option.SubPath("http://"))
    .Mount("https://", HttpFileSystem.Instance, Option.SubPath("https://"))
```

*VirtualFileSystem.Url* can be read, browsed and written to with its different url schemes.

```csharp
VirtualFileSystem.Url.PrintTo(Console.Out, "config://", 2, PrintTree.Format.DefaultPath);
VirtualFileSystem.Url.PrintTo(Console.Out, "data://", 1, PrintTree.Format.DefaultPath);
VirtualFileSystem.Url.PrintTo(Console.Out, "program-data://", 1, PrintTree.Format.DefaultPath);
VirtualFileSystem.Url.PrintTo(Console.Out, "home://", 1, PrintTree.Format.DefaultPath);
VirtualFileSystem.Url.PrintTo(Console.Out, "https://github.com/tagcode/Lexical.FileSystem/tree/master/");
```

Application configuration can be placed in "config://ApplicationName/config.ini".

```csharp
string config = "[Config]\nUser=ExampleUser\n";
VirtualFileSystem.Url.CreateDirectory("config://ApplicationName/");
VirtualFileSystem.Url.CreateFile("config://ApplicationName/config.ini", UTF8Encoding.UTF8.GetBytes(config));
```

Application's user specific local data can be placed in "data://ApplicationName/data.db".

```csharp
byte[] cacheData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
VirtualFileSystem.Url.CreateDirectory("data://ApplicationName/");
VirtualFileSystem.Url.CreateFile("data://ApplicationName/cache.db", cacheData);
```

Application's user documents can be placed in "document://ApplicationName/document".

```csharp
string saveGame = "[Save]\nLocation=12.32N 43.43W\n";
VirtualFileSystem.Url.CreateDirectory("document://ApplicationName/");
VirtualFileSystem.Url.CreateFile("document://ApplicationName/save1.txt", UTF8Encoding.UTF8.GetBytes(saveGame));
```

Program data that is shared with every user can be placed in "program-data://ApplicationName/datafile". These are typically modifiable files.

```csharp
byte[] programData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
VirtualFileSystem.Url.CreateDirectory("program-data://ApplicationName/");
VirtualFileSystem.Url.CreateFile("program-data://ApplicationName/index.db", programData);
```

Application's installed files and binaries are located at "application://". These are typically read-only files.

```csharp
VirtualFileSystem.Url.PrintTo(Console.Out, "application://", format: PrintTree.Format.DefaultPath);
```

# Disposing

Disposable objects can be attached to be disposed along with *VirtualFileSystem*.

```csharp
// Init
object obj = new ReaderWriterLockSlim();
IFileSystemDisposable vfs = new VirtualFileSystem().AddDisposable(obj);

// ... do work ...

// Dispose both
vfs.Dispose();
```

Delegates can be attached to be executed at dispose of *VirtualFileSystem*.

```csharp
IFileSystemDisposable vfs = new VirtualFileSystem()
    .AddDisposeAction(f => Console.WriteLine("Disposed"));
```

**.BelateDispose()** creates a handle that postpones dispose on *.Dispose()*. Actual dispose will proceed once *.Dispose()* is called and
all belate handles are disposed. This can be used for passing the *IFileSystem* to a worker thread. 

```csharp
VirtualFileSystem vfs = new VirtualFileSystem().Mount("", FileSystem.OS);
vfs.Browse("");

// Postpone dispose
IDisposable belateDisposeHandle = vfs.BelateDispose();
// Start concurrent work
Task.Run(() =>
{
    // Do work
    Thread.Sleep(1000);
    vfs.GetEntry("");
    // Release belate handle. Disposes here or below, depending which thread runs last.
    belateDisposeHandle.Dispose();
});

// Start dispose, but postpone it until belatehandle is disposed in another thread.
vfs.Dispose();
```

**.AddMountsToBeDisposed()** Disposes mounted filesystems at the dispose of the *VirtualFileSystem*.

```csharp
IFileSystemDisposable vfs =
    new VirtualFileSystem()
    .Mount("", new FileSystem(""))
    .Mount("/tmp/", new MemoryFileSystem())
    .AddMountsToBeDisposed();
```
