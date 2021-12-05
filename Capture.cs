using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace LiveFrame
{
    public static class Capture
    {
        static public Bitmap GetWindowBitmap(IntPtr hWnd)
        {
            Win32.Rect rect;
            Win32.DwmGetWindowAttribute(hWnd, Win32.DWMWA_EXTENDED_FRAME_BOUNDS, out rect, Marshal.SizeOf(typeof(Win32.Rect)));
            if (rect.Width == 0 || rect.Height == 0)
            {
                return null;
            }

            Bitmap bitmap = new Bitmap(rect.Width, rect.Height);
            var g = Graphics.FromImage(bitmap);
            g.CopyFromScreen(new Point(rect.Left, rect.Top), Point.Empty, new Size(rect.Width, rect.Height));
            g.Dispose();
            return bitmap;
        }
    }
}
