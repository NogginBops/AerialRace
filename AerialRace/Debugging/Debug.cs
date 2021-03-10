using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using AerialRace.RenderData;
using OpenTK.Mathematics;

namespace AerialRace.Debugging
{
    static class Debug
    {
        public static ShaderPipeline DebugPipeline;

        public static int Width, Height;
        public static readonly Vector4 FullUV = new Vector4(0, 0, 1, 1);

        public static AttributeSpecification[] DebugAttributes = 
        {
            new AttributeSpecification("Pos",   3, AttributeType.Float, false, 0),
            new AttributeSpecification("UV",    2, AttributeType.Float, false, 12),
            new AttributeSpecification("Color", 4, AttributeType.Float, false, 20),
        };

        public static DrawList List = new DrawList();
        
        public static void Init(int width, int height)
        {
            Width = width;
            Height = height;

            var vertexProg = RenderDataUtil.CreateShaderProgram("Debug Vertex", 
                ShaderStage.Vertex,
                DebugVertexSource);

            var fragProg = RenderDataUtil.CreateShaderProgram("Debug Fragment",
                ShaderStage.Fragment,
                DebugFragmentSource);

            DebugPipeline = RenderDataUtil.CreateEmptyPipeline("Debug Pipeline");
            RenderDataUtil.AssembleProgramPipeline(DebugPipeline, vertexProg, null, fragProg);
        }

        public static void NewFrame(int width, int height)
        {
            Width = width;
            Height = height;

            List.Clear();
        }

        public static void Line(Vector3 a, Vector3 b, Color4<Rgba> color)
        {
            List.AddVertexWithIndex(a, new Vector2(0, 1), color);
            List.AddVertexWithIndex(b, new Vector2(1, 0), color);

            List.AddCommand(OpenTK.Graphics.OpenGL.PrimitiveType.Lines, 2, BuiltIn.WhiteTex);
        }

        public static void Direction(Vector3 origin, Vector3 direction, Color4<Rgba> color)
        {
            List.AddVertexWithIndex(origin, new Vector2(0, 1), color);
            List.AddVertexWithIndex(origin + direction, new Vector2(1, 0), color);

            List.AddCommand(OpenTK.Graphics.OpenGL.PrimitiveType.Lines, 2, BuiltIn.WhiteTex);
        }

        public static void DirectionNormalized(Vector3 origin, Vector3 direction, Color4<Rgba> color)
        {
            List.AddVertexWithIndex(origin, new Vector2(0, 1), color);
            List.AddVertexWithIndex(origin + direction.Normalized(), new Vector2(1, 0), color);

            List.AddCommand(OpenTK.Graphics.OpenGL.PrimitiveType.Lines, 2, BuiltIn.WhiteTex);
        }










        public static void Print(string? message)
        {
            System.Diagnostics.Debug.Print(message);
        }

        public static void WriteLine(string? message)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }

        public static void WriteLine(string? message, string? category)
        {
            System.Diagnostics.Debug.WriteLine(message, category);
        }

        public static void Write(string? message)
        {
            System.Diagnostics.Debug.Write(message);
        }

        public static void Write(string? message, string? category)
        {
            System.Diagnostics.Debug.Write(message, category);
        }

        [System.Diagnostics.DebuggerHidden]
        public static void Break()
        {
            System.Diagnostics.Debugger.Break();
        }

        [System.Diagnostics.DebuggerHidden]
        [System.Diagnostics.Conditional("DEBUG")]
        [DoesNotReturn]
        public static void Assert()
        {
            throw new Exception("Assert failed!");
        }

        [System.Diagnostics.DebuggerHidden]
        [System.Diagnostics.Conditional("DEBUG")]
        public static void Assert([DoesNotReturnIf(false)] bool mustBeTrue)
        {
            if (mustBeTrue == false)
            {
                throw new Exception("Assert failed!");
            }
        }

        [System.Diagnostics.DebuggerHidden]
        [System.Diagnostics.Conditional("DEBUG")]
        public static void Assert([DoesNotReturnIf(false)] bool mustBeTrue, FormattableString message)
        {
            if (mustBeTrue == false)
            {
                throw new Exception(message.ToString());
            }
        }

        [System.Diagnostics.DebuggerHidden]
        [System.Diagnostics.Conditional("DEBUG")]
        public static void AssertNotNull<T>([NotNull] T? notNull, string name)
        {
            if (notNull == null)
            {
                throw new Exception($"{name} cannot be null!");
            }
        }

        public const string DebugVertexSource = @"#version 450 core

layout (location = 0) in vec3 in_position;
layout (location = 1) in vec2 in_uv;
layout (location = 2) in vec4 in_color;

out gl_PerVertex
{
    vec4 gl_Position;
};

out VertexOutput
{
    vec4 fragColor;
    vec2 fragUV;
};

uniform mat4 vp;

void main(void)
{
    gl_Position = vec4(in_position, 1f) * vp;
    fragColor = in_color;
    fragUV = in_uv;
}
";

        public const string DebugFragmentSource = @"#version 450 core

in VertexOutput
{
    vec4 fragColor;
    vec2 fragUV;
};

out vec4 Color;

uniform sampler2D tex;

void main(void)
{
    vec4 tex = texture(tex, fragUV);
    if (tex.a == 0) discard;
    Color = tex * fragColor;
}
";
    }
}
