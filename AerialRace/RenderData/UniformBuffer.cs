using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AerialRace.RenderData
{
    class UniformBuffer<T> where T : unmanaged
    {
        public string Name;
        public int Handle;
        public BufferFlags Flags;
        public int SizeInBytes => Unsafe.SizeOf<T>();

        public UniformBuffer(string name, int handle, BufferFlags flags)
        {
            Name = name;
            Handle = handle;
            Flags = flags;
        }
    }
}
