using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using System.Threading;
using Lexical.FileSystem;
using Microsoft.Extensions.FileProviders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Lexical.FileSystem.Internal;
using Lexical.FileSystem.Utility;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace Lexical.FileSystem.Tests
{
    [TestClass]
    public class FileProviderSystemTests
    {
        public static byte[] HelloWorld = Encoding.UTF8.GetBytes("Hello World!\r\n");
        public static byte[] HelloWorld_x100 = Encoding.UTF8.GetBytes(Repeat("Hello World!\r\n", 100));
        public static byte[] HelloWorld_x10000 = Encoding.UTF8.GetBytes(Repeat("Hello World!\r\n", 10000));

        public TestContext TestContext { get; set; }

        MemoryFileSystem ram;

        /// <summary>
        /// </summary>
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
        }

        /// <summary>
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
        }

        /// <summary>
        /// </summary>
        [TestMethod]
        public void Test()
        {
            // test observe and event
        }

        static string Repeat(string x, int c) { StringBuilder sb = new StringBuilder(); for (int i = 0; i < c; i++) sb.Append(x); return sb.ToString(); }

    }
}
