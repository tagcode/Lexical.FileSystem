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
filesystem.Move("dir/", "new-name/");
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

The separator character is always forward slash '/'. For example "C:/Windows/win.ini".

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
