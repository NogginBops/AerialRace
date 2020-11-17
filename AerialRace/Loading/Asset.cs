using AerialRace.Debugging;
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

        public AssetDB()
        { }

        public void LoadAllAssetsFromDirectory(string dirPath, bool recursive)
        {
            var directory = new DirectoryInfo(dirPath);

            // Texture assets
            {
                var textureAssetFiles = directory.GetFiles("*.textureasset", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                TextureAssets = new List<TextureAsset>();
                foreach (var textureAssetFile in textureAssetFiles)
                {
                    var textureAsset = TextureAsset.Parse(directory, textureAssetFile);
                    if (textureAsset != null)
                    {
                        TextureAssets.Add(textureAsset);
                    }
                }
            }

            // Mesh assets
            {
                var meshAssetFiles = directory.GetFiles("*.meshasset", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                MeshAssets = new List<MeshAsset>();
                foreach (var textureAssetFile in meshAssetFiles)
                {
                    var meshAsset = MeshAsset.Parse(directory, textureAssetFile);
                    if (meshAsset != null)
                    {
                        MeshAssets.Add(meshAsset);
                    }
                }
            }
        }

        public Asset? SelectedAsset;
        public void ShowAssetBrowser()
        {
            if (ImGui.Begin("Asset browser"))
            {
                ImGui.Columns(2);

                ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnDoubleClick | ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanAvailWidth;
                if (ImGui.TreeNodeEx("Textures", flags))
                {
                    TextureAssetList(ref SelectedAsset);

                    ImGui.TreePop();
                }

                if (ImGui.TreeNodeEx("Meshes", flags))
                {
                    MeshAssetList(ref SelectedAsset);

                    ImGui.TreePop();
                }

                // After all asset lists
                ImGui.NextColumn();

                AssetInspector(SelectedAsset);

                ImGui.End();
            }
        }

        #region AssetLists

        public void TextureAssetList(ref Asset? selected)
        {
            foreach (var asset in TextureAssets)
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

        public void MeshAssetList(ref Asset? selected)
        {
            foreach (var asset in MeshAssets)
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

        #endregion

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
                default:
                    {
                        ImGui.Text($"There is no inspector for the asset type '{asset.GetType()}'");
                    }
                    break;
            }
        }

        public void TextureAssetInspector(TextureAsset asset)
        {
            // FIXME: Undo
            const ImGuiInputTextFlags inputFlags = ImGuiInputTextFlags.EnterReturnsTrue;
            if (ImGui.InputText("Name", ref asset.Name, 200, inputFlags))
                asset.MarkDirty();

            ImGui.LabelText("Guid", $"{{{asset.AssetID}}}");
            ImGui.LabelText("Asset path", asset.AssetFilePath);
            ImGui.LabelText("Texture path", asset.TexturePath);
            //ImGui.SameLine();
            //if (ImGui.Button("Browse..."))
            //{
            // open a file browser!
            //}

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
                ImGui.Separator();

                TexturePreview(asset.LoadedTexture, asset.GenerateMips);
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

        public Asset(string name, string assetFilePath)
        {
            Name = name;
            AssetID = Guid.NewGuid();
            AssetFilePath = assetFilePath;
            IsDirty = true;
            WriteToDisk();
        }

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

        public TextureAsset(string name, string assetFilePath) : base(name, assetFilePath)
        {
            TexturePath = null;
        }

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

        public MeshAsset(string name, string assetFilePath) : base(name, assetFilePath)
        {
            MeshPath = null;
        }

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
}
