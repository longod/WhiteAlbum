﻿using System.Windows;
using System.Windows.Media;

namespace WA.Viewer.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // https://stackoverflow.com/questions/741956/pan-zoom-image
        // https://ni4muraano.hatenablog.com/entry/2017/10/21/135713
        // todo binding command, code behind
        private void image_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            // 基点がおかしい
            // image以外の領域でも出来て欲しい
            // smoothing easing

            const double scale = 1.2;

            var matrix = image.RenderTransform.Value;
            if (e.Delta > 0)
            {
                // mag
                matrix.ScaleAt(scale, scale, e.GetPosition(this).X, e.GetPosition(this).Y);
            }
            else
            {
                // min
                matrix.ScaleAt(1.0 / scale, 1.0 / scale, e.GetPosition(this).X, e.GetPosition(this).Y);
            }

            image.RenderTransform = new MatrixTransform(matrix);
        }

        Point _start;
        private void image_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            image.CaptureMouse();
            _start = e.GetPosition(border);
        }

        private void image_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            image.ReleaseMouseCapture();

            // reset
            var matrix = image.RenderTransform.Value;
            matrix.M11 = 1.0;
            matrix.M12 = 0.0;
            matrix.M21 = 0.0;
            matrix.M22 = 1.0;
            matrix.OffsetX = 0.0;
            matrix.OffsetY = 0.0;
            image.RenderTransform = new MatrixTransform(matrix);
        }

        private void image_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (image.IsMouseCaptured)
            {
                var matrix = image.RenderTransform.Value;

                Vector v = _start - e.GetPosition(border);
                matrix.Translate(-v.X, -v.Y);
                image.RenderTransform = new MatrixTransform(matrix);
                _start = e.GetPosition(border);
            }
        }
    }
}