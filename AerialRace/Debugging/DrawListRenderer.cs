using AerialRace.RenderData;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AerialRace.Debugging
{
    struct DrawListSettings
    {
        public Matrix4 Vp;

        public bool DepthTest;
        public bool DepthWrite;

        public RenderDataUtil.CullMode CullMode;
    }

    static class DrawListRenderer
    {
        public static void RenderDrawList(DrawList list, ref DrawListSettings settings)
        {
            list.UploadData();

            RenderDataUtil.BindIndexBuffer(list.IndexBuffer);

            RenderDataUtil.BindVertexAttribBuffer(0, list.VertexBuffer, 0);
            RenderDataUtil.SetAndEnableVertexAttributes(Debug.DebugAttributes);
            RenderDataUtil.LinkAttributeBuffer(0, 0);
            RenderDataUtil.LinkAttributeBuffer(1, 0);
            RenderDataUtil.LinkAttributeBuffer(2, 0);

            RenderDataUtil.SetDepthTesting(settings.DepthTest);
            RenderDataUtil.SetDepthWrite(settings.DepthWrite);

            RenderDataUtil.SetCullMode(settings.CullMode);

            RenderDataUtil.UniformMatrix4("vp", ShaderStage.Vertex, true, ref settings.Vp);

            int indexBufferOffset = 0;
            foreach (var command in list.Commands)
            {
                switch (command.Command)
                {
                    case DrawCommandType.Points:
                    case DrawCommandType.Lines:
                    case DrawCommandType.LineLoop:
                    case DrawCommandType.LineStrip:
                    case DrawCommandType.Triangles:
                    case DrawCommandType.TriangleStrip:
                    case DrawCommandType.TriangleFan:
                        {
                            RenderDataUtil.BindTexture(0, command.Texture);
                            RenderDataUtil.BindSampler(0, (ISampler?)null);

                            RenderDataUtil.DrawElements((OpenTK.Graphics.OpenGL4.PrimitiveType)command.Command, command.ElementCount, IndexBufferType.UInt32, indexBufferOffset * sizeof(uint));
                            
                            indexBufferOffset += command.ElementCount;
                        }
                        break;
                    case DrawCommandType.SetScissor:
                        // Set the scissor area
                        // FIXME: Figure out what to do for rounding...
                        RenderDataUtil.SetScissor(command.Scissor);
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
