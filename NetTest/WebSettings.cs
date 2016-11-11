using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using System.IO;
using Tamir.IPLib;
using SharpPcap.LibPcap;
using NetLog;

namespace NetTest
{
    public partial class WebSettings : DevExpress.XtraEditors.XtraUserControl
    {

        IniFile inis = new IniFile(Application.StartupPath + "\\settings.ini");
        //public PcapDevice device;
        public bool flagAdapter = false;
        //private int iTest = 0;
        private int iNumContinuous = 0;
        //private int iTimeThrehold = 0;
        private int iTimeLast = 0;
        private int iIndexTemp = 0;
        public WebSettings()
        {
            InitializeComponent();
        }

        public void Init()
        {
            searchAdapter();
            this.cbSelWeb.Items.Clear();
            if (inis.IniReadValue("Web", "web1") != "") this.cbSelWeb.Items.Add(inis.IniReadValue("Web", "web1"));
            if (inis.IniReadValue("Web", "web2") != "") this.cbSelWeb.Items.Add(inis.IniReadValue("Web", "web2"));
            if (inis.IniReadValue("Web", "web3") != "") this.cbSelWeb.Items.Add(inis.IniReadValue("Web", "web3"));
            if (inis.IniReadValue("Web", "web4") != "") this.cbSelWeb.Items.Add(inis.IniReadValue("Web", "web4"));
            if (inis.IniReadValue("Web", "web5") != "") this.cbSelWeb.Items.Add(inis.IniReadValue("Web", "web5"));
            if (inis.IniReadValue("Web", "EnableLoop") == "1") this.chkLoop.Checked = true;
            else this.chkLoop.Checked = false;
            int b;
            int.TryParse(inis.IniReadValue("Web", "Adapter"), out b);
            if (b < 0) { inis.IniWriteValue("Web", "Adapter", "0"); b = 0; }
            if ((cbAdapter.Items.Count > 0) && (cbAdapter.Items.Count > b))
            {
                int.TryParse(inis.IniReadValue("Web", "Adapter"), out b);
                cbAdapter.SelectedIndex = b;
                //thisCaptureOptions.AdapterIndex = cbAdapter.SelectedIndex;
                flagAdapter = true;
            }
            else if (cbAdapter.Items.Count == 0)
            {
                cbAdapter.SelectedIndex = -1;
            }
            //else
            //{
            //    cbAdapter.SelectedIndex = 0;
            //}
            //t= new Thread(new ThreadStart(StartInGrid));
            int iBool;
            int.TryParse(inis.IniReadValue("Web", "CheckContinuous"), out iBool);
            if (iBool > 0) this.chkContinue.Checked = true;
            else this.chkContinue.Checked = false;
            int.TryParse(inis.IniReadValue("Web", "NumContinuous"), out iNumContinuous);       
            int.TryParse(inis.IniReadValue("Web", "CheckCookies"), out iBool);
            if (iBool > 0) this.chkClearCookies.Checked = true;
            else this.chkClearCookies.Checked = false;

            int.TryParse(inis.IniReadValue("Web", "TimeLast"), out iTimeLast);

            this.cbBrowser.Text = inis.IniReadValue("Web", "Browser");
            this.cbSelWeb.Text = inis.IniReadValue("Web", "WebPage");
            this.txtInterval.Text = inis.IniReadValue("Web", "Interval");
            this.txtTimeLast.Text = inis.IniReadValue("Web", "TimeLast");
            this.txtPath.Text = inis.IniReadValue("Web", "Path");
            int.TryParse(inis.IniReadValue("Web", "CheckContinuous"), out iBool);
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
            this.txtContinueNo.Text = inis.IniReadValue("Web", "NumContinuous");
            //iBool = Convert.ToInt16(inis.IniReadValue("Web", "CheckCookies"));
            int.TryParse(inis.IniReadValue("Web", "CheckCookies"), out iBool);
            if (iBool > 0) this.chkClearCookies.Checked = true;
            else this.chkClearCookies.Checked = false;

            //this.IESecurity();
            this.txtPrefs.Text = inis.IniReadValue("Web", "FirefoxLoc");
            this.txtFirefox.Text = inis.IniReadValue("Web", "FirefoxPlus");
            this.txtFCookie.Text = inis.IniReadValue("Web", "FirefoxCookie");
        }




