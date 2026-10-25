using System;
using System.Windows;
using System.Windows.Media;
using ThemeExplorer.Core;
using DrawingColor = System.Drawing.Color;

namespace Microsoft.Windows.Themes
{
    /// <summary>
    /// Theme-derived shell colors.
    /// <para xml:lang="zh-CN">
    /// 从当前 UXTHEME 主题中读取的外壳颜色。任务栏前景来自
    /// <c>Toolbar [TaskBar]</c> 的 <c>TMT_TEXTCOLOR</c>，托盘图标区域前景来自
    /// <c>TrayNotify [TrayNotifyHoriz]</c> 的 <c>TMT_TEXTCOLOR</c>。
    /// </para>
    /// </summary>
    public static class ThemeColors
    {
        // Windows SDK TMT_* values.
        private const int TmtTextColor = 3803;
        private const int TmtTextColorHint = 3824;
        private const int TmtFillColor = 3802;
        private const int TmtFillColorHint = 3821;

        /// <summary>
        /// Foreground color for taskbar text, from <c>Toolbar [TaskBar]</c>.
        /// <para xml:lang="zh-CN">任务栏文字前景色，来自 <c>Toolbar [TaskBar]</c>。</para>
        /// </summary>
        public static Color TaskbarForeground
        {
            get
            {
                Color color;
                if (TryGetTextColor("Toolbar", "TaskBar", new[] { 0, 1 }, out color))
                    return color;

                Color background;
                if (TryGetBackgroundColor("Toolbar", "TaskBar", new[] { 0, 1 }, out background))
                    return GetContrastColor(background);

                // Taskbar text is white in the classic and Aero taskbars when
                // the selected visual style does not publish a text color.
                return Colors.White;
            }
        }

        /// <summary>
        /// Foreground color for the notification area, from
        /// <c>TrayNotify [TrayNotifyHoriz]</c>.
        /// <para xml:lang="zh-CN">
        /// 通知区域文字前景色，来自 <c>TrayNotify [TrayNotifyHoriz]</c>。
        /// </para>
        /// </summary>
        public static Color TrayNotifyForeground
        {
            get
            {
                Color color;
                if (TryGetTextColor("TrayNotify", "TrayNotifyHoriz", new[] { 0, 1 }, out color))
                    return color;

                Color background;
                if (TryGetBackgroundColor("TrayNotify", "TrayNotifyHoriz", new[] { 0, 1 }, out background))
                    return GetContrastColor(background);

                // The notification area sits on the taskbar. If TrayNotify does
                // not publish a text color, keep the taskbar's foreground.
                return TaskbarForeground;
            }
        }

        private static bool TryGetTextColor(string className, string subClass, int[] parts, out Color color)
        {
            color = default(Color);
            using (VisualStyleContext context = new VisualStyleContext())
            using (VisualStyle theme = context.Open(className, subClass))
            {
                if (theme == null)
                    return false;

                foreach (int part in parts)
                {
                    if (TryReadColor(theme, part, 0, TmtTextColor, out color) ||
                        TryReadColor(theme, part, 0, TmtTextColorHint, out color))
                        return true;
                }
            }

            return false;
        }

        private static bool TryGetBackgroundColor(string className, string subClass, int[] parts, out Color color)
        {
            color = default(Color);
            using (VisualStyleContext context = new VisualStyleContext())
            using (VisualStyle theme = context.Open(className, subClass))
            {
                if (theme == null)
                    return false;

                foreach (int part in parts)
                {
                    if (TryReadColor(theme, part, 0, TmtFillColorHint, out color) ||
                        TryReadColor(theme, part, 0, TmtFillColor, out color))
                        return true;
                }
            }

            return false;
        }

        private static bool TryReadColor(VisualStyle theme, int part, int state, int propertyId, out Color color)
        {
            DrawingColor drawing;
            if (theme.TryGetColor(part, state, propertyId, out drawing))
            {
                color = Color.FromRgb(drawing.R, drawing.G, drawing.B);
                return true;
            }

            color = default(Color);
            return false;
        }

        private static Color GetContrastColor(Color background)
        {
            // Relative luminance, using the standard sRGB coefficients. The
            // threshold is intentionally close to the perceptual midpoint so a
            // dark taskbar gets white text and a light one gets black text.
            double luminance = (0.2126 * background.R + 0.7152 * background.G + 0.0722 * background.B) / 255.0;
            return luminance > 0.5 ? Colors.Black : Colors.White;
        }
    }
}
