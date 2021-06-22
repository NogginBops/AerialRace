using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AerialRace.Entities
{
    struct Renderer
    {
        // FIXME: We might want to do something different with the data buffers here...
        public Mesh Mesh;
        public Material Material;
        public bool CastShadows;
    }
}
