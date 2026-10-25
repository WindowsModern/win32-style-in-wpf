using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Windows.Themes;

namespace System.Windows.Controls
{
    /// <summary>
    /// Lays out native ProgressBar chunk parts.  Each chunk is a real HackImage using
    /// PP_CHUNK/PP_CHUNKVERT's natural size and TMT_SIZINGMARGINS as HackThickness.
    /// Chunks are separated by a fixed 2 DIP; overflow is clipped by the parent grid.
    /// </summary>
    public sealed class ThemeProgressChunks : Panel
    {
        private const double ChunkGap = 2.0;
        private ThemeImageData _chunkData;
        private int _pixelWidth;
        private int _pixelHeight;
        private int _partId;
        private int _stateId;
        private string _subClass;
        private readonly DispatcherTimer _timer;
        private double _offset;
        private double _chunkSize;
        private double _spaceSize;
        private double _patternLength;

        public ThemeProgressChunks()
        {
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            SizeChanged += OnSizeChanged;
            _timer = new DispatcherTimer(DispatcherPriority.Render);
            _timer.Interval = TimeSpan.FromMilliseconds(30);
            _timer.Tick += OnTick;
        }

        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
            nameof(Orientation), typeof(Orientation), typeof(ThemeProgressChunks),
            new FrameworkPropertyMetadata(Orientation.Horizontal, OnPropertyChanged));

        public static readonly DependencyProperty PartIdProperty = DependencyProperty.Register(
            nameof(PartId), typeof(int), typeof(ThemeProgressChunks),
            new FrameworkPropertyMetadata(3, OnPropertyChanged));

        public static readonly DependencyProperty StateIdProperty = DependencyProperty.Register(
            nameof(StateId), typeof(int), typeof(ThemeProgressChunks),
            new FrameworkPropertyMetadata(0, OnPropertyChanged));

        public static readonly DependencyProperty SubClassProperty = DependencyProperty.Register(
            nameof(SubClass), typeof(string), typeof(ThemeProgressChunks),
            new FrameworkPropertyMetadata(null, OnPropertyChanged));

        public static readonly DependencyProperty FixedCountProperty = DependencyProperty.Register(
            nameof(FixedCount), typeof(int), typeof(ThemeProgressChunks),
            new FrameworkPropertyMetadata(0, OnPropertyChanged));

        public static readonly DependencyProperty AnimateProperty = DependencyProperty.Register(
            nameof(Animate), typeof(bool), typeof(ThemeProgressChunks),
            new FrameworkPropertyMetadata(false, OnPropertyChanged));

        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        public int PartId
        {
            get { return (int)GetValue(PartIdProperty); }
            set { SetValue(PartIdProperty, value); }
        }

        public int StateId
        {
            get { return (int)GetValue(StateIdProperty); }
            set { SetValue(StateIdProperty, value); }
        }

        public string SubClass
        {
            get { return (string)GetValue(SubClassProperty); }
            set { SetValue(SubClassProperty, value); }
        }

        public int FixedCount
        {
            get { return (int)GetValue(FixedCountProperty); }
            set { SetValue(FixedCountProperty, value); }
        }

