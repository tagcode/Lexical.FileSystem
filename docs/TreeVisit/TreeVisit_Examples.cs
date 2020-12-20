using Lexical.FileSystem;
using Lexical.FileSystem.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace docs
{
    public class TreeVisit_Examples
    {
        public static void Main(string[] args)
        {
            {
                #region Snippet_1
                IFileSystem ram = new MemoryFileSystem();
                ram.CreateDirectory("/tmp/");
                ram.CreateDirectory("/mnt/");
                ram.CreateDirectory("/usr/lex/");
                ram.CreateDirectory("c:/dir/dir/");
                ram.CreateFile("/tmp/helloworld.txt", Encoding.UTF8.GetBytes("Hello World!\r\n"));
                ram.CreateDirectory("file://c:/temp");

                foreach (TreeVisit.Line line in ram.VisitTree())
                {
                    Console.WriteLine(line);
                }
                #endregion Snippet_1

                #region Snippet_2
                foreach (TreeVisit.Line line in ram.VisitTree(depth: 1))
                {
                    Console.WriteLine(line);
                }
                #endregion Snippet_2

                #region Snippet_3
                foreach (TreeVisit.Line line in ram.VisitTree(path: "/tmp/"))
                {
                    Console.WriteLine(line);
                }
                #endregion Snippet_3
            }
        }

    }
}
