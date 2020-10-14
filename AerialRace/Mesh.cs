using AerialRace.RenderData;
using System;
using System.Collections.Generic;
using System.Text;
using Buffer = AerialRace.RenderData.Buffer;

namespace AerialRace
{
    // Meshes in this game have the attributes:
    // vector3 position
    // vector2 uv1
    // vector3 normal
    // ...

    struct Submesh
    {
        public int StartIndex;
        public int IndexCount;
        public Material Material;
    }

    class Mesh
    {
        public string Name;

        public IndexBuffer? Indices;

        public Buffer? Positions;
        public Buffer? UVs;
        public Buffer? Normals;
        public Buffer? VertexColors;

        public Submesh[]? Submeshes;

        public Mesh(string name, IndexBuffer? indices, Buffer? positions, Buffer? uvs, Buffer? normals, Submesh[]? submeshes)
        {
            Name = name;
            Indices = indices;
            Positions = positions;
            UVs = uvs;
            Normals = normals;
            VertexColors = null;
            Submeshes = submeshes;
        }
    }
}
