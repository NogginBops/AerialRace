using OpenTK.Graphics.GL;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using GLFrameBufferTarget = OpenTK.Graphics.OpenGL4.FramebufferTarget;

namespace AerialRace.RenderData
{
    static class RenderDataUtil
    {
        public static int SizeInBytes(BufferDataType type) => type switch
        {
            BufferDataType.UInt8  => sizeof(byte),
            BufferDataType.UInt16 => sizeof(ushort),
            BufferDataType.UInt32 => sizeof(uint),
            BufferDataType.UInt64 => sizeof(ulong),

            BufferDataType.Int8   => sizeof(sbyte),
            BufferDataType.Int16  => sizeof(short),
            BufferDataType.Int32  => sizeof(int),
            BufferDataType.Int64  => sizeof(long),

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

            _ => throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(BufferDataType)),
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BufferDataType GetAssiciatedBufferDataType<T>() where T : unmanaged => (default(T)) switch
        {
            byte _   => BufferDataType.UInt8,
            ushort _ => BufferDataType.UInt16,
            uint _   => BufferDataType.UInt32,
            ulong _  => BufferDataType.UInt64,

            sbyte _ => BufferDataType.Int8,
            short _ => BufferDataType.Int16,
            int _   => BufferDataType.Int32,
            long _  => BufferDataType.Int64,

            float _   => BufferDataType.Float,
            Vector2 _ => BufferDataType.Float2,
            Vector3 _ => BufferDataType.Float3,
            Vector4 _ => BufferDataType.Float4,

            _ => throw new ArgumentException($"No buffer data type that matches type: '{typeof(T)}'"),
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BufferSize(Buffer buffer) => buffer.Elements * SizeInBytes(buffer.DataType);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BufferSize(IndexBuffer buffer) => buffer.Elements * SizeInBytes(buffer.IndexType);

        #region Creation 

        public static Buffer CreateDataBuffer<T>(string name, Span<T> data, BufferFlags flags) where T : unmanaged
        {
            GLUtil.CreateBuffer(name, out int Handle);

            BufferDataType bufferType = GetAssiciatedBufferDataType<T>();

            Debug.Assert(SizeInBytes(bufferType) == Unsafe.SizeOf<T>());

            // GLEXT: ARB_direct_access
            BufferStorageFlags glFlags = ToGLStorageFlags(flags);
            GL.NamedBufferStorage(Handle, data.Length * SizeInBytes(bufferType), ref data[0], glFlags);

            return new Buffer(name, Handle, bufferType, data.Length);
        }

        public static IndexBuffer CreateIndexBuffer(string name, Span<byte> data, BufferFlags flags)
        {
            GLUtil.CreateBuffer(name, out int Handle);

            IndexBufferType bufferType = GetAssiciatedIndexBufferType<byte>();

            Debug.Assert(SizeInBytes(bufferType) == Unsafe.SizeOf<byte>());

            // GLEXT: ARB_direct_access
            BufferStorageFlags glFlags = ToGLStorageFlags(flags);
            GL.NamedBufferStorage(Handle, data.Length * sizeof(byte), ref data[0], glFlags);

            return new IndexBuffer(name, Handle, bufferType, data.Length);
        }

        public static IndexBuffer CreateIndexBuffer(string name, Span<short> data, BufferFlags flags)
        {
            GLUtil.CreateBuffer(name, out int Handle);

            IndexBufferType bufferType = GetAssiciatedIndexBufferType<short>();

            Debug.Assert(SizeInBytes(bufferType) == Unsafe.SizeOf<short>());

            // GLEXT: ARB_direct_access
            BufferStorageFlags glFlags = ToGLStorageFlags(flags);
            GL.NamedBufferStorage(Handle, data.Length * sizeof(short), ref data[0], glFlags);

            return new IndexBuffer(name, Handle, bufferType, data.Length);
        }

        public static IndexBuffer CreateIndexBuffer(string name, Span<int> data, BufferFlags flags)
        {
            GLUtil.CreateBuffer(name, out int Handle);

            IndexBufferType bufferType = GetAssiciatedIndexBufferType<uint>();

            Debug.Assert(SizeInBytes(bufferType) == Unsafe.SizeOf<uint>());

            // GLEXT: ARB_direct_access
            BufferStorageFlags glFlags = ToGLStorageFlags(flags);
            GL.NamedBufferStorage(Handle, data.Length * sizeof(uint), ref data[0], glFlags);

            return new IndexBuffer(name, Handle, bufferType, data.Length);
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
            fbo.DepthAttachment   = depthStencilTexture;
            fbo.StencilAttachment = depthStencilTexture;
        }

        public static ShaderProgram CreateEmptyShaderProgram(string name, ShaderStage stage, bool separable)
        {
            GLUtil.CreateProgram(name, out int shader);
            GL.ProgramParameter(shader, ProgramParameterName.ProgramSeparable, separable ? 1 : 0);
            return new ShaderProgram(name, shader, stage);
        }

        public static bool CreateShaderProgram(string name, ShaderStage stage, string[] sources, [NotNullWhen(true)] out ShaderProgram? program)
        {
            program = CreateEmptyShaderProgram(name, stage, true);

            var shader = GL.CreateShader(ToGLShaderType(stage));
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

        #endregion
    }
}
