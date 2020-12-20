# DisposeList
<details>
  <summary><b>IDisposeList</b> contains lists of disposables. (<u>Click here</u>)</summary>

```csharp
/// <summary>
/// Object that can be attached with <see cref="IDisposable"/>.
/// They will be disposed along with the object.
/// </summary>
public interface IDisposeList : IDisposable
{
    /// <summary>
    /// Has Dispose() been called, has dispose started, or has dispose completed.
    /// </summary>
    bool IsDisposeCalled { get; }

    /// <summary>
    /// Has dispose started or completed.
    /// </summary>
    bool IsDisposing { get; }

    /// <summary>
    /// Has dispose completed.
    /// </summary>
    bool IsDisposed { get; }

    /// <summary>
    /// Add <paramref name="disposableObject"/> that is to be disposed along with the called object.
    /// 
    /// If the implementing object has already been disposed, this method immediately disposes the <paramref name="disposableObject"/>.
    /// </summary>
    /// <param name="disposableObject"></param>
    /// <returns>true if was added to list, false if wasn't but was disposed immediately</returns>
    bool AddDisposable(object disposableObject);

    /// <summary>
    /// Add <paramref name="disposableObjects"/> that are going to be disposed along with the called object.
    /// 
    /// If the implementing object has already been disposed, this method immediately disposes the <paramref name="disposableObjects"/>.
    /// </summary>
    /// <param name="disposableObjects"></param>
    /// <returns>true if were added to list, false if were disposed immediately</returns>
    bool AddDisposables(IEnumerable disposableObjects);

    /// <summary>
    /// Remove <paramref name="disposableObject"/> from the list. 
    /// </summary>
    /// <param name="disposableObject"></param>
    /// <returns>true if was removed, false if it wasn't in the list.</returns>
    bool RemoveDisposable(object disposableObject);

    /// <summary>
    /// Remove <paramref name="disposableObjects"/> from the list. 
    /// </summary>
    /// <param name="disposableObjects"></param>
    /// <returns>true if was removed, false if it wasn't in the list.</returns>
    bool RemoveDisposables(IEnumerable disposableObjects);

    /// <summary>
    /// Invoke <paramref name="disposeAction"/> on the dispose of the object.
    /// 
    /// If parent object is disposed or being disposed, the <paramref name="disposeAction"/> is executed immediately.
    /// </summary>
    /// <param name="disposeAction"></param>
    /// <param name="state"></param>
    /// <returns>true if was added to list, false if was disposed right away</returns>
    bool AddDisposeAction(Action<object> disposeAction, object state);
}
```
</details>
<details>
  <summary><b>IBelatableDispose</b> is interface for objects whose dispose can be postponed. (<u>Click here</u>)</summary>

```csharp
/// <summary>
/// Interface for objects whose dispose can be belated. 
/// 
/// Belating is a reference counting mechanism that is based on disposable handles instead of reference.
/// </summary>
public interface IBelatableDispose : IDisposable
{
    /// <summary>
    /// Post-pone dispose. 
    /// 
    /// Creates a handle that postpones the dispose of the object until all the belate-handles have been disposed.
    /// </summary>
    /// <returns>belating handle that must be diposed</returns>
    /// <exception cref="ObjectDisposedException">thrown if object has already been disposed</exception>
    IDisposable BelateDispose();
}
```
</details>
<p/><p/>

Objects can be attached to be disposed along with the *IDisposeList* object. The attached object doesn't have to implement *IDisposable*, if it doesn't then the object is not added.

```csharp
IDisposable disposable = new ReaderWriterLockSlim();
IDisposeList disposeList = new DisposeList();
disposeList.AddDisposable(disposable);
// ... do work ... and dispose both.
disposeList.Dispose();
```

Delegate can be attached to be executed on dispose.

```csharp
IDisposeList disposeList = new DisposeList();
disposeList.AddDisposeAction(_=>Console.WriteLine("Disposed"), null);
// ... do work ...
disposeList.Dispose();
```

Dispose can be belated. This is useful when object is passed to other threads.

```csharp
IBelatableDispose disposeList = new DisposeList();

// Postpone dispose
IDisposable belateDisposeHandle = disposeList.BelateDispose();
// Start concurrent work
Task.Run(() =>
{
    // Do work
    Thread.Sleep(100);
    // Release belate handle. Disposes here or below, depending which thread runs last.
    belateDisposeHandle.Dispose();
});

// Start dispose, but postpone it until belatehandle is disposed in another thread.
disposeList.Dispose();
```
