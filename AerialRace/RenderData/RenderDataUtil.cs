using AerialRace.Loading;
using OpenTK.Graphics.GL;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Transactions;
using GLFrameBufferTarget = OpenTK.Graphics.OpenGL4.FramebufferTarget;

namespace AerialRace.RenderData
{
    static class RenderDataUtil
    {
        public static float MaxAnisoLevel { get; private set; }

        public static void QueryLimits()
        {
            MaxAnisoLevel = GL.GetFloat((GetPName)All.MaxTextureMaxAnisotropy);
        }

        #region MISC

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

            Color4 _ => BufferDataType.Float4,

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

        public static BufferStorageFlags ToGLStorageFlags(BufferFlags flags)
        {
            BufferStorageFlags result = default;

            if (flags.HasFlag(BufferFlags.MapRead))
                result |= BufferStorageFlags.MapReadBit;

            if (flags.HasFlag(BufferFlags.MapWrite))
                result |= BufferStorageFlags.MapWriteBit;

            if (flags.HasFlag(BufferFlags.MapPersistent))
                result |= BufferStorageFlags.MapPersistentBit;

            if (flags.HasFlag(BufferFlags.Dynamic))
                result |= BufferStorageFlags.DynamicStorageBit;

            return result;
        }

        // FIXME: Resolve the name conflict here?
        public static GLFrameBufferTarget ToGLFramebufferTraget(FramebufferTarget target) => target switch
        {
            FramebufferTarget.Read => GLFrameBufferTarget.ReadFramebuffer,
            FramebufferTarget.Draw => GLFrameBufferTarget.DrawFramebuffer,
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
            TextureType.Texture1D => TextureTarget.Texture1D,
            TextureType.Texture2D => TextureTarget.Texture2D,
            TextureType.Texture3D => TextureTarget.Texture3D,
            TextureType.TexutreCube => TextureTarget.TextureCubeMap,

            TextureType.TextureBuffer => TextureTarget.TextureBuffer,

            TextureType.Texture1DArray => TextureTarget.Texture1DArray,
            TextureType.Texture2DArray => TextureTarget.Texture2DArray,
            TextureType.TexutreCubeArray => TextureTarget.TextureCubeMapArray,

            TextureType.Texture2DMultisample => TextureTarget.Texture2DMultisample,
            TextureType.Texture2DMultisampleArray => TextureTarget.Texture2DMultisampleArray,

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
            TextureFormat.R8Signed => (SizedInternalFormat)All.R8Snorm,
            TextureFormat.R8I => SizedInternalFormat.R8i,
            TextureFormat.R8UI => SizedInternalFormat.R8ui,
            TextureFormat.Rg8 => SizedInternalFormat.Rg8,
            TextureFormat.Rg8Signed => (SizedInternalFormat)All.Rg8Snorm,
            TextureFormat.Rg8I => SizedInternalFormat.Rg8i,
            TextureFormat.Rg8UI => SizedInternalFormat.Rg8ui,
            TextureFormat.Rgb8 => (SizedInternalFormat)All.Rgb8,
            TextureFormat.Rgb8Signed => (SizedInternalFormat)All.Rgb8Snorm,
            TextureFormat.Rgb8I => SizedInternalFormat.Rgba8i,
            TextureFormat.Rgb8UI => SizedInternalFormat.Rgba8ui,
            TextureFormat.Rgba8 => SizedInternalFormat.Rgba8,
            TextureFormat.Rgba8Signed => (SizedInternalFormat)All.Rgba8Snorm,
            TextureFormat.Rgba8I => SizedInternalFormat.Rgba8i,
            TextureFormat.Rgba8UI => SizedInternalFormat.Rgba8ui,
            TextureFormat.sRgb8 => (SizedInternalFormat)All.Srgb8,
            TextureFormat.sRgba8 => (SizedInternalFormat)All.Srgb8Alpha8,
            TextureFormat.R16 => SizedInternalFormat.R16,
            TextureFormat.R16Signed => (SizedInternalFormat)All.R16Snorm,
            TextureFormat.R16F => SizedInternalFormat.R16f,
            TextureFormat.R16I => SizedInternalFormat.R16i,
            TextureFormat.R16UI => SizedInternalFormat.R16ui,
            TextureFormat.Rg16 => SizedInternalFormat.Rg16,
            TextureFormat.Rg16Signed => (SizedInternalFormat)All.Rg16Snorm,
            TextureFormat.Rg16F => SizedInternalFormat.Rg16f,
            TextureFormat.Rg16I => SizedInternalFormat.Rg16i,
            TextureFormat.Rg16UI => SizedInternalFormat.Rg16ui,
            TextureFormat.Rgb16Signed => (SizedInternalFormat)All.Rgb16Snorm,
            TextureFormat.Rgb16F => (SizedInternalFormat)All.Rgb16f,
            TextureFormat.Rgb16I => (SizedInternalFormat)All.Rgb16i,
            TextureFormat.Rgb16UI => (SizedInternalFormat)All.Rgb16ui,
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
            TextureFormat.Rgb32F => (SizedInternalFormat)All.Rgb32f,
            TextureFormat.Rgb32I => (SizedInternalFormat)All.Rgb32i,
            TextureFormat.Rgb32UI => (SizedInternalFormat)All.Rgb32ui,
            TextureFormat.Rgba32F => SizedInternalFormat.Rgba32f,
            TextureFormat.Rgba32I => SizedInternalFormat.Rgba32i,
            TextureFormat.Rgba32UI => SizedInternalFormat.Rgba32ui,
            TextureFormat.Rgb9_E5 => (SizedInternalFormat)All.Rgb9E5,
            _ => throw new InvalidEnumArgumentException(nameof(format), (int)format, typeof(TextureFormat)),
        };

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
            BufferStorageFlags glFlags = ToGLStorageFlags(flags);
            GL.NamedBufferStorage(Handle, data.Length * elementSize, ref data[0], glFlags);

