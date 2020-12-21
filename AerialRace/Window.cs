using AerialRace.Debugging;
using AerialRace.DebugGui;
using AerialRace.Entities;
using AerialRace.Entities.Systems;
using AerialRace.Loading;
using AerialRace.Physics;
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
using System.Threading;
using System.Transactions;

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

        public AssetDB AssetDB;

        ImGuiController ImGuiController;

        Material Material;
        Mesh Mesh;

        Mesh QuadMesh;

        Transform QuadTransform;
        MeshRenderer QuadRenderer;

        Transform ChildTransform;
        MeshRenderer ChildRenderer;

        StaticSetpiece Floor;
        StaticSetpiece Rock;

        Camera Camera;

        Texture TestTexture;
        Sampler DebugSampler;

        Ship Player;
        Texture ShipTexture;

        RigidBody TestBox;
        Transform TestBoxTransform;
        MeshRenderer TestBoxRenderer;

        Framebuffer Shadowmap;
        Framebuffer DepthBuffer;

        ShadowSampler ShadowSampler;

        SkyRenderer Sky;

        //EntityManager Manager = new EntityManager();

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

            Screen.UpdateScreenSize(Size);

            AssetDB = new AssetDB();
            AssetDB.LoadAllAssetsFromDirectory("./", true);

            // Enable backface culling
            // FIXME: This should be a per-material setting
            //GL.Enable(EnableCap.CullFace);
            //GL.CullFace(CullFaceMode.Front);

            RenderDataUtil.SetDepthTesting(true);
            RenderDataUtil.SetDepthFunc(RenderDataUtil.DepthFunc.PassIfLessOrEqual);

            RenderDataUtil.QueryLimits();
            BuiltIn.StaticCtorTrigger();
            Debug.Init(Width, Height);

            Camera = new Camera(100, Width / (float)Height, 0.1f, 10000f, Color4.DarkBlue);
            Camera.Transform.Name = "Camera";
            Camera.Transform.LocalPosition = CameraOffset;

            var meshData = MeshLoader.LoadObjMesh("C:/Users/juliu/source/repos/CoolGraphics/CoolGraphics/Assets/Models/pickaxe02.obj");

            Mesh = RenderDataUtil.CreateMesh("Pickaxe", meshData);

            RenderDataUtil.CreateShaderProgram("Standard Depth Vertex", ShaderStage.Vertex, File.ReadAllText("./Shaders/Depth/StandardDepth.vert"), out ShaderProgram? depthVertProgram);
            RenderDataUtil.CreateShaderProgram("Standard Depth Fragment", ShaderStage.Fragment, File.ReadAllText("./Shaders/Depth/StandardDepth.frag"), out ShaderProgram? depthFragProgram);
            RenderDataUtil.CreatePipeline("Standard Depth", depthVertProgram, null, depthFragProgram, out var depthPipeline);

            RenderDataUtil.CreateShaderProgram("Standard Vertex Shader", ShaderStage.Vertex, File.ReadAllText("./Shaders/Standard.vert"), out ShaderProgram? standardVertex);
            RenderDataUtil.CreateShaderProgram("Standard Fragment Shader", ShaderStage.Fragment, File.ReadAllText("./Shaders/Standard.frag"), out ShaderProgram? standardFragment);

            RenderDataUtil.CreatePipeline("Debug Shader", standardVertex, null, standardFragment, out var standardShader);

            TestTexture = TextureLoader.LoadRgbaImage("UV Test", "./Textures/uvtest.png", true, false);

            DebugSampler = RenderDataUtil.CreateSampler2D("DebugSampler", MagFilter.Linear, MinFilter.LinearMipmapLinear, 16f, WrapMode.Repeat, WrapMode.Repeat);

            //Material = new Material("First Material", firstShader, null);
            Material = new Material("Debug Material", standardShader, depthPipeline);
            Material.Properties.SetTexture("AlbedoTex", TestTexture, DebugSampler);

            // This should be the first refernce to StaticGeometry.
            StaticGeometry.Init();

            QuadMesh = new Mesh("Unit Quad",
                StaticGeometry.UnitQuadIndexBuffer,
                StaticGeometry.CenteredUnitQuadBuffer,
                BuiltIn.StandardAttributes);
            // Add the debug colors as a separate buffer and attribute
            QuadMesh.AddBuffer(StaticGeometry.UnitQuadDebugColorsBuffer);
            QuadMesh.AddAttribute(new AttributeSpecification("Color", 4, RenderData.AttributeType.Float, false, 0));
            // FIXME: Magic numbers
            QuadMesh.AddLink(3, 1);

            QuadTransform = new Transform("Quad", new Vector3(0f, 0f, -2f), Quaternion.FromAxisAngle(Vector3.UnitY, MathF.PI/4f));

            ChildTransform = new Transform("Child", new Vector3(1f, 1f, 0f));

            QuadTransform.Children = new List<Transform>();
            ChildTransform.SetParent(QuadTransform);

            QuadRenderer = new MeshRenderer(QuadTransform, QuadMesh, Material);
            ChildRenderer = new MeshRenderer(ChildTransform, Mesh, Material);

            //RenderDataUtil.CreateShaderProgram("Ship Vertex", ShaderStage.Vertex, new[] { File.ReadAllText("./Shaders/Ship.vert") }, out var shipVertex);
            //RenderDataUtil.CreateShaderProgram("Ship Fragment", ShaderStage.Fragment, new[] { File.ReadAllText("./Shaders/Ship.frag") }, out var shipFragment);

            var shipPipeline = RenderDataUtil.CreateEmptyPipeline("Ship Shader");
            RenderDataUtil.AssembleProgramPipeline(shipPipeline, standardVertex, null, standardFragment);

            ShipTexture = TextureLoader.LoadRgbaImage("ship texture", "./Textures/ship.png", true, false);

            Material shipMaterial = new Material("Ship", shipPipeline, depthPipeline);
            shipMaterial.Properties.SetTexture("AlbedoTex", ShipTexture, DebugSampler);

            Phys.Init();

            Player = new Ship("Ship", MeshLoader.LoadObjMesh("./Models/plane.obj"), shipMaterial);
            Player.IsPlayerShip = true;

            //Camera.Transform.SetParent(Player.Transform);

            var cube = RenderDataUtil.CreateMesh("Test Cube", MeshLoader.LoadObjMesh("./Models/cube.obj"));

            SimpleMaterial physMat = new SimpleMaterial()
            {
                FrictionCoefficient = 0.5f,
                MaximumRecoveryVelocity = 2f,
                SpringSettings = new BepuPhysics.Constraints.SpringSettings(30, 1),
            };

            {
                var floorMat = new Material("Floor Mat", standardShader, depthPipeline);
                floorMat.Properties.SetTexture("AlbedoTex", TestTexture, DebugSampler);

                // FIXME: Figure out why we have a left-handed coordinate system and if that is what we want...
                var FloorTransform = new Transform("Floor", new Vector3(0, 0, 0), Quaternion.FromAxisAngle(Vector3.UnitX, -MathF.PI / 2), Vector3.One * 500);

                Floor = new StaticSetpiece(FloorTransform, QuadMesh, floorMat, new BoxCollider(new Vector3(500, 1, 500), new Vector3(0, -0.5f, 0)), physMat);
            }

            {
                var rockData = MeshLoader.LoadObjMesh("./Models/opengameart/rocks_02/rock_02_tri.obj");
                Mesh rockMesh = RenderDataUtil.CreateMesh("Rock 2", rockData);
                var rockMat = new Material("Rock 2", standardShader, depthPipeline);
                var rockAlbedo = TextureLoader.LoadRgbaImage("Rock 2 Albedo", "./Models/opengameart/rocks_02/diffuse.tga", true, false);
                rockMat.Properties.SetTexture("AlbedoTex", rockAlbedo, DebugSampler);

                var rockTransform = new Transform("Rock", new Vector3(300f, 0f, -2f));

                SimpleMaterial physMatRock = new SimpleMaterial()
                {
                    FrictionCoefficient = 0.85f,
                    MaximumRecoveryVelocity = 2f,
                    SpringSettings = new BepuPhysics.Constraints.SpringSettings(30, 1),
                };

                Rock = new StaticSetpiece(rockTransform, rockMesh, rockMat, new MeshCollider(rockData), physMatRock);
            }

            new StaticCollider(new BoxCollider(new Vector3(1f, 4, 1f)), new Vector3(-0.5f, 1, 0f), physMat);
            new MeshRenderer(new Transform("Cube", new Vector3(-0.5f, 1, 0f), Quaternion.Identity, new Vector3(0.5f, 2, 0.5f)), cube, Material);

            TestBoxTransform = new Transform("Test Box", new Vector3(0, 20f, 0), Quaternion.FromAxisAngle(new Vector3(1, 0, 0), 0.1f), Vector3.One);
            TestBox = new RigidBody(new BoxCollider(new Vector3(1, 1, 1) * 2), TestBoxTransform, 1f, SimpleMaterial.Default, SimpleBody.Default);
            
            TestBoxRenderer = new MeshRenderer(TestBoxTransform, cube, Material);

            ImGuiController = new ImGuiController(Width, Height);

            Shadowmap = RenderDataUtil.CreateEmptyFramebuffer("Shadowmap");
            DepthBuffer = RenderDataUtil.CreateEmptyFramebuffer("Depth Prepass");

            var depthTex = RenderDataUtil.CreateEmpty2DTexture("Depth Prepass Texture", TextureFormat.Depth32F, Width, Height);
            RenderDataUtil.AddDepthAttachment(DepthBuffer, depthTex, 0);

            var status = RenderDataUtil.CheckFramebufferComplete(DepthBuffer, RenderData.FramebufferTarget.Draw);
            if (status != FramebufferStatus.FramebufferComplete)
            {
                Debug.Break();
            }

            var shaowMap = RenderDataUtil.CreateEmpty2DTexture("Shadowmap Texture", TextureFormat.Depth16, 2048 * 2, 2048 * 2);
            RenderDataUtil.AddDepthAttachment(Shadowmap, shaowMap, 0);

            status = RenderDataUtil.CheckFramebufferComplete(Shadowmap, RenderData.FramebufferTarget.Draw);
            if (status != FramebufferStatus.FramebufferComplete)
            {
                Debug.Break();
            }

            ShadowSampler = RenderDataUtil.CreateShadowSampler2D("Shadowmap sampler", MagFilter.Linear, MinFilter.Linear, 16f, WrapMode.Repeat, WrapMode.Repeat, DepthTextureCompareMode.RefToTexture, DepthTextureCompareFunc.Greater);

            RenderDataUtil.CreateShaderProgram("Sky Vertex", ShaderStage.Vertex, File.ReadAllText("./Shaders/Sky.vert"), out var skyVertexProgram);
            RenderDataUtil.CreateShaderProgram("Sky Fragment", ShaderStage.Fragment, File.ReadAllText("./Shaders/Sky.frag"), out var skyFragmentProgram);

            RenderDataUtil.CreatePipeline("Sky", skyVertexProgram, null, skyFragmentProgram, out var skyPipeline);

            Material skyMat = new Material("Sky Material", skyPipeline, null);

            var sunPosition = new Vector3(100, 100, 0);
            Sky = new SkyRenderer(skyMat, sunPosition.Normalized(), new Color4(1f, 1f, 1f, 1f));

            Editor.Editor.InitEditor(this);

            // Setup an always bound VAO
            RenderDataUtil.SetupGlobalVAO();
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            float deltaTime = (float)args.Time;

            Phys.Update(deltaTime);

            var ppos = TestBoxTransform.LocalPosition;
            TestBox.UpdateTransform(TestBoxTransform);

            Debug.NewFrame(Width, Height);

            ImGuiController.Update(this, (float)args.Time);
            // Update above calls ImGui.NewFrame()...
            // ImGui.NewFrame();

            //Manager.UpdateSystems();
            //ShowEntityList(Manager);

            Player.Update(deltaTime);

            for (int i = 0; i < Transform.Transforms.Count; i++)
            {
                Transform.Transforms[i].UpdateMatrices();
            }

            UpdateCamera(deltaTime);
            Camera.Transform.UpdateMatrices();

            if (Editor.Editor.InEditorMode)
            {
                Editor.Editor.EditorCamera.Transform.UpdateMatrices();

                RenderScene(Editor.Editor.EditorCamera);

                Editor.Editor.ShowEditor();
            }
            else
            {
                RenderScene(Camera);
            }

            ImGui.EndFrame();

            using (_ = RenderDataUtil.PushGenericPass("ImGui"))
            {
                ImGuiController.Render();
            }

            // FIXME: Reset gl state!
            // The blend mode is changed to this after imgui
            //GL.BlendEquation(BlendEquationMode.FuncAdd);
            //GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            // Depth testing is also turned off!

            SwapBuffers();
        }

        public void RenderScene(Camera camera)
        {
            using var sceneDebugGroup = RenderDataUtil.PushGenericPass("Scene");

            // Disable culling here. We do this because the drawlist rendering can change this setting.
            // I don't want to figure out in what ways the current code relies on having no culling.
            // - 2020-12-21
            RenderDataUtil.SetCullMode(RenderDataUtil.CullMode.None);

            RenderDataUtil.SetDepthTesting(true);

            // To be able to clear the depth buffer we need to enable writing to it
            RenderDataUtil.SetDepthWrite(true);

            RenderDataUtil.SetColorWrite(ColorChannels.All);

            GL.ClearColor(camera.ClearColor);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // FIXME: Make SkyRenderer take a SkySettings instead.
            Color4 directionalLightColor = SkyRenderer.Instance.SunColor;
            Vector3 directionalLightDir = SkyRenderer.Instance.SunDirection;
            Vector3 directionalLightPos = directionalLightDir * 100f;

            SkySettings skySettings = new SkySettings()
            {
                SunDirection = directionalLightDir,
                SunColor = directionalLightColor,
                SkyColor = new Color4(0.2f, 0.3f, 0.6f, 1f),
                GroundColor = new Color4(0.15f, 0.1f, 0.05f, 1f),
            };

            var proj = Matrix4.CreateOrthographic(500, 500, 0.1f, 1000f);
            var lightView = Matrix4.LookAt(directionalLightPos, Vector3.Zero, directionalLightPos == Vector3.UnitY ? -Vector3.UnitZ : Vector3.UnitY);
            var lightSpace = lightView * proj;

            // What we want to do here is first render the shadowmaps using all renderers
            // Then do a z prepass from the normal camera
            // Then do the final color pass

            using (_ = RenderDataUtil.PushDepthPass("Directional Shadow"))
            {
                RenderPassSettings shadowPass = new RenderPassSettings()
                {
                    IsDepthPass = true,
                    View = lightView,
                    Projection = proj,
                    LightSpace = Matrix4.Identity,
                    ViewPos = directionalLightPos,

                    NearPlane = camera.NearPlane,
                    FarPlane = camera.FarPlane,

                    Sky = skySettings,
                    AmbientLight = new Color4(0.1f, 0.1f, 0.1f, 1f),
                };

                RenderDataUtil.BindDrawFramebuffer(Shadowmap);
                GL.Viewport(0, 0, Shadowmap.DepthAttachment!.Width, Shadowmap.DepthAttachment!.Height);

                RenderDataUtil.SetDepthWrite(true);
                RenderDataUtil.SetColorWrite(ColorChannels.None);
                GL.Clear(ClearBufferMask.DepthBufferBit);
                RenderDataUtil.SetDepthFunc(RenderDataUtil.DepthFunc.PassIfLessOrEqual);

                MeshRenderer.Render(ref shadowPass);
            }

            using (_ = RenderDataUtil.PushDepthPass("Depth prepass"))
            {
                RenderDataUtil.BindDrawFramebuffer(null);
                GL.Viewport(0, 0, Width, Height);

                camera.CalcProjectionMatrix(out proj);

                RenderPassSettings depthPrePass = new RenderPassSettings()
                {
                    IsDepthPass = true,
                    View = camera.Transform.WorldToLocal,
                    Projection = proj,
                    LightSpace = lightSpace,
                    ViewPos = camera.Transform.WorldPosition,

                    NearPlane = camera.NearPlane,
                    FarPlane = camera.FarPlane,

                    Sky = skySettings,
                    AmbientLight = new Color4(0.1f, 0.1f, 0.1f, 1f),
                };

                //RenderDataUtil.BindDrawFramebuffer(DepthBuffer);
                RenderDataUtil.SetDepthWrite(true);
                RenderDataUtil.SetColorWrite(ColorChannels.None);
                GL.Clear(ClearBufferMask.DepthBufferBit);
                RenderDataUtil.SetDepthFunc(RenderDataUtil.DepthFunc.PassIfLessOrEqual);

                MeshRenderer.Render(ref depthPrePass);

                //RenderDataUtil.BindDrawFramebuffer(null);
            }

            using (_ = RenderDataUtil.PushColorPass("Color pass"))
            {
                RenderPassSettings colorPass = new RenderPassSettings()
                {
                    IsDepthPass = false,
                    View = camera.Transform.WorldToLocal,
                    Projection = proj,
                    LightSpace = lightSpace,
                    ViewPos = camera.Transform.WorldPosition,

                    NearPlane = camera.NearPlane,
                    FarPlane = camera.FarPlane,

                    Sky = skySettings,
                    AmbientLight = new Color4(0.1f, 0.1f, 0.1f, 1f),

                    UseShadows = true,
                    ShadowMap = Shadowmap.DepthAttachment,
                    ShadowSampler = ShadowSampler,
                };

                RenderDataUtil.SetDepthWrite(false);
                RenderDataUtil.SetColorWrite(ColorChannels.All);
                RenderDataUtil.SetDepthFunc(RenderDataUtil.DepthFunc.PassIfEqual);

                // We only want to render the skybox when we are rendering the final colors
                SkyRenderer.Render(ref colorPass);
                MeshRenderer.Render(ref colorPass);
            }

            using (_ = RenderDataUtil.PushGenericPass("Scene Debug"))
            {
                // Draw debug stuff
                RenderDataUtil.SetDepthTesting(false);

                Matrix4 viewMatrix = camera.Transform.WorldToLocal;
                camera.CalcProjectionMatrix(out proj);

                //PhysDebugRenderer.RenderColliders();
                //RenderDrawList(PhysDebugRenderer.Drawlist, Debug.DebugPipeline, ref proj, ref viewMatrix);

                // Draw debug drawlist
                {
                    RenderDataUtil.UsePipeline(Debug.DebugPipeline);
                    DrawListSettings settings = new DrawListSettings()
                    {
                        DepthTest = false,
                        DepthWrite = false,
                        Vp = viewMatrix * proj,
                        CullMode = RenderDataUtil.CullMode.None,
                    };
                    DrawListRenderer.RenderDrawList(Debug.List, ref settings);
                }
            }
        }

        public Vector3 CameraOffset = new Vector3(0, 6f, 27f);
        public Quaternion RotationOffset = Quaternion.FromAxisAngle(Vector3.UnitX, MathHelper.DegreesToRadians(-20f));
        void UpdateCamera(float deltaTime)
        {
            MouseInfluenceTimeout -= deltaTime;
            if (MouseInfluenceTimeout < 0.001f)
            {
                MouseInfluenceTimeout = 0;

                Camera.XAxisRotation -= Camera.XAxisRotation * deltaTime * 3f;
                if (Math.Abs(Camera.XAxisRotation) < 0.001f) Camera.XAxisRotation = 0;
                Camera.YAxisRotation -= Camera.YAxisRotation * deltaTime * 3f;
                if (Math.Abs(Camera.YAxisRotation) < 0.001f) Camera.YAxisRotation = 0;
            }

            //var targetPos = Vector3.TransformPosition(CameraOffset, Player.Transform.LocalToWorld);

            var targetPos = Player.Transform.LocalPosition;

            Quaternion rotation =
                    Player.Transform.LocalRotation *
                    Quaternion.FromAxisAngle(Vector3.UnitY, Camera.YAxisRotation) *
                    Quaternion.FromAxisAngle(Vector3.UnitX, Camera.XAxisRotation) *
                    RotationOffset;

            targetPos = targetPos + (rotation * new Vector3(0, 0, CameraOffset.Length));

            Camera.Transform.LocalPosition = Vector3.Lerp(Camera.Transform.LocalPosition, targetPos, 30f * deltaTime);

            Camera.Transform.LocalRotation = Quaternion.Slerp(Camera.Transform.LocalRotation, rotation, 5f * deltaTime);
            //Debug.Print($"Player Local Y: {Player.Transform.LocalPosition.Y}, Player Y: {Player.Transform.WorldPosition.Y}, Camera Y: {Camera.Transform.WorldPosition.Y}");
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
            }
            ImGui.End();
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

            //QuadTransform.LocalRotation *= Quaternion.FromAxisAngle(new Vector3(0, -1, 0), 0.1f * MathF.PI * (float)args.Time);
            //QuadTransform.LocalPosition = new Vector3(1, MathF.Sin(TotalTime * MathF.PI * 0.2f), -2);

            ChildTransform.LocalRotation *= Quaternion.FromAxisAngle(new Vector3(1, 0, 0), MathF.PI * (float)args.Time);

            //Camera.Transform.Rotation *= Quaternion.FromAxisAngle(new Vector3(0, 1, 0), 2 * MathF.PI * (float)args.Time);

            // Exit if needed
            if (IsKeyPressed(Keys.Escape))
            {
                Close();
            }

            // Toggle editor mode
            var ctrlDown = KeyboardState.IsKeyDown(Keys.LeftControl) | KeyboardState.IsKeyDown(Keys.Right);
            if (ctrlDown && KeyboardState.IsKeyPressed(Keys.E))
            {
                Editor.Editor.InEditorMode = !Editor.Editor.InEditorMode;
                
            }

            var io = ImGui.GetIO();
            if (io.WantCaptureKeyboard)
            {
                return;
            }

            if (Editor.Editor.InEditorMode)
            {
                Editor.Editor.UpdateEditor(KeyboardState, MouseState, (float)args.Time);
            }
            else
            {
                HandleKeyboard(KeyboardState, (float)args.Time);
                HandleMouse(MouseState, (float)args.Time);
            }
        }

        public void HandleKeyboard(KeyboardState keyboard, float deltaTime)
        {
            float pitchForce = 0.05f;
            float yawForce = 0.05f;
            float rollForce = 0.08f;

            if (IsKeyDown(Keys.A))
            {
                //Player.Transform.LocalRotation *= new Quaternion(0, deltaTime * 2 * MathF.PI * 0.5f, 0);
                var axis = Player.Transform.LocalDirectionToWorld(new Vector3(0, yawForce * MathHelper.TwoPi, 0));
                Player.RigidBody.Body.ApplyAngularImpulse(axis.ToNumerics() * Player.RigidBody.Mass);
            }

            if (IsKeyDown(Keys.D))
            {
                //Player.Transform.LocalRotation *= new Quaternion(0, deltaTime * -2 * MathF.PI * 0.5f, 0);
                var axis = Player.Transform.LocalDirectionToWorld(new Vector3(0, -yawForce * MathHelper.TwoPi, 0));
                Player.RigidBody.Body.ApplyAngularImpulse(axis.ToNumerics() * Player.RigidBody.Mass);
            }

            if (IsKeyDown(Keys.W))
            {
                //Player.Transform.LocalRotation *= new Quaternion(deltaTime * -2 * MathF.PI * 0.2f, 0, 0);
                var axis = Player.Transform.LocalDirectionToWorld(new Vector3(-pitchForce * MathHelper.TwoPi, 0, 0));
                Player.RigidBody.Body.ApplyAngularImpulse(axis.ToNumerics() * Player.RigidBody.Mass);
            }

            if (IsKeyDown(Keys.S))
            {
                //Player.Transform.LocalRotation *= new Quaternion(deltaTime * 2 * MathF.PI * 0.2f, 0, 0);
                var axis = Player.Transform.LocalDirectionToWorld(new Vector3(pitchForce * MathHelper.TwoPi, 0, 0));
                Player.RigidBody.Body.ApplyAngularImpulse(axis.ToNumerics() * Player.RigidBody.Mass);
            }

            if (IsKeyDown(Keys.Up))
            {
                var axis = Player.Transform.LocalDirectionToWorld(new Vector3(-yawForce * MathHelper.TwoPi, 0, 0));
                Player.RigidBody.Body.ApplyAngularImpulse(axis.ToNumerics() * Player.RigidBody.Mass);
            }

            if (IsKeyDown(Keys.Down))
            {
                var axis = Player.Transform.LocalDirectionToWorld(new Vector3(yawForce * MathHelper.TwoPi, 0, 0));
                Player.RigidBody.Body.ApplyAngularImpulse(axis.ToNumerics() * Player.RigidBody.Mass);
            }

            if (IsKeyDown(Keys.Q))
            {
                var axis = Player.Transform.LocalDirectionToWorld(new Vector3(0, 0, rollForce * MathHelper.TwoPi));
                Player.RigidBody.Body.ApplyAngularImpulse(axis.ToNumerics() * Player.RigidBody.Mass);
                //Player.Transform.LocalRotation *= new Quaternion(0, 0, deltaTime * 2 * MathF.PI * 0.5f);
            }

            if (IsKeyDown(Keys.E))
            {
                var axis = Player.Transform.LocalDirectionToWorld(new Vector3(0, 0, -rollForce * MathHelper.TwoPi));
                Player.RigidBody.Body.ApplyAngularImpulse(axis.ToNumerics() * Player.RigidBody.Mass);
                //Player.Transform.LocalRotation *= new Quaternion(0, 0, deltaTime * -2 * MathF.PI * 0.5f);
            }

            if (IsKeyDown(Keys.Space))
            {
                Player.AccelerationTimer += deltaTime;
                if (Player.AccelerationTimer >= Player.AccelerationTime)
                {
                    Player.AccelerationTimer = Player.AccelerationTime;
                }
            }
            else
            {
                Player.AccelerationTimer -= deltaTime * 2;
                if (Player.AccelerationTimer <= 0)
                {
                    Player.AccelerationTimer = 0;
                }
            }

            float timerPercent = Player.AccelerationTimer / Player.AccelerationTime;
            Player.CurrentAcceleration = MathHelper.Lerp(0, Player.MaxAcceleration, timerPercent);
        }

        public float MouseInfluenceTimeout = 0f;

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
                MouseInfluenceTimeout = 1f;

                var delta = mouse.Delta;

                Camera.YAxisRotation += -delta.X * MouseSpeedX * deltaTime;
                Camera.XAxisRotation += -delta.Y * MouseSpeedY * deltaTime;
                Camera.XAxisRotation = MathHelper.Clamp(Camera.XAxisRotation, CameraMinY * Util.D2R, CameraMaxY * Util.D2R);

                var targetPos = Player.Transform.LocalPosition;

                Quaternion rotation = 
                    Quaternion.FromAxisAngle(Vector3.UnitY, Camera.YAxisRotation) *
                    Quaternion.FromAxisAngle(Vector3.UnitX, Camera.XAxisRotation);

                //Camera.Transform.LocalPosition = targetPos + (rotation * new Vector3(0, 0, CameraOffset.Length));

                //Camera.Transform.LocalRotation = rotation;

                //Camera.Transform.LocalRotation =
                //    Quaternion.FromAxisAngle(Vector3.UnitY, Camera.YAxisRotation) *
                //    Quaternion.FromAxisAngle(Vector3.UnitX, Camera.XAxisRotation);
            }
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            Screen.UpdateScreenSize(new Vector2i(e.Width, e.Height));

            ImGuiController.WindowResized(e.Width, e.Height);

            // FIXME: Adjust things that need to be adjusted
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);

            ImGuiController.PressChar((char)e.Unicode);
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
