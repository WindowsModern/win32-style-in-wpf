using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using ThemeExplorer.Core.Interop;

namespace ThemeExplorer.Core
{
    /// <summary>
    /// A handle to an open visual style (<c>HTHEME</c>) together with strongly typed accessors for its parts, states and properties.
    /// <para xml:lang="zh-CN">已打开视觉样式（<c>HTHEME</c>）的句柄，以及针对其部件、状态和属性的强类型访问方法。</para>
    /// </summary>
    /// <remarks>
    /// Instances are created by <see cref="VisualStyleContext.Open"/>. The object owns the native handle and must be disposed when it is no longer needed.
    /// <para xml:lang="zh-CN">实例由 <see cref="VisualStyleContext.Open"/> 创建。该对象拥有本机句柄，不再需要时必须释放。</para>
    /// </remarks>
    public sealed class VisualStyle : IDisposable
    {
        /// <summary>
        /// <c>TMT_DIBDATA</c>: the background bitmap of a part.
        /// <para xml:lang="zh-CN"><c>TMT_DIBDATA</c>：部件的背景位图。</para>
        /// </summary>
        internal const int TmtDibData = 2;

        /// <summary>
        /// <c>TMT_GLYPHDIBDATA</c>: the glyph bitmap of a part.
        /// <para xml:lang="zh-CN"><c>TMT_GLYPHDIBDATA</c>：部件的字形位图。</para>
        /// </summary>
        internal const int TmtGlyphDibData = 8;

        private readonly SafeThemeHandle _handle;
        private bool _disposed;

        internal VisualStyle(SafeThemeHandle handle)
        {
            _handle = handle;
        }

        /// <summary>
        /// Opens a visual style for the given class without creating a helper window. Returns <c>null</c> when the class is not defined by the current visual style.
        /// <para xml:lang="zh-CN">在不创建辅助窗口的情况下为指定类打开视觉样式。当前视觉样式未定义该类时返回 <c>null</c>。</para>
        /// </summary>
        public static VisualStyle Open(string className)
        {
            return Open(IntPtr.Zero, className);
        }

        /// <summary>
        /// Opens a visual style for the given class using a specific window handle. Passing the
        /// top-level window matters for per-window themes (custom UXTHEME skins); a zero handle
        /// asks UXTHEME for the process/global theme, which can be the default Aero style.
        /// <para xml:lang="zh-CN">使用指定窗口句柄为给定类打开视觉样式。对于按窗口应用的
        /// 自定义 UXTHEME 皮肤，传入顶层窗口句柄很重要；零句柄会取进程/全局主题，可能得到
        /// 默认 Aero 样式。</para>
        /// </summary>
        /// <summary>
        /// Opens a visual style for a class and optional window subclass.  UxTheme
        /// stores the subclass on the HWND, so it is applied before OpenThemeData
        /// and cleared afterwards.
        /// </summary>
        public static VisualStyle Open(IntPtr window, string className, string subClass)
        {
            if (string.IsNullOrEmpty(className))
                throw new ArgumentException("A theme class name is required.", nameof(className));

            if (window == IntPtr.Zero || string.IsNullOrEmpty(subClass))
                return Open(window, className);

            NativeMethods.SetWindowTheme(window, subClass, null);
            try
            {
                IntPtr handle = NativeMethods.OpenThemeData(window, className);
                if (handle == IntPtr.Zero)
                    return null;
                return new VisualStyle(new SafeThemeHandle(handle));
            }
            finally
            {
                NativeMethods.SetWindowTheme(window, null, null);
            }
        }

        public static VisualStyle Open(IntPtr window, string className)
        {
            if (string.IsNullOrEmpty(className))
                throw new ArgumentException("A theme class name is required.", nameof(className));

            IntPtr handle = NativeMethods.OpenThemeData(window, className);
            if (handle == IntPtr.Zero)
                return null;

            return new VisualStyle(new SafeThemeHandle(handle));
        }

        internal IntPtr Handle
        {
            get { return _handle.DangerousGetHandle(); }
        }

        /// <summary>
        /// Whether the underlying native handle is still usable.
        /// <para xml:lang="zh-CN">底层本机句柄是否仍可用。</para>
        /// </summary>
        public bool IsDisposed
        {
            get { return _disposed || _handle.IsClosed || _handle.IsInvalid; }
        }

