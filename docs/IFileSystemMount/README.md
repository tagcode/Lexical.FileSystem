### IFileSystem
<details>
  <summary><b>IFileSystemMount</b> is for mounting filesystems to virtual filesystem. (<u>Click here</u>)</summary>

```csharp
/// <summary>
/// FileSystem that can mount other filesystems into its directory tree.
/// </summary>
public interface IFileSystemMount : IFileSystem, IMountOption
{
    /// <summary>
    /// Mounts zero, one or many <see cref="IFileSystem"/> with optional <see cref="IOption"/> in the parent filesystem.
    /// 
    /// If no mounts are provided, then creates empty virtual directory.
    /// If one mount is provided, then mounts that to parent filesystem, with possible mount option.
    /// If multiple mounts are provided, then mounts a composition of all the filesystem, with the precedence of the order in the provided array.
    /// 
    /// If previous mounts exist at the <paramref name="path"/>, then replaces them with new configuration.
    /// 
    /// If parent filesystem had observers monitoring the <paramref name="path"/>, then observers are notified with new emerged files from the mounted filesystems.
    /// 
    /// The <paramref name="path"/> parameter must end with directory separator character '/', unless root directory "" is mounted.
    /// 
    /// If there is an open stream to a mounted filesystem, then the file is unlinked from the parent filesystem, but stream maintains open.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="mounts">(optional) filesystem and option infos</param>
    /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
    /// <returns>this (parent filesystem)</returns>
    /// <exception cref="NotSupportedException">If operation is not supported</exception>
    IFileSystem Mount(string path, FileSystemAssignment[] mounts, IOption option = null);

    /// <summary>
    /// Unmount a filesystem at <paramref name="path"/>.
    /// 
    /// If there is no mount at <paramref name="path"/>, then does nothing.
    /// If there is an open stream to previously mounted filesystem, that the file is unlinked, but stream remains open.
    /// If there are observers monitoring <paramref name="path"/> in the parent filesystem, then the unmounted files are notified as being deleted.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
    /// <returns>this (parent filesystem)</returns>
    /// <exception cref="NotSupportedException">If operation is not supported</exception>
    IFileSystem Unmount(string path, IOption option = null);

    /// <summary>
    /// List all mounts.
    /// </summary>
    /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException">If operation is not supported</exception>
    IMountEntry[] ListMountPoints(IOption option = null);
}

/// <summary>Mount assignemnt related info <see cref="FileSystemAssignment"/>.</summary>
[Flags]
public enum FileSystemAssignmentFlags
{
    /// <summary>No flags</summary>
    None = 0,
    /// <summary>Signals that filesystem was manually mounted with <see cref="IFileSystemMount.Mount"/>.</summary>
    Mounted = 1,
    /// <summary>Signals that filesystem was automatically mounted based on <see cref="IAutoMountOption"/> options.</summary>
    AutoMounted = 2,
    /// <summary>Filesystem is to be automatically unmounted by timer when it hasn't been used in a configured time.</summary>
    AutoUnmount = 4,
    /// <summary>Mount represents a package file, such as .zip.</summary>
    Package = 8,
    /// <summary>Signaled as decoration assignment</summary>
    Decoration = 16,
}

/// <summary>The filesystem and option assignment.</summary>
public partial struct FileSystemAssignment
{
    /// <summary>(optional) Filesystem.</summary>
    public readonly IFileSystem FileSystem;
    /// <summary>(optional) Overriding option assignment.</summary>
    public readonly IOption Option;
    /// <summary>Is flagged as automatically mounted.</summary>
    public readonly FileSystemAssignmentFlags Flags;

    /// <summary>Create filesystem and option assignment.</summary>
    /// <param name="fileSystem">(optional) file system</param>
    /// <param name="option">(optional) overriding option assignment</param>
    /// <param name="flags">(optional) assignment related flags</param>
    public FileSystemAssignment(IFileSystem fileSystem, IOption option = null, FileSystemAssignmentFlags flags = FileSystemAssignmentFlags.None)
    {
        FileSystem = fileSystem;
        Option = option;
        Flags = flags;
    }
}
```
</details>
<details>
  <summary><b>IFileSystemMountAsync</b> is async for mounting filesystems to virtual filesystem. (<u>Click here</u>)</summary>

```csharp
/// <summary>
/// FileSystem that can mount other filesystems into its directory tree.
/// </summary>
public interface IFileSystemMountAsync : IFileSystem, IMountOption
{
    /// <summary>
    /// Mounts zero, one or many <see cref="IFileSystem"/> with optional <see cref="IOption"/> in the parent filesystem.
    /// 
    /// If no mounts are provided, then creates empty virtual directory.
    /// If one mount is provided, then mounts that to parent filesystem, with possible mount option.
    /// If multiple mounts are provided, then mounts a composition of all the filesystem, with the precedence of the order in the provided array.
    /// 
    /// If previous mounts exist at the <paramref name="path"/>, then replaces them with new configuration.
    /// 
    /// If parent filesystem had observers monitoring the <paramref name="path"/>, then observers are notified with new emerged files from the mounted filesystems.
    /// 
    /// The <paramref name="path"/> parameter must end with directory separator character '/', unless root directory "" is mounted.
    /// 
    /// If there is an open stream to a mounted filesystem, then the file is unlinked from the parent filesystem, but stream maintains open.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="mounts">(optional) filesystem and option infos</param>
    /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
    /// <returns>this (parent filesystem)</returns>
    /// <exception cref="NotSupportedException">If operation is not supported</exception>
    Task<IFileSystem> MountAsync(string path, FileSystemAssignment[] mounts, IOption option = null);

    /// <summary>
    /// Unmount a filesystem at <paramref name="path"/>.
    /// 
    /// If there is no mount at <paramref name="path"/>, then does nothing.
    /// If there is an open stream to previously mounted filesystem, that the file is unlinked, but stream remains open.
    /// If there are observers monitoring <paramref name="path"/> in the parent filesystem, then the unmounted files are notified as being deleted.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
    /// <returns>this (parent filesystem)</returns>
    /// <exception cref="NotSupportedException">If operation is not supported</exception>
    Task<IFileSystem> UnmountAsync(string path, IOption option = null);

    /// <summary>
    /// List all mounts.
    /// </summary>
    /// <param name="option">(optional) operation specific option; capability constraint, a session, security token or credential. Used for authenticating, authorizing or restricting the operation.</param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException">If operation is not supported</exception>
    Task<IMountEntry[]> ListMountPointsAsync(IOption option = null);
}
```
</details>
<p/><p/>
