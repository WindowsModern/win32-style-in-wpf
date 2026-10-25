using System;
using System.Drawing;
using System.Runtime.InteropServices;
using ThemeExplorer.Core.Interop;

namespace ThemeExplorer.Core
{
    /// <summary>
    /// Drawing helpers built on top of <see cref="VisualStyle"/>.
    /// <para xml:lang="zh-CN">构建在 <see cref="VisualStyle"/> 之上的绘制辅助方法。</para>
    /// </summary>
    /// <remarks>
    /// The <c>Try*</c> methods return <c>false</c> (instead of throwing) when the current theme does not support the requested part; this keeps custom painting code simple.
    /// <para xml:lang="zh-CN">当当前主题不支持所请求的部件时，<c>Try*</c> 方法返回 <c>false</c> 而不是抛出异常；这样自定义绘制代码可以保持简单。</para>
    /// </remarks>
    public static class ThemeRenderer
    {
        /// <summary>
        /// Draws the background of a part/state pair into <paramref name="bounds"/>.
        /// <para xml:lang="zh-CN">将部件/状态的背景绘制到 <paramref name="bounds"/>。</para>
        /// </summary>
        public static bool TryDrawBackground(Graphics graphics, VisualStyle style, int part, int state, Rectangle bounds)
        {
            if (graphics == null)
                throw new ArgumentNullException(nameof(graphics));

            IntPtr dc = graphics.GetHdc();
            try
            {
                return TryDrawBackground(dc, style, part, state, bounds);
            }
            finally
            {
                graphics.ReleaseHdc(dc);
            }
        }

