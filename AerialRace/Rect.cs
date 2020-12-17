using AerialRace.Debugging;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AerialRace
{
    struct Rect : IEquatable<Rect>, IShape
    {
        public static readonly Rect Empty = new Rect(0, 0, 0, 0);
        public static readonly Rect Unit  = new Rect(0, 0, 1, 1);

        public float X, Y;
        public float Width, Height;

        public Vector2 Position
        {
            get => new Vector2(X, Y);
            set => (X, Y) = value;
        }

        public Vector2 Size {
            get => new Vector2(Width, Height);
            set => (Width, Height) = value;
        }

        public Vector2 TopLeft => new Vector2(X, Y);
        public Vector2 TopRight => new Vector2(X + Width, Y);
        public Vector2 BottomLeft => new Vector2(X, Y + Height);
        public Vector2 BottomRight => new Vector2(X + Width, Y + Height);

        public Rect(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public Rect(Vector2 position, Vector2 size) : this()
        {
            Position = position;
            Size = size;
        }

        public bool Contains(Vector2 point)
        {
            return (point.X >= X && point.X <= X + Width) && (point.Y >= Y && point.Y <= Y + Height);
        }

        public Rect Pad(float v) => new Rect(X - v, Y - v, Width + (v * 2), Height + (v * 2));

        public override bool Equals(object? obj)
        {
            return obj is Rect rect && Equals(rect);
        }

        public static Rect Inflate(Rect r1, Rect r2)
        {
            float minX = Math.Min(r1.X, r2.X);
            float minY = Math.Min(r1.Y, r2.Y);
            float maxX = Math.Max(r1.X + r1.Width, r2.X + r2.Width);
            float maxY = Math.Max(r1.Y + r1.Height, r2.Y + r2.Height);
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        public bool Equals(Rect other)
        {
            return X == other.X &&
                   Y == other.Y &&
                   Width == other.Width &&
                   Height == other.Height;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Width, Height);
        }

        public void DebugDraw(DrawList list, Color4 Color)
        {
            DebugHelper.RectOutline(list, Position, Size, Debug.FullUV, BuiltIn.WhiteTex, Color);
        }

        public static bool operator ==(Rect left, Rect right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Rect left, Rect right)
        {
            return !(left == right);
        }
    }

    struct Recti : IEquatable<Recti>, IShape
    {
        public static readonly Recti Empty = new Recti(0, 0, 0, 0);
        public static readonly Recti Unit = new Recti(0, 0, 1, 1);

        public int X, Y;
        public int Width, Height;

        public Vector2i Position
        {
            get => new Vector2i(X, Y);
            set => (X, Y) = value;
        }

        public Vector2i Size
        {
            get => new Vector2i(Width, Height);
            set => (Width, Height) = value;
        }

        public Vector2i TopLeft => new Vector2i(X, Y);
        public Vector2i TopRight => new Vector2i(X + Width, Y);
        public Vector2i BottomLeft => new Vector2i(X, Y + Height);
        public Vector2i BottomRight => new Vector2i(X + Width, Y + Height);

        public Recti(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public Recti(Vector2i position, Vector2i size)
        {
            (X, Y) = position;
            (Width, Height) = size;
        }

        public bool Contains(Vector2 point)
        {
            return (point.X >= X && point.X <= X + Width) &&
                   (point.Y >= Y && point.Y <= Y + Height);
        }

        public override bool Equals(object? obj)
        {
            return obj is Rect rect && Equals(rect);
        }

        public static Recti Inflate(Recti r1, Recti r2)
        {
            int minX = Math.Min(r1.X, r2.X);
            int minY = Math.Min(r1.Y, r2.Y);
            int maxX = Math.Max(r1.X + r1.Width, r2.X + r2.Width);
            int maxY = Math.Max(r1.Y + r1.Height, r2.Y + r2.Height);
            return new Recti(minX, minY, maxX - minX, maxY - minY);
        }

        public bool Equals(Recti other)
        {
            return X == other.X &&
                   Y == other.Y &&
                   Width == other.Width &&
                   Height == other.Height;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Width, Height);
        }

        public void DebugDraw(DrawList list, Color4 Color)
        {
            DebugHelper.RectOutline(list, Position, Size, Debug.FullUV, BuiltIn.WhiteTex, Color);
        }

        public static bool operator ==(Recti left, Recti right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Recti left, Recti right)
        {
            return !(left == right);
        }
    }
}
