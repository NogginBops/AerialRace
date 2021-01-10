using AerialRace.Debugging;
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
    static class Gizmos
    {
        public static Framebuffer GizmosOverlay;

        public static ShaderPipeline GizmoOverlayPipeline;

        public static DrawList GizmoDrawList = new DrawList();

        // FIXME: We might want to view the gizmo from different cameras!
        public static Camera Camera;

        public static Vector2 MousePos;
        public static Vector2 MouseDelta;

        public static Vector2 ScreenSize;
        public static Vector2 InvScreenSize;

        public static void Init()
        {
            // Setup the overlay shader
            var overlayFrag = RenderDataUtil.CreateShaderProgram("Gizmo overlay frag", ShaderStage.Fragment, OverlayFrag);
            RenderDataUtil.CreatePipeline("Gizmo overlay", BuiltIn.FullscreenTriangleVertex, null, overlayFrag, out GizmoOverlayPipeline);

            // FIXME: RESIZE: We want to handle screen resize!!
            var color = RenderDataUtil.CreateEmpty2DTexture("Gizmo overlay color", TextureFormat.Rgba8, Debug.Width, Debug.Height);
            var depth = RenderDataUtil.CreateEmpty2DTexture("Gizmo overlay depth", TextureFormat.Depth32F, Debug.Width, Debug.Height);

            GizmosOverlay = RenderDataUtil.CreateEmptyFramebuffer("Gizmos overlay");
            RenderDataUtil.AddColorAttachment(GizmosOverlay, color, 0, 0);
            RenderDataUtil.AddDepthAttachment(GizmosOverlay, depth, 0);

            var status = RenderDataUtil.CheckFramebufferComplete(GizmosOverlay, FramebufferTarget.ReadDraw);
            if (status != OpenTK.Graphics.OpenGL4.FramebufferStatus.FramebufferComplete)
            {
                throw new Exception(status.ToString());
            }
        }

        public static void UpdateInput(MouseState mouse, KeyboardState keyboard, Vector2 screenSize, Camera camera)
        {
            MousePos = mouse.Position;
            MouseDelta = mouse.Delta;

            ScreenSize = screenSize;
            InvScreenSize = Vector2.Divide(Vector2.One, screenSize);

            Camera = camera;
        }

        public static void TransformHandle(Transform transform)
        {
            const float arrowLength = 2;

            Matrix4 l2w = transform.LocalToWorld;

            Vector3 axisX = l2w.Row0.Xyz.Normalized();
            Vector3 axisY = l2w.Row1.Xyz.Normalized();
            Vector3 axisZ = l2w.Row2.Xyz.Normalized();
            Vector3 translation = l2w.Row3.Xyz;

            Direction(GizmoDrawList, translation, axisX, arrowLength, Color4.Red);
            Direction(GizmoDrawList, translation, axisY, arrowLength, Color4.Lime);
            Direction(GizmoDrawList, translation, axisZ, arrowLength, Color4.Blue);

            DebugHelper.Cone(GizmoDrawList, translation + axisX * arrowLength, 0.2f, 0.5f, axisX, 20, Color4.Red);
            DebugHelper.Cone(GizmoDrawList, translation + axisZ * arrowLength, 0.2f, 0.5f, axisZ, 20, Color4.Blue);
            DebugHelper.Cone(GizmoDrawList, translation + axisY * arrowLength, 0.2f, 0.5f, axisY, 20, Color4.Lime);

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

            float depth = Vector3.Dot(Camera.Transform.Forward, light.Transform.WorldPosition - Camera.Transform.WorldPosition);
            float size = Util.LinearStep(depth, 4, 1000);
            size = Util.MapRange(size, 0, 1, 0.8f, 100);

            var color = new Color4(light.Intensity.X, light.Intensity.Y, light.Intensity.Z, 1f);
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


        struct Ray
        {
            public Vector3 Origin;
            public Vector3 Direction;
        }

        struct Cylinder
        {
            public Vector3 Origin;
            public Vector3 Direction;
            public float Radius;
            public float Height;
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
