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


