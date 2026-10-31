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
    /// <c>Toolbar [TaskBar]</c> 按钮部件（part 1，normal 状态）的 <c>TMT_TEXTCOLOR</c>，
    /// 托盘图标区域前景来自 <c>TrayNotify [TrayNotifyHoriz]</c> 的 <c>TMT_TEXTCOLOR</c>。
    /// </para>
    /// </summary>
    public static class ThemeColors
    {
        static ThemeColors()
        {
            ThemeResourceRefresher.EnsureInitialized();
        }

        // Windows SDK TMT_* values.
        private const int TmtTextColor = 3803;
        private const int TmtTextColorHint = 3824;
        private const int TmtFillColor = 3802;
        private const int TmtFillColorHint = 3821;
        private const int TmtEdgeFillColor = 3808;
        private const int TmtEdgeLightColor = 3804;
        private const int TmtEdgeHighlightColor = 3805;
        private const int TmtEdgeShadowColor = 3806;
        private const int TmtEdgeDkShadowColor = 3807;

        /// <summary>
        /// Foreground color for taskbar text, from
        /// <c>Toolbar [TaskBar]</c> button (part 1, normal state)
        /// <c>TMT_TEXTCOLOR</c>. Falls back to the base <c>Toolbar</c> class
        /// when the subclass does not publish it.
        /// <para xml:lang="zh-CN">任务栏文字前景色，来自 <c>Toolbar [TaskBar]</c> 按钮部件（part 1，normal 状态）的 <c>TMT_TEXTCOLOR</c>；子类未发布时再回退到基础 <c>Toolbar</c> 类。</para>
        /// </summary>
        public static Color TaskbarForeground
        {
            get
            {
                Color color;
                if (TryGetTextColor("Toolbar", "TaskBar", 1, 1, out color))
                    return color;
                if (TryGetTextColor("Toolbar", null, 1, 1, out color))
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

        /// <summary>
        /// Foreground color for a normal active window caption.
        /// <para xml:lang="zh-CN">普通活动窗口标题栏的前景色。</para>
        /// </summary>
        public static Color NormalWindowForeground
        {
            get { return GetWindowTextColor(1, true); }
        }

        /// <summary>
        /// Foreground color for an active tool-window caption.
        /// <para xml:lang="zh-CN">活动工具窗口标题栏的前景色。</para>
        /// </summary>
        public static Color ToolWindowForeground
        {
            get { return GetWindowTextColor(2, false); }
        }

        private static Color GetWindowTextColor(int part, bool normalWindow)
        {
            using (VisualStyleContext context = new VisualStyleContext())
            using (VisualStyle theme = context.Open("Window"))
            {
                Color color;
                if (theme != null &&
                    (TryReadColor(theme, part, 1, TmtTextColor, out color) ||
                     TryReadColor(theme, part, 1, TmtTextColorHint, out color)))
                    return color;

                // COLOR_CAPTIONTEXT is the SysMetrics caption text color.
                // Prefer it over a fill-color guess: XP themes often publish
                // a light caption fill but still draw white caption text.
                DrawingColor systemColor;
                if (theme != null && theme.TryGetSystemColor(9, out systemColor) &&
                    systemColor != DrawingColor.Empty)
                    return Color.FromRgb(systemColor.R, systemColor.G, systemColor.B);

                Color fill;
                if (theme != null &&
                    (TryReadColor(theme, part, 1, TmtFillColorHint, out fill) ||
                     TryReadColor(theme, part, 1, TmtFillColor, out fill)))
                    return GetContrastColor(fill);
            }

            return normalWindow ? SystemColors.ActiveCaptionTextColor : NormalWindowForeground;
        }

        /// <summary>
        /// Text color used inside a normal window's client area.
        /// <para xml:lang="zh-CN">普通窗口内容区使用的文本前景色。</para>
        /// </summary>
        public static Color WindowContentForeground
        {
            get
            {
                using (VisualStyleContext context = new VisualStyleContext())
                using (VisualStyle theme = context.Open("Window"))
                {
                    Color color;
                    if (theme != null &&
                        (TryReadColor(theme, 39, 1, TmtTextColor, out color) ||
                         TryReadColor(theme, 39, 1, TmtTextColorHint, out color)))
                        return color;
                }

                return SystemColors.WindowTextColor;
            }
        }

        /// <summary>
        /// Border fill color for the ToolTip Balloon part, from
        /// <c>Tooltip</c> part 3 (Balloon), normal state, <c>TMT_EDGEFILLCOLOR</c>.
        /// Falls back to <c>SystemColors.InfoColor</c> when no theme is active.
        /// <para xml:lang="zh-CN">ToolTip Balloon 部件的边框填充色，来自 <c>Tooltip</c> 部件 3（Balloon）普通状态的 <c>TMT_EDGEFILLCOLOR</c>；无主题时回退到 <c>SystemColors.InfoColor</c>。</para>
        /// </summary>
        public static Color ToolTipEdgeFill
        {
            get { return GetToolTipEdgeColor(TmtEdgeFillColor, SystemColors.InfoColor); }
        }

        /// <summary>
        /// Light border color for the ToolTip Balloon part, from
        /// <c>Tooltip</c> part 3 (Balloon), normal state, <c>TMT_EDGELIGHTCOLOR</c>.
        /// <para xml:lang="zh-CN">ToolTip Balloon 部件的浅边框色，来自 <c>Tooltip</c> 部件 3（Balloon）普通状态的 <c>TMT_EDGELIGHTCOLOR</c>。</para>
        /// </summary>
        public static Color ToolTipEdgeLight
        {
            get { return GetToolTipEdgeColor(TmtEdgeLightColor, SystemColors.ControlLightLightColor); }
        }

        /// <summary>
        /// Highlight border color for the ToolTip Balloon part, from
        /// <c>Tooltip</c> part 3 (Balloon), normal state, <c>TMT_EDGEHIGHLIGHTCOLOR</c>.
        /// <para xml:lang="zh-CN">ToolTip Balloon 部件的高亮边框色，来自 <c>Tooltip</c> 部件 3（Balloon）普通状态的 <c>TMT_EDGEHIGHLIGHTCOLOR</c>。</para>
        /// </summary>
        public static Color ToolTipEdgeHighlight
        {
            get { return GetToolTipEdgeColor(TmtEdgeHighlightColor, SystemColors.ControlLightLightColor); }
        }

        /// <summary>
        /// Shadow border color for the ToolTip Balloon part, from
        /// <c>Tooltip</c> part 3 (Balloon), normal state, <c>TMT_EDGESHADOWCOLOR</c>.
        /// <para xml:lang="zh-CN">ToolTip Balloon 部件的阴影边框色，来自 <c>Tooltip</c> 部件 3（Balloon）普通状态的 <c>TMT_EDGESHADOWCOLOR</c>。</para>
        /// </summary>
        public static Color ToolTipEdgeShadow
        {
            get { return GetToolTipEdgeColor(TmtEdgeShadowColor, SystemColors.ControlDarkColor); }
        }

        /// <summary>
        /// Dark shadow border color for the ToolTip Balloon part, from
        /// <c>Tooltip</c> part 3 (Balloon), normal state, <c>TMT_EDGEDKSHADOWCOLOR</c>.
        /// <para xml:lang="zh-CN">ToolTip Balloon 部件的深阴影边框色，来自 <c>Tooltip</c> 部件 3（Balloon）普通状态的 <c>TMT_EDGEDKSHADOWCOLOR</c>。</para>
        /// </summary>
        public static Color ToolTipEdgeDarkShadow
        {
            get { return GetToolTipEdgeColor(TmtEdgeDkShadowColor, SystemColors.ControlDarkDarkColor); }
        }

        private static Color GetToolTipEdgeColor(int propertyId, Color fallback)
        {
            using (VisualStyleContext context = new VisualStyleContext())
            using (VisualStyle theme = context.Open("Tooltip"))
            {
                Color color;
                if (theme != null &&
                    (TryReadColor(theme, 3, 1, propertyId, out color) ||
                     TryReadColor(theme, 1, 1, propertyId, out color)))
                    return color;
            }

            return fallback;
        }

        private static bool TryGetTextColor(string className, string subClass, int[] parts, out Color color)
        {
            foreach (int part in parts)
            {
                if (TryGetTextColor(className, subClass, part, 0, out color))
                    return true;
            }

            color = default(Color);
            return false;
        }

        private static bool TryGetTextColor(string className, string subClass, int part, int state, out Color color)
        {
            color = default(Color);
            using (VisualStyleContext context = new VisualStyleContext())
            using (VisualStyle theme = context.Open(className, subClass))
            {
                if (theme == null)
                    return false;

                return TryReadColor(theme, part, state, TmtTextColor, out color) ||
                       TryReadColor(theme, part, state, TmtTextColorHint, out color);
            }
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
