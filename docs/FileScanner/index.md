# FileScanner
**FileScanner** scans the tree structure of a filesystem for files that match its configured criteria. It uses concurrent threads.
[!code-csharp[Snippet](Examples.cs#Snippet_1a)]

*FileScanner* needs to be populated with at least one filter, such as wildcard pattern, **.AddWildcard(*string*)**.
[!code-csharp[Snippet](Examples.cs#Snippet_1b)]

Or regular expression.
[!code-csharp[Snippet](Examples.cs#Snippet_1c)]

Or glob pattern.
[!code-csharp[Snippet](Examples.cs#Snippet_1d)]

The initial start path is extracted from the pattern.
[!code-csharp[Snippet](Examples.cs#Snippet_1d2)]

Search is started when **IEnumerator&lt;*IEntry*&gt;** is enumerated from the scanner.
[!code-csharp[Snippet](Examples.cs#Snippet_1e)]

Exceptions that occur at real-time can be captured into concurrent collection.
[!code-csharp[Snippet](Examples.cs#Snippet_1f)]

The property **.ReturnDirectories** determines whether scanner returns directories.
[!code-csharp[Snippet](Examples.cs#Snippet_1g)]

The property **.SetDirectoryEvaluator(<i>Func&lt;IEntry, bool&gt; func</i>)** sets a criteria that determines whether scanner enters a directory.
[!code-csharp[Snippet](Examples.cs#Snippet_1h)]
