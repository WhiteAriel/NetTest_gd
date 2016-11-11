/******************************************************************************
	Copyright 2009-2010 Liu Wei 
/*******************************************************************************/

/*
*Name:			WebTest
*Author:		Liu Wei
*Created:		2009/8
*Last Modified:	2010/5/25 
*Description:
*/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using Tamir.IPLib;
using Tamir.IPLib.Packets;
using System.Threading;
using System.IO;
using System.Management;
using System.Collections;
using System.Diagnostics;
using Finisar.SQLite;
using System.Runtime.InteropServices;
namespace NetTest
{
    public partial class WebTest : DevExpress.XtraEditors.XtraUserControl
    {

        IniFile inis = new IniFile(Application.StartupPath + "\\settings.ini");  //ini class
        CacheClear cc = new CacheClear();   //ie cookies class
        private int iTest = 0;              //loop count
        private int intIndex;

        public bool flagAdapter = false;

        //stop perf
        public string strBro;
        public long iProMem = 0;
        public long iProLength = 0;
        public int iProCPU = 3;

        public string strFile = "";
        public int iDevice;
        public PcapDevice device;
        private DateTime dtStart;                  //start time
        private int iStable;
        private StringBuilder strbFile = new StringBuilder();    //contents of log
        private string strLogFile;                    //log file
        private int intCheckContinuous;
        private int iTimeThrehold = 0;
        private int iTimeLast = 0;
        private int iBool = 0;
        private int iNumContinuous = 0;
        private bool DoTest;
        private string strTempURL;        //random test
        private string DB_NAME = null;

        private BackgroundWorker m_AsyncWorker = new BackgroundWorker();
        //call pktanalyser.dll: performance compute
        [DllImport("pktanalyser")]
        public static extern bool testmain(string CapFile, int a, int b, int c, int d);
        [DllImport("pktanalyser")]
        public static extern int GetCounts();
        [DllImport("pktanalyser")]
        public static extern int GetLossnum();
        [DllImport("pktanalyser")]
        public static extern int GetDupacknum();
        [DllImport("pktanalyser")]
        public static extern int GetRetransmitnum();
        [DllImport("pktanalyser")]
        public static extern int GetMisordernum();

        /******************************************************************************
           init the user components WebTest 
        /*******************************************************************************/
        public WebTest()
        {
            InitializeComponent();
            m_AsyncWorker.WorkerSupportsCancellation = true;
            //m_AsyncWorker.ProgressChanged += new ProgressChangedEventHandler(bwAsync_ProgressChanged);
            m_AsyncWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bwAsync_RunWorkerCompleted);
            m_AsyncWorker.DoWork += new DoWorkEventHandler(bwAsync_DoWork);

            Control.CheckForIllegalCrossThreadCalls = false;

        }

        /******************************************************************************
           init the paras or settings 
        /*******************************************************************************/
        public void Init()
        {
            try
            {
                string a = inis.IniReadValue("Web", "CheckContinuous");
                intCheckContinuous = int.Parse(a);
            }
            catch { MessageBox.Show("�����������Բ��������ã�"); }
            string ite = inis.IniReadValue("Web", "Adapter");
            iDevice = int.Parse(ite);
            ite = inis.IniReadValue("Web", "TimeLast");
            iTimeLast = int.Parse(ite);
            ite = inis.IniReadValue("Web", "NumContinuous");
            iNumContinuous = Convert.ToInt16(ite);
            ite = inis.IniReadValue("Web", "CheckCookies");
            iBool = Convert.ToInt16(ite);
            strBro = inis.IniReadValue("Web", "Browser");
            strTempURL = inis.IniReadValue("Web", "WebPage");
        }

        /******************************************************************************
           interrupt the test whether loop or not 
        /*******************************************************************************/
        private void btnWebStop_Click(object sender, EventArgs e)
        {
            this.sBTest.Enabled = true;
            device.PcapStopCapture();
            this.memoPcap.Items.Clear();
            this.memoPcap.Items.Add("���Ա��û��ж�\n");
            if (DoTest)
            {
                strbFile.Append("���Ա��û��ж�\n");
                if (!File.Exists(this.strLogFile))
                {
                    FileStream fs1 = new FileStream(this.strLogFile, FileMode.CreateNew, FileAccess.Write);
                    StreamWriter sw = new StreamWriter(fs1);
                    sw.Write(this.strbFile.ToString());
                    sw.Close();
                    fs1.Close();
                }
                else
                {
                    FileStream fs1 = new FileStream(this.strLogFile, FileMode.Append, FileAccess.Write);
                    StreamWriter sw = new StreamWriter(fs1);
                    sw.Write(this.strbFile.ToString());
                    sw.Close();
                    fs1.Close();
                }
                this.memoPcap.Items.Add("ץ���ļ�: " + strFile + " ����");

                this.memoPcap.Items.Add("��־�ļ�: " + strLogFile);
            }
            iTest = 0;
            this.timWeb.Stop();
            this.timer1.Stop();
            this.DoTest = false;
        }

