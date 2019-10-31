// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           23.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Internal;
using Lexical.FileSystem.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static Lexical.FileSystem.PrintTree;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Visit extension methods for <see cref="IFileSystem"/>.
    /// </summary>
    public static class TreeVisit
    {
        /// <summary>
        /// Vists tree structure of filesystem. 
        /// 
        /// Starts at <paramref name="path"/> if provided, otherwise starts at root "".
        /// <paramref name="depth"/> sets maximum visit depths.
        /// 
        /// ""
        /// ├──""
        /// │  ├──"mnt"
        /// │  ├──"tmp"
        /// │  │  └──"helloworld.txt"
        /// │  └──"usr"
        /// │     └──"lex"
        /// └──"c:"
        ///    └──"dir"
        ///       └──"dir"
        /// 
        /// 
        /// Any thrown exceptions are printed into the line that produced the error.
        /// </summary>
        /// <param name="filesystem"></param>
        /// <param name="path"></param>
        /// <param name="depth">maximum visit depth</param>
        /// <param name="token">(optional) filesystem implementation specific token, such as session, security token or credential. Used for authorizing or facilitating the action.</param>
        /// <exception cref="Exception">any exception that GetEntry or Browse can throw</exception>
        /// <exception cref="IOException">If Browse returns an entry whose path is not under parent entry's path</exception>
        public static IEnumerable<Line> VisitTree(this IFileSystem filesystem, string path = "", int depth = Int32.MaxValue, IFileSystemToken token = null)
        {
            List<Line> queue = new List<Line>();
            IFileSystemEntry entry = filesystem.GetEntry(path, token);
            if (entry == null) yield break;
            queue.Add( new Line(entry, 0, 0UL) );
            while (queue.Count > 0)
            {
                // Next entry
                int lastIx = queue.Count - 1;
                Line line = queue[lastIx];
                queue.RemoveAt(lastIx);

                // Children
                if (line.Entry.IsDirectory() && line.Level < depth)
                {
                    int startIndex = queue.Count;
                    try
                    {
                        // Browse children
                        IFileSystemEntry[] children = filesystem.Browse(line.Entry.Path, token);
                        // Assert children don't refer to the parent of the parent
                        foreach (IFileSystemEntry child in children) if (line.Entry.Path.StartsWith(child.Path)) throw new IOException($"{child.Path} cannot be child of {line.Entry.Path}");
                        // Bitmask when this level continues
                        ulong levelContinuesBitMask = line.LevelContinuesBitMask | (line.Level < 64 ? 1UL << line.Level : 0UL);
                        // Add children in reverse order
                        foreach (IFileSystemEntry child in children) queue.Add(new Line(child, line.Level + 1, levelContinuesBitMask));
                        // Sort the entries that were added
                        if (children.Length>1) sorter.QuickSortInverse(ref queue, startIndex, queue.Count - 1);
                        // Last entry doesn't continue on its level.
                        if (children.Length>=1) queue[startIndex] = queue[startIndex].NewLevelContinuesBitMask(line.LevelContinuesBitMask);
                    }
                    catch (Exception e)
                    {
                        // Add error to be yielded along
                        line.Error = e;
                    }
                }

                // yield line
                yield return line;
            }
        }

        // Line sorter
        static StructListSorter<List<Line>, Line> sorter = new StructListSorter<List<Line>, Line>(Line.Comparer.TypeNameComparer);

        /// <summary>
        /// Tree visit line.
        /// </summary>
        public struct Line
        {
            /// <summary>
            /// Visited tree entry.
            /// </summary>
            public readonly IFileSystemEntry Entry;

            /// <summary>
            /// Visit depth, starts at 0.
            /// </summary>
            public readonly int Level;

            /// <summary>
            /// Bitmask for each level on whether the level has more entries to come in the enumerator.
            /// </summary>
            public readonly ulong LevelContinuesBitMask;

            /// <summary>
            /// (optional) Browse() error is placed here.
            /// </summary>
            public Exception Error;

            /// <summary>
            /// Entry path.
            /// </summary>
            public string Path => Entry.Path;

            /// <summary>
            /// Entry name.
            /// </summary>
            public string Name => Entry.Name;

            /// <summary>
            /// Create line
            /// </summary>
            /// <param name="entry"></param>
            /// <param name="level"></param>
            /// <param name="levelContinuesBitMask"></param>
            /// <param name="error">(optional) initial error</param>
            public Line(IFileSystemEntry entry, int level, ulong levelContinuesBitMask, Exception error = null)
            {
                Entry = entry;
                Level = level;
                LevelContinuesBitMask = levelContinuesBitMask;
                Error = error;
            }

            /// <summary>
            /// Create line with new value to <see cref="LevelContinuesBitMask"/>.
            /// </summary>
            /// <param name="newLevelContinuesBitMask"></param>
            /// <returns>line with new mask</returns>
            public Line NewLevelContinuesBitMask(ulong newLevelContinuesBitMask)
                => new Line(Entry, Level, newLevelContinuesBitMask, Error);

            /// <summary>
            /// Tests whether there will be more entries to specific <paramref name="level"/>.
            /// </summary>
            /// <param name="level"></param>
            /// <returns></returns>
            public bool LevelContinues(int level)
            {
                // Undefined
                if (level == 0) return false;
                // Not supported after 64 levels
                if (level > 64) return false;
                // Read the bit
                return (LevelContinuesBitMask & 1UL << (level - 1)) != 0UL;
            }

            /// <summary>
            /// Write to <see cref="StringBuilder"/> <paramref name="output"/>.
            /// </summary>
            /// <param name="output"></param>
            /// <param name="format">print format</param>
            public void AppendTo(StringBuilder output, Format format = Format.Default)
            {
                // Number of info fields printed
                int column = 0;
                // Print tree
                if (format.HasFlag(Format.Tree) && Level > 0)
                {
                    if (column++ > 0) output.Append(" ");
                    // Print indents
                    for (int l = 1; l < Level; l++) output.Append(LevelContinues(l) ? "│  " : "   ");
                    // Print last indent
                    if (Level >= 1) output.Append(LevelContinues(Level) ? "├──" : "└──");
                }
                // Print name
                if (format.HasFlag(Format.Name))
                {
                    if (column++ > 0) output.Append(" ");
                    output.Append("\"");
                    output.Append(Entry.Name);
                    output.Append("\"");
                }
                // Print path
                if (format.HasFlag(Format.Path))
                {
                    if (column++ > 0) output.Append(" ");
                    output.Append(Entry.Path);
                }

                // [xx, yy, zz]
                StructList4<string> infos = new StructList4<string>();

                // Print mountpoint
                if (format.HasFlag(Format.Mount) && Entry.IsMountPoint())
                {
                    FileSystemAssignment[] mounts = Entry.Mounts();
                    if (mounts != null)
                    {
                        for (int i = 0; i < mounts.Length; i++)
                        {
                            FileSystemAssignment info = mounts[i];
                            if (info.FileSystem != null)
                            {
                                string fs = info.FileSystem.ToString();
                                if (!String.IsNullOrEmpty(fs)) infos.Add(fs);
                            }
                            if (info.Option != null)
                            {
                                string op = info.Option.ToString();
                                if (!String.IsNullOrEmpty(op)) infos.Add(op);
                            }
                        }
                    }
                }

                // Append drive label [Tank]
                if (format.HasFlag(Format.DriveLabel))
                {
                    string label = Entry.DriveLabel();
                    if (!String.IsNullOrEmpty(label)) infos.Add(label);
                }

                // Append free space [Freespace: 10G]
                if (format.HasFlag(Format.DriveFreespace))
                {
                    long freespace = format.HasFlag(Format.DriveFreespace) ? Entry.DriveFreeSpace() : -1L;
                    if (freespace > 0) infos.Add("Freespace: "+(freespace >> 30) + "G");
                }

                // Append total size [Size: 8G/32G]
                if (format.HasFlag(Format.DriveSize))
                {
                    long freespace = format.HasFlag(Format.DriveFreespace) ? Entry.DriveFreeSpace() : -1L;
                    long size = format.HasFlag(Format.DriveSize) ? Entry.DriveSize() : -1L;
                    long reserved = freespace < 0L ? -1L : size - freespace;
                    if (reserved > 0 && size > 0) infos.Add("Size: "+(reserved >> 30) + "G/" + (size >> 30) + "G");
                    else if (size > 0) infos.Add("Size: "+(size >> 30) + "G");
                }

                // Append drive type [Ram]
                if (format.HasFlag(Format.DriveType))
                {
                    DriveType driveType = Entry.DriveType();
                    if (driveType != DriveType.Unknown) infos.Add(driveType.ToString());
                }

                // Append drive format [NTFS]
                if (format.HasFlag(Format.DriveFormat))
                {
                    string driveFormat = Entry.DriveFormat();
                    if (!String.IsNullOrEmpty(driveFormat)) infos.Add(driveFormat);
                }

                // Print file attributes
                if (format.HasFlag(Format.FileAttributes) && Entry.HasFileAttributes())
                {
                    string attribs = Entry.FileAttributes().ToString();
                    if (!string.IsNullOrEmpty(attribs)) infos.Add(attribs);
                }

                // Print length
                if (format.HasFlag(Format.Length) && Entry.IsFile())
                {
                    long length = Entry.Length();
                    if (length >= 0L) infos.Add(length.ToString());
                }

                // Print error
                if (format.HasFlag(Format.Error) && Error != null)
                {
                    if (String.IsNullOrEmpty(Error.Message)) infos.Add(Error.GetType().Name);
                    else  infos.Add(Error.GetType().Name + ": " + Error.Message);
                }

                // Print physical path
                if (format.HasFlag(Format.PhysicalPath))
                {
                    string physicalPath = Entry.PhysicalPath();
                    if (!String.IsNullOrEmpty(physicalPath)) infos.Add(physicalPath);
                }
                
                // Print colon infos
                if (infos.Count > 0)
                {
                    if (column++ > 0) output.Append(" ");
                    output.Append('[');
                    for (int i = 0; i < infos.Count; i++)
                    {
                        if (i > 0) output.Append(", ");
                        output.Append(infos[i]);
                    }
                    output.Append(']');
                }

                // Next line
                if (format.HasFlag(Format.LineFeed)) output.AppendLine();
            }

            /// <summary>
            /// Write to <see cref="StringBuilder"/> <paramref name="output"/>.
            /// </summary>
            /// <param name="output"></param>
            /// <param name="format">print format</param>
            public void WriteTo(TextWriter output, Format format = Format.Default)
            {
                // Number of info fields printed
                int column = 0;
                // Print tree
                if (format.HasFlag(Format.Tree) && Level>0)
                {
                    if (column++ > 0) output.Write(" ");
                    // Print indents
                    for (int l = 1; l < Level; l++) output.Write(LevelContinues(l) ? "│  " : "   ");
                    // Print last indent
                    if (Level >= 1) output.Write(LevelContinues(Level) ? "├──" : "└──");
                }
                // Print name
                if (format.HasFlag(Format.Name))
                {
                    if (column++ > 0) output.Write(" ");
                    output.Write("\"");
                    output.Write(Entry.Name);
                    output.Write("\"");
                }
                // Print path
                if (format.HasFlag(Format.Path))
                {
                    if (column++ > 0) output.Write(" ");
                    output.Write(Entry.Path);
                }

                // [xx, yy, zz]
                StructList4<string> infos = new StructList4<string>();

                // Print mountpoint
                if (format.HasFlag(Format.Mount) && Entry.IsMountPoint())
                {
                    FileSystemAssignment[] mounts = Entry.Mounts();
                    if (mounts != null)
                    {
                        for (int i = 0; i < mounts.Length; i++)
                        {
                            FileSystemAssignment info = mounts[i];
                            if (info.FileSystem != null)
                            {
                                string fs = info.FileSystem.ToString();
                                if (!String.IsNullOrEmpty(fs)) infos.Add(fs);
                            }
                            if (info.Option != null)
                            {
                                string op = info.Option.ToString();
                                if (!String.IsNullOrEmpty(op)) infos.Add(op);
                            }
                        }
                    }
                }

                // Write drive label [Tank]
                if (format.HasFlag(Format.DriveLabel))
                {
                    string label = Entry.DriveLabel();
                    if (!String.IsNullOrEmpty(label)) infos.Add(label);
                }

                // Append free space [Freespace: 10G]
                if (format.HasFlag(Format.DriveFreespace))
                {
                    long freespace = format.HasFlag(Format.DriveFreespace) ? Entry.DriveFreeSpace() : -1L;
                    if (freespace > 0) infos.Add("Freespace: " + (freespace >> 30) + "G");
                }

                // Append total size [Size: 8G/32G]
                if (format.HasFlag(Format.DriveSize))
                {
                    long freespace = format.HasFlag(Format.DriveFreespace) ? Entry.DriveFreeSpace() : -1L;
                    long size = format.HasFlag(Format.DriveSize) ? Entry.DriveSize() : -1L;
                    long reserved = freespace < 0L ? -1L : size - freespace;
                    if (reserved > 0 && size > 0) infos.Add("Size: " + (reserved >> 30) + "G/" + (size >> 30) + "G");
                    else if (size > 0) infos.Add("Size: " + (size >> 30) + "G");
                }

                // Write drive type [Ram]
                if (format.HasFlag(Format.DriveType))
                {
                    DriveType driveType = Entry.DriveType();
                    if (driveType != DriveType.Unknown) infos.Add(driveType.ToString());
                }

                // Write drive format [NTFS]
                if (format.HasFlag(Format.DriveFormat))
                {
                    string driveFormat = Entry.DriveFormat();
                    if (!String.IsNullOrEmpty(driveFormat)) infos.Add(driveFormat);
                }

                // Print file attributes
                if (format.HasFlag(Format.FileAttributes) && Entry.HasFileAttributes())
                {
                    string attribs = Entry.FileAttributes().ToString();
                    if (!string.IsNullOrEmpty(attribs)) infos.Add(attribs);
                }

                // Print length
                if (format.HasFlag(Format.Length) && Entry.IsFile())
                {
                    long length = Entry.Length();
                    if (length >= 0L) infos.Add(length.ToString());
                }

                // Print error
                if (format.HasFlag(Format.Error) && Error != null)
                {
                    if (String.IsNullOrEmpty(Error.Message)) infos.Add(Error.GetType().Name);
                    else infos.Add(Error.GetType().Name + ": " + Error.Message);
                }

                // Print physical path
                if (format.HasFlag(Format.PhysicalPath))
                {
                    string physicalPath = Entry.PhysicalPath();
                    if (!String.IsNullOrEmpty(physicalPath)) infos.Add(physicalPath);
                }

                // Print colon infos
                if (infos.Count > 0)
                {
                    if (column++ > 0) output.Write(" ");
                    output.Write('[');
                    for (int i = 0; i < infos.Count; i++)
                    {
                        if (i > 0) output.Write(", ");
                        output.Write(infos[i]);
                    }
                    output.Write(']');
                }
                // Next line
                if (format.HasFlag(Format.LineFeed)) output.WriteLine();
            }

            /// <summary>
            /// Print line info.
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                AppendTo(sb);
                return sb.ToString();
            }

            /// <summary>
            /// Print line info.
            /// </summary>
            /// <param name="format">print format</param>
            /// <returns></returns>
            public string ToString(PrintTree.Format format)
            {
                StringBuilder sb = new StringBuilder();
                AppendTo(sb, format);
                return sb.ToString();
            }

            /// <summary>Comparer that sorts by: Type, Name.</summary>
            public class Comparer : IComparer<Line>
            {
                /// <summary>Comparer that sorts by: Type, Name.</summary>
                private static IComparer<Line> typeNameComparer = new Comparer(FileSystemEntryComparer.TypeNameComparer);
                /// <summary>Comparer that sorts by: Type, Name.</summary>
                public static IComparer<Line> TypeNameComparer => typeNameComparer;

                /// <summary>Entry comparer</summary>
                IComparer<IFileSystemEntry> entryComparer;

                /// <summary>
                /// Sort by type, then name, using AlphaNumericComparer
                /// </summary>
                /// <param name="x"></param>
                /// <param name="y"></param>
                /// <returns></returns>
                public int Compare(Line x, Line y)
                    => entryComparer.Compare(x.Entry, y.Entry);

                /// <summary>
                /// Create comparer
                /// </summary>
                /// <param name="entryComparer"></param>
                public Comparer(IComparer<IFileSystemEntry> entryComparer = null)
                {
                    this.entryComparer = entryComparer ?? throw new ArgumentException(nameof(entryComparer));
                }
            }
        }

    }
}
