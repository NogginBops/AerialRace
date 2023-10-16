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
            GL.CreateProgramPipeline(out ProgramPipeline);
            LabelObject(ObjectIdentifier.ProgramPipeline, ProgramPipeline, $"Pipeline: {Name}");
        }

        public static void CreateProgram(string Name, out int Program)
        {
            Program = GL.CreateProgram();
            LabelObject(ObjectIdentifier.Program, Program, $"Program: {Name}");
        }

        public static void CreateShader(string Name, ShaderType type, out int Shader)
        {
            Shader = GL.CreateShader(type);
            LabelObject(ObjectIdentifier.Shader, Shader, $"Shader: {type}: {Name}");
        }

        public static void CreateBuffer(string Name, out int Buffer)
        {
            GL.CreateBuffer(out Buffer);
            LabelObject(ObjectIdentifier.Buffer, Buffer, $"Buffer: {Name}");
        }

        public static void CreateVertexBuffer(string Name, out int Buffer) => CreateBuffer($"VBO: {Name}", out Buffer);

        public static void CreateElementBuffer(string Name, out int Buffer) => CreateBuffer($"EBO: {Name}", out Buffer);

        public static void CreateVertexArray(string Name, out int VAO)
        {
            GL.CreateVertexArray(out VAO);
            LabelObject(ObjectIdentifier.VertexArray, VAO, $"VAO: {Name}");
        }

        public static void CreateTexture(string Name, TextureTarget target, out int Texture)
        {
            GL.CreateTexture(target, out Texture);
            LabelObject(ObjectIdentifier.Texture, Texture, $"Texture: {Name}");
        }

        public static void CreateSampler(string Name, out int Sampler)
        {
            GL.CreateSampler(out Sampler);
            LabelObject(ObjectIdentifier.Sampler, Sampler, $"Sampler: {Name}");
        }

        public static void CreateFramebuffer(string Name, out int FBO)
        {
            GL.CreateFramebuffer(out FBO);
            LabelObject(ObjectIdentifier.Framebuffer, FBO, $"Framebuffer: {Name}");
        }

        public static void CreateQueries(string Name, QueryTarget target, int[] queries)
        {
            GL.CreateQueries(target, queries);
            for (int i = 0; i < queries.Length; i++)
            {
                LabelObject(ObjectIdentifier.Query, queries[i], $"Query: {Name} #{i + 1}");
            }
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
