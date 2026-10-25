using System.Text;
using ThemeExplorer.Core.Interop;

namespace ThemeExplorer.Core
{
    /// <summary>
    /// Process-wide information about the visual styles subsystem.
    /// <para xml:lang="zh-CN">视觉样式子系统的进程级信息。</para>
    /// </summary>
    public static class ThemeSystem
    {
        /// <summary>
        /// Whether visual styles are active for this session and enabled for the current process.
        /// <para xml:lang="zh-CN">当前会话是否启用了视觉样式，并且当前进程是否启用了主题。</para>
        /// </summary>
        public static bool IsVisualStylesEnabled
        {
            get { return NativeMethods.IsThemeActive() && NativeMethods.IsAppThemed(); }
        }

        /// <summary>
        /// Whether a visual style is active in the current session (this is also true for processes that did not opt in to common controls v6).
        /// <para xml:lang="zh-CN">当前会话是否有视觉样式处于活动状态（对于未启用 comctl32 v6 的进程也为 true）。</para>
        /// </summary>
        public static bool IsThemeActive
        {
            get { return NativeMethods.IsThemeActive(); }
        }

        /// <summary>
        /// Whether the current process opted in to themed common controls.
        /// <para xml:lang="zh-CN">当前进程是否启用了主题化公共控件。</para>
        /// </summary>
        public static bool IsAppThemed
        {
            get { return NativeMethods.IsAppThemed(); }
        }

        /// <summary>
        /// Returns the theme file, colour scheme and size currently in use.
        /// <para xml:lang="zh-CN">返回当前使用的主题文件、配色方案和尺寸名称。</para>
        /// </summary>
        public static ThemeInfo GetCurrentTheme()
        {
            var fileName = new StringBuilder(260);
            var colorScheme = new StringBuilder(260);
            var sizeName = new StringBuilder(260);

            int hr = NativeMethods.GetCurrentThemeName(fileName, fileName.Capacity,
                colorScheme, colorScheme.Capacity, sizeName, sizeName.Capacity);

            if (hr < 0)
                return new ThemeInfo(string.Empty, string.Empty, string.Empty);

            return new ThemeInfo(fileName.ToString(), colorScheme.ToString(), sizeName.ToString());
        }
    }
}
