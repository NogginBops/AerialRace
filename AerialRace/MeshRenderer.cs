using AerialRace.Debugging;
using AerialRace.RenderData;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace AerialRace
{
    struct SkySettings
    {
        public Vector3 SunDirection;
        public Color4 SunColor;
        public Color4 SkyColor;
        public Color4 GroundColor;
    }

    // FIXME: Add transparent switch!
    unsafe struct RenderPassSettings
    {
        public bool IsDepthPass;
        public Matrix4 View;
        public Matrix4 Projection;
        //public Matrix4 LightSpace;
        public Vector3 ViewPos;

        public float NearPlane, FarPlane;

        public UniformBuffer<CameraUniformData> CameraUniforms;

        public SkySettings Sky;
        // FIXME: We can use the procedural skybox for ambient light
        public Color4 AmbientLight;

        public bool UseShadows;
        public Texture? ShadowMap;
        public ShadowSampler? ShadowSampler;
        public Vector4 Cascades;
        public Matrix4[]? LightMatrices;

        public Lights? Lights;
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

    [Serializable]
    class MeshRenderer : SelfCollection<MeshRenderer>
    {
        [NonSerialized] public Transform Transform;
        public Mesh Mesh;
        public Material Material;
        public bool CastShadows;

        public MeshRenderer(Transform transform, Mesh mesh, Material material) : base()
        {
            Transform = transform;
            Mesh = mesh;
            Material = material;
            CastShadows = true;
        }

        public static Box3 RecalculateAABB(Box3 AABB, Transform transform)
        {
            // http://www.realtimerendering.com/resources/GraphicsGems/gems/TransBox.c

            var l2w = transform.LocalToWorld;
            var rotation = new Matrix3(l2w);
            var translation = l2w.Row3.Xyz;

            Span<float> Amin = stackalloc float[3];
            Span<float> Amax = stackalloc float[3];
            Span<float> Bmin = stackalloc float[3];
            Span<float> Bmax = stackalloc float[3];

            Amin[0] = AABB.Min.X; Amax[0] = AABB.Max.X;
            Amin[1] = AABB.Min.Y; Amax[1] = AABB.Max.Y;
            Amin[2] = AABB.Min.Z; Amax[2] = AABB.Max.Z;

            Bmin[0] = Bmax[0] = translation.X;
            Bmin[1] = Bmax[1] = translation.Y;
            Bmin[2] = Bmax[2] = translation.Z;

            for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
            {
                var a = rotation[j, i] * Amin[j];
                var b = rotation[j, i] * Amax[j];
                Bmin[i] += a < b ? a : b;
                Bmax[i] += a < b ? b : a;
            }

            return new Box3(Bmin[0], Bmin[1], Bmin[2], Bmax[0], Bmax[1], Bmax[2]);
        }

        static List<Texture> TexturesToBind = new List<Texture>();
        static List<ISampler?> SamplersToBind = new List<ISampler?>();

        // FIXME: Passes
        public static void Render(ref RenderPassSettings settings)
        {
            foreach (var instance in Instances)
            {
                var transform = instance.Transform;
                var mesh = instance.Mesh;
                var material = instance.Material;

                //var worldBounds = RecalculateAABB(mesh.AABB, transform);
                
                RenderDataUtil.BindMeshData(mesh);

                if (settings.IsDepthPass)
                {
                    // FIXME: Should materials with no depth pipeline be considered transparent.
                    // i.e. not drawn in the depth pass?
                    RenderDataUtil.UsePipeline(material.DepthPipeline ?? material.Pipeline);
                }
                else
                {
                    RenderDataUtil.UsePipeline(material.Pipeline);
                }

                RenderDataUtil.SetCullMode(material.Properties.CullMode);

                // Because the matrices should all be updated we don't need to calculate it again
                //transform.GetTransformationMatrix(out var model);
                SetRenderPassUniforms(ref transform.LocalToWorld, ref settings);

                int textureStartIndex = 0;

                int numberOfTextures = material.Properties.Textures.Count;
                if (settings.UseShadows) numberOfTextures++;

                TexturesToBind.Clear();
                SamplersToBind.Clear();

                // FIXME!! Make binding texture better!
                RenderDataUtil.Uniform1("UseShadows", settings.UseShadows ? 1 : 0);
                if (settings.UseShadows)
                {
                    textureStartIndex = 1;

                    RenderDataUtil.Uniform1("ShadowCascades", 0);
                    //RenderDataUtil.BindTexture(0, settings.ShadowMap!, settings.ShadowSampler!);
                    
                    TexturesToBind.Add(settings.ShadowMap!);
                    SamplersToBind.Add(settings.ShadowSampler!);

                    RenderDataUtil.Uniform4("CascadeSplits", settings.Cascades);
                }

                var matProperties = material.Properties;
                for (int i = 0; i < matProperties.Textures.Count; i++)
                {
                    var ioff = textureStartIndex + i;

                    var texProp = matProperties.Textures[i];
                    var name = texProp.Name;

                    RenderDataUtil.Uniform1(name, ioff);
                    //RenderDataUtil.BindTexture(ioff, texProp.Texture, texProp.Sampler);

                    TexturesToBind.Add(texProp.Texture);
                    SamplersToBind.Add(texProp.Sampler);
                }

                RenderDataUtil.BindTextures(0,
                    CollectionsMarshal.AsSpan(TexturesToBind),
                    CollectionsMarshal.AsSpan(SamplersToBind));

                for (int i = 0; i < matProperties.Properties.Count; i++)
                {
                    RenderDataUtil.UniformProperty(ref matProperties.Properties[i]);
                }

                RenderDataUtil.DrawElements(
                    // FIXME: Make our own primitive type enum
                    Primitive.Triangles,
                    instance.Mesh.Indices!.Elements,
                    instance.Mesh.Indices.IndexType, 0);
            }
        }

        // This does not set shadow texture stuff atm as we don't have a nice model for binding textures
        public static void SetRenderPassUniforms(ref Matrix4 model, ref RenderPassSettings settings)
        {
            Transform.MultMVP(ref model, ref settings.View, ref settings.Projection, out var mv, out var mvp);
            Matrix3 normalMatrix = Matrix3.Transpose(new Matrix3(Matrix4.Invert(model)));

            //Matrix4 modelToLightSpace = model * settings.LightSpace;

            if (settings.Lights != null)
            {
                RenderDataUtil.UniformBlock("LightBlock", settings.Lights!.PointLightBuffer);
            }

            RenderDataUtil.UniformBlock("u_CameraBlock", settings.CameraUniforms);

            RenderDataUtil.UniformMatrix4("model", true, ref model);
            RenderDataUtil.UniformMatrix4("view", true, ref settings.View);
            RenderDataUtil.UniformMatrix4("proj", true, ref settings.Projection);
            RenderDataUtil.UniformMatrix4("mv", true, ref mv);
            RenderDataUtil.UniformMatrix4("mvp", true, ref mvp);
            RenderDataUtil.UniformMatrix3("normalMatrix", true, ref normalMatrix);

            RenderDataUtil.Uniform1("camera.near", settings.NearPlane);
            RenderDataUtil.Uniform1("camera.far", settings.FarPlane);
            RenderDataUtil.UniformVector3("camera.position", settings.ViewPos);

            if (settings.LightMatrices != null)
            {
                RenderDataUtil.UniformMatrix4Array("worldToLightSpace", ShaderStage.Fragment, true, settings.LightMatrices);
            }
            else if (settings.UseShadows) Debug.Assert();

            //RenderDataUtil.UniformMatrix4("lightSpaceMatrix", ShaderStage.Vertex, true, ref settings.LightSpace);
            //RenderDataUtil.UniformMatrix4("modelToLightSpace", ShaderStage.Vertex, true, ref modelToLightSpace);

            RenderDataUtil.UniformVector3("sky.SunDirection", settings.Sky.SunDirection);
            RenderDataUtil.UniformVector3("sky.SunColor", settings.Sky.SunColor);
            RenderDataUtil.UniformVector3("sky.SkyColor", settings.Sky.SkyColor);
            RenderDataUtil.UniformVector3("sky.GroundColor", settings.Sky.GroundColor);

            RenderDataUtil.UniformVector3("scene.ambientLight", settings.AmbientLight);

            RenderDataUtil.Uniform1("AttenuationType", Window.LightFalloff);
            RenderDataUtil.Uniform1("LightCutout", Window.LightCutout);
        }

        public static void RenderAABBs(DrawList list)
        {
            foreach (var instance in Instances)
            {
                var transform = instance.Transform;
                var mesh = instance.Mesh;
                var AABB = RecalculateAABB(mesh.AABB, transform);

                DebugHelper.OutlineBox(list, AABB, Color4.Yellow);
            }
        }
    }
}
