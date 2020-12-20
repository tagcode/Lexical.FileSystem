# Operation

The namespace *Lexical.FileSystem.Operation* contains file operation classes.

**IOperation** is ran inside a context of *IOperationSession*. 
[!code-csharp[Snippet](Examples.cs#Snippet_1)]

<pre style="line-height:1.2;">
""
├── "file" [1048576]
└── "file.copy" [1048576]
</pre>

<i>IOperation</i>**.Estimate()** makes checks to test if the operation can be completed. 
It throws exception if there is a visible reason why the operation would not complete.
[!code-csharp[Snippet](Examples.cs#Snippet_1b)]

After <i>.Estimate()</i> is called, the property **.CanRollback** is set. It determines if the operation can be reverted.
[!code-csharp[Snippet](Examples.cs#Snippet_1b1)]

**.Estimate().AssertCanRollback()** asserts rollback is possible.
[!code-csharp[Snippet](Examples.cs#Snippet_1b2)]

**.Run()** executes the operation. It throws an exception on unexpected error.
[!code-csharp[Snippet](Examples.cs#Snippet_1c)]

The caller should test the **.CurrentState** property after *.Run()* to see what the state is.
[!code-csharp[Snippet](Examples.cs#Snippet_1c1)]

... or use **.Run().AssertSuccessful()** to assert it.
[!code-csharp[Snippet](Examples.cs#Snippet_1c2)]

Caller can try rollback upon failure (or anyway).
[!code-csharp[Snippet](Examples.cs#Snippet_1c3)]

Executing **.Run(<i>rollbackOnError</i>)** rollbacks automatically on failure if rollback is possible.
[!code-csharp[Snippet](Examples.cs#Snippet_1c4)]

**CopyTree** copies a directory tree from one place to another.
[!code-csharp[Snippet](Examples.cs#Snippet_2)]

<pre style="line-height:1.2;">
""
├── "dir"
│  └── "dir"
│     └── "dir"
│        └── "file"
└── "dir.copy"
   └── "dir"
      └── "dir"
         └── "file"
</pre>

**Batch** is a list of operations to run as one operation.
[!code-csharp[Snippet](Examples.cs#Snippet_3)]

**TransferTree** moves files from one location to another by copy-and-delete.
[!code-csharp[Snippet](Examples.cs#Snippet_4)]

Session gathers events automatically. Gathered evants can be read from the session object.
[!code-csharp[Snippet](Examples.cs#Snippet_5)]

```none
CopyFile(Src=file, Dst=file.copy, State=Completed) = Estimating
CopyFile(Src=file, Dst=file.copy, State=Completed) = Estimated
CopyFile(Src=file, Dst=file.copy, State=Completed) = Running
CopyFile(Src=file, Dst=file.copy, State=Completed) = Completed
```

Events can subscribed. Copy operation can be configured to send regular progress events. **.SetProgressInterval(<i>long</i>)** sets the interval on how often *Operation.Event.Progress* is sent in terms of bytes.
[!code-csharp[Snippet](Examples.cs#Snippet_6)]

[!code-csharp[Snippet](Examples.cs#OpEventPrinter)]

```none
CopyFile(Src=file, Dst=file.copy, State=Estimating) = Estimating
CopyFile(Src=file, Dst=file.copy, State=Estimated) = Estimated
CopyFile(Src=file, Dst=file.copy, State=Running) = Running
Progress(CopyFile(Src=file, Dst=file.copy, State=Running), 20%)
Progress(CopyFile(Src=file, Dst=file.copy, State=Running), 40%)
Progress(CopyFile(Src=file, Dst=file.copy, State=Running), 60%)
Progress(CopyFile(Src=file, Dst=file.copy, State=Running), 80%)
Progress(CopyFile(Src=file, Dst=file.copy, State=Running), 100%)
CopyFile(Src=file, Dst=file.copy, State=Completed) = Completed
OnCompleted
```

<pre style="line-height:1.2;">
""
├── "file"
└── "file.copy"
</pre>

If cancellation token is canceled, the operation does not proceed.
[!code-csharp[Snippet](Examples.cs#Snippet_7)]
