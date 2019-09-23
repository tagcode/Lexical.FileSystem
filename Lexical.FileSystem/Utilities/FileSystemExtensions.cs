// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           23.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;

namespace Lexical.FileSystem.Utilities
{
    /// <summary>
    /// FileSystem extension methods
    /// </summary>
    public static partial class _FileSystemExtensions
    {
        /// <summary>
        /// Print tree structure of the whole filesystem. 
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
        /// </summary>
        /// <param name="fileSystem"></param>
        /// <param name="output">output such as <see cref="Console.Out"/></param>
        public static void PrintTree(this IFileSystem fileSystem, TextWriter output)
        {
            LinkedList<(IFileSystemEntry, int, ulong)> queue = new LinkedList<(IFileSystemEntry, int, ulong)>();
            queue.AddLast((fileSystem.GetEntry(""), 0, 0UL));
            while (queue.Count > 0)
            {
                // Next directory
                (IFileSystemEntry entry, int level, ulong levelContinuesBitMask) = queue.Last.Value;
                queue.RemoveLast();

                // Print line
                {
                    for (int l = 0; l < level - 1; l++)
                    {
                        bool levelContinues = l <= 63 ? (levelContinuesBitMask & 1UL << l) != 0UL : false;
                        output.Write(levelContinues ? "│  " : "   ");
                    }
                    if (level >= 1)
                    {
                        bool levelContinues = level <= 64 ? (levelContinuesBitMask & 1UL << (level-1)) != 0UL : false;
                        output.Write(levelContinues ? "├──" : "└──");
                    }
                    output.Write("\"");
                    output.Write(entry.Name);
                    output.Write("\"\r\n");
                }

                if (entry is IFileSystemEntryDirectory)
                {
                    // Browse 
                    IFileSystemEntry[] entries = fileSystem.Browse(entry.Path);
                    // Sort
                    Array.Sort(entries, FileSystemEntryComparer.Instance);
                    // Queue
                    for (int i = entries.Length - 1; i >= 0; i--)
                    {
                        int newLevel = level + 1;
                        ulong newLevelContinuesBitMask = i == entries.Length - 1 ? /*Last element in the level*/ levelContinuesBitMask : /*This level has more elements*/ levelContinuesBitMask | 1UL<<level;
                        queue.AddLast((entries[i], newLevel, newLevelContinuesBitMask));
                    }
                }
            }
        }
    }
}
