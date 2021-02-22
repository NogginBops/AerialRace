using AerialRace.Debugging;
using AerialRace.Loading;
using AerialRace.Physics;
using AerialRace.RenderData;
using ImGuiNET;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
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

        public Camera Camera;

        public Mesh Model;

        public Trail LeftTrail;
        public Trail RightTrail;

        public IConvexCollider Collider;
        public RigidBody RigidBody;

        public Material Material;

        public bool IsPlayerShip;

        public float MaxSpeed;

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

            Camera = new Camera(100, 0.1f, 10_000f, new Color4(1f, 0, 1f, 1f));
            Camera.Transform.Name = "Player Camera";
            Camera.Transform.LocalPosition = CameraOffset;

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
                LinearDamping = 0.03f,
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
                    ImGui.Text($"Acceleration: {ReadablePercentage(AccelerationPercent)}%");
                    ImGui.Text($"Boosting: {BoostTimer}s left");
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


        public float BoostTimer = 0f;
        public float BoostTime = 0.4f;
        public float BoostForce = 500000;

        public float AccelerationTime = 0.5f;
        public float DeccelerationTime = 0.2f;
        public float MaxAcceleration = 1;

        public float AccelerationPercent = 0f;
        public float CurrentAcceleration;
        public void UpdateControls(KeyboardState keyboard, float deltaTime)
        {
            float pitchForce = 1000;
            float yawForce = 2500;
            float rollForce = 2000;

            float pitch = GetAxis(keyboard, Keys.W, Keys.S) * pitchForce;
            float yaw = GetAxis(keyboard, Keys.A, Keys.D) * yawForce;
            float roll = GetAxis(keyboard, Keys.Q, Keys.E) * rollForce;

            pitch += GetAxis(keyboard, Keys.Up, Keys.Down) * pitchForce * 2;

            var pitchMoment = Transform.LocalDirectionToWorld((-pitch * MathHelper.TwoPi, 0, 0));
            var yawMoment = Transform.LocalDirectionToWorld((0, yaw * MathHelper.TwoPi, 0));
            var rollMoment = Transform.LocalDirectionToWorld((0, 0, roll * MathHelper.TwoPi));

            RigidBody.Body.ApplyAngularImpulse(pitchMoment.ToNumerics() * deltaTime);
            RigidBody.Body.ApplyAngularImpulse(yawMoment.ToNumerics() * deltaTime);
            RigidBody.Body.ApplyAngularImpulse(rollMoment.ToNumerics() * deltaTime);

            float accelerationDelta = -(1f / DeccelerationTime) * deltaTime;
            if (keyboard.IsKeyDown(Keys.Space))
            {
                accelerationDelta = (1f / AccelerationTime) * deltaTime;
            }

            if (keyboard.IsKeyPressed(Keys.LeftShift) && BoostTimer == 0)
            {
                BoostTimer = BoostTime;
            }

            if (BoostTimer > 0)
            {
                RigidBody.Body.ApplyLinearImpulse(Transform.Forward.ToNumerics() * BoostForce * deltaTime);

                BoostTimer -= deltaTime;
                if (BoostTimer < 0) BoostTimer = 0;
            }

            CurrentAcceleration += accelerationDelta;
            CurrentAcceleration = MathHelper.Clamp(CurrentAcceleration, 0, MaxAcceleration);

            static float GetAxis(KeyboardState keyboard, Keys positive, Keys negative)
            {
                float axis = 0f;
                if (keyboard.IsKeyDown(positive)) axis += 1;
                if (keyboard.IsKeyDown(negative)) axis -= 1;
                return axis;
            }
        }

        public Vector3 CameraOffset = new Vector3(0, 6f, 37f);
        public Quaternion RotationOffset = Quaternion.FromAxisAngle(Vector3.UnitX, MathHelper.DegreesToRadians(-20f));

        public float MouseInfluenceTimeout = 0f;
        public float MouseSpeedX = 0.2f;
        public float MouseSpeedY = 0.2f;
        public float CameraMinY = -75f;
        public float CameraMaxY = 75f;

        public void UpdateCamera(MouseState mouse, float deltaTime)
        {
            // Move the camera
            if (mouse.IsButtonDown(MouseButton.Right))
            {
                MouseInfluenceTimeout = 1f;

                var delta = mouse.Delta;

                Camera.YAxisRotation += -delta.X * MouseSpeedX * deltaTime;
                Camera.XAxisRotation += -delta.Y * MouseSpeedY * deltaTime;
                Camera.XAxisRotation = MathHelper.Clamp(Camera.XAxisRotation, CameraMinY * Util.D2R, CameraMaxY * Util.D2R);
            }

            MouseInfluenceTimeout -= deltaTime;
            if (MouseInfluenceTimeout < 0.001f)
            {
                MouseInfluenceTimeout = 0;

                Camera.XAxisRotation -= Camera.XAxisRotation * deltaTime * 3f;
                if (Math.Abs(Camera.XAxisRotation) < 0.001f) Camera.XAxisRotation = 0;
                Camera.YAxisRotation -= Camera.YAxisRotation * deltaTime * 3f;
                if (Math.Abs(Camera.YAxisRotation) < 0.001f) Camera.YAxisRotation = 0;
            }

            var targetPos = Transform.LocalPosition;

            Quaternion rotation =
                    Transform.LocalRotation * RotationOffset *
                    Quaternion.FromAxisAngle(Vector3.UnitY, Camera.YAxisRotation) *
                    Quaternion.FromAxisAngle(Vector3.UnitX, Camera.XAxisRotation)
                    ;

            targetPos = targetPos + (rotation * new Vector3(0, 0, CameraOffset.Length));

            Camera.Transform.LocalPosition = Vector3.Lerp(Camera.Transform.LocalPosition, targetPos, 30f * deltaTime);

            Camera.Transform.LocalRotation = Quaternion.Slerp(Camera.Transform.LocalRotation, rotation, 5f * deltaTime);
        }
    }
}
