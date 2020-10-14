using System;
using System.Collections.Generic;
using System.Text;

namespace AerialRace.RenderData
{
    [Flags]
    enum BufferFlags : int
    {
        None = 0,
        MapRead = 1,
        MapWrite = 2,
        MapPersistent = 3,
    }

    enum BufferDataType : int
    {
        UInt8  = 1,
        UInt16 = 2,
        UInt32 = 3,
        UInt64 = 4,

        Int8   = 5,
        Int16  = 6,
        Int32  = 7,
        Int64  = 8,

        Float  = 9,
        Float2 = 10,
        Float3 = 11,
        Float4 = 12,
    }

    class Buffer
    {
        public string Name;
        public int Handle;
        public BufferDataType DataType;
        public int Elements;

        public Buffer(string name, int handle, BufferDataType dataType, int elements)
        {
            Name = name;
            Handle = handle;
            DataType = dataType;
            Elements = elements;
        }
    }
}
