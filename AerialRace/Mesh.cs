using AerialRace.Loading;
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
        // We should get our materials from the renderer
        //public Material Material;
    }

    struct AttributeBufferLink
    {
        public int AttribIndex;
        public int BufferIndex;

        public AttributeBufferLink(int attribIndex, int bufferIndex)
        {
            AttribIndex = attribIndex;
            BufferIndex = bufferIndex;
        }
    }

    class Mesh
    {
        public string Name;

        public IndexBuffer? Indices;

        public Buffer[] DataBuffers;
        public AttributeSpecification[] Attributes;

        public AttributeBufferLink[] AttributeBufferLinks;

        public Submesh[]? Submeshes;

        public Mesh(string name, IndexBuffer? indices, Submesh[]? submeshes)
        {
            Name = name;
            Indices = indices;
            DataBuffers = new Buffer[0];
            Attributes = new AttributeSpecification[0];
            AttributeBufferLinks = new AttributeBufferLink[0];
            Submeshes = submeshes;
        }

        public Mesh(string name, IndexBuffer? indices, Buffer standardVertex, Submesh[]? submeshes)
        {
            Name = name;
            Indices = indices;
            DataBuffers = new Buffer[1] { standardVertex };
            Attributes = new AttributeSpecification[0];
            AttributeBufferLinks = new AttributeBufferLink[0];
            Submeshes = submeshes;
        }

        public Mesh(string name, IndexBuffer? indices, Buffer standardVertex, Span<AttributeSpecification> attribs)
        {
            Name = name;
            Indices = indices;
            DataBuffers = new Buffer[1] { standardVertex };
            Attributes = attribs.ToArray();
            AttributeBufferLinks = new AttributeBufferLink[Attributes.Length];
            for (int i = 0; i < AttributeBufferLinks.Length; i++)
            {
                AttributeBufferLinks[i] = new AttributeBufferLink(i, 0);
            }
            Submeshes = null;
        }

        public int AddBuffer(Buffer buffer)
        {
            int index = DataBuffers.Length;
            Array.Resize(ref DataBuffers, DataBuffers.Length + 1);
            DataBuffers[index] = buffer;
            return index;
        }

        public int AddAttribute(AttributeSpecification attrib)
        {
            int index = Attributes.Length;
            Array.Resize(ref Attributes, Attributes.Length + 1);
            Attributes[index] = attrib;
            return index;
        }

        public int AddAttributes(Span<AttributeSpecification> attrib)
        {
            int index = Attributes.Length;
            Array.Resize(ref Attributes, Attributes.Length + attrib.Length);
            for (int i = 0; i < attrib.Length; i++)
            {
                Attributes[index + i] = attrib[i];
            }

            return index;
        }

        public void AddLink(int attribIndex, int bufferIndex)
        {
            int index = AttributeBufferLinks.Length;
            Array.Resize(ref AttributeBufferLinks, AttributeBufferLinks.Length + 1);
            AttributeBufferLinks[index] = new AttributeBufferLink(attribIndex, bufferIndex);
        }
    }
}
