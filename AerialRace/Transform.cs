using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace AerialRace
{
    class Transform
    {
        public Vector3 Position;
        public Vector3 Scale;
        public Quaternion Rotation;

        public Transform()
        {
            Position = Vector3.Zero;
            Scale = Vector3.One;
            Rotation = Quaternion.Identity;
        }

        public void GetTransformationMatrix(out Matrix3x4 matrix)
        {
            matrix = default;

            matrix.M14 = Position.X;
            matrix.M24 = Position.Y;
            matrix.M34 = Position.Z;

            // FIXME: Make this more efficient!!!
            Matrix3.CreateFromQuaternion(Rotation, out Matrix3 rotation);

            matrix.Row0.Xyz = rotation.Row0 * Scale.X;
            matrix.Row1.Xyz = rotation.Row1 * Scale.Y;
            matrix.Row2.Xyz = rotation.Row2 * Scale.Z;
        }
    }
}
