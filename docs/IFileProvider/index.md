# IFileProvider 

There are decorating adapters to and from **IFileProvider** instances.

To use *IFileProvider* decorations, the calling assembly must import the **Microsoft.Extensions.FileProviders.Abstractions** assembly.

## To IFileSystem
The extension method <i>IFileProvider</i><b>.ToFileSystem()</b> adapts *IFileProvider* into *IFileSystem*.
[!code-csharp[Snippet](FileProviderSystem_Examples.cs#Snippet_1)]

Parameters <b>.ToFileSystem(*bool canBrowse, bool canObserve, bool canOpen*)</b> can be used for limiting the capabilities of the adapted *IFileSystem*.
[!code-csharp[Snippet](FileProviderSystem_Examples.cs#Snippet_2)]

**.AddDisposable(<i>object</i>)** attaches a disposable to be disposed along with the *IFileSystem* adapter.
[!code-csharp[Snippet](FileProviderSystem_Examples.cs#Snippet_3)]

**.AddDisposeAction()** attaches a delegate to be ran at dispose. It can be used for disposing the source *IFileProvider*.
[!code-csharp[Snippet](FileProviderSystem_Examples.cs#Snippet_4)]

**.BelateDispose()** creates a handle that postpones dispose on *.Dispose()*. Actual dispose will proceed once *.Dispose()* is called and
all belate handles are disposed. This can be used for passing the *IFileSystem* to a worker thread. 
[!code-csharp[Snippet](FileProviderSystem_Examples.cs#Snippet_5)]

The adapted *IFileSystem* can be used as any filesystem that has Open(), Browse() and Observe() features.
[!code-csharp[Snippet](FileProviderSystem_Examples.cs#Snippet_6)]

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
[!code-csharp[Snippet](FileProviderSystem_Examples.cs#Snippet_7)]

> [!WARNING]
> Note that, observing a IFileProvider through IFileSystem adapter browses
> the subtree of the source IFileProvider and compares snapshots
> in order to produce change events. If observer uses "**" pattern, it will
> browse through the whole IFileProvider.

## To IFileProvider
<i>*IFileSystem*</i><b>.ToFileProvider()</b> adapts *IFileProvider* into *IFileSystem*.
[!code-csharp[Snippet](FileSystemProvider_Examples.cs#Snippet_1)]

**.AddDisposable(<i>object</i>)** attaches a disposable to be disposed along with the *IFileProvider* adapter.
[!code-csharp[Snippet](FileSystemProvider_Examples.cs#Snippet_3)]

**.AddDisposeAction()** attaches a delegate to be ran at dispose. It can be used for disposing the source *IFileSystem*.
[!code-csharp[Snippet](FileSystemProvider_Examples.cs#Snippet_4)]

**.BelateDispose()** creates a handle that postpones dispose on *.Dispose()*. Actual dispose will proceed once *.Dispose()* is called and
all belate handles are disposed. This can be used for passing the *IFileProvider* to a worker thread. 
[!code-csharp[Snippet](FileSystemProvider_Examples.cs#Snippet_5)]

The adapted *IFileProvider* can be used as any fileprovider that can *GetDirectoryContents()*, *GetFileInfo()*, and *Watch()*.
[!code-csharp[Snippet](FileSystemProvider_Examples.cs#Snippet_6)]

<pre style="line-height:1.2;">
C:
D:
E:
</pre>

**.Watch()** attaches a watcher.
[!code-csharp[Snippet](FileSystemProvider_Examples.cs#Snippet_7)]