        /// <summary>
        /// Returns whether the given part/state pair is defined by the style.
        /// <para xml:lang="zh-CN">返回样式是否定义了给定的部件/状态。</para>
        /// </summary>
        public bool IsPartDefined(int part, int state)
        {
            ThrowIfDisposed();
            return NativeMethods.IsThemePartDefined(Handle, part, state);
        }

        /// <summary>
        /// Tries to read an integer property.
        /// <para xml:lang="zh-CN">尝试读取整数属性。</para>
        /// </summary>
        public bool TryGetInt(int part, int state, int propertyId, out int value)
        {
            ThrowIfDisposed();
            return NativeMethods.GetThemeInt(Handle, part, state, propertyId, out value) >= 0;
        }

        /// <summary>
        /// Tries to read a boolean property.
        /// <para xml:lang="zh-CN">尝试读取布尔属性。</para>
        /// </summary>
        public bool TryGetBool(int part, int state, int propertyId, out bool value)
        {
            ThrowIfDisposed();
            int raw;
            if (NativeMethods.GetThemeBool(Handle, part, state, propertyId, out raw) < 0)
            {
                value = false;
                return false;
            }

            value = raw != 0;
            return true;
        }

        /// <summary>
        /// Tries to read a colour property.
        /// <para xml:lang="zh-CN">尝试读取颜色属性。</para>
        /// </summary>
        public bool TryGetColor(int part, int state, int propertyId, out Color value)
        {
            ThrowIfDisposed();
            int raw;
            if (NativeMethods.GetThemeColor(Handle, part, state, propertyId, out raw) < 0)
            {
                value = Color.Empty;
                return false;
            }

            value = Color.FromArgb(raw & 0xFF, (raw >> 8) & 0xFF, (raw >> 16) & 0xFF);
            return true;
        }

        /// <summary>
        /// Tries to read a string property.
        /// <para xml:lang="zh-CN">尝试读取字符串属性。</para>
        /// </summary>
        public bool TryGetString(int part, int state, int propertyId, out string value)
        {
            ThrowIfDisposed();
            var buffer = new StringBuilder(2048);
            if (NativeMethods.GetThemeString(Handle, part, state, propertyId, buffer, buffer.Capacity) < 0)
            {
                value = null;
                return false;
            }

            value = buffer.ToString();
            return true;
        }

        /// <summary>
        /// Tries to read a file name property.
        /// <para xml:lang="zh-CN">尝试读取文件名属性。</para>
        /// </summary>
        public bool TryGetFilename(int part, int state, int propertyId, out string value)
        {
            ThrowIfDisposed();
            var buffer = new StringBuilder(1024);
            if (NativeMethods.GetThemeFilename(Handle, part, state, propertyId, buffer, buffer.Capacity) < 0)
            {
                value = null;
                return false;
            }

            value = buffer.ToString();
            return true;
        }

        /// <summary>
        /// Tries to read a margins property.
        /// <para xml:lang="zh-CN">尝试读取边距属性。</para>
        /// </summary>
        public bool TryGetMargins(int part, int state, int propertyId, out ThemeMargins value)
        {
            ThrowIfDisposed();
            IntPtr dc = NativeMethods.GetDC(IntPtr.Zero);
            try
            {
                MARGINS margins;
                if (NativeMethods.GetThemeMargins(Handle, dc, part, state, propertyId, IntPtr.Zero, out margins) < 0)
                {
                    value = default(ThemeMargins);
                    return false;
                }

                value = new ThemeMargins(margins.cxLeftWidth, margins.cxRightWidth,
                    margins.cyTopHeight, margins.cyBottomHeight);
                return true;
            }
            finally
            {
                NativeMethods.ReleaseDC(IntPtr.Zero, dc);
            }
        }

        /// <summary>
        /// Tries to read a rectangle property.
        /// <para xml:lang="zh-CN">尝试读取矩形属性。</para>
        /// </summary>
        public bool TryGetRect(int part, int state, int propertyId, out Rectangle value)
        {
            ThrowIfDisposed();
            RECT rect;
            if (NativeMethods.GetThemeRect(Handle, part, state, propertyId, out rect) < 0)
            {
                value = Rectangle.Empty;
                return false;
            }

            value = Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
            return true;
        }

