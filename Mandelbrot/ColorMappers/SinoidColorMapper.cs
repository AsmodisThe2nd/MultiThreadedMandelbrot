using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Fractal.ColorMappers
{

    class SinoidColorMapper : AbstractColorMapper
    {
        private Window w;
        private double[] _valueModifiers = { 1, 1, 1 };

        ~SinoidColorMapper()
        {
        }

        public override void MapColor(int iterations, int maxIterations, double realPart, double imPart, ref byte[] colorBuffer)
        {
            double sin = Math.Sin(iterations * (Math.PI / (2 * maxIterations)));
            double cos = Math.Max(0, Math.Cos(iterations * (Math.PI / (2 * maxIterations))));

            colorBuffer[0] = (byte)(_valueModifiers[0] * Math.Floor(cos * 255)); //r
            colorBuffer[1] = (byte)(_valueModifiers[1] * Math.Floor(cos * 255)); //g
            colorBuffer[2] = (byte)(_valueModifiers[2] * Math.Floor(cos * 255)); //b
        }

        public SinoidColorMapper()
        {
            w = new Window();
            Grid rootGrid = new Grid();
            rootGrid.ColumnDefinitions.Add(new ColumnDefinition());
            rootGrid.ColumnDefinitions.Add(new ColumnDefinition());

            rootGrid.RowDefinitions.Add(new RowDefinition());
            rootGrid.RowDefinitions.Add(new RowDefinition());
            rootGrid.RowDefinitions.Add(new RowDefinition());
            rootGrid.RowDefinitions.Add(new RowDefinition());

            w.Content = rootGrid;
            for (int i = 0; i < 3; i++)
            {
                Slider s = new Slider
                {
                    Maximum = 1,
                    Minimum = 0,
                    SmallChange = 0.001
                };
                switch (i)
                {
                    case 0: s.ValueChanged += (sender, args) => { _valueModifiers[0] = ((Slider)sender).Value; }; break;
                    case 1: s.ValueChanged += (sender, args) => { _valueModifiers[1] = ((Slider)sender).Value;  }; break;
                    case 2: s.ValueChanged += (sender, args) => { _valueModifiers[2] = ((Slider)sender).Value; }; break;
                }
                Grid.SetRow(s, i);
                Grid.SetColumn(s, 1);
                rootGrid.Children.Add(s);

            }

            Button b = new Button() {Content = "Apply"};
            b.Click += (sender, args) => { OnRedrawWanted(); };
            Grid.SetRow(b, 3);
            Grid.SetColumnSpan(b, 2);
            rootGrid.Children.Add(b);

            w.Show();
        }
    }
}
