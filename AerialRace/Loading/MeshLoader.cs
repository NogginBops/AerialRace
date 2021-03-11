using AerialRace.Debugging;
using AerialRace.RenderData;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization.Formatters;
using System.Text;
using Debug = AerialRace.Debugging.Debug;

namespace AerialRace.Loading
{
    public struct StandardVertex : IEquatable<StandardVertex>
    {
        public Vector3 Position;
        public Vector2 Uv;
        public Vector3 Normal;

        public StandardVertex(Vector3 position, Vector2 uv, Vector3 normal)
        {
            Position = position;
            Uv = uv;
            Normal = normal;
        }

        public override bool Equals(object? obj)
        {
            return obj is StandardVertex vertex && Equals(vertex);
        }

        public bool Equals(StandardVertex other)
        {
            return Position.Equals(other.Position) &&
                   Uv.Equals(other.Uv) &&
                   Normal.Equals(other.Normal);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Position, Uv, Normal);
        }

        public static bool operator ==(StandardVertex left, StandardVertex right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StandardVertex left, StandardVertex right)
        {
            return !(left == right);
        }
    }

    struct MeshData
    {
        public IndexBufferType IndexType;
        public int[]? Int32Indices;
        public short[]? Int16Indices;
        public byte[]? Int8Indices;
        public StandardVertex[] Vertices;
        public Box3 AABB;
        // FIXME: Add support for additional data streams
    }

    static class MeshLoader
    {
        public struct Face
        {
            public VertexIndices v1;
            public VertexIndices v2;
            public VertexIndices v3;
        }

        public struct VertexIndices : IEquatable<VertexIndices>
        {
            public int posIdx, uvIdx, normalIdx;

            public VertexIndices(int posIdx, int uvIdx, int normalIdx)
            {
                this.posIdx = posIdx;
                this.uvIdx = uvIdx;
                this.normalIdx = normalIdx;
            }

            public override bool Equals(object? obj)
            {
                return obj is VertexIndices indices && Equals(indices);
            }

            public bool Equals(VertexIndices other)
            {
                return posIdx == other.posIdx &&
                       uvIdx == other.uvIdx &&
                       normalIdx == other.normalIdx;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(posIdx, uvIdx, normalIdx);
            }

            public static bool operator ==(VertexIndices left, VertexIndices right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(VertexIndices left, VertexIndices right)
            {
                return !(left == right);
            }
        }

        public static MeshData LoadObjMesh(string filename)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            // TODO: Figure out if reading lines or reading the whole file is faster for this!
            string[] lines = File.ReadAllLines(filename);

            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<Vector3> norms = new List<Vector3>();

            List<Face> faces = new List<Face>();
            List<StandardVertex> vertices = new List<StandardVertex>();

            bool initAABB = false;
            Box3 aabb = default;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                var lineSpan = line.AsSpan();
                if (string.IsNullOrEmpty(line)) continue;
                
                if (line.StartsWithFast("v "))
                {
                    int index1 = line.IndexOf(' ') + 1;
                    int index2 = line.IndexOf(' ', index1) + 1;
                    int index3 = line.IndexOf(' ', index2) + 1;

                    float f1 = Util.ParseFloatFast(lineSpan[index1..(index2 - 1)]);
                    float f2 = Util.ParseFloatFast(lineSpan[index2..(index3 - 1)]);
                    float f3 = Util.ParseFloatFast(lineSpan[index3..]);

                    var pos = new Vector3(f1, f2, f3);
                    if (initAABB == false)
                    {
                        aabb = new Box3(pos, pos);
                        initAABB = true;
                    }
                    else
                    {
                        aabb.Inflate(pos);
                    }

                    verts.Add(pos);
                }
                else if (line.StartsWithFast("vt "))
                {
                    int index1 = line.IndexOf(' ') + 1;
                    int index2 = line.IndexOf(' ', index1) + 1;

                    float u = Util.ParseFloatFast(lineSpan[index1..(index2 - 1)]);
                    float v = Util.ParseFloatFast(lineSpan[index2..]);
                    
                    uvs.Add(new Vector2(u, v));
                }
                else if (line.StartsWithFast("vn "))
                {
                    int index1 = line.IndexOf(' ') + 1;
                    int index2 = line.IndexOf(' ', index1) + 1;
                    int index3 = line.IndexOf(' ', index2) + 1;

                    float n1 = Util.ParseFloatFast(lineSpan[index1..(index2 - 1)]);
                    float n2 = Util.ParseFloatFast(lineSpan[index2..(index3 - 1)]);
                    float n3 = Util.ParseFloatFast(lineSpan[index3..]);
                    
                    norms.Add(new Vector3(n1, n2, n3));
                }
                else if (line.StartsWithFast("f "))
                {
                    Face f;

                    int index1 = line.IndexOf(' ') + 1;
                    int index2 = line.IndexOf(' ', index1) + 1;
                    int index3 = line.IndexOf(' ', index2) + 1;

                    int index11 = line.IndexOf('/', index1, (index2 - 1) - index1) + 1;
                    int index12 = line.IndexOf('/', index11, (index2 - 1) - index11) + 1;

                    int index21 = line.IndexOf('/', index2, (index3 - 1) - index2) + 1;
                    int index22 = line.IndexOf('/', index21, (index3 - 1) - index21) + 1;

                    int index31 = line.IndexOf('/', index3, line.Length - index3) + 1;
                    int index32 = line.IndexOf('/', index31, line.Length - index31) + 1;

                    f.v1.posIdx = Util.ParseIntFast(lineSpan[index1..(index11 - 1)]) - 1;
                    f.v1.uvIdx = Util.ParseIntFast(lineSpan[index11..(index12 - 1)]) - 1;
                    f.v1.normalIdx = Util.ParseIntFast(lineSpan[index12..(index2 - 1)]) - 1;

                    f.v2.posIdx = Util.ParseIntFast(lineSpan[index2..(index21 - 1)]) - 1;
                    f.v2.uvIdx = Util.ParseIntFast(lineSpan[index21..(index22 - 1)]) - 1;
                    f.v2.normalIdx = Util.ParseIntFast(lineSpan[index22..(index3 - 1)]) - 1;

                    f.v3.posIdx = Util.ParseIntFast(lineSpan[index3..(index31 - 1)]) - 1;
                    f.v3.uvIdx = Util.ParseIntFast(lineSpan[index31..(index32 - 1)]) - 1;
                    f.v3.normalIdx = Util.ParseIntFast(lineSpan[index32..]) - 1;

                    faces.Add(f);
                }
                else continue;
            }

            Dictionary<VertexIndices, int> verticesIndexDict = new Dictionary<VertexIndices, int>();
            List<int> indices = new List<int>();

            int dups = 0;
            int index = 0;
            foreach (var face in faces)
            {
                if (verticesIndexDict.TryGetValue(face.v1, out int i1))
                {
                    indices.Add(i1);
                    dups++;
                }
                else
                {
                    indices.Add(index);
                    verticesIndexDict.Add(face.v1, index++);

                    Vector3 pos = verts[face.v1.posIdx];
                    Vector2 uv = uvs[face.v1.uvIdx];
                    Vector3 norm = norms[face.v1.normalIdx];
                    vertices.Add(new StandardVertex(pos, uv, norm));
                }

                if (verticesIndexDict.TryGetValue(face.v2, out int i2))
                {
                    indices.Add(i2);
                    dups++;
                }
                else
                {
                    indices.Add(index);
                    verticesIndexDict.Add(face.v2, index++);
                    Vector3 pos = verts[face.v2.posIdx];
                    Vector2 uv = uvs[face.v2.uvIdx];
                    Vector3 norm = norms[face.v2.normalIdx];
                    vertices.Add(new StandardVertex(pos, uv, norm));
                }

                if (verticesIndexDict.TryGetValue(face.v3, out int i3))
                {
                    indices.Add(i3);
                    dups++;
                }
                else
                {
                    indices.Add(index);
                    verticesIndexDict.Add(face.v3, index++);
                    Vector3 pos = verts[face.v3.posIdx];
                    Vector2 uv = uvs[face.v3.uvIdx];
                    Vector3 norm = norms[face.v3.normalIdx];
                    vertices.Add(new StandardVertex(pos, uv, norm));
                }
            }

            watch.Stop();
            Debug.WriteLine($"Loaded '{filename}' in {watch.ElapsedMilliseconds}ms");

            return new MeshData()
            {
                IndexType = IndexBufferType.UInt32,
                Int32Indices = indices.ToArray(),
                Vertices = vertices.ToArray(),
                AABB = aabb,
            };
        }

        /*
        public static void WriteBinaryMesh(string path, MeshData data)
        {
            using BinaryWriter writer = new BinaryWriter(File.OpenWrite(path));

            writer.Write((int)data.IndexType);
            switch (data.IndexType)
            {
                case IndexBufferType.UInt8:
                    writer.Write(data.Int8Indices!.Length);
                    writer.Write(data.Int8Indices);
                    break;
                case IndexBufferType.UInt16:
                    writer.Write(data.Int16Indices!.Length);
                    for (int i = 0; i < data.Int16Indices.Length; i++)
                    {
                        writer.Write(data.Int16Indices[i]);
                    }
                    break;
                case IndexBufferType.UInt32:
                    writer.Write(data.Int32Indices!.Length);
                    for (int i = 0; i < data.Int32Indices.Length; i++)
                    {
                        writer.Write(data.Int32Indices[i]);
                    }
                    break;
            }

            writer.Write(data.Positions.Length);
            for (int i = 0; i < data.Positions.Length; i++)
            {
                var pos = data.Positions[i];
                writer.Write(pos.X);
                writer.Write(pos.Y);
                writer.Write(pos.Z);
            }

            writer.Write(data.UVs.Length);
            for (int i = 0; i < data.Positions.Length; i++)
            {
                var pos = data.UVs[i];
                writer.Write(pos.X);
                writer.Write(pos.Y);
            }

            writer.Write(data.Normals.Length);
            for (int i = 0; i < data.Positions.Length; i++)
            {
                var pos = data.Normals[i];
                writer.Write(pos.X);
                writer.Write(pos.Y);
                writer.Write(pos.Z);
            }

            writer.Flush();
        }

        public static MeshData ReadBinaryMesh(string path)
        {
            BinaryReader reader = new BinaryReader(File.OpenRead(path));

            int[]?   int32Indices = null;
            short[]? int16Indices = null;
            byte[]?  int8Indices  = null;

            IndexBufferType indexType = (IndexBufferType)reader.ReadInt32();

            int indicesCount = reader.ReadInt32();

            switch (indexType)
            {
                case IndexBufferType.UInt8:
                    int8Indices = new byte[indicesCount];
                    reader.Read(int8Indices, 0, int8Indices.Length);
                    break;
                case IndexBufferType.UInt16:
                    int16Indices = new short[indicesCount];
                    for (int i = 0; i < indicesCount; i++)
                    {
                        int16Indices[i] = reader.ReadInt16();
                    }
                    break;
                case IndexBufferType.UInt32:
                    int32Indices = new int[indicesCount];
                    for (int i = 0; i < indicesCount; i++)
                    {
                        int32Indices[i] = reader.ReadInt32();
                    }
                    break;
            }

            int posCount = reader.ReadInt32();
            Vector3[] positions = new Vector3[posCount];
            for (int i = 0; i < posCount; i++)
            {
                positions[i].X = reader.ReadSingle();
                positions[i].Y = reader.ReadSingle();
                positions[i].Z = reader.ReadSingle();
            }

            int uvCount = reader.ReadInt32();
            Vector2[] uvs = new Vector2[uvCount];
            for (int i = 0; i < uvCount; i++)
            {
                uvs[i].X = reader.ReadSingle();
                uvs[i].Y = reader.ReadSingle();
            }

            int normCount = reader.ReadInt32();
            Vector3[] normals = new Vector3[normCount];
            for (int i = 0; i < normCount; i++)
            {
                normals[i].X = reader.ReadSingle();
                normals[i].Y = reader.ReadSingle();
                normals[i].Z = reader.ReadSingle();
            }

            return new MeshData()
            {
                IndexType = indexType,
                Int8Indices = int8Indices,
                Int16Indices = int16Indices,
                Int32Indices = int32Indices,
                Positions = positions,
                UVs = uvs,
                Normals = normals,
            };
        }
        */
    }
}
