using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using Tamir.IPLib;

namespace NetTest
{
    public partial class FTPSettings : DevExpress.XtraEditors.XtraUserControl
    {
        IniFile inis = new IniFile(Application.StartupPath + "\\settings.ini");
        IniFile indll = new IniFile(Application.StartupPath + "\\net.dll");
        public PcapDevice device;
        public FTPSettings()
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
            int.TryParse(inis.IniReadValue("FTP", "Adapter"), out b);
            if (b < 0) { inis.IniWriteValue("FTP", "Adapter", "0"); b = 0; }
            if ((cbAdapter.Items.Count > 0) && (cbAdapter.Items.Count > b))
            {
                cbAdapter.SelectedIndex = Convert.ToInt32(inis.IniReadValue("FTP", "Adapter"));
                //thisCaptureOptions.AdapterIndex = cbAdapter.SelectedIndex;
                //flagAdapter = true;
            }
            else
                cbAdapter.SelectedIndex = 0;
            this.txtHost.Text = inis.IniReadValue("FTP", "Host");
            this.txtPort.Text = inis.IniReadValue("FTP", "Port");
            this.txtUser.Text = inis.IniReadValue("FTP", "User");
            this.txtPath.Text = inis.IniReadValue("FTP", "Path");
            this.txtPass.Text = indll.IniReadValue("FTP", "Pass");
            this.txtDownPath.Text = inis.IniReadValue("FTP", "DownPath");
            this.txtDown.Text = inis.IniReadValue("FTP", "DownNum");

            int iBool;
            int.TryParse(inis.IniReadValue("FTP", "EnableLoop"), out iBool);
            if (iBool > 0)
            {
                this.chkContinue.Checked = true;
                this.txtDown.Enabled = true;
            }
            else
            {
                this.chkContinue.Checked = false;
                this.txtDown.Enabled = false;
            }
            //this.txtDown.Text = inis.IniReadValue("FTP", "DownNum");
        }

        private void btnPath_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbDlg = new FolderBrowserDialog();  //open a folder browser box
            if (fbDlg.ShowDialog() == DialogResult.OK)
            {
                this.txtPath.Text = fbDlg.SelectedPath;
            }
            
        }

        private void btnSetOK_Click(object sender, EventArgs e)
        {
            int b;
            bool btemp = int.TryParse(this.txtPort.Text,out b);
            if (!btemp)
            {
                MessageBox.Show("请检查端口参数...");
                return;
            }
            btemp = int.TryParse(this.txtDown.Text, out b);
            if (!btemp)
            {
                MessageBox.Show("请检查连续下载测试参数...");
                return;
            }
            inis.IniWriteValue("FTP", "Host", this.txtHost.Text);
            inis.IniWriteValue("FTP", "User", this.txtUser.Text);
            inis.IniWriteValue("FTP", "Port", this.txtPort.Text);
            inis.IniWriteValue("FTP", "Path", this.txtPath.Text);
            inis.IniWriteValue("FTP", "DownPath", this.txtDownPath.Text);
            inis.IniWriteValue("FTP", "Adapter", this.cbAdapter.SelectedIndex.ToString());
            //inis.IniWriteValue("FTP","DownNum",this.txtDown.Text);
            indll.IniWriteValue("FTP", "Pass", this.txtPass.Text);

            try
            {
                int.Parse(this.txtDown.Text);
                inis.IniWriteValue("FTP", "DownNum", this.txtDown.Text);
            }
            catch
            {
                inis.IniWriteValue("FTP", "DownNum", "3");
            }
            if (this.chkContinue.Checked) inis.IniWriteValue("FTP", "EnableLoop", "1");
            else inis.IniWriteValue("FTP", "EnableLoop", "0");

            MessageBox.Show("参数设置成功！");
        }

        private void btnSetCancel_Click(object sender, EventArgs e)
        {
            this.txtHost.Text = inis.IniReadValue("FTP", "Host");
            this.txtPort.Text = inis.IniReadValue("FTP", "Port");
            this.txtUser.Text = inis.IniReadValue("FTP", "User");
            this.txtPath.Text = inis.IniReadValue("FTP", "Path");
            this.txtPass.Text = indll.IniReadValue("FTP", "Pass");
            this.txtDownPath.Text = inis.IniReadValue("FTP", "DownPath");
            this.cbAdapter.SelectedIndex = Convert.ToInt32(inis.IniReadValue("FTP", "Adapter"));
            this.txtDown.Text = inis.IniReadValue("FTP", "DownNum");
            int iBool;
            int.TryParse(inis.IniReadValue("FTP", "EnableLoop"), out iBool);
            if (iBool > 0)
            {
                this.chkContinue.Checked = true;
                this.txtDown.Enabled = true;
            }
            else
            {
                this.chkContinue.Checked = false;
                this.txtDown.Enabled = false;
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

        private void chkContinue_CheckedChanged(object sender, EventArgs e)
        {
            if (this.chkContinue.Checked == true)
            {
                this.txtDown.Enabled = true;
                
            }
            else
            {
                this.txtDown.Enabled = false;
                
            }
        }
    }
}
