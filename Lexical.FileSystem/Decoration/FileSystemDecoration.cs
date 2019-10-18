// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Internal;
using Lexical.FileSystem.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading.Tasks;

namespace Lexical.FileSystem.Decoration
{
    /// <summary>
    /// <see cref="IFileSystem"/> decoration.
    /// 
    /// Supports following decorating modifications:
    /// <list type="bullet">
    ///     <item><see cref="IFileSystemOptionBrowse"/> </item>
    ///     <item><see cref="IFileSystemOptionObserve"/> </item>
    ///     <item><see cref="IFileSystemOptionOpen"/> </item>
    ///     <item><see cref="IFileSystemOptionDelete"/> </item>
    ///     <item><see cref="IFileSystemOptionMove"/> </item>
    ///     <item><see cref="IFileSystemOptionCreateDirectory"/> </item>
    ///     <item><see cref="IFileSystemOptionMount"/> </item>
    ///     <item><see cref="IFileSystemOptionSubPath"/> </item>
    /// </list>
    /// 
    /// See extension methods in <see cref="FileSystems"/> to create decorations, or use <see cref="VirtualFileSystem"/>.
    /// </summary>
    public class FileSystemDecoration : FileSystemBase, IEnumerable<IFileSystem>, IFileSystemBrowse, IFileSystemObserve, IFileSystemOpen, IFileSystemDelete, IFileSystemMove, IFileSystemCreateDirectory, IFileSystemMount, IFileSystemOptionPath
    {
        /// <summary>Zero entries</summary>
        static IFileSystemEntry[] noEntries = new IFileSystemEntry[0];

        /// <summary>FileSystem specific components.</summary>
        protected internal ArrayList<Component> components = new ArrayList<Component>();
        /// <summary>Union of options.</summary>
        protected internal Options Option;
        /// <summary>Count</summary>
        public int Count => components.Count;
        /// <summary>The decorated filesystems.</summary>
        public IFileSystem[] Decorees => components.Array.Select(c=>c.FileSystem).ToArray();
        /// <summary><see cref="IFileSystem"/> casted to <see cref="IDisposable"/>.</summary>
        public IEnumerable<IDisposable> DisposableDecorees => components.Array.Where(fs => fs is IDisposable).Cast<IDisposable>();

        /// <inheritdoc/>
        public FileSystemCaseSensitivity CaseSensitivity => Option.CaseSensitivity;
        /// <inheritdoc/>
        public bool EmptyDirectoryName => Option.EmptyDirectoryName;
        /// <inheritdoc/>
        public virtual bool CanBrowse => Option.CanBrowse;
        /// <inheritdoc/>
        public virtual bool CanGetEntry => Option.CanGetEntry;
        /// <inheritdoc/>
        public override bool CanObserve => Option.CanObserve;
        /// <inheritdoc/>
        public virtual bool CanOpen => Option.CanOpen;
        /// <inheritdoc/>
        public virtual bool CanRead => Option.CanRead;
        /// <inheritdoc/>
        public virtual bool CanWrite => Option.CanWrite;
        /// <inheritdoc/>
        public virtual bool CanCreateFile => Option.CanCreateFile;
        /// <inheritdoc/>
        public virtual bool CanDelete => Option.CanDelete;
        /// <inheritdoc/>
        public virtual bool CanMove => Option.CanMove;
        /// <inheritdoc/>
        public virtual bool CanCreateDirectory => Option.CanCreateDirectory;
        /// <inheritdoc/>
        public virtual bool CanMount => Option.CanMount;
        /// <inheritdoc/>
        public virtual bool CanUnmount => Option.CanUnmount;
        /// <inheritdoc/>
        public virtual bool CanListMountPoints => Option.CanListMountPoints;

        /// <summary>
        /// Root entry
        /// </summary>
        protected IFileSystemEntry rootEntry;

        /// <summary>
        /// The <see cref="IFileSystem"/> reference that is passed to the decorated
        /// <see cref="IFileSystemEntry"/> instances that this class creates.
        /// 
        /// Usually it is the <see cref="FileSystemDecoration"/> itself, but if this class
        /// is used as a component, such as <see cref="VirtualFileSystem"/> does, then
        /// returns the parent filesystem.
        /// </summary>
        protected IFileSystem sourceFileSystem;

        /// <summary>
        /// Create composition of filesystems.
        /// 
        /// A constructor version that exposes its filesystem at a subpath parentPath. 
        /// Also allows to configure what filesystem instance is exposed on decorated file entries and events.
        /// </summary>
        /// <param name="parentFileSystem">(optional) the <see cref="IFileSystem"/> reference to use in the decorated <see cref="IFileSystemEntry"/> that this class returns</param>
        /// <param name="parentPath">(optional) path in parent filesyste, use "" if there is no parent filesystem (vfs)</param>
        /// <param name="assignments"></param>
        public FileSystemDecoration(IFileSystem parentFileSystem, string parentPath, params FileSystemAssignment[] assignments)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            this.components.AddRange( assignments.Select(a=> new Component(parentPath, a)) );
            this.Option = Options.Read(FileSystemOption.Union(this.components.Select(s => s.Option)));

            this.sourceFileSystem = parentFileSystem ?? this;
            this.rootEntry = new FileSystemEntryMount(this.sourceFileSystem, "", "", now, now, Option, this.components.Select(c=>c.Assignment).ToArray());
        }

        /// <summary>FileSystem (as component of composition) specific information</summary>
        public class Component
        {
            /// <summary>FileSystem and option assignment</summary>
            public FileSystemAssignment Assignment;
            /// <summary>FileSystem component</summary>
            public IFileSystem FileSystem => Assignment.FileSystem;
            /// <summary>Intersection of option in <see cref="FileSystem"/> and option that was provided in constructor.</summary>
            public Options Option;
            /// <summary>Tool that converts paths.</summary>
            public IPathConverter Path;

            /// <summary>Create component info.</summary>
            /// <param name="parentPath">The subpath the filesystem starts at</param>
            /// <param name="assignment">filesystem and option</param>
            public Component(string parentPath, FileSystemAssignment assignment)
            {
                this.Assignment = assignment;
                this.Option = Options.Read(assignment.Option == null ? assignment.FileSystem : FileSystemOption.Intersection(assignment.FileSystem, assignment.Option));
                this.Path = new PathConverter(parentPath ?? "", assignment.Option.SubPath() ?? "");
            }

