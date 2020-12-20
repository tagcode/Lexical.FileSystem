# Decoration

The <i>IFileSystems</i><b>.Decorate(<i>IOption</i>)</b> extension method decorates a filesystem with new decorated options. 
Decoration options is an intersection of filesystem's options and the options in the parameters, so decoration reduces features.

```csharp
IFileSystem ram = new MemoryFileSystem();
IFileSystem rom = ram.Decorate(Option.ReadOnly);
```

<i>IFileSystems</i><b>.AsReadOnly()</b> is same as <i>IFileSystems.Decorate(Option.ReadOnly)</i>.

```csharp
IFileSystem rom = ram.AsReadOnly();
```

**Option.NoBrowse** prevents browsing, hiding files.

```csharp
IFileSystem invisible = ram.Decorate(Option.NoBrowse);
```

**Option.SubPath(<i>subpath</i>)** option exposes only a subtree of the decorated filesystem. 
The *subpath* argument must end with slash "/", or else it mounts filename prefix, e.g. "tmp-".


```csharp
IFileSystem ram = new MemoryFileSystem();
ram.CreateDirectory("tmp/dir/");
ram.CreateFile("tmp/dir/file.txt", new byte[] { 32,32,32,32,32,32,32,32,32 });

IFileSystem tmp = ram.Decorate(Option.SubPath("tmp/"));
tmp.PrintTo(Console.Out, format: PrintTree.Format.DefaultPath);
```
<pre style="line-height:1.2;">

└── dir/
   └── dir/file.txt
</pre>

**.AddSourceToBeDisposed()** adds source objects to be disposed along with the decoration.

```csharp
MemoryFileSystem ram = new MemoryFileSystem();
IFileSystemDisposable rom = ram.Decorate(Option.ReadOnly).AddSourceToBeDisposed();
// Do work ...
rom.Dispose();
```

Decorations implement **IDisposeList** and **IBelatableDispose** which allows to attach disposable objects.

```csharp
MemoryFileSystem ram = new MemoryFileSystem();
ram.CreateDirectory("tmp/dir/");
ram.CreateFile("tmp/dir/file.txt", new byte[] { 32, 32, 32, 32, 32, 32, 32, 32, 32 });
IFileSystemDisposable rom = ram.Decorate(Option.ReadOnly).AddDisposable(ram);
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
IFileSystemDisposable rom = ram.Decorate(Option.ReadOnly).AddDisposable(ram.BelateDispose());
IFileSystemDisposable tmp = ram.Decorate(Option.SubPath("tmp/")).AddDisposable(ram.BelateDispose());
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

IFileSystem composition = FileSystemExtensions.Concat(ram, os, fp, embedded)
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
IFileSystem composition = FileSystemExtensions.Concat(ram1, ram2);

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

<b>FileSystems.Concat(<i>(IFileSystem, IOption)[]</i>)</b> applies options to the filesystems.

```csharp
IFileSystem filesystem = FileSystem.Application;
IFileSystem overrides = new MemoryFileSystem();
IFileSystem composition = FileSystemExtensions.Concat(
    (filesystem, null), 
    (overrides, Option.ReadOnly)
);
```
