using Lexical.FileSystem;
using Lexical.FileSystem.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace docs
{
    public class PrintTree_Examples
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
                ram.CreateDirectory("file://c:/temp/");

                ram.PrintTo(Console.Out);
                #endregion Snippet_1

                #region Snippet_2
                StringBuilder sb = new StringBuilder();
                ram.PrintTo(sb);
                #endregion Snippet_2
                Console.WriteLine(sb);

                #region Snippet_3
                Console.WriteLine(ram.Print());
                #endregion Snippet_3

                #region Snippet_4
                Console.WriteLine(ram.Print(depth:1));
                #endregion Snippet_4

                #region Snippet_5
                Console.WriteLine(ram.Print(path:"/tmp/"));
                #endregion Snippet_5

                #region Snippet_6
                string tree =  ram.Print(format: PrintTree.Format.Tree | PrintTree.Format.Path | 
                                                 PrintTree.Format.Length | PrintTree.Format.Error);
                #endregion Snippet_6
                Console.WriteLine(tree);
            }
            {
                #region Snippet_10
                #endregion Snippet_10
            }
            {
                #region Snippet_11
                #endregion Snippet_11
            }
            {
                #region Snippet_12
                #endregion Snippet_12
            }
        }

    }
}
