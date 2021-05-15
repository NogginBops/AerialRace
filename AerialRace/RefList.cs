﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AerialRace
{
    // I wanted to use unmanaged to bound T, but RefList<DrawCommand> won't work then.
    public class RefList<T> : IEnumerable<T> where T : struct
    {
        public const int DEFAULT_SIZE = 16;

        public T[] Data;
        public int Count;

        public int Capacity => Data.Length;

        public int SizeInBytes => Count * Unsafe.SizeOf<T>();

        public RefList()
        {
            Data = new T[DEFAULT_SIZE];
        }

        public RefList(RefList<T> list)
        {
            Data = new T[list.Count];
            Array.Copy(list.Data, Data, list.Count);
        }

        private void Grow(int minCap)
        {
            int newCap = Capacity + (Capacity >> 1);
            Array.Resize(ref Data, Math.Max(newCap, minCap));
        }

        public void EnsureCapacity(int minCap)
        {
            if (Capacity < minCap)
                Grow(minCap);
        }

        public void Add(T element)
        {
            EnsureCapacity(Count + 1);
            Data[Count] = element;
            Count++;
        }

        public void RemoveUnordered(int i)
        {
            Data[i] = Data[Count - 1];
            Count--;
        }

        public void Sort()
        {
            AsSpan().Sort();
        }

        public void Sort(Comparison<T> comparison)
        {
            AsSpan().Sort(comparison);
        }

        public ref T this[int i]
        {
            get 
            {
                if (i >= Count) throw new IndexOutOfRangeException();
                return ref Data[i];
            }
        }

        public int FindIndex(Predicate<T> match)
        {
            for (int i = 0; i < Count; i++)
            {
                if (match(Data[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        public void Clear() => Count = 0;
        
        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return Data[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString()
        {
            return $"[{string.Join(", ", this)}]";
        }

        public Span<T> AsSpan()
        {
            return new Span<T>(Data, 0, Count);
        }
    }
}
