using System;
using System.Collections.Generic;
using System.Text;

namespace AerialRace.Entities
{
    struct ModelEntity : IComponent
    {
        public Transform Transform;
        public Mesh Mesh;
    }
}
