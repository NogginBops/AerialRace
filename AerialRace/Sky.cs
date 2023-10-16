using AerialRace.Loading;
using AerialRace.RenderData;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AerialRace
{
    class Sky
    {
        public static Sky Instance;

        public Transform Transform;

        public Material SkyMaterial;
        public Vector3 SunDirection;
        public Color4<Rgba> SunColor;
        public Color4<Rgba> SkyColor;
        public Color4<Rgba> GroundColor;

        public Sky(Material skyMat, Vector3 sunDir, Color4<Rgba> sunColor, Color4<Rgba> skyColor, Color4<Rgba> groundColor)
        {
            // FIXME: We might not want this later but it's fine for now.
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                throw new System.Exception("There can only be one sky renderer at a time");
            }

            Vector3 axis = Vector3.Cross(sunDir, Vector3.UnitY);
            float angle = Vector3.CalculateAngle(Vector3.UnitY, sunDir);
            Transform = new Transform("Sky", Vector3.Zero, Quaternion.FromAxisAngle(axis, angle));

            SkyMaterial = skyMat;
            SunDirection = sunDir;
            SunColor = sunColor;
            SkyColor = skyColor;
            GroundColor = groundColor;
        }

        public static void Render(ref RenderPassSettings settings)
        {
            if (Instance == null) return;

            var mat = Instance.SkyMaterial;

            RenderDataUtil.UsePipeline(mat.Pipeline);

            var invProj = Matrix4.Invert(settings.Projection);
            var invView = Matrix4.Invert(settings.View);
            var invViewProj = Matrix4.Invert(settings.View * settings.Projection);

            RenderDataUtil.UniformMatrix4("invProj", true, ref invProj);
            RenderDataUtil.UniformMatrix4("invView", true, ref invView);
            RenderDataUtil.UniformMatrix4("invViewProj", true, ref invViewProj);

            RenderDataUtil.UniformVector3("ViewPos", ShaderStage.Fragment, settings.ViewPos);

            RenderDataUtil.UniformVector3("sky.SunDirection", ShaderStage.Fragment, settings.Sky.SunDirection);
            RenderDataUtil.UniformVector3("sky.SunColor", ShaderStage.Fragment, settings.Sky.SunColor);
            RenderDataUtil.UniformVector3("sky.SkyColor", ShaderStage.Fragment, settings.Sky.SkyColor);
            RenderDataUtil.UniformVector3("sky.GroundColor", ShaderStage.Fragment, settings.Sky.GroundColor);

            settings.Metrics.DrawCalls++;
            settings.Metrics.Vertices += 3;
            settings.Metrics.Triangles += 1;

            RenderDataUtil.BindIndexBuffer(null);
            RenderDataUtil.DrawArrays(Primitive.Triangles, 0, 3);
        }
    }
}
