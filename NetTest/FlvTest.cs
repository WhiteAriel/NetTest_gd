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
    //需要确定播放器端的内存对齐字节数
    [StructLayoutAttribute(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct ParaStuct
    {
        public ulong videotime;		//视频时刻
        public int systime;			//系统时间
        public int still; 				//静帧	
        public int blur;				//模糊
        public int skip;				//跳帧
        public int black;				//黑场
        public int definition;			//清晰度
        public int brightness;			//亮度
        public int chroma;				//色度
        public int saturation;			//饱和度
        public int contraction;		//对比度
        public int dev;				///标准差
        public int entro;				//熵
        public double block;			//块效应值
        public double highenerge;		//高频分量
    };  


    public partial class FlvTest : DevExpress.XtraEditors.XtraUserControl
    {
        public static IniFile inis = new IniFile(Application.StartupPath + "\\settings.ini");  //ini class
        public static IniFile inisvlcout = new IniFile(Application.StartupPath + "\\vlc.ini"); //和一些库行为有关，播放器默认读\\VideoPlayer\\vlc.ini，评分读\\vlc.ini
        public static IniFile inisvlc = new IniFile(Application.StartupPath + "\\VideoPlayer\\vlc.ini"); //ini class
        //IniFile inisref = new IniFile(Application.StartupPath + "\\RefTool" + "\\referencesetup.ini");

        public volatile  bool taskon = false;    //表示任务没有运行
        public volatile bool serverTest = false;   //表示执行的是服务器任务还是终端自己的任务

        private int iTest = 0;              //连续播放了多少次
        private static int intCheckContinuous;     //是否连续播放
        private static int iNumContinuous = 0;     //连续播放总次数

        public string strPlayer;            //播放器完整名（含路径）  
        public string strPcapFile = "";     //抓包文件名
        public int iDevice = 0;                 //网卡索引
        public int lastPlayerIndex = 0;
        public LibPcapLiveDevice device;
        private DateTime StartTime =new DateTime();         //开始测试的时间

        private string strPlayFile;         //qoe文件
        private string strLogResult = null;
        private string strXlsLogFile;       //log file path (xls file(xls格式) path)
        private StringBuilder strbFile = new StringBuilder();    //contents of log file (content of xls file)
        private StringBuilder ScoreParam = new StringBuilder();  //百分制参数提取

        public static volatile bool DoTest = false;

        private static PacketCap pcap_packet;

        private MySQLInterface mysqlTest = null;
        private bool mysqlTestFlag = false;

        //事件用来控制阻塞
        private AutoResetEvent videoEndEvent = new AutoResetEvent(true); 
        private int videoHandle = 0;  //StartPlay函数的句柄，需要保存便于停止 

        //抓包进程
        private BackgroundWorker m_AsyncWorker_cap = new BackgroundWorker();

        Queue<ParaStuct> paraQue = new Queue<ParaStuct>();
        object queLock = new object();

        //写文件用于评分
        //FileStream scoreInfile = null;
        //StreamWriter scoreInWriter = null; 

        //参数线程
        Thread paraShowThread = null;

        //枚举表示用户结束、流结束、流异常
        public enum USER_ACT
        {
            SELF_STOP,
            STREAM_END,
            STREAM_EXC,
            DEFAULT
        };
        USER_ACT user_act = USER_ACT.DEFAULT;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern long SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool MoveWindow(IntPtr hwnd, int x, int y, int cx, int cy, bool repaint);

        [DllImport("VideoScore.dll")]
        public static extern double vScore(string strlog, string result, int width, int height);

        [DllImport("CapturePacket.dll")]
        public static extern int StartDispatch(int n, string ini);

        [DllImport("user32.dll", EntryPoint = "ShowWindow", SetLastError = true)]
        public static extern bool ShowWindow(IntPtr hwnd, uint cmdshow);


        //播放器封装
        [DllImport("VideoPlayer.dll")]
        public static extern int StartPlay(string url, IntPtr hwnd, VideoCallBack cb, IntPtr user_data,int sample);

        [DllImport("VideoPlayer.dll")]
        public static extern int StopPlay(int handle);
        
        /******************************************************************************
           init the user components FlvTest 
        /*******************************************************************************/
        public FlvTest()
        {
            InitializeComponent();

            //将几个自定义函数的句柄分配给BackgroundWorker的DoWork、RunWorkerCompleted事件

            m_AsyncWorker_cap.WorkerSupportsCancellation = true;
            m_AsyncWorker_cap.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bwAsync_cap_RunWorkerCompleted);
            m_AsyncWorker_cap.DoWork += new DoWorkEventHandler(bwAsync_cap_DoWork);

            Control.CheckForIllegalCrossThreadCalls = false;
            DoTest = false;

            videoEndEvent.Reset();

            //数据库对象初始化
            mysqlTest = new MySQLInterface(inis.IniReadValue("Mysql", "serverIp"), inis.IniReadValue("Mysql", "user"), inis.IniReadValue("Mysql", "passwd"), inis.IniReadValue("Mysql", "dbname"), inis.IniReadValue("Mysql", "port"));
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
            if (iDevice < 0)
            {
                iDevice = 0;
            }
            pcap_packet = new PacketCap(iDevice);

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

        /******************************************************************************
           start the test single or loop, the main process call WebTesting() 
        /*******************************************************************************/
        public int StartServerTaskFunc()   //如果终端任务在执行，服务器任务等待
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
                        return -1;
                    }

                    this.iTest++;
                    frameNum = 1;

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
                    inisvlc.IniWriteValue("Flv", str, strPlayFile);
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
                    int flvTestRet=this.FlvTesting();
                    if (flvTestRet == 0)
                    {
                        videoEndEvent.WaitOne();   //wait for end of stream
                        StopServerTaskFunc();
                        return 0;
                    }
                    else
                    {
                        PlayException();
                        return flvTestRet;
                    }
                }
                else
                    Thread.Sleep(2000);  //wait 2s if handon task is running 
            }
        }

        /******************************************************************************
          interrupt the test whether loop or not 
       /*******************************************************************************/
        public void StopServerTaskFunc()
        {
            //停止播放，关掉播放器，退出抓包、播放、管道进程
            this.StopClosePlayer();

            Thread.Sleep(500);

            inis.IniWriteValue("Flv", "counts", iTest.ToString());
            //播放次数置零
            iTest = 0;

            //memoPcap信息输出
            DateTime EndTime = DateTime.Now;
            memoPcap.Items.Clear();

            //清空检测静帧跳帧模糊的表盘
            this.ClearGuageData();

            TimeSpan ts = EndTime - StartTime;
            float ts2 = ts.Seconds + (float)ts.Milliseconds / 1000;

            //无参考模式的打分，输出打分结果
            string strpcap = inis.IniReadValue("Flv", "PcapFile");
            strbFile.Append("测试结束,耗时 " + ts.Minutes + "分 " + ts2.ToString() + "秒" + "\r\n");
            strbFile.Append("抓包文件: " + strpcap + "\r\n");
            DisplayState("抓包文件: " + strpcap + "创建\r\n");

            //设置正在测试的标示位
            DoTest = false;
            paraShowThread.Join(1000);   //wait for exit of paraShowThread
            try
            {
                if (paraShowThread.IsAlive)
                    paraShowThread.Abort();
            }
            catch (System.Exception ex)
            {
                Log.Warn(ex.ToString());
            }
  
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
                double score = this.UnRefScore(strfScore);      //成功调用UnRefScore函数后会生成本次测试的qoe_score.txt文件
                StreamWriter ResultTmp = new StreamWriter(File.Create("ResultTxt.tmp"), Encoding.Default);
                //临时总结报告文件，用于满足特定的格式压入数据库
                ResultTmp.Write("Index\tColumn\tValue\r\n");
                int resultIndex = 0;
                if (score >= 0 && score <= 10)
                {
                    int index = this.dataGridView1.Rows.Add();
                    this.dataGridView1.Rows[index].Cells[0].Value = 1;
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
                    this.dataGridView1.Rows[index].Cells[0].Value =1;
                    this.dataGridView1.Rows[index].Cells[1].Value = inis.IniReadValue("Flv", "Envir");
                    this.dataGridView1.Rows[index].Cells[2].Value = 0;
                }

            }
            catch (System.Exception ex)
            {
                Log.Error(Environment.StackTrace, ex);
            }

            //播放结束,写xls文件
            strbFile.Append("测试结束\r\n");
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
            DisplayState("测试结束,耗时 " + ts.Minutes + "分 " + ts.ToString() + "秒" + "\r\n");
            DisplayState("---------------测试完成---------------\r\n");

            comboBox1.Items.Clear();
            comboBox1.Text = "";
            this.dataGridView1.Visible = true;
            //停止控制，为了不让手动和自动冲突
            taskon = false;
            //stop button的设置
            this.btnFlvStart.Enabled = true;
            this.btnFlvStop.Enabled = false;
        }

        private void btnFlvStart_Click(object sender, EventArgs e)
        {
            StartTerminalTaskFunc();
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
                    frameNum = 1;

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
                    int ret=this.FlvTesting();
                    if (ret < 0)  //播放异常
                    {
                        switch (ret)
                        {
                            case -19996:
                                //增加错误备注
                                 DisplayState("无效的采样率参数");
                                break;
                            case -19997:
                                //增加错误备注
                                DisplayState("无效的参数");
                                break;
                            case -19998:
                                //增加错误备注
                                DisplayState("启动播放器任务失败");
                                break;
                            case -19999:
                                    //增加错误备注
                                    DisplayState("没有可执行任务");
                                    break;
                            case -20000:
                                    //增加错误备注
                                    DisplayState("打开URL错误");
                                    break;
                            case -20001:
                                    //增加错误备注
                                    DisplayState("打开视频流出错");
                                    break;
                            case -20002:
                                    //增加错误备注
                                    DisplayState("没有可用视频流");
                                    break;
                            case -20003:
                                    //增加错误备注
                                    DisplayState("找不到编码器");
                                    break;
                            case -20004:
                                    //增加错误备注
                                    DisplayState("无法打开编码器");
                                    break;
                            case -20005:
                                    //增加错误备注
                                    DisplayState("错误的SDL间隔");
                                    break;
                            default:
                                    DisplayState("代码异常");
                                break; 
                        }
                        PlayException();
                    }
        }

        private void btnFlvStop_Click(object sender, EventArgs e)
        {
            StopTerminalTaskFunc();
        }

        public void StopTerminalTaskFunc()
        {
            //停止播放，关掉播放器，退出抓包、播放、管道进程
            this.StopClosePlayer();
            Thread.Sleep(500);
            //设置正在测试的标示位
            DoTest = false;
            paraShowThread.Join(1000);   //wait for exit of paraShowThread
            try
            {
                if (paraShowThread.IsAlive)
                    paraShowThread.Abort();
            }
            catch (System.Exception ex)
            {
                Log.Warn(ex.ToString());
            }
            inis.IniWriteValue("Flv", "counts", iTest.ToString());
            //播放次数置零
            iTest = 0;
            //memoPcap信息输出
            DateTime EndTime = DateTime.Now;
            memoPcap.Items.Clear();
            //清空检测静帧跳帧模糊的表盘
            this.InitChart();
            this.ClearGuageData();
            TimeSpan ts = EndTime - StartTime;
            float ts2 = ts.Seconds + (float)ts.Milliseconds / 1000;
            //无参考模式的打分，输出打分结果
            string strpcap = inis.IniReadValue("Flv", "PcapFile");
            strbFile.Append("测试结束,耗时 " + ts.Minutes + "分 " + ts2.ToString() + "秒" + "\r\n");
            strbFile.Append("抓包文件: " + strpcap + "\r\n");
            DisplayState("抓包文件: " + strpcap + "创建\r\n");

            //对参数文件处理，给出评分
            string strfScore = "qoe_score.txt";   //这个文件只有在正常测试的时候才会有
            if (File.Exists(strfScore))     //删除的是上一次测试的qoe_score.txt文件
            {
                File.Delete(strfScore);
            }
            //打分
            try
            {
                Thread.Sleep(300);
                double score = this.UnRefScore(strfScore);      //成功调用UnRefScore函数后会生成本次测试的qoe_score.txt文件
                StreamWriter ResultTmp = new StreamWriter(File.Create("ResultTxt.tmp"), Encoding.Default);
                //临时总结报告文件，用于满足特定的格式压入数据库
                ResultTmp.Write("Index\tColumn\tValue\r\n");
                int resultIndex = 0;
                if (score >= 0 && score <= 10)
                {
                    int index = this.dataGridView1.Rows.Add();
                    this.dataGridView1.Rows[index].Cells[0].Value = 1;
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
                    this.dataGridView1.Rows[index].Cells[0].Value = 1;
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
            DisplayState("测试结束,耗时 " + ts.Minutes + "分 " + ts.ToString() + "秒" + "\r\n");
            DisplayState("---------------测试完成---------------\r\n");

            comboBox1.Items.Clear();
            comboBox1.Text = "";
            this.dataGridView1.Visible = true;
            //停止控制，为了不让手动和自动冲突
            taskon = false;
            //stop button的设置
            this.btnFlvStart.Enabled = true;
            this.btnFlvStop.Enabled = false;
        }

        private void videoParaShow()
        {
            bool createVideoPara = false;
            if (mysqlTestFlag)
                createVideoPara = mysqlTest.CreatVideoPara();
            string ipandtype = inis.IniReadValue("Task", "currentVideoId") + "#" + "Video";
            while (true)
            {
                if (!DoTest)
                    break;
                lock (queLock)
                {
                    if (paraQue.Count > 0)
                    {
                        ParaStuct ps = paraQue.Dequeue();
                        //添加chart数据
                        chart1.Invoke(addDataDel, this.chart1, (ps.definition ));    //清晰度
                        chart2.Invoke(addDataDel, this.chart2, (ps.brightness ));    //亮度
                        chart3.Invoke(addDataDel, this.chart3, (ps.chroma));        //色度
                        chart4.Invoke(addDataDel, this.chart4, (ps.saturation ));    //饱和度
                        chart5.Invoke(addDataDel, this.chart5, (ps.contraction ));   //对比度
                        frameNum++;
                        //添加gauge data,0/1取值
                        gaugeContainer1.Values["Default"].Value = ps.still * 80;
                        gaugeContainer2.Values["Default"].Value = ps.skip * 80;
                        gaugeContainer3.Values["Default"].Value = ps.blur * 80;

                        Log.Console(String.Format("{0},{1},{2},{3},{4}", ps.brightness, ps.contraction, ps.still, ps.skip, ps.blur));
                        videoPara vp = new videoPara(ps.still, ps.blur, ps.skip, ps.black, ps.definition, ps.brightness, ps.chroma, ps.saturation, ps.contraction, ps.dev, ps.entro, ps.block, ps.highenerge, -1);

                        //scoreInWriter.WriteLine(String.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}\t{17}\t{18}\t{19}", ps.systime, ps.videotime, ps.still, ps.skip, ps.blur, ps.black, ps.definition, ps.brightness, ps.chroma, ps.saturation, ps.contraction, ps.dev, ps.dev, ps.block, ps.entro, ps.highenerge, ps.highenergeLU, ps.highenergeRU, ps.highenergeLD, ps.highenergeRD));
                        //记录插入mysql表中VideoPara
                        if (createVideoPara == true && serverTest)
                            mysqlTest.VideoParaInsertMySQL(ipandtype, vp);
                    }
                    else
                        Thread.Sleep(50);
                }
            }

        }


        private void VideoCallBackFunc(IntPtr para,int callbackType,IntPtr user_data)
        {
            try
            {
                if (callbackType == 20001)   //normal callback
                {
                    ParaStuct ps = (ParaStuct)Marshal.PtrToStructure(para, typeof(ParaStuct));
                    paraQue.Enqueue(ps);
                    
                }
                else if (callbackType == 20000)   //stream end
                {
                    user_act = USER_ACT.STREAM_END;
                    if (serverTest)
                        videoEndEvent.Set();
                    else
                    {
                        DisplayState("检测到流结束帧");
                        
                    }
                }
                else if (callbackType == -20001)  //ERROR_STREAM_EXCEPTION
                {
                    user_act = USER_ACT.STREAM_EXC;
                    if (serverTest)
                        videoEndEvent.Set();
                    else
                    {
                        DisplayState("取帧异常,播放器退出");
                       
                        //StopTerminalTaskFunc();
                    }
                }
            }
            catch (System.Exception ex)
            {
                //StopTerminalTaskFunc();
                Log.Error(ex.ToString());
                Log.Console(ex.ToString());
            }

        }


        void ListenTerminent()
        {
            while (true)
            {
                if (!serverTest && (user_act == USER_ACT.STREAM_END || user_act == USER_ACT.STREAM_EXC))
                {
                    StopTerminalTaskFunc();
                    break;
                }
                else
                    Thread.Sleep(50);
            }
        }



        /******************************************************************************
           the test itself
        /*******************************************************************************/
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] //回调告诉的调用约定，C#默认stdcall，C++是默认的cdcall
        public delegate void VideoCallBack(IntPtr para, int callBackType,IntPtr user_data); //定义播放器委托
        public VideoCallBack vcb;
        private int  FlvTesting()
        {
            DoTest = true;
            //清空图表
            this.InitChart();
            this.ClearGuageData();

            //记录多次测试信息                           
            DisplayState("--------------------------------\r\n");
            DisplayState("第 " + iTest + " 次测试......\r\n");
            strbFile.Append("第 " + iTest + " 次测试......\r\n");

            //获取网卡、IP信息

            DisplayState("网卡: " + inis.IniReadValue("Flv", "IpAddress"));
            strbFile.Append("网卡: " + inis.IniReadValue("Flv", "IpAddress") + "\r\n");
 

            Thread.Sleep(100);
            StartTime = DateTime.Now;
            DisplayState("测试开始时间: " + StartTime.ToString());
            strbFile.Append("测试开始时间: " + StartTime.ToString() + "\r\n");

            //Open the device for capturing
            //true -- means promiscuous mode
            //1000 -- means a read wait of 1000ms
            int capTimeOut = Convert.ToInt32(inis.IniReadValue("Flv", "captimeout"));
            int delay = Convert.ToInt32(inis.IniReadValue("Flv", "delay"));

            //调用播放器接口，同时在回调里处理返回的参数
            ////定义url地址，用于传入数据给vlc
            string strfplay="";
            strfplay = inis.IniReadValue("Flv", "urlPage");     //不管是什么ie地址还是真实地址都存在urlPage下
            DisplayState("测试url: " + strfplay);
            strbFile.Append("测试url: " + strfplay + "\r\n");

            this.videoPictureBox.Visible = true;
            try
            {
                vcb = VideoCallBackFunc;
                //int usrdata = 1;
                IntPtr pA = new IntPtr(0);
                //int lenght = Marshal.SizeOf(usrdata);
                //IntPtr pA = Marshal.AllocHGlobal(lenght);
                videoHandle = StartPlay(strfplay, this.videoPictureBox.Handle, vcb, pA,5);
                //Marshal.FreeHGlobal(pA);
                if (videoHandle >= 0)
                {
                                      
                    if (inis.IniReadValue("Flv", "Envir").Equals("web"))    //抓包的后台线程
                    {
                        if (!m_AsyncWorker_cap.IsBusy)
                        {
                            m_AsyncWorker_cap.RunWorkerAsync();         //引发bwAsync_cap_DoWork事件
                        }
                        Thread.Sleep(100);
                        paraShowThread = new Thread(videoParaShow);
                        paraShowThread.Start();
                        Thread listenTerminentThread = new Thread(ListenTerminent);
                        listenTerminentThread.Start();
                    }
                    return 0;
                }
                else    //播放异常
                {
                    //PlayException();
                    return videoHandle;
                }
                
            }
            catch (System.Exception ex)
            {
                //PlayException();
                Log.Error(ex.ToString());
                Log.Console(ex.ToString());
            }
            return -2;
        }

        private void PlayException()
        {
            this.videoPictureBox.Visible = false;
            DisplayState("播放异常\r\n");
            //stop button的设置
            this.btnFlvStart.Enabled = true;
            this.btnFlvStop.Enabled = false;
            comboBox1.Items.Clear();
            comboBox1.Text = "";
            //清空检测静帧跳帧模糊的表盘
            this.InitChart();
            this.ClearGuageData();
            //设置正在测试的标示位
            DoTest = false;
            //停止控制，为了不让手动和自动冲突
            taskon = false;
        }

        /******************************************************************************
           close the same player before a test start 停止抓包和播放
        /*******************************************************************************/
        public void StopClosePlayer()
        {
            //这里需要控制是否需要停止
            if (user_act != USER_ACT.STREAM_END && user_act != USER_ACT.STREAM_EXC)
            {
                StopPlay(videoHandle);                
            }
            user_act = USER_ACT.DEFAULT;
            this.videoPictureBox.Visible = false;
            //停止抓包
            pcap_packet.Stop();
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
        public double UnRefScore(string strfOut)
        {
            double score = 0;

            string strfIn = inisvlc.IniReadValue("result", "test1");  //读入播放器播放日志文件
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
            /*this.Static = 0;
            this.Skip = 0;
            this.Blur = 0;*/   
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

            /*this.Definition=0;
            this.Brightness=0;
            this.Color=0;
            this.Saturation=0;
            this.Contrast=0; */
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



        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

    }
}