            return new Buffer(name, Handle, bufferType, elementSize, data.Length, flags);
        }

        public static Buffer CreateDataBuffer<T>(string name, int elements, BufferFlags flags) where T : unmanaged
        {
            GLUtil.CreateBuffer(name, out int Handle);

            BufferDataType bufferType = GetAssiciatedBufferDataType<T>();

            int elementSize = Unsafe.SizeOf<T>();
            if (bufferType != BufferDataType.Custom)
                Debug.Assert(SizeInBytes(bufferType) == Unsafe.SizeOf<T>());

            BufferStorageFlags glFlags = ToGLStorageFlags(flags);
            // GLEXT: ARB_direct_access
            GL.NamedBufferStorage(Handle, elements * elementSize, IntPtr.Zero, glFlags);

            return new Buffer(name, Handle, bufferType, elementSize, elements, flags);
        }

        public static IndexBuffer CreateIndexBuffer(string name, Span<byte> data, BufferFlags flags)
        {
            GLUtil.CreateBuffer(name, out int Handle);

            IndexBufferType bufferType = GetAssiciatedIndexBufferType<byte>();

            Debug.Assert(SizeInBytes(bufferType) == Unsafe.SizeOf<byte>());

            // GLEXT: ARB_direct_access
            BufferStorageFlags glFlags = ToGLStorageFlags(flags);
            GL.NamedBufferStorage(Handle, data.Length * sizeof(byte), ref data[0], glFlags);

            return new IndexBuffer(name, Handle, bufferType, sizeof(byte), data.Length, flags);
        }

