using Ikst.MouseHook;
using System;
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

        enum MouseFollowMode
        {
            Center,
            FrameBound
        }

        private NotifyIcon notifyIcon;
        private VisibleMode visibleMode = VisibleMode.Edit;
        private List<HotKey> disposer = new List<HotKey>();
        private HotKey visibleModeHotKey;
        private HotKey blindfoldModeHotKey;
        private HotKey followActiveWindowModeHotKey;
        private HotKey followMouseModeHotKey;
        private Timer timer;
        private bool enableFollowActiveWindow = false;
        private bool enableFollowMouse = false;
        private MouseFollowMode mouseFollowMode = MouseFollowMode.Center;
        private bool enableFindMe = true;
        private bool followSubWindow = true;
        private Bitmap captured;
        private ToolStripMenuItem[] subMenuItems;
        private MouseHook mouseHook;

        public LiveForm()
        {
            InitializeComponent();

            Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);

            notifyIcon = new NotifyIcon
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

            {
                subMenuItems = new ToolStripMenuItem[] {
                    new ToolStripMenuItem("Safe Mode(2FPS)", null, (sender, e) => {
                        SetCaptureMode(sender as ToolStripMenuItem, false, 2);
                    }),
                    new ToolStripMenuItem("Safe Mode(5FPS)", null, (sender, e) => {
                        SetCaptureMode(sender as ToolStripMenuItem, false, 5);
                    }),
                    new ToolStripMenuItem("Safe Mode(10FPS)", null, (sender, e) => {
                        SetCaptureMode(sender as ToolStripMenuItem, false, 10);
                    }),
                    new ToolStripMenuItem("Safe Mode(15FPS)", null, (sender, e) => {
                        SetCaptureMode(sender as ToolStripMenuItem, false, 15);
                    }),
                    new ToolStripMenuItem("Safe Mode(30FPS)", null, (sender, e) => {
                        SetCaptureMode(sender as ToolStripMenuItem, false, 30);
                    }),
                    new ToolStripMenuItem("Safe Mode(60FPS)", null, (sender, e) => {
                        SetCaptureMode(sender as ToolStripMenuItem, false, 60);
                    }),
                    new ToolStripMenuItem("Fast Mode", null, (sender, e) => {
                        SetCaptureMode(sender as ToolStripMenuItem, true, 2);
                    })
                };
                var toolStripMenuItem = new ToolStripMenuItem("&Capture Mode", null, subMenuItems);
                notifyIcon.ContextMenuStrip.Items.Add(toolStripMenuItem);
            }

            {
                var toolStripMenuItem = new ToolStripMenuItem("&Mouse Follow Mode", null, new ToolStripItem[] {
                    new ToolStripMenuItem("Cursor", null, (sender, e) => {
                        mouseFollowMode = MouseFollowMode.Center;
                    }),
                    new ToolStripMenuItem("Frame Bound", null, (sender, e) => {
                        mouseFollowMode = MouseFollowMode.FrameBound;
                    })
                });
                notifyIcon.ContextMenuStrip.Items.Add(toolStripMenuItem);
            }

            {
                var toolStripMenuItem = new ToolStripMenuItem("&Follow Sub-Window", null, (sender, e) => {
                    var item = sender as ToolStripMenuItem;
                    followSubWindow = !followSubWindow;
                    item.Checked = followSubWindow;
                });
                toolStripMenuItem.Checked = followSubWindow;
                notifyIcon.ContextMenuStrip.Items.Add(toolStripMenuItem);
            }

            {
                var toolStripMenuItem = new ToolStripMenuItem("&Quit");
                toolStripMenuItem.Click += (object sender, EventArgs e) =>
                {
                    notifyIcon.Dispose();
                    Application.Exit();
                };
                notifyIcon.ContextMenuStrip.Items.Add(toolStripMenuItem);
            }

            notifyIcon.ContextMenuStrip.Opening += (sender, e) => { timer.Enabled = false; };
            notifyIcon.ContextMenuStrip.Closed += (sender, e) => { timer.Enabled = true; };

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

            followActiveWindowModeHotKey = new HotKey(MOD_KEY.ALT | MOD_KEY.CONTROL | MOD_KEY.SHIFT, Keys.P);
            disposer.Add(followActiveWindowModeHotKey);
            followActiveWindowModeHotKey.HotKeyPush += (sender, e) =>
            {
                enableFollowActiveWindow = !enableFollowActiveWindow;
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

            Disposed += (sender, e) => {
                foreach (var d in disposer)
                {
                    d.Dispose();
                }
            };

            DoubleBuffered = true;

            timer = new Timer();
            timer.Interval = 500;
            timer.Tick += (sender, e) =>
            {
                var foregroundWindowHandle = Win32.GetForegroundWindow();
                if (!followSubWindow)
                {
                    foregroundWindowHandle = Win32.GetAncestor(foregroundWindowHandle, Win32.GetAncestorFlags.GA_ROOTOWNER);
                }

                if (enableFollowActiveWindow)
                {
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
                if (enableFindMe && visibleMode != VisibleMode.Edit)
                {
                    captured = LiveFrame.Capture.GetWindowBitmap(foregroundWindowHandle);
                }

                RefreshTopMost();
            };
            timer.Enabled = true;

            SetCaptureMode(subMenuItems[0], false, 2);

            SwitchEditMode();
        }

        private void SetCaptureMode(ToolStripMenuItem selected, bool isFast, int fps)
        {
            SetFindMeMode(!isFast);
            timer.Interval = 1000 / fps;
            SelectCaptureMode(selected);
        }

        private void SelectCaptureMode(ToolStripMenuItem selected)
        {
            foreach (var item in subMenuItems)
            {
                item.Checked = item == selected;
            }
        }

        private void ToggleMouseFollowModeEnable()
        {
            enableFollowMouse = !enableFollowMouse;
            enableFollowActiveWindow = false;
            if (enableFollowMouse)
            {
                if (mouseHook == null)
                {
                    mouseHook = new MouseHook();
                    mouseHook.MouseMove += (mouseStruct) => {
                        switch(mouseFollowMode)
                        {
                            case MouseFollowMode.Center:
                                {
                                    int x = mouseStruct.pt.x - Width / 2;
                                    int y = mouseStruct.pt.y - Height / 2;

                                    Left = Math.Clamp(x, 0, Screen.PrimaryScreen.Bounds.Width - Width);
                                    Top = Math.Clamp(y, 0, Screen.PrimaryScreen.Bounds.Height - Height);
                                }
                                break;
                            case MouseFollowMode.FrameBound:
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
            switch (mouseFollowMode)
            {
                case MouseFollowMode.Center:
                    mouseFollowMode = MouseFollowMode.FrameBound;
                    break;
                case MouseFollowMode.FrameBound:
                    mouseFollowMode = MouseFollowMode.Center;
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
            Opacity = 1;
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
