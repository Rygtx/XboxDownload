using System;
using System.Windows.Forms;

namespace XboxDownload
{
    public partial class FormStartup : Form
    {
        public FormStartup()
        {
            InitializeComponent();
        }

        private void ButSubmit_Click(object sender, EventArgs e)
        {
            if (SchTaskExt.IsExists(Form1.appName))
                SchTaskExt.DeleteTask(Form1.appName);
            if (cbStartup.Checked)
                SchTaskExt.CreateRestartTask(Form1.appName);
            this.Close();
        }
    }
}
