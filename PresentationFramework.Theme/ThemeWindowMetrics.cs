using System;
using System.Windows;
using ThemeExplorer.Core;
using DrawingSize = System.Drawing.Size;

namespace Microsoft.Windows.Themes
{
    /// <summary>
    /// Window non-client metrics taken from the active UXTHEME Window class.
    /// <para xml:lang="zh-CN">从当前 UXTHEME Window 类取得的窗口非客户区尺寸。</para>
    /// </summary>
    public static class ThemeWindowMetrics
    {
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
        /// Height of an active normal-window title area.
        /// <para xml:lang="zh-CN">活动普通窗口标题区高度。</para>
        /// </summary>
        public static double NormalWindowTitleHeight
        {
            get { return GetPartDimension(1, true); }
        }

        /// <summary>
        /// Height of an active tool-window title area.
        /// <para xml:lang="zh-CN">活动工具窗口标题区高度。</para>
        /// </summary>
        public static double ToolWindowTitleHeight
        {
            get { return GetPartDimension(2, true); }
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
            double left = GetPartDimension(toolWindow ? 10 : 7, false);
            double title = GetPartDimension(toolWindow ? 2 : 1, true);
            double right = GetPartDimension(toolWindow ? 11 : 8, false);
            double bottom = GetPartDimension(toolWindow ? 12 : 9, true);
            return new Thickness(left, title, right, bottom);
        }

        private static double GetPartDimension(int part, bool height)
        {
            DrawingSize size;
            if (TryGetPartSize(part, out size))
                return ToDip(height ? size.Height : size.Width);

            // Conservative XP defaults when the current visual style does not
            // expose a Window part.
            if (part == 1)
                return 28;
            if (part == 2)
                return 18;
            return 4;
        }

        private static bool TryGetPartSize(int part, out DrawingSize size)
        {
            using (VisualStyleContext context = new VisualStyleContext())
            using (VisualStyle style = context.Open("Window"))
            {
                if (style != null &&
                    (style.TryGetPartSize(part, 1, ThemeSize.True, out size) ||
                     style.TryGetPartSize(part, 1, ThemeSize.Min, out size)))
                    return true;
            }

            size = DrawingSize.Empty;
            return false;
        }

        private static double ToDip(int pixels)
        {
            double scale = ThemeImageProvider.Dpi;
            return scale > 0 ? pixels / scale : pixels;
        }
    }
}
