using System;
using System.Collections.Generic;
using System.Text;

namespace AerialRace
{
    static class Util
    {
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

                if (c < '0' || c > '9')
                {
                    return float.NaN;
                }

                wholeNumber *= 10;
                wholeNumber += c - '0';
            }

            int decimals = length;
            int fractionNumber = 0;

            for (int i = 0; i < length; i++)
            {
                char c = str[offset + i];

                if (c < '0' || c > '9' || c == '.')
                {
                    return float.NaN;
                }

                fractionNumber *= 10;
                fractionNumber += c - '0';
            }

            return negative * (wholeNumber + (fractionNumber / (float)Math.Pow(10, decimals)));
        }

        // TODO: We might want some error handling!
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

                if (c < '0' || c > '9')
                {
                    // TODO: Proper error handling?!
                    return -1;
                }

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
    }
}
