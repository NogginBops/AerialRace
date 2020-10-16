using System;
using System.Collections.Generic;
using System.Text;

namespace AerialRace.RenderData
{
    enum AttributeType : int
    {
        UInt8  = 1,
        UInt16 = 2,
        UInt32 = 3,

        Int8  = 4,
        Int16 = 5,
        Int32 = 6,

        Half   = 7,
        Float  = 8,
        Double = 9,
    }

    class AttributeSpecification
    {
        public string Name;
        public int Size;
        public AttributeType Type;
        public bool Normalized;

        public AttributeSpecification(string name, int size, AttributeType type, bool normalized)
        {
            Name = name;
            Size = size;
            Type = type;
            Normalized = normalized;
        }
    }
}