        public static IndexBuffer CreateIndexBuffer(string name, Span<short> data, BufferFlags flags)
        {
            GLUtil.CreateBuffer(name, out int Handle);

            IndexBufferType bufferType = GetAssiciatedIndexBufferType<ushort>();

            Debug.Assert(SizeInBytes(bufferType) == Unsafe.SizeOf<ushort>());

            // GLEXT: ARB_direct_access
            BufferStorageFlags glFlags = ToGLStorageFlags(flags);
            GL.NamedBufferStorage(Handle, data.Length * sizeof(ushort), ref data[0], glFlags);

            return new IndexBuffer(name, Handle, bufferType, sizeof(ushort), data.Length, flags);
        }

        public static IndexBuffer CreateIndexBuffer(string name, Span<ushort> data, BufferFlags flags)
        {
            GLUtil.CreateBuffer(name, out int Handle);

            IndexBufferType bufferType = GetAssiciatedIndexBufferType<ushort>();

            Debug.Assert(SizeInBytes(bufferType) == Unsafe.SizeOf<ushort>());

            // GLEXT: ARB_direct_access
            BufferStorageFlags glFlags = ToGLStorageFlags(flags);
            GL.NamedBufferStorage(Handle, data.Length * sizeof(ushort), ref data[0], glFlags);

            return new IndexBuffer(name, Handle, bufferType, sizeof(ushort), data.Length, flags);
        }

        public static IndexBuffer CreateIndexBuffer(string name, Span<int> data, BufferFlags flags)
        {
            GLUtil.CreateBuffer(name, out int Handle);

            IndexBufferType bufferType = GetAssiciatedIndexBufferType<uint>();

            Debug.Assert(SizeInBytes(bufferType) == Unsafe.SizeOf<uint>());

            // GLEXT: ARB_direct_access
            BufferStorageFlags glFlags = ToGLStorageFlags(flags);
            GL.NamedBufferStorage(Handle, data.Length * sizeof(uint), ref data[0], glFlags);

            return new IndexBuffer(name, Handle, bufferType, sizeof(uint), data.Length, flags);
        }

        public static IndexBuffer CreateIndexBuffer(string name, Span<uint> data, BufferFlags flags)
        {
            GLUtil.CreateBuffer(name, out int Handle);

            IndexBufferType bufferType = GetAssiciatedIndexBufferType<uint>();

            Debug.Assert(SizeInBytes(bufferType) == Unsafe.SizeOf<uint>());

            // GLEXT: ARB_direct_access
            BufferStorageFlags glFlags = ToGLStorageFlags(flags);
            GL.NamedBufferStorage(Handle, data.Length * sizeof(uint), ref data[0], glFlags);

            return new IndexBuffer(name, Handle, bufferType, sizeof(uint), data.Length, flags);
        }

        public static IndexBuffer CreateIndexBuffer<T>(string name, int elements, BufferFlags flags) where T : unmanaged
        {
            GLUtil.CreateBuffer(name, out int Handle);

            IndexBufferType bufferType = GetAssiciatedIndexBufferType<T>();

            var elementSize = SizeInBytes(bufferType);
            Debug.Assert(elementSize == Unsafe.SizeOf<T>());

            // GLEXT: ARB_direct_access
            BufferStorageFlags glFlags = ToGLStorageFlags(flags);
            GL.NamedBufferStorage(Handle, elements * Unsafe.SizeOf<T>(), IntPtr.Zero, glFlags);

            return new IndexBuffer(name, Handle, bufferType, elementSize, elements, flags);
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
            GL.NamedFramebufferTexture(fbo.Handle, FramebufferAttachment.ColorAttachment0 + index, colorAttachment.Handle, mipLevel);

            // Extend the array, add the attachment, sort the attachments by index
            fbo.ColorAttachments ??= new ColorAttachement[0];
            Array.Resize(ref fbo.ColorAttachments, fbo.ColorAttachments.Length + 1);
            fbo.ColorAttachments[^1] = new ColorAttachement(index, colorAttachment);
            Array.Sort(fbo.ColorAttachments, (a1, a2) => a1.Index - a2.Index);
        }

