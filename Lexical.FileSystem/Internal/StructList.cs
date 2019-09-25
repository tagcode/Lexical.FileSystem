// -----------------------------------------------------------------
// Copyright:      Toni Kalajainen 
// Date:           19.3.2019
// -----------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;

namespace Lexical.FileSystem.Internal
{
    /// <summary>
    /// A list where the first 1 element(s) are stack allocated, and rest are allocated from heap when needed.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct StructList1<T> : IList<T>
    {
        /// <summary>
        /// The number of elements that are stack allocated.
        /// </summary>
        const int StackCount = 1;

        /// <summary>
        /// Number of elements
        /// </summary>
        int count;

        /// <summary>
        /// First elements
        /// </summary>
        T _0;

        /// <summary>
        /// Elements after <see cref="StackCount"/>.
        /// </summary>
        List<T> rest;

        /// <summary>
        /// Element comparer
        /// </summary>
        IEqualityComparer<T> elementComparer;

        /// <summary>
        /// Construct lazy list.
        /// </summary>
        /// <param name="elementComparer"></param>
        public StructList1(IEqualityComparer<T> elementComparer = default)
        {
            this.elementComparer = elementComparer;
            count = 0;
            _0 = default;
            rest = null;
        }

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">index is not a valid index in the StructList1`1.</exception>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= count) throw new ArgumentOutOfRangeException();
                switch (index)
                {
                    case 0: return _0;
                    default: return rest[index - StackCount];
                }
            }
            set
            {
                if (index < 0 || index >= count) throw new ArgumentOutOfRangeException();
                switch (index)
                {
                    case 0: _0 = value; return;
                    default: rest[index - StackCount] = value; return;
                }
            }
        }

        /// <summary>
        /// Number of elements in the list
        /// </summary>
        public int Count => count;

        /// <summary>
        /// Is list readonly
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Adds an item to the StructList1`1.
        /// </summary>
        /// <param name="item">The object to add to the StructList1`1.</param>
        /// <exception cref="System.NotSupportedException">The StructList1`1 is read-only.</exception>
        public void Add(T item)
        {
            switch (count)
            {
                case 0: _0 = item; count++; return;
                default:
                    if (rest == null) rest = new List<T>();
                    rest.Add(item);
                    count++;
                    return;
            }
        }

        /// <summary>
        /// Adds an item to the StructList1`1, if the item isn't already in the list.
        /// </summary>
        /// <param name="item">The object to add to the StructList1`1.</param>
        /// <exception cref="System.NotSupportedException">The StructList1`1 is read-only.</exception>
        public void AddIfNew(T item)
        {
            if (Contains(item)) return;
            switch (count)
            {
                case 0: _0 = item; count++; return;
                default:
                    if (rest == null) rest = new List<T>();
                    rest.Add(item);
                    count++;
                    return;
            }
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the StructList1`1.
        /// </summary>
        /// <param name="item">The object to remove from the StructList1`1.</param>
        /// <returns>true if item was successfully removed from the StructList1`1; otherwise, false. This method also returns false if item is not found in the original StructList1`1.</returns>
        public bool Remove(T item)
        {
            IEqualityComparer<T> comparer = elementComparer ?? EqualityComparer<T>.Default;

            if (count == 0) return false;
            if (count >= 1 && comparer.Equals(_0, item)) { RemoveAt(0); return true; }

            if (rest == null) return false;
            bool removed = rest.Remove(item);
            if (removed) count--;
            return removed;
        }

        /// <summary>
        /// Removes the StructList1`1 item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">index is not a valid index in the StructList1`1.</exception>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= count) throw new ArgumentOutOfRangeException();
            if (index <= 0 && count > 1) { _0 = rest[0]; rest.RemoveAt(0); }
            if (index >= StackCount) rest.RemoveAt(index - StackCount);
            count--;
        }

        /// <summary>
        /// Removes and returns the element at the end of the list.
        /// </summary>
        /// <returns>the last element</returns>
        /// <exception cref="InvalidOperationException">If list is empty</exception>
        public T Dequeue()
        {
            if (count == 0) throw new InvalidOperationException();
            int ix = count - 1;
            T result = this[ix];
            RemoveAt(ix);
            return result;
        }

        /// <summary>
        /// Removes all items from the StructList1`1.
        /// </summary>
        /// <exception cref="System.NotSupportedException">The StructList1`1 is read-only.</exception>
        public void Clear()
        {
            if (count >= 1) _0 = default;
            if (rest != null) rest.Clear();
            count = 0;
        }

        /// <summary>
        /// Determines whether the StructList1`1 contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the StructList1`1.</param>
        /// <returns>true if item is found in the StructList1`1; otherwise, false.</returns>
        public bool Contains(T item)
        {
            if (count == 0) return false;
            IEqualityComparer<T> comparer = elementComparer ?? EqualityComparer<T>.Default;
            if (count >= 1 && comparer.Equals(_0, item)) return true;
            if (rest != null) return rest.Contains(item);
            return false;
        }

        /// <summary>
        /// Determines the index of a specific item in the StructList1`1.
        /// </summary>
        /// <param name="item">The object to locate in the StructList1`1.</param>
        /// <returns>The index of item if found in the list; otherwise, -1.</returns>
        public int IndexOf(T item)
        {
            IEqualityComparer<T> comparer = elementComparer ?? EqualityComparer<T>.Default;
            if (count >= 1 && comparer.Equals(_0, item)) return 0;
            if (rest != null) return rest.IndexOf(item) - StackCount;
            return -1;
        }

        /// <summary>
        /// Inserts an item to the StructList1`1 at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which item should be inserted.</param>
        /// <param name="item">The object to insert into the StructList1`1.</param>
        /// <exception cref="ArgumentOutOfRangeException">index is not a valid index in the StructList1`1.</exception>
        public void Insert(int index, T item)
        {
            if (index < 0 || index > count) throw new ArgumentOutOfRangeException();
            if (index >= 1) { if (rest == null) rest = new List<T>(); rest.Insert(index - StackCount, item); }
            if (index <= 0 && count >= 1) { if (rest == null) rest = new List<T>(); rest.Insert(0, _0); }

            count++;
            this[index] = item;
        }

        /// <summary>
        /// Copies the elements of the StructList1`1 to an System.Array, starting at a particular System.Array index.
        /// </summary>
        /// <param name="array">The one-dimensional System.Array that is the destination of the elements copied from StructList1`1. The System.Array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        /// <exception cref="System.ArgumentNullException">array is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">arrayIndex is less than 0.</exception>
        /// <exception cref="System.ArgumentException">The number of elements in the source StructList1`1 is greater than the available space from arrayIndex to the end of the destination array.</exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0) throw new ArgumentOutOfRangeException();
            if (count > array.Length + arrayIndex) throw new ArgumentException();

            if (count >= 1) array[arrayIndex++] = _0;
            if (rest != null) rest.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Create array.
        /// </summary>
        /// <returns></returns>
        public T[] ToArray()
        {
            T[] result = new T[count];
            if (count >= 1) result[0] = _0;
            if (count > 1)
            {
                for (int i = 1; i < count; i++)
                    result[i] = rest[i - 1];
            }
            return result;
        }

        /// <summary>
        /// Create array with elements reversed.
        /// </summary>
        /// <returns></returns>
        public T[] ToReverseArray()
        {
            T[] result = new T[count];
            if (count >= 1) result[count - 1] = _0;
            if (count > 1)
            {
                for (int i = 1; i < count; i++)
                    result[count - 1 - i] = rest[i - 1];
            }
            return result;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            if (count > 0) yield return _0;
            if (rest != null)
            {
                IEnumerator<T> restEtor = rest.GetEnumerator();
                while (restEtor.MoveNext())
                    yield return restEtor.Current;
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            if (count > 0) yield return _0;
            if (rest != null)
            {
                IEnumerator<T> restEtor = rest.GetEnumerator();
                while (restEtor.MoveNext())
                    yield return restEtor.Current;
            }
        }

    }

    /// <summary>
    /// A list where the first 2 element(s) are stack allocated, and rest are allocated from heap when needed.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct StructList2<T> : IList<T>
    {
        /// <summary>
        /// The number of elements that are stack allocated.
        /// </summary>
        const int StackCount = 2;

        /// <summary>
        /// Number of elements
        /// </summary>
        int count;

        /// <summary>
        /// First elements
        /// </summary>
        T _0, _1;

        /// <summary>
        /// Elements after <see cref="StackCount"/>.
        /// </summary>
        List<T> rest;

        /// <summary>
        /// Element comparer
        /// </summary>
        IEqualityComparer<T> elementComparer;

        /// <summary>
        /// Construct lazy list.
        /// </summary>
        /// <param name="elementComparer"></param>
        public StructList2(IEqualityComparer<T> elementComparer = default)
        {
            this.elementComparer = elementComparer;
            count = 0;
            _0 = default;
            _1 = default;
            rest = null;
        }

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">index is not a valid index in the StructList2`1.</exception>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= count) throw new ArgumentOutOfRangeException();
                switch (index)
                {
                    case 0: return _0;
                    case 1: return _1;
                    default: return rest[index - StackCount];
                }
            }
            set
            {
                if (index < 0 || index >= count) throw new ArgumentOutOfRangeException();
                switch (index)
                {
                    case 0: _0 = value; return;
                    case 1: _1 = value; return;
                    default: rest[index - StackCount] = value; return;
                }
            }
        }

        /// <summary>
        /// Number of elements in the list
        /// </summary>
        public int Count => count;

        /// <summary>
        /// Is list readonly
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Adds an item to the StructList2`1.
        /// </summary>
        /// <param name="item">The object to add to the StructList2`1.</param>
        /// <exception cref="System.NotSupportedException">The StructList2`1 is read-only.</exception>
        public void Add(T item)
        {
            switch (count)
            {
                case 0: _0 = item; count++; return;
                case 1: _1 = item; count++; return;
                default:
                    if (rest == null) rest = new List<T>();
                    rest.Add(item);
                    count++;
                    return;
            }
        }

        /// <summary>
        /// Adds an item to the StructList2`1, if the item isn't already in the list.
        /// </summary>
        /// <param name="item">The object to add to the StructList2`1.</param>
        /// <exception cref="System.NotSupportedException">The StructList2`1 is read-only.</exception>
        public void AddIfNew(T item)
        {
            if (Contains(item)) return;
            switch (count)
            {
                case 0: _0 = item; count++; return;
                case 1: _1 = item; count++; return;
                default:
                    if (rest == null) rest = new List<T>();
                    rest.Add(item);
                    count++;
                    return;
            }
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the StructList2`1.
        /// </summary>
        /// <param name="item">The object to remove from the StructList2`1.</param>
        /// <returns>true if item was successfully removed from the StructList2`1; otherwise, false. This method also returns false if item is not found in the original StructList2`1.</returns>
        public bool Remove(T item)
        {
            IEqualityComparer<T> comparer = elementComparer ?? EqualityComparer<T>.Default;

            if (count == 0) return false;
            if (count >= 1 && comparer.Equals(_0, item)) { RemoveAt(0); return true; }
            if (count >= 2 && comparer.Equals(_1, item)) { RemoveAt(1); return true; }

            if (rest == null) return false;
            bool removed = rest.Remove(item);
            if (removed) count--;
            return removed;
        }

        /// <summary>
        /// Removes the StructList2`1 item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">index is not a valid index in the StructList2`1.</exception>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= count) throw new ArgumentOutOfRangeException();
            if (index <= 0 && count > 1) _0 = _1;
            if (index <= 1 && count > 2) { _1 = rest[0]; rest.RemoveAt(0); }
            if (index >= StackCount) rest.RemoveAt(index - StackCount);
            count--;
        }

        /// <summary>
        /// Removes and returns the element at the end of the list.
        /// </summary>
        /// <returns>the last element</returns>
        /// <exception cref="InvalidOperationException">If list is empty</exception>
        public T Dequeue()
        {
            if (count == 0) throw new InvalidOperationException();
            int ix = count - 1;
            T result = this[ix];
            RemoveAt(ix);
            return result;
        }

        /// <summary>
        /// Removes all items from the StructList2`1.
        /// </summary>
        /// <exception cref="System.NotSupportedException">The StructList2`1 is read-only.</exception>
        public void Clear()
        {
            if (count >= 1) _0 = default;
            if (count >= 2) _1 = default;
            if (rest != null) rest.Clear();
            count = 0;
        }

        /// <summary>
        /// Determines whether the StructList2`1 contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the StructList2`1.</param>
        /// <returns>true if item is found in the StructList2`1; otherwise, false.</returns>
        public bool Contains(T item)
        {
            if (count == 0) return false;
            IEqualityComparer<T> comparer = elementComparer ?? EqualityComparer<T>.Default;
            if (count >= 1 && comparer.Equals(_0, item)) return true;
            if (count >= 2 && comparer.Equals(_1, item)) return true;
            if (rest != null) return rest.Contains(item);
            return false;
        }

        /// <summary>
        /// Determines the index of a specific item in the StructList2`1.
        /// </summary>
        /// <param name="item">The object to locate in the StructList2`1.</param>
        /// <returns>The index of item if found in the list; otherwise, -1.</returns>
        public int IndexOf(T item)
        {
            IEqualityComparer<T> comparer = elementComparer ?? EqualityComparer<T>.Default;
            if (count >= 1 && comparer.Equals(_0, item)) return 0;
            if (count >= 2 && comparer.Equals(_1, item)) return 1;
            if (rest != null) return rest.IndexOf(item) - StackCount;
            return -1;
        }

        /// <summary>
        /// Inserts an item to the StructList2`1 at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which item should be inserted.</param>
        /// <param name="item">The object to insert into the StructList2`1.</param>
        /// <exception cref="ArgumentOutOfRangeException">index is not a valid index in the StructList2`1.</exception>
        public void Insert(int index, T item)
        {
            if (index < 0 || index > count) throw new ArgumentOutOfRangeException();
            if (index >= 2) { if (rest == null) rest = new List<T>(); rest.Insert(index - StackCount, item); }
            if (index <= 1 && count >= 2) { if (rest == null) rest = new List<T>(); rest.Insert(0, _1); }
            if (index <= 0 && count >= 1) _1 = _0;

            count++;
            this[index] = item;
        }

        /// <summary>
        /// Copies the elements of the StructList2`1 to an System.Array, starting at a particular System.Array index.
        /// </summary>
        /// <param name="array">The one-dimensional System.Array that is the destination of the elements copied from StructList2`1. The System.Array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        /// <exception cref="System.ArgumentNullException">array is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">arrayIndex is less than 0.</exception>
        /// <exception cref="System.ArgumentException">The number of elements in the source StructList2`1 is greater than the available space from arrayIndex to the end of the destination array.</exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0) throw new ArgumentOutOfRangeException();
            if (count > array.Length + arrayIndex) throw new ArgumentException();

            if (count >= 1) array[arrayIndex++] = _0;
            if (count >= 2) array[arrayIndex++] = _1;
            if (rest != null) rest.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Create array.
        /// </summary>
        /// <returns></returns>
        public T[] ToArray()
        {
            T[] result = new T[count];
            if (count >= 1) result[0] = _0;
            if (count >= 2) result[1] = _1;
            if (count > 2)
            {
                for (int i = 2; i < count; i++)
                    result[i] = rest[i - 2];
            }
            return result;
        }

        /// <summary>
        /// Create array with elements reversed.
        /// </summary>
        /// <returns></returns>
        public T[] ToReverseArray()
        {
            T[] result = new T[count];
            if (count >= 1) result[count - 1] = _0;
            if (count >= 2) result[count - 2] = _1;
            if (count > 2)
            {
                for (int i = 2; i < count; i++)
                    result[count - 1 - i] = rest[i - 2];
            }
            return result;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            if (count > 0) yield return _0;
            if (count > 1) yield return _1;
            if (rest != null)
            {
                IEnumerator<T> restEtor = rest.GetEnumerator();
                while (restEtor.MoveNext())
                    yield return restEtor.Current;
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            if (count > 0) yield return _0;
            if (count > 1) yield return _1;
            if (rest != null)
            {
                IEnumerator<T> restEtor = rest.GetEnumerator();
                while (restEtor.MoveNext())
                    yield return restEtor.Current;
            }
        }

    }

    /// <summary>
    /// A list where the first 4 element(s) are stack allocated, and rest are allocated from heap when needed.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct StructList4<T> : IList<T>
    {
        /// <summary>
        /// The number of elements that are stack allocated.
        /// </summary>
        const int StackCount = 4;

        /// <summary>
        /// Number of elements
        /// </summary>
        int count;

        /// <summary>
        /// First elements
        /// </summary>
        T _0, _1, _2, _3;

        /// <summary>
        /// Elements after <see cref="StackCount"/>.
        /// </summary>
        List<T> rest;

        /// <summary>
        /// Element comparer
        /// </summary>
        IEqualityComparer<T> elementComparer;

        /// <summary>
        /// Construct lazy list.
        /// </summary>
        /// <param name="elementComparer"></param>
        public StructList4(IEqualityComparer<T> elementComparer = default)
        {
            this.elementComparer = elementComparer;
            count = 0;
            _0 = default;
            _1 = default;
            _2 = default;
            _3 = default;
            rest = null;
        }

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">index is not a valid index in the StructList4`1.</exception>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= count) throw new ArgumentOutOfRangeException();
                switch (index)
                {
                    case 0: return _0;
                    case 1: return _1;
                    case 2: return _2;
                    case 3: return _3;
                    default: return rest[index - StackCount];
                }
            }
            set
            {
                if (index < 0 || index >= count) throw new ArgumentOutOfRangeException();
                switch (index)
                {
                    case 0: _0 = value; return;
                    case 1: _1 = value; return;
                    case 2: _2 = value; return;
                    case 3: _3 = value; return;
                    default: rest[index - StackCount] = value; return;
                }
            }
        }

        /// <summary>
        /// Number of elements in the list
        /// </summary>
        public int Count => count;

        /// <summary>
        /// Is list readonly
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Adds an item to the StructList4`1.
        /// </summary>
        /// <param name="item">The object to add to the StructList4`1.</param>
        /// <exception cref="System.NotSupportedException">The StructList4`1 is read-only.</exception>
        public void Add(T item)
        {
            switch (count)
            {
                case 0: _0 = item; count++; return;
                case 1: _1 = item; count++; return;
                case 2: _2 = item; count++; return;
                case 3: _3 = item; count++; return;
                default:
                    if (rest == null) rest = new List<T>();
                    rest.Add(item);
                    count++;
                    return;
            }
        }

        /// <summary>
        /// Adds an item to the StructList4`1, if the item isn't already in the list.
        /// </summary>
        /// <param name="item">The object to add to the StructList4`1.</param>
        /// <exception cref="System.NotSupportedException">The StructList4`1 is read-only.</exception>
        public void AddIfNew(T item)
        {
            if (Contains(item)) return;
            switch (count)
            {
                case 0: _0 = item; count++; return;
                case 1: _1 = item; count++; return;
                case 2: _2 = item; count++; return;
                case 3: _3 = item; count++; return;
                default:
                    if (rest == null) rest = new List<T>();
                    rest.Add(item);
                    count++;
                    return;
            }
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the StructList4`1.
        /// </summary>
        /// <param name="item">The object to remove from the StructList4`1.</param>
        /// <returns>true if item was successfully removed from the StructList4`1; otherwise, false. This method also returns false if item is not found in the original StructList4`1.</returns>
        public bool Remove(T item)
        {
            IEqualityComparer<T> comparer = elementComparer ?? EqualityComparer<T>.Default;

            if (count == 0) return false;
            if (count >= 1 && comparer.Equals(_0, item)) { RemoveAt(0); return true; }
            if (count >= 2 && comparer.Equals(_1, item)) { RemoveAt(1); return true; }
            if (count >= 3 && comparer.Equals(_2, item)) { RemoveAt(2); return true; }
            if (count >= 4 && comparer.Equals(_3, item)) { RemoveAt(3); return true; }

            if (rest == null) return false;
            bool removed = rest.Remove(item);
            if (removed) count--;
            return removed;
        }

        /// <summary>
        /// Removes the StructList4`1 item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">index is not a valid index in the StructList4`1.</exception>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= count) throw new ArgumentOutOfRangeException();
            if (index <= 0 && count > 1) _0 = _1;
            if (index <= 1 && count > 2) _1 = _2;
            if (index <= 2 && count > 3) _2 = _3;
            if (index <= 3 && count > 4) { _3 = rest[0]; rest.RemoveAt(0); }
            if (index >= StackCount) rest.RemoveAt(index - StackCount);
            count--;
        }

        /// <summary>
        /// Removes and returns the element at the end of the list.
        /// </summary>
        /// <returns>the last element</returns>
        /// <exception cref="InvalidOperationException">If list is empty</exception>
        public T Dequeue()
        {
            if (count == 0) throw new InvalidOperationException();
            int ix = count - 1;
            T result = this[ix];
            RemoveAt(ix);
            return result;
        }

        /// <summary>
        /// Removes all items from the StructList4`1.
        /// </summary>
        /// <exception cref="System.NotSupportedException">The StructList4`1 is read-only.</exception>
        public void Clear()
        {
            if (count >= 1) _0 = default;
            if (count >= 2) _1 = default;
            if (count >= 3) _2 = default;
            if (count >= 4) _3 = default;
            if (rest != null) rest.Clear();
            count = 0;
        }

        /// <summary>
        /// Determines whether the StructList4`1 contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the StructList4`1.</param>
        /// <returns>true if item is found in the StructList4`1; otherwise, false.</returns>
        public bool Contains(T item)
        {
            if (count == 0) return false;
            IEqualityComparer<T> comparer = elementComparer ?? EqualityComparer<T>.Default;
            if (count >= 1 && comparer.Equals(_0, item)) return true;
            if (count >= 2 && comparer.Equals(_1, item)) return true;
            if (count >= 3 && comparer.Equals(_2, item)) return true;
            if (count >= 4 && comparer.Equals(_3, item)) return true;
            if (rest != null) return rest.Contains(item);
            return false;
        }

        /// <summary>
        /// Determines the index of a specific item in the StructList4`1.
        /// </summary>
        /// <param name="item">The object to locate in the StructList4`1.</param>
        /// <returns>The index of item if found in the list; otherwise, -1.</returns>
        public int IndexOf(T item)
        {
            IEqualityComparer<T> comparer = elementComparer ?? EqualityComparer<T>.Default;
            if (count >= 1 && comparer.Equals(_0, item)) return 0;
            if (count >= 2 && comparer.Equals(_1, item)) return 1;
            if (count >= 3 && comparer.Equals(_2, item)) return 2;
            if (count >= 4 && comparer.Equals(_3, item)) return 3;
            if (rest != null) return rest.IndexOf(item) - StackCount;
            return -1;
        }

        /// <summary>
        /// Inserts an item to the StructList4`1 at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which item should be inserted.</param>
        /// <param name="item">The object to insert into the StructList4`1.</param>
        /// <exception cref="ArgumentOutOfRangeException">index is not a valid index in the StructList4`1.</exception>
        public void Insert(int index, T item)
        {
            if (index < 0 || index > count) throw new ArgumentOutOfRangeException();
            if (index >= 4) { if (rest == null) rest = new List<T>(); rest.Insert(index - StackCount, item); }
            if (index <= 3 && count >= 4) { if (rest == null) rest = new List<T>(); rest.Insert(0, _3); }
            if (index <= 2 && count >= 3) _3 = _2;
            if (index <= 1 && count >= 2) _2 = _1;
            if (index <= 0 && count >= 1) _1 = _0;

            count++;
            this[index] = item;
        }

        /// <summary>
        /// Copies the elements of the StructList4`1 to an System.Array, starting at a particular System.Array index.
        /// </summary>
        /// <param name="array">The one-dimensional System.Array that is the destination of the elements copied from StructList4`1. The System.Array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        /// <exception cref="System.ArgumentNullException">array is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">arrayIndex is less than 0.</exception>
        /// <exception cref="System.ArgumentException">The number of elements in the source StructList4`1 is greater than the available space from arrayIndex to the end of the destination array.</exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0) throw new ArgumentOutOfRangeException();
            if (count > array.Length + arrayIndex) throw new ArgumentException();

            if (count >= 1) array[arrayIndex++] = _0;
            if (count >= 2) array[arrayIndex++] = _1;
            if (count >= 3) array[arrayIndex++] = _2;
            if (count >= 4) array[arrayIndex++] = _3;
            if (rest != null) rest.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Create array.
        /// </summary>
        /// <returns></returns>
        public T[] ToArray()
        {
            T[] result = new T[count];
            if (count >= 1) result[0] = _0;
            if (count >= 2) result[1] = _1;
            if (count >= 3) result[2] = _2;
            if (count >= 4) result[3] = _3;
            if (count > 4)
            {
                for (int i = 4; i < count; i++)
                    result[i] = rest[i - 4];
            }
            return result;
        }

        /// <summary>
        /// Create array with elements reversed.
        /// </summary>
        /// <returns></returns>
        public T[] ToReverseArray()
        {
            T[] result = new T[count];
            if (count >= 1) result[count - 1] = _0;
            if (count >= 2) result[count - 2] = _1;
            if (count >= 3) result[count - 3] = _2;
            if (count >= 4) result[count - 4] = _3;
            if (count > 4)
            {
                for (int i = 4; i < count; i++)
                    result[count - 1 - i] = rest[i - 4];
            }
            return result;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            if (count > 0) yield return _0;
            if (count > 1) yield return _1;
            if (count > 2) yield return _2;
            if (count > 3) yield return _3;
            if (rest != null)
            {
                IEnumerator<T> restEtor = rest.GetEnumerator();
                while (restEtor.MoveNext())
                    yield return restEtor.Current;
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            if (count > 0) yield return _0;
            if (count > 1) yield return _1;
            if (count > 2) yield return _2;
            if (count > 3) yield return _3;
            if (rest != null)
            {
                IEnumerator<T> restEtor = rest.GetEnumerator();
                while (restEtor.MoveNext())
                    yield return restEtor.Current;
            }
        }
    }

    /// <summary>
    /// A list where the first 12 element(s) are stack allocated, and rest are allocated from heap when needed.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct StructList12<T> : IList<T>
    {
        /// <summary>
        /// The number of elements that are stack allocated.
        /// </summary>
        const int StackCount = 12;

        /// <summary>
        /// Number of elements
        /// </summary>
        int count;

        /// <summary>
        /// First elements
        /// </summary>
        T _0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11;

        /// <summary>
        /// Elements after <see cref="StackCount"/>.
        /// </summary>
        List<T> rest;

        /// <summary>
        /// Element comparer
        /// </summary>
        IEqualityComparer<T> elementComparer;

        /// <summary>
        /// Construct lazy list.
        /// </summary>
        /// <param name="elementComparer"></param>
        public StructList12(IEqualityComparer<T> elementComparer = default)
        {
            this.elementComparer = elementComparer;
            count = 0;
            _0 = default;
            _1 = default;
            _2 = default;
            _3 = default;
            _4 = default;
            _5 = default;
            _6 = default;
            _7 = default;
            _8 = default;
            _9 = default;
            _10 = default;
            _11 = default;
            rest = null;
        }

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">index is not a valid index in the StructList12`1.</exception>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= count) throw new ArgumentOutOfRangeException();
                switch (index)
                {
                    case 0: return _0;
                    case 1: return _1;
                    case 2: return _2;
                    case 3: return _3;
                    case 4: return _4;
                    case 5: return _5;
                    case 6: return _6;
                    case 7: return _7;
                    case 8: return _8;
                    case 9: return _9;
                    case 10: return _10;
                    case 11: return _11;
                    default: return rest[index - StackCount];
                }
            }
            set
            {
                if (index < 0 || index >= count) throw new ArgumentOutOfRangeException();
                switch (index)
                {
                    case 0: _0 = value; return;
                    case 1: _1 = value; return;
                    case 2: _2 = value; return;
                    case 3: _3 = value; return;
                    case 4: _4 = value; return;
                    case 5: _5 = value; return;
                    case 6: _6 = value; return;
                    case 7: _7 = value; return;
                    case 8: _8 = value; return;
                    case 9: _9 = value; return;
                    case 10: _10 = value; return;
                    case 11: _11 = value; return;
                    default: rest[index - StackCount] = value; return;
                }
            }
        }

        /// <summary>
        /// Number of elements in the list
        /// </summary>
        public int Count => count;

        /// <summary>
        /// Is list readonly
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Adds an item to the StructList12`1.
        /// </summary>
        /// <param name="item">The object to add to the StructList12`1.</param>
        /// <exception cref="System.NotSupportedException">The StructList12`1 is read-only.</exception>
        public void Add(T item)
        {
            switch (count)
            {
                case 0: _0 = item; count++; return;
                case 1: _1 = item; count++; return;
                case 2: _2 = item; count++; return;
                case 3: _3 = item; count++; return;
                case 4: _4 = item; count++; return;
                case 5: _5 = item; count++; return;
                case 6: _6 = item; count++; return;
                case 7: _7 = item; count++; return;
                case 8: _8 = item; count++; return;
                case 9: _9 = item; count++; return;
                case 10: _10 = item; count++; return;
                case 11: _11 = item; count++; return;
                default:
                    if (rest == null) rest = new List<T>();
                    rest.Add(item);
                    count++;
                    return;
            }
        }

        /// <summary>
        /// Adds an item to the StructList12`1, if the item isn't already in the list.
        /// </summary>
        /// <param name="item">The object to add to the StructList12`1.</param>
        /// <exception cref="System.NotSupportedException">The StructList12`1 is read-only.</exception>
        public void AddIfNew(T item)
        {
            if (Contains(item)) return;
            switch (count)
            {
                case 0: _0 = item; count++; return;
                case 1: _1 = item; count++; return;
                case 2: _2 = item; count++; return;
                case 3: _3 = item; count++; return;
                case 4: _4 = item; count++; return;
                case 5: _5 = item; count++; return;
                case 6: _6 = item; count++; return;
                case 7: _7 = item; count++; return;
                case 8: _8 = item; count++; return;
                case 9: _9 = item; count++; return;
                case 10: _10 = item; count++; return;
                case 11: _11 = item; count++; return;
                default:
                    if (rest == null) rest = new List<T>();
                    rest.Add(item);
                    count++;
                    return;
            }
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the StructList12`1.
        /// </summary>
        /// <param name="item">The object to remove from the StructList12`1.</param>
        /// <returns>true if item was successfully removed from the StructList12`1; otherwise, false. This method also returns false if item is not found in the original StructList12`1.</returns>
        public bool Remove(T item)
        {
            IEqualityComparer<T> comparer = elementComparer ?? EqualityComparer<T>.Default;

            if (count == 0) return false;
            if (count >= 1 && comparer.Equals(_0, item)) { RemoveAt(0); return true; }
            if (count >= 2 && comparer.Equals(_1, item)) { RemoveAt(1); return true; }
            if (count >= 3 && comparer.Equals(_2, item)) { RemoveAt(2); return true; }
            if (count >= 4 && comparer.Equals(_3, item)) { RemoveAt(3); return true; }
            if (count >= 5 && comparer.Equals(_4, item)) { RemoveAt(4); return true; }
            if (count >= 6 && comparer.Equals(_5, item)) { RemoveAt(5); return true; }
            if (count >= 7 && comparer.Equals(_6, item)) { RemoveAt(6); return true; }
            if (count >= 8 && comparer.Equals(_7, item)) { RemoveAt(7); return true; }
            if (count >= 9 && comparer.Equals(_8, item)) { RemoveAt(8); return true; }
            if (count >= 10 && comparer.Equals(_9, item)) { RemoveAt(9); return true; }
            if (count >= 11 && comparer.Equals(_10, item)) { RemoveAt(10); return true; }
            if (count >= 12 && comparer.Equals(_11, item)) { RemoveAt(11); return true; }

            if (rest == null) return false;
            bool removed = rest.Remove(item);
            if (removed) count--;
            return removed;
        }

        /// <summary>
        /// Removes the StructList12`1 item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">index is not a valid index in the StructList12`1.</exception>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= count) throw new ArgumentOutOfRangeException();
            if (index <= 0 && count > 1) _0 = _1;
            if (index <= 1 && count > 2) _1 = _2;
            if (index <= 2 && count > 3) _2 = _3;
            if (index <= 3 && count > 4) _3 = _4;
            if (index <= 4 && count > 5) _4 = _5;
            if (index <= 5 && count > 6) _5 = _6;
            if (index <= 6 && count > 7) _6 = _7;
            if (index <= 7 && count > 8) _7 = _8;
            if (index <= 8 && count > 9) _8 = _9;
            if (index <= 9 && count > 10) _9 = _10;
            if (index <= 10 && count > 11) _10 = _11;
            if (index <= 11 && count > 12) { _11 = rest[0]; rest.RemoveAt(0); }
            if (index >= StackCount) rest.RemoveAt(index - StackCount);
            count--;
        }

        /// <summary>
        /// Removes and returns the element at the end of the list.
        /// </summary>
        /// <returns>the last element</returns>
        /// <exception cref="InvalidOperationException">If list is empty</exception>
        public T Dequeue()
        {
            if (count == 0) throw new InvalidOperationException();
            int ix = count - 1;
            T result = this[ix];
            RemoveAt(ix);
            return result;
        }

        /// <summary>
        /// Removes all items from the StructList12`1.
        /// </summary>
        /// <exception cref="System.NotSupportedException">The StructList12`1 is read-only.</exception>
        public void Clear()
        {
            if (count >= 1) _0 = default;
            if (count >= 2) _1 = default;
            if (count >= 3) _2 = default;
            if (count >= 4) _3 = default;
            if (count >= 5) _4 = default;
            if (count >= 6) _5 = default;
            if (count >= 7) _6 = default;
            if (count >= 8) _7 = default;
            if (count >= 9) _8 = default;
            if (count >= 10) _9 = default;
            if (count >= 11) _10 = default;
            if (count >= 12) _11 = default;
            if (rest != null) rest.Clear();
            count = 0;
        }

        /// <summary>
        /// Determines whether the StructList12`1 contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the StructList12`1.</param>
        /// <returns>true if item is found in the StructList12`1; otherwise, false.</returns>
        public bool Contains(T item)
        {
            if (count == 0) return false;
            IEqualityComparer<T> comparer = elementComparer ?? EqualityComparer<T>.Default;
            if (count >= 1 && comparer.Equals(_0, item)) return true;
            if (count >= 2 && comparer.Equals(_1, item)) return true;
            if (count >= 3 && comparer.Equals(_2, item)) return true;
            if (count >= 4 && comparer.Equals(_3, item)) return true;
            if (count >= 5 && comparer.Equals(_4, item)) return true;
            if (count >= 6 && comparer.Equals(_5, item)) return true;
            if (count >= 7 && comparer.Equals(_6, item)) return true;
            if (count >= 8 && comparer.Equals(_7, item)) return true;
            if (count >= 9 && comparer.Equals(_8, item)) return true;
            if (count >= 10 && comparer.Equals(_9, item)) return true;
            if (count >= 11 && comparer.Equals(_10, item)) return true;
            if (count >= 12 && comparer.Equals(_11, item)) return true;
            if (rest != null) return rest.Contains(item);
            return false;
        }

        /// <summary>
        /// Determines the index of a specific item in the StructList12`1.
        /// </summary>
        /// <param name="item">The object to locate in the StructList12`1.</param>
        /// <returns>The index of item if found in the list; otherwise, -1.</returns>
        public int IndexOf(T item)
        {
            IEqualityComparer<T> comparer = elementComparer ?? EqualityComparer<T>.Default;
            if (count >= 1 && comparer.Equals(_0, item)) return 0;
            if (count >= 2 && comparer.Equals(_1, item)) return 1;
            if (count >= 3 && comparer.Equals(_2, item)) return 2;
            if (count >= 4 && comparer.Equals(_3, item)) return 3;
            if (count >= 5 && comparer.Equals(_4, item)) return 4;
            if (count >= 6 && comparer.Equals(_5, item)) return 5;
            if (count >= 7 && comparer.Equals(_6, item)) return 6;
            if (count >= 8 && comparer.Equals(_7, item)) return 7;
            if (count >= 9 && comparer.Equals(_8, item)) return 8;
            if (count >= 10 && comparer.Equals(_9, item)) return 9;
            if (count >= 11 && comparer.Equals(_10, item)) return 10;
            if (count >= 12 && comparer.Equals(_11, item)) return 11;
            if (rest != null) return rest.IndexOf(item) - StackCount;
            return -1;
        }

        /// <summary>
        /// Inserts an item to the StructList12`1 at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which item should be inserted.</param>
        /// <param name="item">The object to insert into the StructList12`1.</param>
        /// <exception cref="ArgumentOutOfRangeException">index is not a valid index in the StructList12`1.</exception>
        public void Insert(int index, T item)
        {
            if (index < 0 || index > count) throw new ArgumentOutOfRangeException();
            if (index >= 12) { if (rest == null) rest = new List<T>(); rest.Insert(index - StackCount, item); }
            if (index <= 11 && count >= 12) { if (rest == null) rest = new List<T>(); rest.Insert(0, _11); }
            if (index <= 10 && count >= 11) _11 = _10;
            if (index <= 9 && count >= 10) _10 = _9;
            if (index <= 8 && count >= 9) _9 = _8;
            if (index <= 7 && count >= 8) _8 = _7;
            if (index <= 6 && count >= 7) _7 = _6;
            if (index <= 5 && count >= 6) _6 = _5;
            if (index <= 4 && count >= 5) _5 = _4;
            if (index <= 3 && count >= 4) _4 = _3;
            if (index <= 2 && count >= 3) _3 = _2;
            if (index <= 1 && count >= 2) _2 = _1;
            if (index <= 0 && count >= 1) _1 = _0;

            count++;
            this[index] = item;
        }

        /// <summary>
        /// Copies the elements of the StructList12`1 to an System.Array, starting at a particular System.Array index.
        /// </summary>
        /// <param name="array">The one-dimensional System.Array that is the destination of the elements copied from StructList12`1. The System.Array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        /// <exception cref="System.ArgumentNullException">array is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">arrayIndex is less than 0.</exception>
        /// <exception cref="System.ArgumentException">The number of elements in the source StructList12`1 is greater than the available space from arrayIndex to the end of the destination array.</exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0) throw new ArgumentOutOfRangeException();
            if (count > array.Length + arrayIndex) throw new ArgumentException();

            if (count >= 1) array[arrayIndex++] = _0;
            if (count >= 2) array[arrayIndex++] = _1;
            if (count >= 3) array[arrayIndex++] = _2;
            if (count >= 4) array[arrayIndex++] = _3;
            if (count >= 5) array[arrayIndex++] = _4;
            if (count >= 6) array[arrayIndex++] = _5;
            if (count >= 7) array[arrayIndex++] = _6;
            if (count >= 8) array[arrayIndex++] = _7;
            if (count >= 9) array[arrayIndex++] = _8;
            if (count >= 10) array[arrayIndex++] = _9;
            if (count >= 11) array[arrayIndex++] = _10;
            if (count >= 12) array[arrayIndex++] = _11;
            if (rest != null) rest.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Create array.
        /// </summary>
        /// <returns></returns>
        public T[] ToArray()
        {
            T[] result = new T[count];
            if (count >= 1) result[0] = _0;
            if (count >= 2) result[1] = _1;
            if (count >= 3) result[2] = _2;
            if (count >= 4) result[3] = _3;
            if (count >= 5) result[4] = _4;
            if (count >= 6) result[5] = _5;
            if (count >= 7) result[6] = _6;
            if (count >= 8) result[7] = _7;
            if (count >= 9) result[8] = _8;
            if (count >= 10) result[9] = _9;
            if (count >= 11) result[10] = _10;
            if (count >= 12) result[11] = _11;
            if (count > 12)
            {
                for (int i = 12; i < count; i++)
                    result[i] = rest[i - 12];
            }
            return result;
        }

        /// <summary>
        /// Create array with elements reversed.
        /// </summary>
        /// <returns></returns>
        public T[] ToReverseArray()
        {
            T[] result = new T[count];
            if (count >= 1) result[count - 1] = _0;
            if (count >= 2) result[count - 2] = _1;
            if (count >= 3) result[count - 3] = _2;
            if (count >= 4) result[count - 4] = _3;
            if (count >= 5) result[count - 5] = _4;
            if (count >= 6) result[count - 6] = _5;
            if (count >= 7) result[count - 7] = _6;
            if (count >= 8) result[count - 8] = _7;
            if (count >= 9) result[count - 9] = _8;
            if (count >= 10) result[count - 10] = _9;
            if (count >= 11) result[count - 11] = _10;
            if (count >= 12) result[count - 12] = _11;
            if (count > 12)
            {
                for (int i = 12; i < count; i++)
                    result[count - 1 - i] = rest[i - 12];
            }
            return result;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            if (count > 0) yield return _0;
            if (count > 1) yield return _1;
            if (count > 2) yield return _2;
            if (count > 3) yield return _3;
            if (count > 4) yield return _4;
            if (count > 5) yield return _5;
            if (count > 6) yield return _6;
            if (count > 7) yield return _7;
            if (count > 8) yield return _8;
            if (count > 9) yield return _9;
            if (count > 10) yield return _10;
            if (count > 11) yield return _11;
            if (rest != null)
            {
                IEnumerator<T> restEtor = rest.GetEnumerator();
                while (restEtor.MoveNext())
                    yield return restEtor.Current;
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            if (count > 0) yield return _0;
            if (count > 1) yield return _1;
            if (count > 2) yield return _2;
            if (count > 3) yield return _3;
            if (count > 4) yield return _4;
            if (count > 5) yield return _5;
            if (count > 6) yield return _6;
            if (count > 7) yield return _7;
            if (count > 8) yield return _8;
            if (count > 9) yield return _9;
            if (count > 10) yield return _10;
            if (count > 11) yield return _11;
            if (rest != null)
            {
                IEnumerator<T> restEtor = rest.GetEnumerator();
                while (restEtor.MoveNext())
                    yield return restEtor.Current;
            }
        }

    }

    /// <summary>
    /// A list where the first 24 element(s) are stack allocated, and rest are allocated from heap when needed.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct StructList24<T> : IList<T>
    {
        /// <summary>
        /// The number of elements that are stack allocated.
        /// </summary>
        const int StackCount = 24;

        /// <summary>
        /// Number of elements
        /// </summary>
        int count;

        /// <summary>
        /// First elements
        /// </summary>
        T _0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13, _14, _15, _16, _17, _18, _19, _20, _21, _22, _23;

        /// <summary>
        /// Elements after <see cref="StackCount"/>.
        /// </summary>
        List<T> rest;

        /// <summary>
        /// Element comparer
        /// </summary>
        IEqualityComparer<T> elementComparer;

        /// <summary>
        /// Construct lazy list.
        /// </summary>
        /// <param name="elementComparer"></param>
        public StructList24(IEqualityComparer<T> elementComparer = default)
        {
            this.elementComparer = elementComparer;
            count = 0;
            _0 = default;
            _1 = default;
            _2 = default;
            _3 = default;
            _4 = default;
            _5 = default;
            _6 = default;
            _7 = default;
            _8 = default;
            _9 = default;
            _10 = default;
            _11 = default;
            _12 = default;
            _13 = default;
            _14 = default;
            _15 = default;
            _16 = default;
            _17 = default;
            _18 = default;
            _19 = default;
            _20 = default;
            _21 = default;
            _22 = default;
            _23 = default;
            rest = null;
        }

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">index is not a valid index in the StructList24`1.</exception>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= count) throw new ArgumentOutOfRangeException();
                switch (index)
                {
                    case 0: return _0;
                    case 1: return _1;
                    case 2: return _2;
                    case 3: return _3;
                    case 4: return _4;
                    case 5: return _5;
                    case 6: return _6;
                    case 7: return _7;
                    case 8: return _8;
                    case 9: return _9;
                    case 10: return _10;
                    case 11: return _11;
                    case 12: return _12;
                    case 13: return _13;
                    case 14: return _14;
                    case 15: return _15;
                    case 16: return _16;
                    case 17: return _17;
                    case 18: return _18;
                    case 19: return _19;
                    case 20: return _20;
                    case 21: return _21;
                    case 22: return _22;
                    case 23: return _23;
                    default: return rest[index - StackCount];
                }
            }
            set
            {
                if (index < 0 || index >= count) throw new ArgumentOutOfRangeException();
                switch (index)
                {
                    case 0: _0 = value; return;
                    case 1: _1 = value; return;
                    case 2: _2 = value; return;
                    case 3: _3 = value; return;
                    case 4: _4 = value; return;
                    case 5: _5 = value; return;
                    case 6: _6 = value; return;
                    case 7: _7 = value; return;
                    case 8: _8 = value; return;
                    case 9: _9 = value; return;
                    case 10: _10 = value; return;
                    case 11: _11 = value; return;
                    case 12: _12 = value; return;
                    case 13: _13 = value; return;
                    case 14: _14 = value; return;
                    case 15: _15 = value; return;
                    case 16: _16 = value; return;
                    case 17: _17 = value; return;
                    case 18: _18 = value; return;
                    case 19: _19 = value; return;
                    case 20: _20 = value; return;
                    case 21: _21 = value; return;
                    case 22: _22 = value; return;
                    case 23: _23 = value; return;
                    default: rest[index - StackCount] = value; return;
                }
            }
        }

        /// <summary>
        /// Number of elements in the list
        /// </summary>
        public int Count => count;

        /// <summary>
        /// Is list readonly
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Adds an item to the StructList24`1.
        /// </summary>
        /// <param name="item">The object to add to the StructList24`1.</param>
        /// <exception cref="System.NotSupportedException">The StructList24`1 is read-only.</exception>
        public void Add(T item)
        {
            switch (count)
            {
                case 0: _0 = item; count++; return;
                case 1: _1 = item; count++; return;
                case 2: _2 = item; count++; return;
                case 3: _3 = item; count++; return;
                case 4: _4 = item; count++; return;
                case 5: _5 = item; count++; return;
                case 6: _6 = item; count++; return;
                case 7: _7 = item; count++; return;
                case 8: _8 = item; count++; return;
                case 9: _9 = item; count++; return;
                case 10: _10 = item; count++; return;
                case 11: _11 = item; count++; return;
                case 12: _12 = item; count++; return;
                case 13: _13 = item; count++; return;
                case 14: _14 = item; count++; return;
                case 15: _15 = item; count++; return;
                case 16: _16 = item; count++; return;
                case 17: _17 = item; count++; return;
                case 18: _18 = item; count++; return;
                case 19: _19 = item; count++; return;
                case 20: _20 = item; count++; return;
                case 21: _21 = item; count++; return;
                case 22: _22 = item; count++; return;
                case 23: _23 = item; count++; return;
                default:
                    if (rest == null) rest = new List<T>();
                    rest.Add(item);
                    count++;
                    return;
            }
        }

        /// <summary>
        /// Adds an item to the StructList24`1, if the item isn't already in the list.
        /// </summary>
        /// <param name="item">The object to add to the StructList24`1.</param>
        /// <exception cref="System.NotSupportedException">The StructList24`1 is read-only.</exception>
        public void AddIfNew(T item)
        {
            if (Contains(item)) return;
            switch (count)
            {
                case 0: _0 = item; count++; return;
                case 1: _1 = item; count++; return;
                case 2: _2 = item; count++; return;
                case 3: _3 = item; count++; return;
                case 4: _4 = item; count++; return;
                case 5: _5 = item; count++; return;
                case 6: _6 = item; count++; return;
                case 7: _7 = item; count++; return;
                case 8: _8 = item; count++; return;
                case 9: _9 = item; count++; return;
                case 10: _10 = item; count++; return;
                case 11: _11 = item; count++; return;
                case 12: _12 = item; count++; return;
                case 13: _13 = item; count++; return;
                case 14: _14 = item; count++; return;
                case 15: _15 = item; count++; return;
                case 16: _16 = item; count++; return;
                case 17: _17 = item; count++; return;
                case 18: _18 = item; count++; return;
                case 19: _19 = item; count++; return;
                case 20: _20 = item; count++; return;
                case 21: _21 = item; count++; return;
                case 22: _22 = item; count++; return;
                case 23: _23 = item; count++; return;
                default:
                    if (rest == null) rest = new List<T>();
                    rest.Add(item);
                    count++;
                    return;
            }
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the StructList24`1.
        /// </summary>
        /// <param name="item">The object to remove from the StructList24`1.</param>
        /// <returns>true if item was successfully removed from the StructList24`1; otherwise, false. This method also returns false if item is not found in the original StructList24`1.</returns>
        public bool Remove(T item)
        {
            IEqualityComparer<T> comparer = elementComparer ?? EqualityComparer<T>.Default;

            if (count == 0) return false;
            if (count >= 1 && comparer.Equals(_0, item)) { RemoveAt(0); return true; }
            if (count >= 2 && comparer.Equals(_1, item)) { RemoveAt(1); return true; }
            if (count >= 3 && comparer.Equals(_2, item)) { RemoveAt(2); return true; }
            if (count >= 4 && comparer.Equals(_3, item)) { RemoveAt(3); return true; }
            if (count >= 5 && comparer.Equals(_4, item)) { RemoveAt(4); return true; }
            if (count >= 6 && comparer.Equals(_5, item)) { RemoveAt(5); return true; }
            if (count >= 7 && comparer.Equals(_6, item)) { RemoveAt(6); return true; }
            if (count >= 8 && comparer.Equals(_7, item)) { RemoveAt(7); return true; }
            if (count >= 9 && comparer.Equals(_8, item)) { RemoveAt(8); return true; }
            if (count >= 10 && comparer.Equals(_9, item)) { RemoveAt(9); return true; }
            if (count >= 11 && comparer.Equals(_10, item)) { RemoveAt(10); return true; }
            if (count >= 12 && comparer.Equals(_11, item)) { RemoveAt(11); return true; }
            if (count >= 13 && comparer.Equals(_12, item)) { RemoveAt(12); return true; }
            if (count >= 14 && comparer.Equals(_13, item)) { RemoveAt(13); return true; }
            if (count >= 15 && comparer.Equals(_14, item)) { RemoveAt(14); return true; }
            if (count >= 16 && comparer.Equals(_15, item)) { RemoveAt(15); return true; }
            if (count >= 17 && comparer.Equals(_16, item)) { RemoveAt(16); return true; }
            if (count >= 18 && comparer.Equals(_17, item)) { RemoveAt(17); return true; }
            if (count >= 19 && comparer.Equals(_18, item)) { RemoveAt(18); return true; }
            if (count >= 20 && comparer.Equals(_19, item)) { RemoveAt(19); return true; }
            if (count >= 21 && comparer.Equals(_20, item)) { RemoveAt(20); return true; }
            if (count >= 22 && comparer.Equals(_21, item)) { RemoveAt(21); return true; }
            if (count >= 23 && comparer.Equals(_22, item)) { RemoveAt(22); return true; }
            if (count >= 24 && comparer.Equals(_23, item)) { RemoveAt(23); return true; }

            if (rest == null) return false;
            bool removed = rest.Remove(item);
            if (removed) count--;
            return removed;
        }

        /// <summary>
        /// Removes the StructList24`1 item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">index is not a valid index in the StructList24`1.</exception>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= count) throw new ArgumentOutOfRangeException();
            if (index <= 0 && count > 1) _0 = _1;
            if (index <= 1 && count > 2) _1 = _2;
            if (index <= 2 && count > 3) _2 = _3;
            if (index <= 3 && count > 4) _3 = _4;
            if (index <= 4 && count > 5) _4 = _5;
            if (index <= 5 && count > 6) _5 = _6;
            if (index <= 6 && count > 7) _6 = _7;
            if (index <= 7 && count > 8) _7 = _8;
            if (index <= 8 && count > 9) _8 = _9;
            if (index <= 9 && count > 10) _9 = _10;
            if (index <= 10 && count > 11) _10 = _11;
            if (index <= 11 && count > 12) _11 = _12;
            if (index <= 12 && count > 13) _12 = _13;
            if (index <= 13 && count > 14) _13 = _14;
            if (index <= 14 && count > 15) _14 = _15;
            if (index <= 15 && count > 16) _15 = _16;
            if (index <= 16 && count > 17) _16 = _17;
            if (index <= 17 && count > 18) _17 = _18;
            if (index <= 18 && count > 19) _18 = _19;
            if (index <= 19 && count > 20) _19 = _20;
            if (index <= 20 && count > 21) _20 = _21;
            if (index <= 21 && count > 22) _21 = _22;
            if (index <= 22 && count > 23) _22 = _23;
            if (index <= 23 && count > 24) { _23 = rest[0]; rest.RemoveAt(0); }
            if (index >= StackCount) rest.RemoveAt(index - StackCount);
            count--;
        }

        /// <summary>
        /// Removes and returns the element at the end of the list.
        /// </summary>
        /// <returns>the last element</returns>
        /// <exception cref="InvalidOperationException">If list is empty</exception>
        public T Dequeue()
        {
            if (count == 0) throw new InvalidOperationException();
            int ix = count - 1;
            T result = this[ix];
            RemoveAt(ix);
            return result;
        }

        /// <summary>
        /// Removes all items from the StructList24`1.
        /// </summary>
        /// <exception cref="System.NotSupportedException">The StructList24`1 is read-only.</exception>
        public void Clear()
        {
            if (count >= 1) _0 = default;
            if (count >= 2) _1 = default;
            if (count >= 3) _2 = default;
            if (count >= 4) _3 = default;
            if (count >= 5) _4 = default;
            if (count >= 6) _5 = default;
            if (count >= 7) _6 = default;
            if (count >= 8) _7 = default;
            if (count >= 9) _8 = default;
            if (count >= 10) _9 = default;
            if (count >= 11) _10 = default;
            if (count >= 12) _11 = default;
            if (count >= 13) _12 = default;
            if (count >= 14) _13 = default;
            if (count >= 15) _14 = default;
            if (count >= 16) _15 = default;
            if (count >= 17) _16 = default;
            if (count >= 18) _17 = default;
            if (count >= 19) _18 = default;
            if (count >= 20) _19 = default;
            if (count >= 21) _20 = default;
            if (count >= 22) _21 = default;
            if (count >= 23) _22 = default;
            if (count >= 24) _23 = default;
            if (rest != null) rest.Clear();
            count = 0;
        }

        /// <summary>
        /// Determines whether the StructList24`1 contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the StructList24`1.</param>
        /// <returns>true if item is found in the StructList24`1; otherwise, false.</returns>
        public bool Contains(T item)
        {
            if (count == 0) return false;
            IEqualityComparer<T> comparer = elementComparer ?? EqualityComparer<T>.Default;
            if (count >= 1 && comparer.Equals(_0, item)) return true;
            if (count >= 2 && comparer.Equals(_1, item)) return true;
            if (count >= 3 && comparer.Equals(_2, item)) return true;
            if (count >= 4 && comparer.Equals(_3, item)) return true;
            if (count >= 5 && comparer.Equals(_4, item)) return true;
            if (count >= 6 && comparer.Equals(_5, item)) return true;
            if (count >= 7 && comparer.Equals(_6, item)) return true;
            if (count >= 8 && comparer.Equals(_7, item)) return true;
            if (count >= 9 && comparer.Equals(_8, item)) return true;
            if (count >= 10 && comparer.Equals(_9, item)) return true;
            if (count >= 11 && comparer.Equals(_10, item)) return true;
            if (count >= 12 && comparer.Equals(_11, item)) return true;
            if (count >= 13 && comparer.Equals(_12, item)) return true;
            if (count >= 14 && comparer.Equals(_13, item)) return true;
            if (count >= 15 && comparer.Equals(_14, item)) return true;
            if (count >= 16 && comparer.Equals(_15, item)) return true;
            if (count >= 17 && comparer.Equals(_16, item)) return true;
            if (count >= 18 && comparer.Equals(_17, item)) return true;
            if (count >= 19 && comparer.Equals(_18, item)) return true;
            if (count >= 20 && comparer.Equals(_19, item)) return true;
            if (count >= 21 && comparer.Equals(_20, item)) return true;
            if (count >= 22 && comparer.Equals(_21, item)) return true;
            if (count >= 23 && comparer.Equals(_22, item)) return true;
            if (count >= 24 && comparer.Equals(_23, item)) return true;
            if (rest != null) return rest.Contains(item);
            return false;
        }

        /// <summary>
        /// Determines the index of a specific item in the StructList24`1.
        /// </summary>
        /// <param name="item">The object to locate in the StructList24`1.</param>
        /// <returns>The index of item if found in the list; otherwise, -1.</returns>
        public int IndexOf(T item)
        {
            IEqualityComparer<T> comparer = elementComparer ?? EqualityComparer<T>.Default;
            if (count >= 1 && comparer.Equals(_0, item)) return 0;
            if (count >= 2 && comparer.Equals(_1, item)) return 1;
            if (count >= 3 && comparer.Equals(_2, item)) return 2;
            if (count >= 4 && comparer.Equals(_3, item)) return 3;
            if (count >= 5 && comparer.Equals(_4, item)) return 4;
            if (count >= 6 && comparer.Equals(_5, item)) return 5;
            if (count >= 7 && comparer.Equals(_6, item)) return 6;
            if (count >= 8 && comparer.Equals(_7, item)) return 7;
            if (count >= 9 && comparer.Equals(_8, item)) return 8;
            if (count >= 10 && comparer.Equals(_9, item)) return 9;
            if (count >= 11 && comparer.Equals(_10, item)) return 10;
            if (count >= 12 && comparer.Equals(_11, item)) return 11;
            if (count >= 13 && comparer.Equals(_12, item)) return 12;
            if (count >= 14 && comparer.Equals(_13, item)) return 13;
            if (count >= 15 && comparer.Equals(_14, item)) return 14;
            if (count >= 16 && comparer.Equals(_15, item)) return 15;
            if (count >= 17 && comparer.Equals(_16, item)) return 16;
            if (count >= 18 && comparer.Equals(_17, item)) return 17;
            if (count >= 19 && comparer.Equals(_18, item)) return 18;
            if (count >= 20 && comparer.Equals(_19, item)) return 19;
            if (count >= 21 && comparer.Equals(_20, item)) return 20;
            if (count >= 22 && comparer.Equals(_21, item)) return 21;
            if (count >= 23 && comparer.Equals(_22, item)) return 22;
            if (count >= 24 && comparer.Equals(_23, item)) return 23;
            if (rest != null) return rest.IndexOf(item) - StackCount;
            return -1;
        }

        /// <summary>
        /// Inserts an item to the StructList24`1 at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which item should be inserted.</param>
        /// <param name="item">The object to insert into the StructList24`1.</param>
        /// <exception cref="ArgumentOutOfRangeException">index is not a valid index in the StructList24`1.</exception>
        public void Insert(int index, T item)
        {
            if (index < 0 || index > count) throw new ArgumentOutOfRangeException();
            if (index >= 24) { if (rest == null) rest = new List<T>(); rest.Insert(index - StackCount, item); }
            if (index <= 23 && count >= 24) { if (rest == null) rest = new List<T>(); rest.Insert(0, _23); }
            if (index <= 22 && count >= 23) _23 = _22;
            if (index <= 21 && count >= 22) _22 = _21;
            if (index <= 20 && count >= 21) _21 = _20;
            if (index <= 19 && count >= 20) _20 = _19;
            if (index <= 18 && count >= 19) _19 = _18;
            if (index <= 17 && count >= 18) _18 = _17;
            if (index <= 16 && count >= 17) _17 = _16;
            if (index <= 15 && count >= 16) _16 = _15;
            if (index <= 14 && count >= 15) _15 = _14;
            if (index <= 13 && count >= 14) _14 = _13;
            if (index <= 12 && count >= 13) _13 = _12;
            if (index <= 11 && count >= 12) _12 = _11;
            if (index <= 10 && count >= 11) _11 = _10;
            if (index <= 9 && count >= 10) _10 = _9;
            if (index <= 8 && count >= 9) _9 = _8;
            if (index <= 7 && count >= 8) _8 = _7;
            if (index <= 6 && count >= 7) _7 = _6;
            if (index <= 5 && count >= 6) _6 = _5;
            if (index <= 4 && count >= 5) _5 = _4;
            if (index <= 3 && count >= 4) _4 = _3;
            if (index <= 2 && count >= 3) _3 = _2;
            if (index <= 1 && count >= 2) _2 = _1;
            if (index <= 0 && count >= 1) _1 = _0;

            count++;
            this[index] = item;
        }

        /// <summary>
        /// Copies the elements of the StructList24`1 to an System.Array, starting at a particular System.Array index.
        /// </summary>
        /// <param name="array">The one-dimensional System.Array that is the destination of the elements copied from StructList24`1. The System.Array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        /// <exception cref="System.ArgumentNullException">array is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">arrayIndex is less than 0.</exception>
        /// <exception cref="System.ArgumentException">The number of elements in the source StructList24`1 is greater than the available space from arrayIndex to the end of the destination array.</exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0) throw new ArgumentOutOfRangeException();
            if (count > array.Length + arrayIndex) throw new ArgumentException();

            if (count >= 1) array[arrayIndex++] = _0;
            if (count >= 2) array[arrayIndex++] = _1;
            if (count >= 3) array[arrayIndex++] = _2;
            if (count >= 4) array[arrayIndex++] = _3;
            if (count >= 5) array[arrayIndex++] = _4;
            if (count >= 6) array[arrayIndex++] = _5;
            if (count >= 7) array[arrayIndex++] = _6;
            if (count >= 8) array[arrayIndex++] = _7;
            if (count >= 9) array[arrayIndex++] = _8;
            if (count >= 10) array[arrayIndex++] = _9;
            if (count >= 11) array[arrayIndex++] = _10;
            if (count >= 12) array[arrayIndex++] = _11;
            if (count >= 13) array[arrayIndex++] = _12;
            if (count >= 14) array[arrayIndex++] = _13;
            if (count >= 15) array[arrayIndex++] = _14;
            if (count >= 16) array[arrayIndex++] = _15;
            if (count >= 17) array[arrayIndex++] = _16;
            if (count >= 18) array[arrayIndex++] = _17;
            if (count >= 19) array[arrayIndex++] = _18;
            if (count >= 20) array[arrayIndex++] = _19;
            if (count >= 21) array[arrayIndex++] = _20;
            if (count >= 22) array[arrayIndex++] = _21;
            if (count >= 23) array[arrayIndex++] = _22;
            if (count >= 24) array[arrayIndex++] = _23;
            if (rest != null) rest.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Create array.
        /// </summary>
        /// <returns></returns>
        public T[] ToArray()
        {
            T[] result = new T[count];
            if (count >= 1) result[0] = _0;
            if (count >= 2) result[1] = _1;
            if (count >= 3) result[2] = _2;
            if (count >= 4) result[3] = _3;
            if (count >= 5) result[4] = _4;
            if (count >= 6) result[5] = _5;
            if (count >= 7) result[6] = _6;
            if (count >= 8) result[7] = _7;
            if (count >= 9) result[8] = _8;
            if (count >= 10) result[9] = _9;
            if (count >= 11) result[10] = _10;
            if (count >= 12) result[11] = _11;
            if (count >= 13) result[12] = _12;
            if (count >= 14) result[13] = _13;
            if (count >= 15) result[14] = _14;
            if (count >= 16) result[15] = _15;
            if (count >= 17) result[16] = _16;
            if (count >= 18) result[17] = _17;
            if (count >= 19) result[18] = _18;
            if (count >= 20) result[19] = _19;
            if (count >= 21) result[20] = _20;
            if (count >= 22) result[21] = _21;
            if (count >= 23) result[22] = _22;
            if (count >= 24) result[23] = _23;
            if (count > 24)
            {
                for (int i = 24; i < count; i++)
                    result[i] = rest[i - 24];
            }
            return result;
        }

        /// <summary>
        /// Create array with elements reversed.
        /// </summary>
        /// <returns></returns>
        public T[] ToReverseArray()
        {
            T[] result = new T[count];
            if (count >= 1) result[count - 1] = _0;
            if (count >= 2) result[count - 2] = _1;
            if (count >= 3) result[count - 3] = _2;
            if (count >= 4) result[count - 4] = _3;
            if (count >= 5) result[count - 5] = _4;
            if (count >= 6) result[count - 6] = _5;
            if (count >= 7) result[count - 7] = _6;
            if (count >= 8) result[count - 8] = _7;
            if (count >= 9) result[count - 9] = _8;
            if (count >= 10) result[count - 10] = _9;
            if (count >= 11) result[count - 11] = _10;
            if (count >= 12) result[count - 12] = _11;
            if (count >= 13) result[count - 13] = _12;
            if (count >= 14) result[count - 14] = _13;
            if (count >= 15) result[count - 15] = _14;
            if (count >= 16) result[count - 16] = _15;
            if (count >= 17) result[count - 17] = _16;
            if (count >= 18) result[count - 18] = _17;
            if (count >= 19) result[count - 19] = _18;
            if (count >= 20) result[count - 20] = _19;
            if (count >= 21) result[count - 21] = _20;
            if (count >= 22) result[count - 22] = _21;
            if (count >= 23) result[count - 23] = _22;
            if (count >= 24) result[count - 24] = _23;
            if (count > 24)
            {
                for (int i = 24; i < count; i++)
                    result[count - 1 - i] = rest[i - 24];
            }
            return result;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            if (count > 0) yield return _0;
            if (count > 1) yield return _1;
            if (count > 2) yield return _2;
            if (count > 3) yield return _3;
            if (count > 4) yield return _4;
            if (count > 5) yield return _5;
            if (count > 6) yield return _6;
            if (count > 7) yield return _7;
            if (count > 8) yield return _8;
            if (count > 9) yield return _9;
            if (count > 10) yield return _10;
            if (count > 11) yield return _11;
            if (count > 12) yield return _12;
            if (count > 13) yield return _13;
            if (count > 14) yield return _14;
            if (count > 15) yield return _15;
            if (count > 16) yield return _16;
            if (count > 17) yield return _17;
            if (count > 18) yield return _18;
            if (count > 19) yield return _19;
            if (count > 20) yield return _20;
            if (count > 21) yield return _21;
            if (count > 22) yield return _22;
            if (count > 23) yield return _23;
            if (rest != null)
            {
                IEnumerator<T> restEtor = rest.GetEnumerator();
                while (restEtor.MoveNext())
                    yield return restEtor.Current;
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            if (count > 0) yield return _0;
            if (count > 1) yield return _1;
            if (count > 2) yield return _2;
            if (count > 3) yield return _3;
            if (count > 4) yield return _4;
            if (count > 5) yield return _5;
            if (count > 6) yield return _6;
            if (count > 7) yield return _7;
            if (count > 8) yield return _8;
            if (count > 9) yield return _9;
            if (count > 10) yield return _10;
            if (count > 11) yield return _11;
            if (count > 12) yield return _12;
            if (count > 13) yield return _13;
            if (count > 14) yield return _14;
            if (count > 15) yield return _15;
            if (count > 16) yield return _16;
            if (count > 17) yield return _17;
            if (count > 18) yield return _18;
            if (count > 19) yield return _19;
            if (count > 20) yield return _20;
            if (count > 21) yield return _21;
            if (count > 22) yield return _22;
            if (count > 23) yield return _23;
            if (rest != null)
            {
                IEnumerator<T> restEtor = rest.GetEnumerator();
                while (restEtor.MoveNext())
                    yield return restEtor.Current;
            }
        }

    }
}
