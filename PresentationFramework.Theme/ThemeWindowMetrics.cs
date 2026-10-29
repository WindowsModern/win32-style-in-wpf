using System;
using System.Runtime.InteropServices;
using System.Windows;
using ThemeExplorer.Core;

namespace Microsoft.Windows.Themes
{
    /// <summary>
    /// Window non-client metrics taken from the current system metrics.
    /// <para xml:lang="zh-CN">从当前系统度量取得的窗口非客户区尺寸。</para>
    /// </summary>
    public static class ThemeWindowMetrics
    {
        private const int SmCyCaption = 4;
        private const int SmCxSizeFrame = 32;
        private const int SmCySizeFrame = 33;
        private const int SmCySmCaption = 52;
        private const int SmCxPaddedBorder = 92;

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        /// <summary>
        /// Content margin for an active normal window:
        /// left, title height, right, bottom.
        /// <para xml:lang="zh-CN">活动普通窗口的内容区边距：左、标题高度、右、底。</para>
        /// </summary>
        public static Thickness NormalWindowContentMargin
        {
            get { return GetContentMargin(false); }
        }

        /// <summary>
        /// Content margin for an active tool window:
        /// left, title height, right, bottom.
        /// <para xml:lang="zh-CN">活动工具窗口的内容区边距：左、标题高度、右、底。</para>
        /// </summary>
        public static Thickness ToolWindowContentMargin
        {
            get { return GetContentMargin(true); }
        }

        /// <summary>
        /// Width of the native resize frame at the left and right edges.
        /// <para xml:lang="zh-CN">原生窗口左右缩放边框宽度。</para>
        /// </summary>
        public static double WindowBorderWidth
        {
            get
            {
                int width = GetSystemMetrics(SmCxSizeFrame) + GetSystemMetrics(SmCxPaddedBorder);
                return width > 0 ? ToDip(width) : SystemParameters.ResizeFrameVerticalBorderWidth;
            }
        }

        /// <summary>
        /// Height of the native resize frame at the bottom edge.
        /// <para xml:lang="zh-CN">原生窗口底边缩放边框高度。</para>
        /// </summary>
        public static double WindowBorderHeight
        {
            get
            {
                int height = GetSystemMetrics(SmCySizeFrame) + GetSystemMetrics(SmCxPaddedBorder);
                return height > 0 ? ToDip(height) : SystemParameters.ResizeFrameHorizontalBorderHeight;
            }
        }

        /// <summary>
        /// Caption corner radius for an active normal window.
        /// <para xml:lang="zh-CN">活动普通窗口标题栏圆角半径。</para>
        /// </summary>
        public static double NormalWindowCornerRadius
        {
            get { return GetCornerRadius(1); }
        }

        /// <summary>
        /// Caption corner radius for an active tool window.
        /// <para xml:lang="zh-CN">活动工具窗口标题栏圆角半径。</para>
        /// </summary>
        public static double ToolWindowCornerRadius
        {
            get { return GetCornerRadius(2); }
        }

        /// <summary>
        /// Height of an active normal-window title area.
        /// <para xml:lang="zh-CN">活动普通窗口标题区高度。</para>
        /// </summary>
        public static double NormalWindowTitleHeight
        {
            get
            {
                int height = GetSystemMetrics(SmCyCaption);
                return height > 0 ? ToDip(height) : SystemParameters.WindowCaptionHeight;
            }
        }

        /// <summary>
        /// Height of an active tool-window title area.
        /// <para xml:lang="zh-CN">活动工具窗口标题区高度。</para>
        /// </summary>
        public static double ToolWindowTitleHeight
        {
            get
            {
                int height = GetSystemMetrics(SmCySmCaption);
                return height > 0 ? ToDip(height) : NormalWindowTitleHeight;
            }
        }

        /// <summary>
        /// Caption font size for an active normal window.
        /// <para xml:lang="zh-CN">活动普通窗口标题栏字号。</para>
        /// </summary>
        public static double NormalWindowTitleFontSize
        {
            get { return GetTitleFontSize(1, 801, SystemFonts.CaptionFontSize); }
        }

        /// <summary>
        /// Caption font size for an active tool window.
        /// <para xml:lang="zh-CN">活动工具窗口标题栏字号。</para>
        /// </summary>
        public static double ToolWindowTitleFontSize
        {
            get { return GetTitleFontSize(2, 802, SystemFonts.SmallCaptionFontSize); }
        }

        private static double GetTitleFontSize(int part, int property, double fallback)
        {
            using (VisualStyleContext context = new VisualStyleContext())
            using (VisualStyle style = context.Open("Window"))
            {
                ThemeFontInfo font;
                if (style != null &&
                    (style.TryGetSystemFont(property, out font) ||
                     style.TryGetFont(part, 1, property, out font)) &&
                    font.Height != 0)
                {
                    double size = Math.Abs(font.Height) / ThemeImageProvider.Dpi;
                    if (size > 0) return size;
                }
            }

            return fallback;
        }

        private static Thickness GetContentMargin(bool toolWindow)
        {
            double border = WindowBorderWidth;
            double title = toolWindow ? ToolWindowTitleHeight : NormalWindowTitleHeight;
            return new Thickness(border, title, border, WindowBorderHeight);
        }

        private static double GetCornerRadius(int part)
        {
            // Measure the transparent corner pixels of the caption (top) and
            // the bottom frame.  PreserveAlpha only controls rendering; this
            // measurement is independent of that switch.
            double top = MeasurePartCorner(part, true);
            double bottom = MeasurePartCorner(part == 1 ? 9 : 12, false);
            return Math.Max(0, Math.Max(top, bottom));
        }

        private static double MeasurePartCorner(int part, bool top)
        {
            ThemeImageData data = ThemeImageProvider.GetAlphaNatural(
                "Window", part, 1, IntPtr.Zero);
            if (data == null || data.Image == null)
                return 0;

            System.Windows.Media.Imaging.BitmapSource image = data.Image;
            int width = image.PixelWidth;
            int height = image.PixelHeight;
            if (width < 3 || height < 3)
                return 0;

            int stride = width * 4;
            var pixels = new byte[stride * height];
            image.CopyPixels(pixels, stride, 0);

            double left = MeasureEdge(pixels, width, height, stride, true, top);
            double right = MeasureEdge(pixels, width, height, stride, false, top);
            double radius = (left + right) / 2.0;
            if (radius <= 0)
                return 0;
            return ToDip((int)Math.Round(radius));
        }

        private static double MeasureEdge(byte[] pixels, int width, int height, int stride, bool left, bool top)
        {
            // Walk the outer row and outer column until the first opaque pixel.
            // For a rounded rectangle both distances approximate the radius.
            int edgeY = top ? 0 : height - 1;
            int edgeX = left ? 0 : width - 1;
            int dx = left ? 1 : -1;
            int dy = top ? 1 : -1;
            int horizontal = 0;
            for (int x = edgeX, i = 0; i < width; i++, x += dx)
            {
                if (pixels[x * 4 + 3] > 16)
                {
                    horizontal = i;
                    break;
                }
            }

            int vertical = 0;
            for (int y = edgeY, i = 0; i < height; i++, y += dy)
            {
                if (pixels[y * stride + edgeX * 4 + 3] > 16)
                {
                    vertical = i;
                    break;
                }
            }

            if (horizontal == 0 && vertical == 0)
                return 0;
            return (horizontal + vertical) / 2.0;
        }
        private static double ToDip(int pixels)
        {
            double scale = ThemeImageProvider.Dpi;
            return scale > 0 ? pixels / scale : pixels;
        }
    }
}
