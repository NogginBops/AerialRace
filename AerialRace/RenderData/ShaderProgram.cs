using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Text;

namespace AerialRace.RenderData
{
    enum ShaderStage : int
    {
        Vertex = 1,
        Geometry = 4,
        Fragment = 5,

        // GLVER 4.3
        Compute = 6,
    }

    struct UniformFieldInfo
    {
        public int Location;
        public string Name;
        public int Size;
        // FIXME: Leaking GL enum
        public ActiveUniformType Type;
    }

    // FIXME: We could add layout data here but I think we will only use std140
    struct UniformBlockInfo
    {
        public string BlockName;
        public UniformFieldInfo[] BlockUniforms;
    }

    // This is a opengl program. 
    // Because we are using separable shaders this also works as a opengl shader.
    class ShaderProgram
    {
        public string Name;
        public int Handle;
        public ShaderStage Stage;
        public Dictionary<string, int> UniformLocations;
        public Dictionary<string, int> UniformBlockLocations;
        public UniformFieldInfo[]? UniformInfo;
        public UniformBlockInfo[]? UniformBlockInfo;

        public ShaderProgram(string name, int handle, ShaderStage stage, Dictionary<string, int> uniformLocations, Dictionary<string, int> uniformBlockLocations, UniformFieldInfo[]? uniformInfo, UniformBlockInfo[]? uniformBlockInfo)
        {
            Name = name;
            Handle = handle;
            Stage = stage;
            UniformLocations = uniformLocations;
            UniformBlockLocations = uniformBlockLocations;
            UniformInfo = uniformInfo;
            UniformBlockInfo = uniformBlockInfo;
        }
    }
}