        public static void AddDepthAttachment(Framebuffer fbo, Texture depthTexture, int mipLevel)
        {
            GL.NamedFramebufferTexture(fbo.Handle, FramebufferAttachment.DepthAttachment, depthTexture.Handle, mipLevel);
            fbo.DepthAttachment = depthTexture;
        }

        public static void AddStencilAttachment(Framebuffer fbo, Texture stencilTexture, int mipLevel)
        {
            GL.NamedFramebufferTexture(fbo.Handle, FramebufferAttachment.StencilAttachment, stencilTexture.Handle, mipLevel);
            fbo.StencilAttachment = stencilTexture;
        }

        public static void AddDepthStencilAttachment(Framebuffer fbo, Texture depthStencilTexture, int mipLevel)
        {
            GL.NamedFramebufferTexture(fbo.Handle, FramebufferAttachment.DepthStencilAttachment, depthStencilTexture.Handle, mipLevel);
            fbo.DepthAttachment = depthStencilTexture;
            fbo.StencilAttachment = depthStencilTexture;
        }

        public static ShaderProgram CreateEmptyShaderProgram(string name, ShaderStage stage, bool separable)
        {
            GLUtil.CreateProgram(name, out int shader);
            GL.ProgramParameter(shader, ProgramParameterName.ProgramSeparable, separable ? 1 : 0);
            return new ShaderProgram(name, shader, stage, new Dictionary<string, int>(), null);
        }

        public static bool CreateShaderProgram(string name, ShaderStage stage, string[] sources, [NotNullWhen(true)] out ShaderProgram? program)
        {
            program = CreateEmptyShaderProgram(name, stage, true);

            GLUtil.CreateShader(name, ToGLShaderType(stage), out var shader);
            GL.ShaderSource(shader, sources.Length, sources, (int[]?)null);

            GL.CompileShader(shader);

            GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                string info = GL.GetShaderInfoLog(shader);
                Debug.WriteLine($"Error in {stage} shader '{name}':\n{info}");

                // Do some gl cleanup
                GL.DeleteProgram(program.Handle);
                GL.DeleteShader(shader);

                program = null;
                return false;
            }

            GL.AttachShader(program.Handle, shader);
            GL.LinkProgram(program.Handle);

            GL.DetachShader(program.Handle, shader);
            GL.DeleteShader(shader);

            GL.GetProgram(program.Handle, GetProgramParameterName.LinkStatus, out int isLinked);
            if (isLinked == 0)
            {
                string info = GL.GetProgramInfoLog(program.Handle);
                Debug.WriteLine($"Error in {stage} program '{name}':\n{info}");

                GL.DeleteProgram(program.Handle);
                program = null;
                return false;
            }

            // Now we can inspect this shader!

            Debug.WriteLine($"Uniforms for shader '{name}':");
            Debug.Indent();

            {
                GL.GetProgram(program.Handle, GetProgramParameterName.ActiveUniforms, out int uniformCount);

                program.UniformInfo = new UniformFieldInfo[uniformCount];

                for (int i = 0; i < uniformCount; i++)
                {
                    string uniformName = GL.GetActiveUniform(program.Handle, i, out int size, out ActiveUniformType type);
                    var location = GL.GetUniformLocation(program.Handle, uniformName);

                    UniformFieldInfo fieldInfo;
                    fieldInfo.Location = location;
                    fieldInfo.Name = uniformName;
                    fieldInfo.Size = size;
                    fieldInfo.Type = type;

                    program.UniformInfo[i] = fieldInfo;
                    program.UniformLocations.Add(uniformName, location);

                    Debug.WriteLine($"{name} uniform {location} '{uniformName}' {type} ({size})");
                }
            }

