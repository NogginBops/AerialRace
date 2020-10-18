﻿using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace AerialRace
{
    class Camera
    {
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

        public void CalcViewMatrix(out Matrix4 viewMatrix)
        {
            Transform.GetTransformationMatrix(out viewMatrix);
            viewMatrix.Invert();
        }

        public void CalcProjectionMatrix(out Matrix4 projection)
        {
            Matrix4.CreatePerspectiveFieldOfView(
                Fov * ((float)Math.PI / 180f), Aspect, NearPlane, FarPlane, out projection);
        }
    }
}
