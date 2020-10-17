using System;
using System.Collections.Generic;
using System.Text;

namespace AerialRace.Entities
{
    struct CameraEntity : IEntity
    {
        public Transform Transform;
        public Camera Camera;

        public float MovementSpeed;
    }
}
