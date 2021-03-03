using System;
using System.Collections.Generic;
using System.Text;

namespace AerialRace.RenderData
{
    enum FramebufferTarget
    {
        Read = 1,
        Draw = 2,
        ReadDraw = 3,
    }

    // FIXME: Maybe we can elegantly combine ColorAttachement and FramebufferAttachmentTexture
    // into a sinlge struct.
    struct ColorAttachement
    {
        public int Index;
        public int MipLevel;
        public Texture ColorTexture;

        public ColorAttachement(int index, int mipLevel, Texture colorTexture)
        {
            Index = index;
            MipLevel = mipLevel;
            ColorTexture = colorTexture;
        }
    }

    struct FramebufferAttachmentTexture
    {
        public Texture Texture;
        public int MipLevel;
        public int Layer;

        public FramebufferAttachmentTexture(Texture texture, int mipLevel, int layer)
        {
            Texture = texture;
            MipLevel = mipLevel;
            Layer = layer;
        }
    }

    // FIXME: Add a size field
    class Framebuffer
    {
        public string Name;
        public int Handle;
        public FramebufferAttachmentTexture? DepthAttachment;
        public FramebufferAttachmentTexture? StencilAttachment;
        //public Texture? DepthAttachment;
        //public Texture? StencilAttachment;
        public ColorAttachement[]? ColorAttachments;
        // TODO: We could do something with empty frame buffers? (GL 4.3 or ARB_framebuffer_no_attachments)

        public Framebuffer(string name, int handle, FramebufferAttachmentTexture? depthAttachment, FramebufferAttachmentTexture? stencilAttachment, ColorAttachement[]? colorAttachments)
        {
            Name = name;
            Handle = handle;
            DepthAttachment = depthAttachment;
            StencilAttachment = stencilAttachment;
            ColorAttachments = colorAttachments;
        }
    }
}
