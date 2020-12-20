# Decoration

The <i>IFileSystems</i><b>.Decorate(<i>IOption</i>)</b> extension method decorates a filesystem with new decorated options. 
Decoration options is an intersection of filesystem's options and the options in the parameters, so decoration reduces features.
[!code-csharp[Snippet](Decoration_Examples.cs#Snippet_1a)]

<i>IFileSystems</i><b>.AsReadOnly()</b> is same as <i>IFileSystems.Decorate(Option.ReadOnly)</i>.
[!code-csharp[Snippet](Decoration_Examples.cs#Snippet_1b)]

**Option.NoBrowse** prevents browsing, hiding files.
[!code-csharp[Snippet](Decoration_Examples.cs#Snippet_2b)]

**Option.SubPath(<i>subpath</i>)** option exposes only a subtree of the decorated filesystem. 
The *subpath* argument must end with slash "/", or else it mounts filename prefix, e.g. "tmp-".

[!code-csharp[Snippet](Decoration_Examples.cs#Snippet_3)]
<pre style="line-height:1.2;">

└── dir/
   └── dir/file.txt
</pre>

**.AddSourceToBeDisposed()** adds source objects to be disposed along with the decoration.
[!code-csharp[Snippet](Decoration_Examples.cs#Snippet_4a)]

Decorations implement **IDisposeList** and **IBelatableDispose** which allows to attach disposable objects.
[!code-csharp[Snippet](Decoration_Examples.cs#Snippet_4b)]

If multiple decorations are used, the source reference can be 'forgotten' after construction if belate dispose handles are passed over to decorations.
[!code-csharp[Snippet](Decoration_Examples.cs#Snippet_4c)]

# Concat
<b>FileSystems.Concat(<i>IFileSystem[]</i>)</b> method composes IFileSystem instances into one.
[!code-csharp[Snippet](Composition_Examples.cs#Snippet_1)]

Composed set of files can be browsed.
[!code-csharp[Snippet](Composition_Examples.cs#Snippet_2)]

Files can be read from the composed set.
[!code-csharp[Snippet](Composition_Examples.cs#Snippet_3)]

If two files have same name and path, the file in the first *IFileSystem* overshadows files from later *IFileSystem*s.
[!code-csharp[Snippet](Composition_Examples.cs#Snippet_4)]

<pre style="line-height:1.2;">
""
└── "file.txt" 1024
</pre>

<b>FileSystems.Concat(<i>(IFileSystem, IOption)[]</i>)</b> applies options to the filesystems.
[!code-csharp[Snippet](Composition_Examples.cs#Snippet_5)]
