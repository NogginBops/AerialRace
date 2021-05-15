using AerialRace.Debugging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AerialRace.Entities
{
    unsafe readonly struct Arr<T> where T : unmanaged
    {
        public readonly T* Data;
        public readonly int Length;

        public Arr(T* data, int length)
        {
            Data = data;
            Length = length;
        }

        public T this[int i]
        {
            get
            {
                Debug.Assert(i > 0 && i < Length);
                return Data[i];
            }
            set
            {
                Debug.Assert(i > 0 && i < Length);
                Data[i] = value;
            }
        }
    }

    static unsafe class Heap
    {
        public static T* AllocStruct<T>() where T : unmanaged
        {
            return (T*)Marshal.AllocHGlobal(sizeof(T));
        }

        public static Arr<T> AllocArray<T>(int size) where T : unmanaged
        {
            return new Arr<T>((T*)Marshal.AllocHGlobal(size * sizeof(T)), size);
        }

        public const byte FREED_MEMORY_PATTERN = 0xDD;

        [System.Diagnostics.Conditional("DEBUG")]
        public static void MarkFreed<T>(T* ptr, int elements) where T : unmanaged
        {
            Unsafe.InitBlockUnaligned(ptr, FREED_MEMORY_PATTERN, (uint)(elements * sizeof(T)));
        }

        public static void Free<T>(T* ptr) where T : unmanaged
        {
            MarkFreed(ptr, 1);
            Marshal.FreeHGlobal((IntPtr)ptr);
        }

        public static void Free<T>(Arr<T> arr) where T : unmanaged
        {
            MarkFreed(arr.Data, arr.Length);
            Marshal.FreeHGlobal((IntPtr)arr.Data);
        }
    }

    unsafe struct Bucket<T> where T : unmanaged
    {
        public Arr<T> Array;
        public int Count;
        public Bucket<T>* Next;

        public int  EmptySlots => Array.Length - Count;
        public bool IsEmpty    => Count == 0;

        public static Bucket<T>* Alloc(int size, Bucket<T>* previous = null)
        {
            Bucket<T>* bucket = Heap.AllocStruct<Bucket<T>>();
            Init(bucket, size);

            previous->Next = bucket;

            return bucket;
        }

        public static void Init(Bucket<T>* bucket, int size)
        {
            bucket->Array = Heap.AllocArray<T>(size);
            bucket->Count = 0;
            bucket->Next = null;
        }

        public static void Free(Bucket<T>* bucket, bool recursive)
        {
            do 
            {
                Heap.Free(bucket->Array);
                Heap.Free(bucket);
            } while (recursive && (bucket = bucket->Next) != null);
        }

        public void Add(T element)
        {
            Debug.Assert(Count < Array.Length);
            Array[Count] = element;
            Count++;
        }

        public void ReplaceAt(int i, T replaceWith)
        {
            Debug.Assert(i < Count);
            Array[i] = replaceWith;
        }

        public T RemoveLast()
        {
            Debug.Assert(Count > 0);
            Count--;
            return Array[Count];
        }
    }

    unsafe struct PinnedBucketList<T> where T : unmanaged
    {
        public const int BucketByteSize = 1024;

        public Bucket<T>* Buckets;
        public Bucket<T>* LastBucket;
        public int Count;

        public static int NumElementsInOneBucket => BucketByteSize / sizeof(T);

        public static PinnedBucketList<T>* Alloc(int size)
        {
            PinnedBucketList<T>* list = Heap.AllocStruct<PinnedBucketList<T>>();
            Init(list, size);
            return list;
        }
        
        public static void Init(PinnedBucketList<T>* list, int initialSize)
        {
            Debug.Assert((BucketByteSize / sizeof(T)) == 0);
            list->Buckets = Bucket<T>.Alloc(BucketByteSize / sizeof(T));
            list->LastBucket = list->Buckets;
            list->Count = 0;
        }

        public static void Free(PinnedBucketList<T>* list)
        {
            // Free all buckets, this includes LastBucket
            Bucket<T>.Free(list->Buckets, true);
            Heap.Free(list);
        }

        public void Add(T element)
        {
            // Check if the last bucket has place for this element
            if (LastBucket->EmptySlots <= 0)
            {
                // The last bucket is full, we need to allocate a new bucket
                LastBucket = Bucket<T>.Alloc(NumElementsInOneBucket, LastBucket);
            }
            LastBucket->Add(element);
            Count++;
        }

        public void RemoveUnorderedAt(int i)
        {
            Debug.Assert(i < Count);

            // Find the right bucket
            Bucket<T>* bucket = Buckets;
            while (i > NumElementsInOneBucket && bucket != null)
            {
                i -= NumElementsInOneBucket;
                bucket = bucket->Next;
            }

            // Remove the last element and move it here?
            T last;
            if (LastBucket->IsEmpty == false)
                last = LastBucket->RemoveLast();
            //else while ()

            //bucket->ReplaceAt(i, last);
            Count--;

            // If the last bucket is empty and it's not the 
            if (LastBucket->IsEmpty && bucket != LastBucket)
            {
                // There is an empty bucket at the end
                while (bucket->Next != LastBucket)
                    bucket = bucket->Next;

                LastBucket = bucket;

                // Free and remove the previous last bucket
                Bucket<T>.Free(bucket->Next, false);
                bucket->Next = null;
            }
        }
    }

    unsafe struct PinnedList<T> where T : unmanaged
    {
        public T* Data;
        public int Length;
        public int Count;

        public static PinnedList<T> Init(int initialSize = 16)
        {
            PinnedList<T> list;
            list.Data = (T*)Marshal.AllocHGlobal(initialSize * sizeof(T));
            list.Length = initialSize;
            list.Count = 0;
            return list;
        }

        public void EnsureSize(int size)
        {
            if (size >= Length)
            {
                int newLength = Length + (Length / 2);
                if (newLength < size) newLength = size;
                Data = (T*)Marshal.ReAllocHGlobal((IntPtr)Data, (IntPtr)(newLength * sizeof(T)));
            }
        }

        public void Add(T element)
        {
            Debug.Assert(Data != null);
            
            EnsureSize(Count + 1);
            Data[Count] = element;
        }

        public void RemoveUnorderedAt(int i)
        {
            Debug.Assert(Data != null);
            Debug.Assert(i < Count);
            Debug.Assert(i > 0);

            Data[i] = Data[Count - 1];
            Count--;
        }
    }
}
