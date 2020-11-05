﻿using BepuPhysics;
using BepuUtilities;
using BepuUtilities.Memory;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace AerialRace.Physics
{
    struct PoseIntegratorCallbacks : IPoseIntegratorCallbacks
    {
        public Simulation Simulation;
        public BodyProperty<SimpleBody> BodyProps;

        public Vector3 Gravity;
        Vector3 GravityDt;

        float deltaTime;

        /// <summary>
        /// Gets how the pose integrator should handle angular velocity integration.
        /// </summary>
        // TODO: Figure out what mode we actually want
        public AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;

        public PoseIntegratorCallbacks(Vector3 gravity, BodyProperty<SimpleBody> bodyProps, float linearDamping = .03f, float angularDamping = .03f) : this()
        {
            Gravity = gravity;

            BodyProps = bodyProps;
        }

        public void Initialize(Simulation simulation)
        {
            Simulation = simulation;
            BodyProps.Initialize(Simulation);
        }

        /// <summary>
        /// Called prior to integrating the simulation's active bodies. When used with a substepping timestepper, this could be called multiple times per frame with different time step values.
        /// </summary>
        /// <param name="dt">Current time step duration.</param>
        public void PrepareForIntegration(float dt)
        {
            // No reason to recalculate gravity * dt for every body; just cache it ahead of time.
            GravityDt = Gravity * dt;

            deltaTime = dt;
        }

        /// <summary>
        /// Callback called for each active body within the simulation during body integration.
        /// </summary>
        /// <param name="bodyIndex">Index of the body being visited.</param>
        /// <param name="pose">Body's current pose.</param>
        /// <param name="localInertia">Body's current local inertia.</param>
        /// <param name="workerIndex">Index of the worker thread processing this body.</param>
        /// <param name="velocity">Reference to the body's current velocity to integrate.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IntegrateVelocity(int bodyIndex, in RigidPose pose, in BodyInertia localInertia, int workerIndex, ref BodyVelocity velocity)
        {
            //Note that we avoid accelerating kinematics. Kinematics are any body with an inverse mass of zero (so a mass of ~infinity). No force can move them.
            if (localInertia.InverseMass > 0)
            {
                // Get the gravity vector depending on if this vector is affected by gravity
                BodyHandle handle = Simulation.Bodies.ActiveSet.IndexToHandle[bodyIndex];
                SimpleBody body = BodyProps[handle];

                var gravityDt = body.HasGravity ? GravityDt : Vector3.Zero;

                float linearDampingDt = MathF.Pow(MathHelper.Clamp(1 - body.LinearDamping, 0, 1), deltaTime);
                float angularDampingDt = MathF.Pow(MathHelper.Clamp(1 - body.AngularDamping, 0, 1), deltaTime);

                velocity.Linear = (velocity.Linear + gravityDt) * linearDampingDt;
                velocity.Angular *= angularDampingDt;
            }
        }
    }
}