            {
                GL.GetProgram(program.Handle, GetProgramParameterName.ActiveUniformBlocks, out int uniformBlockCount);

                UniformBlockInfo[] blockInfo = new UniformBlockInfo[uniformBlockCount];

                for (int i = 0; i < uniformBlockCount; i++)
                {
                    GL.GetActiveUniformBlock(program.Handle, i, ActiveUniformBlockParameter.UniformBlockActiveUniforms, out int uniformsInBlockCount);

                    Span<int> uniformIndices = stackalloc int[uniformsInBlockCount];
                    GL.GetActiveUniformBlock(program.Handle, i, ActiveUniformBlockParameter.UniformBlockActiveUniformIndices, out uniformIndices[0]);

                    GL.GetActiveUniformBlock(program.Handle, i, ActiveUniformBlockParameter.UniformBlockNameLength, out int nameLength);
                    GL.GetActiveUniformBlockName(program.Handle, i, nameLength, out _, out string uniformBlockName);

                    blockInfo[i].BlockName = uniformBlockName;
                    blockInfo[i].BlockUniforms = new UniformFieldInfo[uniformIndices.Length];

                    Debug.WriteLine($"Block {i} '{uniformBlockName}':");
                    Debug.Indent();
                    var uniformInfo = blockInfo[i].BlockUniforms;
                    for (int j = 0; j < uniformIndices.Length; j++)
                    {
                        string uniformName = GL.GetActiveUniform(program.Handle, uniformIndices[j], out int uniformSize, out var uniformType);

                        uniformInfo[j].Location = uniformIndices[j];
                        uniformInfo[j].Name = uniformName;
                        uniformInfo[j].Size = uniformSize;
                        uniformInfo[j].Type = uniformType;

                        Debug.WriteLine($"{name} uniform {uniformIndices[j]} '{uniformName}' {uniformType} ({uniformSize})");
                    }
                    Debug.Unindent();
                }
                Debug.Unindent();
            }

            return true;
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

        public static Sampler CreateSampler2D(string name, MagFilter magFilter, MinFilter minFilter, float anisoLevel, WrapMode xAxisWrap, WrapMode yAxisWrap)
        {
            GLUtil.CreateSampler(name, out int sampler);
            
            GL.SamplerParameter(sampler, SamplerParameterName.TextureMagFilter, (int)ToGLTextureMagFilter(magFilter));
            GL.SamplerParameter(sampler, SamplerParameterName.TextureMinFilter, (int)ToGLTextureMinFilter(minFilter));

            GL.SamplerParameter(sampler, SamplerParameterName.TextureWrapS, (int)ToGLTextureWrapMode(xAxisWrap));
            GL.SamplerParameter(sampler, SamplerParameterName.TextureWrapS, (int)ToGLTextureWrapMode(yAxisWrap));

            GL.SamplerParameter(sampler, SamplerParameterName.TextureMaxAnisotropyExt, MathHelper.Clamp(anisoLevel, 1f, MaxAnisoLevel));

            return new Sampler(name, sampler, SamplerType.Sampler2D, SamplerDataType.Float, magFilter, minFilter, 0, -1000, 1000, 1.0f, xAxisWrap, yAxisWrap, WrapMode.Repeat, new Color4(0f, 0f, 0f, 0f), false);
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
            
            var posbuffer =    CreateDataBuffer<Vector3>($"{name}: Position", data.Positions, BufferFlags.None);
            var uvbuffer =     CreateDataBuffer<Vector2>($"{name}: UV", data.UVs, BufferFlags.None);
            var normalbuffer = CreateDataBuffer<Vector3>($"{name}: Normal", data.Normals, BufferFlags.None);

            return new Mesh(name, indexbuffer, posbuffer, uvbuffer, normalbuffer, null);
        }

        public static Texture Create1PixelTexture(string name, Color4 color)
        {
            GLUtil.CreateTexture(name, TextureTarget.Texture2D, out int texture);

            var format = TextureFormat.Rgba32F;
            GL.TextureStorage2D(texture, 1, ToGLSizedInternalFormat(format), 1, 1);

            GL.TextureSubImage2D(texture, 0, 0, 0, 1, 1, PixelFormat.Rgba, PixelType.Float, ref color);

            GL.GenerateTextureMipmap(texture);

            return new Texture(name, texture, TextureType.Texture2D, format, 1, 1, 1, 0, 0, 1);
        }

