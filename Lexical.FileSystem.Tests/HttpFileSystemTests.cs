using Lexical.FileSystem.Utility;
using Lexical.Utils.Permutation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lexical.FileSystem.Tests
{
    [TestClass]
    public class HttpFileSystemTests
    {
        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void Initialize()
        {
        }

        [TestCleanup]
        public void Cleanup()
        {
        }

        [TestMethod]
        public void TestExtractName()
        {
            Assert.AreEqual("lexical.fi", HttpFileSystem.GetEntryName("http://lexical.fi/"));
            Assert.AreEqual("Dir", HttpFileSystem.GetEntryName("http://lexical.fi/Dir/"));
            Assert.AreEqual("file.txt", HttpFileSystem.GetEntryName("http://lexical.fi/Dir/file.txt"));
            Assert.AreEqual("Dir", HttpFileSystem.GetEntryName("http://lexical.fi/Dir/?query=value"));
            Assert.AreEqual("file.txt", HttpFileSystem.GetEntryName("http://lexical.fi/Dir/file.txt?query=value"));
            Assert.AreEqual(null, HttpFileSystem.GetEntryName(""));
            Assert.AreEqual(null, HttpFileSystem.GetEntryName("/"));
        }

    }

}