            /// <summary>Create component info.</summary>
            /// <param name="parentPath">The subpath the filesystem starts at</param>
            /// <param name="assignment">filesystem and option</param>
            /// <param name="option">consolidated options</param>
            public Component(string parentPath, FileSystemAssignment assignment, Options option)
            {
                this.Assignment = assignment;
                this.Option = option;
                this.Path = new PathConverter(parentPath ?? "", assignment.Option.SubPath() ?? "");
            }
        }

        /// <summary>Comparer that <see cref="SetComponents"/> uses.</summary>
        protected static IEqualityComparer<Triple<string, IFileSystem, Options>> componentTupleComparer = new Triple<string, IFileSystem, Options>.EqualityComparer(StringComparer.InvariantCulture, EqualityComparer<IFileSystem>.Default, EqualityComparer<Options>.Default/*<-change this to better*/);

        /// <summary>
        /// Set new list components. Recycles previous components if path, filesystem and option matches.
        /// </summary>
        /// <param name="componentsAdded">list of components added</param>
        /// <param name="componentsRemoved">list of components removed</param>
        /// <param name="componentsReused">list of previous components that were reused</param>
        /// <param name="parentPath">Path in parent (vfs) filesystem, "" if there is vfs</param>
        /// <param name="assignments"></param>
        protected internal void SetComponents(ref StructList2<Component> componentsAdded, ref StructList2<Component> componentsRemoved , ref StructList2<Component> componentsReused, string parentPath, params FileSystemAssignment[] assignments)
        {
            lock (this.components.SyncRoot)
            {
                var oldComponents = components.Array;
                var oldComponentLineMap = components.ToDictionary(c => new Triple<string, IFileSystem, Options>(c.Path.ParentPath, c.FileSystem, c.Option), componentTupleComparer);
                components.Clear();
                foreach(FileSystemAssignment assignment in assignments)
                {
                    // Take intersection and consolidate options
                    Options consolidatedOptions = Options.Read(assignment.Option == null ? assignment.FileSystem : FileSystemOption.Intersection(assignment.FileSystem, assignment.Option));

                    // Reuse previous component
                    Component reusedComponent;
                    if (oldComponentLineMap.TryGetValue(new Triple<string, IFileSystem, Options>(parentPath, assignment.FileSystem, consolidatedOptions), out reusedComponent))
                    {
                        this.components.Add(reusedComponent);
                        componentsAdded.Add(reusedComponent);
                    } else
                    // Create new component
                    {
                        Component newComponent = new Component(parentPath, assignment, consolidatedOptions);
                        this.components.Add(newComponent);
                        componentsAdded.Add(newComponent);
                    }
                }

                // Removed components
                foreach (var oldComponent in oldComponents)
                {
                    if (!this.components.Contains(oldComponent)) componentsRemoved.Add(oldComponent);
                }

                // Update root entry
                DateTimeOffset now = DateTimeOffset.UtcNow;
                this.rootEntry = new FileSystemEntryMount(this.sourceFileSystem, "", "", now, now, Option, this.components.Array.Select(c => c.Assignment).ToArray());
                // Update options
                this.Option = Options.Read(FileSystemOption.Union(this.components.Select(s => s.Option)));
            }
        }

        /// <summary>
        /// Set <paramref name="eventHandler"/> to be used for handling observer events.
        /// 
        /// If <paramref name="eventHandler"/> is null, then events are processed in the threads
        /// that make modifications to filesystem.
        /// </summary>
        /// <param name="eventHandler">(optional) factory that handles observer events</param>
        /// <returns>memory filesystem</returns>
        public FileSystemDecoration SetEventDispatcher(TaskFactory eventHandler)
        {
            ((IFileSystemObserve)this).SetEventDispatcher(eventHandler);
            return this;
        }

        /// <summary>
        /// Browse a directory for file and subdirectory entries.
        /// </summary>
        /// <param name="path">path to directory, "" is root, separator is "/"</param>
        /// <returns>a snapshot of file and directory entries</returns>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support browse</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        public IFileSystemEntry[] Browse(string path)
        {
            // Assert argument
            if (path == null) throw new ArgumentNullException(nameof(path));
            // Assert not disposed
            if (IsDisposed) throw new ObjectDisposedException(GetType().FullName);

            try
            {
                // Get reference
                Component[] components = this.components.Array;
                // Zero components
                if (components.Length == 0)
                {
                    // Assert permission to browse
                    if (!Option.CanBrowse) throw new NotSupportedException(nameof(Browse));
                    // No entries
                    return noEntries;
                }

                // One filesystem component
                if (components.Length == 1)
                {
                    // Get reference
                    var component = components[0];
                    // Assert permission to browse
                    if (!component.Option.CanBrowse) throw new NotSupportedException(nameof(Browse));
                    // Convert Path
                    String/*Segment*/ childPath;
                    if (!component.Path.ParentToChild(path, out childPath)) return noEntries;
                    // Browse
                    IFileSystemEntry[] childEntries = component.FileSystem.Browse(childPath);
                    // Result array to be filled
                    IFileSystemEntry[] result = new IFileSystemEntry[childEntries.Length];
                    // Is result array filled with null enties
                    int removedCount = 0;
                    // Decorate elements
                    for (int i = 0; i < childEntries.Length; i++)
                    {
                        // Get entry
                        IFileSystemEntry e = childEntries[i];
                        // Convert path
                        String parentPath;
                        if (!component.Path.ChildToParent(e.Path, out parentPath)) { /* Path conversion failed. Omit entry. Remove it later */ removedCount++; continue; }
                        // Decorate
                        result[i] = CreateEntry(e, sourceFileSystem, parentPath, component.Option);
                    }
                    // Remove null entries
                    if (removedCount > 0)
                    {
                        IFileSystemEntry[] newResult = new IFileSystemEntry[result.Length - removedCount];
                        int ix = 0;
                        foreach (var e in result) if (e != null) newResult[ix++] = e;
                        result = newResult;
                    }
                    return result;
                }

                // Many components
                {
                    // browse result of each filesystem
                    StructList4<(Component, IFileSystemEntry[])> entryArrays = new StructList4<(Component, IFileSystemEntry[])>();
                    // path exists and browse supported
                    bool exists = false, supported = false;
                    // Number of total entries
                    int entryCount = 0;

                    // Create hashset for removing overlapping entry names
                    HashSet<string> filenames = new HashSet<string>();
                    foreach (Component component in components)
                    {
                        // Assert component can browse
                        if (!component.Option.CanBrowse) continue;
                        // Convert Path
                        String/*Segment*/ childPath;
                        if (!component.Path.ParentToChild(path, out childPath)) continue;
                        // Catch NotSupported
                        try
                        {
                            // Browse
                            IFileSystemEntry[] component_entries = component.FileSystem.Browse(childPath);
                            entryArrays.Add((component, component_entries));
                            entryCount += component_entries.Length;
                            exists = true; supported = true;
                        }
                        catch (DirectoryNotFoundException) { supported = true; }
                        catch (NotSupportedException) { }
                    }
                    if (!supported) throw new NotSupportedException(nameof(Browse));
                    if (!exists) throw new DirectoryNotFoundException(path);

                    // Create list for result
                    List<IFileSystemEntry> result = new List<IFileSystemEntry>(entryCount);

                    for (int i = 0; i < entryArrays.Count; i++)
                    {
                        // Get component and result array
                        (Component c, IFileSystemEntry[] array) = entryArrays[i];
                        // Prune and decorate
                        foreach (IFileSystemEntry e in array)
                        {
                            // Remove already existing entry
                            if (filenames != null && !filenames.Add(e.Name)) continue;
                            // Convert path
                            String/*Segment*/ parentPath;
                            if (!c.Path.ChildToParent(e.Path, out parentPath)) continue;
                            // Decorate
                            IFileSystemEntry ee = CreateEntry(e, sourceFileSystem, parentPath, c.Option);
                            // Add to result
                            result.Add(ee);
                        }
                    }

                    // Return as array
                    return result.ToArray();
                }
            }
            // Update references in the expception and let it fly
            catch (FileSystemException e) when (FileSystemExceptionUtil.Set(e, sourceFileSystem, path))
            {
                // Never goes here
                return noEntries;
            }
        }

