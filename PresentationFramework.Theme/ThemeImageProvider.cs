using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ThemeExplorer.Core;
using Microsoft.Windows.Themes.Interop;

namespace Microsoft.Windows.Themes
{
    /// <summary>
    /// Renders Win32 theme parts on demand and keeps only weak references to the results.
    /// </summary>
    /// <remarks>
    /// The cache is deliberately weak: while a control uses an image it stays alive, when the
    /// last control unloads the image becomes collectable.  No static strong reference to a
    /// <see cref="BitmapSource"/> exists in this class.
    /// </remarks>
    internal static class ThemeImageProvider
    {
        private static readonly bool IsXpOrOlder = Environment.OSVersion.Version.Major < 6;

        private const int TmtSizingMargins = 3601;
        private const int TmtContentMargins = 3602;
        private const int TmtBorderSize = 2403;
        private const int TmtProgressChunkSize = 2411;
        private const int TmtProgressSpaceSize = 2412;
        private const int TmtSourceShrink = 2213;
        private const int TmtFillColorHint = 3821;
        private const int MaxCacheEntries = 256;

        private static readonly object Gate = new object();
        private static readonly Dictionary<string, WeakReference> Cache =
            new Dictionary<string, WeakReference>(StringComparer.Ordinal);
        private static readonly double DpiScale = ComputeDpiScale();

        internal static double Dpi
        {
            get { return DpiScale; }
        }

        private static int _createdCount;

        internal static int CreatedCount
        {
            get { lock (Gate) { return _createdCount; } }
        }

        internal static string[] GetCacheDetails()
        {
            lock (Gate)
            {
                PurgeLocked();
                var details = new System.Collections.Generic.List<string>();
                foreach (System.Collections.Generic.KeyValuePair<string, WeakReference> pair in Cache)
                {
                    ThemeImageData data = pair.Value.Target as ThemeImageData;
                    if (data == null || data.Image == null)
                        continue;

                    details.Add(pair.Key
                        + "|pixels=" + data.Image.PixelWidth.ToString(CultureInfo.InvariantCulture)
                        + "x" + data.Image.PixelHeight.ToString(CultureInfo.InvariantCulture)
                        + "|margin=" + data.Margins.Left.ToString("R", CultureInfo.InvariantCulture)
                        + "," + data.Margins.Top.ToString("R", CultureInfo.InvariantCulture)
                        + "," + data.Margins.Right.ToString("R", CultureInfo.InvariantCulture)
                        + "," + data.Margins.Bottom.ToString("R", CultureInfo.InvariantCulture)
                        + "|sizing=" + data.SizingMargins.Left.ToString("R", CultureInfo.InvariantCulture)
                        + "," + data.SizingMargins.Top.ToString("R", CultureInfo.InvariantCulture)
                        + "," + data.SizingMargins.Right.ToString("R", CultureInfo.InvariantCulture)
                        + "," + data.SizingMargins.Bottom.ToString("R", CultureInfo.InvariantCulture)
                        + "|content=" + data.ContentMargins.Left.ToString("R", CultureInfo.InvariantCulture)
                        + "," + data.ContentMargins.Top.ToString("R", CultureInfo.InvariantCulture)
                        + "," + data.ContentMargins.Right.ToString("R", CultureInfo.InvariantCulture)
                        + "," + data.ContentMargins.Bottom.ToString("R", CultureInfo.InvariantCulture)
                        + "|content-supported=" + data.HasContentMargins.ToString());
                }
                return details.ToArray();
            }
        }

        internal static string[] GetCacheKeys()
        {
            lock (Gate)
            {
                PurgeLocked();
                var keys = new string[Cache.Count];
                Cache.Keys.CopyTo(keys, 0);
                return keys;
            }
        }

        internal static int AliveCount
        {
            get
            {
                lock (Gate)
                {
                    PurgeLocked();
                    return Cache.Count;
                }
            }
        }

