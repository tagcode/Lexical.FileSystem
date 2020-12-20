using Lexical.FileSystem.Internal;
using Lexical.FileSystem.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lexical.FileSystem.Tests
{
    [TestClass]
    public class PathEnumerableTests
    {
        [TestInitialize]
        public void Initialize() { }

        [TestCleanup]
        public void Cleanup() { }

        [TestMethod]
        public void GetParent()
        {
            Assert.AreEqual("", (String)PathEnumerable.GetParent(""));
            Assert.AreEqual("", (String)PathEnumerable.GetParent("/"));
            Assert.AreEqual("", (String)PathEnumerable.GetParent("dir"));
            Assert.AreEqual("", (String)PathEnumerable.GetParent("dir/"));
            Assert.AreEqual("dir/", (String)PathEnumerable.GetParent("dir/file"));
            Assert.AreEqual("dir/", (String)PathEnumerable.GetParent("dir/dir"));
            Assert.AreEqual("dir/", (String)PathEnumerable.GetParent("dir/dir/"));
            Assert.AreEqual("dir/dir/", (String)PathEnumerable.GetParent("dir/dir/dir"));
            Assert.AreEqual("dir/dir/", (String)PathEnumerable.GetParent("dir/dir/dir/"));
        }

        [TestMethod]
        public void Enumerable()
        {
            string[] arr;
            // "" -> [""]
            arr = PathToArray("", false);
            Assert.AreEqual(1, arr.Length);
            Assert.AreEqual("", arr[0]);

            // "dir/dir/file" -> ["dir", "dir", "file"]
            arr = PathToArray("dir/dir/file", false);
            Assert.AreEqual(3, arr.Length);
            Assert.AreEqual("dir", arr[0]);
            Assert.AreEqual("dir", arr[1]);
            Assert.AreEqual("file", arr[2]);

            // "dir/dir/path/" -> ["dir", "dir", "path", ""]
            arr = PathToArray("dir/dir/path/", false);
            Assert.AreEqual(4, arr.Length);
            Assert.AreEqual("dir", arr[0]);
            Assert.AreEqual("dir", arr[1]);
            Assert.AreEqual("path", arr[2]);
            Assert.AreEqual("", arr[3]);

            // "/mnt/shared/" -> ["", "mnt", "shared", ""]
            arr = PathToArray("/mnt/shared/", false);
            Assert.AreEqual(4, arr.Length);
            Assert.AreEqual("", arr[0]);
            Assert.AreEqual("mnt", arr[1]);
            Assert.AreEqual("shared", arr[2]);
            Assert.AreEqual("", arr[3]);

            // "/" -> ["", ""]
            arr = PathToArray("/", false);
            Assert.AreEqual(2, arr.Length);
            Assert.AreEqual("", arr[0]);
            Assert.AreEqual("", arr[1]);

            // "//" -> ["", "", ""]
            arr = PathToArray("//", false);
            Assert.AreEqual(3, arr.Length);
            Assert.AreEqual("", arr[0]);
            Assert.AreEqual("", arr[1]);
            Assert.AreEqual("", arr[2]);

        }

        string[] PathToArray(string path, bool ignoreTrailingSlash)
            => new PathEnumerable(path, ignoreTrailingSlash).Select(s => (string)s).ToArray();
    }
}