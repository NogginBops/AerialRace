using OpenTK.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace AerialRace.RenderData
{
    class ShaderPipeline
    {
        public string Name;
        public ProgramPipelineHandle Handle;
        public ShaderProgram? VertexProgram;
        public ShaderProgram? GeometryProgram;
        public ShaderProgram? FramgmentProgram;

        public ShaderPipeline(string name, ProgramPipelineHandle handle, ShaderProgram? vertexProgram, ShaderProgram? geometryProgram, ShaderProgram? framgmentProgram)
        {
            Name = name;
            Handle = handle;
            VertexProgram = vertexProgram;
            GeometryProgram = geometryProgram;
            FramgmentProgram = framgmentProgram;
        }
    }
}
