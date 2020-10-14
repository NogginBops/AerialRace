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

        public Vector3 Forward => Rotation * -Vector3.UnitZ;
        public Vector3 Right => Rotation * Vector3.UnitX;
        public Vector3 Up => Rotation * Vector3.UnitY;

        public Transform()
        {
            Position = Vector3.Zero;
            Scale = Vector3.One;
            Rotation = Quaternion.Identity;
        }

        public Transform(Vector3 position)
        {
            Position = position;
            Scale = Vector3.One;
            Rotation = Quaternion.Identity;
        }

        public Transform(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Scale = Vector3.One;
            Rotation = rotation;
        }

        public Transform(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            Position = position;
            Scale = scale;
            Rotation = rotation;
        }

        public void GetTransformationMatrix(out Matrix4x3 matrix)
        {
            // FIXME: Make this more efficient!!!
            Matrix3.CreateFromQuaternion(Rotation, out Matrix3 rotation);

            matrix.Row0 = rotation.Row0 * Scale.X;
            matrix.Row1 = rotation.Row1 * Scale.Y;
            matrix.Row2 = rotation.Row2 * Scale.Z;
            matrix.Row3 = Position;
        }
    }
}
