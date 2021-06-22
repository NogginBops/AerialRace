using AerialRace.Loading;
using AerialRace.RenderData;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using AerialRace.Debugging;

namespace AerialRace
{
    class TerrainRenderer : SelfCollection<TerrainRenderer>
    {
        static readonly AttributeSpecification PositionAttribute = new AttributeSpecification("Terrain Position", 2, AttributeType.Float, false, 0);

        struct ChunkData
        {
            public Vector2[] Positions;
            public short[] Indices;
            public int Width, Height;
        }

        public Transform Transform;
        public Texture HeightTexture;
        public Mesh Chunk;
        public Material Material;
        public float Height;

        public TerrainRenderer(string name, Texture heightTexture, float height, Material material)
        {
            Transform = new Transform(name);

            HeightTexture = heightTexture;
            Height = height;

            Material = material;

            List<Vector2> positions = new List<Vector2>();
            List<short> indices = new List<short>();
            const int SIZE = 64;
            for (int x = 0; x < SIZE; x++)
            {
                for (int y = 0; y < SIZE; y++)
                {
                    positions.Add(new Vector2(x, y));
                }
            }

            for (int x = 0; x < SIZE - 1; x++)
            {
                for (int y = 0; y < SIZE - 1; y++)
                {
                    indices.Add((short)x);
                    indices.Add((short)(x + SIZE));
                    indices.Add((short)(x + SIZE + 1));

                    indices.Add((short)x);
                    indices.Add((short)(x + SIZE + 1));
                    indices.Add((short)(x + 1));
                }
            }

            IndexBuffer indexbuffer = RenderDataUtil.CreateIndexBuffer($"{name}: Terrain Chunk Indices", CollectionsMarshal.AsSpan(indices), BufferFlags.None);

            RenderData.Buffer positionBuffer = RenderDataUtil.CreateDataBuffer($"{name}: Terrain Poitions", CollectionsMarshal.AsSpan(positions), BufferFlags.None);

            Chunk = new Mesh("Terrain chunk", indexbuffer, null);
            {
                int bufferIndex = Chunk.AddBuffer(positionBuffer);
                int attributeIndex = Chunk.AddAttribute(PositionAttribute);
                Chunk.AddLink(bufferIndex, attributeIndex);
            }

            Chunk.AABB = new Box3(new Vector3(0, 0, 0), new Vector3(SIZE, Height, SIZE));
        }

        public static void Render(ref RenderPassSettings settings)
        {
            foreach (var instance in Instances)
            {
                var transform = instance.Transform;
                var mesh = instance.Chunk;
                var material = instance.Material;

                RenderDataUtil.BindMeshData(instance.Chunk);

                if (settings.IsDepthPass)
                {
                    // FIXME: Should materials with no depth pipeline be considered transparent.
                    // i.e. not drawn in the depth pass?
                    RenderDataUtil.UsePipeline(material.DepthPipeline ?? material.Pipeline);
                }
                else
                {
                    RenderDataUtil.UsePipeline(material.Pipeline);
                }

                RenderDataUtil.SetCullMode(material.Properties.CullMode);

                // FIXME
                MeshRenderer.SetRenderPassUniforms(ref settings);
                MeshRenderer.SetPerModelData(ref transform.LocalToWorld, ref settings);

                int textureStartIndex = 0;

                // FIXME!! Make binding texture better!
                RenderDataUtil.Uniform1("UseShadows", settings.UseShadows ? 1 : 0);
                if (settings.UseShadows)
                {
                    textureStartIndex = 1;

                    RenderDataUtil.Uniform1("ShadowCascades", 0);
                    RenderDataUtil.BindTexture(0, settings.ShadowMap!, settings.ShadowSampler!);

                    RenderDataUtil.Uniform4("CascadeSplits", settings.Cascades);
                }

                var matProperties = material.Properties;
                for (int i = 0; i < matProperties.Textures.Count; i++)
                {
                    var ioff = textureStartIndex + i;

                    var texProp = matProperties.Textures[i];
                    var name = texProp.Name;

                    RenderDataUtil.Uniform1(name, ioff);
                    RenderDataUtil.BindTexture(ioff, texProp.Texture, texProp.Sampler);
                }

                for (int i = 0; i < matProperties.Properties.Count; i++)
                {
                    RenderDataUtil.UniformProperty(ref matProperties.Properties[i]);
                }

                RenderDataUtil.DrawElements(
                    Primitive.Triangles,
                    mesh.Indices!.Elements,
                    mesh.Indices.IndexType, 0);
            }
        }
    }
}
