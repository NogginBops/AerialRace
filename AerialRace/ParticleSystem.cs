using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AerialRace.Particles
{
    public interface ISizeCalc
    {
        float Calculate(in ParticleSystemData particle, int index, float dt);
    }

    public struct ConstantSize : ISizeCalc
    {
        public float Size;

        public float Calculate(in ParticleSystemData particle, int index, float dt)
        {
            return Size;
        }
    }

    public interface IColorCalc
    {
        Vector3 Calculate(in ParticleSystemData particle, int index, float dt);
    }

    public struct ConstantColor : IColorCalc
    {
        public Vector3 Color;

        public Vector3 Calculate(in ParticleSystemData particle, int index, float dt)
        {
            return Color;
        }
    }

    public struct ColorOverLifetime : IColorCalc
    {
        public Vector3 StartColor;
        public Vector3 EndColor;

        public Vector3 Calculate(in ParticleSystemData particle, int index, float dt)
        {
            float life = particle.GetLifePercentage(index);

            return Vector3.Lerp(StartColor, EndColor, life);
        }
    }

    public interface IPositionCalc
    {
        Vector3 Calculate(in ParticleSystemData particle, int index, float dt);
    }

    public struct SimpleIntegrateVelocity : IPositionCalc
    {
        public Vector3 Calculate(in ParticleSystemData particle, int index, float dt)
        {
            return particle.Position[index] + particle.Velocity[index] * dt;
        }
    }

    public interface IVelocityCalc
    {
        Vector3 Calculate(in ParticleSystemData particle, int index, float dt);
    }

    public struct NoVelocity : IVelocityCalc
    {
        public Vector3 Calculate(in ParticleSystemData particle, int index, float dt)
        {
            return Vector3.Zero;
        }
    }

    public struct SimpleGravity : IVelocityCalc
    {
        public Vector3 Gravity;

        public Vector3 Calculate(in ParticleSystemData particle, int index, float dt)
        {
            return particle.Velocity[index] + Gravity * dt;
        }
    }

    public struct ParticleSystemData
    {
        public int Particles;
        public int ActiveParticles;
        public Vector3[] Position;
        public Vector3[] Velocity;
        public Vector3[] Color;
        public float[] Size;
        public float[] Age;
        public float[] Lifetime;

        public float GetLifePercentage(int i)
        {
            return Age[i] / Lifetime[i];
        }
    }

    public class ParticleSystem<TSize, TColor, TPosition, TVelocity>
        where TSize : struct, ISizeCalc
        where TColor : struct, IColorCalc
        where TPosition : struct, IPositionCalc
        where TVelocity : struct, IVelocityCalc
    {
        public ParticleSystemData Particles;

        public TSize SizeCalc;
        public TColor ColorCalc;
        public TPosition PositionCalc;
        public TVelocity VelocityCalc;

        public ParticleSystem(int maxParticles)
        {
            Particles.Particles = maxParticles;
            Particles.Position = new Vector3[Particles.Particles];
            Particles.Velocity = new Vector3[Particles.Particles];
            Particles.Size = new float[Particles.Particles];
            Particles.Lifetime = new float[Particles.Particles];
            Particles.Color = new Vector3[Particles.Particles];

            Random rand = new Random();
            for (int i = 0; i < Particles.Particles; i++)
            {
                Particles.Lifetime[i] = 10f;
                Particles.Position[i] = rand.NextPosition((-10, 0, -10), (10, 10, 10));
            }
        }

        public void Update(float deltaTime)
        {
            for (int i = 0; i < Particles.Particles; i++)
            {
                ref float age = ref Particles.Age[i];
                ref float lifetime = ref Particles.Lifetime[i];

                if (age < lifetime)
                {
                    age += deltaTime;

                    Particles.Velocity[i] = VelocityCalc.Calculate(Particles, i, deltaTime);
                    Particles.Position[i] = PositionCalc.Calculate(Particles, i, deltaTime);
                    Particles.Size[i] = SizeCalc.Calculate(Particles, i, deltaTime);
                    Particles.Color[i] = ColorCalc.Calculate(Particles, i, deltaTime);
                }
                else
                {
                    // kill particle
                }
            }
        }
    }
}
