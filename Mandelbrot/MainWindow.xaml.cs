using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using Xceed.Wpf.Toolkit;

namespace Fractal
{
    /// <summary>
    ///     Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly object _bmpLock;
        private readonly bool _initialized;
        private readonly Mandelbrot _previewMandelbrot;
        private readonly int _previewMaxTileSize = 50;
        private readonly Mandelbrot _renderMandelbrot;
        private readonly int _renderMaxTileSize = 300;

        private readonly DispatcherTimer _timer;
        private BitmapSource _bmp;


        private decimal _centerX;
        private decimal _centerY;
        private decimal _complexZoom = 1;
        private int _previewMaxIt = 1000;

        private int _previewResultX = 1920 * 3;
        private int _previewResultY = 1080;
        private int _renderMaxIt = 1000;

        private int _renderResultX = 1920 * 3;
        private int _renderResultY = 1080;

        private bool _typing;

        public MainWindow()
        {
            InitializeComponent();

            _bmpLock = new object();


            _previewMandelbrot = new Mandelbrot(_previewMaxIt, _centerX, _centerY, _complexZoom,
                _previewResultX, _previewResultY, _previewMaxTileSize);
            _previewMandelbrot.NewTileAvailable += RefreshImage;


            _renderMandelbrot = new Mandelbrot(_renderMaxIt, _centerX, _centerY, _complexZoom,
                _renderResultX, _renderResultY, _renderMaxTileSize);
            _renderMandelbrot.NewTileAvailable += RefreshImage;

            _timer = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(200)};
            _timer.Tick += StopTyping;
            _timer.IsEnabled = true;
            _initialized = true;

            Preview();
        }

        private void StopTyping(object sender, EventArgs e)
        {
            _typing = false;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            _renderMandelbrot.Calculate(_renderMaxIt, _centerX, _centerY, _complexZoom, _renderResultX,
                _renderResultY);
        }

        public void RefreshImage(object sender, EventArgs e)
        {
            lock (_bmpLock)
            {
                Dispatcher.BeginInvoke(new Action(() => _bmp = ((Mandelbrot) sender).getImage().Clone()));
                Dispatcher.BeginInvoke(new Action(() => image.Source = _bmp));
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sd = new SaveFileDialog();
            sd.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            sd.Filter = "PNG|*.png";
            if (sd.ShowDialog() == true)
                lock (_bmpLock)
                {
                    if (_bmp != null)
                        using (var fileStream = new FileStream(sd.FileName, FileMode.Create))
                        {
                            BitmapEncoder encoder = new PngBitmapEncoder();

                            encoder.Frames.Add(BitmapFrame.Create(_bmp));
                            encoder.Save(fileStream);
                        }
                }
        }

        private void Preview()
        {
            if (!_initialized ||
                previewWidthInput.Value == 0 ||
                previewHeightInput.Value == 0 ||
                zoomInput.Value == 0 ||
                previewIterationInput.Value == 0
            )
                return;
            _renderMandelbrot?.StopRendering();
            _previewMandelbrot?.StopRendering();
            _previewMandelbrot?.Calculate(_previewMaxIt, _centerX, _centerY, _complexZoom,
                _previewResultX, _previewResultY);
        }


        private void renderIterationInput_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _renderMaxIt = (int) ((DecimalUpDown) sender).Value.GetValueOrDefault();
        }

        private void renderWidthInput_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _renderResultX = (int) ((DecimalUpDown) sender).Value.GetValueOrDefault();
        }

        private void renderHeightInput_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _renderResultY = (int) ((DecimalUpDown) sender).Value.GetValueOrDefault();
        }


        private void previewIterationInput_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _previewMaxIt = (int) ((DecimalUpDown) sender).Value.GetValueOrDefault();
            Preview();
        }

        private void previewWidthInput_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _previewResultX = (int) ((DecimalUpDown) sender).Value.GetValueOrDefault();
            Preview();
        }

        private void previewHeightInput_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _previewResultY = (int) ((DecimalUpDown) sender).Value.GetValueOrDefault();
            Preview();
        }


        private void centerXInput_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _centerX = ((DecimalUpDown) sender).Value.GetValueOrDefault();
            Preview();
        }

        private void centerYInput_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _centerY = ((DecimalUpDown) sender).Value.GetValueOrDefault();
            Preview();
        }

        private void zoomInput_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _complexZoom = ((DecimalUpDown) sender).Value.GetValueOrDefault();
            Preview();
        }
    }
}