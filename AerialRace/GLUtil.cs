using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace AerialRace
{
    public static class GLUtil
    {
        public static void LabelObject(ObjectIdentifier objLabelIdent, int glObject, string name)
        {
            GL.ObjectLabel(objLabelIdent, (uint)glObject, name.Length, name);
        }

        public static void CreateProgramPipeline(string Name, out ProgramPipelineHandle ProgramPipeline)
        {
            Unsafe.SkipInit(out ProgramPipeline);
            GL.CreateProgramPipeline(out ProgramPipeline);
            LabelObject(ObjectIdentifier.ProgramPipeline, ProgramPipeline.Handle, $"Pipeline: {Name}");
        }

        public static void CreateProgram(string Name, out ProgramHandle Program)
        {
            Program = GL.CreateProgram();
            LabelObject(ObjectIdentifier.Program, Program.Handle, $"Program: {Name}");
        }

        public static void CreateShader(string Name, ShaderType type, out ShaderHandle Shader)
        {
            Shader = GL.CreateShader(type);
            LabelObject(ObjectIdentifier.Shader, Shader.Handle, $"Shader: {type}: {Name}");
        }

        public static void CreateBuffer(string Name, out BufferHandle Buffer)
        {
            Buffer = GL.CreateBuffer();
            LabelObject(ObjectIdentifier.Buffer, Buffer.Handle, $"Buffer: {Name}");
        }

        public static void CreateVertexBuffer(string Name, out BufferHandle Buffer) => CreateBuffer($"VBO: {Name}", out Buffer);

        public static void CreateElementBuffer(string Name, out BufferHandle Buffer) => CreateBuffer($"EBO: {Name}", out Buffer);

        public static void CreateVertexArray(string Name, out VertexArrayHandle VAO)
        {
            VAO = GL.CreateVertexArray();
            LabelObject(ObjectIdentifier.VertexArray, VAO.Handle, $"VAO: {Name}");
        }

        public static void CreateTexture(string Name, TextureTarget target, out TextureHandle Texture)
        {
            Texture = GL.CreateTexture(target);
            LabelObject(ObjectIdentifier.Texture, Texture.Handle, $"Texture: {Name}");
        }

        public static void CreateSampler(string Name, out SamplerHandle Sampler)
        {
            Sampler = GL.CreateSampler();
            LabelObject(ObjectIdentifier.Sampler, Sampler.Handle, $"Sampler: {Name}");
        }

        public static void CreateFramebuffer(string Name, out FramebufferHandle FBO)
        {
            FBO = GL.CreateFramebuffer();
            LabelObject(ObjectIdentifier.Framebuffer, FBO.Handle, $"Framebuffer: {Name}");
        }

        // FIXME: Idk what this should be!!
        private const int DebugGroupMessageId = 1;

        public static void PushDebugGroup(string Message)
        {
            GL.PushDebugGroup(DebugSource.DebugSourceApplication, DebugGroupMessageId, Message.Length, Message);
        }

        public static void PushShadowMapPass(string Name)
        {
            PushDebugGroup($"Shadow map Pass: {Name}");
        }

        // FIXME: Append target information!! e.g. (1 Target + Depth)
        public static void PushColorPass(string Name)
        {
            PushDebugGroup($"Color pass: {Name}");
        }

        public static void PushPostProcessPass(string Name)
        {
            PushDebugGroup($"PostFX: {Name}");
        }

        public static void PopDebugGroup()
        {
            GL.PopDebugGroup();
        }

        [Conditional("DEBUG")]
        public static void CheckGLError(string title)
        {
            var error = GL.GetError();
            if (error != ErrorCode.NoError)
            {
                Debug.Print($"{title}: {error}");
            }
        }

    }
}
