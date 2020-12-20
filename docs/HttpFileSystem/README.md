# HttpFileSystem
**new HttpFileSystem(<i>HttpClient, IOption</i>)** creates a new http based filesystem.

```csharp
IFileSystem fs = new HttpFileSystem(httpClient: default, option: default);
```

**HttpFileSystem.Instance** is the default singleton instance.

```csharp
IFileSystem fs = HttpFileSystem.Instance;
```

Opening a resource with **FileMode.Open** and **FileAccess.Read** parameters makes a GET request.

```csharp
using (var s = HttpFileSystem.Instance.Open("http://lexical.fi/", FileMode.Open, FileAccess.Read, FileShare.None))
{
    byte[] data = StreamUtils.ReadFully(s);
    String str = UTF8Encoding.UTF8.GetString(data);
    Console.WriteLine(str);
}
```

Web resources can be used with generic extension methods such as **.CopyFile()**.

```csharp
MemoryFileSystem ram = new MemoryFileSystem();
HttpFileSystem.Instance.CopyFile("http://lexical.fi", ram, "document.txt");
ram.PrintTo(Console.Out);
```

<pre style="line-height:1.2;">
""
└── "document.txt"
</pre>

Opening a resource with **FileMode.Create** and **FileAccess.Write** makes a PUT request.

```csharp
byte[] data = new byte[1024];
using (var s = HttpFileSystem.Instance.Open("http://lexical.fi/", FileMode.Create, FileAccess.Write, FileShare.None))
    s.Write(data);
```

HttpFileSystem can be constructed with various options, such as SubPath and custom http header.

```csharp
// Create options
List<KeyValuePair<string, IEnumerable<string>>> headers = new List<KeyValuePair<string, IEnumerable<string>>>();
headers.Add(new KeyValuePair<string, IEnumerable<string>>("User-Agent", new String[] { "MyUserAgent" }));
IOption option1 = new Token(headers, typeof(System.Net.Http.Headers.HttpHeaders).FullName);
IOption option2 = Option.SubPath("http://lexical.fi/");
IOption options = Option.Union(option1, option2);

// Create FileSystem
IFileSystem fs = new HttpFileSystem(option: options);

// Read resource
using (var s = fs.Open("index.html", FileMode.Open, FileAccess.Read, FileShare.None))
{
    byte[] data = StreamUtils.ReadFully(s);
    String str = UTF8Encoding.UTF8.GetString(data);
    Console.WriteLine(str);
}
```

User authentication header **AuthenticationHeaderValue** can be wrapped in **Token** and passed to *Open()* method.

```csharp
AuthenticationHeaderValue authentication = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes($"webuser:webpassword")));
IToken token = new Token(authentication, typeof(AuthenticationHeaderValue).FullName);
using (var s = HttpFileSystem.Instance.Open("https://lexical.fi/FileSystem/private/document.txt", FileMode.Open, FileAccess.Read, FileShare.None, token))
{
    byte[] data = new byte[4096];
    int c = s.Read(data, 0, 1024);
    String str = UTF8Encoding.UTF8.GetString(data, 0, c);
    Console.WriteLine(str);
}
```

Another way is to pass user authentication token at construction of *HttpFileSystem*. 
The token must be given glob patterns where the token applies, for example "http://lexical.fi/FileSystem/private/**".

```csharp
// Authentication header
AuthenticationHeaderValue authentication = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes($"webuser:webpassword")));

// Token
IToken token = new Token(
    authentication, 
    typeof(AuthenticationHeaderValue).FullName, 
    "https://lexical.fi/FileSystem/private/**",
    "https://www.lexical.fi/FileSystem/private/**"
);

// Create FileSystem
IFileSystem fs = new HttpFileSystem(default, token);

// Open
using (var s = fs.Open("https://lexical.fi/FileSystem/private/document.txt", FileMode.Open, FileAccess.Read, FileShare.None))
{
    byte[] data = new byte[4096];
    int c = s.Read(data, 0, 1024);
    String str = UTF8Encoding.UTF8.GetString(data, 0, c);
    Console.WriteLine(str);
}
```

Third way is to pass authentication token into a decoration. 

```csharp
// Authentication header
AuthenticationHeaderValue authentication = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes($"webuser:webpassword")));

// Create token
IToken token = new Token(authentication, typeof(AuthenticationHeaderValue).FullName, "https://lexical.fi/FileSystem/private/**");

// Pass token into decorator
IFileSystem decoration = HttpFileSystem.Instance.Decorate(token);

// Open
using (var s = decoration.Open("https://lexical.fi/FileSystem/private/document.txt", FileMode.Open, FileAccess.Read, FileShare.None))
{
    byte[] data = new byte[4096];
    int c = s.Read(data, 0, 1024);
    String str = UTF8Encoding.UTF8.GetString(data, 0, c);
    Console.WriteLine(str);
}
```

**.Delete(<i>uri</i>)** sends DELETE http request.

```csharp
HttpFileSystem.Instance.Delete("https://lexical.fi/FileSystem/private/document.txt");
```

**.Browse(<i>uri</i>)** reads html document and parses links that refer to immediate child files and directories.

```csharp
var authBlob = Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes($"webuser:webpassword"));
var authentication = new AuthenticationHeaderValue("Basic", authBlob);
var token = new Token(authentication, typeof(AuthenticationHeaderValue).FullName, "https://lexical.fi/FileSystem/private/**");

IEnumerable<IEntry> entries = HttpFileSystem.Instance.Browse("https://lexical.fi/FileSystem/private/", token);
```

**.GetEntry(<i>uri</i>)** reads resource headers and returns entry.

```csharp
var authBlob = Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes($"webuser:webpassword"));
var authentication = new AuthenticationHeaderValue("Basic", authBlob);
var token = new Token(authentication, typeof(AuthenticationHeaderValue).FullName, "https://lexical.fi/FileSystem/private/**");

IEntry entry = HttpFileSystem.Instance.GetEntry("https://lexical.fi/FileSystem/private/document.txt", token);
```

File system can be scanned with *.VisitTree()* and *.PrintTo()* extension methods.

```csharp
var authBlob = Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes($"webuser:webpassword"));
var authentication = new AuthenticationHeaderValue("Basic", authBlob);
var token = new Token(authentication, typeof(AuthenticationHeaderValue).FullName, "https://lexical.fi/FileSystem/private/**");

HttpFileSystem.Instance.PrintTo(Console.Out, "https://lexical.fi/FileSystem/private/", option: token);
```

<pre style="line-height:1.2;">
"private"
├── "Directory"
│  └── "file.txt"
├── "Folder"
│  └── "file.txt"
└── "document.txt"
</pre>

On github too. Notice that, only directories are returned from "/tree/", as files are on different url branch "/blob/".

```csharp
HttpFileSystem.Instance.PrintTo(Console.Out, "https://github.com/tagcode/Lexical.FileSystem/tree/master/");
```

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

```csharp
// Cancel token
CancellationTokenSource cancelSrc = new CancellationTokenSource();
IToken token = new Token(cancelSrc.Token, typeof(CancellationToken).FullName);

// Set canceled
cancelSrc.Cancel();

// Read
HttpFileSystem.Instance.Open("http://lexical.fi/", FileMode.Open, FileAccess.Read, FileShare.None, token);
```

