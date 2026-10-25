using System;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Windows.Themes.Interop;

namespace Microsoft.Windows.Themes
{
    /// <summary>
    /// Draws a theme part into a 32bpp DIB section and copies the pixels into a frozen
    /// <see cref="BitmapSource"/>.  Every native object is destroyed before the call returns.
    /// </summary>
    internal static class ThemeDibRenderer
    {
        internal static BitmapSource Render(int width, int height, double dpiScale,
            System.Drawing.Color background, Action<IntPtr> draw)
        {
            if (width <= 0 || height <= 0 || draw == null)
                return null;

            var info = new NativeGdi.BITMAPINFO();
            info.bmiHeader.biSize = Marshal.SizeOf(typeof(NativeGdi.BITMAPINFOHEADER));
            info.bmiHeader.biWidth = width;
            info.bmiHeader.biHeight = -height;          // top-down
            info.bmiHeader.biPlanes = 1;
            info.bmiHeader.biBitCount = 32;
            info.bmiHeader.biCompression = 0;           // BI_RGB

            IntPtr dc = NativeGdi.CreateCompatibleDC(IntPtr.Zero);
            if (dc == IntPtr.Zero)
                return null;

            IntPtr bits;
            IntPtr dib = NativeGdi.CreateDIBSection(dc, ref info, NativeGdi.DIB_RGB_COLORS,
                out bits, IntPtr.Zero, 0);
            if (dib == IntPtr.Zero)
            {
                NativeGdi.DeleteDC(dc);
                return null;
            }

            IntPtr previous = NativeGdi.SelectObject(dc, dib);
            try
            {
                using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromHdc(dc))
                {
                    graphics.Clear(background);
                }

                draw(dc);

                int stride = width * 4;
                var pixels = new byte[stride * height];
                Marshal.Copy(bits, pixels, 0, pixels.Length);

                BitmapSource source = BitmapSource.Create(width, height,
                    96.0 * dpiScale, 96.0 * dpiScale,
                    PixelFormats.Bgr32, null, pixels, stride);
                source.Freeze();
                return source;
            }
            finally
            {
                NativeGdi.SelectObject(dc, previous);
                NativeGdi.DeleteObject(dib);
                NativeGdi.DeleteDC(dc);
            }
        }
    }
}
