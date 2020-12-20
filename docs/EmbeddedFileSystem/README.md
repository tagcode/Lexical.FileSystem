# EmbeddedFileSystem
EmbeddedFileSystem is a file system to the embedded resources of an assembly.

```csharp
IFileSystem filesystem = new EmbeddedFileSystem(typeof(Program).Assembly);
```

Embedded resources can be browsed.

```csharp
foreach (var entry in filesystem.Browse(""))
    Console.WriteLine(entry.Path);
```

Embedded resources can be read.

```csharp
using(Stream s = filesystem.Open("docs.example-file.txt", FileMode.Open, FileAccess.Read, FileShare.Read))
{
    Console.WriteLine(s.Length);
}
```

All the embedded files are flat on the root.

```csharp
filesystem.PrintTo(Console.Out);
```

<pre style="line-height:1.2;">
""
├──"MyAssembly.embedded-file-1.txt"
└──"MyAssembly.embedded-file-2.txt"
</pre>
