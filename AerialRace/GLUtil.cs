﻿using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace AerialRace
{
    public static class GLUtil
    {
        public static void LabelObject(ObjectLabelIdentifier objLabelIdent, int glObject, string name)
        {
            GL.ObjectLabel(objLabelIdent, glObject, name.Length, name);
        }

        public static void CreateProgramPipeline(string Name, out int ProgramPipeline)
        {
            GL.CreateProgramPipelines(1, out ProgramPipeline);
            LabelObject(ObjectLabelIdentifier.Program, ProgramPipeline, $"Pipeline: {Name}");
        }

        public static void CreateProgram(string Name, out int Program)
        {
            Program = GL.CreateProgram();
            LabelObject(ObjectLabelIdentifier.Program, Program, $"Program: {Name}");
        }

        public static void CreateShader(string Name, ShaderType type, out int Shader)
        {
            Shader = GL.CreateShader(type);
            LabelObject(ObjectLabelIdentifier.Shader, Shader, $"Shader: {type}: {Name}");
        }

        public static void CreateBuffer(string Name, out int Buffer)
        {
            GL.CreateBuffers(1, out Buffer);
            LabelObject(ObjectLabelIdentifier.Buffer, Buffer, $"Buffer: {Name}");
        }

        public static void CreateVertexBuffer(string Name, out int Buffer) => CreateBuffer($"VBO: {Name}", out Buffer);

        public static void CreateElementBuffer(string Name, out int Buffer) => CreateBuffer($"EBO: {Name}", out Buffer);

        public static void CreateVertexArray(string Name, out int VAO)
        {
            GL.CreateVertexArrays(1, out VAO);
            LabelObject(ObjectLabelIdentifier.VertexArray, VAO, $"VAO: {Name}");
        }

        public static void CreateTexture(TextureTarget target, string Name, out int Texture)
        {
            GL.CreateTextures(target, 1, out Texture);
            LabelObject(ObjectLabelIdentifier.Texture, Texture, $"Texture: {Name}");
        }

        public static void CreateFramebuffer(string Name, out int FBO)
        {
            GL.CreateFramebuffers(1, out FBO);
            LabelObject(ObjectLabelIdentifier.Framebuffer, FBO, $"Framebuffer: {Name}");
        }

        // FIXME: Idk what this should be!!
        private const int DebugGroupMessageId = 1;

        public static void PushDebugGroup(string Message)
        {
            GL.PushDebugGroup(DebugSourceExternal.DebugSourceApplication, DebugGroupMessageId, Message.Length, Message);
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
    }
}