        #endregion

        public static void ReallocBuffer(ref Buffer buffer, int newElementCount)
        {
            GL.DeleteBuffer(buffer.Handle);
            GLUtil.CreateBuffer(buffer.Name, out buffer.Handle);

            BufferStorageFlags glFlags = ToGLStorageFlags(buffer.Flags);
            // GLEXT: ARB_direct_access
            GL.NamedBufferStorage(buffer.Handle, newElementCount * buffer.ElementSize, IntPtr.Zero, glFlags);
            buffer.Elements = newElementCount;
        }

        public static void ReallocBuffer(ref IndexBuffer buffer, int newSize)
        {
            GL.DeleteBuffer(buffer.Handle);
            GLUtil.CreateElementBuffer(buffer.Name, out buffer.Handle);

            BufferStorageFlags glFlags = ToGLStorageFlags(buffer.Flags);
            // GLEXT: ARB_direct_access
            GL.NamedBufferStorage(buffer.Handle, newSize * SizeInBytes(buffer.IndexType), IntPtr.Zero, glFlags);
            buffer.Elements = newSize;
        }

        #region Deletion

        // Deletes the buffer and resets it's contents
        public static void DeleteBuffer(ref Buffer? buffer)
        {
            GL.DeleteBuffer(buffer?.Handle ?? 0);
            buffer = default;
        }

        // Deletes the buffer and resets it's contents
        public static void DeleteBuffer(ref IndexBuffer? buffer)
        {
            GL.DeleteBuffer(buffer?.Handle ?? 0);
            buffer = default;
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
        }

        const int MinimumNumberOfVertexAttributes = 16;

        // Represents the state of the 16 guaranteed vertex attributes
        public static VertexAttribute[] Attributes = new VertexAttribute[MinimumNumberOfVertexAttributes];
        public static Buffer?[] AttributeBuffers     = new Buffer[MinimumNumberOfVertexAttributes];
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