        /// <summary>
        /// Get entry of a single file or directory.
        /// </summary>
        /// <param name="path">path to a directory or to a single file, "" is root, separator is "/"</param>
        /// <returns>entry, or null if entry is not found</returns>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support exists</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        public IFileSystemEntry GetEntry(string path)
        {
            // Assert argument
            if (path == null) throw new ArgumentNullException(nameof(path));
            // Assert not disposed
            if (IsDisposed) throw new ObjectDisposedException(GetType().FullName);
            // Return root
            if (path == "") return rootEntry;

            try
            {
                // Get reference
                Component[] components = this.components.Array;
                // Zero components
                if (components.Length == 0)
                {
                    // Assert can get entry
                    if (!this.Option.CanGetEntry) throw new NotSupportedException(nameof(GetEntry));
                    // No match
                    return null;
                }

                // One component
                if (components.Length == 1)
                {
                    // Get reference
                    var component = components[0];
                    // Assert can get entry
                    if (!component.Option.CanGetEntry) throw new NotSupportedException(nameof(GetEntry));
                    // Return root
                    if (path == "") return rootEntry;

                    // Convert Path
                    String/*Segment*/ childPath;
                    if (!component.Path.ParentToChild(path, out childPath)) return null;
                    // GetEntry
                    IFileSystemEntry childEntry = component.FileSystem.GetEntry(childPath);
                    // Got no result
                    if (childEntry == null) return null;
                    // Convert again
                    String/*Segment*/ parentPath;
                    if (!component.Path.ChildToParent(childEntry.Path, out parentPath)) return null;
                    // Decorate
                    childEntry = CreateEntry(childEntry, sourceFileSystem, parentPath, component.Option);
                    // Return
                    return childEntry;
                }

                // Many components
                else
                {
                    // Assert can get entry
                    if (!Option.CanGetEntry) throw new NotSupportedException(nameof(GetEntry));

                    bool supported = false;
                    foreach (Component component in components)
                    {
                        // Assert can get entry
                        if (!component.Option.CanGetEntry) continue;

                        // Convert Path
                        String/*Segment*/ childPath;
                        if (!component.Path.ParentToChild(path, out childPath)) continue;

                        try
                        {
                            // Try to get etnry
                            IFileSystemEntry e = component.FileSystem.GetEntry(childPath);
                            // Didn't throw exception
                            supported = true;
                            // Continue
                            if (e == null) continue;
                            // Convert again
                            String/*Segment*/ parentPath;
                            if (!component.Path.ChildToParent(e.Path, out parentPath)) continue;
                            // Decorate
                            e = CreateEntry(e, this, parentPath, component.Option);
                            // Return
                            return e;
                        }
                        catch (DirectoryNotFoundException) { supported = true; }
                        catch (NotSupportedException) { }
                    }
                    if (!supported) throw new NotSupportedException(nameof(GetEntry));

                    // Return root
                    if (path == "") return rootEntry;
                }
            }
            // Update references in the expception and let it fly
            catch (FileSystemException e) when (FileSystemExceptionUtil.Set(e, sourceFileSystem, path))
            {
                // Never goes here
                return null;
            }

            return null;
        }

