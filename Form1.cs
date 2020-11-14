using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace LiveFrame
{
    public partial class Form1 : Form
    {
        private NotifyIcon notifyIcon;
        private bool editable = false;


        public Form1()
        {
            InitializeComponent();

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

            FormBorderStyle = FormBorderStyle.Sizable;
            Opacity = 0.25;
        }

        private void ToggleEditMode()
        {
            if (editable)
            {
                FormBorderStyle = FormBorderStyle.Sizable;
                Opacity = 0.25;
            }
            else
            {
                FormBorderStyle = FormBorderStyle.None;
                Opacity = 0;
            }
            editable = !editable;
        }
    }
}
