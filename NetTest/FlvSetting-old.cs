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
                    cbAdapter.Items.Add("TD-CDMA网卡" + "--ip:" + dev.PcapIpAddress);
                else
                {
                    if ((strNetName.Contains("Wireless")) || (strNetName.Contains("wireless")))
                    {
                        if (dev.PcapIpAddress != "0.0.0.0") 
                            cbAdapter.Items.Add("无线网卡" + "--ip:" + dev.PcapIpAddress);
                        else
                            cbAdapter.Items.Add("无线网卡");

                    }
                    else
                    {
                        if (strNetName.Contains("VPN")) cbAdapter.Items.Add("VPN");
                        else
                        {
                            if (dev.PcapIpAddress != "0.0.0.0")
                            {
                                cbAdapter.Items.Add("网卡" + idev + "--ip:" + dev.PcapIpAddress);                                
                            }
                            else
                                cbAdapter.Items.Add("网卡" + idev);
                            idev++;
                        }
                    }
                }
            }

            this.cbSelurl.Items.Clear();
            if (inis.IniReadValue("Flv", "url1") != "") this.cbSelurl.Items.Add(inis.IniReadValue("Flv", "url1"));
            if (inis.IniReadValue("Flv", "url2") != "") this.cbSelurl.Items.Add(inis.IniReadValue("Flv", "url2"));
            if (inis.IniReadValue("Flv", "url3") != "") this.cbSelurl.Items.Add(inis.IniReadValue("Flv", "url3"));
            if (inis.IniReadValue("Flv", "url4") != "") this.cbSelurl.Items.Add(inis.IniReadValue("Flv", "url4"));
            if (inis.IniReadValue("Flv", "url5") != "") this.cbSelurl.Items.Add(inis.IniReadValue("Flv", "url5"));
            if (inis.IniReadValue("Flv", "url6") != "") this.cbSelurl.Items.Add(inis.IniReadValue("Flv", "url6"));
            if (inis.IniReadValue("Flv", "url7") != "") this.cbSelurl.Items.Add(inis.IniReadValue("Flv", "url7"));
            if (inis.IniReadValue("Flv", "url8") != "") this.cbSelurl.Items.Add(inis.IniReadValue("Flv", "url8"));
            if (inis.IniReadValue("Flv", "url9") != "") this.cbSelurl.Items.Add(inis.IniReadValue("Flv", "url9"));
            if (inis.IniReadValue("Flv", "url10") != "") this.cbSelurl.Items.Add(inis.IniReadValue("Flv", "url10"));
           
            if (inis.IniReadValue("Flv", "urlPage") != "")
                this.cbSelurl.Text = inis.IniReadValue("Flv","urlPage");
            else
                this.cbSelurl.Text = inis.IniReadValue("Flv","url1");

            this.cbSeltorrent.Items.Clear();
            if (inis.IniReadValue("Flv", "torrent1") != "") this.cbSeltorrent.Items.Add(inis.IniReadValue("Flv", "torrent1"));
            if (inis.IniReadValue("Flv", "torrent2") != "") this.cbSeltorrent.Items.Add(inis.IniReadValue("Flv", "torrent2"));
            if (inis.IniReadValue("Flv", "torrent3") != "") this.cbSeltorrent.Items.Add(inis.IniReadValue("Flv", "torrent3"));
            if (inis.IniReadValue("Flv", "torrent4") != "") this.cbSeltorrent.Items.Add(inis.IniReadValue("Flv", "torrent4"));
            if (inis.IniReadValue("Flv", "torrent5") != "") this.cbSeltorrent.Items.Add(inis.IniReadValue("Flv", "torrent5"));
            if (inis.IniReadValue("Flv", "torrent6") != "") this.cbSeltorrent.Items.Add(inis.IniReadValue("Flv", "torrent6"));
            if (inis.IniReadValue("Flv", "torrent7") != "") this.cbSeltorrent.Items.Add(inis.IniReadValue("Flv", "torrent7"));
            if (inis.IniReadValue("Flv", "torrent8") != "") this.cbSeltorrent.Items.Add(inis.IniReadValue("Flv", "torrent8"));
            if (inis.IniReadValue("Flv", "torrent9") != "") this.cbSeltorrent.Items.Add(inis.IniReadValue("Flv", "torrent9"));
            if (inis.IniReadValue("Flv", "torrent10") != "") this.cbSeltorrent.Items.Add(inis.IniReadValue("Flv", "torrent10"));

            if (inis.IniReadValue("Flv", "torrentPage") != "")
                this.cbSeltorrent.Text = inis.IniReadValue("Flv", "torrentPage");
            else
                this.cbSeltorrent.Text = inis.IniReadValue("Flv", "torrent1");

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

            this.txtResultPath.Text = inis.IniReadValue("Flv", "Path");
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

            if (inis.IniReadValue("Flv", "Envir").Equals("web"))
            {
                this.rBtnweb.Checked = true;
                this.rBtnp2p.Checked = false;
                this.cbSelurl.Enabled = true;
                this.cbSeltorrent.Enabled = false;
 
            }
            else if (inis.IniReadValue("Flv", "Envir").Equals("p2p"))
            {
                this.rBtnweb.Checked = false;
                this.rBtnp2p.Checked = true;
                this.cbSelurl.Enabled = false;
                this.cbSeltorrent.Enabled = true;

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
            inis.IniWriteValue("Flv", "Path", this.txtResultPath.Text);
            inis.IniWriteValue("Flv", "NumContinuous", this.txtContinueNo.Text);
            inis.IniWriteValue("Flv", "Adapter", this.cbAdapter.SelectedIndex.ToString());

            if (this.rBtnweb.Checked == true)
            {
                if (this.cbSelurl.Text != "")
                    inis.IniWriteValue("Flv", "urlPage", this.cbSelurl.Text);
                else
                    inis.IniWriteValue("Flv", "urlPage", inis.IniReadValue("Flv", "url1"));
                
                inis.IniWriteValue("Flv", "Envir", "web");

                inis.IniWriteValue("Flv", "Player", Application.StartupPath+"\\VideoPlayer");
                this.strpath = inis.IniReadValue("Flv","Player") + "\\path.txt";
                if (File.Exists(this.strpath))
                {
                    File.Delete(this.strpath);
                }
                FileStream fs1 = new FileStream(this.strpath, FileMode.CreateNew, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs1, Encoding.Default);
                sw.Write(inis.IniReadValue("Flv","urlPage")+"\r\n");
                sw.Write("7\r\n");
                sw.Close();
                fs1.Close();
            }
            else if(this.rBtnp2p.Checked == true)
            {
                if (this.cbSelurl.Text != "")
                    inis.IniWriteValue("Flv", "torrentPage", this.cbSeltorrent.Text);
                else
                    inis.IniWriteValue("Flv", "torrentPage", inis.IniReadValue("Flv", "torrent1"));  
             
                inis.IniWriteValue("Flv", "Envir", "p2p");

                inis.IniWriteValue("Flv", "Downloader", Application.StartupPath + "\\DownLoader");
                this.strpath = inis.IniReadValue("Flv", "Downloader") + "\\config\\url.txt";
                if (File.Exists(this.strpath))
                {
                    File.Delete(this.strpath);
                }
                FileStream fs1 = new FileStream(this.strpath, FileMode.CreateNew, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs1, Encoding.Default);
                sw.Write(inis.IniReadValue("Flv", "torrentPage") + "\r\n");                
                sw.Close();
                fs1.Close();

                inis.IniWriteValue("Flv","videoCurrent",inis.IniReadValue("Flv", "Downloader")+inis.IniReadValue("Flv","video"+(this.cbSeltorrent.SelectedIndex+1).ToString()));
                string strplayPath = inis.IniReadValue("Flv", "Downloader") + "\\config\\p2pPlayer\\path.txt";
                if (File.Exists(strplayPath))
                {
                    File.Delete(strplayPath);
                }
                FileStream fs2 = new FileStream(strplayPath, FileMode.CreateNew, FileAccess.Write);
                StreamWriter sw1 = new StreamWriter(fs2, Encoding.Default);
                sw1.Write(inis.IniReadValue("Flv", "videoCurrent") + "\r\n");
                sw1.Write("7\r\n");
                sw1.Close();
                fs2.Close();

                string pref = inis.IniReadValue("Flv", "Downloader") + "\\config\\preferences.ini";
                File.Copy(Application.StartupPath + "\\preferences.ini", pref, true );
            }       

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
            this.cbSelurl.Text = inis.IniReadValue("Flv", "urlPage");
            this.cbSeltorrent.Text = inis.IniReadValue("Flv", "torrentPage");
            
            this.txtResultPath.Text = inis.IniReadValue("Flv", "Path");
            
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

        private void btnDownPath_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbDlg = new FolderBrowserDialog();  //open a folder browser box
            if (fbDlg.ShowDialog() == DialogResult.OK)
            {
                this.txtDownPath.Text = fbDlg.SelectedPath;
            }
        }

        private void rBtnweb_Click(object sender, EventArgs e)
        {
            this.cbSelurl.Enabled = true;
            this.cbSeltorrent.Enabled = false;            
            this.rBtnp2p.Checked = false;
        }

        private void rBtnp2p_Click(object sender, EventArgs e)
        {
            this.cbSelurl.Enabled = false;
            this.cbSeltorrent.Enabled = true;            
            this.rBtnweb.Checked = false;            
        }

    }
}