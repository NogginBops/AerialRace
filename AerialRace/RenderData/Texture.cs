using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using System.Text;

namespace AerialRace.RenderData
{
    enum TextureType : int
    {
        Texture1D = 1,
        Texture2D = 2,
        Texture3D = 3,
        TexutreCube = 4,

        TextureBuffer = 5,

        Texture1DArray = 6,
        Texture2DArray = 7,
        TexutreCubeArray = 9,

        Texture2DMultisample = 10,
        Texture2DMultisampleArray = 11,
    }

    enum TextureFormat : int
    {
        R8       = 1,
        R8Signed = 2,
        R8I      = 3,
        R8UI     = 4,

        Rg8       = 5,
        Rg8Signed = 6,
        Rg8I      = 7,
        Rg8UI     = 8,

        Rgb8       = 9,
        Rgb8Signed = 10,
        Rgb8I      = 11,
        Rgb8UI     = 12,

        Rgba8       = 13,
        Rgba8Signed = 14,
        Rgba8I      = 15,
        Rgba8UI     = 16,

        sRgb8      = 17,
        sRgba8     = 18,

        R16        = 19,
        R16Signed  = 20,
        R16F       = 21,
        R16I       = 22,
        R16UI      = 23,

        Rg16       = 24,
        Rg16Signed = 25,
        Rg16F      = 26,
        Rg16I      = 27,
        Rg16UI     = 28,

        Rgb16       = 29,
        Rgb16Signed = 30,
        Rgb16F      = 31,
        Rgb16I      = 32,
        Rgb16UI     = 33,

        Rgba16   = 34,
        // There is no GL_Rgba16_SNORM format
        Rgba16F  = 35,
        Rgba16I  = 36,
        Rgba16UI = 37,

        R32       = 38,
        R32Signed = 39,
        R32F      = 40,
        R32I      = 41,
        R32UI     = 42,

        Rg32       = 43,
        Rg32Signed = 44,
        Rg32F      = 45,
        Rg32I      = 46,
        Rg32UI     = 47,

        Rgb32       = 48,
        Rgb32Signed = 49,
        Rgb32F      = 50,
        Rgb32I      = 51,
        Rgb32UI     = 52,

        Rgb9_E5 = 53,
    }

    class Texture
    {
        public string Name;
        public int Handle;
        public TextureType Type;
        public TextureFormat Format;
        public int Width, Height, Depth;
        public int BaseLevel, MaxLevel;
        public int MipLevels;
        // TODO: We could add swizzle and stencil parameters

        public Texture(string name, int handle, TextureType type, TextureFormat format, int width, int height, int depth, int baseLevel, int maxLevel, int mipLevels)
        {
            Name = name;
            Handle = handle;
            Type = type;
            Format = format;
            Width = width;
            Height = height;
            Depth = depth;
            BaseLevel = baseLevel;
            MaxLevel = maxLevel;
            MipLevels = mipLevels;
        }
    }
}
