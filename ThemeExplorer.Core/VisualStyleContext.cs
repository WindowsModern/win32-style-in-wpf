using System;
using System.Runtime.InteropServices;
using ThemeExplorer.Core.Interop;

namespace ThemeExplorer.Core
{
    /// <summary>
    /// Owns the hidden helper window that UXTHEME needs in order to apply a window subclass before opening a theme. Create one context per UI thread and reuse it for all <see cref="VisualStyle"/> instances.
    /// <para xml:lang="zh-CN">拥有隐藏辅助窗口，UXTHEME 需要它来在打开主题前应用窗口子类。每个 UI 线程创建一个上下文，并对所有 <see cref="VisualStyle"/> 实例复用它。</para>
    /// </summary>
    /// <remarks>
    /// The context is not thread safe and is meant to be used on the UI thread.
    /// <para xml:lang="zh-CN">上下文不是线程安全的，应在 UI 线程上使用。</para>
    /// </remarks>
    public sealed class VisualStyleContext : IDisposable
    {
        private IntPtr _window;
        private bool _disposed;

        /// <summary>
        /// Creates the context and the hidden helper window.
        /// <para xml:lang="zh-CN">创建上下文和隐藏的辅助窗口。</para>
        /// </summary>
        public VisualStyleContext()
        {
            IntPtr instance = NativeMethods.GetModuleHandle(null);
            _window = NativeMethods.CreateWindowEx(0, "STATIC", string.Empty, 0,
                0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, instance, IntPtr.Zero);

            // Some XP/WPF combinations fail to create the system "STATIC" helper
            // window with the EXE module handle.  Try a zero hInstance, then fall
            // back to a zero-HWND context.  OpenThemeData(NULL, ...) still returns
            // the process/global theme, so the app remains usable.
            if (_window == IntPtr.Zero)
            {
                _window = NativeMethods.CreateWindowEx(0, "STATIC", string.Empty, 0,
                    0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            }

            // A zero window is a supported fallback; do not throw here.  Throwing
            // in the WPF App.OnStartup path terminates ThemeExplorer.Wpf on XP.
        }

        /// <summary>
        /// Opens a visual style for the given class. Returns <c>null</c> when the class is not themed by the current visual style.
        /// <para xml:lang="zh-CN">为给定类打开视觉样式。当当前视觉样式未定义该类时返回 <c>null</c>。</para>
        /// </summary>
        /// <param name="className">Class name, for example "Button".</param>
        /// <param name="subClass">Optional window subclass, for example "ExplorerBar".</param>
        public VisualStyle Open(string className, string subClass = null)
        {
            ThrowIfDisposed();
            if (string.IsNullOrEmpty(className))
                throw new ArgumentException("A theme class name is required.", nameof(className));

            // The subclass is a property of the window, not of OpenThemeData, so it
            // has to be applied first (this mirrors the original C implementation).
            if (_window != IntPtr.Zero)
                NativeMethods.SetWindowTheme(_window, string.IsNullOrEmpty(subClass) ? null : subClass, null);

            IntPtr handle = NativeMethods.OpenThemeData(_window, className);
            if (handle == IntPtr.Zero)
                return null;

            return new VisualStyle(new SafeThemeHandle(handle));
        }

        /// <summary>
        /// Destroys the hidden helper window.
        /// <para xml:lang="zh-CN">销毁隐藏的辅助窗口。</para>
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            if (_window != IntPtr.Zero)
            {
                NativeMethods.DestroyWindow(_window);
                _window = IntPtr.Zero;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(VisualStyleContext));
        }
    }
}
