# FileScanner
**FileScanner** scans the tree structure of a filesystem for files that match its configured criteria. It uses concurrent threads.

```csharp
IFileSystem fs = new MemoryFileSystem();
fs.CreateDirectory("myfile.zip/folder");
fs.CreateFile("myfile.zip/folder/somefile.txt");

FileScanner filescanner = new FileScanner(fs);
```

*FileScanner* needs to be populated with at least one filter, such as wildcard pattern, **.AddWildcard(*string*)**.

```csharp
filescanner.AddWildcard("*.zip");
```

Or regular expression.

```csharp
filescanner.AddRegex(path: "", pattern: new Regex(@".*\.zip"));
```

Or glob pattern.

```csharp
filescanner.AddGlobPattern("**.zip/**.txt");
```

The initial start path is extracted from the pattern.

```csharp
filescanner.AddGlobPattern("myfile.zip/**.txt");
```

Search is started when **IEnumerator&lt;*IEntry*&gt;** is enumerated from the scanner.

```csharp
foreach (IEntry entry in filescanner)
{
    Console.WriteLine(entry.Path);
}
```

Exceptions that occur at real-time can be captured into concurrent collection.

```csharp
// Collect errors
filescanner.errors = new ConcurrentBag<Exception>();
// Run scan
IEntry[] entries = filescanner.ToArray();
// View errors
foreach (Exception e in filescanner.errors) Console.WriteLine(e);
```

The property **.ReturnDirectories** determines whether scanner returns directories.

```csharp
filescanner.ReturnDirectories = true;
```

The property **.SetDirectoryEvaluator(<i>Func&lt;IEntry, bool&gt; func</i>)** sets a criteria that determines whether scanner enters a directory.

```csharp
filescanner.SetDirectoryEvaluator(e => e.Name != "tmp");
```
