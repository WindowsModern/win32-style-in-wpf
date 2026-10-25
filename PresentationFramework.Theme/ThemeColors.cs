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
    /// 从当前 UXTHEME 主题中读取的外壳颜色。任务栏文字颜色优先读取
    /// <c>TMT_TEXTCOLOR</c>，主题未提供时按任务栏背景的明度计算黑色/白色前景。
    /// </para>
    /// </summary>
    public static class ThemeColors
    {
        // Windows SDK TMT_* values.
        private const int TmtTextColor = 3803;
        private const int TmtTextColorHint = 3824;
        private const int TmtFillColorHint = 3821;

        /// <summary>
        /// Foreground color for taskbar text.
        /// <para xml:lang="zh-CN">任务栏文字的前景色。</para>
        /// </summary>
        public static Color TaskbarForeground
        {
            get
            {
                Color themed;
                if (TryGetTextColor(out themed))
                    return themed;

                Color background;
                if (TryGetBackgroundColor(out background))
                    return GetContrastColor(background);

                return SystemColors.WindowTextColor;
            }
        }

        private static bool TryGetTextColor(out Color color)
        {
            // The TaskBar class is split by edge, not by state. Prefer the bottom
            // edge, then the right edge, then part 0 for themes that put the
            // text color on the class itself.
            foreach (string className in new[] { "TaskBar", "Taskbar" })
            {
                using (VisualStyle theme = VisualStyle.Open(className))
                {
                    if (theme == null)
                        continue;

                    foreach (int part in new[] { 1, 2, 0 })
                    {
                        if (TryReadColor(theme, part, 0, TmtTextColor, out color) ||
                            TryReadColor(theme, part, 0, TmtTextColorHint, out color))
                            return true;
                    }
                }
            }

            color = default(Color);
            return false;
        }

        private static bool TryGetBackgroundColor(out Color color)
        {
            foreach (string className in new[] { "TaskBar", "Taskbar" })
            {
                using (VisualStyle theme = VisualStyle.Open(className))
                {
                    if (theme == null)
                        continue;

                    foreach (int part in new[] { 1, 2, 0 })
                    {
                        if (TryReadColor(theme, part, 0, TmtFillColorHint, out color))
                            return true;
                    }
                }
            }

            color = default(Color);
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
