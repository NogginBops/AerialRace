using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace AerialRace
{
    public class Transform
    {
        // FIXME: We want a good way to Create and Delete entities
        public static List<Transform> Transforms = new List<Transform>();

        public static void MultMVP(ref Matrix4 model, ref Matrix4 view, ref Matrix4 projection, out Matrix4 mv, out Matrix4 mvp)
        {
            Matrix4.Mult(model, view, out mv);
            Matrix4.Mult(mv, projection, out mvp);
        }

        public string Name = "Default Name";

        public int LinearizedIndex = 0;
        public Vector3 LocalPosition = Vector3.Zero;
        public Vector3 LocalScale = Vector3.One;
        public Quaternion LocalRotation = Quaternion.Identity;

        public Vector3 WorldPosition {
            get => Vector3.TransformPosition(Vector3.Zero, LocalToWorld); //Transformations.MultPosition(LocalPosition, ref LocalToWorld);
            set => LocalPosition = Vector3.TransformPosition(value, WorldToLocal); //= Transformations.MultPosition(value, ref WorldToLocal);
        }

        public Transform? Parent = null;
        public List<Transform>? Children = null;

        public Matrix4 LocalToWorld;
        public Matrix4 WorldToLocal;

        // FIXME: Make WorldForward, and ParentForward different things!
        public Vector3 Forward => Vector3.TransformVector(-Vector3.UnitZ, LocalToWorld);//Transformations.MultDirection(-Vector3.UnitZ, ref LocalToWorld);
        public Vector3 Right   => Vector3.TransformVector( Vector3.UnitX, LocalToWorld);//Transformations.MultDirection( Vector3.UnitX, ref LocalToWorld);
        public Vector3 Up      => Vector3.TransformVector( Vector3.UnitY, LocalToWorld);//Transformations.MultDirection( Vector3.UnitY, ref LocalToWorld);

        public Transform()
        {
            AddTransform(this);
        }

        public Transform(string name) : this()
        {
            Name = name;
        }

        public Transform(Vector3 position) : this()
        {
            LocalPosition = position;
            LocalScale = Vector3.One;
            LocalRotation = Quaternion.Identity;
        }

        public Transform(string name, Vector3 position) : this()
        {
            Name = name;
            LocalPosition = position;
            LocalScale = Vector3.One;
            LocalRotation = Quaternion.Identity;
        }

        public Transform(Vector3 position, Quaternion rotation) : this()
        {
            LocalPosition = position;
            LocalScale = Vector3.One;
            LocalRotation = rotation;
        }

        public Transform(string name, Vector3 position, Quaternion rotation) : this()
        {
            Name = name;
            LocalPosition = position;
            LocalScale = Vector3.One;
            LocalRotation = rotation;
        }

        public Transform(Vector3 position, Quaternion rotation, Vector3 scale) : this()
        {
            LocalPosition = position;
            LocalScale = scale;
            LocalRotation = rotation;
        }

        public Transform(string name, Vector3 position, Quaternion rotation, Vector3 scale) : this()
        {
            Name = name;
            LocalPosition = position;
            LocalScale = scale;
            LocalRotation = rotation;
        }

        public static void AddTransform(Transform transform)
        {
            Transforms.Add(transform);
        }

        public void SetParent(Transform parent)
        {
            if (Parent != null)
                Parent.RemoveChildInternal(this);

            Parent = parent;
            Parent.AddChildInternal(this);
        }

        public void AddChildInternal(Transform child)
        {
            if (Children == null)
                Children = new List<Transform>();

            Children.Add(child);
        }

        public void RemoveChildInternal(Transform child)
        {
            if (Children != null)
                Children.Remove(child);
        }

        public void UpdateMatrices()
        {
            GetTransformationMatrix(out LocalToWorld);
            WorldToLocal = LocalToWorld;
            WorldToLocal.Invert();
        }

        public void GetTransformationMatrix(out Matrix4 matrix)
        {
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

        public Vector3 LocalDirectionToWorld(Vector3 localDir)
        {
            return Vector3.TransformVector(localDir, LocalToWorld);
        }

        public Vector3 WorldDirectionToLocal(Vector3 worldDir)
        {
            return Vector3.TransformVector(worldDir, WorldToLocal);
        }

        public Vector3 LocalPositionToWorld(in Vector3 localPos)
        {
            return Vector3.TransformPosition(localPos, LocalToWorld);
        }

        public Vector3 WorldPositionToLocal(in Vector3 worldPos)
        {
            return Vector3.TransformPosition(worldPos, WorldToLocal);
        }
    }
}
