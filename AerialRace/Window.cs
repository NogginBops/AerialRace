using AerialRace.Loading;
using AerialRace.RenderData;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
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

        Mesh QuadMesh;

        Camera Camera;
        Transform CameraTransform;

        private readonly static DebugProc DebugProcCallback = Window_DebugProc;
        private static GCHandle DebugProcGCHandle;

        protected override void OnLoad()
        {
            base.OnLoad();
            Directory.SetCurrentDirectory("..\\..\\..\\Assets");

            DebugProcGCHandle = GCHandle.Alloc(DebugProcCallback, GCHandleType.Normal);
            GL.DebugMessageCallback(DebugProcCallback, IntPtr.Zero);
            GL.Enable(EnableCap.DebugOutput);
            GL.Enable(EnableCap.DebugOutputSynchronous);

            // Enable backface culling
            // FIXME: This should be a per-material setting
            //GL.Enable(EnableCap.CullFace);
            //GL.CullFace(CullFaceMode.Front);

            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);

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

            RenderDataUtil.CreateShaderProgram("Debug Vertex Shader", ShaderStage.Vertex, new[] { File.ReadAllText("./Shaders/SimpleVertex.vert") }, out ShaderProgram? debugVertex);
            RenderDataUtil.CreateShaderProgram("Debug Fragment Shader", ShaderStage.Fragment, new[] { File.ReadAllText("./Shaders/SimpleDebug.frag") }, out ShaderProgram? debugFragment);

            var debugShader = RenderDataUtil.CreateEmptyPipeline("Debug Shader");
            RenderDataUtil.AssembleProgramPipeline(debugShader, debugVertex, null, debugFragment);

            //Material = new Material("First Material", firstShader, null);
            Material = new Material("Debug Material", debugShader, null);

            StaticGeometry.InitBuffers();

            QuadMesh = new Mesh("Unit Quad",
                StaticGeometry.UnitQuadIndexBuffer,
                StaticGeometry.CenteredUnitQuadPositionsBuffer,
                StaticGeometry.UnitQuadUVsBuffer,
                StaticGeometry.UnitQuadNormalsBuffer,
                null);
            QuadMesh.VertexColors = StaticGeometry.UnitQuadDebugColorsBuffer;


            // Setup an always bound VAO
            {
                GLUtil.CreateVertexArray("The one VAO", out int VAO);
                GL.BindVertexArray(VAO);

                // Positions
                GL.VertexAttribFormat(0, 3, VertexAttribType.Float, false, 0);
                // UVs
                GL.VertexAttribFormat(1, 2, VertexAttribType.Float, false, 0);
                // Normals
                GL.VertexAttribFormat(2, 3, VertexAttribType.Float, false, 0);
                // Colors
                GL.VertexAttribFormat(3, 4, VertexAttribType.Float, false, 0);
            }
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            GL.ClearColor(Camera.ClearColor);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.EnableVertexAttribArray(0);
            GL.BindVertexBuffer(0, QuadMesh.Positions!.Handle, IntPtr.Zero, RenderDataUtil.SizeInBytes(QuadMesh.Positions.DataType));

            GL.EnableVertexAttribArray(1);
            GL.BindVertexBuffer(1, QuadMesh.UVs!.Handle, IntPtr.Zero, RenderDataUtil.SizeInBytes(QuadMesh.UVs.DataType));

            GL.EnableVertexAttribArray(2);
            GL.BindVertexBuffer(2, QuadMesh.Normals!.Handle, IntPtr.Zero, RenderDataUtil.SizeInBytes(QuadMesh.Normals.DataType));

            GL.EnableVertexAttribArray(3);
            GL.BindVertexBuffer(3, QuadMesh.VertexColors!.Handle, IntPtr.Zero, RenderDataUtil.SizeInBytes(QuadMesh.VertexColors.DataType));

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, QuadMesh.Indices!.Handle);

            GL.UseProgram(0);
            GL.BindProgramPipeline(Material.Pipeline.Handle);

            Transform transform = new Transform();
            transform.Position = new Vector3(0f, 0, -2f);
            transform.GetTransformationMatrix(out var transformMat4x3);
            var transformMat = new Matrix4(
                new Vector4(transformMat4x3.Row0, 0),
                new Vector4(transformMat4x3.Row1, 0),
                new Vector4(transformMat4x3.Row2, 0),
                new Vector4(transformMat4x3.Row3, 1));

            Camera.CalcProjectionMatrix(out var proj);

            var mvp = transformMat * proj;
            GL.ProgramUniformMatrix4(Material.Pipeline.VertexProgram!.Handle, 1, true, ref proj);
            GL.ProgramUniformMatrix4(Material.Pipeline.VertexProgram!.Handle, 0, true, ref transformMat);

            GL.DrawElements(PrimitiveType.Triangles, QuadMesh.Indices.Elements, RenderDataUtil.ToGLDrawElementsType(QuadMesh.Indices.IndexType), 0);

            transform.Position = new Vector3(0f, 1f, -2f);
            transform.Rotation = Quaternion.FromAxisAngle(new Vector3(0, 1, 0), (MathF.PI * 3) / 4 );
            transform.GetTransformationMatrix(out transformMat4x3);
            transformMat = new Matrix4(
                new Vector4(transformMat4x3.Row0, 0),
                new Vector4(transformMat4x3.Row1, 0),
                new Vector4(transformMat4x3.Row2, 0),
                new Vector4(transformMat4x3.Row3, 1));

            Camera.CalcProjectionMatrix(out proj);

            mvp = transformMat * proj;
            GL.ProgramUniformMatrix4(Material.Pipeline.VertexProgram!.Handle, 1, true, ref proj);
            GL.ProgramUniformMatrix4(Material.Pipeline.VertexProgram!.Handle, 0, true, ref transformMat);

            GL.DrawElements(PrimitiveType.Triangles, QuadMesh.Indices.Elements, RenderDataUtil.ToGLDrawElementsType(QuadMesh.Indices.IndexType), 0);

            SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            HandleKeyboard(KeyboardState);
        }

        public void HandleKeyboard(KeyboardState keyboard)
        {
            
            if (IsKeyPressed(Keys.Escape))
            {
                Close();
            }
        }

        private static void Window_DebugProc(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr messagePtr, IntPtr userParam)
        {
            string message = Marshal.PtrToStringAnsi(messagePtr, length);

            bool showMessage = true;

            switch (source)
            {
                case DebugSource.DebugSourceApplication:
                    showMessage = false;
                    break;
                case DebugSource.DontCare:
                case DebugSource.DebugSourceApi:
                case DebugSource.DebugSourceWindowSystem:
                case DebugSource.DebugSourceShaderCompiler:
                case DebugSource.DebugSourceThirdParty:
                case DebugSource.DebugSourceOther:
                default:
                    showMessage = true;
                    break;
            }

            if (showMessage)
            {
                switch (severity)
                {
                    case DebugSeverity.DontCare:
                        Debug.Print($"[DontCare] {message}");
                        break;
                    case DebugSeverity.DebugSeverityNotification:
                        //Debug.Print($"[Notification] [{source}] {message}");
                        break;
                    case DebugSeverity.DebugSeverityHigh:
                        Debug.Print($"[Error] [{source}] {message}");
                        break;
                    case DebugSeverity.DebugSeverityMedium:
                        Debug.Print($"[Warning] [{source}] {message}");
                        break;
                    case DebugSeverity.DebugSeverityLow:
                        Debug.Print($"[Info] [{source}] {message}");
                        break;
                    default:
                        Debug.Print($"[default] {message}");
                        break;
                }
            }
        }
    }
}
