using Lexical.FileSystem;
using Lexical.FileSystem.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace docs
{
    public class IFileSystemFileAttribute_Examples
    {
        public static void Main(string[] args)
        {
            {
                // <Example_1>
                IFileSystem filesystem = new FileSystem("");
                filesystem.SetFileAttribute("C:/Temp/File.txt", FileAttributes.ReadOnly);
                // </Example_1>
            }

        }
    }

}
