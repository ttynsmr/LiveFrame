using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace LiveFrame
{
    public static class Capture
    {
        static public Bitmap GetWindowBitmap(IntPtr hWnd)
        {
            IntPtr hDcWindow = Win32.GetWindowDC(hWnd);
            if (hDcWindow == IntPtr.Zero)
            {
                return null;
            }

            Win32.Rect rect = new Win32.Rect();
            Win32.DwmGetWindowAttribute(hWnd, Win32.DWMWA_EXTENDED_FRAME_BOUNDS, out rect, Marshal.SizeOf(typeof(Win32.Rect)));

            if (rect.Width == 0 || rect.Height == 0)
            {
                if (hDcWindow != IntPtr.Zero)
                { 
                    Win32.ReleaseDC(hWnd, hDcWindow);
                }
                return null;
            }

            Bitmap bitmap = new Bitmap(rect.Width, rect.Height);
            Graphics graphics = Graphics.FromImage(bitmap);

            IntPtr hDcCaptured = graphics.GetHdc();

            Win32.BitBlt(hDcCaptured, 0, 0, rect.Width, rect.Height, hDcWindow, 0, 0, Win32.SRCCOPY);

            graphics.ReleaseHdc(hDcCaptured);
            graphics.Dispose();

            Win32.ReleaseDC(hWnd, hDcWindow);

            return bitmap;
        }
    }
}
