﻿using AerialRace.RenderData;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace AerialRace.Loading
{
    static class TextureLoader
    {
        public static Texture LoadRgbaImage(string name, string path, bool generateMipmap, bool srgb)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            // ImageSharp supports Jpeg, Png, Bmp, Gif, and Tga file formats.
            var image = Image.Load<Rgba32>(path);

            var loadTime = watch.ElapsedMilliseconds;

            var mipmapLevels = generateMipmap ?
                MathF.ILogB(Math.Max(image.Width, image.Height)) :
                1;

            var internalFormat = srgb ?
                SizedInternalFormat.Srgb8Alpha8 :
                SizedInternalFormat.Rgba8;

            GLUtil.CreateTexture(name, TextureTarget.Texture2d, out TextureHandle texture);
            GL.TextureStorage2D(texture, mipmapLevels, internalFormat, image.Width, image.Height);

            if (image.TryGetSinglePixelSpan(out Span<Rgba32> allData))
            {
                image.Mutate(x => x.Flip(FlipMode.Vertical));
                GL.TextureSubImage2D(texture, 0, 0, 0, image.Width, image.Height, PixelFormat.Rgba, (PixelType)All.UnsignedInt8888Rev, in allData[0]);
            }
            else
            {
                // Resort to doing this line by line.
                for (int i = 0; i < image.Height; i++)
                {
                    var row = image.GetPixelRowSpan(i);
                    GL.TextureSubImage2D(texture, 0, 0, image.Height - 1 - i, image.Width, 1, PixelFormat.Rgba, (PixelType)All.UnsignedInt8888Rev, in row[0]);
                }
            }

            if (generateMipmap) GL.GenerateTextureMipmap(texture);

            // glTextureStorage2D already sets TextureBaseLevel = 0 and TextureMaxLevel = mipmaplevels - 1
            // so we don't need to call anything here!
            // GL.TextureParameter(texture, TextureParameterName.TextureMaxLevel, mipmapLevels - 1);

            watch.Stop();
            Debug.WriteLine($"Loaded texture '{path}' in {watch.ElapsedMilliseconds}ms ({loadTime}ms load time)");

            return new Texture(name, texture, TextureType.Texture2D, TextureFormat.Rgba8, image.Width, image.Height, 1, 0, mipmapLevels - 1, mipmapLevels);
        }
    }
}
