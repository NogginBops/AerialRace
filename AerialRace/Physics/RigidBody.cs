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
        public ICollider Shape;

        public BodyReference Body;

        public RigidBody(ICollider shape, Transform transform, float mass, SimpleMaterial material, SimpleBody bodyProp)
        {
            Shape = shape;
            Body = Phys.AddDynamicBody(
                new RigidPose(transform.LocalPosition.ToNumerics(), transform.LocalRotation.ToNumerics()),
                shape,
                mass,
                0.1f,
                new BodyActivityDescription(0.001f),
                material,
                bodyProp);
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
