# HttpFileSystem
**new HttpFileSystem(<i>HttpClient, IOption</i>)** creates a new http based filesystem.
[!code-csharp[Snippet](Examples.cs#Snippet_1)]

**HttpFileSystem.Instance** is the default singleton instance.
[!code-csharp[Snippet](Examples.cs#Snippet_2)]

Opening a resource with **FileMode.Open** and **FileAccess.Read** parameters makes a GET request.
[!code-csharp[Snippet](Examples.cs#Snippet_3)]

Web resources can be used with generic extension methods such as **.CopyFile()**.
[!code-csharp[Snippet](Examples.cs#Snippet_4)]

<pre style="line-height:1.2;">
""
└── "document.txt"
</pre>

Opening a resource with **FileMode.Create** and **FileAccess.Write** makes a PUT request.
[!code-csharp[Snippet](Examples.cs#Snippet_5)]

HttpFileSystem can be constructed with various options, such as SubPath and custom http header.
[!code-csharp[Snippet](Examples.cs#Snippet_6)]

User authentication header **AuthenticationHeaderValue** can be wrapped in **Token** and passed to *Open()* method.
[!code-csharp[Snippet](Examples.cs#Snippet_7)]

Another way is to pass user authentication token at construction of *HttpFileSystem*. 
The token must be given glob patterns where the token applies, for example "http://lexical.fi/FileSystem/private/**".
[!code-csharp[Snippet](Examples.cs#Snippet_8)]

Third way is to pass authentication token into a decoration. 
[!code-csharp[Snippet](Examples.cs#Snippet_9)]

**.Delete(<i>uri</i>)** sends DELETE http request.
[!code-csharp[Snippet](Examples.cs#Snippet_10)]

**.Browse(<i>uri</i>)** reads html document and parses links that refer to immediate child files and directories.
[!code-csharp[Snippet](Examples.cs#Snippet_11a)]

**.GetEntry(<i>uri</i>)** reads resource headers and returns entry.
[!code-csharp[Snippet](Examples.cs#Snippet_11b)]

File system can be scanned with *.VisitTree()* and *.PrintTo()* extension methods.
[!code-csharp[Snippet](Examples.cs#Snippet_12)]

<pre style="line-height:1.2;">
"private"
├── "Directory"
│  └── "file.txt"
├── "Folder"
│  └── "file.txt"
└── "document.txt"
</pre>

On github too. Notice that, only directories are returned from "/tree/", as files are on different url branch "/blob/".
[!code-csharp[Snippet](Examples.cs#Snippet_13)]

<pre style="line-height:1.2;">
"master"
├── "Lexical.FileSystem"
│  ├── "Decoration"
│  ├── "Extensions"
│  ├── "Internal"
│  ├── "Package"
│  └── "Utility"
└── "Lexical.FileSystem.Abstractions"
   ├── "Extensions"
   ├── "FileProvider"
   ├── "Internal"
   ├── "Option"
   ├── "Package"
   └── "Utility"
</pre>

*CancellationToken* can be passed to break-up operation.
[!code-csharp[Snippet](Examples.cs#Snippet_14)]

> [!NOTE]
> HttpFileSystem doesn't support stream seeking. It can only stream content.
