using AerialRace.Debugging;
using AerialRace.RenderData;
using OpenTK.Mathematics;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace AerialRace
{
    struct DirectionalLight
    {
        public Vector3 Direction;
        public Color4 Color;
    }

    struct RenderPassSettings
    {
        public bool IsDepthPass;
        public Matrix4 View;
        public Matrix4 Projection;
        public Matrix4 LightSpace;
        public Vector3 ViewPos;

        public float NearPlane, FarPlane;

        public DirectionalLight DirectionalLight;
        public Color4 AmbientLight;

        public bool UseShadows;
        public Texture? ShadowMap;
        public ShadowSampler? ShadowSampler;
    }

    abstract class SelfCollection<TSelf> where TSelf : SelfCollection<TSelf>
    {
        public static List<TSelf> Instances = new List<TSelf>();

        public bool Destroyed = false;

        public SelfCollection()
        {
            Instances.Add((TSelf)this);
        }

        public void Destroy()
        {
            Instances.Remove((TSelf)this);
            Destroyed = true;
        }
    }

    class MeshRenderer : SelfCollection<MeshRenderer>
    {
        public Transform Transform;
        public Mesh Mesh;
        public Material Material;

        public MeshRenderer(Transform transform, Mesh mesh, Material material) : base()
        {
            Transform = transform;
            Mesh = mesh;
            Material = material;
        }

        // FIXME: Passes
        public static void Render(ref RenderPassSettings settings)
        {
            foreach (var instance in Instances)
            {
                var transform = instance.Transform;
                var mesh = instance.Mesh;
                var material = instance.Material;

                RenderDataUtil.BindMeshData(mesh);

                if (settings.IsDepthPass)
                {
                    RenderDataUtil.UsePipeline(material.DepthPipeline ?? material.Pipeline);
                }
                else
                {
                    RenderDataUtil.UsePipeline(material.Pipeline);
                }

                // Because the matrices should all be updated we don't need to calculate it again
                //transform.GetTransformationMatrix(out var model);
                var model = transform.LocalToWorld;

                Transformations.MultMVP(ref model, ref settings.View, ref settings.Projection, out var mv, out var mvp);
                Matrix3 normalMatrix = Matrix3.Transpose(new Matrix3(Matrix4.Invert(model)));

                Matrix4 modelToLightSpace = model * settings.LightSpace;

                RenderDataUtil.UniformMatrix4("model", ShaderStage.Vertex, true, ref model);
                RenderDataUtil.UniformMatrix4("view", ShaderStage.Vertex, true, ref settings.View);
                RenderDataUtil.UniformMatrix4("proj", ShaderStage.Vertex, true, ref settings.Projection);
                RenderDataUtil.UniformMatrix4("mv", ShaderStage.Vertex, true, ref mv);
                RenderDataUtil.UniformMatrix4("mvp", ShaderStage.Vertex, true, ref mvp);
                RenderDataUtil.UniformMatrix3("normalMatrix", ShaderStage.Vertex, true, ref normalMatrix);

                RenderDataUtil.UniformMatrix4("lightSpaceMatrix", ShaderStage.Vertex, true, ref settings.LightSpace);
                RenderDataUtil.UniformMatrix4("modelToLightSpace", ShaderStage.Vertex, true, ref modelToLightSpace);

                RenderDataUtil.UniformVector3("ViewPos", ShaderStage.Fragment, settings.ViewPos);

                RenderDataUtil.UniformVector3("dirLight.direction", ShaderStage.Fragment, settings.DirectionalLight.Direction.Normalized());
                RenderDataUtil.UniformVector3("dirLight.color", ShaderStage.Fragment, settings.DirectionalLight.Color);

                RenderDataUtil.UniformVector3("scene.ambientLight", ShaderStage.Fragment, settings.AmbientLight);

                int textureStartIndex = 0;

                // FIXME!! Make binding texture better!
                RenderDataUtil.Uniform1("UseShadows", ShaderStage.Vertex, settings.UseShadows ? 1 : 0);
                RenderDataUtil.Uniform1("UseShadows", ShaderStage.Fragment, settings.UseShadows ? 1 : 0);
                if (settings.UseShadows)
                {
                    textureStartIndex = 1;

                    RenderDataUtil.Uniform1("ShadowMap", ShaderStage.Vertex, 0);
                    RenderDataUtil.Uniform1("ShadowMap", ShaderStage.Fragment, 0);
                    RenderDataUtil.BindTexture(0, settings.ShadowMap!);
                    RenderDataUtil.BindSampler(0, settings.ShadowSampler!);
                    //RenderDataUtil.BindSampler(0, (ISampler?)null);
                }

                var matProperties = material.Properties;
                for (int i = 0; i < matProperties.Textures.Count; i++)
                {
                    var ioff = textureStartIndex + i;

                    var texProp = matProperties.Textures[i];
                    var name = texProp.Name;

                    RenderDataUtil.Uniform1(name, ShaderStage.Vertex, ioff);
                    RenderDataUtil.Uniform1(name, ShaderStage.Fragment, ioff);
                    RenderDataUtil.BindTexture(ioff, texProp.Texture, texProp.Sampler);
                }

                RenderDataUtil.DrawElements(
                    // FIXME: Make our own primitive type enum
                    OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles,
                    instance.Mesh.Indices!.Elements,
                    instance.Mesh.Indices.IndexType, 0);
            }
        }
    }
}
