﻿using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Constraints;
using BepuUtilities.Memory;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace AerialRace.Physics
{
    // Used for pose integration data
    struct SimpleBody
    {
        public static SimpleBody Default => new SimpleBody()
        {
            HasGravity = true,
            LinearDamping = 0.03f,
            AngularDamping = 0.03f,
        };

        public bool HasGravity;
        public float LinearDamping;
        public float AngularDamping;
    }

    // Used for narrow phase collision material data
    public struct SimpleMaterial
    {
        public static SimpleMaterial Default => new SimpleMaterial()
        {
            SpringSettings = new SpringSettings(30, 1),
            FrictionCoefficient = 1f,
            MaximumRecoveryVelocity = 2f,
        };

        public SpringSettings SpringSettings;
        public float FrictionCoefficient;
        public float MaximumRecoveryVelocity;
    }

    static class Phys
    {
        public static Simulation Simulation;
        public static BufferPool BufferPool = new BufferPool();

        public static CollidableProperty<SimpleMaterial> Materials;
        public static BodyProperty<SimpleBody> BodyProps;

        public static void Init()
        {
            Materials = new CollidableProperty<SimpleMaterial>();
            BodyProps = new BodyProperty<SimpleBody>();

            Simulation = Simulation.Create(
                BufferPool,
                new NarrowPhaseCallbacks(Materials),
                new PoseIntegratorCallbacks(new Vector3(0, -9.81f, 0).ToNumerics(), BodyProps),
                new PositionLastTimestepper());
        }

        public static BodyReference AddDynamicBody(RigidPose pose, ICollider collider, float mass, float speculativeMargin, BodyActivityDescription bodyActivityDesc, SimpleMaterial mat, SimpleBody bodyProp)
        {
            collider.Shape.ComputeInertia(mass, out var inertia);
            var handle = Simulation.Bodies.Add(
                BodyDescription.CreateDynamic(
                    pose,
                    inertia,
                    new CollidableDescription(collider.TypedIndex, speculativeMargin),
                    bodyActivityDesc)
                );

            // Allocate and set the material for this body
            Materials.Allocate(handle) = mat;

            // Allocate and set the body pose data
            BodyProps.Allocate(handle) = bodyProp;

            return new BodyReference(handle, Simulation.Bodies);
        }

        public static StaticReference AddStaticBody(Vector3 position, ICollider collider, float speculativeMargin, SimpleMaterial material)
        {
            var handle = Simulation.Statics.Add(
                new StaticDescription(
                    position.ToNumerics(),
                    collider.TypedIndex,
                    speculativeMargin
                ));

            Materials.Allocate(handle) = material;

            return new StaticReference(handle, Simulation.Statics);
        }

        public static void Update(float dt)
        {
            Simulation.Timestep(dt);
        }

        public static void Dispose()
        {
            Simulation.Dispose();
            BufferPool.Clear();
        }
    }
}
