using AerialRace.Debugging;
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
        public static Material GizmoMaterial;
        public static DrawList GizmoDrawList = new DrawList();

        // FIXME: We might want to view the gizmo from different cameras!
        public static Camera Camera;

        public static Vector2 MousePos;
        public static Vector2 MouseDelta;

        public static Vector2 ScreenSize;
        public static Vector2 InvScreenSize;

        public static void Init()
        {
            RenderData.RenderDataUtil.CreateShaderProgram("Gizmo Vertex", RenderData.ShaderStage.Vertex, VertexSource, out var vertex);
            RenderData.RenderDataUtil.CreateShaderProgram("Gizmo Fragment", RenderData.ShaderStage.Fragment, FragmentSource, out var fragment);
            RenderData.RenderDataUtil.CreatePipeline("Gizmo", vertex, null, fragment, out var pipeline);
            GizmoMaterial = new Material("Gizmo", pipeline, null);
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
            Vector4 worldPos = new Vector4(transform.WorldPosition, 1);
            Camera.CalcViewProjection(out var vp);

            const float arrowLength = 2;

            Vector4 wp = new Vector4(transform.WorldPosition, 1);
            Vector4 wp_plusX = new Vector4(transform.WorldPosition + transform.Right.Normalized() * arrowLength, 1);
            Vector4 wp_minusZ = new Vector4(transform.WorldPosition + transform.Forward.Normalized() * arrowLength, 1);
            Vector4 wp_plusY = new Vector4(transform.WorldPosition + transform.Up.Normalized() * arrowLength, 1);

            Line(GizmoDrawList, wp.Xyz, wp_plusX.Xyz, Color4.Red);
            Line(GizmoDrawList, wp.Xyz, wp_plusY.Xyz, Color4.Lime); // because for some reason Green isn't green...
            Line(GizmoDrawList, wp.Xyz, wp_minusZ.Xyz, Color4.Blue);

            var m = new Matrix3(transform.LocalToWorld);

            // FIXME: We actually want the worldspace rotation!!
            // We might need to modify this to make it work
            Quaternion Zaxis_rot = transform.LocalRotation;
            DebugHelper.Cone(GizmoDrawList, wp_minusZ.Xyz, 0.2f, 0.4f, Zaxis_rot, 20, Color4.Blue);

            Quaternion Xaxis_rot = Zaxis_rot * Quaternion.FromAxisAngle(Vector3.UnitY, -MathF.PI/2);
            DebugHelper.Cone(GizmoDrawList, wp_plusY.Xyz, 0.2f, 0.3f, Xaxis_rot, 20, Color4.Red);
            
            Quaternion Yaxis_rot = Zaxis_rot * Quaternion.FromAxisAngle(Vector3.UnitX, MathF.PI / 2);
            DebugHelper.Cone(GizmoDrawList, wp_plusY.Xyz, 0.2f, 0.3f, Yaxis_rot, 20, Color4.Lime);
            
            static void Line(DrawList list, Vector3 from, Vector3 to, Color4 color)
            {
                list.AddVertexWithIndex(from, (0, 1), color);
                list.AddVertexWithIndex(to,   (1, 0), color);
                list.AddCommand(OpenTK.Graphics.OpenGL4.PrimitiveType.Lines, 2, BuiltIn.WhiteTex);
            }

            static void Rect(DrawList list, Vector3 origin, Vector3 base1, Vector3 base2, Color4 color)
            {
                list.AddVertex(origin, (0, 0), color);
                list.AddVertex(origin, (0, 0), color);
                list.AddVertex(origin, (0, 0), color);
                list.AddVertex(origin, (0, 0), color);

            }
        }


        // FIXME: This is actually the same as all of the other drawlist rendering!
        // So we should remove this
        private const string VertexSource = @"#version 460 core

layout (location = 0) in vec3 in_position;
layout (location = 2) in vec4 in_color;

out gl_PerVertex
{
    vec4 gl_Position;
};

out VertexOutput
{
    vec4 v_color;
};

uniform mat4 vp;

void main(void)
{
    gl_Position = vec4(in_position, 1f) * vp;
    v_color = in_color;
}
";

        private const string FragmentSource = @"#version 460 core

in VertexOutput
{
    vec4 v_color;
};

out vec4 Color;

void main(void)
{
    Color = vec4(v_color.rgb, 1);
}
";
    }
}
