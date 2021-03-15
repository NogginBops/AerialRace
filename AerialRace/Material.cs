using AerialRace.RenderData;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static AerialRace.RenderData.RenderDataUtil;

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
        Invalid,

        Bool,
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
        [FieldOffset(0)] public string Name;
        [FieldOffset(8)] public PropertyType Type;
        [FieldOffset(12)] public bool BoolValue;
        [FieldOffset(12)] public int IntValue;
        [FieldOffset(12)] public float FloatValue;
        [FieldOffset(12)] public Vector2 Vector2Value;
        [FieldOffset(12)] public Vector3 Vector3Value;
        [FieldOffset(12)] public Vector4 Vector4Value;
        [FieldOffset(12)] public Color4 ColorValue;
        [FieldOffset(12)] public Matrix3 Matrix3Value;
        [FieldOffset(12)] public Matrix4 Matrix4Value;

        public unsafe Property(string name, bool b)
        {
            this = default;
            Name = name;
            Type = PropertyType.Bool;
            BoolValue = b;
        }

        public unsafe Property(string name, int i)
        {
            this = default;
            Name = name;
            Type = PropertyType.Int;
            IntValue = i;
        }

        public unsafe Property(string name, float f)
        {
            this = default;
            Name = name;
            Type = PropertyType.Float;
            FloatValue = f;
        }

        public unsafe Property(string name, Vector2 vec2)
        {
            this = default;
            Name = name;
            Type = PropertyType.Float2;
            Vector2Value = vec2;
        }

        public unsafe Property(string name, Vector3 vec3)
        {
            this = default;
            Name = name;
            Type = PropertyType.Float3;
            Vector3Value = vec3;
        }

        public unsafe Property(string name, Vector4 vec4)
        {
            this = default;
            Name = name;
            Type = PropertyType.Float4;
            Vector4Value = vec4;
        }

        public unsafe Property(string name, Color4 col)
        {
            this = default;
            Name = name;
            Type = PropertyType.Color;
            ColorValue = col;
        }

        public unsafe Property(string name, Matrix3 mat3)
        {
            this = default;
            Name = name;
            Type = PropertyType.Matrix3;
            Matrix3Value = mat3;
        }

        public unsafe Property(string name, Matrix4 mat4)
        {
            this = default;
            Name = name;
            Type = PropertyType.Matrix4;
            Matrix4Value = mat4;
        }
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
        public RefList<Property> Properties = new RefList<Property>();
        public List<TextureProperty> Textures = new List<TextureProperty>();
        public CullMode CullMode = CullMode.Back;

        public MaterialProperties()
        { }

        public MaterialProperties(MaterialProperties properties)
        {
            Properties = new RefList<Property>(properties.Properties);
            Textures = new List<TextureProperty>(properties.Textures);
        }

        public void SetProperty(Property property)
        {
            var index = Properties.FindIndex(sp => sp.Name == property.Name);
            if (index == -1)
            {
                Properties.Add(property);
            }
            else
            {
                Properties[index] = property;
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
