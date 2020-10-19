using AerialRace.Entities.Components;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace AerialRace.Entities.Systems
{
    class TransformSystem : System
    {
        public override Signature GetSignature(EntityManager manager)
        {
            return CreateSignature<Components.Transform, Components.LocalToWorld>(manager);
        }

        public override void Update(EntityManager manager)
        {
            foreach (var @ref in Entities)
            {
                ref var transform = ref manager.GetComponent<Components.Transform>(@ref);
                ref var localToWorld = ref manager.GetComponent<LocalToWorld>(@ref);

                ref var matrix = ref localToWorld.ToWorld;

                // FIXME: Make this more efficient
                Matrix3.CreateFromQuaternion(transform.LocalRotation, out Matrix3 rotation);

                matrix.Row0 = new Vector4(rotation.Row0 * transform.LocalScale.X, 0);
                matrix.Row1 = new Vector4(rotation.Row1 * transform.LocalScale.Y, 0);
                matrix.Row2 = new Vector4(rotation.Row2 * transform.LocalScale.Z, 0);
                matrix.Row3 = new Vector4(transform.LocalPosition, 1);
            }
        }
    }
}
