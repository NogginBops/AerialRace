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

        // The 00 to 11 map to UV coordinates of the plane
        // 00 is lower left
        // 01 is upper left
        // 10 is lower right
        // 11 is upper right

        public Vector3 Near00;
        public Vector3 Near01;
        public Vector3 Near10;
        public Vector3 Near11;

        public Vector3 Far00;
        public Vector3 Far01;
        public Vector3 Far10;
        public Vector3 Far11;

        public Vector3 AveragePoint
        {
            get
            {
                Vector3 average = default;

                average += Near00;
                average += Near01;
                average += Near10;
                average += Near11;

                average += Far00;
                average += Far01;
                average += Far10;
                average += Far11;

                return average / 8;
            }
        }

        public FrustumPoints(Box3 box)
        {
            var min = box.Min;
            var max = box.Max;

            // FIXME: Maybe flip Z min/max here??
            Near00 = new Vector3(min.X, min.Y, min.Z);
            Near01 = new Vector3(min.X, max.Y, min.Z);
            Near10 = new Vector3(max.X, min.Y, min.Z);
            Near11 = new Vector3(max.X, max.Y, min.Z);
            Far00 = new Vector3(min.X, min.Y, max.Z);
            Far01 = new Vector3(min.X, max.Y, max.Z);
            Far10 = new Vector3(max.X, min.Y, max.Z);
            Far11 = new Vector3(max.X, max.Y, max.Z);
        }

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

        [Obsolete]
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

    public struct FrustumPlanes
    {
        public Plane Left, Right;
        public Plane Top, Bottom;
        public Plane Near, Far;
        
        public FrustumPlanes(in FrustumPoints points)
        {
            Vector3 lrUp = points.Near11 - points.Near10;
            Vector3 lrForward = points.Far10 - points.Near10;
            Vector3 lrLeft = points.Near00 - points.Near10;

            Vector3 flDown = points.Far00 - points.Far01;
            Vector3 flBack = points.Near01 - points.Far01;
            Vector3 flRight = points.Far11 - points.Far01;

            Near = Plane.FromPositionAndNormal(points.Near10, Vector3.Cross(lrUp, -lrLeft).Normalized());
            Right = Plane.FromPositionAndNormal(points.Near10, Vector3.Cross(lrForward, -lrUp).Normalized());
            Bottom = Plane.FromPositionAndNormal(points.Near10, Vector3.Cross(lrLeft, -lrForward).Normalized());

            Far = Plane.FromPositionAndNormal(points.Far01, Vector3.Cross(flDown, flRight).Normalized());
            Left = Plane.FromPositionAndNormal(points.Far01, Vector3.Cross(flBack, flDown).Normalized());
            Top = Plane.FromPositionAndNormal(points.Far01, Vector3.Cross(flRight, flBack).Normalized());
        }
    }

    struct ShadowCaster
    {
        public Box3 AABB;
        public int CulledMask;
    }

    ref struct CascadedShadowContext
    {
        public const int Cascades = 4;
        public Camera Camera;
        public Vector2i ShadowMapResolution;
        public Vector3 Direction;
        public Span<ShadowCaster> ShadowCasters;
        public float CorrectionFactor;

        public Span<float> Splits;
        public Span<Vector3> LightPositions;
        public Span<Matrix4> LightViews;
        public Span<Matrix4> LightProjs;
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

        public static FrustumPoints SliceFrustum(Camera camera, float minDistance, float maxDistance)
        {
            camera.CalcViewProjection(out var ivp);
            ivp.Invert();

            var minNDC = Util.LinearDepthToNDC(minDistance, camera.NearPlane, camera.FarPlane);
            var maxNDC = Util.LinearDepthToNDC(maxDistance, camera.NearPlane, camera.FarPlane);

            FrustumPoints frustum = FrustumPoints.NDC;

            frustum.Near00.Z = minNDC;
            frustum.Near01.Z = minNDC;
            frustum.Near10.Z = minNDC;
            frustum.Near11.Z = minNDC;

            frustum.Far00.Z = maxNDC;
            frustum.Far01.Z = maxNDC;
            frustum.Far10.Z = maxNDC;
            frustum.Far11.Z = maxNDC;

            // This projects the NDC coordinates into world space
            FrustumPoints.ApplyProjection(in frustum, in ivp, out frustum);

            return frustum;
        }

        public static void CalculateCascades(ref CascadedShadowContext context)
        {
            var splits = context.Splits;
            for (int i = 0; i < splits.Length; i++)
            {
                splits[i] = CalculateZSplit(i + 1, splits.Length, context.Camera.NearPlane, context.Camera.FarPlane, context.CorrectionFactor);
            }

            var nearPlane = context.Camera.NearPlane;
            FitDirectionalLightProjectionToCamera(ref context, 0, nearPlane, splits[0]);
            FitDirectionalLightProjectionToCamera(ref context, 1, splits[0], splits[1]);
            FitDirectionalLightProjectionToCamera(ref context, 2, splits[1], splits[2]);
            FitDirectionalLightProjectionToCamera(ref context, 3, splits[2], splits[3]);
        }

        public static void FitDirectionalLightProjectionToCamera(ref CascadedShadowContext context, int cascadeIndex, float minDistance, float maxDistance)
        {
            // TODO: Figure out why when there are shadow casters
            // cascades 1, 2, & 3 render a lot of stuff that they shouldn't.
            // Not sure why it's onl 1, 2, & 3 but that is what what happens.

            //int debugIndex = Window.DebugCascadeSelection;

            // Figure out all frustum corner points.
            // Make them aligned to the light direction
            // Calculate a AABB of all the frustom points
            //   This AABB gives information used to create a projection
            // Calculate the front center point of the AABB and transform it back to world space (this is the view position)
            //   We might want to offset the view pos even further back...?
            // Use the view position to construct the view matrix

            var camera = context.Camera;

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

            /*if (cascadeIndex == debugIndex)
            {
                Debugging.DebugHelper.FrustumPoints(Editor.Gizmos.GizmoDrawList, cameraFrustum,
                    Color4.Cyan, Color4.Blue);
            }*/

            var up = Vector3.Dot(context.Direction, Vector3.UnitY) > 0.99f ? Vector3.UnitX : Vector3.UnitY;

            // Rotate the points so that the light direction is along negative Z.
            // light = world
            // Z    = -direction
            // X    = cross(direction, up)
            // Y    = cross(Z, -X);
            var z = -context.Direction;
            var x = Vector3.Cross(context.Direction, up).Normalized();
            var y = Vector3.Cross(z, x).Normalized();
            var lightToWorldSpace = new Matrix4(
                new Vector4(x, 0),
                new Vector4(y, 0),
                new Vector4(z, 0),
                new Vector4(0, 0, 0, 1)
                );
            var worldToLightSpace = lightToWorldSpace.Inverted();
            FrustumPoints.ApplyTransform(in cameraFrustum, in worldToLightSpace, out var AABBPoints);
            FrustumPoints.CalculateAABB(in AABBPoints, out var frustumAABB);

            // Ok, so now we have a AABB of the camera furstum in light space.
            // Now we want to extend the AABB in the Z direction to include all
            // shadowcasters that could cast shadows in this direction.

            /*
            if (cascadeIndex == debugIndex)
            {
                Debugging.DebugHelper.FrustumPoints(Editor.Gizmos.GizmoDrawList,
                    AABBPoints, Color4.Red, Color4.Orange);

                Debugging.DebugHelper.FrustumPoints(Editor.Gizmos.GizmoDrawList,
                    new FrustumPoints(frustumAABB), Color4.Red, Color4.Orange);

                //var a = new FrustumPoints(frustumAABB);
                //a = cameraFrustum;
                //var boxb = frustumAABB.Translated((0, 10, 0));
                //FrustumPoints.FromAABB(boxb, out var b);

                Debugging.DebugHelper.Line(Editor.Gizmos.GizmoDrawList, Vector3.Zero, Vector3.UnitX * 10, Color4.White, Color4.Red);
                Debugging.DebugHelper.Line(Editor.Gizmos.GizmoDrawList, Vector3.Zero, Vector3.UnitY * 10, Color4.White, Color4.Green);
                Debugging.DebugHelper.Line(Editor.Gizmos.GizmoDrawList, Vector3.Zero, Vector3.UnitZ * 10, Color4.White, Color4.Blue);

                /*
                Matrix3 ident = Matrix3.Identity;
                Debugging.DebugHelper.Cube(Editor.Gizmos.GizmoDrawList, a.Near00, new Vector3(1f), ident, Color4.Red);
                Debugging.DebugHelper.Cube(Editor.Gizmos.GizmoDrawList, a.Near01, new Vector3(1f), ident, Color4.Green);
                Debugging.DebugHelper.Cube(Editor.Gizmos.GizmoDrawList, a.Near10, new Vector3(1f), ident, Color4.Blue);
                Debugging.DebugHelper.Cube(Editor.Gizmos.GizmoDrawList, a.Near11, new Vector3(1f), ident, Color4.Yellow);

                Debugging.DebugHelper.Cube(Editor.Gizmos.GizmoDrawList, a.Far00, new Vector3(1f), ident, Color4.Red);
                Debugging.DebugHelper.Cube(Editor.Gizmos.GizmoDrawList, a.Far01, new Vector3(1f), ident, Color4.Green);
                Debugging.DebugHelper.Cube(Editor.Gizmos.GizmoDrawList, a.Far10, new Vector3(1f), ident, Color4.Blue);
                Debugging.DebugHelper.Cube(Editor.Gizmos.GizmoDrawList, a.Far11, new Vector3(1f), ident, Color4.Yellow);
                /

                /*Debugging.DebugHelper.FrustumPoints(Editor.Gizmos.GizmoDrawList,
                    a, Color4.Cyan, Color4.Red);
                Debugging.DebugHelper.FrustumPoints(Editor.Gizmos.GizmoDrawList,
                    b, Color4.Magenta, Color4.Green);/


                FrustumPlanes planes = new FrustumPlanes(new FrustumPoints(frustumAABB));
                Vector3 average = frustumAABB.Center;
                Vector2 halfSize = (10, 10);

                Debugging.DebugHelper.Line(Editor.Gizmos.GizmoDrawList, average, planes.Near.Normal * 10, Color4.Red, Color4.Red);
                Debugging.DebugHelper.Line(Editor.Gizmos.GizmoDrawList, average, planes.Far.Normal * 10, Color4.Green, Color4.Green);
                Debugging.DebugHelper.Line(Editor.Gizmos.GizmoDrawList, average, planes.Left.Normal * 10, Color4.Blue, Color4.Blue);
                Debugging.DebugHelper.Line(Editor.Gizmos.GizmoDrawList, average, planes.Right.Normal * 10, Color4.Yellow, Color4.Yellow);
                Debugging.DebugHelper.Line(Editor.Gizmos.GizmoDrawList, average, planes.Top.Normal * 10, Color4.Magenta, Color4.Magenta);
                Debugging.DebugHelper.Line(Editor.Gizmos.GizmoDrawList, average, planes.Bottom.Normal * 10, Color4.Cyan, Color4.Cyan);
                /*
                Debugging.DebugHelper.Plane(Editor.Gizmos.GizmoDrawList, planes.Near, average, halfSize, Color4.Red);
                Debugging.DebugHelper.Plane(Editor.Gizmos.GizmoDrawList, planes.Far, average, halfSize, Color4.Green);
                Debugging.DebugHelper.Plane(Editor.Gizmos.GizmoDrawList, planes.Left, average, halfSize, Color4.Blue);
                Debugging.DebugHelper.Plane(Editor.Gizmos.GizmoDrawList, planes.Right, average, halfSize, Color4.Yellow);
                Debugging.DebugHelper.Plane(Editor.Gizmos.GizmoDrawList, planes.Top, average, halfSize, Color4.Magenta);
                Debugging.DebugHelper.Plane(Editor.Gizmos.GizmoDrawList, planes.Bottom, average, halfSize, Color4.Cyan);
                /
            }
            */

            //int notCulled = context.ShadowCasters.Length;

            // Because -Z is forward we want the biggest Z value for shadow casters
            float newMaxZ = frustumAABB.Max.Z;
            for (int i = 0; i < context.ShadowCasters.Length; i++)
            {
                // Get the shadowcaster bounds points in world space
                var points = new FrustumPoints(context.ShadowCasters[i].AABB);
                // Transforms the world space points into light space
                FrustumPoints.ApplyTransform(in points, in worldToLightSpace, out var lightSpacePoints);

                Plane left = new Plane((-1, 0, 0), -frustumAABB.Center.X - frustumAABB.HalfSize.X);
                Plane right = new Plane((1, 0, 0), frustumAABB.Center.X - frustumAABB.HalfSize.X);
                Plane top = new Plane((0, -1, 0), -(frustumAABB.Center.Y + frustumAABB.HalfSize.Y));
                Plane bottom = new Plane((0, 1, 0), frustumAABB.Center.Y - frustumAABB.HalfSize.Y);

                bool aboveLeft = IsFrustumAbovePlane(left, lightSpacePoints);
                bool aboveRight = IsFrustumAbovePlane(right, lightSpacePoints);
                bool aboveTop = IsFrustumAbovePlane(top, lightSpacePoints);
                bool aboveBottom = IsFrustumAbovePlane(bottom, lightSpacePoints);

                if (aboveLeft && aboveRight && aboveTop && aboveBottom)
                {
                    newMaxZ = MathF.Max(lightSpacePoints.Near00.Z, newMaxZ);
                    newMaxZ = MathF.Max(lightSpacePoints.Near01.Z, newMaxZ);
                    newMaxZ = MathF.Max(lightSpacePoints.Near10.Z, newMaxZ);
                    newMaxZ = MathF.Max(lightSpacePoints.Near11.Z, newMaxZ);

                    newMaxZ = MathF.Max(lightSpacePoints.Far00.Z, newMaxZ);
                    newMaxZ = MathF.Max(lightSpacePoints.Far01.Z, newMaxZ);
                    newMaxZ = MathF.Max(lightSpacePoints.Far10.Z, newMaxZ);
                    newMaxZ = MathF.Max(lightSpacePoints.Far11.Z, newMaxZ);
                    
                    /*if (cascadeIndex == debugIndex)
                    {
                        Color4 yellow = Color4.Red.WithAlpha(0.5f);
                        Debugging.DebugHelper.FrustumPoints(Debugging.Debug.DepthTestList, lightSpacePoints, yellow, yellow);
                    }*/
                }
                else
                {
                    context.ShadowCasters[i].CulledMask |= 1 << cascadeIndex;
                    //notCulled--;
                }

                /*
                if (cascadeIndex == debugIndex)
                {
                    if (i == 0)
                    {
                        Vector2 size = (10, 10);
                        Debugging.DebugHelper.Plane(Editor.Gizmos.GizmoDrawList, left, frustumAABB.Center, size, Color4.Blue);
                        Debugging.DebugHelper.Plane(Editor.Gizmos.GizmoDrawList, right, frustumAABB.Center, size, Color4.Yellow);
                        Debugging.DebugHelper.Plane(Debugging.Debug.DepthTestList, top, frustumAABB.Center, size, Color4.Magenta);
                        Debugging.DebugHelper.Plane(Editor.Gizmos.GizmoDrawList, bottom, frustumAABB.Center, size, Color4.Cyan);
                    }
                    /*
                    if (aboveLeft)
                    {
                        Color4 color = Color4.Blue.WithAlpha(0.25f);
                        Debugging.DebugHelper.FrustumPoints(Debugging.Debug.DepthTestList, lightSpacePoints,
                                                    color, color);
                    }
                    
                    if (aboveRight)
                    {
                        Color4 color = Color4.Yellow.WithAlpha(0.25f);
                        Debugging.DebugHelper.FrustumPoints(Debugging.Debug.DepthTestList, lightSpacePoints,
                                                    color, color);
                    }

                    if (aboveTop)
                    {
                        Color4 color = Color4.Magenta.WithAlpha(0.25f);
                        Debugging.DebugHelper.FrustumPoints(Debugging.Debug.DepthTestList, lightSpacePoints,
                                                    color, color);
                    }

                    if (aboveBottom)
                    {
                        Color4 color = Color4.Cyan.WithAlpha(0.25f);
                        Debugging.DebugHelper.FrustumPoints(Debugging.Debug.DepthTestList, lightSpacePoints,
                                                    color, color);
                    }
                    */
                    /*
                    */
                    /*if (intersected)
                    {
                        //Debugging.DebugHelper.FrustumPoints(Debugging.Debug.DepthTestList, newPoints,
                        //                            Color4.Magenta, Color4.Magenta);
                    }*/
                    /*
                    if (aboveLeft && aboveRight && aboveTop && aboveBottom)
                    {
                        Color4 color = Color4.White.WithAlpha(0.5f);
                        Debugging.DebugHelper.FrustumPoints(Debugging.Debug.DepthTestList, lightSpacePoints,
                                                    color, color);
                    }
                    else
                    {
                        Color4 yellow = Color4.Red.WithAlpha(0.5f);
                        Debugging.DebugHelper.FrustumPoints(Debugging.Debug.DepthTestList, lightSpacePoints, yellow, yellow);
                    }
                    /
                    {
                        //Debugging.DebugHelper.Quad(Editor.Gizmos.GizmoDrawList,
                        //    );
                    }
                    
                }
                */
            }

            /*
            if (cascadeIndex == debugIndex)
            {
                Debugging.Debug.WriteLine($"Not culled {notCulled}");
            }
            */

            // FIXME: Calculate min Z from Shadow receivers
            //if (frustumAABB.Min.Z > newMaxZ) frustumAABB.Min = new Vector3(frustumAABB.Min.X, frustumAABB.Min.Y, newMaxZ);
            if (frustumAABB.Max.Z < newMaxZ) frustumAABB.Max = new Vector3(frustumAABB.Max.X, frustumAABB.Max.Y, newMaxZ);

            float worldUnitsPerTexel = (maxDistance - minDistance) / context.ShadowMapResolution.X;

            frustumAABB.Min = Util.Floor(frustumAABB.Min / worldUnitsPerTexel) * worldUnitsPerTexel;
            frustumAABB.Max = Util.Ceil(frustumAABB.Max / worldUnitsPerTexel) * worldUnitsPerTexel;

            Matrix4.CreateOrthographic(frustumAABB.Size.X, frustumAABB.Size.Y, 0, frustumAABB.Size.Z, out context.LightProjs[cascadeIndex]);

            var distance = -frustumAABB.HalfSize.Z;
            var viewPosition = Vector3.TransformPosition(frustumAABB.Center, lightToWorldSpace) + context.Direction * distance;

            context.LightPositions[cascadeIndex] = viewPosition;

            var view = Matrix4.LookAt(viewPosition, viewPosition + context.Direction, up);
            context.LightViews[cascadeIndex] = view;

            /*
            if (cascadeIndex == debugIndex)
            {
                Debugging.DebugHelper.FrustumPoints(Editor.Gizmos.GizmoDrawList,
                    new FrustumPoints(frustumAABB), Color4.Orange, Color4.Green);

                var view2 = Matrix4.LookAt(viewPosition, viewPosition + Vector3.UnitX, up);
                var lightSpace = view * context.LightProjs[cascadeIndex];
                lightSpace.Invert();
                FrustumPoints.ApplyProjection(FrustumPoints.NDC, lightSpace, out var test);

                //Debugging.DebugHelper.FrustumPoints(Editor.Gizmos.GizmoDrawList, test,
                //    Color4.Cyan, Color4.Blue);
            }
            */
        }
        
        public static void FitDirectionalLightProjectionToCamera(Camera camera, Vector3 direction, float minDistance, float maxDistance, Span<Box3> shadowCasters, out Matrix4 view, out Matrix4 projection, out Vector3 viewPosition)
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
            //   Color4.Yellow, Color4.YellowGreen);

            var up = Vector3.Dot(direction, Vector3.UnitY) > 0.99f ? Vector3.UnitX : Vector3.UnitY;

            // Rotate the points so that the light direction is along negative Z.
            // light = world
            // Z    = -direction
            // X    = cross(direction, up)
            // Y    = cross(Z, -X);
            var z = -direction;
            var x = Vector3.Cross(direction, up).Normalized();
            var y = Vector3.Cross(z, x).Normalized();
            var lightToWorldSpace = new Matrix4(
                new Vector4(x, 0),
                new Vector4(y, 0),
                new Vector4(z, 0),
                new Vector4(0, 0, 0, 1)
                );
            var worldToLightSpace = lightToWorldSpace.Inverted();
            FrustumPoints.ApplyTransform(in cameraFrustum, in worldToLightSpace, out var AABBPoints);
            FrustumPoints.CalculateAABB(in AABBPoints, out var frustumAABB);

            // Ok, so now we have a AABB of the camera furstum in light space.
            // Now we want to extend the AABB in the Z direction to include all
            // shadowcasters that could cast shadows in this direction.

            // Because -Z is forward we want the biggest Z value for shadow casters
            float newMaxZ = frustumAABB.Max.Z;
            for (int i = 0; i < shadowCasters.Length; i++)
            {
                // Get the shadowcaster bounds points in world space
                var points = new FrustumPoints(shadowCasters[i]);
                // Transforms the world space points into light space
                FrustumPoints.ApplyTransform(in points, in worldToLightSpace, out var newPoints);
                
                IntersectsIgnoreZ(newPoints.Near00, frustumAABB, ref newMaxZ);
                IntersectsIgnoreZ(newPoints.Near01, frustumAABB, ref newMaxZ);
                IntersectsIgnoreZ(newPoints.Near10, frustumAABB, ref newMaxZ);
                IntersectsIgnoreZ(newPoints.Near11, frustumAABB, ref newMaxZ);

                IntersectsIgnoreZ(newPoints.Far00, frustumAABB, ref newMaxZ);
                IntersectsIgnoreZ(newPoints.Far01, frustumAABB, ref newMaxZ);
                IntersectsIgnoreZ(newPoints.Far10, frustumAABB, ref newMaxZ);
                IntersectsIgnoreZ(newPoints.Far11, frustumAABB, ref newMaxZ);

                static void IntersectsIgnoreZ(Vector3 point, Box3 box, ref float max)
                {
                    // FIXME!!!
                    //if (box.Min.X < point.X && point.X < box.Max.X &&
                    //   box.Min.Y < point.Y && point.Y < box.Max.Y)
                    {
                        if (point.Z > max) max = point.Z;
                    }
                }
            }

            // FIXME: Calculate min Z from Shadow receivers
            //if (frustumAABB.Min.Z > newMaxZ) frustumAABB.Min = new Vector3(frustumAABB.Min.X, frustumAABB.Min.Y, newMaxZ);
            if (frustumAABB.Max.Z < newMaxZ) frustumAABB.Max = new Vector3(frustumAABB.Max.X, frustumAABB.Max.Y, newMaxZ);
            
            Matrix4.CreateOrthographic(frustumAABB.Size.X, frustumAABB.Size.Y, 0, frustumAABB.Size.Z, out projection);

            var distance = -frustumAABB.HalfSize.Z;
            viewPosition = Vector3.TransformPosition(frustumAABB.Center, lightToWorldSpace) + direction * distance;

            view = Matrix4.LookAt(viewPosition, viewPosition + direction, up);
            
            var lightSpace = view * projection;
            lightSpace.Invert();
            FrustumPoints.ApplyProjection(FrustumPoints.NDC, lightSpace, out var test);

            //Debugging.DebugHelper.FrustumPoints(Editor.Gizmos.GizmoDrawList, test,
            //    Color4.Magenta, Color4.Magenta);
        }

        public static bool IsBoxAbovePlane(ref Plane plane, ref Box3 aabb)
        {
            Vector4 planeVec = new Vector4(plane.Normal, -plane.Offset);
            bool above = false;
            above |= Vector4.Dot(planeVec, new Vector4(aabb.Min.X, aabb.Min.Y, aabb.Min.Z, 1f)) > 0;
            above |= Vector4.Dot(planeVec, new Vector4(aabb.Max.X, aabb.Min.Y, aabb.Min.Z, 1f)) > 0;
            above |= Vector4.Dot(planeVec, new Vector4(aabb.Min.X, aabb.Max.Y, aabb.Min.Z, 1f)) > 0;
            above |= Vector4.Dot(planeVec, new Vector4(aabb.Max.X, aabb.Max.Y, aabb.Min.Z, 1f)) > 0;
            above |= Vector4.Dot(planeVec, new Vector4(aabb.Min.X, aabb.Min.Y, aabb.Max.Z, 1f)) > 0;
            above |= Vector4.Dot(planeVec, new Vector4(aabb.Max.X, aabb.Min.Y, aabb.Max.Z, 1f)) > 0;
            above |= Vector4.Dot(planeVec, new Vector4(aabb.Min.X, aabb.Max.Y, aabb.Max.Z, 1f)) > 0;
            above |= Vector4.Dot(planeVec, new Vector4(aabb.Max.X, aabb.Max.Y, aabb.Max.Z, 1f)) > 0;
            return above;
        }

        public static bool IsFrustumAbovePlane(in Plane plane, in FrustumPoints points)
        {
            Vector4 planeVec = new Vector4(plane.Normal, -plane.Offset);
            bool above = false;

            above |= Vector4.Dot(planeVec, new Vector4(points.Near00, 1f)) > 0;
            above |= Vector4.Dot(planeVec, new Vector4(points.Near01, 1f)) > 0;
            above |= Vector4.Dot(planeVec, new Vector4(points.Near10, 1f)) > 0;
            above |= Vector4.Dot(planeVec, new Vector4(points.Near11, 1f)) > 0;

            above |= Vector4.Dot(planeVec, new Vector4(points.Far00, 1f)) > 0;
            above |= Vector4.Dot(planeVec, new Vector4(points.Far01, 1f)) > 0;
            above |= Vector4.Dot(planeVec, new Vector4(points.Far10, 1f)) > 0;
            above |= Vector4.Dot(planeVec, new Vector4(points.Far11, 1f)) > 0;

            return above;
        }
    }
}
