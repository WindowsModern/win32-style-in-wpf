using System;
using Microsoft.Win32.SafeHandles;
using ThemeExplorer.Core.Interop;

namespace ThemeExplorer.Core
{
    /// <summary>
    /// Safe handle owning an <c>HTHEME</c> returned by <c>OpenThemeData</c>.
    /// <para xml:lang="zh-CN">拥有 <c>OpenThemeData</c> 返回的 <c>HTHEME</c> 的安全句柄。</para>
    /// </summary>
    internal sealed class SafeThemeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SafeThemeHandle()
            : base(true)
        {
        }

        internal SafeThemeHandle(IntPtr handle)
            : base(true)
        {
            SetHandle(handle);
        }

        protected override bool ReleaseHandle()
        {
            return NativeMethods.CloseThemeData(handle) >= 0;
        }
    }
}
