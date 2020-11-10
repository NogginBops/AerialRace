using AerialRace.RenderData;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
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
        //public unsafe void* UBOStruct;

        public MaterialProperties Properties;

        public Material(string name, ShaderPipeline pipeline, ShaderPipeline? depthPipeline)
        {
            Name = name;
            Pipeline = pipeline;
            DepthPipeline = depthPipeline;
            Properties = new MaterialProperties();
        }
    }

    enum PropertyType : int
    {
        Int,

        Float,
        Float2,
        Float3,
        Float4,

        Color,

        Matrix3,
        Matrix4,
    }

    [StructLayout(LayoutKind.Explicit)]
    struct Property
    {
        [FieldOffset(0)] public PropertyType Type;
        [FieldOffset(4)] public int IntValue;
        [FieldOffset(4)] public float FloatValue;
        [FieldOffset(4)] public Vector2 Vector2Value;
        [FieldOffset(4)] public Vector3 Vector3Value;
        [FieldOffset(4)] public Vector4 Vector4Value;
        [FieldOffset(4)] public Color4 ColorValue;
        [FieldOffset(4)] public Matrix3 Matrix3Value;
        [FieldOffset(4)] public Matrix4 Matrix4Value;
    }

    struct TextureProperty
    {
        public string Name;
        public Texture Texture;
        public ISampler? Sampler;

        public TextureProperty(string name, Texture texture, ISampler? sampler)
        {
            Name = name;
            Texture = texture;
            Sampler = sampler;
        }
    }

    class MaterialProperties
    {
        public List<(string Name, Property Prop)> Properties = new List<(string, Property)>();
        public List<TextureProperty> Textures = new List<TextureProperty>();

        public MaterialProperties()
        { }

        public MaterialProperties(MaterialProperties properties)
        {
            Properties = new List<(string, Property)>(properties.Properties);
            Textures = new List<TextureProperty>(properties.Textures);
        }

        public void SetProperty(string name, Property property)
        {
            var index = Properties.FindIndex(sp => sp.Name == name);
            if (index == -1)
            {
                Properties.Add((name, property));
            }
            else
            {
                Properties[index] = (name, property);
            }
        }

        public void SetTexture(string name, Texture texture, Sampler sampler)
        {
            var index = Textures.FindIndex(st => st.Name == name);
            if (index == -1)
            {
                Textures.Add(new TextureProperty(name, texture, sampler));
            }
            else
            {
                Textures[index] = new TextureProperty(name, texture, sampler);
            }
        }

        public void SetTexture(string name, Texture texture)
        {
            var index = Textures.FindIndex(st => st.Name == name);
            if (index == -1)
            {
                Textures.Add(new TextureProperty(name, texture, null));
            }
            else
            {
                Textures[index] = new TextureProperty(name, texture, null);
            }
        }
    }
}
