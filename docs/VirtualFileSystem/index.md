# VirtualFileSystem

**new VirtualFileSystem()** creates virtual filesystem. Other filesystems can be mounted as part of it.
[!code-csharp[Snippet](Examples.cs#Snippet_1)]

**.Mount(<i>path</i>, <i>filesystem</i>)** assigns a filesystem to a mountpoint.
[!code-csharp[Snippet](Examples.cs#Snippet_2a)]

File systems can be assigned to multiple points.
[!code-csharp[Snippet](Examples.cs#Snippet_2b)]

File system can be assigned as a child of an earlier assignment. Child assignment has higher evaluation priority than parent. In the following example, "/tmp/" is evaluated from **MemoryFileSystem** first, and then concatenated with potential directory "/tmp/" from the **FileSystem.OS**.
[!code-csharp[Snippet](Examples.cs#Snippet_2c)]

**.Unmount(<i>path</i>)** removes filesystem assignments.
[!code-csharp[Snippet](Examples.cs#Snippet_3a)]

Previously assigned filesystem can be replaced.
[!code-csharp[Snippet](Examples.cs#Snippet_3b)]

**.Mount(<i>path</i>, params <i>IFilesystem</i>, <i>IOption</i>)** assigns filesystem with mount option.
[!code-csharp[Snippet](Examples.cs#Snippet_4a)]

Option such as *Option.SubPath()*.
[!code-csharp[Snippet](Examples.cs#Snippet_4d)]

**.Mount(<i>path</i>, params <i>IFilesystem[]</i>)** assigns multiple filesystems into one mountpoint.
[!code-csharp[Snippet](Examples.cs#Snippet_4b)]

**.Mount(<i>path</i>, params (<i>IFilesystem</i>, <i>IOption</i>)[])** assigns multiple filesystems with mount options.
[!code-csharp[Snippet](Examples.cs#Snippet_4c)]

If virtual filesystem is assigned with *null* filesystem, then empty mountpoint is created. Mountpoint cannot be deleted with **.Delete()** method, only remounted or unmounted.
*null* assignment doesn't have any interface capabilities, such as *.Browse()*.
[!code-csharp[Snippet](Examples.cs#Snippet_5a)]

<pre style="line-height:1.2;">
└──/
   └── /tmp/ NotSupportedException: Browse
</pre>

# Observing

Observer can be placed before and after mounting. If observer is placed before, then mounting will notify the observer with *ICreateEvent* event for all the added files.
[!code-csharp[Snippet](Examples.cs#Snippet_6a)]

[!code-csharp[Snippet](Examples.cs#PrintObserver)]

```none
StartEvent(VirtualFileSystem, 19.10.2019 11.34.08 +00:00)
MountEvent(VirtualFileSystem, 19.10.2019 11.34.08 +00:00, , MemoryFileSystem, )
CreateEvent(VirtualFileSystem, 19.10.2019 11.34.08 +00:00, /dir/file.txt)
```

Observer filter can be an intersection of the mounted filesystem's contents.
[!code-csharp[Snippet](Examples.cs#Snippet_6b)]

```none
StartEvent(VirtualFileSystem, 19.10.2019 11.34.23 +00:00)
CreateEvent(VirtualFileSystem, 19.10.2019 11.34.23 +00:00, /dir/file.txt)
```

**.Unmount()** dispatches events of unmounted files as if they were deleted.
[!code-csharp[Snippet](Examples.cs#Snippet_6c)]

```none
Delete(VirtualFileSystem, 19.10.2019 11.34.39 +00:00, /dir/file.txt)
```

If filesystem is mounted with **Option.NoObserve**, then the assigned filesystem cannot be observed, and it won't dispatch events of added files on mount.
[!code-csharp[Snippet](Examples.cs#Snippet_6d)]

Observer isn't closed by unmounting. It can be closed by disposing its handle.
[!code-csharp[Snippet](Examples.cs#Snippet_6e)]

```none
OnCompleted
```

... or by disposing the virtual filesystem.
[!code-csharp[Snippet](Examples.cs#Snippet_6f)]

```none
OnCompleted
```

# Singleton

**VirtualFileSystem.Url** is a singleton instance that has the following filesystems mounted as urls.
[!code-csharp[Snippet](../../../FileSystem.GitHub/Lexical.FileSystem/VirtualFileSystem.cs#doc)]

*VirtualFileSystem.Url* can be read, browsed and written to with its different url schemes.
[!code-csharp[Snippet](Examples.cs#Snippet_12b)]

Application configuration can be placed in "config://ApplicationName/config.ini".
[!code-csharp[Snippet](Examples.cs#Snippet_12c)]

Application's user specific local data can be placed in "data://ApplicationName/data.db".
[!code-csharp[Snippet](Examples.cs#Snippet_12d)]

Application's user documents can be placed in "document://ApplicationName/document".
[!code-csharp[Snippet](Examples.cs#Snippet_12e)]

Program data that is shared with every user can be placed in "program-data://ApplicationName/datafile". These are typically modifiable files.
[!code-csharp[Snippet](Examples.cs#Snippet_12f)]

Application's installed files and binaries are located at "application://". These are typically read-only files.
[!code-csharp[Snippet](Examples.cs#Snippet_12g)]

# Disposing

Disposable objects can be attached to be disposed along with *VirtualFileSystem*.
[!code-csharp[Snippet](Examples.cs#Snippet_10a)]

Delegates can be attached to be executed at dispose of *VirtualFileSystem*.
[!code-csharp[Snippet](Examples.cs#Snippet_10b)]

**.BelateDispose()** creates a handle that postpones dispose on *.Dispose()*. Actual dispose will proceed once *.Dispose()* is called and
all belate handles are disposed. This can be used for passing the *IFileSystem* to a worker thread. 
[!code-csharp[Snippet](Examples.cs#Snippet_10c)]

**.AddMountsToBeDisposed()** Disposes mounted filesystems at the dispose of the *VirtualFileSystem*.
[!code-csharp[Snippet](Examples.cs#Snippet_10d)]
