﻿using OpenTK.Mathematics;
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
                    transform.LocalPosition,
                    transform.LocalScale,
                    transform.LocalRotation);
            }
        }

        public static void ResolveTransformations(TransformationData[] transforms, Matrix4[] resolved)
        {
            for (int i = 0; i < transforms.Length; i++)
            {
                ref TransformationData data = ref transforms[i];
                ref Matrix4 matrix = ref resolved[i];

                // FIXME: Make this more efficient!!!
                Matrix3.CreateFromQuaternion(data.Rotation, out Matrix3 rotation);

                var scale = data.Scale;
                matrix.Row0 = new Vector4(rotation.Row0 * scale.X, 0);
                matrix.Row1 = new Vector4(rotation.Row1 * scale.Y, 0);
                matrix.Row2 = new Vector4(rotation.Row2 * scale.Z, 0);
                matrix.Row3 = new Vector4(data.Position, 1);

                if (data.Parent != 0)
                {
                    Debug.Assert(data.Parent < i);

                    ref Matrix4 parent = ref resolved[data.Parent];

                    // Left multiply by the parent matrix
                    Matrix4.Mult(in matrix, in parent, out matrix);
                }
            }
        }

        public static void MultMVP(ref Matrix4 model, ref Matrix4 view, ref Matrix4 projection, out Matrix4 mvp)
        {
            Matrix4.Mult(model, view, out var mv);
            Matrix4.Mult(mv, projection, out mvp);
        }
    }

    class Transform
    {
        public static List<Transform> Roots = new List<Transform>();

        public int LinearizedIndex = 0;
        public Vector3 LocalPosition;
        public Vector3 LocalScale;
        public Quaternion LocalRotation;

        public Vector3 WorldPosition {
            get => Vector3.TransformPosition(LocalPosition, LocalToWorld); //Transformations.MultPosition(LocalPosition, ref LocalToWorld);
            set => LocalPosition = Vector3.TransformPosition(LocalPosition, WorldToLocal); //= Transformations.MultPosition(value, ref WorldToLocal);
        }

        public Transform? Parent = null;
        public List<Transform>? Children = null;

        public Matrix4 LocalToWorld;
        public Matrix4 WorldToLocal;

        //public Vector3 Forward2 => Rotation * -Vector3.UnitZ;
        public Vector3 Forward => Vector3.TransformVector(-Vector3.UnitZ, LocalToWorld);//Transformations.MultDirection(-Vector3.UnitZ, ref LocalToWorld);
        public Vector3 Right   => Vector3.TransformVector( Vector3.UnitX, LocalToWorld);//Transformations.MultDirection( Vector3.UnitX, ref LocalToWorld);
        public Vector3 Up      => Vector3.TransformVector( Vector3.UnitY, LocalToWorld);//Transformations.MultDirection( Vector3.UnitY, ref LocalToWorld);

        public Transform()
        {
            LocalPosition = Vector3.Zero;
            LocalScale = Vector3.One;
            LocalRotation = Quaternion.Identity;
        }

        public Transform(Vector3 position)
        {
            LocalPosition = position;
            LocalScale = Vector3.One;
            LocalRotation = Quaternion.Identity;
        }

        public Transform(Vector3 position, Quaternion rotation)
        {
            LocalPosition = position;
            LocalScale = Vector3.One;
            LocalRotation = rotation;
        }

        public Transform(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            LocalPosition = position;
            LocalScale = scale;
            LocalRotation = rotation;
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

        public void GetTransformationMatrix(out Matrix4 matrix)
        {
            // FIXME: Make this more efficient
            Matrix3.CreateFromQuaternion(LocalRotation, out Matrix3 rotation);

            matrix.Row0 = new Vector4(rotation.Row0 * LocalScale.X, 0);
            matrix.Row1 = new Vector4(rotation.Row1 * LocalScale.Y, 0);
            matrix.Row2 = new Vector4(rotation.Row2 * LocalScale.Z, 0);
            matrix.Row3 = new Vector4(LocalPosition, 1);

            if (Parent != null)
            {
                // FIXME: This can be made more efficient
                Parent.GetTransformationMatrix(out var parentMatrix);

                Matrix4.Mult(in matrix, in parentMatrix, out matrix);
                //Transformations.Mult4x3(ref matrix, ref parentMatrix, out matrix);
            }
        }
    }
}
