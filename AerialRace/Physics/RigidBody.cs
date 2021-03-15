using BepuPhysics;
using BepuPhysics.Collidables;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace AerialRace.Physics
{
    // FIXME: We want to move away from SelfCollections!!
    class RigidBody : SelfCollection<RigidBody>
    {
        //public List<> Shapes;

        // FIXME!!
        public IConvexCollider Shape;

        public BodyReference Body;

        public float Mass;

        // FIXME: Should we do interpolation or extrapolation??
        public RigidPose PreviousPose;

        public RigidBody(IConvexCollider shape, Transform transform, float mass, SimpleMaterial material, SimpleBody bodyProp)
        {
            Shape = shape;
            Body = Phys.AddDynamicBody(
                new RigidPose(transform.LocalPosition.AsNumerics(), transform.LocalRotation.AsNumerics()),
                shape,
                mass,
                0.1f,
                new BodyActivityDescription(0.001f),
                material,
                bodyProp);
            Mass = mass;
        }

        public void UpdatePreviousState()
        {
            PreviousPose = Body.Pose;
        }

        // FIXME: We want to handle scaling in some way.
        // or maybe rigidbodies can't be scaled.
        public void UpdateTransform(Transform transform)
        {
            var pose = Body.Pose;
            transform.LocalRotation = Quaternion.Slerp(PreviousPose.Orientation.AsOpenTK(), pose.Orientation.AsOpenTK(), Phys.Alpha);
            transform.LocalPosition = Vector3.Lerp(PreviousPose.Position.AsOpenTK(), pose.Position.AsOpenTK(), Phys.Alpha);

            //transform.LocalRotation = pose.Orientation.AsOpenTK();
            //transform.LocalPosition = pose.Position.AsOpenTK() - transform.LocalRotation * Shape.Center;
        }
    }
}
