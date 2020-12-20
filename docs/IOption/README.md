### IOption
<details>
  <summary><b>IOption</b> is root interface filesystem options. (<u>Click here</u>)</summary>

```csharp
/// <summary>
/// Interface for filesystem options. 
/// 
/// See sub-interfaces:
/// <list type="bullet">
///     <item><see cref="IAdaptableOption"/></item>
///     <item><see cref="ISubPathOption"/></item>
///     <item><see cref="IPathInfo"/></item>
///     <item><see cref="IAutoMountOption"/></item>
///     <item><see cref="IToken"/></item>
///     <item><see cref="IOpenOption"/></item>
///     <item><see cref="IObserveOption"/></item>
///     <item><see cref="IMoveOption"/></item>
///     <item><see cref="IBrowseOption"/></item>
///     <item><see cref="ICreateDirectoryOption"/></item>
///     <item><see cref="IDeleteOption"/></item>
///     <item><see cref="IMountOption"/></item>
/// </list>
/// 
/// The options properties must be immutable in the implementing classes.
/// </summary>
public interface IOption
{
}
```
</details>
<details>
  <summary><b>IOpenOption</b> is capabilities of <i>IFileSystemOpen</i> interface. (<u>Click here</u>)</summary>

```csharp
public interface IOpenOption : IOption
{
    /// <summary>Can open file</summary>
    bool CanOpen { get; }
    /// <summary>Can open file for reading(</summary>
    bool CanRead { get; }
    /// <summary>Can open file for writing.</summary>
    bool CanWrite { get; }
    /// <summary>Can open and create file.</summary>
    bool CanCreateFile { get; }
}
```
</details>
<details>
  <summary><b>IObserveOption</b> is capabilities of <i>IFileSystemObserve</i> interface. (<u>Click here</u>)</summary>

```csharp
public interface IObserveOption : IOption
{
    /// <summary>Has Observe capability.</summary>
    bool CanObserve { get; }
}
```
</details>
<details>
  <summary><b>IMoveOption</b> is capabilities of <i>IFileSystemMove</i> interface. (<u>Click here</u>)</summary>

```csharp
public interface IMoveOption : IOption
{
    /// <summary>Can Move files within same volume.</summary>
    bool CanMove { get; }
}
```
</details>
<details>
  <summary><b>IBrowseOption</b> is capabilities of <i>IFileSystemBrowse</i> interface. (<u>Click here</u>)</summary>

```csharp
public interface IBrowseOption : IOption
{
    /// <summary>Has Browse capability.</summary>
    bool CanBrowse { get; }
    /// <summary>Has GetEntry capability.</summary>
    bool CanGetEntry { get; }
}
```
</details>
<details>
  <summary><b>ICreateDirectoryOption</b> is capabilities of <i>IFileSystemCreateDirectory</i> interface. (<u>Click here</u>)</summary>

```csharp
public interface ICreateDirectoryOption : IOption
{
    /// <summary>Has CreateDirectory capability.</summary>
    bool CanCreateDirectory { get; }
}
```
</details>
<details>
  <summary><b>IDeleteOption</b> is capabilities of <i>IFileSystemDelete</i> interface. (<u>Click here</u>)</summary>

```csharp
public interface IDeleteOption : IOption
{
    /// <summary>Has Delete capability.</summary>
    bool CanDelete { get; }
}
```
</details>
<details>
  <summary><b>IMountOption</b> is capabilities of <i>IFileSystemMount</i> interface. (<u>Click here</u>)</summary>

```csharp
public interface IMountOption : IOption
{
    /// <summary>Can filesystem mount other filesystems.</summary>
    bool CanMount { get; }
    /// <summary>Is filesystem allowed to unmount a mount.</summary>
    bool CanUnmount { get; }
    /// <summary>Is filesystem allowed to list mountpoints.</summary>
    bool CanListMountPoints { get; }
}
```
</details>
<details>
  <summary><b>IFileAttributeOption</b> is capabilities of <i>IFileSystemFileAttribute</i> interface. (<u>Click here</u>)</summary>