        /******************************************************************************
           start the test single or loop, the main process call WebTesting() 
        /*******************************************************************************/
        private void sBTest_Click(object sender, EventArgs e)
        {
            iTimeThrehold = 0;
            if (strbFile.Length > 0) strbFile.Remove(0, strbFile.Length);

            if (intCheckContinuous == 0) this.iTest = 0;
            if (iTest == 0) intIndex = int.Parse(inis.IniReadValue("Web", "WebIndex"));
            iProMem = 0;
            this.sBTest.Enabled = false;
            this.timer1.Enabled = false;

            this.btnWebStop.Enabled = true;

            strFile = inis.IniReadValue("Web", "Path") + "\\Web-" + inis.IniReadValue("Web", "Browser") + "-"
                + inis.IniReadValue("Web", "WebPage") + "-" + DateTime.Now.Year.ToString() + "-"
                + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "-"
                + DateTime.Now.Hour.ToString() + "-" + DateTime.Now.Minute.ToString() + "-"
                + DateTime.Now.Second.ToString();// +".cap";
            strLogFile = strFile + ".xls";
            strFile = strFile + ".cap";

            if (!Directory.Exists(inis.IniReadValue("Web", "Path"))) Directory.CreateDirectory(inis.IniReadValue("Web", "Path"));

            this.WebTesting();
        }

        /******************************************************************************
           the test itself
        /*******************************************************************************/
        private void WebTesting()
        {
            this.DoTest = true;
            this.iTest++;
            if (iTest == 1) this.memoPcap.Items.Clear();
            if (iTest > 1)
            {
                this.memoPcap.Items.Add("--------------------------------");

            }



            this.memoPcap.Items.Add("�� " + iTest + " �β���......");
            strbFile.Append("�� " + iTest + " �β���......\r\n");

            try
            {
                this.CloseBrowser();
                if (iBool > 0)
                {
                    if (strBro == "Google")
                    {
                        Thread.Sleep(400);
                        this.CleanFiles(inis.IniReadValue("Web", "GoogleCookies"));
                        Thread.Sleep(400);
                        this.memoPcap.Items.Add(" cookies, caches ɾ���������...");
                        strbFile.Append(" cookies, caches ɾ���������...");

                    }
                    if (strBro == "Firefox Plus")
                    {
                        if (!File.Exists(inis.IniReadValue("Web", "FirefoxPlus")))
                        {
                            this.memoPcap.Items.Add("�����жϣ��޷���Firefox");
                            if (strbFile.Length > 0) strbFile.Remove(0, strbFile.Length);
                            return;
                        }

                        string str = this.ClearFirefoxCookies();
                        if (str == "Cookies�ɹ�ɾ��...")
                        {
                            this.memoPcap.Items.Add(" cookies, caches ɾ���������...");
                            strbFile.Append(" cookies, caches ɾ���������...\r\n");

                        }
                        else
                        {
                            this.memoPcap.Items.Add(str);
                            strbFile.Append(str + "\r\n");
                        }
                    }
                    if (strBro == "IE Explorer")
                    {
                        int iResultCookies = cc.ClearAllCookies(string.Empty);
                        int iResultCaches = cc.ClearAllCache(string.Empty);
                        this.memoPcap.Items.Add(iResultCookies + " cookies, " + iResultCaches + " caches ɾ��...");
                        strbFile.Append(iResultCookies + " cookies, " + iResultCaches + " caches ɾ��...\r\n");

                    }
                    this.ClearDns();


                }
                else
                {
                    this.memoPcap.Items.Add("Cookies/caches δ��ȫɾ��...");
                    strbFile.Append("Cookies/caches δ��ȫɾ��...\r\n");

                }
            }
            catch (Exception ex)
            {

                this.memoPcap.Items.Add(ex.Message);
                this.memoPcap.Items.Add("Cookies/caches δ��ȫɾ��...");
                strbFile.Append("Cookies/caches δ��ȫɾ��...\r\n");

            }



            PcapDeviceList devices = SharpPcap.GetAllDevices();

            device = devices[iDevice];
            string ip = device.PcapIpAddress;
            this.memoPcap.Items.Add("���������ر�");
            this.memoPcap.Items.Add("����: " + device.PcapDescription);
            strbFile.Append("����: " + device.PcapDescription + "\r\n");

            this.memoPcap.Items.Add("IP��ַ: " + device.PcapIpAddress);
            strbFile.Append("IP��ַ: " + device.PcapIpAddress + "\r\n");

            Thread.Sleep(100);
            this.dtStart = DateTime.Now;
            this.memoPcap.Items.Add("���Կ�ʼʱ��: " + dtStart.ToString());
            strbFile.Append("���Կ�ʼʱ��: " + dtStart.ToString() + "\r\n");

            //Register our handler function to the 'packet arrival' event
            device.PcapOnPacketArrival +=
                new SharpPcap.PacketArrivalEvent(device_PcapOnPacketArrival);

            //Open the device for capturing
            //true -- means promiscuous mode
            //1000 -- means a read wait of 1000ms

            device.PcapOpen(true, 100);
            device.PcapSetFilter("(tcp or udp) and host " + ip);
            device.PcapDumpOpen(strFile);
            if (!m_AsyncWorker.IsBusy)
            {
                m_AsyncWorker.RunWorkerAsync();
            }
            this.timWeb.Enabled = true;

        }

