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

        public static Buffer CenteredUnitQuadPositionsBuffer;
        public static readonly Vector3[] CenteredUnitQuadPositions = new Vector3[]
        {
            new Vector3(-.5f, -.5f, 0f),
            new Vector3(-.5f,  .5f, 0f),
            new Vector3( .5f,  .5f, 0f),
            new Vector3( .5f, -.5f, 0f),
        };

        public static Buffer UnitQuadPositionsBuffer;
        public static readonly Vector3[] UnitQuadPositions = new Vector3[]
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 1f, 0f),
            new Vector3(1f, 1f, 0f),
            new Vector3(1f, 0f, 0f),
        };

        public static Buffer UnitQuadUVsBuffer;
        public static readonly Vector2[] UnitQuadUVs = new Vector2[]
        {
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f),
        };

        public static Buffer UnitQuadNormalsBuffer;
        public static readonly Vector3[] UnitQuadNormals = new Vector3[]
        {
            new Vector3(0f, 0f, -1f),
            new Vector3(0f, 0f, -1f),
            new Vector3(0f, 0f, -1f),
            new Vector3(0f, 0f, -1f),
        };

        public static Buffer UnitQuadDebugColorsBuffer;
        public static readonly Color4[] UnitQuadDebugColors = new Color4[]
        {
            new Color4(0f, 0f, 0f, 1f),
            new Color4(1f, 1f, 1f, 1f),
            new Color4(1f, 0f, 0f, 1f),
            new Color4(0f, 0f, 0f, 1f),
        };

        public static void InitBuffers()
        {
            UnitQuadIndexBuffer = RenderDataUtil.CreateIndexBuffer("Unit Quad Indices", UnitQuadIndices, BufferFlags.None);
            CenteredUnitQuadPositionsBuffer = RenderDataUtil.CreateDataBuffer<Vector3>("Centered Unit Quad Positions", CenteredUnitQuadPositions, BufferFlags.None);
            UnitQuadPositionsBuffer = RenderDataUtil.CreateDataBuffer<Vector3>("Unit Quad Positions", UnitQuadPositions, BufferFlags.None);
            UnitQuadUVsBuffer = RenderDataUtil.CreateDataBuffer<Vector2>("Unit Quad UVs", UnitQuadUVs, BufferFlags.None);
            UnitQuadNormalsBuffer = RenderDataUtil.CreateDataBuffer<Vector3>("Unit Quad Normals", UnitQuadNormals, BufferFlags.None);
            UnitQuadDebugColorsBuffer = RenderDataUtil.CreateDataBuffer<Color4>("Unit Quad Debug Colors", UnitQuadDebugColors, BufferFlags.None);
        }
    }
}
