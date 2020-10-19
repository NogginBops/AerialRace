using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace AerialRace
{
    class Ship
    {
        public Transform Transform;

        public Mesh Model;

        public Material Material;

        public bool IsPlayerShip;

        public float MaxSpeed;

        public float CurrentSpeed;

        public Vector3 Velocity;

        public Ship(Mesh mesh, Material material)
        {
            Transform = new Transform("Ship");
            Model = mesh;
            Material = material;
        }

        // This is done per frame to update the ships position and stuff
        public void Update(float deltaTime)
        {
            // For now only the player ship is updated
            if (IsPlayerShip == false) return;

            Transform.LocalPosition += Velocity * deltaTime;
        }
    }
}
