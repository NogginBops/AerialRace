using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AerialRace.Loading
{
    public class DirtyCollection<T>
    {
        public T[] Elements;
        public bool[] DirtyFlags;
        public int Count;

        public DirtyCollection(int initialSize = 16)
        {
            Elements = new T[initialSize];
            DirtyFlags = new bool[initialSize];
            Count = 0;
        }

        public void EnsureSize(int size)
        {
            if (Elements.Length < size)
            {
                int newSize = Elements.Length + Elements.Length / 2;
                if (newSize < size) newSize = size;
                Array.Resize(ref Elements, newSize);
                Array.Resize(ref DirtyFlags, newSize);
            }
        }

        public bool Add(T element)
        {
            for (int i = 0; i < Count; i++)
            {
                if (EqualityComparer<T>.Default.Equals(element, Elements[i]))
                {
                    return false;
                }
            }

            EnsureSize(Count + 1);

            Elements[Count] = element;
            DirtyFlags[Count] = false;
            Count++;
            return true;
        }

        public bool Remove(T element)
        {
            for (int i = 0; i < Count; i++)
            {
                if (EqualityComparer<T>.Default.Equals(element, Elements[i]))
                {
                    Elements[i] = Elements[Count - 1];
                    DirtyFlags[i] = DirtyFlags[Count - 1];
                    Count--;
                    return true;
                }
            }

            return false;
        }

        public bool IsDirty(int i)
        {
            if (i >= Count) throw new IndexOutOfRangeException();
            return DirtyFlags[i];
        }

        public void MarkDirty(int i)
        {
            if (i >= Count) throw new IndexOutOfRangeException();
            DirtyFlags[i] = true;
        }

        public void MarkDirty(T element)
        {
            for (int i = 0; i < Count; i++)
            {
                if (EqualityComparer<T>.Default.Equals(element, Elements[i]))
                {
                    DirtyFlags[i] = true;
                    return;
                }
            }

            throw new InvalidOperationException();
        }

        public void MarkClean(int i)
        {
            if (i >= Count) throw new IndexOutOfRangeException();
            DirtyFlags[i] = false;
        }

        public T this[int i]
        {
            get
            {
                if (i >= Count) throw new IndexOutOfRangeException();
                return Elements[i];
            }
        }
    }
}