        /// <summary>
        /// Open a file for reading and/or writing. File can be created when <paramref name="fileMode"/> is <see cref="FileMode.Create"/> or <see cref="FileMode.CreateNew"/>.
        /// </summary>
        /// <param name="path">Relative path to file. Directory separator is "/". Root is without preceding "/", e.g. "dir/file.xml"</param>
        /// <param name="fileMode">determines whether to open or to create the file</param>
        /// <param name="fileAccess">how to access the file, read, write or read and write</param>
        /// <param name="fileShare">how the file will be shared by processes</param>
        /// <returns>open file stream</returns>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is an empty string (""), contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support opening files</exception>
        /// <exception cref="FileNotFoundException">The file cannot be found, such as when mode is FileMode.Truncate or FileMode.Open, and and the file specified by path does not exist. The file must already exist in these modes.</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="fileMode"/>, <paramref name="fileAccess"/> or <paramref name="fileShare"/> contains an invalid value.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        public Stream Open(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
        {
            // Assert argument
            if (path == null) throw new ArgumentNullException(nameof(path));
            // Assert not disposed
            if (IsDisposed) throw new ObjectDisposedException(GetType().FullName);

            try
            {
                // Get reference
                Component[] components = this.components.Array;
                // Zero components
                if (components.Length == 0)
                {
                    // Assert can open
                    if (!this.Option.CanOpen) throw new NotSupportedException(nameof(Open));
                    if (!this.Option.CanRead && fileAccess.HasFlag(FileAccess.Read)) throw new NotSupportedException(nameof(CanRead));
                    if (!this.Option.CanWrite && fileAccess.HasFlag(FileAccess.Write)) throw new NotSupportedException(nameof(CanWrite));
                    if (!this.Option.CanCreateFile && fileMode != FileMode.Open) throw new NotSupportedException(nameof(CanCreateFile));
                    throw new FileNotFoundException(path);
                }

                // One Component
                if (components.Length == 1)
                {
                    // Get reference
                    var component = components[0];
                    if (!component.Option.CanOpen) throw new NotSupportedException(nameof(Open));
                    if (!component.Option.CanRead && fileAccess.HasFlag(FileAccess.Read)) throw new NotSupportedException(nameof(CanRead));
                    if (!component.Option.CanWrite && fileAccess.HasFlag(FileAccess.Write)) throw new NotSupportedException(nameof(CanWrite));
                    if (!component.Option.CanCreateFile && fileMode != FileMode.Open) throw new NotSupportedException(nameof(CanCreateFile));

                    // Convert Path
                    String childPath;
                    if (!component.Path.ParentToChild(path, out childPath)) throw new FileNotFoundException(path);
                    // Open
                    return component.FileSystem.Open(childPath, fileMode, fileAccess, fileShare);
                }

                // Many components
                {
                    bool supported = false;
                    foreach (Component component in components)
                    {
                        if (!component.Option.CanOpen) continue;
                        if (!component.Option.CanRead && fileAccess.HasFlag(FileAccess.Read)) continue;
                        if (!component.Option.CanWrite && fileAccess.HasFlag(FileAccess.Write)) continue;
                        if (!component.Option.CanCreateFile && fileMode != FileMode.Open) continue;
                        // Convert Path
                        String childPath;
                        if (!component.Path.ParentToChild(path, out childPath)) continue;
                        try
                        {
                            return component.FileSystem.Open(childPath, fileMode, fileAccess, fileShare);
                        }
                        catch (FileNotFoundException) { supported = true; }
                        catch (NotSupportedException) { }
                    }
                    if (!supported) throw new NotSupportedException(nameof(Open));
                    throw new FileNotFoundException(path);
                }
            }
            // Update references in the expception and let it fly
            catch (FileSystemException e) when (FileSystemExceptionUtil.Set(e, sourceFileSystem, path))
            {
                // Never goes here
                throw new NotSupportedException(nameof(Open));
            }
        }

        /// <summary>
        /// Delete a file or directory.
        /// 
        /// If <paramref name="recursive"/> is false and <paramref name="path"/> is a directory that is not empty, then <see cref="IOException"/> is thrown.
        /// If <paramref name="recursive"/> is true, then any file or directory within <paramref name="path"/> is deleted as well.
        /// </summary>
        /// <param name="path">path to a file or directory</param>
        /// <param name="recursive">if path refers to directory, recurse into sub directories</param>
        /// <exception cref="FileNotFoundException">The specified path is invalid.</exception>
        /// <exception cref="IOException">On unexpected IO error, or if <paramref name="path"/> refered to a directory that wasn't empty and <paramref name="recursive"/> is false</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is an empty string (""), contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support deleting files</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="path"/> refers to non-file device</exception>
        /// <exception cref="ObjectDisposedException"/>
        public void Delete(string path, bool recursive = false)
        {
            // Assert argument
            if (path == null) throw new ArgumentNullException(nameof(path));
            // Assert not disposed
            if (IsDisposed) throw new ObjectDisposedException(GetType().FullName);

            try
            {
                // Get reference
                Component[] components = this.components.Array;
                // Zero components
                if (components.Length == 0)
                {
                    // Assert can delete
                    if (!this.Option.CanDelete) throw new NotSupportedException(nameof(Delete));
                    throw new FileNotFoundException(path);
                }

                // One component
                if (components.Length == 1)
                {
                    // Get reference
                    var component = components[0];
                    // Assert can delete
                    if (!component.Option.CanDelete) throw new NotSupportedException(nameof(Delete));
                    // Convert Path
                    String childPath;
                    if (!component.Path.ParentToChild(path, out childPath)) throw new FileNotFoundException(path);
                    // Delete
                    component.FileSystem.Delete(childPath, recursive);
                }

                // Many components
                else
                {
                    bool supported = false;
                    bool ok = false;
                    foreach (Component component in components)
                    {
                        // Assert can delete
                        if (!component.Option.CanDelete) continue;
                        // Convert Path
                        String childPath;
                        if (!component.Path.ParentToChild(path, out childPath)) continue;
                        try
                        {
                            component.FileSystem.Delete(childPath, recursive);
                            ok = true; supported = true;
                        }
                        catch (FileNotFoundException) { supported = true; }
                        catch (NotSupportedException) { }
                    }
                    if (!supported) throw new NotSupportedException(nameof(Delete));
                    if (!ok) throw new FileNotFoundException(path);
                }
            }
            // Update references in the expception and let it fly
            catch (FileSystemException e) when (FileSystemExceptionUtil.Set(e, sourceFileSystem, path))
            {
                // Never goes here
                throw new NotSupportedException(nameof(Open));
            }
        }

