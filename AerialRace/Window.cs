using AerialRace.Debugging;
using AerialRace.DebugGui;
using AerialRace.Entities;
using AerialRace.Entities.Systems;
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
//using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
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

        public int Width => Size.X;
        public int Height => Size.Y;

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

        Ship Player;
        Texture ShipTexture;

        AttributeSpecification[] StandardAttributes;

        EntityManager Manager = new EntityManager();

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
            Manager.RegisterType<Entities.Components.Transform>();
            Manager.RegisterType<Entities.Components.LocalToWorld>();

            Manager.RegisterSystem(new TransformSystem());


            var @ref = Manager.CreateEntity();
            Manager.AddComponent(@ref, new Entities.Components.Transform() { LocalPosition = new Vector3(0, 1, -2) });
            Manager.AddComponent(@ref, new Entities.Components.LocalToWorld());

            // Enable backface culling
            // FIXME: This should be a per-material setting
            //GL.Enable(EnableCap.CullFace);
            //GL.CullFace(CullFaceMode.Front);

            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);

            RenderDataUtil.QueryLimits();
            BuiltIn.StaticCtorTrigger();
            Debug.Init(Width, Height);

            Camera = new Camera(100, Width / (float)Height, 0.1f, 10000f, Color4.DarkBlue);
            Camera.Transform.Name = "Camera";
            Camera.Transform.LocalPosition = new Vector3(0, 8f, 14f);

            var meshData = MeshLoader.LoadObjMesh("C:/Users/juliu/source/repos/CoolGraphics/CoolGraphics/Assets/Models/pickaxe02.obj");

            Mesh = RenderDataUtil.CreateMesh("Pickaxe", meshData);

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
            QuadTransform.Name = "Quad";


            ChildTransform = new Transform(new Vector3(1f, 1f, 0f));
            ChildTransform.Name = "Child";

            QuadTransform.Children = new List<Transform>();
            ChildTransform.SetParent(QuadTransform);

            //QuadTransform.Children.Add(Camera.Transform);
            //Camera.Transform.Parent = QuadTransform;

            FloorTransform = new Transform(new Vector3(0, 0, 0), Quaternion.FromAxisAngle(Vector3.UnitX, MathF.PI / 2), Vector3.One * 500);
            FloorTransform.Name = "Floor";

            TestTexture = TextureLoader.LoadRgbaImage("UV Test", "./Textures/uvtest.png", true, false);

            DebugSampler = RenderDataUtil.CreateSampler2D("DebugSampler", MagFilter.Linear, MinFilter.LinearMipmapLinear, 16f, WrapMode.Repeat, WrapMode.Repeat);

            Mesh shipMesh = RenderDataUtil.CreateMesh("Ship", MeshLoader.LoadObjMesh("./Models/plane.obj"));

            RenderDataUtil.CreateShaderProgram("Ship Vertex", ShaderStage.Vertex, new[] { File.ReadAllText("./Shaders/Ship.vert") }, out var shipVertex);
            RenderDataUtil.CreateShaderProgram("Ship Fragment", ShaderStage.Fragment, new[] { File.ReadAllText("./Shaders/Ship.frag") }, out var shipFragment);

            var shipPipeline = RenderDataUtil.CreateEmptyPipeline("Ship Shader");
            RenderDataUtil.AssembleProgramPipeline(shipPipeline, shipVertex, null, shipFragment);

            Material shipMaterial = new Material("Ship", shipPipeline, null);

            ShipTexture = TextureLoader.LoadRgbaImage("ship texture", "./Textures/ship.png", true, false);

            Player = new Ship(shipMesh, shipMaterial);
            Player.IsPlayerShip = true;

            //Camera.Transform.SetParent(Player.Transform);

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

            float deltaTime = (float)args.Time;

            Debug.NewFrame(Width, Height);

            imGuiController.Update(this, (float)args.Time);
            // Update above calls ImGui.NewFrame()...
            // ImGui.NewFrame();

            //Manager.UpdateSystems();
            //ShowEntityList(Manager);

            ShowTransformHierarchy();

            Player.Update(deltaTime);

            for (int i = 0; i < Transform.Transforms.Count; i++)
            {
                Transform.Transforms[i].UpdateMatrices();
            }

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

            Transformations.MultMVP(ref transformMatrix, ref viewMatrix, ref proj, out var mv, out var mvp);
            
            RenderDataUtil.UniformMatrix4("mvp", ShaderStage.Vertex, true, ref mvp);

            GL.DrawElements(PrimitiveType.Triangles, QuadMesh.Indices!.Elements, RenderDataUtil.ToGLDrawElementsType(QuadMesh.Indices.IndexType), 0);

            FloorTransform.GetTransformationMatrix(out transformMatrix);
            Transformations.MultMVP(ref transformMatrix, ref viewMatrix, ref proj, out mv, out mvp);

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

            Transformations.MultMVP(ref transformMatrix, ref viewMatrix, ref proj, out mv, out mvp);

            RenderDataUtil.UniformMatrix4("mvp", ShaderStage.Vertex, true, ref mvp);

            GL.DrawElements(PrimitiveType.Triangles, Mesh.Indices!.Elements, RenderDataUtil.ToGLDrawElementsType(Mesh.Indices.IndexType), 0);

            RenderPlayerShip(Camera, Player);

            ImGui.ShowDemoWindow();

            ImGui.EndFrame();
            imGuiController.Render();

            // FIXME: Reset gl state!
            // The blend mode is changed to this after imgui
            //GL.BlendEquation(BlendEquationMode.FuncAdd);
            //GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            //GL.Enable(EnableCap.CullFace);

            // Draw debug stuff
            GL.Disable(EnableCap.DepthTest);

            viewMatrix = Camera.Transform.WorldToLocal;
            Camera.CalcProjectionMatrix(out proj);

            RenderDrawList(Debug.List, Debug.DebugPipeline, ref proj, ref viewMatrix);
            
            GL.Enable(EnableCap.DepthTest);

            SwapBuffers();
        }

        void RenderDrawList(DrawList list, ShaderPipeline pipeline, ref Matrix4 projection, ref Matrix4 view)
        {
            // Upload draw list data to gpu
            list.UploadData();

            RenderDataUtil.SetAndEnableVertexAttributes(Debug.DebugAttributes, 0);

            RenderDataUtil.BindVertexAttribBuffer(0, list.VertexBuffer, 0);
            RenderDataUtil.BindVertexAttribBuffer(1, list.VertexBuffer, 12);
            RenderDataUtil.BindVertexAttribBuffer(2, list.VertexBuffer, 20);
            RenderDataUtil.BindIndexBuffer(list.IndexBuffer);

            RenderDataUtil.UsePipeline(pipeline);

            RenderDataUtil.UniformMatrix4("projection", ShaderStage.Vertex, true, ref projection);
            RenderDataUtil.UniformMatrix4("view", ShaderStage.Vertex, true, ref view);

            Matrix4 vp = view * projection;
            RenderDataUtil.UniformMatrix4("vp", ShaderStage.Vertex, true, ref vp);

            GL.BindSampler(0, DebugSampler.Handle);

            // Reset the scissor area
            GL.Scissor(0, 0, Width, Height);

            int indexBufferOffset = 0;
            foreach (var command in list.Commands)
            {
                switch (command.Command)
                {
                    case DrawCommandType.Points:
                    case DrawCommandType.Lines:
                    case DrawCommandType.LineLoop:
                    case DrawCommandType.LineStrip:
                    case DrawCommandType.Triangles:
                    case DrawCommandType.TriangleStrip:
                    case DrawCommandType.TriangleFan:
                        {
                            // Do normal rendering
                            //Material material = command.Material ?? DefaultMaterial;
                            //material.UseMaterial();

                            //int unit = TextureBinder.BindTexture(command.TextureHandle);
                            //material.Shader.SetTexture("tex", unit);

                            // FIXME: Texture!!
                            GL.BindTextureUnit(0, command.TextureHandle);

                            GL.DrawElements((PrimitiveType)command.Command, command.ElementCount, DrawElementsType.UnsignedInt, indexBufferOffset * sizeof(uint));

                            indexBufferOffset += command.ElementCount;

                            //TextureBinder.ReleaseTexture(command.TextureHandle);
                        }
                        break;
                    case DrawCommandType.SetScissor:
                        // Set the scissor area
                        // FIXME: Figure out what to do for rounding...
                        var scissor = command.Scissor;
                        GL.Scissor(
                            (int)scissor.X,
                            (int)(Height - (scissor.Y + scissor.Height)),
                            (int)scissor.Width,
                            (int)scissor.Height);
                        break;
                    default:
                        break;
                }
            }
        }

        public void RenderPlayerShip(Camera camera, Ship ship)
        {
            RenderDataUtil.SetAndEnableVertexAttributes(StandardAttributes, 0);

            RenderDataUtil.BindVertexAttribBuffer(0, ship.Model.Positions!);
            RenderDataUtil.BindVertexAttribBuffer(1, ship.Model.UVs!);
            RenderDataUtil.BindVertexAttribBuffer(2, ship.Model.Normals!);
            RenderDataUtil.DisableVertexAttribute(3);

            RenderDataUtil.BindIndexBuffer(ship.Model.Indices!);

            RenderDataUtil.UsePipeline(ship.Material.Pipeline);

            var modelMatrix = ship.Transform.LocalToWorld;
            var viewMatrix = camera.Transform.WorldToLocal;
            camera.CalcProjectionMatrix(out var proj);
            Transformations.MultMVP(ref modelMatrix, ref viewMatrix, ref proj, out var mv, out var mvp);

            //Matrix3 normalMatrix = Matrix3.Transpose(new Matrix3(Matrix4.Invert(mv)));
            Matrix3 normalMatrix = Matrix3.Transpose(new Matrix3(Matrix4.Invert(modelMatrix)));

            RenderDataUtil.UniformMatrix4("mvp", ShaderStage.Vertex, true, ref mvp);
            RenderDataUtil.UniformMatrix3("normalMatrix", ShaderStage.Vertex, true, ref normalMatrix);

            RenderDataUtil.UniformVector3("ViewPos", ShaderStage.Fragment, camera.Transform.WorldPosition);

            GL.BindTextureUnit(0, ShipTexture.Handle);

            GL.DrawElements(PrimitiveType.Triangles, ship.Model.Indices!.Elements, RenderDataUtil.ToGLDrawElementsType(ship.Model.Indices.IndexType), 0);
        }

        public static Transform? SelectedTransform;
        public void ShowTransformHierarchy()
        {
            if (ImGui.Begin("Hierarchy"))
            {
                ImGui.Columns(2);

                var roots = Transform.Transforms.Where(t => t.Parent == null).ToArray();

                for (int i = 0; i < roots.Length; i++)
                {
                    ShowTransform(roots[i]);
                }

                ImGui.NextColumn();

                if (SelectedTransform != null)
                {
                    System.Numerics.Vector3 pos = SelectedTransform.LocalPosition.ToNumerics();
                    if (ImGui.DragFloat3("Position", ref pos, 0.1f))
                        SelectedTransform.LocalPosition = pos.ToOpenTK();

                    // FIXME: Display euler angles
                    //System.Numerics.Vector4 rot = SelectedTransform.LocalRotation.ToNumerics();
                    //if (ImGui.DragFloat4("Rotation", ref rot, 0.1f))
                    //    SelectedTransform.LocalRotation = rot.ToOpenTKQuat();

                    System.Numerics.Vector3 scale = SelectedTransform.LocalScale.ToNumerics();
                    if (ImGui.DragFloat3("Scale", ref scale, 0.1f))
                        SelectedTransform.LocalScale = scale.ToOpenTK();
                }
                else
                {
                    ImGui.Text("No transform selected");
                }

                ImGui.End();
            }

            static void ShowTransform(Transform transform)
            {
                ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnDoubleClick | ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanAvailWidth;
                if (SelectedTransform == transform)
                {
                    flags |= ImGuiTreeNodeFlags.Selected;
                }
                
                if (transform.Children?.Count > 0)
                {
                    bool open = ImGui.TreeNodeEx(transform.Name, flags);

                    if (ImGui.IsItemClicked())
                    {
                        SelectedTransform = transform;
                    }

                    if (open)
                    {
                        for (int i = 0; i < transform.Children.Count; i++)
                        {
                            ShowTransform(transform.Children[i]);
                        }

                        ImGui.TreePop();
                    }
                }
                else
                {
                    flags |= ImGuiTreeNodeFlags.Leaf;
                    ImGui.TreeNodeEx(transform.Name, flags);

                    if (ImGui.IsItemClicked())
                    {
                        SelectedTransform = transform;
                    }

                    ImGui.TreePop();
                }
            }
        }

        public EntityRef? SelectedEntity;
        public void ShowEntityList(EntityManager manager)
        {
            if (ImGui.Begin("Entities"))
            {
                ImGui.Columns(2);

                for (int i = 0; i < manager.EntityCount; i++)
                {
                    ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnDoubleClick | ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanAvailWidth;

                    if (SelectedEntity?.Handle == i)
                    {
                        flags |= ImGuiTreeNodeFlags.Selected;
                    }

                    flags |= ImGuiTreeNodeFlags.Leaf;

                    bool open = ImGui.TreeNodeEx($"Entity #{i}", flags);

                    if (ImGui.IsItemClicked())
                    {
                        SelectedEntity = new EntityRef(manager.Entities[i]);
                    }

                    if (open)
                    {
                        ImGui.TreePop();
                    }
                    else
                    {

                    }
                }

                ImGui.NextColumn();

                if (SelectedEntity != null)
                {
                    var sig = manager.GetSignature(SelectedEntity.Value);
                    ImGui.Text($"Signature field upper: {Convert.ToString((long)sig.ComponentMask.Field2, 2).PadLeft(64, '0')}");
                    ImGui.Text($"Signature field lower: {Convert.ToString((long)sig.ComponentMask.Field1, 2).PadLeft(64, '0')}");
                }
                else
                {
                    ImGui.Text("No entity selected");
                }

                ImGui.End();
            }
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

            var io = ImGui.GetIO();
            if (io.WantCaptureKeyboard)
            {
                return;
            }

            if (IsKeyDown(Keys.A))
            {
                Player.Transform.LocalRotation *= new Quaternion(0, deltaTime * 2 * MathF.PI * 0.5f, 0);
            }

            if (IsKeyDown(Keys.D))
            {
                Player.Transform.LocalRotation *= new Quaternion(0, deltaTime * -2 * MathF.PI * 0.5f, 0);
            }

            if (IsKeyDown(Keys.W))
            {
                Player.Transform.LocalRotation *= new Quaternion(deltaTime * -2 * MathF.PI * 0.2f, 0, 0);
            }

            if (IsKeyDown(Keys.S))
            {
                Player.Transform.LocalRotation *= new Quaternion(deltaTime * 2 * MathF.PI * 0.2f, 0, 0);
            }

            if (IsKeyDown(Keys.Up))
            {
                Player.Transform.LocalRotation *= new Quaternion(deltaTime * -2 * MathF.PI * 0.8f, 0, 0);
            }

            if (IsKeyDown(Keys.Down))
            {
                Player.Transform.LocalRotation *= new Quaternion(deltaTime * 2 * MathF.PI * 0.8f, 0, 0);
            }

            if (IsKeyDown(Keys.Q))
            {
                Player.Transform.LocalRotation *= new Quaternion(0, 0, deltaTime * 2 * MathF.PI * 0.5f);
            }

            if (IsKeyDown(Keys.E))
            {
                Player.Transform.LocalRotation *= new Quaternion(0, 0, deltaTime * -2 * MathF.PI * 0.5f);
            }

            if (IsKeyDown(Keys.Space))
            {
                Player.CurrentAcceleration = 60f;
            }
            else
            {
                Player.CurrentAcceleration = 0f;
            }

            /*
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
            }*/
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
                        Debug.Break();
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
