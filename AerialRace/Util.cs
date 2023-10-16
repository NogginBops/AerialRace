using AerialRace.Debugging;
using OpenTK;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace AerialRace
{
    static class Util
    {
        public const float D2R = MathF.PI / 180f;
        public const float R2D = 180f / MathF.PI;

        public const float Sqrt3 = 1.73205080757f;

        public static float ParseFloatFast(string str)
        {
            return ParseFloatFast(str, 0, str.Length);
        }

        // NOTE: This can be done faster than it is done atm
        public static float ParseFloatFast(string str, int offset, int length)
        {
            float negative = 1;
            if (str[offset] == '-')
            {
                negative = -1;
                offset++;
                length--;
            }

            int wholeNumber = 0;

            for (int i = 0; i < length; i++)
            {
                char c = str[offset + i];

                if (c == '.')
                {
                    offset += i + 1;
                    length -= i + 1;
                    break;
                }

                Debug.Assert(c >= '0' && c <= '9');

                wholeNumber *= 10;
                wholeNumber += c - '0';
            }

            int decimals = length;
            int fractionNumber = 0;

            for (int i = 0; i < length; i++)
            {
                char c = str[offset + i];

                Debug.Assert(c >= '0' && c <= '9' && c != '.');

                fractionNumber *= 10;
                fractionNumber += c - '0';
            }

            return negative * (wholeNumber + (fractionNumber / MathF.Pow(10, decimals)));
        }

        public static float ParseFloatFast(ReadOnlySpan<char> str)
        {
            float negative = 1;
            if (str[0] == '-')
            {
                negative = -1;
                str = str[1..];
            }

            long wholeNumber = 0;

            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];

                if (c == '.')
                {
                    str = str[(i + 1)..];
                    break;
                }

                Debug.Assert(c >= '0' && c <= '9');

                wholeNumber *= 10;
                wholeNumber += c - '0';
            }

            int decimals = str.Length;
            long fractionNumber = 0;

            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];

                Debug.Assert(c >= '0' && c <= '9' && c != '.');

                fractionNumber *= 10;
                fractionNumber += c - '0';
            }

            return negative * (wholeNumber + (fractionNumber / MathF.Pow(10, decimals)));
        }

        public static int ParseIntFast(string str, int offset, int length)
        {
            int negative = 1;
            if (str[offset] == '-')
            {
                negative = -1;
                offset += 1;
                length -= 1;
            }

            int number = 0;

            for (int i = 0; i < length; i++)
            {
                char c = str[offset + i];

                Debug.Assert(c >= '0' && c <= '9');

                number *= 10;
                number += c - '0';
            }

            return negative * number;
        }

        public static int ParseIntFast(ReadOnlySpan<char> str)
        {
            int negative = 1;
            if (str[0] == '-')
            {
                negative = -1;
                str = str[1..];
            }

            int number = 0;

            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];

                Debug.Assert(c >= '0' && c <= '9');

                number *= 10;
                number += c - '0';
            }

            return negative * number;
        }

        public static bool StartsWithFast(this string str, string start)
        {
            if (start.Length > str.Length) return false;

            for (int i = 0; i < start.Length; i++)
            {
                if (str[i] != start[i]) return false;
            }

            return true;
        }



        public static System.Numerics.Vector3 ToNumerics(this Vector3 vec3) =>
            Unsafe.As<Vector3, System.Numerics.Vector3>(ref vec3);

        public static ref System.Numerics.Vector3 AsNumerics(ref this Vector3 vec3) =>
            ref Unsafe.As<Vector3, System.Numerics.Vector3>(ref vec3);

        public static ref Vector3 AsOpenTK(ref this System.Numerics.Vector3 vec3) =>
            ref Unsafe.As<System.Numerics.Vector3, Vector3>(ref vec3);

        public static ref System.Numerics.Vector4 AsNumerics(ref this Vector4 vec4) =>
            ref Unsafe.As<Vector4, System.Numerics.Vector4>(ref vec4);

        public static ref Vector4 AsNumerics(ref this System.Numerics.Vector4 vec4) =>
            ref Unsafe.As<System.Numerics.Vector4, Vector4>(ref vec4);

        public static ref System.Numerics.Quaternion AsNumerics(ref this Quaternion quat) =>
            ref Unsafe.As<Quaternion, System.Numerics.Quaternion>(ref quat);

        public static ref Quaternion AsOpenTK(ref this System.Numerics.Quaternion quat) =>
            ref Unsafe.As<System.Numerics.Quaternion, Quaternion>(ref quat);

        public static ref Vector4 AsVector4<T>(ref this Color4<T> col) where T : IColorSpace4 =>
            ref Unsafe.As<Color4<T>, Vector4>(ref col);

        public static ref Vector3 AsVector3<T>(ref this Color4<T> col) where T : IColorSpace4 =>
            ref Unsafe.As<Color4<T>, Vector3>(ref col);

        public static ref System.Numerics.Vector4 AsNumerics4<T>(ref this Color4<T> col) where T : IColorSpace4 =>
            ref Unsafe.As<Color4<T>, System.Numerics.Vector4>(ref col);

        public static ref System.Numerics.Vector3 AsNumerics3<T>(ref this Color4<T> col) where T : IColorSpace4 =>
            ref Unsafe.As<Color4<T>, System.Numerics.Vector3>(ref col);



        public static Vector3 Abs(this Vector3 vec3) =>
            new Vector3(MathF.Abs(vec3.X), MathF.Abs(vec3.Y), MathF.Abs(vec3.Z));

        public static Vector3 Proj(this Vector3 vec, Vector3 projectOn)
        {
            return Vector3.Dot(vec, projectOn) * projectOn;
        }

        public static float LinearStep(float x, float edge0, float edge1)
        {
            x = MathHelper.Clamp((x - edge0) / (edge1 - edge0), 0, 1);
            return x;
        }

        public static float SmoothStep(float x, float edge0, float edge1)
        {
            x = MathHelper.Clamp((x - edge0) / (edge1 - edge0), 0, 1);
            return x * x * (3 - 2 * x);
        }

        public static float SmootherStep(float x, float edge0, float edge1)
        {
            x = MathHelper.Clamp((x - edge0) / (edge1 - edge0), 0, 1);
            return x * x * x * (x * (x * 6 - 15) - 10);
        }

        // FIXME: MathHelper.MapRange is wrong in this version of opentk.
        // Change to that when the function is fixed.
        public static float MapRange(float value, float valueMin, float valueMax, float resultMin, float resultMax)
        {
            float inRange = valueMax - valueMin;
            float resultRange = resultMax - resultMin;
            return resultMin + (resultRange * ((value - valueMin) / inRange));
        }

        public static float LinearDepthToNDC(float linearDepth, float nearPlane, float farPlane)
        {
            var diff = farPlane - nearPlane;
            return (farPlane + nearPlane) / diff - (2 * nearPlane * farPlane) / (linearDepth * diff);
        }

        public static float NDCDepthToLinear(float ndcDepth, float nearPlane, float farPlane)
        {
            return (2 * nearPlane * farPlane) / (farPlane + nearPlane - ndcDepth * (farPlane - nearPlane));
        }

        public static Vector3 UnprojectNDC(Vector3 ndc, ref Matrix4 inverseViewMatrix)
        {
            return Vector3.TransformPerspective(ndc, inverseViewMatrix);
        }



        public static bool Before(this DateTime before, DateTime later)
        {
            return DateTime.Compare(before, later) < 0;
        }

        public static Color4<Rgba> WithAlpha(this Color4<Rgba> color, float alpha)
        {
            return new Color4<Rgba>(color.X, color.Y, color.Z, alpha);
        }


        public static float NextFloat(this Random rand) => (float)rand.NextDouble();

        public static Vector3 NextPosition(this Random rand, Vector3 min, Vector3 max)
        {
            Vector3 pos = new Vector3(rand.NextFloat(), rand.NextFloat(), rand.NextFloat());
            return (pos * (max - min)) + min;
        }

        public static Color4<Rgba> NextColorHue(this Random rand, float saturation, float value)
        {
            return new Color4<Hsva>(rand.NextFloat(), saturation, value, 1f).ToRgba();
        }

        public static Vector3 NextOnUnitSphere(this Random rand)
        {
            float theta = 2 * MathF.PI * rand.NextFloat();
            float phi = MathF.Acos(1 - 2 * rand.NextFloat());
            float sinPhi = MathF.Sin(phi);
            float x = sinPhi * MathF.Cos(theta);
            float y = sinPhi * MathF.Sin(theta);
            float z = MathF.Cos(phi);
            return new Vector3(x, y, z);
        }


        public static Vector3 Floor(Vector3 vec)
        {
            return new Vector3(MathF.Floor(vec.X), MathF.Floor(vec.Y), MathF.Floor(vec.Z));
        }

        public static Vector3 Ceil(Vector3 vec)
        {
            return new Vector3(MathF.Ceiling(vec.X), MathF.Ceiling(vec.Y), MathF.Ceiling(vec.Z));
        }
    }
}
