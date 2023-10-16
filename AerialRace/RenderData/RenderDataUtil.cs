using AerialRace.Debugging;
using AerialRace.Loading;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Transactions;
using GLFrameBufferTarget = OpenTK.Graphics.OpenGL.FramebufferTarget;
using GLTextureAccess = OpenTK.Graphics.OpenGL.BufferAccessARB;

namespace AerialRace.RenderData
{
    // FIXME: Find a place for this
    [Flags] public enum ColorChannels : byte { None = 0, Red = 1, Green = 2, Blue = 4, Alpha = 8, All = 0x0F }

    [Flags]
    public enum ClearMask
    {
        Color = 1 << 0,
        Depth = 1 << 1,
        Stencil = 1 << 2,
    }

    public enum CullMode
    {
        None = 0,
        Front = 1,
        Back = 2,
        FrontAndBack = 3,
    }

    public enum DepthFunc
    {
        AlwaysPass = 0,
        NeverPass = 1,
        PassIfLessOrEqual = 2,
        PassIfEqual = 3,
    }

    public enum Primitive
    {
        Points = 1,
        Lines = 2,
        LineLoop = 3,
        LineStrip = 4,
        Triangles = 5,
        TriangleStrip = 6,
        TriangleFan = 7,

        // FIXME: Tesselation primitives?
    }

    static class RenderDataUtil
    {
        public static float MaxAnisoLevel { get; private set; }

        public static int MaxCombinedTextureUnits { get; private set; }

        public static int MaxSamples;
        public static int MaxColorTextureSamples;
        public static int MaxDepthStencilTextureSamples;

        public static void QueryLimits()
        {
            MaxAnisoLevel = GL.GetFloat((GetPName)All.MaxTextureMaxAnisotropy);

            MaxCombinedTextureUnits = GL.GetInteger(GetPName.MaxCombinedTextureImageUnits);

            MaxSamples = GL.GetInteger((GetPName)All.MaxSamples);
        }

        #region MISC

        public static TextureType ToTextureType(this SamplerType sType)
        {
            return (TextureType)sType;
        }

        public static TextureType ToTextureType(this ShadowSamplerType sType)
        {
            return (TextureType)sType;
        }

        public static int SizeInBytes(BufferDataType type) => type switch
        {
            BufferDataType.UInt8  => sizeof(byte),
            BufferDataType.UInt16 => sizeof(ushort),
            BufferDataType.UInt32 => sizeof(uint),
            BufferDataType.UInt64 => sizeof(ulong),

            BufferDataType.Int8  => sizeof(sbyte),
            BufferDataType.Int16 => sizeof(short),
            BufferDataType.Int32 => sizeof(int),
            BufferDataType.Int64 => sizeof(long),

            BufferDataType.Float  => sizeof(float),
            BufferDataType.Float2 => Unsafe.SizeOf<Vector2>(),
            BufferDataType.Float3 => Unsafe.SizeOf<Vector3>(),
            BufferDataType.Float4 => Unsafe.SizeOf<Vector4>(),

            _ => throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(BufferDataType)),
        };

        public static int SizeInBytes(IndexBufferType type) => type switch
        {
            IndexBufferType.UInt8 => sizeof(byte),
            IndexBufferType.UInt16 => sizeof(ushort),
            IndexBufferType.UInt32 => sizeof(uint),

            _ => throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(IndexBufferType)),
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BufferDataType GetAssiciatedBufferDataType<T>() where T : unmanaged => (default(T)) switch
        {
            byte _ => BufferDataType.UInt8,
            ushort _ => BufferDataType.UInt16,
            uint _ => BufferDataType.UInt32,
            ulong _ => BufferDataType.UInt64,

            sbyte _ => BufferDataType.Int8,
            short _ => BufferDataType.Int16,
            int _ => BufferDataType.Int32,
            long _ => BufferDataType.Int64,

            float _ => BufferDataType.Float,
            Vector2 _ => BufferDataType.Float2,
            Vector3 _ => BufferDataType.Float3,
            Vector4 _ => BufferDataType.Float4,

            Color4<Rgba> _ => BufferDataType.Float4,

            _ => BufferDataType.Custom,
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IndexBufferType GetAssiciatedIndexBufferType<T>() where T : unmanaged => (default(T)) switch
        {
            byte _ => IndexBufferType.UInt8,
            ushort _ => IndexBufferType.UInt16,
            uint _ => IndexBufferType.UInt32,

            _ => throw new ArgumentException($"No buffer data type that matches type: '{typeof(T)}'"),
        };

        public static BufferStorageMask ToGLStorageMask(BufferFlags flags)
        {
            BufferStorageMask result = default;

            if (flags.HasFlag(BufferFlags.MapRead))
                result |= BufferStorageMask.MapReadBit;

            if (flags.HasFlag(BufferFlags.MapWrite))
                result |= BufferStorageMask.MapWriteBit;

            if (flags.HasFlag(BufferFlags.MapPersistent))
                result |= BufferStorageMask.MapPersistentBit;

            if (flags.HasFlag(BufferFlags.Dynamic))
                result |= BufferStorageMask.DynamicStorageBit;

            return result;
        }

        // FIXME: Resolve the name conflict here?
        public static GLFrameBufferTarget ToGLFramebufferTraget(FramebufferTarget target) => target switch
        {
            FramebufferTarget.Read => GLFrameBufferTarget.ReadFramebuffer,
            FramebufferTarget.Draw => GLFrameBufferTarget.DrawFramebuffer,
            FramebufferTarget.ReadDraw => GLFrameBufferTarget.Framebuffer,
            _ => throw new InvalidEnumArgumentException(nameof(target), (int)target, typeof(FramebufferTarget)),
        };

        public static ShaderType ToGLShaderType(ShaderStage stage) => stage switch
        {
            ShaderStage.Vertex => ShaderType.VertexShader,
            ShaderStage.Geometry => ShaderType.GeometryShader,
            ShaderStage.Fragment => ShaderType.FragmentShader,

            ShaderStage.Compute => ShaderType.ComputeShader,

            _ => throw new InvalidEnumArgumentException(nameof(stage), (int)stage, typeof(ShaderStage)),
        };

        public static DrawElementsType ToGLDrawElementsType(IndexBufferType type) => type switch
        {
            IndexBufferType.UInt8 => DrawElementsType.UnsignedByte,
            IndexBufferType.UInt16 => DrawElementsType.UnsignedShort,
            IndexBufferType.UInt32 => DrawElementsType.UnsignedInt,

            _ => throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(IndexBufferType)),
        };