        internal static bool HasVisibleAlpha(BitmapSource image)
        {
            if (image == null || image.PixelWidth <= 0 || image.PixelHeight <= 0)
                return false;

            int stride = image.PixelWidth * 4;
            var pixels = new byte[stride * image.PixelHeight];
            image.CopyPixels(pixels, stride, 0);
            for (int i = 3; i < pixels.Length; i += 4)
            {
                if (pixels[i] > 8)
                    return true;
            }
            return false;
        }
        internal static System.Drawing.Color GetBackgroundColor(DependencyObject element)
        {
            DependencyObject current = element;
            while (current != null)
            {
                var control = current as Control;
                if (control != null)
                {
                    System.Drawing.Color color;
                    if (TryGetSolidColor(control.Background, out color))
                        return color;
                }

                var panel = current as Panel;
                if (panel != null)
                {
                    System.Drawing.Color color;
                    if (TryGetSolidColor(panel.Background, out color))
                        return color;
                }

                var border = current as Border;
                if (border != null)
                {
                    System.Drawing.Color color;
                    if (TryGetSolidColor(border.Background, out color))
                        return color;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return System.Drawing.SystemColors.Control;
        }

        private static bool TryGetSolidColor(Brush brush, out System.Drawing.Color color)
        {
            var solid = brush as SolidColorBrush;
            if (solid != null && solid.Color.A > 0)
            {
                color = System.Drawing.Color.FromArgb(solid.Color.A, solid.Color.R, solid.Color.G, solid.Color.B);
                return true;
            }

            color = System.Drawing.SystemColors.Control;
            return false;
        }

        private static System.Drawing.Color ResolveBackground(VisualStyle style, string className,
            int part, int state, System.Drawing.Color fallback)
        {
            if (string.Equals(className, "Progress", StringComparison.Ordinal) && (part == 1 || part == 2))
            {
                System.Drawing.Color hint;
                if (style.TryGetColor(part, state, TmtFillColorHint, out hint))
                    return hint;
            }
            return fallback;
        }
        private static string BackgroundKey(System.Drawing.Color? background)
        {
            if (!background.HasValue)
                return "default";

            System.Drawing.Color color = background.Value;
            return color.A + "," + color.R + "," + color.G + "," + color.B;
        }
        internal static ThemeImageData Get(string className, int part, int state, IntPtr window, System.Drawing.Color? background = null)
        {
            if (string.IsNullOrEmpty(className) || !ThemeSystem.IsVisualStylesEnabled)
                return null;

            ThemeInfo theme = ThemeSystem.GetCurrentTheme();
            string key = string.Concat(
                theme.FileName, "|", theme.ColorScheme, "|", theme.SizeName, "|",
                className, "|", part.ToString(CultureInfo.InvariantCulture), "|",
                state.ToString(CultureInfo.InvariantCulture), "|",
                window.ToInt64().ToString(CultureInfo.InvariantCulture), "|",
                DpiScale.ToString("R", CultureInfo.InvariantCulture), "|bg=", BackgroundKey(background));

            lock (Gate)
            {
                WeakReference reference;
                if (Cache.TryGetValue(key, out reference) && reference.IsAlive)
                    return (ThemeImageData)reference.Target;

                ThemeImageData data = Render(className, part, state, window, background);
                if (data == null)
                    return null;

                if (Cache.Count >= MaxCacheEntries)
                    PurgeLocked();
                if (Cache.Count >= MaxCacheEntries)
                    Cache.Clear();

                Cache[key] = new WeakReference(data);
                _createdCount++;
                return data;
            }
        }

        internal static void ResetCreatedImageCount()
        {
            lock (Gate)
                _createdCount = 0;
        }

        internal static void ClearCache()
        {
            lock (Gate)
            {
                Cache.Clear();
            }
        }

        private static void PurgeLocked()
        {
            if (Cache.Count == 0)
                return;

            var dead = new List<string>();
            foreach (KeyValuePair<string, WeakReference> entry in Cache)
            {
                if (!entry.Value.IsAlive)
                    dead.Add(entry.Key);
            }

            for (int i = 0; i < dead.Count; i++)
                Cache.Remove(dead[i]);
        }

        internal static ThemeImageData GetSized(string className, int part, int state, IntPtr window,
            int pixelWidth, int pixelHeight, System.Drawing.Color? background = null, string subClass = null)
        {
            if (string.IsNullOrEmpty(className) || !ThemeSystem.IsVisualStylesEnabled)
                return null;
            if (pixelWidth <= 0 || pixelHeight <= 0 || IsGlyph(className, part))
                return Get(className, part, state, window, background);

            ThemeInfo theme = ThemeSystem.GetCurrentTheme();
            string key = string.Concat(
                theme.FileName, "|", theme.ColorScheme, "|", theme.SizeName, "|",
                className, "|", part.ToString(CultureInfo.InvariantCulture), "|",
                state.ToString(CultureInfo.InvariantCulture), "|",
                window.ToInt64().ToString(CultureInfo.InvariantCulture), "|",
                DpiScale.ToString("R", CultureInfo.InvariantCulture), "|",
                pixelWidth.ToString(CultureInfo.InvariantCulture), "x",
                pixelHeight.ToString(CultureInfo.InvariantCulture), "|bg=", BackgroundKey(background),
                "|sub=", subClass ?? string.Empty);

            lock (Gate)
            {
                WeakReference reference;
                if (Cache.TryGetValue(key, out reference) && reference.IsAlive)
                    return (ThemeImageData)reference.Target;

                ThemeImageData data = RenderSized(className, part, state, window, pixelWidth, pixelHeight, background, subClass);
                if (data != null)
                {
                    if (Cache.Count >= MaxCacheEntries)
                        PurgeLocked();
                    Cache[key] = new WeakReference(data);
                    _createdCount++;
                }
                return data;
            }
        }

        internal static ThemeImageData GetAlphaNatural(string className, int part, int state, IntPtr window, string subClass = null)
        {
            if (string.IsNullOrEmpty(className) || !ThemeSystem.IsVisualStylesEnabled)
                return null;

            ThemeInfo theme = ThemeSystem.GetCurrentTheme();
            string key = string.Concat(
                theme.FileName, "|", theme.ColorScheme, "|", theme.SizeName, "|",
                className, "|", part.ToString(CultureInfo.InvariantCulture), "|",
                state.ToString(CultureInfo.InvariantCulture), "|",
                window.ToInt64().ToString(CultureInfo.InvariantCulture), "|",
                DpiScale.ToString("R", CultureInfo.InvariantCulture), "|alpha|",
                subClass ?? string.Empty);

            lock (Gate)
            {
                WeakReference reference;
                if (Cache.TryGetValue(key, out reference) && reference.IsAlive)
                    return (ThemeImageData)reference.Target;

                ThemeImageData data = RenderAlphaNatural(className, part, state, window, subClass);
                if (data != null)
                {
                    if (Cache.Count >= MaxCacheEntries)
                        PurgeLocked();
                    Cache[key] = new WeakReference(data);
                    _createdCount++;
                }
                return data;
            }
        }

        internal static int AlphaProbeLimit
        {
            get { return 2; }
        }

        private static ThemeImageData RenderAlphaNatural(string className, int part, int state, IntPtr window, string subClass)
        {
            using (VisualStyle style = OpenStyle(className, window, subClass))
            {
                if (style == null)
                    return null;

                int actualPart;
                System.Drawing.Size natural;
                TryGetPartSizeWithFallback(style, className, part, state, out actualPart, out natural);
                return RenderReconstructedAlpha(style, actualPart, state, natural.Width, natural.Height);
            }
        }

        private static ThemeImageData RenderReconstructedAlpha(VisualStyle style, int part, int state,
            int width, int height)
        {
            if (width <= 0 || height <= 0)
                return null;

            const int darkLevel = 63;
            const int lightLevel = 191;
            const int levelDelta = lightLevel - darkLevel;

            System.Drawing.Color dark = System.Drawing.Color.FromArgb(darkLevel, darkLevel, darkLevel);
            System.Drawing.Color light = System.Drawing.Color.FromArgb(lightLevel, lightLevel, lightLevel);
            BitmapSource onDark = ThemeDibRenderer.Render(width, height, DpiScale, dark,
                delegate(IntPtr hdc)
                {
                    ThemeRenderer.TryDrawBackground(hdc, style, part, state,
                        new System.Drawing.Rectangle(0, 0, width, height));
                });
            BitmapSource onLight = ThemeDibRenderer.Render(width, height, DpiScale, light,
                delegate(IntPtr hdc)
                {
                    ThemeRenderer.TryDrawBackground(hdc, style, part, state,
                        new System.Drawing.Rectangle(0, 0, width, height));
                });
            if (onDark == null || onLight == null)
                return null;

            int stride = width * 4;
            var darkPixels = new byte[stride * height];
            var lightPixels = new byte[stride * height];
            onDark.CopyPixels(darkPixels, stride, 0);
            onLight.CopyPixels(lightPixels, stride, 0);

            var output = new byte[stride * height];
            for (int i = 0; i < darkPixels.Length; i += 4)
            {
                int alphaSum = 0;
                for (int c = 0; c < 3; c++)
                {
                    int darkValue = darkPixels[i + c];
                    int lightValue = lightPixels[i + c];
                    int a = 255 - (lightValue - darkValue) * 255 / levelDelta;
                    if (a < 0) a = 0;
                    if (a > 255) a = 255;
                    alphaSum += a;
                }

                int alpha = alphaSum / 3;
                if (alpha < 3)
                {
                    output[i] = 0;
                    output[i + 1] = 0;
                    output[i + 2] = 0;
                    output[i + 3] = 0;
                    continue;
                }

                output[i + 3] = (byte)alpha;
                int darkContribution = darkLevel * (255 - alpha);
                for (int c = 0; c < 3; c++)
                {
                    int value = (darkPixels[i + c] * 255 - darkContribution) / alpha;
                    if (value < 0) value = 0;
                    if (value > 255) value = 255;
                    output[i + c] = (byte)value;
                }
            }

            BitmapSource image = BitmapSource.Create(width, height,
                96.0 * DpiScale, 96.0 * DpiScale, PixelFormats.Bgra32, null, output, stride);
            image.Freeze();
            return new ThemeImageData(image, new Thickness(0), new Thickness(0),
                new Thickness(0), false);
        }
        private static ThemeImageData RenderRawAlpha(VisualStyle style, int part, int state)
        {
            IntPtr hbitmap;
            if (!ThemeRenderer.TryGetBitmapHandle(style, part, state, ThemeBitmapKind.Background, out hbitmap))
                return null;

            NativeGdi.BITMAP bitmapInfo;
            if (NativeGdi.GetObject(hbitmap, Marshal.SizeOf(typeof(NativeGdi.BITMAP)), out bitmapInfo) == 0)
                return null;

            int width = bitmapInfo.bmWidth;
            int height = bitmapInfo.bmHeight;
            if (width <= 0 || height <= 0)
                return null;

            var info = new NativeGdi.BITMAPINFO();
            info.bmiHeader.biSize = Marshal.SizeOf(typeof(NativeGdi.BITMAPINFOHEADER));
            info.bmiHeader.biWidth = width;
            info.bmiHeader.biHeight = height;
            info.bmiHeader.biPlanes = 1;
            info.bmiHeader.biBitCount = 32;
            info.bmiHeader.biCompression = 0;

            int stride = width * 4;
            var pixels = new byte[stride * height];
            IntPtr dc = NativeGdi.CreateCompatibleDC(IntPtr.Zero);
            if (dc == IntPtr.Zero)
                return null;
            try
            {
                if (NativeGdi.GetDIBits(dc, hbitmap, 0, (uint)height, pixels, ref info, NativeGdi.DIB_RGB_COLORS) == 0)
                    return null;
            }
            finally
            {
                NativeGdi.DeleteDC(dc);
            }

            var topDown = new byte[pixels.Length];
            for (int y = 0; y < height; y++)
                Buffer.BlockCopy(pixels, (height - 1 - y) * stride, topDown, y * stride, stride);

            BitmapSource image = BitmapSource.Create(width, height,
                96.0 * DpiScale, 96.0 * DpiScale,
                PixelFormats.Bgra32, null, topDown, stride);
            image.Freeze();
            return new ThemeImageData(image, new Thickness(0), new Thickness(0),
                new Thickness(0), false);
        }
        internal static ThemeImageData GetNatural(string className, int part, int state, IntPtr window, string subClass = null, System.Drawing.Color? background = null)
        {
            if (string.IsNullOrEmpty(className) || !ThemeSystem.IsVisualStylesEnabled)
                return null;

            ThemeInfo theme = ThemeSystem.GetCurrentTheme();
            string key = string.Concat(
                theme.FileName, "|", theme.ColorScheme, "|", theme.SizeName, "|",
                className, "|", part.ToString(CultureInfo.InvariantCulture), "|",
                state.ToString(CultureInfo.InvariantCulture), "|",
                window.ToInt64().ToString(CultureInfo.InvariantCulture), "|",
                DpiScale.ToString("R", CultureInfo.InvariantCulture), "|natural|",
                subClass ?? string.Empty, "|bg=", BackgroundKey(background));

            lock (Gate)
            {
                WeakReference reference;
                if (Cache.TryGetValue(key, out reference) && reference.IsAlive)
                    return (ThemeImageData)reference.Target;

                ThemeImageData data = RenderNatural(className, part, state, window, subClass, background);
                if (data != null)
                {
                    if (Cache.Count >= MaxCacheEntries)
                        PurgeLocked();
                    Cache[key] = new WeakReference(data);
                    _createdCount++;
                }
                return data;
            }
        }

        private static ThemeImageData RenderAlpha(VisualStyle style, int part, int state)
        {
            using (System.Drawing.Bitmap bitmap = ThemeRenderer.GetBitmap(style, part, state,
                ThemeBitmapKind.Background))
            {
                if (bitmap == null || bitmap.Width <= 0 || bitmap.Height <= 0)
                    return null;

                System.Drawing.Imaging.BitmapData sourceData = bitmap.LockBits(
                    new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    System.Drawing.Imaging.ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                try
                {
                    int stride = sourceData.Stride;
                    var pixels = new byte[stride * bitmap.Height];
                    System.Runtime.InteropServices.Marshal.Copy(sourceData.Scan0, pixels, 0, pixels.Length);

                    BitmapSource image = BitmapSource.Create(bitmap.Width, bitmap.Height,
                        96.0 * DpiScale, 96.0 * DpiScale,
                        PixelFormats.Bgra32, null, pixels, stride);
                    image.Freeze();
                    return new ThemeImageData(image, new Thickness(0), new Thickness(0),
                        new Thickness(0), false);
                }
                finally
                {
                    bitmap.UnlockBits(sourceData);
                }
            }
        }
        private static ThemeImageData RenderNatural(string className, int part, int state, IntPtr window, string subClass, System.Drawing.Color? background)
        {
            using (VisualStyle style = OpenStyle(className, window, subClass))
            {
                if (style == null)
                    return null;

                int actualPart;
                System.Drawing.Size natural;
                TryGetPartSizeWithFallback(style, className, part, state, out actualPart, out natural);
                if (natural.Width <= 0 || natural.Height <= 0)
                    return null;


                ThemeMargins margins;
                if (!TryGetSizingMargins(style, actualPart, state, out margins))
                    margins = new ThemeMargins(0, 0, 0, 0);

                ThemeMargins contentMargins;
                bool hasContentMargins = TryGetContentMargins(style, actualPart, state, out contentMargins);
                if (!hasContentMargins)
                    contentMargins = new ThemeMargins(0, 0, 0, 0);

                System.Drawing.Color actualBackground = ResolveBackground(style, className, actualPart, state,
                    background ?? (string.Equals(className, "Edit", StringComparison.Ordinal)
                        ? System.Drawing.SystemColors.Window
                        : System.Drawing.SystemColors.Control));

                BitmapSource image = ThemeDibRenderer.Render(natural.Width, natural.Height, DpiScale, actualBackground,
                    delegate(IntPtr hdc)
                    {
                        ThemeRenderer.TryDrawBackground(hdc, style, actualPart, state,
                            new System.Drawing.Rectangle(0, 0, natural.Width, natural.Height));
                    });

                if (image == null)
                    return null;

                return new ThemeImageData(image,
                    new Thickness(margins.Left, margins.Top, margins.Right, margins.Bottom),
                    new Thickness(margins.Left, margins.Top, margins.Right, margins.Bottom),
                    new Thickness(contentMargins.Left, contentMargins.Top, contentMargins.Right, contentMargins.Bottom),
                    hasContentMargins);
            }
        }

        private static ThemeImageData RenderSized(string className, int part, int state, IntPtr window,
            int pixelWidth, int pixelHeight, System.Drawing.Color? background, string subClass)
        {
            using (VisualStyle style = OpenStyle(className, window, subClass))
            {
                if (style == null)
                    return null;

                int actualPart;
                System.Drawing.Size natural;
                TryGetPartSizeWithFallback(style, className, part, state, out actualPart, out natural);


                System.Drawing.Color actualBackground = ResolveBackground(style, className, actualPart, state,
                    background ?? (string.Equals(className, "Edit", StringComparison.Ordinal)
                        ? System.Drawing.SystemColors.Window
                        : System.Drawing.SystemColors.Control));

                BitmapSource image = ThemeDibRenderer.Render(pixelWidth, pixelHeight, DpiScale, actualBackground,
                    delegate(IntPtr hdc)
                    {
                        ThemeRenderer.TryDrawBackground(hdc, style, actualPart, state,
                            new System.Drawing.Rectangle(0, 0, pixelWidth, pixelHeight));
                    });

                if (image == null)
                    return null;

                ThemeMargins contentMargins;
                bool hasContentMargins = TryGetContentMargins(style, actualPart, state, out contentMargins);
                if (!hasContentMargins)
                    contentMargins = new ThemeMargins(0, 0, 0, 0);

                ThemeMargins sizingMargins;
                if (!TryGetSizingMargins(style, actualPart, state, out sizingMargins))
                    sizingMargins = new ThemeMargins(0, 0, 0, 0);

                return new ThemeImageData(image, new Thickness(0),
                    new Thickness(sizingMargins.Left, sizingMargins.Top, sizingMargins.Right, sizingMargins.Bottom),
                    new Thickness(contentMargins.Left, contentMargins.Top, contentMargins.Right, contentMargins.Bottom),
                    hasContentMargins);
            }
        }

        private static ThemeImageData Render(string className, int part, int state, IntPtr window, System.Drawing.Color? background)
        {
            using (VisualStyle style = OpenStyle(className, window))
            {
                if (style == null)
                    return null;


                int actualPart;
                System.Drawing.Size natural;
                TryGetPartSizeWithFallback(style, className, part, state, out actualPart, out natural);

                bool glyph = IsGlyph(className, actualPart);
                ThemeMargins margins = new ThemeMargins(0, 0, 0, 0);
                bool hasMargins = !glyph && TryGetMargins(style, actualPart, state, className, out margins);

                int width;
                int height;
                if (hasMargins)
                {
                    width = Math.Max(natural.Width, 64);
                    height = Math.Max(natural.Height, 40);
                }
                else
                {
                    width = Math.Max(natural.Width, 1);
                    height = Math.Max(natural.Height, 1);
                    margins = new ThemeMargins(0, 0, 0, 0);
                }

                int left = ClampMargin(margins.Left, width);
                int top = ClampMargin(margins.Top, height);
                int right = ClampMargin(margins.Right, width);
                int bottom = ClampMargin(margins.Bottom, height);
                if (left + right >= width)
                {
                    left = width / 4;
                    right = width / 4;
                }
                if (top + bottom >= height)
                {
                    top = height / 4;
                    bottom = height / 4;
                }

                System.Drawing.Color actualBackground = ResolveBackground(style, className, actualPart, state,
                    background ?? (string.Equals(className, "Edit", StringComparison.Ordinal)
                        ? System.Drawing.SystemColors.Window
                        : System.Drawing.SystemColors.Control));

                int pixelWidth = width;
                int pixelHeight = height;
                BitmapSource image = ThemeDibRenderer.Render(pixelWidth, pixelHeight, DpiScale, actualBackground,
                    delegate(IntPtr hdc)
                    {
                        ThemeRenderer.TryDrawBackground(hdc, style, actualPart, state,
                            new System.Drawing.Rectangle(0, 0, pixelWidth, pixelHeight));
                    });

                if (image == null)
                    return null;

                ThemeMargins contentMargins;
                bool hasContentMargins = TryGetContentMargins(style, actualPart, state, out contentMargins);
                if (!hasContentMargins)
                    contentMargins = new ThemeMargins(0, 0, 0, 0);

                ThemeMargins sizingMargins;
                if (!TryGetSizingMargins(style, actualPart, state, out sizingMargins))
                    sizingMargins = new ThemeMargins(0, 0, 0, 0);

                return new ThemeImageData(image, new Thickness(left, top, right, bottom),
                    new Thickness(sizingMargins.Left, sizingMargins.Top, sizingMargins.Right, sizingMargins.Bottom),
                    new Thickness(contentMargins.Left, contentMargins.Top, contentMargins.Right, contentMargins.Bottom),
                    hasContentMargins);
            }
        }

        private static VisualStyle OpenStyle(string className, IntPtr window)
        {
            return OpenStyle(className, window, null);
        }

        private static VisualStyle OpenStyle(string className, IntPtr window, string subClass)
        {
            // Always try the real window first.  On XP the process-wide global
            // handle can stay pinned to the previous visual style for the whole
            // process lifetime; the window handle gets the style currently applied
            // to that window.  Fall back to global only when the window handle has
            // no theme for this class.
            VisualStyle style = VisualStyle.Open(window, className, subClass);
            if (style == null && window != IntPtr.Zero)
                style = VisualStyle.Open(window, className);
            if (style == null)
                style = VisualStyle.Open(className);
            return style;
        }

        private static void TryGetPartSizeWithFallback(VisualStyle style, string className,
            int part, int state, out int actualPart, out System.Drawing.Size natural)
        {
            int[] candidates = GetPartCandidates(className, part);
            for (int i = 0; i < candidates.Length; i++)
            {
                if (TryGetPartSize(style, candidates[i], state, out natural))
                {
                    actualPart = candidates[i];
                    return;
                }
            }

            actualPart = part;
            natural = new System.Drawing.Size(16, 16);
        }

        private static bool TryGetPartSize(VisualStyle style, int part, int state, out System.Drawing.Size natural)
        {
            if (style.TryGetPartSize(part, state, ThemeSize.True, out natural)
                && natural.Width > 0 && natural.Height > 0)
            {
                return true;
            }

            return style.TryGetPartSize(part, state, ThemeSize.Min, out natural)
                && natural.Width > 0 && natural.Height > 0;
        }

        private static int[] GetPartCandidates(string className, int part)
        {
            if (string.Equals(className, "Tab", StringComparison.Ordinal))
            {
                if (part == 5) return IsXpOrOlder ? new[] { 1, 5 } : new[] { 5, 1 };
                if (part == 9) return IsXpOrOlder ? new[] { 10, 9 } : new[] { 9, 10 };
            }
            if (string.Equals(className, "Progress", StringComparison.Ordinal))
            {
                if (part == 5) return IsXpOrOlder ? new[] { 3, 5 } : new[] { 5, 3 };
                if (part == 6) return IsXpOrOlder ? new[] { 4, 6 } : new[] { 6, 4 };
            }
            if (string.Equals(className, "Menu", StringComparison.Ordinal))
            {
                if (part == 14) return IsXpOrOlder ? new[] { 1, 14 } : new[] { 14, 1 };
                if (part == 16) return IsXpOrOlder ? new[] { 5, 16 } : new[] { 16, 5 };
                if (part == 15) return IsXpOrOlder ? new[] { 6, 15 } : new[] { 15, 6 };
            }

            return new[] { part };
        }

        internal static bool TryGetProgressMetrics(out int chunkSize, out int spaceSize)
        {
            return TryGetProgressMetrics(1, out chunkSize, out spaceSize);
        }

        internal static bool TryGetProgressMetrics(int part, out int chunkSize, out int spaceSize)
        {
            chunkSize = 0;
            spaceSize = 0;

            using (VisualStyle style = OpenStyle("Progress", IntPtr.Zero))
            {
                if (style == null)
                    return false;

                bool hasChunk = style.TryGetInt(part, 1, TmtProgressChunkSize, out chunkSize);
                bool hasSpace = style.TryGetInt(part, 1, TmtProgressSpaceSize, out spaceSize);
                if (!hasChunk)
                    hasChunk = style.TryGetInt(1, 1, TmtProgressChunkSize, out chunkSize);
                if (!hasSpace)
                    hasSpace = style.TryGetInt(1, 1, TmtProgressSpaceSize, out spaceSize);
                return hasChunk || hasSpace;
            }
        }

        private static bool TryGetSizingMargins(VisualStyle style, int part, int state, out ThemeMargins margins)
        {
            return style.TryGetMargins(part, state, TmtSizingMargins, out margins);
        }

        private static bool TryGetContentMargins(VisualStyle style, int part, int state, out ThemeMargins margins)
        {
            return style.TryGetMargins(part, state, TmtContentMargins, out margins);
        }

        private static bool TryGetMargins(VisualStyle style, int part, int state, string className,
            out ThemeMargins margins)
        {
            if (style.TryGetMargins(part, state, TmtSizingMargins, out margins)
                && margins.Left + margins.Right + margins.Top + margins.Bottom > 0)
            {
                return true;
            }

            if (style.TryGetMargins(part, state, TmtContentMargins, out margins)
                && margins.Left + margins.Right + margins.Top + margins.Bottom > 0)
            {
                return true;
            }

            int borderSize;
            if (style.TryGetInt(part, state, TmtBorderSize, out borderSize) && borderSize > 0)
            {
                margins = new ThemeMargins(borderSize, borderSize, borderSize, borderSize);
                return true;
            }

            // Windows XP does not expose TMT_SIZINGMARGINS for every class; use conservative
            // defaults so the nine-patch corners still stay fixed.
            if (string.Equals(className, "Button", StringComparison.Ordinal) && part == 1)
            {
                margins = new ThemeMargins(4, 4, 4, 4);
                return true;
            }
            if (string.Equals(className, "Edit", StringComparison.Ordinal) && part == 1)
            {
                margins = new ThemeMargins(2, 2, 2, 2);
                return true;
            }
            if (string.Equals(className, "Button", StringComparison.Ordinal) && part == 4)
            {
                margins = new ThemeMargins(4, 4, 4, 4);        // GroupBox frame
                return true;
            }
            if (string.Equals(className, "Tab", StringComparison.Ordinal) && (part == 1 || part == 5 || part == 9 || part == 10))
            {
                margins = new ThemeMargins(3, 3, 3, 3);
                return true;
            }
            if (string.Equals(className, "Progress", StringComparison.Ordinal) && (part == 1 || part == 2 || part == 3 || part == 4))
            {
                margins = new ThemeMargins(2, 2, 2, 2);
                return true;
            }
            if (string.Equals(className, "Status", StringComparison.Ordinal) && part == 1)
            {
                margins = new ThemeMargins(2, 2, 2, 2);
                return true;
            }
            if (string.Equals(className, "Menu", StringComparison.Ordinal) && (part == 1 || part == 5 || part == 6 || part == 9 || part == 10))
            {
                margins = new ThemeMargins(2, 2, 2, 2);
                return true;
            }
            if (string.Equals(className, "Tooltip", StringComparison.Ordinal) && part == 1)
            {
                margins = new ThemeMargins(4, 4, 4, 4);
                return true;
            }
            if (string.Equals(className, "Combobox", StringComparison.Ordinal) && part == 4)
            {
                margins = new ThemeMargins(2, 2, 2, 2);
                return true;
            }
            if (string.Equals(className, "Scrollbar", StringComparison.Ordinal) && part >= 4 && part <= 7)
            {
                margins = new ThemeMargins(2, 2, 2, 2);
                return true;
            }

            margins = new ThemeMargins(0, 0, 0, 0);
            return false;
        }

        private static bool IsGlyph(string className, int part)
        {
            return string.Equals(className, "Button", StringComparison.Ordinal)
                && (part == 2 || part == 3);
        }

        private static int ClampMargin(int value, int limit)
        {
            if (value <= 0)
                return 0;
            if (value > limit / 2)
                return limit / 2;
            return value;
        }

        private static double ComputeDpiScale()
        {
            try
            {
                using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
                {
                    double scale = graphics.DpiX / 96.0;
                    return scale <= 0 ? 1.0 : scale;
                }
            }
            catch (ArgumentException)
            {
                return 1.0;
            }
        }
    }
}