        /// <summary>
        /// Try to move/rename a file or directory.
        /// </summary>
        /// <param name="oldPath">old path of a file or directory</param>
        /// <param name="newPath">new path of a file or directory</param>
        /// <exception cref="FileNotFoundException">The specified <paramref name="oldPath"/> is invalid.</exception>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="FileNotFoundException">The specified path is invalid.</exception>
        /// <exception cref="ArgumentNullException">path is null</exception>
        /// <exception cref="ArgumentException">path is an empty string (""), contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support renaming/moving files</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">path refers to non-file device, or an entry already exists at <paramref name="newPath"/></exception>
        /// <exception cref="ObjectDisposedException"/>
        public void Move(string oldPath, string newPath)
        {
            // Assert argument
            if (oldPath == null) throw new ArgumentNullException(nameof(oldPath));
            // Assert argument
            if (newPath == null) throw new ArgumentNullException(nameof(newPath));
            // Assert not disposed
            if (IsDisposed) throw new ObjectDisposedException(GetType().FullName);

            try
            {
                // Get reference
                Component[] components = this.components.Array;
                // Zero components
                if (components.Length == 0)
                {
                    // Assert can move
                    if (!this.Option.CanMove) throw new NotSupportedException(nameof(Move));
                    // Nothing to move
                    throw new FileNotFoundException(oldPath);
                }

                // One component
                else if (components.Length == 1)
                {
                    // Get reference
                    var component = components[0];
                    // Assert can move
                    if (!component.Option.CanMove) throw new NotSupportedException(nameof(Move));
                    // Convert paths
                    String oldChildPath, newChildPath;
                    if (!component.Path.ParentToChild(oldPath, out oldChildPath)) throw new FileNotFoundException(oldPath);
                    if (!component.Path.ParentToChild(newPath, out newChildPath)) throw new FileNotFoundException(newPath);
                    // Move
                    component.FileSystem.Move(oldChildPath, newChildPath);
                    // Done
                    return;
                }

                // Many components
                else
                {
                    bool supported = false;
                    bool ok = false;
                    foreach (Component component in components)
                    {
                        // Assert can move
                        if (!component.Option.CanMove) continue;
                        // Convert paths
                        String oldChildPath, newChildPath;
                        if (!component.Path.ParentToChild(oldPath, out oldChildPath)) continue;
                        if (!component.Path.ParentToChild(newPath, out newChildPath)) continue;

                        try
                        {
                            component.FileSystem.Move(oldChildPath, newChildPath);
                            ok = true; supported = true;
                        }
                        catch (FileNotFoundException) { supported = true; }
                        catch (NotSupportedException) { }
                    }
                    if (!supported) throw new NotSupportedException(nameof(Move));
                    if (!ok) throw new FileNotFoundException(oldPath);
                }
            }
            // Update references in the expception and let it fly
            catch (FileSystemException e) when (FileSystemExceptionUtil.Set(e, sourceFileSystem, oldPath))
            {
                // Never goes here
                throw new NotSupportedException(nameof(Open));
            }
        }

        /// <summary>
        /// Create a directory, or multiple cascading directories.
        /// 
        /// If directory at <paramref name="path"/> already exists, then returns without exception.
        /// </summary>
        /// <param name="path">Relative path to file. Directory separator is "/". The root is without preceding slash "", e.g. "dir/dir2"</param>
        /// <returns>true if directory exists after the method, false if directory doesn't exist</returns>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is an empty string (""), contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support create directory</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        public void CreateDirectory(string path)
        {
            // Assert argument
            if (path == null) throw new ArgumentNullException(nameof(path));
            // Assert not disposed
            if (IsDisposed) throw new ObjectDisposedException(GetType().FullName);

            try
            {
                // Get reference
                Component[] components = this.components.Array;
                // Zero components
                if (components.Length == 0)
                {
                    // Assert can create
                    if (!this.Option.CanCreateDirectory) throw new NotSupportedException(nameof(CreateDirectory));
                    // No filesystem
                    throw new NotSupportedException(nameof(Mount));
                }

                // One component
                else if (components.Length == 1)
                {
                    // Get reference
                    var component = components[0];
                    // Assert can create
                    if (!component.Option.CanCreateDirectory) throw new NotSupportedException(nameof(CreateDirectory));
                    // Convert Path
                    String childPath;
                    if (!component.Path.ParentToChild(path, out childPath)) throw new FileNotFoundException(path);
                    // Delete
                    component.FileSystem.CreateDirectory(childPath);
                }

                // Many components
                else
                {
                    bool supported = false;
                    bool ok = false;
                    foreach (Component component in components)
                    {
                        // Assert can create
                        if (!component.Option.CanCreateDirectory) continue;
                        // Convert Path
                        String childPath;
                        if (!component.Path.ParentToChild(path, out childPath)) continue;
                        try
                        {
                            component.FileSystem.CreateDirectory(childPath);
                            ok = true; supported = true;
                        }
                        catch (FileNotFoundException) { supported = true; }
                        catch (NotSupportedException) { }
                    }
                    if (!supported) throw new NotSupportedException(nameof(CreateDirectory));
                    if (!ok) throw new FileNotFoundException(path);
                }
            }
            // Update references in the expception and let it fly
            catch (FileSystemException e) when (FileSystemExceptionUtil.Set(e, sourceFileSystem, path))
            {
                // Never goes here
                throw new NotSupportedException(nameof(CreateDirectory));
            }
        }

        /// <inheritdoc/>
        public IFileSystem Mount(string path, params FileSystemAssignment[] mounts)
        {
            // Assert argument
            if (path == null) throw new ArgumentNullException(nameof(path));
            // Assert not disposed
            if (IsDisposed) throw new ObjectDisposedException(GetType().FullName);

            try
            {
                // Get reference
                Component[] components = this.components.Array;
                // Zero components
                if (components.Length == 0)
                {
                    // Assert can mount
                    if (!this.Option.CanMount) throw new NotSupportedException(nameof(Mount));
                    // No filesystem
                    throw new NotSupportedException(nameof(Mount));
                }

                // One component
                else if (components.Length == 1)
                {
                    // Get reference
                    var component = components[0];
                    // Assert can create
                    if (!component.Option.CanMount) throw new NotSupportedException(nameof(Mount));
                    // Convert Path
                    String childPath;
                    if (!component.Path.ParentToChild(path, out childPath)) throw new FileNotFoundException(path);
                    // Mount
                    component.FileSystem.Mount(childPath, mounts);
                }

                // Many components
                else
                {
                    bool supported = false;
                    bool ok = false;
                    foreach (Component component in components)
                    {
                        // Assert can mount
                        if (!component.Option.CanMount) continue;
                        // Convert Path
                        String childPath;
                        if (!component.Path.ParentToChild(path, out childPath)) continue;
                        try
                        {
                            component.FileSystem.Mount(childPath, mounts);
                            ok = true; supported = true;
                            break; // one ok is enough
                        }
                        catch (FileNotFoundException) { supported = true; }
                        catch (NotSupportedException) { }
                    }
                    if (!supported) throw new NotSupportedException(nameof(Mount));
                    if (!ok) throw new NotSupportedException(nameof(Mount));
                }
                // Return self
                return this;
            }
            // Update references in the expception and let it fly
            catch (FileSystemException e) when (FileSystemExceptionUtil.Set(e, sourceFileSystem, path))
            {
                // Never goes here
                throw new NotSupportedException(nameof(Mount));
            }
        }