        /// <summary>
        /// Tries to read a position property.
        /// <para xml:lang="zh-CN">尝试读取位置属性。</para>
        /// </summary>
        public bool TryGetPosition(int part, int state, int propertyId, out Point value)
        {
            ThrowIfDisposed();
            POINT point;
            if (NativeMethods.GetThemePosition(Handle, part, state, propertyId, out point) < 0)
            {
                value = Point.Empty;
                return false;
            }

            value = new Point(point.x, point.y);
            return true;
        }

        /// <summary>
        /// Tries to read a size property.
        /// <para xml:lang="zh-CN">尝试读取尺寸属性。</para>
        /// </summary>
        public bool TryGetPartSize(int part, int state, ThemeSize size, out Size value)
        {
            ThrowIfDisposed();
            IntPtr dc = NativeMethods.GetDC(IntPtr.Zero);
            try
            {
                SIZE nativeSize;
                if (NativeMethods.GetThemePartSize(Handle, dc, part, state, IntPtr.Zero, (int)size, out nativeSize) < 0)
                {
                    value = Size.Empty;
                    return false;
                }

                value = new Size(nativeSize.cx, nativeSize.cy);
                return true;
            }
            finally
            {
                NativeMethods.ReleaseDC(IntPtr.Zero, dc);
            }
        }

        /// <summary>
        /// Tries to read a font property. Returns a description rather than a <see cref="Font"/> so that no GDI object has to be owned by the caller.
        /// <para xml:lang="zh-CN">尝试读取字体属性。返回描述对象而不是 <see cref="Font"/>，这样调用方无需拥有 GDI 对象。</para>
        /// </summary>
        public bool TryGetFont(int part, int state, int propertyId, out ThemeFontInfo value)
        {
            ThrowIfDisposed();
            IntPtr dc = NativeMethods.GetDC(IntPtr.Zero);
            try
            {
                LOGFONT font;
                if (NativeMethods.GetThemeFont(Handle, dc, part, state, propertyId, out font) < 0)
                {
                    value = null;
                    return false;
                }

                value = new ThemeFontInfo(font.lfFaceName, font.lfHeight, font.lfWeight,
                    font.lfItalic != 0, font.lfUnderline != 0);
                return true;
            }
            finally
            {
                NativeMethods.ReleaseDC(IntPtr.Zero, dc);
            }
        }

        /// <summary>
        /// Tries to read a system font (SysMetrics section), for example
        /// <c>TMT_CAPTIONFONT</c> or <c>TMT_SMALLCAPTIONFONT</c>.
        /// <para xml:lang="zh-CN">尝试读取系统字体（SysMetrics 区），例如标题栏字体或小标题栏字体。</para>
        /// </summary>
        public bool TryGetSystemFont(int fontId, out ThemeFontInfo value)
        {
            ThrowIfDisposed();
            try
            {
                LOGFONT font;
                if (NativeMethods.GetThemeSysFont(Handle, fontId, out font) < 0)
                {
                    value = null;
                    return false;
                }

                value = new ThemeFontInfo(font.lfFaceName, font.lfHeight, font.lfWeight,
                    font.lfItalic != 0, font.lfUnderline != 0);
                return true;
            }
            catch (EntryPointNotFoundException)
            {
                value = null;
                return false;
            }
        }

        /// <summary>
        /// Tries to read a system color (SysMetrics section), using a
        /// <c>COLOR_*</c> index such as <c>COLOR_ACTIVECAPTIONTEXT</c>.
        /// <para xml:lang="zh-CN">尝试读取系统颜色（SysMetrics 区），使用 <c>COLOR_*</c> 索引。</para>
        /// </summary>
        public bool TryGetSystemColor(int colorId, out Color value)
        {
            ThrowIfDisposed();
            try
            {
                uint color = NativeMethods.GetThemeSysColor(Handle, colorId);
                value = Color.FromArgb((int)(color & 0xFF),
                    (int)((color >> 8) & 0xFF),
                    (int)((color >> 16) & 0xFF));
                return true;
            }
            catch (EntryPointNotFoundException)
            {
                value = Color.Empty;
                return false;
            }
        }