        private void btnSetOK_Click(object sender, EventArgs e)
        {
            //inis.IniWriteValue("Web", "WebPage", this.cbSelWeb.Items[this.cbSelWeb.SelectedIndex].ToString());
            //string str = this.cbSelWeb.Text;
            //str = validateFileName(str);   //检查网页名
            inis.IniWriteValue("Web", "WebPage", this.cbSelWeb.Text);
       
            inis.IniWriteValue("Web", "Browser", this.cbBrowser.Items[this.cbBrowser.SelectedIndex].ToString());
            try
            {
                int.Parse(this.txtInterval.Text);
                inis.IniWriteValue("Web", "Interval", this.txtInterval.Text);
            }
            catch
            {
                inis.IniWriteValue("Web", "Interval", "10");
            }
            try
            {
                int.Parse(this.txtContinueNo.Text);
                inis.IniWriteValue("Web", "NumContinuous", this.txtContinueNo.Text);
            }
            catch
            {
                inis.IniWriteValue("Web", "NumContinuous", "3");
            }
            try
            {
                int.Parse(this.txtTimeLast.Text);
                inis.IniWriteValue("Web", "TimeLast", this.txtTimeLast.Text);
            }
            catch
            {
                inis.IniWriteValue("Web", "TimeLast", "60");
            }

            try
            {
                if (!Directory.Exists(this.txtPath.Text))
                    Directory.CreateDirectory(this.txtPath.Text);
                inis.IniWriteValue("Web", "Path", this.txtPath.Text);  //不抛出异常说明正确，写入配置
            }
            catch (System.Exception ex)
            {
               Log.Console(Environment.StackTrace,ex); Log.Warn(Environment.StackTrace,ex);
                this.txtPath.Text = inis.IniReadValue("Web", "Path");
                MessageBox.Show("存储路径错误！");   //出错不写入配置
                return;
            }

           // inis.IniWriteValue("Web", "Path", this.txtPath.Text);
            inis.IniWriteValue("Web", "Adapter", this.cbAdapter.SelectedIndex.ToString());
            if (this.chkContinue.Checked) inis.IniWriteValue("Web", "CheckContinuous", "1");
            else inis.IniWriteValue("Web", "CheckContinuous", "0");
            if ((this.chkLoop.Checked) && (this.chkContinue.Checked))
                inis.IniWriteValue("Web", "EnableLoop", "1");
            else
                inis.IniWriteValue("Web", "EnableLoop", "0");
            int iIndex = this.cbSelWeb.SelectedIndex + 1;
            inis.IniWriteValue("Web", "WebIndex", iIndex.ToString());
            inis.IniWriteValue("Web", "LoopIndex", iIndex.ToString());
            //this.bar3.Text = "Parameters Updated";
            MessageBox.Show("参数设置成功！");
        }

        private void btnSetCancel_Click(object sender, EventArgs e)
        {
            this.cbBrowser.Text = inis.IniReadValue("Web", "Browser");
            this.cbSelWeb.Text = inis.IniReadValue("Web", "WebPage");
            this.txtInterval.Text = inis.IniReadValue("Web", "Interval");
            this.txtPath.Text = inis.IniReadValue("Web", "Path");
            this.cbAdapter.SelectedIndex = Convert.ToInt32(inis.IniReadValue("Web", "Adapter"));

            int iBool = Convert.ToInt16(inis.IniReadValue("Web", "CheckContinuous"));
            if (iBool > 0) this.chkContinue.Checked = true;
            else this.chkContinue.Checked = false;
            this.txtContinueNo.Text = inis.IniReadValue("Web", "NumContinuous");
            this.txtTimeLast.Text = inis.IniReadValue("Web", "TimeLast");
        }



