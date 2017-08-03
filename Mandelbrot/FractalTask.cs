using System;
using System.Security.Cryptography.X509Certificates;
using System.Windows;

namespace Fractal
{
    enum TaskType
    {
        Calculate,
        Draw
    }

    class FractalTask
    {
        private decimal _stepSizeX;
        private decimal _stepSizeY;
        private decimal _complexStartX;
        private decimal _complexStartY;
        private Point _upperLeftCorner;
        private int _width;
        private int _height;
        private int _maxIterations;
        private TaskType _type;
        
        private int[,] _results; //iterations, real, im

        public FractalTask(int maxIterations, decimal stepSizeX, decimal stepSizeY, decimal complexStartX, decimal complexStartY, Point upperLeftCorner, int width, int height, TaskType type)
        {
            this._stepSizeX = stepSizeX;
            this._stepSizeY = stepSizeY;
            this._complexStartX = complexStartX;
            this._complexStartY = complexStartY;
            this._upperLeftCorner = upperLeftCorner;
            this._width = width;
            this._height = height;
            this._type = type;
            this._maxIterations = maxIterations;
            _results = new int[width, height];
        }


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
            if (x < _width && x >= 0)
                return _complexStartX + _stepSizeX * x;
            else
            {
                throw new IndexOutOfRangeException();
            }
        }

        public int MaxIterations
        {
            get => _maxIterations;
            set => _maxIterations = value;
        }

        public decimal GetImaginaryPart(int y)
        {
            if (y < _height && y >= 0)
                return _complexStartY + _stepSizeY * y;
            else
            {
                throw  new IndexOutOfRangeException();
            }
        }


        public decimal StepSizeX
        {
            get => _stepSizeX;
            set => _stepSizeX = value;
        }

        public decimal StepSizeY
        {
            get => _stepSizeY;
            set => _stepSizeY = value;
        }

        public TaskType Type {
            get => _type;
            set => _type = value;
        }

        public decimal ComplexStartX {
            get => _complexStartX;
            set => _complexStartX = value;
        }

        public decimal ComplexStartY {
            get => _complexStartY;
            set => _complexStartY = value;
        }

        public Point UpperLeftCorner {
            get => _upperLeftCorner;
            set => _upperLeftCorner = value;
        }

        public int Width {
            get => _width;
            set => _width = value;
        }

        public int Height {
            get => _height;
            set => _height = value;
        }
    }
}
