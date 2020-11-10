using System;
using System.Collections.Generic;
using System.Text;

namespace AerialRace.RenderData
{
    enum FramebufferTarget
    {
        Read = 1,
        Draw = 2,
    }

    struct ColorAttachement
    {
        public int Index;
        public Texture ColorTexture;

        public ColorAttachement(int index, Texture colorTexture)
        {
            this.Index = index;
            ColorTexture = colorTexture;
        }
    }

    // FIXME: Add a size field
    class Framebuffer
    {
        public string Name;
        public int Handle;
        public Texture? DepthAttachment;
        public Texture? StencilAttachment;
        public ColorAttachement[]? ColorAttachments;
        // TODO: We could do something with empty frame buffers? (GL 4.3 or ARB_framebuffer_no_attachments)

        public Framebuffer(string name, int handle, Texture? depthAttachment, Texture? stencilAttachment, ColorAttachement[]? colorAttachments)
        {
            Name = name;
            Handle = handle;
            DepthAttachment = depthAttachment;
            StencilAttachment = stencilAttachment;
            ColorAttachments = colorAttachments;
        }
    }
}
