using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace AerialRace.Entities.Components
{
    // This compoment is special as it will need to be processed

    struct Transform : IComponent
    {
        public Quaternion LocalRotation;
        public Vector3 LocalPosition;
        public Vector3 LocalScale;
    }

    struct LocalToParent : IComponent
    {
        public Matrix4 ToParent;
    }

    struct LocalToWorld : IComponent
    {
        public Matrix4 ToWorld;
    }

    struct Parent : IComponent
    {
        public EntityRef ParentRef;
    }
}
