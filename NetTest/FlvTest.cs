using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using System.Threading;
using System.IO;
using System.Management;
using System.Collections;
using System.Diagnostics;
using Finisar.SQLite;
using System.Runtime.InteropServices;
using System.Reflection;
using System.IO.Pipes;
using System.Drawing.Drawing2D;
using Dundas.Charting.WinControl;
using SharpPcap;
using PacketDotNet;
using SharpPcap.LibPcap;
using System.Net;
using System.Net.Sockets;
using System.Timers;
using MultiMySQL;
using NetLog;

namespace NetTest
{

    public partial class FlvTest : DevExpress.XtraEditors.XtraUserControl
    {
        public static IniFile inis = new IniFile(Application.StartupPath + "\\settings.ini");  //ini class
        public static IniFile inisvlcout = new IniFile(Application.StartupPath + "\\vlc.ini"); //和一些库行为有关，播放器默认读\\VideoPlayer\\vlc.ini，评分读\\vlc.ini
        public static IniFile inisvlc = new IniFile(Application.StartupPath + "\\VideoPlayer\\vlc.ini"); //ini class
        //IniFile inisref = new IniFile(Application.StartupPath + "\\RefTool" + "\\referencesetup.ini");

        public bool taskon = false;    //表示任务没有运行
        public bool serverTest = false;   //表示执行的是服务器任务还是终端自己的任务


        ArrayList player_list = new ArrayList();    //保存播放器进程信息
        ArrayList port_list = new ArrayList();      //保存播放器的端口号

        private int iTest = 0;              //连续播放了多少次
        private static int intCheckContinuous;     //是否连续播放
        private static int iNumContinuous = 0;     //连续播放总次数

        public string strPlayer;            //播放器完整名（含路径）  
        public string strPcapFile = "";     //抓包文件名
        public int iDevice = 0;                 //网卡索引
        public int lastPlayerIndex = 0;
        public LibPcapLiveDevice device;
        private ArrayList StartTimeList = new ArrayList();           //开始测试的时间

        private string strPlayFile;         //qoe文件
        private string strLogResult = null;
        private string strXlsLogFile;       //log file path (xls file(xls格式) path)
        private ArrayList strbFileList = new ArrayList();    //contents of log file (content of xls file)
        private StringBuilder ScoreParam = new StringBuilder();  //百分制参数提取

        public static bool DoTest = false;
        public bool IsStartPlay;
        public bool StartStopTest;

        private static PacketCap pcap_packet;


        private MySQLInterface mysqlTest = null;
        private bool mysqlTestFlag = false;
        //判断一定时间内是否进程管道有数据过来的定时器
        System.Timers.Timer myTimer;


        //播放视频文件
        private BackgroundWorker m_AsyncWorker = new BackgroundWorker();
        //管道线程列表，每个播放器对应一个单独的管道
        ArrayList pipeList = new ArrayList();
        //抓包进程
        private BackgroundWorker m_AsyncWorker_cap = new BackgroundWorker();

        private double Definition;          //清晰度
        private double Brightness;          //亮度
        private double Color;               //色度
        private double Saturation;          //饱和度
        private double Contrast;            //对比度

        private int Static = 0;              //静帧
        private int Skip = 0;                //跳帧
        private int Blur = 0;                //模糊

        //[DllImport("user32.dll", EntryPoint = "GetWindowThreadProcessId", SetLastError = true,CharSet = CharSet.Unicode, ExactSpelling = true,CallingConvention = CallingConvention.StdCall)]
        //private static extern long GetWindowThreadProcessId(long hWnd, long lpdwProcessId);

        //[DllImport("user32.dll", SetLastError = true)]
        //private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        //[DllImport("user32.dll", EntryPoint = "GetWindowLongA", SetLastError = true)]
        //private static extern long GetWindowLong(IntPtr hwnd, int nIndex);

        //[DllImport("user32.dll", EntryPoint = "SetWindowLongA", SetLastError = true)]
        //private static extern long SetWindowLong(IntPtr hwnd, int nIndex, long dwNewLong);

        //[DllImport("user32.dll", SetLastError = true)]
        //private static extern long SetWindowPos(IntPtr hwnd, long hWndInsertAfter, long x, long y, long cx, long cy, long wFlags);

        //[DllImport("user32.dll", EntryPoint = "PostMessageA", SetLastError = true)]
        //private static extern bool PostMessage(IntPtr hwnd, uint Msg, long wParam, long lParam);

        //[DllImport("user32.dll")]
        //public static extern void SetForegroundWindow(IntPtr hwnd);

        //[DllImport("user32.dll")]
        //public static extern long SetWindowPos(long hwnd, long hWndInsertAfter, long X, long y, long cx, long cy, long wFlagslong);

        //[DllImport("user32.dll", CharSet = CharSet.Auto)]
        //public static extern IntPtr SendMessage(IntPtr hwnd, int msg, int wParam, int lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern long SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool MoveWindow(IntPtr hwnd, int x, int y, int cx, int cy, bool repaint);

        [DllImport("VideoScore.dll")]
        public static extern double vScore(string strlog, string result, int width, int height);

        //[DllImport("CapturePacket.dll")]
        //public static extern int StartCapture(int intf, string filename);

        //[DllImport("CapturePacket.dll")]
        //public static extern void StopCapture();

        //[DllImport("CapturePacket.dll")]
        //public static extern ushort GetPortByPID(int pid);

        [DllImport("CapturePacket.dll")]
        public static extern int StartDispatch(int n, string ini);

        [DllImport("user32.dll", EntryPoint = "ShowWindow", SetLastError = true)]
        public static extern bool ShowWindow(IntPtr hwnd, uint cmdshow);

        /******************************************************************************
           init the user components FlvTest 
        /*******************************************************************************/
        public FlvTest()
        {
            InitializeComponent();

            //将几个自定义函数的句柄分配给BackgroundWorker的DoWork、RunWorkerCompleted事件
            m_AsyncWorker.WorkerSupportsCancellation = true;
            m_AsyncWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bwAsync_RunWorkerCompleted);
            m_AsyncWorker.DoWork += new DoWorkEventHandler(bwAsync_DoWork);

            m_AsyncWorker_cap.WorkerSupportsCancellation = true;
            m_AsyncWorker_cap.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bwAsync_cap_RunWorkerCompleted);
            m_AsyncWorker_cap.DoWork += new DoWorkEventHandler(bwAsync_cap_DoWork);

            Control.CheckForIllegalCrossThreadCalls = false;
            DoTest = false;


