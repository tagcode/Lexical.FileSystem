using Lexical.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace docs
{
    public class EmbeddedFileSystem_Examples
    {
        public static void Main(string[] args)
        {
            {
                #region Snippet_1
                IFileSystem filesystem = new EmbeddedFileSystem(typeof(Program).Assembly);
                #endregion Snippet_1

                #region Snippet_2
                foreach (var entry in filesystem.Browse(""))
                    Console.WriteLine(entry.Path);
                #endregion Snippet_2

                #region Snippet_3
                using(Stream s = filesystem.Open("docs.example-file.txt", FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    Console.WriteLine(s.Length);
                }
                #endregion Snippet_3

                #region Snippet_4
                filesystem.PrintTo(Console.Out);
                #endregion Snippet_4

            }
        }
    }
}
