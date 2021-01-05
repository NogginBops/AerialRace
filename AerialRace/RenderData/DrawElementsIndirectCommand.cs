using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AerialRace.RenderData
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 20)]
    struct DrawElementsIndirectCommand
    {
        public int Count;
        public int InstaceCount;
        public int FirstIndex;
        public int BaseVertex;
        public int BaseInstace;
    }
}
