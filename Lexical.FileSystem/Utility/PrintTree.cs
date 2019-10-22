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
        [Flags]
        public enum Format : Int32
        {
            /// <summary>
            /// Print tree lines.
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
            Tree = 1 << 1,

            /// <summary>
            /// Print entry name.
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
            Name = 1 << 8,

            /// <summary>
            /// Print entry path.
            /// 
            /// ├──/
            /// │  ├──/mnt/
            /// │  ├──/tmp/
            /// │  │  └──/tmp/helloworld.txt
            /// │  └──/usr/
            /// │     └──/usr/myuser/
            /// └──c:
            ///    └──c:/dir/
            ///       └──c:/dir/dir/
            /// </summary>
            Path = 1 << 9,

            /// <summary>
            /// Print length on files
            /// 
            /// ├──/
            /// │  ├──/mnt/
            /// │  ├──/tmp/
            /// │  │  └──/tmp/helloworld.txt [128]
            /// │  └──/usr/
            /// │     └──/usr/myuser/
            /// </summary>
            Length = 1 << 12,

            /// <summary>
            /// Print mount info on mountpoints.
            /// 
            /// ├──/
            /// │  ├──/application/ [C:\Program Files (x86)\MyApplication]
            /// │  ├──/assembly/ [AssemblyFileSystem]
            /// │  ├──/tmp/ [MemoryFileSystem]
            /// │  ├──/docs/ [C:\Users\myuser\MyDocuments]
            /// │  └──/home/ [C:\Users\myuser]
            /// </summary>
            Mount = 1 << 14,

            /// <summary>
            /// Print drive label [Tank]
            /// </summary>
            DriveLabel = 1 << 15,

            /// <summary>
            /// Print drive free space [32G/]
            /// </summary>
            DriveFreespace = 1 << 16,

            /// <summary>
            /// Print drive total size [512GB]
            /// </summary>
            DriveSize = 1 << 16,

            /// <summary>
            /// Print drive format if other than <see cref="DriveType.Unknown"/> [Ram]
            /// </summary>
            DriveType = 1 << 17,

            /// <summary>
            /// Print drive type [NTFS]
            /// </summary>
            DriveFormat = 1 << 18,

            /// <summary>
            /// Print file attributes
            /// </summary>
            FileAttributes = 1 << 19,

            /// <summary>
            /// Print error on files
            /// 
            /// ├──/
            /// │  └──/tmp/ [IOException: File not found.]
            /// </summary>
            Error = 1 << 29,

            /// <summary>
            /// Print "\n"
            /// </summary>
            LineFeed = 1 << 30,

            /// <summary>
            /// Default format.
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
            Default = Tree | Name | Error,

            /// <summary>
            /// Print entry path.
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
            DefaultPath = Tree | Path | Error,

            /// <summary>
            /// All flags
            /// </summary>
            All = -1/*-Path*/,

            /// <summary>
            /// All flags with Name (excludes Path)
            /// </summary>
            AllWithName = -1 - Path,

            /// <summary>
            /// All flags with Path (excludes Name)
            /// </summary>
            AllWithPath = -1 - Name,

        }

        /// <summary>
        /// Print tree structure of the whole filesystem. 
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
        /// <param name="output">output such as <see cref="Console.Out"/></param>
        /// <param name="path"></param>
        /// <param name="depth">maximum visit depth</param>
        /// <param name="format">print format</param>
        public static void PrintTo(this IFileSystem filesystem, TextWriter output, string path = "", int depth = Int32.MaxValue, Format format = Format.Default)
        {
            foreach (TreeVisit.Line line in filesystem.VisitTree(path, depth))
                line.WriteTo(output, format | Format.LineFeed);
        }

        /// <summary>
        /// Print tree structure of the whole filesystem. 
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
        /// <param name="output">output</param>
        /// <param name="path"></param>
        /// <param name="depth">maximum visit depth</param>
        /// <param name="format">print format</param>
        public static void PrintTo(this IFileSystem filesystem, StringBuilder output, string path = "", int depth = Int32.MaxValue, Format format = Format.Default)
        {
            foreach (TreeVisit.Line line in filesystem.VisitTree(path, depth))
                line.AppendTo(output, format | Format.LineFeed);
        }

        /// <summary>
        /// Print tree structure of the whole filesystem. 
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
        /// <param name="format">print format</param>
        /// <returns>Tree as string</returns>
        public static String Print(this IFileSystem filesystem, string path = "", int depth = Int32.MaxValue, Format format = Format.Default)
        {
            StringBuilder sb = new StringBuilder();
            foreach (TreeVisit.Line line in filesystem.VisitTree(path, depth))
                line.AppendTo(sb, format | Format.LineFeed);
            return sb.ToString();
        }
    }
}
