using AerialRace.Debugging;
using AerialRace.DebugGui;
using AerialRace.RenderData;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
                FileBrowser.CurrentPath = Path.GetDirectoryName(path) ?? "";

                return true;
            }

            return false;
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
                // Maybe have a warning for now?
                return FileBrowser.SelectedPath;
            }
        }

        public AssetDB()
        { }

        public void LoadAllAssetsFromDirectory(string dirPath, bool recursive)
        {
            var directory = new DirectoryInfo(dirPath);

            var seachOpt = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            // Texture assets
            {
                var assetFiles = directory.GetFiles("*.textureasset", seachOpt);
                TextureAssets = new List<TextureAsset>();
                foreach (var assetFile in assetFiles)
                {
                    var asset = TextureAsset.Parse(directory, assetFile);
                    if (asset != null)
                    {
                        TextureAssets.Add(asset);
                        AssetDictionary.Add(asset.AssetID, asset);
                    }
                }
            }

            // Mesh assets
            {
                var assetFiles = directory.GetFiles("*.meshasset", seachOpt);
                MeshAssets = new List<MeshAsset>();
                foreach (var assetFile in assetFiles)
                {
                    var asset = MeshAsset.Parse(directory, assetFile);
                    if (asset != null)
                    {
                        MeshAssets.Add(asset);
                        AssetDictionary.Add(asset.AssetID, asset);
                    }
                }
            }

            // Shader assets
            {
                var assetFiles = directory.GetFiles("*.shaderasset", seachOpt);
                ShaderAssets = new List<ShaderAsset>();
                foreach (var assetFile in assetFiles)
                {
                    var asset = ShaderAsset.Parse(directory, assetFile);
                    if (asset != null)
                    {
                        ShaderAssets.Add(asset);
                        AssetDictionary.Add(asset.AssetID, asset);
                    }
                }
            }

            // Material assets
            {
                var assetFiles = directory.GetFiles("*.materialasset", seachOpt);
                MaterialAssets = new List<MaterialAsset>();
                foreach (var assetFile in assetFiles)
                {
                    var asset = MaterialAsset.Parse(directory, assetFile);
                    if (asset != null)
                    {
                        MaterialAssets.Add(asset);
                        AssetDictionary.Add(asset.AssetID, asset);
                    }
                }
            }
        }

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
            if (ImGui.Begin("Asset browser", ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoFocusOnAppearing))
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

                // After all asset lists
                ImGui.NextColumn();

                AssetInspector(SelectedAsset);
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

            // FIXME: Asset ref fields
            ImGui.LabelText("Pipeline", $"{{{asset.Pipeline}}}");
            ImGui.LabelText("Depth Pipeline", $"{{{asset.DepthPipeline}}}");

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

        public string CreateTexturePath;
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
                RenderDataUtil.CreateShaderProgram(
                    Name + " Vertex",
                    ShaderStage.Vertex,
                    File.ReadAllText(VertexShaderPath),
                    out vertex);

            if (GeometryShaderPath != null)
                RenderDataUtil.CreateShaderProgram(
                    Name + " Geometry",
                    ShaderStage.Geometry,
                    File.ReadAllText(GeometryShaderPath),
                    out geometry);

            if (FragmentShaderPath != null)
                RenderDataUtil.CreateShaderProgram(
                    Name + " Fragment",
                    ShaderStage.Fragment,
                    File.ReadAllText(FragmentShaderPath),
                    out fragment);

            RenderDataUtil.CreatePipeline(Name, vertex, geometry, fragment, out LoadedPipeline);
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

    class StaticSetpieceAsset : Asset
    {
        public override AssetType Type => throw new NotImplementedException();

        public StaticSetpieceAsset(in AssetBaseInfo assetInfo) : base(assetInfo)
        {

        }

        public override void WriteAssetProperties(TextWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}