        /******************************************************************************
           trigger for complete a test and judge a loop 
        /*******************************************************************************/
        private void timWeb_Tick(object sender, EventArgs e)
        {
            iTimeThrehold++;
            if (iTimeThrehold * 3 <= iTimeLast)
            {
                FileInfo f = new FileInfo(strFile);
                long iLengthTemp = f.Length / 1024;
                if (iLengthTemp < 700) return;
                long iLengthInc = iLengthTemp - iProLength;
                iProLength = iLengthTemp;
                if (iLengthInc > 100)
                {
                    long iMemTemp = 0;
                    int iCPUSec = 100;
                    Process[] p = Process.GetProcessesByName(strBro);
                    if (p.Length > 0)
                    {
                        iMemTemp = p[0].WorkingSet64 / 1024;// p[0].VirtualMemorySize64/1024;
                        iCPUSec = p[0].TotalProcessorTime.Milliseconds;
                    }
                    else return;
                    long iMemInc = iMemTemp - iProMem;
                    iProMem = iMemTemp;
                    Application.DoEvents();
                    if ((strBro == "IE Explorer") || ((strBro == "Google")))
                    {
                        if (iCPUSec > 500) return;
                        if (iMemInc > 50) return;
                    }
                    else
                    {
                        if (iCPUSec > 400) return;
                        if (iMemInc > 20) return;
                        iStable++;
                        if (iStable < 2) return;
                    }
                }
            }
            device.PcapStopCapture();


            DateTime dtEnd = DateTime.Now;
            TimeSpan ts = dtEnd - dtStart;
            float ts2 = ts.Seconds + (float)ts.Milliseconds / 1000;


            this.memoPcap.Items.Add("���Խ���,��ʱ " + ts.Minutes + "�� " + ts2.ToString() + "��");
            strbFile.Append("���Խ���,��ʱ " + ts.Minutes + "�� " + ts2.ToString() + "��" + "\r\n");

            strbFile.Append("ץ���ļ�: " + strFile + "\r\n");

            this.DoTest = false;
            this.performance(ts2);


            if (!File.Exists(this.strLogFile))
            {
                FileStream fs1 = new FileStream(this.strLogFile, FileMode.CreateNew, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs1, Encoding.Default);
                sw.Write(this.strbFile.ToString());
                sw.Close();
                fs1.Close();
            }
            else
            {
                FileStream fs1 = new FileStream(this.strLogFile, FileMode.Append, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs1, Encoding.Default);
                sw.Write(this.strbFile.ToString());
                sw.Close();
                fs1.Close();
            }
            this.memoPcap.Items.Add("ץ���ļ�: " + strFile + " ����");

            this.memoPcap.Items.Add("��־�ļ�: " + strLogFile);

            if (intCheckContinuous > 0)
            {
                if (iTest < iNumContinuous)
                {
                    if (inis.IniReadValue("Web", "EnableLoop") == "1")
                    {

                        while (true)
                        {
                            intIndex++;
                            if (intIndex > 5) intIndex -= 5;
                            if (inis.IniReadValue("Web", "web" + intIndex.ToString()) != "")
                            {
                                inis.IniWriteValue("Web", "WebPage", inis.IniReadValue("Web", "web" + intIndex.ToString()));
                                break;
                            }
                        }
                    }
                    this.timer1.Interval = Convert.ToInt32(inis.IniReadValue("Web", "Interval")) * 1000;
                    this.timer1.Enabled = true;
                    this.timer1.Start();
                }
                else
                {
                    this.timer1.Enabled = false;
                    this.timWeb.Enabled = false;
                    this.sBTest.Enabled = true;
                    this.btnWebStop.Enabled = false;
                    this.iTest = 0;
                    this.memoPcap.Items.Add("---------------�������---------------");
                    inis.IniWriteValue("Web", "WebPage", strTempURL);
                }
            }
            else { this.sBTest.Enabled = true; this.btnWebStop.Enabled = false; iTest = 0; }
            this.timWeb.Stop();
            this.timWeb.Enabled = false;

        }

