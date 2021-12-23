using Ikst.ScreenCapture;
using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace LiveFrame
{
    public static class Capture
    {
        static public Bitmap GetWindowBitmap(IntPtr hWnd)
        {
            _ = Win32.DwmGetWindowAttribute(hWnd, Win32.DWMWA_EXTENDED_FRAME_BOUNDS, out Win32.Rect rect, Marshal.SizeOf(typeof(Win32.Rect)));
            if (rect.Width == 0 || rect.Height == 0)
            {
                return null;
            }

            return ScreenCapture.Capture(rect.Left, rect.Top, rect.Width, rect.Height, true);
        }
    }
}
