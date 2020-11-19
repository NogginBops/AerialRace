﻿using AerialRace.Debugging;
using AerialRace.DebugGui;
using AerialRace.RenderData;
using ImGuiNET;
using System;
using System.Collections.Generic;
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
    }

    public struct AssetRef
    {
        public Guid AssetID;

        public AssetRef(Guid id)
        {
            AssetID = id;
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
        public List<TextureAsset> TextureAssets = new List<TextureAsset>();
        public List<MeshAsset> MeshAssets = new List<MeshAsset>();
        public List<ShaderAsset> ShaderAssets = new List<ShaderAsset>();

        private ImGuiFileBrowser FileBrowser = new ImGuiFileBrowser() { /*CurrentPath = Directory.GetCurrentDirectory()*/ };

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
                    }
                }
            }
        }

        public Asset? SelectedAsset;
        public void ShowAssetBrowser()
        {
            if (ImGui.Begin("Asset browser"))
            {
                if (ImGui.BeginMenuBar())
                {
                    if (ImGui.BeginMenu("Create..."))
                    {
                        ImGui.MenuItem("Test");
                    }

                    ImGui.EndMenuBar();
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

                // After all asset lists
                ImGui.NextColumn();

                AssetInspector(SelectedAsset);

                
            }
            ImGui.End();
        }

        public void AssetList<T>(List<T> assets, ref Asset? selected) where T : Asset
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
                default:
                    {
                        ImGui.Text($"There is no inspector for the asset type '{asset.GetType()}'");
                    }
                    break;
            }
        }

        private void BasicAssetInspector(Asset asset)
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

            bool browseTexturePath = false;

            ImGui.LabelText("Texture path", asset.TexturePath);
            ImGui.SameLine();
            if (ImGui.Button("Browse..."))
                browseTexturePath = true;
            ImGui.NewLine();

            if (browseTexturePath)
            {
                ImGui.OpenPopup("Open File");
                //FileBrowser.CurrentPath = Path.GetDirectoryName(asset.TexturePath) ?? "";
            }

            if (FileBrowser.ShowFileDialog("Open File", DialogMode.Open, new System.Numerics.Vector2(), "*.*"))
            {
                asset.TexturePath = FileBrowser.SelectedPath;
                asset.MarkDirty();
            }

            if (ImGui.Checkbox("Generate mips", ref asset.GenerateMips)) asset.MarkDirty();
            if (ImGui.Checkbox("Is sRGB", ref asset.IsSrgb)) asset.MarkDirty();

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

            ImGui.LabelText("Mesh path", asset.MeshPath);
            //ImGui.SameLine();
            //if (ImGui.Button("Browse..."))
            //{
            // open a file browser!
            //}

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

            if (asset.VertexShaderPath != null)
                ImGui.LabelText("Vertex path", asset.VertexShaderPath);

            if (asset.GeometryShaderPath != null)
                ImGui.LabelText("Geometry path", asset.GeometryShaderPath);

            if (asset.FragmentShaderPath != null)
                ImGui.LabelText("Fragment path", asset.FragmentShaderPath);

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

        public AssetRef GetRef()
        {
            return new AssetRef(AssetID);
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
                    new string[] {
                        File.ReadAllText(VertexShaderPath) 
                    }, 
                    out vertex);

            if (GeometryShaderPath != null)
                RenderDataUtil.CreateShaderProgram(
                    Name + " Geometry",
                    ShaderStage.Geometry,
                    new string[] {
                        File.ReadAllText(GeometryShaderPath) 
                    },
                    out geometry);

            if (FragmentShaderPath != null)
                RenderDataUtil.CreateShaderProgram(
                    Name + " Fragment",
                    ShaderStage.Fragment,
                    new string[] {
                        File.ReadAllText(FragmentShaderPath) 
                    },
                    out fragment);

            RenderDataUtil.CreatePipeline(Name, vertex, geometry, fragment, out LoadedPipeline);
            return true;
        }
    }
}
