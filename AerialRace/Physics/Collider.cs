using BepuPhysics.Collidables;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace AerialRace.Physics
{
    interface ICollider
    {
        public IConvexShape Shape { get; }
        public TypedIndex TypedIndex { get; }

        public Vector3 Center { get; }
    }
}
