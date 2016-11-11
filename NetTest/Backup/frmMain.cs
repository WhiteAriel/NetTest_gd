using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Management;
using Tamir.IPLib;
using System.Net.Mail;
using System.IO;
using System.Diagnostics;
using System.Threading;
namespace NetTest
{
    public partial class frmMain : Form
    {
        IniFile inis = new IniFile(Application.StartupPath + "\\settings.ini");
        IniFile indll = new IniFile(Application.StartupPath + "\\net.dll");
        public frmMain()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            //PcapDeviceList devices = SharpPcap.GetAllDevices();
            //int idev = 1;
            //if(devices.Count==0)
            //{
            //    MessageBox.Show("未发现有效网卡,程序退出！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    Application.Exit();
            //}
            string strPcap=System.Environment.GetFolderPath(Environment.SpecialFolder.System) + "\\wpcap.dll";
            //QuickRegistry reg = new QuickRegistry();
            //reg.OpenKey("HKEY_LOCAL_MACHINE", @"software/Microsoft/Windows NT/CurrentVersion", false);
            //string strPcap = reg.GetValue("SystemRoot").ToString();
            //strPcap += "\\system32\\wpcap.dll";
            if (!File.Exists(strPcap))
            {
                MessageBox.Show("运行本程序前请先安装WinPcap！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Application.Exit();
            }
            string cpuInfo = "";
            ManagementClass cimobject = new ManagementClass("Win32_Processor");
            ManagementObjectCollection moc = cimobject.GetInstances();
            foreach (ManagementObject mo in moc)
            {
                cpuInfo = mo.Properties["ProcessorId"].Value.ToString();
            }
            string strPC1 = indll.IniReadValue("ID", "new1");
            string strPC2 = indll.IniReadValue("ID", "new2");
            string strPC3 = indll.IniReadValue("ID", "new3");
            if ((strPC1 == "") || (strPC1 == cpuInfo))
            {
                indll.IniWriteValue("ID", "new1", cpuInfo);
            }
            else
            {
                if ((strPC2 == "") || (strPC2 == cpuInfo)) { indll.IniWriteValue("ID", "new2", cpuInfo); }
                else
                {
                    if ((strPC3 == "") || (strPC3 == cpuInfo)) { indll.IniWriteValue("ID", "new3", cpuInfo); }
                    else
                    {
                        //if ((strPC1 != cpuInfo) && (strPC2 != cpuInfo) && (strPC3 != cpuInfo))
                        //{
                        MessageBox.Show("超过使用范围,程序退出");
                        this.Dispose();
                        //}
                    }
                }
            }
            
            PcapDeviceList devices = SharpPcap.GetAllDevices();
            if (devices.Count < 1)
            {
                MessageBox.Show("未发现有效网卡,程序退出！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Application.Exit();
            }
           
            //int iTemp = 0;
            this.webSettings1.Visible = false;
            this.webTest1.Visible = false;
            this.flvTest1.Visible = false;
            this.flvSetting1.Visible = false;
            this.ftpTest1.Visible = false;
            this.ftpSettings1.Visible = false;
            this.mailSet1.Visible = false;
            this.mailTest1.Visible = false;

            Process[] p = Process.GetProcessesByName("NetaTest.exe");
            if (p.Length > 0)
            {
                for (int i = 0; i < p.Length; i++)
                {
                    p[i].CloseMainWindow();
                    p[i].Kill();
                }
            }

            if (inis.IniReadValue("All", "ShowTips") != "0")
            {
                frmTips tips = new frmTips();
                tips.ShowDialog();
            }
            
        }

        private void navBarWeb_LinkClicked(object sender, DevExpress.XtraNavBar.NavBarLinkEventArgs e)
        {
            Thread.Sleep(500);
            this.webTest1.Visible = true;
            this.webTest1.Dock = DockStyle.Fill;
            this.webSettings1.Visible = false;
            this.flvSetting1.Visible = false;
            this.flvTest1.Visible = false;
            this.ftpTest1.Visible = false;
            this.ftpSettings1.Visible = false;
            this.mailSet1.Visible = false;
            this.mailTest1.Visible = false;
            this.webTest1.Init();
            this.lbText.Text = "Web测试...";
        }

        private void navBarWebSet_LinkClicked(object sender, DevExpress.XtraNavBar.NavBarLinkEventArgs e)
        {
            Thread.Sleep(500);
            this.webSettings1.Visible = true;
            this.webSettings1.Dock = DockStyle.Fill;
            this.webTest1.Visible = false;
            this.flvSetting1.Visible = false;
            this.flvTest1.Visible = false;
            this.ftpTest1.Visible = false;
            this.ftpSettings1.Visible = false;
            this.mailSet1.Visible = false;
            this.mailTest1.Visible = false;
            this.webSettings1.Init();
            this.lbText.Text = "Web设置...";
        }

        private void navBarFTP_LinkClicked(object sender, DevExpress.XtraNavBar.NavBarLinkEventArgs e)
        {
            Thread.Sleep(500);
            this.ftpTest1.Visible = true;
            this.ftpTest1.Dock = DockStyle.Fill;
            this.webSettings1.Visible = false;
            this.webTest1.Visible = false;
            this.flvSetting1.Visible = false;
            this.flvTest1.Visible = false;
            this.ftpSettings1.Visible = false;
            this.mailSet1.Visible = false;
            this.mailTest1.Visible = false;
            this.lbText.Text = "FTP测试...";
        }

        private void navBarFTPSet_LinkClicked(object sender, DevExpress.XtraNavBar.NavBarLinkEventArgs e)
        {
            Thread.Sleep(500);
            this.ftpSettings1.Visible = true;
            this.ftpSettings1.Dock = DockStyle.Fill;
            this.webSettings1.Visible = false;
            this.flvSetting1.Visible = false;
            this.flvTest1.Visible = false;
            this.ftpTest1.Visible = false;
            this.webTest1.Visible = false;
            this.mailSet1.Visible = false;
            this.mailTest1.Visible = false;
            this.ftpSettings1.Init();
            this.lbText.Text = "FTP设置...";
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            //this.ftpSettings1.Dispose();
            //this.ftp1.Dispose();
            //this.webTest1.Dispose();
            //this.webSettings1.Dispose();
        }

        private void navBarMailSet_LinkClicked(object sender, DevExpress.XtraNavBar.NavBarLinkEventArgs e)
        {
            Thread.Sleep(500);
            this.webSettings1.Visible = false;
            this.webTest1.Visible = false;
            this.ftpTest1.Visible = false;
            this.ftpSettings1.Visible = false;
            this.flvSetting1.Visible = false;
            this.flvTest1.Visible = false;
            this.mailTest1.Visible = false;
            this.mailSet1.Visible = true;
            this.mailSet1.Dock = DockStyle.Fill;
            this.mailSet1.Init();
            this.lbText.Text = "E-Mail设置...";
        }

        private void navBarMail_LinkClicked(object sender, DevExpress.XtraNavBar.NavBarLinkEventArgs e)
        {
            Thread.Sleep(500);
            this.webSettings1.Visible = false;
            this.webTest1.Visible = false;
            this.ftpTest1.Visible = false;
            this.ftpSettings1.Visible = false;
            this.flvSetting1.Visible = false;
            this.flvTest1.Visible = false;
            this.mailSet1.Visible = false;
            this.mailTest1.Visible = true;
            this.mailTest1.Dock = DockStyle.Fill;
            this.lbText.Text = "E-Mail测试...";
            
        }

        private void navBarFlv_LinkClicked(object sender, DevExpress.XtraNavBar.NavBarLinkEventArgs e)
        {
            Thread.Sleep(500);
            this.flvTest1.Visible = true;
            this.flvTest1.Dock = DockStyle.Fill;
            this.flvSetting1.Visible = false;
            this.webTest1.Visible = false;
            this.webSettings1.Visible = false;
            this.ftpTest1.Visible = false;
            this.ftpSettings1.Visible = false;
            this.mailSet1.Visible = false;
            this.mailTest1.Visible = false;
            this.flvTest1.Init();
            this.lbText.Text = "流媒体测试...";
        }

        private void navBarFlvSet_LinkClicked(object sender, DevExpress.XtraNavBar.NavBarLinkEventArgs e)
        {
            Thread.Sleep(500);
            this.flvSetting1.Visible = true;
            this.flvSetting1.Dock = DockStyle.Fill;
            this.flvTest1.Visible = false;
            this.webTest1.Visible = false;
            this.webSettings1.Visible = false;
            this.ftpTest1.Visible = false;
            this.ftpSettings1.Visible = false;
            this.mailSet1.Visible = false;
            this.mailTest1.Visible = false;
            this.flvSetting1.Init();
            this.lbText.Text = "流媒体设置...";
        }

        private void barBtnAbout_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            frmAbout abt = new frmAbout();
            abt.ShowDialog();
        }

        private void barButtonItem1_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            frmTips tips = new frmTips();
            tips.ShowDialog();
        }

        private void barBtnHelp_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            System.Diagnostics.Process.Start("readme.doc");
        }

        private void barButtonItem2_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            frmFTPShow tip = new frmFTPShow();
            tip.ShowDialog();
        }






    }
}