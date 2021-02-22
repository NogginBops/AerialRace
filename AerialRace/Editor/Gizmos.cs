using AerialRace.Debugging;
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

        public static Vector2 MousePos;
        public static Vector2 MouseDelta;

        public static Ray MouseRay;

        public static Vector2i ScreenSize;
        public static Vector2 InvScreenSize;

        public static void Init()
        {
            // Setup the overlay shader
            var overlayFrag = RenderDataUtil.CreateShaderProgram("Gizmo overlay frag", ShaderStage.Fragment, OverlayFrag);
            GizmoOverlayPipeline = RenderDataUtil.CreatePipeline("Gizmo overlay", BuiltIn.FullscreenTriangleVertex, null, overlayFrag);

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

            ScreenSize = screenSize;
            InvScreenSize = Vector2.Divide(Vector2.One, screenSize);

            MouseRay = camera.RayFromPixel(MousePos, ScreenSize);
            Debug.WriteLine($"Pixel: {MousePos}, Ray: {MouseRay.Origin} + t{MouseRay.Direction}");

            Camera = camera;
        }

        public static void TransformHandle(Transform transform)
        {
            const float arrowLength = 2;
            const float radius = 0.2f;

            Matrix4 l2w = transform.LocalToWorld;

            Vector3 axisX = l2w.Row0.Xyz.Normalized();
            Vector3 axisY = l2w.Row1.Xyz.Normalized();
            Vector3 axisZ = l2w.Row2.Xyz.Normalized();
            Vector3 translation = l2w.Row3.Xyz;
            
            // FIXME: Make cylinder intersection work
            Cylinder xAxisCylinder = new Cylinder(translation, translation + axisX * arrowLength, radius);
            float t = Cylinder.Intersect(MouseRay, xAxisCylinder);
            //Debug.WriteLine($"Cylinder: A={xAxisCylinder.A}, B={xAxisCylinder.B}, r={xAxisCylinder.Radius}, t={t}");
            Color4 xAxisColor = Color4.Red;
            xAxisColor = t < 0 ? Color4.Pink : Color4.White;

            DebugHelper.Cylinder(GizmoDrawList, xAxisCylinder, 20, xAxisColor);
            
            Direction(GizmoDrawList, translation, axisX, arrowLength, Color4.Red);
            Direction(GizmoDrawList, translation, axisY, arrowLength, Color4.Lime);
            Direction(GizmoDrawList, translation, axisZ, arrowLength, Color4.Blue);

            DebugHelper.Cone(GizmoDrawList, translation + axisX * arrowLength, radius, 0.5f, axisX, 20, xAxisColor);
            DebugHelper.Cone(GizmoDrawList, translation + axisZ * arrowLength, radius, 0.5f, axisZ, 20, Color4.Blue);
            DebugHelper.Cone(GizmoDrawList, translation + axisY * arrowLength, radius, 0.5f, axisY, 20, Color4.Lime);

            Matrix3 rotation = new Matrix3(axisX, axisY, axisZ);

            const float ScaleBoxSize = 0.4f;
            Vector3 halfSize = (ScaleBoxSize, ScaleBoxSize, ScaleBoxSize);
            halfSize /= 2f;
            float boxDist = arrowLength + ScaleBoxSize + 0.4f;

            Cube(GizmoDrawList, translation + axisX * boxDist, halfSize, rotation, Color4.Red);
            Cube(GizmoDrawList, translation + axisY * boxDist, halfSize, rotation, Color4.Lime);
            Cube(GizmoDrawList, translation + axisZ * boxDist, halfSize, rotation, Color4.Blue);

            static void Direction(DrawList list, Vector3 origin, Vector3 dir, float length, Color4 color)
            {
                list.AddVertexWithIndex(origin, (0, 1), color);
                list.AddVertexWithIndex(origin + (dir * length), (1, 0), color);
                list.AddCommand(OpenTK.Graphics.OpenGL4.PrimitiveType.Lines, 2, BuiltIn.WhiteTex);
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

                list.AddCommand(OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles, faces * 6, BuiltIn.WhiteTex);
            }
        }

        // FIXME: Find a better way to render icons so that the alpha blending becomes correct!
        // When we fix this we should remove the discard from the DebugPipeline fragment shader.
        public static void LightIcon(Light light)
        {
            Vector3 right = Camera.Transform.Right;
            Vector3 up = Camera.Transform.Up;

            Vector3 pos = light.Transform.WorldPosition;

            var color = new Color4(light.Intensity.X, light.Intensity.Y, light.Intensity.Z, 1f);

            //OutlineSphere(GizmoDrawList, pos, light.Radius, 50, color);

            float depth = Vector3.Dot(Camera.Transform.Forward, light.Transform.WorldPosition - Camera.Transform.WorldPosition);
            float size = Util.LinearStep(depth, 4, 1000);
            size = Util.MapRange(size, 0, 1, 0.8f, 100);

            Billboard(GizmoDrawList, pos, right, up, size, EditorResources.PointLightIcon, color);
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

            list.AddCommand(OpenTK.Graphics.OpenGL4.PrimitiveType.TriangleStrip, 4, texture);
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

            list.AddCommand(OpenTK.Graphics.OpenGL4.PrimitiveType.LineLoop, segments, BuiltIn.WhiteTex);

            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)segments;

                float x = MathF.Cos(t * 2 * MathF.PI);
                float y = MathF.Sin(t * 2 * MathF.PI);

                Vector3 offset = new Vector3(x, 0, y) * radius;

                list.AddVertexWithIndex(pos + offset, new Vector2(x, y), color);
            }

            list.AddCommand(OpenTK.Graphics.OpenGL4.PrimitiveType.LineLoop, segments, BuiltIn.WhiteTex);

            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)segments;

                float x = MathF.Cos(t * 2 * MathF.PI);
                float y = MathF.Sin(t * 2 * MathF.PI);

                Vector3 offset = new Vector3(0, x, y) * radius;

                list.AddVertexWithIndex(pos + offset, new Vector2(x, y), color);
            }

            list.AddCommand(OpenTK.Graphics.OpenGL4.PrimitiveType.LineLoop, segments, BuiltIn.WhiteTex);
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
