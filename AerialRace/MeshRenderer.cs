using AerialRace.Debugging;
using AerialRace.RenderData;
using OpenTK.Graphics.OpenGL;
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
        public Color4<Rgba> SunColor;
        public Color4<Rgba> SkyColor;
        public Color4<Rgba> GroundColor;
    }

    struct RenderPassMetrics
    {
        public int Vertices;
        public int Triangles;

        public int DrawCalls;
        public int Culled;

        public int MeshesRenderers;



        public void Combine(RenderPassMetrics metrics)
        {
            Vertices += metrics.Vertices;
            Triangles += metrics.Triangles;

            DrawCalls += metrics.DrawCalls;
            Culled += metrics.Culled;

            MeshesRenderers += metrics.MeshesRenderers;
        }
    }

    // FIXME: Add transparent switch!
    unsafe struct RenderPassSettings
    {
        public string Name;

        public bool IsDepthPass;
        public bool IsTransparentPass;
        public Matrix4 View;
        public Matrix4 Projection;
        //public Matrix4 LightSpace;
        public Vector3 ViewPos;

        public float NearPlane, FarPlane;

        public CameraFrustumCullingData? CullingData;
        public UniformBuffer<CameraUniformData> CameraUniforms;

        public SkySettings Sky;
        // FIXME: We can use the procedural skybox for ambient light
        public Color4<Rgba> AmbientLight;

        public bool UseShadows;
        public Texture? ShadowMap;
        public ShadowSampler? ShadowSampler;
        public Vector4 Cascades;
        public Matrix4[]? LightMatrices;

        public Lights? Lights;

        // Shadow pass data
        public RefList<Mathematics.ShadowCaster>? ShadowCasterCullingData;
        public int Cascade;

        public RenderPassMetrics Metrics;
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

        public static bool InFrustum(CameraFrustumCullingData camera, Box3 AABB)
        {
            bool aboveLeft = Mathematics.Shadows.IsBoxAbovePlane(ref camera.Planes.Left, ref AABB);
            bool aboveRight = Mathematics.Shadows.IsBoxAbovePlane(ref camera.Planes.Right, ref AABB);
            bool aboveTop = Mathematics.Shadows.IsBoxAbovePlane(ref camera.Planes.Top, ref AABB);
            bool aboveBottom = Mathematics.Shadows.IsBoxAbovePlane(ref camera.Planes.Bottom, ref AABB);
            bool aboveNear = Mathematics.Shadows.IsBoxAbovePlane(ref camera.Planes.Near, ref AABB);
            bool aboveFar = Mathematics.Shadows.IsBoxAbovePlane(ref camera.Planes.Far, ref AABB);

            Mathematics.FrustumPoints points = new Mathematics.FrustumPoints(AABB);

            /*
            */
            /*
            if (aboveNear)
            {
                Color4 color = Color4.Red.WithAlpha(0.25f);
                Debugging.DebugHelper.FrustumPoints(Debugging.Debug.DepthTestList, points,
                                            color, color);
            }
            if (aboveFar)
            {
                Color4 color = Color4.Green.WithAlpha(0.25f);
                Debugging.DebugHelper.FrustumPoints(Debugging.Debug.DepthTestList, points,
                                            color, color);
            }

            if (aboveLeft)
            {
                Color4 color = Color4.Blue.WithAlpha(0.25f);
                Debugging.DebugHelper.FrustumPoints(Debugging.Debug.DepthTestList, points,
                                            color, color);
            }

            if (aboveRight)
            {
                Color4 color = Color4.Yellow.WithAlpha(0.25f);
                Debugging.DebugHelper.FrustumPoints(Debugging.Debug.DepthTestList, points,
                                            color, color);
            }

            if (aboveTop)
            {
                Color4 color = Color4.Magenta.WithAlpha(0.25f);
                Debugging.DebugHelper.FrustumPoints(Debugging.Debug.DepthTestList, points,
                                            color, color);
            }

            if (aboveBottom)
            {
                Color4 color = Color4.Cyan.WithAlpha(0.25f);
                Debugging.DebugHelper.FrustumPoints(Debugging.Debug.DepthTestList, points,
                                            color, color);
            }
            if (aboveLeft && aboveRight && aboveTop && aboveBottom && aboveNear && aboveFar)
            {
                Color4 color = Color4.White.WithAlpha(0.5f);
                Debugging.DebugHelper.FrustumPoints(Debugging.Debug.DepthTestList, points,
                                            color, color);
            }
            else
            {
                Color4 yellow = Color4.Red.WithAlpha(0.25f);
                Debugging.DebugHelper.FrustumPoints(Debugging.Debug.DepthTestList, points, yellow, yellow);
            }
            */

            return aboveLeft && aboveRight && aboveTop && aboveBottom && aboveNear && aboveFar;
            
            if (InFrustum(camera, new Vector3(AABB.Min.X, AABB.Min.Y, AABB.Min.Z)) == true) return true;
            if (InFrustum(camera, new Vector3(AABB.Min.X, AABB.Max.Y, AABB.Min.Z)) == true) return true;
            if (InFrustum(camera, new Vector3(AABB.Max.X, AABB.Min.Y, AABB.Min.Z)) == true) return true;
            if (InFrustum(camera, new Vector3(AABB.Max.X, AABB.Max.Y, AABB.Min.Z)) == true) return true;
            if (InFrustum(camera, new Vector3(AABB.Min.X, AABB.Min.Y, AABB.Max.Z)) == true) return true;
            if (InFrustum(camera, new Vector3(AABB.Min.X, AABB.Max.Y, AABB.Max.Z)) == true) return true;
            if (InFrustum(camera, new Vector3(AABB.Max.X, AABB.Min.Y, AABB.Max.Z)) == true) return true;
            if (InFrustum(camera, new Vector3(AABB.Max.X, AABB.Max.Y, AABB.Max.Z)) == true) return true;

            return false;

            static bool InFrustum(CameraFrustumCullingData camera, Vector3 point)
            {
                var v = point - camera.Position;
                float distanceAlongForward = Vector3.Dot(v, camera.Forward);
                if (distanceAlongForward < camera.NearPlane || distanceAlongForward > camera.FarPlane)
                    return false;

                return true;

                float halfHeightAtZ = distanceAlongForward * camera.TanHalfVFov;
                float verticalDistance = Vector3.Dot(v, camera.Up);
                if (verticalDistance < -halfHeightAtZ || verticalDistance > halfHeightAtZ)
                    return false;

                Vector3 cameraRight = Vector3.Cross(camera.Forward, camera.Up);
                float halfWidthAtZ = halfHeightAtZ * camera.Aspect;
                float horizontalDistance = Vector3.Dot(v, cameraRight);
                if (horizontalDistance < -halfWidthAtZ || horizontalDistance > halfWidthAtZ)
                    return false;

                return true;
            }
        }

        static List<Texture> TexturesToBind = new List<Texture>();
        static List<ISampler?> SamplersToBind = new List<ISampler?>();

        static List<ShaderPipeline> PipelinesWithRenderPassUniformsSet = new List<ShaderPipeline>();

        // FIXME: Passes
        public static void Render(ref RenderPassSettings settings)
        {
            PipelinesWithRenderPassUniformsSet.Clear();

            int index = 0;
            foreach (var instance in Instances)
            {
                var transform = instance.Transform;
                var mesh = instance.Mesh;
                var material = instance.Material;

                if (Window.FrustumCulling && settings.IsDepthPass && settings.ShadowCasterCullingData != null)
                {
                    bool culled = (settings.ShadowCasterCullingData[index].CulledMask & (1 << settings.Cascade)) == 1;
                    if (culled)
                    {
                        settings.Metrics.Culled++;
                        index++;
                        continue;
                    }
                }
                index++;

                if (Window.FrustumCulling && settings.CullingData != null)
                {
                    if (mesh.AABB == default)
                    {
                        Debug.Break();
                    }

                    var worldBounds = RecalculateAABB(mesh.AABB, transform);
                    if (InFrustum(settings.CullingData.Value, worldBounds) == false)
                    {
                        settings.Metrics.Culled++;
                        continue;
                    }
                }

                RenderDataUtil.BindMeshData(mesh);

                ShaderPipeline pipeline;
                if (settings.IsDepthPass)
                {
                    // FIXME: Should materials with no depth pipeline be considered transparent.
                    // i.e. not drawn in the depth pass?
                    pipeline = material.DepthPipeline ?? material.Pipeline;
                }
                else
                {
                    pipeline = material.Pipeline;
                }

                if (settings.IsTransparentPass != material.Properties.Transparent)
                {
                    continue;
                }

                RenderDataUtil.UsePipeline(pipeline);

                RenderDataUtil.SetCullMode(material.Properties.CullMode);
                RenderDataUtil.SetAlphaToCoverage(material.Properties.AlphaToCoverage);

                if (PipelinesWithRenderPassUniformsSet.Contains(pipeline) == false)
                {
                    SetRenderPassUniforms(ref settings);
                    PipelinesWithRenderPassUniformsSet.Add(pipeline);
                }

                // Because the matrices should all be updated we don't need to calculate it again
                // transform.GetTransformationMatrix(out var model);
                SetPerModelData(ref transform.LocalToWorld, ref settings);
                
                //int textureStartIndex = 1;

                int numberOfTextures = material.Properties.Textures.Count;
                if (settings.UseShadows) numberOfTextures++;

                TexturesToBind.Clear();
                SamplersToBind.Clear();

                // FIXME!! Make binding texture better!
                if (settings.UseShadows)
                {
                    int location = RenderDataUtil.GetUniformLocation("ShadowCascades", RenderDataUtil.CurrentPipeline.FragmentProgram);
                    if (location != -1)
                    {
                        RenderDataUtil.Uniform1("UseShadows", settings.UseShadows ? 1 : 0);

                        //textureStartIndex = 1;

                        RenderDataUtil.Uniform1("ShadowCascades", 0);
                        //RenderDataUtil.BindTexture(0, settings.ShadowMap!, settings.ShadowSampler!);

                        TexturesToBind.Add(settings.ShadowMap!);
                        SamplersToBind.Add(settings.ShadowSampler!);

                        RenderDataUtil.Uniform4("CascadeSplits", settings.Cascades);
                    }
                }

                var matProperties = material.Properties;
                for (int i = 0; i < matProperties.Textures.Count; i++)
                {
                    //var ioff = textureStartIndex + i;

                    var texProp = matProperties.Textures[i];
                    var name = texProp.Name;

                    int location = RenderDataUtil.GetUniformLocation(name, RenderDataUtil.CurrentPipeline.FragmentProgram);

                    if (location != -1)
                    {
                        var ioff = TexturesToBind.Count;

                        RenderDataUtil.Uniform1(name, ioff);
                        //RenderDataUtil.BindTexture(ioff, texProp.Texture, texProp.Sampler);

                        TexturesToBind.Add(texProp.Texture);
                        SamplersToBind.Add(texProp.Sampler);
                    }
                    else
                    {
                        // FIXME: Remove this, this is so that stuff doesn't break
                        // when we substitue a shader with an error shader.
                        // It would cause the wrong texture to be bound and cause general weirdness.
                        // We want a better way to make sure the right data is bound.
                        RenderDataUtil.Uniform1(name, 10);
                    }
                }

                RenderDataUtil.BindTextures(0,
                    CollectionsMarshal.AsSpan(TexturesToBind),
                    CollectionsMarshal.AsSpan(SamplersToBind));

                for (int i = 0; i < matProperties.Properties.Count; i++)
                {
                    RenderDataUtil.UniformProperty(ref matProperties.Properties[i]);
                }

                ref var metrics = ref settings.Metrics;
                metrics.DrawCalls++;
                metrics.MeshesRenderers++;
                metrics.Triangles += instance.Mesh.Indices!.Elements / 3;

                // FIXME: Don't assume there is any data buffers!!
                metrics.Vertices += instance.Mesh.DataBuffers[0].Elements;

                RenderDataUtil.DrawElements(
                    // FIXME: Make our own primitive type enum
                    Primitive.Triangles,
                    instance.Mesh.Indices!.Elements,
                    instance.Mesh.Indices.IndexType, 0);
            }

            //Debug.WriteLine($"Culled {culled} meshes for pass '{settings.Name}'");
        }

        // This does not set shadow texture stuff atm as we don't have a nice model for binding textures
        public static void SetPerModelData(ref Matrix4 model, ref RenderPassSettings settings)
        {
            Transform.MultMVP(ref model, ref settings.View, ref settings.Projection, out var mv, out var mvp);
            Matrix3 normalMatrix = Matrix3.Transpose(new Matrix3(Matrix4.Invert(model)));

            RenderDataUtil.UniformMatrix4("model", true, ref model);
            RenderDataUtil.UniformMatrix4("mv", true, ref mv);
            RenderDataUtil.UniformMatrix4("mvp", true, ref mvp);
            RenderDataUtil.UniformMatrix3("normalMatrix", true, ref normalMatrix);
        }

        public static void SetRenderPassUniforms(ref RenderPassSettings settings)
        {
            if (settings.Lights != null)
            {
                RenderDataUtil.UniformBlock("LightBlock", settings.Lights!.PointLightBuffer);
            }

            if (settings.LightMatrices != null)
            {
                RenderDataUtil.UniformMatrix4Array("worldToLightSpace", ShaderStage.Fragment, true, settings.LightMatrices);
            }
            else if (settings.UseShadows) Debug.Assert();

            RenderDataUtil.UniformBlock("u_CameraBlock", settings.CameraUniforms);

            RenderDataUtil.UniformMatrix4("view", true, ref settings.View);
            RenderDataUtil.UniformMatrix4("proj", true, ref settings.Projection);

            RenderDataUtil.Uniform1("camera.near", settings.NearPlane);
            RenderDataUtil.Uniform1("camera.far", settings.FarPlane);
            RenderDataUtil.UniformVector3("camera.position", settings.ViewPos);

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
