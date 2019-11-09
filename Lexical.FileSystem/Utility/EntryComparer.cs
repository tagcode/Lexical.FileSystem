// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           10.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Internal;
using System.Collections.Generic;

namespace Lexical.FileSystem.Utility
{
    /// <summary>
    /// Equality comparer compares <see cref="IEntry"/> for Path, Date, Length and FileSystem equality.
    /// 
    /// Order comparer compares by type, then by name. 
    /// </summary>
    public class EntryComparer
    {
        /// <summary>Equality comparer for: Path, Date, Length, Type</summary>
        private static IEqualityComparer<IEntry> pathDateLengthTypeEqualityComparer = new _PathDateLengthTypeEqualityComparer();
        /// <summary>Comparer that sorts by: Type, Name.</summary>
        private static IComparer<IEntry> typeNameComparer = new _TypeNameComparer();

        /// <summary>Equality comparer for: Path, Date, Length, Type</summary>
        public static IEqualityComparer<IEntry> PathDateLengthTypeEqualityComparer => pathDateLengthTypeEqualityComparer;
        /// <summary>Comparer that sorts by: Type, Name.</summary>
        public static IComparer<IEntry> TypeNameComparer => typeNameComparer;

        /// <summary>Equality comparer for: Path, Date, Length, Type</summary>
        class _PathDateLengthTypeEqualityComparer : IEqualityComparer<IEntry>
        {
            /// <summary>
            /// Compare entries.
            /// 
            /// Two nulls are considered equal.
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <returns></returns>
            public bool Equals(IEntry x, IEntry y)
            {
                if (x == null && y == null) return true;
                if (x == null || y == null) return false;

                bool xfile = x.IsFile(), yfile = y.IsFile();
                bool xdir = x.IsDirectory(), ydir = y.IsDirectory();
                bool xdrive = x.IsDrive(), ydrive = y.IsDrive();
                //bool xmountpoint = x.IsMountpoint(), ymountpoint = y.IsMountpoint();

                if (xfile != yfile) return false;
                if (xdir != ydir) return false;
                if (xdrive != ydrive) return false;

                if (x.FileSystem != y.FileSystem) return false;
                if (x.Path != y.Path) return false;
                if (x.LastModified != y.LastModified) return false;
                if (xfile && yfile && x.Length() != y.Length()) return false;

                return true;
            }

        /// <summary>
        /// Calculate hash
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public int GetHashCode(IEntry entry)
        {
            int hash = 0;
            if (entry.FileSystem != null) hash ^= entry.FileSystem.GetHashCode();

            if (entry.Path != null) hash = hash * 13 + entry.Path.GetHashCode();
            else if (entry.Name != null) hash = hash * 13 + entry.Name.GetHashCode();

            hash = hash * 7 + entry.LastModified.GetHashCode();

            hash = hash * 3 + entry.GetType().GetHashCode();

            if (entry is IFileEntry file) hash = hash * 11 + file.Length.GetHashCode();

            return hash;
        }
        }

        /// <summary>Comparer that sorts by: Type, Name.</summary>
        class _TypeNameComparer : IComparer<IEntry>
        {
            /// <summary>
            /// Sort by type, then name, using AlphaNumericComparer
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <returns></returns>
            public int Compare(IEntry x, IEntry y)
            {
                if (x == null && y == null) return 0;
                if (x == null) return -1;
                if (y == null) return 1;

                // Order by type
                int x_score = 0;
                if (x.IsDrive()) x_score += 4;
                if (x.IsDirectory()) x_score += 2;
                if (x.IsFile()) x_score += 1;

                int y_score = 0;
                if (y.IsDrive()) y_score += 4;
                if (y.IsDirectory()) y_score += 2;
                if (y.IsFile()) y_score += 1;

                int d = x_score - y_score;
                if (d != 0) return d;

                // Order by name
                return AlphaNumericComparer.Default.Compare(x.Name, y.Name);
            }
        }

    }
}
