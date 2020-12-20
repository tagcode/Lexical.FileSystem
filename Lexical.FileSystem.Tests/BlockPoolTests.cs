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
    public class BlockPoolTests
    {
        public TestContext TestContext { get; set; }

        static Case count16queue16 = new Case("Pool", "Count=16,Queue=16", null, run => new BlockPool(4096, 16, 16));
        static Case count16queue0 = new Case("Pool", "Count=16,Queue=0", null, run => new BlockPool(4096, 16, 0));

        [TestInitialize]
        public void Initialize()
        {
        }

        [TestCleanup]
        public void Cleanup()
        {
        }

        [TestMethod]
        public void TryAllocate()
        {
            PermutationSetup permutation = new PermutationSetup();
            permutation.Add(count16queue0);
            permutation.Add(count16queue16);

            foreach (Scenario scenario in permutation.Scenarios)
            {
                using (Run run = scenario.Run().Initialize())
                {
                    IBlockPool pool = run.Parameters["Pool"] as BlockPool;
                    byte[] data = null;
                    for (int i = 0; i < 16; i++)
                    {
                        Assert.IsTrue(pool.TryAllocate(out data));
                        Assert.AreEqual(4096, data.Length);
                    }
                    byte[] data2;
                    Assert.IsFalse(pool.TryAllocate(out data2));
                    pool.Return(data);
                    Assert.IsTrue(pool.TryAllocate(out data));
                }
            }

        }

        [TestMethod]
        public void Allocate()
        {
            PermutationSetup permutation = new PermutationSetup();
            permutation.Add(count16queue0);
            permutation.Add(count16queue16);

            foreach (Scenario scenario in permutation.Scenarios)
            {
                using (Run run = scenario.Run().Initialize())
                {
                    IBlockPool pool = run.Parameters["Pool"] as BlockPool;
                    byte[] data = null;
                    for (int i = 0; i < 16; i++)
                    {
                        data = pool.Allocate();
                        Assert.IsTrue(data != null);
                        Assert.AreEqual(4096, data.Length);
                    }
                    Semaphore s = new Semaphore(0, 256);
                    Task.Run(() =>
                    {
                        pool.Allocate();
                        s.Release();
                    });

                    Assert.IsFalse(s.WaitOne(500));
                    pool.Return(data);
                    Assert.IsTrue(s.WaitOne(500));
                }
            }
        }

    }

}
