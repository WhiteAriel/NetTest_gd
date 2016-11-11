using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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

using System.Reflection;


namespace NetTest
{
    public partial class FlvTest : DevExpress.XtraEditors.XtraUserControl
    {

        IniFile inis = new IniFile(Application.StartupPath + "\\settings.ini");  //ini class
        private int iTest = 0;              //loop count
        private int intIndex;

        public bool flagAdapter = false;
        
        //stop perf
        public string strPlayer;
        public long iProMem = 0;
        public long iProLength = 0;
        public int iProCPU = 3;

        public string strFile = "";
        public int iDevice;
        public PcapDevice device;
        private DateTime dtStart;                  //start time
        
        private StringBuilder strbFile = new StringBuilder();    //contents of log
        private string strLogFile;                    //log file
        private int intCheckContinuous;
        //private int iTimeThrehold = 0;
        private int iTimeLast = 0;
        private int iBool = 0;
        private int iNumContinuous = 0;
        private bool DoTest;
        private string strTempURL;        //random test
        //private string DB_NAME = null;



        [DllImport("user32.dll", EntryPoint = "GetWindowThreadProcessId", SetLastError = true,
     CharSet = CharSet.Unicode, ExactSpelling = true,
     CallingConvention = CallingConvention.StdCall)]
        private static extern long GetWindowThreadProcessId(long hWnd, long lpdwProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern long SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongA", SetLastError = true)]
        private static extern long GetWindowLong(IntPtr hwnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongA", SetLastError = true)]
        private static extern long SetWindowLong(IntPtr hwnd, int nIndex, long dwNewLong);
        //private static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern long SetWindowPos(IntPtr hwnd, long hWndInsertAfter, long x, long y, long cx, long cy, long wFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool MoveWindow(IntPtr hwnd, int x, int y, int cx, int cy, bool repaint);

        [DllImport("user32.dll", EntryPoint = "PostMessageA", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hwnd, uint Msg, long wParam, long lParam);

        [DllImport("user32.dll")]
        public static extern void SetForegroundWindow(IntPtr hwnd);


      

        private BackgroundWorker m_AsyncWorker = new BackgroundWorker();


        /******************************************************************************
           init the user components FlvTest 
        /*******************************************************************************/
        public FlvTest()
        {
            InitializeComponent();
            m_AsyncWorker.WorkerSupportsCancellation = true;
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
                string a = inis.IniReadValue("Flv", "CheckContinuous");
                intCheckContinuous = int.Parse(a);
            }
            catch { MessageBox.Show("请检查连续测试参数的设置！"); }
            string ite = inis.IniReadValue("Flv", "Adapter");
            iDevice = int.Parse(ite);
            ite = inis.IniReadValue("Flv", "TimeLast");
            iTimeLast = int.Parse(ite);
            ite = inis.IniReadValue("Flv", "NumContinuous");
            iNumContinuous = Convert.ToInt16(ite);
            //ite = inis.IniReadValue("Flv", "CheckCookies");
            //iBool = Convert.ToInt16(ite);
            strPlayer = inis.IniReadValue("Flv", "Player") + "\\VlcDialog_dbg.exe";
            strTempURL = inis.IniReadValue("Flv", "UrlPage");
        }

        /******************************************************************************
           interrupt the test whether loop or not 
        /*******************************************************************************/
        private void btnFlvStop_Click(object sender, EventArgs e)
        {
            this.btnFlvStart.Enabled = true;
            this.btnFlvStop.Enabled = false;
            device.PcapStopCapture();
            this.ClosePlayer();
            this.memoPcap.Items.Clear();
            this.memoPcap.Items.Add("测试被用户中断\n");
            DoTest = false;
            if (!DoTest)
            {
                strbFile.Append("测试被用户中断\n");

                if (File.Exists(this.strLogFile))
                {
                    File.Delete(this.strLogFile);
                }
                //File.Move(inis.IniReadValue("Flv", "Player") + "\\result0.txt", this.strLogFile);

                //File.Move("result0.txt", this.strLogFile);

                this.memoPcap.Items.Add("抓包文件: " + strFile + " 创建");
                if (File.Exists("result0.txt"))
                {
                    File.Copy("result0.txt", this.strLogFile);
                    this.memoPcap.Items.Add("日志文件: " + strLogFile);
                }
                else
                {
                    this.memoPcap.Items.Add("日志文件生成失败");
                }
            }
            iTest = 0;
            this.timFlv.Stop();
            this.timer1.Stop();
            this.DoTest = false;
        }

        /******************************************************************************
           start the test single or loop, the main process call WebTesting() 
        /*******************************************************************************/
        private void btnFlvStart_Click(object sender, EventArgs e)
        {
            //iTimeThrehold = 0;
            if (strbFile.Length > 0) strbFile.Remove(0, strbFile.Length);
            
            if (intCheckContinuous==0) this.iTest = 0;
            if(iTest==0) intIndex = int.Parse(inis.IniReadValue("Flv", "UrlIndex"));
            iProMem = 0;
            this.btnFlvStart.Enabled = false;
            this.timer1.Stop();
            this.timer1.Enabled = false;

            this.btnFlvStop.Enabled = true;

            strFile = inis.IniReadValue("Flv", "Path") + "\\Flv-" + "-" + DateTime.Now.Year.ToString() + "-"
                + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "-"
                + DateTime.Now.Hour.ToString() + "-" + DateTime.Now.Minute.ToString() + "-"
                + DateTime.Now.Second.ToString();// +".cap";
            //strLogFile = strFile + ".xls";
            strLogFile = strFile + ".txt";
            strFile = strFile + ".cap";

            if (!Directory.Exists(inis.IniReadValue("Flv", "Path"))) Directory.CreateDirectory(inis.IniReadValue("Flv", "Path"));
            
            this.FlvTesting();
        }

        /******************************************************************************
           the test itself
        /*******************************************************************************/
        private void FlvTesting()
        {
            this.DoTest = true;

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
                this.ClosePlayer();
                if (iBool > 0)
                {
                    if (!File.Exists(strPlayer))
                    {
                        this.memoPcap.Items.Add("测试中断，无法打开播放器");
                        if (strbFile.Length > 0) strbFile.Remove(0, strbFile.Length);
                        return;
                    }                  
                }
            }
            catch (Exception ex)
            {
                this.memoPcap.Items.Add(ex.Message);
            }

            PcapDeviceList devices = SharpPcap.GetAllDevices();

            device = devices[iDevice];
            string ip = device.PcapIpAddress;


            this.memoPcap.Items.Add("相关播放器关闭");
            this.memoPcap.Items.Add("网卡: " + device.PcapDescription);
            strbFile.Append("网卡: " + device.PcapDescription + "\r\n");
            
            this.memoPcap.Items.Add("IP地址: " + device.PcapIpAddress);
            strbFile.Append("IP地址: " + device.PcapIpAddress + "\r\n");
            
            Thread.Sleep(100);
            this.dtStart = DateTime.Now;
            this.memoPcap.Items.Add("测试开始时间: " + dtStart.ToString());
            strbFile.Append("测试开始时间: " + dtStart.ToString() + "\r\n");
            
            //Register our handler function to the 'packet arrival' event
            device.PcapOnPacketArrival +=
                new SharpPcap.PacketArrivalEvent(device_PcapOnPacketArrival);

            //Open the device for capturing
            //true -- means promiscuous mode
            //1000 -- means a read wait of 1000ms

            device.PcapOpen(true, 100);
            //device.PcapSetFilter("(tcp or udp) and host " + ip);
            device.PcapSetFilter("host " + ip);
            device.PcapDumpOpen(strFile);
            if (!m_AsyncWorker.IsBusy)
            {
                m_AsyncWorker.RunWorkerAsync();
            }
            this.timFlv.Enabled = true;
            
        }

        /******************************************************************************
           trigger for complete a test and judge a loop 
        /*******************************************************************************/
        private void timFlv_Tick(object sender, EventArgs e)
        {
            if (strPlayer == "") return;
            string strProcessFile = "VlcDialog_dbg";

            Process[] p = Process.GetProcessesByName(strProcessFile);
            if (p.Length > 0)
                return;

            DateTime dtEnd = DateTime.Now;
            TimeSpan ts = dtEnd - dtStart;
            float ts2 = ts.Seconds + (float)ts.Milliseconds / 1000;
            device.PcapStopCapture();



            this.memoPcap.Items.Add("测试结束,耗时 " + ts.Minutes + "分 " + ts2.ToString() + "秒");
            strbFile.Append("测试结束,耗时 " + ts.Minutes + "分 " + ts2.ToString() + "秒" + "\r\n");
            
            strbFile.Append("抓包文件: " + strFile + "\r\n");

            this.DoTest = false;

            if (File.Exists(this.strLogFile))
            {
                File.Delete(this.strLogFile);
            }
           
            this.memoPcap.Items.Add("抓包文件: " + strFile + " 创建");

            if (File.Exists("result0.txt"))
            {
                File.Copy("result0.txt", this.strLogFile);
                this.memoPcap.Items.Add("日志文件: " + strLogFile);
            }
            else
            {
                this.memoPcap.Items.Add("日志文件生成失败");
            }
            

            this.timFlv.Enabled = false;

            if (intCheckContinuous > 0 && iTest < iNumContinuous)
            {

                this.timer1.Interval = Convert.ToInt32(inis.IniReadValue("Flv", "Interval")) * 1000;
                this.timer1.Enabled = true;
                this.timer1.Start();


            }
            else
            {
                this.timer1.Enabled = false;
                this.btnFlvStart.Enabled = true;
                this.btnFlvStop.Enabled = false;
                this.iTest = 0;
                this.memoPcap.Items.Add("---------------测试完成---------------");
            }
        }

        /******************************************************************************
           worker
        /*******************************************************************************/
        private void bwAsync_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {

                IntPtr appWin;

                string strProcessFile = strPlayer;
                string testURL = inis.IniReadValue("Flv", "UrlPage");
                ProcessStartInfo psi = new ProcessStartInfo(strPlayer);

                psi.FileName = strProcessFile;
                //psi.Arguments = testURL;
                psi.RedirectStandardInput = true;
                psi.RedirectStandardOutput = true;
                psi.UseShellExecute = false;
                psi.WindowStyle = ProcessWindowStyle.Hidden;

                Process ps = new Process();
                ps.StartInfo = psi;
                ps.EnableRaisingEvents = true;
                //ps.Exited +=new EventHandler(ps_Exited);           

                this.memoPcap.Items.Add("播放器: " + strPlayer);
                strbFile.Append("播放器: " + strPlayer + "\r\n");
                
                this.memoPcap.Items.Add("页面: " + testURL);
                strbFile.Append("页面: " + testURL + "\r\n");
                
                ps.Start();

                if (ps.WaitForInputIdle())
                {

                    while (ps.MainWindowHandle.ToInt32() == 0)
                    {
                        Thread.Sleep(100);
                        ps.Refresh();//必须刷新状态才能重新获得
                    }
                    ps.StartInfo = psi;

                    // Get the main handle
                    appWin = ps.MainWindowHandle;
                    
                    // Put it into this form
                    SetParent(appWin, this.splitContainerControl1.Panel1.Handle);
                    // Move the window to overlay it on this window
                    //MoveWindow(appWin, 0, 0, _mainParent.dockPanel1.Width, _mainParent.dockPanel1.Height, true);
                    MoveWindow(appWin, 0, 0, this.splitContainerControl1.Panel1.Width, this.splitContainerControl1.Panel1.Height, true);

                }


            }
            catch (Exception ex)
            {
                this.memoPcap.Items.Add(ex.Message);
                this.btnFlvStart.Enabled = true;
                this.btnFlvStop.Enabled = false;
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
                this.memoPcap.Items.Add("播放器错误");
                
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
           close the same player before a test start
        /*******************************************************************************/
        private void ClosePlayer()
        {
            //player
            if (strPlayer == "") return;
            string strProcessFile = "VlcDialog_dbg";
 
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
    }
}

