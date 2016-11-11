using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace NetTest
{
    public partial class frmFTPShow : Form
    {
        IniFile inis = new IniFile(Application.StartupPath + "\\settings.ini");

        public frmFTPShow()
        {
            InitializeComponent();
        }

        private void frmFTPShow_Load(object sender, EventArgs e)
        {
            if (inis.IniReadValue("FTP", "ShowTips") == "1")
                this.checkBox1.Checked = false;
            else
                this.checkBox1.Checked = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (this.checkBox1.Checked)
                inis.IniWriteValue("FTP","ShowTips","0");
            else
                inis.IniWriteValue("FTP", "ShowTips", "1");
            this.Close();
        }
    }
}