using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Fractal
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Mandelbrot m;
        decimal sectionSizeX = 4;
        decimal sectionSizeY = 4;
        decimal startX = -2;
        decimal startY = -2;
        int resultX = 1920;
        int resultY = 1920;
        int maxIt = 1000;
        private int maxTileSize = 300;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (m == null)
            {
                m = new Mandelbrot(maxIt, startX, startY, sectionSizeX, sectionSizeY, resultX, resultY, maxTileSize);
                m.NewTileAvailable += RefreshImage;
            }
            m.Calculate(maxIt, startX, startY, sectionSizeX, sectionSizeY, resultX, resultY);
        }

        public void RefreshImage(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => image.Source = m.getImage()));
        }
    }
}
