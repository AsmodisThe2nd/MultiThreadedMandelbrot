using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using Xceed.Wpf.Toolkit;

namespace Fractal
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Mandelbrot _previewMandelbrot;
        private Mandelbrot _renderMandelbrot;



        decimal _startX = -2;
        decimal _startY = -2;
        decimal _sectionSizeX = 10;
        decimal _sectionSizeY;

        int _renderResultX = 1920 * 3;
        int _renderResultY = 1080;
        int _renderMaxIt = 1000;
        private int _renderMaxTileSize = 300;

        int _previewResultX = 1920 * 3;
        int _previewResultY = 1080;
        int _previewMaxIt = 1000;
        private int _previewMaxTileSize = 50;

        private BitmapSource bmp;

        private object bmpLock;

        private bool _initialized;
        public MainWindow()
        {
            InitializeComponent();

            bmpLock = new object();
            _sectionSizeY = (decimal)_renderResultY / (decimal)_renderResultX * _sectionSizeX;
            _startY = (-1) * _sectionSizeY / 2;
            _startX = (-1) * _sectionSizeX / 2;

            if (_previewMandelbrot == null)
            {
                _previewMandelbrot = new Mandelbrot(_previewMaxIt, _startX, _startY, _sectionSizeX, _sectionSizeY, _previewResultX, _previewResultY, _previewMaxTileSize);
                _previewMandelbrot.NewTileAvailable += RefreshImage;
            }
            if (_renderMandelbrot == null)
            {
                _renderMandelbrot = new Mandelbrot(_renderMaxIt, _startX, _startY, _sectionSizeX, _sectionSizeY, _renderResultX, _renderResultY, _renderMaxTileSize);
                _renderMandelbrot.NewTileAvailable += RefreshImage;
            }
          //  _initialized = true;

        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (_renderMandelbrot == null)
            {
                _renderMandelbrot = new Mandelbrot(_renderMaxIt, _startX, _startY, _sectionSizeX, _sectionSizeY, _renderResultX, _renderResultY, _renderMaxTileSize);
                _renderMandelbrot.NewTileAvailable += RefreshImage;
            }
            _renderMandelbrot.Calculate(_renderMaxIt, _startX, _startY, _sectionSizeX, _sectionSizeY, _renderResultX, _renderResultY);
        }

        public void RefreshImage(object sender, EventArgs e)
        {
            lock (bmpLock)
            {
                Dispatcher.BeginInvoke(new Action(() => bmp = ((Mandelbrot)sender).getImage().Clone()));
                Dispatcher.BeginInvoke(new Action(() => image.Source = bmp));
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sd = new SaveFileDialog();
            sd.InitialDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
            sd.Filter = "PNG|*.png";
            if (sd.ShowDialog() == true)
            {
                if (bmp != null)
                {
                    using (var fileStream = new FileStream(sd.FileName, FileMode.Create))
                    {
                        BitmapEncoder encoder = new PngBitmapEncoder();

                        encoder.Frames.Add(BitmapFrame.Create(bmp));
                        encoder.Save(fileStream);
                    }
                }
            }
        }

        void Preview()
        {
            _renderMandelbrot?.StopRendering();
            _previewMandelbrot?.StopRendering();
            _previewMandelbrot?.Calculate(_previewMaxIt, _startX, _startY, _sectionSizeX, _sectionSizeY, _previewResultX, _previewResultY);
        }

        private void previewIterationInput_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _previewMaxIt = (int)((DecimalUpDown)sender).Value.GetValueOrDefault();
            Preview();
        }

        private void previewWidthInput_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _previewResultX = (int)((DecimalUpDown)sender).Value.GetValueOrDefault();
            if (_initialized)
            {
                decimal res = (decimal) _previewResultX / (decimal) _previewResultY;
                complexWidthInput.Value = res * complexHeightInput.Value;
                renderWidthInput.Value = res * renderHeightInput.Value;
            }
            Preview();
        }

        private void previewHeightInput_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _previewResultY = (int)((DecimalUpDown)sender).Value.GetValueOrDefault();
            if (_initialized)
            {
                decimal res = (decimal) _previewResultY / (decimal) _previewResultX;
                complexHeightInput.Value = res * complexWidthInput.Value;
                renderHeightInput.Value = res * renderWidthInput.Value;
            }
            Preview();
        }

        private void renderIterationInput_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _renderMaxIt = (int)((DecimalUpDown)sender).Value.GetValueOrDefault();

        }

        private void renderWidthInput_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _renderResultX = (int)((DecimalUpDown)sender).Value.GetValueOrDefault();
            if (_initialized)
            {
                decimal res = (decimal) _renderResultX / (decimal) _renderResultY;
                complexWidthInput.Value = res * complexHeightInput.Value;
                previewWidthInput.Value = res * previewHeightInput.Value;
            }
        }

        private void renderHeightInput_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _renderResultY = (int)((DecimalUpDown)sender).Value.GetValueOrDefault();
            if (_initialized)
            {
                decimal res = (decimal) _renderResultY / (decimal) _renderResultX;
                complexHeightInput.Value = res * complexWidthInput.Value;
                previewHeightInput.Value = res * previewWidthInput.Value;
            }

        }

        private void startXInput_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _startX = ((DecimalUpDown)sender).Value.GetValueOrDefault();
            Preview();
        }

        private void startYInput_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _startY = ((DecimalUpDown)sender).Value.GetValueOrDefault();
            Preview();
        }

        private void complexWidthInput_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _sectionSizeX = ((DecimalUpDown)sender).Value.GetValueOrDefault();
            Preview();
        }

        private void complexHeightInput_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            _sectionSizeY = ((DecimalUpDown)sender).Value.GetValueOrDefault();
            Preview();
        }
    }
}
