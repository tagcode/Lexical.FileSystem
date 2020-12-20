using Lexical.FileSystem.Utility;
using Lexical.Utils.Permutation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lexical.FileSystem.Tests
{
    [TestClass]
    public class TokenTests
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
        public void Test1()
        {
            // Test no constraint token
            {
                IToken t = new Token("Session");
                object tokenString;
                Assert.IsTrue(t.TryGetToken("MyPath", typeof(string).FullName, out tokenString));
                Assert.AreEqual("Session", tokenString);
                Assert.AreEqual(t, t.ListTokens(true).ToArray()[0]);
            }

            // Test constraint token
            {
                IToken t = new Token("Session", typeof(String).FullName, "/MyPath/**");
                object tokenString;
                Assert.IsTrue(t.TryGetToken("/MyPath/File", typeof(string).FullName, out tokenString));
                Assert.AreEqual("Session", tokenString);
                Assert.AreEqual(t, t.ListTokens(true).ToArray()[0]);

                Assert.IsFalse(t.TryGetToken("/WrongPath/File", typeof(string).FullName, out tokenString));
                Assert.IsFalse(t.TryGetToken("/MyPath/File", typeof(object).FullName, out tokenString));
            }

            // Test token array
            {
                IToken t1 = new Token("Session1", typeof(String).FullName, "/Path1/**");
                IToken t2 = new Token("Session2", typeof(String).FullName, "/Path2/**");
                IToken t = new TokenList(t1, t2);

                object tokenString;
                Assert.IsTrue(t.TryGetToken("/Path1/File", typeof(string).FullName, out tokenString));
                Assert.AreEqual("Session1", tokenString);
                Assert.IsTrue(t.TryGetToken("/Path2/File", typeof(string).FullName, out tokenString));
                Assert.AreEqual("Session2", tokenString);
                Assert.AreEqual(t1, t.ListTokens(true).ToArray()[0]);
                Assert.AreEqual(t2, t.ListTokens(true).ToArray()[1]);

                Assert.IsFalse(t.TryGetToken("/WrongPath/File", typeof(string).FullName, out tokenString));
                Assert.IsFalse(t.TryGetToken("/MyPath/File", typeof(object).FullName, out tokenString));

            }

        }

    }

}
