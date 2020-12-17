using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AerialRace
{
    static class Screen
    {
        public delegate void OnScreenResize(Vector2i Size);
        
        public static event OnScreenResize? OnResize;

        public static Vector2i Size;

        public static int Width => Size.X;
        public static int Height => Size.Y;

        public static void UpdateScreenSize(Vector2i size)
        {
            Size = size;

            OnResize?.Invoke(size);
        }
    }
}
