using AerialRace.Loading;
using AerialRace.RenderData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AerialRace.Editor
{
    static class EditorResources
    {
        // FIXME: Move this to some editor thing!!
        public static readonly Texture PointLightIcon;

        static EditorResources()
        {
            PointLightIcon = TextureLoader.LoadRgbaImage("Builtin UV Test", "./Editor/Icons/PointLightGizmoIcon.png", true, false);
        }
    }
}
