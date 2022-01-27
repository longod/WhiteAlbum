namespace WA
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    // https://www.mesta-automation.com/tecniques-scaling-wpf-application/
    public class DpiAwareDecorator : Decorator
    {
        public DpiAwareDecorator()
        {
            // fixme load時だけだと、dpiの異なるモニタをまたいだ時にそれに追従できない
            // これを適用した場合、親のこのtransformを考慮するようにしないと子要素の移動や拡大がズレる
            Loaded += (s, e) =>
            {
                if (Enable)
                {
                    Matrix m = PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice;
                    LayoutTransform = CalculateAwarenessTransform(m.M11, m.M22);
                }
            };
        }

        // 呼び出されるようにするにはmanifestで適切な値(true or true/pm)を指定しておく必要がある
        // https://docs.microsoft.com/en-us/windows/win32/hidpi/setting-the-default-dpi-awareness-for-a-process
        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
        {
            if (Enable)
            {
                LayoutTransform = CalculateAwarenessTransform(newDpi.DpiScaleX, newDpi.DpiScaleY);
            }
            base.OnDpiChanged(oldDpi, newDpi);
        }

        // dpi scalingを打ち消すtransform
        private static ScaleTransform CalculateAwarenessTransform(double dpiScaleX, double dpiScaleY)
        {
            ScaleTransform dpiTransform = new ScaleTransform(1.0 / dpiScaleX, 1.0 / dpiScaleY);
            if (dpiTransform.CanFreeze)
            {
                dpiTransform.Freeze();
            }
            return dpiTransform;
        }

        public static readonly DependencyProperty EnableProperty =
            DependencyProperty.Register("Enable",
                typeof(bool),
                typeof(DpiAwareDecorator),
                new PropertyMetadata(true));

        public bool Enable
        {
            get
            {
                return (bool)GetValue(EnableProperty);
            }

            set
            {
                SetValue(EnableProperty, value);
            }
        }
    }
}
