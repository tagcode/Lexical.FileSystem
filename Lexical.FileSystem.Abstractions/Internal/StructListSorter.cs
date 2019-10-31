// -----------------------------------------------------------------
// Copyright:      Toni Kalajainen 
// Date:           19.3.2019
// -----------------------------------------------------------------
using System.Collections.Generic;

namespace Lexical.FileSystem.Internal
{
    /// <summary>
    /// Inplace sorter that is intended specifically for struct based lists, but works on any <see cref="IList{T}"/>.
    /// </summary>
    /// <typeparam name="List"></typeparam>
    /// <typeparam name="Element"></typeparam>
    public struct StructListSorter<List, Element> where List : IList<Element>
    {
        IComparer<Element> comparer;
        /// <summary>
        /// Create sorter
        /// </summary>
        /// <param name="comparer"></param>
        public StructListSorter(IComparer<Element> comparer)
        {
            this.comparer = comparer ?? Comparer<Element>.Default;
        }

        /// <summary>
        /// Reverse elements of a list
        /// </summary>
        /// <param name="list"></param>
        public static void Reverse(ref List list)
        {
            int mid = list.Count / 2;
            for (int i = 0, j = list.Count - 1; i < mid; i++, j--)
            {
                // Swap list[i] and list[j]
                Element tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }

        /// <summary>
        /// Sort elements of list
        /// </summary>
        /// <param name="list"></param>
        public void Sort(ref List list)
            => QuickSort(ref list, 0, list.Count - 1);

        /// <summary>
        /// Sort elements of list
        /// </summary>
        /// <param name="list"></param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        public void QuickSort(ref List list, int left, int right)
        {
            if (left < right)
            {
                int pivot = Partition(ref list, left, right);

                if (pivot > 1)
                {
                    QuickSort(ref list, left, pivot - 1);
                }
                if (pivot + 1 < right)
                {
                    QuickSort(ref list, pivot + 1, right);
                }
            }
        }

        private int Partition(ref List list, int left, int right)
        {
            if (left > right) return -1;
            int end = left;
            Element pivot = list[right];
            for (int i = left; i < right; i++)
            {
                int c = comparer.Compare(list[i], pivot);
                if (c < 0)
                {
                    // Swap list[i] and list[end]
                    Element tmp = list[i];
                    list[i] = list[end];
                    list[end] = tmp;
                    end++;
                }
            }
            // Swap list[end] and list[right]
            {
                Element tmp = list[end];
                list[end] = list[right];
                list[right] = tmp;
            }
            return end;
        }

        /// <summary>
        /// Sort elements of list
        /// </summary>
        /// <param name="list"></param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        public void QuickSortInverse(ref List list, int left, int right)
        {
            if (left < right)
            {
                int pivot = PartitionInverse(ref list, left, right);

                if (pivot > 1)
                {
                    QuickSortInverse(ref list, left, pivot - 1);
                }
                if (pivot + 1 < right)
                {
                    QuickSortInverse(ref list, pivot + 1, right);
                }
            }
        }

        private int PartitionInverse(ref List list, int left, int right)
        {
            if (left > right) return -1;
            int end = left;
            Element pivot = list[right];
            for (int i = left; i < right; i++)
            {
                int c = comparer.Compare(list[i], pivot);
                if (c > 0)
                {
                    // Swap list[i] and list[end]
                    Element tmp = list[i];
                    list[i] = list[end];
                    list[end] = tmp;
                    end++;
                }
            }
            // Swap list[end] and list[right]
            {
                Element tmp = list[end];
                list[end] = list[right];
                list[right] = tmp;
            }
            return end;
        }


    }
}
