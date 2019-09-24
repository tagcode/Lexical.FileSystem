// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           23.9.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.IO;
using System.Text;

namespace Lexical.FileSystem
{
    /// <summary>
    /// Print extension methods for <see cref="IFileSystem"/>.
    /// </summary>
    public static class PrintTree
    {
        /// <summary>
        /// Print format
        /// </summary>
        public enum Format
        {
            /// <summary>
            /// Print format is entry name.
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
            /// </summary>
            Name,

            /// <summary>
            /// Print format is entry path.
            /// 
            /// ├──/
            /// │  ├──/mnt
            /// │  ├──/tmp
            /// │  │  └──/tmp/helloworld.txt
            /// │  └──/usr
            /// │     └──/usr/lex
            /// └──c:
            ///    └──c:/dir
            ///       └──c:/dir/dir
            /// </summary>
            Path
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
        /// <param name="output">output such as <see cref="Console.Out"/></param>
        /// <param name="startPath"></param>
        /// <param name="maxLevel"></param>
        /// <param name="printFormat">print format</param>
        public static void PrintTreeTo(this IFileSystem fileSystem, TextWriter output, string startPath = "", int maxLevel = Int32.MaxValue, Format printFormat = Format.Name)
        {
            foreach (TreeVisit.Line line in fileSystem.VisitTree(startPath, maxLevel))
            {
                // Print indents
                for (int l = 0; l < line.Level - 1; l++) output.Write(line.LevelContinues(l) ? "│  " : "   ");
                // Print last indent
                if (line.Level >= 1) output.Write(line.LevelContinues(line.Level - 1) ? "├──" : "└──");
                // Print name
                if (printFormat == Format.Name)
                {
                    output.Write("\"");
                    output.Write(line.Entry.Name);
                    output.Write("\"");
                }
                else if (printFormat == Format.Path)
                {
                    output.Write(line.Entry.Path);
                }
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
        /// <param name="printFormat">print format</param>
        public static void AppendTreeTo(this IFileSystem fileSystem, StringBuilder output, string startPath = "", int maxLevel = Int32.MaxValue, Format printFormat = Format.Name)
        {
            foreach (TreeVisit.Line line in fileSystem.VisitTree(startPath, maxLevel))
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
        /// <param name="printFormat">print format</param>
        /// <returns>Tree as string</returns>
        public static String PrintTreeText(this IFileSystem fileSystem, string startPath = "", int maxLevel = Int32.MaxValue, Format printFormat = Format.Name)
        {
            StringBuilder sb = new StringBuilder();
            foreach (TreeVisit.Line line in fileSystem.VisitTree(startPath, maxLevel))
            {
                line.AppendTo(sb, printFormat);
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
