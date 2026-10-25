using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Microsoft.Windows.Themes
{
    /// <summary>
    /// Per-element state for <see cref="ThemeImage"/>.  It loads the image only while the
    /// element is loaded and drops the reference on unload.
    /// </summary>
    internal sealed class ThemeImageHost
    {
        private readonly FrameworkElement _element;
        private HackImageTransition _transition;
        private ThemeImageData _data;
        private bool _loaded;
        private bool _updateScheduled;
        private DispatcherTimer _updateTimer;
        private int _pixelWidth;
        private int _pixelHeight;
        private static readonly System.Collections.Generic.List<WeakReference> LiveHosts =
            new System.Collections.Generic.List<WeakReference>();

        internal ThemeImageHost(FrameworkElement element)
        {
            _element = element;
            lock (LiveHosts)
                LiveHosts.Add(new WeakReference(this));
        }

        internal static void RefreshAll()
        {
            var alive = new System.Collections.Generic.List<WeakReference>();
            lock (LiveHosts)
            {
                for (int i = 0; i < LiveHosts.Count; i++)
                {
                    var host = LiveHosts[i].Target as ThemeImageHost;
                    if (host == null)
                        continue;
                    alive.Add(LiveHosts[i]);
                    if (host._loaded)
                        host.Update();
                }
                LiveHosts.Clear();
                LiveHosts.AddRange(alive);
            }
        }

        private int PixelWidth()
        {
            return _element.ActualWidth > 0
                ? Math.Max(1, (int)Math.Round(_element.ActualWidth * ThemeImageProvider.Dpi))
                : 0;
        }

        private int PixelHeight()
        {
            return _element.ActualHeight > 0
                ? Math.Max(1, (int)Math.Round(_element.ActualHeight * ThemeImageProvider.Dpi))
                : 0;
        }

        internal void OnLoaded(object sender, RoutedEventArgs e)
        {
            _loaded = true;

            // Do not render during the first Loaded pass on XP: UxTheme can still
            // return the previous/default theme at that point and the wrong images
            // would be cached.  Wait until the window has really been shown.
            ScheduleUpdate();
        }

        private void ScheduleUpdate()
        {
            if (_updateScheduled)
                return;

            _updateScheduled = true;
            if (_updateTimer == null)
            {
                _updateTimer = new DispatcherTimer(DispatcherPriority.Background, _element.Dispatcher);
                _updateTimer.Interval = TimeSpan.FromMilliseconds(50);
                _updateTimer.Tick += delegate
                {
                    _updateTimer.Stop();
                    _updateScheduled = false;
                    if (_loaded)
                        Update();
                };
            }
            _updateTimer.Start();
        }

        internal void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _loaded = false;
            Release();
        }

        internal void Invalidate()
        {
            if (!_loaded)
                return;

            if (_data == null)
                ScheduleUpdate();
            else
                Update();
        }

        private void Update()
        {
            string className = ThemeImage.GetClass(_element);
            string subClass = ThemeImage.GetSubClass(_element);
            int part = ThemeImage.GetPart(_element);
            int state = ThemeImage.GetState(_element);
            if (string.IsNullOrEmpty(className) || part < 0 || state < 0)
            {
                Release();
                return;
            }

            IntPtr windowHandle = GetWindowHandle();
            if (windowHandle == IntPtr.Zero)
            {
                // The first Loaded/idle pass can run before the HwndSource exists.
                // Rendering now would cache a default-theme image under the zero-HWND key.
                ScheduleUpdate();
                return;
            }

            _pixelWidth = PixelWidth();
            _pixelHeight = PixelHeight();
            System.Drawing.Color background = ThemeImageProvider.GetBackgroundColor(_element);
            ThemeImageData data;
            bool useAlpha = ThemeImage.GetPreserveAlpha(_element)
                && !(Environment.OSVersion.Version.Major < 6 &&
                     string.Equals(className, "Scrollbar", StringComparison.Ordinal));

            if (useAlpha)
                data = ThemeImageProvider.GetAlphaNatural(className, part, state, windowHandle, subClass);
            else
                data = null;
            if (data == null || !ThemeImageProvider.HasVisibleAlpha(data.Image))
                data = ThemeImage.GetRenderMode(_element) == ThemeImage.ThemeImageRenderMode.Natural
                    ? ThemeImageProvider.GetNatural(className, part, state, windowHandle, subClass, background)
                    : ThemeImageProvider.GetSized(className, part, state, windowHandle, _pixelWidth, _pixelHeight, background, subClass);
            if (data == null)
            {
                Release();
                return;
            }

            // Keep the data (not just the BitmapSource) alive while this element is
            // loaded: the weak cache stores the data object, and the element is what
            // keeps it rooted.
            BitmapSource source = data.Image;
            if (ThemeImage.GetCropProgressSpace(_element))
            {
                int chunkSize;
                int spaceSize;
                int barPart = ThemeImage.GetPart(_element) == 6 ? 2 : 1;
                if (ThemeImageProvider.TryGetProgressMetrics(barPart, out chunkSize, out spaceSize) && spaceSize > 0)
                {
                    int pixels = Math.Max(1, (int)Math.Round(spaceSize * ThemeImageProvider.Dpi));
                    int cropWidth = ThemeImage.GetPart(_element) == 6 ? source.PixelWidth : Math.Max(1, source.PixelWidth - pixels);
                    int cropHeight = ThemeImage.GetPart(_element) == 6 ? Math.Max(1, source.PixelHeight - pixels) : source.PixelHeight;
                    if (cropWidth < source.PixelWidth || cropHeight < source.PixelHeight)
                    {
                        int cropY = ThemeImage.GetPart(_element) == 6
                            ? source.PixelHeight - cropHeight
                            : 0;
                        source = new CroppedBitmap(source,
                            new Int32Rect(0, cropY, cropWidth, cropHeight));
                    }
                }
            }
            _data = data;

            var hack = _element as HackImage;
            if (hack != null)
            {
                TimeSpan duration = ThemeImage.GetTransitionDuration(_element);
                if (duration > TimeSpan.Zero && hack.Source != null)
                {
                    StartTransition(hack, data, duration, part);
                }
                else
                {
                    CancelTransition();
                    hack.HackThickness = data.Margins;
                    ThemeImage.SetHackThickness(_element, data.Margins);
                    ThemeImage.SetChunkMargin(_element, GetChunkMargin(data, part));
                    ApplyChunkContainerMargin(data, part);
                    hack.Source = source;
                }
                return;
            }

            var image = _element as Image;
            if (image != null)
            {
                CancelTransition();
                image.Source = data.Image;
            }
        }

        private void ApplyChunkContainerMargin(ThemeImageData data, int part)
        {
            if (part != 1 && part != 2)
                return;

            Panel root = _element.Parent as Panel;
            if (root == null)
                return;

            foreach (UIElement child in root.Children)
            {
                FrameworkElement container = child as FrameworkElement;
                if (container != null && string.Equals(container.Name, "ChunkContainer", StringComparison.Ordinal))
                {
                    container.Margin = GetChunkMargin(data, part);
                    break;
                }
            }
        }
        private static Thickness GetChunkMargin(ThemeImageData data, int part)
        {
            if (data.HasContentMargins)
                return data.ContentMargins;

            // Same fallback for Bar and BarVert: content margins first,
            // then the part sizing margins.
            return data.SizingMargins;
        }
        private void StartTransition(HackImage target, ThemeImageData data, TimeSpan duration, int part)
        {
            CancelTransition();

            target.HackThickness = data.Margins;
            ThemeImage.SetHackThickness(_element, data.Margins);
            ThemeImage.SetChunkMargin(_element, GetChunkMargin(data, part));
            ApplyChunkContainerMargin(data, part);

            var transition = new HackImageTransition(target, target.Source, data.Image, duration);
            transition.Completed += OnTransitionCompleted;
            _transition = transition;
            transition.Start();
        }

        private void CancelTransition()
        {
            HackImageTransition transition = _transition;
            _transition = null;
            if (transition == null)
                return;

            transition.Completed -= OnTransitionCompleted;
            transition.Dispose();
        }

        private void OnTransitionCompleted(object sender, EventArgs e)
        {
            var transition = (HackImageTransition)sender;
            transition.Completed -= OnTransitionCompleted;
            if (ReferenceEquals(transition, _transition))
                _transition = null;
            transition.Dispose();
        }

        private IntPtr GetWindowHandle()
        {
            Window window = Window.GetWindow(_element);
            if (window != null)
            {
                IntPtr handle = new WindowInteropHelper(window).Handle;
                if (handle != IntPtr.Zero)
                    return handle;
            }

            HwndSource source = PresentationSource.FromVisual(_element) as HwndSource;
            return source != null ? source.Handle : IntPtr.Zero;
        }

        private void Release()
        {
            CancelTransition();
            _data = null;

            var hack = _element as HackImage;
            if (hack != null)
            {
                hack.Source = null;
                hack.HackThickness = new Thickness(0);
                return;
            }

            var image = _element as Image;
            if (image != null)
                image.Source = null;
        }
    }
}
