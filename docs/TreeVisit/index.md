# VisitTree
The extension method <i>IFileSystem</i><b>.VisitTree(string path, int depth)</b> visits a tree structure of a filesystem.

[!code-csharp[Snippet](TreeVisit_Examples.cs#Snippet_1)]

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
[!code-csharp[Snippet](TreeVisit_Examples.cs#Snippet_2)]

<pre style="line-height:1.2;">
""
├── ""
├── "c:"
└── "file:"
</pre>

Parameter *path* determines start location.
[!code-csharp[Snippet](TreeVisit_Examples.cs#Snippet_3)]

<pre style="line-height:1.2;">
"tmp"
└── "helloworld.txt"
</pre>

# PrintTree

The extension method <i>IFileSystem</i><b>.PrintTo(TextWriter output, string path, int depth, Format format)</b> prints a tree structure 
to a *TextWriter* such as **Console.Out** (*stdout*).

[!code-csharp[Snippet](PrintTree_Examples.cs#Snippet_1)]

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
[!code-csharp[Snippet](PrintTree_Examples.cs#Snippet_2)]

<i>IFileSystem</i><b>.Print(string path, int depth, Format format)</b> prints out to as *string*.
[!code-csharp[Snippet](PrintTree_Examples.cs#Snippet_3)]

Parameter *depth* determines visit depth.
[!code-csharp[Snippet](PrintTree_Examples.cs#Snippet_4)]

<pre style="line-height:1.2;">
""
├── ""
├── "c:"
└── "file:"
</pre>

Parameter *path* determines start location.
[!code-csharp[Snippet](PrintTree_Examples.cs#Snippet_5)]

<pre style="line-height:1.2;">
"tmp"
└── "helloworld.txt"
</pre>

Parameter *format* determines which infos are printed out. For example **PrintTree.Format.Path** prints out full path instead of name.
[!code-csharp[Snippet](PrintTree_Examples.cs#Snippet_6)]

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


