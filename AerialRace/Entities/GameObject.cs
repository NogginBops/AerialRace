using AerialRace.Debugging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AerialRace.Entities
{
    [Flags]
    enum Components : int
    {
        None = 0,
        Name = 1 << 0,
        Mesh = 1 << 1,
        Renderer = 1 << 2,
    }

    class World
    {
        const int INITIAL_NAMESPACE_SIZE = 1000;
        public int NamespaceSize = INITIAL_NAMESPACE_SIZE;

        public List<GameObject> Objects = new List<GameObject>();

        public Queue<int> IDQueue =  new Queue<int>();
        public int NextID = 0;

        public SparseList<Name> Names = new SparseList<Name>(INITIAL_NAMESPACE_SIZE);
        public SparseList<MeshRef> Meshes = new SparseList<MeshRef>(INITIAL_NAMESPACE_SIZE);
        public SparseList<Renderer> Renderers = new SparseList<Renderer>(INITIAL_NAMESPACE_SIZE);

        public World()
        {

        }

        public GameObject CreateEntity(string name)
        {
            int id;
            if (IDQueue.Count > 0)
                id = IDQueue.Dequeue();
            else id = NextID++;

            GameObject obj = new GameObject();
            obj.ID = id;
            obj.Components = Components.None;
            obj.Transform = new Transform(name);

            Objects.Add(obj);

            return obj;
        }

        public void AddComponent<T>(GameObject obj, ref T values)
            where T : struct
        {
            if (typeof(T) == typeof(Name))
            {
                obj.Components |= Components.Name;
                ref Name name = ref Names.Allocate(obj.ID);
                name = Unsafe.As<T, Name>(ref values);
            }
            else if (typeof(T) == typeof(MeshRef))
            {
                obj.Components |= Components.Mesh;
                ref MeshRef mesh = ref Meshes.Allocate(obj.ID);
                mesh = Unsafe.As<T, MeshRef>(ref values);
            }
            else if (typeof(T) == typeof(Renderer))
            {
                obj.Components |= Components.Mesh;
                ref Renderer mesh = ref Renderers.Allocate(obj.ID);
                mesh = Unsafe.As<T, Renderer>(ref values);
            }
            else Debug.Assert($"Invalid component type '{typeof(T).Name}'.");
        }

        public ref T GetComponent<T>(GameObject obj)
        {
            if (typeof(T) == typeof(Name))
            {
                ref Name name = ref Names.TryGetOrNull(obj.ID);
                if (Unsafe.IsNullRef(ref name))
                    throw new InvalidOperationException($"Obj {obj.ID} does not have a component of type '{typeof(T).Name}'.");
                else return ref Unsafe.As<Name, T>(ref name);
            }
            else if (typeof(T) == typeof(MeshRef))
            {
                ref MeshRef mesh = ref Meshes.TryGetOrNull(obj.ID);
                if (Unsafe.IsNullRef(ref mesh))
                    throw new InvalidOperationException($"Obj {obj.ID} does not have a component of type '{typeof(T).Name}'.");
                else return ref Unsafe.As<MeshRef, T>(ref mesh);
            }
            else if (typeof(T) == typeof(Renderer))
            {
                ref Renderer renderer = ref Renderers.TryGetOrNull(obj.ID);
                if (Unsafe.IsNullRef(ref renderer))
                    throw new InvalidOperationException($"Obj {obj.ID} does not have a component of type '{typeof(T).Name}'.");
                else return ref Unsafe.As<Renderer, T>(ref renderer);
            }
            else
            {
                Debug.Assert($"Invalid component type '{typeof(T).Name}'.");
                return ref Unsafe.NullRef<T>();
            }
        }
    }

    class GameObject
    {
        public int ID;
        public Components Components;
        public Transform Transform;
        
    }

    struct Name
    {
        public string NameStr;

        public Name(string name)
        {
            NameStr = name;
        }
    }

    struct MeshRef
    {
        public Mesh Mesh;
    }
}
