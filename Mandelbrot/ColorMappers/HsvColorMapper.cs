using System;

namespace Fractal.ColorMappers
{
    internal class HsvColorMapper : AbstractColorMapper
    {

        public override void MapColor(int i, int maxI, double r, double c, ref byte[] colorRgb)
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