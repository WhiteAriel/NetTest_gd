using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace NetTest
{
    public partial class frmAbout : Form
    {
        public frmAbout()
        {
            InitializeComponent();
        }

        private void btnSetOK_Click(object sender, EventArgs e)
        {
            this.Close();
            this.Dispose();
        }

        private void frmAbout_Load(object sender, EventArgs e)
        {

        }
    }
}