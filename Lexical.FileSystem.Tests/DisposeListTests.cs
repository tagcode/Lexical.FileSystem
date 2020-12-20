using Lexical.FileSystem.Internal;
using Lexical.FileSystem.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace Lexical.FileSystem.Tests
{
    [TestClass]
    public class DisposeListTests
    {
        public TestContext TestContext { get; set; }

        /// <summary>
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
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
        public void TestDisposeList()
        {
            Semaphore s = new Semaphore(0, 100);
            // Create object
            MyDisposable disp = new MyDisposable()
                .AddDisposeAction(d => s.Release(), s);
            //
            Assert.IsFalse(s.WaitOne(100));
            Assert.IsFalse(disp.IsDisposeCalled);
            Assert.IsFalse(disp.IsDisposing);
            Assert.IsFalse(disp.IsDisposed);
            disp.Dispose();
            Assert.IsTrue(s.WaitOne(1000));
            Assert.IsTrue(disp.IsDisposeCalled);
            Assert.IsTrue(disp.IsDisposing);
            Assert.IsTrue(disp.IsDisposed);
        }

        /// <summary>
        /// </summary>
        [TestMethod]
        public void TestBelateDispose()
        {
            Semaphore s = new Semaphore(0, 100);
            // Create object
            MyDisposable disp = new MyDisposable();
            // Belate dispose, this handle is passed to the task
            IDisposable belateHandle = disp.BelateDispose();
            // Start work
            Task.Run(() =>
            {
                // Do work
                Thread.Sleep(1000);
                //
                Assert.IsFalse(disp.IsDisposing);
                Assert.IsTrue(disp.IsDisposeCalled);
                // Release belate handle
                belateHandle.Dispose();
                // Check is disposed
                Assert.IsTrue(disp.IsDisposed);
                //
                s.Release();
            });
            // Start dispose, which is postponed
            disp.Dispose();
            // Check is disposed
            Assert.IsTrue(disp.IsDisposeCalled);
            Assert.IsFalse(disp.IsDisposed);
            Assert.IsFalse(disp.IsDisposing);

            // The test exists before the thread
            s.WaitOne();
            Assert.IsTrue(disp.IsDisposeCalled);
            Assert.IsTrue(disp.IsDisposing);
            Assert.IsTrue(disp.IsDisposed);
        }

        /// <summary>
        /// Test non-dispose object
        /// </summary>
        [TestMethod]
        public void TestNonDisposeList()
        {
            Semaphore s = new Semaphore(0, 100);
            // Create object
            DisposeList disp = FileSystem.OS
                .AddDisposeAction(d => s.Release(), s);
            //
            Assert.IsFalse(s.WaitOne(1000));
            Assert.IsFalse(disp.IsDisposeCalled);
            Assert.IsFalse(disp.IsDisposing);
            Assert.IsFalse(disp.IsDisposed);
            disp.Dispose();
            Assert.IsTrue(s.WaitOne(1000));
            Assert.IsFalse(disp.IsDisposeCalled);
            Assert.IsFalse(disp.IsDisposing);
            Assert.IsFalse(disp.IsDisposed);
        }

        /// <summary>
        /// </summary>
        [TestMethod]
        public void TestBelateNonDispose()
        {
            Semaphore s = new Semaphore(0, 100);
            // Create object
            DisposeList disp = FileSystem.OS;
            // Belate dispose, this handle is passed to the task
            IDisposable belateHandle = disp.BelateDispose();
            // Start work
            Task.Run(() =>
            {
                // Do work
                Thread.Sleep(1000);
                //
                Assert.IsFalse(disp.IsDisposing);
                Assert.IsTrue(disp.IsDisposeCalled);
                // Release belate handle
                belateHandle.Dispose();
                // Check is disposed
                Assert.IsFalse(disp.IsDisposed);
                //
                s.Release();
            });
            // Start dispose, which is postponed
            disp.Dispose();
            // Check is disposed
            Assert.IsTrue(disp.IsDisposeCalled);
            Assert.IsFalse(disp.IsDisposed);
            Assert.IsFalse(disp.IsDisposing);

            // The test exists before the thread
            s.WaitOne();
            Assert.IsFalse(disp.IsDisposeCalled);
            Assert.IsFalse(disp.IsDisposing);
            Assert.IsFalse(disp.IsDisposed);
        }

        class MyDisposable : DisposeList
        {
            protected override void InnerDispose(ref StructList4<Exception> disposeErrors)
            {
            }
            public MyDisposable AddDisposeAction(Action<object> disposeAction, object state)
            {
                ((IDisposeList)this).AddDisposeAction(disposeAction, state);
                return this;
            }
            public MyDisposable AddDisposable(object disposable)
            {
                ((IDisposeList)this).AddDisposable(disposable);
                return this;
            }
            public MyDisposable AddDisposables(IEnumerable disposables)
            {
                ((IDisposeList)this).AddDisposables(disposables);
                return this;
            }
            public MyDisposable RemoveDisposable(object disposable)
            {
                ((IDisposeList)this).RemoveDisposable(disposable);
                return this;
            }
            public MyDisposable RemoveDisposables(IEnumerable disposables)
            {
                ((IDisposeList)this).RemoveDisposables(disposables);
                return this;
            }
        }
    }
}
