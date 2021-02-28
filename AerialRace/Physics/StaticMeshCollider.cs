using AerialRace.Loading;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using OpenTK.Mathematics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AerialRace.Physics
{
    class StaticMeshCollider : ICollider
    {
        public BepuPhysics.Collidables.Mesh Mesh;

        public IShape Shape => Mesh;
        public TypedIndex TypedIndex { get; private set; }
        public Vector3 Center { get; private set; }

        public Box3 Bounds;

        public StaticMeshCollider(MeshData mesh)
        {
            // FIXME: We might want to make this faster!
            var positions = mesh.Vertices.Select(v => v.Position.AsNumerics()).ToArray();
            var tris = AllocateTriangles(mesh);

            var scale = Vector3.One;
            Mesh = new BepuPhysics.Collidables.Mesh(tris, scale.AsNumerics(), Phys.BufferPool);

            TypedIndex = Phys.Simulation.Shapes.Add(Mesh);

            //Center = center.ToOpenTK();
            Mesh.ComputeBounds(System.Numerics.Quaternion.Identity, out var min, out var max);
            Bounds = new Box3(min.AsOpenTK(), max.AsOpenTK());
        }

        public static Buffer<Triangle> AllocateTriangles(MeshData data)
        {
            var triangleCount = data.IndexType switch
            {
                RenderData.IndexBufferType.UInt8 => data.Int8Indices!.Length / 3,
                RenderData.IndexBufferType.UInt16 => data.Int16Indices!.Length / 3,
                RenderData.IndexBufferType.UInt32 => data.Int32Indices!.Length / 3,
                _ => throw new System.Exception(),
            };
            Phys.BufferPool.Take<Triangle>(triangleCount, out var buffer);

            for (int i = 0; i < triangleCount; i++)
            {
                int i1 = GetIndexFromMesh(i * 3 + 0, data);
                int i2 = GetIndexFromMesh(i * 3 + 1, data);
                int i3 = GetIndexFromMesh(i * 3 + 2, data);

                buffer[i] = new Triangle(
                    data.Vertices[i1].Position.AsNumerics(),
                    data.Vertices[i3].Position.AsNumerics(),
                    data.Vertices[i2].Position.AsNumerics()
                    );
            }

            return buffer;

            static int GetIndexFromMesh(int i, MeshData data)
            {
                switch (data.IndexType)
                {
                    case RenderData.IndexBufferType.UInt8:
                        return data.Int8Indices![i];
                    case RenderData.IndexBufferType.UInt16:
                        return (ushort)data.Int16Indices![i];
                    case RenderData.IndexBufferType.UInt32:
                        return data.Int32Indices![i];
                    default:
                        return -1;
                }
            }
        }
    }
}
