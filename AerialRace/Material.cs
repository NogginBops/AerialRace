using AerialRace.RenderData;
using System;
using System.Collections.Generic;
using System.Text;

namespace AerialRace
{
    // We will use uniform buffer objects for uniforms
    class Material
    {
        public string Name;

        public ShaderPipeline Pipeline;
        public ShaderPipeline? DepthPipeline;

        // FIXME: We want to be able to cast this into a struct
        // or we want to do some shader introspection stuff to
        // automagically generate some thing...
        // Or we instead generate a uniform preable from a struct
        public unsafe void* UBOStruct;

        public Material(string name, ShaderPipeline pipeline, ShaderPipeline? depthPipeline)
        {
            Name = name;
            Pipeline = pipeline;
            DepthPipeline = depthPipeline;
        }
    }
}
