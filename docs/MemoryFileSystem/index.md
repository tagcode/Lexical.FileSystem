# MemoryFileSystem

**MemoryFileSystem** is a memory based filesystem.
[!code-csharp[Snippet](Examples.cs#Snippet_1)]

Files are based on blocks. Maximum number of blocks is 2^31-1. The <i>blockSize</i> can be set in constructor. The default blocksize is 1024. 
[!code-csharp[Snippet](Examples.cs#Snippet_1b)]

Files can be browsed.
[!code-csharp[Snippet](Examples.cs#Snippet_2)]

Files can be opened for reading.
[!code-csharp[Snippet](Examples.cs#Snippet_3a)]

And for writing.
[!code-csharp[Snippet](Examples.cs#Snippet_3b)]

Files and directories can be observed for changes.
[!code-csharp[Snippet](Examples.cs#Snippet_4)]

Directories can be created.
[!code-csharp[Snippet](Examples.cs#Snippet_5)]

Directories can be created recursively. 
[!code-csharp[Snippet](Examples.cs#Snippet_5a)]

The root is "".
<pre style="line-height:1.2;">
""
└──"dir1"
   └──"dir2"
      └──"dir3"
</pre>

*MemoryFileSystem* can create empty directory names. For example, a slash '/' at the start of a path refers to an empty directory right under the root.
[!code-csharp[Snippet](Examples.cs#Snippet_5b)]

<pre style="line-height:1.2;">
""
└──""
   └──"tmp"
      └──"dir"
</pre>

Path "file://" refers to three directories; the root, "file:" and a empty-named directory between two slashes "//".
[!code-csharp[Snippet](Examples.cs#Snippet_5d)]

<pre style="line-height:1.2;">
""
└──"file:"
   └──""
</pre>

Directories can be deleted.
[!code-csharp[Snippet](Examples.cs#Snippet_6)]

Files and directories can be renamed and moved.
[!code-csharp[Snippet](Examples.cs#Snippet_7)]


# Disposing

Disposable objects can be attached to be disposed along with *FileSystem*.
[!code-csharp[Snippet](Examples.cs#Snippet_10a)]

Delegates can be attached to be executed at dispose of *FileSystem*.
[!code-csharp[Snippet](Examples.cs#Snippet_10b)]

**.BelateDispose()** creates a handle that postpones dispose on *.Dispose()*. Actual dispose proceeds once *.Dispose()* is called and
all belate handles are disposed. This can be used for passing the *IFileSystem* to worker threads. 
[!code-csharp[Snippet](Examples.cs#Snippet_10c)]

# Size Limit

Constructor **new MemoryFileSystem(<i>blockSize</i>, <i>maxSpace</i>)** creates size limited filesystem. Memory limitation applies to files only, not to directory structure.
[!code-csharp[Snippet](Examples.cs#Snippet_20a)]

Printing with **PrintTree.Format.DriveFreespace | PrintTree.Format.DriveSize** flags show drive size.
[!code-csharp[Snippet](Examples.cs#Snippet_20b)]

```none
"" [Freespace: 15G, Size: 1G/16G, Ram]
└── "file" [1073741824]
```

If filesystem runs out of space, it throws **FileSystemExceptionOutOfDiskSpace**.
[!code-csharp[Snippet](Examples.cs#Snippet_20c)]

Available space can be shared between *MemoryFileSystem* instances with **IBlockPool**.
[!code-csharp[Snippet](Examples.cs#Snippet_20d)]

Deleted file is returned back to pool once all open streams are closed.
[!code-csharp[Snippet](Examples.cs#Snippet_20e)]
