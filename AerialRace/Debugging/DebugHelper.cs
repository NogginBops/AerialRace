using AerialRace.RenderData;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace AerialRace.Debugging
{
    static class DebugHelper
    {
        public static Vector2 PixelsToGL(Vector2 pixels)
        {
            return new Vector2(
                ((pixels.X / Debug.Width) - 0.5f) * 2f,
                -((pixels.Y / Debug.Height) - 0.5f) * 2f);
        }

        public static Vector2 PixelsToGL(float x, float y)
        {
            return PixelsToGL(new Vector2(x, y));
        }

        public static void Rect(DrawList List, Rect Rect, Vector4 UVs, Texture Texture, Color4 Color)
        {
            List.AddCommand(PrimitiveType.TriangleStrip, 4, Texture.Handle);

            List.AddVertexWithIndex(PixelsToGL(Rect.X, Rect.Y + Rect.Height), UVs.Xy, Color);
            List.AddVertexWithIndex(PixelsToGL(Rect.X, Rect.Y), UVs.Xw, Color);
            List.AddVertexWithIndex(PixelsToGL(Rect.Position + Rect.Size), UVs.Zy, Color);
            List.AddVertexWithIndex(PixelsToGL(Rect.X + Rect.Width, Rect.Y), UVs.Zw, Color);
        }

        public static void Rect(DrawList List, Rect Rect, Vector4 UVs, Texture Texture, Color4 Color1, Color4 Color2, Color4 Color3, Color4 Color4)
        {
            List.AddCommand(PrimitiveType.TriangleStrip, 4, Texture.Handle);

            List.AddVertexWithIndex(PixelsToGL(Rect.X, Rect.Y + Rect.Height), UVs.Xy, Color1);
            List.AddVertexWithIndex(PixelsToGL(Rect.X, Rect.Y), UVs.Xw, Color2);
            List.AddVertexWithIndex(PixelsToGL(Rect.Position + Rect.Size), UVs.Zy, Color3);
            List.AddVertexWithIndex(PixelsToGL(Rect.X + Rect.Width, Rect.Y), UVs.Zw, Color4);
        }

        public static void Rect(DrawList List, Vector2 Position, Vector2 Size, Vector4 UVs, Texture Texture, Color4 Color)
        {
            List.AddCommand(PrimitiveType.TriangleStrip, 4, Texture.Handle);

            List.AddVertexWithIndex(PixelsToGL(Position.X, Position.Y + Size.Y), UVs.Xy, Color);
            List.AddVertexWithIndex(PixelsToGL(Position.X, Position.Y), UVs.Xw, Color);
            List.AddVertexWithIndex(PixelsToGL(Position + Size), UVs.Zy, Color);
            List.AddVertexWithIndex(PixelsToGL(Position.X + Size.X, Position.Y), UVs.Zw, Color);
        }

        public static void RectOutline(DrawList List, Vector2 Position, Vector2 Size, Vector4 UVs, Texture Texture, Color4 Color)
        {
            List.AddCommand(PrimitiveType.LineStrip, 5, Texture.Handle);

            List.AddRelativeIndecies(new[] { 0, 1, 2, 3, 0 });

            List.AddVertex(PixelsToGL(Position.X, Position.Y + Size.Y), UVs.Xy, Color);
            List.AddVertex(PixelsToGL(Position.X, Position.Y), UVs.Xw, Color);
            List.AddVertex(PixelsToGL(Position.X + Size.X, Position.Y), UVs.Zw, Color);
            List.AddVertex(PixelsToGL(Position + Size), UVs.Zy, Color);
        }

        public static void OutlineCircle(DrawList list, Vector2 Position, float Radius, Color4 color, int Segments)
        {
            if (Segments <= 2) throw new ArgumentException(nameof(Segments));

            list.Prewarm(Segments);

            for (int i = 0; i < Segments; i++)
            {
                float t = i / (float)Segments;

                float x = MathF.Cos(t * 2 * MathF.PI);
                float y = MathF.Sin(t * 2 * MathF.PI);

                Vector2 pos = new Vector2(x, y) * Radius;

                list.AddVertexWithIndex(PixelsToGL(Position + pos), new Vector2(x, y), color);
            }

            list.AddCommand(PrimitiveType.LineLoop, Segments, BuiltIn.WhiteTex.Handle);
        }

        struct Vertex
        {
            public Vector3 Pos;
            public Vector2 UV;
            public Vertex(Vector3 pos, Vector2 uv)
            {
                Pos = pos;
                UV = uv;
            }
        }

        static readonly Vertex[] BoxVertices = new[]
            {
                new Vertex(new Vector3(-1, -1, -1),  new Vector2(0, 1)),
                new Vertex(new Vector3(-1, -1, 1),   new Vector2(1, 1)),
                new Vertex(new Vector3(-1, 1, -1),   new Vector2(0, 0)),
                new Vertex(new Vector3(-1, 1, 1),    new Vector2(1, 0)),

                new Vertex(new Vector3(1, -1, -1),   new Vector2(1, 0)),
                new Vertex(new Vector3(1, 1, -1),    new Vector2(0, 0)),
                new Vertex(new Vector3(1, -1, 1),    new Vector2(1, 1)),
                new Vertex(new Vector3(1, 1, 1),     new Vector2(0, 1)),

                new Vertex(new Vector3(-1, -1, -1),  new Vector2(1, 0)),
                new Vertex(new Vector3(1, -1, -1),   new Vector2(0, 0)),
                new Vertex(new Vector3(-1, -1, 1),   new Vector2(1, 1)),
                new Vertex(new Vector3(1, -1, 1),    new Vector2(0, 1)),

                new Vertex(new Vector3(-1, 1, -1),   new Vector2(1, 0)),
                new Vertex(new Vector3(-1, 1, 1),    new Vector2(0, 0)),
                new Vertex(new Vector3(1, 1, -1),    new Vector2(1, 1)),
                new Vertex(new Vector3(1, 1, 1),     new Vector2(0, 1)),

                new Vertex(new Vector3(-1, -1, -1),  new Vector2(0, 1)),
                new Vertex(new Vector3(-1, 1, -1),   new Vector2(1, 1)),
                new Vertex(new Vector3(1, -1, -1),   new Vector2(0, 0)),
                new Vertex(new Vector3(1, 1, -1),    new Vector2(1, 0)),

                new Vertex(new Vector3(-1, -1, 1),   new Vector2(0, 1)),
                new Vertex(new Vector3(1, -1, 1),    new Vector2(1, 1)),
                new Vertex(new Vector3(-1, 1, 1),    new Vector2(0, 0)),
                new Vertex(new Vector3(1, 1, 1),     new Vector2(1, 0)),
            };

        public static void OutlineBox(DrawList list, Vector3 center, Quaternion orientation, Vector3 halfSize, Color4 color)
        {
            list.Prewarm(BoxVertices.Length);
            /*
            for (int i = 0; i < BoxVertices.Length; i++)
            {
                int @base = i / 4;
                int index = i % 4;

                Vector3 pos = center + (orientation * (BoxVertices[i].Pos * halfSize));
                list.AddVertex(pos, BoxVertices[i].UV, color);

                //list.AddIndexRelativeToLastVertex;
            }*/

            foreach (var v in BoxVertices)
            {
                Vector3 pos = center + (orientation * (v.Pos * halfSize));
                list.AddVertexWithIndex(pos, v.UV, color);
            }

            list.AddCommand(PrimitiveType.Lines, BoxVertices.Length, BuiltIn.WhiteTex.Handle);
        }
    }
}
