using Lexical.FileSystem;
using System;
using System.IO;
using System.Text;

namespace docs
{
    // <docs>
    class ExampleFileSystem : IFileSystem, IFileSystemBrowse, IFileSystemOpen, IPathInfo
    {
        public bool CanOpen => true;
        public bool CanRead => true;
        public bool CanWrite => true;
        public bool CanCreateFile => true;
        public bool CanBrowse => true;
        public bool CanGetEntry => true;
        public FileSystemCaseSensitivity CaseSensitivity => FileSystemCaseSensitivity.CaseSensitive;
        public bool EmptyDirectoryName => false;

        DirectoryEntry rootEntry;
        FileEntry fileEntry;

        public ExampleFileSystem()
        {
            DateTimeOffset time = DateTimeOffset.MinValue;
            rootEntry = new DirectoryEntry(this, "", "", time, time, null);
            fileEntry = new FileEntry(this, "example.txt", "example.txt", time, time, 11L, null);
        }

        public IDirectoryContent Browse(string path, IOption option = null)
        {
            // Browse root
            if (path == rootEntry.Path) return new DirectoryContent(this, path, new IEntry[] { fileEntry });
            // Not found
            return new DirectoryNotFound(this, path);
        }

        public IEntry GetEntry(string path, IOption option = null)
        {
            // Root entry
            if (path == rootEntry.Path) return rootEntry;
            // File entry
            if (path == fileEntry.Path) return fileEntry;
            // Entry not found
            return null;
        }

        public Stream Open(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare, IOption option = null)
        {
            if (path != fileEntry.Path) throw new FileNotFoundException(path);
            if (fileMode != FileMode.Open) throw new NotSupportedException();
            if (fileAccess != FileAccess.Read) throw new NotSupportedException();
            byte[] data = UTF8Encoding.UTF8.GetBytes("Hello World");
            return new MemoryStream(data);
        }
    }
    // </docs>
}
