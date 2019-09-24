// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           23.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
        /// Starts at <paramref name="startPath"/> if provided, otherwise starts at root "".
        /// <paramref name="maxLevel"/> sets maximum visit depths.
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
        /// <param name="fileSystem"></param>
        /// <param name="startPath"></param>
        /// <param name="maxLevel"></param>
        public static IEnumerable<Line> VisitTree(this IFileSystem fileSystem, string startPath = "", int maxLevel = Int32.MaxValue)
        {
            List<Line> queue = new List<Line>();
            queue.Add( new Line(fileSystem.GetEntry(startPath), 0, 0UL) );
            while (queue.Count > 0)
            {
                // Next entry
                int lastIx = queue.Count - 1;
                Line line = queue[lastIx];
                queue.RemoveAt(lastIx);

                // Children
                if (line.Entry.IsDirectory() && line.Level < maxLevel)
                {
                    try
                    {
                        // Browse children
                        IFileSystemEntry[] children = fileSystem.Browse(line.Entry.Path);
                        // Sort
                        Array.Sort(children, FileSystemEntryComparer.TypeNameComparer);
                        // Mask for entries that are not last in the array
                        ulong levelContinuesBitMask = line.LevelContinuesBitMask;
                        if (line.Level<=64) levelContinuesBitMask |= 1UL << (line.Level - 1);
                        // Add children
                        for (int i = children.Length - 1; i >= 0; i--)
                        {
                            // Child
                            IFileSystemEntry child = children[i];
                            // Assert
                            if (line.Entry.Path.StartsWith(child.Path)) throw new IOException($"{child.Path} cannot be child of {line.Entry.Path}");
                            // Choose which mask to use
                            ulong bitmask = i < children.Length - 1 ? /*not last entry*/ levelContinuesBitMask : /*last entry*/ line.LevelContinuesBitMask;
                            // Queue
                            queue.Add(new Line(child, line.Level+1, bitmask));
                        }
                    }
                    catch (Exception e)
                    {
                        // Store error
                        line.Error = e;
                    }
                }

                // yield line
                yield return line;
            }
        }

        /// <summary>
        /// Tree visitor line.
        /// </summary>
        public struct Line
        {
            /// <summary>
            /// Visited tree entry.
            /// </summary>
            public readonly IFileSystemEntry Entry;

            /// <summary>
            /// Visit depth, 0 is start path.
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
            /// Tests whether there will be more entries to specific <paramref name="level"/>.
            /// </summary>
            /// <param name="level"></param>
            /// <returns></returns>
            public bool LevelContinues(int level) 
                => level >= 64 ? 
                   /*Not supported after 64 levels*/ false : 
                   /*Read bit*/ (LevelContinuesBitMask & 1UL << (level - 1)) != 0UL;

            /// <summary>
            /// Write to <see cref="StringBuilder"/> <paramref name="output"/>.
            /// </summary>
            /// <param name="output"></param>
            /// <param name="printFormat">print format</param>
            public void AppendTo(StringBuilder output, PrintTree.Format printFormat = PrintTree.Format.Name)
            {
                // Print indents
                for (int l = 0; l < Level - 1; l++) output.Append(LevelContinues(l) ? "│  " : "   ");
                // Print last indent
                if (Level >= 1) output.Append(LevelContinues(Level - 1) ? "├──" : "└──");
                // Print name
                if (printFormat == PrintTree.Format.Name)
                {
                    output.Append("\"");
                    output.Append(Entry.Name);
                    output.Append("\"");
                }
                else if (printFormat == PrintTree.Format.Path)
                {
                    output.Append(Entry.Path);
                }
                // Print error
                if (Error != null)
                {
                    output.Append(" ");
                    output.Append(Error.GetType().Name);
                    output.Append(": ");
                    output.Append(Error.Message);
                }
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
        }

    }
}