        private void btnIEOK_Click(object sender, EventArgs e)
        {
            QuickRegistry reg = new QuickRegistry();
            reg.OpenKey("HKEY_CURRENT_USER", @"SOFTWARE\Microsoft\Windows\CurrentVersion\Internet Settings\zones\3", true);
            //int iTemp = this.radioAC.SelectedIndex;
            //if (iTemp == 2) reg.SetValue("1001", 3);
            //else reg.SetValue("1001", iTemp);
            //iTemp = this.radioACNo.SelectedIndex;
            //if (iTemp == 2) reg.SetValue("1004", 3);
            //else reg.SetValue("1004", iTemp);
            //iTemp = this.radioACRun.SelectedIndex;
            //if (iTemp == 2) reg.SetValue("1200", 3);
            //else reg.SetValue("1200", iTemp);
            //iTemp = this.radioACSafe.SelectedIndex;
            //if (iTemp == 2) reg.SetValue("1201", 3);
            //else reg.SetValue("1201", iTemp);

            //iTemp = this.radioScript.SelectedIndex;
            //if (iTemp == 2) reg.SetValue("1400", 3);
            //else reg.SetValue("1400", iTemp);
            //iTemp = this.radioJS.SelectedIndex;
            //if (iTemp == 2) reg.SetValue("1402", 3);
            //else reg.SetValue("1402", iTemp);
            //iTemp = (int)reg.GetValue("1402");
            //if (iTemp == 3) this.radioJS.SelectedIndex = 2;
            //else this.radioJS.SelectedIndex = iTemp;
            //iTemp = (int)reg.GetValue("2001");
            //if (iTemp == 3) this.radioNet.SelectedIndex = 2;
            //else this.radioNet.SelectedIndex = iTemp;
            //iTemp = (int)reg.GetValue("2004");
            //if (iTemp == 3) this.radioNetNA.SelectedIndex = 2;
            //else this.radioNetNA.SelectedIndex = iTemp;
            //this.label1.Text = "IE参数修改成功";
        }

        private void btnIECancel_Click(object sender, EventArgs e)
        {
            this.IESecurity();
        }

