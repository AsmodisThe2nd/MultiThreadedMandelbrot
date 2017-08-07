using System;
using System.Windows;

namespace Fractal
{
    internal enum TaskType
    {
        Calculate,
        Draw
    }

    internal class FractalTask
    {
        private readonly int[,] _results; //iterations, real, im

        public FractalTask(int maxIterations, decimal stepSizeX, decimal stepSizeY, decimal complexStartX,
            decimal complexStartY, Point upperLeftCorner, int width, int height, TaskType type)
        {
            StepSizeX = stepSizeX;
            StepSizeY = stepSizeY;
            ComplexStartX = complexStartX;
            ComplexStartY = complexStartY;
            UpperLeftCorner = upperLeftCorner;
            Width = width;
            Height = height;
            Type = type;
            MaxIterations = maxIterations;
            _results = new int[width, height];
        }

        public int MaxIterations { get; set; }


        public decimal StepSizeX { get; set; }

        public decimal StepSizeY { get; set; }

        public TaskType Type { get; set; }

        public decimal ComplexStartX { get; set; }

        public decimal ComplexStartY { get; set; }

        public Point UpperLeftCorner { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }


        public void SetIterations(int x, int y, int result)
        {
            _results[x, y] = result;
        }

        public int GetIterations(int x, int y)
        {
            return _results[x, y];
        }

        public decimal GetRealPart(int x)
        {
            if (x < Width && x >= 0)
                return ComplexStartX + StepSizeX * x;
            throw new IndexOutOfRangeException();
        }

        public decimal GetImaginaryPart(int y)
        {
            if (y < Height && y >= 0)
                return ComplexStartY - StepSizeY * y;
            throw new IndexOutOfRangeException();
        }
    }
}