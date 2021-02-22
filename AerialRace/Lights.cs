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
        public Vector4 PositionAndInvRadius;
        public Vector4 Intensity;
    }

    public class Light
    {
        public Transform Transform;
        public Vector3 Intensity;
        public float Radius;
        public float Candela;

        public Light(Transform transform, Color4 intensity, float radius, float candela)
        {
            Transform = transform;
            Intensity = new Vector3(intensity.R, intensity.G, intensity.B);
            Radius = radius;
            Candela = candela;
        }

        // https://seblagarde.files.wordpress.com/2015/07/course_notes_moving_frostbite_to_pbr_v32.pdf
        // https://www.wolframalpha.com/input/?i=int+x+from+0.01+to+infinity+%281%2Fmax%28x%5E2%2C+0.01%5E2%29%29*%28min%28max%28%281-%28x%5E4%2Fr%5E4%29%29+%2C0%29%2C1%29%29
        public static float CalculatePointLightPercentageAccuracyFromRadius(float radius)
        {
            float r2 = radius * radius;
            return (((3e-7f / (10 * r2 * r2)) - (3f / (4f * radius))) / 100f) + 1;
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
                lightData.PositionAndInvRadius = new Vector4(light.Transform.WorldPosition, 1f / (light.Radius));
                lightData.Intensity = new Vector4(light.Intensity, light.Candela);
            }

            RenderData.RenderDataUtil.UploadBufferData(PointLightBuffer, 0, ref i, 1);
            RenderData.RenderDataUtil.UploadBufferData(PointLightBuffer, 16, PointLights.AsSpan());
        }

        // FIXME: Some way to reference a light. 
        // We want to be able to change and and delete lights after all...
        public Light AddPointLight(string name, Vector3 pos, Color4 intensity, float radius, float candela)
        {
            Transform transform = new Transform(name, pos);
            Light light = new Light(transform, intensity, radius, candela);
            LightsList.Add(light);
            return light;
        }
    }
}
