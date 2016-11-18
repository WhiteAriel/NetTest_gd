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

        public volatile bool serverTest = false;    //�����Ƿ��Ƿ������·������񣬷��������������ֶ���ͣ
        public volatile bool Taskon = false;        //��ʾ�Ƿ��������ڽ���
        private object taskLock = new object();

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
        System.Timers.Timer webTimer =null; 




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
        private static extern int SetParent(IntPtr hWndChild, IntPtr hWndParent);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        private static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter,
                    int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern uint SetWindowLong(IntPtr hwnd, int nIndex, uint newLong);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern uint GetWindowLong(IntPtr hwnd, int nIndex);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool ShowWindow(IntPtr hWnd, short State);


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
            webTimer=new System.Timers.Timer(3000);//ʵ����Timer�࣬���ü��ʱ��Ϊ3000���룻
            webTimer.Elapsed += new System.Timers.ElapsedEventHandler(timWeb_Tick);//����ʱ���ʱ��ִ���¼���
            webTimer.AutoReset = true;
            Init();
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
            lock (taskLock)
            {
                Taskon = false;
            }
            try
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
                //this.timWeb.Stop();
                webTimer.Enabled = false;
                webTimer.Stop();
                this.timer1.Stop();
                //ֹͣwebbrowser
                webEx.Visible = false;
                //CloseBrowser();
                DoTest = false;
            }
            catch (System.Exception ex)
            {
                Log.Console(ex.ToString());
                Log.Console(ex.ToString());
            }
           
        }


        private void btnWebStop_Click(object sender, EventArgs e)
        {
            webStopFunc();
        }

        //��ҳ������ɻص�
        private void webEx_LoadCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            Log.Console(" WebBrowserDocumentCompletedEvent trigger WebTest End!");
            Log.Info(" WebBrowserDocumentCompletedEvent trigger WebTest End!");
            lock (taskLock)
            {
                if (!DoTest)   //�����ֶ�ֹͣ��ʱ��ֹͣ��
                    return;
                DoTest = false;
            }

                //ֹͣץ��
                try
                {
                    //һ������ֹͣ�����͹رն�ʱ��
                    webTimer.Stop();
                    webTimer.Enabled = false;
                    device.PcapStopCapture();
                    device.PcapClose();
                    webEx.Visible = false;
                }
                catch (System.Exception ex)
                {
                    Log.Console(Environment.StackTrace, ex);
                    Log.Error(Environment.StackTrace, ex);
                }

                DateTime dtEnd = DateTime.Now;
                TimeSpan ts = dtEnd - dtStart;
                float ts2 = ts.Seconds + (float)ts.Milliseconds / 1000;


                this.memoPcap.Items.Add("���Խ���,��ʱ " + ts.Minutes + "�� " + ts2.ToString() + "��");
                strbFile.Append("���Խ���,��ʱ " + ts.Minutes + "�� " + ts2.ToString() + "��" + "\r\n");

                strbFile.Append("ץ���ļ�: " + strFile + "\r\n");

                DoTest = false;
                try
                {
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

                    this.sBTest.Enabled = true;
                    this.btnWebStop.Enabled = false;
                }
                catch (Exception ex)
                {
                    Log.Error(Environment.StackTrace, ex);
                    Log.Console(Environment.StackTrace, ex);
                }

                Taskon = false;
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
        public void WebServerTaskStartFunc()
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
                    this.btnWebStop.Enabled = false;

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
                    while (Taskon)
                            Thread.Sleep(2000);   //�����ȴ���ʱ����������                   
                    break;
                }
                else
                    Thread.Sleep(2000);
            }

        }


        public void WebTerminalTaskStartFunc()
        {
            Taskon = true;   //����ʼִ��
            iTimeThrehold = 0;
            if (strbFile.Length > 0) strbFile.Remove(0, strbFile.Length);

            if (intCheckContinuous == 0) this.iTest = 0;
            if (iTest == 0) intIndex = int.Parse(inis.IniReadValue("Web", "WebIndex"));
            iProMem = 0;
            this.sBTest.Enabled = false;
            this.timer1.Enabled = false;
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
        }


        private void sBTest_Click(object sender, EventArgs e)
        {
            //webStartFunc();
            WebTerminalTaskStartFunc();
        }

        /******************************************************************************
           the test itself
        /*******************************************************************************/
        private void WebTesting()
        {
            if (iDevice < 0)
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
                //this.CloseBrowser();
                //    if (iBool > 0)
                //    {
                //        if (strBro == "Google")
                //        {
                //            Thread.Sleep(400);
                //            //this.CleanFiles(inis.IniReadValue("Web", "GoogleCookies"));
                //            string dir = System.Environment.GetEnvironmentVariable("AppData");   //��ȡ��ֵ���и�Roarming
                //            int n = dir.LastIndexOf("\\");
                //            dir = dir.Substring(0, n) + @"\Local\Google\Chrome\User Data\Default\Cache";

                //            this.CleanFiles(dir);    //ɾ�������ļ���
                //            Thread.Sleep(400);
                //            this.memoPcap.Items.Add(" cookies, caches ɾ���������...");
                //           strbFile.Append(" cookies, caches ɾ���������...");
                //        }
                //        if (strBro == "Firefox")
                //        {
                //            string str = this.ClearFirefoxCookies();
                //            if (str == "Cookies�ɹ�ɾ��...")
                //            {
                //                this.memoPcap.Items.Add(" cookies, caches ɾ���������...");
                //                strbFile.Append(" cookies, caches ɾ���������...\r\n");
                //            }
                //            else
                //            {
                //                this.memoPcap.Items.Add(str);
                //                strbFile.Append(str + "\r\n");
                //            }
                //        }
                //        if (strBro == "IE Explorer")
                //        {
                //            int iResultCookies = cc.ClearAllCookies(string.Empty);
                //            int iResultCaches = cc.ClearAllCache(string.Empty);
                //            this.memoPcap.Items.Add(iResultCookies + " cookies, " + iResultCaches + " caches ɾ��...");
                //            strbFile.Append(iResultCookies + " cookies, " + iResultCaches + " caches ɾ��...\r\n");
                //        }
                //        this.ClearDns();
                //    }
                //    else
                //    {
                //        this.memoPcap.Items.Add("Cookies/caches δ��ȫɾ��...");
                //        strbFile.Append("Cookies/caches δ��ȫɾ��...\r\n");
                //    }
                //}
                //catch (Exception ex)
                //{
                //    this.memoPcap.Items.Add(ex.Message);
                //    this.memoPcap.Items.Add("Cookies/caches δ��ȫɾ��...");
                //    strbFile.Append("Cookies/caches δ��ȫɾ��...\r\n");
                //}

                try
                {
                    if (iBool > 0)
                    {
                        int iResultCookies = cc.ClearAllCookies(string.Empty);
                        int iResultCaches = cc.ClearAllCache(string.Empty);
                        this.memoPcap.Items.Add(iResultCookies + " cookies, " + iResultCaches + " caches ɾ��...");
                        strbFile.Append(iResultCookies + " cookies, " + iResultCaches + " caches ɾ��...\r\n");
                        this.ClearDns();
                    }
                    else
                    {
                        this.memoPcap.Items.Add("Cookies/caches δ��ȫɾ��...");
                        strbFile.Append("Cookies/caches δ��ȫɾ��...\r\n");
                    }
                }
                catch (System.Exception ex)
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

                device.PcapStartCapture();   //�����ײ�ץ��

                //����webbrowser
                webEx.Visible = true;
                string testUrl = inis.IniReadValue("Web", "WebPage");
                Uri url = new Uri(testUrl);
                webEx.Navigate(url);
                //�򿪳�ʱ��ʱ��
                webTimer.Enabled = true;
                webTimer.Start();
            }
            catch (Exception ex)
            {
                Log.Console(Environment.StackTrace, ex);
                Log.Error(Environment.StackTrace, ex);
            }
            //if (!m_AsyncWorker.IsBusy)
            //{
            //    m_AsyncWorker.RunWorkerAsync();
            //}

        }


        /******************************************************************************
           trigger for complete a test and judge a loop 
        /*******************************************************************************/
        private void timWeb_Tick(object sender, EventArgs e)
        {
            iTimeThrehold++;
            if (iTimeThrehold*3 <= iTimeLast)  //3���ӽ���һ��
                return;   //û����ʱʱ��

            Log.Console("Timer trigger WebTest End!");
            Log.Info("Timer trigger WebTest End!");
            lock (taskLock)
            {
                if (!DoTest)   //�����ֶ�ֹͣ����ؽ��������Ѿ�������
                    return;
                DoTest = false;
            }
            try
            {
                //һ������ֹͣ�����͹رն�ʱ��
                webTimer.Enabled = false;
                webTimer.Stop();              
                //ֹͣ���أ���֪���ܲ�����ֹ�������Ӧ����
                webEx.Visible = false;
                device.PcapStopCapture();
                device.PcapClose();
            }
            catch (System.Exception ex)
            {
                Log.Console(ex.ToString());
                Log.Error(ex.ToString());
            }

            DateTime dtEnd = DateTime.Now;
            TimeSpan ts = dtEnd - dtStart;
            float ts2 = ts.Seconds + (float)ts.Milliseconds / 1000;


            this.memoPcap.Items.Add("���Խ���,��ʱ " + ts.Minutes + "�� " + ts2.ToString() + "��");
            strbFile.Append("���Խ���,��ʱ " + ts.Minutes + "�� " + ts2.ToString() + "��" + "\r\n");

            strbFile.Append("ץ���ļ�: " + strFile + "\r\n");

            
            try
            {
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
                    if (iTest < iNumContinuous)   //���Դ�������ѭ�����Դ���
                    {
                        if (inis.IniReadValue("Web", "EnableLoop") == "1")  //�������
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
                        this.sBTest.Enabled = true;
                        this.btnWebStop.Enabled = false;
                        this.iTest = 0;
                        this.memoPcap.Items.Add("---------------�������---------------");
                        inis.IniWriteValue("Web", "WebPage", strTempURL);
                    }
                }
                else
                {
                    this.sBTest.Enabled = true;
                    this.btnWebStop.Enabled = false;
                    iTest = 0;
                }
            }
            catch (Exception ex)
            {
                Log.Error(Environment.StackTrace, ex);
                Log.Console(Environment.StackTrace, ex);
            }
            Taskon = false;    
            //CloseBrowser();                    
        }

        /******************************************************************************
           compute the performance;
           ts2: timespan the test lasts
        /*******************************************************************************/
        private void performance(float ts2)
        {
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
                Console.WriteLine("{0},{1}",Environment.StackTrace, ex);
                Log.Console(Environment.StackTrace, ex); Log.Warn(Environment.StackTrace, ex);
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
                    Log.Console(Environment.StackTrace, ex); Log.Warn(Environment.StackTrace, ex);
                }
                return;
            }
            pcap_file_close_inCS();         //����ڴ���Ϣ
            //����û�г���
            FileStream fsPacket = new FileStream(tmpFile, FileMode.Open, FileAccess.Read);
            StreamReader srPacket = new StreamReader(fsPacket, Encoding.Default);
            srPacket.ReadLine();   //��һ��Ϊ����
            string strLine = srPacket.ReadLine();
            string[] str ;//= strLine.Split(new Char[] { '\t' });  //Tcp�ش���	Tcp��������	HTTP Get�ɹ���	Dns��Ӧ�ӳ� Dns��Ӧ�ɹ���
            List<string> kpi=new List<string>();
            List<string> kpiName = new List<string>();

            strbFile.Append("--------------------------------------\r\n");
            strbFile.Append("|  �� �� ָ ��  |  ��  ֵ  |\r\n");
            strbFile.Append("--------------------------------------\r\n");
            strbFile.Append("|  ҵ����ʱ(��)    |  " + ts2.ToString("F2") + "|\r\n");
            strbFile.Append("--------------------------------------\r\n");

            this.memoPcap.Items.Add("--------------------------------------\r\n");
            this.memoPcap.Items.Add("|  �� �� ָ ��  |  ��  ֵ |\r\n");
            this.memoPcap.Items.Add("--------------------------------------\r\n");
            this.memoPcap.Items.Add("|  ҵ����ʱ(��) |  " + ts2.ToString("F2") + "|\r\n");
            this.memoPcap.Items.Add("--------------------------------------\r\n");

            while (strLine!=null)
            {
                str = strLine.Split(new Char[] { '\t' });
                if (str.Length == 3)
                {
                    kpiName.Add(str[1]);
                    kpi.Add(str[2]);
                }
                else
                {
                    kpiName.Add("Tcp�ش���(%)");
                    kpi.Add("0");
                }
                    strLine = srPacket.ReadLine();
            }
            srPacket.Close();
            fsPacket.Close();
            if (kpi.Count == 5 && kpiName.Count==5)
            {
                strbFile.Append("|  " + kpiName[0] + "  |" + kpi[0] + "|\r\n");
                strbFile.Append("--------------------------------------\r\n");
                strbFile.Append("|  " + kpiName[1] + "  |" + kpi[1] + "|\r\n");
                strbFile.Append("--------------------------------------\r\n");
                strbFile.Append("|  " + kpiName[2] + "  |" + kpi[2] + "|\r\n");
                strbFile.Append("--------------------------------------\r\n");
                strbFile.Append("|  " + kpiName[3] + "  |" + kpi[3] + "|\r\n");
                strbFile.Append("--------------------------------------\r\n");
                strbFile.Append("|  " + kpiName[4] + "  |" + kpi[4] + "|\r\n");
                strbFile.Append("--------------------------------------\r\n");
               
                this.memoPcap.Items.Add("|  " + kpiName[0] + "  |" + kpi[0] + "|\r\n");
                this.memoPcap.Items.Add("--------------------------------------\r\n");
                this.memoPcap.Items.Add("|  " + kpiName[1] + "  |" + kpi[1] + "|\r\n");
                this.memoPcap.Items.Add("--------------------------------------\r\n");
                this.memoPcap.Items.Add("|  " + kpiName[2] + "  |" + kpi[2] + "|\r\n");
                this.memoPcap.Items.Add("--------------------------------------\r\n");
                this.memoPcap.Items.Add("|  " + kpiName[3] + "  |" + kpi[3] + "|\r\n");
                this.memoPcap.Items.Add("--------------------------------------\r\n");
                this.memoPcap.Items.Add("|  " + kpiName[4] + "  |" + kpi[4] + "|\r\n");
                this.memoPcap.Items.Add("--------------------------------------\r\n");

            }
        }


        private double WebScore(double[] pra)
        {
            if (pra.Length == 6)
            {
                double[] praScore = new double[6];
                //ҵ����ʱ
                if (pra[0] <= 2.0)
                    praScore[0] = 100.0;
                else if (pra[0] <= 10.0)
                    praScore[0] = 100.0 - (pra[0] - 2.0) * 5.0;
                else
                    praScore[0] = 100.0 - (10.0 - 2.0) * 5.0 - (pra[0] - 10.0) * 10.0;
                if (praScore[0] < 0.0) praScore[0] = 0.0;
                //Tcp�ش���
                if (pra[1] <= 0.2)
                    praScore[1] = 100.0;
                else if (pra[1] <= 0.5)
                    praScore[1] = 100.0 - ((pra[1] - 0.2) / 0.1) * 5.0;
                else
                    praScore[1] = 100.0 - ((0.5 - 0.2) / 0.1) * 5.0 - ((pra[1] - 0.5) / 0.1) * 10.0;
                if (praScore[1] < 0.0) praScore[1] = 0.0;
                //Tcp��������
                if (pra[2] <= 0.5)
                    praScore[2] = 100.0;
                else if (pra[2] <= 1.0)
                    praScore[2] = 100.0 - ((pra[2] - 0.5) / 0.1) * 5.0;
                else
                    praScore[2] = 100.0 - ((1.0 - 0.5) / 0.1) * 5.0 - ((pra[2] - 1.0) / 0.1) * 10.0;
                if (praScore[2] < 0.0) praScore[2] = 0.0;
                //HTTP��ʱ
                if (pra[3] <= 50.0)
                    praScore[3] = 100.0;
                else if (pra[3] <= 80.0)
                    praScore[3] = 100.0 - ((pra[3] - 50.0) / 10.0) * 2.0;
                else
                    praScore[3] = 100.0 - ((80.0 - 50.0) / 10.0) * 2.0 - ((pra[3] - 80.0) / 10.0) * 5.0;
                if (praScore[3] < 0.0) praScore[3] = 0.0;
                //Dns��Ӧ�ӳ�
                if (pra[4] <= 50.0)
                    praScore[4] = 100.0;
                else if (pra[4] <= 80.0)
                    praScore[4] = 100.0 - ((pra[4] - 50.0) / 10.0) * 5.0;
                else
                    praScore[4] = 100.0 - ((80.0 - 50.0) / 10.0) * 5.0 - ((pra[4] - 80.0) / 10.0) * 10.0;
                if (praScore[4] < 0.0) praScore[4] = 0.0;
                //Dns��Ӧ�ɹ���
                if (pra[5] >= 99.99)
                    praScore[5] = 100.0;
                else if (pra[5] >= 99.9)
                    praScore[5] = 100.0 - ((99.99 - pra[5]) / 0.1) * 5.0;
                else
                    praScore[5] = 100.0 - ((99.99 - 99.9) / 0.1) * 5.0 - ((99.9 - pra[5]) / 0.1) * 10.0;
                if (praScore[5] < 0) praScore[5] = 0.0;

                double webScore = 0.2 * praScore[0] + 0.2 * praScore[1] + 0.1 * praScore[2] + 0.2 * praScore[3] + 0.2 * praScore[4] + 0.1 * praScore[5];
                return webScore;
            }
            return 0.0;
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
                if (strBro == "Firefox") strProcessFile = "firefox.exe";
                string testURL = inis.IniReadValue("Web", "WebPage");
                ProcessStartInfo psi = new ProcessStartInfo(strProcessFile);
                psi.FileName = strProcessFile;
                //psi.RedirectStandardInput = true;
                //psi.RedirectStandardOutput = true;
                //psi.UseShellExecute = false;
                if(!serverTest)
                    psi.WindowStyle = ProcessWindowStyle.Hidden;
                else
                    psi.WindowStyle = ProcessWindowStyle.Minimized;
                //psi.CreateNoWindow = true;
                psi.Arguments = testURL;
                Process ps = new Process();
                this.memoPcap.Items.Add("�����: " + strBro);
                strbFile.Append("�����: " + strBro + "\r\n");
                this.memoPcap.Items.Add("ҳ��: " + testURL);
                strbFile.Append("ҳ��: " + testURL + "\r\n");
                ps.StartInfo = psi;
                ps.Start();
                //�������ȴ�,������������ľ����û�д���,���ܿ��Ƴ���
                while (ps.MainWindowHandle.ToInt32() == 0)
                {
                    Thread.Sleep(300);
                    ps.Refresh();//����ˢ��״̬�������»��
                    ps.StartInfo = psi;
                }
                //���ñ���ܳ���ĸ�����
                SetParent(ps.MainWindowHandle, this.panelExplore.Handle);
                //�ı�ߴ�
                ResizeControl(ps);
                //�ָ�����
                ShowWindow(ps.MainWindowHandle, (short)SW_RESTORE);
                
 
            }
            catch (System.ComponentModel.Win32Exception ex)   //����û������
            {
                this.memoPcap.Items.Add(ex.Message);
                Log.Error(ex.ToString());
                //�����������޷��򿪣��ر��ж���ʱ����ץ���ļ�
                device.PcapClose();
                webTimer.Enabled = false;
                webTimer.Stop();
                //��ť�ͱ�������
                this.sBTest.Enabled = true;
                this.btnWebStop.Enabled = false;
                DoTest = false;
                Taskon = false;
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
             0 - 70,
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
                Log.Console(Environment.StackTrace, ex);
                Log.Error(Environment.StackTrace, ex);
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
            }
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
