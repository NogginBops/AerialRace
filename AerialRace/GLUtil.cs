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

        public static void CreateProgramPipeline(string Name, out int ProgramPipeline)
        {
            Unsafe.SkipInit(out ProgramPipeline);
            GL.CreateProgramPipeline(out Unsafe.As<int, uint>(ref ProgramPipeline));
            LabelObject(ObjectIdentifier.ProgramPipeline, ProgramPipeline, $"Pipeline: {Name}");
        }

        public static void CreateProgram(string Name, out int Program)
        {
            Program = (int)GL.CreateProgram();
            LabelObject(ObjectIdentifier.Program, Program, $"Program: {Name}");
        }

        public static void CreateShader(string Name, ShaderType type, out int Shader)
        {
            Shader = (int)GL.CreateShader(type);
            LabelObject(ObjectIdentifier.Shader, Shader, $"Shader: {type}: {Name}");
        }

        public static void CreateBuffer(string Name, out int Buffer)
        {
            Buffer = (int)GL.CreateBuffer();
            LabelObject(ObjectIdentifier.Buffer, Buffer, $"Buffer: {Name}");
        }

        public static void CreateVertexBuffer(string Name, out int Buffer) => CreateBuffer($"VBO: {Name}", out Buffer);

        public static void CreateElementBuffer(string Name, out int Buffer) => CreateBuffer($"EBO: {Name}", out Buffer);

        public static void CreateVertexArray(string Name, out int VAO)
        {
            VAO = (int)GL.CreateVertexArray();
            LabelObject(ObjectIdentifier.VertexArray, VAO, $"VAO: {Name}");
        }

        public static void CreateTexture(string Name, TextureTarget target, out int Texture)
        {
            Texture = (int)GL.CreateTexture(target);
            LabelObject(ObjectIdentifier.Texture, Texture, $"Texture: {Name}");
        }

        public static void CreateSampler(string Name, out int Sampler)
        {
            Sampler = (int)GL.CreateSampler();
            LabelObject(ObjectIdentifier.Sampler, Sampler, $"Sampler: {Name}");
        }

        public static void CreateFramebuffer(string Name, out int FBO)
        {
            FBO = (int)GL.CreateFramebuffer();
            LabelObject(ObjectIdentifier.Framebuffer, FBO, $"Framebuffer: {Name}");
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
