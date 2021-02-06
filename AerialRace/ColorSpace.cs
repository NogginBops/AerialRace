using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AerialRace
{
    record Primaries(Vector2d R_xy, Vector2d G_xy, Vector2d B_xy)
    {
        public static readonly Primaries sRGB = new Primaries(
                new Vector2d(0.64, 0.33),
                new Vector2d(0.30, 0.60),
                new Vector2d(0.15, 0.06)
            );

        public static readonly Primaries AP0 = new Primaries(
                new Vector2d(0.73470, 0.26530),
                new Vector2d(0.00000, 1.00000),
                new Vector2d(0.00010,-0.07700)
            );

        public static readonly Primaries AP1 = new Primaries(
                new Vector2d(0.713, 0.293),
                new Vector2d(0.165, 0.830),
                new Vector2d(0.128, 0.044)
            );
    }

    record ColorSpace(Primaries Primaries, Vector2d W_xy)
    {
        public static readonly Vector2d A = new Vector2d(0.44758, 0.40745);
        public static readonly Vector2d D50 = new Vector2d(0.34567, 0.35850);
        public static readonly Vector2d D55 = new Vector2d(0.33242, 0.34743);
        public static readonly Vector2d D65 = new Vector2d(0.31271, 0.32902);
        public static readonly Vector2d D75 = new Vector2d(0.29902, 0.31485);
        public static readonly Vector2d E = new Vector2d(1/3, 1/3);
        public static readonly Vector2d AP0_White = new Vector2d(0.32168, 0.33767);
        public static readonly Vector2d AP1_White = new Vector2d(0.32168, 0.33767);

        public static readonly ColorSpace Linear_sRGB = new ColorSpace(
                Primaries.sRGB,
                D65
            );

        public static readonly ColorSpace ACES2065_1 = new ColorSpace(
                Primaries.AP0,
                AP0_White
            );

        public static readonly ColorSpace ACEScg = new ColorSpace(
                Primaries.AP0,
                AP1_White
            );

        public static Matrix3d CalcConvertionMatrix(ColorSpace from, ColorSpace to)
        {
            var fromToCIEXYZ = from.CalcSpaceToCIE_XYZ();
            var toToCIEXYZ = to.CalcSpaceToCIE_XYZ();
            var CIEXYZToto = toToCIEXYZ.Inverted();

            return fromToCIEXYZ * CIEXYZToto;
        }

        public Matrix3d CalcSpaceToCIE_XYZ()
        {
            static Vector3d To_xyz(Vector2d xy) => new Vector3d(xy.X, xy.Y, 1 - xy.X - xy.Y);

            Vector3d R_xyz = To_xyz(Primaries.R_xy);
            Vector3d G_xyz = To_xyz(Primaries.G_xy);
            Vector3d B_xyz = To_xyz(Primaries.B_xy);
            Vector3d W_xyz = To_xyz(W_xy);

            Vector3d W_XYZ = W_xyz / W_xyz.Y;

            Matrix3d RGB_Mat = new Matrix3d(R_xyz, G_xyz, B_xyz);
            Matrix3d RGB_Mat_inv = RGB_Mat.Inverted();

            Vector3d scale = W_XYZ * RGB_Mat_inv;

            Matrix3d M = RGB_Mat;
            M.Row0 *= scale.X;
            M.Row1 *= scale.Y;
            M.Row2 *= scale.Z;

            return M;
        }
    }
}
