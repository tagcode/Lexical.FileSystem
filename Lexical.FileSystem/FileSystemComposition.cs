// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           14.6.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Composition of multiple <see cref="IFileSystem"/>s.
    /// </summary>
    public class FileSystemComposition : FileSystemBase, IEnumerable<IFileSystem>, IFileSystemBrowse, IFileSystemObserve, IFileSystemOpen, IFileSystemDelete, IFileSystemMove, IFileSystemCreateDirectory
    {
        /// <summary>
        /// File system components.
        /// </summary>
        protected IFileSystem[] fileSystems;

        /// <summary>
        /// Count 
        /// </summary>
        public int Count => fileSystems.Length;

        /// <summary>
        /// Union of capabilities
        /// </summary>
        protected FileSystemCapabilities capabilities;

        /// <summary>
        /// Union of capabilities.
        /// </summary>
        public override FileSystemCapabilities Capabilities => capabilities;

        /// <summary>
        /// Create composition of file systems
        /// </summary>
        /// <param name="fileSystems"></param>
        public FileSystemComposition(params IFileSystem[] fileSystems)
        {
            this.fileSystems = fileSystems;
            foreach (IFileSystem fs in fileSystems)
                capabilities |= fs.Capabilities;            
        }

        /// <summary>
        /// Create colletion of file systems
        /// </summary>
        /// <param name="fileSystems"></param>
        public FileSystemComposition(IEnumerable<IFileSystem> fileSystems)
        {
            this.fileSystems = fileSystems.ToArray();
            foreach (IFileSystem fs in this.fileSystems) capabilities |= fs.Capabilities;
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
        public FileSystemEntry[] Browse(string path)
        {
            StructList24<FileSystemEntry> entries = new StructList24<FileSystemEntry>();
            bool exists = false, supported = false;
            foreach (var filesystem in fileSystems)
            {
                if ((filesystem.Capabilities & FileSystemCapabilities.Browse) == 0UL) continue;
                try
                {
                    FileSystemEntry[] list = filesystem.Browse(path);
                    exists = true; supported = true;
                    foreach (FileSystemEntry e in list)
                        entries.Add(new FileSystemEntry { FileSystem = this, Name = e.Name, Path = e.Path, Length = e.Length, Type = FileSystemEntryType.File, LastModified = e.LastModified });
                }
                catch (DirectoryNotFoundException) { supported = true; }
                catch (NotSupportedException) { }
            }
            if (!supported) throw new NotSupportedException(nameof(Browse));
            if (!exists) throw new DirectoryNotFoundException(path);
            return entries.ToArray();
        }

        /// <summary>
        /// Tests whether a file or directory exists.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support exists</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path, such as when access is Write or ReadWrite and the file or directory is set for read-only access.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        public bool Exists(string path)
        {
            bool supported = false;
            foreach (var filesystem in fileSystems)
            {
                if ((filesystem.Capabilities & FileSystemCapabilities.Exists) == 0UL) continue;
                try
                {
                    bool exists = filesystem.Exists(path);
                    if (exists) return true;
                    supported = true;
                }
                catch (DirectoryNotFoundException) { supported = true; }
                catch (NotSupportedException) { }
            }
            if (!supported) throw new NotSupportedException(nameof(Browse));
            return false;
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
            bool supported = false;
            foreach (var filesystem in fileSystems)
            {
                if ((filesystem.Capabilities & FileSystemCapabilities.Open) == 0UL) continue;
                try
                {
                    return filesystem.Open(path, fileMode, fileAccess, fileShare);
                }
                catch (FileNotFoundException) { supported = true; }
                catch (NotSupportedException) { }
            }
            if (!supported) throw new NotSupportedException(nameof(Browse));
            throw new FileNotFoundException(path);
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
            bool supported = false;
            bool ok = false;
            foreach (var filesystem in fileSystems)
            {
                if ((filesystem.Capabilities & FileSystemCapabilities.Delete) == 0UL) continue;
                try
                {
                    filesystem.Delete(path, recursive);
                    ok = true; supported = true;
                }
                catch (FileNotFoundException) { supported = true; }
                catch (NotSupportedException) { }
            }
            if (!supported) throw new NotSupportedException(nameof(Browse));
            if (!ok) throw new FileNotFoundException(path);
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
            bool supported = false;
            bool ok = false;
            foreach (IFileSystem filesystem in fileSystems)
            {
                if ((filesystem.Capabilities & FileSystemCapabilities.Move) == 0UL) continue;
                try
                {
                    filesystem.Move(oldPath, newPath);
                    ok = true; supported = true;
                }
                catch (FileNotFoundException) { supported = true; }
                catch (NotSupportedException) { }
            }
            if (!supported) throw new NotSupportedException(nameof(Browse));
            if (!ok) throw new FileNotFoundException(oldPath);
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
            bool supported = false;
            bool ok = false;
            foreach (IFileSystem filesystem in fileSystems)
            {
                if ((filesystem.Capabilities & FileSystemCapabilities.CreateDirectory) == 0UL) continue;
                try
                {
                    filesystem.CreateDirectory(path);
                    ok = true; supported = true;
                }
                catch (FileNotFoundException) { supported = true; }
                catch (NotSupportedException) { }
            }
            if (!supported) throw new NotSupportedException(nameof(Browse));
            if (!ok) throw new FileNotFoundException(path);
        }

        /// <summary>
        /// Attach an <paramref name="observer"/> on to a single file or directory. 
        /// Observing a directory will observe the whole subtree.
        /// </summary>
        /// <param name="path">path to file or directory. The directory separator is "/". The root is without preceding slash "", e.g. "dir/dir2"</param>
        /// <param name="observer"></param>
        /// <returns>dispose handle</returns>
        /// <exception cref="IOException">On unexpected IO error</exception>
        /// <exception cref="SecurityException">If caller did not have permission</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> contains only white space, or contains one or more invalid characters</exception>
        /// <exception cref="NotSupportedException">The <see cref="IFileSystem"/> doesn't support observe</exception>
        /// <exception cref="UnauthorizedAccessException">The access requested is not permitted by the operating system for the specified path.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. For example, on Windows-based platforms, paths must be less than 248 characters, and file names must be less than 260 characters.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="path"/> refers to a non-file device, such as "con:", "com1:", "lpt1:", etc.</exception>
        /// <exception cref="ObjectDisposedException"/>
        public IDisposable Observe(string path, IObserver<FileSystemEntryEvent> observer)
        {
            StructList12<IDisposable> disposables = new StructList12<IDisposable>();
            ObserverAdapter adapter = new ObserverAdapter(this, observer);
            foreach (var filesystem in fileSystems)
            {
                if ((filesystem.Capabilities & FileSystemCapabilities.Observe) == 0UL) continue;
                try
                {
                    IDisposable disposable = filesystem.Observe(path, adapter);
                    disposables.Add(disposable);
                }
                catch (NotSupportedException) { }
            }
            if (disposables.Count == 0) throw new NotSupportedException(nameof(Observe));
            adapter.disposables = disposables.ToArray();
            return adapter;
        }

        class ObserverAdapter : IDisposable, IObserver<FileSystemEntryEvent>
        {
            IFileSystem filesystem;
            IObserver<FileSystemEntryEvent> observer;
            public IDisposable[] disposables;

            public ObserverAdapter(IFileSystem filesystem, IObserver<FileSystemEntryEvent> observer)
            {
                this.filesystem = filesystem;
                this.observer = observer;
            }

            public void OnCompleted()
                => observer.OnCompleted();

            public void OnError(Exception error)
                => observer.OnError(error);

            public void OnNext(FileSystemEntryEvent value)
                => observer.OnNext(new FileSystemEntryEvent { FileSystem = filesystem, ChangeEvents = value.ChangeEvents, Path = value.Path });

            public void Dispose()
            {
                StructList4<Exception> errors = new StructList4<Exception>();
                foreach (IDisposable d in disposables)
                {
                    try
                    {
                        d.Dispose();
                    }
                    catch (AggregateException ae)
                    {
                        foreach (Exception e in ae.InnerExceptions) errors.Add(e);
                    }
                    catch (Exception e)
                    {
                        errors.Add(e);
                    }
                }

                if (errors.Count > 0) throw new AggregateException(errors);
            }
        }

        /// <summary>
        /// Add <paramref name="disposable"/> to list of objects to be disposed along with the system.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns>filesystem</returns>
        public FileSystemComposition AddDisposable(object disposable) => AddDisposableBase(disposable) as FileSystemComposition;

        /// <summary>
        /// Remove disposable from dispose list.
        /// </summary>
        /// <param name="disposable"></param>
        /// <returns></returns>
        public FileSystemComposition RemoveDisposable(object disposable) => RemoveDisposableBase(disposable) as FileSystemComposition;

        /// <summary>
        /// Get file systems
        /// </summary>
        /// <returns></returns>
        public IEnumerator<IFileSystem> GetEnumerator()
            => ((IEnumerable<IFileSystem>)fileSystems).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => fileSystems.GetEnumerator();

        /// <summary>
        /// Print info
        /// </summary>
        /// <returns></returns>
        public override string ToString()
            => String.Join<IFileSystem>(", ", fileSystems);

    }

}