        /// <summary>
        /// Unmount a filesystem at <paramref name="path"/>.
        /// 
        /// If there is no mount at <paramref name="path"/>, then does nothing.
        /// If there is an open stream to previously mounted filesystem, that stream is unlinked from the filesystem.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>this (parent filesystem)</returns>
        /// <exception cref="NotSupportedException">If operation is not supported</exception>
        public IFileSystem Unmount(string path)
        {
            // Assert argument
            if (path == null) throw new ArgumentNullException(nameof(path));
            // Assert not disposed
            if (IsDisposed) throw new ObjectDisposedException(GetType().FullName);

            try
            {
                // Get reference
                Component[] components = this.components.Array;
                // Zero components
                if (components.Length == 0)
                {
                    // Assert can unmount
                    if (!this.Option.CanUnmount) throw new NotSupportedException(nameof(Unmount));
                    // No filesystem
                    throw new NotSupportedException(nameof(Unmount));
                }

                // One component
                else if (components.Length == 1)
                {
                    // Get reference
                    var component = components[0];
                    // Assert can create
                    if (!component.Option.CanUnmount) throw new NotSupportedException(nameof(Unmount));
                    // Convert Path
                    String childPath;
                    if (!component.Path.ParentToChild(path, out childPath)) throw new FileNotFoundException(path);
                    // Unmount
                    component.FileSystem.Unmount(childPath);
                }

                // Many components
                else
                {
                    bool supported = false;
                    bool ok = false;
                    foreach (Component component in components)
                    {
                        // Assert can unmount
                        if (!component.Option.CanUnmount) continue;
                        // Convert Path
                        String childPath;
                        if (!component.Path.ParentToChild(path, out childPath)) continue;
                        try
                        {
                            component.FileSystem.Unmount(childPath);
                            ok = true; supported = true;
                        }
                        catch (FileNotFoundException) { supported = true; }
                        catch (NotSupportedException) { }
                    }
                    if (!supported) throw new NotSupportedException(nameof(Unmount));
                    if (!ok) throw new NotSupportedException(nameof(Unmount));
                }
                // Return self
                return this;
            }
            // Update references in the expception and let it fly
            catch (FileSystemException e) when (FileSystemExceptionUtil.Set(e, sourceFileSystem, path))
            {
                // Never goes here
                throw new NotSupportedException(nameof(Unmount));
            }
        }

        /// <summary>
        /// List all mounts.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">If operation is not supported</exception>
        public IFileSystemEntryMount[] ListMountPoints()
        {
            // Assert not disposed
            if (IsDisposed) throw new ObjectDisposedException(GetType().FullName);

            try
            {
                // Get reference
                Component[] components = this.components.Array;
                // Zero components
                if (components.Length == 0)
                {
                    // Assert can 
                    if (!this.Option.CanListMountPoints) throw new NotSupportedException(nameof(ListMountPoints));
                    // No filesystem mounts
                    return new IFileSystemEntryMount[0];
                }

                // One component
                if (components.Length == 1)
                {
                    // Get reference
                    var component = components[0];
                    // Assert can create
                    if (!component.Option.CanListMountPoints) throw new NotSupportedException(nameof(ListMountPoints));
                    // List
                    IFileSystemEntryMount[] childEntries = component.FileSystem.ListMountPoints();
                    // Result array to be filled
                    IFileSystemEntryMount[] result = new IFileSystemEntryMount[childEntries.Length];
                    // Is result array filled with null enties
                    int removedCount = 0;
                    // Decorate elements
                    for (int i = 0; i < childEntries.Length; i++)
                    {
                        // Get entry
                        IFileSystemEntryMount e = childEntries[i];
                        // Convert path
                        String parentPath;
                        if (!component.Path.ChildToParent(e.Path, out parentPath)) { /* Path conversion failed. Omit entry. Remove it later */ removedCount++; continue; }
                        // Decorate
                        result[i] = (IFileSystemEntryMount)CreateEntry(e, sourceFileSystem, parentPath, component.Option);
                    }
                    // Remove null entries
                    if (removedCount > 0)
                    {
                        IFileSystemEntryMount[] newResult = new IFileSystemEntryMount[result.Length - removedCount];
                        int ix = 0;
                        foreach (var e in result) if (e != null) newResult[ix++] = e;
                        result = newResult;
                    }
                    // Return entries
                    return result;
                }

                // Many components
                else
                {
                    // result from each filesystem
                    StructList4<(Component, IFileSystemEntryMount[])> entryArrays = new StructList4<(Component, IFileSystemEntryMount[])>();
                    // list supported
                    bool supported = false;
                    // Number of total entries
                    int entryCount = 0;

                    // Create hashset for removing overlapping entry paths
                    HashSet<string> paths = new HashSet<string>();
                    foreach (Component component in components)
                    {
                        // Assert component can browse
                        if (!component.Option.CanListMountPoints) continue;
                        // Catch NotSupported
                        try
                        {
                            // Browse
                            IFileSystemEntryMount[] component_entries = component.FileSystem.ListMountPoints();
                            entryArrays.Add((component, component_entries));
                            entryCount += component_entries.Length;
                            supported = true;
                        }
                        catch (DirectoryNotFoundException) { supported = true; }
                        catch (NotSupportedException) { }
                    }
                    if (!supported) throw new NotSupportedException(nameof(ListMountPoints));

                    // Create list for result
                    List<IFileSystemEntryMount> result = new List<IFileSystemEntryMount>(entryCount);

                    for (int i = 0; i < entryArrays.Count; i++)
                    {
                        // Get component and result array
                        (Component c, IFileSystemEntry[] array) = entryArrays[i];
                        // Prune and decorate
                        foreach (IFileSystemEntry e in array)
                        {
                            // Remove already existing entry
                            if (paths != null && !paths.Add(e.Path)) continue;
                            // Convert path
                            String parentPath;
                            if (!c.Path.ChildToParent(e.Path, out parentPath)) continue;
                            // Decorate
                            IFileSystemEntryMount ee = (IFileSystemEntryMount)CreateEntry(e, sourceFileSystem, parentPath, c.Option);
                            // Add to result
                            result.Add(ee);
                        }
                    }

                    // Return as array
                    return result.ToArray();
                }

            }
            // Update references in the expception and let it fly
            catch (FileSystemException e) when (FileSystemExceptionUtil.Set(e, sourceFileSystem, null))
            {
                // Never goes here
                throw new NotSupportedException(nameof(ListMountPoints));
            }
        }

