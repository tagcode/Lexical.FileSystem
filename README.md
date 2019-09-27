# Introduction
Lexical.FileSystem is a virtual filesystem class libraries for .NET.

NuGet Packages:
* Lexical.FileSystem ([Website](http://lexical.fi/FileSystem/index.html), [Github](https://github.com/tagcode/Lexical.FileSystem), [Nuget](https://www.nuget.org/packages/Lexical.FileSystem/))
* Lexical.FileSystem.Abstractions ([Github](https://github.com/tagcode/Lexical.FileSystem/tree/master/Lexical.FileSystem.Abstractions), [Nuget](https://www.nuget.org/packages/Lexical.FileSystem.Abstractions/))

# FileSystem

# FileSystem

**new FileSystem(<i>path</i>)** creates an instance to a path in local directory. "" path refers to operating system root.
[!code-csharp[Snippet](Examples.cs#Snippet_1)]

*FileSystem* can be browsed.
[!code-csharp[Snippet](Examples.cs#Snippet_2)]

Files can be opened for reading.
[!code-csharp[Snippet](Examples.cs#Snippet_3a)]

And for for writing.
[!code-csharp[Snippet](Examples.cs#Snippet_3b)]

Files and directories can be observed for changes.
[!code-csharp[Snippet](Examples.cs#Snippet_4)]

Directories can be created.
[!code-csharp[Snippet](Examples.cs#Snippet_5)]

Directories can be deleted.
[!code-csharp[Snippet](Examples.cs#Snippet_6)]

Files and directories can be renamed and moved.
[!code-csharp[Snippet](Examples.cs#Snippet_7)]

Singleton instance **FileSystem.OS** refers to a filesystem at the OS root.
[!code-csharp[Snippet](Examples.cs#Snippet_8a)]

Extension method **.VisitTree()** visits filesystem. On root path "" *FileSystem.OS* returns drive letters.
[!code-csharp[Snippet](Examples.cs#Snippet_8b)]

```none
""
├──"C:"
└──"D:"
```

On linux it returns slash '/' root.
[!code-csharp[Snippet](Examples.cs#Snippet_8c)]

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
[!code-csharp[Snippet](Examples.cs#Snippet_8d)]

**FileSystem.Tmp** refers to the running user's temp directory.
[!code-csharp[Snippet](Examples.cs#Snippet_8e)]

Disposable objects can be attached to be disposed along with *FileSystem*.
[!code-csharp[Snippet](Examples.cs#Snippet_10a)]

Delegates can be attached to be executed at dispose of *FileSystem*.
[!code-csharp[Snippet](Examples.cs#Snippet_10b)]

**.BelateDispose()** creates a handle that postpones dispose on *.Dispose()*. Actual dispose will proceed once *.Dispose()* is called and
all belate handles are disposed. This can be used for passing the *IFileSystem* to a worker thread. 
[!code-csharp[Snippet](Examples.cs#Snippet_10c)]
