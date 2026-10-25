using System;

namespace Microsoft.Windows.Themes
{
    /// <summary>
    /// Optional diagnostics for the lazy theme-image pipeline.  Useful in tests to prove that
    /// images are only created when a control is loaded and that the weak cache drops them
    /// afterwards.
    /// <para xml:lang="zh-CN">按需主题图片管线的可选诊断接口。可在测试中证明：只有控件加载时才会创建图片，之后弱缓存会释放它们。</para>
    /// </summary>
    public static class ThemeImageDiagnostics
    {
        /// <summary>
        /// Total number of theme images rendered since the process started.
        /// <para xml:lang="zh-CN">进程启动以来渲染的主题图片总数。</para>
        /// </summary>
        public static int AlphaProbeLimit
        {
            get { return ThemeImageProvider.AlphaProbeLimit; }
        }

        public static int CreatedImageCount
        {
            get { return ThemeImageProvider.CreatedCount; }
        }

        /// <summary>
        /// Number of images still alive in the weak cache (dead entries are removed first).
        /// <para xml:lang="zh-CN">弱缓存中仍然存活的图片数量（会先移除已死亡条目）。</para>
        /// </summary>
        public static int CachedImageCount
        {
            get { return ThemeImageProvider.AliveCount; }
        }

        /// <summary>
        /// Returns the current cache keys.  Each key contains theme file/color/size,
        /// class, part, state, HWND and DPI, which makes it useful for diagnostics.
        /// <para xml:lang="zh-CN">返回当前缓存键。每个键包含主题文件/配色/尺寸、类、部件、状态、
        /// HWND 和 DPI，便于诊断。</para>
        /// </summary>
        public static string[] GetCacheKeys()
        {
            return ThemeImageProvider.GetCacheKeys();
        }

        /// <summary>
        /// Returns cache entries with pixel size and nine-patch margins.  This is
        /// diagnostic only and intentionally exposes the raw cache key.
        /// <para xml:lang="zh-CN">返回带像素尺寸和九宫 margin 的缓存条目，仅用于诊断。</para>
        /// </summary>
        public static string[] GetCacheDetails()
        {
            return ThemeImageProvider.GetCacheDetails();
        }

        /// <summary>
        /// Gets TMT_PROGRESSCHUNKSIZE / TMT_PROGRESSSPACESIZE from the current
        /// Progress theme part.  Returns false when the theme does not define them.
        /// </summary>
        public static bool GetProgressMetrics(out int chunkSize, out int spaceSize)
        {
            return ThemeImageProvider.TryGetProgressMetrics(out chunkSize, out spaceSize);
        }

        /// <summary>
        /// Drops all cache keys.        /// <summary>
        /// Drops all cache keys.  Images still referenced by controls stay alive; the rest can
        /// be collected immediately.
        /// <para xml:lang="zh-CN">清空所有缓存键。仍被控件引用的图片会继续存活，其余可立即回收。</para>
        /// </summary>
        public static void ResetCreatedImageCount()
        {
            ThemeImageProvider.ResetCreatedImageCount();
        }

        /// <summary>
        /// Re-opens the theme and re-renders every live ThemeImage. Useful after the
        /// XP UxTheme service finishes switching to the current visual style.
        /// </summary>
        public static void RefreshAll()
        {
            ThemeImageProvider.ClearCache();
            ThemeImageHost.RefreshAll();
        }

        public static void ClearCache()
        {
            ThemeImageProvider.ClearCache();
        }
    }
}
