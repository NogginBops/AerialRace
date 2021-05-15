using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace AerialRace
{
    struct BitArray128 : IEquatable<BitArray128>
    {
        public const int Slots = 128;

        public ulong Field1, Field2;

        public BitArray128(ulong f1, ulong f2)
        {
            Field1 = f1;
            Field2 = f2;
        }

        public bool this[int index]
        {
            get
            {
                Debug.Assert(index >= 0, $"Index cannot be a negative number: {index}");
                Debug.Assert(index < 128, $"Index cannot be larger than 128: {index}");

                ulong mask = 1u << index;
                ref ulong field = ref (index > 64 ? ref Field2 : ref Field1);
                return (field & mask) == mask;
            }
            set
            {
                Debug.Assert(index >= 0, $"Index cannot be a negative number: {index}");
                Debug.Assert(index < 128, $"Index cannot be larger than 128: {index}");

                uint mask = 1u << index;
                ref ulong field = ref (index > 64 ? ref Field2 : ref Field1);
                if (value)
                {
                    field |= mask;
                }
                else
                {
                    field &= ~mask;
                }
            }
        }

        public void Clear()
        {
            Field1 = 0;
            Field2 = 0;
        }

        public override bool Equals(object? obj)
        {
            return obj is BitArray128 array && Equals(array);
        }

        public bool Equals(BitArray128 other)
        {
            return Field1 == other.Field1 &&
                   Field2 == other.Field2;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Field1, Field2);
        }

        public static BitArray128 operator &(BitArray128 a, BitArray128 b)
        {
            return new BitArray128(a.Field1 & b.Field1, a.Field2 & b.Field2);
        }

        public static BitArray128 operator |(BitArray128 a, BitArray128 b)
        {
            return new BitArray128(a.Field1 & b.Field1, a.Field2 & b.Field2);
        }

        public static bool operator ==(BitArray128 left, BitArray128 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BitArray128 left, BitArray128 right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return $"{Convert.ToString((long)Field2, 2)}{Convert.ToString((long)Field1, 2)}";
        }
    }
}
