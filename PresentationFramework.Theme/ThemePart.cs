using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using Microsoft.Windows.Themes;

namespace System.Windows.Controls
{
    /// <summary>
    /// A single Win32 theme part drawn at the element's exact pixel size. Unlike
    /// <see cref="HackImage"/>, this control never applies nine-patch slicing: UxTheme
    /// receives the real target rectangle, exactly like Theme Explorer's part preview.
    /// </summary>
    public sealed class ThemePart : FrameworkElement
    {
        private ThemeImageData _data;
        private BitmapSource _source;
        private int _pixelWidth;
        private int _pixelHeight;
        private string _className;
        private int _partId;
        private int _stateId;
        private string _subClass;
        private bool _tile;
        private bool _watchingIndeterminate;
        private ProgressBar _progressHost;

        static ThemePart()
        {
            // Keep the ProgressBar track invisible for every template, including
            // binary BAML resources.  The track is only a measuring aid; its
            // Background would otherwise cover the theme Bar fill.
            EventManager.RegisterClassHandler(typeof(ProgressBar), FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnProgressBarLoaded));
        }

        private static void OnProgressBarLoaded(object sender, RoutedEventArgs e)
        {
            var bar = sender as ProgressBar;
            if (bar == null)
                return;

            var track = bar.Template == null ? null : bar.Template.FindName("PART_Track", bar) as UIElement;
            if (track != null)
                track.Visibility = Visibility.Hidden;
        }

        public ThemePart()
        {
            Loaded += delegate { ApplyProgressOverlayMode(); ApplyIndeterminateOverlaySize(); };
            SizeChanged += delegate { ApplyIndeterminateOverlaySize(); };
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            SizeChanged += OnSizeChanged;
        }

        public static readonly DependencyProperty ClassNameProperty = DependencyProperty.Register(
            nameof(ClassName), typeof(string), typeof(ThemePart),
            new FrameworkPropertyMetadata(string.Empty, OnPropertyChanged));

        public static readonly DependencyProperty PartIdProperty = DependencyProperty.Register(
            nameof(PartId), typeof(int), typeof(ThemePart),
            new FrameworkPropertyMetadata(0, OnPropertyChanged));

        public static readonly DependencyProperty StateIdProperty = DependencyProperty.Register(
            nameof(StateId), typeof(int), typeof(ThemePart),
            new FrameworkPropertyMetadata(0, OnPropertyChanged));

        public static readonly DependencyProperty PreserveAlphaProperty = DependencyProperty.Register(
            nameof(PreserveAlpha), typeof(bool), typeof(ThemePart),
            new FrameworkPropertyMetadata(false, OnPropertyChanged));

