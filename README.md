# Introduction
Lexical.FileSystem is a virtual filesystem class libraries for .NET.

NuGet Packages:
* Lexical.FileSystem ([Website](http://lexical.fi/FileSystem/index.html), [Github](https://github.com/tagcode/Lexical.FileSystem), [Nuget](https://www.nuget.org/packages/Lexical.FileSystem/))
* Lexical.FileSystem.Abstractions ([Website](http://lexical.fi/docs/IFileSystem/index.html), [Github](https://github.com/tagcode/Lexical.FileSystem/tree/master/Lexical.FileSystem.Abstractions), [Nuget](https://www.nuget.org/packages/Lexical.FileSystem.Abstractions/))
* License ([Apache-2.0 license](http://www.apache.org/licenses/LICENSE-2.0))

Please leave a comment or feedback on [github](https://github.com/tagcode/Lexical.FileSystem/issues), or kindle a star if you like the library. 

Contents:
* [FileSystem](#FileSystem)
* [VirtualFileSystem](#VirtualFileSystem)
* [Decoration](#Decoration)
* [IFileProvider](#IFileProvider)
* [MemoryFileSystem](#MemoryFileSystem)
* [HttpFileSystem](#HttpFileSystem)
* [FileScanner](#FileScanner)
* [VisitTree](#VisitTree)

# FileSystem

**new FileSystem(<i>path</i>)** creates an instance of filesystem at *path*. 

```csharp
IFileSystem fs = new FileSystem(@"C:\Temp\");
```

*FileSystem* can be browsed.

```csharp
foreach (var entry in fs.Browse(""))
    Console.WriteLine(entry.Path);
```

Files can be opened for reading.

```csharp
using (Stream s = fs.Open("file.txt", FileMode.Open, FileAccess.Read, FileShare.Read))
{
    Console.WriteLine(s.Length);
}
```

And for for writing.

```csharp
using (Stream s = fs.Open("somefile.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
{
    s.WriteByte(32);
}
```

Directories can be created.

```csharp
fs.CreateDirectory("dir/");
```

Directories can be deleted.

```csharp
fs.Delete("dir/", recurse: true);
```

Files and directories can be renamed and moved.

```csharp
fs.CreateDirectory("dir/");
fs.Move("dir/", "new-name/");
```

And file attributes changed.

```csharp
fs.SetFileAttribute("myfile", FileAttributes.ReadOnly);
```

# Singleton

The singleton instance **FileSystem.OS** refers to a filesystem at the OS root.

```csharp
IFileSystem fs = FileSystem.OS;
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
└── "18e1904137f065db88dfbd23609eb877"
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
| FileSystem.Config                | User's cloud-sync program configuration (roaming data).                                          | "C:\\Users\\<i>&lt;user&gt;</i>\\AppData\\Roaming"    | "/home/<i>&lt;user&gt;</i>/.config"      |
| FileSystem.Data                  | User's local program data.                                                                       | "C:\\Users\\<i>&lt;user&gt;</i>\\AppData\\Local"      | "/home/<i>&lt;user&gt;</i>/.local/share" |
| FileSystem.ProgramData           | Program data that is shared with every user.                                                     | "C:\\ProgramData"                                     | "/usr/share"                             |
| FileSystem.Desktop               | User's desktop.                                                                                  | "C:\\Users\\<i>&lt;user&gt;</i>\\Desktop"             | "/home/user/Desktop"                     |
| FileSystem.MyPictures            | User's pictures.                                                                                 | "C:\\Users\\<i>&lt;user&gt;</i>\\Pictures"            | "/home/user/Pictures"                    |
| FileSystem.MyVideos              | User's videos.                                                                                   | "C:\\Users\\<i>&lt;user&gt;</i>\\Videos"              | "/home/user/Videos"                      |
| FileSystem.MyMusic               | User's music.                                                                                    | "C:\\Users\\<i>&lt;user&gt;</i>\\Music"               | "/home/user/Music"                       |
| FileSystem.Templates             | Templates.                                                                                       | "C:\\Users\\<i>&lt;user&gt;</i>\\AppData\\Roaming\\Microsoft\\Windows\\Templates" | "/home/user/Templates"         |

**IFileEntry.PhysicalPath()** returns physical path of file entry.

```csharp
foreach(var line in FileSystem.Temp.VisitTree(depth:2))
    Console.WriteLine(line.Entry.PhysicalPath());
```

TreeVisitor prints physical path with **PrintTree.Format.PhysicalPath** flag.

```csharp
FileSystem.Temp.PrintTo(
    output: Console.Out, 
    depth: 2, 
    format: PrintTree.Format.Default | PrintTree.Format.PhysicalPath);
```
<pre style="line-height:1.2;">
"" [C:\Users\\<i>&lt;user&gt;</i>\\AppData\Local\Temp\]
├── "dmk55ohj.jjp" [C:\Users\\<i>&lt;user&gt;</i>\\AppData\Local\Temp\dmk55ohj.jjp]
├── "wrz4cms5.r2f" [C:\Users\\<i>&lt;user&gt;</i>\\AppData\Local\Temp\wrz4cms5.r2f]
└── "18e1904137f065db88dfbd23609eb877" [C:\Users\\<i>&lt;user&gt;</i>\\AppData\Local\Temp\18e1904137f065db88dfbd23609eb877]
</pre>

# Observing

Files and directories can be observed for changes.

```csharp
IObserver<IFileSystemEvent> observer = new Observer();
IFileSystemObserver handle = FileSystem.OS.Observe("C:/**", observer);
```

Observer can be used in a *using* scope.

```csharp
using (var handle = FileSystem.Temp.Observe("*.dat", new PrintObserver()))
{
    FileSystem.Temp.CreateFile("file.dat", new byte[] { 32, 32, 32, 32 });
    FileSystem.Temp.Delete("file.dat");

    Thread.Sleep(1000);
}
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
Start(C:\Users\\<i>&lt;user&gt;</i>\\AppData\Local\Temp\, 23.10.2019 16.27.01 +00:00)
Create(C:\Users\\<i>&lt;user&gt;</i>\\AppData\Local\Temp\, 23.10.2019 16.27.01 +00:00, file.dat)
Change(C:\Users\\<i>&lt;user&gt;</i>\\AppData\Local\Temp\, 23.10.2019 16.27.01 +00:00, file.dat)
Delete(C:\Users\\<i>&lt;user&gt;</i>\\AppData\Local\Temp\, 23.10.2019 16.27.01 +00:00, file.dat)
OnCompleted
```


# Disposing

Disposable objects can be attached to be disposed along with *FileSystem*.

```csharp
// Init
object obj = new ReaderWriterLockSlim();
IFileSystemDisposable fs = new FileSystem("").AddDisposable(obj);

// ... do work ...

// Dispose both
fs.Dispose();
```

Delegates can be attached to be executed at dispose of *FileSystem*.

```csharp
IFileSystemDisposable fs = new FileSystem("")
    .AddDisposeAction(f => Console.WriteLine("Disposed"));
```

**.BelateDispose()** creates a handle that postpones dispose on *.Dispose()*. Actual dispose will proceed once *.Dispose()* is called and
all belate handles are disposed. This can be used for passing the *IFileSystem* to a worker thread. 

```csharp
FileSystem fs = new FileSystem("");
fs.Browse("");

// Postpone dispose
IDisposable belateDisposeHandle = fs.BelateDispose();
// Start concurrent work
Task.Run(() =>
{
    // Do work
    Thread.Sleep(1000);
    fs.GetEntry("");
    // Release belate handle. Disposes here or below, depending which thread runs last.
    belateDisposeHandle.Dispose();
});

// Start dispose, but postpone it until belatehandle is disposed in another thread.
fs.Dispose();
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

Observer can be placed before and after mounting. If observer is placed before, then mounting will notify the observer with *IFileSystemEventCreate* event for all the added files.

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
    .Mount("http://", HttpFileSystem.Instance, FileSystemOption.SubPath("http://"))
    .Mount("https://", HttpFileSystem.Instance, FileSystemOption.SubPath("https://"))
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

Application's shared program data can be placed in "program-data://ApplicationName/datafile". These are typically modifiable files.

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

# HttpFileSystem
**new HttpFileSystem(<i>HttpClient, IFileSystemOption</i>)** creates a new http based filesystem.

```csharp
IFileSystem fs = new HttpFileSystem(httpClient: default, option: default);
```

**HttpFileSystem.Instance** is the default singleton instance.

```csharp
IFileSystem fs = HttpFileSystem.Instance;
```

Opening a resource with **FileMode.Open** and **FileAccess.Read** parameters makes a GET request.

```csharp
using (var s = HttpFileSystem.Instance.Open("http://lexical.fi/", FileMode.Open, FileAccess.Read, FileShare.None))
{
    byte[] data = StreamUtils.ReadFully(s);
    String str = UTF8Encoding.UTF8.GetString(data);
    Console.WriteLine(str);
}
```

Web resources can be used with generic extension methods such as **.CopyFile()**.

```csharp
MemoryFileSystem ram = new MemoryFileSystem();
HttpFileSystem.Instance.CopyFile("http://lexical.fi", ram, "document.txt");
ram.PrintTo(Console.Out);
```

<pre style="line-height:1.2;">
""
└── "document.txt"
</pre>

Opening a resource with **FileMode.Create** and **FileAccess.Write** makes a PUT request.

```csharp
byte[] data = new byte[1024];
using (var s = HttpFileSystem.Instance.Open("http://lexical.fi/", FileMode.Create, FileAccess.Write, FileShare.None))
    s.Write(data);
```

HttpFileSystem can be constructed with various options, such as SubPath and custom http header.

```csharp
// Create options
List<KeyValuePair<string, IEnumerable<string>>> headers = new List<KeyValuePair<string, IEnumerable<string>>>();
headers.Add(new KeyValuePair<string, IEnumerable<string>>("User-Agent", new String[] { "MyUserAgent" }));
IFileSystemOption option1 = new FileSystemToken(headers, typeof(System.Net.Http.Headers.HttpHeaders).FullName);
IFileSystemOption option2 = FileSystemOption.SubPath("http://lexical.fi/");
IFileSystemOption options = FileSystemOption.Union(option1, option2);

// Create FileSystem
IFileSystem fs = new HttpFileSystem(option: options);

// Read resource
using (var s = fs.Open("index.html", FileMode.Open, FileAccess.Read, FileShare.None))
{
    byte[] data = StreamUtils.ReadFully(s);
    String str = UTF8Encoding.UTF8.GetString(data);
    Console.WriteLine(str);
}
```

User authentication header **AuthenticationHeaderValue** can be wrapped in **FileSystemToken** and passed to *Open()* method.

```csharp
AuthenticationHeaderValue authentication = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes($"webuser:webpassword")));
IFileSystemToken token = new FileSystemToken(authentication, typeof(AuthenticationHeaderValue).FullName);
using (var s = HttpFileSystem.Instance.Open("https://lexical.fi/FileSystem/private/document.txt", FileMode.Open, FileAccess.Read, FileShare.None, token))
{
    byte[] data = new byte[4096];
    int c = s.Read(data, 0, 1024);
    String str = UTF8Encoding.UTF8.GetString(data, 0, c);
    Console.WriteLine(str);
}
```

Another way is to pass user authentication token at construction of *HttpFileSystem*. 
The token must be given glob patterns where the token applies, for example "http://lexical.fi/FileSystem/private/**".

```csharp
// Authentication header
AuthenticationHeaderValue authentication = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes($"webuser:webpassword")));

// Token
IFileSystemToken token = new FileSystemToken(
    authentication, 
    typeof(AuthenticationHeaderValue).FullName, 
    "https://lexical.fi/FileSystem/private/**",
    "https://www.lexical.fi/FileSystem/private/**"
);

// Create FileSystem
IFileSystem fs = new HttpFileSystem(default, token);

// Open
using (var s = fs.Open("https://lexical.fi/FileSystem/private/document.txt", FileMode.Open, FileAccess.Read, FileShare.None))
{
    byte[] data = new byte[4096];
    int c = s.Read(data, 0, 1024);
    String str = UTF8Encoding.UTF8.GetString(data, 0, c);
    Console.WriteLine(str);
}
```

Third way is to pass authentication token into a decoration. 

```csharp
// Authentication header
AuthenticationHeaderValue authentication = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes($"webuser:webpassword")));

// Create token
IFileSystemToken token = new FileSystemToken(authentication, typeof(AuthenticationHeaderValue).FullName, "https://lexical.fi/FileSystem/private/**");

// Pass token into decorator
IFileSystem decoration = HttpFileSystem.Instance.Decorate(token);

// Open
using (var s = decoration.Open("https://lexical.fi/FileSystem/private/document.txt", FileMode.Open, FileAccess.Read, FileShare.None))
{
    byte[] data = new byte[4096];
    int c = s.Read(data, 0, 1024);
    String str = UTF8Encoding.UTF8.GetString(data, 0, c);
    Console.WriteLine(str);
}
```

**.Delete(<i>uri</i>)** sends DELETE http request.

```csharp
HttpFileSystem.Instance.Delete("https://lexical.fi/FileSystem/private/document.txt");
```

**.Browse(<i>uri</i>)** reads html document and parses links that refer to immediate child files and directories.

```csharp
var authBlob = Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes($"webuser:webpassword"));
var authentication = new AuthenticationHeaderValue("Basic", authBlob);
var token = new FileSystemToken(authentication, typeof(AuthenticationHeaderValue).FullName, "https://lexical.fi/FileSystem/private/**");

IFileSystemEntry[] entries = HttpFileSystem.Instance.Browse("https://lexical.fi/FileSystem/private/", token);
```

**.GetEntry(<i>uri</i>)** reads resource headers and returns entry.

```csharp
var authBlob = Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes($"webuser:webpassword"));
var authentication = new AuthenticationHeaderValue("Basic", authBlob);
var token = new FileSystemToken(authentication, typeof(AuthenticationHeaderValue).FullName, "https://lexical.fi/FileSystem/private/**");

IFileSystemEntry entry = HttpFileSystem.Instance.GetEntry("https://lexical.fi/FileSystem/private/document.txt", token);
```

File system can be scanned with *.VisitTree()* and *.PrintTo()* extension methods.

```csharp
var authBlob = Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes($"webuser:webpassword"));
var authentication = new AuthenticationHeaderValue("Basic", authBlob);
var token = new FileSystemToken(authentication, typeof(AuthenticationHeaderValue).FullName, "https://lexical.fi/FileSystem/private/**");

HttpFileSystem.Instance.PrintTo(Console.Out, "https://lexical.fi/FileSystem/private/", token: token);
```

<pre style="line-height:1.2;">
"private"
├── "Directory"
│  └── "file.txt"
├── "Folder"
│  └── "file.txt"
└── "document.txt"
</pre>

On github too. Notice that, only directories are returned from "/tree/", as files are on different url branch "/blob/".

```csharp
HttpFileSystem.Instance.PrintTo(Console.Out, "https://github.com/tagcode/Lexical.FileSystem/tree/master/");
```

<pre style="line-height:1.2;">
"master"
├── "Lexical.FileSystem"
│  ├── "Decoration"
│  ├── "Extensions"
│  ├── "Internal"
│  ├── "Package"
│  └── "Utility"
└── "Lexical.FileSystem.Abstractions"
   ├── "Extensions"
   ├── "FileProvider"
   ├── "Internal"
   ├── "Option"
   ├── "Package"
   └── "Utility"
</pre>

*CancellationToken* can be passed as a token.

```csharp
// Cancel token
CancellationTokenSource cancelSrc = new CancellationTokenSource();
IFileSystemToken token = new FileSystemToken(cancelSrc.Token, typeof(CancellationToken).FullName);

// Set canceled
cancelSrc.Cancel();

// Read
HttpFileSystem.Instance.Open("http://lexical.fi/", FileMode.Open, FileAccess.Read, FileShare.None, token);
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

# VisitTree
The extension method <i>IFileSystem</i><b>.VisitTree(string path, int depth)</b> visits a tree structure of a filesystem.


```csharp
IFileSystem ram = new MemoryFileSystem();
ram.CreateDirectory("/tmp/");
ram.CreateDirectory("/mnt/");
ram.CreateDirectory("/usr/lex/");
ram.CreateDirectory("c:/dir/dir/");
ram.CreateFile("/tmp/helloworld.txt", Encoding.UTF8.GetBytes("Hello World!\r\n"));
ram.CreateDirectory("file://c:/temp");

foreach (TreeVisit.Line line in ram.VisitTree())
{
    Console.WriteLine(line);
}
```

<pre style="line-height:1.2;">
""
├── ""
│  ├── "mnt"
│  ├── "tmp"
│  │  └── "helloworld.txt"
│  └── "usr"
│     └── "lex"
├── "c:"
│  └── "dir"
│     └── "dir"
└── "file:"
   └── ""
      └── "c:"
         └── "temp"
</pre>

Parameter *depth* determines visit depth.

```csharp
foreach (TreeVisit.Line line in ram.VisitTree(depth: 1))
{
    Console.WriteLine(line);
}
```

<pre style="line-height:1.2;">
""
├── ""
├── "c:"
└── "file:"
</pre>

Parameter *path* determines start location.

```csharp
foreach (TreeVisit.Line line in ram.VisitTree(path: "/tmp/"))
{
    Console.WriteLine(line);
}
```

<pre style="line-height:1.2;">
"tmp"
└── "helloworld.txt"
</pre>

# PrintTree

The extension method <i>IFileSystem</i><b>.PrintTo(TextWriter output, string path, int depth, Format format)</b> prints a tree structure 
to a *TextWriter* such as **Console.Out** (*stdout*).


```csharp
IFileSystem ram = new MemoryFileSystem();
ram.CreateDirectory("/tmp/");
ram.CreateDirectory("/mnt/");
ram.CreateDirectory("/usr/lex/");
ram.CreateDirectory("c:/dir/dir/");
ram.CreateFile("/tmp/helloworld.txt", Encoding.UTF8.GetBytes("Hello World!\r\n"));
ram.CreateDirectory("file://c:/temp/");

ram.PrintTo(Console.Out);
```

<pre style="line-height:1.2;">
""
├── ""
│  ├── "mnt"
│  ├── "tmp"
│  │  └── "helloworld.txt"
│  └── "usr"
│     └── "lex"
├── "c:"
│  └── "dir"
│     └── "dir"
└── "file:"
   └── ""
      └── "c:"
         └── "temp"
</pre>

<i>IFileSystem</i><b>.PrintTo(TextWriter output, string path, int depth, Format format)</b> appends to a **StringBuilder**.

```csharp
StringBuilder sb = new StringBuilder();
ram.PrintTo(sb);
```

<i>IFileSystem</i><b>.Print(string path, int depth, Format format)</b> prints out to as *string*.

```csharp
Console.WriteLine(ram.Print());
```

Parameter *depth* determines visit depth.

```csharp
Console.WriteLine(ram.Print(depth:1));
```

<pre style="line-height:1.2;">
""
├── ""
├── "c:"
└── "file:"
</pre>

Parameter *path* determines start location.

```csharp
Console.WriteLine(ram.Print(path:"/tmp/"));
```

<pre style="line-height:1.2;">
"tmp"
└── "helloworld.txt"
</pre>

Parameter *format* determines which infos are printed out. For example **PrintTree.Format.Path** prints out full path instead of name.

```csharp
string tree =  ram.Print(format: PrintTree.Format.Tree | PrintTree.Format.Path | 
                                 PrintTree.Format.Length | PrintTree.Format.Error);
```

<pre style="line-height:1.2;">
├── /
│  ├── /mnt/
│  ├── /tmp/
│  │  └── /tmp/helloworld.txt 14
│  └── /usr/
│     └── /usr/lex/
├── c:/
│  └── c:/dir/
│     └── c:/dir/dir/
└── file:/
   └── file://
      └── file://c:/
         └── file://c:/temp/
</pre>

**PrintTree.Format** flags:

| Flag     | Description |
|:---------|:------------|
| <i>PrintTree.Format.</i>Tree     | The tree structure. |
| <i>PrintTree.Format.</i>Name     | Entry name.  |
| <i>PrintTree.Format.</i>Path     | Entry path.  |
| <i>PrintTree.Format.</i>Length   | Entry length for file entires. |
| <i>PrintTree.Format.</i>Error    | Traverse error. |
| <i>PrintTree.Format.</i>LineFeed | Next line. |
| <i>PrintTree.Format.</i>Mount    | Mounted filesystem.                                                  |
| <i>PrintTree.Format.</i>DriveLabel | Label of filesystem drive or volume.                                          |
| <i>PrintTree.Format.</i>DriveFreespace | Available free space on the drive or volume.                          |
| <i>PrintTree.Format.</i>DriveSize| Total size of drive or volume.                                       |
| <i>PrintTree.Format.</i>DriveType| Drive or volume type, such as: Fixed, Removeable, Network, Ram |
| <i>PrintTree.Format.</i>DriveFormat| FileSystem format, such as: NTFS, FAT32, EXT4 |
| <i>PrintTree.Format.</i>FileAttributes| File attributes, such as: ReadOnly, Hidden, System, Directory, Archive |
| <i>PrintTree.Format.</i>PhysicalPath| Physical path |
||
| <i>PrintTree.Format.</i>Default  | <i>PrintTree.Format.</i>Tree &#124; <i>PrintTree.Format.</i>Name &#124; <i>PrintTree.Format.</i>Error |
| <i>PrintTree.Format.</i>DefaultPath | <i>PrintTree.Format.</i>Tree &#124; <i>PrintTree.Format.</i>Path &#124; <i>PrintTree.Format.</i>Error |
| <i>PrintTree.Format.</i>All      | All flags.                                                           |
| <i>PrintTree.Format.</i>AllWithName | All flags with name printing (excludes path printing). |
| <i>PrintTree.Format.</i>AllWithPath | All flags with path printing (excludes name printing). |



