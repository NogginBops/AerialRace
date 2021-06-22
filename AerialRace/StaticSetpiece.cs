using AerialRace.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AerialRace
{
    class StaticSetpiece : SelfCollection<StaticSetpiece>
    {
        // Perf: This could be optimized to a constant matrix
        public Transform Transform;

        public Mesh Mesh;

        public Material Material;
        public MeshRenderer Renderer;

        public ICollider Collider;
        public StaticCollider StaticCollider;

        public StaticSetpiece(Transform transform, Mesh mesh, Material material, ICollider collider, SimpleMaterial physMaterial) : base()
        {
            Transform = transform;
            Transform.UpdateMatrices();

            Mesh = mesh;

            Material = material;
            Renderer = new MeshRenderer(transform, mesh, material);

            Collider = collider;
            StaticCollider = new StaticCollider(Collider, transform.WorldPosition, physMaterial);
        }

    }
}
