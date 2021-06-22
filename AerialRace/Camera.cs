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

    struct CameraFrustumCullingData
    {
        public FrustumPoints Points;
        public FrustumPlanes Planes;

        public Vector3 Position;
        public Vector3 Forward;
        public Vector3 Up;

        public float NearPlane, FarPlane;
        public float TanHalfVFov;
        public float Aspect;

        public static CameraFrustumCullingData FromCamera(Camera camera)
        {
            CameraFrustumCullingData data;

            FrustumPoints ndc = FrustumPoints.NDC;
            camera.CalcViewProjection(out var ivp);
            ivp.Invert();

            FrustumPoints.ApplyProjection(in ndc, in ivp, out data.Points);
            data.Planes = new FrustumPlanes(data.Points);

            data.Position = camera.Transform.WorldPosition;
            data.Forward = camera.Transform.Forward;
            data.Up = camera.Transform.Up;
            data.NearPlane = camera.NearPlane;
            data.FarPlane = camera.FarPlane;
            data.TanHalfVFov = MathF.Tan(camera.VerticalFov * 0.5f);
            data.Aspect = camera.Aspect;

            return data;
        }
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

        public float VerticalFov;
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

        public Camera(string name, float verticalFov, float near, float far, Color4 clear)
        {
            Transform = new Transform(name);
            ClearColor = clear;
            VerticalFov = verticalFov;
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
                        VerticalFov * Util.D2R,
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
            data.FieldOfView = VerticalFov;
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
