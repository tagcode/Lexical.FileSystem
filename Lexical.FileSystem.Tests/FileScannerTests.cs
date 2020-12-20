using Lexical.FileSystem.Internal;
using Lexical.FileSystem.Utility;
using Microsoft.Extensions.FileProviders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lexical.FileSystem.Tests
{
    [TestClass]
    public class FileScannerTests
    {
        static string Repeat(string x, int c) { StringBuilder sb = new StringBuilder(); for (int i = 0; i < c; i++) sb.Append(x); return sb.ToString(); }
        public static byte[] HelloWorld = Encoding.UTF8.GetBytes("Hello World!\r\n");
        public static byte[] HelloWorld_x100 = Encoding.UTF8.GetBytes(Repeat("Hello World!\r\n", 100));
        public static byte[] HelloWorld_x10000 = Encoding.UTF8.GetBytes(Repeat("Hello World!\r\n", 10000));
        MemoryFileSystem ram;
        FileSystem tmp;

        [TestInitialize]
        public void Initialize() 
        {
            ram = new MemoryFileSystem();
            ram.CreateDirectory("/tmp");
            ram.CreateDirectory("/tmp/dir");
            ram.CreateDirectory("/mnt");
            ram.CreateDirectory("/usr/lex");
            ram.CreateDirectory("c:/dir/dir");
            ram.CreateFile("/tmp/helloworld.txt", HelloWorld);
            ram.CreateFile("/tmp/helloworld_100.txt", HelloWorld_x100);
            ram.CreateFile("/tmp/helloworld_10000.txt", HelloWorld_x10000);
            ram.CreateDirectory("file://");

            tmp = FileSystem.Temp;
            tmp.CreateDirectory("mydir");
            tmp.CreateFile("mydir/helloworld.dat1", HelloWorld);
            tmp.CreateFile("mydir/helloworld.dat100", HelloWorld_x100);
            tmp.CreateFile("mydir/helloworld.dat10000", HelloWorld_x10000);
        }

        [TestCleanup]
        public void Cleanup() { }

        [TestMethod]
        public void Test1()
        {
            string pattern = FileSystem.Temp.Path.Replace('\\', '/')+"**.dat*";
            IFileProvider fp = new PhysicalFileProvider(FileSystem.Temp.Path);
            IFileSystem fs = fp.ToFileSystem();
            FileScanner scanner = new FileScanner(fs)
                .SetReturnDirectories(false)
                .SetReturnFiles(true)
                .AddGlobPattern("mydir/**.dat*");
            IEntry[] entries = scanner.ToArray();
            IEntry e = fs.GetEntry("mydir/helloworld.dat1");

        }

    }
}