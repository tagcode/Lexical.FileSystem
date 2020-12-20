# FileSystem

**new FileSystem(<i>path</i>)** creates an instance of filesystem at *path*. 

```csharp
IFileSystem fs = new FileSystem(@"C:\Temp\");
```

**.Browse(<i>path</i>)** returns a snapshot of directory contents. 

```csharp
IDirectoryContent contents = fs.Browse("C:/Windows/");
```

*IDirectoryContent* is enumerable *IEnumerable&lt;IEntry&gt;*.

```csharp
foreach (IEntry entry in fs.Browse("C:/Windows/"))
    Console.WriteLine(entry.Path);
```

**.AssertExists()** asserts that directory exists. It throws *DirectoryNotFound* if not found.

```csharp
foreach (var entry in fs.Browse("C:/Windows/").AssertExists())
    Console.WriteLine(entry.Path);
```

**.GetEntry(<i>path</i>)** reads a single file or directory entry. Returns null if entry is not found.

```csharp
IEntry e = FileSystem.OS.GetEntry("C:/Windows/win.ini");
Console.WriteLine(e.Path);
```

**.AssertExists()** asserts that null is not returned. Throws *FileNotFoundException* if entry was not found.

```csharp
IEntry e = FileSystem.OS.GetEntry("C:/Windows/win.ini").AssertExists();
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
"" [C:\Users\\user</i>\\AppData\Local\Temp\]
├── "dmk55ohj.jjp" [C:\Users\\user</i>\\AppData\Local\Temp\dmk55ohj.jjp]
├── "wrz4cms5.r2f" [C:\Users\\user</i>\\AppData\Local\Temp\wrz4cms5.r2f]
└── "18e1904137f065db88dfbd23609eb877" [C:\Users\\user</i>\\AppData\Local\Temp\18e1904137f065db88dfbd23609eb877]
</pre>

# Observing

Files and directories can be observed for changes.

```csharp
IObserver<IEvent> observer = new Observer();
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
class PrintObserver : IObserver<IEvent>
{
    public void OnCompleted() => Console.WriteLine("OnCompleted");
    public void OnError(Exception error) => Console.WriteLine(error);
    public void OnNext(IEvent @event) => Console.WriteLine(@event);
}
```

```none
StartEvent(C:\Users\\<i>&lt;user&gt;</i>\\AppData\Local\Temp\, 23.10.2019 16.27.01 +00:00)
CreateEvent(C:\Users\\<i>&lt;user&gt;</i>\\AppData\Local\Temp\, 23.10.2019 16.27.01 +00:00, file.dat)
ChangeEvent(C:\Users\\<i>&lt;user&gt;</i>\\AppData\Local\Temp\, 23.10.2019 16.27.01 +00:00, file.dat)
DeleteEvent(C:\Users\\<i>&lt;user&gt;</i>\\AppData\Local\Temp\, 23.10.2019 16.27.01 +00:00, file.dat)
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
