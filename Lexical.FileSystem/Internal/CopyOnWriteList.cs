// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           7.10.2018
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;

namespace Lexical.FileSystem.Internal
{
    /// <summary>
    /// Thread-safe <see cref="IList{T}"/> collection. 
    /// 
    /// List is modified under mutually exclusive lock <see cref="SyncRoot"/>.
    /// 
    /// Enumeration creats an array snapshot which will not throw <see cref="InvalidOperationException"/> if 
    /// list is modified while being enumerated.
    /// </summary>
    public class CopyOnWriteList<T> : IList<T>, ICollection
    {
        /// <summary>Empty array of T</summary>
        static T[] EmptyArray = new T[0];

        /// <summary>
        /// Get or set an element at <paramref name="index"/>.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T this[int index]
        {
            get => Array[index];
            set { lock (SyncRoot) { list[index] = value; snapshot = null; } }
        }

        /// <summary>The number of elements.</summary>
        public int Count
        {
            get
            {
                var _snaphost = snapshot;
                if (_snaphost != null) return _snaphost.Length;
                lock (SyncRoot) return list.Count;
            }
        }

        /// <summary>Is in readonly state</summary>
        public virtual bool IsReadOnly => false;

        /// <summary>
        /// Last snapshot. This snapshot is cleared when internal <see cref="list"/> is modified.
        /// </summary>
        protected T[] snapshot;

        /// <summary>
        /// Array snapshot of current contents. Makes a snapshot if retrieved after content is modified.
        /// </summary>
        public T[] Array => snapshot ?? BuildArray();

        /// <summary>Internal list. Lazy allocation.</summary>
        protected List<T> _list;

        /// <summary>Internal list. Lazy allocation.</summary>
        protected List<T> list { get { lock (SyncRoot) return _list ?? (_list = new List<T>()); } }

        /// <summary>Is synchronized</summary>
        public bool IsSynchronized => true;

        /// <summary>object that can be used to synchronize access</summary>
        public object SyncRoot { get; protected set; } = new object();

        /// <summary>
        /// Create copy-on-write list
        /// </summary>
        public CopyOnWriteList()
        {
        }

        /// <summary>
        /// Create copy-on-write list.
        /// </summary>
        /// <param name="strsEnumr"></param>
        public CopyOnWriteList(IEnumerable<T> strsEnumr)
        {
            this._list = new List<T>(strsEnumr);
        }

        /// <summary>
        /// Construct array
        /// </summary>
        /// <returns></returns>
        protected virtual T[] BuildArray()
        {
            lock (SyncRoot)
            {
                return snapshot = list.Count == 0 ? EmptyArray : list.ToArray();
            }
        }

        /// <summary>
        /// Clear last array
        /// </summary>
        protected virtual void ClearCache()
        {
            this.snapshot = null;
        }

        /// <summary>
        /// Add element
        /// </summary>
        /// <param name="item"></param>
        public virtual void Add(T item)
        {
            lock (SyncRoot)
            {
                list.Add(item);
                ClearCache();
            }
        }

        /// <summary>
        /// Add elements
        /// </summary>
        /// <param name="items"></param>
        public virtual void AddRange(IEnumerable<T> items)
        {
            lock (SyncRoot)
            {
                list.AddRange(items);
                ClearCache();
            }
        }

        /// <summary>
        /// Clear elements
        /// </summary>
        public virtual void Clear()
        {
            lock (SyncRoot)
            {
                if (list.Count == 0) return;
                list.Clear();
                ClearCache();
            }
        }

        /// <summary>
        /// Test if contains element
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(T item)
        {
            var snapshot = Array;
            for (int ix = 0; ix < snapshot.Length; ix++)
                if (snapshot[ix].Equals(item)) return true;
            return false;
        }

        /// <summary>
        /// Copy elements to array
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            var snapshot = Array;
            System.Array.Copy(snapshot, 0, array, arrayIndex, snapshot.Length);
        }

        /// <summary>
        /// Copy elements to array
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(Array array, int arrayIndex)
        {
            var snapshot = Array;
            System.Array.Copy(snapshot, 0, array, arrayIndex, snapshot.Length);
        }

        /// <summary>
        /// Index of element.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int IndexOf(T item)
        {
            var snapshot = Array;
            for (int ix = 0; ix < snapshot.Length; ix++)
                if (snapshot[ix].Equals(item)) return ix;
            return -1;
        }

        /// <summary>
        /// Insert element
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        public virtual void Insert(int index, T item)
        {
            lock (SyncRoot)
            {
                list.Insert(index, item);
                ClearCache();
            }
        }

        /// <summary>
        /// Copy elements from <paramref name="newContent"/>.
        /// </summary>
        /// <param name="newContent"></param>
        public void CopyFrom(IEnumerable<T> newContent)
        {
            lock (SyncRoot)
            {
                list.Clear();
                list.AddRange(newContent);
                ClearCache();
            }
        }

        /// <summary>
        /// Remove element
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public virtual bool Remove(T item)
        {
            lock (SyncRoot)
            {
                bool wasRemoved = list.Remove(item);
                if (wasRemoved) ClearCache();
                return wasRemoved;
            }
        }

        /// <summary>
        /// Remove element at index.
        /// </summary>
        /// <param name="index"></param>
        public virtual void RemoveAt(int index)
        {
            lock (SyncRoot)
            {
                list.RemoveAt(index);
                ClearCache();
            }
        }

        /// <summary>
        /// Get enumerator to a snapsot list. Does not throw <see cref="InvalidOperationException"/>.
        /// </summary>
        /// <returns>enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
            => Array.GetEnumerator();

        /// <summary>
        /// Get enumerator to a snapsot list. Does not throw <see cref="InvalidOperationException"/>.
        /// </summary>
        /// <returns>enumerator</returns>
        public IEnumerator<T> GetEnumerator()
            => ((IEnumerable<T>)Array).GetEnumerator();
    }
}
