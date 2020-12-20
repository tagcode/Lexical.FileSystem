# Operation

The namespace *Lexical.FileSystem.Operation* contains file operation classes.

**IOperation** is ran inside a context of *IOperationSession*. 

```csharp
IFileSystem ms = new MemoryFileSystem();
ms.CreateFile("file", new byte[1024*1024]);

using(var s = new OperationSession())
{
    new CopyFile(s, ms, "file", ms, "file.copy")
        .Estimate()
        .AssertCanRollback()
        .Run()
        .AssertSuccessful();
}
```

<pre style="line-height:1.2;">
""
├── "file" [1048576]
└── "file.copy" [1048576]
</pre>

<i>IOperation</i>**.Estimate()** makes checks to test if the operation can be completed. 
It throws exception if there is a visible reason why the operation would not complete.

```csharp
var s = new OperationSession();
var op = new CopyFile(s, ms, "file", ms, "file.copy");
op.Estimate();
```

After <i>.Estimate()</i> is called, the property **.CanRollback** is set. It determines if the operation can be reverted.

```csharp
Console.WriteLine(op.CanRollback);
```

**.Estimate().AssertCanRollback()** asserts rollback is possible.

```csharp
op.Estimate().AssertCanRollback();
```

**.Run()** executes the operation. It throws an exception on unexpected error.

```csharp
op.Run();
```

The caller should test the **.CurrentState** property after *.Run()* to see what the state is.

```csharp
if (op.CurrentState == OperationState.Completed) 
    Console.WriteLine("ok");
```

... or use **.Run().AssertSuccessful()** to assert it.

```csharp
op.Run().AssertSuccessful();
```

Caller can try rollback upon failure (or anyway).

```csharp
try
{
    op.Run();
}
catch (Exception)
{
    // Rollback
    op.CreateRollback()?.Run();
}
```

Executing **.Run(<i>rollbackOnError</i>)** rollbacks automatically on failure if rollback is possible.

```csharp
op.Run(rollbackOnError: true);
```

**CopyTree** copies a directory tree from one place to another.

```csharp
IFileSystem ms = new MemoryFileSystem();
ms.CreateDirectory("dir/dir/dir/");
ms.CreateFile("dir/dir/dir/file", new byte[1024 * 1024]);

using (var s = new OperationSession())
{
    var op = new CopyTree(s, ms, "dir", ms, "dir.copy");

    op.Estimate().AssertCanRollback().Run().AssertSuccessful();
}
```

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

```csharp
IFileSystem ms = new MemoryFileSystem();                
using (var s = new OperationSession(policy: OperationPolicy.EstimateOnRun/*important*/))
{
    Batch batch = new Batch(s, OperationPolicy.Unset);

    batch.Ops.Add(new CreateDirectory(s, ms, "dir/dir/dir"));
    batch.Ops.Add(new CopyTree(s, ms, "dir", ms, "dir.copy"));
    batch.Ops.Add(new Delete(s, ms, "dir/dir", true, policy: OperationPolicy.EstimateOnRun));
    batch.Estimate().Run().AssertSuccessful();
}
```

**TransferTree** moves files from one location to another by copy-and-delete.

```csharp
IFileSystem ms_src = new MemoryFileSystem();
ms_src.CreateDirectory("dir/dir/dir/");
ms_src.CreateFile("dir/dir/dir/file", new byte[1024 * 1024]);

IFileSystem ms_dst = new MemoryFileSystem();

using (var s = new OperationSession())
{
    var op = new TransferTree(s, ms_src, "dir", ms_dst, "dir.elsewhere");
    op.Estimate().AssertCanRollback().Run().AssertSuccessful();
}
```

Session gathers events automatically. Gathered evants can be read from the session object.

```csharp
IFileSystem ms = new MemoryFileSystem();
ms.CreateFile("file", new byte[1024 * 1024]);

using (var s = new OperationSession())
{
    new CopyFile(s, ms, "file", ms, "file.copy")
        .Estimate()
        .AssertCanRollback()
        .Run()
        .AssertSuccessful();

    foreach (var @event in s.Events)
        Console.WriteLine(@event);
}
ms.PrintTo(Console.Out);
```

```none
CopyFile(Src=file, Dst=file.copy, State=Completed) = Estimating
CopyFile(Src=file, Dst=file.copy, State=Completed) = Estimated
CopyFile(Src=file, Dst=file.copy, State=Completed) = Running
CopyFile(Src=file, Dst=file.copy, State=Completed) = Completed
```

Events can subscribed. Copy operation can be configured to send regular progress events. **.SetProgressInterval(<i>long</i>)** sets the interval on how often *Operation.Event.Progress* is sent in terms of bytes.

```csharp
IFileSystem ms = new MemoryFileSystem();
ms.CreateFile("file", new byte[1024 * 20]);

using (var s = new OperationSession().SetProgressInterval(1024))
{
    s.Subscribe(new OpEventPrinter());

    new CopyFile(s, ms, "file", ms, "file.copy")
        .Estimate()
        .AssertCanRollback()
        .Run()
        .AssertSuccessful();
}
ms.PrintTo(Console.Out);
```


```csharp
class OpEventPrinter : IObserver<IOperationEvent>
{
    public void OnCompleted() => Console.WriteLine("OnCompleted");
    public void OnError(Exception error) => Console.WriteLine(error);
    public void OnNext(IOperationEvent @event) => Console.WriteLine(@event);
}
```

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

```csharp
IFileSystem ms = new MemoryFileSystem();
ms.CreateFile("file", new byte[1024 * 10]);
CancellationTokenSource cancelSrc = new CancellationTokenSource();

using (var s = new OperationSession(cancelSrc: cancelSrc))
{
    cancelSrc.Cancel();
    new Move(s, ms, "file", ms, "file.moved")
        .Estimate()
        .AssertCanRollback()
        .Run()
        .AssertSuccessful();
}
```
