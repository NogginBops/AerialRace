﻿using AerialRace.Mathematics;
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
        // FIXME: We want to remove this because we don't really know what size we are drawing to.
        // This info should be part of the draw drawlist if we really need this information.
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
            List.AddCommand(PrimitiveType.TriangleStrip, 4, Texture);

            List.AddVertexWithIndex(PixelsToGL(Rect.X, Rect.Y + Rect.Height), UVs.Xy, Color);
            List.AddVertexWithIndex(PixelsToGL(Rect.X, Rect.Y), UVs.Xw, Color);
            List.AddVertexWithIndex(PixelsToGL(Rect.Position + Rect.Size), UVs.Zy, Color);
            List.AddVertexWithIndex(PixelsToGL(Rect.X + Rect.Width, Rect.Y), UVs.Zw, Color);
        }

        public static void Rect(DrawList List, Rect Rect, Vector4 UVs, Texture Texture, Color4 Color1, Color4 Color2, Color4 Color3, Color4 Color4)
        {
            List.AddCommand(PrimitiveType.TriangleStrip, 4, Texture);

            List.AddVertexWithIndex(PixelsToGL(Rect.X, Rect.Y + Rect.Height), UVs.Xy, Color1);
            List.AddVertexWithIndex(PixelsToGL(Rect.X, Rect.Y), UVs.Xw, Color2);
            List.AddVertexWithIndex(PixelsToGL(Rect.Position + Rect.Size), UVs.Zy, Color3);
            List.AddVertexWithIndex(PixelsToGL(Rect.X + Rect.Width, Rect.Y), UVs.Zw, Color4);
        }

        public static void Rect(DrawList List, Vector2 Position, Vector2 Size, Vector4 UVs, Texture Texture, Color4 Color)
        {
            List.AddCommand(PrimitiveType.TriangleStrip, 4, Texture);

            List.AddVertexWithIndex(PixelsToGL(Position.X, Position.Y + Size.Y), UVs.Xy, Color);
            List.AddVertexWithIndex(PixelsToGL(Position.X, Position.Y), UVs.Xw, Color);
            List.AddVertexWithIndex(PixelsToGL(Position + Size), UVs.Zy, Color);
            List.AddVertexWithIndex(PixelsToGL(Position.X + Size.X, Position.Y), UVs.Zw, Color);
        }

        public static void RectOutline(DrawList List, Vector2 Position, Vector2 Size, Vector4 UVs, Texture Texture, Color4 Color)
        {
            List.AddCommand(PrimitiveType.LineStrip, 5, Texture);

            List.AddRelativeIndices(new[] { 0, 1, 2, 3, 0 });

            List.AddVertex(PixelsToGL(Position.X, Position.Y + Size.Y), UVs.Xy, Color);
            List.AddVertex(PixelsToGL(Position.X, Position.Y), UVs.Xw, Color);
            List.AddVertex(PixelsToGL(Position.X + Size.X, Position.Y), UVs.Zw, Color);
            List.AddVertex(PixelsToGL(Position + Size), UVs.Zy, Color);
        }

        public static void OutlineCircle(DrawList list, Vector2 Position, float Radius, Color4 color, int Segments)
        {
            Debug.Assert(Segments >= 3, $"Segments cannot be less than 3. {Segments}");

            list.Prewarm(Segments);

            for (int i = 0; i < Segments; i++)
            {
                float t = i / (float)Segments;

                float x = MathF.Cos(t * 2 * MathF.PI);
                float y = MathF.Sin(t * 2 * MathF.PI);

                Vector2 pos = new Vector2(x, y) * Radius;

                list.AddVertexWithIndex(PixelsToGL(Position + pos), new Vector2(x, y), color);
            }

            list.AddCommand(PrimitiveType.LineLoop, Segments, BuiltIn.WhiteTex);
        }

        public struct Vertex
        {
            public Vector3 Pos;
            public Vector2 UV;
            public Vertex(Vector3 pos, Vector2 uv)
            {
                Pos = pos;
                UV = uv;
            }
        }

        public static readonly Vertex[] BoxVertices = new[]
            {
                // Right face
                new Vertex(new Vector3( 1, -1, -1), new Vector2(1, 0)),
                new Vertex(new Vector3( 1,  1, -1), new Vector2(1, 1)),
                new Vertex(new Vector3( 1, -1,  1), new Vector2(0, 0)),
                new Vertex(new Vector3( 1,  1,  1), new Vector2(0, 1)),
                // Left face
                new Vertex(new Vector3(-1, -1,  1),  new Vector2(1, 0)),
                new Vertex(new Vector3(-1,  1,  1),  new Vector2(1, 1)),
                new Vertex(new Vector3(-1, -1, -1),  new Vector2(0, 0)),
                new Vertex(new Vector3(-1,  1, -1),  new Vector2(0, 1)),
                // Front face
                new Vertex(new Vector3(-1, -1, -1),  new Vector2(1, 0)),
                new Vertex(new Vector3(-1,  1, -1),  new Vector2(1, 1)),
                new Vertex(new Vector3( 1, -1, -1),  new Vector2(0, 0)),
                new Vertex(new Vector3( 1,  1, -1),  new Vector2(0, 1)),
                // Back face
                new Vertex(new Vector3( 1, -1,  1),  new Vector2(1, 0)),
                new Vertex(new Vector3( 1,  1,  1),  new Vector2(1, 1)),
                new Vertex(new Vector3(-1, -1,  1),  new Vector2(0, 0)),
                new Vertex(new Vector3(-1,  1,  1),  new Vector2(0, 1)),
                // Top face
                new Vertex(new Vector3( 1,  1,  1),  new Vector2(1, 0)),
                new Vertex(new Vector3( 1,  1, -1),  new Vector2(1, 1)),
                new Vertex(new Vector3(-1,  1,  1),  new Vector2(0, 0)),
                new Vertex(new Vector3(-1,  1, -1),  new Vector2(0, 1)),
                // Bottom face
                new Vertex(new Vector3(-1, -1,  1),  new Vector2(1, 0)),
                new Vertex(new Vector3(-1, -1, -1),  new Vector2(1, 1)),
                new Vertex(new Vector3( 1, -1,  1),  new Vector2(0, 0)),
                new Vertex(new Vector3( 1, -1, -1),  new Vector2(0, 1)),
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

            list.AddCommand(PrimitiveType.Lines, BoxVertices.Length, BuiltIn.WhiteTex);
        }

        // --------------------------
        // ----  New 3D helpers  ----
        // --------------------------

        public static void RectOutline(DrawList List, Vector3 center, Vector3 U, Vector3 V, Vector4 UVs, Texture Texture, Color4 Color)
        {
            var halfU = U / 2f;
            var halfV = V / 2f;

            List.AddVertexWithIndex(center - halfU - halfV, UVs.Xy, Color);
            List.AddVertexWithIndex(center + halfU - halfV, UVs.Xy, Color);
            List.AddVertexWithIndex(center + halfU + halfV, UVs.Xy, Color);
            List.AddVertexWithIndex(center - halfU + halfV, UVs.Xy, Color);

            List.AddCommand(PrimitiveType.LineLoop, 4, Texture);
        }

        public static void Cone(DrawList list, Vector3 @base, float radius, float height, Quaternion orientation, int segments, Color4 color)
        {
            Debug.Assert(segments >= 3, $"A cone cannot have less than 3 segments. {segments}");

            list.PrewarmVertices(segments + 2);
            list.PrewarmIndices(segments * 6);

            var dir = orientation * - Vector3.UnitZ;

            list.AddVertex(@base, new Vector2(0, 0), color);
            int baseVert = list.TakeIndexOfLastVertex();

            list.AddVertex(@base + dir, new Vector2(1, 0), color);
            int topVert = list.TakeIndexOfLastVertex();

            int baseIndex = topVert + 1;

            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)(segments - 1);

                float x = MathF.Cos(t * 2 * MathF.PI) * radius;
                float y = MathF.Sin(t * 2 * MathF.PI) * radius;
                Vector3 pos = @base + (orientation * new Vector3(x, y, 0));
                list.AddVertex(pos, (0, 1), color);

                // This is one of the triangles of the base circle
                // It goes from the circle center, to the current pos, and ends a the next pos
                // The % is so that we loop back to 0 when we get to the end.
                list.AddIndex(baseVert);
                list.AddIndex(baseIndex + ((i + 1) % segments));
                list.AddIndex(baseIndex + i);

                // Do the same for the top vertex, but in reverse winding order
                list.AddIndex(topVert);
                list.AddIndex(baseIndex + i);
                list.AddIndex(baseIndex + ((i + 1) % segments));
            }

            list.AddCommand(PrimitiveType.Triangles, segments * 3 * 2, BuiltIn.WhiteTex);
        }

        public static (Vector3, Vector3) GetAny2Perp(Vector3 v)
        {
            // Choose a direction, if that direction is too close to the direction of v
            // we choose another direction
            // We migth want to use random here do really drive home that you get *any*
            // perpendicular vector
            Vector3 v2 = Vector3.UnitY;
            if (MathF.Abs(Vector3.Dot(v, v2)) > 0.99f)
                v2 = Vector3.UnitX;

            var v3 = Vector3.Cross(v, v2).Normalized();
            var v2n = Vector3.Cross(v, v3).Normalized();

            return (v3, v2n);
        }

        public static void Cone(DrawList list, Vector3 tip, float radius, float height, in Vector3 direction, int segments, Color4 color)
        {
            Debug.Assert(segments >= 3, $"A cone cannot have less than 3 segments. {segments}");

            list.PrewarmVertices(segments + 2);
            list.PrewarmIndices(segments * 6);

            // Reverse direction and multiply by height to get the vector from the tip to the base.
            var dir = -direction * height;

            var @base = tip + dir;
            list.AddVertex(@base, new Vector2(0, 0), color);
            int baseVert = list.TakeIndexOfLastVertex();

            list.AddVertex(tip, new Vector2(1, 0), color);
            int topVert = list.TakeIndexOfLastVertex();

            // We might want to fully defined rotation in the future
            var (v1, v2) = GetAny2Perp(direction);

            int baseIndex = topVert + 1;
            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)(segments - 1);
                
                float x = MathF.Cos(t * 2 * MathF.PI) * radius;
                float y = MathF.Sin(t * 2 * MathF.PI) * radius;
                Vector3 pos = @base + (x * v1) + (y * v2);
                list.AddVertex(pos, (0, 1), color);

                // This is one of the triangles of the base circle
                // It goes from the circle center, to the current pos, and ends a the next pos
                // The % is so that we loop back to 0 when we get to the end.
                list.AddIndex(baseVert);
                list.AddIndex(baseIndex + ((i + 1) % segments));
                list.AddIndex(baseIndex + i);

                // Do the same for the top vertex, but in reverse winding order
                list.AddIndex(topVert);
                list.AddIndex(baseIndex + i);
                list.AddIndex(baseIndex + ((i + 1) % segments));
            }

            list.AddCommand(PrimitiveType.Triangles, segments * 3 * 2, BuiltIn.WhiteTex);
        }

        public static void Cylinder(DrawList list, Cylinder cylinder, int segments, Color4 color)
        {
            Debug.Assert(segments >= 3, $"A cylinder cannot have less than 3 segments. {segments}");

            list.AddVertex(cylinder.A, new Vector2(0, 0), color);
            int AVert = list.TakeIndexOfLastVertex();

            list.AddVertex(cylinder.B, new Vector2(1, 0), color);
            int BVert = list.TakeIndexOfLastVertex();

            // We might want to fully defined rotation in the future
            var v0 = (cylinder.A - cylinder.B).Normalized();
            var (v1, v2) = GetAny2Perp(v0);

            int ABase = AVert + 2;
            int BBase = BVert + 2;
            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)(segments - 1);

                float x = MathF.Cos(t * 2 * MathF.PI) * cylinder.Radius;
                float y = MathF.Sin(t * 2 * MathF.PI) * cylinder.Radius;
                var vec = (x * v1) + (y * v2);

                Vector3 Apos = cylinder.A + vec;
                list.AddVertex(Apos, (0, 1), color);
                var ACurr = list.TakeIndexOfLastVertex();

                Vector3 Bpos = cylinder.B + vec;
                list.AddVertex(Bpos, (0, 1), color);
                var BCurr = list.TakeIndexOfLastVertex();

                var AOffset = (ACurr - ABase) / 2;
                var ANext = ABase + ((AOffset + 1) % segments) * 2;

                var BOffset = (BCurr - BBase) / 2;
                var BNext = BBase + ((BOffset + 1) % segments) * 2;

                // This is one of the triangles of the A circle
                // It goes from the circle center, to the current pos, and ends a the next pos
                // The % is so that we loop back to 0 when we get to the end.
                list.AddIndex(AVert);
                list.AddIndex(ACurr);
                list.AddIndex(ANext);

                list.AddIndex(BVert);
                list.AddIndex(BNext);
                list.AddIndex(BCurr);
                
                list.AddIndex(ACurr);
                list.AddIndex(BNext);
                list.AddIndex(ANext);
                
                list.AddIndex(ACurr);
                list.AddIndex(BCurr);
                list.AddIndex(BNext);
            }

            list.AddCommand(PrimitiveType.Triangles, segments * 3 * 4, BuiltIn.WhiteTex);
        }
    }
}
