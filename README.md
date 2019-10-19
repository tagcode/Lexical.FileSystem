# Introduction
Lexical.FileSystem is a virtual filesystem class libraries for .NET.

NuGet Packages:
* Lexical.FileSystem ([Website](http://lexical.fi/FileSystem/index.html), [Github](https://github.com/tagcode/Lexical.FileSystem), [Nuget](https://www.nuget.org/packages/Lexical.FileSystem/))
* Lexical.FileSystem.Abstractions ([Website](http://lexical.fi/docs/IFileSystem/index.html), [Github](https://github.com/tagcode/Lexical.FileSystem/tree/master/Lexical.FileSystem.Abstractions), [Nuget](https://www.nuget.org/packages/Lexical.FileSystem.Abstractions/))


# FileSystem

**new FileSystem(<i>path</i>)** creates an instance of filesystem at directory. Path "" refers to operating system root.

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
using (IDisposable handle = filesystem.Observe("C:/**", observer))
{
}
```

Directories can be created.

```csharp
filesystem.CreateDirectory("dir/");
```

Directories can be deleted.

```csharp
filesystem.Delete("dir/", recurse: true);
```

Files and directories can be renamed and moved.

```csharp
filesystem.CreateDirectory("dir/");
filesystem.Move( "dir/", "new-name/");
```

# File structure
The singleton instance **FileSystem.OS** refers to a filesystem at the OS root.

```csharp
IFileSystem filesystem = FileSystem.OS;
```

Extension method **.VisitTree()** visits filesystem. On root path "" *FileSystem.OS* returns drive letters.

```csharp
foreach (var line in FileSystem.OS.VisitTree(depth: 2))
    Console.WriteLine(line);
```


<pre style="line-height:1.2;">
""
├──"C:"
│  ├── "hiberfil.sys"
│  ├── "pagefile.sys"
│  ├── "swapfile.sys"
│  ├── "Documents and Settings"
│  ├── "Program Files"
│  ├── "Program Files (x86)"
│  ├── "System Volume Information"
│  ├── "Users"
│  └── "Windows10"
└──"D:"
</pre>

> [!NOTE]
> The separator character is always forward slash '/'. For example "C:/Windows/win.ini".

Extension method **.PrintTo()** appends the visited filesystem to text output. 


```csharp
FileSystem.OS.PrintTo(Console.Out, depth: 2, format: PrintTree.Format.DefaultPath);
```

<pre style="line-height:1.2;">
├── C:/
│  ├── C:/hiberfil.sys
│  ├── C:/pagefile.sys
│  ├── C:/swapfile.sys
│  ├── C:/Documents and Settings/
│  ├── C:/Program Files/
│  ├── C:/Program Files (x86)/
│  ├── C:/System Volume Information/
│  ├── C:/Users/
│  └── C:/Windows/
└── D:/
</pre>

On linux *FileSystem.OS* returns slash '/' root.

```csharp
FileSystem.OS.PrintTo(Console.Out, depth: 3, format: PrintTree.Format.DefaultPath);
```

<pre style="line-height:1.2;">

└──/
   ├──/bin/
   ├──/boot/
   ├──/dev/
   ├──/etc/
   ├──/lib/
   ├──/media/
   ├──/mnt/
   ├──/root/
   ├──/sys/
   ├──/usr/
   └──/var/
</pre>

**FileSystem.Application** refers to the application's root directory.

```csharp
FileSystem.Application.PrintTo(Console.Out);
```

<pre style="line-height:1.2;">
""
├── "Application.dll"
├── "Application.runtimeconfig.json"
├── "Lexical.FileSystem.Abstractions.dll"
└── "Lexical.FileSystem.dll"
</pre>

**FileSystem.Temp** refers to the running user's temp directory.

```csharp
FileSystem.Temp.PrintTo(Console.Out, depth: 1);
```

<pre style="line-height:1.2;">
""
├── "dmk55ohj.jjp"
├── "wrz4cms5.r2f"
├── "18e1904137f065db88dfbd23609eb877"
└── "82e759b7-b237-45f7-91b9-8450b0732a6e.tmp"
</pre>

**Singleton** instances:

| Name                             | Description                                                                                      | On Windows                                            | On Linux                                 |
|:---------------------------------|:-------------------------------------------------------------------------------------------------|:------------------------------------------------------|:-----------------------------------------|
| FileSystem.OS                    | Operating system root.                                                                           | ""                                                    | ""                                       |
| FileSystem.Application           | Running application's base directory.                                                            |                                                       |                                          |
| FileSystem.UserProfile           | The user's profile folder.                                                                       | "C:\\Users\\<i>&lt;user&gt;</i>"                      | "/home/<i>&lt;user&gt;</i>"              |
| FileSystem.MyDocuments           | The My Documents folder.                                                                         | "C:\\Users\\<i>&lt;user&gt;</i>\\Documents"           | "/home/<i>&lt;user&gt;</i>"              |
| FileSystem.Personal              | A common repository for documents.                                                               | "C:\\Users\\<i>&lt;user&gt;</i>\\Documents"           | "/home/<i>&lt;user&gt;</i>"              |
| FileSystem.Temp                  | Running user's temp directory.                                                                   | "C:\\Users\\<i>&lt;user&gt;</i>\\AppData\\Local\\Temp"| "/tmp                                    |
| FileSystem.ApplicationData       | A common repository for application-specific data for the current roaming user.                  | "C:\\Users\\<i>&lt;user&gt;</i>\\AppData\\Roaming"    | "/home/<i>&lt;user&gt;</i>/.config"      |
| FileSysten.LocalApplicationData  | A common repository for application-specific data that is used by the current, non-roaming user. | "C:\\Users\\<i>&lt;user&gt;</i>\\AppData\\Local"      | "/home/<i>&lt;user&gt;</i>/.local/share" |
| FileSysten.CommonApplicationData | A common repository for application-specific data that is used by all users.                     | "C:\\ProgramData"                                     | "/usr/share"                             |

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
IFileSystem vfs = new VirtualFileSystem()
    .Mount("file://", FileSystem.OS)
    .Mount("tmp://", new MemoryFileSystem())
    .Mount("home://", FileSystem.Personal)
    .Mount("docs://", FileSystem.MyDocuments)
    .Mount("application://", FileSystem.Application);
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

**.Mount(<i>path</i>, params <i>IFilesystem</i>, <i>IFileSystemOption</i>)** assigns filesystem with mount option.

```csharp
IFileSystem vfs = new VirtualFileSystem();
vfs.Mount("/app/", FileSystem.Application, FileSystemOption.ReadOnly);
```

Option such as *FileSystemOption.SubPath()*.

```csharp
IFileSystem vfs = new VirtualFileSystem();
string appDir = AppDomain.CurrentDomain.BaseDirectory.Replace('\\', '/');
vfs.Mount("/app/", FileSystem.OS, FileSystemOption.SubPath(appDir));
```

**.Mount(<i>path</i>, params <i>IFilesystem[]</i>)** assigns multiple filesystems into one mountpoint.

```csharp
IFileSystem vfs = new VirtualFileSystem();
IFileSystem overrides = new MemoryFileSystem();
overrides.CreateFile("important.dat", new byte[] { 12, 23, 45, 67, 89 });
vfs.Mount("/app/", overrides, FileSystem.Application);
```

**.Mount(<i>path</i>, params (<i>IFilesystem</i>, <i>IFileSystemOption</i>)[])** assigns multiple filesystems with mount options.

```csharp
IFileSystem overrides = new MemoryFileSystem();
IFileSystem vfs = new VirtualFileSystem();

vfs.Mount("/app/", 
    (overrides, FileSystemOption.ReadOnly), 
    (FileSystem.Application, FileSystemOption.ReadOnly)
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

Observer can be placed before and after mounting. If observer is placed before, then mounting will notify the observer with Create event for all the added files.

```csharp
IFileSystem vfs = new VirtualFileSystem();
vfs.Observe("**", new PrintObserver());

IFileSystem ram = new MemoryFileSystem();
ram.CreateDirectory("/dir/");
ram.CreateFile("/dir/file.txt", new byte[] { 32, 65, 66 });

vfs.Mount("", ram);
```


```csharp
class PrintObserver : IObserver<IFileSystemEvent>
{
    public void OnCompleted() => Console.WriteLine("OnCompleted");
    public void OnError(Exception error) => Console.WriteLine(error);
    public void OnNext(IFileSystemEvent @event) => Console.WriteLine(@event);
}
```

```none
Start(VirtualFileSystem, 19.10.2019 11.34.08 +00:00)
Create(VirtualFileSystem, 19.10.2019 11.34.08 +00:00, /dir/file.txt)
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
Start(VirtualFileSystem, 19.10.2019 11.34.23 +00:00)
Create(VirtualFileSystem, 19.10.2019 11.34.23 +00:00, /dir/file.txt)
```

**.Unmount()** dispatches events of unmounted files as if they were deleted.

```csharp
vfs.Unmount("");
```

```none
Delete(VirtualFileSystem, 19.10.2019 11.34.39 +00:00, /dir/file.txt)
```

If filesystem is mounted with **FileSystemOption.NoObserve**, then the assigned filesystem cannot be observed, and it won't dispatch events of added files on mount.

```csharp
vfs.Mount("", ram, FileSystemOption.NoObserve);
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

# Disposing

Disposable objects can be attached to be disposed along with *VirtualFileSystem*.

```csharp
// Init
object obj = new ReaderWriterLockSlim();
IFileSystemDisposable filesystem = new VirtualFileSystem().AddDisposable(obj);

// ... do work ...

// Dispose both
filesystem.Dispose();
```

Delegates can be attached to be executed at dispose of *VirtualFileSystem*.

```csharp
IFileSystemDisposable filesystem = new VirtualFileSystem()
    .AddDisposeAction(f => Console.WriteLine("Disposed"));
```

**.BelateDispose()** creates a handle that postpones dispose on *.Dispose()*. Actual dispose will proceed once *.Dispose()* is called and
all belate handles are disposed. This can be used for passing the *IFileSystem* to a worker thread. 

```csharp
VirtualFileSystem filesystem = new VirtualFileSystem().Mount("", FileSystem.OS);
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

# Decoration

The <i>IFileSystems</i><b>.Decorate(<i>IFileSystemOption</i>)</b> extension method decorates a filesystem with new decorated options. 
Decoration options is an intersection of filesystem's options and the options in the parameters, so decoration reduces features.

```csharp
IFileSystem ram = new MemoryFileSystem();
IFileSystem rom = ram.Decorate(FileSystemOption.ReadOnly);
```

<i>IFileSystems</i><b>.AsReadOnly()</b> is same as <i>IFileSystems.Decorate(FileSystemOption.ReadOnly)</i>.

```csharp
IFileSystem rom = ram.AsReadOnly();
```

**FileSystemOption.NoBrowse** prevents browsing, hiding files.

```csharp
IFileSystem invisible = ram.Decorate(FileSystemOption.NoBrowse);
```

**FileSystemOption.SubPath(<i>subpath</i>)** option exposes only a subtree of the decorated filesystem. 
The *subpath* argument must end with slash "/", or else it mounts to prefix of files.


```csharp
IFileSystem ram = new MemoryFileSystem();
ram.CreateDirectory("tmp/dir/");
ram.CreateFile("tmp/dir/file.txt", new byte[] { 32,32,32,32,32,32,32,32,32 });

IFileSystem tmp = ram.Decorate(FileSystemOption.SubPath("tmp/"));
tmp.PrintTo(Console.Out, format: PrintTree.Format.DefaultPath);
```
<pre style="line-height:1.2;">

└── dir/
   └── dir/file.txt
</pre>

**.AddSourceToBeDisposed()** adds source objects to be disposed along with the decoration.

```csharp
MemoryFileSystem ram = new MemoryFileSystem();
IFileSystemDisposable rom = ram.Decorate(FileSystemOption.ReadOnly).AddSourceToBeDisposed();
// Do work ...
rom.Dispose();
```

Decorations implement **IDisposeList** and **IBelatableDispose** which allows to attach disposable objects.

```csharp
MemoryFileSystem ram = new MemoryFileSystem();
ram.CreateDirectory("tmp/dir/");
ram.CreateFile("tmp/dir/file.txt", new byte[] { 32, 32, 32, 32, 32, 32, 32, 32, 32 });
IFileSystemDisposable rom = ram.Decorate(FileSystemOption.ReadOnly).AddDisposable(ram);
// Do work ...
rom.Dispose();
```

If multiple decorations are used, the source reference can be 'forgotten' after construction if belate dispose handles are passed over to decorations.

```csharp
// Create ram filesystem
MemoryFileSystem ram = new MemoryFileSystem();
ram.CreateDirectory("tmp/dir/");
ram.CreateFile("tmp/dir/file.txt", new byte[] { 32, 32, 32, 32, 32, 32, 32, 32, 32 });

// Create decorations
IFileSystemDisposable rom = ram.Decorate(FileSystemOption.ReadOnly).AddDisposable(ram.BelateDispose());
IFileSystemDisposable tmp = ram.Decorate(FileSystemOption.SubPath("tmp/")).AddDisposable(ram.BelateDispose());
ram.Dispose(); // <- is actually postponed

// Do work ...

// Dispose rom1 and tmp, disposes ram as well
rom.Dispose();
tmp.Dispose();
```

# Concat
<b>FileSystems.Concat(<i>IFileSystem[]</i>)</b> method composes IFileSystem instances into one.

```csharp
IFileSystem ram = new MemoryFileSystem();
IFileSystem os = FileSystem.OS;
IFileSystem fp = new PhysicalFileProvider(AppDomain.CurrentDomain.BaseDirectory).ToFileSystem()
    .AddDisposeAction(fs=>fs.FileProviderDisposable?.Dispose());
IFileSystem embedded = new EmbeddedFileSystem(typeof(Composition_Examples).Assembly);

IFileSystem composition = FileSystems.Concat(ram, os, fp, embedded)
    .AddDisposable(embedded)
    .AddDisposable(fp)
    .AddDisposable(os);
```

Composed set of files can be browsed.

```csharp
foreach (var entry in composition.VisitTree(depth: 1))
    Console.WriteLine(entry);
```

Files can be read from the composed set.

```csharp
using (Stream s = composition.Open("docs.example-file.txt", FileMode.Open, FileAccess.Read, FileShare.Read))
{
    Console.WriteLine(s.Length);
}
```

If two files have same name and path, the file in the first *IFileSystem* overshadows files from later *IFileSystem*s.

```csharp
IFileSystem ram1 = new MemoryFileSystem();
IFileSystem ram2 = new MemoryFileSystem();
IFileSystem composition = FileSystems.Concat(ram1, ram2);

// Create file of 1024 bytes
ram1.CreateFile("file.txt", new byte[1024]);

// Create file of 10 bytes
ram2.CreateFile("file.txt", new byte[10]);

// Get only one entry size of 1024 bytes.
composition.PrintTo(Console.Out, format: PrintTree.Format.Default | PrintTree.Format.Length);
```

<pre style="line-height:1.2;">
""
└── "file.txt" 1024
</pre>

<b>FileSystems.Concat(<i>(IFileSystem, IFileSystemOption)[]</i>)</b> applies options to the filesystems.

```csharp
IFileSystem filesystem = FileSystem.Application;
IFileSystem overrides = new MemoryFileSystem();
IFileSystem composition = FileSystems.Concat(
    (filesystem, null), 
    (overrides, FileSystemOption.ReadOnly)
);
```

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
IObserver<IFileSystemEvent> observer = new Observer();
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
IObserver<IFileSystemEvent> observer = new Observer();
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

# FileScanner
**FileScanner** scans the tree structure of a filesystem for files that match its configured criteria. It uses concurrent threads.

```csharp
IFileSystem fs = new MemoryFileSystem();
fs.CreateDirectory("myfile.zip/folder");
fs.CreateFile("myfile.zip/folder/somefile.txt");

FileScanner filescanner = new FileScanner(fs);
```

*FileScanner* needs to be populated with at least one filter, such as wildcard pattern, **.AddWildcard(*string*)**.

```csharp
filescanner.AddWildcard("*.zip");
```

Or regular expression.

```csharp
filescanner.AddRegex(path: "", pattern: new Regex(@".*\.zip"));
```

Or glob pattern.

```csharp
filescanner.AddGlobPattern("**.zip/**.txt");
```

The initial start path is extracted from the pattern.

```csharp
filescanner.AddGlobPattern("myfile.zip/**.txt");
```

Search is started when **IEnumerator&lt;*IFileSystemEntry*&gt;** is enumerated from the scanner.

```csharp
foreach (IFileSystemEntry entry in filescanner)
{
    Console.WriteLine(entry.Path);
}
```

Exceptions that occur at real-time can be captured into concurrent collection.

```csharp
// Collect errors
filescanner.errors = new ConcurrentBag<Exception>();
// Run scan
IFileSystemEntry[] entries = filescanner.ToArray();
// View errors
foreach (Exception e in filescanner.errors) Console.WriteLine(e);
```

The property **.ReturnDirectories** determines whether scanner returns directories.

```csharp
filescanner.ReturnDirectories = true;
```

The property **.SetDirectoryEvaluator(<i>Func&lt;IFileSystemEntry, bool&gt; func</i>)** sets a criteria that determines whether scanner enters a directory.

```csharp
filescanner.SetDirectoryEvaluator(e => e.Name != "tmp");
```

