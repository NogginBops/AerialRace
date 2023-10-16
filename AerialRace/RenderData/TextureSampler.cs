using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;

namespace AerialRace.RenderData
{
    // ROBUSTNESS: 
    // We rely on the sampler types having the same values as the corresponding texture type.
    // - 2020-11-10
    enum SamplerType : int
    {
        Sampler1D   = TextureType.Texture1D,
        Sampler2D   = TextureType.Texture2D,
        Sampler3D   = TextureType.Texture3D,
        SamplerCube = TextureType.TextureCube,

        SamplerBuffer    = TextureType.TextureBuffer,

        Sampler1DArray   = TextureType.Texture1DArray,
        Sampler2DArray   = TextureType.Texture2DArray,
        SamplerCubeArray = TextureType.TextureCubeArray,

        Sampler2DMultisample      = TextureType.Texture2DMultisample,
        Sampler2DMultisampleArray = TextureType.Texture2DMultisampleArray,
    }

    enum SamplerDataType : int
    {
        UnsignedInt = 1,
        SignedInt   = 2,
        Float       = 3,
    }

    // ROBUSTNESS: 
    // We rely on the sampler types having the same values as the corresponding texture type.
    // - 2020-11-10
    enum ShadowSamplerType : int
    {
        Sampler1D   = TextureType.Texture1D,
        Sampler2D   = TextureType.Texture2D,
        SamplerCube = TextureType.TextureCube,

        Sampler1DArray   = TextureType.Texture1DArray,
        Sampler2DArray   = TextureType.Texture2DArray,
        SamplerCubeArray = TextureType.TextureCubeArray,
    }

    enum MagFilter : int
    {
        Nearest = 1,
        Linear  = 2,
    }

    enum MinFilter : int
    {
        Nearest = 1,
        Linear = 2,
        NearestMipmapNearest = 3,
        LinearMipmapNearest = 4,
        NearestMipmapLinear = 5,
        LinearMipmapLinear = 6,
    }

    enum WrapMode : int
    {
        Repeat = 1,
        MirroredRepeat = 2,
        ClampToEdge = 3,
        ClampToBorder = 4,
    }

    enum TextureCoordinate : int
    {
        S = 1,
        T = 2,
        R = 3,
    }

    enum DepthTextureCompareMode : int
    {
        RefToTexture = 1,
        None = 2,
    }

    enum DepthTextureCompareFunc : int
    {
        Less = 1,
        Greater = 2,
        LessThanOrEqual = 3,
        GreaterThanOrEqual = 4,
        Equal = 5,
        NotEqual = 6,
        Always = 7,
        Never = 8,
    }

    // We just have this interface so that we can keep an array of sampler and shadow samplers
    // - 2020-11-10
    interface ISampler
    {
        public string Name { get; }
        public int Handle { get; }
        public TextureType Type { get; }
    }

    class Sampler : ISampler
    {
        public string Name;
        public int Handle;

        string ISampler.Name => Name;
        int ISampler.Handle => Handle;
        TextureType ISampler.Type => (TextureType)Type;

        // Sampler objects don't actually have a type, 
        //but it might be usefull to keep track of what it's supposed to be
        public SamplerType Type;
        public SamplerDataType DataType;

        public MagFilter MagFilter;
        public MinFilter MinFilter;

        public float LODBias, LODMin, LODMax;

        // GLEXT:  EXT_texture_filter_anisotropic
        public float MaxAnisotropy;

        public WrapMode WrapModeS, WrapModeT, WrapModeR;
        public Color4<Rgba> BorderColor;

        // GLEXT: ARB_seamless_cubemap_per_texture
        public bool SeamlessCube;

        public Sampler(string name, int handle, SamplerType type, SamplerDataType dataType, MagFilter magFilter, MinFilter minFilter, float lodBias, float lodMin, float lodMax, float maxAnisotropy, WrapMode wrapModeS, WrapMode wrapModeT, WrapMode wrapModeR, Color4<Rgba> borderColor, bool seamlessCube)
        {
            Name = name;
            Handle = handle;
            Type = type;
            MagFilter = magFilter;
            MinFilter = minFilter;
            DataType = dataType;
            LODBias = lodBias;
            LODMin = lodMin;
            LODMax = lodMax;
            MaxAnisotropy = maxAnisotropy;
            WrapModeS = wrapModeS;
            WrapModeT = wrapModeT;
            WrapModeR = wrapModeR;
            BorderColor = borderColor;
            SeamlessCube = seamlessCube;
        }
    }

    class ShadowSampler : ISampler
    {
        public string Name;
        public int Handle;

        string ISampler.Name => Name;
        int ISampler.Handle => Handle;
        TextureType ISampler.Type => (TextureType)Type;

        // Sampler objects don't actually have a type, 
        //but it might be usefull to keep track of what it's supposed to be
        public ShadowSamplerType Type;

        public MagFilter MagFilter;
        public MinFilter MinFilter;

        public float LODBias, LODMin, LODMax;

        // GLEXT:  EXT_texture_filter_anisotropic
        public float MaxAnisotropy;

        public WrapMode WrapModeS, WrapModeT, WrapModeR;
        public Color4<Rgba>? BorderColor;

        public bool SeamlessCube;

        public DepthTextureCompareMode CompareMode;
        public DepthTextureCompareFunc CompareFunc;

        public ShadowSampler(string name, int handle, ShadowSamplerType type, MagFilter magFilter, MinFilter minFilter, float lodBias, float lodMin, float lodMax, float maxAnisotropy, WrapMode wrapModeS, WrapMode wrapModeT, WrapMode wrapModeR, Color4<Rgba>? borderColor, bool seamlessCube, DepthTextureCompareMode compareMode, DepthTextureCompareFunc compareFunc)
        {
            Name = name;
            Handle = handle;
            Type = type;
            MagFilter = magFilter;
            MinFilter = minFilter;
            LODBias = lodBias;
            LODMin = lodMin;
            LODMax = lodMax;
            MaxAnisotropy = maxAnisotropy;
            WrapModeS = wrapModeS;
            WrapModeT = wrapModeT;
            WrapModeR = wrapModeR;
            BorderColor = borderColor;
            SeamlessCube = seamlessCube;
            CompareMode = compareMode;
            CompareFunc = compareFunc;
        }
    }
}
