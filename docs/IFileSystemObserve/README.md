### IFileSystemObserve
<details>
  <summary><b>IFileSystemObserve</b> is interface for observing directory and file changes. (<u>Click here</u>)</summary>

```csharp
/// <summary>
/// File system that observe file and directory changes.
/// </summary>
public interface IFileSystemObserve : IFileSystem, IObserveOption
{
    /// <summary>
    /// Attach an <paramref name="observer"/> on to a directory. 
    /// 
    /// The <paramref name="filter"/> determines the file pattern to observe.
    ///  "*" Matches to any sequence characters within one folder.
    ///  "**" Matches to any sequence characters including directory levels '/'.
    ///  "?" Matches to one and exactly one character.
    /// 
    /// Examples:
    ///   <list type="bullet">
    ///     <item>"**" is any file in any directory.</item>
    ///     <item>"**/file.txt", to monitor "file.txt" in any subdirectory (excluding root due to missing "/")</item>
    ///     <item>"*" is any set of characters file in one directory. For example "mydir/somefile*.txt"</item>
    ///     <item>"", to monitor changes to the root directory itself, but not its files</item>
    ///     <item>"dir", to monitor the dir itself, but not its files</item>
    ///     <item>"dir/", to monitor the dir itself, but not its files</item>
    ///     <item>"dir/file", to monitor one file "file"</item>
    ///     <item>"dir/*", to monitor files in a dir but not subdirectories</item>
    ///     <item>"dir/**", to monitor files in a dir and its subdirectories</item>
    ///   </list>
    /// 
    /// The implementation sends the reference to the observer handle in a <see cref="IStartEvent"/> event before this method returns to caller.
    /// </summary>
    /// <param name="filter">file filter as glob pattern. </param>
    /// <param name="observer"></param>
    /// <param name="state">(optional) </param>
    /// <param name="eventDispatcher">(optional) event dispatcher to use to dispatch events to this obawecwe</param>
    /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
    /// <returns>handle to the observer, dispose to cancel the observe</returns>
    /// <exception cref="IOException">On unexpected IO error</exception>
    /// <exception cref="SecurityException">If caller did not have permission</exception>
    /// <exception cref="ArgumentNullException"><paramref name="filter"/> is null</exception>
    /// <exception cref="ArgumentException"><paramref name="filter"/> contains only white space, or contains one or more invalid characters</exception>
    /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support observe</exception>
    /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path.</exception>
    /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
    /// <exception cref="InvalidOperationException">If <paramref name="filter"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
    /// <exception cref="ObjectDisposedException"/>
    IFileSystemObserver Observe(string filter, IObserver<IEvent> observer, object state = null, IEventDispatcher eventDispatcher = null, IOption option = null);
}

/// <summary>
/// Observer information.
/// </summary>
public interface IFileSystemObserver : IDisposable
{
    /// <summary>
    /// The file system where the observer was attached.
    /// </summary>
    IFileSystem FileSystem { get; }

    /// <summary>
    /// File filter as glob pattern.
    /// </summary>
    String Filter { get; }

    /// <summary>
    /// Callback.
    /// </summary>
    IObserver<IEvent> Observer { get; }

    /// <summary>
    /// State object that was attached at construction.
    /// </summary>
    Object State { get; }

    /// <summary>
    /// (optional) Event dispatcher.
    /// </summary>
    IEventDispatcher Dispatcher { get; }
}
```
</details>
<details>
  <summary><b>IFileSystemObserveAsync</b> is async interface for observing directory and file changes. (<u>Click here</u>)</summary>

```csharp
/// <summary>
/// File system that observe file and directory changes.
/// </summary>
public interface IFileSystemObserveAsync : IFileSystem, IObserveOption
{
    /// <summary>
    /// Attach an <paramref name="observer"/> on to a directory. 
    /// 
    /// The <paramref name="filter"/> determines the file pattern to observe.
    ///  "*" Matches to any sequence characters within one folder.
    ///  "**" Matches to any sequence characters including directory levels '/'.
    ///  "?" Matches to one and exactly one character.
    /// 
    /// Examples:
    ///   <list type="bullet">
    ///     <item>"**" is any file in any directory.</item>
    ///     <item>"**/file.txt", to monitor "file.txt" in any subdirectory (excluding root due to missing "/")</item>
    ///     <item>"*" is any set of characters file in one directory. For example "mydir/somefile*.txt"</item>
    ///     <item>"", to monitor changes to the root directory itself, but not its files</item>
    ///     <item>"dir", to monitor the dir itself, but not its files</item>
    ///     <item>"dir/", to monitor the dir itself, but not its files</item>
    ///     <item>"dir/file", to monitor one file "file"</item>
    ///     <item>"dir/*", to monitor files in a dir but not subdirectories</item>
    ///     <item>"dir/**", to monitor files in a dir and its subdirectories</item>
    ///   </list>
    /// 
    /// The implementation sends the reference to the observer handle in a <see cref="IStartEvent"/> event before this method returns to caller.
    /// </summary>
    /// <param name="filter">file filter as glob pattern. </param>
    /// <param name="observer"></param>
    /// <param name="state">(optional) </param>
    /// <param name="eventDispatcher">(optional) event dispatcher to use to dispatch events to this obawecwe</param>
    /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
    /// <returns>handle to the observer, dispose to cancel the observe</returns>
    /// <exception cref="IOException">On unexpected IO error</exception>
    /// <exception cref="SecurityException">If caller did not have permission</exception>
    /// <exception cref="ArgumentNullException"><paramref name="filter"/> is null</exception>
    /// <exception cref="ArgumentException"><paramref name="filter"/> contains only white space, or contains one or more invalid characters</exception>
    /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support observe</exception>
    /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path.</exception>
    /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
    /// <exception cref="InvalidOperationException">If <paramref name="filter"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
    /// <exception cref="ObjectDisposedException"/>
    Task<IFileSystemObserver> ObserveAsync(string filter, IObserver<IEvent> observer, object state = null, IEventDispatcher eventDispatcher = null, IOption option = null);
}
```
</details>
<p/><p/>

<i>IFileSystem</i>**.Observe(<i>filter, observer, state, eventDispatcher</i>)** observe directories and files for modifications.
Filter uses glob pattern. 

```csharp
IObserver<IEvent> observer = new Observer();
using (IDisposable handle = FileSystem.Application.Observe("**", observer))
{
}
```

Optionally *eventDispatcher* parameter determines how events are dispatched.

```csharp
string path = AppDomain.CurrentDomain.BaseDirectory;
IFileSystem filesystem = new FileSystem(path);
IObserver<IEvent> observer = new Observer();
IDisposable handle = FileSystem.Application.Observe(
    filter: "**", 
    observer: observer, 
    eventDispatcher: EventDispatcher.Instance);
filesystem.CreateDirectory("dir/");
```

**EventTaskDispatcher** dispatches events with task factory.

```csharp
string path = AppDomain.CurrentDomain.BaseDirectory;
IFileSystem filesystem = new FileSystem(path);
IObserver<IEvent> observer = new Observer();
IDisposable handle = FileSystem.Application.Observe(
    filter: "**", 
    observer: observer, 
    eventDispatcher: EventTaskDispatcher.Instance);
filesystem.CreateDirectory("dir/");
```


*IEventDispatcher* singleton instances:

| Name                                  | Description                                                                                      |
|:--------------------------------------|:-------------------------------------------------------------------------------------------------|
| EventDispatcher.Instance    | Dispatches events in the API caller's thread.                                                    |
| EventTaskDispatcher.Instance| Dispatches events concurrently in a TaskFactory.                                                 |
