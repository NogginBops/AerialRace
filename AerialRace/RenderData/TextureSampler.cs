using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace AerialRace.RenderData
{
    enum SamplerType
    {
        Sampler1D   = 1,
        Sampler2D   = 2,
        Sampler3D   = 3,
        SamplerCube = 4,

        Sampler1DArray   = 5,
        Sampler2DArray   = 6,
        SamplerCubeArray = 7,
        SamplerBuffer    = 8,

        Sampler2DMultisample      = 9,
        Sampler2DMultisampleArray = 10,
    }

    enum SamplerDataType
    {
        UnsignedInt,
        SignedInt,
        Float,
    }

    enum ShadowSamplerType
    {
        Sampler1D   = 1,
        Sampler2D   = 2,
        SamplerCube = 3,

        Sampler1DArray   = 4,
        Sampler2DArray   = 5,
        SamplerCubeArray = 6,
    }

    enum MagFilter
    {
        Nearest,
        Linear,
    }

    enum MinFilter
    {
        Nearest = 1,
        Linear = 2,
        NearestMipmapNearest = 3,
        LinearMipmapNearest = 4,
        NearestMipmapLinear = 5,
        LinearMipmapLinear = 6,
    }

    enum WrapMode
    {
        Repeat = 1,
        MirroredRepeat = 2,
        ClampToEdge = 3,
        ClampToBorder = 4,
    }

    enum TextureCoordinate
    {
        S = 1,
        T = 2,
        R = 3,
    }

    enum DepthTextureCompareMode
    {
        RefToTexture,
        None,
    }

    enum DepthTextureCompareFunc
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

    class Sampler
    {
        public int Handle;

        // Sampler objects don't actually have a type, 
        //but it might be usefull to keep track of what it's supposed to be
        public SamplerType Type;
        public SamplerDataType DataType;

        public int LODBias, LODMin, LODMax;

        // GLEXT:  EXT_texture_filter_anisotropic
        public int MaxAnisotropicLevel;

        public WrapMode WrapModeS, WrapModeT, WrapModeR;
        public Color4? BorderColor;

        public bool SeamlessCube;

        public Sampler(int handle, SamplerType type, SamplerDataType dataType, int lODBias, int lODMin, int lODMax, WrapMode wrapModeS, WrapMode wrapModeT, WrapMode wrapModeR, Color4? borderColor, bool seamlessCube)
        {
            Handle = handle;
            Type = type;
            DataType = dataType;
            LODBias = lODBias;
            LODMin = lODMin;
            LODMax = lODMax;
            WrapModeS = wrapModeS;
            WrapModeT = wrapModeT;
            WrapModeR = wrapModeR;
            BorderColor = borderColor;
            SeamlessCube = seamlessCube;
        }
    }

    class ShadowSampler
    {
        public int Handle;

        // Sampler objects don't actually have a type, 
        //but it might be usefull to keep track of what it's supposed to be
        public ShadowSamplerType Type;

        public int LODBias, LODMin, LODMax;

        // GLEXT:  EXT_texture_filter_anisotropic
        public int MaxAnisotropicLevel;

        public WrapMode WrapModeS, WrapModeT, WrapModeR;
        public Color4? BorderColor;

        public bool SeamlessCube;

        public DepthTextureCompareMode CompareMode;
        public DepthTextureCompareFunc CompareFunc;

        public ShadowSampler(int handle, ShadowSamplerType type, int lODBias, int lODMin, int lODMax, WrapMode wrapModeS, WrapMode wrapModeT, WrapMode wrapModeR, Color4? borderColor, bool seamlessCube, DepthTextureCompareMode compareMode, DepthTextureCompareFunc compareFunc)
        {
            Handle = handle;
            Type = type;
            LODBias = lODBias;
            LODMin = lODMin;
            LODMax = lODMax;
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
