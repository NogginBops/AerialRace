using AerialRace.RenderData;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AerialRace.Debugging
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = (3 + 2 + 4) * sizeof(float))]
    struct DrawListVertex
    {
        public static readonly int SizeInBytes = Marshal.SizeOf(typeof(DrawListVertex));

        public Vector3 Position;
        public Vector2 UV;
        public Color4 Color;

        public DrawListVertex(Vector3 position, Vector2 uv, Color4 color)
        {
            Position = position;
            UV = uv;
            Color = color;
        }

        public override string ToString()
        {
            return $"Vertex({Position}, {UV})";
        }
    }

    enum DrawCommandType
    {
        Points = Primitive.Points,
        Lines = Primitive.Lines,
        LineLoop = Primitive.LineLoop,
        LineStrip = Primitive.LineStrip,
        Triangles = Primitive.Triangles,
        TriangleStrip = Primitive.TriangleStrip,
        TriangleFan = Primitive.TriangleFan,
        SetScissor,
    }

    struct DrawCommand
    {
        public DrawCommandType Command;
        public int ElementCount;
        public Texture? Texture;
        // We could change this to be something like a material index instead... and make it
        public Material? Material;
        public Matrix3 Transform;
        public Recti Scissor;

        public DrawCommand(Primitive primitive, int elementCount, Texture? texture = null, Material? material = null)
        {
            Command = (DrawCommandType) primitive;
            ElementCount = elementCount;
            Texture = texture;
            Material = material;
            Transform = default;
            Scissor = default;
        }

        public DrawCommand(Recti scissor)
        {
            Command = DrawCommandType.SetScissor;
            ElementCount = default;
            Texture = default;
            Material = default;
            Transform = default;
            Scissor = scissor;
        }
    }

    class DrawList : IDisposable
    {
        public RefList<DrawCommand> Commands = new RefList<DrawCommand>();
        public RefList<DrawListVertex> Vertices = new RefList<DrawListVertex>();
        public RefList<int> Indicies = new RefList<int>();

        public RenderData.Buffer VertexBuffer;
        public RenderData.IndexBuffer IndexBuffer;

        public DrawList()
        {
            VertexBuffer = RenderDataUtil.CreateDataBuffer<DrawListVertex>("Debug Vertex Data", 1000, BufferFlags.Dynamic);
            IndexBuffer = RenderDataUtil.CreateIndexBuffer<uint>("Debug Index Buffer", 1000, BufferFlags.Dynamic);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UploadData()
        {
            if (Vertices.Count > VertexBuffer.Elements)
            {
                int newSize = VertexBuffer.Elements + (VertexBuffer.Elements / 2);
                if (newSize < Vertices.Count) newSize = Vertices.Count;
                RenderDataUtil.ReallocBuffer(ref VertexBuffer, newSize);
            }

            if (Indicies.Count > IndexBuffer.Elements)
            {
                int newSize = IndexBuffer.Elements + (IndexBuffer.Elements / 2);
                if (newSize < Indicies.Count) newSize = Indicies.Count;
                RenderDataUtil.ReallocBuffer(ref IndexBuffer, newSize);
            }

            GL.NamedBufferSubData(VertexBuffer.Handle, IntPtr.Zero, Vertices.SizeInBytes, ref Vertices.Data[0]);
            GL.NamedBufferSubData(IndexBuffer.Handle, IntPtr.Zero, Indicies.SizeInBytes, ref Indicies.Data[0]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddCommand(Primitive type, int elementCount, Texture texture, Material? material = null)
        {
            Commands.Add(new DrawCommand(type, elementCount, texture, material));
        }

        public void SetScissor(Recti rect)
        {
            Commands.Add(new DrawCommand(rect));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddVertex(Vector2 Pos, Vector2 UV, Color4 Color)
        {
            Vertices.Add(new DrawListVertex(new Vector3(Pos), UV, Color));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddVertex(Vector3 Pos, Vector2 UV, Color4 Color)
        {
            Vertices.Add(new DrawListVertex(Pos, UV, Color));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int AddVertexWithIndex(Vector2 Pos, Vector2 UV, Color4 Color)
        {
            Vertices.Add(new DrawListVertex(new Vector3(Pos), UV, Color));
            return AddIndexOfLastVertex();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int AddVertexWithIndex(Vector3 Pos, Vector2 UV, Color4 Color)
        {
            Vertices.Add(new DrawListVertex(Pos, UV, Color));
            return AddIndexOfLastVertex();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddIndex(int index)
        {
            Indicies.Add(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddIndexRelativeToLastVertex(int relativeIndex)
        {
            Indicies.Add(Vertices.Count - 1 + relativeIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int TakeIndexOfLastVertex()
        {
            return Vertices.Count - 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int AddIndexOfLastVertex()
        {
            Indicies.Add(Vertices.Count - 1);
            return Vertices.Count - 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddNewLinearIndices(int count)
        {
            int @base = Vertices.Count;
            for (int i = 0; i < count; i++)
            {
                Indicies.Add(@base + i);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRelativeIndices(Span<int> relativeIndicies)
        {
            int baseIndex = Vertices.Count;
            for (int i = 0; i < relativeIndicies.Length; i++)
            {
                Indicies.Add(baseIndex + relativeIndicies[i]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PrewarmVertices(int count)
        {
            Vertices.EnsureCapacity(Vertices.Count + count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PrewarmIndices(int count)
        {
            Indicies.EnsureCapacity(Indicies.Count + count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Prewarm(int count)
        {
            PrewarmVertices(count);
            PrewarmIndices(count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            Commands.Clear();
            Vertices.Clear();
            Indicies.Clear();
        }

        public void Dispose()
        {
            RenderDataUtil.DeleteBuffer(ref VertexBuffer!);
            RenderDataUtil.DeleteBuffer(ref IndexBuffer!);
        }
    }
}
