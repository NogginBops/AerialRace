using AerialRace.Loading;
using AerialRace.RenderData;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace AerialRace
{
    internal class Window : GameWindow
    {
        public Window(GameWindowSettings gwSettings, NativeWindowSettings nwSettins) : base(gwSettings, nwSettins)
        {
            Title += ": OpenGL Version: " + GL.GetString(StringName.Version);
        }

        Material Material;
        Mesh Mesh;

        Camera Camera;
        Transform CameraTransform;

        protected override void OnLoad()
        {
            base.OnLoad();
            Directory.SetCurrentDirectory("..\\..\\..\\Assets");

            var (Width, Height) = Size;
            Camera = new Camera(90, Width / (float)Height, 0.1f, 1000f, Color4.DarkBlue);
            CameraTransform = new Transform();

            var meshData = MeshLoader.LoadObjMesh("C:/Users/juliu/source/repos/CoolGraphics/CoolGraphics/Assets/Models/pickaxe02.obj");

            var indexbuffer = RenderDataUtil.CreateIndexBuffer("pickaxe02.obj", meshData.Int32Indices, BufferFlags.None);

            var posbuffer    = RenderDataUtil.CreateDataBuffer<Vector3>("pixaxe02.obj: Position", meshData.Positions, BufferFlags.None);
            var uvbuffer     = RenderDataUtil.CreateDataBuffer<Vector2>("pixaxe02.obj: UV",       meshData.UVs,       BufferFlags.None);
            var normalbuffer = RenderDataUtil.CreateDataBuffer<Vector3>("pixaxe02.obj: Normal",   meshData.Normals,   BufferFlags.None);

            Mesh = new Mesh("pickaxe02.obj", indexbuffer, posbuffer, uvbuffer, normalbuffer, null);

            RenderDataUtil.CreateShaderProgram("Standard Vertex Shader", ShaderStage.Vertex, new[] { File.ReadAllText("./Shaders/StandardVertex.vert") }, out ShaderProgram? vertexProgram);
            RenderDataUtil.CreateShaderProgram("UV Debug Fragment Shader", ShaderStage.Fragment, new[] { File.ReadAllText("./Shaders/Debug.frag") }, out ShaderProgram? fragmentProgram);

            var firstShader = RenderDataUtil.CreateEmptyPipeline("First shader pipeline");
            RenderDataUtil.AssembleProgramPipeline(firstShader, vertexProgram, null, fragmentProgram);

            Material = new Material("First Material", firstShader, null);
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            GL.ClearColor(Camera.ClearColor);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);



            SwapBuffers();
        }
    }
}
