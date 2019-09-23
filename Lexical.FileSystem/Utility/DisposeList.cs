// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           21.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Internal;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Lexical.FileSystem.Utility
{
    /// <summary>
    /// A disposable that manages a list of disposable objects.
    /// 
    /// All attached disposables are disposed at <see cref="Dispose"/>.
    /// 
    /// Subclasses that inherit <see cref="DisposeList"/> can put their own dispose
    /// behaviour into <see cref="InnerDispose(ref StructList4{Exception})"/>.
    /// InnerDispose will be called only once. 
    /// </summary>
    public class DisposeList : IDisposeList, IBelatableDispose
    {
        /// <summary>
        /// Lock for modifying <see cref="DisposeList"/>.
        /// </summary>
        protected object m_disposelist_lock = new object();

        /// <summary>
        /// List of disposables that has been attached with this object.
        /// </summary>
        protected StructList2<IDisposable> disposeList = new StructList2<IDisposable>();

        /// <summary>
        /// State that is set when disposing starts and finalizes.
        /// Is changed with Interlocked. 
        ///  0 - not disposed
        ///  1 - dispose called, but not started
        ///  2 - disposing started
        ///  3 - disposed
        ///  
        /// When disposing starts, new objects cannot be added to the object, instead they are disposed right at away.
        /// </summary>
        protected long disposing;

        /// <summary>
        /// Has disposing has start requested but is post-poned for now.
        /// </summary>
        public bool IsDisposeCalled => Interlocked.Read(ref disposing) == 1L;

        /// <summary>
        /// Has disposing has started or completed.
        /// </summary>
        public bool IsDisposing => Interlocked.Read(ref disposing) >= 2L;

        /// <summary>
        /// Is disposing completed
        /// </summary>
        public bool IsDisposed => Interlocked.Read(ref disposing) == 3L;

        /// <summary>
        /// Number of belate handles
        /// </summary>
        protected int belateHandleCount;

        /// <summary>
        /// Delay dispose until belate handle is disposed.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException">Thrown if object has already been disposed.</exception>
        public IDisposable BelateDispose()
        {
            // Create handle
            BelateHandle handle = new BelateHandle(this);
            lock (m_disposelist_lock)
            {
                // Dispose has already been started
                if (IsDisposing) throw new ObjectDisposedException(GetType().FullName);
                // Add counter
                belateHandleCount++;
            }
            // Return handle
            return handle;
        }

        /// <summary>
        /// A handle that postpones dispose of the <see cref="DisposeList"/> object.
        /// </summary>
        class BelateHandle : IDisposable
        {
            DisposeList parent;

            public BelateHandle(DisposeList parent)
            {
                this.parent = parent;
            }

            public void Dispose()
            {
                // Only one thread can dispose
                DisposeList _parent = Interlocked.CompareExchange(ref parent, null, parent);
                // Handle has already been disposed
                if (_parent == null) return;
                // Should dispose be started
                bool processDispose = false;
                // Decrement handle count
                lock (_parent.m_disposelist_lock)
                {
                    int newCount = --_parent.belateHandleCount;
                    // Is not the handle.
                    if (newCount > 0) return;
                    // Send state from 1 to 2
                    processDispose = Interlocked.CompareExchange(ref _parent.disposing, 2L, 1L) == 1L;
                }
                // Start dispose
                if (processDispose) _parent.ProcessDispose();
            }
        }

        /// <summary>
        /// Dispose all attached diposables and call <see cref="InnerDispose(ref StructList4{Exception})"/>.
        /// </summary>
        /// <exception cref="AggregateException">thrown if disposing threw errors</exception>
        public virtual void Dispose()
        {
            // Set object state to have dispose started
            Interlocked.CompareExchange(ref disposing, 1L, 0L);

            // Should dispose be started
            bool processDispose = false;

            lock (m_disposelist_lock)
            {
                // Post-pone if there are belate handles
                if (belateHandleCount > 0) return;
                // Set state to dispose called
                processDispose = Interlocked.CompareExchange(ref disposing, 2L, 1L) == 1L;
            }

            // Start dispose
            if (processDispose) ProcessDispose();
        }

        /// <summary>
        /// Dispose all attached diposables and call <see cref="InnerDispose(ref StructList4{Exception})"/>.
        /// </summary>
        /// <exception cref="AggregateException">thrown if disposing threw errors</exception>
        protected virtual void ProcessDispose()
        {
            // Dispose called
            Interlocked.CompareExchange(ref disposing, 1L, 0L);
            // Dispose started, only one thread processes the dispose.
            if (Interlocked.CompareExchange(ref disposing, 2L, 1L) != 2L) return;

            // Extract snapshot, clear array
            IDisposable[] toDispose = null;
            lock (m_disposelist_lock) { toDispose = disposeList.ToArray(); disposeList.Clear(); }

            // Captured errors
            StructList4<Exception> disposeErrors = new StructList4<Exception>();

            // Dispose disposables
            DisposeAndCapture(toDispose, ref disposeErrors);

            // Call InnerDispose(). Capture errors to compose it with others.
            try
            {
                InnerDispose(ref disposeErrors);
            }
            catch (Exception e)
            {
                // Capture error
                disposeErrors.Add(e);
            }

            // Is disposed
            Interlocked.CompareExchange(ref disposing, 3L, 1L);
            Interlocked.CompareExchange(ref disposing, 3L, 2L);

            // Throw captured errors
            if (disposeErrors.Count>0) throw new AggregateException(disposeErrors);
        }

        /// <summary>
        /// Override this for dispose mechanism of the implementing class.
        /// </summary>
        /// <param name="disposeErrors">list that can be instantiated and where errors can be added</param>
        /// <exception cref="Exception">any exception is captured and aggregated with other errors</exception>
        protected virtual void InnerDispose(ref StructList4<Exception> disposeErrors)
        {
        }

        /// <summary>
        /// Add <paramref name="disposableObject"/> to be disposed with the object.
        /// 
        /// If parent object is disposed or being disposed, the disposable will be disposed immedialy.
        /// </summary>
        /// <param name="disposableObject"></param>
        /// <returns>true if was added to list, false if was disposed right away</returns>
        bool IDisposeList.AddDisposable(Object disposableObject)
        {
            // Argument error
            if (disposableObject == null) throw new ArgumentNullException(nameof(disposableObject));
            // Cast to IDisposable
            IDisposable disposable = disposableObject as IDisposable;
            // Was not disposable, was not added to list
            if (disposable == null) return false;            
            // Parent is disposed/ing
            if (IsDisposing) { disposable.Dispose(); return false; }
            // Add to list
            lock (m_disposelist_lock) disposeList.Add(disposable);
            // Check parent again
            if (IsDisposing) { lock (m_disposelist_lock) disposeList.Remove(disposable); disposable.Dispose(); return false; }
            // OK
            return true;
        }

        /// <summary>
        /// Invoke <paramref name="disposeAction"/> on the dispose of the object.
        /// 
        /// If parent object is disposed or being disposed, the disposable will be disposed immedialy.
        /// </summary>
        /// <param name="disposeAction"></param>
        /// <returns>true if was added to list, false if was disposed right away</returns>
        protected bool AddDisposeAction(Action disposeAction)
        {
            // Argument error
            if (disposeAction == null) throw new ArgumentNullException(nameof(disposeAction));
            // Parent is disposed/ing
            if (IsDisposing) { disposeAction.Invoke(); return false; }
            // Adapt to IDisposable
            IDisposable disposable = new DisposeAction(disposeAction);
            // Add to list
            lock (m_disposelist_lock) disposeList.Add( disposable );
            // Check parent again
            if (IsDisposing) { lock (m_disposelist_lock) disposeList.Remove(disposable); disposable.Dispose(); return false; }
            // OK
            return true;
        }

        /// <summary>
        /// Adapts <see cref="Action"/> into <see cref="IDisposable"/>.
        /// </summary>
        class DisposeAction : IDisposable
        {
            Action a;

            public DisposeAction(Action a)
            {
                this.a = a ?? throw new ArgumentNullException(nameof(a));
            }

            public void Dispose()
            {
                a.Invoke();
            }
        }

        /// <summary>
        /// Add <paramref name="disposableObjects"/> to be disposed with the object.
        /// </summary>
        /// <param name="disposableObjects"></param>
        /// <returns></returns>
        bool IDisposeList.AddDisposables(IEnumerable<Object> disposableObjects)
        {
            // Argument error
            if (disposableObjects == null) throw new ArgumentNullException(nameof(disposableObjects));
            // Parent is disposed/ing
            if (IsDisposing)
            {
                // Captured errors
                StructList4<Exception> disposeErrors = new StructList4<Exception>();
                // Dispose now
                DisposeAndCapture(disposableObjects, ref disposeErrors);
                // Throw captured errors
                if (disposeErrors.Count>0) throw new AggregateException(disposeErrors);
                return false;
            }

            // Add to list
            lock (m_disposelist_lock)
                foreach (Object d in disposableObjects)
                    if (d is IDisposable disposable)
                        disposeList.Add(disposable);

            // Check parent again
            if (IsDisposing)
            {
                // Captured errors
                StructList4<Exception> disposeErrors = new StructList4<Exception>();
                // Dispose now
                DisposeAndCapture(disposableObjects, ref disposeErrors);
                // Remove
                lock (m_disposelist_lock) foreach (IDisposable d in disposableObjects) disposeList.Remove(d);
                // Throw captured errors
                if (disposeErrors.Count>0) throw new AggregateException(disposeErrors);
                return false;
            }

            // OK
            return true;
        }

        /// <summary>
        /// Remove <paramref name="disposableObject"/> from list of attached disposables.
        /// </summary>
        /// <param name="disposableObject"></param>
        /// <returns>true if an item of <paramref name="disposableObject"/> was removed, false if it wasn't there</returns>
        bool IDisposeList.RemoveDisposable(object disposableObject)
        {
            // Argument error
            if (disposableObject == null) throw new ArgumentNullException(nameof(disposableObject));
            // Cast to IDisposable
            IDisposable disposable = disposableObject as IDisposable;
            // Was not IDisposable
            if (disposable == null) return false;
            // Remove from list
            lock (m_disposelist_lock)
            {
                return disposeList.Remove(disposable);
            }
        }

        /// <summary>
        /// Remove <paramref name="disposableObjects"/> from the list. 
        /// </summary>
        /// <param name="disposableObjects"></param>
        /// <returns>true if was removed, false if it wasn't in the list.</returns>
        bool IDisposeList.RemoveDisposables(IEnumerable<object> disposableObjects)
        {
            // Argument error
            if (disposableObjects == null) throw new ArgumentNullException(nameof(disposableObjects));

            bool ok = true;
            lock (this)
            {
                if (disposableObjects == null) return false;
                foreach (Object disposableObject in disposableObjects)
                    if (disposableObject is IDisposable disposable)
                        ok &= disposeList.Remove(disposable);
                return ok;
            }
        }

        /// <summary>
        /// Dispose enumerable and capture errors
        /// </summary>
        /// <param name="disposableObjects">list of disposables</param>
        /// <param name="disposeErrors">list to be created if errors occur</param>
        public static void DisposeAndCapture(IEnumerable<object> disposableObjects, ref StructList4<Exception> disposeErrors)
        {
            if (disposableObjects == null) return;

            // Dispose disposables
            foreach (IDisposable disposableObject in disposableObjects)
            {
                if (disposableObject is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch (AggregateException ae)
                    {
                        foreach (Exception e in ae.InnerExceptions)
                            disposeErrors.Add(e);
                    }
                    catch (Exception e)
                    {
                        // Capture error
                        disposeErrors.Add(e);
                    }
                }
            }
        }
    }
}
