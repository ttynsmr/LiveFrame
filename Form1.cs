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

            notifyIcon.Click += NotifyIcon_DoubleClick;
            notifyIcon.DoubleClick += NotifyIcon_DoubleClick1;
            
            Click += NotifyIcon_DoubleClick;

            FormBorderStyle = FormBorderStyle.Sizable;
            Opacity = 0.25;
        }

        private void NotifyIcon_DoubleClick1(object sender, System.EventArgs e)
        {
            Application.Exit();
        }

        private void NotifyIcon_DoubleClick(object sender, System.EventArgs e)
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
