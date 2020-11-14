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

            Click += (sender, e) => ToggleEditMode();

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