            // Positions
            GL.VertexAttribFormat(0, 3, VertexAttribType.Float, false, 0);
            // UVs
            GL.VertexAttribFormat(1, 2, VertexAttribType.Float, false, 0);
            // Normals
            GL.VertexAttribFormat(2, 3, VertexAttribType.Float, false, 0);
            // Colors
            GL.VertexAttribFormat(3, 4, VertexAttribType.Float, false, 0);
        }

        public static void SetAndEnableVertexAttribute(int index, AttributeSpecification spec, int bufferOffset)
        {
            ref VertexAttribute attrib = ref Attributes[index];

            // Only set the vertex attrib format if it's actually different from what is already bound
            if (attrib.Size != spec.Size ||
                attrib.Type != spec.Type ||
                attrib.Normalized != spec.Normalized ||
                attrib.Offset != bufferOffset)
            {
                GL.VertexAttribFormat(index, spec.Size, ToGLAttribType(spec.Type), spec.Normalized, bufferOffset);

                attrib.Size = spec.Size;
                attrib.Type = spec.Type;
                attrib.Normalized = spec.Normalized;
                attrib.Offset = bufferOffset;
            }
            
            if (attrib.Active == false)
            {
                GL.EnableVertexAttribArray(index);
                attrib.Active = true;
            }
        }

        public static void DisableVertexAttribute(int index)
        {
            if (Attributes[index].Active)
            {
                GL.DisableVertexAttribArray(index);
                Attributes[index].Active = false;
            }
        }

        public static void SetAndEnableVertexAttributes(Span<AttributeSpecification> attribs, int bufferOffset)
        {
            for (int i = 0; i < attribs.Length; i++)
            {
                SetAndEnableVertexAttribute(i, attribs[i], bufferOffset);
            }
        }

        public static void BindVertexAttribBuffer(int index, Buffer buffer)
        {
            if (AttributeBuffers[index] != buffer)
            {
                GL.BindVertexBuffer(index, buffer!.Handle, IntPtr.Zero, buffer.ElementSize);

                AttributeBuffers[index] = buffer;
            }
        }

        public static void BindVertexAttribBuffer(int index, Buffer buffer, int offset)
        {
            if (AttributeBuffers[index] != buffer)
            {
                GL.BindVertexBuffer(index, buffer!.Handle, (IntPtr)offset, buffer.ElementSize);

                AttributeBuffers[index] = buffer;
            }
        }

        public static void ClearVertexAttribBuffer(int index)
        {
            if (AttributeBuffers[index] != null)
            {
                GL.BindVertexBuffer(index, 0, IntPtr.Zero, 0);
                AttributeBuffers[index] = null;
            }
        }

        public static void BindIndexBuffer(IndexBuffer buffer)
        {
            if (CurrentIndexBuffer != buffer)
            {
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, buffer.Handle);
                CurrentIndexBuffer = buffer;
            }
        }

        public static void UnbindIndexBuffer()
        {
            if (CurrentIndexBuffer != null)
            {
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
                CurrentIndexBuffer = null;
            }
        }

        #endregion

        #region Shaders and Uniforms

        public static ShaderPipeline? CurrentPipeline;

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
                ShaderStage.Fragment => CurrentPipeline.FramgmentProgram,

                ShaderStage.Compute => throw new NotImplementedException(),

                _ => throw new ArgumentException($"Unknown shader stage: {stage}", nameof(stage)),
            };

            if (prog == null)
                throw new ArgumentException($"The pipeline '{CurrentPipeline.Name}' does not contain a shader for the {stage} stage.", nameof(stage));

            return prog;
        }

        public static int GetUniformLocation(string uniform, ShaderStage stage)
        {
            var prog = GetPipelineStage(stage);

            if (prog == null)
            {
                Debug.Print($"Looking for a uniform '{uniform}' in a shader stage ({stage}) in the pipeline '{CurrentPipeline!.Name}' which doesn't have a shader for that stage!");
                return -1;
            }
            else
            {
                return GetUniformLocation(uniform, prog);
            }
        }

        public static int GetUniformLocation(string uniform, ShaderProgram program)
        {
            if (program.UniformLocations.TryGetValue(uniform, out int location))
            {
                return location;
            }
            else
            {
                Debug.Print($"The uniform '{uniform}' does not exist in the shader '{program.Name}'!");
                program.UniformLocations.Add(uniform, -1);
                return -1;
            }
        }

        public static void UniformMatrix4(string uniformName, ShaderStage stage, bool transpose, ref Matrix4 matrix)
        {
            var prog = GetPipelineStage(stage);
            var location = GetUniformLocation(uniformName, prog);

            GL.ProgramUniformMatrix4(prog.Handle, location, transpose, ref matrix);
        }

        public static void UniformMatrix3(string uniformName, ShaderStage stage, bool transpose, ref Matrix3 matrix)
        {
            var prog = GetPipelineStage(stage);
            var location = GetUniformLocation(uniformName, prog);

            GL.ProgramUniformMatrix3(prog.Handle, location, transpose, ref matrix);
        }

        public static void UniformVector3(string uniformName, ShaderStage stage, Vector3 vec3)
        {
            var prog = GetPipelineStage(stage);
            var location = GetUniformLocation(uniformName, prog);

            GL.ProgramUniform3(prog.Handle, location, vec3);
        }

        public static void Uniform1(string uniformName, ShaderStage stage, int i)
        {
            var prog = GetPipelineStage(stage);
            var location = GetUniformLocation(uniformName, prog);

            GL.ProgramUniform1(prog.Handle, location, i);
        }

        #endregion

        #region Texture Binding



        #endregion
    }
}
