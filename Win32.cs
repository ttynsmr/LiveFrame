using System;
using System.Runtime.InteropServices;

namespace LiveFrame
{
    class Win32
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
 
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowRect(IntPtr hWnd, out Rect rect);

        public const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
        [DllImport("dwmapi.dll")]
        public static extern int DwmGetWindowAttribute(IntPtr hWnd, int dwAttribute, out Rect rect, int cbAttribute);

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width { get { return Right - Left; } }
            public int Height { get { return Bottom - Top; } }
        }
    }
}
