using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace System.Windows.Controls
{
    /// <summary>
    /// Indeterminate ProgressBar highlight, modelled on the WPF Luna/Royale
    /// indeterminate animation rather than stretching a UxTheme fill part.
    /// </summary>
    public sealed class ThemeProgressMarquee : FrameworkElement
    {
        private readonly DispatcherTimer _timer;
        private DateTime _start = DateTime.UtcNow;
        private double _offset;

        public ThemeProgressMarquee()
        {
            SnapsToDevicePixels = true;
            IsHitTestVisible = false;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            _timer = new DispatcherTimer(DispatcherPriority.Render);
            _timer.Interval = TimeSpan.FromMilliseconds(30);
            _timer.Tick += OnTick;
        }

        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
            nameof(Orientation), typeof(Orientation), typeof(ThemeProgressMarquee),
            new FrameworkPropertyMetadata(Orientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
            nameof(Fill), typeof(Brush), typeof(ThemeProgressMarquee),
            new FrameworkPropertyMetadata(SystemColors.HighlightBrush, FrameworkPropertyMetadataOptions.AffectsRender));

        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        public Brush Fill
        {
            get { return (Brush)GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _start = DateTime.UtcNow;
            _timer.Start();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
        }

        private void OnTick(object sender, EventArgs e)
        {
            double length = Orientation == Orientation.Vertical ? ActualHeight : ActualWidth;
            if (length <= 0)
                return;

            double band = Math.Max(24.0, length * 0.3);
            double span = length + band;
            double elapsed = (DateTime.UtcNow - _start).TotalSeconds;
            _offset = (elapsed * 120.0) % span;
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            double length = Orientation == Orientation.Vertical ? ActualHeight : ActualWidth;
            if (length <= 0)
                return;

            double band = Math.Max(24.0, length * 0.3);
            double position = _offset - band;
            Brush brush = Fill ?? SystemColors.HighlightBrush;

            if (Orientation == Orientation.Vertical)
            {
                drawingContext.DrawRectangle(brush, null,
                    new Rect(0, position, ActualWidth, band));
            }
            else
            {
                drawingContext.DrawRectangle(brush, null,
                    new Rect(position, 0, band, ActualHeight));
            }
        }
    }
}
