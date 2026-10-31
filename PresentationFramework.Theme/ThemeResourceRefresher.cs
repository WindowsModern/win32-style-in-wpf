using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;

namespace Microsoft.Windows.Themes
{
    /// <summary>
    /// Refreshes theme-derived brush, font-size and metric resources after a
    /// visual-style or system-color change. The values are normally created by
    /// <c>x:Static</c> at dictionary load, so replacing the dictionary entries
    /// is what makes existing <c>DynamicResource</c> consumers update.
    /// <para xml:lang="zh-CN">视觉样式或系统颜色变化后刷新主题画刷、字号和度量资源。资源值通常在字典加载时由 <c>x:Static</c> 固化，因此只有替换字典项，现有的 <c>DynamicResource</c> 使用方才会更新。</para>
    /// </summary>
    public static class ThemeResourceRefresher
    {
        private static bool initialized;

        /// <summary>
        /// Subscribes to system preference changes once. Called automatically when
        /// <see cref="ThemeColors"/> is first used; applications do not need to call it.
        /// <para xml:lang="zh-CN">只订阅一次系统首选项变化。首次使用 <see cref="ThemeColors"/> 时自动调用，应用无需手动调用。</para>
        /// </summary>
        internal static void EnsureInitialized()
        {
            if (initialized)
                return;

            initialized = true;
            try
            {
                SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
            }
            catch (InvalidOperationException)
            {
                // SystemEvents needs a message pump; design-time or service hosts may not have one.
            }
        }

        /// <summary>
        /// Refreshes every known theme resource in the application resources.
        /// Returns the number of dictionary entries that changed.
        /// <para xml:lang="zh-CN">刷新应用程序资源中所有已知的主题资源，返回发生变化的字典项数量。</para>
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
                // GlobalForegroundBrush is often overridden by applications; only
                // replace it in the library dictionary, identified by TaskbarForeground.
                if (dictionary.Contains(ThemeResourceKeys.TaskbarForeground))
                    changed += RefreshBrush(dictionary, ThemeResourceKeys.GlobalForeground, ThemeColors.TaskbarForeground);
                changed += RefreshBrush(dictionary, ThemeResourceKeys.TrayNotifyForeground, ThemeColors.TrayNotifyForeground);
                changed += RefreshBrush(dictionary, ThemeResourceKeys.NormalWindowForeground, ThemeColors.NormalWindowForeground);
                changed += RefreshBrush(dictionary, ThemeResourceKeys.ToolWindowForeground, ThemeColors.ToolWindowForeground);
                changed += RefreshBrush(dictionary, ThemeResourceKeys.WindowContentForeground, ThemeColors.WindowContentForeground);
                changed += RefreshBrush(dictionary, ThemeResourceKeys.GlobalFlyoutForeground, ThemeColors.WindowContentForeground);

                changed += RefreshDouble(dictionary, ThemeResourceKeys.NormalWindowTitleFontSize, ThemeWindowMetrics.NormalWindowTitleFontSize);
                changed += RefreshDouble(dictionary, ThemeResourceKeys.ToolWindowTitleFontSize, ThemeWindowMetrics.ToolWindowTitleFontSize);
                changed += RefreshDouble(dictionary, ThemeResourceKeys.NormalWindowTitleHeight, ThemeWindowMetrics.NormalWindowTitleHeight);
                changed += RefreshDouble(dictionary, ThemeResourceKeys.ToolWindowTitleHeight, ThemeWindowMetrics.ToolWindowTitleHeight);
                changed += RefreshThickness(dictionary, ThemeResourceKeys.NormalWindowContentMargin, ThemeWindowMetrics.NormalWindowContentMargin);
                changed += RefreshThickness(dictionary, ThemeResourceKeys.ToolWindowContentMargin, ThemeWindowMetrics.ToolWindowContentMargin);
            }
            return changed;
        }

        private static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category != UserPreferenceCategory.VisualStyle &&
                e.Category != UserPreferenceCategory.Color)
                return;

            RefreshFromSystemEvent();
        }

        private static void RefreshFromSystemEvent()
        {
            Application application = Application.Current;
            if (application == null)
                return;

            Dispatcher dispatcher = application.Dispatcher;
            if (dispatcher == null || dispatcher.HasShutdownStarted)
                return;

            if (dispatcher.CheckAccess())
            {
                SafeRefresh();
                return;
            }

            try
            {
                dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(SafeRefresh));
            }
            catch (InvalidOperationException)
            {
                // The dispatcher started shutting down between the checks above.
            }
        }

        private static void SafeRefresh()
        {
            try
            {
                Refresh();
            }
            catch (InvalidOperationException)
            {
                // The application may be shutting down or replacing its resources.
            }
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
