using AerialRace.Debugging;
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

        public Mesh Model;

        public Material Material;

        public bool IsPlayerShip;

        public float MaxSpeed;

        public float CurrentAcceleration;

        public Vector3 Velocity;

        public float ForwardArea;
        public float UpwardsArea;

        public float DragCoefficient = 0.1f;

        public float DragCoefficientStalling = 0.01f;

        public bool Stalling = false;

        public Ship(Mesh mesh, Material material)
        {
            Transform = new Transform("Ship");
            Model = mesh;
            Material = material;

            MeshRenderer = new MeshRenderer(Transform, Model, Material);

            Velocity = Vector3.Zero;// -Vector3.UnitZ;
        }

        // This is done per frame to update the ships position and stuff
        public void Update(float deltaTime)
        {
            // For now only the player ship is updated
            if (IsPlayerShip == false) return;

            // Add acceleration
            Velocity += Transform.Forward * CurrentAcceleration * deltaTime;

            float coefficient = Stalling ? DragCoefficientStalling : DragCoefficient;
            Vector3 liftDrag = Transform.Up * -Vector3.Dot(Transform.Up, Velocity) * coefficient;
            Vector3 liftDragSideways = Transform.Right * -Vector3.Dot(Transform.Right, Velocity) * coefficient * 0.5f;

            float forwardSpeed = Vector3.Dot(Transform.Forward, Velocity);
            Vector3 drag = -Transform.Forward * forwardSpeed * forwardSpeed * 0.0001f;

            Debug.Direction(Transform.LocalPosition, liftDrag, Stalling ? Color4.Red : Color4.Lime);

            Debug.Direction(Transform.LocalPosition, Velocity, Color4.Blue);

            Debug.Direction(Transform.LocalPosition, Transform.Forward * 10, Color4.Red);

            Stalling = Vector3.Dot(Transform.Forward, Velocity.Normalized()) < 0.95f;

            //Debug.Print($"{Vector3.Dot(Transform.Forward, Velocity.Normalized())}");

            // Multiply mass
            Velocity += liftDrag;
            Velocity += liftDragSideways;

            Velocity += drag;

            //float speed = Velocity.Length;
            //Vector3 drag = 0.5f * speed * speed * DragCoefficient * ForwardArea;

            Transform.LocalPosition += Velocity * deltaTime;
        }
    }
}