        /// <summary>
        /// Attach an <paramref name="observer"/> on to a single file or directory. 
        /// Observing a directory will observe the whole subtree.
        /// </summary>
        /// <param name="filter">path to file or directory. The directory separator is "/". The root is without preceding slash "", e.g. "dir/dir2"</param>
        /// <param name="observer"></param>
        /// <param name="state">(optional) </param>
        /// <returns>dispose handle</returns>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="filter"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="filter"/> contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support observe</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="filter"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        public override IFileSystemObserver Observe(string filter, IObserver<IFileSystemEvent> observer, object state = null)
        {
            // Assert argument
            if (filter == null) throw new ArgumentNullException(nameof(filter));
            // Assert not disposed
            if (IsDisposed) throw new ObjectDisposedException(GetType().FullName);
            // Assert supported
            if (!Option.CanObserve) throw new NotSupportedException(nameof(Observe));
            // Get reference
            Component[] components = this.components.Array;
            // Zero components
            if (components.Length == 0) throw new NotSupportedException(nameof(Observe));

            // Create adapter
            ObserverDecorator adapter = new ObserverDecorator(this, filter, observer, state, true);
            try
            {
                // Send IFileSystemEventStart, must be sent before subscribing forwardees
                observer.OnNext(new FileSystemEventStart(adapter, DateTimeOffset.UtcNow));

                // Observe each component
                foreach (Component component in components)
                {
                    // Assert can observe
                    if (!component.Option.CanObserve) continue;
                    // Convert Path
                    String childPath;
                    if (!component.Path.ParentToChild(filter, out childPath)) continue;
                    try
                    {
                        // Try Observe
                        IDisposable disposable = component.FileSystem.Observe(childPath, adapter, new ObserverDecorator.StateInfo(component.Path, component));
                        // Attach disposable
                        ((IDisposeList)adapter).AddDisposable(disposable);
                    }
                    catch (NotSupportedException) { }
                    catch (ArgumentException) { } // FileSystem.PatternObserver throws directory is not found, TODO create contract for proper exception
                }

                // Return adapter
                return adapter;
            }
            // Update references in the expception and let it fly
            catch (FileSystemException e) when (FileSystemExceptionUtil.Set(e, sourceFileSystem, filter))
            {
                // Never goes here
                throw new NotSupportedException(nameof(Observe));
            }
            catch (Exception) when (DisposeObserver(adapter)) { /*Never goes here*/ throw new Exception(); }
            bool DisposeObserver(ObserverDecorator handle) { handle?.Dispose(); return false; }
        }

        /// <summary>
        /// Add source <see cref="IFileSystem"/> instances to be disposed along with this decoration.
        /// </summary>
        /// <returns>self</returns>
        public FileSystemDecoration AddSourceToBeDisposed()
        {
            AddDisposables(Decorees);
            return this;
        }

        /// <summary>
        /// Invoke <paramref name="disposeAction"/> on the dispose of the object.
        /// 
        /// If parent object is disposed or being disposed, the disposable will be disposed immedialy.
        /// </summary>
        /// <param name="disposeAction"></param>
        /// <returns>self</returns>
        public FileSystemDecoration AddDisposeAction(Action<FileSystemDecoration> disposeAction)
        {
            // Argument error
            if (disposeAction == null) throw new ArgumentNullException(nameof(disposeAction));
            // Parent is disposed/ing
            if (IsDisposing) { disposeAction(this); return this; }
            // Adapt to IDisposable
            IDisposable disposable = new DisposeAction<FileSystemDecoration>(disposeAction, this);
            // Add to list
            lock (m_disposelist_lock) disposeList.Add(disposable);
            // Check parent again
            if (IsDisposing) { lock (m_disposelist_lock) disposeList.Remove(disposable); disposable.Dispose(); return this; }
            // OK
            return this;
        }

        /// <summary>
        /// Invoke <paramref name="disposeAction"/> on the dispose of the object.
        /// 
        /// If parent object is disposed or being disposed, the disposable will be disposed immedialy.
        /// </summary>
        /// <param name="disposeAction"></param>
        /// <param name="state"></param>
        /// <returns>self</returns>
        public FileSystemDecoration AddDisposeAction(Action<object> disposeAction, object state)
        {
            ((IDisposeList)this).AddDisposeAction(disposeAction, state);
            return this;
        }

        /// <summary>
        /// Add <paramref name="disposable"/> to list of objects to be disposed along with the system.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns>filesystem</returns>
        public FileSystemDecoration AddDisposable(object disposable)
        {
            ((IDisposeList)this).AddDisposable(disposable);
            return this;
        }

        /// <summary>
        /// Add <paramref name="disposables"/> to list of objects to be disposed along with the system.
        /// </summary>
        /// <param name="disposables"></param>
        /// <returns>filesystem</returns>
        public FileSystemDecoration AddDisposables(IEnumerable disposables)
        {
            ((IDisposeList)this).AddDisposables(disposables);
            return this;
        }

        /// <summary>
        /// Remove <paramref name="disposable"/> from dispose list.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns></returns>
        public FileSystemDecoration RemoveDisposable(object disposable)
        {
            ((IDisposeList)this).RemoveDisposable(disposable);
            return this;
        }

        /// <summary>
        /// Remove <paramref name="disposables"/> from dispose list.
        /// </summary>
        /// <param name="disposables"></param>
        /// <returns></returns>
        public FileSystemDecoration RemoveDisposables(IEnumerable disposables)
        {
            ((IDisposeList)this).RemoveDisposables(disposables);
            return this;
        }

        /// <summary>
        /// Get file systems
        /// </summary>
        /// <returns></returns>
        public IEnumerator<IFileSystem> GetEnumerator()
            => ((IEnumerable<IFileSystem>)this.Decorees).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => this.Decorees.GetEnumerator();

        /// <summary>
        /// Print info
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => GetType().Name + "(" + String.Join<IFileSystem>(", ", Decorees) + ")";

