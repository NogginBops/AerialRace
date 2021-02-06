using OpenTK.Mathematics;
using System;

namespace AerialRace.Mathematics
{
    public struct Ray
    {
        public Vector3 Origin;
        public Vector3 Direction;

        public Ray(Vector3 origin, Vector3 direction)
        {
            Origin = origin;
            Direction = direction;
        }
    }

    public struct Cylinder
    {
        public Vector3 A;
        public Vector3 B;
        public float Radius;

        public Cylinder(Vector3 a, Vector3 b, float radius)
        {
            A = a;
            B = b;
            Radius = radius;
        }

        // https://www.iquilezles.org/www/articles/intersectors/intersectors.htm
        // TODO: Understand what is actually going on here and
        // rename all of the variables so that they actually make sense
        public static float Intersect(Ray ray, Cylinder cylinder)
        {
            var Δp = cylinder.B - cylinder.A;
            

            var ro = ray.Origin;
            var rd = ray.Direction;
            var ra = cylinder.Radius;

            Vector3 ca = cylinder.B - cylinder.A;
            Vector3 oc = ro - cylinder.A;
            
            float caca = Vector3.Dot(ca, ca);
            float card = Vector3.Dot(ca, rd);
            float caoc = Vector3.Dot(ca, oc);

            float a = caca - card * card;
            float b = caca * Vector3.Dot(oc, rd) - caoc * card;
            float c = caca * Vector3.Dot(oc, oc) - caoc * caoc - ra * ra * caca;
            float h = b * b - a * c;

            if (h < 0.0f) return -1;

            h = MathF.Sqrt(h);

            float t = (-b - h) / a;
            float y = caoc + t * card;

            if (y > 0.0f && y < caca) return t;

            t = (((y < 0.0f) ? 0 : caca) - caoc) / card;

            if (MathF.Abs(b + a * t) < h) return t;

            return -1;
        }
    }
}
