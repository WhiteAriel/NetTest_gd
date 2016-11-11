using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using DevExpress.XtraEditors;
using Tamir.IPLib;

namespace NetTest
{
    public partial class FlvSetting : DevExpress.XtraEditors.XtraUserControl
    {
        IniFile inis = new IniFile(Application.StartupPath + "\\settings.ini");

        public PcapDevice device;
        string strpath;

        public FlvSetting()
        {
            InitializeComponent();
        }
        
        public void Init()
        {
            PcapDeviceList devices = SharpPcap.GetAllDevices();
            int idev = 1;
            cbAdapter.Items.Clear();
            foreach (PcapDevice dev in devices)
            {
                /* Description */
                string strNetName = dev.PcapDescription;
                if ((strNetName.Contains("PPP")) || (strNetName.Contains("SLIP")) || (strNetName.Contains("ppp")))
                    cbAdapter.Items.Add("TD-CDMA网卡");
                else
                {
                    if ((strNetName.Contains("Wireless")) || (strNetName.Contains("wireless")))
                        cbAdapter.Items.Add("无线网卡");
                    else
                    {
                        if (strNetName.Contains("VPN")) cbAdapter.Items.Add("VPN");
                        else
                        {
                            cbAdapter.Items.Add("网卡" + idev);
                            idev++;
                        }
                    }
                }
            }

            int b;
            int.TryParse(inis.IniReadValue("Flv", "Adapter"), out b);
            if (b < 0) { inis.IniWriteValue("Flv", "Adapter", "0"); b = 0; }
            if ((cbAdapter.Items.Count > 0) && (cbAdapter.Items.Count > b))
            {
                cbAdapter.SelectedIndex = Convert.ToInt32(inis.IniReadValue("Flv", "Adapter"));
                //thisCaptureOptions.AdapterIndex = cbAdapter.SelectedIndex;
                //flagAdapter = true;
            }
            else
                cbAdapter.SelectedIndex = 0;
            this.txtUrl1.Text = inis.IniReadValue("Flv", "urlPage");
            this.txtPlayerPath.Text = inis.IniReadValue("Flv", "Player");
            this.txtResultPath.Text = inis.IniReadValue("Flv", "Path");
            this.txtDownPath.Text = inis.IniReadValue("Flv", "DPath");
            this.txtContinueNo.Text = inis.IniReadValue("Flv", "NumContinuous");
            this.strpath = inis.IniReadValue("Flv","player")+"\\path.txt";
            
            int iBool;
            int.TryParse(inis.IniReadValue("Flv", "CheckContinuous"), out iBool);
            if (iBool > 0)
            {
                this.chkContinue.Checked = true;
                this.txtContinueNo.Enabled = true;
            }
            else
            {
                this.chkContinue.Checked = false;
                this.txtContinueNo.Enabled = false;
            }

        }

        private void btnResultPath_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbDlg = new FolderBrowserDialog();  //open a folder browser box
            if (fbDlg.ShowDialog() == DialogResult.OK)
            {
                this.txtResultPath.Text = fbDlg.SelectedPath;
            }
            
        }

        private void btnSetOK_Click(object sender, EventArgs e)
        {

            inis.IniWriteValue("Flv", "urlPage", this.txtUrl1.Text);
            inis.IniWriteValue("Flv", "URL1", this.txtUrl1.Text);
            inis.IniWriteValue("Flv", "Player", this.txtPlayerPath.Text);
            inis.IniWriteValue("Flv", "Path", this.txtResultPath.Text);
            inis.IniWriteValue("Flv", "DPath", this.txtDownPath.Text);
            inis.IniWriteValue("Flv", "NumContinuous", this.txtContinueNo.Text);

            this.strpath = this.txtPlayerPath.Text+ "\\path.txt";
            if (File.Exists(this.strpath))
            {
                File.Delete(this.strpath);
            }           
            FileStream fs1 = new FileStream(this.strpath, FileMode.CreateNew, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs1, Encoding.Default);
            sw.Write(this.txtUrl1.Text.ToString());
            sw.Close();
            fs1.Close();
         


            try
            {
                int.Parse(this.txtContinueNo.Text);
                inis.IniWriteValue("Flv", "NumContinuous", this.txtContinueNo.Text);
            }
            catch
            {
                inis.IniWriteValue("Flv", "NumContinuous", "3");
            }
            if (this.chkContinue.Checked) inis.IniWriteValue("Flv", "CheckContinuous", "1");
            else inis.IniWriteValue("Flv", "CheckContinuous", "0");

            MessageBox.Show("参数设置成功！");
        }

        private void btnSetCancel_Click(object sender, EventArgs e)
        {
            this.txtUrl1.Text = inis.IniReadValue("Flv", "urlPage");
            this.txtPlayerPath.Text = inis.IniReadValue("Flv", "Player");
            this.txtResultPath.Text = inis.IniReadValue("Flv", "Path");
            this.txtDownPath.Text = inis.IniReadValue("Flv", "DPath");
            this.txtContinueNo.Text = inis.IniReadValue("Flv", "NumContinuous");

            this.cbAdapter.SelectedIndex = Convert.ToInt32(inis.IniReadValue("Flv", "Adapter"));
            this.txtContinueNo.Text = inis.IniReadValue("Flv", "NumContinuous");
            int iBool;
            int.TryParse(inis.IniReadValue("Flv", "CheckContinuous"), out iBool);
            if (iBool > 0)
            {
                this.chkContinue.Checked = true;
                this.txtContinueNo.Enabled = true;
            }
            else
            {
                this.chkContinue.Checked = false;
                this.txtContinueNo.Enabled = false;
            }
        }

        private void chkContinue_CheckedChanged(object sender, EventArgs e)
        {
            if (this.chkContinue.Checked == true)
            {
                this.txtContinueNo.Enabled = true;
                
            }
            else
            {
                this.txtContinueNo.Enabled = false;
                
            }
        }

        private void btnPlayerPath_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbDlg = new FolderBrowserDialog();  //open a folder browser box
            if (fbDlg.ShowDialog() == DialogResult.OK)
            {
                this.txtPlayerPath.Text = fbDlg.SelectedPath;
            }

        }

        private void btnDownPath_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbDlg = new FolderBrowserDialog();  //open a folder browser box
            if (fbDlg.ShowDialog() == DialogResult.OK)
            {
                this.txtDownPath.Text = fbDlg.SelectedPath;
            }
        }
    }
}