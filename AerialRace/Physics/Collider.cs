using BepuPhysics.Collidables;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace AerialRace.Physics
{
    interface IConvexCollider : ICollider
    {
        public IConvexShape ConvexShape { get; }
    }

    interface ICollider
    {
        public IShape Shape { get; }
        public TypedIndex TypedIndex { get; }
        public Vector3 Center { get; }
    }
}
