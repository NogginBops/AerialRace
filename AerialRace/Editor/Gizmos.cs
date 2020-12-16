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
            Camera.CalcViewProjection(out var vp);
            Vector4 worldPos = new Vector4(transform.WorldPosition, 1);
            Vector4 screenPos = worldPos * vp;
            screenPos /= screenPos.W;

            Vector2 size = (1, 1) * InvScreenSize;

            Rect rect = new Rect(screenPos.X, screenPos.Y, size.X, size.Y);
            DebugHelper.Rect(GizmoDrawList, rect, Debug.FullUV, BuiltIn.WhiteTex, Color4.CornflowerBlue);
        }

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

void main(void)
{
    gl_Position = vec4(in_position, 1f);
    v_color = in_color;
}
";

        private const string FragmentSource = @"#version 460 core

in VertexOutput
{
    vec4 v_color;
}

out vec4 Color;

void main(void)
{
    Color = vec4(v_color.rgb, 1);
}
";
    }
}
