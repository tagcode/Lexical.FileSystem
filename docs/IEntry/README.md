### IEntry
<details>
 <summary><b>IEntry</b> is root interface entry interfaces. (<u>Click here</u>)</summary>

```csharp
/// <summary>
/// Entry that represents a node of a <see cref="IFileSystem"/>.
/// 
/// The entry represents the snapshot state at the time of creation.
/// 
/// See <see cref="IEntry"/> sub-interfaces:
/// <list type="bullet">
///     <item><see cref="IFileEntry"/></item>
///     <item><see cref="IDirectoryEntry"/></item>
///     <item><see cref="IDriveEntry"/></item>
///     <item><see cref="IMountEntry"/></item>
///     <item><see cref="IEntryOptions"/></item>
///     <item><see cref="IEntryFileAttributes"/></item>
///     <item><see cref="IEntryPhysicalPath"/></item>
///     <item><see cref="IEntryDecoration"/></item>
/// </list>
/// </summary>
public interface IEntry
{
    /// <summary>
    /// (optional) Associated file system.
    /// </summary>
    IFileSystem FileSystem { get; }

    /// <summary>
    /// Path that is relative to the <see cref="IFileSystem"/>.
    /// 
    /// Separator is forward slash "/".
    /// Directories end with "/" unless root directory.
    /// </summary>
    string Path { get; }

    /// <summary>
    /// Entry name in its parent context.
    /// 
    /// All characters are legal, including control characters, except forward slash '/'. 
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Date time of last modification. In UTC time, if possible. If Unknown returns <see cref="DateTimeOffset.MinValue"/>.
    /// </summary>
    DateTimeOffset LastModified { get; }

    /// <summary>
    /// Last access time of entry. If Unknown returns <see cref="DateTimeOffset.MinValue"/>.
    /// </summary>
    DateTimeOffset LastAccess { get; }
}
```
</details>
<details>
 <summary><b>IFileEntry</b> is interface file entries. (<u>Click here</u>)</summary>

```csharp
/// <summary>
/// File entry
/// </summary>
public interface IFileEntry : IEntry
{
    /// <summary>
    /// Tests if entry represents a file.
    /// </summary>
    bool IsFile { get; }

    /// <summary>
    /// File length. -1 if is length is unknown.
    /// </summary>
    long Length { get; }
}
```
</details>
<details>
 <summary><b>IDirectoryEntry</b> is interface directory entries. (<u>Click here</u>)</summary>

```csharp
/// <summary>
/// Directory entry that can be browsed for contents with <see cref="IFileSystemBrowse"/>.
/// </summary>
public interface IDirectoryEntry : IEntry
{
    /// <summary>
    /// Tests if entry represents a directory.
    /// </summary>
    bool IsDirectory { get; }
}
```
</details>
<details>
 <summary><b>IDriveEntry</b> is interface for drive entries. (<u>Click here</u>)</summary>

```csharp
/// <summary>
/// Drive or volume entry. 
/// 
/// If drive class is browsable, then the implementation also implements <see cref="IDirectoryEntry"/>.
/// </summary>
public interface IDriveEntry : IEntry
{
    /// <summary>
    /// Tests if entry represents a drive or volume.
    /// </summary>
    bool IsDrive { get; }

    /// <summary>
    /// Drive type.
    /// </summary>
    DriveType DriveType { get; }

    /// <summary>
    /// Free space, -1L if unknown.
    /// </summary>
    long DriveFreeSpace { get; }

    /// <summary>
    /// Total size of drive or volume. -1L if unkown.
    /// </summary>
    long DriveSize { get; }

    /// <summary>
    /// Label, or null if unknown.
    /// </summary>
    String DriveLabel { get; }

    /// <summary>
    /// File system format.
    /// 
    /// Examples:
    /// <list type="bullet">
    ///     <item>NTFS</item>
    ///     <item>FAT32</item>
    /// </list>
    /// </summary>
    String DriveFormat { get; }
}
```
</details>
<details>
 <summary><b>IMountEntry</b> is interface for mount root directory entries. (<u>Click here</u>)</summary>

```csharp
/// <summary>
/// Entry represents a mount point (decoration or virtual filesystem directory). 
/// </summary>
public interface IMountEntry : IEntry
{
    /// <summary>
    /// Tests if directory represents a mount point.
    /// </summary>
    bool IsMountPoint { get; }

    /// <summary>
    /// (optional) Manually mounted filesystem(s).
    /// </summary>
    FileSystemAssignment[] Mounts { get; }
}
```
</details>
<details>
 <summary><b>IEntryDecoration</b> is interface for decoration implementations. (<u>Click here</u>)</summary>

```csharp
/// <summary>
/// Optional interface that exposes decoree.
/// </summary>
public interface IEntryDecoration : IEntry
{
    /// <summary>
    /// (Optional) Original entry that is being decorated.
    /// </summary>
    IEntry Original { get; }
}
```
</details>
<details>
 <summary><b>IEntryOptions</b> is interface for entry specific filesystem capability options. (<u>Click here</u>)</summary>

```csharp
/// <summary>
/// Entry specific filesystem capability options.
/// </summary>
public interface IEntryOptions : IEntry
{
    /// <summary>
    /// (optional) Options that apply to this entry. The options here are equal or subset of the options in the parenting <see cref="IFileSystem"/>.
    /// </summary>
    IOption Options { get; }
}
```
</details>
<details>
 <summary><b>IEntryFileAttributes</b> is interface for entry file attributes. (<u>Click here</u>)</summary>

```csharp
/// <summary>
/// Entry file Attributes.
/// </summary>
public interface IEntryFileAttributes : IEntry
{
    /// <summary>
    /// True, if has attached <see cref="System.IO.FileAttributes"/>.
    /// </summary>
    bool HasFileAttributes { get; }

    /// <summary>
    /// (optional) File attributes
    /// </summary>
    FileAttributes FileAttributes { get; }
}
```
</details>
<details>
 <summary><b>IEntryPhysicalPath</b> is interface for entry physical path. (<u>Click here</u>)</summary>

```csharp
/// <summary>
/// Optional interface for entries that may have a physical file or directory path.
/// </summary>
public interface IEntryPhysicalPath : IEntry
{
    /// <summary>
    /// (optional) Physical (OS) path to file or directory.
    /// </summary>
    String PhysicalPath { get; }
}
```
</details>
<p/><p/>