        public static TextureTarget ToGLTextureTarget(TextureType type) => type switch
        {
            TextureType.Texture1D => TextureTarget.Texture1d,
            TextureType.Texture2D => TextureTarget.Texture2d,
            TextureType.Texture3D => TextureTarget.Texture3d,
            TextureType.TextureCube => TextureTarget.TextureCubeMap,

            TextureType.TextureBuffer => TextureTarget.TextureBuffer,

            TextureType.Texture1DArray => TextureTarget.Texture1dArray,
            TextureType.Texture2DArray => TextureTarget.Texture2dArray,
            TextureType.TextureCubeArray => TextureTarget.TextureCubeMapArray,

            TextureType.Texture2DMultisample => TextureTarget.Texture2dMultisample,
            TextureType.Texture2DMultisampleArray => TextureTarget.Texture2dMultisampleArray,

            _ => throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(TextureType)),
        };

        public static VertexAttribType ToGLAttribType(AttributeType type) => type switch
        {
            AttributeType.UInt8 => VertexAttribType.UnsignedByte,
            AttributeType.UInt16 => VertexAttribType.UnsignedShort,
            AttributeType.UInt32 => VertexAttribType.UnsignedInt,

            AttributeType.Int8 => VertexAttribType.Byte,
            AttributeType.Int16 => VertexAttribType.Short,
            AttributeType.Int32 => VertexAttribType.Int,

            AttributeType.Half => VertexAttribType.HalfFloat,
            AttributeType.Float => VertexAttribType.Float,
            AttributeType.Double => VertexAttribType.Double,

            _ => throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(AttributeType)),
        };

        public static TextureWrapMode ToGLTextureWrapMode(WrapMode mode) => mode switch
        {
            WrapMode.Repeat => TextureWrapMode.Repeat,
            WrapMode.MirroredRepeat => TextureWrapMode.MirroredRepeat,
            WrapMode.ClampToEdge => TextureWrapMode.ClampToEdge,
            WrapMode.ClampToBorder => TextureWrapMode.ClampToBorder,

            _ => throw new InvalidEnumArgumentException(nameof(mode), (int)mode, typeof(WrapMode)),
        };

        public static TextureMagFilter ToGLTextureMagFilter(MagFilter filter) => filter switch
        {
            MagFilter.Nearest => TextureMagFilter.Nearest,
            MagFilter.Linear  => TextureMagFilter.Linear,

            _ => throw new InvalidEnumArgumentException(nameof(filter), (int)filter, typeof(MinFilter)),
        };

        public static TextureMinFilter ToGLTextureMinFilter(MinFilter filter) => filter switch
        {
            MinFilter.Nearest => TextureMinFilter.Nearest,
            MinFilter.Linear  => TextureMinFilter.Linear,

            MinFilter.NearestMipmapNearest => TextureMinFilter.NearestMipmapNearest,
            MinFilter.LinearMipmapNearest  => TextureMinFilter.LinearMipmapNearest,
            MinFilter.NearestMipmapLinear  => TextureMinFilter.NearestMipmapLinear,
            MinFilter.LinearMipmapLinear   => TextureMinFilter.LinearMipmapLinear,

            _ => throw new InvalidEnumArgumentException(nameof(filter), (int)filter, typeof(MinFilter)),
        };

        public static SizedInternalFormat ToGLSizedInternalFormat(TextureFormat format) => format switch
        {
            TextureFormat.R8 => SizedInternalFormat.R8,
            TextureFormat.R8Signed => SizedInternalFormat.R8Snorm,
            TextureFormat.R8I => SizedInternalFormat.R8i,
            TextureFormat.R8UI => SizedInternalFormat.R8ui,
            TextureFormat.Rg8 => SizedInternalFormat.Rg8,
            TextureFormat.Rg8Signed => SizedInternalFormat.Rg8Snorm,
            TextureFormat.Rg8I => SizedInternalFormat.Rg8i,
            TextureFormat.Rg8UI => SizedInternalFormat.Rg8ui,
            TextureFormat.Rgb8 => SizedInternalFormat.Rgb8,
            TextureFormat.Rgb8Signed => SizedInternalFormat.Rgb8Snorm,
            TextureFormat.Rgb8I => SizedInternalFormat.Rgba8i,
            TextureFormat.Rgb8UI => SizedInternalFormat.Rgba8ui,
            TextureFormat.Rgba8 => SizedInternalFormat.Rgba8,
            TextureFormat.Rgba8Signed => SizedInternalFormat.Rgba8Snorm,
            TextureFormat.Rgba8I => SizedInternalFormat.Rgba8i,
            TextureFormat.Rgba8UI => SizedInternalFormat.Rgba8ui,
            TextureFormat.sRgb8 => SizedInternalFormat.Srgb8,
            TextureFormat.sRgba8 => SizedInternalFormat.Srgb8Alpha8,
            TextureFormat.R16 => SizedInternalFormat.R16,
            TextureFormat.R16Signed => SizedInternalFormat.R16Snorm,
            TextureFormat.R16F => SizedInternalFormat.R16f,
            TextureFormat.R16I => SizedInternalFormat.R16i,
            TextureFormat.R16UI => SizedInternalFormat.R16ui,
            TextureFormat.Rg16 => SizedInternalFormat.Rg16,
            TextureFormat.Rg16Signed => SizedInternalFormat.Rg16Snorm,
            TextureFormat.Rg16F => SizedInternalFormat.Rg16f,
            TextureFormat.Rg16I => SizedInternalFormat.Rg16i,
            TextureFormat.Rg16UI => SizedInternalFormat.Rg16ui,
            TextureFormat.Rgb16Signed => SizedInternalFormat.Rgb16Snorm,
            TextureFormat.Rgb16F => SizedInternalFormat.Rgb16f,
            TextureFormat.Rgb16I => SizedInternalFormat.Rgb16i,
            TextureFormat.Rgb16UI => SizedInternalFormat.Rgb16ui,
            TextureFormat.Rgba16 => SizedInternalFormat.Rgba16,
            TextureFormat.Rgba16F => SizedInternalFormat.Rgba16f,
            TextureFormat.Rgba16I => SizedInternalFormat.Rgba16i,
            TextureFormat.Rgba16UI => SizedInternalFormat.Rgba16ui,
            TextureFormat.R32F => SizedInternalFormat.R32f,
            TextureFormat.R32I => SizedInternalFormat.R32i,
            TextureFormat.R32UI => SizedInternalFormat.R32ui,
            TextureFormat.Rg32F => SizedInternalFormat.Rg32f,
            TextureFormat.Rg32I => SizedInternalFormat.Rg32i,
            TextureFormat.Rg32UI => SizedInternalFormat.Rg32ui,
            TextureFormat.Rgb32F => SizedInternalFormat.Rgb32f,
            TextureFormat.Rgb32I => SizedInternalFormat.Rgb32i,
            TextureFormat.Rgb32UI => SizedInternalFormat.Rgb32ui,
            TextureFormat.Rgba32F => SizedInternalFormat.Rgba32f,
            TextureFormat.Rgba32I => SizedInternalFormat.Rgba32i,
            TextureFormat.Rgba32UI => SizedInternalFormat.Rgba32ui,
            TextureFormat.Rgb9_E5 => SizedInternalFormat.Rgb9E5,
            TextureFormat.Depth16 => (SizedInternalFormat)All.DepthComponent16,
            TextureFormat.Depth24 => (SizedInternalFormat)All.DepthComponent24,
            TextureFormat.Depth32F => (SizedInternalFormat)All.DepthComponent32f,
            TextureFormat.Depth24Stencil8 => (SizedInternalFormat)All.Depth24Stencil8,
            TextureFormat.Depth32FStencil8 => (SizedInternalFormat)All.Depth32fStencil8,
            TextureFormat.Stencil8 => (SizedInternalFormat)All.StencilIndex8,
            _ => throw new InvalidEnumArgumentException(nameof(format), (int)format, typeof(TextureFormat)),
        };

        public static TextureCompareMode ToGLTextureCompareMode(DepthTextureCompareMode mode) => mode switch
        {
            DepthTextureCompareMode.RefToTexture => TextureCompareMode.CompareRefToTexture,
            DepthTextureCompareMode.None => TextureCompareMode.None,
            
            _ => throw new InvalidEnumArgumentException(nameof(mode), (int)mode, typeof(DepthTextureCompareMode)),
        };

        // We are using an empty enum definition for now until we get a TextureCompareFunc enum in the binder.
        public enum TextureCompareFunc : int { }
        public static TextureCompareFunc ToGLTextureCompareFunc(DepthTextureCompareFunc func) => func switch
        {
            DepthTextureCompareFunc.Less => (TextureCompareFunc)All.Less,
            DepthTextureCompareFunc.Greater => (TextureCompareFunc)All.Greater,
            DepthTextureCompareFunc.LessThanOrEqual => (TextureCompareFunc)All.Lequal,
            DepthTextureCompareFunc.GreaterThanOrEqual => (TextureCompareFunc)All.Gequal,
            DepthTextureCompareFunc.Equal => (TextureCompareFunc)All.Equal,
            DepthTextureCompareFunc.NotEqual => (TextureCompareFunc)All.Notequal,
            DepthTextureCompareFunc.Always => (TextureCompareFunc)All.Always,
            DepthTextureCompareFunc.Never => (TextureCompareFunc)All.Never,

            _ => throw new InvalidEnumArgumentException(nameof(func), (int)func, typeof(DepthTextureCompareFunc)),
        };

        public static DepthFunction ToGLDepthFunction(DepthFunc func) => func switch
        {
            DepthFunc.AlwaysPass => DepthFunction.Always,
            DepthFunc.NeverPass => DepthFunction.Never,
            DepthFunc.PassIfLessOrEqual => DepthFunction.Lequal,
            DepthFunc.PassIfEqual => DepthFunction.Equal,

            _ => throw new InvalidEnumArgumentException(nameof(func), (int)func, typeof(DepthFunc)),
        };

        public static ClearBufferMask ToGLClearBufferMask(ClearMask mask)
        {
            ClearBufferMask result = default;

            if (mask.HasFlag(ClearMask.Color))
                result |= ClearBufferMask.ColorBufferBit;

            if (mask.HasFlag(ClearMask.Depth))
                result |= ClearBufferMask.DepthBufferBit;

            if (mask.HasFlag(ClearMask.Stencil))
                result |= ClearBufferMask.StencilBufferBit;

            return result;
        }

        public static PrimitiveType ToGLPrimitiveType(Primitive primitive) => primitive switch
        {
            Primitive.Points => PrimitiveType.Points,
            Primitive.Lines => PrimitiveType.Lines,
            Primitive.LineLoop => PrimitiveType.LineLoop,
            Primitive.LineStrip => PrimitiveType.LineStrip,
            Primitive.Triangles => PrimitiveType.Triangles,
            Primitive.TriangleStrip => PrimitiveType.TriangleStrip,
            Primitive.TriangleFan => PrimitiveType.TriangleFan,

            _ => throw new InvalidEnumArgumentException(nameof(primitive), (int)primitive, typeof(Primitive)),
        };

        public static QueryTarget ToGLQueryTarget(QueryType type) => type switch
        {
            QueryType.TimeElapsed => QueryTarget.TimeElapsed,
            QueryType.Timestamp => (QueryTarget)QueryCounterTarget.Timestamp,
            _ => throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(QueryType)),
        };

        public static bool IsMultisampleType(TextureType type)
        {
            return type switch
            {
                TextureType.Texture2DMultisample => true,
                TextureType.Texture2DMultisampleArray => true,
                _ => false,
            };
        }

        public static GLTextureAccess ToGLTextureAccess(TextureAccess textureAccess)
        {
            return textureAccess switch
            {
                TextureAccess.ReadWrite => GLTextureAccess.ReadWrite,
                TextureAccess.ReadOnly => GLTextureAccess.ReadOnly,
                TextureAccess.WriteOnly => GLTextureAccess.WriteOnly,
                _ => throw new InvalidEnumArgumentException(nameof(textureAccess), (int)textureAccess, typeof(TextureAccess)),
            };
        }

        #endregion

        #region Creation 

        public static Buffer CreateDataBuffer<T>(string name, Span<T> data, BufferFlags flags) where T : unmanaged
        {
            GLUtil.CreateBuffer(name, out int Handle);

            BufferDataType bufferType = GetAssiciatedBufferDataType<T>();

            int elementSize = Unsafe.SizeOf<T>();
            if (bufferType != BufferDataType.Custom) 
                Debug.Assert(SizeInBytes(bufferType) == Unsafe.SizeOf<T>());

            // GLEXT: ARB_direct_access
            BufferStorageMask glFlags = ToGLStorageMask(flags);
            GL.NamedBufferStorage(Handle, (ReadOnlySpan<T>)data, glFlags);

            return new Buffer(name, Handle, bufferType, elementSize, data.Length, flags);
        }

        public static Buffer CreateDataBuffer<T>(string name, int elements, BufferFlags flags) where T : unmanaged
        {
            GLUtil.CreateBuffer(name, out int Handle);

            BufferDataType bufferType = GetAssiciatedBufferDataType<T>();

            int elementSize = Unsafe.SizeOf<T>();
            if (bufferType != BufferDataType.Custom)
                Debug.Assert(SizeInBytes(bufferType) == Unsafe.SizeOf<T>());

            BufferStorageMask glFlags = ToGLStorageMask(flags);
            // GLEXT: ARB_direct_access
            GL.NamedBufferStorage(Handle, elements * elementSize, IntPtr.Zero, glFlags);

            return new Buffer(name, Handle, bufferType, elementSize, elements, flags);
        }

        public static Buffer CreateDataBuffer(string name, int bytes, BufferFlags flags)
        {
            GLUtil.CreateBuffer(name, out int Handle);

            BufferDataType bufferType = BufferDataType.Custom;

            BufferStorageMask glFlags = ToGLStorageMask(flags);
            // GLEXT: ARB_direct_access
            GL.NamedBufferStorage(Handle, bytes, IntPtr.Zero, glFlags);

            return new Buffer(name, Handle, bufferType, -1, bytes, flags);
        }

        public static Buffer CreateDataBuffer(string name, int elements, int elementSize, BufferFlags flags)
        {
            GLUtil.CreateBuffer(name, out int Handle);

            BufferDataType bufferType = BufferDataType.Custom;

            BufferStorageMask glFlags = ToGLStorageMask(flags);
            var bytes = elements * elementSize;

            // GLEXT: ARB_direct_access
            GL.NamedBufferStorage(Handle, bytes, IntPtr.Zero, glFlags);
            
            return new Buffer(name, Handle, bufferType, elementSize, bytes, flags);
        }

        public static IndexBuffer CreateIndexBuffer(string name, Span<byte> data, BufferFlags flags)
        {
            GLUtil.CreateBuffer(name, out int Handle);

            IndexBufferType bufferType = GetAssiciatedIndexBufferType<byte>();

            Debug.Assert(SizeInBytes(bufferType) == Unsafe.SizeOf<byte>());

            // GLEXT: ARB_direct_access
            BufferStorageMask glFlags = ToGLStorageMask(flags);
            GL.NamedBufferStorage(Handle, (ReadOnlySpan<byte>)data, glFlags);

            return new IndexBuffer(name, Handle, bufferType, sizeof(byte), data.Length, flags);
        }

        public static IndexBuffer CreateIndexBuffer(string name, Span<short> data, BufferFlags flags)
        {
            GLUtil.CreateBuffer(name, out int Handle);

            IndexBufferType bufferType = GetAssiciatedIndexBufferType<ushort>();

            Debug.Assert(SizeInBytes(bufferType) == Unsafe.SizeOf<ushort>());

            // GLEXT: ARB_direct_access
            BufferStorageMask glFlags = ToGLStorageMask(flags);
            GL.NamedBufferStorage(Handle, (ReadOnlySpan<short>)data, glFlags);

            return new IndexBuffer(name, Handle, bufferType, sizeof(ushort), data.Length, flags);
        }

        public static IndexBuffer CreateIndexBuffer(string name, Span<ushort> data, BufferFlags flags)
        {
            GLUtil.CreateBuffer(name, out int Handle);

            IndexBufferType bufferType = GetAssiciatedIndexBufferType<ushort>();

            Debug.Assert(SizeInBytes(bufferType) == Unsafe.SizeOf<ushort>());

            // GLEXT: ARB_direct_access
            BufferStorageMask glFlags = ToGLStorageMask(flags);
            GL.NamedBufferStorage(Handle, (ReadOnlySpan<ushort>)data, glFlags);

            return new IndexBuffer(name, Handle, bufferType, sizeof(ushort), data.Length, flags);
        }

        public static IndexBuffer CreateIndexBuffer(string name, Span<int> data, BufferFlags flags)
        {
            GLUtil.CreateBuffer(name, out int Handle);

            IndexBufferType bufferType = GetAssiciatedIndexBufferType<uint>();

            Debug.Assert(SizeInBytes(bufferType) == Unsafe.SizeOf<uint>());

            // GLEXT: ARB_direct_access
            BufferStorageMask glFlags = ToGLStorageMask(flags);
            GL.NamedBufferStorage(Handle, (ReadOnlySpan<int>)data, glFlags);

            return new IndexBuffer(name, Handle, bufferType, sizeof(uint), data.Length, flags);
        }

        public static IndexBuffer CreateIndexBuffer(string name, Span<uint> data, BufferFlags flags)
        {
            GLUtil.CreateBuffer(name, out int Handle);

            IndexBufferType bufferType = GetAssiciatedIndexBufferType<uint>();

            Debug.Assert(SizeInBytes(bufferType) == Unsafe.SizeOf<uint>());

            // GLEXT: ARB_direct_access
            BufferStorageMask glFlags = ToGLStorageMask(flags);
            GL.NamedBufferStorage(Handle, (ReadOnlySpan<uint>)data, glFlags);

            return new IndexBuffer(name, Handle, bufferType, sizeof(uint), data.Length, flags);
        }

        public static IndexBuffer CreateIndexBuffer<T>(string name, int elements, BufferFlags flags) where T : unmanaged
        {
            GLUtil.CreateBuffer(name, out int Handle);

            IndexBufferType bufferType = GetAssiciatedIndexBufferType<T>();

            var elementSize = SizeInBytes(bufferType);
            Debug.Assert(elementSize == Unsafe.SizeOf<T>());

            // GLEXT: ARB_direct_access
            BufferStorageMask glFlags = ToGLStorageMask(flags);
            GL.NamedBufferStorage(Handle, elements * Unsafe.SizeOf<T>(), IntPtr.Zero, glFlags);

            return new IndexBuffer(name, Handle, bufferType, elementSize, elements, flags);
        }

        public static UniformBuffer<T> CreateUniformBuffer<T>(string name, BufferFlags flags)
            where T : unmanaged
        {
            GLUtil.CreateBuffer(name, out int handle);

            GL.NamedBufferStorage(handle, Unsafe.SizeOf<T>(), IntPtr.Zero, ToGLStorageMask(flags));

            return new UniformBuffer<T>(name, handle, flags);
        }

        public static Framebuffer CreateEmptyFramebuffer(string name)
        {
            GLUtil.CreateFramebuffer(name, out int fbo);

            return new Framebuffer(name, fbo, null, null, null);
        }

        // FIXME: Here we are "leaking" a opengl enum...
        public static FramebufferStatus CheckFramebufferComplete(Framebuffer fbo, FramebufferTarget target)
        {
            return GL.CheckNamedFramebufferStatus(fbo.Handle, ToGLFramebufferTraget(target));
        }

        public static void AddColorAttachment(Framebuffer fbo, Texture colorAttachment, int index, int mipLevel)
        {
            GL.NamedFramebufferTexture(fbo.Handle, (FramebufferAttachment)((int)FramebufferAttachment.ColorAttachment0 + index), colorAttachment.Handle, mipLevel);

            var attachment = new ColorAttachement(index, mipLevel, colorAttachment);

            // First we check if this index already exists for this framebuffer
            bool addNew = true;
            for (int i = 0; i < fbo.ColorAttachments?.Length; i++)
            {
                if (fbo.ColorAttachments[i].Index == index)
                {
                    fbo.ColorAttachments[i] = attachment;
                    addNew = false;
                }
            }

            if (addNew)
            {
                // Extend the array, add the attachment, sort the attachments by index
                fbo.ColorAttachments ??= Array.Empty<ColorAttachement>();
                Array.Resize(ref fbo.ColorAttachments, fbo.ColorAttachments.Length + 1);
                fbo.ColorAttachments[^1] = attachment;
                Array.Sort(fbo.ColorAttachments, (a1, a2) => a1.Index - a2.Index);
            }
        }

        public static void AddDepthAttachment(Framebuffer fbo, Texture depthTexture, int mipLevel)
        {
            GL.NamedFramebufferTexture(fbo.Handle, FramebufferAttachment.DepthAttachment, depthTexture.Handle, mipLevel);
            fbo.DepthAttachment = new FramebufferAttachmentTexture(depthTexture, mipLevel, 0);
        }

        public static void AddDepthLayerAttachment(Framebuffer fbo, Texture depthTexture, int mipLevel, int layer)
        {
            GL.NamedFramebufferTextureLayer(fbo.Handle, FramebufferAttachment.DepthAttachment, depthTexture.Handle, mipLevel, layer);
            fbo.DepthAttachment = new FramebufferAttachmentTexture(depthTexture, mipLevel, layer);
        }

        public static void AddStencilAttachment(Framebuffer fbo, Texture stencilTexture, int mipLevel)
        {
            GL.NamedFramebufferTexture(fbo.Handle, FramebufferAttachment.StencilAttachment, stencilTexture.Handle, mipLevel);
            fbo.StencilAttachment = new FramebufferAttachmentTexture(stencilTexture, mipLevel, 0);
        }

        public static void AddDepthStencilAttachment(Framebuffer fbo, Texture depthStencilTexture, int mipLevel)
        {
            GL.NamedFramebufferTexture(fbo.Handle, FramebufferAttachment.DepthStencilAttachment, depthStencilTexture.Handle, mipLevel);
            var depthStencil = new FramebufferAttachmentTexture(depthStencilTexture, mipLevel, 0);
            fbo.DepthAttachment = depthStencil;
            fbo.StencilAttachment = depthStencil;
        }
        
        /*
        // FIXME: Make error handling for shader that don't compile better!
        public static ShaderProgram CreateShaderProgram(string name, ShaderStage stage, string source)
        {
            GLUtil.CreateProgram(name, out int handle);
            GL.ProgramParameter(handle, ProgramParameterName.ProgramSeparable, 1);

            //program = new ShaderProgram(name, handle, stage, new Dictionary<string, int>(), new Dictionary<string, int>(), null, null);

            GLUtil.CreateShader(name, ToGLShaderType(stage), out var shader);
            GL.ShaderSource(shader, source);

            GL.CompileShader(shader);

            GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                string info = GL.GetShaderInfoLog(shader);
                Debug.WriteLine($"Error in {stage} shader '{name}':\n{info}");

                // Do some gl cleanup
                GL.DeleteProgram(handle);
                GL.DeleteShader(shader);

                // FIXME: return null?
                throw new Exception();
            }

            GL.AttachShader(handle, shader);
            GL.LinkProgram(handle);

            GL.DetachShader(handle, shader);
            GL.DeleteShader(shader);

            GL.GetProgram(handle, GetProgramParameterName.LinkStatus, out int isLinked);
            if (isLinked == 0)
            {
                string info = GL.GetProgramInfoLog(handle);
                Debug.WriteLine($"Error in {stage} program '{name}':\n{info}");

                GL.DeleteProgram(handle);
                throw new Exception();
            }

            // Now we can inspect this shader!

            //Debug.WriteLine($"Uniforms for shader '{name}':");
            //Debug.Indent();

            Dictionary<string, int> uniformLocations = new Dictionary<string, int>();

            UniformFieldInfo[] uniformInfo;
            {
                GL.GetProgram(handle, GetProgramParameterName.ActiveUniforms, out int uniformCount);

                uniformInfo = new UniformFieldInfo[uniformCount];

                for (int i = 0; i < uniformCount; i++)
                {
                    string uniformName = GL.GetActiveUniform(handle, i, out int size, out ActiveUniformType type);
                    var location = GL.GetUniformLocation(handle, uniformName);

                    UniformFieldInfo fieldInfo;
                    fieldInfo.Location = location;
                    fieldInfo.Name = uniformName;
                    fieldInfo.Size = size;
                    fieldInfo.Type = type;

                    uniformInfo[i] = fieldInfo;
                    uniformLocations.Add(uniformName, location);

                    //Debug.WriteLine($"{name} uniform {location} '{uniformName}' {type} ({size})");
                }
            }

            Dictionary<string, int> uniformBlockBindings = new Dictionary<string, int>();

            UniformBlockInfo[] blockInfo;
            {
                GL.GetProgram(handle, GetProgramParameterName.ActiveUniformBlocks, out int uniformBlockCount);

                blockInfo = new UniformBlockInfo[uniformBlockCount];

                for (int i = 0; i < uniformBlockCount; i++)
                {
                    GL.GetActiveUniformBlock(handle, i, ActiveUniformBlockParameter.UniformBlockActiveUniforms, out int uniformsInBlockCount);

                    Span<int> uniformIndices = stackalloc int[uniformsInBlockCount];
                    GL.GetActiveUniformBlock(handle, i, ActiveUniformBlockParameter.UniformBlockActiveUniformIndices, out uniformIndices[0]);

                    GL.GetActiveUniformBlock(handle, i, ActiveUniformBlockParameter.UniformBlockNameLength, out int nameLength);
                    GL.GetActiveUniformBlockName(handle, i, nameLength, out _, out string uniformBlockName);

                    int blockIndex = GL.GetUniformBlockIndex(handle, uniformBlockName);

                    blockInfo[i].Name = uniformBlockName;
                    blockInfo[i].Index = blockIndex;
                    blockInfo[i].Members = new UniformFieldInfo[uniformIndices.Length];

                    uniformBlockBindings.Add(uniformBlockName, blockIndex);

                    //Debug.WriteLine($"Block {i} '{uniformBlockName}':");
                    //Debug.Indent();
                    var blockMember = blockInfo[i].Members;
                    for (int j = 0; j < uniformIndices.Length; j++)
                    {
                        string uniformName = GL.GetActiveUniform(handle, uniformIndices[j], out int uniformSize, out var uniformType);

                        blockMember[j].Location = uniformIndices[j];
                        blockMember[j].Name = uniformName;
                        blockMember[j].Size = uniformSize;
                        blockMember[j].Type = uniformType;

                        //Debug.WriteLine($"{name} uniform {uniformIndices[j]} '{uniformName}' {uniformType} ({uniformSize})");
                    }
                    //Debug.Unindent();
                }
                //Debug.Unindent();
            }

            return new ShaderProgram(name, handle, stage, uniformLocations, uniformBlockBindings, uniformInfo, blockInfo);
        }

        public static ShaderPipeline CreateEmptyPipeline(string name)
        {
            GLUtil.CreateProgramPipeline(name, out int pipeline);
            return new ShaderPipeline(name, pipeline, null, null, null);
        }

        public static bool AssembleProgramPipeline(ShaderPipeline pipeline, ShaderProgram? vertex, ShaderProgram? geometry, ShaderProgram? fragment)
        {
            if (vertex != null)
                GL.UseProgramStages(pipeline.Handle, ProgramStageMask.VertexShaderBit, vertex.Handle);
            pipeline.VertexProgram = vertex;

            if (geometry != null)
                GL.UseProgramStages(pipeline.Handle, ProgramStageMask.GeometryShaderBit, geometry.Handle);
            pipeline.GeometryProgram = geometry;

            if (fragment != null)
                GL.UseProgramStages(pipeline.Handle, ProgramStageMask.FragmentShaderBit, fragment.Handle);
            pipeline.FramgmentProgram = fragment;

            GL.ValidateProgramPipeline(pipeline.Handle);
            GL.GetProgramPipeline(pipeline.Handle, ProgramPipelineParameter.ValidateStatus, out int valid);
            if (valid == 0)
            {
                GL.GetProgramPipeline(pipeline.Handle, ProgramPipelineParameter.InfoLogLength, out int logLength);
                GL.GetProgramPipelineInfoLog(pipeline.Handle, logLength, out _, out string info);

                Debug.WriteLine($"Error in program pipeline '{pipeline.Name}':\n{info}");

                // FIXME: Consider using this for debug!!
                // GL.DebugMessageInsert()
                return false;
            }
            else
            {
                return true;
            }
        }

        public static ShaderPipeline CreatePipeline(string name, ShaderProgram? vertex, ShaderProgram? geometry, ShaderProgram? fragment)
        {
            GLUtil.CreateProgramPipeline(name, out var handle);

            var pipeline = new ShaderPipeline(name, handle, null, null, null);

            if (vertex != null)
                GL.UseProgramStages(pipeline.Handle, ProgramStageMask.VertexShaderBit, vertex.Handle);
            pipeline.VertexProgram = vertex;

            if (geometry != null)
                GL.UseProgramStages(pipeline.Handle, ProgramStageMask.GeometryShaderBit, geometry.Handle);
            pipeline.GeometryProgram = geometry;

            if (fragment != null)
                GL.UseProgramStages(pipeline.Handle, ProgramStageMask.FragmentShaderBit, fragment.Handle);
            pipeline.FramgmentProgram = fragment;

            GL.ValidateProgramPipeline(pipeline.Handle);
            GL.GetProgramPipeline(pipeline.Handle, ProgramPipelineParameter.ValidateStatus, out int valid);
            if (valid == 0)
            {
                GL.GetProgramPipeline(pipeline.Handle, ProgramPipelineParameter.InfoLogLength, out int logLength);
                GL.GetProgramPipelineInfoLog(pipeline.Handle, logLength, out _, out string info);

                Debug.WriteLine($"Error in program pipeline '{pipeline.Name}':\n{info}");

                // FIXME: Consider using this for debug!!
                // GL.DebugMessageInsert()
                throw new Exception();
            }

            return pipeline;
        }

        public static ShaderPipeline CreatePipeline(string name, string vertexPath, string fragmentPath)
        {
            var vertProgram = CreateShaderProgram($"{name}: Vertex", ShaderStage.Vertex, File.ReadAllText(vertexPath));
            var fragProgram = CreateShaderProgram($"{name}: Fragment", ShaderStage.Fragment, File.ReadAllText(fragmentPath));
            return CreatePipeline(name, vertProgram, null, fragProgram);
        }
        */

        public static Sampler CreateSampler2D(string name, MagFilter magFilter, MinFilter minFilter, float anisoLevel, WrapMode xAxisWrap, WrapMode yAxisWrap)
        {
            GLUtil.CreateSampler(name, out int sampler);
            
            GL.SamplerParameteri(sampler, SamplerParameterI.TextureMagFilter, (int)ToGLTextureMagFilter(magFilter));
            GL.SamplerParameteri(sampler, SamplerParameterI.TextureMinFilter, (int)ToGLTextureMinFilter(minFilter));

            GL.SamplerParameteri(sampler, SamplerParameterI.TextureWrapS, (int)ToGLTextureWrapMode(xAxisWrap));
            GL.SamplerParameteri(sampler, SamplerParameterI.TextureWrapS, (int)ToGLTextureWrapMode(yAxisWrap));

            GL.SamplerParameterf(sampler, SamplerParameterF.TextureMaxAnisotropy, MathHelper.Clamp(anisoLevel, 1f, MaxAnisoLevel));
            
            return new Sampler(name, sampler, SamplerType.Sampler2D, SamplerDataType.Float, magFilter, minFilter, 0, -1000, 1000, 1.0f, xAxisWrap, yAxisWrap, WrapMode.Repeat, new Color4<Rgba>(0f, 0f, 0f, 0f), false);
        }

        public static Sampler CreateSampler2DMultisample(string name, MagFilter magFilter, MinFilter minFilter, float anisoLevel, WrapMode xAxisWrap, WrapMode yAxisWrap)
        {
            GLUtil.CreateSampler(name, out int sampler);

            GL.SamplerParameteri(sampler, SamplerParameterI.TextureMagFilter, (int)ToGLTextureMagFilter(magFilter));
            GL.SamplerParameteri(sampler, SamplerParameterI.TextureMinFilter, (int)ToGLTextureMinFilter(minFilter));

            GL.SamplerParameteri(sampler, SamplerParameterI.TextureWrapS, (int)ToGLTextureWrapMode(xAxisWrap));
            GL.SamplerParameteri(sampler, SamplerParameterI.TextureWrapS, (int)ToGLTextureWrapMode(yAxisWrap));

            GL.SamplerParameterf(sampler, SamplerParameterF.TextureMaxAnisotropy, MathHelper.Clamp(anisoLevel, 1f, MaxAnisoLevel));

            return new Sampler(name, sampler, SamplerType.Sampler2DMultisample, SamplerDataType.Float, magFilter, minFilter, 0, -1000, 1000, 1.0f, xAxisWrap, yAxisWrap, WrapMode.Repeat, new Color4<Rgba>(0f, 0f, 0f, 0f), false);
        }

        public static ShadowSampler CreateShadowSampler2D(string name, MagFilter magFilter, MinFilter minFilter, float anisoLevel, WrapMode xAxisWrap, WrapMode yAxisWrap, DepthTextureCompareMode depthCompMode, DepthTextureCompareFunc depthCompFunc)
        {
            GLUtil.CreateSampler(name, out int sampler);

            GL.SamplerParameteri(sampler, SamplerParameterI.TextureMagFilter, (int)ToGLTextureMagFilter(magFilter));
            GL.SamplerParameteri(sampler, SamplerParameterI.TextureMinFilter, (int)ToGLTextureMinFilter(minFilter));

            GL.SamplerParameteri(sampler, SamplerParameterI.TextureWrapS, (int)ToGLTextureWrapMode(xAxisWrap));
            GL.SamplerParameteri(sampler, SamplerParameterI.TextureWrapS, (int)ToGLTextureWrapMode(yAxisWrap));

            GL.SamplerParameterf(sampler, SamplerParameterF.TextureMaxAnisotropy, MathHelper.Clamp(anisoLevel, 1f, MaxAnisoLevel));

            GL.SamplerParameteri(sampler, SamplerParameterI.TextureCompareMode, (int)ToGLTextureCompareMode(depthCompMode));
            GL.SamplerParameteri(sampler, SamplerParameterI.TextureCompareFunc, (int)ToGLTextureCompareFunc(depthCompFunc));

            return new ShadowSampler(name, sampler, ShadowSamplerType.Sampler2D, magFilter, minFilter, 0, -1000, 1000, anisoLevel, xAxisWrap, yAxisWrap, WrapMode.Repeat, new Color4<Rgba>(0f, 0f, 0f, 0f), false, depthCompMode, depthCompFunc);
        }

        public static ShadowSampler CreateShadowSampler2DArray(string name, MagFilter magFilter, MinFilter minFilter, float anisoLevel, WrapMode xAxisWrap, WrapMode yAxisWrap, DepthTextureCompareMode depthCompMode, DepthTextureCompareFunc depthCompFunc)
        {
            GLUtil.CreateSampler(name, out int sampler);

            GL.SamplerParameteri(sampler, SamplerParameterI.TextureMagFilter, (int)ToGLTextureMagFilter(magFilter));
            GL.SamplerParameteri(sampler, SamplerParameterI.TextureMinFilter, (int)ToGLTextureMinFilter(minFilter));

            GL.SamplerParameteri(sampler, SamplerParameterI.TextureWrapS, (int)ToGLTextureWrapMode(xAxisWrap));
            GL.SamplerParameteri(sampler, SamplerParameterI.TextureWrapS, (int)ToGLTextureWrapMode(yAxisWrap));

            GL.SamplerParameterf(sampler, SamplerParameterF.TextureMaxAnisotropy, MathHelper.Clamp(anisoLevel, 1f, MaxAnisoLevel));

            GL.SamplerParameteri(sampler, SamplerParameterI.TextureCompareMode, (int)ToGLTextureCompareMode(depthCompMode));
            GL.SamplerParameteri(sampler, SamplerParameterI.TextureCompareFunc, (int)ToGLTextureCompareFunc(depthCompFunc));

            return new ShadowSampler(name, sampler, ShadowSamplerType.Sampler2DArray, magFilter, minFilter, 0, -1000, 1000, anisoLevel, xAxisWrap, yAxisWrap, WrapMode.Repeat, new Color4<Rgba>(0f, 0f, 0f, 0f), false, depthCompMode, depthCompFunc);
        }

        public static Mesh CreateMesh(string name, MeshData data)
        {
            var indexbuffer = data.IndexType switch
            {
                IndexBufferType.UInt8 =>  CreateIndexBuffer(name, data.Int8Indices,  BufferFlags.None),
                IndexBufferType.UInt16 => CreateIndexBuffer(name, data.Int16Indices, BufferFlags.None),
                IndexBufferType.UInt32 => CreateIndexBuffer(name, data.Int32Indices, BufferFlags.None),
                _ => throw new Exception($"Unknown index type '{data.IndexType}'"),
            };

            var vertbuffer = CreateDataBuffer<StandardVertex>($"{name}: Pos UV Normal", data.Vertices, BufferFlags.None);

            // Create a mesh with no submeshes.
            var mesh = new Mesh(name, indexbuffer, null);

            mesh.AABB = data.AABB;

            // Add the vertex data stream with the standard attribute layout.
            var stdVertex = mesh.AddBuffer(vertbuffer);
            mesh.AddAttributes(BuiltIn.StandardAttributes);
            for (int i = 0; i < BuiltIn.StandardAttributes.Length; i++)
            {
                mesh.AddLink(i, stdVertex);
            }

            return mesh;
        }

        public static Texture Create1PixelTexture(string name, Color4<Rgba> color)
        {
            GLUtil.CreateTexture(name, TextureTarget.Texture2d, out int texture);

            var format = TextureFormat.Rgba32F;
            GL.TextureStorage2D(texture, 1, ToGLSizedInternalFormat(format), 1, 1);

            GL.TextureSubImage2D(texture, 0, 0, 0, 1, 1, PixelFormat.Rgba, PixelType.Float, in color);

            GL.GenerateTextureMipmap(texture);

            return new Texture(name, texture, TextureType.Texture2D, format, 1, 1, 1, 0, 0, 1, 1, false);
        }

        public static Texture CreateEmpty2DTexture(string name, TextureFormat format, int width, int height)
        {
            GLUtil.CreateTexture(name, TextureTarget.Texture2d, out int texture);

            GL.TextureStorage2D(texture, 1, ToGLSizedInternalFormat(format), width, height);

            return new Texture(name, texture, TextureType.Texture2D, format, width, height, 1, 0, 0, 1, 1, false);
        }

        public static Texture CreateEmpty2DTextureArray(string name, TextureFormat format, int width, int height, int length)
        {
            GLUtil.CreateTexture(name, TextureTarget.Texture2dArray, out int texture);

            GL.TextureStorage3D(texture, 1, ToGLSizedInternalFormat(format), width, height, length);
            
            return new Texture(name, texture, TextureType.Texture2DArray, format, width, height, length, 0, 0, 1, 1, false);
        }

        public static Texture CreateEmptyMultisample2DTexture(string name, TextureFormat format, int width, int height, int samples, bool fixedSampleLocations)
        {
            GLUtil.CreateTexture(name, TextureTarget.Texture2dMultisample, out int texture);

            int maxFormatSamples = default;
            GL.GetInternalformati(TextureTarget.Texture2dMultisample, (InternalFormat)ToGLSizedInternalFormat(format), InternalFormatPName.Samples, 1, ref maxFormatSamples);
            // FIXME: Don't assert? Maybe just print a warning?
            Debug.Assert(samples <= maxFormatSamples, $"Texture does not support the specified number of samples!");

            GL.TextureStorage2DMultisample(texture, samples, ToGLSizedInternalFormat(format), width, height, fixedSampleLocations);

            return new Texture(name, texture, TextureType.Texture2DMultisample, format, width, height, 1, 0, 0, 1, samples, fixedSampleLocations);
        }

        #endregion

        #region Modification

        public static void SetMinMaxLod(ref Sampler sampler, float min, float max)
        {
            GL.SamplerParameterf(sampler.Handle, SamplerParameterF.TextureMinLod, min);
            GL.SamplerParameterf(sampler.Handle, SamplerParameterF.TextureMaxLod, max);

            sampler.LODMin = min;
            sampler.LODMax = max;
        }

        public static void RecompileShader(ShaderProgram program, string source)
        {
            GLUtil.CreateProgram(program.Name, out int newProgram);


        }

        public static unsafe void ClearTexture<T>(Texture texture, int level, Span<T> data) where T : unmanaged
        {
            Debug.Assert(data.Length == 4, "We only support non-depth non-stencil clears atm.");

            Debug.Assert(texture.Format == TextureFormat.R32UI, "We only support R32ui formats atm.");

            // FIXME: Differentiate textures with integer internal format!
            PixelFormat format = PixelFormat.RedInteger;

            PixelType type;
            if (typeof(T) == typeof(byte))
            {
                type = PixelType.UnsignedByte;
            }
            else if (typeof(T) == typeof(int))
            {
                type = PixelType.UnsignedInt;
            }
            else if (typeof(T) == typeof(float))
            {
                type = PixelType.Float;
            }
            else if (typeof(T) == typeof(Half))
            {
                type = PixelType.HalfFloat;
            }
            else
            {
                throw new ArgumentException($"This function doesn't support the type {typeof(T)} yet.");
            }

            fixed (void* ptr = data)
            {
                GL.ClearTexImage(texture.Handle, level, format, type, (IntPtr)ptr);
            }
        }

        #endregion

        // FIXME: Typesafe buffers?
        public static void UploadBufferData<T>(Buffer buffer, int byteOffset, Span<T> data) where T : unmanaged
        {
            Debug.Assert(buffer.Flags.HasFlag(BufferFlags.Dynamic), $"Cannot upload to a buffer that is not marked dynamic.");

            if (data.Length != 0)
            {
                int size = Unsafe.SizeOf<T>();
                GL.NamedBufferSubData(buffer.Handle, (IntPtr)byteOffset, (ReadOnlySpan<T>)data);
            }
        }

        public static void UploadBufferData<T>(Buffer buffer, int byteOffset, ref T data, int elements) where T : unmanaged
        {
            Debug.Assert(buffer.Flags.HasFlag(BufferFlags.Dynamic), $"Cannot upload to a buffer that is not marked dynamic.");

            int size = Unsafe.SizeOf<T>();
            GL.NamedBufferSubData(buffer.Handle, (IntPtr)byteOffset, elements * size, in data);
        }

        public static void UpdateUniformBuffer<T>(UniformBuffer<T> uBuffer, ref T data)
            where T : unmanaged
        {
            Debug.Assert(uBuffer.Flags.HasFlag(BufferFlags.Dynamic), $"Cannot upload to a uniform buffer that is not marked dynamic.");

            GL.NamedBufferSubData(uBuffer.Handle, IntPtr.Zero, Unsafe.SizeOf<T>(), in data);
        }

        public static void ReallocBuffer(ref Buffer buffer, int newElementCount)
        {
            GL.DeleteBuffer(buffer.Handle);
            GLUtil.CreateBuffer(buffer.Name, out buffer.Handle);

            BufferStorageMask glFlags = ToGLStorageMask(buffer.Flags);
            // GLEXT: ARB_direct_access

            GL.NamedBufferStorage(buffer.Handle, newElementCount * buffer.ElementSize, IntPtr.Zero, glFlags);
            buffer.Elements = newElementCount;
        }

        public static void ReallocBuffer(ref IndexBuffer buffer, int newSize)
        {
            GL.DeleteBuffer(buffer.Handle);
            GLUtil.CreateElementBuffer(buffer.Name, out buffer.Handle);

            BufferStorageMask glFlags = ToGLStorageMask(buffer.Flags);
            // GLEXT: ARB_direct_access
            GL.NamedBufferStorage(buffer.Handle, newSize * SizeInBytes(buffer.IndexType), IntPtr.Zero, glFlags);
            buffer.Elements = newSize;
        }

        /// <summary>
        /// Resizes all attachments of a framebuffer to the new size
        /// </summary>
        public static void ResizeFramebuffer(Framebuffer buffer, Vector2i newSize)
        {
            for (int i = 0; i < buffer.ColorAttachments?.Length; i++)
            {
                var original = buffer.ColorAttachments[i].ColorTexture;

                Texture tex = CreateResizedTexture2D(original, newSize);

                AddColorAttachment(buffer, tex, buffer.ColorAttachments[i].Index, buffer.ColorAttachments[i].MipLevel);

                // FIXME: Figure out if we should really delete the texture here?
                DeleteTexture(ref original);
            }

            bool combinedDepthStencil = buffer.DepthAttachment?.Texture == buffer.StencilAttachment?.Texture;
            if (combinedDepthStencil && buffer.DepthAttachment.HasValue)
            {
                var original = buffer.DepthAttachment.Value.Texture;
                var combined = CreateResizedTexture2D(original, newSize);
                AddDepthStencilAttachment(buffer, combined, buffer.DepthAttachment.Value.MipLevel);

                // FIXME: Figure out if we should really delete the texture here?
                DeleteTexture(ref original);
            }
            else
            {
                if (buffer.DepthAttachment.HasValue)
                {
                    var original = buffer.DepthAttachment.Value.Texture;
                    var depth = CreateResizedTexture2D(original, newSize);
                    AddDepthAttachment(buffer, depth, buffer.DepthAttachment.Value.MipLevel);

                    // FIXME: Figure out if we should really delete the texture here?
                    DeleteTexture(ref original);
                }

                if (buffer.StencilAttachment.HasValue)
                {
                    var original = buffer.StencilAttachment.Value.Texture;
                    var stencil = CreateResizedTexture2D(original, newSize);
                    AddStencilAttachment(buffer, stencil, buffer.StencilAttachment.Value.MipLevel);

                    // FIXME: Figure out if we should really delete the texture here?
                    DeleteTexture(ref original);
                }
            }

            var status = CheckFramebufferComplete(buffer, FramebufferTarget.ReadDraw);
            if (status != FramebufferStatus.FramebufferComplete)
            {
                Debug.WriteLine($"Fame buffer not complete after resize! {status}");
            }
        }

        /// <summary>
        /// This function handles resizing all 2D textures, including depth/stencil and multisample.
        /// </summary>
        public static Texture CreateResizedTexture2D(Texture texture, Vector2i newSize)
        {
            Debug.Assert(texture.Depth == 1, $"We only support resizing 2D textures atm (this means we can't resize 2D texture arrays)");
            Debug.Assert(texture.BaseLevel == 0, $"We don't handle special mip setups in this function yet.");
            Debug.Assert(texture.MaxLevel == texture.MipLevels - 1, $"We don't handle special mip setups in this function yet.");

            GLUtil.CreateTexture(texture.Name, ToGLTextureTarget(texture.Type), out int handle);

            if (IsMultisampleType(texture.Type))
            {
                int maxFormatSamples = default;
                GL.GetInternalformati(ToGLTextureTarget(texture.Type), (InternalFormat)ToGLSizedInternalFormat(texture.Format), InternalFormatPName.Samples, 1, ref maxFormatSamples);
                // FIXME: Don't assert? Maybe just print a warning?
                Debug.Assert(texture.Samples <= maxFormatSamples, $"Texture does not support the specified number of samples!");

                GL.TextureStorage2DMultisample(handle, texture.Samples, ToGLSizedInternalFormat(texture.Format), newSize.X, newSize.Y, texture.FixedSampleLocations);
            }
            else
            {
                GL.TextureStorage2D(handle, texture.MipLevels, ToGLSizedInternalFormat(texture.Format), newSize.X, newSize.Y);
            }

            return new Texture(texture.Name, handle,
                texture.Type, texture.Format,
                newSize.X, newSize.Y, texture.Depth,
                texture.BaseLevel, texture.MaxLevel, texture.MipLevels,
                texture.Samples, texture.FixedSampleLocations);
        }

        #region Deletion

        // Deletes the buffer and resets it's contents
        public static void DeleteBuffer(ref Buffer? buffer)
        {
            GL.DeleteBuffer(buffer?.Handle ?? 0);
            if (buffer != null) buffer.Handle = -1;
            buffer = default;
        }

        // Deletes the buffer and resets it's contents
        public static void DeleteBuffer(ref IndexBuffer? buffer)
        {
            GL.DeleteBuffer(buffer?.Handle ?? 0);
            if (buffer != null) buffer.Handle = -1;
            buffer = default;
        }

        public static void DeleteTexture(ref Texture? texture)
        {
            GL.DeleteTexture(texture?.Handle ?? 0);
            if (texture != null) texture.Handle = -1;
            texture = null;
        }

        #endregion

        #region Attributes

        public struct VertexAttribute
        {
            public bool Active;
            public int Size;
            public AttributeType Type;
            public bool Normalized;
            public int Offset;

            public override string ToString()
            {
                return $"Active: {Active} {Type} {Size} Offset: {Offset} Normalized: {Normalized}";
            }
        }

        public const int MinimumNumberOfVertexAttributes = 16;

        // Represents the state of the 16 guaranteed vertex attributes
        public static VertexAttribute[] Attributes = new VertexAttribute[MinimumNumberOfVertexAttributes];
        public static Buffer?[] AttributeBuffers     = new Buffer[MinimumNumberOfVertexAttributes];
        public static int[] AttributeToBufferLinks = new int[MinimumNumberOfVertexAttributes];
        public static IndexBuffer? CurrentIndexBuffer = null;


        public static void SetupGlobalVAO()
        {
            GLUtil.CreateVertexArray("The one VAO", out int VAO);
            GL.BindVertexArray(VAO);

            for (int i = 0; i < Attributes.Length; i++)
            {
                Attributes[i].Active = false;
                Attributes[i].Size = 0;
                Attributes[i].Type = 0;
                Attributes[i].Normalized = false;
            }

            for (int i = 0; i < AttributeToBufferLinks.Length; i++)
            {
                AttributeToBufferLinks[i] = i;
            }
        }

        public static void SetVertexAttribute(int index, AttributeSpecification spec)
        {
            ref VertexAttribute attrib = ref Attributes[index];

            // Only set the vertex attrib format if it's actually different from what is already bound
            if (attrib.Size != spec.Size ||
                attrib.Type != spec.Type ||
                attrib.Normalized != spec.Normalized ||
                attrib.Offset != spec.Offset)
            {
                GL.VertexAttribFormat((uint)index, spec.Size, ToGLAttribType(spec.Type), spec.Normalized, (uint)spec.Offset);

                attrib.Size = spec.Size;
                attrib.Type = spec.Type;
                attrib.Normalized = spec.Normalized;
                attrib.Offset = spec.Offset;
            }
        }

        public static void SetAndEnableVertexAttribute(int index, AttributeSpecification spec)
        {
            ref VertexAttribute attrib = ref Attributes[index];

            // Only set the vertex attrib format if it's actually different from what is already bound
            if (attrib.Size != spec.Size ||
                attrib.Type != spec.Type ||
                attrib.Normalized != spec.Normalized ||
                attrib.Offset != spec.Offset)
            {
                GL.VertexAttribFormat((uint)index, spec.Size, ToGLAttribType(spec.Type), spec.Normalized, (uint)spec.Offset);

                attrib.Size = spec.Size;
                attrib.Type = spec.Type;
                attrib.Normalized = spec.Normalized;
                attrib.Offset = spec.Offset;
            }
            
            if (attrib.Active == false)
            {
                GL.EnableVertexAttribArray((uint)index);
                attrib.Active = true;
            }
        }

        public static void DisableVertexAttribute(int index)
        {
            if (Attributes[index].Active)
            {
                GL.DisableVertexAttribArray((uint)index);
                Attributes[index].Active = false;
            }
        }

        public static void DisableAllVertexAttributes()
        {
            for (int i = 0; i < Attributes.Length; i++)
            {
                DisableVertexAttribute(i);
            }
        }

        public static void SetAndEnableVertexAttributes(Span<AttributeSpecification> attribs)
        {
            for (int i = 0; i < attribs.Length; i++)
            {
                SetAndEnableVertexAttribute(i, attribs[i]);
            }

            // FIXME: Do we want to do this?
            for (int i = attribs.Length; i < Attributes.Length; i++)
            {
                DisableVertexAttribute(i);
            }
        }

        public static void BindVertexAttribBuffer(int index, Buffer buffer)
        {
            if (AttributeBuffers[index] != buffer)
            {
                GL.BindVertexBuffer((uint)index, buffer!.Handle, IntPtr.Zero, buffer.ElementSize);

                AttributeBuffers[index] = buffer;
            }
        }

        public static void BindVertexAttribBuffer(int index, Buffer buffer, int offset)
        {
            if (AttributeBuffers[index] != buffer)
            {
                GL.BindVertexBuffer((uint)index, buffer!.Handle, (IntPtr)offset, buffer.ElementSize);

                AttributeBuffers[index] = buffer;
            }
        }

        public static void LinkAttributeBuffer(AttributeBufferLink link)
        {
            if (AttributeToBufferLinks[link.AttribIndex] != link.BufferIndex)
            {
                GL.VertexAttribBinding((uint)link.AttribIndex, (uint)link.BufferIndex);

                AttributeToBufferLinks[link.AttribIndex] = link.BufferIndex;
            }
        }

        public static void LinkAttributeBuffers(Span<AttributeBufferLink> links)
        {
            for (int i = 0; i < links.Length; i++)
            {
                LinkAttributeBuffer(links[i]);
            }
        }

        public static void LinkAttributeBuffer(int attribIndex, int bufferIndex)
        {
            if (AttributeToBufferLinks[attribIndex] != bufferIndex)
            {
                GL.VertexAttribBinding((uint)attribIndex, (uint)bufferIndex);

                AttributeToBufferLinks[attribIndex] = bufferIndex;
            }
        }

        public static void ClearVertexAttribBuffer(int index)
        {
            if (AttributeBuffers[index] != null)
            {
                GL.BindVertexBuffer((uint)index, 0, IntPtr.Zero, 0);
                AttributeBuffers[index] = null;
            }
        }

        public static void BindIndexBuffer(IndexBuffer? buffer)
        {
            if (CurrentIndexBuffer != buffer)
            {
                GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, buffer?.Handle ?? 0);
                CurrentIndexBuffer = buffer;
            }
        }

        public static void UnbindIndexBuffer()
        {
            if (CurrentIndexBuffer != null)
            {
                GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
                CurrentIndexBuffer = null;
            }
        }

        public static void BindMeshData(Mesh mesh)
        {
            BindIndexBuffer(mesh.Indices!);

            for (int i = 0; i < mesh.DataBuffers.Length; i++)
            {
                BindVertexAttribBuffer(i, mesh.DataBuffers[i]);
            }

            for (int i = 0; i < mesh.Attributes.Length; i++)
            {
                SetAndEnableVertexAttribute(i, mesh.Attributes[i]);
            }

            // FIXME: Do we want to do this?
            for (int i = mesh.Attributes.Length; i < Attributes.Length; i++)
            {
                DisableVertexAttribute(i);
            }

            for (int i = 0; i < mesh.AttributeBufferLinks.Length; i++)
            {
                LinkAttributeBuffer(mesh.AttributeBufferLinks[i]);
            }
        }

        #endregion

        #region Shaders and Uniforms

        public static ShaderPipeline? CurrentPipeline;

        [MemberNotNull(nameof(CurrentPipeline))]
        public static void UsePipeline(ShaderPipeline pipeline)
        {
            if (CurrentPipeline != pipeline)
            {
                GL.BindProgramPipeline(pipeline.Handle);
                CurrentPipeline = pipeline;
            }
        }

        public static ShaderProgram GetPipelineStage(ShaderStage stage)
        {
            if (CurrentPipeline == null)
                throw new InvalidOperationException("There is no program pipeline bound so we can't set uniforms!!");

            ShaderProgram? prog = stage switch
            {
                ShaderStage.Vertex => CurrentPipeline.VertexProgram,
                ShaderStage.Geometry => CurrentPipeline.GeometryProgram,
                ShaderStage.Fragment => CurrentPipeline.FragmentProgram,

                ShaderStage.Compute => throw new NotImplementedException(),

                _ => throw new ArgumentException($"Unknown shader stage: {stage}", nameof(stage)),
            };

            if (prog == null)
                throw new ArgumentException($"The pipeline '{CurrentPipeline.Name}' does not contain a shader for the {stage} stage.", nameof(stage));

            return prog;
        }

        public static bool TryGetPipelineStage(ShaderStage stage, [NotNullWhen(true)] out ShaderProgram? program)
        {
            if (CurrentPipeline == null)
                throw new InvalidOperationException("There is no program pipeline bound so we can't set uniforms!!");

            program = stage switch
            {
                ShaderStage.Vertex => CurrentPipeline.VertexProgram,
                ShaderStage.Geometry => CurrentPipeline.GeometryProgram,
                ShaderStage.Fragment => CurrentPipeline.FragmentProgram,

                ShaderStage.Compute => throw new NotImplementedException(),

                _ => throw new ArgumentException($"Unknown shader stage: {stage}", nameof(stage)),
            };

            return program != null;
        }

        public static int GetUniformLocation(string uniform, ShaderProgram? program)
        {
            if (program == null) return -1;
            if (program.UniformLocations.TryGetValue(uniform, out int location))
            {
                return location;
            }
            else
            {
                //Debug.Print($"The uniform '{uniform}' does not exist in the shader '{program.Name}'!");
                program.UniformLocations.Add(uniform, -1);
                return -1;
            }
        }

        public static bool TryGetUniformInfo(string uniform, ShaderProgram? program, out UniformFieldInfo info)
        {
            if (program == null || program.UniformInfo == null)
            {
                info = default;
                return false;
            }

            for (int i = 0; i < program.UniformInfo.Length; i++)
            {
                if (program.UniformInfo[i].Name == uniform)
                {
                    info = program.UniformInfo[i];
                    return true;
                }
            }
            info = default;
            return false;
        }

        public static void UniformProperty(ref Property property)
        {
            if (CurrentPipeline == null) throw new Exception();
            var pipeline = CurrentPipeline;

            // FIXME: Remove the need to set uniforms for both vertex and fragment stages!!

            var vert = pipeline.VertexProgram!;
            var vertLoc = GetUniformLocation(property.Name, vert);

            var frag = pipeline.FragmentProgram!;
            var fragLoc = GetUniformLocation(property.Name, frag);

            if (vertLoc == -1 && fragLoc == -1) return;

            switch (property.Type)
            {
                case PropertyType.Invalid:
                    throw new Exception("You probably forgot the set the property type.");
                case PropertyType.Bool:
                    GL.ProgramUniform1i(vert.Handle, vertLoc, property.BoolValue ? 1 : 0);
                    GL.ProgramUniform1i(frag.Handle, fragLoc, property.BoolValue ? 1 : 0);
                    break;
                case PropertyType.Int:
                    GL.ProgramUniform1i(vert.Handle, vertLoc, property.IntValue);
                    GL.ProgramUniform1i(frag.Handle, fragLoc, property.IntValue);
                    break;
                case PropertyType.Float:
                    GL.ProgramUniform1f(vert.Handle, vertLoc, property.FloatValue);
                    GL.ProgramUniform1f(frag.Handle, fragLoc, property.FloatValue);
                    break;
                case PropertyType.Float2:
                    GL.ProgramUniform2f(vert.Handle, vertLoc, 1, property.Vector2Value);
                    GL.ProgramUniform2f(frag.Handle, fragLoc, 1, property.Vector2Value);
                    break;
                case PropertyType.Float3:
                    GL.ProgramUniform3f(vert.Handle, vertLoc, 1, property.Vector3Value);
                    GL.ProgramUniform3f(frag.Handle, fragLoc, 1, property.Vector3Value);
                    break;
                case PropertyType.Float4:
                    GL.ProgramUniform4f(vert.Handle, vertLoc, 1, property.Vector4Value);
                    GL.ProgramUniform4f(frag.Handle, fragLoc, 1, property.Vector4Value);
                    break;
                case PropertyType.Color:
                    // FIXME: Is this a vec3 or vec4????
                    GL.ProgramUniform4f(vert.Handle, vertLoc, 1, property.ColorValue.AsVector4());
                    GL.ProgramUniform4f(frag.Handle, fragLoc, 1, property.ColorValue.AsVector4());
                    break;
                case PropertyType.Matrix3:
                    GL.ProgramUniformMatrix3f(vert.Handle, vertLoc, 1, true, in property.Matrix3Value);
                    GL.ProgramUniformMatrix3f(frag.Handle, fragLoc, 1, true, in property.Matrix3Value);
                    break;
                case PropertyType.Matrix4:
                    GL.ProgramUniformMatrix4f(vert.Handle, vertLoc, 1, true, in property.Matrix4Value);
                    GL.ProgramUniformMatrix4f(frag.Handle, fragLoc, 1, true, in property.Matrix4Value);
                    break;
                default:
                    break;
            }
        }

        public static void UniformMatrix4(string uniformName, bool transpose, ref Matrix4 matrix)
        {
            Debug.Assert(CurrentPipeline != null);
            if (CurrentPipeline.Uniforms.TryGetValue(uniformName, out var uniform))
            {
                if (uniform.Stages.HasFlag(ShaderStages.Vertex))
                    GL.ProgramUniformMatrix4f(
                        CurrentPipeline.VertexProgram.Handle,
                        uniform.VertexLocation,
                        1,
                        transpose,
                        in matrix);

                if (uniform.Stages.HasFlag(ShaderStages.Fragment))
                    GL.ProgramUniformMatrix4f(
                        CurrentPipeline.FragmentProgram.Handle,
                        uniform.FragmentLocation,
                        1,
                        transpose,
                        in matrix);
            }
            //UniformMatrix4(uniformName, ShaderStage.Vertex, transpose, ref matrix);
            //UniformMatrix4(uniformName, ShaderStage.Geometry, transpose, ref matrix);
            //UniformMatrix4(uniformName, ShaderStage.Fragment, transpose, ref matrix);
        }

        public static void UniformMatrix4(string uniformName, ShaderStage stage, bool transpose, ref Matrix4 matrix)
        {
            if (TryGetPipelineStage(stage, out var prog))
            {
                var location = GetUniformLocation(uniformName, prog);
                if (location == -1) return;
                GL.ProgramUniformMatrix4f(prog.Handle, location, 1, transpose, in matrix);
            }
        }

        // FIXME: We probably want to handle matrices slightly different than we are currently doing
        // uniform matrices atm only get their "[0]" name put into the reflected data
        // we could change that but this is fine
        public static void UniformMatrix4Array(string uniformName, ShaderStage stage, bool transpose, Matrix4[] matrices)
        {
            var prog = GetPipelineStage(stage);
            if (TryGetUniformInfo(uniformName + "[0]", prog, out var info))
            {
                Debug.Assert(info.Size >= matrices.Length);
                GL.ProgramUniformMatrix4f(prog.Handle, info.Location, matrices.Length, transpose, matrices);
            }
        }

        public static void UniformMatrix3(string uniformName, bool transpose, ref Matrix3 matrix)
        {
            UniformMatrix3(uniformName, ShaderStage.Vertex, transpose, ref matrix);
            UniformMatrix3(uniformName, ShaderStage.Geometry, transpose, ref matrix);
            UniformMatrix3(uniformName, ShaderStage.Fragment, transpose, ref matrix);
        }

        public static void UniformMatrix3(string uniformName, ShaderStage stage, bool transpose, ref Matrix3 matrix)
        {
            if (TryGetPipelineStage(stage, out var prog))
            {
                var location = GetUniformLocation(uniformName, prog);
                if (location == -1) return;
                GL.ProgramUniformMatrix3f(prog.Handle, location, 1, transpose, in matrix);
            }
        }

        public static void UniformVector3(string uniformName, Vector3 vec3)
        {
            Debug.Assert(CurrentPipeline != null);
            if (CurrentPipeline.Uniforms.TryGetValue(uniformName, out var uniform))
            {
                if (uniform.Stages.HasFlag(ShaderStages.Vertex))
                    GL.ProgramUniform3f(
                        CurrentPipeline.VertexProgram.Handle,
                        uniform.VertexLocation,
                        1,
                        in vec3);

                if (uniform.Stages.HasFlag(ShaderStages.Fragment))
                    GL.ProgramUniform3f(
                        CurrentPipeline.FragmentProgram.Handle,
                        uniform.FragmentLocation,
                        1,
                        in vec3);
            }
        }

        public static void UniformVector3(string uniformName, ShaderStage stage, Vector3 vec3)
        {
            if (TryGetPipelineStage(stage, out var prog))
            {
                var location = GetUniformLocation(uniformName, prog);
                if (location == -1) return;
                GL.ProgramUniform3f(prog.Handle, location, 1, vec3);
            }
        }

        public static void UniformVector3(string uniformName, Color4<Rgba> color)
        {
            UniformVector3(uniformName, ShaderStage.Vertex, color);
            UniformVector3(uniformName, ShaderStage.Geometry, color);
            UniformVector3(uniformName, ShaderStage.Fragment, color);
        }

        public static void UniformVector3(string uniformName, Color3<Rgb> color)
        {
            UniformVector3(uniformName, ShaderStage.Vertex, color);
            UniformVector3(uniformName, ShaderStage.Geometry, color);
            UniformVector3(uniformName, ShaderStage.Fragment, color);
        }

        public static void UniformVector3(string uniformName, ShaderStage stage, Color4<Rgba> color)
        {
            if (TryGetPipelineStage(stage, out var prog))
            {
                var location = GetUniformLocation(uniformName, prog);
                if (location == -1) return;
                Vector3 vec = new Vector3(color.X, color.Y, color.Z);
                GL.ProgramUniform3f(prog.Handle, location, 1, in vec);
            }
        }

        public static void UniformVector3(string uniformName, ShaderStage stage, Color3<Rgb> color)
        {
            if (TryGetPipelineStage(stage, out var prog))
            {
                var location = GetUniformLocation(uniformName, prog);
                if (location == -1) return;
                Vector3 vec = new Vector3(color.X, color.Y, color.Z);
                GL.ProgramUniform3f(prog.Handle, location, 1, in vec);
            }
        }

        public static void Uniform1(string uniformName, int i)
        {
            Uniform1(uniformName, ShaderStage.Vertex, i);
            Uniform1(uniformName, ShaderStage.Geometry, i);
            Uniform1(uniformName, ShaderStage.Fragment, i);
        }

        public static void Uniform1(string uniformName, ShaderStage stage, int i)
        {
            if (TryGetPipelineStage(stage, out var prog))
            {
                var location = GetUniformLocation(uniformName, prog);
                if (location == -1) return;
                GL.ProgramUniform1i(prog.Handle, location, i);
            }
        }

        public static void Uniform1(string uniformName, float f)
        {
            Uniform1(uniformName, ShaderStage.Vertex, f);
            Uniform1(uniformName, ShaderStage.Geometry, f);
            Uniform1(uniformName, ShaderStage.Fragment, f);
        }

        public static void Uniform1(string uniformName, ShaderStage stage, float f)
        {
            if (TryGetPipelineStage(stage, out var prog))
            {
                var location = GetUniformLocation(uniformName, prog);
                if (location == -1) return;
                GL.ProgramUniform1f(prog.Handle, location, 1, f);
            }
        }

        public static void Uniform4(string uniformName, Vector4 vec4)
        {
            Uniform4(uniformName, ShaderStage.Vertex, vec4);
            Uniform4(uniformName, ShaderStage.Geometry, vec4);
            Uniform4(uniformName, ShaderStage.Fragment, vec4);
        }

        public static void Uniform4(string uniformName, ShaderStage stage, Vector4 vec4)
        {
            if (TryGetPipelineStage(stage, out var prog))
            {
                var location = GetUniformLocation(uniformName, prog);
                if (location == -1) return;
                GL.ProgramUniform4f(prog.Handle, location, 1, vec4);
            }
        }

        public static void UniformBlock(string blockName, Buffer buffer)
        {
            UniformBlock(blockName, ShaderStage.Vertex, buffer);
            UniformBlock(blockName, ShaderStage.Geometry, buffer);
            UniformBlock(blockName, ShaderStage.Fragment, buffer);
        }

        public static void UniformBlock(string blockName, ShaderStage stage, Buffer buffer)
        {
            if (TryGetPipelineStage(stage, out var program))
            {
                if (program.UniformBlockIndices.TryGetValue(blockName, out int index))
                {
                    GL.BindBufferBase(BufferTargetARB.UniformBuffer, (uint)index, buffer.Handle);
                }
            }
        }

        public static void UniformBlock<T>(string blockName, UniformBuffer<T> buffer)
            where T : unmanaged
        {
            UniformBlock(blockName, ShaderStage.Vertex, buffer);
            UniformBlock(blockName, ShaderStage.Geometry, buffer);
            UniformBlock(blockName, ShaderStage.Fragment, buffer);
        }

        public static void UniformBlock<T>(string blockName, ShaderStage stage, UniformBuffer<T> buffer)
            where T : unmanaged
        {
            if (TryGetPipelineStage(stage, out var program))
            {
                if (program.UniformBlockIndices.TryGetValue(blockName, out int index))
                {
                    GL.BindBufferBase(BufferTargetARB.UniformBuffer, (uint)index, buffer.Handle);
                }
            }
        }

        #endregion

        #region Texture Binding

        public const int MinTextureUnits = 16;

        public static Texture?[] BoundTextures = new Texture[MinTextureUnits];
        public static ISampler?[] BoundSamplers = new ISampler[MinTextureUnits];

        // FIXME: We need to store more info than this, e.g. what layer was bound!
        public static Texture?[] BoundImages = new Texture[MinTextureUnits];


        public static void BindTexture(int unit, Texture texture, Sampler sampler)
        {
            BindTexture(unit, texture);
            BindSampler(unit, sampler);
        }

        public static void BindTexture(int unit, Texture texture, ShadowSampler sampler)
        {
            if (texture.Type != sampler.Type.ToTextureType())
            {
                Debug.Print($"Sampler at unit '{unit}' doesn't match the bound texture type '{BoundTextures[unit]?.Type}' (sampler type: {sampler.Type})");
            }

            BindTexture(unit, texture);
            BindSampler(unit, sampler);
        }

        public static void BindTexture(int unit, Texture texture, ISampler? sampler)
        {
            // FIXME!
            //if (BoundTextures[unit]?.Type != sampler.Type.ToTextureType())
            //{
            //    Debug.Print($"Sampler at unit '{unit}' doesn't match the bound texture type '{BoundTextures[unit]?.Type}' (sampler type: {sampler.Type})");
            //}

            BindTexture(unit, texture);
            BindSampler(unit, sampler);
        }

        public static void BindTexture(int unit, Texture? texture)
        {
            if (BoundTextures[unit] != texture)
            {
                GL.BindTextureUnit((uint)unit, texture?.Handle ?? 0);
                BoundTextures[unit] = texture;
            }
        }

        public static void BindTextureUnsafe(int unit, int textureHandle)
        {
            GL.BindTextureUnit((uint)unit, textureHandle);

            BoundTextures[unit] = null;
        }

        public static void BindSampler(int unit, Sampler sampler)
        {
            BindSampler(unit, (ISampler)sampler);
        }

        public static void BindSampler(int unit, ShadowSampler sampler)
        {
            BindSampler(unit, (ISampler)sampler);
        }

        public static void BindSampler(int unit, ISampler? sampler)
        {
            var boundTexture = BoundTextures[unit];
            if (boundTexture != null && sampler != null)
            {
                if (boundTexture.Type != sampler.Type)
                {
                    Debug.Print($"Sampler at unit '{unit}' doesn't match the bound texture type '{boundTexture.Type}' (sampler type: {sampler.Type})");
                }
            }
            
            if (BoundSamplers[unit] != sampler)
            {
                GL.BindSampler((uint)unit, sampler?.Handle ?? 0);

                BoundSamplers[unit] = sampler;
            }
        }

        public static void BindTextures(int startUnit, Span<Texture> textures, Span<ISampler?> samplers)
        {
            Debug.Assert(textures.Length == samplers.Length);

            BindTextures(startUnit, textures);
            BindSamplers(startUnit, samplers);
        }

        public static void BindTextures(int startUnit, Span<Texture> textures)
        {
            // We could add thing that looks the how many textures
            // match the currently bound textures from the start of the span
            // and skip binding them.

            if (textures.Length == 0) return;

            Span<int> handles = stackalloc int[textures.Length];
            for (int i = 0; i < textures.Length; i++)
            {
                BoundTextures[startUnit + i] = textures[i];

                handles[i] = textures[i].Handle;
            }

            GL.BindTextures((uint)startUnit, handles);
        }

        public static void BindSamplers(int startUnit, Span<ISampler?> samplers)
        {
            // We could add thing that looks the how many textures
            // match the currently bound textures from the start of the span
            // and skip binding them.

            if (samplers.Length == 0) return;

            Span<int> handles = stackalloc int[samplers.Length];
            for (int i = 0; i < samplers.Length; i++)
            {
                BoundSamplers[startUnit + i] = samplers[i];

                handles[i] = samplers[i]?.Handle ?? 0;
            }

            GL.BindSamplers((uint)startUnit, (ReadOnlySpan<int>)handles);
        }

        public static void BindImage(int unit, Texture texture, int level, TextureAccess access)
        {
            // FIXME: We need to save how the image was bound!
            if (BoundImages[unit] != texture)
            {
                GL.BindImageTexture((uint)unit, texture.Handle, level, false, 0, ToGLTextureAccess(access), (InternalFormat)ToGLSizedInternalFormat(texture.Format));

                BoundImages[unit] = texture;
            }
        }

        #endregion

        #region Framebuffer

        public static Box2i CurrentViewport;
        public static void SetViewport(int x, int y, int width, int height)
        {
            SetViewport(new Box2i(x, y, x + width, y + height));
        }

        public static void SetViewport(Box2i viewport)
        {
            if (CurrentViewport != viewport)
            {
                GL.Viewport(viewport.Min.X, viewport.Min.Y, viewport.Size.X, viewport.Size.Y);
                CurrentViewport = viewport;
            }
        }

        public static Framebuffer? DrawBuffer;
        public static Framebuffer? ReadBuffer;

        public static void BindDrawFramebuffer(Framebuffer? buffer)
        {
            // If the buffer is null, bind 0 as the draw buffer, otherwise bind the framebuffer
            if (DrawBuffer != buffer)
            {
                GL.BindFramebuffer(GLFrameBufferTarget.DrawFramebuffer, buffer?.Handle ?? 0);
                DrawBuffer = buffer;
            }
        }

        public static void BindReadFramebuffer(Framebuffer? buffer)
        {
            if (ReadBuffer != buffer)
            {
                // If the buffer is null, bind 0 as the draw buffer, otherwise bind the framebuffer
                GL.BindFramebuffer(GLFrameBufferTarget.ReadFramebuffer, buffer?.Handle ?? 0);
                ReadBuffer = buffer;
            }
        }

        public static void BindDrawFramebufferSetViewport(Framebuffer buffer)
        {
            BindDrawFramebuffer(buffer);

            // FIXME: We might want to store these values explicitly in our code.
            int defaultWidth = GL.GetNamedFramebufferParameteri(buffer.Handle, GetFramebufferParameter.FramebufferDefaultWidth);
            int defaultHeight = GL.GetNamedFramebufferParameteri(buffer.Handle, GetFramebufferParameter.FramebufferDefaultHeight);
            
            bool hasAttachments =
                buffer.DepthAttachment != null ||
                buffer.ColorAttachments != null;

            int width = int.MaxValue;
            int height = int.MaxValue;

            if (hasAttachments)
            {
                if (buffer.DepthAttachment != null)
                {
                    width = Math.Min(width, buffer.DepthAttachment.Value.Texture.Width);
                    height = Math.Min(width, buffer.DepthAttachment.Value.Texture.Height);
                }

                if (buffer.StencilAttachment != null)
                {
                    width = Math.Min(width, buffer.StencilAttachment.Value.Texture.Width);
                    height = Math.Min(width, buffer.StencilAttachment.Value.Texture.Height);
                }

                for (int i = 0; i < buffer.ColorAttachments?.Length; i++)
                {
                    width = Math.Min(width, buffer.ColorAttachments[i].ColorTexture.Width);
                    height = Math.Min(width, buffer.ColorAttachments[i].ColorTexture.Height);
                }
            }
            else
            {
                width = defaultWidth;
                height = defaultHeight;
            }

            SetViewport(0, 0, width, height);
        }

        // FIXME: This might be too much state change in one function 
        // making things harder to reason about...
        // FIXME: Make our own ClearBufferMask enum (leaking enum)
        public static void BindDrawFramebufferSetViewportAndClear(Framebuffer buffer, Color4<Rgba> clearColor, ClearMask mask)
        {
            BindDrawFramebufferSetViewport(buffer);
            SetClearColor(clearColor);
            Clear(mask);
        }

        #endregion

        public static Color4<Rgba> ClearColor;
        public static void SetClearColor(Color4<Rgba> color)
        {
            if (ClearColor != color)
            {
                GL.ClearColor(color);
                ClearColor = color;
            }
        }

        public static void Clear(ClearMask mask)
        {
            if (mask.HasFlag(ClearMask.Color) && ColorWrite == ColorChannels.None)
                Debug.WriteLine("[Warning] Trying to clear color with color write disabled!");

            if (mask.HasFlag(ClearMask.Depth) && DepthWrite == false)
                Debug.WriteLine("[Warning] Trying to clear depth with depth write disabled!");

            GL.Clear(ToGLClearBufferMask(mask));
        }

        public static void DrawElements(Primitive type, int elements, IndexBufferType indexType, int offset)
        {
            if (CurrentIndexBuffer == null) throw new Exception("Cannot draw all elements if there is no element buffer bound!");

            GL.DrawElements(ToGLPrimitiveType(type), elements, ToGLDrawElementsType(indexType), offset);
        }

        public static void DrawAllElements(Primitive type)
        {
            if (CurrentIndexBuffer == null) throw new Exception("Cannot draw all elements if there is no element buffer bound!");

            GL.DrawElements(ToGLPrimitiveType(type), CurrentIndexBuffer.Elements, ToGLDrawElementsType(CurrentIndexBuffer.IndexType), 0);
        }

        public static void DrawArrays(Primitive type, int offset, int vertices)
        {
            if (CurrentIndexBuffer != null)
                Debug.WriteLine("WARNING: Calling DrawArrays while there is an index buffer bound. This is probably not intentional.");

            GL.DrawArrays(ToGLPrimitiveType(type), offset, vertices);
        }

        public static void Dispatch(int xGroups, int yGroups, int zGroups)
        {
            // FIXME: Check that the current pipeline is a compute shader
            if (CurrentPipeline?.ComputeProgram == null)
                throw new Exception($"Trying to dispatch compute shader but the current pipeline '{CurrentPipeline?.Name}' doesn't contain a compute shader.");

            // FIXME: Do some validation on groups??
            GL.DispatchCompute((uint)xGroups, (uint)yGroups, (uint)zGroups);
        }

        public static Recti CurrentScissor = Recti.Empty;
        public static void SetScissor(Recti rect)
        {
            if (CurrentScissor != rect)
            {
                // FIXME: This will break if the screen size is changed
                GL.Scissor(rect.X, (Screen.Height - (rect.Y + rect.Height)), rect.Width, rect.Height);
                CurrentScissor = rect;
            }
        }

        private static bool DepthTesting;
        public static void SetDepthTesting(bool shouldTest)
        {
            if (DepthTesting != shouldTest)
            {
                if (shouldTest) GL.Enable(EnableCap.DepthTest);
                else GL.Disable(EnableCap.DepthTest);

                DepthTesting = shouldTest;
            }
        }

        private static bool DepthWrite;
        public static void SetDepthWrite(bool write)
        {
            if (DepthWrite != write)
            {
                GL.DepthMask(write);
                DepthWrite = write;
            }
        }

        private static DepthFunc CurrentDepthFunc;
        public static void SetDepthFunc(DepthFunc func)
        {
            if (CurrentDepthFunc != func)
            {
                GL.DepthFunc(ToGLDepthFunction(func));
                CurrentDepthFunc = func;
            }
        }

        private static ColorChannels ColorWrite;
        public static void SetColorWrite(ColorChannels flags)
        {
            if (ColorWrite != flags)
            {
                GL.ColorMask(
                    flags.HasFlag(ColorChannels.Red),
                    flags.HasFlag(ColorChannels.Green),
                    flags.HasFlag(ColorChannels.Blue),
                    flags.HasFlag(ColorChannels.Alpha));

                ColorWrite = flags;
            }
        }

        private static bool CullingFaces = false;
        private static CullMode CullMode = CullMode.None;
        public static void SetCullMode(CullMode mode)
        {
            if (CullMode != mode)
            {
                if (mode == CullMode.None)
                {
                    GL.Disable(EnableCap.CullFace);
                    CullingFaces = false;
                }
                else
                {
                    if (CullingFaces == false)
                    {
                        GL.Enable(EnableCap.CullFace);
                        CullingFaces = true;
                    }

                    // FIXME: Make a "proper" convertion?
                    switch (mode)
                    {
                        case CullMode.Front:
                            GL.CullFace(TriangleFace.Front);
                            break;
                        case CullMode.Back:
                            GL.CullFace(TriangleFace.Back);
                            break;
                        case CullMode.FrontAndBack:
                            GL.CullFace(TriangleFace.FrontAndBack);
                            break;
                        default:
                            throw new Exception();
                    }
                }

                CullMode = mode;
            }
        }

        private static bool AlphaToCoverage = false;
        public static void SetAlphaToCoverage(bool alphaToCoverage)
        {
            if (AlphaToCoverage != alphaToCoverage)
            {
                if (alphaToCoverage) GL.Enable(EnableCap.SampleAlphaToCoverage);
                else                 GL.Disable(EnableCap.SampleAlphaToCoverage);
                AlphaToCoverage = alphaToCoverage;
            }
        }

        public static void DisableBlending()
        {
            GL.Disable(EnableCap.Blend);
        }

        public static void SetNormalAlphaBlending()
        {
            const BlendEquationModeEXT eq = BlendEquationModeEXT.FuncAdd;
            SetBlendModeFull(true, eq, eq, 
                BlendingFactor.SrcAlpha,
                BlendingFactor.OneMinusSrcAlpha,
                BlendingFactor.One,
                BlendingFactor.Zero);
        }

        public static void SetBlendModeFull(bool blending, BlendEquationModeEXT colorEq, BlendEquationModeEXT alphaEq, BlendingFactor colorBlendSrc, BlendingFactor colorBlendDest, BlendingFactor alphaBlendSrc, BlendingFactor alphaBlendDest)
        {
            if (blending) GL.Enable(EnableCap.Blend);
            else          GL.Disable(EnableCap.Blend);
            
            GL.BlendEquationSeparate(colorEq, alphaEq);
            GL.BlendFuncSeparate(colorBlendSrc, colorBlendDest, alphaBlendSrc, alphaBlendDest);
        }

        #region Debug Regions

        public struct DebugGroup : IDisposable
        {
            public int ProfileSpanID;

            public static DebugGroup Create(string name, int passID)
            {
                DebugGroup group;
                group.ProfileSpanID = passID;
                Editor.Profiling.PushSpan(name, passID);
                return group;
            }

            public void Dispose()
            {
                //EndQuery(PassInfo.Query);
                //var ready = IsQueryReady(PassInfo.Query);
                //long time = GetTimeElapsedQueryResult(PassInfo.Query, false);
                //PassInfo.GPUTime.Current = time / 1_000_000_000d;
                Editor.Profiling.PopSpan(ProfileSpanID);

                //Debug.WriteLine($"{PassInfo.Name}: {PassInfo.GPUTime.Current}, {ready}");
                PopDebugGroup();
            }
        }

        // FIXME: Have PushIndexedPass instead of having a "seed" argument

        public static DebugGroup PushGenericPass(string name,
            int seed = 0,
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            // Get callsite "unique" hash
            GLUtil.PushDebugGroup(name);
            int callsiteHash = HashCode.Combine(seed, sourceFilePath, sourceLineNumber);
            var group = DebugGroup.Create(name, callsiteHash);
            return group;
        }

        public static DebugGroup PushDepthPass(string name,
            int seed = 0,
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            return PushGenericPass($"Depth pass: {name}", seed, sourceFilePath, sourceLineNumber);
        }

        public static DebugGroup PushColorPass(string name,
            int seed = 0,
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            return PushGenericPass($"Color pass: {name}", seed, sourceFilePath, sourceLineNumber);
        }

        public static DebugGroup PushCombinedPass(string name,
            int seed = 0,
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            return PushGenericPass($"Color + depth pass: {name}", seed, sourceFilePath, sourceLineNumber);
        }

        public static void PopDebugGroup()
        {
            GLUtil.PopDebugGroup();
        }

        #endregion

        #region Queries

        public static BufferedQuery CreateQuery(string name, QueryType type, int bufferLength)
        {
            int[] startQueries = new int[bufferLength];
            int[] endQueries = new int[bufferLength];
            GLUtil.CreateQueries($"{name} start", ToGLQueryTarget(type), startQueries);
            GLUtil.CreateQueries($"{name} end", ToGLQueryTarget(type), endQueries);
            return new BufferedQuery(name, type, startQueries, endQueries);
        }

        public static void BeginQuery(BufferedQuery query)
        {
            GL.QueryCounter(query.StartHandles[query.CurrentQuery], QueryCounterTarget.Timestamp);
        }

        public static void EndQuery(BufferedQuery query)
        {
            GL.QueryCounter(query.EndHandles[query.CurrentQuery], QueryCounterTarget.Timestamp);

            // The update the next query we want to record with
            query.CurrentQuery = (query.CurrentQuery + 1) % query.StartHandles.Length;
        }

        public static bool IsQueryReady(BufferedQuery query)
        {
            int lastQuery = query.EndHandles[query.ReadQuery % query.EndHandles.Length];
            int isAvaiable = GL.GetQueryObjecti(lastQuery, QueryObjectParameterName.QueryResultAvailable);
            return isAvaiable == (int)All.True;
        }

        public static long GetTimeElapsedQueryResult(BufferedQuery query, bool waitIfNotAvailable)
        {
            int lastStartQuery = query.StartHandles[query.ReadQuery % query.StartHandles.Length];
            int lastEndQuery = query.EndHandles[query.ReadQuery % query.EndHandles.Length];

            QueryObjectParameterName waitOrNot = waitIfNotAvailable ?
                QueryObjectParameterName.QueryResult :
                QueryObjectParameterName.QueryResultNoWait;

            long startTime = GL.GetQueryObjecti64(lastStartQuery, waitOrNot);
            long endTime = GL.GetQueryObjecti64(lastEndQuery, waitOrNot);

            // Go to the next query
            query.ReadQuery++;
            query.ReadQuery %= query.StartHandles.Length;

            return endTime - startTime;
        }

        public const int QueryBufferingLength = 3;
        public const int QueryAllocationSize = 16;

        public unsafe struct QueryBuffer
        {
            public fixed int Handles[QueryBufferingLength];
        }

        public class Query
        {
            public int Handle;
            public QueryBuffer Buffer;

            public Query(int handle)
            {
                //Buffer.Handles[0] = handle;
                Handle = handle;
            }
        }

        public static Stack<int> UnusedQueries = new Stack<int>();

        public static List<Query>[] AllQueries = new List<Query>[QueryBufferingLength];
        public static List<Query> FrameQueries;

        public static void FlipQueries(int frame)
        {
            FrameQueries = AllQueries[frame % QueryBufferingLength];
        }
        /*
        public int BeginTime(QueryTarget target)
        {
            // Find first free query or allocate
            if (UnusedQueries.Count < 0)
            {
                // Allocate queries
                Span<int> queries = stackalloc int[QueryAllocationSize * QueryBufferingLength];
                GL.GenQueries(QueryAllocationSize * QueryBufferingLength, out queries[0]);

                for (int buffer = 0; buffer < QueryBufferingLength; buffer++)
                {
                    for (int i = 0; i < QueryAllocationSize; i++)
                    {
                        int handle = queries[i + buffer * QueryAllocationSize];
                        AllQueries[buffer].Add(new Query(handle));

                        if (buffer == 0) UnusedQueries.Push(AllQueries[0].Count - 1);
                    }
                }
            }

            GL.BeginQuery(target, 0);
        }
        */
        #endregion
    }
}
