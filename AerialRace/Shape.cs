using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AerialRace.Debugging;

namespace AerialRace.Debugging
{
    interface IShape
    {
        bool Contains(Vector2 point);

        void DebugDraw(DrawList list, Color4<Rgba> Color);
    }

    struct Triangle : IShape
    {
        public Vector2 A;
        public Vector2 B;
        public Vector2 C;

        public static bool PointInTriangle(Vector2 p, Vector2 p0, Vector2 p1, Vector2 p2)
        {
            // https://stackoverflow.com/questions/2049582/how-to-determine-if-a-point-is-in-a-2d-triangle

            var s = p0.Y * p2.X - p0.X * p2.Y + (p2.Y - p0.Y) * p.X + (p0.X - p2.X) * p.Y;
            var t = p0.X * p1.Y - p0.Y * p1.X + (p0.Y - p1.Y) * p.X + (p1.X - p0.X) * p.Y;

            if ((s < 0) != (t < 0))
                return false;

            var A = -p1.Y * p2.X + p0.Y * (p2.X - p1.X) + p0.X * (p1.Y - p2.Y) + p1.X * p2.Y;

            return A < 0 ? (s <= 0 && s + t >= A) : (s >= 0 && s + t <= A);
        }

        public bool Contains(Vector2 point)
        {
            return PointInTriangle(point, A, B, C);
        }

        public void DebugDraw(DrawList list, Color4<Rgba> color)
        {
            // FIXME! UV coordinates??
            list.Prewarm(3);
            list.AddVertexWithIndex(DebugHelper.PixelsToGL(A), new Vector2(0f, 0f), color);
            list.AddVertexWithIndex(DebugHelper.PixelsToGL(B), new Vector2(0f, 0f), color);
            list.AddVertexWithIndex(DebugHelper.PixelsToGL(C), new Vector2(0f, 0f), color);
            list.AddCommand(PrimitiveType.LineLoop, 3, BuiltIn.WhiteTex);
        }
    }

    struct Annulus : IShape
    {
        public Vector2 Center;
        public float InnerRadius;
        public float OuterRadius;

        public bool Contains(Vector2 point)
        {
            float dist = (point - Center).LengthSquared;
            return InnerRadius * InnerRadius < dist && dist < OuterRadius * OuterRadius;
        }

        public void DebugDraw(DrawList list, Color4<Rgba> color)
        {
            DebugHelper.OutlineCircle(list, Center, InnerRadius, color, 20);
            DebugHelper.OutlineCircle(list, Center, OuterRadius, color, 20);
        }
    }
}
