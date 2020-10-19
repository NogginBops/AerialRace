using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;

namespace AerialRace.Entities
{
    interface IComponent
    { }

    struct EntityRef
    {
        public int Handle;
        public int Generation;

        public EntityRef(Entity e)
        {
            Handle = e.Handle;
            Generation = e.Generation;
        }
    }

    struct Entity
    {
        public int Handle;
        public int Generation;
        //public string Name;
    }

    interface IComponentCollection
    {
        public void EntityDestroyed(EntityRef @ref);
    }

    class ComponentCollection<T> : IComponentCollection where T : struct, IComponent
    {
        public const int DefaultSize = 128;

        public T[] Components = new T[DefaultSize];
        public int[] EntityAtIndex = new int[DefaultSize];

        // FIXME!!
        // This array could be used to store an array the same size as the number of entities
        // Using this we would not need the linear search when finding the index for an entitiy.
        // This is a must for when there is a lot of entitites
        public int[] IndexToEntity = new int[0];
        public int Count;

        public ComponentCollection()
        {
            Count = 0;
        }

        public void ResizeIfNecessary(int requiredSize)
        {
            if (requiredSize < Count)
                return;

            // Here we need to resize

            int newSize = Count + (Count / 2);
            if (newSize < requiredSize) newSize = requiredSize;

            Array.Resize(ref Components, newSize);
            Array.Resize(ref EntityAtIndex, newSize);
            //Array.Resize(ref IndexToEntitiy, newSize);
        }

        public void InsertComponent(EntityRef @ref, T component)
        {
            if (TryGetEntityIndex(@ref, out int index))
            {
                // Here we replace the existing component.

                Components[index] = component;
            }
            else
            {
                // Here we insert the data at the end of the components array
                ResizeIfNecessary(Count + 1);

                EntityAtIndex[Count] = @ref.Handle;
                // FIXME: Fix IndexToEntity array.

                Components[Count] = component;

                Count++;
            }
        }

        public bool DeleteComponent(EntityRef @ref)
        {
            if (TryGetEntityIndex(@ref, out int index))
            {
                DeleteComponent(index);

                return true;
            }
            else
            {
                Trace.TraceError($"Trying to remove non-exsistent component of type '{typeof(T).GetType()}' from entity '{@ref.Handle}'");
                return false;
            }
        }

        public void EntityDestroyed(EntityRef @ref)
        {
            if (TryGetEntityIndex(@ref, out int index))
            {
                DeleteComponent(index);
            }
        }

        private void DeleteComponent(int index)
        {
            // Move the last component to this index and update the tables
            Components[index] = Components[Count - 1];
            Components[Count - 1] = default;

            EntityAtIndex[index] = EntityAtIndex[Count - 1];
            EntityAtIndex[Count - 1] = default;

            // FIXME: IndexToEntity

            Count--;
        }

        public bool TryGetComponent(EntityRef @ref, out T comp)
        {
            if (TryGetEntityIndex(@ref, out int index))
            {
                comp = Components[index];
                return true;
            }
            else
            {
                comp = default;
                return false;
            }
        }

        public ref T GetComponent(EntityRef @ref)
        {
            if (TryGetEntityIndex(@ref, out int index))
            {
                return ref Components[index];
            }
            else
            {
                throw new Exception($"The entity '{@ref}' doesn't have a component of type '{typeof(T)}'");
            }
        }

        // FIXME: This could be made O(1)
        public bool TryGetEntityIndex(EntityRef @ref, out int index)
        {
            for (int i = 0; i < Count; i++)
            {
                if (EntityAtIndex[i] == @ref.Handle)
                {
                    index = i;
                    return true;
                }
            }

            index = -1;
            return false;
        }
    }

    struct Signature
    {
        // The size of this must be the same as MaxComponents
        public BitArray128 ComponentMask;

        public Signature(BitArray128 mask)
        {
            ComponentMask = mask;
        }
    }

    class EntityManager
    {
        public const int MaxComponents = 128;

        public const int DefaultSize = 128;
        public readonly Entity[] Entities = new Entity[128];
        public int EntityCount;
        public int AliveEntities = 0;

