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
    public partial class MailSet : DevExpress.XtraEditors.XtraUserControl
    {
        IniFile inis = new IniFile(Application.StartupPath + "\\settings.ini");
        IniFile indll = new IniFile(Application.StartupPath + "\\net.dll");
        public PcapDevice device;

        public MailSet()
        {
            InitializeComponent();
        }

        private void btnDownPath_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbDlg = new FolderBrowserDialog();  //open a folder browser box
            if (fbDlg.ShowDialog() == DialogResult.OK)
            {
                this.txtDownPath.Text = fbDlg.SelectedPath;
            }
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
            int.TryParse(inis.IniReadValue("Mail", "Adapter"), out b);
            if (b < 0) { inis.IniWriteValue("Mail", "Adapter", "0"); b = 0; }
            if ((cbAdapter.Items.Count > 0) && (cbAdapter.Items.Count > b))
            {
                cbAdapter.SelectedIndex = Convert.ToInt32(inis.IniReadValue("Mail", "Adapter"));
                //thisCaptureOptions.AdapterIndex = cbAdapter.SelectedIndex;
                //flagAdapter = true;
            }
            else
                cbAdapter.SelectedIndex = 0;
            this.txtSMTP.Text = inis.IniReadValue("Mail", "SMTP");
            this.txtPOP3.Text = inis.IniReadValue("Mail", "POP3");
            this.txtUser.Text = inis.IniReadValue("Mail", "Account");
            this.txtPath.Text = inis.IniReadValue("Mail", "Path");
            this.txtPass.Text = indll.IniReadValue("Mail", "Pass");
            this.txtDownPath.Text = inis.IniReadValue("Mail", "DownPath");
            this.txtSPort.Text = inis.IniReadValue("Mail", "PortSMTP");
            this.txtPPort.Text = inis.IniReadValue("Mail", "PortPOP3");
            this.txtTimeLast.Text = inis.IniReadValue("Mail", "TimeSMTP");
            if (inis.IniReadValue("Mail", "SSL") == "0") this.chkSSL.Checked = false;
            else this.chkSSL.Checked = true;

            if (inis.IniReadValue("Mail", "CheckContinuous") == "1")
            {
                this.chkContinue.Checked = true;
                this.txtContinueNo.Enabled = true;
            }
            else
            {
                this.chkContinue.Checked = false;
                this.txtContinueNo.Enabled = false;
            }
            this.txtContinueNo.Text = inis.IniReadValue("Mail", "NumContinuous");
        }

        private void btnSetOK_Click(object sender, EventArgs e)
        {
            int b;
            bool btemp = int.TryParse(this.txtSPort.Text,out b);
            if(!btemp) 
            {
                MessageBox.Show("SMTP端口设置错误...");
                return;
            }
            btemp = int.TryParse(this.txtPPort.Text, out b);
            if (!btemp)
            {
                MessageBox.Show("POP3端口设置错误...");
                return;
            }
            inis.IniWriteValue("Mail", "SMTP", this.txtSMTP.Text);
            inis.IniWriteValue("Mail", "POP3", this.txtPOP3.Text);
            inis.IniWriteValue("Mail", "Account", this.txtUser.Text);
            inis.IniWriteValue("Mail", "Path", this.txtPath.Text);
            inis.IniWriteValue("Mail", "DownPath", this.txtDownPath.Text);
            inis.IniWriteValue("Mail", "Adapter", this.cbAdapter.SelectedIndex.ToString());
            indll.IniWriteValue("Mail", "Pass", this.txtPass.Text);
            inis.IniWriteValue("Mail","PortSMTP",this.txtSPort.Text);
            inis.IniWriteValue("Mail", "PortPOP3", this.txtPPort.Text);
            if (this.chkSSL.Checked) inis.IniWriteValue("Mail", "SSL", "1");
            else inis.IniWriteValue("Mail", "SSL", "0");
            try
            {
                int.Parse(this.txtTimeLast.Text);
                inis.IniWriteValue("Mail", "TimeSMTP", this.txtTimeLast.Text);
            }
            catch
            {
                inis.IniWriteValue("Mail", "TimeSMTP", "120");
            }
            try
            {
                int.Parse(this.txtContinueNo.Text);
                inis.IniWriteValue("Mail", "NumContinuous", this.txtContinueNo.Text);
            }
            catch
            {
                inis.IniWriteValue("Mail", "NumContinuous", "3");
            }
            if (this.chkContinue.Checked) inis.IniWriteValue("Mail", "CheckContinuous", "1");
            else inis.IniWriteValue("Mail", "CheckContinuous", "0");

            MessageBox.Show("参数设置成功！");
        }

        private void btnSetCancel_Click(object sender, EventArgs e)
        {
            this.txtSMTP.Text = inis.IniReadValue("Mail", "SMTP");
            this.txtPOP3.Text = inis.IniReadValue("Mail", "POP3");
            this.txtUser.Text = inis.IniReadValue("Mail", "Account");
            this.txtPath.Text = inis.IniReadValue("Mail", "Path");
            this.txtPass.Text = indll.IniReadValue("Mail", "Pass");
            this.txtDownPath.Text = inis.IniReadValue("Mail", "DownPath");
            this.cbAdapter.SelectedIndex = Convert.ToInt32(inis.IniReadValue("FTP", "Adapter"));
            this.txtSPort.Text = inis.IniReadValue("Mail", "PortSMTP");
            this.txtPPort.Text = inis.IniReadValue("Mail", "PortPOP3");
            if (inis.IniReadValue("Mail", "SSL") == "0") this.chkSSL.Checked = false;
            else this.chkSSL.Checked = true;
            this.txtTimeLast.Text = inis.IniReadValue("Mail", "TimeSMTP");
            int iBool = Convert.ToInt16(inis.IniReadValue("Mail", "CheckContinuous"));
            if (iBool > 0) this.chkContinue.Checked = true;
            else this.chkContinue.Checked = false;
            this.txtContinueNo.Text = inis.IniReadValue("Mail", "NumContinuous");
        }

        private void btnPath_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbDlg = new FolderBrowserDialog();  //open a folder browser box
            if (fbDlg.ShowDialog() == DialogResult.OK)
            {
                this.txtPath.Text = fbDlg.SelectedPath;
            }
        }

        private void chkSSL_CheckedChanged(object sender, EventArgs e)
        {
            if (this.chkSSL.Checked)
            {
                this.txtSPort.Text = "587";
                this.txtPPort.Text = "995";
            }
            else
            {
                this.txtSPort.Text = "25";
                this.txtPPort.Text = "110";
            }
        }

        private void chkContinue_CheckedChanged(object sender, EventArgs e)
        {
            if (this.chkContinue.Checked == true)
            {
                this.txtContinueNo.Enabled = true;
                //this.chkLoop.Enabled = true;
            }
            else
            {
                this.txtContinueNo.Enabled = false;
                //this.chkLoop.Checked = false;
            }
        }
    }
}