        private void btnPrefsOK_Click(object sender, EventArgs e)
        {
            if (this.txtPrefs.Text == "")
            {
                //this.label1.Text = "prefs.js文件缺失"; 
                return;
            }
            //Process[] p = Process.GetProcessesByName("FireFox Plus");
            //if (p.Length > 0)
            //{
            //    for (int i = 0; i < p.Length; i++) p[i].Kill();
            //}
            FileStream fs = new FileStream(this.txtPrefs.Text, FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(fs);
            string line = sr.ReadLine();
            StringBuilder sb = new StringBuilder();
            bool f1 = false, f2 = false, f3 = false, f4 = false;
            //bool f5=false,f6 = false, f7 = false, f8 = false;
            string strT;
            for (int i = 1; line != null; i++)
            {
                if (line.Length < 10) { sb.Append(line + "\r\n"); line = sr.ReadLine(); continue; }
                if (line.Substring(0, 4) == "user")
                {
                    int iStart = line.IndexOf("\"");
                    int iEnd = line.LastIndexOf("\"");
                    strT = line.Substring(iStart + 1, iEnd - iStart - 1);
                    if (strT == "dom.disable_open_during_load")
                    {
                        if (this.chkWindow.Checked) { sb.Append("\r\n"); f1 = true; line = sr.ReadLine(); continue; }
                    }
                    if (strT == "permissions.default.image")
                    {
                        if (this.chkImage.Checked) { sb.Append("\r\n"); f2 = true; line = sr.ReadLine(); continue; }
                    }
                    if (strT == "javascript.enabled")
                    {
                        if (this.chkJS.Checked) { sb.Append("\r\n"); f3 = true; line = sr.ReadLine(); continue; }
                    }
                    if (strT == "security.enable_java")
                    {
                        if (this.chkJava.Checked) { sb.Append("\r\n"); f4 = true; line = sr.ReadLine(); continue; }
                    }
                    if (strT == "browser.sessionstore.enabled") { sb.Append("\r\n"); line = sr.ReadLine(); continue; }
                    //if (strT == "privacy.sanitize.sanitizeOnShutdown")
                    //{
                    //    sb.Append("user_pref(\"privacy.sanitize.sanitizeOnShutdown\", true);" + "\r\n");
                    //    f5 = true;
                    //    line = sr.ReadLine(); 
                    //    continue; 
                    //}
                    //if (strT == "privacy.item.cookies") { f6 = true; }
                    //if (strT == "privacy.item.offlineApps") { f7 = true; }
                    //if (strT == "privacy.item.passwords") { f8 = true; }
                }
                sb.Append(line + "\r\n");
                line = sr.ReadLine();
            }
            //user_pref("dom.disable_open_during_load", false);
            //user_pref("javascript.enabled", false);
            //user_pref("permissions.default.image", 2);
            //user_pref("security.enable_java", false);
            sb.Append("\r\n");
            if (!f1)
            {
                if (!this.chkWindow.Checked)
                {
                    sb.Append("user_pref(\"dom.disable_open_during_load\", false);" + "\r\n");
                }
            }
            if (!f2)
            {
                if (!this.chkImage.Checked)
                {
                    sb.Append("user_pref(\"permissions.default.image\", 2);" + "\r\n");
                }
            }
            if (!f3)
            {
                if (!this.chkJS.Checked)
                {
                    sb.Append("user_pref(\"javascript.enabled\", false);" + "\r\n");
                }
            }
            if (!f4)
            {
                if (!this.chkJava.Checked)
                {
                    sb.Append("user_pref(\"security.enable_java\", false);" + "\r\n");
                }
            }
            //if (!f5)
            //{
            //    sb.Append("user_pref(\"privacy.sanitize.sanitizeOnShutdown\", true);" + "\r\n");
            //}
            //if (!f6)
            //{
            //    sb.Append("user_pref(\"privacy.item.cookies\", true);" + "\r\n");
            //}
            //if (!f7)
            //{
            //    sb.Append("user_pref(\"privacy.item.offlineApps\", true);" + "\r\n");
            //}
            //if (!f8)
            //{
            //    sb.Append("user_pref(\"privacy.item.passwords\", true);" + "\r\n");
            //} 
            sr.Close();
            fs.Close();
            FileStream fs1 = new FileStream(this.txtPrefs.Text, FileMode.Open, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs1);
            sw.Write(sb.ToString());
            sw.Close();
            //fs.Close();
            fs1.Close();
            //this.label1.Text = "Firefox Plus 参数修改成功";
            sb.Remove(0, sb.Length);
        }

        private void btnPrefsCancel_Click(object sender, EventArgs e)
        {
            this.txtPrefs.Text = inis.IniReadValue("Web", "FirefoxLoc");
            this.txtFirefox.Text = inis.IniReadValue("Web", "FirefoxPlus");
            this.txtFCookie.Text = inis.IniReadValue("Web", "FirefoxCookie");
            if (this.txtPrefs.Text != "")
            {
                this.FirefoxSecurity(this.txtPrefs.Text);
            }
        }

        private void btnGoogleOK_Click(object sender, EventArgs e)
        {
            if (this.txtGoogleCookie.Text != "")
                inis.IniWriteValue("Web", "GoogleCookies", this.txtGoogleCookie.Text);
            //this.label1.Text = "Web测试参数设置——修改成功";
        }

        private void btnGoogleCancel_Click(object sender, EventArgs e)
        {
            this.txtGoogleCookie.Text = inis.IniReadValue("Web", "GoogleCookies");
        }

        private void IESecurity()
        {
            QuickRegistry reg = new QuickRegistry();
            reg.OpenKey("HKEY_CURRENT_USER", @"SOFTWARE\Microsoft\Windows\CurrentVersion\Internet Settings\zones\3", false);
            //int iTemp = (int)reg.GetValue("1001");
            //if (iTemp == 3) this.radioAC.SelectedIndex = 2;
            //else this.radioAC.SelectedIndex = iTemp;
            //iTemp = (int)reg.GetValue("1004");
            //if (iTemp == 3) this.radioACNo.SelectedIndex = 2;
            //else this.radioACNo.SelectedIndex = iTemp;
            //iTemp = (int)reg.GetValue("1200");
            //if (iTemp == 3) this.radioACRun.SelectedIndex = 2;
            //else this.radioACRun.SelectedIndex = iTemp;
            //iTemp = (int)reg.GetValue("1201");
            //if (iTemp == 3) this.radioACSafe.SelectedIndex = 2;
            //else this.radioACSafe.SelectedIndex = iTemp;
            //iTemp = (int)reg.GetValue("1400");
            //if (iTemp == 3) this.radioScript.SelectedIndex = 2;
            //else this.radioScript.SelectedIndex = iTemp;
            //iTemp = (int)reg.GetValue("1402");
            //if (iTemp == 3) this.radioJS.SelectedIndex = 2;
            //else this.radioJS.SelectedIndex = iTemp;
            //iTemp = (int)reg.GetValue("2001");
            //if (iTemp == 3) this.radioNet.SelectedIndex = 2;
            //else this.radioNet.SelectedIndex = iTemp;
            //iTemp = (int)reg.GetValue("2004");
            //if (iTemp == 3) this.radioNetNA.SelectedIndex = 2;
            //else this.radioNetNA.SelectedIndex = iTemp;
        }

        private void FirefoxSecurity(string strFileName)
        {
            //user_pref("dom.disable_open_during_load", false);
            //user_pref("javascript.enabled", false);
            //user_pref("permissions.default.image", 2);
            //user_pref("security.enable_java", false);
            JParser jp = new JParser();
            StreamReader theReader;
            string JSource = "";
            try
            {
                theReader = new StreamReader(strFileName);
                JSource = theReader.ReadToEnd();
                JSource = jp.reQuoted.Replace(JSource, jp.ReplaceSingleQuote);
                theReader.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }


            // Single quotes..


            // Break into lines..
            string[] jslines = JSource.Split("\n"[0]);

            //bool comment = false;
            bool flgchkwindow = true;
            bool flgchkImage = true;
            bool flgchkJS = true;
            bool flgchkJava = true;
            //bool flgchksession = true;
            for (int i = 0; i < jslines.Length; i++)
            {

                string strLineNow = jslines[i];
                if (strLineNow.Length < 10) continue;
                if (strLineNow.Substring(0, 4) == "user")
                {
                    int iStart = strLineNow.IndexOf("\"");
                    int iEnd = strLineNow.LastIndexOf("\"");
                    string strT = strLineNow.Substring(iStart + 1, iEnd - iStart - 1);

                    if (strT == "dom.disable_open_during_load") { this.chkWindow.Checked = false; flgchkwindow = false; }
                    if (strT == "permissions.default.image") { this.chkImage.Checked = false; flgchkImage = false; }
                    if (strT == "javascript.enabled") { this.chkJS.Checked = false; flgchkJS = false; }
                    if (strT == "security.enable_java") { this.chkJava.Checked = false; flgchkJava = false; }
                    //if (strT == "browser.sessionstore.enabled ") { flgchksession = false; }
                    if (flgchkwindow) this.chkWindow.Checked = true;
                    if (flgchkImage) this.chkImage.Checked = true;
                    if (flgchkJS) this.chkJS.Checked = true;
                    if (flgchkJava) this.chkJava.Checked = true;
                }


            }
        }

        private void chkContinue_CheckedChanged(object sender, EventArgs e)
        {
            if (this.chkContinue.Checked == true)
            {
                this.txtContinueNo.Enabled = true;
                this.chkLoop.Enabled = true;
            }
            else
            {
                this.txtContinueNo.Enabled = false;
                this.chkLoop.Enabled = false;
            }
        }

        private void btnPath_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbDlg = new FolderBrowserDialog();  //open a folder browser box
            if (fbDlg.ShowDialog() == DialogResult.OK)
            {
                this.txtPath.Text = fbDlg.SelectedPath;
            }
        }

