using AerialRace.RenderData;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization.Formatters;
using System.Text;

namespace AerialRace.Loading
{
    static class MeshLoader
    {
        public struct Face
        {
            public int vertex1, vertex2, vertex3;
            public int uv1, uv2, uv3;
            public int normal1, normal2, normal3;
        }

        public struct Vertex : IEquatable<Vertex>
        {
            public Vector3 Position;
            public Vector2 Uv;
            public Vector3 Normal;

            public Vertex(Vector3 position, Vector2 uv, Vector3 normal)
            {
                Position = position;
                Uv = uv;
                Normal = normal;
            }

            public override bool Equals(object? obj)
            {
                return obj is Vertex vertex && Equals(vertex);
            }

            public bool Equals(Vertex other)
            {
                return Position.Equals(other.Position) &&
                       Uv.Equals(other.Uv) &&
                       Normal.Equals(other.Normal);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Position, Uv, Normal);
            }

            public static bool operator ==(Vertex left, Vertex right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Vertex left, Vertex right)
            {
                return !(left == right);
            }
        }

        public struct MeshData
        {
            public IndexBufferType IndexType;
            public int[]? Int32Indices;
            public short[]? Int16Indices;
            public byte[]? Int8Indices;
            public Vector3[] Positions;
            public Vector2[] UVs;
            public Vector3[] Normals;
        }

        public static MeshData LoadObjMesh(string filename)
        {
            // TODO: Figure out if reading lines or reading the whole file is faster for this!
            string[] lines = File.ReadAllLines(filename);

            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<Vector3> norms = new List<Vector3>();

            List<Face> faces = new List<Face>();
            List<Vertex> vertices = new List<Vertex>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrEmpty(line)) continue;
                //string[] tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (line.StartsWithFast("v "))
                {
                    int index1 = line.IndexOf(' ') + 1;
                    int index2 = line.IndexOf(' ', index1) + 1;
                    int index3 = line.IndexOf(' ', index2) + 1;

                    float f1 = Util.ParseFloatFast(line, index1, (index2 - 1) - index1);
                    float f2 = Util.ParseFloatFast(line, index2, (index3 - 1) - index2);
                    float f3 = Util.ParseFloatFast(line, index3, line.Length - index3);

                    verts.Add(new Vector3(f1, f2, f3));
                }
                else if (line.StartsWithFast("vt "))
                {
                    int index1 = line.IndexOf(' ') + 1;
                    int index2 = line.IndexOf(' ', index1) + 1;

                    float u = Util.ParseFloatFast(line, index1, (index2 - 1) - index1);
                    float v = Util.ParseFloatFast(line, index2, line.Length - index2);

                    uvs.Add(new Vector2(u, v));
                }
                else if (line.StartsWithFast("vn "))
                {
                    int index1 = line.IndexOf(' ') + 1;
                    int index2 = line.IndexOf(' ', index1) + 1;
                    int index3 = line.IndexOf(' ', index2) + 1;

                    float n1 = Util.ParseFloatFast(line, index1, (index2 - 1) - index1);
                    float n2 = Util.ParseFloatFast(line, index2, (index3 - 1) - index2);
                    float n3 = Util.ParseFloatFast(line, index3, line.Length - index3);

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

                    f.vertex1 = Util.ParseIntFast(line, index1, (index11 - 1) - index1) - 1;
                    f.uv1 = Util.ParseIntFast(line, index11, (index12 - 1) - index11) - 1;
                    f.normal1 = Util.ParseIntFast(line, index12, (index2 - 1) - index12) - 1;

                    f.vertex2 = Util.ParseIntFast(line, index2, (index21 - 1) - index2) - 1;
                    f.uv2 = Util.ParseIntFast(line, index21, (index22 - 1) - index21) - 1;
                    f.normal2 = Util.ParseIntFast(line, index22, (index3 - 1) - index22) - 1;

                    f.vertex3 = Util.ParseIntFast(line, index3, (index31 - 1) - index3) - 1;
                    f.uv3 = Util.ParseIntFast(line, index31, (index32 - 1) - index31) - 1;
                    f.normal3 = Util.ParseIntFast(line, index32, line.Length - index32) - 1;

                    faces.Add(f);
                }
                else continue;
            }

            Dictionary<Vertex, int> verticesIndexDict = new Dictionary<Vertex, int>();
            List<int> indices = new List<int>();

            int dups = 0;
            int index = 0;
            foreach (var face in faces)
            {
                Vector3 p1 = verts[face.vertex1];
                Vector3 p2 = verts[face.vertex2];
                Vector3 p3 = verts[face.vertex3];

                Vector2 uv1 = uvs[face.uv1];
                Vector2 uv2 = uvs[face.uv2];
                Vector2 uv3 = uvs[face.uv3];

                Vector3 n1 = norms[face.normal1];
                Vector3 n2 = norms[face.normal2];
                Vector3 n3 = norms[face.normal3];

                Vertex v1 = new Vertex(p1, uv1, n1);
                Vertex v2 = new Vertex(p2, uv2, n2);
                Vertex v3 = new Vertex(p3, uv3, n3);

                if (verticesIndexDict.TryGetValue(v1, out int i1))
                {
                    indices.Add(i1);
                    dups++;
                }
                else
                {
                    indices.Add(index);
                    vertices.Add(v1);
                    verticesIndexDict.Add(v1, index++);
                }

                if (verticesIndexDict.TryGetValue(v2, out int i2))
                {
                    indices.Add(i2);
                    dups++;
                }
                else
                {
                    indices.Add(index);
                    vertices.Add(v2);
                    verticesIndexDict.Add(v2, index++);
                }

                if (verticesIndexDict.TryGetValue(v3, out int i3))
                {
                    indices.Add(i3);
                    dups++;
                }
                else
                {
                    indices.Add(index);
                    vertices.Add(v3);
                    verticesIndexDict.Add(v3, index++);
                }
            }

            Vector3[] positionsArray = new Vector3[vertices.Count];
            Vector2[] uvsArray = new Vector2[vertices.Count];
            Vector3[] normalsArray = new Vector3[vertices.Count];
            for (int i = 0; i < vertices.Count; i++)
            {
                Vertex vert = vertices[i];

                positionsArray[i] = vert.Position;
                uvsArray[i] = vert.Uv;
                normalsArray[i] = vert.Normal;
            }

            return new MeshData()
            {
                IndexType = IndexBufferType.UInt32,
                Int32Indices = indices.ToArray(),
                Positions = positionsArray,
                UVs = uvsArray,
                Normals = normalsArray,
            };
        }

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
    }
}
