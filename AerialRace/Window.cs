using AerialRace.Debugging;
using AerialRace.DebugGui;
using AerialRace.Loading;
using AerialRace.Physics;
using AerialRace.RenderData;
using AerialRace.Particles;
using AerialRace.Mathematics;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace AerialRace
{
    internal class Window : GameWindow
    {
        public Window(GameWindowSettings gwSettings, NativeWindowSettings nwSettins) : base(gwSettings, nwSettins)
        {
            Title += ": OpenGL Version: " + GL.GetString(StringName.Version);
        }

        public static int LightFalloff = 1;
        public static float LightCutout = 0.01f;

        public static bool UseMSAA;
        public static bool FrustumCulling = true;

        public static Tonemap CurrentTonemap;

        public enum Tonemap
        {
            Half,
            ASCESApprox,
            Reinhard,
        }

        public int Width => Size.X;
        public int Height => Size.Y;

        public AssetDB AssetDB;

        ImGuiController ImGuiController;

        Scene CurrentScene;

        Material Material;
        Mesh Mesh;

        Mesh QuadMesh;

        Transform QuadTransform;
        MeshRenderer QuadRenderer;

        Transform ChildTransform;
        MeshRenderer ChildRenderer;

        StaticSetpiece Floor;
        StaticSetpiece Rock;
        StaticSetpiece Terrain;

        Texture TestTexture;
        Sampler DebugSampler;

        Sampler StandardSampler;

        // FIXME: Remove static
        public static Ship Player;
        Texture ShipTexture;

        RigidBody TestBox;
        Transform TestBoxTransform;
        MeshRenderer TestBoxRenderer;

        Framebuffer Shadowmap;
        Texture ShadowmapCascadeArray;
        ShadowSampler ShadowSampler;
        Vector4 CascadeSplits = (1, 1, 1, 1);
        public static int DebugCascadeSelection = 0;
        float CorrectionFactor = 0.02f;
        const int Cascades = 4;

        Sampler HDRSampler;
        Framebuffer HDRSceneBuffer;
        ShaderPipeline HDRToLDRPipeline;

        Sampler MultisampleHDRSampler;
        Framebuffer MultisampleHDRSceneBuffer;
        ShaderPipeline MultisampleHDRToLDRPipeline;

        ShaderPipeline VectorscopePipeline;
        RenderData.Buffer HDRSceneVectorscopeBuffer;

        // FIXME!!
        public static Sky Sky;

        public static Lights Lights;

        //EntityManager Manager = new EntityManager();

        private readonly static DebugProc DebugProcCallback = Window_DebugProc;
        private static GCHandle DebugProcGCHandle;

        protected override void OnLoad()
        {
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            base.OnLoad();
            Directory.SetCurrentDirectory("..\\..\\..\\Assets");

            DebugProcGCHandle = GCHandle.Alloc(DebugProcCallback, GCHandleType.Normal);
            GL.DebugMessageCallback(DebugProcCallback, IntPtr.Zero);
            GL.Enable(EnableCap.DebugOutput);
#if DEBUG
            // Disables two core driver, we don't want this in a release build
            GL.Enable(EnableCap.DebugOutputSynchronous);
#endif

            Debug.WriteLine("sRGB to CIE XYZ");
            Debug.WriteLine(Matrix3d.Transpose(ColorSpace.Linear_sRGB.CalcSpaceToCIE_XYZ()).ToString());
            Debug.WriteLine("ACES2065_1 to CIE XYZ");
            Debug.WriteLine(Matrix3d.Transpose(ColorSpace.ACES2065_1.CalcSpaceToCIE_XYZ()).ToString());
            Debug.WriteLine("ACEScg to CIE XYZ");
            Debug.WriteLine(Matrix3d.Transpose(ColorSpace.ACEScg.CalcSpaceToCIE_XYZ()).ToString());

            Debug.WriteLine("sRGB to ACEScg");
            Debug.WriteLine(Matrix3d.Transpose(ColorSpace.CalcConvertionMatrix(ColorSpace.Linear_sRGB, ColorSpace.ACEScg)).ToString());

            Screen.UpdateScreenSize(Size);
            Screen.NewFrame();

            AssetDB = new AssetDB();
            AssetDB.LoadAllAssetsFromDirectory("./", true);

            RenderDataUtil.SetDepthTesting(true);
            RenderDataUtil.SetDepthFunc(DepthFunc.PassIfLessOrEqual);

            RenderDataUtil.QueryLimits();
            BuiltIn.StaticCtorTrigger();
            Debug.Init(Width, Height);

            VSync = VSyncMode.On;

            Lights = new Lights();
            var light1 = Lights.AddPointLight("Light 1", new Vector3(1, 5, 1), Color4.AntiqueWhite, 30, 100);

            Transform randLights = new Transform("Lights");
            Random rand = new Random();
            for (int i = 0; i < 20; i++)
            {
                var pos = rand.NextPosition((-150, 1f, -150), (150, 30, 150));
                var color = rand.NextColorHue(1, 1);
                var radius = Util.MapRange(rand.NextFloat(), 0, 1, 40, 200);
                var light = Lights.AddPointLight($"Light {i+2}", pos, color, radius, rand.NextFloat() * 1000);

                light.Transform.SetParent(randLights);
            }

            var meshData = MeshLoader.LoadObjMesh("C:/Users/juliu/source/repos/CoolGraphics/CoolGraphics/Assets/Models/pickaxe02.obj");

            Mesh = RenderDataUtil.CreateMesh("Pickaxe", meshData);

            var depthVertProgram = ShaderCompiler.CompileProgram("Standard Depth Vertex", ShaderStage.Vertex, "./Shaders/Depth/StandardDepth.vert");
            var depthFragProgram = ShaderCompiler.CompileProgram("Standard Depth Fragment", ShaderStage.Fragment, "./Shaders/Depth/StandardDepth.frag");
            var depthPipeline = ShaderCompiler.CompilePipeline("Standard Depth", depthVertProgram, depthFragProgram);

            var depthCutoutVertProgram = ShaderCompiler.CompileProgram("Cutout Depth Vertex", ShaderStage.Vertex, "./Shaders/Depth/CutoutDepth.vert");
            var depthCutoutFragProgram = ShaderCompiler.CompileProgram("Cutout Depth Fragment", ShaderStage.Fragment, "./Shaders/Depth/CutoutDepth.frag");
            var depthCutoutPipeline = ShaderCompiler.CompilePipeline("Cutout Depth", depthCutoutVertProgram, depthCutoutFragProgram);

            var standardVertex = ShaderCompiler.CompileProgram("Standard Vertex Shader", ShaderStage.Vertex, "./Shaders/Standard.vert");
            var standardFragment = ShaderCompiler.CompileProgram("Standard Fragment Shader", ShaderStage.Fragment, "./Shaders/Standard.frag");

            var standardShader = ShaderCompiler.CompilePipeline("Debug Shader", standardVertex, standardFragment);

            DebugSampler = RenderDataUtil.CreateSampler2D("DebugSampler", MagFilter.Linear, MinFilter.LinearMipmapLinear, 16f, WrapMode.Repeat, WrapMode.Repeat);

            StandardSampler = RenderDataUtil.CreateSampler2D("Standard Sampler", MagFilter.Linear, MinFilter.LinearMipmapLinear, 16f, WrapMode.Repeat, WrapMode.Repeat);

            Material = new Material("Debug Material", standardShader, depthPipeline);
            Material.Properties.SetTexture("AlbedoTex", BuiltIn.UVTest, StandardSampler);
            Material.Properties.SetTexture("NormalTex", BuiltIn.FlatNormalTex, StandardSampler);
            Material.Properties.SetTexture("RoughnessTex", BuiltIn.WhiteTex, StandardSampler);
            Material.Properties.SetTexture("MetallicTex", BuiltIn.WhiteTex, StandardSampler);

            Material.Properties.SetProperty(new Property("material.Metallic", 0.02f));
            Material.Properties.SetProperty(new Property("material.Roughness", 0.9f));
            Material.Properties.SetProperty(new Property("InvertRoughness", false));

            // This should be the first refernce to StaticGeometry.
            StaticGeometry.Init();

            QuadMesh = new Mesh("Unit Quad",
                StaticGeometry.UnitQuadIndexBuffer,
                StaticGeometry.CenteredUnitQuadBuffer,
                BuiltIn.StandardAttributes);
            // FIXME: Have some better way to compute this...
            QuadMesh.AABB.Inflate(new Vector3(0.5f, 0.5f, 0));
            QuadMesh.AABB.Inflate(new Vector3(-0.5f, -0.5f, 0));
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

            var shipPipeline = ShaderCompiler.CompilePipeline("Ship Shader", standardVertex, null, standardFragment);

            ShipTexture = TextureLoader.LoadRgbaImage("ship texture", "./Textures/ship.png", true, true);

            Material shipMaterial = new Material("Ship", shipPipeline, depthPipeline);
            shipMaterial.Properties.SetTexture("AlbedoTex", ShipTexture, StandardSampler);
            shipMaterial.Properties.SetTexture("NormalTex", BuiltIn.FlatNormalTex, StandardSampler);
            shipMaterial.Properties.SetTexture("RoughnessTex", BuiltIn.WhiteTex, StandardSampler);
            shipMaterial.Properties.SetTexture("MetallicTex", BuiltIn.WhiteTex, StandardSampler);

            shipMaterial.Properties.SetProperty(new Property("material.Tint", Vector3.One));
            shipMaterial.Properties.SetProperty(new Property("uvScale", Vector2.One));
            shipMaterial.Properties.SetProperty(new Property("material.Metallic", 0.8f));
            shipMaterial.Properties.SetProperty(new Property("material.Roughness", 0.3f));
            shipMaterial.Properties.SetProperty(new Property("InvertRoughness", false));

            var trailPipeline = ShaderCompiler.CompilePipeline("Trail", "./Shaders/Trail.vert", "./Shaders/Trail.frag");
            var trailMat = new Material("Player Trail Mat", trailPipeline, null);

            Phys.Init();

            Player = new Ship("Ship", MeshLoader.LoadObjMesh("./Models/plane.obj"), shipMaterial, trailMat);
            Player.IsPlayerShip = true;

            var cube = RenderDataUtil.CreateMesh("Test Cube", MeshLoader.LoadObjMesh("./Models/cube.obj"));

            SimpleMaterial physMat = new SimpleMaterial()
            {
                FrictionCoefficient = 0.9f,
                MaximumRecoveryVelocity = 2f,
                SpringSettings = new BepuPhysics.Constraints.SpringSettings(30, 1),
            };

            {
                var paintedMetal = TextureLoader.LoadRgbaImage("Painted metal Albedo", "./Textures/PaintedMetal013_1K-PNG/PaintedMetal013_1K_Color.png", true, true);
                var paintedMetalNormal = TextureLoader.LoadRgbaImage("Painted metal Normal", "./Textures/PaintedMetal013_1K-PNG/PaintedMetal013_1K_Normal.png", true, false);
                var paintedMetalRoughness = TextureLoader.LoadRgbaImage("Painted metal Roughness", "./Textures/PaintedMetal013_1K-PNG/PaintedMetal013_1K_Roughness.png", true, false);
                var paintedMetalMetalness = TextureLoader.LoadRgbaImage("Painted metal Metalness", "./Textures/PaintedMetal013_1K-PNG/PaintedMetal013_1K_Metalness.png", true, false);

                var floorMat = new Material("Floor Mat", standardShader, depthPipeline);
                floorMat.Properties.SetTexture("AlbedoTex", paintedMetal, StandardSampler);
                floorMat.Properties.SetTexture("NormalTex", paintedMetalNormal, StandardSampler);
                floorMat.Properties.SetTexture("RoughnessTex", paintedMetalRoughness, StandardSampler);
                floorMat.Properties.SetTexture("MetallicTex", paintedMetalMetalness, StandardSampler);

                floorMat.Properties.SetProperty(new Property("Tint", Vector3.One));
                floorMat.Properties.SetProperty(new Property("uvScale", Vector2.One * 10));
                floorMat.Properties.SetProperty(new Property("material.Metallic", 1f));
                floorMat.Properties.SetProperty(new Property("material.Roughness", 1f));
                floorMat.Properties.SetProperty(new Property("InvertRoughness", false));

                floorMat.Properties.CullMode = CullMode.None;

                // FIXME: Figure out why we have a left-handed coordinate system and if that is what we want...
                var FloorTransform = new Transform("Floor", new Vector3(0, 0, 0), Quaternion.FromAxisAngle(Vector3.UnitX, -MathF.PI / 2), Vector3.One * 500);

                Floor = new StaticSetpiece(FloorTransform, QuadMesh, floorMat, new BoxCollider(new Vector3(500, 1, 500), new Vector3(0, -0.5f, 0)), physMat);
            }

            {
                var terrainMeshData = MeshLoader.LoadObjMesh("./Models/sketchfab/icy-terrain-export/source/Icy_Terrain_export.obj");
                for (int i = 0; i < terrainMeshData.Vertices.Length; i++)
                {
                    terrainMeshData.Vertices[i].Position *= 400;
                    terrainMeshData.AABB.Inflate(terrainMeshData.Vertices[i].Position);
                }
                Mesh terrainMesh = RenderDataUtil.CreateMesh("Terrain", terrainMeshData);

                var albedo = TextureLoader.LoadRgbaImage("Terrain Albedo", "./Models/sketchfab/icy-terrain-export/textures/SingleAsset_Terrain_C_Lowres_None_BaseColor.png", true, true);
                var normal = TextureLoader.LoadRgbaImage("Terrain Normal", "./Models/sketchfab/icy-terrain-export/textures/SingleAsset_Terrain_C_Lowres_None_Normal_4.png", true, false);
                var roughness = TextureLoader.LoadRgbaImage("Terrain Roughness", "./Models/sketchfab/icy-terrain-export/textures/SingleAsset_Terrain_C_Lowres_None_Roughnes.png", true, false);

                var terrainMat = new Material("Terrain", standardShader, depthPipeline);
                terrainMat.Properties.SetTexture("AlbedoTex", albedo, StandardSampler);
                terrainMat.Properties.SetTexture("NormalTex", normal, StandardSampler);
                terrainMat.Properties.SetTexture("RoughnessTex", roughness, StandardSampler);
                terrainMat.Properties.SetTexture("MetallicTex", BuiltIn.WhiteTex, StandardSampler);

                terrainMat.Properties.SetProperty(new Property("InvertRoughness", false));
                terrainMat.Properties.SetProperty(new Property("uvScale", Vector2.One));
                terrainMat.Properties.SetProperty(new Property("Tint", Vector3.One));
                terrainMat.Properties.SetProperty(new Property("material.Metallic", 0.01f));
                terrainMat.Properties.SetProperty(new Property("material.Roughness", 1f));
                terrainMat.Properties.SetProperty(new Property("InvertRoughness", false));

                var TerrainTransform = new Transform("Terrain", Vector3.Zero, Quaternion.Identity, Vector3.One);

                SimpleMaterial physMatTerrain = new SimpleMaterial()
                {
                    FrictionCoefficient = 0.9f,
                    MaximumRecoveryVelocity = 2f,
                    SpringSettings = new BepuPhysics.Constraints.SpringSettings(30, 1),
                };

                //Terrain = new StaticSetpiece(TerrainTransform, terrainMesh, terrainMat, new StaticMeshCollider(terrainMeshData, TerrainTransform.LocalScale), physMatTerrain);
            }

            {
                var rockData = MeshLoader.LoadObjMesh("./Models/opengameart/rocks_02/rock_02_tri.obj");
                Mesh rockMesh = RenderDataUtil.CreateMesh("Rock 2", rockData);
                var rockMat = new Material("Rock 2", standardShader, depthPipeline);
                var rockAlbedo = TextureLoader.LoadRgbaImage("Rock 2 Albedo", "./Models/opengameart/rocks_02/diffuse.tga", true, true);
                var rockNormal = TextureLoader.LoadRgbaImage("Rock 2 Normal", "./Models/opengameart/rocks_02/normal.tga", true, false);
                var rockSpecular = TextureLoader.LoadRgbaImage("Rock 2 Specular", "./Models/opengameart/rocks_02/specular.tga", true, false);
                rockMat.Properties.SetTexture("AlbedoTex", rockAlbedo, StandardSampler);
                rockMat.Properties.SetTexture("NormalTex", rockNormal, StandardSampler);
                rockMat.Properties.SetTexture("RoughnessTex", rockSpecular, StandardSampler);
                rockMat.Properties.SetTexture("MetallicTex", BuiltIn.WhiteTex, StandardSampler);
                rockMat.Properties.SetProperty(new Property("InvertRoughness", true));
                rockMat.Properties.SetProperty(new Property("uvScale", Vector2.One));
                rockMat.Properties.SetProperty(new Property("Tint", Vector3.One));
                rockMat.Properties.SetProperty(new Property("material.Metallic", 0.1f));
                rockMat.Properties.SetProperty(new Property("material.Roughness", 0.6f));
                rockMat.Properties.SetProperty(new Property("InvertRoughness", false));

                var rockTransform = new Transform("Rock", new Vector3(300f, 0f, -2f));

                SimpleMaterial physMatRock = new SimpleMaterial()
                {
                    FrictionCoefficient = 0.85f,
                    MaximumRecoveryVelocity = 2f,
                    SpringSettings = new BepuPhysics.Constraints.SpringSettings(30, 1),
                };

                Rock = new StaticSetpiece(rockTransform, rockMesh, rockMat, new StaticMeshCollider(rockData, rockTransform.LocalScale), physMatRock);
            }

            //new StaticCollider(new BoxCollider(new Vector3(1f, 4, 1f)), new Vector3(-0.5f, 1, 0f), physMat);
            //new MeshRenderer(new Transform("Cube", new Vector3(-0.5f, 1, 0f), Quaternion.Identity, new Vector3(0.5f, 2, 0.5f)), cube, Material);

            {
                var data = MeshLoader.LoadObjMesh("./Models/dome.obj");
                Mesh dome = RenderDataUtil.CreateMesh("Dome", data);

                var domeMat = new Material("Dome Mat", standardShader, depthPipeline);
                domeMat.Properties.SetTexture("AlbedoTex", BuiltIn.UVTest, StandardSampler);
                domeMat.Properties.SetTexture("NormalTex", BuiltIn.FlatNormalTex, StandardSampler);

                domeMat.Properties.SetProperty(new Property("material.Tint", Vector3.One));
                domeMat.Properties.SetProperty(new Property("material.Metallic", 0.8f));
                domeMat.Properties.SetProperty(new Property("material.Roughness", 0.5f));
                domeMat.Properties.SetProperty(new Property("InvertRoughness", false));
                
                SimpleMaterial physMatDome = new SimpleMaterial()
                {
                    FrictionCoefficient = 1f,
                    MaximumRecoveryVelocity = 2f,
                    SpringSettings = new BepuPhysics.Constraints.SpringSettings(30, 1),
                };

                //new StaticSetpiece(new Transform("Dome"), dome, domeMat, new StaticMeshCollider(data), physMatDome);
            }

            {
                var data = MeshLoader.LoadObjMesh("./Models/tropical plant/tropical_plant.obj");
                Mesh plant = RenderDataUtil.CreateMesh("Plant", data);

                var plantAlbedo = TextureLoader.LoadRgbaImage("Plant 2 Albedo", "./Models/tropical plant/diffuse.tga", true, true);
                var plantNormal = TextureLoader.LoadRgbaImage("Plant 2 Normal", "./Models/tropical plant/normal.tga", true, false);
                var plantSpecular = TextureLoader.LoadRgbaImage("Plant 2 Specular", "./Models/tropical plant/specular.tga", true, false);

                // We don't care about discarding fragments in the color shader as those pixels will fail
                // depth test from the depth prepass anyways.
                var plantMat = new Material("Dome Mat", standardShader, depthCutoutPipeline);
                plantMat.Properties.SetTexture("AlbedoTex", plantAlbedo, StandardSampler);
                plantMat.Properties.SetTexture("NormalTex", plantNormal, StandardSampler);
                plantMat.Properties.SetTexture("RoughnessTex", plantNormal, StandardSampler);

                plantMat.Properties.SetProperty(new Property("InvertRoughness", true));

                plantMat.Properties.SetProperty(new Property("AlphaCutout", 0.1f));

                plantMat.Properties.SetProperty(new Property("material.Tint", Vector3.One));
                plantMat.Properties.SetProperty(new Property("material.Metallic", 0.02f));
                plantMat.Properties.SetProperty(new Property("material.Roughness", 1f));
                plantMat.Properties.SetProperty(new Property("InvertRoughness", false));

                plantMat.Properties.CullMode = CullMode.None;

                SimpleMaterial physMatDome = new SimpleMaterial()
                {
                    FrictionCoefficient = 0.2f,
                    MaximumRecoveryVelocity = 2f,
                    SpringSettings = new BepuPhysics.Constraints.SpringSettings(30, 1),
                };

                //new StaticSetpiece(new Transform("Plant"), plant, plantMat, new MeshCollider(data), physMatDome);
            }

            // Load sponza
            if (true) {
                Transform baseTransform = new Transform("Sponza");

                baseTransform.LocalScale = new Vector3(0.2f, 0.2f, 0.2f);

                var objs = MeshLoader.LoadObjectsFromObj("./Models/Sponza/sponza.obj");

                SimpleMaterial physMatSponza = new SimpleMaterial()
                {
                    FrictionCoefficient = 0.2f,
                    MaximumRecoveryVelocity = 2f,
                    SpringSettings = new BepuPhysics.Constraints.SpringSettings(30, 1),
                };

                var sponzaMat = new Material("Sponza Mat", standardShader, depthCutoutPipeline);

                sponzaMat.Properties.SetTexture("AlbedoTex", BuiltIn.UVTest, StandardSampler);
                sponzaMat.Properties.SetTexture("NormalTex", BuiltIn.FlatNormalTex, StandardSampler);
                sponzaMat.Properties.SetTexture("RoughnessTex", BuiltIn.WhiteTex, StandardSampler);
                sponzaMat.Properties.SetTexture("MetallicTex", BuiltIn.WhiteTex, StandardSampler);

                sponzaMat.Properties.SetProperty(new Property("AlphaCutout", 0.1f));
                sponzaMat.Properties.SetProperty(new Property("uvScale", Vector2.One));
                sponzaMat.Properties.SetProperty(new Property("material.Tint", Vector3.One));
                sponzaMat.Properties.SetProperty(new Property("material.Metallic", 0.02f));
                sponzaMat.Properties.SetProperty(new Property("material.Roughness", 0.4f));
                sponzaMat.Properties.SetProperty(new Property("InvertRoughness", false));

                Dictionary<string, Texture> sponzaTextures = new Dictionary<string, Texture>();

                sponzaMat.Properties.CullMode = CullMode.None;

                foreach (var obj in objs)
                {
                    var objData = MeshLoader.ObjToMeshData(obj);

                    var objMat = new Material($"Sponza {obj.Name} Mat", sponzaMat);

                    if (obj.Material.MapKd != null)
                    {
                        if (sponzaTextures.TryGetValue(obj.Material.MapKd, out var texture) == false)
                        {
                            texture = TextureLoader.LoadRgbaImage($"Sponza {obj.Material.Name} map_Kd", obj.Material.MapKd, true, true);
                            sponzaTextures.Add(obj.Material.MapKd, texture);
                        }

                        objMat.Properties.SetTexture("AlbedoTex", texture, StandardSampler);
                    }
                    else
                    {
                        objMat.Properties.SetTexture("AlbedoTex", BuiltIn.WhiteTex);
                        objMat.Properties.SetProperty(new Property("Tint", obj.Material.Kd));
                    }
                    objMat.Properties.SetProperty(new Property("Tint", obj.Material.Kd));

                    if (obj.Material.MapDisp != null)
                    {
                        if (sponzaTextures.TryGetValue(obj.Material.MapDisp, out var texture) == false)
                        {
                            texture = TextureLoader.LoadRgbaImage($"Sponza {obj.Material.Name} map_Disp", obj.Material.MapDisp, true, true);
                            sponzaTextures.Add(obj.Material.MapDisp, texture);
                        }

                        objMat.Properties.SetTexture("NormalTex", texture, StandardSampler);
                    }
                    else
                    {
                        //objMat.Properties.SetTexture("AlbedoTex", BuiltIn.);
                    }

                    objMat.Properties.SetProperty(new Property("AlphaCutout", obj.Material.d * 0.5f));

                    if (obj.Material.d != 0)
                        objMat.Properties.AlphaToCoverage = true;

                    if (objData.AABB == default)
                    {
                        Debug.Assert();
                    }

                    var mesh = RenderDataUtil.CreateMesh(obj.Name, objData);

                    Transform transform = new Transform(obj.Name);
                    transform.SetParent(baseTransform);

                    //new MeshRenderer(transform, mesh, objMat);
                    new StaticSetpiece(transform, mesh, objMat, new StaticMeshCollider(objData, transform.LocalScale * baseTransform.LocalScale), physMatSponza);
                }
            }

            TestBoxTransform = new Transform("Test Box", new Vector3(0, 20f, 0), Quaternion.FromAxisAngle(new Vector3(1, 0, 0), 0.1f), Vector3.One);
            TestBox = new RigidBody(new BoxCollider(new Vector3(1, 1, 1) * 2), TestBoxTransform, 1f, SimpleMaterial.Default, SimpleBody.Default);
            
            TestBoxRenderer = new MeshRenderer(TestBoxTransform, cube, Material);

            ImGuiController = new ImGuiController(Width, Height);

            {
                {
                    HDRSceneBuffer = RenderDataUtil.CreateEmptyFramebuffer("HDR Scene Buffer");

                    var hdrColor = RenderDataUtil.CreateEmpty2DTexture("HDR Texture", TextureFormat.Rgba32F, Width, Height);
                    RenderDataUtil.AddColorAttachment(HDRSceneBuffer, hdrColor, 0, 0);

                    var hdrDepth = RenderDataUtil.CreateEmpty2DTexture("Depth Prepass Texture", TextureFormat.Depth32F, Width, Height);
                    RenderDataUtil.AddDepthAttachment(HDRSceneBuffer, hdrDepth, 0);

                    Screen.RegisterFramebuffer(HDRSceneBuffer);

                    var status = RenderDataUtil.CheckFramebufferComplete(HDRSceneBuffer, RenderData.FramebufferTarget.Draw);
                    if (status != FramebufferStatus.FramebufferComplete)
                    {
                        Debug.Break();
                    }

                    var hdrToLdr = ShaderCompiler.CompileProgram("HDR to LDR", ShaderStage.Fragment, "./Shaders/Post/HDRToLDR.frag");
                    HDRToLDRPipeline = ShaderCompiler.CompilePipeline("HDR to LDR", BuiltIn.FullscreenTriangleVertex, hdrToLdr);

                    HDRSampler = RenderDataUtil.CreateSampler2D("HDR Postprocess Sampler", MagFilter.Linear, MinFilter.Linear, 0, WrapMode.ClampToEdge, WrapMode.ClampToEdge);
                }

                {
                    // FIXME: Make this not hardcoded!
                    const int SAMPLES = 8;

                    MultisampleHDRSceneBuffer = RenderDataUtil.CreateEmptyFramebuffer("Multisample HDR Scene Buffer");

                    var msHdrColor = RenderDataUtil.CreateEmptyMultisample2DTexture("Multisample HDR Texture", TextureFormat.Rgba32F, Width, Height, SAMPLES, false);
                    RenderDataUtil.AddColorAttachment(MultisampleHDRSceneBuffer, msHdrColor, 0, 0);

                    var msHdrDepth = RenderDataUtil.CreateEmptyMultisample2DTexture("Multisample Depth Prepass Texture", TextureFormat.Depth32F, Width, Height, SAMPLES, false);
                    RenderDataUtil.AddDepthAttachment(MultisampleHDRSceneBuffer, msHdrDepth, 0);

                    Screen.RegisterFramebuffer(MultisampleHDRSceneBuffer);

                    var status = RenderDataUtil.CheckFramebufferComplete(MultisampleHDRSceneBuffer, RenderData.FramebufferTarget.Draw);
                    if (status != FramebufferStatus.FramebufferComplete)
                    {
                        Debug.Break();
                    }

                    var multisampleHdrToLdr = ShaderCompiler.CompileProgram("Multisample HDR to LDR", ShaderStage.Fragment, "./Shaders/Post/MultisampleHDRToLDR.frag");
                    MultisampleHDRToLDRPipeline = ShaderCompiler.CompilePipeline("Multisample HDR to LDR", BuiltIn.FullscreenTriangleVertex, multisampleHdrToLdr);

                    MultisampleHDRSampler = RenderDataUtil.CreateSampler2DMultisample("Multisample HDR Postprocess Sampler", MagFilter.Linear, MinFilter.Linear, 0, WrapMode.ClampToEdge, WrapMode.ClampToEdge);
                }
            }

            {
                VectorscopePipeline = ShaderCompiler.CompilePipeline("Vectorscope", "./Shaders/VectorScopeTest.vert", "./Shaders/Unlit.frag");
                HDRSceneVectorscopeBuffer = RenderDataUtil.CreateDataBuffer("Vectorscope buffer", Width * Height, 4 * 4, BufferFlags.Dynamic);
            }

            {
                Shadowmap = RenderDataUtil.CreateEmptyFramebuffer("Shadowmap");
                /*
                var shaowMap = RenderDataUtil.CreateEmpty2DTexture("Shadowmap Texture", TextureFormat.Depth16, 2048 * 2, 2048 * 2);
                RenderDataUtil.AddDepthAttachment(Shadowmap, shaowMap, 0);
                */
                ShadowmapCascadeArray = RenderDataUtil.CreateEmpty2DTextureArray("Shadowmap cascades", TextureFormat.Depth24, 2048, 2048, Cascades); //*/4096, 4096, Cascades);
                RenderDataUtil.AddDepthLayerAttachment(Shadowmap, ShadowmapCascadeArray, 0, 0);

                // The shadowmap should not resize with the size of the screen!
                // Screen.RegisterFramebuffer(Shadowmap);

                var status = RenderDataUtil.CheckFramebufferComplete(Shadowmap, RenderData.FramebufferTarget.Draw);
                if (status != FramebufferStatus.FramebufferComplete)
                {
                    Debug.Break();
                }

                ShadowSampler = RenderDataUtil.CreateShadowSampler2DArray("Shadowmap sampler", MagFilter.Linear, MinFilter.Linear, 16f, WrapMode.ClampToEdge, WrapMode.ClampToEdge, DepthTextureCompareMode.RefToTexture, DepthTextureCompareFunc.Greater);
            }

            var skyVertexProgram = ShaderCompiler.CompileProgram("Sky Vertex", ShaderStage.Vertex, "./Shaders/Sky.vert");
            var skyFragmentProgram = ShaderCompiler.CompileProgram("Sky Fragment", ShaderStage.Fragment, "./Shaders/Sky.frag");

            var skyPipeline = ShaderCompiler.CompilePipeline("Sky", skyVertexProgram, skyFragmentProgram);

            Material skyMat = new Material("Sky Material", skyPipeline, null);

            var sunPosition = new Vector3(100, 100, 20);
            Sky = new Sky(skyMat,
                sunPosition.Normalized(),
                new Color4(1f, 1f, 1f, 1f),
                //new Color4(2f, 3f, 6f, 1f),
                new Color4(2f/6f, 3f/6f, 6f/6f, 1f),
                new Color4(0.188f, 0.082f, 0.016f, 1f));

            Editor.Editor.InitEditor(this);

            // Setup an always bound VAO
            RenderDataUtil.SetupGlobalVAO();

            watch.Stop();
            Debug.WriteLine($"OnLoad took {watch.ElapsedMilliseconds}ms");
        }

        public float ShaderReloadCheckTimer = 0;
        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            Editor.Profiling.NewFrame();
            // FIXME: passID = -1 is bad
            Editor.Profiling.PushSpan("Frame", -1);

            ShaderReloadCheckTimer += (float)args.Time;
            if (ShaderReloadCheckTimer >= 1)
            {
                LiveShaderLoader.RecompileShadersIfNeeded();
                ShaderReloadCheckTimer = 0;
            }

            Screen.NewFrame();

            float deltaTime = (float)args.Time;

            Phys.Update(deltaTime);

            var ppos = TestBoxTransform.LocalPosition;
            TestBox.UpdateTransform(TestBoxTransform);

            Debug.NewFrame(Width, Height);

            ImGuiController.Update(this, (float)args.Time);
            // Update above calls ImGui.NewFrame()...
            // ImGui.NewFrame();

            Player.Update(deltaTime);

            for (int i = 0; i < Transform.Transforms.Count; i++)
            {
                Transform.Transforms[i].UpdateMatrices();
            }

            Lights.UpdateBufferData();

            RenderPassMetrics metrics;
            if (Editor.Editor.InEditorMode)
            {
                Editor.Editor.EditorCamera.Transform.UpdateMatrices();

                RenderScene(Editor.Editor.EditorCamera, out metrics);

                Editor.Editor.ShowEditor(ref metrics);
            }
            else
            {
                RenderScene(Player.Camera, out metrics);
            }

            Editor.Editor.ShowProfiler(metrics);

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

            using (_ = RenderDataUtil.PushGenericPass("SwapBuffer"))
            {
                SwapBuffers();
            }

            Editor.Profiling.PopSpan(-1);
        }

        public void RenderScene(Camera camera, out RenderPassMetrics metrics)
        {
            using var sceneDebugGroup = RenderDataUtil.PushGenericPass("Scene");

            metrics = default;

            // Disable culling here. We do this because the drawlist rendering can change this setting.
            // I don't want to figure out in what ways the current code relies on having no culling.
            // - 2020-12-21
            RenderDataUtil.SetCullMode(CullMode.None);

            RenderDataUtil.SetDepthTesting(true);

            // To be able to clear the depth buffer we need to enable writing to it
            RenderDataUtil.SetDepthWrite(true);

            RenderDataUtil.SetColorWrite(ColorChannels.All);

            RenderDataUtil.SetClearColor(camera.ClearColor);
            RenderDataUtil.Clear(ClearMask.Color | ClearMask.Depth);

            // FIXME: Find a better place to do this work.
            camera.UpdateUniformBuffer();

            // FIXME: Calculate this at a better place?
            Sky.SunDirection = Sky.Transform.LocalRotation * Vector3.UnitX;

            SkySettings skySettings = new SkySettings()
            {
                SunDirection = Sky.SunDirection,
                SunColor = Sky.SunColor,
                SkyColor = Sky.SkyColor,
                GroundColor = Sky.GroundColor,
            };

            Span<Matrix4> lightViews = stackalloc Matrix4[Cascades];
            Span<Matrix4> lightProjs = stackalloc Matrix4[Cascades];
            Span<Vector3> lightPoss = stackalloc Vector3[Cascades];
            Span<Matrix4> lightSpaces = stackalloc Matrix4[Cascades];

            Camera shadowCamera = Player.Camera;
            Camera cullingCamera = Player.Camera;
            shadowCamera = camera;
            cullingCamera = camera;

            Span<float> splits = stackalloc float[Cascades];
            for (int i = 0; i < splits.Length; i++)
            {
                splits[i] = Shadows.CalculateZSplit(i + 1, splits.Length, shadowCamera.NearPlane, shadowCamera.FarPlane, CorrectionFactor);
            }

            // FIXME: This is a bad allocation to have!
            RefList<ShadowCaster> shadowCasters = new RefList<ShadowCaster>();
            foreach (var meshRenderer in MeshRenderer.Instances)
            {
                if (meshRenderer.CastShadows)
                {
                    shadowCasters.Add(new ShadowCaster(){
                        AABB = MeshRenderer.RecalculateAABB(meshRenderer.Mesh.AABB, meshRenderer.Transform),
                        CulledMask = 0,
                    });
                }
            }

            CascadedShadowContext cascadesContext = new CascadedShadowContext()
            {
                Camera = shadowCamera,
                // FIXME: A better way to get the resolution of an FBO
                ShadowMapResolution = Shadowmap.DepthAttachment!.Value.Texture.Size2D,
                Direction = -Sky.SunDirection,
                ShadowCasters = shadowCasters.AsSpan(),
                CorrectionFactor = CorrectionFactor,

                Splits = splits,
                LightPositions = lightPoss,
                LightViews = lightViews,
                LightProjs = lightProjs,
            };

            Shadows.CalculateCascades(ref cascadesContext);

            for (int i = 0; i < lightSpaces.Length; i++)
            {
                lightSpaces[i] = lightViews[i] * lightProjs[i];
            }

            Matrix4[] lightMatrices = lightSpaces.ToArray();

            // What we want to do here is first render the shadowmaps using all renderers
            // Then do a z prepass from the normal camera
            // Then do the final color pass

            // FIXME: Do shadow passes for the lights that need them
            using (_ = RenderDataUtil.PushDepthPass("Directional Shadow"))
            {
                for (int i = 0; i < Cascades; i++)
                {
                    using (_ = RenderDataUtil.PushDepthPass($"Cascade {i}", i))
                    {
                        RenderPassSettings shadowPass = new RenderPassSettings()
                        {
                            Name = $"Cascade {i}",

                            IsDepthPass = true,
                            IsTransparentPass = false,
                            View = lightViews[i],
                            Projection = lightProjs[i],
                            ViewPos = lightPoss[i],

                            // FIXME: Are these correct??
                            // or are these conceptually just the main camera
                            // even if we are using a different projection??
                            NearPlane = camera.NearPlane,
                            FarPlane = camera.FarPlane,

                            // FIXME: We need to be able to do orthographic culling here!
                            //CullingData = CameraFrustumCullingData.FromCamera(camera),
                            CameraUniforms = camera.UniformData,

                            Sky = skySettings,
                            AmbientLight = new Color4(0.1f, 0.1f, 0.1f, 1f),

                            Lights = Lights,

                            ShadowCasterCullingData = shadowCasters,
                            Cascade = i,
                        };

                        // We do not want to resize the Shadowmap with the screen size!
                        // Screen.ResizeToScreenSizeIfNecessary(Shadowmap);
                        RenderDataUtil.AddDepthLayerAttachment(Shadowmap, ShadowmapCascadeArray, 0, i);

                        RenderDataUtil.BindDrawFramebufferSetViewport(Shadowmap);

                        RenderDataUtil.SetDepthWrite(true);
                        RenderDataUtil.SetColorWrite(ColorChannels.None);
                        RenderDataUtil.Clear(ClearMask.Depth);
                        RenderDataUtil.SetDepthFunc(DepthFunc.PassIfLessOrEqual);

                        MeshRenderer.Render(ref shadowPass);

                        metrics.Combine(shadowPass.Metrics);
                    }
                }
            }

            CascadeSplits.X = splits[0];
            CascadeSplits.Y = splits[1];
            CascadeSplits.Z = splits[2];
            CascadeSplits.W = splits[3];

            if (ImGui.Begin("Cascades"))
            {
                CascadeSplits.X = Util.LinearDepthToNDC(splits[0], shadowCamera.NearPlane, shadowCamera.FarPlane);
                CascadeSplits.Y = Util.LinearDepthToNDC(splits[1], shadowCamera.NearPlane, shadowCamera.FarPlane);
                CascadeSplits.Z = Util.LinearDepthToNDC(splits[2], shadowCamera.NearPlane, shadowCamera.FarPlane);
                CascadeSplits.W = Util.LinearDepthToNDC(splits[3], shadowCamera.NearPlane, shadowCamera.FarPlane);

                CascadeSplits.X = splits[0];
                CascadeSplits.Y = splits[1];
                CascadeSplits.Z = splits[2];
                CascadeSplits.W = splits[3];

                ImGui.Text($"Cascade 1: {splits[0]}m ({CascadeSplits.X})");
                ImGui.Text($"Cascade 2: {splits[1]}m ({CascadeSplits.Y})");
                ImGui.Text($"Cascade 3: {splits[2]}m ({CascadeSplits.Z})");
                ImGui.Text($"Cascade 4: {splits[3]}m ({CascadeSplits.W})");

                ImGui.SliderFloat("Correction", ref CorrectionFactor, 0, 1);
                ImGui.SliderInt("Layer", ref DebugCascadeSelection, 0, 3);

                for (int i = 0; i < splits.Length; i++)
                {
                    ImGui.Text($"Split {i}: {splits[i]}");
                }

                var imageRef = ImGuiController.ReferenceTextureArray(Shadowmap.DepthAttachment?.Texture!, -1, DebugCascadeSelection);
                ImGui.Image((IntPtr)imageRef, new System.Numerics.Vector2(500, 500), new System.Numerics.Vector2(0, 1), new System.Numerics.Vector2(1, 0));

            }
            ImGui.End();

            Framebuffer colorBuffer = UseMSAA ? MultisampleHDRSceneBuffer : HDRSceneBuffer;

            Matrix4 proj;
            using (_ = RenderDataUtil.PushDepthPass("Depth prepass"))
            {
                Screen.ResizeToScreenSizeIfNecessary(colorBuffer);

                RenderDataUtil.BindDrawFramebufferSetViewport(colorBuffer);

                camera.CalcProjectionMatrix(out proj);

                RenderPassSettings depthPrePass = new RenderPassSettings()
                {
                    Name = "Depth prepass",

                    IsDepthPass = true,
                    IsTransparentPass = false,
                    View = camera.Transform.WorldToLocal,
                    Projection = proj,
                    ViewPos = camera.Transform.WorldPosition,

                    NearPlane = camera.NearPlane,
                    FarPlane = camera.FarPlane,

                    CullingData = CameraFrustumCullingData.FromCamera(cullingCamera),
                    CameraUniforms = camera.UniformData,

                    Sky = skySettings,
                    AmbientLight = new Color4(0.1f, 0.1f, 0.1f, 1f),

                    Lights = Lights,
                };

                RenderDataUtil.SetDepthWrite(true);
                RenderDataUtil.SetColorWrite(ColorChannels.None);
                RenderDataUtil.Clear(ClearMask.Depth);
                
                RenderDataUtil.SetDepthFunc(DepthFunc.PassIfLessOrEqual);

                MeshRenderer.Render(ref depthPrePass);

                metrics.Combine(depthPrePass.Metrics);
            }

            using (_ = RenderDataUtil.PushColorPass("Color pass"))
            {
                RenderPassSettings colorPass = new RenderPassSettings()
                {
                    Name = "Color pass",

                    IsDepthPass = false,
                    IsTransparentPass = false,
                    View = camera.Transform.WorldToLocal,
                    Projection = proj,
                    ViewPos = camera.Transform.WorldPosition,

                    NearPlane = camera.NearPlane,
                    FarPlane = camera.FarPlane,

                    CullingData = CameraFrustumCullingData.FromCamera(cullingCamera),
                    CameraUniforms = camera.UniformData,

                    Sky = skySettings,
                    AmbientLight = new Color4(0.1f, 0.1f, 0.1f, 1f),

                    UseShadows = true,
                    ShadowMap = ShadowmapCascadeArray,
                    ShadowSampler = ShadowSampler,
                    Cascades = CascadeSplits,
                    LightMatrices = lightMatrices,

                    Lights = Lights,
                };

                RenderDataUtil.SetDepthWrite(false);
                RenderDataUtil.SetColorWrite(ColorChannels.All);
                RenderDataUtil.Clear(ClearMask.Color);

                RenderDataUtil.SetDepthFunc(DepthFunc.PassIfEqual);

                //Sky.Render(ref colorPass);
                MeshRenderer.Render(ref colorPass);
                // FIXME: Add transparent switch to render pass thing!
                //TrailRenderer.Render(ref colorPass);

                metrics.Combine(colorPass.Metrics);
            }

            using (_ = RenderDataUtil.PushColorPass("Transparent pass"))
            {
                RenderPassSettings transparentPass = new RenderPassSettings()
                {
                    Name = "Transparent pass",

                    IsDepthPass = false,
                    IsTransparentPass = true,
                    View = camera.Transform.WorldToLocal,
                    Projection = proj,
                    ViewPos = camera.Transform.WorldPosition,

                    NearPlane = camera.NearPlane,
                    FarPlane = camera.FarPlane,

                    CullingData = CameraFrustumCullingData.FromCamera(cullingCamera),
                    CameraUniforms = camera.UniformData,

                    Sky = skySettings,
                    AmbientLight = new Color4(0.1f, 0.1f, 0.1f, 1f),

                    UseShadows = true,
                    ShadowMap = ShadowmapCascadeArray,
                    ShadowSampler = ShadowSampler,
                    Cascades = CascadeSplits,
                    LightMatrices = lightMatrices,

                    Lights = Lights,
                };

                RenderDataUtil.SetDepthWrite(false);
                RenderDataUtil.SetColorWrite(ColorChannels.All);
                RenderDataUtil.SetDepthFunc(DepthFunc.PassIfLessOrEqual);
                RenderDataUtil.SetBlendModeFull(true,
                    BlendEquationMode.FuncAdd,
                    BlendEquationMode.FuncAdd,
                    BlendingFactorSrc.SrcAlpha,
                    BlendingFactorDest.OneMinusSrcAlpha,
                    BlendingFactorSrc.SrcAlpha,
                    BlendingFactorDest.OneMinusSrcAlpha);

                // FIXME: Add transparent switch to render pass thing!
                //SkyRenderer.Render(ref transparentPass);
                //MeshRenderer.Render(ref transparentPass);
                TrailRenderer.Render(ref transparentPass);

                if (camera == Player.Camera)
                {
                    using (_ = RenderDataUtil.PushGenericPass("AABB Debug"))
                    {
                        MeshRenderer.RenderAABBs(Debug.DepthTestList);
                    }
                }

                if (false) {
                    var cullingData = CameraFrustumCullingData.FromCamera(cullingCamera);

                    DebugHelper.FrustumPoints(Editor.Gizmos.GizmoDrawList, cullingData.Points,
                        Color4.Blue, Color4.Cyan);

                    FrustumPlanes planes = cullingData.Planes;
                    Vector3 average = cullingData.Position + cullingData.Forward * 100;
                    Vector2 halfSize = (10, 10);

                    DebugHelper.Plane(Editor.Gizmos.GizmoDrawList, planes.Near, average, halfSize, Color4.Red);
                    DebugHelper.Plane(Editor.Gizmos.GizmoDrawList, planes.Far, average, halfSize, Color4.Green);
                    DebugHelper.Plane(Editor.Gizmos.GizmoDrawList, planes.Left, average, halfSize, Color4.Blue);
                    DebugHelper.Plane(Editor.Gizmos.GizmoDrawList, planes.Right, average, halfSize, Color4.Yellow);
                    DebugHelper.Plane(Editor.Gizmos.GizmoDrawList, planes.Top, average, halfSize, Color4.Magenta);
                    DebugHelper.Plane(Editor.Gizmos.GizmoDrawList, planes.Bottom, average, halfSize, Color4.Cyan);

                    var plane = planes.Near;
                    DebugHelper.Line(Editor.Gizmos.GizmoDrawList, plane.Normal * plane.Offset, plane.Normal * plane.Offset + plane.Normal * 10, Color4.Cyan, Color4.White);
                }

                    //var frustum = Shadows.SliceFrustum(shadowCamera, splits[0], splits[1]);

                    //DebugHelper.Quad(Debug.DepthTestList, frustum.Near00, frustum.Near01, frustum.Near10, frustum.Near11, new Color4(1, 0, 0, 0.5f));

                    DrawListSettings drawListSettings = new DrawListSettings
                    {
                        DepthTest = true,
                        DepthWrite = false,
                        Vp = camera.Transform.WorldToLocal * proj,
                        CullMode = CullMode.None,
                    };

                    RenderDataUtil.UsePipeline(Debug.DebugPipeline);
                    DrawListRenderer.RenderDrawList(Debug.DepthTestList, ref drawListSettings);

                    RenderDataUtil.SetNormalAlphaBlending();

                    metrics.Combine(drawListSettings.Metrics);
                    metrics.Combine(transparentPass.Metrics);
                }

                using (_ = RenderDataUtil.PushGenericPass("HDR to LDR pass"))
                {
                    RenderDataUtil.SetCullMode(CullMode.Back);

                    RenderDataUtil.BindDrawFramebuffer(null);
                    RenderDataUtil.SetViewport(0, 0, Width, Height);

                    if (UseMSAA)
                    {
                        RenderDataUtil.UsePipeline(MultisampleHDRToLDRPipeline);

                        RenderDataUtil.Uniform1("Tonemap", ShaderStage.Fragment, (int)CurrentTonemap);
                        RenderDataUtil.Uniform1("HDR", ShaderStage.Fragment, 0);
                        RenderDataUtil.Uniform1("Samples", ShaderStage.Fragment, MultisampleHDRSceneBuffer.ColorAttachments![0].ColorTexture.Samples);
                        RenderDataUtil.BindTexture(0, MultisampleHDRSceneBuffer.ColorAttachments![0].ColorTexture, MultisampleHDRSampler);
                    }
                    else
                    {
                        RenderDataUtil.UsePipeline(HDRToLDRPipeline);

                        RenderDataUtil.Uniform1("Tonemap", ShaderStage.Fragment, (int)CurrentTonemap);
                        RenderDataUtil.Uniform1("HDR", ShaderStage.Fragment, 0);
                        RenderDataUtil.BindTexture(0, HDRSceneBuffer.ColorAttachments![0].ColorTexture, HDRSampler);
                    }

                    RenderDataUtil.SetDepthWrite(false);
                    RenderDataUtil.SetColorWrite(ColorChannels.All);
                    RenderDataUtil.Clear(ClearMask.Color);

                    RenderDataUtil.SetDepthFunc(DepthFunc.AlwaysPass);
                    RenderDataUtil.SetNormalAlphaBlending();

                    RenderDataUtil.BindIndexBuffer(null);
                    RenderDataUtil.DrawArrays(Primitive.Triangles, 0, 3);

                    metrics.Vertices += 3;
                }

                /**
                using (_ = RenderDataUtil.PushGenericPass("HDR to Vectorscope"))
                {
                    GL.BindBuffer(BufferTarget.PixelPackBuffer, HDRSceneVectorscopeBuffer.Handle);

                    //GL.PixelStore(PixelStoreParameter.PackAlignment, 1);

                    GL.GetTextureImage(HDRSceneBuffer.ColorAttachments![0].ColorTexture.Handle, 0, PixelFormat.Rgba, PixelType.Float, HDRSceneVectorscopeBuffer.SizeInBytes, IntPtr.Zero);

                    GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);

                    RenderDataUtil.UsePipeline(VectorscopePipeline);

                    RenderDataUtil.BindIndexBuffer(null);
                    RenderDataUtil.DisableAllVertexAttributes();
                    RenderDataUtil.BindVertexAttribBuffer(0, HDRSceneVectorscopeBuffer);
                    var attrib = new AttributeSpecification("Test", 4, RenderData.AttributeType.Float, false, 0);
                    RenderDataUtil.SetAndEnableVertexAttribute(0, attrib);
                    RenderDataUtil.LinkAttributeBuffer(0, 0);

                    RenderDataUtil.DrawArrays(Primitive.Points, 0, HDRSceneVectorscopeBuffer.Elements);
                }
                */

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
                        CullMode = CullMode.None,
                    };
                    DrawListRenderer.RenderDrawList(Debug.List, ref settings);

                    metrics.Combine(settings.Metrics);
                }
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

            //QuadTransform.LocalRotation *= Quaternion.FromAxisAngle(new Vector3(0, -1, 0), 0.1f * MathF.PI * (float)args.Time);
            //QuadTransform.LocalPosition = new Vector3(1, MathF.Sin(TotalTime * MathF.PI * 0.2f), -2);

            ChildTransform.LocalRotation *= Quaternion.FromAxisAngle(new Vector3(1, 0, 0), MathF.PI * (float)args.Time);

            //Camera.Transform.Rotation *= Quaternion.FromAxisAngle(new Vector3(0, 1, 0), 2 * MathF.PI * (float)args.Time);

            // Exit if needed
            if (IsKeyPressed(Keys.Escape))
            {
                Close();
            }

            if (IsKeyPressed(Keys.H))
            {
                if (LightFalloff == 0)
                {
                    LightFalloff = 1;
                }
                else
                {
                    LightFalloff = 0;
                }
            }

            if (IsKeyPressed(Keys.F11))
            {
                if (WindowState == WindowState.Fullscreen)
                {
                    WindowState = WindowState.Normal;
                }
                else
                {
                    WindowState = WindowState.Fullscreen;
                }
            }

            // Toggle editor mode
            var ctrlDown = KeyboardState.IsKeyDown(Keys.LeftControl) | KeyboardState.IsKeyDown(Keys.Right);
            if (ctrlDown && KeyboardState.IsKeyPressed(Keys.E))
            {
                Editor.Editor.InEditorMode = !Editor.Editor.InEditorMode;
                
            }

            // FIXME: Make a keyboard input thing that has a on/off toggle to
            // make this easier.
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
            var io = ImGui.GetIO();
            if (io.WantCaptureKeyboard)
            {
                return;
            }

            Player.UpdateControls(keyboard, deltaTime);
        }

        public void HandleMouse(MouseState mouse, float deltaTime)
        {
            // FIXME: Make this better so that we don't have to split mouse and keyboard input!
            var io = ImGui.GetIO();
            if (io.WantCaptureMouse)
                return;

            Player.UpdateCamera(mouse, deltaTime);
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
                        //Debug.Break();
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
