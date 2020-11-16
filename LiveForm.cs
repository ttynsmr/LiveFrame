using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace LiveFrame
{
    public partial class LiveForm : Form
    {
        private NotifyIcon notifyIcon;
        private bool editable = true;
        private HotKey hotKey;

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
                SetDesktopBounds((int)(Left + deltaWidth),
                                 (int)(Top + deltaHeight),
                                 (int)(Width - deltaWidth * 2),
                                 (int)(Height - deltaHeight * 2));
            };

            hotKey = new HotKey(MOD_KEY.ALT | MOD_KEY.CONTROL | MOD_KEY.SHIFT, Keys.L);
            hotKey.HotKeyPush += (sender, e) =>
            {
                ToggleEditMode();
            };

            EnableEditMode();
        }

        private void ToggleEditMode()
        {
            if (editable)
            {
                DisableEditMode();
            }
            else
            {
                EnableEditMode();
            }
        }

        private void DisableEditMode()
        {
            Opacity = 0;
            editable = false;
        }

        private void EnableEditMode()
        {
            Opacity = 0.5;
            editable = true;
        }
    }
}
