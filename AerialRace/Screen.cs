using AerialRace.Debugging;
using AerialRace.RenderData;
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

        public static List<Framebuffer> TrackedFramebuffers = new List<Framebuffer>();
        public static HashSet<Framebuffer> FramebuffersToResize = new HashSet<Framebuffer>();

        public static Vector2i Size;

        public static int Width => Size.X;
        public static int Height => Size.Y;

        public static float Aspect => Size.X / (float)Size.Y;

        public static bool ResizedThisFrame;

        public static void UpdateScreenSize(Vector2i size)
        {
            Size = size;
            ResizedThisFrame = true;
            OnResize?.Invoke(size);
        }

        public static void NewFrame()
        {
            if (ResizedThisFrame)
            {
                FramebuffersToResize.UnionWith(TrackedFramebuffers);
                ResizedThisFrame = false;
            }
        }

        public static void RegisterFramebuffer(Framebuffer buffer)
        {
            Debug.Assert(TrackedFramebuffers.Contains(buffer) == false, $"Adding already added buffer to be tracked!");
            TrackedFramebuffers.Add(buffer);
        }

        public static void UnRegisterFramebuffer(Framebuffer buffer)
        {
            var removed = TrackedFramebuffers.Remove(buffer);
            Debug.Assert(removed, $"Failed to remove framebuffer!");
        }

        public static bool ShouldResize(Framebuffer buffer)
        {
            Debug.Assert(TrackedFramebuffers.Contains(buffer), $"The framebuffer {buffer.Name} is not tracked!");

            return FramebuffersToResize.Contains(buffer);
        }

        public static void MarkResized(Framebuffer buffer)
        {
            var removed = FramebuffersToResize.Remove(buffer);
            Debug.Assert(removed, $"Failed to mark framebuffer as resized! Maybe it's already resized?");
        }

        public static void ResizeToScreenSizeIfNecessary(Framebuffer buffer)
        {
            if (ShouldResize(buffer))
            {
                RenderDataUtil.ResizeFramebuffer(buffer, Size);

                MarkResized(buffer);
            }
        }

        public static void ResizeToScreenSizeIfNecessary(Texture texture)
        {
            // FIXME: Keep track that we have resized this texture this frame??
            if (ResizedThisFrame)
            {
                RenderDataUtil.CreateResizedTexture2D(texture, Size);
            }
        }
    }
}
