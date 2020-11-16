using BepuPhysics;
using BepuPhysics.Collidables;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace AerialRace.Physics
{
    class BoxCollider : ICollider
    {
        // FIXME: We want to update the shape and everything if this value is changed
        public Box Box;
        public TypedIndex BoxShape;

        public IConvexShape Shape => Box;
        public TypedIndex TypedIndex => BoxShape;

        public Vector3 Center => Vector3.Zero;

        public BoxCollider(Vector3 size)
        {
            Box = new Box(size.X, size.Y, size.Z);
            BoxShape = Phys.Simulation.Shapes.Add(Box);
        }
    }
}
