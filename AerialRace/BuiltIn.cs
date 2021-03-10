using AerialRace.Loading;
using AerialRace.RenderData;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace AerialRace
{
    static class BuiltIn
    {
        public static void StaticCtorTrigger() { }

        public static readonly Texture WhiteTex;
        public static readonly Texture WhiteTransparentTex;
        public static readonly Texture BlackTex;
        public static readonly Texture BlackTransparentTex;
        public static readonly Texture FlatNormalTex;

        public static readonly Texture UVTest;

        public static readonly AttributeSpecification[] StandardAttributes;

        public static ShaderProgram ErrorVertexProgram;
        public static ShaderProgram ErrorFragmentProgram;

        public static ShaderPipeline ErrorShaderPipeline;

        public static ShaderProgram FullscreenTriangleVertex;

        static BuiltIn()
        {
            WhiteTex = RenderDataUtil.Create1PixelTexture("BuiltIn.White", new Color4<Rgba>(1f, 1f, 1f, 1f));
            WhiteTransparentTex = RenderDataUtil.Create1PixelTexture("BuiltIn.WhiteTransparent", new Color4<Rgba>(1f, 1f, 1f, 0f));
            BlackTex = RenderDataUtil.Create1PixelTexture("BuiltIn.Black", new Color4<Rgba>(0f, 0f, 0f, 1f));
            BlackTransparentTex = RenderDataUtil.Create1PixelTexture("BuiltIn.BlackTransparent", new Color4<Rgba>(0f, 0f, 0f, 0f));
            FlatNormalTex = RenderDataUtil.Create1PixelTexture("BuiltIn.FlatNormal", new Color4<Rgba>(.5f, .5f, 1f, 1f));

            // FIXME: This might fail... do we want to load this here?
            UVTest = TextureLoader.LoadRgbaImage("Builtin UV Test", "./Textures/uvtest.png", true, true);

            // These are attributes defined in the "standard" vertex stream.
            StandardAttributes = new[]
            {
                new AttributeSpecification("Position",     3, AttributeType.Float, false, 0),
                new AttributeSpecification("UV",           2, AttributeType.Float, false, 12),
                new AttributeSpecification("Normal",       3, AttributeType.Float, false, 20),
            };

            // FIXME: Make error handling for shader that don't compile better!
            ErrorVertexProgram = RenderDataUtil.CreateShaderProgram("Error vertex", ShaderStage.Vertex, ErrorVertexSource);
            ErrorFragmentProgram = RenderDataUtil.CreateShaderProgram("Error fragment", ShaderStage.Fragment, ErrorFragmentSource);

            ErrorShaderPipeline = RenderDataUtil.CreatePipeline("Error pipeline", ErrorVertexProgram, null, ErrorFragmentProgram);

            FullscreenTriangleVertex = RenderDataUtil.CreateShaderProgram("Fullscreen triangle vert", ShaderStage.Vertex, FullscreenTriangleVertexSource);
        }

        public const string ErrorVertexSource = @"#version 450 core

layout (location = 0) in vec3 in_position;

out gl_PerVertex
{
    vec4 gl_Position;
};

uniform mat4 mvp;

void main(void)
{
    gl_Position = vec4(in_position, 1f) * mvp;
}
";

        public const string ErrorFragmentSource = @"#version 450 core

out vec4 Color;

void main(void)
{
    Color = vec4(1, 0, 1, 1);
}
";

        public const string FullscreenTriangleVertexSource = @"#version 460 core

out gl_PerVertex
{
    out vec4 gl_Position;
};

out VertexOutput
{
    vec2 uv;
};
 
void main()
{
    float x = -1.0 + float((gl_VertexID & 1) << 2);
    float y = -1.0 + float((gl_VertexID & 2) << 1);
    uv.x = (x+1.0)*0.5;
    uv.y = (y+1.0)*0.5;
    gl_Position = vec4(x, y, 0, 1);
}
";

    }
}