        /// <summary>
        /// Draws the background of a part/state pair into the given device context. This overload is useful when the caller already owns an HDC, for example when rendering into a DIB section for WPF.
        /// <para xml:lang="zh-CN">将部件/状态的背景绘制到给定的设备上下文。当调用方已经拥有 HDC（例如为 WPF 渲染到 DIB 区域）时，此重载很有用。</para>
        /// </summary>
        public static bool TryDrawBackground(IntPtr hdc, VisualStyle style, int part, int state, Rectangle bounds)
        {
            if (hdc == IntPtr.Zero)
                return false;
            if (style == null || style.IsDisposed || bounds.Width <= 0 || bounds.Height <= 0)
                return false;

            var rect = new RECT(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
            return NativeMethods.DrawThemeBackground(style.Handle, hdc, part, state, ref rect, ref rect) >= 0;
        }

        /// <summary>
        /// Draws the themed text of a part/state pair into <paramref name="bounds"/>.
        /// <para xml:lang="zh-CN">将部件/状态的主题文字绘制到 <paramref name="bounds"/>。</para>
        /// </summary>
        public static bool TryDrawText(Graphics graphics, VisualStyle style, int part, int state,
            string text, Rectangle bounds, ThemeTextFormat format)
        {
            if (graphics == null)
                throw new ArgumentNullException(nameof(graphics));

            IntPtr dc = graphics.GetHdc();
            try
            {
                return TryDrawText(dc, style, part, state, text, bounds, format);
            }
            finally
            {
                graphics.ReleaseHdc(dc);
            }
        }

        /// <summary>
        /// Draws the themed text of a part/state pair into the given device context.
        /// <para xml:lang="zh-CN">将部件/状态的主题文字绘制到给定的设备上下文。</para>
        /// </summary>
        public static bool TryDrawText(IntPtr hdc, VisualStyle style, int part, int state,
            string text, Rectangle bounds, ThemeTextFormat format)
        {
            if (hdc == IntPtr.Zero || style == null || style.IsDisposed || string.IsNullOrEmpty(text))
                return false;

            var rect = new RECT(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
            return NativeMethods.DrawThemeText(style.Handle, hdc, part, state, text, -1,
                (int)format, 0, ref rect) >= 0;
        }

        /// <summary>
        /// Draws the parent window background; used for parts that are partially transparent.
        /// <para xml:lang="zh-CN">绘制父窗口背景；用于部分透明的部件。</para>
        /// </summary>
        public static bool TryDrawParentBackground(IntPtr window, Graphics graphics, Rectangle bounds)
        {
            if (graphics == null)
                throw new ArgumentNullException(nameof(graphics));
            if (window == IntPtr.Zero)
                return false;

            var rect = new RECT(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
            IntPtr dc = graphics.GetHdc();
            try
            {
                return NativeMethods.DrawThemeParentBackground(window, dc, ref rect) >= 0;
            }
            finally
            {
                graphics.ReleaseHdc(dc);
            }
        }

        /// <summary>
        /// Returns whether the background of a part/state pair is partially transparent.
        /// <para xml:lang="zh-CN">返回部件/状态的背景是否部分透明。</para>
        /// </summary>
        public static bool IsBackgroundPartiallyTransparent(VisualStyle style, int part, int state)
        {
            if (style == null || style.IsDisposed)
                return false;
            return NativeMethods.IsThemeBackgroundPartiallyTransparent(style.Handle, part, state);
        }

        /// <summary>
        /// Returns a managed copy of the background or glyph bitmap of a part, or <c>null</c> when the theme does not provide one.
        /// <para xml:lang="zh-CN">返回部件背景或字形位图的托管副本；主题未提供时返回 <c>null</c>。</para>
        /// </summary>
        /// <summary>
        /// Returns the raw HBITMAP for a theme part without converting it through
        /// System.Drawing.Bitmap (which discards alpha).  The handle belongs to
        /// the theme; do not delete it.
        /// </summary>
        public static bool TryGetBitmapHandle(VisualStyle style, int part, int state,
            ThemeBitmapKind kind, out IntPtr bitmap)
        {
            bitmap = IntPtr.Zero;
            if (style == null || style.IsDisposed)
                return false;

            if (kind == ThemeBitmapKind.Background)
            {
                return DynamicThemeApi.TryGetBitmap(style.Handle, part, state,
                    VisualStyle.TmtDibData, ThemeBitmapFlags.Direct, out bitmap);
            }

            return DynamicThemeApi.TryGetBitmap(style.Handle, part, state,
                VisualStyle.TmtGlyphDibData, ThemeBitmapFlags.Direct, out bitmap)
                || DynamicThemeApi.TryGetBitmap(style.Handle, part, state,
                    3, ThemeBitmapFlags.Direct, out bitmap);
        }
        public static Bitmap GetBitmap(VisualStyle style, int part, int state, ThemeBitmapKind kind)
        {
            if (style == null || style.IsDisposed)
                return null;

            IntPtr bitmapHandle;
            bool found;
            if (kind == ThemeBitmapKind.Background)
            {
                found = DynamicThemeApi.TryGetBitmap(style.Handle, part, state,
                    VisualStyle.TmtDibData, ThemeBitmapFlags.Direct, out bitmapHandle);
            }
            else
            {
                // The original tool documents a Windows 7 quirk: TMT_GLYPHDIBDATA
                // (8) fails there while the literal 3 works.  Try both.
                found = DynamicThemeApi.TryGetBitmap(style.Handle, part, state,
                    VisualStyle.TmtGlyphDibData, ThemeBitmapFlags.Direct, out bitmapHandle)
                    || DynamicThemeApi.TryGetBitmap(style.Handle, part, state,
                        3, ThemeBitmapFlags.Direct, out bitmapHandle);
            }

            if (!found)
                return null;

            try
            {
                // GBF_DIRECT returns a bitmap owned by the theme: copy it and never
                // delete the native handle.
                using (Bitmap source = Image.FromHbitmap(bitmapHandle))
                {
                    return new Bitmap(source);
                }
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (ExternalException)
            {
                return null;
            }
        }

        /// <summary>
        /// Queries the size of a part/state pair.
        /// <para xml:lang="zh-CN">查询部件/状态的尺寸。</para>
        /// </summary>
        public static bool TryGetPartSize(VisualStyle style, int part, int state, ThemeSize size, out Size value)
        {
            if (style == null || style.IsDisposed)
            {
                value = Size.Empty;
                return false;
            }

            return style.TryGetPartSize(part, state, size, out value);
        }

        /// <summary>
        /// Queries the length of an in-memory stream property.
        /// <para xml:lang="zh-CN">查询内存流属性的长度。</para>
        /// </summary>
        public static bool TryGetStreamLength(VisualStyle style, int part, int state, int propertyId, out int length)
        {
            if (style == null || style.IsDisposed)
            {
                length = 0;
                return false;
            }

            return style.TryGetStream(part, state, propertyId, out length);
        }
    }
}
