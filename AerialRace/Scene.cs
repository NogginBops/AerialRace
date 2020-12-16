using System;
using System.Collections.Generic;
using System.Text;

namespace AerialRace
{
    // This is the "in game" representation of a scene
    class Scene
    {
        public string Name;

        public List<StaticSetpiece> Setpieces;

        public Scene(string name, List<StaticSetpiece> setpieces)
        {
            Name = name;
            Setpieces = setpieces;
        }
    }
}
