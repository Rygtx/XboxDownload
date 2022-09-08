using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace XboxDownload
{
    public partial class FormNSHomepage : Form
    {
        public FormNSHomepage()
        {
            InitializeComponent();

            tbNSHomepage.Text = Properties.Settings.Default.NSHomepage;
        }

        private void ButSubmit_Click(object sender, EventArgs e)
        {
            string homepage = tbNSHomepage.Text.Trim();
            if (!Regex.IsMatch(homepage, @"https?://")) homepage = "https://" + homepage;
            Properties.Settings.Default.NSHomepage = homepage;
            Properties.Settings.Default.Save();
            this.Close();
        }
    }
}
