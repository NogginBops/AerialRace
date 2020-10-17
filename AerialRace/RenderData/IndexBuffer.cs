using System;
using System.Collections.Generic;
using System.Text;

namespace AerialRace.RenderData
{
    enum IndexBufferType : int
    {
        UInt8 = BufferDataType.UInt8,
        UInt16 = BufferDataType.UInt16,
        UInt32 = BufferDataType.UInt32,
    }

    class IndexBuffer
    {
        public string Name;
        public int Handle;
        public IndexBufferType IndexType;
        public int Elements;
        public BufferFlags Flags;

        public IndexBuffer(string name, int handle, IndexBufferType indexType, int elements, BufferFlags flags)
        {
            Name = name;
            Handle = handle;
            IndexType = indexType;
            Elements = elements;
            Flags = flags;
        }
    }
}
