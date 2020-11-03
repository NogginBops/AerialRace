using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace AerialRace.Physics
{
    class Phys
    {
        public static Simulation Simulation;
        public static BufferPool BufferPool = new BufferPool();

        public static void Init()
        {
            Simulation = Simulation.Create(BufferPool, new NarrowPhaseCallbacks(), new PoseIntegratorCallbacks(new Vector3(0, -9.81f, 0)), new PositionLastTimestepper());
        }

        public static void Test()
        {
            var box = new Box(1, 1, 1);
            box.ComputeInertia(1, out var shpereInertia);
            var b = Simulation.Bodies.Add(
                BodyDescription.CreateDynamic(
                    new Vector3(0, 5, 0),
                    shpereInertia,
                    new CollidableDescription(Simulation.Shapes.Add(box), 0.1f),
                    new BodyActivityDescription(0.01f)
                    )
                );

            Simulation.Statics.Add(
                new StaticDescription(
                    new Vector3(0, 0, 0),
                    new CollidableDescription(
                        Simulation.Shapes.Add(
                            new Box(500, 1, 500)
                            ),
                        0.1f)
                    )
                );
        }

        public static void Update(float dt)
        {
            Simulation.Timestep(dt);
        }

        public static void Dispose()
        {
            Simulation.Dispose();
            BufferPool.Clear();
        }
    }
}
