using System;
using System.Collections.Generic;
using System.Text;

namespace AerialRace.Entities
{
    abstract class System
    {
        public List<EntityRef> Entities = new List<EntityRef>();

        public static Signature CreateSignature<T1>(EntityManager manager) where T1 : struct, IComponent
        {
            int type1 = manager.GetComponentType<T1>();
            BitArray128 sig = new BitArray128();
            sig[type1] = true;
            return new Signature(sig);
        }

        public static Signature CreateSignature<T1, T2>(EntityManager manager)
            where T1 : struct, IComponent
            where T2 : struct, IComponent
        {
            int type1 = manager.GetComponentType<T1>();
            int type2 = manager.GetComponentType<T2>();
            BitArray128 sig = new BitArray128();
            sig[type1] = true;
            sig[type2] = true;
            return new Signature(sig);
        }

        public static Signature CreateSignature<T1, T2, T3>(EntityManager manager)
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
        {
            int type1 = manager.GetComponentType<T1>();
            int type2 = manager.GetComponentType<T2>();
            int type3 = manager.GetComponentType<T3>();
            BitArray128 sig = new BitArray128();
            sig[type1] = true;
            sig[type2] = true;
            sig[type3] = true;
            return new Signature(sig);
        }

        public static Signature CreateSignature<T1, T2, T3, T4>(EntityManager manager)
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
        {
            int type1 = manager.GetComponentType<T1>();
            int type2 = manager.GetComponentType<T2>();
            int type3 = manager.GetComponentType<T3>();
            int type4 = manager.GetComponentType<T4>();
            BitArray128 sig = new BitArray128();
            sig[type1] = true;
            sig[type2] = true;
            sig[type3] = true;
            sig[type4] = true;
            return new Signature(sig);
        }

        public abstract Signature GetSignature(EntityManager manager);
        public abstract void Update(EntityManager manager);
    }
}
