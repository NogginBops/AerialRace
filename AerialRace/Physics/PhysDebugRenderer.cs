using AerialRace.Debugging;
using BepuPhysics;
using BepuPhysics.Collidables;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AerialRace.Physics
{
    static class PhysDebugRenderer
    {
        public static DrawList Drawlist = new DrawList();

        public static void RenderColliders()
        {
            Drawlist.Clear();

            var simulation = Phys.Simulation;
            var bodies = simulation.Bodies;
            for (int i = 0; i < bodies.Sets.Length; i++)
            {
                ref var set = ref bodies.Sets[i];
                if (set.Allocated)
                {
                    for (int bodyIndex = 0; bodyIndex < set.Count; bodyIndex++)
                    {
                        RenderBody(simulation.Shapes, bodies, i, bodyIndex);
                    }
                }
            }
        }

        public static void RenderBody(Shapes shapes, Bodies bodies, int setIndex, int bodyIndex)
        {
            ref var set = ref bodies.Sets[setIndex];
            var handle = set.IndexToHandle[bodyIndex];
            
            ref BodyDynamics state = ref set.DynamicsState[bodyIndex];

            ref var activity = ref set.Activity[bodyIndex];
            ref var inertia = ref state.Inertia.Local;

            Color4<Rgba> color;
            if (Bodies.IsKinematic(inertia))
            {
                color = new Color4<Rgba>(0f, 0.609f, 0.37f, 1f);
            }
            else
            {
                color = new Color4<Rgba>(0.8f, 0.1f, 0.566f, 1f);
            }

            if (bodyIndex == 0)
            {
                if (activity.SleepCandidate)
                {
                    color = new Color4<Rgba>(color.X * 0.35f, color.Y * 0.35f, color.Z * 0.7f, 1f);
                }
            }
            else
            {
                color = new Color4<Rgba>(color.X * 0.2f, color.Y * 0.2f, color.Z * 0.4f, 1f);
            }

            RenderShape(shapes, set.Collidables[bodyIndex].Shape, ref state.Motion.Pose, color);
        }

        public static unsafe void RenderShape(Shapes shapes, TypedIndex shapeIndex, ref RigidPose pose, Color4<Rgba> color)
        {
            if (shapeIndex.Exists)
            {
                shapes[shapeIndex.Type].GetShapeData(shapeIndex.Index, out var data, out _);
                RenderShape(data, shapeIndex.Type, shapes, ref pose, color);
            }
        }

        public static unsafe void RenderShape(void* data, int shapeType, Shapes shapes, ref RigidPose pose, Color4<Rgba> color)
        {

            switch (shapeType)
            {
                case Sphere.Id:
                    {
                        // FIXME: Render sphere!
                        DebugHelper.OutlineCircle(
                            Drawlist,
                            pose.Position.AsOpenTK().Xy,
                            Unsafe.AsRef<Sphere>(data).Radius,
                            color,
                            20);
                    }
                    break;
                case Box.Id:
                    {
                        ref var box = ref Unsafe.AsRef<Box>(data);
                        DebugHelper.OutlineBox(
                            Drawlist,
                            pose.Position.AsOpenTK(),
                            pose.Orientation.AsOpenTK(),
                            new Vector3(box.HalfWidth, box.HalfHeight, box.HalfLength),
                            color);
                    }
                    break;
                case ConvexHull.Id:

                    break;
                case BepuPhysics.Collidables.Mesh.Id:

                    break;
                default:
                    throw new Exception($"Unknown shape type {shapeType}.");
            }
        }
    }
}
