using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Fractal
{
    internal class Mandelbrot
    {
        private readonly object _byteBufferLock;
        private bool _changedSinceLastCalculation;
        private int _maxIterations;
        private readonly int _maxPixelTileSize;
        private readonly int _numThreadsPerTaskQueue = 4;
        private byte[] _pixelValues;
        private int _pixelWidth, _pixelHeight;
        private readonly object _settingsLock;
        private bool _stopThreads;
        private decimal _x, _y, _complexWidth, _complexHeight;
        private readonly ConcurrentQueue<FractalTask> _drawingTasks;
        private readonly ConcurrentQueue<FractalTask> _tasks;
        private bool _stopRendering;

        public Mandelbrot(int maxIterations, decimal x, decimal y, decimal width, decimal height, int pixelWidth = 1920,
            int pixelHeight = 1080, int maxTileSize = 64)
        {
            _x = x;
            _y = y;
            _complexWidth = width;
            _complexHeight = height;
            _settingsLock = new object();
            _byteBufferLock = new object();
            _stopThreads = false;
            _maxPixelTileSize = maxTileSize;
            _pixelWidth = pixelWidth;
            _pixelHeight = pixelHeight;
            _maxIterations = maxIterations;
            _tasks = new ConcurrentQueue<FractalTask>();
            _drawingTasks = new ConcurrentQueue<FractalTask>();
            _pixelValues = new byte[PixelFormats.Bgra32.BitsPerPixel / 8 * _pixelWidth * _pixelHeight];
            var mainThread = new Thread(MainThreadTask) { IsBackground = true, Name = "MandelbrotMainThread" };

            for (var i = 0; i < _numThreadsPerTaskQueue; i++)
            {
                var workerThread = new Thread(WorkerTask) { IsBackground = true, Name = "WorkerThread " + i };
                var drawingThread = new Thread(DrawingWorkerTask) { IsBackground = true, Name = "DrawingThread " + i }; //Both kinds of Threads do both tasks. But drawing Threads prioritize drawing and normal workers prioritize calculation
                workerThread.Start();
                drawingThread.Start();
            }
            mainThread.Start();
        }

        private void WorkerTask()
        {
            while (!_stopThreads)
            {
                if (!_stopRendering)
                {
                    FractalTask t;
                    var workToDo = _tasks.TryDequeue(out t);
                    if (!workToDo)
                    {
                        workToDo = _drawingTasks.TryDequeue(out t);
                        if (!workToDo)
                            Thread.Sleep(100);
                        else
                            Draw(t);
                    }
                    else
                    {
                        InternalCalculate(t);
                    }
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
        }

        private void DrawingWorkerTask()
        {
            while (!_stopThreads)
            {
                if (!_stopRendering)
                {
                    FractalTask t;
                    var workToDo = _drawingTasks.TryDequeue(out t);
                    if (!workToDo)
                    {
                        workToDo = _tasks.TryDequeue(out t);
                        if (!workToDo)
                            Thread.Sleep(100);
                        else
                            InternalCalculate(t);
                    }
                    else
                    {
                        Draw(t);
                    }
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
        }

        ~Mandelbrot()
        {
            _stopThreads = true;
        }


        private void MainThreadTask()
        {
            while (!_stopThreads)
            {
                lock (_settingsLock)
                {
                    if (_changedSinceLastCalculation)
                    {
                        Debug.WriteLine("Creating tasks...");
                        PartitionAndPlanTasks();
                        _changedSinceLastCalculation = false;
                    }
                }
                Thread.Sleep(100);
            }
        }

        private void PartitionAndPlanTasks()
        {
            var numTilesX = (int)Math.Ceiling(_pixelWidth / (double)_maxPixelTileSize);
            var numTilesY = (int)Math.Ceiling(_pixelHeight / (double)_maxPixelTileSize);
            var stepX = _complexWidth / _pixelWidth;
            var stepY = _complexHeight / _pixelHeight;

            for (var tileY = 0; tileY < numTilesY; tileY++)
                for (var tileX = 0; tileX < numTilesX; tileX++)
                {
                    if (_stopRendering) return;
                    var borderX = Math.Min(_pixelWidth - tileX * _maxPixelTileSize, _maxPixelTileSize);
                    var borderY = Math.Min(_pixelHeight - tileY * _maxPixelTileSize, _maxPixelTileSize);


                    var currentCornerX = tileX * _maxPixelTileSize;
                    var currentCornerY = tileY * _maxPixelTileSize;
                    var complexStartX = _x + currentCornerX * stepX;
                    var complexStartY = _y + currentCornerY * stepY;

                    var t = new FractalTask(_maxIterations, stepX, stepY, complexStartX, complexStartY,
                        new Point(currentCornerX, currentCornerY), borderX, borderY, TaskType.Calculate);
                    
                    _tasks.Enqueue(t);
                }
        }

        public void Calculate(int maxIterations, decimal x, decimal y, decimal width, decimal height, int pixelWidth,
            int pixelHeight)
        {
            lock (_settingsLock)
            {
                _maxIterations = maxIterations;
                _x = x;
                _y = y;
                _complexWidth = width;
                _complexHeight = height;
                if (_pixelWidth != pixelWidth || _pixelHeight != pixelHeight)
                    _pixelValues =
                        new byte[PixelFormats.Bgra32.BitsPerPixel / 8 * pixelWidth *
                                 pixelHeight]; //render size changed so we can't reuse our buffer

                _pixelWidth = pixelWidth;
                _pixelHeight = pixelHeight;

                _stopRendering = false;
                _changedSinceLastCalculation = true;
            }
        }


        //  public double[][,] calculate(int resultX = 1920, int resultY = 1080, int maxIterations = 1000, Image img = null)

        private void InternalCalculate(FractalTask t)
        {
            for (var y = 0; y < t.Height; y++)
            {
                var cIm = t.GetImaginaryPart(y);

                for (var x = 0; x < t.Width; x++)
                {
                    var cReal = t.GetRealPart(x);
                    decimal zReal = 0;
                    decimal zIm = 0;
                    decimal zAbs = 0;
                    var n = 0;

                    while (zAbs <= 4 && n <= t.MaxIterations)
                    {
                        var tmp = zReal;
                        zReal = zReal * zReal - zIm * zIm + cReal;
                        zIm = 2 * tmp * zIm + cIm;
                        zAbs = zReal * zReal + zIm * zIm;
                        n++;
                    }
                    if (_stopRendering) return;

                    t.SetIterations(x, y, n);

                }
            }

            t.Type = TaskType.Draw;
            if (!_stopRendering)
            {
                _drawingTasks.Enqueue(t);
            }
        }

        private void Draw(FractalTask t)
        {
            var colorBuffer = new byte[3];


            for (var y = 0; y < t.Height; y++)
                for (var x = 0; x < t.Width; x++)
                {
                    var currentPos = 4 * ((int)t.UpperLeftCorner.X + x +
                                          ((int)t.UpperLeftCorner.Y + y) * _pixelWidth);

                    if (t.GetIterations(x, y) >= _maxIterations)
                    {
                        _pixelValues[currentPos] = 0;
                        _pixelValues[currentPos + 1] = 0;
                        _pixelValues[currentPos + 2] = 0;
                        _pixelValues[currentPos + 3] = 255;
                    }
                    else
                    {
                        MapColor(Convert.ToInt32(t.GetIterations(x, y)), Math.Abs(Convert.ToDouble(t.GetImaginaryPart(y))),
                            Math.Abs(Convert.ToDouble(t.GetRealPart(x))), ref colorBuffer);
                        _pixelValues[currentPos] = colorBuffer[2];
                        _pixelValues[currentPos + 1] = colorBuffer[1];
                        _pixelValues[currentPos + 2] = colorBuffer[0];
                        _pixelValues[currentPos + 3] = 255;
                    }
                }
            if (_stopRendering) return;

            OnTileAvailable(new EventArgs());
        }

        protected virtual void OnTileAvailable(EventArgs e)
        {
            NewTileAvailable?.Invoke(this, e);
        }

        public event EventHandler NewTileAvailable;


        public BitmapSource getImage()
        {
            lock (_byteBufferLock)
            {
                var img = BitmapSource.Create(_pixelWidth, _pixelHeight, 96, 96, PixelFormats.Bgra32, null,
                    _pixelValues, _pixelWidth * PixelFormats.Bgra32.BitsPerPixel / 8);
                return img;
            }
        }


        private void MapColor(int i, double r, double c, ref byte[] colorRGB)
        {
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

            ColorFromHSV(hue, 0.8, 1.0, ref colorRGB);
        }

        public static void ColorFromHSV(double hue, double saturation, double value, ref byte[] colorRGB)
        {
            var hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            var f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            var v = Convert.ToByte(value);
            var p = Convert.ToByte(value * (1 - saturation));
            var q = Convert.ToByte(value * (1 - f * saturation));
            var t = Convert.ToByte(value * (1 - (1 - f) * saturation));


            colorRGB[0] = hi == 0 ? v : (hi == 1 ? q : (hi == 2 || hi == 3 ? p : (hi == 4 ? t : v)));
            colorRGB[1] = hi == 0 ? t : (hi == 1 ? v : (hi == 2 ? v : (hi == 3 ? q : (hi == 4 ? p : p))));
            colorRGB[2] = hi == 0 ? p : (hi == 1 ? p : (hi == 2 ? t : (hi == 3 ? v : (hi == 4 ? v : q))));
        }

        public void StopRendering()
        {
            _stopRendering = true;

            FractalTask t;

            while (!_tasks.IsEmpty)
            {
                _tasks.TryDequeue(out t);
            }
            while (!_drawingTasks.IsEmpty)
            {
                _drawingTasks.TryDequeue(out t);
            }

        }
    }
}