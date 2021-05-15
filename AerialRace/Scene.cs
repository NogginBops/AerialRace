using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AerialRace
{
    // This is the "in game" representation of a scene
    class Scene
    {
        public string Name;
        // Maybe?
        public Guid SceneID;

        public List<Transform> Transforms;

        public List<StaticSetpiece> Setpieces;

        public Ship Player;

        // FIXME: Split the sky from the sky renderer
        public Sky Sky;



        public Scene(string name, Ship player, Sky sky, List<StaticSetpiece> setpieces)
        {
            Name = name;
            Setpieces = setpieces;

            Player = player;
            Sky = sky;

            Transforms = new List<Transform>();
            Transforms.Add(player.Transform);
            Transforms.AddRange(setpieces.Select(s => s.Transform));
        }

        public void Add(StaticSetpiece setpiece)
        {
            Setpieces.Add(setpiece);
            Transforms.Add(setpiece.Transform);
        }

        public void SetSun(Sky sky)
        {
            Sky = sky;
        }

        public void SetPlatyer(Ship player)
        {
            if (Player != null) Transforms.Remove(Player.Transform);

            Player = player;
            Transforms.Add(player.Transform);
        }
    }
}
