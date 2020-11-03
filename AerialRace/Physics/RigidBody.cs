using BepuPhysics;
using BepuPhysics.Collidables;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace AerialRace.Physics
{
    class RigidBody
    {
        //public List<> Shapes;

        // FIXME!!
        public BoxCollider Shape;

        public BodyReference Body;

        public RigidBody(BoxCollider shape, Transform transform, float mass)
        {
            Shape = shape;

            shape.Box.ComputeInertia(mass, out var inertia);

            var bodyHandle = Phys.Simulation.Bodies.Add(
                BodyDescription.CreateDynamic(
                        new RigidPose(transform.LocalPosition.ToNumerics(), transform.LocalRotation.ToNumerics()),
                        inertia,
                        new CollidableDescription(shape.BoxShape, 0.1f),
                        new BodyActivityDescription(0.001f)
                    )
                );

            Body = new BodyReference(bodyHandle, Phys.Simulation.Bodies);
        }

        // FIXME: We want to handle scaling in some way.
        // or maybe rigidbodies can't be scaled.
        public void UpdateTransform(Transform transform)
        {
            var pose = Body.Pose;
            transform.LocalPosition = pose.Position.ToOpenTK();
            transform.LocalRotation = pose.Orientation.ToOpenTK();
        }
    }
}