        private void btnFCookie_Click(object sender, EventArgs e)
        {
            OpenFileDialog OpenSqliteDialog = new OpenFileDialog();
            OpenSqliteDialog.Filter = "数据文件(*.sqlite)|*.sqlite";
            OpenSqliteDialog.Multiselect = false;
            DialogResult result = OpenSqliteDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                this.txtFirefox.Text = OpenSqliteDialog.FileName;
                inis.IniWriteValue("Web", "FirefoxCookie", this.txtFCookie.Text);
                //this.FirefoxSecurity(this.txtPrefs.Text);
            }
        }

        private void btnFirefox_Click(object sender, EventArgs e)
        {
            OpenFileDialog OpenJScriptDialog = new OpenFileDialog();
            OpenJScriptDialog.Filter = "程序文件(*.exe)|*.exe";
            OpenJScriptDialog.Multiselect = false;
            DialogResult result = OpenJScriptDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                this.txtFirefox.Text = OpenJScriptDialog.FileName;
                inis.IniWriteValue("Web", "FirefoxPlus", this.txtFirefox.Text);
                //this.FirefoxSecurity(this.txtPrefs.Text);
            }
        }

        private void btnPrefs_Click(object sender, EventArgs e)
        {
            OpenFileDialog OpenJScriptDialog = new OpenFileDialog();
            OpenJScriptDialog.Filter = "脚本文件(*.js)|*.js";
            OpenJScriptDialog.Multiselect = false;
            DialogResult result = OpenJScriptDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                this.txtPrefs.Text = OpenJScriptDialog.FileName;
                inis.IniWriteValue("Web", "FirefoxLoc", this.txtPrefs.Text);
                this.FirefoxSecurity(this.txtPrefs.Text);
            }
        }

        private void btnGoogleCookie_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbDlg = new FolderBrowserDialog();  //open a folder browser box
            if (fbDlg.ShowDialog() == DialogResult.OK)
            {
                this.txtGoogleCookie.Text = fbDlg.SelectedPath;
            }
        }

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            int i;
            if (this.iIndexTemp > 0)
            {
                inis.IniWriteValue("Web", "web" + iIndexTemp.ToString(), this.cbSelWeb.Text);
                i = iIndexTemp;
            }
            else
            {
                inis.IniWriteValue("Web", "web" + inis.IniReadValue("Web", "WebIndex"), this.cbSelWeb.Text);
                inis.IniWriteValue("Web", "webPage", this.cbSelWeb.Text);
                i = int.Parse(inis.IniReadValue("Web", "WebIndex"));
            }
            this.cbSelWeb.Items.Clear();
            if (inis.IniReadValue("Web", "web1") != "") this.cbSelWeb.Items.Add(inis.IniReadValue("Web", "web1"));
            if (inis.IniReadValue("Web", "web2") != "") this.cbSelWeb.Items.Add(inis.IniReadValue("Web", "web2"));
            if (inis.IniReadValue("Web", "web3") != "") this.cbSelWeb.Items.Add(inis.IniReadValue("Web", "web3"));
            if (inis.IniReadValue("Web", "web4") != "") this.cbSelWeb.Items.Add(inis.IniReadValue("Web", "web4"));
            if (inis.IniReadValue("Web", "web5") != "") this.cbSelWeb.Items.Add(inis.IniReadValue("Web", "web5"));
            MessageBox.Show("目的网址" + i + "已更新");

        }

        private void cbSelWeb_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.iIndexTemp = this.cbSelWeb.SelectedIndex + 1;
        }

        private void tabWebSet_SelectedPageChanged(object sender, DevExpress.XtraTab.TabPageChangedEventArgs e)
        {
            if (this.tabWebSet.SelectedTabPageIndex == 1)
                this.IESecurity();
            if (this.tabWebSet.SelectedTabPageIndex == 2)
            {
                if (this.txtPrefs.Text != "")
                {
                    this.FirefoxSecurity(this.txtPrefs.Text);
                }
            }
            if (this.tabWebSet.SelectedTabPageIndex == 3)
                this.txtGoogleCookie.Text = inis.IniReadValue("Web", "GoogleCookies");
        }

        private void searchAdapter()
        {
            System.Version ver = System.Environment.OSVersion.Version;
            var devices = LibPcapLiveDeviceList.Instance;
            cbAdapter.Items.Clear();
            int idev = 1;
            if (ver.Major == 5 && ver.Minor == 1)     //XP系统时ip地址获取
            {
                inis.IniWriteValue("Flv", "SystemInfo", "XP");
                for (int i = 0; i < devices.Count; i++)
                {
                    /* Description */
                    string strNetName = devices[i].Description;
                    if (devices[i].Addresses.Count != 0 && devices[i].Opened)
                    {
                        if ((strNetName.Contains("PPP")) || (strNetName.Contains("SLIP")) || (strNetName.Contains("ppp")))
                            cbAdapter.Items.Add("TD-CDMA网卡" + "--ip:" + devices[i].Addresses[0].Addr.ipAddress);
                        else
                        {
                            if ((strNetName.Contains("Wireless")) || (strNetName.Contains("wireless")))
                            {
                                if (devices[i].Addresses[0].Addr.ipAddress != null
                                    && !devices[i].Addresses[0].Addr.ipAddress.ToString().Contains("0.0.0.0"))
                                    cbAdapter.Items.Add("无线网卡" + "--ip:" + devices[i].Addresses[0].Addr.ipAddress);
                                else
                                    cbAdapter.Items.Add("无线网卡");
                            }
                            else
                            {
                                if (strNetName.Contains("VPN"))
                                    cbAdapter.Items.Add("VPN");
                                else
                                {
                                    if (devices[i].Addresses[0].Addr.ipAddress != null
                                        && !devices[i].Addresses[0].Addr.ipAddress.ToString().Contains("0.0.0.0"))
                                        cbAdapter.Items.Add("网卡" + idev + "--ip:" + devices[i].Addresses[0].Addr.ipAddress);
                                    else
                                        cbAdapter.Items.Add("网卡" + idev + "--ip:" + devices[i].Addresses[0].Addr.ipAddress);
                                    idev++;
                                }
                            }
                        }
                    }
                }
            }

            if (ver.Major == 6 && ver.Minor == 1)     //Win7系统时ip地址获取
            {
                inis.IniWriteValue("Flv", "SystemInfo", "Win7");
                for (int i = 0; i < devices.Count; i++)
                {
                    /* Description */
                    string strNetName = devices[i].Description;
                    if (devices[i].Addresses.Count != 0 && devices[i].Opened)
                    {
                        if ((strNetName.Contains("PPP")) || (strNetName.Contains("SLIP")) || (strNetName.Contains("ppp")))
                            cbAdapter.Items.Add("TD-CDMA网卡" + "--ip:" + devices[i].Addresses[1].Addr.ipAddress);
                        else
                        {
                            if ((strNetName.Contains("Wireless")) || (strNetName.Contains("wireless")))
                            {
                                if (devices[i].Addresses[1].Addr.ipAddress != null
                                    && !devices[i].Addresses[1].Addr.ipAddress.ToString().Contains("0.0.0.0"))
                                    cbAdapter.Items.Add("无线网卡" + "--ip:" + devices[i].Addresses[1].Addr.ipAddress);
                                else
                                    cbAdapter.Items.Add("无线网卡");
                            }
                            else
                            {
                                if (strNetName.Contains("VPN"))
                                    cbAdapter.Items.Add("VPN");
                                else
                                {
                                    if (devices[i].Addresses[1].Addr.ipAddress != null
                                        && !devices[i].Addresses[1].Addr.ipAddress.ToString().Contains("0.0.0.0"))
                                        cbAdapter.Items.Add("网卡" + idev + "--ip:" + devices[i].Addresses[1].Addr.ipAddress);
                                    else
                                        cbAdapter.Items.Add("网卡" + idev + "--ip:" + devices[i].Addresses[0].Addr.ipAddress);
                                    idev++;
                                }
                            }
                        }
                    }

                }
            }


           


        }


        private void btnSearchWebAdapter_Click(object sender, EventArgs e)
        {
            searchAdapter();
        }
    }
}