        /// <summary>Flattened options for (slight) performance gain.</summary>
        public class Options : IFileSystemOptionBrowse, IFileSystemOptionObserve, IFileSystemOptionOpen, IFileSystemOptionDelete, IFileSystemOptionMove, IFileSystemOptionCreateDirectory, IFileSystemOptionMount, IFileSystemOptionPath, IFileSystemOptionSubPath
        {
            // TODO Implement Hash-Equals //
            // TODO Implement Union & Intersection //

            /// <inheritdoc/>
            public bool CanBrowse { get; set; }
            /// <inheritdoc/>
            public bool CanGetEntry { get; set; }
            /// <inheritdoc/>
            public bool CanObserve { get; set; }
            /// <inheritdoc/>
            public bool CanSetEventDispatcher { get; set; }
            /// <inheritdoc/>
            public bool CanOpen { get; set; }
            /// <inheritdoc/>
            public bool CanRead { get; set; }
            /// <inheritdoc/>
            public bool CanWrite { get; set; }
            /// <inheritdoc/>
            public bool CanCreateFile { get; set; }
            /// <inheritdoc/>
            public bool CanDelete { get; set; }
            /// <inheritdoc/>
            public bool CanMove { get; set; }
            /// <inheritdoc/>
            public bool CanCreateDirectory { get; set; }
            /// <inheritdoc/>
            public bool CanMount { get; set; }
            /// <inheritdoc/>
            public bool CanUnmount { get; set; }
            /// <inheritdoc/>
            public bool CanListMountPoints { get; set; }
            /// <inheritdoc/>
            public FileSystemCaseSensitivity CaseSensitivity { get; set; }
            /// <inheritdoc/>
            public bool EmptyDirectoryName { get; set; }
            /// <inheritdoc/>
            public string SubPath { get; set; }

            /// <summary>
            /// Read options from <paramref name="option"/> and return flattened object.
            /// </summary>
            /// <param name="option"></param>
            /// <returns></returns>
            public static Options Read(IFileSystemOption option)
            {
                Options result = new Options();
                result.CanBrowse = option.CanBrowse();
                result.CanGetEntry = option.CanGetEntry();
                result.CanObserve = option.CanObserve();
                result.CanSetEventDispatcher = option.CanSetEventDispatcher();
                result.CanOpen = option.CanOpen();
                result.CanRead = option.CanRead();
                result.CanWrite = option.CanWrite();
                result.CanCreateFile = option.CanCreateFile();
                result.CanDelete = option.CanDelete();
                result.CanMount = option.CanMount();
                result.CanCreateFile = option.CanCreateFile();
                result.CanDelete = option.CanDelete();
                result.CanMove = option.CanMove();
                result.CanCreateDirectory = option.CanCreateDirectory();
                result.CanMount = option.CanMount();
                result.CanUnmount = option.CanUnmount();
                result.CanListMountPoints = option.CanListMountPoints();
                result.SubPath = option.SubPath();
                return result;
            }

            /// <summary>
            /// Create intersection with another option
            /// </summary>
            /// <param name="option"></param>
            /// <returns>this if <paramref name="option"/> is null or new instance with intersection</returns>
            public Options Intersection(IFileSystemOption option)
            {
                if (option == null) return this;
                Options result = new Options();
                result.CanBrowse = this.CanBrowse | option.CanBrowse();
                result.CanGetEntry = this.CanGetEntry | option.CanGetEntry();
                result.CanObserve = this.CanObserve | option.CanObserve();
                result.CanSetEventDispatcher = this.CanSetEventDispatcher | option.CanSetEventDispatcher();
                result.CanOpen = this.CanOpen | option.CanOpen();
                result.CanRead = this.CanRead | option.CanRead();
                result.CanWrite = this.CanWrite | option.CanWrite();
                result.CanCreateFile = this.CanCreateFile | option.CanCreateFile();
                result.CanDelete = this.CanDelete | option.CanDelete();
                result.CanMount = this.CanMount | option.CanMount();
                result.CanCreateFile = this.CanCreateFile | option.CanCreateFile();
                result.CanDelete = this.CanDelete | option.CanDelete();
                result.CanMove = this.CanMove | option.CanMove();
                result.CanCreateDirectory = this.CanCreateDirectory | option.CanCreateDirectory();
                result.CanMount = this.CanMount | option.CanMount();
                result.CanUnmount = this.CanUnmount | option.CanUnmount();
                result.CanListMountPoints = this.CanListMountPoints | option.CanListMountPoints();
                result.SubPath = this.SubPath ?? option.SubPath();
                return result;
            }
        }


        /// <summary>Override this to change entry class. Must implement <see cref="IFileSystemEntryMount"/></summary>
        protected virtual IFileSystemEntry CreateEntry(IFileSystemEntry original, IFileSystem newFileSystem, string newPath, Options optionModifier)
            => new Entry(original, newFileSystem, newPath, optionModifier);

        /// <summary>
        /// New overriding filesystem, Path and Option modifier
        /// </summary>
        protected internal class Entry : Lexical.FileSystem.Decoration.FileSystemEntryDecoration
        {
            /// <summary>New overriding filesystem.</summary>
            protected IFileSystem newFileSystem;
            /// <summary>New overriding path.</summary>
            protected string newPath;
            /// <summary>New overriding filesystem.</summary>
            public override IFileSystem FileSystem => newFileSystem;
            /// <summary>New path.</summary>
            public override string Path => newPath;
            /// <summary>(optional) Option that will be intersected lazily with original options.</summary>
            protected Options optionModifier;
            /// <summary>Lazily construction intersection of <see cref="optionModifier"/> and Original.Option()</summary>
            protected IFileSystemOption optionIntersection;
            /// <summary>Intersection of Original.Option() and <see cref="optionModifier"/></summary>
            public override IFileSystemOption Option => optionIntersection ?? (optionIntersection = optionModifier == null ? Original.Options() : optionModifier.Intersection(Original.Options()));
            /// <summary>
            /// Create decoration with <paramref name="newFileSystem"/>.
            /// </summary>
            /// <param name="original"></param>
            /// <param name="newFileSystem"></param>
            /// <param name="newPath"></param>
            /// <param name="optionModifier">(optional) option that will be applied to original option with intersection</param>
            public Entry(IFileSystemEntry original, IFileSystem newFileSystem, string newPath, Options optionModifier) : base(original)
            {
                this.newFileSystem = newFileSystem;
                this.newPath = newPath;
                this.optionModifier = optionModifier;
            }
        }
    }

}