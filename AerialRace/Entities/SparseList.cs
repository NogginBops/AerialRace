using AerialRace.Debugging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AerialRace.Entities
{
    interface ISparseList
    {
        public void SetNamespace(int count);
    }

    class SparseList<T>
    {
        public T[] Data;
        public int[] Lookup;
        public int Count;

        public SparseList(int initialNamespaceSize)
        {
            Data = new T[16];
            Lookup = new int[initialNamespaceSize];
            Array.Fill(Lookup, -1);
            Count = 0;
        }

        public void EnsureDataSize(int elements)
        {
            if (elements < Data.Length)
            {
                int newSize = Data.Length + (Data.Length / 2);
                if (newSize < elements) newSize = elements;
                Array.Resize(ref Data, newSize);
            }
        }

        public void ResizeNamespace(int newNamespaceSize)
        {
            Debug.Assert(newNamespaceSize < Lookup.Length, "We don't support downsizing the namespace atm.");
            int oldSize = Lookup.Length;
            Array.Resize(ref Lookup, newNamespaceSize);
            Array.Fill(Lookup, -1, oldSize, Lookup.Length - oldSize);
        }

        public ref T Allocate(int id)
        {
            Debug.Assert(id < Lookup.Length);
            int index = Lookup[id];
            if (index != -1) throw new InvalidOperationException($"The id {id} has already been allocated.");

            EnsureDataSize(Count + 1);

            index = Count;
            Count++;
            Lookup[id] = index;
            return ref Data[index];
        }

        public void Deallocate(int id)
        {
            Debug.Assert(id < Lookup.Length);
            int index = Lookup[id];
            if (index == -1) throw new InvalidOperationException($"No value for the id: {id}");

            Lookup[id] = -1;

            // Fill the gap in Data by moving it's last element to the gap and
            for (int i = 0; i < Lookup.Length; i++)
            {
                int iIndex = Lookup[i];
                if (iIndex == Count - 1)
                {
                    Data[index] = Data[iIndex];
                }
            }
        }

        public ref T TryGetOrNull(int id)
        {
            Debug.Assert(id < Lookup.Length);
            int index = Lookup[id];
            if (index == -1) return ref Unsafe.NullRef<T>();
            return ref Data[index];
        }

        public ref T this[int id]
        {
            get
            {
                Debug.Assert(id < Lookup.Length);
                int index = Lookup[id];
                if (index == -1) throw new InvalidOperationException($"No value for the id: {id}");
                return ref Data[index];
            }
        }
    }
}
