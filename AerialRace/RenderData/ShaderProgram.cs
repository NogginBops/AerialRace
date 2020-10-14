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

    // This is a opengl program. 
    // Because we are using separable shaders this also works as a opengl shader.
    class ShaderProgram
    {
        public string Name;
        public int Handle;
        public ShaderStage Stage;

        public ShaderProgram(string name, int handle, ShaderStage stage)
        {
            Name = name;
            Handle = handle;
            Stage = stage;
        }
    }
}
