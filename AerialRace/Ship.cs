using AerialRace.Debugging;
using AerialRace.Loading;
using AerialRace.Physics;
using AerialRace.RenderData;
using ImGuiNET;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace AerialRace
{
    class Ship
    {
        public Transform Transform;

        public MeshRenderer MeshRenderer;
        public TrailRenderer LeftTrailRenderer;
        public TrailRenderer RightTrailRenderer;

        public Mesh Model;

        public Trail LeftTrail;
        public Trail RightTrail;

        public IConvexCollider Collider;
        public RigidBody RigidBody;

        public Material Material;

        public bool IsPlayerShip;

        public float MaxSpeed;

        public float AccelerationTimer;
        public float AccelerationTime = 0.5f;
        public float CurrentAcceleration;
        public float MaxAcceleration = 1;

        public float ForwardStallSpeed = 40;

        public Vector3 Velocity;

        public float ForwardArea;
        public float UpwardsArea;

        public float DragCoefficient = 0.1f;

        public float DragCoefficientStalling = 0.01f;

        public bool Stalling = false;

        public Ship(string name, MeshData meshData, Material material, Material trailMaterial)
        {
            Transform = new Transform("Ship", new Vector3(0, 50f, 0));
            Model = RenderDataUtil.CreateMesh(name, meshData);
            Material = material;

            MeshRenderer = new MeshRenderer(Transform, Model, Material);

            float trailLength = 5f;
            int trailSegments = 2000;
            LeftTrail  = new Trail("Ship trail left", trailLength, 0.1f, trailSegments);
            LeftTrailRenderer = new TrailRenderer(LeftTrail, trailMaterial);
            RightTrail = new Trail("Ship trail right", trailLength, 0.1f, trailSegments);
            RightTrailRenderer = new TrailRenderer(RightTrail, trailMaterial);

            SimpleMaterial playerPhysMat = new SimpleMaterial()
            {
                FrictionCoefficient = 1f,
                MaximumRecoveryVelocity = 2f,
                SpringSettings = new BepuPhysics.Constraints.SpringSettings(30, 1),
            };
            
            SimpleBody playerBodyProp = new SimpleBody()
            {
                HasGravity = true,
                LinearDamping = 0.3f,
                AngularDamping = 0.8f,
            };

            Collider = new MeshCollider(meshData);
            //Collider = new BoxCollider(new Vector3(5f, 1f, 5f));
            RigidBody = new RigidBody(Collider, Transform, 200f, playerPhysMat, playerBodyProp);

            // Never let this body go to sleep
            RigidBody.Body.Activity.SleepThreshold = -1;

            Velocity = Vector3.Zero;// -Vector3.UnitZ;
        }

        // This is done per frame to update the ships position and stuff
        public void Update(float deltaTime)
        {
            RigidBody.UpdateTransform(Transform);
            Transform.UpdateMatrices();

            // For now only the player ship is updated
            if (IsPlayerShip == false) return;

            Velocity = RigidBody.Body.Velocity.Linear.ToOpenTK();

            var forwardVel = Transform.Forward * Vector3.Dot(Velocity, Transform.Forward);
            var cosVertVel = Vector3.Dot(Velocity, Transform.Up);
            var verticalVel = Transform.Up * cosVertVel;
            var lateralVel = Transform.Right * Vector3.Dot(Velocity, Transform.Right);

            const float VerticalLiftFactor = 1f;
            const float LateralLiftFactor = 0.2f;
            const float density = 1.2041f;
            const float verticalArea = 10f;
            const float lateralArea = 1f;
            //var verticalLift = -verticalVel * VerticalLiftFactor;
            var verticalLift = VerticalLiftFactor * ((density * -verticalVel) / 2f) * verticalArea;
            var lateralLift = LateralLiftFactor * ((density * -lateralVel) / 2f) * lateralArea;

            bool stalling = false;
            if (forwardVel.LengthSquared < ForwardStallSpeed * ForwardStallSpeed)
            {
                verticalLift *= 0.1f;
                stalling = true;
            }

            RigidBody.Body.ApplyLinearImpulse((verticalLift).ToNumerics());
            Debug.Direction(Transform.LocalPosition, verticalLift/RigidBody.Mass, Color4.Lime);

            RigidBody.Body.ApplyLinearImpulse((lateralLift * deltaTime).ToNumerics());
            Debug.Direction(Transform.LocalPosition, lateralLift, Color4.Lime);

            Debug.Direction(Transform.LocalPosition, Velocity, Color4.Blue);

            //var drag = Velocity * -0.01f;
            //RigidBody.Body.ApplyLinearImpulse((drag).ToNumerics());

            RigidBody.Body.ApplyLinearImpulse((Transform.Forward * CurrentAcceleration * RigidBody.Mass).ToNumerics());

            //Debug.Direction(Transform.LocalPosition, Velocity, Color4.Blue);

            float separation = 4.5f;
            LeftTrail.Update(-Transform.Right * separation + Transform.WorldPosition, deltaTime);
            RightTrail.Update(Transform.Right * separation + Transform.WorldPosition, deltaTime);

            {
                if (ImGui.Begin("Plane Stats", ImGuiWindowFlags.NoFocusOnAppearing))
                {
                    ImGui.Text($"Acceleration: {ReadablePercentage(AccelerationTimer / AccelerationTime)}");
                    ImGui.Text($"Stalling: {stalling}");
                    ImGui.Text($"Velocity: ({ReadableString(Velocity)}) - {Velocity.Length}");
                    ImGui.Text($"Vertical Velocity: ({ReadableString(verticalVel)}) - {ReadablePercentage(verticalVel.Length / Velocity.Length)}");
                    ImGui.Text($"Lateral Velocity: ({ReadableString(lateralVel)}) - {ReadablePercentage(lateralVel.Length / Velocity.Length)}");
                }
                ImGui.End();
            }


            static string ReadablePercentage(float f)
            {
                return $"{f * 100:000.}%";
            }

            static string ReadableString(Vector3 vec3)
            {
                return $"{vec3.X:0.000}, {vec3.Y:0.000}, {vec3.Z:0.000}";
            }

            return;

            // Add acceleration
            Velocity += Transform.Forward * CurrentAcceleration * deltaTime;

            float coefficient = Stalling ? DragCoefficientStalling : DragCoefficient;
            Vector3 liftDrag = Transform.Up * -Vector3.Dot(Transform.Up, Velocity) * coefficient;
            Vector3 liftDragSideways = Transform.Right * -Vector3.Dot(Transform.Right, Velocity) * coefficient * 0.5f;

            float forwardSpeed = Vector3.Dot(Transform.Forward, Velocity);
            //Vector3 drag = -Transform.Forward * forwardSpeed * forwardSpeed * 0.0001f;

            Debug.Direction(Transform.LocalPosition, liftDrag, Stalling ? Color4.Red : Color4.Lime);

            Debug.Direction(Transform.LocalPosition, Velocity, Color4.Blue);

            Debug.Direction(Transform.LocalPosition, Transform.Forward * 10, Color4.Red);

            Stalling = Vector3.Dot(Transform.Forward, Velocity.Normalized()) < 0.95f;

            //Debug.Print($"{Vector3.Dot(Transform.Forward, Velocity.Normalized())}");

            // Multiply mass
            Velocity += liftDrag;
            Velocity += liftDragSideways;

            //Velocity += drag;

            //float speed = Velocity.Length;
            //Vector3 drag = 0.5f * speed * speed * DragCoefficient * ForwardArea;

            Transform.LocalPosition += Velocity * deltaTime;
        }
    }
}
