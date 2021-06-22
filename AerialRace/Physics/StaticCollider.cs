using BepuPhysics;
using BepuPhysics.Collidables;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace AerialRace.Physics
{
    class StaticCollider
    {
        public ICollider Shape;
        public StaticReference Static;

        public StaticCollider(ICollider shape, Vector3 position, SimpleMaterial material)
        {
            Shape = shape;
            Static = Phys.AddStaticBody(position + shape.Center, shape, 0.1f, material);
        }
    }
}
