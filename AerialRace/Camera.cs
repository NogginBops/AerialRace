using AerialRace.Mathematics;
using AerialRace.RenderData;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace AerialRace
{
    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    struct CameraData
    {
        public Vector3 Pos;
        public float Fov;
        public float Aspect;
        public float NearPlane;
        public float FarPlane;
    }

    class Camera
    {
        public static RenderData.Buffer CameraData;

        static Camera()
        {
            CameraData = RenderDataUtil.CreateDataBuffer<CameraData>("Camera Uniform Data Buffer", 16, BufferFlags.Dynamic);
        }

        public static void UpdateCameraData(Camera camera)
        {

        }

        public Transform Transform;

        public Color4 ClearColor;

        public float Fov;
        public float Aspect;
        public float NearPlane;
        public float FarPlane;


        // Used by the mouse controls for cameras
        // We can remove this later
        public float YAxisRotation, XAxisRotation;

        public Camera(float fov, float aspect, float near, float far, Color4 clear)
        {
            Transform = new Transform();
            ClearColor = clear;
            Fov = fov;
            Aspect = aspect;
            NearPlane = near;
            FarPlane = far;
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
            Matrix4.CreatePerspectiveFieldOfView(
                Fov * (MathF.PI / 180f), Aspect, NearPlane, FarPlane, out projection);
        }

        public void GetCameraDataBlock(out CameraData data)
        {
            data.Pos = Transform.WorldPosition;
            data.Fov = Fov;
            data.Aspect = Aspect;
            data.NearPlane = NearPlane;
            data.FarPlane = FarPlane;
        }

        public Ray RayFromPixel(Vector2 pixel, Vector2i resolution)
        {
            Vector3 pixelVec = new Vector3(pixel.X, pixel.Y, 0f);
            CalcViewProjection(out var vp);
            vp.Invert();
            var res = Vector3.Unproject(pixelVec, 0, 0, resolution.X, resolution.Y, NearPlane, FarPlane, vp);
            var pos = Transform.WorldPosition;
            var direction = Vector3.Normalize(res - pos);
            return new Ray(pos, direction);
        }
    }
}
