# FileSystem

**new FileSystem(<i>path</i>)** creates an instance of filesystem at *path*. 
[!code-csharp[Snippet](Examples.cs#Snippet_1a)]

**.Browse(<i>path</i>)** returns a snapshot of directory contents. 
[!code-csharp[Snippet](Examples.cs#Snippet_2a)]

*IDirectoryContent* is enumerable *IEnumerable&lt;IEntry&gt;*.
[!code-csharp[Snippet](Examples.cs#Snippet_2d)]

**.AssertExists()** asserts that directory exists. It throws *DirectoryNotFound* if not found.
[!code-csharp[Snippet](Examples.cs#Snippet_2e)]

**.GetEntry(<i>path</i>)** reads a single file or directory entry. Returns null if entry is not found.
[!code-csharp[Snippet](Examples.cs#Snippet_2f)]

**.AssertExists()** asserts that null is not returned. Throws *FileNotFoundException* if entry was not found.
[!code-csharp[Snippet](Examples.cs#Snippet_2g)]

Files can be opened for reading.
[!code-csharp[Snippet](Examples.cs#Snippet_3a)]

And for for writing.
[!code-csharp[Snippet](Examples.cs#Snippet_3b)]

Directories can be created.
[!code-csharp[Snippet](Examples.cs#Snippet_5)]

Directories can be deleted.
[!code-csharp[Snippet](Examples.cs#Snippet_6)]

Files and directories can be renamed and moved.
[!code-csharp[Snippet](Examples.cs#Snippet_7)]

And file attributes changed.
[!code-csharp[Snippet](Examples.cs#Snippet_7f)]

# Singleton

The singleton instance **FileSystem.OS** refers to a filesystem at the OS root.
[!code-csharp[Snippet](Examples.cs#Snippet_8a)]

Extension method **.VisitTree()** visits filesystem. On root path "" *FileSystem.OS* returns drive letters.
[!code-csharp[Snippet](Examples.cs#Snippet_8b)]


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

[!code-csharp[Snippet](Examples.cs#Snippet_8b2)]

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
[!code-csharp[Snippet](Examples.cs#Snippet_8c)]

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
[!code-csharp[Snippet](Examples.cs#Snippet_8d)]

<pre style="line-height:1.2;">
""
├── "Application.dll"
├── "Application.runtimeconfig.json"
├── "Lexical.FileSystem.Abstractions.dll"
└── "Lexical.FileSystem.dll"
</pre>

**FileSystem.Temp** refers to the running user's temp directory.
[!code-csharp[Snippet](Examples.cs#Snippet_8e)]

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
[!code-csharp[Snippet](Examples.cs#Snippet_8f)]

TreeVisitor prints physical path with **PrintTree.Format.PhysicalPath** flag.
[!code-csharp[Snippet](Examples.cs#Snippet_8g)]
<pre style="line-height:1.2;">
"" [C:\Users\\user</i>\\AppData\Local\Temp\]
├── "dmk55ohj.jjp" [C:\Users\\user</i>\\AppData\Local\Temp\dmk55ohj.jjp]
├── "wrz4cms5.r2f" [C:\Users\\user</i>\\AppData\Local\Temp\wrz4cms5.r2f]
└── "18e1904137f065db88dfbd23609eb877" [C:\Users\\user</i>\\AppData\Local\Temp\18e1904137f065db88dfbd23609eb877]
</pre>

# Observing

Files and directories can be observed for changes.
[!code-csharp[Snippet](Examples.cs#Snippet_9a)]

Observer can be used in a *using* scope.
[!code-csharp[Snippet](Examples.cs#Snippet_9b)]

[!code-csharp[Snippet](..\VirtualFileSystem\Examples.cs#PrintObserver)]

```none
StartEvent(C:\Users\\<i>&lt;user&gt;</i>\\AppData\Local\Temp\, 23.10.2019 16.27.01 +00:00)
CreateEvent(C:\Users\\<i>&lt;user&gt;</i>\\AppData\Local\Temp\, 23.10.2019 16.27.01 +00:00, file.dat)
ChangeEvent(C:\Users\\<i>&lt;user&gt;</i>\\AppData\Local\Temp\, 23.10.2019 16.27.01 +00:00, file.dat)
DeleteEvent(C:\Users\\<i>&lt;user&gt;</i>\\AppData\Local\Temp\, 23.10.2019 16.27.01 +00:00, file.dat)
OnCompleted
```


# Disposing

Disposable objects can be attached to be disposed along with *FileSystem*.
[!code-csharp[Snippet](Examples.cs#Snippet_10a)]

Delegates can be attached to be executed at dispose of *FileSystem*.
[!code-csharp[Snippet](Examples.cs#Snippet_10b)]

**.BelateDispose()** creates a handle that postpones dispose on *.Dispose()*. Actual dispose will proceed once *.Dispose()* is called and
all belate handles are disposed. This can be used for passing the *IFileSystem* to a worker thread. 
[!code-csharp[Snippet](Examples.cs#Snippet_10c)]
