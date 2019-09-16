// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           21.12.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using Lexical.FileSystem.Internal;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Lexical.FileSystem.Utils
{
    /// <summary>
    /// A disposable that manages a list of disposable objects.
    /// It will dispose them all at Dispose().
    /// </summary>
    public class DisposeList : IDisposeList
    {
        /// <summary>
        /// Lock for modifying dispose list.
        /// </summary>
        protected object m_disposelist_lock = new object();

        /// <summary>
        /// List of disposables that has been attached with this object.
        /// </summary>
        protected StructList4<IDisposable> disposeList = new StructList4<IDisposable>();

        /// <summary>
        /// State that is set when disposing starts and finalizes.
        /// Is changed with Interlocked. 
        ///  0 - not disposed
        ///  1 - disposing
        ///  2 - disposed
        ///  
        /// When disposing starts, new objects cannot be added to the object, instead they are disposed right at away.
        /// </summary>
        protected long disposing;

        /// <summary>
        /// Property that checks thread-synchronously whether disposing has started or completed.
        /// </summary>
        public bool IsDisposing => Interlocked.Read(ref disposing) >= 1L;

        /// <summary>
        /// Property that checks thread-synchronously whether disposing has started.
        /// </summary>
        public bool IsDisposed => Interlocked.Read(ref disposing) == 2L;

        /// <summary>
        /// Dispose all attached diposables and call <see cref="InnerDispose(ref StructList4{Exception})"/>.
        /// </summary>
        /// <exception cref="AggregateException">thrown if disposing threw errors</exception>
        public virtual void Dispose()
        {
            // Is disposing
            Interlocked.CompareExchange(ref disposing, 1L, 0L);

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
            Interlocked.CompareExchange(ref disposing, 2L, 1L);

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
