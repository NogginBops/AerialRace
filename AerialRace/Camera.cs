using AerialRace.Mathematics;
using AerialRace.RenderData;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace AerialRace
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct CameraUniformData
    {
        public Vector4 Position;
        public float NearPlane;
        public float FarPlane;
        public float FieldOfView;
        public float Aspect;
    }

    enum ProjectionType
    {
        Perspective,
        Orthographic,
    }

    [Serializable]
    class Camera
    {
        public Transform Transform;

        public Color4 ClearColor;

        public ProjectionType ProjectionType;

        public float Fov;
        public Box2 Viewport;
        public float NearPlane;
        public float FarPlane;

        public UniformBuffer<CameraUniformData> UniformData;

        // The vertical orthograpic size
        public float OrthograpicSize;

        public float Aspect => (Viewport.Size.X * Screen.Width) / (Viewport.Size.Y * Screen.Height);

        // Used by the mouse controls for cameras
        // We can remove this later
        public float YAxisRotation, XAxisRotation;

        public Camera(string name, float fov, float near, float far, Color4 clear)
        {
            Transform = new Transform(name);
            ClearColor = clear;
            Fov = fov;
            Viewport = new Box2((0, 0), (1, 1));
            NearPlane = near;
            FarPlane = far;

            UniformData = RenderDataUtil.CreateUniformBuffer<CameraUniformData>(name, BufferFlags.Dynamic);
        }

        public void CalcViewProjection(out Matrix4 vp)
        {
            CalcViewMatrix(out var view);
            CalcProjectionMatrix(out var proj);
            Matrix4.Mult(view, proj, out vp);
        }

        public void CalcViewMatrix(out Matrix4 viewMatrix)
        {
            Transform.GetTransformationMatrix(out viewMatrix);
            viewMatrix.Invert();
        }

        public void CalcProjectionMatrix(out Matrix4 projection)
        {
            switch (ProjectionType)
            {
                case ProjectionType.Perspective:
                    Matrix4.CreatePerspectiveFieldOfView(
                        Fov * (MathF.PI / 180f),
                        Aspect, NearPlane, FarPlane, out projection);
                    break;
                case ProjectionType.Orthographic:
                    Matrix4.CreateOrthographic(
                        OrthograpicSize * Aspect, OrthograpicSize,
                        0, FarPlane, out projection);
                    break;
                default:
                    throw new Exception();
            }
        }

        public void UpdateUniformBuffer()
        {
            GetCameraDataBlock(out var data);
            RenderDataUtil.UpdateUniformBuffer(UniformData, ref data);
        }

        public void GetCameraDataBlock(out CameraUniformData data)
        {
            // FIXME: 1/0 depending on perspective vs ortho?
            data.Position = new Vector4(Transform.WorldPosition, 1);
            data.NearPlane = NearPlane;
            data.FarPlane = FarPlane;
            data.FieldOfView = Fov;
            data.Aspect = Aspect;
        }

        public Ray RayFromPixel(Vector2 pixel, Vector2i resolution)
        {
            Vector3 pixelVec = new Vector3(pixel.X, resolution.Y - pixel.Y, NearPlane);
            CalcViewProjection(out var vp);
            vp.Invert();
            var res = Vector3.Unproject(pixelVec, 0, 0, resolution.X, resolution.Y, NearPlane, FarPlane, vp);
            var pos = Transform.WorldPosition;
            var direction = Vector3.Normalize(res - pos);
            return new Ray(pos, direction);
        }
    }
}
