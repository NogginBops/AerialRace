using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace AerialRace
{
    struct TransformationData
    {
        public int Parent;
        public Vector3 Position;
        public Vector3 Scale;
        public Quaternion Rotation;

        public TransformationData(int parent, Vector3 position, Vector3 scale, Quaternion rotation)
        {
            Parent = parent;
            Position = position;
            Scale = scale;
            Rotation = rotation;
        }
    }

    class Transformations
    {
        public List<Transform> TransformRoots = new List<Transform>();

        public static void LinearizeTransformations(List<Transform> roots, TransformationData[] placed)
        {
            Stack<Transform> stack = new Stack<Transform>();

            for (int i = 0; i < roots.Count; i++)
            {
                stack.Push(roots[i]);
            }

            int index = 0;

            while (stack.Count > 0)
            {
                Transform transform = stack.Pop();

                transform.LinearizedIndex = index;

                index++;

                // Push all of the children to the stack
                for (int i = 0; i < transform.Children?.Count; i++)
                {
                    stack.Push(transform.Children[i]);
                }

                placed[index] = new TransformationData(
                    transform.Parent?.LinearizedIndex ?? 0,
                    transform.Position,
                    transform.Scale,
                    transform.Rotation);
            }
        }

        public static void ResolveTransformations(TransformationData[] transforms, Matrix4x3[] resolved)
        {
            for (int i = 0; i < transforms.Length; i++)
            {
                ref TransformationData data = ref transforms[i];
                ref Matrix4x3 matrix = ref resolved[i];

                // FIXME: Make this more efficient!!!
                Matrix3.CreateFromQuaternion(data.Rotation, out Matrix3 rotation);

                var scale = data.Scale;
                matrix.Row0 = rotation.Row0 * scale.X;
                matrix.Row1 = rotation.Row1 * scale.Y;
                matrix.Row2 = rotation.Row2 * scale.Z;
                matrix.Row3 = data.Position;

                if (data.Parent != 0)
                {
                    Debug.Assert(data.Parent < i);

                    ref Matrix4x3 parent = ref resolved[data.Parent];

                    // Left multiply by the parent matrix
                    Mult4x3(ref parent, ref matrix, out matrix);
                }
            }
        }

        public static void Mult4x3(ref Matrix4x3 left, ref Matrix4x3 right, out Matrix4x3 result)
        {
            Vector4 rightColumn0 = right.Column0;
            Vector4 rightColumn1 = right.Column1;
            Vector4 rightColumn2 = right.Column2;

            Vector3 leftRow0 = left.Row0;
            Vector3 leftRow1 = left.Row1;
            Vector3 leftRow2 = left.Row2;
            Vector3 leftRow3 = left.Row3;

            result.Row0.X = DotImplicit0(leftRow0, rightColumn0);
            result.Row0.Y = DotImplicit0(leftRow0, rightColumn1);
            result.Row0.Z = DotImplicit0(leftRow0, rightColumn2);

            result.Row1.X = DotImplicit0(leftRow1, rightColumn0);
            result.Row1.Y = DotImplicit0(leftRow1, rightColumn1);
            result.Row1.Z = DotImplicit0(leftRow1, rightColumn2);

            result.Row2.X = DotImplicit0(leftRow2, rightColumn0);
            result.Row2.Y = DotImplicit0(leftRow2, rightColumn1);
            result.Row2.Z = DotImplicit0(leftRow2, rightColumn2);

            result.Row3.X = DotImplicit1(leftRow3, rightColumn0);
            result.Row3.Y = DotImplicit1(leftRow3, rightColumn1);
            result.Row3.Z = DotImplicit1(leftRow3, rightColumn2);
        }

        public static void Mult4x3(ref Matrix4x3 left, ref Matrix4 right, out Matrix4 result)
        {
            Vector4 rightColumn0 = right.Column0;
            Vector4 rightColumn1 = right.Column1;
            Vector4 rightColumn2 = right.Column2;
            Vector4 rightColumn3 = right.Column3;

            Vector3 leftRow0 = left.Row0;
            Vector3 leftRow1 = left.Row1;
            Vector3 leftRow2 = left.Row2;
            Vector3 leftRow3 = left.Row3;

            result.Row0.X = DotImplicit0(leftRow0, rightColumn0);
            result.Row0.Y = DotImplicit0(leftRow0, rightColumn1);
            result.Row0.Z = DotImplicit0(leftRow0, rightColumn2);
            result.Row0.W = DotImplicit1(leftRow0, rightColumn3);

            result.Row1.X = DotImplicit0(leftRow1, rightColumn0);
            result.Row1.Y = DotImplicit0(leftRow1, rightColumn1);
            result.Row1.Z = DotImplicit0(leftRow1, rightColumn2);
            result.Row1.W = DotImplicit1(leftRow1, rightColumn3);

            result.Row2.X = DotImplicit0(leftRow2, rightColumn0);
            result.Row2.Y = DotImplicit0(leftRow2, rightColumn1);
            result.Row2.Z = DotImplicit0(leftRow2, rightColumn2);
            result.Row2.W = DotImplicit1(leftRow2, rightColumn3);

            result.Row3.X = DotImplicit1(leftRow3, rightColumn0);
            result.Row3.Y = DotImplicit1(leftRow3, rightColumn1);
            result.Row3.Z = DotImplicit1(leftRow3, rightColumn2);
            result.Row3.W = DotImplicit1(leftRow3, rightColumn3);
        }

        public static void MultMVP(ref Matrix4x3 model, ref Matrix4x3 view, ref Matrix4 projection, out Matrix4 mvp)
        {
            Mult4x3(ref model, ref view, out var mv);
            Mult4x3(ref mv, ref projection, out mvp);
        }

        public static Vector3 MultPosition(Vector3 vec, ref Matrix4x3 matrix)
        {
            MultPosition(ref vec, ref matrix, out Vector3 result);
            return result;
        }

        public static void MultPosition(ref Vector3 vec, ref Matrix4x3 matrix, out Vector3 result)
        {
            result.X = Vector3.Dot(matrix.Row0, vec) + matrix.Row3.X;
            result.Y = Vector3.Dot(matrix.Row1, vec) + matrix.Row3.Y;
            result.Z = Vector3.Dot(matrix.Row2, vec) + matrix.Row3.Z;
        }

        public static Vector3 MultPosition(ref Matrix4x3 matrix, Vector3 vec)
        {
            MultPosition(ref matrix, ref vec, out Vector3 result);
            return result;
        }

        public static void MultPosition(ref Matrix4x3 matrix, ref Vector3 vec, out Vector3 result)
        {
            result.X = DotImplicit1(vec, matrix.Column0);
            result.Y = DotImplicit1(vec, matrix.Column1);
            result.Z = DotImplicit1(vec, matrix.Column2);
        }

        public static Vector3 MultDirection(Vector3 vec, ref Matrix4x3 matrix)
        {
            MultDirection(ref vec, ref matrix, out Vector3 result);
            return result;
        }

        public static void MultDirection(ref Vector3 vec, ref Matrix4x3 matrix, out Vector3 result)
        {
            result.X = Vector3.Dot(matrix.Row0, vec);
            result.Y = Vector3.Dot(matrix.Row1, vec);
            result.Z = Vector3.Dot(matrix.Row2, vec);
        }

        public static Vector3 MultDirection(ref Matrix4x3 matrix, Vector3 vec)
        {
            MultDirection(ref matrix, ref vec, out Vector3 result);
            return result;
        }

        public static void MultDirection(ref Matrix4x3 matrix, ref Vector3 vec, out Vector3 result)
        {
            result.X = DotImplicit0(vec, matrix.Column0);
            result.Y = DotImplicit0(vec, matrix.Column1);
            result.Z = DotImplicit0(vec, matrix.Column2);
        }

        public static float DotImplicit0(Vector3 vec3, Vector4 vec4)
        {
            return vec3.X * vec4.X +
                   vec3.Y * vec4.Y +
                   vec3.Z * vec4.Z;
        }

        public static float DotImplicit1(Vector3 vec3, Vector4 vec4)
        {
            return vec3.X * vec4.X +
                   vec3.Y * vec4.Y +
                   vec3.Z * vec4.Z +
                   vec4.W;
        }
    }

    class Transform
    {
        public static List<Transform> Roots = new List<Transform>();

        public int LinearizedIndex = 0;
        public Vector3 Position;
        public Vector3 Scale;
        public Quaternion Rotation;

        public Transform? Parent = null;
        public List<Transform>? Children = null;

        public Matrix4x3 LocalToWorld;
        public Matrix4x3 WorldToLocal;

        //public Vector3 Forward2 => Rotation * -Vector3.UnitZ;
        public Vector3 Forward => Transformations.MultDirection(ref LocalToWorld, -Vector3.UnitZ);
        public Vector3 Right   => Transformations.MultDirection(ref LocalToWorld,  Vector3.UnitX);
        public Vector3 Up      => Transformations.MultDirection(ref LocalToWorld,  Vector3.UnitY);

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

        public static void AddTransform(Transform transform)
        {
            Roots.Add(transform);
        }

        public void UpdateMatrices()
        {
            GetTransformationMatrix(out LocalToWorld);
            WorldToLocal = LocalToWorld;
            WorldToLocal.Invert();
        }

        public void GetTransformationMatrix(out Matrix4x3 matrix)
        {
            // FIXME: Make this more efficient
            Matrix3.CreateFromQuaternion(Rotation, out Matrix3 rotation);

            matrix.Row0 = rotation.Row0 * Scale.X;
            matrix.Row1 = rotation.Row1 * Scale.Y;
            matrix.Row2 = rotation.Row2 * Scale.Z;
            matrix.Row3 = Position;

            if (Parent != null)
            {
                // FIXME: This can be made more efficient
                Parent.GetTransformationMatrix(out var parentMatrix);

                Transformations.Mult4x3(ref matrix, ref parentMatrix, out matrix);
            }
        }
    }
}