```csharp
public interface IFileAttributeOption : IOption
{
    /// <summary>Has SetFileAttribute capability.</summary>
    bool CanSetFileAttribute { get; }
}
```
</details>
<details>
  <summary><b>ISubPathOption</b> is interface for subpath option. (<u>Click here</u>)</summary>

```csharp
public interface ISubPathOption : IOption
{
    /// <summary>Sub-path.</summary>
    String SubPath { get; }
}
```
</details>
<details>
  <summary><b>IAutoMountOption</b> is interface for automatic mounting of package files. (<u>Click here</u>)</summary>

```csharp
public interface IAutoMountOption : IOption
{
    /// <summary>Package loaders that can mount package files, such as .zip.</summary>
    IPackageLoader[] AutoMounters { get; }
}
```
</details>
<details>
  <summary><b>IPathInfo</b> is interface for filesystem's path info options. (<u>Click here</u>)</summary>

```csharp
public interface IPathInfo : IOption
{
    /// <summary>Case sensitivity</summary>
    FileSystemCaseSensitivity CaseSensitivity { get; }
    /// <summary>Filesystem allows empty string "" directory names. The value of this property excludes the default empty "" root path.</summary>
    bool EmptyDirectoryName { get; }
}
```
</details>
<details>
  <summary><b>IAdaptableOption</b> is interface for runtime option classes. (<u>Click here</u>)</summary>

```csharp
/// <summary>
/// Interface for option classes that adapt to option types at runtime.
/// Also enumerates supported <see cref="IOption"/> option type interfaces.
/// </summary>
public interface IAdaptableOption : IOption, IEnumerable<KeyValuePair<Type, IOption>>
{
    /// <summary>
    /// Get option with type interface.
    /// </summary>
    /// <param name="optionInterfaceType">Subtype of <see cref="IOption"/></param>
    /// <returns>Option or null</returns>
    IOption GetOption(Type optionInterfaceType);
}
```
</details>
<p/><p/>

<br/>
*FileSystemOption* singletons:

| Flag     | Description |
|:---------|:------------|
| <i>Option.</i>Join() | Takes first instance of each option. |
| <i>Option.</i>Union() | Take union of options. |
| <i>Option.</i>Intersection() | Take intersection of options. |
| <i>Option.</i>ReadOnly | Read-only operations allowed, deny modification and write operations |
| <i>Option.</i>NoOptions | No options |
| <i>Option.</i>Path(caseSensitivity, emptyDirectoryName) | Path options |
| <i>Option.</i>Observe | Observe is allowed. |
| <i>Option.</i>NoObserve | Observe is not allowed |
| <i>Option.</i>Open(canOpen, canRead, canWrite, canCreateFile) | Open options |
| <i>Option.</i>OpenReadWriteCreate | Open, Read, Write, Create |
| <i>Option.</i>OpenReadWrite | Open, Read, Write |
| <i>Option.</i>OpenRead | Open, Read |
| <i>Option.</i>NoOpen | No access |
| <i>Option.</i>Mount | Mount is allowed. |
| <i>Option.</i>NoMount | Mount is not allowed |
| <i>Option.</i>Move | Move and rename is allowed. |
| <i>Option.</i>NoMove | Move and rename not allowed.  |
| <i>Option.</i>Delete | Delete allowed. |
| <i>Option.</i>NoDelete | Delete not allowed. |
| <i>Option.</i>CreateDirectory | CreateDirectory allowed. |
| <i>Option.</i>NoCreateDirectory | CreateDirectory not allowed. |
| <i>Option.</i>Browse | Browse allowed. |
| <i>Option.</i>NoBrowse | Browse not allowed. |
| <i>Option.</i>SubPath(subpath) | Create option for sub-path. Used with decorator and VirtualFileSystem. |
| <i>Option.</i>NoSubPath | No mount path. |
