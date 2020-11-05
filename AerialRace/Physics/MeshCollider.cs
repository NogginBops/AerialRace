using AerialRace.Loading;
using BepuPhysics.Collidables;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AerialRace.Physics
{
    class MeshCollider : ICollider
    {
        public ConvexHull Hull;
        
        public IConvexShape Shape => Hull;
        public TypedIndex TypedIndex { get; private set; }
        public Vector3 Center { get; private set; }

        public Box3 Bounds;

        public MeshCollider(MeshData mesh)
        {
            // FIXME: We might want to make this faster!
            var positions = mesh.Vertices.Select(v => v.Position.ToNumerics()).ToArray();
            Hull = new ConvexHull(positions, Phys.BufferPool, out var center);

            TypedIndex = Phys.Simulation.Shapes.Add(Hull);

            Center = center.ToOpenTK();
            Hull.ComputeBounds(System.Numerics.Quaternion.Identity, out var min, out var max);
            Bounds = new Box3(min.ToOpenTK(), max.ToOpenTK());
        }
    }
}