        /******************************************************************************
           compute the performance;
           ts2: timespan the test lasts
        /*******************************************************************************/
        private void performance(float ts2)
        {
            string ip = device.PcapIpAddress;
            string temp = ip.Substring(0, ip.IndexOf("."));
            int a = int.Parse(temp);
            ip = ip.Substring(temp.Length + 1);
            temp = ip.Substring(0, ip.IndexOf("."));
            int b = int.Parse(temp);
            ip = ip.Substring(temp.Length + 1);
            temp = ip.Substring(0, ip.IndexOf("."));
            int c = int.Parse(temp);
            ip = ip.Substring(temp.Length + 1);
            int d = int.Parse(ip);

            bool opencap = testmain(strFile, a, b, c, d);//OpenCap(strFile);OpenCap(strFile);////
            if (opencap)
            {
                int count = GetCounts();
                int lossnum = GetLossnum();
                int dupacknum = GetDupacknum();
                int misordernum = GetMisordernum();
                int retransmitnum = GetRetransmitnum();
                if (count > 0)
                {
                    float loss = (float)lossnum * 100 / count;
                    float retrans = (float)retransmitnum * 100 / count;
                    float mis = (float)misordernum * 100 / count;
                    string perf1, perf2, perf3;
                    if (loss < 1.8) perf1 = "��  ��";
                    else
                    {
                        if (loss < 2.1) perf1 = "һ  ��";
                        else perf1 = "��  ��";
                    }
                    if (retrans < 2) perf2 = "��  ��";
                    else
                    {
                        if (retrans < 2.2) perf2 = "һ  ��";
                        else perf2 = "��  ��";
                    }
                    if (mis < 1.9) perf3 = "��  ��";
                    else
                    {
                        if (mis < 2.2) perf3 = "һ  ��";
                        else perf3 = "��  ��";
                    }
                    strbFile.Append("--------------------------------------\r\n");
                    strbFile.Append("|  �� �� ָ ��  |  ��  ֵ  |  ��  ��  |\r\n");
                    strbFile.Append("--------------------------------------\r\n");
                    strbFile.Append("|  ҵ����ʱ(��) |  " + ts2.ToString("F2") + "   |          |\r\n");
                    strbFile.Append("--------------------------------------\r\n");
                    strbFile.Append("|  �� �� ��(%)  |  " + loss.ToString("F2") + "  |" + perf1 + "  |\r\n");
                    strbFile.Append("--------------------------------------\r\n");
                    strbFile.Append("|  �� �� ��(%)  |  " + retrans.ToString("F2") + "  |" + perf2 + "  |\r\n");
                    strbFile.Append("--------------------------------------\r\n");
                    strbFile.Append("|  ʧ �� ��(%)  |  " + mis.ToString("F2") + "  |" + perf3 + "  |\r\n");
                    strbFile.Append("--------------------------------------\r\n");

                }
                else
                {
                    strbFile.Append("�ܰ�����ʧ...\r\n");

                }

            }
            else
            {
                strbFile.Append("��ָ�����ʧ��...\r\n");

            }
        }

