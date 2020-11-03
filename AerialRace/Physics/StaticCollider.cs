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
        public BoxCollider Shape;

        public StaticCollider(BoxCollider shape, Vector3 position)
        {
            Shape = shape;

            Phys.Simulation.Statics.Add(
                new StaticDescription(
                    position.ToNumerics(),
                    shape.BoxShape,
                    0.1f
                ));
        }
    }
}
