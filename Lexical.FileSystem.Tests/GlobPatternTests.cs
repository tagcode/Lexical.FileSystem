using Lexical.FileSystem.Internal;
using Lexical.FileSystem.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lexical.FileSystem.Tests
{
    [TestClass]
    public class GlobPatternTests
    {
        [TestInitialize]
        public void Initialize() { }

        [TestCleanup]
        public void Cleanup() { }

        [TestMethod]
        public void GlobPatternSetUnion()
        {
            {
                Assert.AreEqual("?b", GlobPatternSet.Union("ab", "cb"));
                Assert.AreEqual("**", GlobPatternSet.Union("a*", "**"));
                Assert.AreEqual("a**", GlobPatternSet.Union("a*", "a**"));
                Assert.AreEqual("*/*", GlobPatternSet.Union("abc/def", "*/*"));
                Assert.AreEqual("", GlobPatternSet.Union("", ""));
                Assert.AreEqual("", GlobPatternSet.Union("", ""));
            }
        }

        [TestMethod]
        public void GlobPatternSetIntersection()
        {
            {
                Assert.AreEqual("*/*", GlobPatternSet.Intersection("**", "*/*"));
                Assert.AreEqual("*?", GlobPatternSet.Intersection("**", "*?"));
                Assert.AreEqual("*a", GlobPatternSet.Intersection("**a", "*"));

                Assert.AreEqual("*/*/*.txt", GlobPatternSet.Intersection("**.txt", "*/*/*"));
                Assert.AreEqual(null, GlobPatternSet.Intersection("kissa", "koira"));
                Assert.AreEqual("kissa?", GlobPatternSet.Intersection("kissa*", "kissa?"));

                Assert.AreEqual("/home/user/*.zip/*.txt", GlobPatternSet.Intersection("/*/user/*.zip/*.txt", "/home/**"));

                Assert.AreEqual("", GlobPatternSet.Intersection("", ""));
            }
        }

        [TestMethod]
        public void GlobPatternInfo()
        {
            {
                GlobPatternInfo gpi = new GlobPatternInfo("");
                Assert.AreEqual("", gpi.Stem);
                Assert.AreEqual("", gpi.Suffix);
                Assert.AreEqual(0, gpi.SuffixDepth);
            }
            {
                GlobPatternInfo gpi = new GlobPatternInfo("dir/dir/file.txt");
                Assert.AreEqual("dir/dir/file.txt", gpi.Stem);
                Assert.AreEqual("", gpi.Suffix);
                Assert.AreEqual(0, gpi.SuffixDepth);
            }
            {
                GlobPatternInfo gpi = new GlobPatternInfo("*.txt");
                Assert.AreEqual("", gpi.Stem);
                Assert.AreEqual("*.txt", gpi.Suffix);
                Assert.AreEqual(1, gpi.SuffixDepth);
            }
            {
                GlobPatternInfo gpi = new GlobPatternInfo("**.txt");
                Assert.AreEqual("", gpi.Stem);
                Assert.AreEqual("**.txt", gpi.Suffix);
                Assert.AreEqual(int.MaxValue, gpi.SuffixDepth);
            }
            {
                GlobPatternInfo gpi = new GlobPatternInfo("/*.txt");
                Assert.AreEqual("/", gpi.Stem);
                Assert.AreEqual("*.txt", gpi.Suffix);
                Assert.AreEqual(1, gpi.SuffixDepth);
            }
            {
                GlobPatternInfo gpi = new GlobPatternInfo("*/*.txt");
                Assert.AreEqual("", gpi.Stem);
                Assert.AreEqual("*/*.txt", gpi.Suffix);
                Assert.AreEqual(2, gpi.SuffixDepth);
            }
            {
                GlobPatternInfo gpi = new GlobPatternInfo("/**.txt");
                Assert.AreEqual("/", gpi.Stem);
                Assert.AreEqual("**.txt", gpi.Suffix);
                Assert.AreEqual(int.MaxValue, gpi.SuffixDepth);
            }
            {
                GlobPatternInfo gpi = new GlobPatternInfo("dir/dir/*/*.txt");
                Assert.AreEqual("dir/dir/", gpi.Stem);
                Assert.AreEqual("*/*.txt", gpi.Suffix);
                Assert.AreEqual(2, gpi.SuffixDepth);
            }
            {
                GlobPatternInfo gpi = new GlobPatternInfo("dir/dir?/*/*.txt");
                Assert.AreEqual("dir/", gpi.Stem);
                Assert.AreEqual("dir?/*/*.txt", gpi.Suffix);
                Assert.AreEqual(3, gpi.SuffixDepth);
            }
            {
                GlobPatternInfo gpi = new GlobPatternInfo("dir/dir/dir/*/*.txt");
                Assert.AreEqual("dir/dir/dir/", gpi.Stem);
                Assert.AreEqual("*/*.txt", gpi.Suffix);
                Assert.AreEqual(2, gpi.SuffixDepth);
            }
            {
                GlobPatternInfo gpi = new GlobPatternInfo("dir/dir/dir?/*/*.txt");
                Assert.AreEqual("dir/dir/", gpi.Stem);
                Assert.AreEqual("dir?/*/*.txt", gpi.Suffix);
                Assert.AreEqual(3, gpi.SuffixDepth);
            }
            {
                GlobPatternInfo gpi = new GlobPatternInfo("dir/dir/dir?/*/**.txt");
                Assert.AreEqual("dir/dir/", gpi.Stem);
                Assert.AreEqual("dir?/*/**.txt", gpi.Suffix);
                Assert.AreEqual(int.MaxValue, gpi.SuffixDepth);
            }
            {
                GlobPatternInfo gpi = new GlobPatternInfo("dir/*/dir/dir/dir/file.txt");
                Assert.AreEqual("dir/", gpi.Stem);
                Assert.AreEqual("*/dir/dir/dir/file.txt", gpi.Suffix);
                Assert.AreEqual(5, gpi.SuffixDepth);
            }
            {
                GlobPatternInfo gpi = new GlobPatternInfo("dir/**");
                Assert.AreEqual("dir/", gpi.Stem);
                Assert.AreEqual("**", gpi.Suffix);
                Assert.AreEqual(int.MaxValue, gpi.SuffixDepth);
            }
            {
                GlobPatternInfo gpi = new GlobPatternInfo("dir/*");
                Assert.AreEqual("dir/", gpi.Stem);
                Assert.AreEqual("*", gpi.Suffix);
                Assert.AreEqual(1, gpi.SuffixDepth);
            }
            {
                GlobPatternInfo gpi = new GlobPatternInfo("dir/");
                Assert.AreEqual("dir/", gpi.Stem);
                Assert.AreEqual("", gpi.Suffix);
                Assert.AreEqual(0, gpi.SuffixDepth);
            }
            {
                GlobPatternInfo gpi = new GlobPatternInfo("dir");
                Assert.AreEqual("dir", gpi.Stem);
                Assert.AreEqual("", gpi.Suffix);
                Assert.AreEqual(0, gpi.SuffixDepth);
            }
            {
                GlobPatternInfo gpi = new GlobPatternInfo("dir/file.txt");
                Assert.AreEqual("dir/file.txt", gpi.Stem);
                Assert.AreEqual("", gpi.Suffix);
                Assert.AreEqual(0, gpi.SuffixDepth);
            }
            {
                GlobPatternInfo gpi = new GlobPatternInfo("dir/file*.txt");
                Assert.AreEqual("dir/", gpi.Stem);
                Assert.AreEqual("file*.txt", gpi.Suffix);
                Assert.AreEqual(1, gpi.SuffixDepth);
            }
            {
                GlobPatternInfo gpi = new GlobPatternInfo("dir/file**.txt");
                Assert.AreEqual("dir/", gpi.Stem);
                Assert.AreEqual("file**.txt", gpi.Suffix);
                Assert.AreEqual(int.MaxValue, gpi.SuffixDepth);
            }
            {
                GlobPatternInfo gpi = new GlobPatternInfo("dir*/file.txt");
                Assert.AreEqual("", gpi.Stem);
                Assert.AreEqual("dir*/file.txt", gpi.Suffix);
                Assert.AreEqual(2, gpi.SuffixDepth);
            }

        }

    }
}