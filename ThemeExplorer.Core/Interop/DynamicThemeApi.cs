using System;
using System.Runtime.InteropServices;

namespace ThemeExplorer.Core.Interop
{
    /// <summary>
    /// Resolves the optional UXTHEME.DLL entry points at run time. They exist on Windows Vista and later but not on Windows XP, and the original tool was careful to keep running there.
    /// <para xml:lang="zh-CN">在运行时解析 UXTHEME.DLL 的可选入口点。这些入口点存在于 Windows Vista 及更高版本，Windows XP 上没有；原版工具为此做了兼容处理。</para>
    /// </summary>
    internal static class DynamicThemeApi
    {
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate int GetThemeBitmapDelegate(IntPtr hTheme, int iPartId, int iStateId, int iPropId,
            uint dwFlags, out IntPtr phBitmap);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate int GetThemeStreamDelegate(IntPtr hTheme, int iPartId, int iStateId, int iPropId,
            out IntPtr ppvStream, out uint pcbStream, IntPtr hInst);

        private static readonly object SyncRoot = new object();
        private static bool _initialized;
        private static GetThemeBitmapDelegate _getThemeBitmap;
        private static GetThemeStreamDelegate _getThemeStream;

        internal static bool TryGetBitmap(IntPtr theme, int part, int state, int prop, ThemeBitmapFlags flags, out IntPtr bitmap)
        {
            EnsureInitialized();
            if (_getThemeBitmap == null)
            {
                bitmap = IntPtr.Zero;
                return false;
            }

            int hr = _getThemeBitmap(theme, part, state, prop, (uint)flags, out bitmap);
            if (hr < 0 || bitmap == IntPtr.Zero)
            {
                bitmap = IntPtr.Zero;
                return false;
            }
            return true;
        }

        internal static bool TryGetStream(IntPtr theme, int part, int state, int prop, IntPtr module, out int length)
        {
            EnsureInitialized();
            length = 0;
            if (_getThemeStream == null)
                return false;

            IntPtr stream;
            uint size;
            int hr = _getThemeStream(theme, part, state, prop, out stream, out size, module);
            if (hr < 0 || stream == IntPtr.Zero)
                return false;

            length = (int)size;
            return true;
        }

        private static void EnsureInitialized()
        {
            if (_initialized)
                return;

            lock (SyncRoot)
            {
                if (_initialized)
                    return;

                IntPtr module = NativeMethods.LoadLibrary(NativeMethods.UxThemeDll);
                if (module != IntPtr.Zero)
                {
                    IntPtr address = NativeMethods.GetProcAddress(module, "GetThemeBitmap");
                    if (address != IntPtr.Zero)
                        _getThemeBitmap = (GetThemeBitmapDelegate)Marshal.GetDelegateForFunctionPointer(address, typeof(GetThemeBitmapDelegate));

                    address = NativeMethods.GetProcAddress(module, "GetThemeStream");
                    if (address != IntPtr.Zero)
                        _getThemeStream = (GetThemeStreamDelegate)Marshal.GetDelegateForFunctionPointer(address, typeof(GetThemeStreamDelegate));
                }

                _initialized = true;
            }
        }
    }
}