        public readonly Signature[] EntitySignatures = new Signature[128];

        public readonly Queue<int> ReusableHandles = new Queue<int>();

        public int NextComponentType = 0;
        public readonly Dictionary<Type, (int CompType, IComponentCollection Components)> ComponentCollections = new Dictionary<Type, (int, IComponentCollection)>();

        public List<System> Systems = new List<System>();
        public Dictionary<System, Signature> SystemSignatures = new Dictionary<System, Signature>();

        public EntityManager()
        { }

        #region Systems

        public void RegisterSystem(System system)
        {
            Systems.Add(system);
            SystemSignatures.Add(system, system.GetSignature(this));
        }

        public void UpdateSystems()
        {
            foreach (var system in Systems)
            {
                system.Update(this);
            }
        }

        #endregion

        #region Entities

        public void RegisterType<T>() where T : struct, IComponent
        {
            Debug.Assert(NextComponentType + 1 < MaxComponents, $"Cannot have more than {MaxComponents}");

            ComponentCollections.Add(typeof(T), (NextComponentType, new ComponentCollection<T>()));
            NextComponentType++;
        }

        public int GetComponentType<T>() where T : struct, IComponent
        {
            if (ComponentCollections.TryGetValue(typeof(T), out var value))
            {
                return value.CompType;
            }
            else
            {
                Trace.TraceError($"Component type '{typeof(T)}' is not registered. So you can not get the type of that component.");
                return -1;
            }
        }

        public bool IsReferenceCurrent(EntityRef @ref)
        {
            return Entities[@ref.Handle].Generation == @ref.Generation;
        }

        // FIXME: Resize the entities arrays when needed!!
        public EntityRef CreateEntity()
        {
            int handle;
            if (ReusableHandles.Count > 0)
            {
                // There are handles that we can reuse
                handle = ReusableHandles.Dequeue();
            }
            else
            {
                // FIXME: Here we would need to resize

                handle = EntityCount;
                EntityCount++;
            }

            // The first free index in the array
            ref Entity entity = ref Entities[handle];

            entity.Handle = handle;
            // Rollover is what we want here
            unchecked { entity.Generation += 1; }

            AliveEntities++;

            return new EntityRef(entity);
        }

        public bool DeleteEntity(EntityRef @ref)
        {
            if (IsReferenceCurrent(@ref))
            {
                Entities[@ref.Handle].Handle = -1;

                ReusableHandles.Enqueue(@ref.Handle);

                AliveEntities--;

                return true;
            }
            else
            {
                return false;
            }
        }

        public void SetSignature(EntityRef @ref, Signature signature)
        {
            Debug.Assert(IsReferenceCurrent(@ref));

            EntitySignatures[@ref.Handle] = signature;
        }

        public Signature GetSignature(EntityRef @ref)
        {
            Debug.Assert(IsReferenceCurrent(@ref));

            return EntitySignatures[@ref.Handle];
        }

        public void AddComponent<T>(EntityRef @ref, T component) where T : struct, IComponent
        {
            Debug.Assert(IsReferenceCurrent(@ref));

            if (ComponentCollections.TryGetValue(typeof(T), out var components) == false)
            {
                Trace.TraceError($"Trying to add an unregisterd component type '{typeof(T)}'");
                return;
            }

            var componentCollection = components.Components as ComponentCollection<T>;
            componentCollection!.InsertComponent(@ref, component);

            EntitySignatures[@ref.Handle].ComponentMask[GetComponentType<T>()] = true;
        }

        public ref T GetComponent<T>(EntityRef @ref) where T : struct, IComponent
        {
            Debug.Assert(IsReferenceCurrent(@ref));

            if (ComponentCollections.TryGetValue(typeof(T), out var components) == false)
            {
                throw new Exception($"Trying to add an unregisterd component type '{typeof(T)}'");
            }

            var componentCollection = components.Components as ComponentCollection<T>;
            return ref componentCollection!.GetComponent(@ref);
        }

        #endregion
    }
}
