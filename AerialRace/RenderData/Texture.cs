using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using System.Text;

namespace AerialRace.RenderData
{
    enum TextureFormat
    {
        R8,
        Rg8,
        Rgb8,
        Rgba8,

        R16F,
        R32F,
        RG16F,
        RG32F,

        RGB16F,
        RGB32F,
    }

    class Texture
    {
        public string Name;
        public int Handle;
        public TextureFormat Format;
        public int Width, Height, Depth;
        public int MipmapBaseLevel, MipmapMaxLevel;
        // TODO: We could add swizzle and stencil parameters

        public Texture(string name, int handle, TextureFormat format, int width, int height, int depth, int mipmapBaseLevel, int mipmapMaxLevel)
        {
            Name = name;
            Handle = handle;
            Format = format;
            Width = width;
            Height = height;
            Depth = depth;
            MipmapBaseLevel = mipmapBaseLevel;
            MipmapMaxLevel = mipmapMaxLevel;
        }
    }
}
