using System;
using System.Configuration;

namespace Fractal.ColorMappers
{
    abstract class AbstractColorMapper
    {
        public event EventHandler redrawWanted;


        public abstract void MapColor(int iterations, int maxIterations, double realPart, double imPart, ref byte[] colorBuffer);

        protected virtual void OnRedrawWanted()
        {
            redrawWanted?.Invoke(this, EventArgs.Empty);
        }
    }

}