// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           10.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System.Collections.Generic;

namespace Lexical.FileSystem.Utilities
{
    /// <summary>
    /// Equality comparer compares <see cref="IFileSystemEntry"/> for Path, Date, Length and FileSystem equality.
    /// 
    /// Order comparer compares by type, then by name. 
    /// </summary>
    public class FileSystemEntryComparer : IEqualityComparer<IFileSystemEntry>, IComparer<IFileSystemEntry>
    {
        /// <summary>
        /// Singleton instance.
        /// </summary>
        private static FileSystemEntryComparer instance = new FileSystemEntryComparer();

        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static FileSystemEntryComparer Instance => instance;

        /// <summary>
        /// Compare entries.
        /// 
        /// Two nulls are considered equal.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(IFileSystemEntry x, IFileSystemEntry y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;

            IFileSystemEntryFile xfile = x as IFileSystemEntryFile, yfile = y as IFileSystemEntryFile;
            IFileSystemEntryDirectory xdir = x as IFileSystemEntryDirectory, ydir = y as IFileSystemEntryDirectory;
            IFileSystemEntryDrive xdrive = x as IFileSystemEntryDrive, ydrive = y as IFileSystemEntryDrive;

            if ((xfile != null) != (yfile != null)) return false;
            if ((xdir != null) != (ydir != null)) return false;
            if ((xdrive != null) != (ydrive != null)) return false;

            if (x.FileSystem != y.FileSystem) return false;
            if (x.Path != y.Path) return false;
            if (x.LastModified != y.LastModified) return false;
            if ((xfile != null) && (yfile != null) && xfile.Length != yfile.Length) return false;

            return true;
        }

        /// <summary>
        /// Calculate hash
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public int GetHashCode(IFileSystemEntry entry)
        {
            int hash = 0;
            if (entry.FileSystem != null) hash ^= entry.FileSystem.GetHashCode();

            if (entry.Path != null) hash = hash * 13 + entry.Path.GetHashCode();
            else if (entry.Name != null) hash = hash * 13 + entry.Name.GetHashCode();

            hash = hash * 7 + entry.LastModified.GetHashCode();

            hash = hash * 3 + entry.GetType().GetHashCode();

            if (entry is IFileSystemEntryFile file) hash = hash * 11 + file.Length.GetHashCode();

            return hash;
        }

        /// <summary>
        /// Sort by type, then name, using AlphaNumericComparer
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int Compare(IFileSystemEntry x, IFileSystemEntry y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            // Order by type
            int x_score = 0, y_score = 0;
            if (x is IFileSystemEntryDrive) x_score += 4;
            if (x is IFileSystemEntryDirectory) x_score += 2;
            if (x is IFileSystemEntryFile) x_score += 1;
            if (y is IFileSystemEntryDrive) y_score += 4;
            if (y is IFileSystemEntryDirectory) y_score += 2;
            if (y is IFileSystemEntryFile) y_score += 1;
            int d = x_score - y_score;
            if (d != 0) return d;

            // Order by name
            return AlphaNumericComparer.Default.Compare(x.Name, y.Name);
        }

    }
}
