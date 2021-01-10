using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AerialRace
{
    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    struct PointLight
    {
        public Vector4 PositionAndInvSqrRadius;
        public Vector4 Intensity;
    }

    public class Light
    {
        public Transform Transform;
        public Vector3 Intensity;
        public float Radius;

        public Light(Transform transform, Color4 intensity, float radius)
        {
            Transform = transform;
            Intensity = new Vector3(intensity.R, intensity.G, intensity.B);
            Radius = radius;
        }
    }

    class Lights
    {
        public List<Light> LightsList = new List<Light>();

        public PointLight[] PointLights = new PointLight[MaxLights];

        public const int MaxLights = 256;
        public RenderData.Buffer PointLightBuffer; 

        public Lights()
        {
            int bufferSize = 16 + 256 * Unsafe.SizeOf<PointLight>();
            PointLightBuffer = RenderData.RenderDataUtil.CreateDataBuffer("All point lights", bufferSize, RenderData.BufferFlags.Dynamic);
        }

        public void UpdateBufferData()
        {
            int i = 0;
            foreach (var light in LightsList)
            {
                ref var lightData = ref PointLights[i++];
                lightData.PositionAndInvSqrRadius = new Vector4(light.Transform.WorldPosition, 1f / (light.Radius * light.Radius));
                lightData.Intensity = new Vector4(light.Intensity, 1);
            }

            RenderData.RenderDataUtil.UploadBufferData(PointLightBuffer, 0, ref i, 1);
            RenderData.RenderDataUtil.UploadBufferData(PointLightBuffer, 16, PointLights.AsSpan());
        }

        // FIXME: Some way to reference a light. 
        // We want to be able to change and and delete lights after all...
        public Light AddPointLight(string name, Vector3 pos, Color4 intensity, float radius)
        {
            Transform transform = new Transform(name, pos);
            Light light = new Light(transform, intensity, radius);
            LightsList.Add(light);
            return light;
        }
    }
}