        public static readonly DependencyProperty NaturalSizeProperty = DependencyProperty.Register(
            nameof(NaturalSize), typeof(bool), typeof(ThemePart),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure, OnPropertyChanged));

        public static readonly DependencyProperty SubClassProperty = DependencyProperty.Register(
            nameof(SubClass), typeof(string), typeof(ThemePart),
            new FrameworkPropertyMetadata(null, OnPropertyChanged));

        public static readonly DependencyProperty TileProperty = DependencyProperty.Register(
            nameof(Tile), typeof(bool), typeof(ThemePart),
            new FrameworkPropertyMetadata(false, OnPropertyChanged));

        /// <summary>Theme class name, for example "Combobox" or "TrackBar".</summary>
        public string ClassName
        {
            get { return (string)GetValue(ClassNameProperty); }
            set { SetValue(ClassNameProperty, value); }
        }

        /// <summary>Win32 part id.</summary>
        public int PartId
        {
            get { return (int)GetValue(PartIdProperty); }
            set { SetValue(PartIdProperty, value); }
        }

        /// <summary>Win32 state id.</summary>
        public int StateId
        {
            get { return (int)GetValue(StateIdProperty); }
            set { SetValue(StateIdProperty, value); }
        }

        public bool PreserveAlpha
        {
            get { return (bool)GetValue(PreserveAlphaProperty); }
            set { SetValue(PreserveAlphaProperty, value); }
        }

        public bool NaturalSize
        {
            get { return (bool)GetValue(NaturalSizeProperty); }
            set { SetValue(NaturalSizeProperty, value); }
        }

        public string SubClass
        {
            get { return (string)GetValue(SubClassProperty); }
            set { SetValue(SubClassProperty, value); }
        }

        public bool Tile
        {
            get { return (bool)GetValue(TileProperty); }
            set { SetValue(TileProperty, value); }
        }

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ThemePart)d).Update();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Update();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _data = null;
            _source = null;
            _pixelWidth = 0;
            _pixelHeight = 0;
            _className = null;
            _partId = 0;
            _stateId = 0;
            InvalidateVisual();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            int width = PixelWidth();
            int height = PixelHeight();
            if (width != _pixelWidth || height != _pixelHeight)
                Update();
        }

        private int PixelWidth()
        {
            return ActualWidth > 0
                ? Math.Max(1, (int)Math.Round(ActualWidth * ThemeImageProvider.Dpi))
                : 0;
        }

        private int PixelHeight()
        {
            return ActualHeight > 0
                ? Math.Max(1, (int)Math.Round(ActualHeight * ThemeImageProvider.Dpi))
                : 0;
        }

        private void Update()
        {
            if (!IsLoaded)
                return;

            string className = ClassName;
            if (string.IsNullOrEmpty(className) || PartId <= 0)
            {
                _data = null;
                _source = null;
                InvalidateVisual();
                return;
            }

            int width = PixelWidth();
            int height = PixelHeight();
            if (width == _pixelWidth && height == _pixelHeight && _source != null
                && string.Equals(className, _className, StringComparison.Ordinal)
                && PartId == _partId && StateId == _stateId
                && string.Equals(SubClass, _subClass, StringComparison.Ordinal))
                return;

            _pixelWidth = width;
            _pixelHeight = height;
            _className = className;
            _partId = PartId;
            _stateId = StateId;
            _subClass = SubClass;
            _tile = Tile;

            IntPtr hwnd = IntPtr.Zero;
            Window window = Window.GetWindow(this);
            if (window != null)
                hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;

            System.Drawing.Color background = ThemeImageProvider.GetBackgroundColor(this);

            // XP's Scrollbar track/arrow parts do not blend reliably against
            // the probe backgrounds; use the original sized bitmap there.
            // Two-part overlays (Thumb 2/3 and Gripper 8/9) are handled
            // separately; only the Gripper needs alpha reconstruction.
            bool useAlpha = PreserveAlpha
                && !(Environment.OSVersion.Version.Major < 6 &&
                     string.Equals(className, "Scrollbar", StringComparison.Ordinal) &&
                     PartId != 2 && PartId != 3 && PartId != 8 && PartId != 9);

            _data = useAlpha
                ? ThemeImageProvider.GetAlphaNatural(className, PartId, StateId, hwnd, SubClass)
                : null;
            if (_data == null || !ThemeImageProvider.HasVisibleAlpha(_data.Image))
                _data = ThemeImageProvider.GetSized(className, PartId, StateId, hwnd, width, height, background);
            _source = _data != null ? _data.Image : null;
            InvalidateVisual();
            InvalidateMeasure();
        }

        private void ApplyProgressOverlayMode()
        {
            if (!string.Equals(ClassName, "Progress", StringComparison.Ordinal))
                return;
            if (PartId < 7 || PartId > 10)
                return;

            // The bitmap layout is controlled by the template's Tile value.
            // Pulse is visible in the normal continuous state, but hidden
            // while the ProgressBar is indeterminate.
            if (PartId == 7 || PartId == 9)
            {
                ProgressBar bar = FindProgressBar();
                if (bar != null)
                {
                    if (!_watchingIndeterminate)
                    {
                        _watchingIndeterminate = true;
                        _progressHost = bar;
                        System.ComponentModel.DependencyPropertyDescriptor
                            .FromProperty(ProgressBar.IsIndeterminateProperty, typeof(ProgressBar))
                            .AddValueChanged(bar, OnIndeterminateChanged);
                    }
                    UpdatePulseVisibility(bar);
                }
            }        }
        private ProgressBar FindProgressBar()
        {
            DependencyObject current = Parent;
            while (current != null)
            {
                var bar = current as ProgressBar;
                if (bar != null)
                    return bar;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private void OnIndeterminateChanged(object sender, EventArgs e)
        {
            UpdatePulseVisibility(sender as ProgressBar);
        }

        private void UpdatePulseVisibility(ProgressBar bar)
        {
            if (bar == null)
                return;

            if (bar.IsIndeterminate)
                Visibility = Visibility.Collapsed;
            else
                ClearValue(VisibilityProperty);
        }
        private bool IsIndeterminateOverlay
        {
            get
            {
                return string.Equals(ClassName, "Progress", StringComparison.Ordinal)
                    && (PartId == 8 || PartId == 10)
                    && string.Equals(SubClass, "Indeterminate", StringComparison.Ordinal);
            }
        }

        private void ApplyIndeterminateOverlaySize()
        {
            if (!IsLoaded || !IsIndeterminateOverlay)
                return;

            FrameworkElement parent = Parent as FrameworkElement;
            if (parent == null)
                return;

            double available = PartId == 8 ? parent.ActualWidth : parent.ActualHeight;
            if (available <= 0)
                return;

            double length = ThemeProgress.IsAero ? 100.0 : available * 0.25;
            if (length > available)
                length = available;

            if (PartId == 8)
            {
                HorizontalAlignment = HorizontalAlignment.Left;
                Width = length;
                Height = available;

                var transform = RenderTransform as TranslateTransform;
                if (transform != null && transform.X <= -length + 0.5)
                {
                    var animation = new DoubleAnimation(-length, available + length,
                        TimeSpan.FromSeconds(2.0))
                    {
                        RepeatBehavior = RepeatBehavior.Forever
                    };
                    transform.BeginAnimation(TranslateTransform.XProperty, animation);
                }
            }
            else
            {
                VerticalAlignment = VerticalAlignment.Bottom;
                Height = length;
                Width = available;

                var transform = RenderTransform as TranslateTransform;
                if (transform != null && transform.Y <= -length + 0.5)
                {
                    var animation = new DoubleAnimation(-length, available + length,
                        TimeSpan.FromSeconds(2.0))
                    {
                        RepeatBehavior = RepeatBehavior.Forever
                    };
                    transform.BeginAnimation(TranslateTransform.YProperty, animation);
                }
            }
        }
        protected override Size MeasureOverride(Size availableSize)
        {
            if (IsIndeterminateOverlay)
            {
                double available = PartId == 8 ? availableSize.Width : availableSize.Height;
                if (!double.IsInfinity(available) && available > 0)
                {
                    double length = ThemeProgress.IsAero ? 100.0 : available * 0.25;
                    if (length > available)
                        length = available;

                    if (PartId == 8)
                    {
                        HorizontalAlignment = HorizontalAlignment.Left;
                        Width = length;
                        Height = available;
                    }
                    else
                    {
                        VerticalAlignment = VerticalAlignment.Bottom;
                        Height = length;
                        Width = available;
                    }
                }
            }
            if (NaturalSize && _source != null)
                return new Size(_source.Width, _source.Height);

            double width = double.IsInfinity(availableSize.Width)
                ? (_source != null ? _source.Width : 0)
                : availableSize.Width;
            double height = double.IsInfinity(availableSize.Height)
                ? (_source != null ? _source.Height : 0)
                : availableSize.Height;
            return new Size(width, height);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (_source == null || RenderSize.Width <= 0 || RenderSize.Height <= 0)
                return;

            if (_tile && _source.Width > 0 && _source.Height > 0)
            {
                for (double y = 0; y < RenderSize.Height; y += _source.Height)
                    for (double x = 0; x < RenderSize.Width; x += _source.Width)
                        drawingContext.DrawImage(_source,
                            new Rect(x, y, _source.Width, _source.Height));
                return;
            }

            drawingContext.DrawImage(_source,
                new Rect(0, 0, RenderSize.Width, RenderSize.Height));
        }
    }
}