        /// <summary>
        /// Tries to read an integer list property.
        /// <para xml:lang="zh-CN">尝试读取整数列表属性。</para>
        /// </summary>
        public bool TryGetIntList(int part, int state, int propertyId, out int[] value)
        {
            ThrowIfDisposed();

            // The native INTLIST is variable length.  MAX_INTLIST_COUNT in the SDK
            // is 402 entries; the original tool used 410 ints, keep that margin.
            byte[] buffer = new byte[410 * sizeof(int)];
            GCHandle pin = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                if (NativeMethods.GetThemeIntList(Handle, part, state, propertyId, pin.AddrOfPinnedObject()) < 0)
                {
                    value = null;
                    return false;
                }

                int count = BitConverter.ToInt32(buffer, 0);
                if (count < 0)
                    count = 0;
                if (count > 409)
                    count = 409;

                int[] values = new int[count];
                for (int i = 0; i < count; i++)
                    values[i] = BitConverter.ToInt32(buffer, sizeof(int) * (i + 1));

                value = values;
                return true;
            }
            finally
            {
                pin.Free();
            }
        }

        /// <summary>
        /// Tries to read an in-memory stream property (Windows Vista and later).
        /// <para xml:lang="zh-CN">尝试读取内存流属性（Windows Vista 及更高版本）。</para>
        /// </summary>
        public bool TryGetStream(int part, int state, int propertyId, out int length)
        {
            ThrowIfDisposed();
            return DynamicThemeApi.TryGetStream(Handle, part, state, propertyId, IntPtr.Zero, out length);
        }

        /// <summary>
        /// Tries to read a stream stored inside the current theme file (Windows Vista and later).
        /// <para xml:lang="zh-CN">尝试读取当前主题文件中存储的流（Windows Vista 及更高版本）。</para>
        /// </summary>
        internal bool TryGetDiskStream(int part, int state, int propertyId, out int length)
        {
            ThrowIfDisposed();
            length = 0;

            ThemeInfo info = ThemeSystem.GetCurrentTheme();
            if (string.IsNullOrEmpty(info.FileName))
                return false;

            IntPtr module = NativeMethods.LoadLibraryEx(info.FileName, IntPtr.Zero, NativeMethods.LOAD_LIBRARY_AS_DATAFILE);
            if (module == IntPtr.Zero)
                return false;

            try
            {
                return DynamicThemeApi.TryGetStream(Handle, part, state, propertyId, module, out length);
            }
            finally
            {
                NativeMethods.FreeLibrary(module);
            }
        }

        /// <summary>
        /// Returns where the value of a property comes from (state, part, class or global defaults).
        /// <para xml:lang="zh-CN">返回属性值的来源（状态、部件、类或全局默认值）。</para>
        /// </summary>
        public ThemePropertyOrigin GetPropertyOrigin(int part, int state, int propertyId)
        {
            ThrowIfDisposed();
            int origin;
            if (NativeMethods.GetThemePropertyOrigin(Handle, part, state, propertyId, out origin) < 0)
                return ThemePropertyOrigin.NotDefined;

            switch (origin)
            {
                case 1: return ThemePropertyOrigin.State;
                case 2: return ThemePropertyOrigin.Part;
                case 3: return ThemePropertyOrigin.Class;
                case 4: return ThemePropertyOrigin.Global;
                default: return ThemePropertyOrigin.NotDefined;
            }
        }

        /// <summary>
        /// Tries to read the value of a property defined by <see cref="ThemeDatabase"/>.
        /// <para xml:lang="zh-CN">尝试读取由 <see cref="ThemeDatabase"/> 定义的属性值。</para>
        /// </summary>
        public bool TryGetProperty(int part, int state, ThemePropertyInfo property, out ThemePropertyValue value)
        {
            ThrowIfDisposed();
            return ThemePropertyReader.TryRead(this, part, state, property, out value);
        }

        /// <summary>
        /// Releases the native theme handle.
        /// <para xml:lang="zh-CN">释放本机主题句柄。</para>
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _handle.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(VisualStyle));
        }
    }
}
