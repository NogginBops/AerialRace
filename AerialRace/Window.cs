using AerialRace.DebugGui;
using AerialRace.Loading;
using AerialRace.RenderData;
using ImGuiNET;
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

        ImGuiController imGuiController;

        Material Material;
        Mesh Mesh;

        Mesh QuadMesh;
        Transform QuadTransform;

        Transform ChildTransform;

        Transform FloorTransform;

        Camera Camera;

        Texture TestTexture;
        Sampler DebugSampler;

        AttributeSpecification[] StandardAttributes;

        private readonly static DebugProc DebugProcCallback = Window_DebugProc;
#pragma warning disable IDE0052 // Remove unread private members
        private static GCHandle DebugProcGCHandle;
#pragma warning restore IDE0052 // Remove unread private members

        protected override void OnLoad()
        {
            base.OnLoad();
            Directory.SetCurrentDirectory("..\\..\\..\\Assets");

            DebugProcGCHandle = GCHandle.Alloc(DebugProcCallback, GCHandleType.Normal);
            GL.DebugMessageCallback(DebugProcCallback, IntPtr.Zero);
            GL.Enable(EnableCap.DebugOutput);
#if DEBUG
            // Disables two core driver, we don't want this in a release build
            GL.Enable(EnableCap.DebugOutputSynchronous);
#endif

            // Enable backface culling
            // FIXME: This should be a per-material setting
            //GL.Enable(EnableCap.CullFace);
            //GL.CullFace(CullFaceMode.Front);

            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);

            RenderDataUtil.QueryLimits();

            var (Width, Height) = Size;
            Camera = new Camera(90, Width / (float)Height, 0.1f, 1000f, Color4.DarkBlue);
            Camera.Transform.LocalPosition = new Vector3(0, 1f, 0);

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

            QuadTransform = new Transform(new Vector3(0f, 0f, -2f), Quaternion.FromAxisAngle(Vector3.UnitY, MathF.PI/4f));

            ChildTransform = new Transform(new Vector3(1f, 1f, 0f));

            QuadTransform.Children = new List<Transform>();
            QuadTransform.Children.Add(ChildTransform);
            ChildTransform.Parent = QuadTransform;

            //QuadTransform.Children.Add(Camera.Transform);
            //Camera.Transform.Parent = QuadTransform;

            FloorTransform = new Transform(new Vector3(0, 0, 0), Quaternion.FromAxisAngle(Vector3.UnitX, MathF.PI / 2), Vector3.One * 5);

            TestTexture = TextureLoader.LoadRgbaImage("UV Test", "./Textures/uvtest.png", true, false);

            DebugSampler = RenderDataUtil.CreateSampler2D("DebugSampler", MagFilter.Linear, MinFilter.LinearMipmapLinear, 16f, WrapMode.Repeat, WrapMode.Repeat);

            StandardAttributes = new[]
            {
                // Position
                new AttributeSpecification("Position",     3, RenderData.AttributeType.Float, false),
                new AttributeSpecification("UV",           2, RenderData.AttributeType.Float, false),
                new AttributeSpecification("Normal",       3, RenderData.AttributeType.Float, false),
                new AttributeSpecification("VertexColor1", 4, RenderData.AttributeType.Float, false),
            };

            imGuiController = new ImGuiController(Width, Height);

            // Setup an always bound VAO
            RenderDataUtil.SetupGlobalVAO();
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            imGuiController.Update(this, (float)args.Time);
            // Update above calls ImGui.NewFrame()...
            // ImGui.NewFrame();

            //Transformations.LinearizeTransformations(Transform.Roots, )

            QuadTransform.UpdateMatrices();
            ChildTransform.UpdateMatrices();
            Camera.Transform.UpdateMatrices();
            FloorTransform.UpdateMatrices();

            GL.ClearColor(Camera.ClearColor);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            RenderDataUtil.SetAndEnableVertexAttributes(StandardAttributes, 0);

            RenderDataUtil.BindVertexAttribBuffer(0, QuadMesh.Positions!);
            RenderDataUtil.BindVertexAttribBuffer(1, QuadMesh.UVs!);
            RenderDataUtil.BindVertexAttribBuffer(2, QuadMesh.Normals!);
            RenderDataUtil.BindVertexAttribBuffer(3, QuadMesh.VertexColors!);

            RenderDataUtil.BindIndexBuffer(QuadMesh.Indices!);

            GL.UseProgram(0);
            RenderDataUtil.UsePipeline(Material.Pipeline);

            GL.BindTextureUnit(0, TestTexture.Handle);
            GL.BindSampler(0, DebugSampler.Handle);

            QuadTransform.GetTransformationMatrix(out var transformMatrix);

            var viewMatrix = Camera.Transform.WorldToLocal;

            Camera.CalcProjectionMatrix(out var proj);

            Transformations.MultMVP(ref transformMatrix, ref viewMatrix, ref proj, out var mvp);
            
            RenderDataUtil.UniformMatrix4("mvp", ShaderStage.Vertex, true, ref mvp);

            GL.DrawElements(PrimitiveType.Triangles, QuadMesh.Indices!.Elements, RenderDataUtil.ToGLDrawElementsType(QuadMesh.Indices.IndexType), 0);

            FloorTransform.GetTransformationMatrix(out transformMatrix);
            Transformations.MultMVP(ref transformMatrix, ref viewMatrix, ref proj, out mvp);

            //Debug.WriteLine($"Equal: {Camera.Transform.Forward == Camera.Transform.Forward2}, Diff: {Camera.Transform.Forward - Camera.Transform.Forward2}");

            RenderDataUtil.UniformMatrix4("mvp", ShaderStage.Vertex, true, ref mvp);

            GL.DrawElements(PrimitiveType.Triangles, QuadMesh.Indices!.Elements, RenderDataUtil.ToGLDrawElementsType(QuadMesh.Indices.IndexType), 0);

            RenderDataUtil.BindVertexAttribBuffer(0, Mesh.Positions!);
            RenderDataUtil.BindVertexAttribBuffer(1, Mesh.UVs!);
            RenderDataUtil.BindVertexAttribBuffer(2, Mesh.Normals!);
            // RenderDataUtil.BindVertexAttribBuffer(3, Mesh.VertexColors!);
            RenderDataUtil.DisableVertexAttribute(3);

            RenderDataUtil.BindIndexBuffer(Mesh.Indices!);

            ChildTransform.GetTransformationMatrix(out transformMatrix);

            viewMatrix = Camera.Transform.WorldToLocal;

            Camera.CalcProjectionMatrix(out proj);

            Transformations.MultMVP(ref transformMatrix, ref viewMatrix, ref proj, out mvp);

            RenderDataUtil.UniformMatrix4("mvp", ShaderStage.Vertex, true, ref mvp);

            GL.DrawElements(PrimitiveType.Triangles, Mesh.Indices!.Elements, RenderDataUtil.ToGLDrawElementsType(Mesh.Indices.IndexType), 0);

            ImGui.ShowDemoWindow();

            ImGui.EndFrame();
            imGuiController.Render();

            // FIXME: Reset gl state!
            // The blend mode is changed to this after imgui
            //GL.BlendEquation(BlendEquationMode.FuncAdd);
            //GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            //GL.Enable(EnableCap.CullFace);
            GL.Enable(EnableCap.DepthTest);

            SwapBuffers();
        }

        float TotalTime = 0;
        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            TotalTime += (float)args.Time;

            if (args.Time > 0.17)
            {
                Debug.WriteLine($"Long frame time: {args.Time:0.000}");
            }

            QuadTransform.LocalRotation *= Quaternion.FromAxisAngle(new Vector3(0, -1, 0), 0.1f * MathF.PI * (float)args.Time);
            QuadTransform.LocalPosition = new Vector3(1, MathF.Sin(TotalTime * MathF.PI * 0.2f), -2);

            //ChildTransform.Rotation *= Quaternion.FromAxisAngle(new Vector3(1, 0, 0), MathF.PI * (float)args.Time);

            //Camera.Transform.Rotation *= Quaternion.FromAxisAngle(new Vector3(0, 1, 0), 2 * MathF.PI * (float)args.Time);

            HandleKeyboard(KeyboardState, (float)args.Time);
            HandleMouse(MouseState, (float)args.Time);
        }

        public void HandleKeyboard(KeyboardState keyboard, float deltaTime)
        {
            if (IsKeyPressed(Keys.Escape))
            {
                Close();
            }

            if (IsKeyDown(Keys.W))
            {
                Camera.Transform.LocalPosition += Camera.Transform.Forward * deltaTime;
            }

            if (IsKeyDown(Keys.S))
            {
                Camera.Transform.LocalPosition += -Camera.Transform.Forward * deltaTime;
            }

            if (IsKeyDown(Keys.A))
            {
                Camera.Transform.LocalPosition += -Camera.Transform.Right * deltaTime;
            }

            if (IsKeyDown(Keys.D))
            {
                Camera.Transform.LocalPosition += Camera.Transform.Right * deltaTime;
            }

            if (IsKeyDown(Keys.Space))
            {
                Camera.Transform.LocalPosition += new Vector3(0f, 1f, 0f) * deltaTime;
            }

            if (IsKeyDown(Keys.LeftShift))
            {
                Camera.Transform.LocalPosition += new Vector3(0f, -1f, 0f) * deltaTime;
            }
        }

        public float MouseSpeedX = 0.2f;
        public float MouseSpeedY = 0.2f;
        public float CameraMinY = -75f;
        public float CameraMaxY =  75f;
        public void HandleMouse(MouseState mouse, float deltaTime)
        {
            var io = ImGui.GetIO();
            if (io.WantCaptureMouse)
                return;

            // Move the camera
            if (mouse.IsButtonDown(MouseButton.Right))
            {
                var delta = mouse.Delta;

                Camera.YAxisRotation += -delta.X * MouseSpeedX * deltaTime;
                Camera.XAxisRotation += -delta.Y * MouseSpeedY * deltaTime;
                Camera.XAxisRotation = MathHelper.Clamp(Camera.XAxisRotation, CameraMinY * Util.D2R, CameraMaxY * Util.D2R);

                Camera.Transform.LocalRotation =
                    Quaternion.FromAxisAngle(Vector3.UnitY, Camera.YAxisRotation) *
                    Quaternion.FromAxisAngle(Vector3.UnitX, Camera.XAxisRotation);
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            imGuiController.MouseScroll(e.Offset);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            imGuiController.WindowResized(e.Width, e.Height);

            // FIXME: Adjust things that need to be adjusted
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);

            imGuiController.PressChar((char)e.Unicode);
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
                        Debugger.Break();
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
