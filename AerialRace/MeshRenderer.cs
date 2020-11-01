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
        public static void Render(Camera camera)
        {
            foreach (var instance in Instances)
            {
                var transform = instance.Transform;
                var mesh = instance.Mesh;
                var material = instance.Material;

                RenderDataUtil.BindMeshData(mesh);
                RenderDataUtil.UsePipeline(material.Pipeline);

                transform.GetTransformationMatrix(out var model);
                var view = camera.Transform.WorldToLocal;
                camera.CalcProjectionMatrix(out var proj);

                Transformations.MultMVP(ref model, ref view, ref proj, out var mv, out var mvp);
                Matrix3 normalMatrix = Matrix3.Transpose(new Matrix3(Matrix4.Invert(model)));

                RenderDataUtil.UniformMatrix4("model", ShaderStage.Vertex, true, ref model);
                RenderDataUtil.UniformMatrix4("view", ShaderStage.Vertex, true, ref view);
                RenderDataUtil.UniformMatrix4("proj", ShaderStage.Vertex, true, ref proj);
                RenderDataUtil.UniformMatrix4("mv", ShaderStage.Vertex, true, ref mv);
                RenderDataUtil.UniformMatrix4("mvp", ShaderStage.Vertex, true, ref mvp);
                RenderDataUtil.UniformMatrix3("normalMatrix", ShaderStage.Vertex, true, ref normalMatrix);

                RenderDataUtil.UniformVector3("ViewPos", ShaderStage.Fragment, camera.Transform.WorldPosition);

                RenderDataUtil.UniformVector3("dirLight.direction", ShaderStage.Fragment, new Vector3(1f, -1, 0).Normalized());
                RenderDataUtil.UniformVector3("dirLight.color", ShaderStage.Fragment, new Vector3(1f, 1f, 1f));

                RenderDataUtil.UniformVector3("scene.ambientLight", ShaderStage.Fragment, Vector3.One * 0.1f);

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
