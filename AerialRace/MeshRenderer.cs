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
        public Vector3 ViewPos;

        public DirectionalLight DirectionalLight;
        public Color4 AmbientLight;
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
        // FIXME: We should have a better system for binding and using textures
        public static Dictionary<string, int> NameToTextureUnit = new Dictionary<string, int>();

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

                RenderDataUtil.UniformMatrix4("model", ShaderStage.Vertex, true, ref model);
                RenderDataUtil.UniformMatrix4("view", ShaderStage.Vertex, true, ref settings.View);
                RenderDataUtil.UniformMatrix4("proj", ShaderStage.Vertex, true, ref settings.Projection);
                RenderDataUtil.UniformMatrix4("mv", ShaderStage.Vertex, true, ref mv);
                RenderDataUtil.UniformMatrix4("mvp", ShaderStage.Vertex, true, ref mvp);
                RenderDataUtil.UniformMatrix3("normalMatrix", ShaderStage.Vertex, true, ref normalMatrix);

                RenderDataUtil.UniformVector3("ViewPos", ShaderStage.Fragment, settings.ViewPos);

                RenderDataUtil.UniformVector3("dirLight.direction", ShaderStage.Fragment, settings.DirectionalLight.Direction.Normalized());
                RenderDataUtil.UniformVector3("dirLight.color", ShaderStage.Fragment, settings.DirectionalLight.Color);

                RenderDataUtil.UniformVector3("scene.ambientLight", ShaderStage.Fragment, settings.AmbientLight);

                NameToTextureUnit.Clear();

                var matProperties = material.Properties;
                for (int i = 0; i < matProperties.Textures.Count; i++)
                {
                    var (name, tex) = matProperties.Textures[i];

                    RenderDataUtil.Uniform1(name, ShaderStage.Vertex, i);
                    RenderDataUtil.Uniform1(name, ShaderStage.Fragment, i);
                    RenderDataUtil.BindTexture(i, tex);

                    NameToTextureUnit.Add(name, i);
                }

                for (int i = 0; i < matProperties.Samplers.Count; i++)
                {
                    var (name, sampler) = matProperties.Samplers[i];

                    if (NameToTextureUnit.TryGetValue(name, out int unit))
                    {
                        RenderDataUtil.BindSampler(unit, sampler);
                    }
                    else
                    {
                        Debug.Print($"There is no texture for the sampler '{name}'.");
                    }
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
