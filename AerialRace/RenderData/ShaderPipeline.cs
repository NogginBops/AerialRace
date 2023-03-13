using System;
using System.Collections.Generic;
using System.Text;

namespace AerialRace.RenderData
{
    [Flags]
    enum ShaderStages : int
    {
        Vertex = 1 << 0,
        Geometry = 1 << 1,
        Fragment = 1 << 2,
    }

    struct UniformLocation
    {
        public ShaderStages Stages;
        public int VertexLocation;
        public int FragmentLocation;

        public UniformLocation(ShaderStages stages, int vertex, int fragment)
        {
            Stages = stages;
            VertexLocation = vertex;
            FragmentLocation = fragment;
        }
    }

    class ShaderPipeline
    {
        public string Name;
        public int Handle;
        public ShaderProgram? VertexProgram;
        public ShaderProgram? GeometryProgram;
        public ShaderProgram? FragmentProgram;

        public ShaderProgram? ComputeProgram;

        public Dictionary<string, UniformLocation> Uniforms;

        public ShaderPipeline(string name, int handle, ShaderProgram? vertexProgram, ShaderProgram? geometryProgram, ShaderProgram? framgmentProgram, ShaderProgram? computeProgram)
        {
            Name = name;
            Handle = handle;
            VertexProgram = vertexProgram;
            GeometryProgram = geometryProgram;
            FragmentProgram = framgmentProgram;
            ComputeProgram = computeProgram;

            Uniforms = new Dictionary<string, UniformLocation>();
        }

        // FIXME: Move this logic out of here
        public void UpdateUniforms()
        {
            if (VertexProgram?.UniformInfo != null)
            {
                foreach (var uniform in VertexProgram.UniformInfo)
                {
                    Uniforms.Add(uniform.Name, new UniformLocation(ShaderStages.Vertex, uniform.Location, -1));
                }
            }

            if (FragmentProgram?.UniformInfo != null)
            {
                foreach (var uniform in FragmentProgram.UniformInfo)
                {
                    if (Uniforms.TryGetValue(uniform.Name, out var uniformVal))
                    {
                        uniformVal.Stages |= ShaderStages.Fragment;
                        uniformVal.FragmentLocation = uniform.Location;
                        Uniforms[uniform.Name] = uniformVal;
                    }
                    else
                    {
                        Uniforms.Add(uniform.Name, new UniformLocation(ShaderStages.Fragment, -1, uniform.Location));
                    }
                }
            }
        }
    }
}
