using AerialRace.RenderData;
using OpenTK.Mathematics;
using System.Collections.Generic;
using System.Text;

namespace AerialRace.Loading
{
    static class StaticGeometry
    {
        public static IndexBuffer UnitQuadIndexBuffer;
        public static readonly byte[] UnitQuadIndices = new byte[]
        {
            0, 2, 1,
            0, 3, 2,
        };

        public static Buffer CenteredUnitQuadBuffer;
        public static readonly StandardVertex[] CenteredUnitQuad = new StandardVertex[]
        {
            new StandardVertex(new Vector3(-.5f, -.5f, 0f), new Vector2(0f, 0f), new Vector3(0f, 0f, 1f)),
            new StandardVertex(new Vector3(-.5f,  .5f, 0f), new Vector2(0f, 1f), new Vector3(0f, 0f, 1f)),
            new StandardVertex(new Vector3( .5f,  .5f, 0f), new Vector2(1f, 1f), new Vector3(0f, 0f, 1f)),
            new StandardVertex(new Vector3( .5f, -.5f, 0f), new Vector2(1f, 0f), new Vector3(0f, 0f, 1f)),
        };

        public static Buffer UnitQuadBuffer;
        public static readonly StandardVertex[] UnitQuad = new StandardVertex[]
        {
            new StandardVertex(new Vector3(0f, 0f, 0f), new Vector2(0f, 0f), new Vector3(0f, 0f, 1f)),
            new StandardVertex(new Vector3(0f, 1f, 0f), new Vector2(0f, 1f), new Vector3(0f, 0f, 1f)),
            new StandardVertex(new Vector3(1f, 1f, 0f), new Vector2(1f, 1f), new Vector3(0f, 0f, 1f)),
            new StandardVertex(new Vector3(1f, 0f, 0f), new Vector2(1f, 0f), new Vector3(0f, 0f, 1f)),
        };

        public static Buffer UnitQuadDebugColorsBuffer;
        public static readonly Color4[] UnitQuadDebugColors = new Color4[]
        {
            new Color4(0f, 0f, 0f, 1f),
            new Color4(1f, 1f, 1f, 1f),
            new Color4(1f, 0f, 0f, 1f),
            new Color4(0f, 0f, 0f, 1f),
        };

        // FIXME: Make sure this is only called while there is a GL context current
        static StaticGeometry()
        {
            UnitQuadIndexBuffer = RenderDataUtil.CreateIndexBuffer("Unit Quad Indices", UnitQuadIndices, BufferFlags.None);
            CenteredUnitQuadBuffer = RenderDataUtil.CreateDataBuffer<StandardVertex>("Centered Unit Quad", CenteredUnitQuad, BufferFlags.None);
            UnitQuadBuffer = RenderDataUtil.CreateDataBuffer<StandardVertex>("Unit Quad", UnitQuad, BufferFlags.None);
            UnitQuadDebugColorsBuffer = RenderDataUtil.CreateDataBuffer<Color4>("Unit Quad Debug Colors", UnitQuadDebugColors, BufferFlags.None);
        }

        public static void Init() { }
    }
}
