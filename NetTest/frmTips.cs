using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace NetTest
{
    public partial class frmTips : Form
    {
        IniFile inis = new IniFile(Application.StartupPath + "\\settings.ini");
        public frmTips()
        {
            InitializeComponent();
        }

        private void btnSetOK_Click(object sender, EventArgs e)
        {
            if(this.checkBox1.Checked)
                inis.IniWriteValue("All", "ShowTips", "0");
            else
                inis.IniWriteValue("All", "ShowTips", "1");
            this.Close();
            this.Dispose();
        }

        private void frmTips_Load(object sender, EventArgs e)
        {
            if (inis.IniReadValue("All", "ShowTips") =="0")
                this.checkBox1.Checked = true;
            else
                this.checkBox1.Checked = false;
        }
    }
}