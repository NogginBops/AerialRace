using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AerialRace.Mathematics
{
    public struct FrustumPoints
    {
        public static readonly FrustumPoints NDC = new FrustumPoints()
        {
            Near00 = (-1f, -1f, -1f),
            Near01 = (-1f,  1f, -1f),
            Near10 = ( 1f, -1f, -1f),
            Near11 = ( 1f,  1f, -1f),

            Far00 = (-1f, -1f, 1f),
            Far01 = (-1f,  1f, 1f),
            Far10 = ( 1f, -1f, 1f),
            Far11 = ( 1f,  1f, 1f),
        };

        public Vector3 Near00;
        public Vector3 Near01;
        public Vector3 Near10;
        public Vector3 Near11;

        public Vector3 Far00;
        public Vector3 Far01;
        public Vector3 Far10;
        public Vector3 Far11;

        public static void ApplyProjection(in FrustumPoints points, in Matrix4 projection, out FrustumPoints result)
        {
            // Using the same variable as an 'in' and 'out' parameter is fine
            // because Vector3.TransformPerspective doesn't read the values after it writes them
            Vector3.TransformPerspective(in points.Near00, in projection, out result.Near00);
            Vector3.TransformPerspective(in points.Near01, in projection, out result.Near01);
            Vector3.TransformPerspective(in points.Near10, in projection, out result.Near10);
            Vector3.TransformPerspective(in points.Near11, in projection, out result.Near11);

            Vector3.TransformPerspective(in points.Far00, in projection, out result.Far00);
            Vector3.TransformPerspective(in points.Far01, in projection, out result.Far01);
            Vector3.TransformPerspective(in points.Far10, in projection, out result.Far10);
            Vector3.TransformPerspective(in points.Far11, in projection, out result.Far11);
        }

        public static void ApplyTransform(in FrustumPoints points, in Matrix4 transform, out FrustumPoints result)
        {
            // Using the same variable as an 'in' and 'out' parameter is fine
            // because Vector3.TransformPerspective doesn't read the values after it writes them
            Vector3.TransformPosition(in points.Near00, in transform, out result.Near00);
            Vector3.TransformPosition(in points.Near01, in transform, out result.Near01);
            Vector3.TransformPosition(in points.Near10, in transform, out result.Near10);
            Vector3.TransformPosition(in points.Near11, in transform, out result.Near11);

            Vector3.TransformPosition(in points.Far00, in transform, out result.Far00);
            Vector3.TransformPosition(in points.Far01, in transform, out result.Far01);
            Vector3.TransformPosition(in points.Far10, in transform, out result.Far10);
            Vector3.TransformPosition(in points.Far11, in transform, out result.Far11);
        }

        public static void CalculateAABB(in FrustumPoints points, out Box3 aabb)
        {
            aabb = new Box3(points.Near00, points.Far00);

            aabb.Inflate(points.Near01);
            aabb.Inflate(points.Near10);
            aabb.Inflate(points.Near11);

            aabb.Inflate(points.Far01);
            aabb.Inflate(points.Far10);
            aabb.Inflate(points.Far11);
        }

        public static void FromAABB(in Box3 aabb, out FrustumPoints points)
        {
            var X = (aabb.HalfSize.X, 0, 0);
            var Y = (0, aabb.HalfSize.Y, 0);
            var Z = (0, 0, aabb.HalfSize.Z);

            // NOTE: I have not though about the order of these.
            points.Near00 = aabb.Center + X + Y - Z;
            points.Near01 = aabb.Center + X - Y - Z;
            points.Near10 = aabb.Center - X + Y - Z;
            points.Near11 = aabb.Center - X - Y - Z;

            points.Far00 = aabb.Center + X + Y + Z;
            points.Far01 = aabb.Center + X - Y + Z;
            points.Far10 = aabb.Center - X + Y + Z;
            points.Far11 = aabb.Center - X - Y + Z;
        }
    }

    static class Shadows
    {
        // https://developer.download.nvidia.com/SDK/10.5/opengl/src/cascaded_shadow_maps/doc/cascaded_shadow_maps.pdf
        public static float CalculateZSplit(int i, int N, float near, float far, float λ)
        {
            float t = i / (float)N;
            float normal = near * MathF.Pow((far / near), t);
            float correction = near + t * (far - near);
            return MathHelper.Lerp(normal, correction, λ);
        }

        public static void FitDirectionalLightProjectionToCamera(Camera camera, Vector3 direction, out Matrix4 view, out Matrix4 projection, out Vector3 viewPosition)
        {
            // Figure out all frustum corner points.
            // Make them aligned to the light direction
            // Calculate a AABB of all the frustom points
            //   This AABB gives information used to create a projection
            // Calculate the front center point of the AABB and transform it back to world space (this is the view position)
            //   We might want to offset the view pos even further back...?
            // Use the view position to construct the view matrix

            camera.CalcViewProjection(out var ivp);
            ivp.Invert();

            FrustumPoints cameraFrustum = FrustumPoints.NDC;
            // This projects the NDC coordinates into world space
            FrustumPoints.ApplyProjection(in cameraFrustum, in ivp, out cameraFrustum);

            var up = Vector3.Dot(direction, Vector3.UnitY) > 0.99f ? Vector3.UnitX : Vector3.UnitY;
            var directionView = Matrix4.LookAt(Vector3.Zero, direction, up);
            FrustumPoints.ApplyTransform(in cameraFrustum, in directionView, out var AABBPoints);
            FrustumPoints.CalculateAABB(in AABBPoints, out var AABB);

            var size = AABB.Size;

            Matrix4.CreateOrthographic(size.X, size.Y, 0, size.Z, out projection);

            var distance = -AABB.HalfSize.Z;
            viewPosition = direction * distance;

            view = Matrix4.LookAt(viewPosition, viewPosition + direction, up);
        }

        public static void FitDirectionalLightProjectionToCamera(Camera camera, Vector3 direction, float minDistance, float maxDistance, out Matrix4 view, out Matrix4 projection, out Vector3 viewPosition)
        {
            // Figure out all frustum corner points.
            // Make them aligned to the light direction
            // Calculate a AABB of all the frustom points
            //   This AABB gives information used to create a projection
            // Calculate the front center point of the AABB and transform it back to world space (this is the view position)
            //   We might want to offset the view pos even further back...?
            // Use the view position to construct the view matrix

            camera.CalcViewProjection(out var ivp);
            ivp.Invert();

            var minNDC = Util.LinearDepthToNDC(minDistance, camera.NearPlane, camera.FarPlane);
            var maxNDC = Util.LinearDepthToNDC(maxDistance, camera.NearPlane, camera.FarPlane);

            FrustumPoints cameraFrustum = FrustumPoints.NDC;

            cameraFrustum.Near00.Z = minNDC;
            cameraFrustum.Near01.Z = minNDC;
            cameraFrustum.Near10.Z = minNDC;
            cameraFrustum.Near11.Z = minNDC;

            cameraFrustum.Far00.Z = maxNDC;
            cameraFrustum.Far01.Z = maxNDC;
            cameraFrustum.Far10.Z = maxNDC;
            cameraFrustum.Far11.Z = maxNDC;

            // This projects the NDC coordinates into world space
            FrustumPoints.ApplyProjection(in cameraFrustum, in ivp, out cameraFrustum);

            //Debugging.DebugHelper.FrustumPoints(Editor.Gizmos.GizmoDrawList, cameraFrustum,
            //    Color4.Yellow, Color4.YellowGreen);

            var up = Vector3.Dot(direction, Vector3.UnitY) > 0.99f ? Vector3.UnitX : Vector3.UnitY;

            // Rotate the points so that the light direction is along negative Z.
            // light = world
            // Z    = -direction
            // X    = cross(direction, up)
            // Y    = cross(Z, -X);
            var z = -direction;
            var x = Vector3.Cross(direction, up).Normalized();
            var y = Vector3.Cross(z, x).Normalized();
            var directionView = new Matrix4(
                new Vector4(x, 0),
                new Vector4(-y, 0),
                new Vector4(z, 0),
                new Vector4(0, 0, 0, 1)
                );
            directionView.Invert();
            //var directionView = Matrix4.LookAt(Vector3.Zero, -direction, up);
            //directionView = Matrix4.Identity;
            FrustumPoints.ApplyTransform(in cameraFrustum, in directionView, out var AABBPoints);
            FrustumPoints.CalculateAABB(in AABBPoints, out var AABB);
            /*
            Debugging.DebugHelper.FrustumPoints(Editor.Gizmos.GizmoDrawList, AABBPoints,
                Color4.Cyan, Color4.Cyan);

            FrustumPoints.FromAABB(AABB, out var AABBResult);
            Debugging.DebugHelper.FrustumPoints(Editor.Gizmos.GizmoDrawList, AABBResult,
                Color4.Blue, Color4.Blue);
            */
            Matrix4.CreateOrthographic(AABB.Size.X, AABB.Size.Y, 0, AABB.Size.Z, out projection);

            var distance = -AABB.HalfSize.Z;
            var invDirectionView = directionView.Inverted();
            viewPosition = Vector3.TransformPosition(AABB.Center, invDirectionView) + direction * distance;

            //Debugging.DebugHelper.Line(Editor.Gizmos.GizmoDrawList, Vector3.TransformPosition(AABB.Center, invDirectionView), viewPosition, Color4.Lime, Color4.Yellow);

            view = Matrix4.LookAt(viewPosition, viewPosition + direction, up);

            var lightSpace = view * projection;
            lightSpace.Invert();
            FrustumPoints.ApplyProjection(FrustumPoints.NDC, lightSpace, out var test);

            //Debugging.DebugHelper.FrustumPoints(Editor.Gizmos.GizmoDrawList, test,
            //    Color4.Magenta, Color4.Magenta);
        }
    }
}
