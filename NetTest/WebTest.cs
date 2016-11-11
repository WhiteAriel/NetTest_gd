/******************************************************************************
	Copyright 2009-2010 hgh
/*******************************************************************************/

/*
*Name:			WebTest
*Author:		hgh
*Created:		2009/8
*Last Modified:	2016/4/15 
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
//using Finisar.SQLite;
using System.Data.SQLite;
using System.Runtime.InteropServices;
using NetLog;


namespace NetTest
{
    public partial class WebTest : DevExpress.XtraEditors.XtraUserControl
    {

        IniFile inis = new IniFile(Application.StartupPath + "\\settings.ini");  //ini class
        CacheClear cc = new CacheClear();   //ie cookies class
        private int iTest = 0;              //loop count
        private int intIndex;

        public bool flagAdapter = false;

        public bool serverTest = false;    //�����Ƿ��Ƿ������·������񣬷��������������ֶ���ͣ
        public bool Taskon = false;        //��ʾ�Ƿ��������ڽ���

        //stop perf
        public string strBro;
        public long iProMem = 0;
        public long iProLength = 0;
        public int iProCPU = 3;

        public string strFile = "";
        public int iDevice;
        public PcapDevice device;
        public NetworkDevice netDev;
        private DateTime dtStart;                  //start time
        private int iStable;
        private StringBuilder strbFile = new StringBuilder();    //contents of log
        private string strLogFile;                    //log file
        private int intCheckContinuous;
        private int iTimeThrehold = 0;
        private int iTimeLast = 0;
        private int iBool = 0;
        private int iNumContinuous = 0;
        public static bool DoTest;
        private string strTempURL;        //random test



        private BackgroundWorker m_AsyncWorker = new BackgroundWorker();

        [DllImport("NetpryDll.dll")]
        public extern static int pcap_file_dissect_inCS(string pathfilename);

        //�ر�Pcap�ļ�(�����ļ���Ҫ�����ݰ�������TCP������DNS������HTTP����)
        [DllImport("NetpryDll.dll")]
        public extern static void pcap_file_close_inCS();

        //������Խ��
        [DllImport("NetpryDll.dll")]
        public extern static int webTest_tofile(string tmpfilename);

       

[DllImport("user32.dll")]
private static extern int SetParent(IntPtr hWndChild,IntPtr hWndParent);

[DllImport("user32.dll")]
private static extern bool ShowWindowAsync(IntPtr hWnd,int nCmdShow);

[DllImport("user32.dll", SetLastError = true)]
private static extern bool PostMessage(IntPtr hWnd,uint Msg,int wParam,int lParam);

[DllImport("user32.dll", EntryPoint = "SetWindowPos")]
private static extern bool SetWindowPos(IntPtr hWnd,int hWndInsertAfter,
            int X,int Y,int cx,int cy,uint uFlags);

[DllImport("user32.dll")]
private static extern int SendMessage(IntPtr hWnd,uint Msg,int wParam,int lParam);

[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
private static extern uint SetWindowLong(IntPtr hwnd, int nIndex, uint newLong);

[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
private static extern uint GetWindowLong(IntPtr hwnd, int nIndex);

[DllImport("user32.dll", CharSet = CharSet.Auto)]
private static  extern bool ShowWindow(IntPtr hWnd, short State);


private const int HWND_TOP = 0x0;
private const int WM_COMMAND = 0x0112;
private const int WM_QT_PAINT = 0xC2DC;
private const int WM_PAINT = 0x000F;
private const int WM_SIZE = 0x0005;
private const int SWP_FRAMECHANGED = 0x0020;
public const int SW_MAXIMIZE = 3;
public const int SW_MINIMIZE = 6;
public const int SW_NORMAL = 1;
public const int SW_RESTORE = 9;

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
        public void webStopFunc()
        {
            this.sBTest.Enabled = true;
            this.btnWebStop.Enabled = false;
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
                Thread.Sleep(300);

            }
            iTest = 0;
            this.timWeb.Stop();
            this.timer1.Stop();
            CloseBrowser();
            DoTest = false;
            Taskon = false;
        }


        private void btnWebStop_Click(object sender, EventArgs e)
        {
            webStopFunc();
        }

        private string validateFileName(string str)    //������ҳ�в������ļ������ַ�
        {
            if (str.Contains("|"))
            {
                str = str.Replace('|', '_');
            }
            if (str.Contains("\\"))
            {
                str = str.Replace('\\', '_');
            }
            if (str.Contains("/"))
            {
                str = str.Replace('/', '_');
            }
            if (str.Contains(":"))
            {
                str = str.Replace(':', '_');
            }
            if (str.Contains(@"*"))
            {
                str = str.Replace(@"*", "_");
            }
            if (str.Contains(@"?"))
            {
                str = str.Replace(@"?", "_");
            }
            if (str.Contains(@"<"))
            {
                str = str.Replace(@"<", "_");
            }
            if (str.Contains(@">"))
            {
                str = str.Replace(@">", "_");
            }
            return str;
        }





        /******************************************************************************
           start the test single or loop, the main process call WebTesting() 
        /*******************************************************************************/
        public void webStartFunc()
        {
            while (true)
            {
                if (Taskon == false)
                {
                    Taskon = true;   //����ʼִ��
                    iTimeThrehold = 0;
                    if (strbFile.Length > 0) strbFile.Remove(0, strbFile.Length);

                    if (intCheckContinuous == 0) this.iTest = 0;
                    if (iTest == 0) intIndex = int.Parse(inis.IniReadValue("Web", "WebIndex"));
                    iProMem = 0;
                    this.sBTest.Enabled = false;
                    this.timer1.Enabled = false;

                    if (serverTest == true)   //���������������ֶ�ֹͣ
                        this.btnWebStop.Enabled = false;
                    else
                        this.btnWebStop.Enabled = true;

                    string tmp = inis.IniReadValue("Web", "WebPage");
                    tmp = validateFileName(tmp);

                    strFile = inis.IniReadValue("Web", "Path") + "\\Web-" + inis.IniReadValue("Web", "Browser") + "-"
                        + tmp + "-" + DateTime.Now.Year.ToString() + "-"
                        + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "-"
                        + DateTime.Now.Hour.ToString() + "-" + DateTime.Now.Minute.ToString() + "-"
                        + DateTime.Now.Second.ToString();// +".cap";
                    strLogFile = strFile + ".xls";
                    strFile = strFile + ".pcap";

                    inis.IniWriteValue("Web", "webPcapPath", strFile);   //ץ���ļ�·��

                    if (!Directory.Exists(inis.IniReadValue("Web", "Path"))) Directory.CreateDirectory(inis.IniReadValue("Web", "Path"));

                    this.WebTesting();
                    break;
                }
                else
                    Thread.Sleep(2000);
            }
           
        }



        private void sBTest_Click(object sender, EventArgs e)
        {
            webStartFunc();
        }

        /******************************************************************************
           the test itself
        /*******************************************************************************/
        private void WebTesting()
        {
            if (iDevice<0)
            {
                this.memoPcap.Items.Clear();
                this.memoPcap.Items.Add("û�����������������������á�\n");
                this.memoPcap.Items.Add("�����˳���\n");
                this.sBTest.Enabled = true;
                this.btnWebStop.Enabled = false;
                return;
            }
            PcapDeviceList devices = Tamir.IPLib.SharpPcap.GetAllDevices();
            device = devices[iDevice];
            if (device is NetworkDevice)
            {
                netDev = (NetworkDevice)device;    //�������PcapDeviceList������NetworkDevice��ת�����ܻ�ȡip
            }
            else
            {
                this.memoPcap.Items.Add("--------------����Ч����,���������ã�---------------");
                this.sBTest.Enabled = true;
                this.btnWebStop.Enabled = false;
                return;
            }



            DoTest = true;
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
                        //this.CleanFiles(inis.IniReadValue("Web", "GoogleCookies"));
                        string dir = System.Environment.GetEnvironmentVariable("AppData");   //��ȡ��ֵ���и�Roarming
                        int n=dir.LastIndexOf("\\");
                        dir=dir.Substring(0,n)+@"\Local\Google\Chrome\User Data\Default\Cache";
                    
                        this.CleanFiles(dir);    //ɾ�������ļ���
                        Thread.Sleep(400);
                        this.memoPcap.Items.Add(" cookies, caches ɾ���������...");
                        strbFile.Append(" cookies, caches ɾ���������...");

                    }
                    if (strBro == "Firefox")
                    {
                        //if (!File.Exists(inis.IniReadValue("Web", "FirefoxPlus")))
                        //{
                        //    this.memoPcap.Items.Add("�����жϣ��޷���Firefox");
                        //    if (strbFile.Length > 0) strbFile.Remove(0, strbFile.Length);
                        //    return;
                        //}

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

            string ip = netDev.IpAddress;
            this.memoPcap.Items.Add("���������ر�");
            this.memoPcap.Items.Add("����: " + device.PcapDescription);
            strbFile.Append("����: " + device.PcapDescription + "\r\n");

            this.memoPcap.Items.Add("IP��ַ: " + netDev.IpAddress);
            strbFile.Append("IP��ַ: " + netDev.IpAddress + "\r\n");

            Thread.Sleep(100);
            this.dtStart = DateTime.Now;
            this.memoPcap.Items.Add("���Կ�ʼʱ��: " + dtStart.ToString());
            strbFile.Append("���Կ�ʼʱ��: " + dtStart.ToString() + "\r\n");

            //Register our handler function to the 'packet arrival' event
            device.PcapOnPacketArrival +=
                new Tamir.IPLib.SharpPcap.PacketArrivalEvent(device_PcapOnPacketArrival);

            //Open the device for capturing
            //true -- means promiscuous mode
            //1000 -- means a read wait of 1000ms

            device.PcapOpen(true, 100);
            device.PcapSetFilter("(tcp or udp) and host " + ip);     //��ȡIP���ڹ���
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
            device.PcapClose();
            //this.CloseBrowser();    //��ֹ���Ի��߲�����ɵ�ʱ��ر�����������²��Ե�ʱ��Ҫ�ټ�飬��������׳�����

            DateTime dtEnd = DateTime.Now;
            TimeSpan ts = dtEnd - dtStart;
            float ts2 = ts.Seconds + (float)ts.Milliseconds / 1000;


            this.memoPcap.Items.Add("���Խ���,��ʱ " + ts.Minutes + "�� " + ts2.ToString() + "��");
            strbFile.Append("���Խ���,��ʱ " + ts.Minutes + "�� " + ts2.ToString() + "��" + "\r\n");

            strbFile.Append("ץ���ļ�: " + strFile + "\r\n");

            DoTest = false;
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
            //Thread.Sleep(300);
            CloseBrowser();
        }

        /******************************************************************************
           compute the performance;
           ts2: timespan the test lasts
        /*******************************************************************************/
        private void performance(float ts2)
        {
            //string ip = device.PcapIpAddress;
            //string ip = netDev.IpAddress;
            //string temp = ip.Substring(0, ip.IndexOf("."));
            //int a = int.Parse(temp);
            //ip = ip.Substring(temp.Length + 1);
            //temp = ip.Substring(0, ip.IndexOf("."));
            //int b = int.Parse(temp);
            //ip = ip.Substring(temp.Length + 1);
            //temp = ip.Substring(0, ip.IndexOf("."));
            //int c = int.Parse(temp);
            //ip = ip.Substring(temp.Length + 1);
            //int d = int.Parse(ip);

            //bool opencap = testmain(strFile, a, b, c, d);//OpenCap(strFile);OpenCap(strFile);////
            //if (opencap)
            //{
            //    int count = GetCounts();
            //    int lossnum = GetLossnum();
            //    int dupacknum = GetDupacknum();
            //    int misordernum = GetMisordernum();
            //    int retransmitnum = GetRetransmitnum();
            //    if (count > 0)
            //    {
            //        float loss = (float)lossnum * 100 / count;
            //        float retrans = (float)retransmitnum * 100 / count;
            //        float mis = (float)misordernum * 100 / count;
            //        string perf1, perf2, perf3;
            //        if (loss < 1.8) perf1 = "��  ��";
            //        else
            //        {
            //            if (loss < 2.1) perf1 = "һ  ��";
            //            else perf1 = "��  ��";
            //        }
            //        if (retrans < 2) perf2 = "��  ��";
            //        else
            //        {
            //            if (retrans < 2.2) perf2 = "һ  ��";
            //            else perf2 = "��  ��";
            //        }
            //        if (mis < 1.9) perf3 = "��  ��";
            //        else
            //        {
            //            if (mis < 2.2) perf3 = "һ  ��";
            //            else perf3 = "��  ��";
            //        }

            int i = -1;
            string tmpFile = "dissectWebTest.tmp";
            try
            {
                i = pcap_file_dissect_inCS(strFile);
                
                //������ʱ�ļ�
                FileStream fs = File.Create(tmpFile);
                fs.Close();
                webTest_tofile(tmpFile);        //���Խ��д��dissectWebTest.tmp��
            }
            catch (System.Exception ex)
            {
                i = -1;
               Log.Console(Environment.StackTrace,ex); Log.Warn(Environment.StackTrace,ex);
            }

            if (i < 0)
            {
                //���ݰ��򿪴���ֱ���˳���                
                try
                {
                    pcap_file_close_inCS();
                }
                catch (System.Exception ex)
                {
                   Log.Console(Environment.StackTrace,ex); Log.Warn(Environment.StackTrace,ex);
                }
                return;
            }         
           pcap_file_close_inCS();         //����ڴ���Ϣ
            //����û�г���
           FileStream fsPacket = new FileStream(tmpFile, FileMode.Open, FileAccess.Read);
           StreamReader srPacket = new StreamReader(fsPacket, Encoding.Default);
           string  strLine = srPacket.ReadLine();   //��һ��Ϊ����
           strLine = srPacket.ReadLine();
           string[] str = strLine.Split(new Char[] {'\t'},5);  //Tcp�ش���	Tcp��������	HTTP Get�ɹ���	Dns��Ӧ�ӳ� Dns��Ӧ�ɹ���
           srPacket.Close();        
        strbFile.Append("--------------------------------------\r\n");
        strbFile.Append("|  �� �� ָ ��  |  ��  ֵ  |  ��  ��  |\r\n");
        strbFile.Append("--------------------------------------\r\n");
        strbFile.Append("|  ҵ����ʱ(��)    |  " + ts2.ToString("F2") + "|\r\n");
        strbFile.Append("--------------------------------------\r\n");
        strbFile.Append("|  Tcp�ش���(%)    |  " + str[0]  +"|\r\n");
        strbFile.Append("--------------------------------------\r\n");
        strbFile.Append("|  Tcp��������(/s) |  " + str[1]  +"|\r\n");
        strbFile.Append("--------------------------------------\r\n");
        strbFile.Append("|  HTTP Get�ɹ���(%)|  " + str[2] + "|\r\n");
        strbFile.Append("--------------------------------------\r\n");
        strbFile.Append("|  Dns��Ӧ�ӳ�    |  " + str[3] + "|\r\n");
        strbFile.Append("--------------------------------------\r\n");
        strbFile.Append("|  Dns��Ӧ�ɹ���(%)|  " + str[4] + "|\r\n");
        strbFile.Append("--------------------------------------\r\n");

        //this.memoPcap.Items.Add("\n"); 
        //this.memoPcap.Items.Add("Web������������:\n");
        //this.memoPcap.Items.Add("--------------------------------------\r\n");
        //this.memoPcap.Items.Add("|  �� �� ָ ��  |  ��  ֵ  |  ��  ��  |\r\n");
        //this.memoPcap.Items.Add("--------------------------------------\r\n");
        //this.memoPcap.Items.Add("|  ҵ����ʱ(��) |  " + ts2.ToString("F2") + "   |          |\r\n");
        //this.memoPcap.Items.Add("--------------------------------------\r\n");
        //this.memoPcap.Items.Add("|  �� �� ��(%)  |  " + loss.ToString("F2") + "  |" + perf1 + "  |\r\n");
        //this.memoPcap.Items.Add("--------------------------------------\r\n");
        //this.memoPcap.Items.Add("|  �� �� ��(%)  |  " + retrans.ToString("F2") + "  |" + perf2 + "  |\r\n");
        //this.memoPcap.Items.Add("--------------------------------------\r\n");
        //this.memoPcap.Items.Add("|  ʧ �� ��(%)  |  " + mis.ToString("F2") + "  |" + perf3 + "  |\r\n");
        //this.memoPcap.Items.Add("--------------------------------------\r\n");

        this.memoPcap.Items.Add("--------------------------------------\r\n");
        this.memoPcap.Items.Add("|  �� �� ָ ��  |  ��  ֵ  |  ��  ��  |\r\n");
        this.memoPcap.Items.Add("--------------------------------------\r\n");
        this.memoPcap.Items.Add("|  ҵ����ʱ(��) |  " + ts2.ToString("F2") + "|\r\n");
        this.memoPcap.Items.Add("--------------------------------------\r\n");
        this.memoPcap.Items.Add("|  Tcp�ش���(%)  |  " + str[0] + "|\r\n");
        this.memoPcap.Items.Add("--------------------------------------\r\n");
        this.memoPcap.Items.Add("|  Tcp��������(/s) |  " + str[1] + "|\r\n");
        this.memoPcap.Items.Add("--------------------------------------\r\n");
        this.memoPcap.Items.Add("|  HTTP Get�ɹ���(%)|  " + str[2] + "|\r\n");
        this.memoPcap.Items.Add("--------------------------------------\r\n");
        this.memoPcap.Items.Add("|  Dns��Ӧ�ӳ�  |  " + str[3] + "|\r\n");
        this.memoPcap.Items.Add("--------------------------------------\r\n");
        this.memoPcap.Items.Add("|  Dns��Ӧ�ɹ���(%)|  " + str[4] + "|\r\n");
        this.memoPcap.Items.Add("--------------------------------------\r\n");
          
            //    else
            //    {
            //        strbFile.Append("�ܰ�����ʧ...\r\n");
            //        this.memoPcap.Items.Add("�ܰ�����ʧ...\r\n");
            //    }

            //}
            //else
            //{
            //    strbFile.Append("��ָ�����ʧ��...\r\n");
            //    this.memoPcap.Items.Add("��ָ�����ʧ��...\r\n");
            //}
            
        }

        /******************************************************************************
           worker
        /*******************************************************************************/
        private void bwAsync_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                string strProcessFile = strBro;
                if (strBro == "IE Explorer") strProcessFile = "iexplore.exe";
                if (strBro == "Google") strProcessFile = "chrome.exe";
                //if (strBro == "Firefox") strProcessFile = inis.IniReadValue("Web", "FirefoxPlus");// "Firefox";
                if (strBro == "Firefox") strProcessFile = "firefox.exe";
                //this.memoPcap.Items.Add(strProcessFile);
                 string testURL = inis.IniReadValue("Web", "WebPage");
                ProcessStartInfo psi = new ProcessStartInfo(strProcessFile);
                ///psi = new ProcessStartInfo(strProcessFile);
                psi.FileName = strProcessFile;
                //psi.RedirectStandardInput = true;
                //psi.RedirectStandardOutput = true;
                //psi.UseShellExecute = false;
                psi.WindowStyle = ProcessWindowStyle.Minimized;
                psi.Arguments = testURL;

                Process ps = new Process();
                
       
                //if (strBro == "Firefox ") ps.StartInfo.FileName = strProcessFile;
                //else
                //ps.StartInfo.FileName = strProcessFile + ".exe";
                //ps.StartInfo.Arguments = testURL;
                this.memoPcap.Items.Add("�����: " + strBro);
                strbFile.Append("�����: " + strBro + "\r\n");

                this.memoPcap.Items.Add("ҳ��: " + testURL);
                strbFile.Append("ҳ��: " + testURL + "\r\n");

               // ps.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
                ps.StartInfo = psi;
                ps.Start();

                    //�������ȴ�,������������ľ����û�д���,���ܿ��Ƴ���
                while (ps.MainWindowHandle.ToInt32() == 0)
                {
                    Thread.Sleep(300);
                    ps.Refresh();//����ˢ��״̬�������»��
                    ps.StartInfo = psi;
                }
                //Thread.Sleep(1500);
               //���ñ���ܳ���ĸ�����
                SetParent(ps.MainWindowHandle, this.panelExplore.Handle);
                //�ָ�����
                ShowWindow(ps.MainWindowHandle, (short)SW_RESTORE);
                //�ı�ߴ�
                ResizeControl(ps);

            }
            catch (Exception ex)
            {
                this.memoPcap.Items.Add(ex.Message);
                this.sBTest.Enabled = true;
                this.btnWebStop.Enabled = false;
                DoTest = false;
                return;
            }

            device.PcapStartCapture();
        }

        private void MinimizeWindow(Process ps)
        {
            //SetParent(ps.MainWindowHandle, null);
            ShowWindow(ps.MainWindowHandle, (short)SW_MINIMIZE);
        }


        //����Ƕ������λ�úͳߴ�
        private void ResizeControl(Process ps)
        {
            SendMessage(ps.MainWindowHandle, WM_COMMAND, WM_PAINT, 0);
            PostMessage(ps.MainWindowHandle, WM_QT_PAINT, 0, 0);

        
             SetWindowPos(
           ps.MainWindowHandle,
             HWND_TOP,
             0,//����ƫ����,��ԭ�����ڵĲ˵���ס
              0 -70,
             (int)this.Width - 150,          //���ؿ��
             (int)this.Height - 190,     //���ظ߶�
             SWP_FRAMECHANGED);
    
            SendMessage(ps.MainWindowHandle, WM_COMMAND, WM_SIZE, 0);
        }



        /******************************************************************************
           worker instance complete
        /*******************************************************************************/
        private void bwAsync_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //DoTest = false;
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
            if (strBro == "Firefox") strProcessFile = "Firefox";
            try
            {
                Process[] p = Process.GetProcessesByName(strProcessFile);
                if (p.Length > 0)                     //�رպ��������ص����н���
                {
                    for (int i = 0; i < p.Length; i++)
                    {
                        p[i].CloseMainWindow();
                        p[i].Kill();
                    }
                }
            }
            catch (System.Exception ex)
            {
                this.memoPcap.Items.Add(ex.Message);
            }
           

        }

        /******************************************************************************
          clear all files and subdirs in dir 
        /*******************************************************************************/
        private void CleanFiles(string dir)
        {

            //string dir=System.Environment.GetEnvironmentVariable("AppData") + @"\Local\Google\Chrome\User Data\Default\Cache"; //@��ʾ/����ʾת���ַ�
            if (Directory.Exists(dir)) //�����������ļ���ɾ��֮ 
            {
                foreach (string d in Directory.GetFileSystemEntries(dir))
                {
                    if (File.Exists(d))
                        File.Delete(d); //ֱ��ɾ�����е��ļ� 
                    else
                        CleanFiles(d); //�ݹ�ɾ�����ļ��� 
                }
                //Directory.Delete(dir); //ɾ���ѿ��ļ��� 
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
            //DB_NAME = inis.IniReadValue("Web", "FirefoxCookie");
            string dbPath = string.Empty; //cookies.sqlite�ļ�·��
            DirectoryInfo di = new DirectoryInfo(System.Environment.GetEnvironmentVariable("AppData") + @"\Mozilla\Firefox\Profiles\"); //@��ʾ/����ʾת���ַ�
            DirectoryInfo[] dirs = di.GetDirectories();//��ȡ���ļ����б� 
            if (dirs != null) { dbPath = dirs[0].FullName + "\\cookies.sqlite"; }   //��ȡfirefox��cookie��ַ,����cookies.sqlite��Ȩ�޿���
            //�������Ҫ�ڹ���ԱȨ��������
            //dbPath = "D:\\a.sqlite";
            try
            {

                if (dbPath == null)
                {
                    strcallback = "��Firefox Cookies�ļ�����...";

                    return strcallback;
                }

                //string connString = String.Format("Data Source={0};New=False;Version=3", dbPath);
                //string connString = String.Format("Data Source={0}", dbPath);
                //SQLiteConnection sqlconn = new SQLiteConnection(connString);
                SQLiteConnection conn = new SQLiteConnection();
                SQLiteConnectionStringBuilder connsb = new SQLiteConnectionStringBuilder();
                connsb.DataSource = dbPath;
                conn.ConnectionString = connsb.ToString();
                conn.Open();
                //sqlconn.Open();      //������
                string CommandText = "delete from moz_cookies";

                SQLiteCommand SQLiteCommand = new SQLiteCommand(CommandText, conn);  //ִ������
                SQLiteCommand.ExecuteNonQuery();

                conn.Close();

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
