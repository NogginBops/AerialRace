using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace AerialRace.Entities
{
    interface IEntity
    { }

    class EntityCollection<T> where T : struct, IEntity
    {
        public List<T> Entities = new List<T>();
    }

    static class EntityManager
    {
        public static readonly EntityCollection<CameraEntity> Cameras = new EntityCollection<CameraEntity>();
        public static readonly EntityCollection<ModelEntity> Models = new EntityCollection<ModelEntity>();
    }
}
