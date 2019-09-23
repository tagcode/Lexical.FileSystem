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
    /// <see cref="IFileSystem"/> tree visitor extension methods.
    /// </summary>
    public static class FileSystemPrintTree
    {
        /// <summary>
        /// Print tree structure of the whole filesystem. 
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
        /// <param name="output">output such as <see cref="Console.Out"/></param>
        /// <param name="startPath"></param>
        /// <param name="maxLevel"></param>
        public static void PrintTree(this IFileSystem fileSystem, TextWriter output, string startPath = "", int maxLevel = Int32.MaxValue)
        {
            foreach(Line line in fileSystem.VisitTree(startPath, maxLevel))
            {
                // Print indents
                for (int l = 0; l < line.Level - 1; l++) output.Write(line.LevelContinues(l) ? "│  " : "   ");
                // Print last indent
                if (line.Level >= 1) output.Write(line.LevelContinues(line.Level - 1) ? "├──" : "└──");
                // Print name
                output.Write("\"");
                output.Write(line.Entry.Name);
                output.Write("\"");
                // Print error
                if (line.Error != null)
                {
                    output.Write(" ");
                    output.Write(line.Error.GetType().Name);
                    output.Write(": ");
                    output.Write(line.Error.Message);
                }
                // Print line-feed
                output.WriteLine();
            }
        }

        /// <summary>
        /// Print tree structure of the whole filesystem. 
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
        /// <param name="output">output</param>
        /// <param name="startPath"></param>
        /// <param name="maxLevel"></param>
        public static void PrintTree(this IFileSystem fileSystem, StringBuilder output, string startPath = "", int maxLevel = Int32.MaxValue)
        {
            foreach (Line line in fileSystem.VisitTree(startPath, maxLevel))
            {
                // Print indents
                for (int l = 0; l < line.Level - 1; l++) output.Append(line.LevelContinues(l) ? "│  " : "   ");
                // Print last indent
                if (line.Level >= 1) output.Append(line.LevelContinues(line.Level - 1) ? "├──" : "└──");
                // Print name
                output.Append("\"");
                output.Append(line.Entry.Name);
                output.Append("\"");
                // Print error
                if (line.Error != null)
                {
                    output.Append(" ");
                    output.Append(line.Error.GetType().Name);
                    output.Append(": ");
                    output.Append(line.Error.Message);
                }
                // Print line-feed
                output.AppendLine();
            }
        }

        /// <summary>
        /// Print tree structure of the whole filesystem. 
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
        /// <returns>Tree as string</returns>
        public static String PrintTreeToString(this IFileSystem fileSystem, string startPath = "", int maxLevel = Int32.MaxValue)
        {
            StringBuilder sb = new StringBuilder();
            foreach (Line line in fileSystem.VisitTree(startPath, maxLevel))
            {
                line.AppendTo(sb);
                sb.AppendLine();
            }
            return sb.ToString();
        }

        /// <summary>
        /// Vists tree structure of filesystem. 
        /// 
        /// Starts at <paramref name="startPath"/> if provided, otherwise starts at root "".
        /// <paramref name="maxLevel"/> sets maximum visit depths.
        /// 
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
                        IFileSystemEntry[] entries = fileSystem.Browse(line.Entry.Path);
                        // Sort
                        Array.Sort(entries, FileSystemEntryComparer.Instance);
                        // Mask for entries that are not last in the array
                        ulong levelContinuesBitMask = line.LevelContinuesBitMask;
                        if (line.Level<=64) levelContinuesBitMask |= 1UL << (line.Level - 1);
                        // Add children
                        for (int i = entries.Length - 1; i >= 0; i--)
                        {
                            // Choose which mask to use
                            ulong bitmask = i < entries.Length - 1 ? /*not last entry*/ levelContinuesBitMask : /*last entry*/ line.LevelContinuesBitMask;
                            // Queue
                            queue.Add(new Line(entries[i], line.Level+1, bitmask));
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
            public void AppendTo(StringBuilder output)
            {
                // Print indents
                for (int l = 0; l < Level - 1; l++) output.Append(LevelContinues(l) ? "│  " : "   ");
                // Print last indent
                if (Level >= 1) output.Append(LevelContinues(Level - 1) ? "├──" : "└──");
                // Print name
                output.Append("\"");
                output.Append(Entry.Name);
                output.Append("\"");
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
