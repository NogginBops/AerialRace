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
        }
    }
}