        public bool Animate
        {
            get { return (bool)GetValue(AnimateProperty); }
            set { SetValue(AnimateProperty, value); }
        }

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ThemeProgressChunks)d).Update();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Update();
            if (Animate)
                _timer.Start();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            _chunkData = null;
            Children.Clear();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (IsLoaded)
                Update();
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (!IsLoaded || !Animate)
                return;

            _offset += 2.0;
            double track = Orientation == Orientation.Vertical ? ActualHeight : ActualWidth;
            if (_patternLength > 0)
            {
                // Walk the full track.  A pattern-only loop only moves the
                // chunks by one pattern width and never reaches the end.
                double span = track + _patternLength;
                if (_offset >= span)
                    _offset = 0.0;
            }
            else
            {
                if (_offset > track + 160.0)
                    _offset = 0.0;
            }
            InvalidateArrange();
        }

        private void Update()
        {
            if (!IsLoaded)
                return;

            int width = ActualWidth > 0 ? Math.Max(1, (int)Math.Round(ActualWidth * ThemeImageProvider.Dpi)) : 0;
            int height = ActualHeight > 0 ? Math.Max(1, (int)Math.Round(ActualHeight * ThemeImageProvider.Dpi)) : 0;
            if (width == _pixelWidth && height == _pixelHeight && _chunkData != null
                && PartId == _partId && StateId == _stateId
                && string.Equals(SubClass, _subClass, StringComparison.Ordinal))
                return;

            _pixelWidth = width;
            _pixelHeight = height;
            _partId = PartId;
            _stateId = StateId;
            _subClass = SubClass;

            IntPtr hwnd = IntPtr.Zero;
            Window window = Window.GetWindow(this);
            if (window != null)
                hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;

            int chunkPixels;
            int spacePixels;
            int barPart = Orientation == Orientation.Vertical ? 2 : 1;
            if (ThemeImageProvider.TryGetProgressMetrics(barPart, out chunkPixels, out spacePixels))
            {
                _chunkSize = chunkPixels * ThemeImageProvider.Dpi;
                _spaceSize = spacePixels * ThemeImageProvider.Dpi;
            }
            else
            {
                _chunkSize = 0;
                _spaceSize = 0;
            }

            // No fallback: if the theme has no PP_CHUNK/PP_CHUNKVERT, chunked
            // mode simply has no chunk image.  Smooth fill is a separate template.
            System.Drawing.Color background = ThemeImageProvider.GetBackgroundColor(this);
            _chunkData = ThemeImageProvider.GetNatural("Progress", PartId, StateId, hwnd, SubClass, background);
            _offset = 0.0;
            RebuildChildren();
        }

        private void RebuildChildren()
        {
            Children.Clear();
            if (_chunkData == null || _chunkData.Image == null)
                return;

            double chunkWidth = Orientation == Orientation.Vertical
                ? Math.Max(1.0, ActualWidth)
                : Math.Max(1.0, _chunkSize > 0 ? _chunkSize : _chunkData.Image.Width);
            double chunkHeight = Orientation == Orientation.Vertical
                ? Math.Max(1.0, _chunkSize > 0 ? _chunkSize : _chunkData.Image.Height)
                : Math.Max(1.0, ActualHeight);

            double gap = _spaceSize;
            double length = Orientation == Orientation.Vertical ? ActualHeight : ActualWidth;
            if (length <= 0 && FixedCount == 0)
                return;

            double chunkLength = Orientation == Orientation.Vertical ? chunkHeight : chunkWidth;
            int count = FixedCount > 0
                ? FixedCount
                : Math.Max(1, (int)Math.Ceiling((length + gap) / (chunkLength + gap)));
            _patternLength = count * (chunkLength + gap);

            for (int i = 0; i < count; i++)
            {
                // PP_CHUNK margins are axis-specific here: a horizontal chunk
                // must fill the bar height, so only left/right margins apply;
                // a vertical chunk only uses top/bottom margins.
                // PP_CHUNKVERT is a rotated chunk: its left/right margins
                // describe the visible width, not the stacking direction.
                Thickness chunkHack = new Thickness(
                    _chunkData.Margins.Left, 0, _chunkData.Margins.Right, 0);

                BitmapSource chunkSource = CropChunk(_chunkData.Image, chunkWidth, chunkHeight);
                var image = new HackImage
                {
                    Source = chunkSource,
                    HackThickness = chunkHack,
                    Width = chunkWidth,
                    Height = chunkHeight,
                    Margin = new Thickness(0)
                };
                Children.Add(image);
            }
        }

        private BitmapSource CropChunk(BitmapSource source, double chunkWidth, double chunkHeight)
        {
            if (source == null)
                return null;

            if (Orientation == Orientation.Horizontal)
            {
                int cropWidth = Math.Min(source.PixelWidth,
                    Math.Max(1, (int)Math.Round(chunkWidth * ThemeImageProvider.Dpi)));
                if (cropWidth >= source.PixelWidth)
                    return source;
                return new CroppedBitmap(source,
                    new Int32Rect(0, 0, cropWidth, source.PixelHeight));
            }
            else
            {
                int cropHeight = Math.Min(source.PixelHeight,
                    Math.Max(1, (int)Math.Round(chunkHeight * ThemeImageProvider.Dpi)));
                if (cropHeight >= source.PixelHeight)
                    return source;
                // Vertical chunk: trim the top and keep the bottom of the
                // source image, matching the native chunk orientation.
                return new CroppedBitmap(source,
                    new Int32Rect(0, source.PixelHeight - cropHeight,
                        source.PixelWidth, cropHeight));
            }
        }
        protected override Size MeasureOverride(Size availableSize)
        {
            double width = double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width;
            double height = double.IsInfinity(availableSize.Height) ? 0 : availableSize.Height;
            foreach (UIElement child in Children)
                child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            return new Size(width, height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            double gap = _spaceSize;
            double x = _offset;
            double bottom = finalSize.Height - _offset;
            foreach (UIElement child in Children)
            {
                if (Orientation == Orientation.Vertical)
                {
                    child.Arrange(new Rect(0, bottom - child.DesiredSize.Height,
                        finalSize.Width, child.DesiredSize.Height));
                    bottom -= child.DesiredSize.Height + gap;
                }
                else
                {
                    child.Arrange(new Rect(x, 0, child.DesiredSize.Width, finalSize.Height));
                    x += child.DesiredSize.Width + gap;
                }
            }
            return finalSize;
        }
    }
}
