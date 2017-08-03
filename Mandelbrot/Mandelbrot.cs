using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Fractal
{
    class Mandelbrot
    {
        private decimal _x, _y, _complexWidth, _complexHeight;
        int _pixelWidth = 0, _pixelHeight = 0;
        bool _changedSinceLastCalculation = false;
        private int _maxIterations;
        private bool _stopThreads;

        private object _settingsLock;
        private object _byteBufferLock;

        private int _maxPixelTileSize;
        private Byte[] _pixelValues;

        private ConcurrentQueue<FractalTask> tasks;
        private ConcurrentQueue<FractalTask> drawingTasks;

        private int _numThreadsPerTaskQueue = 4;

        private int _oldPixelWidth;
        private int _oldPixelHeight;


        public Mandelbrot(int maxIterations, decimal x, decimal y, decimal width, decimal height, int pixelWidth = 1920, int pixelHeight = 1080, int maxTileSize = 64)
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
            tasks = new ConcurrentQueue<FractalTask>();
            drawingTasks = new ConcurrentQueue<FractalTask>();
            _pixelValues = new Byte[(PixelFormats.Bgra32.BitsPerPixel / 8) * _pixelWidth * _pixelHeight];
            Thread mainThread = new Thread(MainThreadTask) { IsBackground = true, Name = "MandelbrotMainThread" };

            for (int i = 0; i < _numThreadsPerTaskQueue; i++)
            {
                Thread workerThread = new Thread(WorkerTask) { IsBackground = true, Name = "WorkerThread " + i };
                Thread drawingThread = new Thread(DrawingWorkerTask) { IsBackground = true, Name = "DrawingThread " + i };
                workerThread.Start();
                drawingThread.Start();
            }
            mainThread.Start();
        }

        private void WorkerTask()
        {
            while (!_stopThreads)
            {
                FractalTask t;
                bool workToDo = tasks.TryDequeue(out t);
                if (!workToDo)
                {

                    workToDo = drawingTasks.TryDequeue(out t);
                    if (!workToDo)
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                    else
                    {
                        Draw(t);
                    }
                }
                else
                {
                    InternalCalculate(t);
                }
            }
        }

        private void DrawingWorkerTask()
        {
            while (!_stopThreads)
            {
                FractalTask t;
                bool workToDo = drawingTasks.TryDequeue(out t);
                if (!workToDo)
                {
                    workToDo = tasks.TryDequeue(out t);
                    if (!workToDo)
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                    else
                    {
                        InternalCalculate(t);
                    }
                }
                else
                {
                    Draw(t);
                }

            }
        }

        ~Mandelbrot()
        {
            _stopThreads = true;
        }


        void MainThreadTask()
        {
            while (!_stopThreads)
            {

                lock (_settingsLock)
                {
                    if (_changedSinceLastCalculation)
                    {
                        System.Diagnostics.Debug.WriteLine("Creating tasks...");
                        PartitionAndPlanTasks();
                        _changedSinceLastCalculation = false;
                    }
                }
                Thread.Sleep(100);
            }
        }

        private void PartitionAndPlanTasks()
        {
            int numTilesX = (int)Math.Ceiling((double)_pixelWidth / (double)_maxPixelTileSize);
            int numTilesY = (int)Math.Ceiling((double)_pixelHeight / (double)_maxPixelTileSize);
            decimal stepX = _complexWidth / _pixelWidth;
            decimal stepY = _complexHeight / _pixelHeight;

            for (int tileX = 0; tileX < numTilesX; tileX++)
            {
                for (int tileY = 0; tileY < numTilesY; tileY++)
                {
                    int borderX = (int)Math.Min(_pixelWidth - tileX * _maxPixelTileSize, _maxPixelTileSize);
                    int borderY = (int)Math.Min(_pixelHeight - tileY * _maxPixelTileSize, _maxPixelTileSize);


                    int currentCornerX = tileX * _maxPixelTileSize;
                    int currentCornerY = tileY * _maxPixelTileSize;
                    decimal complexStartX = _x + currentCornerX * stepX;
                    decimal complexStartY = _y + currentCornerY * stepY;

                    FractalTask t = new FractalTask(_maxIterations, stepX, stepY, complexStartX, complexStartY, new Point(currentCornerX, currentCornerY), borderX, borderY, TaskType.Calculate);
                    tasks.Enqueue(t);

                }
            }
        }

        public void Calculate(int maxIterations, decimal x, decimal y, decimal width, decimal height, int pixelWidth, int pixelHeight)
        {
            lock (_settingsLock)
            {
                _maxIterations = maxIterations;
                _x = x;
                _y = y;
                _complexWidth = width;
                _complexHeight = height;
                if (_pixelWidth != pixelWidth || _pixelHeight != pixelHeight)
                {
                    _pixelValues = new Byte[(PixelFormats.Bgra32.BitsPerPixel / 8) * pixelWidth * pixelHeight]; //render size changed so we can't reuse our buffer
                }

                _pixelWidth = pixelWidth;
                _pixelHeight = pixelHeight;


                _changedSinceLastCalculation = true;
            }
        }




        //  public double[][,] calculate(int resultX = 1920, int resultY = 1080, int maxIterations = 1000, Image img = null)

        private void InternalCalculate(FractalTask t)
        {
            for (int y = 0; y < t.Height; y++)
            {
                var cIm = t.GetImaginaryPart(y);

                for (int x = 0; x < t.Width; x++)
                {

                    decimal cReal = t.GetRealPart(x);
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
                    t.SetIterations(x, y, n);
                }
            }

            t.Type = TaskType.Draw;
            drawingTasks.Enqueue(t);
        }

        private void Draw(FractalTask t)
        {
            Byte[] colorBuffer = new Byte[3];


            for (int y = 0; y < t.Height; y++)
            {
                for (int x = 0; x < t.Width; x++)
                {
                    int currentPos = 4 * (((int)t.UpperLeftCorner.X + x) +
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

            }
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
                BitmapSource img = BitmapSource.Create(_pixelWidth, _pixelHeight, 96, 96, PixelFormats.Bgra32, null,
                    _pixelValues, (_pixelWidth * PixelFormats.Bgra32.BitsPerPixel) / 8);
                return img;

            }

        }


        private void MapColor(int i, double r, double c, ref Byte[] colorRGB)
        {
            double di = (double)i;
            double zn;
            double hue;

            zn = Math.Sqrt(r + c);
            if (zn <= 0)
                zn = 0.1;

            hue = di + 1.0 - Math.Abs(Math.Log(zn)) / Math.Log(2.0);  // 2 is escape radius
            hue = 0.95 + 20.0 * hue; // adjust to make it prettier
                                     // the hsv function expects values from 0 to 360
            while (hue > 360.0)
                hue -= 360.0;
            while (hue < 0.0)
                hue += 360.0;

            ColorFromHSV(hue, 0.8, 1.0, ref colorRGB);
        }

        public static void ColorFromHSV(double hue, double saturation, double value, ref Byte[] colorRGB)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            Byte v = Convert.ToByte(value);
            Byte p = Convert.ToByte(value * (1 - saturation));
            Byte q = Convert.ToByte(value * (1 - f * saturation));
            Byte t = Convert.ToByte(value * (1 - (1 - f) * saturation));


            colorRGB[0] = hi == 0 ? v : (hi == 1 ? q : (hi == 2 || hi == 3 ? p : (hi == 4 ? t : v)));
            colorRGB[1] = hi == 0 ? t : (hi == 1 ? v : (hi == 2 ? v : (hi == 3 ? q : (hi == 4 ? p : p))));
            colorRGB[2] = hi == 0 ? p : (hi == 1 ? p : (hi == 2 ? t : (hi == 3 ? v : (hi == 4 ? v : q))));
        }
    }
}