            //数据库对象初始化
            mysqlTest = new MySQLInterface(inis.IniReadValue("Mysql", "serverIp"), inis.IniReadValue("Mysql", "user"), inis.IniReadValue("Mysql", "passwd"));
            if (mysqlTest.MysqlInit(inis.IniReadValue("Mysql", "dbname")))
                mysqlTestFlag = true;
            this.RealTimechart();

        }

        /******************************************************************************
           init the paras or settings 
        /*******************************************************************************/
        public void Init()
        {
            //清空表图
            this.InitChart();

            //读取连续测试信息
            try
            {
                string cc = inis.IniReadValue("Flv", "CheckContinuous");
                if (cc != null)
                    intCheckContinuous = int.Parse(cc);
                else
                    iNumContinuous = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("请检查连续测试参数的设置！");
                Log.Error(Environment.StackTrace, ex);
            }

            if (intCheckContinuous > 0)     //是连续测试
            {
                string nc = inis.IniReadValue("Flv", "NumContinuous");
                if (nc != null)
                    iNumContinuous = int.Parse(nc);
                else
                    iNumContinuous = 0;
            }
            else
                iNumContinuous = 0;

            //获取网卡信息
            if (inis.IniReadValue("Flv", "Envir").Equals("web"))
            {
                string iadapter = inis.IniReadValue("Flv", "Adapter");
                var devices2 = LibPcapLiveDeviceList.Instance;
                iDevice = int.Parse(iadapter);
                if (iDevice < devices2.Count)
                {
                    try
                    {
                        //pcap_packet = new PacketCap(iDevice);
                    }
                    catch (Exception ex)
                    {
                        Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
                        MessageBox.Show("网卡序号:" + iDevice.ToString());
                    }
                }
                else
                {
                    MessageBox.Show("请首先正确选择网卡，再重新进行测试！");
                    return;
                }
            }

            //播放器所在目录
            inis.IniWriteValue("Flv", "Player", Application.StartupPath + "\\VideoPlayer");
            //获取播放器信息
            strPlayer = inis.IniReadValue("Flv", "Player") + "\\VLCDialog.exe";
            this.dataGridView1.Visible = false;

            if (iDevice < 0)
            {
                iDevice = 0;
            }
            pcap_packet = new PacketCap(iDevice);

            //命名管道定时器
            myTimer = new System.Timers.Timer(1000);//定时周期50毫秒
            myTimer.Elapsed += myTimer_Elapsed;//到2秒了做的事件
            myTimer.AutoReset = false; //是否不断重复定时器操作

        }


        //50毫秒后的定时器操作
        void myTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            gaugeContainer1.Values["Default"].Value = 100;
        }

        /******************************************************************************
        pipe worker
        /*******************************************************************************/
        private void bwAsync_pipe_DoWork(object sender, DoWorkEventArgs e)     //完成后引发bwAsync_pipe_RunWorkerCompleted
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            bool createVideoPara = false;
            if (mysqlTestFlag)
                createVideoPara = mysqlTest.CreatVideoPara();
            videoPara vp = new videoPara();
            string ipandtype = inis.IniReadValue("Task", "currentVideoId") + "#" + "Video";
            try
            {
                //新建管道
                NamedPipeServerStream pipeServer = new NamedPipeServerStream("MyPipe", PipeDirection.InOut, 10);
                //等待连接
                pipeServer.WaitForConnection();
                //读管道流
                StreamReader sr = new StreamReader(pipeServer);

                //记录管道传送数据
                char[] buffer = new char[256];
                int n;

                string[] arrErro = new string[5];
                Queue<string> qTime = new Queue<string>();
                Queue<int> qStatic = new Queue<int>();     //静止
                Queue<int> qSkip = new Queue<int>();      //跳帧
                Queue<int> qBlur = new Queue<int>();      //模糊

                bool cleared = false;
                while (pipeServer.IsConnected)
                {

                    n = sr.Read(buffer, 0, buffer.Length);  //这个Read是个阻塞函数，很明显在该程序里播放器回传的抽样参数间隔是大于while循环的，所以网络在中断的时候Read函数是不返回的
                    myTimer.Enabled = false; //喂狗，时间超过1s认为视频不播放
                    if (n == 0)
                    {
                        if (pipeServer != null)
                        {
                            if (pipeServer.IsConnected)
                            {
                                pipeServer.Disconnect();
                                Thread.Sleep(1000);//必须延迟，等待管道内的while循环探测到连接断开，不再进行数据读写
                            }
                            pipeServer.Close();
                        }

                        return;
                    }

                    string x = null;
                    for (int i = 0; i <= n - 1; i++)
                    {
                        x += buffer[i].ToString();      //x为记录数据的长字符串
                    }

                    char[] seper = { '\t', ' ' };
                    string[] ld = x.Split(seper);      //将x中数据按"\t"作为分隔，提取地数据存入ld数组

                    if (ld.Length >= 13)          //人为设置，假设ld中数据长度大于13则开始播放
                    {
                        myTimer.Enabled = true;  //开启看门狗
                        IsStartPlay = true;

                        if (IsStartPlay && !vlcStart)
                        {
                            startTime = DateTime.Now;
                            vlcStart = true;
                        }

                        //仪表和曲线图清理
                        if (!cleared)
                        {
                            chart1.Invoke(clearDataDel, this.chart1);
                            chart2.Invoke(clearDataDel, this.chart2);
                            chart3.Invoke(clearDataDel, this.chart3);
                            chart4.Invoke(clearDataDel, this.chart4);
                            chart5.Invoke(clearDataDel, this.chart5);
                            this.Static = 0;
                            this.Skip = 0;
                            this.Blur = 0;
                            cleared = true;
                        }

                        //更新曲线图，获取数据
                        this.Definition = Convert.ToDouble(ld[7 - 1]);        //ld[7-1]: ld数组第一行第七列的元素
                        this.Brightness = Convert.ToDouble(ld[8 - 1]);
                        this.Color = Convert.ToDouble(ld[9 - 1]);
                        this.Saturation = Convert.ToDouble(ld[10 - 1]);
                        this.Contrast = Convert.ToDouble(ld[11 - 1]);

                        vp.clarity = this.Definition;
                        vp.brightness = this.Brightness;
                        vp.Chroma = this.Color;
                        vp.saturation = this.Saturation;
                        vp.Contrast = this.Saturation;

                        chart1.Invoke(addDataDel, this.chart1, this.Definition);     //清晰度
                        chart2.Invoke(addDataDel, this.chart2, this.Brightness);    //亮度
                        chart3.Invoke(addDataDel, this.chart3, this.Color);          //色度
                        chart4.Invoke(addDataDel, this.chart4, this.Saturation);    //饱和度
                        chart5.Invoke(addDataDel, this.chart5, this.Contrast);      //对比度
                        frameNum++;

                        //更新仪表盘（队列操作）
                        DateTime time = DateTime.Now;
                        arrErro[0] = time.ToLongTimeString();

                        int offset = 2;
                        for (int i = offset; i <= offset + 3; i++)
                        {
                            arrErro[i + 1 - offset] = ld[i];
                        }

                        qTime.Enqueue(arrErro[0]);
                        qStatic.Enqueue(Convert.ToInt32(arrErro[1]));
                        qSkip.Enqueue(Convert.ToInt32(arrErro[2]));
                        qBlur.Enqueue(Convert.ToInt32(arrErro[3]));

                        this.Static += Convert.ToInt32(arrErro[1]);
                        this.Skip += Convert.ToInt32(arrErro[2]);
                        this.Blur += Convert.ToInt32(arrErro[3]);

                        string videoInfo = null;
                        if (qTime.Count > 0)
                        {
                            double thrSencond = 5;
                            for (; Convert.ToDateTime(qTime.Peek()).AddSeconds(thrSencond) <= Convert.ToDateTime(arrErro[0]) && qTime.Count > 1; qTime.Dequeue())
                            {
                                this.Static -= qStatic.Dequeue();
                                this.Skip -= qSkip.Dequeue();
                                this.Blur -= qBlur.Dequeue();
                            }
                            gaugeContainer1.Values["Default"].Value = vp.screenstatic = ((double)this.Static) / qTime.Count * 100;
                            gaugeContainer2.Values["Default"].Value = vp.screenjump = ((double)this.Skip) / qTime.Count * 100;
                            gaugeContainer3.Values["Default"].Value = vp.screenfuzzy = ((double)this.Blur) / qTime.Count * 100;

                            videoInfo = "3-" + this.Definition.ToString() + "-" + this.Brightness + "-" + this.Color + "-" + this.Saturation + "-" +
                        this.Contrast + "-" + (Convert.ToInt32(((double)this.Static) / qTime.Count * 100)).ToString() + "-" + (Convert.ToInt32(((double)this.Skip) / qTime.Count * 100)).ToString() +
                        "-" + (Convert.ToInt32(((double)this.Blur) / qTime.Count * 100)).ToString();

                            //记录插入mysql表中VideoPara
                            if (createVideoPara == true && serverTest)
                                mysqlTest.VideoParaInsertMySQL(ipandtype, vp);
                        }
                        else
                        {
                            videoInfo = "3-" + this.Definition.ToString() + "-" + this.Brightness + "-" + this.Color + "-" + this.Saturation + "-" +
                        this.Contrast + "-" + "0" + "-" + "0" + "-" + "0";
                        }

                    }

                    if (bw.CancellationPending)
                    {
                        e.Cancel = true;
                        if (pipeServer != null)
                        {
                            if (pipeServer.IsConnected)
                            {
                                pipeServer.Disconnect();
                                Thread.Sleep(1000);//必须延迟，等待管道内的while循环探测到连接断开，不再进行数据读写
                            }
                            pipeServer.Close();

                        }
                        //释放定时器资源
                        myTimer.Close(); //释放Timer占用的资源
                        myTimer.Dispose();
                        return;
                    }
                }
            }
            catch (IOException ioEx)
            {
                DisplayState(ioEx.Message);

            }
            catch (Exception ex)
            {//    此处很容易出错，认真研究
                Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
                DisplayState(" 命名管道被主程序销毁！");
                return;
            }
            //释放定时器资源
            myTimer.Close(); //释放Timer占用的资源
            myTimer.Dispose();
        }

        /******************************************************************************
           pipe worker instance complete
        /*******************************************************************************/
        private void bwAsync_pipe_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                DisplayState("命名管道错误");
                return;
            }

            // Check to see if the background process was cancelled.
            if (e.Cancelled)
            {
                //if (this.m_pipeServer != null)
                //    this.m_pipeServer.Close();
                DisplayState("命名管道撤销");
                return;
            }
        }

        /******************************************************************************
           pcap worker
        /*******************************************************************************/
        private void bwAsync_cap_DoWork(object sender, DoWorkEventArgs e)      //完成后引发bwAsync_cap_RunWorkerCompleted
        {                                                                                                                   //抓包线程
            try
            {

                pcap_packet.Start(strPcapFile);
                //StartCapture(iDevice, strPcapFile);
            }
            catch (Exception ex)
            {
                DisplayState(ex.Message);
                //btnFlvStart.Enabled = true;
                //this.btnFlvStop.Enabled = false;
                Log.Error(Environment.StackTrace, ex);
                return;
            }

        }

        /******************************************************************************
          pcap worker instance complete
        /*******************************************************************************/
        private void bwAsync_cap_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                DisplayState("抓包错误\r\n");
                return;
            }

            // Check to see if the background process was cancelled.
            if (e.Cancelled)
            {
                pcap_packet.Stop();
                //StopCapture();
                DisplayState("抓包任务撤销\r\n");
                return;
            }

        }

        //Thread auotoStopThread = null;

        /******************************************************************************
           start the test single or loop, the main process call WebTesting() 
        /*******************************************************************************/
        public void StartServerTaskFunc()   //兼容服务器任务和终端任务，如果终端任务在执行，服务器任务等待
        {
            while (true)
            {
                if (!taskon)
                {
                    Log.Info("It's serverTask!");
                    this.btnFlvStart.Enabled = false;
                    this.btnFlvStop.Enabled = false;   //服务器任务不允许终端停止
                    this.taskon = true;

                    if (inis.IniReadValue("Flv", "urlPage").Equals(""))
                    {
                        DisplayState("错误的视频解析地址,请重新设置！！");
                        Log.Warn("错误的视频解析地址,请重新设置!");
                        this.btnFlvStart.Enabled = true;
                        this.btnFlvStop.Enabled = false;
                        return;
                    }

                    this.iTest++;

                    timer1.Enabled = true;
                    timer1.Start();

                    vlcStart = false;
                    startTime = DateTime.Now;

                    frameNum = 1;

                    //播放器还未开始播放，也即播放器画面中还没有数据
                    this.IsStartPlay = false;
                    //设置停止测试的标示位
                    this.StartStopTest = false;

                    //指定播放日志文件、xls文件、抓包文件名
                    string strtmp = null;

                    string resultPath = "";
                    resultPath = inis.IniReadValue("Flv", "PlayPcapPath");

                    if (!Directory.Exists(resultPath))
                    {
                        try
                        {
                            Directory.CreateDirectory(resultPath);
                        }

                        catch (System.Exception ex)
                        {
                            Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
                            resultPath = "C:\\TestLog";
                            Directory.CreateDirectory(resultPath);
                        }
                    }

                    strtmp = resultPath + "\\" + "VOD-" + inis.IniReadValue("Flv", "Envir") + "-";
                    strtmp += DateTime.Now.Year.ToString() + "-"
                        + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "-"
                        + DateTime.Now.Hour.ToString() + "-" + DateTime.Now.Minute.ToString() + "-"
                        + DateTime.Now.Second.ToString();
                    strPlayFile = strtmp + "_" + iTest + ".txt";
                    string str = "test" + iTest;
                    string pcapfile = strtmp + "_" + iTest + ".pcap";
                    inisvlc.IniWriteValue("result", str, strPlayFile);
                    inisvlcout.IniWriteValue("result", str, strPlayFile);
                    inis.IniWriteValue("result", str, pcapfile);

                    strXlsLogFile = strtmp + "_" + iTest + ".xlsx";
                    inis.IniWriteValue("xlsxLogFile", str, strXlsLogFile);

                    strLogResult = strtmp + "_" + iTest + "-log.txt";
                    inis.IniWriteValue("LogResult", str, strLogResult);///播放日志+无参考打分
                    ///
                    //如果第一次点开始测试
                    if (iTest == 1)
                    {
                        strPcapFile = strtmp + "_temp.pcap";
                        inis.IniWriteValue("Flv", "PcapFile", strPcapFile);

                        this.dataGridView1.Rows.Clear();
                        memoPcap.Items.Clear();
                    }
                    this.ClearDns();
                    this.FlvTesting();
                    break;
                }
                else
                    Thread.Sleep(2000);  //wait 2s if handon task is running 
            }
        }

        public void StartTerminalTaskFunc()   //兼容服务器任务和终端任务，如果终端任务在执行，服务器任务等待
        {
                    Log.Info("It's serverTask!");
                    this.btnFlvStart.Enabled = false;
                    this.btnFlvStop.Enabled = true;     //终端任务可以暂停
                    this.taskon = true;

                    if (inis.IniReadValue("Flv", "urlPage").Equals(""))
                    {
                        DisplayState("错误的视频解析地址,请重新设置！！");
                        Log.Warn("错误的视频解析地址,请重新设置!");
                        this.btnFlvStart.Enabled = true;
                        this.btnFlvStop.Enabled = false;
                        return;
                    }

                    this.iTest++;

                    timer1.Enabled = true;
                    timer1.Start();
                    vlcStart = false;
                    startTime = DateTime.Now;
                    frameNum = 1;

                    //播放器还未开始播放，也即播放器画面中还没有数据
                    this.IsStartPlay = false;
                    //设置停止测试的标示位
                    this.StartStopTest = false;

                    //指定播放日志文件、xls文件、抓包文件名
                    string strtmp = null;

                    string resultPath = "";
                    resultPath = inis.IniReadValue("Flv", "PlayPcapPath");

                    if (!Directory.Exists(resultPath))
                    {
                        try
                        {
                            Directory.CreateDirectory(resultPath);
                        }

                        catch (System.Exception ex)
                        {
                            Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
                            resultPath = "C:\\TestLog";
                            Directory.CreateDirectory(resultPath);
                        }
                    }

                    strtmp = resultPath + "\\" + "VOD-" + inis.IniReadValue("Flv", "Envir") + "-";
                    strtmp += DateTime.Now.Year.ToString() + "-"
                        + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "-"
                        + DateTime.Now.Hour.ToString() + "-" + DateTime.Now.Minute.ToString() + "-"
                        + DateTime.Now.Second.ToString();
                    strPlayFile = strtmp + "_" + iTest + ".txt";
                    string str = "test" + iTest;
                    string pcapfile = strtmp + "_" + iTest + ".pcap";
                    inisvlc.IniWriteValue("result", str, strPlayFile);
                    inisvlcout.IniWriteValue("result", str, strPlayFile);
                    inis.IniWriteValue("result", str, pcapfile);

                    strXlsLogFile = strtmp + "_" + iTest + ".xlsx";
                    inis.IniWriteValue("xlsxLogFile", str, strXlsLogFile);

                    strLogResult = strtmp + "_" + iTest + "-log.txt";
                    inis.IniWriteValue("LogResult", str, strLogResult);///播放日志+无参考打分
                    ///
                    //如果第一次点开始测试
                    if (iTest == 1)
                    {
                        strPcapFile = strtmp + "_temp.pcap";
                        inis.IniWriteValue("Flv", "PcapFile", strPcapFile);

                        this.dataGridView1.Rows.Clear();
                        memoPcap.Items.Clear();
                    }
                    this.ClearDns();
                    this.FlvTesting();
        }

        private void btnFlvStart_Click(object sender, EventArgs e)
        {
            //startFunc();
            StartTerminalTaskFunc();
        }

        /******************************************************************************
           interrupt the test whether loop or not 
        /*******************************************************************************/
        public void StopServerTaskFunc()
        {
            //stop button的设置
            this.btnFlvStart.Enabled = true;
            this.btnFlvStop.Enabled = false;
            //设置停止测试标识位
            StartStopTest = true;

            //停止播放，关掉播放器，退出抓包、播放、管道进程
            this.StopClosePlayer();

            Thread.Sleep(500);

            inis.IniWriteValue("Flv", "counts", iTest.ToString());
            //播放次数置零
            iTest = 0;
            timer1.Stop();
            timer1.Enabled = false;

            //memoPcap信息输出
            DateTime dtEnd = DateTime.Now;
            memoPcap.Items.Clear();

            //清空检测静帧跳帧模糊的表盘
            this.ClearGuageData();


            if (inis.IniReadValue("Flv", "Envir").Equals("web"))
            {
                //无参考模式的打分，输出打分结果
                for (int i = 0; i <= port_list.Count; i++)
                {
                    //将播放器的端口号写入配置文件，供分包程序使用
                    //ushort port = (ushort)(port_list[i]);
                    //inis.IniWriteValue("port", "test" + (i + 1), Convert.ToString(port));

                    DateTime dtStart = (DateTime)(StartTimeList[i]);
                    StringBuilder strbFile = (StringBuilder)(strbFileList[i]);
                    //string strpcap = inis.IniReadValue("result", "test" + (i + 1));
                    string strpcap = inis.IniReadValue("Flv", "PcapFile");
                    TimeSpan ts = dtEnd - dtStart;
                    float ts2 = ts.Seconds + (float)ts.Milliseconds / 1000;

                    strbFile.Append("测试结束,耗时 " + ts.Minutes + "分 " + ts2.ToString() + "秒" + "\r\n");
                    strbFile.Append("抓包文件: " + strpcap + "\r\n");
                    DisplayState("抓包文件: " + strpcap + "创建\r\n");

                    //对参数文件处理，给出评分
                    string strfScore = "qoe_score.txt";   //这个文件只有在正常测试的时候才会有
                    if (File.Exists(strfScore))     //删除的是上一次测试的qoe_score.txt文件
                    {
                        File.Delete(strfScore);
                    }
                    Thread.Sleep(500);

                    //打分
                    try
                    {
                        double score = this.UnRefScore(i + 1, strfScore);      //成功调用UnRefScore函数后会生成本次测试的qoe_score.txt文件
                        StreamWriter ResultTmp = new StreamWriter(File.Create("ResultTxt.tmp"), Encoding.Default);
                        //临时总结报告文件，用于满足特定的格式压入数据库
                        ResultTmp.Write("Index\tColumn\tValue\r\n");
                        int resultIndex = 0;
                        if (score >= 0 && score <= 10)
                        {
                            int index = this.dataGridView1.Rows.Add();
                            this.dataGridView1.Rows[index].Cells[0].Value = i + 1;
                            this.dataGridView1.Rows[index].Cells[1].Value = inis.IniReadValue("Flv", "Envir");
                            this.dataGridView1.Rows[index].Cells[2].Value = (score * 10).ToString().Substring(0, 5);
                            FileStream fs1 = new FileStream(strfScore, FileMode.Open, FileAccess.Read);
                            StreamReader sr1 = new StreamReader(fs1, Encoding.Default);
                            sr1.ReadLine();    //可以查看qoe_score.txt的格式
                            int[] columns = { 7, 5, 2, 5, 5, 2, 4 };
                            string videoInfo = null;
                            string[] infoNames = null;
                            string[] infoValues = null;
                            for (int ind = 0; ind < columns.Length; ind++)
                            {
                                videoInfo = sr1.ReadLine();
                                infoNames = videoInfo.Split('\t');
                                videoInfo = sr1.ReadLine();
                                infoValues = videoInfo.Split('\t');
                                if (infoNames.Length == columns[ind] && infoNames.Length == infoValues.Length)
                                {
                                    for (int k = 0; k < infoNames.Length; k++)
                                        ResultTmp.WriteLine((++resultIndex).ToString() + "\t" + infoNames[k] + "\t" + infoValues[k]);
                                }
                            }
                            //视觉缺陷-指标评分(100分制):	98.60
                            //块效应-高频分量评分(100分制):	47.80
                            int[] column2s = { 2, 2 };
                            for (int j = 0; j < column2s.Length; j++)
                            {
                                videoInfo = sr1.ReadLine();
                                infoNames = videoInfo.Split(':');
                                if (infoNames.Length == 2)
                                    ResultTmp.WriteLine((++resultIndex).ToString() + "\t" + infoNames[0] + infoNames[1]);
                            }
                            //综合评分(100分制):	63.04
                            videoInfo = sr1.ReadLine();
                            infoNames = videoInfo.Split(':');
                            if (infoNames.Length == 2)
                                ResultTmp.WriteLine((++resultIndex).ToString() + "\tVideoScore" + infoNames[1]);
                            sr1.Close();
                            fs1.Close();
                            ResultTmp.Close();
                        }
                        else
                        {
                            int index = this.dataGridView1.Rows.Add();
                            this.dataGridView1.Rows[index].Cells[0].Value = i + 1;
                            this.dataGridView1.Rows[index].Cells[1].Value = inis.IniReadValue("Flv", "Envir");
                            this.dataGridView1.Rows[index].Cells[2].Value = 0;
                        }

                    }
                    catch (System.Exception ex)
                    {
                        Log.Error(Environment.StackTrace, ex);
                    }



                    //播放结束,写xls文件
                    strbFile.Append("测试被用户中断\r\n");
                    //获取评分模块处理结果,写入xls文件缓存
                    if (File.Exists(strfScore))       //将本次测试生成的qoe_score.txt中的内容写入strbFile
                    {
                        strbFile.Append("\r\n");
                        FileStream fs1 = new FileStream(strfScore, FileMode.Open, FileAccess.Read);
                        StreamReader sr = new StreamReader(fs1, Encoding.Default);
                        strbFile.Append(sr.ReadToEnd());
                        sr.Close();
                        fs1.Close();
                    }

                    //写入xls(txt格式)文件
                    string strLogResult = strpcap.Replace(".pcap", "-log.txt");
                    if (File.Exists(strLogResult))
                    {
                        File.Delete(strLogResult);
                    }
                    FileStream fs3 = new FileStream(strLogResult, FileMode.Create, FileAccess.Write);
                    StreamWriter sw3 = new StreamWriter(fs3, Encoding.Default);
                    sw3.Write(strbFile);
                    sw3.Close();
                    fs3.Close();
                    DisplayState("日志文件:" + strLogResult + "生成成功\r\n");
                }
            }
            else
            {
                for (int i = 0; i < StartTimeList.Count; i++)
                {
                    DateTime dtStart = (DateTime)(StartTimeList[i]);
                    StringBuilder strbFile = (StringBuilder)(strbFileList[i]);
                    string strpcap = inis.IniReadValue("result", "test" + (i + 1));
                    TimeSpan ts = dtEnd - dtStart;
                    float ts2 = ts.Seconds + (float)ts.Milliseconds / 1000;

                    strbFile.Append("测试结束,耗时 " + ts.Minutes + "分 " + ts2.ToString() + "秒" + "\r\n");
                    //对参数文件处理，给出评分
                    string strfScore = "qoe_score.txt";
                    if (File.Exists(strfScore))     //删除的是上一次测试的qoe_score.txt文件
                    {
                        File.Delete(strfScore);
                    }
                    Thread.Sleep(500);
                    //打分
                    double score = this.UnRefScore(i + 1, strfScore);      //成功调用UnRefScore函数后会生成本次测试的qoe_score.txt文件

                    if (score >= 0 && score <= 10)
                    {
                        int index = this.dataGridView1.Rows.Add();
                        this.dataGridView1.Rows[index].Cells[0].Value = i + 1;
                        this.dataGridView1.Rows[index].Cells[1].Value = inis.IniReadValue("Flv", "Envir");
                        this.dataGridView1.Rows[index].Cells[2].Value = (score * 10).ToString().Substring(0, 5);
                    }
                    else
                    {
                        int index = this.dataGridView1.Rows.Add();
                        this.dataGridView1.Rows[index].Cells[0].Value = i + 1;
                        this.dataGridView1.Rows[index].Cells[1].Value = inis.IniReadValue("Flv", "Envir");
                        this.dataGridView1.Rows[index].Cells[2].Value = 0;
                        Log.Warn(string.Format("评分有误!分数:{0}", score.ToString()));
                    }


                    //播放结束,写xls文件
                    strbFile.Append("测试被用户中断\r\n");
                    //获取评分模块处理结果,写入xls文件缓存
                    if (File.Exists(strfScore))       //将本次测试生成的qoe_score.txt中的内容写入strbFile
                    {
                        strbFile.Append("\r\n");
                        FileStream fs1 = new FileStream(strfScore, FileMode.Open, FileAccess.Read);
                        StreamReader sr = new StreamReader(fs1, Encoding.Default);
                        strbFile.Append(sr.ReadToEnd());
                        sr.Close();
                        fs1.Close();
                    }

                    //写入xls(txt格式)文件
                    string strLogResult = strpcap.Replace(".pcap", "-log.txt");
                    if (File.Exists(strLogResult))
                    {
                        File.Delete(strLogResult);
                    }
                    FileStream fs3 = new FileStream(strLogResult, FileMode.Create, FileAccess.Write);
                    StreamWriter sw3 = new StreamWriter(fs3, Encoding.Default);
                    sw3.Write(strbFile);
                    sw3.Close();
                    fs3.Close();
                    DisplayState("日志文件:" + strLogResult + "生成成功\r\n");
                }
            }

            DateTime start = (DateTime)(StartTimeList[0]);
            TimeSpan timediff = dtEnd - start;
            float timediff2 = timediff.Seconds + (float)timediff.Milliseconds / 1000;
            DisplayState("测试结束,耗时 " + timediff.Minutes + "分 " + timediff2.ToString() + "秒" + "\r\n");
            DisplayState("---------------测试完成---------------\r\n");

            port_list.Clear();
            player_list.Clear();
            StartTimeList.Clear();
            strbFileList.Clear();
            comboBox1.Items.Clear();
            comboBox1.Text = "";
            this.dataGridView1.Visible = true;
            //设置正在测试的标示位
            DoTest = false;
            //设置开始停止测试的标识位
            StartStopTest = false;
            //停止控制，为了不让手动和自动冲突
            taskon = false;
        }


        public void StopTerminalTaskFunc()
        {
            //stop button的设置
            this.btnFlvStart.Enabled = true;
            this.btnFlvStop.Enabled = false;
            //设置停止测试标识位
            StartStopTest = true;

            //停止播放，关掉播放器，退出抓包、播放、管道进程
            this.StopClosePlayer();

            Thread.Sleep(500);

            inis.IniWriteValue("Flv", "counts", iTest.ToString());
            //播放次数置零
            iTest = 0;
            timer1.Stop();
            timer1.Enabled = false;

            //memoPcap信息输出
            DateTime dtEnd = DateTime.Now;
            memoPcap.Items.Clear();

            //清空检测静帧跳帧模糊的表盘
            this.ClearGuageData();


            if (inis.IniReadValue("Flv", "Envir").Equals("web"))
            {
                //无参考模式的打分，输出打分结果
                for (int i = 0; i <= port_list.Count; i++)
                {
                    //将播放器的端口号写入配置文件，供分包程序使用
                    //ushort port = (ushort)(port_list[i]);
                    //inis.IniWriteValue("port", "test" + (i + 1), Convert.ToString(port));

                    DateTime dtStart = (DateTime)(StartTimeList[i]);
                    StringBuilder strbFile = (StringBuilder)(strbFileList[i]);
                    //string strpcap = inis.IniReadValue("result", "test" + (i + 1));
                    string strpcap = inis.IniReadValue("Flv", "PcapFile");
                    TimeSpan ts = dtEnd - dtStart;
                    float ts2 = ts.Seconds + (float)ts.Milliseconds / 1000;

                    strbFile.Append("测试结束,耗时 " + ts.Minutes + "分 " + ts2.ToString() + "秒" + "\r\n");
                    strbFile.Append("抓包文件: " + strpcap + "\r\n");
                    DisplayState("抓包文件: " + strpcap + "创建\r\n");

                    //对参数文件处理，给出评分
                    string strfScore = "qoe_score.txt";   //这个文件只有在正常测试的时候才会有
                    if (File.Exists(strfScore))     //删除的是上一次测试的qoe_score.txt文件
                    {
                        File.Delete(strfScore);
                    }
                    Thread.Sleep(500);

                    //打分
                    try
                    {
                        double score = this.UnRefScore(i + 1, strfScore);      //成功调用UnRefScore函数后会生成本次测试的qoe_score.txt文件
                        StreamWriter ResultTmp = new StreamWriter(File.Create("ResultTxt.tmp"), Encoding.Default);
                        //临时总结报告文件，用于满足特定的格式压入数据库
                        ResultTmp.Write("Index\tColumn\tValue\r\n");
                        int resultIndex = 0;
                        if (score >= 0 && score <= 10)
                        {
                            int index = this.dataGridView1.Rows.Add();
                            this.dataGridView1.Rows[index].Cells[0].Value = i + 1;
                            this.dataGridView1.Rows[index].Cells[1].Value = inis.IniReadValue("Flv", "Envir");
                            this.dataGridView1.Rows[index].Cells[2].Value = (score * 10).ToString().Substring(0, 5);
                            FileStream fs1 = new FileStream(strfScore, FileMode.Open, FileAccess.Read);
                            StreamReader sr1 = new StreamReader(fs1, Encoding.Default);
                            sr1.ReadLine();    //可以查看qoe_score.txt的格式
                            int[] columns = { 7, 5, 2, 5, 5, 2, 4 };
                            string videoInfo = null;
                            string[] infoNames = null;
                            string[] infoValues = null;
                            for (int ind = 0; ind < columns.Length; ind++)
                            {
                                videoInfo = sr1.ReadLine();
                                infoNames = videoInfo.Split('\t');
                                videoInfo = sr1.ReadLine();
                                infoValues = videoInfo.Split('\t');
                                if (infoNames.Length == columns[ind] && infoNames.Length == infoValues.Length)
                                {
                                    for (int k = 0; k < infoNames.Length; k++)
                                        ResultTmp.WriteLine((++resultIndex).ToString() + "\t" + infoNames[k] + "\t" + infoValues[k]);
                                }
                            }
                            //视觉缺陷-指标评分(100分制):	98.60
                            //块效应-高频分量评分(100分制):	47.80
                            int[] column2s = { 2, 2 };
                            for (int j = 0; j < column2s.Length; j++)
                            {
                                videoInfo = sr1.ReadLine();
                                infoNames = videoInfo.Split(':');
                                if (infoNames.Length == 2)
                                    ResultTmp.WriteLine((++resultIndex).ToString() + "\t" + infoNames[0] + infoNames[1]);
                            }
                            //综合评分(100分制):	63.04
                            videoInfo = sr1.ReadLine();
                            infoNames = videoInfo.Split(':');
                            if (infoNames.Length == 2)
                                ResultTmp.WriteLine((++resultIndex).ToString() + "\tVideoScore" + infoNames[1]);
                            sr1.Close();
                            fs1.Close();
                            ResultTmp.Close();
                        }
                        else
                        {
                            int index = this.dataGridView1.Rows.Add();
                            this.dataGridView1.Rows[index].Cells[0].Value = i + 1;
                            this.dataGridView1.Rows[index].Cells[1].Value = inis.IniReadValue("Flv", "Envir");
                            this.dataGridView1.Rows[index].Cells[2].Value = 0;
                        }

                    }
                    catch (System.Exception ex)
                    {
                        Log.Error(Environment.StackTrace, ex);
                    }



                    //播放结束,写xls文件
                    strbFile.Append("测试被用户中断\r\n");
                    //获取评分模块处理结果,写入xls文件缓存
                    if (File.Exists(strfScore))       //将本次测试生成的qoe_score.txt中的内容写入strbFile
                    {
                        strbFile.Append("\r\n");
                        FileStream fs1 = new FileStream(strfScore, FileMode.Open, FileAccess.Read);
                        StreamReader sr = new StreamReader(fs1, Encoding.Default);
                        strbFile.Append(sr.ReadToEnd());
                        sr.Close();
                        fs1.Close();
                    }

                    //写入xls(txt格式)文件
                    string strLogResult = strpcap.Replace(".pcap", "-log.txt");
                    if (File.Exists(strLogResult))
                    {
                        File.Delete(strLogResult);
                    }
                    FileStream fs3 = new FileStream(strLogResult, FileMode.Create, FileAccess.Write);
                    StreamWriter sw3 = new StreamWriter(fs3, Encoding.Default);
                    sw3.Write(strbFile);
                    sw3.Close();
                    fs3.Close();
                    DisplayState("日志文件:" + strLogResult + "生成成功\r\n");
                }
            }
            else
            {
                for (int i = 0; i < StartTimeList.Count; i++)
                {
                    DateTime dtStart = (DateTime)(StartTimeList[i]);
                    StringBuilder strbFile = (StringBuilder)(strbFileList[i]);
                    string strpcap = inis.IniReadValue("result", "test" + (i + 1));
                    TimeSpan ts = dtEnd - dtStart;
                    float ts2 = ts.Seconds + (float)ts.Milliseconds / 1000;

                    strbFile.Append("测试结束,耗时 " + ts.Minutes + "分 " + ts2.ToString() + "秒" + "\r\n");
                    //对参数文件处理，给出评分
                    string strfScore = "qoe_score.txt";
                    if (File.Exists(strfScore))     //删除的是上一次测试的qoe_score.txt文件
                    {
                        File.Delete(strfScore);
                    }
                    Thread.Sleep(500);
                    //打分
                    double score = this.UnRefScore(i + 1, strfScore);      //成功调用UnRefScore函数后会生成本次测试的qoe_score.txt文件

                    if (score >= 0 && score <= 10)
                    {
                        int index = this.dataGridView1.Rows.Add();
                        this.dataGridView1.Rows[index].Cells[0].Value = i + 1;
                        this.dataGridView1.Rows[index].Cells[1].Value = inis.IniReadValue("Flv", "Envir");
                        this.dataGridView1.Rows[index].Cells[2].Value = (score * 10).ToString().Substring(0, 5);
                    }
                    else
                    {
                        int index = this.dataGridView1.Rows.Add();
                        this.dataGridView1.Rows[index].Cells[0].Value = i + 1;
                        this.dataGridView1.Rows[index].Cells[1].Value = inis.IniReadValue("Flv", "Envir");
                        this.dataGridView1.Rows[index].Cells[2].Value = 0;
                        Log.Warn(string.Format("评分有误!分数:{0}", score.ToString()));
                    }


                    //播放结束,写xls文件
                    strbFile.Append("测试被用户中断\r\n");
                    //获取评分模块处理结果,写入xls文件缓存
                    if (File.Exists(strfScore))       //将本次测试生成的qoe_score.txt中的内容写入strbFile
                    {
                        strbFile.Append("\r\n");
                        FileStream fs1 = new FileStream(strfScore, FileMode.Open, FileAccess.Read);
                        StreamReader sr = new StreamReader(fs1, Encoding.Default);
                        strbFile.Append(sr.ReadToEnd());
                        sr.Close();
                        fs1.Close();
                    }

                    //写入xls(txt格式)文件
                    string strLogResult = strpcap.Replace(".pcap", "-log.txt");
                    if (File.Exists(strLogResult))
                    {
                        File.Delete(strLogResult);
                    }
                    FileStream fs3 = new FileStream(strLogResult, FileMode.Create, FileAccess.Write);
                    StreamWriter sw3 = new StreamWriter(fs3, Encoding.Default);
                    sw3.Write(strbFile);
                    sw3.Close();
                    fs3.Close();
                    DisplayState("日志文件:" + strLogResult + "生成成功\r\n");
                }
            }

            DateTime start = (DateTime)(StartTimeList[0]);
            TimeSpan timediff = dtEnd - start;
            float timediff2 = timediff.Seconds + (float)timediff.Milliseconds / 1000;
            DisplayState("测试结束,耗时 " + timediff.Minutes + "分 " + timediff2.ToString() + "秒" + "\r\n");
            DisplayState("---------------测试完成---------------\r\n");

            port_list.Clear();
            player_list.Clear();
            StartTimeList.Clear();
            strbFileList.Clear();
            comboBox1.Items.Clear();
            comboBox1.Text = "";
            this.dataGridView1.Visible = true;
            //设置正在测试的标示位
            DoTest = false;
            //设置开始停止测试的标识位
            StartStopTest = false;
            //停止控制，为了不让手动和自动冲突
            taskon = false;
        }



        private void btnFlvStop_Click(object sender, EventArgs e)
        {
            //stopFunc();      
            StopTerminalTaskFunc();
        }

        /******************************************************************************
           the test itself
        /*******************************************************************************/
        private void FlvTesting()
        {
            StringBuilder strbFile = new StringBuilder();
            DoTest = true;

            //清空图表
            this.InitChart();
            this.ClearGuageData();

            //记录多次测试信息                           
            DisplayState("--------------------------------\r\n");
            DisplayState("第 " + iTest + " 次测试......\r\n");
            strbFile.Append("第 " + iTest + " 次测试......\r\n");

            //启动播放器
            try
            {
                if (!File.Exists(strPlayer))
                {
                    DisplayState("测试中断，无法找到播放器");
                    //如果找不到播放器，那么就直接中断程序
                    btnFlvStart.Enabled = true;
                    this.btnFlvStop.Enabled = false;
                    DoTest = false;
                    return;
                }
            }
            catch (Exception ex)
            {
                DisplayState(ex.Message);
                Log.Error(Environment.StackTrace, ex);
            }

            //获取网卡、IP信息
            if (inis.IniReadValue("Flv", "Envir").Equals("web"))   //zc
            {
                DisplayState("网卡: " + inis.IniReadValue("Flv", "IpAddress"));
                strbFile.Append("网卡: " + inis.IniReadValue("Flv", "IpAddress") + "\r\n");
            }

            Thread.Sleep(100);
            DateTime dtStart = DateTime.Now;
            DisplayState("测试开始时间: " + dtStart.ToString());
            strbFile.Append("测试开始时间: " + dtStart.ToString() + "\r\n");
            strbFileList.Add(strbFile);
            StartTimeList.Add(dtStart);

            //Open the device for capturing
            //true -- means promiscuous mode
            //1000 -- means a read wait of 1000ms
            int capTimeOut = Convert.ToInt32(inis.IniReadValue("Flv", "captimeout"));
            int delay = Convert.ToInt32(inis.IniReadValue("Flv", "delay"));

            BackgroundWorker m_AsyncWorker_pipe = new BackgroundWorker();
            m_AsyncWorker_pipe.WorkerSupportsCancellation = true;
            m_AsyncWorker_pipe.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bwAsync_pipe_RunWorkerCompleted);
            m_AsyncWorker_pipe.DoWork += new DoWorkEventHandler(bwAsync_pipe_DoWork);

            if (inis.IniReadValue("Flv", "Envir").Equals("web"))    //抓包的后台线程
            {
                if (!m_AsyncWorker_cap.IsBusy)
                {
                    m_AsyncWorker_cap.RunWorkerAsync();         //引发bwAsync_cap_DoWork事件
                }
                Thread.Sleep(100);
            }

            //开通与播放模块的管道
            if (!m_AsyncWorker_pipe.IsBusy)
            {
                m_AsyncWorker_pipe.RunWorkerAsync();         //引发bwAsync_pipe_DoWork事件
                Thread.Sleep(delay);
            }
            pipeList.Add(m_AsyncWorker_pipe);

            //下载视频文件并播放
            if (!m_AsyncWorker.IsBusy)                 //下载视频文件的后台线程
            {
                m_AsyncWorker.RunWorkerAsync();       //引发bwAsync_DoWork事件
            }
        }


        /******************************************************************************
           播放视频文件
        /*******************************************************************************/
        private void bwAsync_DoWork(object sender, DoWorkEventArgs e)       //完成后引发bwAsync_RunWorkerCompleted
        {                                                                                                             //视频播放线程
            ////定义url地址，用于传入数据给vlc
            string strfplay = null;
            if (inis.IniReadValue("Flv", "Envir").Equals("web"))
            {
                strfplay = inis.IniReadValue("Flv", "urlPage");     //不管是什么ie地址还是真实地址都存在urlPage下
            }


            //写入播放链接真实地址
            string keyname = "relurl" + iTest;
            inisvlc.IniWriteValue("URL", keyname, strfplay);
            Thread.Sleep(500);

            //注：此处直接从strPlayer读取播放器名字是正确的，因为在set界面中，在web、rtsp选择切换时(不用点击确定)
            //就已经把"Flv""Envir"修改了，而在从设置到播放过程的切换时，会执行Init(),在其中strPlayer就会被修改
            try
            {
                string strProcessFile = strPlayer;
                ProcessStartInfo psi = new ProcessStartInfo(strPlayer);
                psi = new ProcessStartInfo(strPlayer);
                psi.FileName = strProcessFile;
                psi.RedirectStandardInput = true;
                psi.RedirectStandardOutput = true;
                psi.UseShellExecute = false;
                psi.WindowStyle = ProcessWindowStyle.Hidden;

                Process ps = new Process();
                ps.StartInfo = psi;
                ps.EnableRaisingEvents = true;

                StringBuilder strbFile = (StringBuilder)(strbFileList[iTest - 1]);
                if (inis.IniReadValue("Flv", "Envir").Equals("web"))
                {
                    DisplayState("页面: " + inis.IniReadValue("Flv", "urlPage"));
                    strbFile.Append("页面: " + inis.IniReadValue("Flv", "urlPage") + "\r\n");
                }

                //启动vlc
                ps.Start();

                if (ps.WaitForInputIdle())
                {
                    while (ps.MainWindowHandle.ToInt32() == 0)
                    {
                        Thread.Sleep(100);
                        ps.Refresh();//必须刷新状态才能重新获得
                        ps.StartInfo = psi;
                    }

                    ShowWindow(ps.MainWindowHandle, 5);
                    SetParent(ps.MainWindowHandle, this.PanelVI.Handle);
                    MoveWindow(ps.MainWindowHandle, -19, -41, 522, 475, true);

                    this.splitContainerControl1.Panel1.Refresh();
                    player_list.Add(ps);
                    string player = "播放器" + iTest;
                    comboBox1.Items.Add(player);
                    comboBox1.SelectedItem = player;
                }
            }
            catch (Exception ex)
            {
                DisplayState(ex.Message);
                Log.Error(Environment.StackTrace, ex);
                return;
            }

        }

        /******************************************************************************
           worker instance complete
        /*******************************************************************************/
        private void bwAsync_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                DisplayState("播放器错误");
                return;
            }

            // Check to see if the background process was cancelled.
            if (e.Cancelled)
            {
                DisplayState("任务撤销");
                return;
            }
        }

        /******************************************************************************
           close the same player before a test start 停止抓包和播放
        /*******************************************************************************/
        public void StopClosePlayer()
        {
            if (strPlayer == "")
                return;
            //考虑到VI播放器有很多异常处理没有完成,在运行时可能会报错,推荐通过直接杀进程来退出
            string strProcessFile = "VLCDialog";
            Process[] p = Process.GetProcessesByName(strProcessFile);
            if (p.Length > 0)
            {
                foreach (Process pro in p)
                {
                    try
                    {
                        pro.Kill();
                        Thread.Sleep(100);
                    }
                    catch (System.Exception ex)
                    {
                        Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
                        pro.Kill();
                        Thread.Sleep(100);
                    }

                }
            }

            //取消播放线程
            Thread.Sleep(2000);
            this.m_AsyncWorker.CancelAsync();
            this.m_AsyncWorker.Dispose();

            //取消管道线程
            BackgroundWorker bw;
            for (int i = 0; i < pipeList.Count; i++)
            {
                bw = (BackgroundWorker)(pipeList[i]);
                bw.CancelAsync();
                bw.Dispose();
            }
            pipeList.Clear();

            //停止抓包
            if (DoTest && (inis.IniReadValue("Flv", "Envir").Equals("web")))
            {
                //StopCapture();
                pcap_packet.Stop();
            }
            //取消抓包线程
            this.m_AsyncWorker_cap.CancelAsync();
            this.m_AsyncWorker_cap.Dispose();
        }

        /******************************************************************************
            call cmd: ipconfig/flushdns 清理DNS记录以便于抓包完整
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
            try
            {
                p.Start();
                p.WaitForExit();
            }
            catch (System.Exception ex)
            {
                Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
            }

        }

        /***********************************************************************
         *                      调用无参考打分模块
         * ***********************************************************************/
        public double UnRefScore(int i, string strfOut)
        {
            double score = 0;

            string strfIn = inisvlc.IniReadValue("result", "test" + i);  //读入播放器播放日志文件
            //string resolution = inisvlc.IniReadValue("resolution", "test" + i);
            //int n = resolution.IndexOf("*");
            //int width = Convert.ToInt32(resolution.Substring(0, n));                 //视频宽度
            //int height = Convert.ToInt32(resolution.Substring(n + 1, resolution.Length - n - 1));                 
            //视频高度，由于打分模块尺寸只有三种352*288,640*480,720*576，所以此次只能做个近似（352*272~~352*288）
            int width = 640;
            int height = 480;
            try
            {
                score = vScore(strfIn, strfOut, width, height);
            }
            catch (Exception ex)
            {
                Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
                return -1;
            }
            return score;
        }

        /********************************************************************************
        private Dundas.Charting.WinControl.Chart chart1;
         *******************************************************************************/
        public delegate void AddDataDelegate(Chart ichart, double yValue);
        public AddDataDelegate addDataDel;

        public delegate void ClearDataDelegate(Chart ichart);
        public ClearDataDelegate clearDataDel;

        int frameNum = 1;

        private void RealTimechart()
        {
            clearDataDel += new ClearDataDelegate(ClearChartData);
            addDataDel += new AddDataDelegate(AddData);
        }

        public void AddData(Chart ichart, double yValue)
        {
            // Define some variables
            int numberOfPointsInChart = 40;
            int numberOfPointsAfterRemoval = 38;

            ichart.Series[0].Points.AddXY(frameNum, yValue);

            // Keep a constant number of points by removing them from the left
            while (ichart.Series[0].Points.Count > numberOfPointsInChart)
            {
                // Remove data points on the left side
                while (ichart.Series[0].Points.Count > numberOfPointsAfterRemoval)
                {
                    ichart.Series[0].Points.RemoveAt(0);
                }

                // Adjust X axis scale
                ichart.ChartAreas["Default"].AxisX.Minimum = frameNum - numberOfPointsAfterRemoval;
                ichart.ChartAreas["Default"].AxisX.Maximum = ichart.ChartAreas["Default"].AxisX.Minimum + numberOfPointsInChart;
            }

            // Invalidate chart
            ichart.Invalidate();
        }

        public void AddNewPoint(Chart ichart, DateTime timeStamp, Dundas.Charting.WinControl.Series ptSeries, double yValue)
        {
            // Add new data point to its series.
            ptSeries.Points.AddXY(timeStamp.ToOADate(), yValue);

            // remove all points from the source series older than 1.5 minutes.
            double removeBefore = timeStamp.AddSeconds((double)(80) * (-1)).ToOADate();

            //remove oldest values to maintain a constant number of data points
            while (ptSeries.Points[0].XValue < removeBefore)
            {
                ptSeries.Points.RemoveAt(0);
            }

            ichart.ChartAreas[0].AxisX.Minimum = ptSeries.Points[0].XValue;
            ichart.ChartAreas[0].AxisX.Maximum = DateTime.FromOADate(ptSeries.Points[0].XValue).AddMinutes(1.5).ToOADate();
            double band = ptSeries.Points.FindMaxValue().YValues[0] - ptSeries.Points.FindMinValue().YValues[0];

            double Interval = 5;
            ichart.ChartAreas[0].AxisY.Interval = Interval;
            ichart.ChartAreas[0].AxisY.Maximum = ((int)(ptSeries.Points.FindMaxValue().YValues[0] / Interval) + 2) * Interval;
            if (ptSeries.Points.FindMinValue().YValues[0] < Interval)
                ichart.ChartAreas[0].AxisY.Minimum = ((int)(ptSeries.Points.FindMinValue().YValues[0] / Interval)) * Interval;
            else
                ichart.ChartAreas[0].AxisY.Minimum = ((int)(ptSeries.Points.FindMinValue().YValues[0] / Interval) - 1) * Interval;

            ichart.ChartAreas[0].AxisY.Interval = (ichart.ChartAreas[0].AxisY.Maximum - ichart.ChartAreas[0].AxisY.Minimum) / 5;
            ichart.Invalidate();
        }

        public void ClearChartData(Chart ichart)
        {
            DateTime timeStamp = DateTime.Now;

            foreach (Series ptSeries in ichart.Series)
            {
                ptSeries.Points.Clear();
            }

            //改成帧序号
            ichart.ChartAreas[0].AxisX.Minimum = 0;
            ichart.ChartAreas[0].AxisX.Maximum = 50;
            ichart.Invalidate();
        }

        public void ClearGuageData()
        {
            gaugeContainer1.Values["Default"].Value = 0;
            gaugeContainer2.Values["Default"].Value = 0;
            gaugeContainer3.Values["Default"].Value = 0;
        }

        public void InitChart()
        {
            chart1.Invoke(clearDataDel, this.chart1);
            chart2.Invoke(clearDataDel, this.chart2);
            chart3.Invoke(clearDataDel, this.chart3);
            chart4.Invoke(clearDataDel, this.chart4);
            chart5.Invoke(clearDataDel, this.chart5);

            this.chart1.Invoke(addDataDel, this.chart1, 0);
            this.chart2.Invoke(addDataDel, this.chart2, 0);
            this.chart3.Invoke(addDataDel, this.chart3, 0);
            this.chart4.Invoke(addDataDel, this.chart4, 0);
            this.chart5.Invoke(addDataDel, this.chart5, 0);
        }

        //public static Thread serverThread = null;  //用于接收远程控制端发送的配置参数
        //TcpListener mylsn;      //服务器监听 
        public static Socket mysock;          //服务器套接字

        string destIp = "127.0.0.1";           //目的ip地址
        int destPort = 8002;                   //目的端口
        string localIp = "127.0.0.1";          //本地Ip
        int localPort = 8001;                  //本地端口

        //string score = "100";
        public static bool isAutoTest = false;
        public static bool hasClient = false;

        //Socket serverSocket = null;

        private void FlvTest_Load(object sender, EventArgs e)
        {
            start.Enabled = false;
            try
            {
                localIp = inis.IniReadValue("Flv", "localIP");
                localPort = Convert.ToInt32(inis.IniReadValue("Flv", "localPort"));
                destIp = inis.IniReadValue("Flv", "destIP");
                destPort = Convert.ToInt32(inis.IniReadValue("Flv", "destPort"));
            }
            catch (System.Exception ex)
            {
                Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
            }
        }

        delegate void deleShow(string s);
        public void DisplayState(string info)
        {
            memoPcap.Items.Add(info);
            memoPcap.SelectedIndex = memoPcap.Items.Count - 1;
        }
        public void DisplayState2(string info)
        {
            deleShow ds = new deleShow(ShowInfo);
            Invoke(ds, new object[] { info });
        }

        private void ShowInfo(string info)
        {
            memoPcap.Items.Add(info);
            memoPcap.SelectedIndex = memoPcap.Items.Count - 1;
        }

        public static TcpClient playClient;   //客户端，用于向服务器传输播放器的实时数据
        DateTime startTime;     //开始播放时刻
        //DateTime endTime;       //停止播放时刻
        //static int playTime;           //播放时长
        bool vlcStart = false;

        delegate void deleScore(string s);

        // 定时器timer1 每秒触发一次，用来检测播放器下载视频使用的端口号
        private void timer1_Elapsed(object sender, System.Timers.ElapsedEventArgs e)    //1s定时器，用来获取端口号
        {
            int i;
            ushort port = 0;
            Process ps;
            for (i = 0; i < player_list.Count; i++)
            {
                ps = (Process)(player_list[i]);
                //port = GetPortByPID(ps.Id);
                if (port != 0)
                {
                    if (port_list.Count == i)
                    {
                        port_list.Add(port);
                    }
                    else
                    {
                        port_list[i] = port;
                    }
                }
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int i = comboBox1.SelectedIndex;
            if (player_list.Count > 1)
            {
                Process plast = (Process)(player_list[lastPlayerIndex]);
                ShowWindow(plast.MainWindowHandle, 0);
            }
            Process p = (Process)(player_list[i]);
            IntPtr pwnd = p.MainWindowHandle;
            ShowWindow(pwnd, 5);
            SetParent(pwnd, this.PanelVI.Handle);
            MoveWindow(pwnd, -19, -41, 522, 475, true);
            lastPlayerIndex = i;
        }

    }
}

