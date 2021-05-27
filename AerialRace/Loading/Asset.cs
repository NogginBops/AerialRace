using AerialRace.Debugging;
using AerialRace.DebugGui;
using AerialRace.RenderData;
using ImGuiNET;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AerialRace.Loading
{
    enum AssetType
    {
        Texture,
        Mesh,
        Shader,
        Material,
        PhysicsMaterial,
        StaticSetpiece,
        Scene,
    }

    struct AssetRef<T> where T : Asset
    {
        public Guid AssetID;

        public bool HasRef => AssetID != Guid.Empty;

        public AssetRef(Guid id)
        {
            AssetID = id;
        }

        public override string ToString()
        {
            return AssetID.ToString();
        }
    }

    public struct AssetBaseInfo
    {
        public string Name;
        public Guid AssetID;
        public string AssetFilePath;

        public static AssetBaseInfo NewAsset(string name, string path)
        {
            return new AssetBaseInfo
            {
                Name = name,
                AssetID = Guid.NewGuid(),
                AssetFilePath = path
            };
        }
    }

    // FIXME: Check that the Giud that we get are actually unique!!
    class AssetDB
    {
        public Dictionary<Guid, Asset> AssetDictionary = new Dictionary<Guid, Asset>();
        public List<TextureAsset> TextureAssets = new List<TextureAsset>();
        public List<MeshAsset> MeshAssets = new List<MeshAsset>();
        public List<ShaderAsset> ShaderAssets = new List<ShaderAsset>();
        public List<MaterialAsset> MaterialAssets = new List<MaterialAsset>();
        public List<PhysicsMaterialAsset> PhysicsMaterialAssets = new List<PhysicsMaterialAsset>();
        public List<StaticSetpieceAsset> StaticSetpieceAssets = new List<StaticSetpieceAsset>();

        private string BaseDirectory = Directory.GetCurrentDirectory();
        private ImGuiFileBrowser FileBrowser = new ImGuiFileBrowser() { /*CurrentPath = Directory.GetCurrentDirectory()*/ };

        private bool PathField(string name, in string? path)
        {
            bool browsePath = false;

            ImGui.LabelText(name, path);
            ImGui.SameLine();
            ImGui.PushID(name);
            if (ImGui.Button("Browse..."))
                browsePath = true;

            ImGui.PopID();

            if (browsePath)
            {
                ImGui.OpenPopup("Open File");
                FileBrowser.SetPath(Path.GetDirectoryName(path) + "/" ?? "./");

                return true;
            }

            return false;
        }

        public bool AssetField<T>(string name, ref AssetRef<T> assetRef) where T : Asset
        {
            T? t = ResolveReference(assetRef);
            if (t == null)
            {
                ImGui.LabelText(name, $"null");
            }
            else
            {
                ImGui.LabelText(name, $"{t.Name} {{{t.AssetID}}}");
            }
            ImGui.SameLine();
            ImGui.PushID(assetRef.AssetID.ToString());

            bool changed = false;
            if (ImGui.Button("Browse..."))
            {
                // open asset browser
            }
            
            ImGui.PopID();

            return changed;
        }

        private bool ShowBrowser(string filter = "*.*")
        {
            return FileBrowser.ShowFileDialog("Open File", DialogMode.Open, new System.Numerics.Vector2(700, 310), filter);
        }

        public string GetSelectedPath()
        {
            if (FileBrowser.SelectedPath.StartsWith(BaseDirectory))
            {
                return Path.GetRelativePath(BaseDirectory, FileBrowser.SelectedPath);
            }
            else
            {
                // TODO: Move files or stuff!!
                Debug.WriteLine($"[WARNING] Selected path is not inside the asset folder! '{FileBrowser.SelectedPath}'");
                return FileBrowser.SelectedPath;
            }
        }

        public AssetDB()
        { }

        public void LoadAllAssetsFromDirectory(string dirPath, bool recursive)
        {
            var directory = new DirectoryInfo(dirPath);

            LoadAssets(directory, recursive, "*.textureasset",  ref TextureAssets, AssetDictionary, TextureAsset.Parse);
            LoadAssets(directory, recursive, "*.meshasset",     ref MeshAssets, AssetDictionary, MeshAsset.Parse);
            LoadAssets(directory, recursive, "*.shaderasset",   ref ShaderAssets, AssetDictionary, ShaderAsset.Parse);
            LoadAssets(directory, recursive, "*.materialasset", ref MaterialAssets, AssetDictionary, MaterialAsset.Parse);
            LoadAssets(directory, recursive, "*.physmatasset",  ref PhysicsMaterialAssets, AssetDictionary, PhysicsMaterialAsset.Parse);
            LoadAssets(directory, recursive, "*.setpiece", ref StaticSetpieceAssets, AssetDictionary, StaticSetpieceAsset.Parse);

            static void LoadAssets<T>(DirectoryInfo directory, bool recursive, string searchPattern, ref List<T> assets, Dictionary<Guid, Asset> assetDictionary, AssetLoadFunction<T> loadFunction) where T : Asset
            {
                var seachOpt = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var assetFiles = directory.GetFiles(searchPattern, seachOpt);
                assets = new List<T>();
                foreach (var assetFile in assetFiles)
                {
                    var asset = loadFunction(directory, assetFile);
                    if (asset != null)
                    {
                        assets.Add(asset);
                        assetDictionary.Add(asset.AssetID, asset);
                    }
                }
            }
        }

        public delegate T? AssetLoadFunction<T>(DirectoryInfo directory, FileInfo file) where T : Asset;

        public T? ResolveReference<T>(AssetRef<T> @ref) where T : Asset
        {
            if (@ref.HasRef == false) return null;

            if (AssetDictionary.TryGetValue(@ref.AssetID, out var asset))
            {
                return (T)asset;
            }
            else
            {
                return null;
            }
        }

        public Asset? SelectedAsset;
        public void ShowAssetBrowser()
        {
            if (ImGui.Begin("Asset browser", ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoScrollbar))
            {
                bool openCreateTexture = false;

                if (ImGui.BeginMenuBar())
                {
                    if (ImGui.BeginMenu("Create..."))
                    {
                        if (ImGui.MenuItem("New Texture"))
                        {
                            openCreateTexture = true;
                        }

                        ImGui.EndMenu();
                    }

                    ImGui.EndMenuBar();
                }

                if (openCreateTexture)
                {
                    ImGui.OpenPopup("Create Texture");
                }

                if (ShowCreateTexturePopup("Create Texture", openCreateTexture, out TextureAsset? newTextureAsset))
                {
                    TextureAssets.Add(newTextureAsset);
                }

                ImGui.Columns(2);
                
                if (ImGui.BeginChild("Browser"))
                {
                    ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnDoubleClick | ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanAvailWidth;

                    if (ImGui.TreeNodeEx("Textures", flags))
                    {
                        AssetList(TextureAssets, ref SelectedAsset);

                        ImGui.TreePop();
                    }

                    if (ImGui.TreeNodeEx("Meshes", flags))
                    {
                        AssetList(MeshAssets, ref SelectedAsset);

                        ImGui.TreePop();
                    }

                    if (ImGui.TreeNodeEx("Shaders", flags))
                    {
                        AssetList(ShaderAssets, ref SelectedAsset);

                        ImGui.TreePop();
                    }

                    if (ImGui.TreeNodeEx("Materials", flags))
                    {
                        AssetList(MaterialAssets, ref SelectedAsset);

                        ImGui.TreePop();
                    }

                    if (ImGui.TreeNodeEx("Physics Materials", flags))
                    {
                        AssetList(PhysicsMaterialAssets, ref SelectedAsset);

                        ImGui.TreePop();
                    }

                    if (ImGui.TreeNodeEx("Static Setpieces", flags))
                    {
                        AssetList(StaticSetpieceAssets, ref SelectedAsset);

                        ImGui.TreePop();
                    }
                }
                ImGui.EndChild();
                
                // After all asset lists
                ImGui.NextColumn();

                if (ImGui.BeginChild("Inspector"))
                {
                    AssetInspector(SelectedAsset);
                }
                ImGui.EndChild();
            }
            ImGui.End();
        }

        public static void AssetList<T>(List<T> assets, ref Asset? selected) where T : Asset
        {
            foreach (var asset in assets)
            {
                var flags = ImGuiTreeNodeFlags.OpenOnDoubleClick | ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanAvailWidth;
                flags |= ImGuiTreeNodeFlags.Leaf;

                if (selected == asset)
                {
                    flags |= ImGuiTreeNodeFlags.Selected;
                }

                bool isOpen = ImGui.TreeNodeEx(asset.Name, flags);

                if (ImGui.IsItemClicked())
                {
                    selected = asset;
                }

                if (isOpen)
                {
                    ImGui.TreePop();
                }
            }
        }

        #region Inspectors

        public void AssetInspector(Asset? asset)
        {
            if (asset == null)
            {
                ImGui.Text("No asset selected!");
                return;
            }

            switch (asset)
            {
                case TextureAsset ta:
                    TextureAssetInspector(ta);
                    break;
                case MeshAsset ma:
                    MeshAssetInspector(ma);
                    break;
                case ShaderAsset sa:
                    ShaderAssetInspector(sa);
                    break;
                case MaterialAsset ma:
                    MaterialAssetInspector(ma);
                    break;
                case PhysicsMaterialAsset pma:
                    PhysicsMaterialAssetInspector(pma);
                    break;
                case StaticSetpieceAsset ssa:
                    StaticSetpieceAssetInspector(ssa);
                    break;
                default:
                    {
                        ImGui.Text($"There is no inspector for the asset type '{asset.GetType()}'");
                    }
                    break;
            }
        }

        private static void BasicAssetInspector(Asset asset)
        {
            // FIXME: Undo
            const ImGuiInputTextFlags inputFlags = ImGuiInputTextFlags.EnterReturnsTrue;
            if (ImGui.InputText("Name", ref asset.Name, 200, inputFlags))
                asset.MarkDirty();

            ImGui.LabelText("Guid", $"{{{asset.AssetID}}}");
            if (asset.AssetID == Guid.Empty)
            {
                ImGui.SameLine();
                if (ImGui.Button("Generate guid"))
                {
                    asset.AssetID = Guid.NewGuid();
                    asset.MarkDirty();
                }
            }
            
            ImGui.LabelText("Asset path", asset.AssetFilePath);
        }

        public void TextureAssetInspector(TextureAsset asset)
        {
            // FIXME: Undo
            BasicAssetInspector(asset);

            bool setTexturePath = PathField("Texture path", in asset.TexturePath);

            if (ImGui.Checkbox("Generate mips", ref asset.GenerateMips)) asset.MarkDirty();
            if (ImGui.Checkbox("Is sRGB", ref asset.IsSrgb)) asset.MarkDirty();

            if (ShowBrowser())
            {
                if (setTexturePath)
                {
                    asset.TexturePath = GetSelectedPath();
                    asset.MarkDirty();
                }
            }

            if (asset.IsDirty)
            {
                if (ImGui.Button("Save changes"))
                {
                    RenderDataUtil.DeleteTexture(ref asset.LoadedTexture);
                    asset.WriteToDisk();
                }
            }
            else
            {
                ImGui.Spacing();
            }

            if (asset.LoadedTexture == null) asset.LoadTexture();

            if (asset.LoadedTexture != null)
            {
                // FIXME: This draws a line through both columns...
                //ImGui.Separator();
                ImGui.Spacing();

                TexturePreview(asset.LoadedTexture, asset.GenerateMips);
            }
        }

        public void MeshAssetInspector(MeshAsset asset)
        {
            BasicAssetInspector(asset);

            bool setMeshPath = PathField("Mesh path", in asset.MeshPath);

            if (ShowBrowser())
            {
                if (setMeshPath)
                {
                    asset.MeshPath = GetSelectedPath();
                    asset.MarkDirty();
                }
            }

            if (asset.IsDirty)
            {
                if (ImGui.Button("Save changes"))
                {
                    //RenderDataUtil.DeleteTexture(ref asset);
                    asset.WriteToDisk();
                }
            }
            else
            {
                ImGui.Spacing();
            }
        }

        public void ShaderAssetInspector(ShaderAsset asset)
        {
            BasicAssetInspector(asset);

            bool setVertex = false, setGeometry = false, setFragment = false;

            if (asset.VertexShaderPath != null)
                setVertex = PathField("Vertex path", in asset.VertexShaderPath);

            if (asset.GeometryShaderPath != null)
                setGeometry = PathField("Geometry path", in asset.GeometryShaderPath);

            if (asset.FragmentShaderPath != null)
                setFragment = PathField("Fragment path", in asset.FragmentShaderPath);

            if (ShowBrowser())
            {
                string selectedPath = GetSelectedPath();
                if (setVertex)
                {
                    asset.VertexShaderPath = selectedPath;
                    asset.MarkDirty();
                }

                if (setGeometry)
                {
                    asset.GeometryShaderPath = selectedPath;
                    asset.MarkDirty();
                }

                if (setFragment)
                {
                    asset.FragmentShaderPath = selectedPath;
                    asset.MarkDirty();
                }
            }

            if (asset.IsDirty)
            {
                if (ImGui.Button("Save changes"))
                {
                    //RenderDataUtil.DeleteTexture(ref asset.LoadedTexture);
                    asset.WriteToDisk();
                }
            }
            else
            {
                ImGui.Spacing();
            }
        }

        public void MaterialAssetInspector(MaterialAsset asset)
        {
            BasicAssetInspector(asset);

            if (AssetField("Pipeline", ref asset.Pipeline)) asset.MarkDirty();
            if (AssetField("Depth Pipeline", ref asset.DepthPipeline)) asset.MarkDirty();

            if (asset.IsDirty)
            {
                if (ImGui.Button("Save changes"))
                {
                    //RenderDataUtil.DeleteTexture(ref asset.LoadedTexture);
                    asset.WriteToDisk();
                }
            }
            else
            {
                ImGui.Spacing();
            }
        }

        public void PhysicsMaterialAssetInspector(PhysicsMaterialAsset asset)
        {
            BasicAssetInspector(asset);

            {
                float freq = asset.Material.SpringSettings.Frequency;
                if (ImGui.DragFloat("Frequency", ref freq))
                {
                    asset.Material.SpringSettings.Frequency = freq;
                    asset.MarkDirty();
                }
            }
            
            {
                float damping = asset.Material.SpringSettings.DampingRatio;
                if (ImGui.DragFloat("Damping Ratio", ref damping))
                {
                    asset.Material.SpringSettings.DampingRatio = damping;
                    asset.MarkDirty();
                }
            }

            if (ImGui.DragFloat("Friction Coefficient", ref asset.Material.FrictionCoefficient))
                asset.MarkDirty();

            if (ImGui.DragFloat("Maximum Recovery Velocity", ref asset.Material.MaximumRecoveryVelocity))
                asset.MarkDirty();

            if (asset.IsDirty)
            {
                if (ImGui.Button("Save changes"))
                {
                    asset.WriteToDisk();
                }
            }
            else
            {
                ImGui.Spacing();
            }
        }

        public void StaticSetpieceAssetInspector(StaticSetpieceAsset asset)
        {
            BasicAssetInspector(asset);

            if (AssetField("Mesh", ref asset.Mesh)) asset.MarkDirty();
            if (AssetField("Material", ref asset.Material)) asset.MarkDirty();

            if (ImGui.Checkbox("Use Mesh As Collider", ref asset.UseMeshAsCollider))
                asset.MarkDirty();

            if (ImGui.Checkbox("Generate Convex Hull", ref asset.GenerateConvexHull))
                asset.MarkDirty();

            if (AssetField("Physics Material", ref asset.PhysicsMaterial)) asset.MarkDirty();

            if (asset.IsDirty)
            {
                if (ImGui.Button("Save changes"))
                {
                    asset.WriteToDisk();
                }
            }
            else
            {
                ImGui.Spacing();
            }
        }

        #endregion

        #region Preview

        public float previewLevel = 0;
        public bool showMip = false;
        public void TexturePreview(Texture texture, bool hasMips)
        {
            if (ImGui.CollapsingHeader($"Texture preview '{texture.Name}'", ImGuiTreeNodeFlags.DefaultOpen))
            {
                float level = -1;
                if (hasMips)
                {
                    ImGui.Checkbox("Show mip level", ref showMip);

                    if (showMip)
                    {
                        ImGui.SliderFloat("Mip", ref previewLevel, texture.BaseLevel, texture.MaxLevel);
                        level = previewLevel;
                    }
                    else
                    {
                        ImGui.TextDisabled("Not showing any specific mip");
                    }

                    ImGui.Spacing();
                }

                var size = new System.Numerics.Vector2(256, 256);
                var uv0 = new System.Numerics.Vector2(1, 0);
                var uv1 = new System.Numerics.Vector2(0, 1);
                ImGui.Image((IntPtr)ImGuiController.ReferenceTexture(texture, level), size, uv1, uv0);
            }
        }

        #endregion

        #region Creators

        public AssetBaseInfo CreateAssetInfo;

        public string CreateTexturePath = "";
        public bool CreateGenerateMips;
        public bool CreateIsSrgb;
        public bool ShowCreateTexturePopup(string name, bool opening, [NotNullWhen(true)] out TextureAsset? asset)
        {
            bool open = true;
            ImGuiIOPtr io = ImGui.GetIO();
            ImGui.SetNextWindowPos(io.DisplaySize * 0.5f, ImGuiCond.Appearing, new System.Numerics.Vector2(0.5f, 0.5f));
            if (ImGui.BeginPopupModal(name, ref open, ImGuiWindowFlags.None))
            {
                if (opening)
                {
                    Debug.Print($"Opening '{name}'!!");

                    // FIXME: Unique name
                    CreateAssetInfo.Name = "texture";
                    CreateAssetInfo.AssetFilePath = "./texture.textureasset";
                    CreateAssetInfo.AssetID = Guid.NewGuid();

                    CreateTexturePath = "";
                    CreateGenerateMips = true;
                    CreateIsSrgb = false;
                }

                // FIXME: Max length
                ImGui.InputText("Name", ref CreateAssetInfo.Name, 256);

                bool setAssetPath = PathField("Asset path", in CreateAssetInfo.AssetFilePath);

                ImGui.LabelText("Asset ID", $"{CreateAssetInfo.AssetID}");

                bool setTexturePath = PathField("Texture path", in CreateTexturePath);

                ImGui.Checkbox("Generate Mips", ref CreateGenerateMips);
                ImGui.Checkbox("Is sRGB", ref CreateIsSrgb);

                if (ShowBrowser())
                {
                    string selectedPath = GetSelectedPath();

                    if (setAssetPath) CreateAssetInfo.AssetFilePath = selectedPath;

                    if (setTexturePath) CreateTexturePath = selectedPath;
                }

                // FIXME: Validate the asset!
                if (ImGui.Button("Create Asset"))
                {
                    asset = new TextureAsset(CreateAssetInfo, CreateTexturePath, CreateGenerateMips, CreateIsSrgb);
                    return true;
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }
            

            asset = null;
            return false;
        }

        #endregion
    }

    abstract class Asset
    {
        public string Name;
        public Guid AssetID;
        /// <summary>
        /// Path the file containing data about this asset.
        /// </summary>
        public string AssetFilePath;

        public bool IsDirty = false;

        public abstract AssetType Type { get; }

        public Asset(in AssetBaseInfo info)
        {
            Name = info.Name;
            AssetID = info.AssetID;
            AssetFilePath = info.AssetFilePath;
        }

        public void MarkDirty() => IsDirty = true;

        public void WriteIfDirty()
        {
            if (IsDirty) WriteToDisk();
        }

        public void WriteToDisk()
        {
            using var writer = new StreamWriter(File.OpenWrite(AssetFilePath), Encoding.UTF8, -1, false);

            writer.WriteLine($"Name: {Name}");
            writer.WriteLine($"AssetID: {AssetID}");

            WriteAssetProperties(writer);

            writer.Flush();

            IsDirty = false;
        }

        public abstract void WriteAssetProperties(TextWriter writer);

        public AssetRef<T> GetRef<T>() where T : Asset
        {
            if (GetType().IsAssignableTo(typeof(T)) == false)
                throw new Exception("Can't convert this asset into a ref of that type!");
            return new AssetRef<T>(AssetID);
        }
    }

    class TextureAsset : Asset
    {
        public override AssetType Type => AssetType.Texture;

        public string? TexturePath;
        public bool GenerateMips = false;
        public bool IsSrgb = false;

        public Texture? LoadedTexture;

        public TextureAsset(in AssetBaseInfo assetInfo, string? texturePath, bool generateMips, bool isSrgb) : base(assetInfo)
        {
            TexturePath = texturePath;
            GenerateMips = generateMips;
            IsSrgb = isSrgb;
        }

        public static TextureAsset? Parse(DirectoryInfo assetDirectory, FileInfo assetFile)
        {
            // FIXME: We don't want assets without a guid to pass this!
            using var textReader = assetFile.OpenText();
            AssetBaseInfo info = default;
            info.AssetFilePath = Path.GetRelativePath(assetDirectory.FullName, assetFile.FullName);
            string? texturePath = default;
            bool generateMips = false;
            bool isSrgb = false;
            while (textReader.EndOfStream == false)
            {
                var line = textReader.ReadLine()!;
                if (line.StartsWith("Name: "))
                {
                    info.Name = line.Substring("Name: ".Length);
                }
                else if (line.StartsWith("AssetID: "))
                {
                    if (Guid.TryParse(line.Substring("AssetID: ".Length), out info.AssetID) == false)
                    {
                        Debug.WriteLine($"[Asset] Error: Guid for asset {info.Name} was corrupt. Here is a new one {{{Guid.NewGuid()}}}");
                        return null;
                    }
                }
                else if (line.StartsWith("Texture: "))
                {
                    texturePath = line.Substring("Texture: ".Length);
                }
                else if (line.StartsWith("GenerateMips: "))
                {
                    if (bool.TryParse(line.Substring("GenerateMips: ".Length), out generateMips) == false)
                    {
                        Debug.WriteLine($"[Asset] Error: GenerateMips property for texture asset {info.Name} was corrupt.");
                        return null;
                    }
                }
                else if (line.StartsWith("IsSrgb: "))
                {
                    if (bool.TryParse(line.Substring("IsSrgb: ".Length), out isSrgb) == false)
                    {
                        Debug.WriteLine($"[Asset] Error: IsSRGB property for texture asset {info.Name} was corrupt.");
                        return null;
                    }
                }
            }

            return new TextureAsset(info, texturePath, generateMips, isSrgb);
        }

        public override void WriteAssetProperties(TextWriter writer)
        {
            writer.WriteLine($"Texture: {TexturePath}");
            writer.WriteLine($"GenerateMips: {GenerateMips}");
            writer.WriteLine($"IsSrgb: {IsSrgb}");
        }

        [MemberNotNullWhen(true, nameof(LoadedTexture))]
        public bool LoadTexture()
        {
            if (LoadedTexture != null)
            {
                Debug.Print($"Texture asset '{Name}' is already loaded!");
                return false;
            }

            if (TexturePath == null)
            {
                Debug.Print($"Texture asset '{Name}' doesn't have a texture path.");
                return false;
            }

            LoadedTexture = TextureLoader.LoadRgbaImage(Name, TexturePath, GenerateMips, IsSrgb);

            return true;
        }
    }

    class MeshAsset : Asset
    {
        public override AssetType Type => AssetType.Mesh;

        public string? MeshPath;

        public MeshData? MeshData;
        public Mesh? Mesh;

        public MeshAsset(in AssetBaseInfo assetInfo, string? meshPath) : base(assetInfo)
        {
            MeshPath = meshPath;
        }

        public static MeshAsset? Parse(DirectoryInfo assetDirectory, FileInfo assetFile)
        {
            // FIXME: We don't want assets without a guid to pass this!
            using var textReader = assetFile.OpenText();
            AssetBaseInfo info = default;
            info.AssetFilePath = Path.GetRelativePath(assetDirectory.FullName, assetFile.FullName);
            string? meshPath = default;
            while (textReader.EndOfStream == false)
            {
                var line = textReader.ReadLine()!;
                if (line.StartsWith("Name: "))
                {
                    info.Name = line.Substring("Name: ".Length);
                }
                else if (line.StartsWith("AssetID: "))
                {
                    if (Guid.TryParse(line.Substring("AssetID: ".Length), out info.AssetID) == false)
                    {
                        Debug.WriteLine($"[Asset] Error: Guid for asset {info.Name} was corrupt. Here is a new one {{{Guid.NewGuid()}}}");
                        return null;
                    }
                }
                else if (line.StartsWith("Mesh: "))
                {
                    meshPath = line.Substring("Mesh: ".Length);
                }
            }

            return new MeshAsset(info, meshPath);
        }

        public override void WriteAssetProperties(TextWriter writer)
        {
            writer.WriteLine($"Mesh: {MeshPath}");
        }

        [MemberNotNullWhen(true, nameof(Mesh))]
        [MemberNotNullWhen(true, nameof(MeshData))]
        public bool LoadMesh()
        {
            if (MeshPath == null)
                return false;

            MeshData = MeshLoader.LoadObjMesh(MeshPath);
            Mesh = RenderDataUtil.CreateMesh(Name, MeshData ?? throw new Exception());
            return true;
        }
    }

    class ShaderAsset : Asset
    {
        public override AssetType Type => AssetType.Shader;

        public string? VertexShaderPath;
        public string? GeometryShaderPath;
        public string? FragmentShaderPath;

        public ShaderPipeline? LoadedPipeline;

        public ShaderAsset(in AssetBaseInfo assetInfo, string? vertex, string? geometry, string? fragment) : base(assetInfo)
        {
            VertexShaderPath = vertex;
            GeometryShaderPath = geometry;
            FragmentShaderPath = fragment;
        }

        public static ShaderAsset? Parse(DirectoryInfo assetDirectory, FileInfo assetFile)
        {
            // FIXME: We don't want assets without a guid to pass this!
            using var textReader = assetFile.OpenText();
            AssetBaseInfo info = default;
            info.AssetFilePath = Path.GetRelativePath(assetDirectory.FullName, assetFile.FullName);
            string? vertex = default;
            string? geometry = default;
            string? fragment = default;
            while (textReader.EndOfStream == false)
            {
                var line = textReader.ReadLine()!;
                if (line.StartsWith("Name: "))
                {
                    info.Name = line.Substring("Name: ".Length);
                }
                else if (line.StartsWith("AssetID: "))
                {
                    if (Guid.TryParse(line.Substring("AssetID: ".Length), out info.AssetID) == false)
                    {
                        Debug.WriteLine($"[Asset] Error: Guid for asset {info.Name} was corrupt. Here is a new one {{{Guid.NewGuid()}}}");
                        return null;
                    }
                }
                else if (line.StartsWith("Vertex: "))
                {
                    vertex = line.Substring("Vertex: ".Length);
                }
                else if (line.StartsWith("Geometry: "))
                {
                    geometry = line.Substring("Geometry: ".Length);
                }
                else if (line.StartsWith("Fragment: "))
                {
                    fragment = line.Substring("Fragment: ".Length);
                }
            }

            return new ShaderAsset(info, vertex, geometry, fragment);
        }

        public override void WriteAssetProperties(TextWriter writer)
        {
            if (VertexShaderPath != null) writer.WriteLine($"Vertex: {VertexShaderPath}");
            if (GeometryShaderPath != null) writer.WriteLine($"Geometry: {GeometryShaderPath }");
            if (FragmentShaderPath != null) writer.WriteLine($"Fragment: {FragmentShaderPath}");
        }

        [MemberNotNullWhen(true, nameof(LoadedPipeline))]
        public bool LoadShader()
        {
            if (LoadedPipeline != null)
            {
                Debug.Print($"Shader asset '{Name}' is already loaded!");
                return false;
            }

            ShaderProgram? vertex = null;
            ShaderProgram? geometry = null;
            ShaderProgram? fragment = null;

            // FIXME: Check for errors and display them in the shader inspector!!

            if (VertexShaderPath != null)
                
                vertex = ShaderCompiler.CompileProgram(
                    Name + " Vertex",
                    ShaderStage.Vertex,
                    VertexShaderPath);

            if (GeometryShaderPath != null)
                geometry = ShaderCompiler.CompileProgram(
                    Name + " Geometry",
                    ShaderStage.Geometry,
                    GeometryShaderPath);

            if (FragmentShaderPath != null)
                fragment = ShaderCompiler.CompileProgram(
                    Name + " Fragment",
                    ShaderStage.Fragment,
                    FragmentShaderPath);

            LoadedPipeline = ShaderCompiler.CompilePipeline(Name, vertex, geometry, fragment);
            return true;
        }
    }

    class MaterialAsset : Asset
    {
        public override AssetType Type => AssetType.Material;

        public AssetRef<ShaderAsset> Pipeline;
        public AssetRef<ShaderAsset> DepthPipeline;

        // FIXME: Material properties
        //public 

        public Material? LoadedMaterial;

        public MaterialAsset(in AssetBaseInfo assetInfo, AssetRef<ShaderAsset> pipeline, AssetRef<ShaderAsset> depthPipeline) : base(assetInfo)
        {
            Pipeline = pipeline;
            DepthPipeline = depthPipeline;
        }

        public static MaterialAsset? Parse(DirectoryInfo assetDirectory, FileInfo assetFile)
        {
            // FIXME: We don't want assets without a guid to pass this!
            using var textReader = assetFile.OpenText();
            AssetBaseInfo info = default;
            info.AssetFilePath = Path.GetRelativePath(assetDirectory.FullName, assetFile.FullName);
            AssetRef<ShaderAsset> pipeline = default;
            AssetRef<ShaderAsset> depthPipeline = default;
            while (textReader.EndOfStream == false)
            {
                var line = textReader.ReadLine()!;
                if (line.StartsWith("Name: "))
                {
                    info.Name = line.Substring("Name: ".Length);
                }
                else if (line.StartsWith("AssetID: "))
                {
                    if (Guid.TryParse(line.Substring("AssetID: ".Length), out info.AssetID) == false)
                    {
                        Debug.WriteLine($"[Asset] Error: Guid for asset {info.Name} was corrupt. Here is a new one {{{Guid.NewGuid()}}}");
                        return null;
                    }
                }
                else if (line.StartsWith("Pipeline: "))
                {
                    pipeline = new AssetRef<ShaderAsset>(Guid.Parse(line["Pipeline: ".Length..]));
                }
                else if (line.StartsWith("DepthPipeline: "))
                {
                    depthPipeline = new AssetRef<ShaderAsset>(Guid.Parse(line["DepthPipeline: ".Length..]));
                }
            }

            return new MaterialAsset(info, pipeline, depthPipeline);
        }

        public override void WriteAssetProperties(TextWriter writer)
        {
            if (Pipeline.HasRef) writer.WriteLine($"Pipeline: {Pipeline.AssetID}");
            if (DepthPipeline.HasRef) writer.WriteLine($"DepthPipeline: {DepthPipeline.AssetID}");
        }

        // FIXME: Properties
        [MemberNotNullWhen(true, nameof(LoadedMaterial))]
        public bool LoadMaterial(AssetDB db)
        {
            var pipeline = db.ResolveReference(Pipeline);
            var depthPipeline = db.ResolveReference(DepthPipeline);

            // If both are null there is nothing to load really
            if (pipeline == null && depthPipeline == null)
                return false;

            if (pipeline == null || pipeline.LoadShader() == false)
                return false;

            if (depthPipeline == null || depthPipeline.LoadShader() == false)
                return false;

            LoadedMaterial = new Material(Name, pipeline.LoadedPipeline!, depthPipeline.LoadedPipeline);
            return true;
        }
    }

    class PhysicsMaterialAsset : Asset
    {
        public override AssetType Type => AssetType.PhysicsMaterial;

        public Physics.SimpleMaterial Material;

        public PhysicsMaterialAsset(in AssetBaseInfo assetInfo, Physics.SimpleMaterial material) : base(assetInfo)
        {
            Material = material;
        }

        public static PhysicsMaterialAsset? Parse(DirectoryInfo assetDirectory, FileInfo assetFile)
        {
            // FIXME: We don't want assets without a guid to pass this!
            using var textReader = assetFile.OpenText();
            AssetBaseInfo info = default;
            info.AssetFilePath = Path.GetRelativePath(assetDirectory.FullName, assetFile.FullName);
            AerialRace.Physics.SimpleMaterial material = default;
            while (textReader.EndOfStream == false)
            {
                var line = textReader.ReadLine()!;
                if (line.StartsWith("Name: "))
                {
                    info.Name = line.Substring("Name: ".Length);
                }
                else if (line.StartsWith("AssetID: "))
                {
                    if (Guid.TryParse(line.Substring("AssetID: ".Length), out info.AssetID) == false)
                    {
                        Debug.WriteLine($"[Asset] Error: Guid for asset {info.Name} was corrupt. Here is a new one {{{Guid.NewGuid()}}}");
                        return null;
                    }
                }
                else if (line.StartsWith("Frequency: "))
                {
                    material.SpringSettings.Frequency = float.Parse(line.Substring("Frequency: ".Length), CultureInfo.InvariantCulture);
                }
                else if (line.StartsWith("DampingRatio: "))
                {
                    material.SpringSettings.DampingRatio = float.Parse(line.Substring("DampingRatio: ".Length), CultureInfo.InvariantCulture);
                }
                else if (line.StartsWith("FrictionCoefficient: "))
                {
                    material.FrictionCoefficient = float.Parse(line.Substring("FrictionCoefficient: ".Length), CultureInfo.InvariantCulture);
                }
                else if (line.StartsWith("MaximumRecoveryVelocity: "))
                {
                    material.MaximumRecoveryVelocity = float.Parse(line.Substring("MaximumRecoveryVelocity: ".Length), CultureInfo.InvariantCulture);
                }
            }

            return new PhysicsMaterialAsset(info, material);
        }

        public override void WriteAssetProperties(TextWriter writer)
        {
            var culture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            writer.WriteLine($"Frequency: {Material.SpringSettings.Frequency}");
            writer.WriteLine($"DampingRatio: {Material.SpringSettings.DampingRatio}");
            writer.WriteLine($"FrictionCoefficient: {Material.FrictionCoefficient}");
            writer.WriteLine($"MaximumRecoveryVelocity: {Material.MaximumRecoveryVelocity}");

            CultureInfo.CurrentCulture = culture;
        }
    }

    class StaticSetpieceAsset : Asset
    {
        public override AssetType Type => AssetType.StaticSetpiece;

        public AssetRef<MeshAsset> Mesh;
        public AssetRef<MaterialAsset> Material;

        public bool UseMeshAsCollider;
        public bool GenerateConvexHull;
        public AssetRef<PhysicsMaterialAsset> PhysicsMaterial;

        public StaticSetpiece? Setpiece;

        public StaticSetpieceAsset(in AssetBaseInfo assetInfo, AssetRef<MeshAsset> mesh, AssetRef<MaterialAsset> material, bool useMeshAsCollider, bool generateConvexHull, AssetRef<PhysicsMaterialAsset> physicsMaterial) : base(assetInfo)
        {
            Mesh = mesh;
            Material = material;
            UseMeshAsCollider = useMeshAsCollider;
            GenerateConvexHull = generateConvexHull;
            PhysicsMaterial = physicsMaterial;
        }

        public static StaticSetpieceAsset? Parse(DirectoryInfo assetDirectory, FileInfo assetFile)
        {
            // FIXME: We don't want assets without a guid to pass this!
            using var textReader = assetFile.OpenText();
            AssetBaseInfo info = default;
            info.AssetFilePath = Path.GetRelativePath(assetDirectory.FullName, assetFile.FullName);
            AssetRef<MeshAsset> mesh = default;
            AssetRef<MaterialAsset> material = default;
            bool? useMeshAsCollider = default;
            bool? generateConvexHull = default;
            AssetRef<PhysicsMaterialAsset> physicsMaterial = default;
            while (textReader.EndOfStream == false)
            {
                var line = textReader.ReadLine()!;
                if (line.StartsWith("Name: "))
                {
                    info.Name = line.Substring("Name: ".Length);
                }
                else if (line.StartsWith("AssetID: "))
                {
                    if (Guid.TryParse(line.Substring("AssetID: ".Length), out info.AssetID) == false)
                    {
                        Debug.WriteLine($"[Asset] Error: Guid for asset {info.Name} was corrupt. Here is a new one {{{Guid.NewGuid()}}}");
                        return null;
                    }
                }
                else if (line.StartsWith("Mesh: "))
                {
                    mesh = new AssetRef<MeshAsset>(Guid.Parse(line["Mesh: ".Length..]));
                }
                else if (line.StartsWith("Material: "))
                {
                    material = new AssetRef<MaterialAsset>(Guid.Parse(line["Material: ".Length..]));
                }
                else if (line.StartsWith("UseMeshAsCollider: "))
                {
                    useMeshAsCollider = bool.Parse(line["UseMeshAsCollider: ".Length..]);
                }
                else if (line.StartsWith("GenerateConvexHull: "))
                {
                    generateConvexHull = bool.Parse(line["GenerateConvexHull: ".Length..]);
                }
                else if (line.StartsWith("PhysicsMaterial: "))
                {
                    physicsMaterial = new AssetRef<PhysicsMaterialAsset>(Guid.Parse(line["PhysicsMaterial: ".Length..]));
                }
            }

            Debug.AssertNotNull(useMeshAsCollider, nameof(useMeshAsCollider));
            Debug.AssertNotNull(generateConvexHull, nameof(generateConvexHull));

            return new StaticSetpieceAsset(info, mesh, material, useMeshAsCollider.Value, generateConvexHull.Value, physicsMaterial);
        }

        public override void WriteAssetProperties(TextWriter writer)
        {
            if (Mesh.HasRef) writer.WriteLine($"Mesh: {Mesh.AssetID}");
            if (Material.HasRef) writer.WriteLine($"Material: {Material.AssetID}");
            writer.WriteLine($"UseMeshAsCollider: {UseMeshAsCollider}");
            writer.WriteLine($"GenerateConvexHull: {GenerateConvexHull}");
            if (PhysicsMaterial.HasRef) writer.WriteLine($"PhysicsMaterial: {PhysicsMaterial.AssetID}");
        }

        [MemberNotNullWhen(true, nameof(Setpiece))]
        public bool LoadStaticSetpiece(AssetDB db)
        {
            var mesh = db.ResolveReference(Mesh);
            var material = db.ResolveReference(Material);
            var physicsMaterial = db.ResolveReference(PhysicsMaterial);

            if (mesh == null || mesh.LoadMesh() == false)
                return false;

            if (material == null || material.LoadMaterial(db) == false)
                return false;

            if (physicsMaterial == null)
                return false;

            if (UseMeshAsCollider == false)
            {
                Debug.WriteLine("We only support MeshColliders for static meshes atm!!");
                return false;
            }

            var meshData = mesh.MeshData ?? throw new Exception();

            Physics.ICollider collider;
            if (GenerateConvexHull)
            {
                collider = new Physics.MeshCollider(meshData);
            }
            else
            {
                collider = new Physics.StaticMeshCollider(meshData, Vector3.One);
            }

            Setpiece = new StaticSetpiece(new Transform(), mesh.Mesh, material.LoadedMaterial, collider, physicsMaterial.Material);
            return true;
        }
    }
}
