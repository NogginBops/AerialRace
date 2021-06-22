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

        public CullMode CullMode;

        public RenderPassMetrics Metrics;
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

            settings.Metrics.Vertices += list.Vertices.Count;

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

                            settings.Metrics.DrawCalls++;
                            if (command.Command == DrawCommandType.Triangles)
                                settings.Metrics.Triangles += command.ElementCount / 3;
                            else if (command.Command == DrawCommandType.TriangleStrip)
                                settings.Metrics.Triangles += command.ElementCount - 2;
                            else if (command.Command == DrawCommandType.TriangleFan)
                                settings.Metrics.Triangles += command.ElementCount - 2;

                            RenderDataUtil.DrawElements((Primitive)command.Command, command.ElementCount, IndexBufferType.UInt32, indexBufferOffset * sizeof(uint));
                            
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
