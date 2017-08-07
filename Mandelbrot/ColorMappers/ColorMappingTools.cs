using System;

namespace Fractal.ColorMappers
{
    internal class ColorMappingTools
    {
        public static void ColorFromHsv(double hue, double saturation, double value, ref byte[] colorRgb)
        {
            var hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            var f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            var v = Convert.ToByte(value);
            var p = Convert.ToByte(value * (1 - saturation));
            var q = Convert.ToByte(value * (1 - f * saturation));
            var t = Convert.ToByte(value * (1 - (1 - f) * saturation));


            colorRgb[0] = hi == 0 ? v : (hi == 1 ? q : (hi == 2 || hi == 3 ? p : (hi == 4 ? t : v)));
            colorRgb[1] = hi == 0 ? t : (hi == 1 ? v : (hi == 2 ? v : (hi == 3 ? q : (hi == 4 ? p : p))));
            colorRgb[2] = hi == 0 ? p : (hi == 1 ? p : (hi == 2 ? t : (hi == 3 ? v : (hi == 4 ? v : q))));
        }
    }
}