        /******************************************************************************
           worker
        /*******************************************************************************/
        private void bwAsync_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                string strProcessFile = strBro;
                if (strBro == "IE Explorer") strProcessFile = "iexplore";
                if (strBro == "Google") strProcessFile = "chrome";
                if (strBro == "Firefox Plus") strProcessFile = inis.IniReadValue("Web", "FirefoxPlus");// "Firefox";
                //this.memoPcap.Items.Add(strProcessFile);
                Process ps = new Process();
                string testURL = inis.IniReadValue("Web", "WebPage");
                if (strBro == "Firefox Plus") ps.StartInfo.FileName = strProcessFile;
                else ps.StartInfo.FileName = strProcessFile + ".exe";
                ps.StartInfo.Arguments = testURL;
                this.memoPcap.Items.Add("�����: " + strBro);
                strbFile.Append("�����: " + strBro + "\r\n");

                this.memoPcap.Items.Add("ҳ��: " + testURL);
                strbFile.Append("ҳ��: " + testURL + "\r\n");

                ps.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
                ps.Start();

            }
            catch (Exception ex)
            {
                this.memoPcap.Items.Add(ex.Message);
                this.sBTest.Enabled = true;
                this.btnWebStop.Enabled = false;
                return;
            }

            device.PcapStartCapture();
        }

        /******************************************************************************
           worker instance complete
        /*******************************************************************************/
        private void bwAsync_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.DoTest = false;
            if (e.Error != null)
            {
                this.memoPcap.Items.Add("���������");

                return;
            }

            // Check to see if the background process was cancelled.
            if (e.Cancelled)
            {
                this.memoPcap.Items.Add("������");

                return;
            }


        }

        /******************************************************************************
           override the PCap packet receive
        /*******************************************************************************/
        private static void device_PcapOnPacketArrival(object sender, Packet packet)
        {
            PcapDevice device = (PcapDevice)sender;
            //if device has a dump file opened
            if (device.PcapDumpOpened)
            {
                //dump the packet to the file
                device.PcapDump(packet);
                //this.memoPcap.Text+="Packet dumped to file.\n";
            }
        }

        /******************************************************************************
           close the same browser before a test start
        /*******************************************************************************/
        private void CloseBrowser()
        {
            //iexplore; firefox; chrome
            if (strBro == "") return;
            string strProcessFile = strBro;
            if (strBro == "IE Explorer") strProcessFile = "iexplore";
            if (strBro == "Google") strProcessFile = "chrome";
            if (strBro == "Firefox Plus") strProcessFile = "Firefox";
            Process[] p = Process.GetProcessesByName(strProcessFile);
            if (p.Length > 0)
            {
                for (int i = 0; i < p.Length; i++)
                {
                    p[i].CloseMainWindow();
                    p[i].Kill();
                }
            }

        }

        /******************************************************************************
          clear all files and subdirs in dir 
        /*******************************************************************************/
        private void CleanFiles(string dir)
        {

            if (Directory.Exists(dir)) //�����������ļ���ɾ��֮ 
            {
                foreach (string d in Directory.GetFileSystemEntries(dir))
                {
                    if (File.Exists(d))
                        File.Delete(d); //ֱ��ɾ�����е��ļ� 
                    else
                        CleanFiles(d); //�ݹ�ɾ�����ļ��� 
                }
                Directory.Delete(dir); //ɾ���ѿ��ļ��� 
                //Response.Write(dir + " �ļ���ɾ���ɹ�");
            }
            //else
            //    Response.Write(dir + " ���ļ��в�����"); //����ļ��в���������ʾ 

        }

        /******************************************************************************
           call cmd: ipconfig/flushdns
        /*******************************************************************************/
        private void ClearDns()
        {
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.FileName = "ipconfig.exe";
            p.StartInfo.Arguments = "/flushdns";
            p.Start();
            //string Text = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

        }

        /******************************************************************************
           call sqlite3 methods to clear the Firefox cookies in a DB way
        /*******************************************************************************/
        private string ClearFirefoxCookies()
        {
            string strcallback = "";
            DB_NAME = inis.IniReadValue("Web", "firefoxCookie");
            try
            {

                if (DB_NAME == null)
                {
                    strcallback = "��Firefox Cookies�ļ�����...";

                    return strcallback;
                }

                string connString = String.Format("Data Source={0};New=False;Version=3", DB_NAME);

                SQLiteConnection sqlconn = new SQLiteConnection(connString);

                sqlconn.Open();

                string CommandText = "delete from moz_cookies";

                SQLiteCommand SQLiteCommand = new SQLiteCommand(CommandText, sqlconn);
                SQLiteCommand.ExecuteNonQuery();

                sqlconn.Close();

                strcallback = "Cookies�ɹ�ɾ��...";
            }
            catch (SQLiteException sqlex)
            {
                strcallback = sqlex.Message;
            }
            return strcallback;
        }
    }
}
