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

        public bool serverTest = false;    //任务是否是服务器下发的任务，服务器任务不允许手动暂停
        public bool Taskon = false;        //表示是否有任务在进行

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

        //关闭Pcap文件(用于文件概要、数据包解析、TCP解析、DNS解析、HTTP解析)
        [DllImport("NetpryDll.dll")]
        public extern static void pcap_file_close_inCS();

        //输出测试结果
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
            catch { MessageBox.Show("请检查连续测试参数的设置！"); }
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
            this.memoPcap.Items.Add("测试被用户中断\n");
            if (DoTest)
            {
                strbFile.Append("测试被用户中断\n");
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

                this.memoPcap.Items.Add("抓包文件: " + strFile + " 创建");

                this.memoPcap.Items.Add("日志文件: " + strLogFile);
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

        private string validateFileName(string str)    //处理网页中不能做文件名的字符
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
                    Taskon = true;   //任务开始执行
                    iTimeThrehold = 0;
                    if (strbFile.Length > 0) strbFile.Remove(0, strbFile.Length);

                    if (intCheckContinuous == 0) this.iTest = 0;
                    if (iTest == 0) intIndex = int.Parse(inis.IniReadValue("Web", "WebIndex"));
                    iProMem = 0;
                    this.sBTest.Enabled = false;
                    this.timer1.Enabled = false;

                    if (serverTest == true)   //服务器任务不允许手动停止
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

                    inis.IniWriteValue("Web", "webPcapPath", strFile);   //抓包文件路径

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
                this.memoPcap.Items.Add("没有联网网卡！请检查网络设置。\n");
                this.memoPcap.Items.Add("测试退出。\n");
                this.sBTest.Enabled = true;
                this.btnWebStop.Enabled = false;
                return;
            }
            PcapDeviceList devices = Tamir.IPLib.SharpPcap.GetAllDevices();
            device = devices[iDevice];
            if (device is NetworkDevice)
            {
                netDev = (NetworkDevice)device;    //必须进行PcapDeviceList到子类NetworkDevice的转换才能换取ip
            }
            else
            {
                this.memoPcap.Items.Add("--------------非有效网卡,请重新设置！---------------");
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

            this.memoPcap.Items.Add("第 " + iTest + " 次测试......");
            strbFile.Append("第 " + iTest + " 次测试......\r\n");

            try
            {
                this.CloseBrowser();     
                if (iBool > 0)
                {
                    if (strBro == "Google")
                    {
                        Thread.Sleep(400);
                        //this.CleanFiles(inis.IniReadValue("Web", "GoogleCookies"));
                        string dir = System.Environment.GetEnvironmentVariable("AppData");   //获取的值后有个Roarming
                        int n=dir.LastIndexOf("\\");
                        dir=dir.Substring(0,n)+@"\Local\Google\Chrome\User Data\Default\Cache";
                    
                        this.CleanFiles(dir);    //删除缓冲文件夹
                        Thread.Sleep(400);
                        this.memoPcap.Items.Add(" cookies, caches 删除操作完成...");
                        strbFile.Append(" cookies, caches 删除操作完成...");

                    }
                    if (strBro == "Firefox")
                    {
                        //if (!File.Exists(inis.IniReadValue("Web", "FirefoxPlus")))
                        //{
                        //    this.memoPcap.Items.Add("测试中断，无法打开Firefox");
                        //    if (strbFile.Length > 0) strbFile.Remove(0, strbFile.Length);
                        //    return;
                        //}

                        string str = this.ClearFirefoxCookies();
                        if (str == "Cookies成功删除...")
                        {
                            this.memoPcap.Items.Add(" cookies, caches 删除操作完成...");
                            strbFile.Append(" cookies, caches 删除操作完成...\r\n");

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
                        this.memoPcap.Items.Add(iResultCookies + " cookies, " + iResultCaches + " caches 删除...");
                        strbFile.Append(iResultCookies + " cookies, " + iResultCaches + " caches 删除...\r\n");

                    }
                    this.ClearDns();


                }
                else
                {
                    this.memoPcap.Items.Add("Cookies/caches 未完全删除...");
                    strbFile.Append("Cookies/caches 未完全删除...\r\n");

                }
            }
            catch (Exception ex)
            {

                this.memoPcap.Items.Add(ex.Message);
                this.memoPcap.Items.Add("Cookies/caches 未完全删除...");
                strbFile.Append("Cookies/caches 未完全删除...\r\n");

            }

            string ip = netDev.IpAddress;
            this.memoPcap.Items.Add("相关浏览器关闭");
            this.memoPcap.Items.Add("网卡: " + device.PcapDescription);
            strbFile.Append("网卡: " + device.PcapDescription + "\r\n");

            this.memoPcap.Items.Add("IP地址: " + netDev.IpAddress);
            strbFile.Append("IP地址: " + netDev.IpAddress + "\r\n");

            Thread.Sleep(100);
            this.dtStart = DateTime.Now;
            this.memoPcap.Items.Add("测试开始时间: " + dtStart.ToString());
            strbFile.Append("测试开始时间: " + dtStart.ToString() + "\r\n");

            //Register our handler function to the 'packet arrival' event
            device.PcapOnPacketArrival +=
                new Tamir.IPLib.SharpPcap.PacketArrivalEvent(device_PcapOnPacketArrival);

            //Open the device for capturing
            //true -- means promiscuous mode
            //1000 -- means a read wait of 1000ms

            device.PcapOpen(true, 100);
            device.PcapSetFilter("(tcp or udp) and host " + ip);     //获取IP用于过滤
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
            //this.CloseBrowser();    //终止测试或者测试完成的时候关闭浏览器，重新测试的时候要再检查，这里关容易出问题

            DateTime dtEnd = DateTime.Now;
            TimeSpan ts = dtEnd - dtStart;
            float ts2 = ts.Seconds + (float)ts.Milliseconds / 1000;


            this.memoPcap.Items.Add("测试结束,耗时 " + ts.Minutes + "分 " + ts2.ToString() + "秒");
            strbFile.Append("测试结束,耗时 " + ts.Minutes + "分 " + ts2.ToString() + "秒" + "\r\n");

            strbFile.Append("抓包文件: " + strFile + "\r\n");

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
            this.memoPcap.Items.Add("抓包文件: " + strFile + " 创建");

            this.memoPcap.Items.Add("日志文件: " + strLogFile);

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
                    this.memoPcap.Items.Add("---------------测试完成---------------");
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
            //        if (loss < 1.8) perf1 = "良  好";
            //        else
            //        {
            //            if (loss < 2.1) perf1 = "一  般";
            //            else perf1 = "较  差";
            //        }
            //        if (retrans < 2) perf2 = "良  好";
            //        else
            //        {
            //            if (retrans < 2.2) perf2 = "一  般";
            //            else perf2 = "较  差";
            //        }
            //        if (mis < 1.9) perf3 = "良  好";
            //        else
            //        {
            //            if (mis < 2.2) perf3 = "一  般";
            //            else perf3 = "较  差";
            //        }

            int i = -1;
            string tmpFile = "dissectWebTest.tmp";
            try
            {
                i = pcap_file_dissect_inCS(strFile);
                
                //创建临时文件
                FileStream fs = File.Create(tmpFile);
                fs.Close();
                webTest_tofile(tmpFile);        //测试结果写入dissectWebTest.tmp中
            }
            catch (System.Exception ex)
            {
                i = -1;
               Log.Console(Environment.StackTrace,ex); Log.Warn(Environment.StackTrace,ex);
            }

            if (i < 0)
            {
                //数据包打开错误，直接退出！                
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
           pcap_file_close_inCS();         //清除内存信息
            //解析没有出错
           FileStream fsPacket = new FileStream(tmpFile, FileMode.Open, FileAccess.Read);
           StreamReader srPacket = new StreamReader(fsPacket, Encoding.Default);
           string  strLine = srPacket.ReadLine();   //第一行为解释
           strLine = srPacket.ReadLine();
           string[] str = strLine.Split(new Char[] {'\t'},5);  //Tcp重传率	Tcp并发特性	HTTP Get成功率	Dns响应延迟 Dns响应成功率
           srPacket.Close();        
        strbFile.Append("--------------------------------------\r\n");
        strbFile.Append("|  量 化 指 标  |  数  值  |  评  分  |\r\n");
        strbFile.Append("--------------------------------------\r\n");
        strbFile.Append("|  业务延时(秒)    |  " + ts2.ToString("F2") + "|\r\n");
        strbFile.Append("--------------------------------------\r\n");
        strbFile.Append("|  Tcp重传率(%)    |  " + str[0]  +"|\r\n");
        strbFile.Append("--------------------------------------\r\n");
        strbFile.Append("|  Tcp并发特性(/s) |  " + str[1]  +"|\r\n");
        strbFile.Append("--------------------------------------\r\n");
        strbFile.Append("|  HTTP Get成功率(%)|  " + str[2] + "|\r\n");
        strbFile.Append("--------------------------------------\r\n");
        strbFile.Append("|  Dns响应延迟    |  " + str[3] + "|\r\n");
        strbFile.Append("--------------------------------------\r\n");
        strbFile.Append("|  Dns响应成功率(%)|  " + str[4] + "|\r\n");
        strbFile.Append("--------------------------------------\r\n");

        //this.memoPcap.Items.Add("\n"); 
        //this.memoPcap.Items.Add("Web测试质量报告:\n");
        //this.memoPcap.Items.Add("--------------------------------------\r\n");
        //this.memoPcap.Items.Add("|  量 化 指 标  |  数  值  |  评  分  |\r\n");
        //this.memoPcap.Items.Add("--------------------------------------\r\n");
        //this.memoPcap.Items.Add("|  业务延时(秒) |  " + ts2.ToString("F2") + "   |          |\r\n");
        //this.memoPcap.Items.Add("--------------------------------------\r\n");
        //this.memoPcap.Items.Add("|  丢 包 率(%)  |  " + loss.ToString("F2") + "  |" + perf1 + "  |\r\n");
        //this.memoPcap.Items.Add("--------------------------------------\r\n");
        //this.memoPcap.Items.Add("|  重 传 率(%)  |  " + retrans.ToString("F2") + "  |" + perf2 + "  |\r\n");
        //this.memoPcap.Items.Add("--------------------------------------\r\n");
        //this.memoPcap.Items.Add("|  失 序 率(%)  |  " + mis.ToString("F2") + "  |" + perf3 + "  |\r\n");
        //this.memoPcap.Items.Add("--------------------------------------\r\n");

        this.memoPcap.Items.Add("--------------------------------------\r\n");
        this.memoPcap.Items.Add("|  量 化 指 标  |  数  值  |  评  分  |\r\n");
        this.memoPcap.Items.Add("--------------------------------------\r\n");
        this.memoPcap.Items.Add("|  业务延时(秒) |  " + ts2.ToString("F2") + "|\r\n");
        this.memoPcap.Items.Add("--------------------------------------\r\n");
        this.memoPcap.Items.Add("|  Tcp重传率(%)  |  " + str[0] + "|\r\n");
        this.memoPcap.Items.Add("--------------------------------------\r\n");
        this.memoPcap.Items.Add("|  Tcp并发特性(/s) |  " + str[1] + "|\r\n");
        this.memoPcap.Items.Add("--------------------------------------\r\n");
        this.memoPcap.Items.Add("|  HTTP Get成功率(%)|  " + str[2] + "|\r\n");
        this.memoPcap.Items.Add("--------------------------------------\r\n");
        this.memoPcap.Items.Add("|  Dns响应延迟  |  " + str[3] + "|\r\n");
        this.memoPcap.Items.Add("--------------------------------------\r\n");
        this.memoPcap.Items.Add("|  Dns响应成功率(%)|  " + str[4] + "|\r\n");
        this.memoPcap.Items.Add("--------------------------------------\r\n");
          
            //    else
            //    {
            //        strbFile.Append("总包数丢失...\r\n");
            //        this.memoPcap.Items.Add("总包数丢失...\r\n");
            //    }

            //}
            //else
            //{
            //    strbFile.Append("包指标分析失败...\r\n");
            //    this.memoPcap.Items.Add("包指标分析失败...\r\n");
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
                this.memoPcap.Items.Add("浏览器: " + strBro);
                strbFile.Append("浏览器: " + strBro + "\r\n");

                this.memoPcap.Items.Add("页面: " + testURL);
                strbFile.Append("页面: " + testURL + "\r\n");

               // ps.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
                ps.StartInfo = psi;
                ps.Start();

                    //这里必须等待,否则启动程序的句柄还没有创建,不能控制程序
                while (ps.MainWindowHandle.ToInt32() == 0)
                {
                    Thread.Sleep(300);
                    ps.Refresh();//必须刷新状态才能重新获得
                    ps.StartInfo = psi;
                }
                //Thread.Sleep(1500);
               //设置被绑架程序的父窗口
                SetParent(ps.MainWindowHandle, this.panelExplore.Handle);
                //恢复窗口
                ShowWindow(ps.MainWindowHandle, (short)SW_RESTORE);
                //改变尺寸
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


        //控制嵌入程序的位置和尺寸
        private void ResizeControl(Process ps)
        {
            SendMessage(ps.MainWindowHandle, WM_COMMAND, WM_PAINT, 0);
            PostMessage(ps.MainWindowHandle, WM_QT_PAINT, 0, 0);

        
             SetWindowPos(
           ps.MainWindowHandle,
             HWND_TOP,
             0,//设置偏移量,把原来窗口的菜单遮住
              0 -70,
             (int)this.Width - 150,          //像素宽度
             (int)this.Height - 190,     //像素高度
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
                this.memoPcap.Items.Add("浏览器错误");
                return;
            }

            // Check to see if the background process was cancelled.
            if (e.Cancelled)
            {
                this.memoPcap.Items.Add("任务撤销");
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
                if (p.Length > 0)                     //关闭和浏览器相关的所有进程
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

            //string dir=System.Environment.GetEnvironmentVariable("AppData") + @"\Local\Google\Chrome\User Data\Default\Cache"; //@表示/不表示转义字符
            if (Directory.Exists(dir)) //如果存在这个文件夹删除之 
            {
                foreach (string d in Directory.GetFileSystemEntries(dir))
                {
                    if (File.Exists(d))
                        File.Delete(d); //直接删除其中的文件 
                    else
                        CleanFiles(d); //递归删除子文件夹 
                }
                //Directory.Delete(dir); //删除已空文件夹 
                //Response.Write(dir + " 文件夹删除成功");
            }
            //else
            //    Response.Write(dir + " 该文件夹不存在"); //如果文件夹不存在则提示 

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
            string dbPath = string.Empty; //cookies.sqlite文件路径
            DirectoryInfo di = new DirectoryInfo(System.Environment.GetEnvironmentVariable("AppData") + @"\Mozilla\Firefox\Profiles\"); //@表示/不表示转义字符
            DirectoryInfo[] dirs = di.GetDirectories();//获取子文件夹列表 
            if (dirs != null) { dbPath = dirs[0].FullName + "\\cookies.sqlite"; }   //获取firefox的cookie地址,由于cookies.sqlite有权限控制
            //所以软件要在管理员权限下运行
            //dbPath = "D:\\a.sqlite";
            try
            {

                if (dbPath == null)
                {
                    strcallback = "打开Firefox Cookies文件错误...";

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
                //sqlconn.Open();      //打开连接
                string CommandText = "delete from moz_cookies";

                SQLiteCommand SQLiteCommand = new SQLiteCommand(CommandText, conn);  //执行命令
                SQLiteCommand.ExecuteNonQuery();

                conn.Close();

                strcallback = "Cookies成功删除...";
            }
            catch (SQLiteException sqlex)
            {
                strcallback = sqlex.Message;
            }
            return strcallback;
        }
    }
}
