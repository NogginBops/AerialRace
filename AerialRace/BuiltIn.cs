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

        public static readonly AttributeSpecification[] StandardAttributes;

        public static ShaderProgram ErrorVertexProgram;
        public static ShaderProgram ErrorFragmentProgram;

        public static ShaderPipeline ErrorShaderPipeline;

        static BuiltIn()
        {
            WhiteTex = RenderDataUtil.Create1PixelTexture("BuiltIn.White", new Color4(1f, 1f, 1f, 1f));
            WhiteTransparentTex = RenderDataUtil.Create1PixelTexture("BuiltIn.WhiteTransparent", new Color4(1f, 1f, 1f, 0f));
            BlackTex = RenderDataUtil.Create1PixelTexture("BuiltIn.Black", new Color4(0f, 0f, 0f, 1f));
            BlackTransparentTex = RenderDataUtil.Create1PixelTexture("BuiltIn.BlackTransparent", new Color4(0f, 0f, 0f, 0f));
            FlatNormalTex = RenderDataUtil.Create1PixelTexture("BuiltIn.FlatNormal", new Color4(.5f, .5f, 1f, 1f));

            // These are attributes defined in the "standard" vertex stream.
            StandardAttributes = new[]
            {
                new AttributeSpecification("Position",     3, AttributeType.Float, false, 0),
                new AttributeSpecification("UV",           2, AttributeType.Float, false, 12),
                new AttributeSpecification("Normal",       3, AttributeType.Float, false, 20),
            };

            // FIXME: Make error handling for shader that don't compile better!
            RenderDataUtil.CreateShaderProgram("Error vertex", ShaderStage.Vertex, new[] { ErrorVertexSource }, out ErrorVertexProgram);
            RenderDataUtil.CreateShaderProgram("Error fragment", ShaderStage.Fragment, new[] { ErrorFragmentSource }, out ErrorFragmentProgram);

            ErrorShaderPipeline = RenderDataUtil.CreateEmptyPipeline("Error pipeline");
            RenderDataUtil.AssembleProgramPipeline(ErrorShaderPipeline, ErrorVertexProgram, null, ErrorFragmentProgram);
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
    }
}
