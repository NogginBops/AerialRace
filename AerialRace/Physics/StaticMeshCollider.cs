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
    class StaticMeshCollider
    {
        public BepuPhysics.Collidables.Mesh Mesh;

        public IShape Shape => Mesh;
        public TypedIndex TypedIndex { get; private set; }
        public Vector3 Center { get; private set; }

        public Box3 Bounds;

        public StaticMeshCollider(MeshData mesh)
        {
            // FIXME: We might want to make this faster!
            var positions = mesh.Vertices.Select(v => v.Position.ToNumerics()).ToArray();
            Mesh = new BepuPhysics.Collidables.Mesh();

            TypedIndex = Phys.Simulation.Shapes.Add(Mesh);

            //Center = center.ToOpenTK();
            Mesh.ComputeBounds(System.Numerics.Quaternion.Identity, out var min, out var max);
            Bounds = new Box3(min.ToOpenTK(), max.ToOpenTK());
        }

        public static Buffer<Triangle> AllocateTriangles(MeshData data)
        {
            int triangleCount = 0;
            switch (data.IndexType)
            {
                case RenderData.IndexBufferType.UInt8:
                    triangleCount = data.Int8Indices!.Length / 3;
                    break;
                case RenderData.IndexBufferType.UInt16:
                    triangleCount = data.Int16Indices!.Length / 3;
                    break;
                case RenderData.IndexBufferType.UInt32:
                    triangleCount = data.Int32Indices!.Length / 3;
                    break;
                default:
                    throw new System.Exception();
            }
            
            Phys.BufferPool.Take<Triangle>(triangleCount, out var buffer);

            for (int i = 0; i < triangleCount; i++)
            {
                int i1 = GetIndexFromMesh(i * 3 + 0, data);
                int i2 = GetIndexFromMesh(i * 3 + 1, data);
                int i3 = GetIndexFromMesh(i * 3 + 2, data);

                buffer[0] = new Triangle(
                    data.Vertices[i1].Position.ToNumerics(),
                    data.Vertices[i2].Position.ToNumerics(),
                    data.Vertices[i3].Position.ToNumerics()
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
