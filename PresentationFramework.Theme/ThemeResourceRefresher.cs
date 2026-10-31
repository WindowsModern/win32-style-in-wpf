using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Microsoft.Windows.Themes
{
    /// <summary>
    /// Refreshes theme-derived brush, font-size and metric resources after a
    /// visual-style or system-color change. The values are normally created by
    /// <c>x:Static</c> at dictionary load, so replacing the dictionary entries
    /// is what makes existing <c>DynamicResource</c> consumers update.
    /// </summary>
    public static class ThemeResourceRefresher
    {
        /// <summary>
        /// Refreshes every known theme resource in the application resources.
        /// Returns the number of dictionary entries that changed.
        /// </summary>
        public static int Refresh()
        {
            Application application = Application.Current;
            if (application == null)
                return 0;

            int changed = 0;
            foreach (ResourceDictionary dictionary in Enumerate(application.Resources))
            {
                changed += RefreshBrush(dictionary, ThemeResourceKeys.TaskbarForeground, ThemeColors.TaskbarForeground);
                changed += RefreshBrush(dictionary, ThemeResourceKeys.TrayNotifyForeground, ThemeColors.TrayNotifyForeground);
                changed += RefreshBrush(dictionary, ThemeResourceKeys.NormalWindowForeground, ThemeColors.NormalWindowForeground);
                changed += RefreshBrush(dictionary, ThemeResourceKeys.ToolWindowForeground, ThemeColors.ToolWindowForeground);
                changed += RefreshBrush(dictionary, ThemeResourceKeys.WindowContentForeground, ThemeColors.WindowContentForeground);

                changed += RefreshDouble(dictionary, ThemeResourceKeys.NormalWindowTitleFontSize, ThemeWindowMetrics.NormalWindowTitleFontSize);
                changed += RefreshDouble(dictionary, ThemeResourceKeys.ToolWindowTitleFontSize, ThemeWindowMetrics.ToolWindowTitleFontSize);
                changed += RefreshDouble(dictionary, ThemeResourceKeys.NormalWindowTitleHeight, ThemeWindowMetrics.NormalWindowTitleHeight);
                changed += RefreshDouble(dictionary, ThemeResourceKeys.ToolWindowTitleHeight, ThemeWindowMetrics.ToolWindowTitleHeight);
                changed += RefreshThickness(dictionary, ThemeResourceKeys.NormalWindowContentMargin, ThemeWindowMetrics.NormalWindowContentMargin);
                changed += RefreshThickness(dictionary, ThemeResourceKeys.ToolWindowContentMargin, ThemeWindowMetrics.ToolWindowContentMargin);
            }
            return changed;
        }

        private static IEnumerable<ResourceDictionary> Enumerate(ResourceDictionary root)
        {
            if (root == null)
                yield break;

            yield return root;
            foreach (ResourceDictionary merged in root.MergedDictionaries)
            {
                foreach (ResourceDictionary dictionary in Enumerate(merged))
                    yield return dictionary;
            }
        }

        private static int RefreshBrush(ResourceDictionary dictionary, string key, Color color)
        {
            if (!dictionary.Contains(key))
                return 0;

            var brush = new SolidColorBrush(color);
            brush.Freeze();
            dictionary[key] = brush;
            return 1;
        }

        private static int RefreshDouble(ResourceDictionary dictionary, string key, double value)
        {
            if (!dictionary.Contains(key))
                return 0;

            dictionary[key] = value;
            return 1;
        }

        private static int RefreshThickness(ResourceDictionary dictionary, string key, Thickness value)
        {
            if (!dictionary.Contains(key))
                return 0;

            dictionary[key] = value;
            return 1;
        }
    }
}
