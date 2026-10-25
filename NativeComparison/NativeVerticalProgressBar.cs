using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NativeComparison
{
    /// <summary>
    /// WinForms does not expose PBS_VERTICAL.  Add it to the native
    /// msctls_progress32 style before the control creates its HWND.
    /// </summary>
    internal sealed class NativeVerticalProgressBar : ProgressBar
    {
        private const int PbsVertical = 0x04;
        private const int GwlStyle = -16;
        private const int SwpNoMove = 0x0002;
        private const int SwpNoSize = 0x0001;
        private const int SwpNoZOrder = 0x0004;
        private const int SwpFrameChanged = 0x0020;

        [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int x, int y, int cx, int cy, uint flags);

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // ProgressBar can overwrite the CreateParams style while creating
            // the native control.  Apply PBS_VERTICAL to the real HWND as well.
            int style = GetWindowLong(Handle, GwlStyle);
            if ((style & PbsVertical) == 0)
            {
                SetWindowLong(Handle, GwlStyle, style | PbsVertical);
                SetWindowPos(Handle, IntPtr.Zero, 0, 0, 0, 0,
                    SwpNoMove | SwpNoSize | SwpNoZOrder | SwpFrameChanged);
            }
        }
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams parameters = base.CreateParams;
                parameters.Style |= PbsVertical;
                return parameters;
            }
        }
    }
}
