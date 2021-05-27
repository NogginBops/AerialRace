using AerialRace.RenderData;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Buffer = AerialRace.RenderData.Buffer;

namespace AerialRace
{
    [Serializable]
    class TrailRenderer : SelfCollection<TrailRenderer>
    {
        public static readonly AttributeSpecification PositionAttributeSpec = new AttributeSpecification("Trail Position", 3, AttributeType.Float, false, 0);
        public Trail Trail;
        public Material Material;

        public TrailRenderer(Trail trail, Material material)
        {
            Trail = trail;
            Material = material;
        }

        public static void Render(ref RenderPassSettings settings)
        {
            foreach (var instance in Instances)
            {
                int vertices = instance.Trail.UploadData();

                RenderDataUtil.BindIndexBuffer(null);
                RenderDataUtil.BindVertexAttribBuffer(0, instance.Trail.VertexBuffer, 0);
                RenderDataUtil.SetAndEnableVertexAttribute(0, PositionAttributeSpec);
                RenderDataUtil.LinkAttributeBuffer(0, 0);

                var material = instance.Material;
                RenderDataUtil.UsePipeline(instance.Material.Pipeline);

                var model = Matrix4.Identity;
                Transform.MultMVP(ref model, ref settings.View, ref settings.Projection, out var mv, out var mvp);
                Matrix3 normalMatrix = Matrix3.Transpose(new Matrix3(Matrix4.Invert(model)));

                //Matrix4 modelToLightSpace = model * settings.LightSpace;

                RenderDataUtil.UniformMatrix4("model", true, ref model);
                RenderDataUtil.UniformMatrix4("view", true, ref settings.View);
                RenderDataUtil.UniformMatrix4("proj", true, ref settings.Projection);
                RenderDataUtil.UniformMatrix4("mv", true, ref mv);
                RenderDataUtil.UniformMatrix4("mvp", true, ref mvp);
                //RenderDataUtil.UniformMatrix3("normalMatrix", ShaderStage.Vertex, true, ref normalMatrix);

                //RenderDataUtil.UniformMatrix4("lightSpaceMatrix", ShaderStage.Vertex, true, ref settings.LightSpace);
                //RenderDataUtil.UniformMatrix4("modelToLightSpace", ShaderStage.Vertex, true, ref modelToLightSpace);

                RenderDataUtil.UniformVector3("ViewPos", ShaderStage.Fragment, settings.ViewPos);

                RenderDataUtil.UniformVector3("sky.SunDirection", ShaderStage.Fragment, settings.Sky.SunDirection);
                RenderDataUtil.UniformVector3("sky.SunColor", ShaderStage.Fragment, settings.Sky.SunColor);
                RenderDataUtil.UniformVector3("sky.SkyColor", ShaderStage.Fragment, settings.Sky.SkyColor);
                RenderDataUtil.UniformVector3("sky.GroundColor", ShaderStage.Fragment, settings.Sky.GroundColor);

                RenderDataUtil.UniformVector3("scene.ambientLight", ShaderStage.Fragment, settings.AmbientLight);

                var textureStartIndex = 0;
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

                RenderDataUtil.DrawArrays(Primitive.LineStrip, 0, vertices);
            }
        }
    }

    class RingBufferExternalArray
    {
        public readonly int Size;

        public int WriteHead = 0;
        public int ReadHead = 0;
        public int Count = 0;

        public RingBufferExternalArray(int size)
        {
            Size = size;
        }

        public int Write()
        {
            if (Count == Size)
            {
                //throw new NotSupportedException("Ring buffer is full.");
                ReadHead = (ReadHead + 1) % Size;
                Count--;
            }

            int index = WriteHead;

            Count++;
            WriteHead = (WriteHead + 1) % Size;

            return index;
        }

        public int Read()
        {
            if (Count == 0)
                throw new NotSupportedException("Ring buffer is empty.");

            int index = WriteHead;

            Count--;
            ReadHead = (ReadHead + 1) % Size;

            return index;
        }
    }

    class Trail
    {
        public string Name;
        public float TrailTime;

        public Buffer VertexBuffer;

        public int MaxSegments => RingBuffer.Size;
        public RingBufferExternalArray RingBuffer;
        public Vector3[] TrailPositions;
        public float[] TrailTimes;

        public float MinDistanceSqr;

        public Trail(string name, float time, float minDistance, int maxSegments)
        {
            Name = name;

            TrailTime = time;
            MinDistanceSqr = minDistance * minDistance;

            RingBuffer = new RingBufferExternalArray(maxSegments);
            TrailPositions = new Vector3[maxSegments];
            TrailTimes = new float[maxSegments];

            VertexBuffer = RenderDataUtil.CreateDataBuffer<Vector3>($"Trail: {name}", maxSegments, BufferFlags.Dynamic);
        }

        // Returns the number of vertices in the buffer
        public int UploadData()
        {
            Span<Vector3> positions = TrailPositions;

            if (RingBuffer.Count != 0)
            {
                if (RingBuffer.ReadHead < RingBuffer.WriteHead)
                {
                    var validPositions = positions[RingBuffer.ReadHead..RingBuffer.WriteHead];
                    Debugging.Debug.Assert(validPositions.Length == RingBuffer.Count);
                    RenderDataUtil.UploadBufferData(VertexBuffer, 0, validPositions);
                    return validPositions.Length;
                }
                else
                {
                    // Here there is discontious memory
                    var first = positions[RingBuffer.ReadHead..];
                    var second = positions[0..RingBuffer.WriteHead];
                    Debugging.Debug.Assert((first.Length + second.Length) == RingBuffer.Count);
                    RenderDataUtil.UploadBufferData(VertexBuffer, 0, first);
                    RenderDataUtil.UploadBufferData(VertexBuffer, first.Length * Unsafe.SizeOf<Vector3>(), second);
                    return first.Length + second.Length;
                }
            }
            else
            {
                return 0;
            }
        }

        public void Update(Vector3 position, float deltaTime)
        {
            for (int i = RingBuffer.ReadHead; i != RingBuffer.WriteHead; i = (i + 1) % MaxSegments)
            {
                TrailTimes[i] -= deltaTime;

                if (TrailTimes[i] <= 0)
                {
                    // Read one value from the buffer
                    RingBuffer.Read();

                    // We don't need to do this but we do it anyways
                    TrailTimes[i] = 0;
                    TrailPositions[i] = Vector3.Zero;
                }
            }

            if (RingBuffer.Count == 0)
            {
                var index = RingBuffer.Write();
                TrailPositions[index] = position;
                TrailTimes[index] = TrailTime;
            }
            else
            {
                if (Vector3.DistanceSquared(TrailPositions[RingBuffer.ReadHead], position) > MinDistanceSqr)
                {
                    var index = RingBuffer.Write();
                    TrailPositions[index] = position;
                    TrailTimes[index] = TrailTime;
                }
            }
        }
    }
}
