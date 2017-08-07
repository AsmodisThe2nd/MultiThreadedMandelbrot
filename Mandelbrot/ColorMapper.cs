using System;

namespace Fractal
{
    interface IColorMapper
    {
        void MapColor(int iterations, int maxIterations, double realPart, double imPart, ref byte[] colorBuffer);
    }

    class ColorMappingTools
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


    class HsvColorMapper : IColorMapper
    {
        public void MapColor(int i, int maxI, double r, double c, ref byte[] colorRgb)
        {
            if (i >= maxI)
            {
                colorRgb[0] = 0;
                colorRgb[1] = 0;
                colorRgb[2] = 0;
                return;

            }
            double di = i;
            double zn;
            double hue;

            zn = Math.Sqrt(r + c);
            if (zn <= 0)
                zn = 0.1;

            hue = di + 1.0 - Math.Abs(Math.Log(zn)) / Math.Log(2.0); // 2 is escape radius
            hue = 0.95 + 20.0 * hue; // adjust to make it prettier
            // the hsv function expects values from 0 to 360
            while (hue > 360.0)
                hue -= 360.0;
            while (hue < 0.0)
                hue += 360.0;

            ColorMappingTools.ColorFromHsv(hue, 0.8, 1.0, ref colorRgb);
        }
    }
}