using Ikst.MouseHook;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
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

        enum FollowMode
        {
            None,
            ActiveWindow,
            MouseCenter,
            MouseFrameBound
        }

        enum CaptureMode
        {
            SafeMode2,
            SafeMode5,
            SafeMode10,
            SafeMode15,
            SafeMode30,
            SafeMode60,
            FastMode
        }

        struct CaptureModeSettings
        {
            public CaptureModeSettings(CaptureMode captureMode, int frameRate, ToolStripMenuItem menuItem)
            {
                this.CaptureMode = captureMode;
                this.FrameRate = frameRate;
                this.MenuItem = menuItem;
            }
            
            public CaptureMode CaptureMode { get; private set; }
            public int FrameRate { get; private set; }
            public ToolStripMenuItem MenuItem { get; private set; }
        }

        private VisibleMode visibleMode = VisibleMode.Edit;
        private FollowMode followMode = FollowMode.None;
        private MouseHook mouseHook;
        private readonly NotifyIcon notifyIcon;
        private readonly List<HotKey> disposer = new();
        private readonly HotKey visibleModeHotKey;
        private readonly HotKey blindfoldModeHotKey;
        private readonly HotKey followActiveWindowModeHotKey;
        private readonly HotKey followMouseModeHotKey;
        private readonly Timer timer;
        private bool enableFindMe = true;
        private static bool FollowSubWindow {
            get { return Properties.Settings.Default.FollowSubWindow; }
            set { Properties.Settings.Default.FollowSubWindow = value; }
        }
        private Bitmap captured;
        private Dictionary<CaptureMode, CaptureModeSettings> subMenuCaptureModeSettings;
        private Dictionary<FollowMode, ToolStripMenuItem> subMenuMouseFollowModeItems = new();

        public LiveForm()
        {
            InitializeComponent();

            InitializeDefaults();

            Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);

            notifyIcon = InitializeTrayIcon();

            if (Enum.IsDefined(typeof(FollowMode), Properties.Settings.Default.FollowMode))
            {
                SetFollowMode((FollowMode)Enum.Parse(typeof(FollowMode), Properties.Settings.Default.FollowMode));
                if (followMode == FollowMode.MouseCenter || followMode == FollowMode.MouseFrameBound)
                {
                    ApplyMouseFollowMode();
                }
            }

            Click += (sender, e) =>
            {
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
                    _ = Win32.SendMessage(Handle, Win32.WM_NCLBUTTONDOWN, Win32.HT_CAPTION, 0);
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
                float aspectRatio = Width / (float)Height;
                int zoom = e.Delta;
                float deltaWidth = zoom * aspectRatio / 10;
                float deltaHeight = zoom * 1.0f / 10;
                SetBounds((int)(Left + deltaWidth),
                    (int)(Top + deltaHeight),
                    (int)(Width - deltaWidth * 2),
                    (int)(Height - deltaHeight * 2));
            };

            Disposed += (sender, e) =>
            {
                foreach (var d in disposer)
                {
                    d.Dispose();
                }
            };

            visibleModeHotKey = new HotKey(MOD_KEY.ALT | MOD_KEY.CONTROL | MOD_KEY.SHIFT, Keys.L);
            disposer.Add(visibleModeHotKey);
            visibleModeHotKey.HotKeyPush += (sender, e) =>
            {
                ToggleEditMode();
            };

            followActiveWindowModeHotKey = new HotKey(MOD_KEY.ALT | MOD_KEY.CONTROL | MOD_KEY.SHIFT, Keys.P);
            disposer.Add(followActiveWindowModeHotKey);
            followActiveWindowModeHotKey.HotKeyPush += (sender, e) =>
            {
                switch (followMode)
                {
                    case FollowMode.None:
                    case FollowMode.MouseCenter:
                    case FollowMode.MouseFrameBound:
                        SetFollowMode(FollowMode.ActiveWindow);
                        break;
                    case FollowMode.ActiveWindow:
                        SetFollowMode(FollowMode.None);
                        break;
                }
            };

            followMouseModeHotKey = new HotKey(MOD_KEY.ALT | MOD_KEY.CONTROL | MOD_KEY.SHIFT, Keys.M);
            disposer.Add(followMouseModeHotKey);
            followMouseModeHotKey.HotKeyPush += (sender, e) =>
            {
                ToggleMouseFollowModeEnable();
            };

            blindfoldModeHotKey = new HotKey(MOD_KEY.ALT | MOD_KEY.CONTROL | MOD_KEY.SHIFT, Keys.B);
            disposer.Add(blindfoldModeHotKey);
            blindfoldModeHotKey.HotKeyPush += (sender, e) =>
            {
                ToggleBlindfoldMode();
            };

            blindfoldModeHotKey = new HotKey(MOD_KEY.ALT | MOD_KEY.CONTROL | MOD_KEY.SHIFT, Keys.F);
            disposer.Add(blindfoldModeHotKey);
            blindfoldModeHotKey.HotKeyPush += (sender, e) =>
            {
                FitToActiveWindow();
            };

            DoubleBuffered = true;

            timer = new Timer
            {
                Interval = 500
            };
            timer.Tick += (sender, e) =>
            {
                var foregroundWindowHandle = Win32.GetForegroundWindow();
                if (!FollowSubWindow)
                {
                    foregroundWindowHandle = Win32.GetAncestor(foregroundWindowHandle, Win32.GetAncestorFlags.GA_ROOTOWNER);
                }

                if (followMode == FollowMode.ActiveWindow)
                {
                    FitToWindow(foregroundWindowHandle);
                }
                else
                {
                    foregroundWindowHandle = Handle;
                }

                if (captured != null)
                {
                    captured.Dispose();
                    captured = null;
                }
                if (enableFindMe && visibleMode != VisibleMode.Edit && visibleMode != VisibleMode.Blindfold)
                {
                    captured = LiveFrame.Capture.GetWindowBitmap(foregroundWindowHandle);
                }

                RefreshTopMost();
            };
            timer.Enabled = true;

            var captureModeSetting = subMenuCaptureModeSettings[(CaptureMode)Enum.Parse(typeof(CaptureMode), Properties.Settings.Default.CaptureMode)];
            SetCaptureMode(captureModeSetting);
            SetFollowMode(followMode);

            SwitchEditMode();
        }

        private void InitializeDefaults()
        {
            if (!Enum.TryParse(typeof(FollowMode), Properties.Settings.Default.FollowMode, false, out object followMode))
            {
                Properties.Settings.Default.CaptureMode = FollowMode.None.ToString();
            }

            if (!Enum.TryParse(typeof(CaptureMode), Properties.Settings.Default.CaptureMode, false, out object captureMode))
            {
                Properties.Settings.Default.CaptureMode = CaptureMode.SafeMode2.ToString();
            }
        }

        private void FitToActiveWindow()
        {
            var foregroundWindowHandle = Win32.GetForegroundWindow();
            if (!FollowSubWindow)
            {
                foregroundWindowHandle = Win32.GetAncestor(foregroundWindowHandle, Win32.GetAncestorFlags.GA_ROOTOWNER);
            }
            FitToWindow(foregroundWindowHandle);
        }

        private void FitToWindow(IntPtr foregroundWindowHandle)
        {
            _ = Win32.DwmGetWindowAttribute(foregroundWindowHandle, Win32.DWMWA_EXTENDED_FRAME_BOUNDS, out Win32.Rect rect, Marshal.SizeOf(typeof(Win32.Rect)));

            // 自分のウィンドウサイズのギャップを計算してサイズを補正する
            Win32.GetWindowRect(Handle, out Win32.Rect rect1);
            _ = Win32.DwmGetWindowAttribute(Handle, Win32.DWMWA_EXTENDED_FRAME_BOUNDS, out Win32.Rect rect2, Marshal.SizeOf(typeof(Win32.Rect)));

            rect.Left += rect1.Left - rect2.Left;
            rect.Top += rect1.Top - rect2.Top;
            rect.Right += rect1.Right - rect2.Right;
            rect.Bottom += rect1.Bottom - rect2.Bottom;

            SetBounds(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
        }

        private NotifyIcon InitializeTrayIcon()
        {
            var notifyIcon = new NotifyIcon
            {
                Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location),
                Visible = true,
                Text = "LiveFrame",
                ContextMenuStrip = new ContextMenuStrip()
            };
            notifyIcon.MouseClick += (sender, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ToggleEditMode();
                }
            };

            var createCaptureModeSettingPair = (CaptureMode captureMode, int frameRate, string menuItemText) =>
            {
                return new Tuple<CaptureMode, CaptureModeSettings>(
                    captureMode,
                    new CaptureModeSettings(captureMode, frameRate, new ToolStripMenuItem(string.Format(menuItemText, frameRate), null, (sender, e) =>
                    {
                        SetCaptureMode(subMenuCaptureModeSettings[captureMode]);
                    }))
                );
            };

            subMenuCaptureModeSettings = (new[] {
                createCaptureModeSettingPair(CaptureMode.SafeMode2, 2, "Safe Mode({0}FPS)"),
                createCaptureModeSettingPair(CaptureMode.SafeMode5, 5, "Safe Mode({0}FPS)"),
                createCaptureModeSettingPair(CaptureMode.SafeMode10, 10, "Safe Mode({0}FPS)"),
                createCaptureModeSettingPair(CaptureMode.SafeMode15, 15, "Safe Mode({0}FPS)"),
                createCaptureModeSettingPair(CaptureMode.SafeMode30, 30, "Safe Mode({0}FPS)"),
                createCaptureModeSettingPair(CaptureMode.SafeMode60, 60, "Safe Mode({0}FPS)"),
                createCaptureModeSettingPair(CaptureMode.FastMode, 2, "Fast Mode"),
            }).ToDictionary(x => x.Item1, x => x.Item2);

            var captureModeSubMenu = new ToolStripMenuItem("&Capture Mode", null, subMenuCaptureModeSettings.Values.Select((s) => s.MenuItem).ToArray());
            notifyIcon.ContextMenuStrip.Items.Add(captureModeSubMenu);

            subMenuMouseFollowModeItems = new Dictionary<FollowMode, ToolStripMenuItem> {
                {
                    FollowMode.None,
                    new ToolStripMenuItem("None", null, (sender, e) => { SetFollowMode(FollowMode.None); })
                },
                {
                    FollowMode.ActiveWindow,
                    new ToolStripMenuItem("Active Window", null, (sender, e) => { SetFollowMode(FollowMode.ActiveWindow); })
                },
                {
                    FollowMode.MouseCenter,
                    new ToolStripMenuItem("Mouse Cursor", null, (sender, e) => { SetFollowMode(FollowMode.MouseCenter); })
                },
                {
                    FollowMode.MouseFrameBound,
                    new ToolStripMenuItem("Mouse Frame Bound", null, (sender, e) => { SetFollowMode(FollowMode.MouseFrameBound); })
                }
            };
            var mouseFollowModeSubMenu = new ToolStripMenuItem("&Follow Mode", null, subMenuMouseFollowModeItems.Values.ToArray());
            notifyIcon.ContextMenuStrip.Items.Add(mouseFollowModeSubMenu);

            var followSubWindowMenu = new ToolStripMenuItem("&Follow Sub-Window", null, (sender, e) =>
            {
                var item = sender as ToolStripMenuItem;
                FollowSubWindow = !FollowSubWindow;
                item.Checked = FollowSubWindow;
            });
            followSubWindowMenu.Checked = FollowSubWindow;
            notifyIcon.ContextMenuStrip.Items.Add(followSubWindowMenu);

            var aboutMenu = new ToolStripMenuItem($"About LiveFrame Version {Application.ProductVersion.Split('+')[0]}");
            aboutMenu.Enabled = false;
            notifyIcon.ContextMenuStrip.Items.Add(aboutMenu);

            var quitMenu = new ToolStripMenuItem("&Quit");
            quitMenu.Click += (object sender, EventArgs e) =>
            {
                notifyIcon.Dispose();
                Application.Exit();
            };
            notifyIcon.ContextMenuStrip.Items.Add(quitMenu);

            notifyIcon.ContextMenuStrip.Opening += (sender, e) => { timer.Enabled = false; };
            notifyIcon.ContextMenuStrip.Closed += (sender, e) => { timer.Enabled = true; };

            return notifyIcon;
        }

        private void SetFollowMode(FollowMode mode)
        {
            Properties.Settings.Default.FollowMode = mode.ToString();
            followMode = mode;
            foreach (var item in subMenuMouseFollowModeItems)
            {
                item.Value.Checked = item.Key == mode;
            }
        }

        private void SetCaptureMode(CaptureModeSettings captureModeSettings)
        {
            Properties.Settings.Default.CaptureMode = captureModeSettings.CaptureMode.ToString();
            SetFindMeMode(captureModeSettings.CaptureMode != CaptureMode.FastMode);
            timer.Interval = 1000 / captureModeSettings.FrameRate;
            SelectCaptureMode(captureModeSettings.MenuItem);
        }

        private void SelectCaptureMode(ToolStripMenuItem selected)
        {
            foreach (var item in subMenuCaptureModeSettings)
            {
                item.Value.MenuItem.Checked = item.Value.MenuItem == selected;
            }
        }

        private void ToggleMouseFollowModeEnable()
        {
            ToggleMouseFollowMode();

            ApplyMouseFollowMode();
        }

        private void ApplyMouseFollowMode()
        {
            if (followMode == FollowMode.MouseCenter || followMode == FollowMode.MouseFrameBound)
            {
                if (mouseHook == null)
                {
                    mouseHook = new MouseHook();
                    mouseHook.MouseMove += (mouseStruct) =>
                    {
                        switch (followMode)
                        {
                            case FollowMode.MouseCenter:
                                {
                                    int x = mouseStruct.pt.x - Width / 2;
                                    int y = mouseStruct.pt.y - Height / 2;

                                    Left = Math.Clamp(x, 0, Screen.PrimaryScreen.Bounds.Width - Width);
                                    Top = Math.Clamp(y, 0, Screen.PrimaryScreen.Bounds.Height - Height);
                                }
                                break;
                            case FollowMode.MouseFrameBound:
                                {
                                    int x = mouseStruct.pt.x;
                                    int y = mouseStruct.pt.y;

                                    if (x < Left)
                                    {
                                        Left = x;
                                    }

                                    if (x > Left + Width)
                                    {
                                        Left = x - Width;
                                    }

                                    if (y < Top)
                                    {
                                        Top = y;
                                    }

                                    if (y > Top + Height)
                                    {
                                        Top = y - Height;
                                    }

                                    Left = Math.Clamp(Left, 0, Screen.PrimaryScreen.Bounds.Width - Width);
                                    Top = Math.Clamp(Top, 0, Screen.PrimaryScreen.Bounds.Height - Height);
                                }
                                break;
                        }
                    };
                }
                mouseHook.Start();
            }
            else
            {
                mouseHook.Stop();
            }
        }

        private void ToggleMouseFollowMode()
        {
            switch (followMode)
            {
                case FollowMode.MouseCenter:
                    SetFollowMode(FollowMode.MouseFrameBound);
                    break;
                case FollowMode.None:
                case FollowMode.ActiveWindow:
                case FollowMode.MouseFrameBound:
                    SetFollowMode(FollowMode.MouseCenter);
                    break;
                default:
                    break;
            }
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            // チラつき防止のため何もしない
        }

        private void LiveForm_Paint(object sender, PaintEventArgs e)
        {
            if (captured != null)
            {
                e.Graphics.DrawImageUnscaled(captured, Point.Empty);
            }
            else
            {
                e.Graphics.DrawRectangle(new Pen(Color.Black), ClientRectangle);
            }
        }

        private void RefreshTopMost()
        {
            TopMost = !TopMost;
            Refresh();
            TopMost = !TopMost;
        }

        private void SetFindMeMode(bool enable)
        {
            enableFindMe = enable;
            SetVisibleMode(visibleMode);
        }

        private void SetVisibleMode(VisibleMode mode)
        {
            visibleMode = mode;
            switch (visibleMode)
            {
                case VisibleMode.Edit:
                    SwitchEditMode();
                    break;
                case VisibleMode.Live:
                    SwitchLiveMode();
                    break;
                case VisibleMode.Blindfold:
                    SwitchBlindfoldMode();
                    break;
            }
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
            switch (visibleMode)
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
            labelLiveFrame.Visible = false;
            if (enableFindMe)
            {
                Text = "LiveFrame find me!";
                ShowInTaskbar = true;
                labelBeRightBack.Visible = false;
            }
            else
            {
                Text = string.Empty;
                ShowInTaskbar = false;
                labelBeRightBack.Visible = true;
            }
            RefreshTopMost();
        }

        private void SwitchEditMode()
        {
            FormBorderStyle = FormBorderStyle.Sizable;
            Opacity = 0.5;
            visibleMode = VisibleMode.Edit;
            labelLiveFrame.Visible = true;
            labelBeRightBack.Visible = false;
            if (enableFindMe)
            {
                Text = "LiveFrame find me!";
                ShowInTaskbar = true;
            }
            else
            {
                Text = string.Empty;
                ShowInTaskbar = false;
            }
        }

        private void SwitchBlindfoldMode()
        {
            if (enableFindMe)
            {
                Opacity = 0;
            }
            else
            {
                Opacity = 1;
            }
            visibleMode = VisibleMode.Blindfold;
            labelLiveFrame.Visible = false;
            labelBeRightBack.Visible = true;
            if (enableFindMe)
            {
                Text = "LiveFrame find me!";
                ShowInTaskbar = true;
            }
            else
            {
                Text = string.Empty;
                ShowInTaskbar = false;
            }
        }
    }
}
