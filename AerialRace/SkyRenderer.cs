﻿using AerialRace.Loading;
using AerialRace.RenderData;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AerialRace
{
    class SkyRenderer
    {
        public static readonly RenderData.Buffer FarPlaneNDCQuadBuffer;
        public static readonly Vector3[] FarPlaneNDCQuad = new Vector3[]
        {
            new Vector3(-1, -1, 1),
            new Vector3(-1,  1, 1),
            new Vector3( 1,  1, 1),
            new Vector3( 1, -1, 1),
        };

        public static readonly AttributeSpecification[] skyboxAttribs = new[]
        {
            new AttributeSpecification("Position", 3, RenderData.AttributeType.Float, false, 0),
        };

        public static readonly AttributeBufferLink[] skyboxAttribBufferLinks = new[]
        {
            // Pos attribute to Pos buffer
            new AttributeBufferLink(0, 0)
        };

        static SkyRenderer()
        {
            FarPlaneNDCQuadBuffer = RenderDataUtil.CreateDataBuffer<Vector3>("Sky far plane quad", FarPlaneNDCQuad, BufferFlags.None);
        }

        public static SkyRenderer Instance;

        public Material SkyMaterial;
        public Vector3 SunDirection;
        public Color4 SunColor;
        public Color4 SkyColor;
        public Color4 GroundColor;

        public SkyRenderer(Material skyMat, Vector3 sunDir, Color4 sunColor, Color4 skyColor, Color4 groundColor)
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

            // Setup skybox render data
            RenderDataUtil.BindIndexBuffer(StaticGeometry.UnitQuadIndexBuffer);
            RenderDataUtil.BindVertexAttribBuffer(0, FarPlaneNDCQuadBuffer);
            RenderDataUtil.SetAndEnableVertexAttributes(skyboxAttribs);
            RenderDataUtil.LinkAttributeBuffers(skyboxAttribBufferLinks);

            RenderDataUtil.UsePipeline(mat.Pipeline);

            var invProj = Matrix4.Invert(settings.Projection);
            var invView = Matrix4.Invert(settings.View);
            var invViewProj = Matrix4.Invert(settings.View * settings.Projection);

            RenderDataUtil.UniformMatrix4("invProj", ShaderStage.Vertex, true, ref invProj);
            RenderDataUtil.UniformMatrix4("invView", ShaderStage.Vertex, true, ref invView);
            RenderDataUtil.UniformMatrix4("invViewProj", ShaderStage.Vertex, true, ref invViewProj);

            RenderDataUtil.UniformMatrix4("invProj", ShaderStage.Fragment, true, ref invProj);
            RenderDataUtil.UniformMatrix4("invView", ShaderStage.Fragment, true, ref invView);
            RenderDataUtil.UniformMatrix4("invViewProj", ShaderStage.Fragment, true, ref invViewProj);

            RenderDataUtil.Uniform1("nearPlane", ShaderStage.Vertex, settings.NearPlane);
            RenderDataUtil.Uniform1("farPlane", ShaderStage.Vertex, settings.FarPlane);

            RenderDataUtil.UniformVector3("ViewPos", ShaderStage.Fragment, settings.ViewPos);

            RenderDataUtil.UniformVector3("sky.SunDirection", ShaderStage.Fragment, settings.Sky.SunDirection);
            RenderDataUtil.UniformVector3("sky.SunColor", ShaderStage.Fragment, settings.Sky.SunColor);
            RenderDataUtil.UniformVector3("sky.SkyColor", ShaderStage.Fragment, settings.Sky.SkyColor);
            RenderDataUtil.UniformVector3("sky.GroundColor", ShaderStage.Fragment, settings.Sky.GroundColor);

            RenderDataUtil.DrawAllElements(PrimitiveType.Triangles);
        }
    }
}
