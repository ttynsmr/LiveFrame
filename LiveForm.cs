using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LiveFrame
{
    public partial class LiveForm : Form
    {
        enum VisibleMode
        {
            Edit,
            Live,
            Blindfold
        }

        private NotifyIcon notifyIcon;
        private VisibleMode visibleMode = VisibleMode.Edit;
        private List<HotKey> disposer = new List<HotKey>();
        private HotKey visibleModeHotKey;
        private HotKey blindfoldModeHotKey;
        private HotKey followModeHotKey;
        private Timer timer;

        public LiveForm()
        {
            InitializeComponent();

            Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);

            notifyIcon = new NotifyIcon
            {
                Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location),
                Visible = true
            };
            notifyIcon.Click += (sender, e) => ToggleEditMode();
            notifyIcon.DoubleClick += (sender, e) => Application.Exit();

            Click += (sender, e) => {
                if (((MouseEventArgs)e).Button == MouseButtons.Right)
                {
                    ToggleEditMode();
                }
            };

            MouseDown += (sender, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    Win32.ReleaseCapture();
                    Win32.SendMessage(Handle, Win32.WM_NCLBUTTONDOWN, Win32.HT_CAPTION, 0);
                }
            };

            KeyDown += (sender, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    ToggleEditMode();
                }
            };

            MouseWheel += (sender, e) =>
            {
                // zoom in / out
                float aspectRatio = (float)Width / (float)Height;
                int zoom = e.Delta;
                float deltaWidth = zoom * aspectRatio / 10;
                float deltaHeight = zoom * 1.0f / 10;
                SetBounds((int)(Left + deltaWidth),
                                 (int)(Top + deltaHeight),
                                 (int)(Width - deltaWidth * 2),
                                 (int)(Height - deltaHeight * 2));
            };

            visibleModeHotKey = new HotKey(MOD_KEY.ALT | MOD_KEY.CONTROL | MOD_KEY.SHIFT, Keys.L);
            disposer.Add(visibleModeHotKey);
            visibleModeHotKey.HotKeyPush += (sender, e) =>
            {
                ToggleEditMode();
            };

            followModeHotKey = new HotKey(MOD_KEY.ALT | MOD_KEY.CONTROL | MOD_KEY.SHIFT, Keys.P);
            disposer.Add(followModeHotKey);
            followModeHotKey.HotKeyPush += (sender, e) =>
            {
                timer.Enabled = !timer.Enabled;
            };

            blindfoldModeHotKey = new HotKey(MOD_KEY.ALT | MOD_KEY.CONTROL | MOD_KEY.SHIFT, Keys.B);
            disposer.Add(blindfoldModeHotKey);
            blindfoldModeHotKey.HotKeyPush += (sender, e) =>
            {
                ToggleBlindfoldMode();
            };

            Disposed += (sender, e) => {
                foreach (var d in disposer)
                {
                    d.Dispose();
                }
            };

            timer = new Timer();
            timer.Interval = 500;
            timer.Tick += (sender, e) =>
            {
                var foregroundWindowHandle = Win32.GetForegroundWindow();
                Win32.Rect rect = new Win32.Rect();
                Win32.DwmGetWindowAttribute(foregroundWindowHandle, Win32.DWMWA_EXTENDED_FRAME_BOUNDS, out rect, Marshal.SizeOf(typeof(Win32.Rect)));

                // 自分のウィンドウサイズのギャップを計算してサイズを補正する
                Win32.Rect rect1 = new Win32.Rect();
                Win32.GetWindowRect(Handle, out rect1);
                Win32.Rect rect2 = new Win32.Rect();
                Win32.DwmGetWindowAttribute(Handle, Win32.DWMWA_EXTENDED_FRAME_BOUNDS, out rect2, Marshal.SizeOf(typeof(Win32.Rect)));

                rect.Left += rect1.Left - rect2.Left;
                rect.Top += rect1.Top - rect2.Top;
                rect.Right += rect1.Right - rect2.Right;
                rect.Bottom += rect1.Bottom - rect2.Bottom;

                SetBounds(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
            };
            timer.Enabled = false;

            SwitchEditMode();
        }

        private void ToggleBlindfoldMode()
        {
            switch (visibleMode)
            {
                case VisibleMode.Edit:
                    break;
                case VisibleMode.Live:
                    SwitchBlindfoldMode();
                    break;
                case VisibleMode.Blindfold:
                    SwitchLiveMode();
                    break;
            }
        }

        private void ToggleEditMode()
        {
            switch(visibleMode)
            {
                case VisibleMode.Edit:
                    SwitchLiveMode();
                    break;
                case VisibleMode.Live:
                case VisibleMode.Blindfold:
                    SwitchEditMode();
                    break;
            }
        }

        private void SwitchLiveMode()
        {
            FormBorderStyle = FormBorderStyle.None;
            Opacity = 0;
            visibleMode = VisibleMode.Live;
            label1.Visible = false;
            label2.Visible = true;
            TopMost = false;
            Refresh();
            TopMost = true;
        }

        private void SwitchEditMode()
        {
            FormBorderStyle = FormBorderStyle.Sizable;
            Opacity = 0.5;
            visibleMode = VisibleMode.Edit;
            label1.Visible = true;
            label2.Visible = false;
        }

        private void SwitchBlindfoldMode()
        {
            Opacity = 1;
            visibleMode = VisibleMode.Blindfold;
            label1.Visible = false;
            label2.Visible = true;
        }
    }
}
