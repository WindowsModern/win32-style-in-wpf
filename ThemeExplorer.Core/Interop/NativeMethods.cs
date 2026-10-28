using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ThemeExplorer.Core.Interop
{
    /// <summary>
    /// Raw P/Invoke declarations for UXTHEME.DLL and the few Win32 functions the wrapper needs. Everything here is an implementation detail of the <see cref="ThemeExplorer.Core"/> assembly.
    /// <para xml:lang="zh-CN">UXTHEME.DLL 及封装所需少量 Win32 函数的原始 P/Invoke 声明。这里的全部内容都属于 <see cref="ThemeExplorer.Core"/> 程序集的实现细节。</para>
    /// </summary>
    internal static class NativeMethods
    {
        internal const string UxThemeDll = "uxtheme.dll";
        internal const string User32Dll = "user32.dll";
        internal const string Kernel32Dll = "kernel32.dll";

        internal const int LOAD_LIBRARY_AS_DATAFILE = 0x00000002;

        // ------------------------------------------------------------------
        // Theme lifetime
        // ------------------------------------------------------------------

        [DllImport(UxThemeDll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern IntPtr OpenThemeData(IntPtr hwnd, string pszClassList);

        [DllImport(UxThemeDll, ExactSpelling = true)]
        internal static extern int CloseThemeData(IntPtr hTheme);

        [DllImport(UxThemeDll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int SetWindowTheme(IntPtr hwnd, string pszSubAppName, string pszSubIdList);

        [DllImport(UxThemeDll, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsThemeActive();

        [DllImport(UxThemeDll, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsAppThemed();

        [DllImport(UxThemeDll, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsThemePartDefined(IntPtr hTheme, int iPartId, int iStateId);

        [DllImport(UxThemeDll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int GetCurrentThemeName(
            StringBuilder pszThemeFileName, int cchMaxNameChars,
            StringBuilder pszColorBuff, int cchMaxColorChars,
            StringBuilder pszSizeBuff, int cchMaxSizeChars);

        // ------------------------------------------------------------------
        // Property getters
        // ------------------------------------------------------------------

        [DllImport(UxThemeDll, ExactSpelling = true)]
        internal static extern int GetThemeColor(IntPtr hTheme, int iPartId, int iStateId, int iPropId, out int pColor);

        [DllImport(UxThemeDll, ExactSpelling = true)]
        internal static extern int GetThemeInt(IntPtr hTheme, int iPartId, int iStateId, int iPropId, out int piVal);

        [DllImport(UxThemeDll, ExactSpelling = true)]
        internal static extern int GetThemeBool(IntPtr hTheme, int iPartId, int iStateId, int iPropId, out int pfVal);

        [DllImport(UxThemeDll, ExactSpelling = true)]
        internal static extern int GetThemeMetric(IntPtr hTheme, IntPtr hdc, int iPartId, int iStateId, int iPropId, out int piVal);

        [DllImport(UxThemeDll, ExactSpelling = true)]
        internal static extern int GetThemeMargins(IntPtr hTheme, IntPtr hdc, int iPartId, int iStateId, int iPropId, IntPtr prc, out MARGINS pMargins);

        [DllImport(UxThemeDll, ExactSpelling = true)]
        internal static extern int GetThemeRect(IntPtr hTheme, int iPartId, int iStateId, int iPropId, out RECT pRect);

        [DllImport(UxThemeDll, ExactSpelling = true)]
        internal static extern int GetThemePosition(IntPtr hTheme, int iPartId, int iStateId, int iPropId, out POINT pPoint);

        [DllImport(UxThemeDll, ExactSpelling = true)]
        internal static extern int GetThemePartSize(IntPtr hTheme, IntPtr hdc, int iPartId, int iStateId, IntPtr prc, int eSize, out SIZE psz);

        [DllImport(UxThemeDll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int GetThemeFont(IntPtr hTheme, IntPtr hdc, int iPartId, int iStateId, int iPropId, out LOGFONT pFont);

        [DllImport(UxThemeDll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int GetThemeSysFont(IntPtr hTheme, int iFontId, out LOGFONT pFont);

        [DllImport(UxThemeDll, ExactSpelling = true)]
        internal static extern uint GetThemeSysColor(IntPtr hTheme, int iColorId);

        [DllImport(UxThemeDll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int GetThemeString(IntPtr hTheme, int iPartId, int iStateId, int iPropId, StringBuilder pszBuff, int cchMaxBuffChars);

        [DllImport(UxThemeDll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int GetThemeFilename(IntPtr hTheme, int iPartId, int iStateId, int iPropId, StringBuilder pszFilename, int cchMaxBuffChars);

        [DllImport(UxThemeDll, ExactSpelling = true)]
        internal static extern int GetThemeIntList(IntPtr hTheme, int iPartId, int iStateId, int iPropId, IntPtr pIntList);

        [DllImport(UxThemeDll, ExactSpelling = true)]
        internal static extern int GetThemePropertyOrigin(IntPtr hTheme, int iPartId, int iStateId, int iPropId, out int pOrigin);

        // ------------------------------------------------------------------
        // Drawing
        // ------------------------------------------------------------------

        [DllImport(UxThemeDll, ExactSpelling = true)]
        internal static extern int DrawThemeBackground(IntPtr hTheme, IntPtr hdc, int iPartId, int iStateId, ref RECT pRect, ref RECT pClipRect);

        [DllImport(UxThemeDll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int DrawThemeText(IntPtr hTheme, IntPtr hdc, int iPartId, int iStateId,
            string pszText, int cchText, int dwTextFlags, int dwTextFlags2, ref RECT pRect);

        [DllImport(UxThemeDll, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsThemeBackgroundPartiallyTransparent(IntPtr hTheme, int iPartId, int iStateId);

        [DllImport(UxThemeDll, ExactSpelling = true)]
        internal static extern int DrawThemeParentBackground(IntPtr hwnd, IntPtr hdc, ref RECT prc);

        // ------------------------------------------------------------------
        // Misc Win32
        // ------------------------------------------------------------------

        [DllImport(User32Dll, ExactSpelling = true)]
        internal static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport(User32Dll, ExactSpelling = true)]
        internal static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport(User32Dll, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr CreateWindowEx(
            int dwExStyle, string lpClassName, string lpWindowName, int dwStyle,
            int x, int y, int nWidth, int nHeight,
            IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport(User32Dll, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport(Kernel32Dll, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport(Kernel32Dll, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport(Kernel32Dll, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, int dwFlags);

        [DllImport(Kernel32Dll, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FreeLibrary(IntPtr hModule);

        [DllImport(Kernel32Dll, CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);
    }
}
