using AerialRace.Debugging;
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

    class AssetDB
    {
        public Dictionary<AssetType, List<Asset>> Assets = new Dictionary<AssetType, List<Asset>>();

        public AssetDB()
        { }

        public void LoadAllAssetsFromDirectory(string dirPath, bool recursive)
        {
            var directory = new DirectoryInfo(dirPath);
            var textureAssetFiles = directory.GetFiles("*.textureasset", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

            List<Asset> textureAssets = new List<Asset>();
            Assets.Add(AssetType.Texture, textureAssets);

            foreach (var textureAssetFile in textureAssetFiles)
            {
                var textureAsset = TextureAsset.Parse(directory, textureAssetFile);
                if (textureAsset != null)
                {
                    textureAssets.Add(textureAsset);
                }
            }
        }
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

        public abstract Asset ParseAsset(string assetFile);

        public abstract Asset CreateAsset(string name, string assetFilePath);

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

        public TextureAsset(string name, string assetFilePath) : base(name, assetFilePath)
        {
            TexturePath = null;
        }

        public TextureAsset(in AssetBaseInfo assetInfo, string? texturePath) : base(assetInfo)
        {
            TexturePath = texturePath;
        }

        public override TextureAsset ParseAsset(string assetFile)
        {
            throw new NotImplementedException();
        }

        public override TextureAsset CreateAsset(string name, string assetFilePath)
        {
            return new TextureAsset(name, assetFilePath);
        }

        public static TextureAsset? Parse(DirectoryInfo assetDirectory, FileInfo assetFile)
        {
            // FIXME: We don't want assets without a guid to pass this!
            using var textReader = assetFile.OpenText();
            AssetBaseInfo info = default;
            info.AssetFilePath = Path.GetRelativePath(assetDirectory.FullName, assetFile.FullName);
            string? texturePath = default;
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
                        Debug.WriteLine($"[Asset] Error: Guid for asset {info.Name} was corrupt.");
                        return null;
                    }
                }
                else if (line.StartsWith("Texture: "))
                {
                    texturePath = line.Substring("Texture: ".Length);
                }
            }

            return new TextureAsset(info, texturePath);
        }

        public override void WriteAssetProperties(TextWriter writer)
        {
            writer.WriteLine($"Texture: {TexturePath}");
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

        public override MeshAsset ParseAsset(string assetFile)
        {
            throw new NotImplementedException();
        }

        public override MeshAsset CreateAsset(string name, string assetFilePath)
        {
            return new MeshAsset(name, assetFilePath);
        }

        public override void WriteAssetProperties(TextWriter writer)
        {
            writer.WriteLine($"Mesh: {MeshPath}");
        }
    }
}
