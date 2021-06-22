using AerialRace.Debugging;
using AerialRace.Loading;
using AerialRace.Mathematics;
using AerialRace.RenderData;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AerialRace.Editor
{
    static partial class Gizmos
    {
        public static Framebuffer GizmosOverlay;

        public static ShaderPipeline GizmoOverlayPipeline;

        public static DrawList GizmoDrawList = new DrawList();

        // FIXME: We might want to view the gizmo from different cameras!
        public static Camera Camera;

        public static bool DisplayLightGizmos = false;
        public static bool DisplayCameraGizmos = false;
        public static bool DisplayTransformGizmos = true;

        public static Vector2 MousePos;
        public static Vector2 MouseDelta;

        public static Ray MouseRay;

        public static Vector2i ScreenSize;
        public static Vector2 InvScreenSize;

        public static bool LeftMousePressed;
        public static bool RightMousePressed;

        public static bool LeftMouseReleased;
        public static bool RightMouseReleased;

        public static bool LeftMouseDown;
        public static bool RightMouseDown;

        public static void Init()
        {
            // Setup the overlay shader
            var overlayFrag = ShaderCompiler.CompileProgramFromSource("Gizmo overlay frag", ShaderStage.Fragment, OverlayFrag);
            GizmoOverlayPipeline = ShaderCompiler.CompilePipeline("Gizmo overlay", BuiltIn.FullscreenTriangleVertex, overlayFrag);

            // FIXME: RESIZE: We want to handle screen resize!!
            var color = RenderDataUtil.CreateEmpty2DTexture("Gizmo overlay color", TextureFormat.Rgba8, Debug.Width, Debug.Height);
            var depth = RenderDataUtil.CreateEmpty2DTexture("Gizmo overlay depth", TextureFormat.Depth32F, Debug.Width, Debug.Height);

            GizmosOverlay = RenderDataUtil.CreateEmptyFramebuffer("Gizmos overlay");
            RenderDataUtil.AddColorAttachment(GizmosOverlay, color, 0, 0);
            RenderDataUtil.AddDepthAttachment(GizmosOverlay, depth, 0);

            Screen.RegisterFramebuffer(GizmosOverlay);

            var status = RenderDataUtil.CheckFramebufferComplete(GizmosOverlay, FramebufferTarget.ReadDraw);
            if (status != OpenTK.Graphics.OpenGL4.FramebufferStatus.FramebufferComplete)
            {
                throw new Exception(status.ToString());
            }
        }

        public static void UpdateInput(MouseState mouse, KeyboardState keyboard, Vector2i screenSize, Camera camera)
        {
            MousePos = mouse.Position;
            MouseDelta = mouse.Delta;

            LeftMousePressed = mouse.IsButtonDown(MouseButton.Left) && !mouse.WasButtonDown(MouseButton.Left);
            RightMousePressed = mouse.IsButtonDown(MouseButton.Right) && !mouse.WasButtonDown(MouseButton.Right);
            LeftMouseReleased =  mouse.WasButtonDown(MouseButton.Left) && !mouse.IsButtonDown(MouseButton.Left);
            RightMouseReleased = mouse.WasButtonDown(MouseButton.Right) && !mouse.IsButtonDown(MouseButton.Right);
            LeftMouseDown = mouse.IsButtonDown(MouseButton.Left);
            RightMouseDown = mouse.IsButtonDown(MouseButton.Right);

            ScreenSize = screenSize;
            InvScreenSize = Vector2.Divide(Vector2.One, screenSize);

            MouseRay = camera.RayFromPixel(MousePos, ScreenSize);
            //Debug.WriteLine($"Pixel: {MousePos}, Ray: {MouseRay.Origin} + t{MouseRay.Direction}");

            Camera = camera;
        }

        enum Axis
        {
            None, 
            X, Y, Z,
            XRotation,
            YRotation,
            ZRotation,
        }

        static Axis EditAxis = Axis.None;
        static Vector3 StartPosition = default;
        static Vector3 StartWorldPosition = default;
        static Quaternion StartRotation = default;
        static Vector3 StartRotationPos = default;
        static Vector3 PreviousPoint = default;
        public static void TransformHandle(Transform transform)
        {
            if (DisplayTransformGizmos == false) return;

            float arrowLength = 2;
            float radius = 0.2f;

            Matrix4 l2w = transform.LocalToWorld;

            Vector3 axisX = l2w.Row0.Xyz.Normalized();
            Vector3 axisY = l2w.Row1.Xyz.Normalized();
            Vector3 axisZ = l2w.Row2.Xyz.Normalized();
            Vector3 translation = l2w.Row3.Xyz;

            float depth = Vector3.Dot(Camera.Transform.Forward, transform.WorldPosition - Camera.Transform.WorldPosition);
            float size = Util.LinearStep(depth, 25, 5000);
            size = Util.MapRange(size, 0, 1, 2f, 100);

            // FIXME: Is there a better way to handle this??
            if (Camera.ProjectionType == ProjectionType.Orthographic)
                size = 1;

            arrowLength *= size;
            radius *= size;

            var xRay = new Ray(translation, axisX);
            var yRay = new Ray(translation, axisY);
            var zRay = new Ray(translation, axisZ);

            float rotationRadius = 2f * size;

            var xDisk = new Disk(translation, axisX, rotationRadius);
            var yDisk = new Disk(translation, axisY, rotationRadius);
            var zDisk = new Disk(translation, axisZ, rotationRadius);

            Color4 xAxisColor = new Color4(0.8f, 0f, 0f, 1f);
            Color4 yAxisColor = new Color4(0f, 0.8f, 0f, 1f);
            Color4 zAxisColor = new Color4(0f, 0f, 0.8f, 1f);

            Color4 xRotationColor = new Color4(0.8f, 0f, 0f, 1f);
            Color4 yRotationColor = new Color4(0f, 0.8f, 0f, 1f);
            Color4 zRotationColor = new Color4(0f, 0f, 0.8f, 1f);

            var closestTranslateAxis = GetClosestAxis(MouseRay, xRay, yRay, zRay, arrowLength, radius, out var translationDistance, out var translationPoint);

            var closestRotationAxis = GetClosestRotationAxis(MouseRay, xDisk, yDisk, zDisk, radius, out var rotationDistance, out var rotationPoint);

            Axis closestAxis =
                translationDistance < rotationDistance ?
                closestTranslateAxis :
                closestRotationAxis;

            if (LeftMousePressed)
            {
                EditAxis = closestAxis;
                PreviousPoint = translationDistance < rotationDistance ? translationPoint : rotationPoint;
                StartPosition = transform.LocalPosition;
                StartWorldPosition = transform.WorldPosition;
                StartRotationPos = rotationPoint;
                StartRotation = transform.LocalRotation;
            }

            if (LeftMouseReleased && EditAxis != Axis.None)
            {
                if (EditAxis == Axis.X || EditAxis == Axis.Y || EditAxis == Axis.Z)
                {
                    Undo.EditorUndoStack.PushAlreadyDone(new Translate()
                    {
                        Transform = transform,
                        StartPosition = StartPosition,
                        EndPosition = transform.LocalPosition
                    });

                    var setpiece = StaticSetpiece.Instances.Find(piece => piece.Transform == transform);
                    if (setpiece != null)
                    {
                        Undo.EditorUndoStack.Do(new PhysTranslate()
                        {
                            Setpiece = setpiece,
                            StartPosition = StartWorldPosition,
                            EndPosition = transform.WorldPosition,
                        });
                    }
                }
                else if (EditAxis == Axis.XRotation || EditAxis == Axis.YRotation || EditAxis == Axis.ZRotation)
                {
                    Undo.EditorUndoStack.PushAlreadyDone(new Rotate()
                    {
                        Transform = transform,
                        StartRotation = StartRotation,
                        EndRotation = transform.LocalRotation
                    });
                }
            }

            if (LeftMouseDown == false)
            {
                EditAxis = Axis.None;
            }
            else
            {
                switch (EditAxis)
                {
                    case Axis.X:
                    case Axis.Y:
                    case Axis.Z:
                        {
                            Ray ray = EditAxis switch
                            {
                                Axis.X => xRay,
                                Axis.Y => yRay,
                                Axis.Z => zRay,
                                _ => throw new Exception()
                            };

                            ClosestDistanceToLine(MouseRay, ray, out _, out var axisT);

                            var newPoint = ray.GetPoint(axisT);

                            var localPrevPoint = transform.WorldPositionToLocal(PreviousPoint);
                            var localNewPoint = transform.WorldPositionToLocal(newPoint);

                            // We are dragging some axis!
                            var delta = localNewPoint - localPrevPoint;

                            //delta = newPoint - PreviousPoint;

                            delta = transform.WorldDirectionToLocal(delta);

                            if (delta.Length > 0)
                            {
                                ;
                            }

                            transform.LocalPosition += delta;
                            transform.UpdateMatrices();
                            PreviousPoint = newPoint;
                        }
                        break;
                    case Axis.XRotation:
                    case Axis.YRotation:
                    case Axis.ZRotation:
                        {
                            Disk disk = EditAxis switch
                            {
                                Axis.XRotation => xDisk,
                                Axis.YRotation => yDisk,
                                Axis.ZRotation => zDisk,
                                _ => throw new Exception()
                            };

                            ClosestDistanceToDisk(MouseRay, disk, out var newPoint);

                            var current = newPoint - translation;
                            var previous = PreviousPoint - translation;

                            DebugHelper.Line(GizmoDrawList, translation, StartRotationPos, Color4.Yellow, Color4.Yellow);
                            DebugHelper.Line(GizmoDrawList, translation, newPoint, Color4.Purple, Color4.Purple);

                            var θ = Vector3.CalculateAngle(previous, current);
                            var sign = Vector3.Dot(Vector3.Cross(previous, current), disk.Normal);
                            θ = MathF.CopySign(θ, sign);
                            Debug.Assert(float.IsNaN(θ) == false);
                            var axis = EditAxis switch
                            {
                                Axis.XRotation => Vector3.UnitX,
                                Axis.YRotation => Vector3.UnitY,
                                Axis.ZRotation => Vector3.UnitZ,
                                _ => throw new Exception()
                            };
                            transform.LocalRotation *= Quaternion.FromAxisAngle(axis, θ);
                            PreviousPoint = newPoint;
                        }
                        break;
                    case Axis.None:
                    default:
                        break;
                }
            }

            switch (EditAxis)
            {
                case Axis.X:
                    xAxisColor = Color4.White;
                    break;
                case Axis.Y:
                    yAxisColor = Color4.White;
                    break;
                case Axis.Z:
                    zAxisColor = Color4.White;
                    break;
                case Axis.XRotation:
                    xRotationColor = Color4.White;
                    break;
                case Axis.YRotation:
                    yRotationColor = Color4.White;
                    break;
                case Axis.ZRotation:
                    zRotationColor = Color4.White;
                    break;
                case Axis.None:
                    switch (closestAxis)
                    {
                        case Axis.X:
                            xAxisColor = new Color4(1f, 0.4f, 0.4f, 1f);
                            break;
                        case Axis.Y:
                            yAxisColor = new Color4(0.4f, 1f, 0.4f, 1f);
                            break;
                        case Axis.Z:
                            zAxisColor = new Color4(0.4f, 0.4f, 1f, 1f);
                            break;
                        case Axis.XRotation:
                            xRotationColor = new Color4(1f, 0.4f, 0.4f, 1f);
                            break;
                        case Axis.YRotation:
                            yRotationColor = new Color4(0.4f, 1f, 0.4f, 1f);
                            break;
                        case Axis.ZRotation:
                            zRotationColor = new Color4(0.4f, 0.4f, 1f, 1f);
                            break;
                        case Axis.None:
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }

            OutlineDisk(GizmoDrawList, xDisk, 50, xRotationColor);
            OutlineDisk(GizmoDrawList, yDisk, 50, yRotationColor);
            OutlineDisk(GizmoDrawList, zDisk, 50, zRotationColor);

            Direction(GizmoDrawList, translation, axisX, arrowLength, xAxisColor);
            Direction(GizmoDrawList, translation, axisY, arrowLength, yAxisColor);
            Direction(GizmoDrawList, translation, axisZ, arrowLength, zAxisColor);

            float height = 0.5f * size;
            DebugHelper.Cone(GizmoDrawList, translation + axisX * arrowLength, radius, height, axisX, 20, xAxisColor);
            DebugHelper.Cone(GizmoDrawList, translation + axisY * arrowLength, radius, height, axisY, 20, yAxisColor);
            DebugHelper.Cone(GizmoDrawList, translation + axisZ * arrowLength, radius, height, axisZ, 20, zAxisColor);

            Matrix3 rotation = new Matrix3(axisX, axisY, axisZ);

            float ScaleBoxSize = 0.4f * size;
            Vector3 halfSize = (ScaleBoxSize, ScaleBoxSize, ScaleBoxSize);
            halfSize /= 2f;
            float boxDist = arrowLength + ScaleBoxSize + 0.4f * size;

            Cube(GizmoDrawList, translation + axisX * boxDist, halfSize, rotation, Color4.Red);
            Cube(GizmoDrawList, translation + axisY * boxDist, halfSize, rotation, Color4.Lime);
            Cube(GizmoDrawList, translation + axisZ * boxDist, halfSize, rotation, Color4.Blue);

            static void Direction(DrawList list, Vector3 origin, Vector3 dir, float length, Color4 color)
            {
                list.AddVertexWithIndex(origin, (0, 1), color);
                list.AddVertexWithIndex(origin + (dir * length), (1, 0), color);
                list.AddCommand(Primitive.Lines, 2, BuiltIn.WhiteTex);
            }

            static void Cube(DrawList list, Vector3 center, Vector3 halfSize, in Matrix3 rot, Color4 color)
            {
                Span<int> iArray = stackalloc int[6] { 0, 1, 2, 1, 3, 2 };

                const int faces = 6;

                int index = 0;
                for (int f = 0; f < faces; f++)
                {
                    list.AddRelativeIndices(iArray);
                    for (int i = 0; i < 4; i++)
                    {
                        Vector3 offset = (DebugHelper.BoxVertices[index].Pos * halfSize) * rot;
                        Vector3 pos = center + offset;
                        list.AddVertex(pos , DebugHelper.BoxVertices[index].UV, color);
                        
                        index++;
                    }
                }

                list.AddCommand(Primitive.Triangles, faces * 6, BuiltIn.WhiteTex);
            }

            // From: https://nelari.us/post/gizmos/
            static float ClosestDistanceToLine(Ray r1, Ray r2, out float t1, out float t2)
            {
                var dp = r2.Origin - r1.Origin;
                var v12 = r1.Direction.LengthSquared;
                var v22 = r2.Direction.LengthSquared;
                var v1v2 = Vector3.Dot(r1.Direction, r2.Direction);

                float det = v1v2 * v1v2 - v12 * v22;

                if (MathF.Abs(det) > 0.00001f)
                {
                    var invDet = 1 / det;
                    var dpv1 = Vector3.Dot(dp, r1.Direction);
                    var dpv2 = Vector3.Dot(dp, r2.Direction);

                    // For some reason we need to negate these...?
                    t1 = -invDet * (v22 * dpv1 - v1v2 * dpv2);
                    t2 = -invDet * (v1v2 * dpv1 - v12 * dpv2);

                    return (dp + r2.Direction * t2 - r1.Direction * t1).Length;
                }
                else
                {
                    var a = Vector3.Cross(dp, r1.Direction);
                    t1 = float.MaxValue;
                    t2 = float.MaxValue;
                    return MathF.Sqrt(a.LengthSquared / v12);
                }
            }
            
            static Axis GetClosestAxis(Ray mouseRay, Ray xRay, Ray yRay, Ray zRay, float arrowLength, float radius, out float distance, out Vector3 point)
            {
                float xDistance = ClosestDistanceToLine(MouseRay, xRay, out var xMouseT, out var xAxisT);
                float yDistance = ClosestDistanceToLine(MouseRay, yRay, out var yMouseT, out var yAxisT);
                float zDistance = ClosestDistanceToLine(MouseRay, zRay, out var zMouseT, out var zAxisT);

                distance = float.MaxValue;
                Axis editAxis = Axis.None;

                if (xMouseT > 0 && xDistance <= radius && xAxisT >= 0 && xAxisT <= arrowLength)
                {
                    editAxis = Axis.X;
                    distance = xDistance;
                }

                if (yMouseT > 0 && yDistance <= radius && yAxisT >= 0 && yAxisT <= arrowLength)
                {
                    if (yDistance < distance)
                    {
                        editAxis = Axis.Y;
                        distance = yDistance;
                    }
                }

                if (zMouseT > 0 && zDistance <= radius && zAxisT >= 0 && zAxisT <= arrowLength)
                {
                    if (zDistance < distance)
                    {
                        editAxis = Axis.Z;
                        distance = zDistance;
                    }
                }

                point = editAxis switch
                {
                    Axis.X => xRay.GetPoint(xAxisT),
                    Axis.Y => yRay.GetPoint(yAxisT),
                    Axis.Z => zRay.GetPoint(zAxisT),
                    _ => default,
                };

                return editAxis;
            }

            static float ClosestDistanceToDisk(Ray ray, Disk circle, out Vector3 point)
            {
                Plane f = new Plane(circle.Normal, Vector3.Dot(circle.Center, circle.Normal));

                var (v1, v2) = DebugHelper.GetAny2Perp(f.Normal);

                float t = Plane.Intersect(f, ray);
                if (t >= 0)
                {
                    var onPlane = ray.GetPoint(t);
                    point = circle.Center + circle.Radius * (onPlane - circle.Center).Normalized();

                    return (onPlane - point).Length;
                }
                else
                {
                    point = circle.Radius * Reject(ray.Origin - circle.Center, circle.Normal).Normalized();

                    return PointLineDistance(ray, point);
                }

                static float PointLineDistance(Ray ray, Vector3 point)
                {
                    var v = point - ray.Origin;
                    return Reject(v, ray.Direction).Length;
                }

                static Vector3 Reject(Vector3 a, Vector3 b)
                {
                    return a - Vector3.Dot(a, b) * b;
                }
            }

            static Axis GetClosestRotationAxis(Ray mouseRay, Disk xDisk, Disk yDisk, Disk zDisk, float interactionRadius, out float distance, out Vector3 point)
            {
                float xDistance = ClosestDistanceToDisk(MouseRay, xDisk, out var xPos);
                float yDistance = ClosestDistanceToDisk(MouseRay, yDisk, out var yPos);
                float zDistance = ClosestDistanceToDisk(MouseRay, zDisk, out var zPos);
                
                point = default;

                distance = float.MaxValue;
                Axis editAxis = Axis.None;
                if (xDistance <= interactionRadius)
                {
                    editAxis = Axis.XRotation;
                    distance = xDistance;
                }

                if (yDistance <= interactionRadius)
                {
                    if (yDistance < distance)
                    {
                        editAxis = Axis.YRotation;
                        distance = yDistance;
                    }
                }

                if (zDistance <= interactionRadius)
                {
                    if (zDistance < distance)
                    {
                        editAxis = Axis.ZRotation;
                        distance = zDistance;
                    }
                }

                point = editAxis switch
                {
                    Axis.XRotation => xPos,
                    Axis.YRotation => yPos,
                    Axis.ZRotation => zPos,
                    _ => default,
                };
                return editAxis;
            }
        }

        public static void CameraGizmo(Camera camera)
        {
            if (DisplayCameraGizmos == false) return;

            camera.CalcViewProjection(out var vp);
            vp.Invert();
            FrustumPoints.ApplyProjection(FrustumPoints.NDC, vp, out var frustum);
            DebugHelper.FrustumPoints(GizmoDrawList, frustum, Color4.White, Color4.White);
        }

        // FIXME: Find a better way to render icons so that the alpha blending becomes correct!
        // When we fix this we should remove the discard from the DebugPipeline fragment shader.
        public static void LightIcon(Light light)
        {
            if (DisplayLightGizmos == false) return;

            Vector3 right = Camera.Transform.Right;
            Vector3 up = Camera.Transform.Up;

            Vector3 pos = light.Transform.WorldPosition;

            var color = new Color4(light.Intensity.X, light.Intensity.Y, light.Intensity.Z, 1f);

            //OutlineSphere(GizmoDrawList, pos, light.Radius, 50, color);

            float depth = Vector3.Dot(Camera.Transform.Forward, light.Transform.WorldPosition - Camera.Transform.WorldPosition);
            float size = Util.LinearStep(depth, 4, 1000);
            size = Util.MapRange(size, 0, 1, 0.8f, 100);

            // FIXME: Is there a better way to handle this??
            if (Camera.ProjectionType == ProjectionType.Orthographic)
                size = Camera.OrthograpicSize * 0.1f;

            Billboard(GizmoDrawList, pos, right, up, size, EditorResources.PointLightIcon, color);
        }

        public static void RenderAABBGizmo(Transform transform, MeshRenderer renderer)
        {

        }

        public static void Billboard(DrawList list, Vector3 position, Vector3 right, Vector3 up, float size, Texture texture, Color4 color)
        {
            list.Prewarm(4);

            float size2 = size / 2f;
            var right2 = right * size2;
            var up2 = up * size2;
            list.AddVertexWithIndex(position - right2 - up2, (0, 0), color);
            list.AddVertexWithIndex(position + right2 - up2, (1, 0), color);
            list.AddVertexWithIndex(position - right2 + up2, (0, 1), color);
            list.AddVertexWithIndex(position + right2 + up2, (1, 1), color);

            list.AddCommand(Primitive.TriangleStrip, 4, texture);
        }

        public static void OutlineDisk(DrawList list, Disk disk, int segments, Color4 color)
        {
            if (segments <= 2) throw new ArgumentException($"Segments cannot be less than 2. {segments}", nameof(segments));
            list.Prewarm(segments);

            var (v1, v2) = DebugHelper.GetAny2Perp(disk.Normal);

            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)segments;

                float x = MathF.Cos(t * 2 * MathF.PI);
                float y = MathF.Sin(t * 2 * MathF.PI);

                Vector3 offset = (x * v1 + y * v2) * disk.Radius;

                list.AddVertexWithIndex(disk.Center + offset, new Vector2(x, y), color);
            }

            list.AddCommand(Primitive.LineLoop, segments, BuiltIn.WhiteTex);
        }

        public static void OutlineSphere(DrawList list, Vector3 pos, float radius, int segments, Color4 color)
        {
            if (segments <= 2) throw new ArgumentException($"Segments cannot be less than 2. {segments}", nameof(segments));
            list.Prewarm(segments);

            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)segments;

                float x = MathF.Cos(t * 2 * MathF.PI);
                float y = MathF.Sin(t * 2 * MathF.PI);

                Vector3 offset = new Vector3(x, y, 0) * radius;

                list.AddVertexWithIndex(pos + offset, new Vector2(x, y), color);
            }

            list.AddCommand(Primitive.LineLoop, segments, BuiltIn.WhiteTex);

            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)segments;

                float x = MathF.Cos(t * 2 * MathF.PI);
                float y = MathF.Sin(t * 2 * MathF.PI);

                Vector3 offset = new Vector3(x, 0, y) * radius;

                list.AddVertexWithIndex(pos + offset, new Vector2(x, y), color);
            }

            list.AddCommand(Primitive.LineLoop, segments, BuiltIn.WhiteTex);

            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)segments;

                float x = MathF.Cos(t * 2 * MathF.PI);
                float y = MathF.Sin(t * 2 * MathF.PI);

                Vector3 offset = new Vector3(0, x, y) * radius;

                list.AddVertexWithIndex(pos + offset, new Vector2(x, y), color);
            }

            list.AddCommand(Primitive.LineLoop, segments, BuiltIn.WhiteTex);
        }

        public const string OverlayFrag = @"#version 460 core

in VertexOutput
{
    vec2 uv;
};

out vec4 Color;

uniform sampler2D overlayTex;

void main(void)
{
    Color = texture(overlayTex, uv);
}
";
    }
}
