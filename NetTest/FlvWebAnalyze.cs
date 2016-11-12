/******************************************************************************
 
 web和rtsp分析修改
 * 1.常规分析--------单个链接测试完成后，立即分析，结果既显示在面板上，也保存在excel中
 * 
 * 2.批量分析--------连续测试多个链接后，一起分析，结果不再面板上显示，只保存在excel中
 * 
 * 3.webInfo+TcpDns+InOut+DelayJitter+txtResult+ResultStore 
 
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;
using System.Drawing.Drawing2D;
using Dundas.Charting.WinControl;
using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using MultiMySQL;
using NetLog;


namespace NetTest
{
    public struct AverValue
    {
        public static double AverDNS = 0.0;
        public static double AverHTTP = 0.0;    //Web响应延时
        public static double AverRTSPRTT = 0.0; //Rtsp响应延时
        public static double AverInOut = 0.0;
        public static double AverDelay = 0.0;
        public static double AverJitter = 0.0;
        public static string TcpInfo = "";
        public static string TcpEx = "";
        public static string FrameRate = "";


        public static void InitValue()
        {
            AverDNS = 0.0;
            AverHTTP = 0.0;
            AverRTSPRTT = 0.0;
            AverInOut = 0.0;
            AverDelay = 0.0;
            AverJitter = 0.0;
            TcpInfo = "";
            TcpEx = "";
            FrameRate = "";
        }
    }

    public partial class FlvWebAnalyze : DevExpress.XtraEditors.XtraUserControl
    {
        IniFile inis = new IniFile(Application.StartupPath + "\\settings.ini");  //ini class
        IniFile inisvlc = new IniFile(Application.StartupPath + "\\VideoPlayer" + "\\vlc.ini");
        IniFile inisref = new IniFile(Application.StartupPath + "\\refTool" + "\\referencesetup.ini");

        const int PACKETPAGESIZE = 2000;      //数据包展示页面每次加载1000条
        const int GROUPCOUNT = 200;           //每次数据表刷新200条记录
        //测试次数
        // int testNum = 0;
        //全局变量，判断是否分析结束
        bool IsAnalysed = false;
        //抓包文件名
        string PcapFileName = null;
        //播放日志文件
        string TxtFileName = null;
        //分析耗时
        //int AnalysingTimeCount = 0;
        //记录前一个选择项索引
        int prevSelectIndex = 0;
        //吞吐量所选的尺度选项
        double[] timeScale = new double[] { 1.0, 0.1, 0.01 };
        //所有数据包个数
        int totalPacketCnt = 0;
        //帧长分布中不同帧长对应个数
        int[] rangeCount = new int[11];
        //错误信息记录
        string WrongReason = null;
        //测试报告txt格式
        public string strTxtResult = null;
        //Excel处理程序
        ExcelProcess processExcel = new ExcelProcess();
        //xls文件
        string strXlsLogFile = null;
        //判断txt转换为xsl
        bool iTxt2Xls = false;
        //f分析次数
        // int iStartAnalyze = 0;
        //批处理cap文件名称
        //string[] filesinpath = null;
        //判断是否是选择现在pcap文件
        public static bool isSelectPcap = false;

        private bool analyzeOn = false;
        public bool serverTest = false;

        //数据库对象
        private MySQLInterface mysqlWeb = null;
        private bool mysqlWebFlag = false;
        //private MySQLInterface mysqlWeb = new MySQLInterface(inis.IniReadValue("Mysql", "serverIp"), inis.IniReadValue("Mysql", "user"), inis.IniReadValue("Mysql", "passwd"), inis.IniReadValue("Mysql", "dbname"));
        //设置解析线程
        Thread setParseTrd = null;

        //数据包分页变量
        int currentPage = 1;  //当前页码
        int totalNum = 0;    //总记录数，初始化为0
        int pageNum = 0;      //总页数

        //tcp正常流分页
        int currentPageTcpGene = 1;  //当前页码
        int totalNumTcpGene = 0;    //总记录数，初始化为0
        int pageNumTcpGene = 0;      //总页数


        //当前任务id和类型
        string currentId;


        ArrayList datalist = ArrayList.Synchronized(new ArrayList());//数据包arraylist，保存cap包名字

        //定义填充listview的BackgroundWorker，防止界面进程阻塞 
        //private BackgroundWorker m_AsyncWorkerTcp = new BackgroundWorker();  //Tcp界面，暂时不支持停止
        //private BackgroundWorker m_AsyncWorkerSave = new BackgroundWorker(); //抖动延迟界面

        public void Init()
        {
            PacAnaly.SelectedTabPageIndex = 12;
            //lsvResult.Items.Clear();
            //设置分析完成判断指示
            IsAnalysed = false;
            //将抓包文件名置空
            PcapFileName = null;
            //将播放日志文件名置空
            TxtFileName = null;
            //清空图表
            //this.InitChart();
            //this.InitListView();
            //清空上次计算的平均值
            //AverValue.InitValue();

            //将帧长分析的时间尺度标识出
            int i = 0;
            object[] obj = new object[timeScale.Length];
            foreach (double s in timeScale)
            {
                obj[i++] = s.ToString() + "秒";
            }
            ScaleComboBox.Items.Clear();
            //添加尺度选择组合框
            ScaleComboBox.Items.AddRange(obj);
            //尺度默认为1.0
            ScaleComboBox.SelectedIndex = 0;

            this.btnStartAnaly.Enabled = true;
        }

        public void InitChart()
        {
            this.InitWebChart();
            this.ChartTcpGenr.Invoke(clearDataDel, this.ChartTcpGenr);
            this.ChartDNS.Invoke(clearDataDel, this.ChartDNS);
            this.ChartInOut.Invoke(clearDataDel, this.ChartInOut);
            this.ChartFrameLength.Invoke(clearDataDel, this.ChartFrameLength);
            this.ChartDelayJitter.Invoke(clearDataDel, this.ChartDelayJitter);
        }

        public void InitListView()
        {
            LVSum.Items.Clear();
            LVPacketAnalys.Items.Clear();
            LVTCPGeneral.Items.Clear();
            LVTCPEx.Items.Clear();
            LVDNSAnalys.Items.Clear();
            LVHTTPAnalys.Items.Clear();
            LVInOut.Items.Clear();
            LVFrameLength.Items.Clear();
            LVDelayJitter.Items.Clear();
            lsvResult.Items.Clear();
            strTxtResult = Application.StartupPath + "\\TxtResult.txt";

            DelayAvg.Visible = false;
            DelayMax.Visible = false;
            DelayMin.Visible = false;
            JitterAvg.Visible = false;
            JitterMax.Visible = false;
            JitterMin.Visible = false;

            InOutAvg.Visible = false;
            InOutMax.Visible = false;
            InOutMin.Visible = false;

            DelayMax.Text = "延时最大值:";
            DelayMin.Text = "延时最小值:";
            DelayAvg.Text = "延时平均值:";
            JitterMax.Text = "抖动最大值:";
            JitterMin.Text = "抖动最小值:";
            JitterAvg.Text = "抖动平均值:";

            InOutAvg.Text = "吞吐量平均值:";
            InOutMax.Text = "吞吐量最大值:";
            InOutMin.Text = "吞吐量最小值:";
            if (File.Exists(strTxtResult))
                File.Delete(strTxtResult);
        }

        public FlvWebAnalyze()
        {
            InitializeComponent();
            this.RealTimechart();
            datalist.Clear();
            mysqlWeb = new MySQLInterface(inis.IniReadValue("Mysql", "serverIp"), inis.IniReadValue("Mysql", "user"), inis.IniReadValue("Mysql", "passwd"));
            if (mysqlWeb.MysqlInit(inis.IniReadValue("Mysql", "dbname")))
                mysqlWebFlag = true;
        }


        private void ParsePacket()
        {
            //4部分的解析函数

            currentId = inis.IniReadValue("Task", "currentVideoId");
            try
            {
                WebInfoAnalys();         //web播放的相关内容解析
                InOutFrameLenAnalys();    //分析吞吐量、帧长分布信息
                PcapTcpDnsHttpAnalys();  //分析文件概要、数据包、TCP、DNS、HTTP等信息
                ResultDisplay();          //测试报告结果显示
                //storeResult();
            }
            catch (System.Exception ex)
            {
                Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
            }
            if (WrongReason != null && !serverTest)
            {
                MessageBox.Show(WrongReason + "出错可能的原因有：\n 1、没有相关的数据包 \n 2、网卡选择不正确 \n 3、服务器上没有相关视频文件 \n 4、解析不支持对应的视频格式 \n");
            }
            btnStartAnaly.Enabled = true;
            btnWebSelCap.Enabled = true;
            isSelectPcap = false;
            analyzeOn = false;
            serverTest = false;
        }

        public void StartServerAnalyzeFunc()
        {
            while (true)
            {
                if (analyzeOn == false)
                {
                    //清除Excel进程
                    analyzeOn = true;
                    Process[] p = Process.GetProcessesByName("EXCEL");
                    if (p.Length > 0)
                    {
                        for (int i = 0; i < p.Length; i++)
                        {
                            p[i].CloseMainWindow();
                            p[i].Kill();
                        }
                    }
                    if (isSelectPcap == false)              //如果没有在分析之前选择pcap文件时读取上一次的文件
                    {
                        PcapFileName = inis.IniReadValue("Flv", "PcapFile");
                        TxtFileName = inis.IniReadValue("Flv", "PlayerFile");
                    }


                    if (!File.Exists(PcapFileName))
                    {
                        MessageBox.Show("找不到数据包文件：" + PcapFileName);
                        Log.Warn("找不到数据包文件");
                        return;
                    }

                    IsAnalysed = true;    //是否进行了分析
                    WrongReason = null;   //清空错误信息
                    //清空图表
                    this.InitChart();
                    this.InitListView();
                    //清空上次计算的平均值
                    AverValue.InitValue();

                    btnStartAnaly.Enabled = false;
                    btnWebSelCap.Enabled = false;

                    try
                    {
                        setParseTrd = new Thread(new ThreadStart(ParsePacket));
                        setParseTrd.Start();
                    }
                    catch (System.Exception ex)
                    {
                        Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
                    }
                    break;
                }
                else
                    Thread.Sleep(1500);   //休眠等待
            }

        }


        public void StartTerminalAnalyzeFunc()
        {
                    //清除Excel进程
                    analyzeOn = true;
                    Process[] p = Process.GetProcessesByName("EXCEL");
                    if (p.Length > 0)
                    {
                        for (int i = 0; i < p.Length; i++)
                        {
                            p[i].CloseMainWindow();
                            p[i].Kill();
                        }
                    }
                    if (isSelectPcap == false)              //如果没有在分析之前选择pcap文件时读取上一次的文件
                    {
                        PcapFileName = inis.IniReadValue("Flv", "PcapFile");
                        TxtFileName = inis.IniReadValue("Flv", "PlayerFile");
                    }


                    if (!File.Exists(PcapFileName))
                    {
                        MessageBox.Show("找不到数据包文件：" + PcapFileName);
                        Log.Warn("找不到数据包文件");
                        return;
                    }

                    IsAnalysed = true;    //是否进行了分析
                    WrongReason = null;   //清空错误信息
                    //清空图表
                    this.InitChart();
                    this.InitListView();
                    //清空上次计算的平均值
                    AverValue.InitValue();

                    btnStartAnaly.Enabled = false;
                    btnWebSelCap.Enabled = false;

                    try
                    {
                        setParseTrd = new Thread(new ThreadStart(ParsePacket));
                        setParseTrd.Start();
                    }
                    catch (System.Exception ex)
                    {
                        Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
                    }
        }

        public void btnStartAnaly_Click(object sender, EventArgs e)
        {
            //startFunc();
            StartTerminalAnalyzeFunc();
        }

        //保存测试报告
        private void storeResult()
        {
            //保存截图
            {
                //根据解析的数据包名指定保存图片文件路径
                string SavedPicPath = PcapFileName;
                //指定保存图片文件路径
                SavedPicPath = PcapFileName;
                SavedPicPath = SavedPicPath.Remove(SavedPicPath.Length - 5);
                if (!Directory.Exists(SavedPicPath))
                    Directory.CreateDirectory(SavedPicPath);
                //先对图片保存目录进行清空
                string[] FileInPath = Directory.GetFiles(SavedPicPath);
                foreach (string str in FileInPath)
                    File.Delete(str);
                //指定保存图片文件格式
                ChartImageFormat format = ChartImageFormat.Jpeg;
                //保存图片
                try
                {
                    ChartDNS.SaveAsImage(SavedPicPath + "\\ChartDns.jpg", format);
                    ChartTcpGenr.SaveAsImage(SavedPicPath + "\\ChartTcpGener.jpg", format);
                    ChartInOut.SaveAsImage(SavedPicPath + "\\ChartInOut.jpg", format);
                    ChartFrameLength.SaveAsImage(SavedPicPath + "\\ChartFrameLength.jpg", format);
                    ChartDelayJitter.SaveAsImage(SavedPicPath + "\\ChartDelayJitter.jpg", format);
                }
                catch (System.Exception ex)
                {
                    Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
                }

            }


            // txt转excel文件
            //strXlsLogFile = inis.IniReadValue("Flv", "LogFile");
            iTxt2Xls = processExcel.txt2Xlsx(strTxtResult, strXlsLogFile);
            if (!iTxt2Xls)
            {
                MessageBox.Show("日志文件" + strXlsLogFile + "创建失败！\n 请检查本机是否安装好Office软件！");
                return;
            }

            StreamReader sReader = null;
            try
            {
                string path = strTxtResult;
                string lineContent = null;

                sReader = new StreamReader(path, Encoding.Default);
                while ((lineContent = sReader.ReadLine()) != null)
                {
                    TransferFiles.SendVarData(lineContent + "\n");
                }
                sReader.Close();
            }
            catch (System.Exception ex)
            {
                //MessageBox.Show(ex.Message.ToString());
                if (sReader != null)
                {
                    sReader.Close();
                }
                Log.Error(Environment.StackTrace, ex);
            }

            return;
        }

        class TransferFiles
        {

            public TransferFiles()
            {

            }

            public static int SendVarData(string data) // return integer indicate how many data sent.
            {
                byte[] info = System.Text.Encoding.Default.GetBytes(data);
                if (FlvTest.hasClient)
                {
                    try
                    {
                        FlvTest.mysock.Send(info);
                    }
                    catch (System.Exception ex)
                    {
                        //MessageBox.Show(ex.Message.ToString());
                        Log.Error(Environment.StackTrace, ex);
                    }
                }


                return 0;
            }

            public static byte[] ReceiveVarData(Socket s) // return array that store the received data.
            {
                int total = 0;
                int recv;
                byte[] datasize = new byte[4];
                recv = s.Receive(datasize, 0, 4, SocketFlags.None);//receive the size of data array for initialize a array.
                int size = BitConverter.ToInt32(datasize, 0);
                int dataleft = size;
                byte[] data = new byte[size];

                while (total < size)
                {
                    recv = s.Receive(data, total, dataleft, SocketFlags.None);
                    if (recv == 0)
                    {
                        data = null;
                        break;
                    }
                    total += recv;
                    dataleft -= recv;
                }

                return data;

            }
        }


        /********************************************************************
                           对Pcap包分析，得到Web的相关信息
         * *********************************************************************/
        [DllImport("VSMFlv.dll")]
        public extern static int VideoStreamMediaFlv(string strPcapFileName);
        private void WebInfoAnalys()
        {
            int AnalysOK = 0;
            //清空上次分析留存的临时文件
            ClearTmpFile();
            //InitWebChart();

            try
            {
                AnalysOK = VideoStreamMediaFlv(PcapFileName);
            }
            catch (System.Exception ex)
            {
                Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
            }

            if (AnalysOK == -6)
            {
                IsAnalysed = true;
                if (File.Exists("NoHttpDetectedError")) File.Delete("NoHttpDetectedError");
                WrongReason += " 数据包传输不是采用http协议传输，web解析无法完成 \n";
                return;
            }
            else if (AnalysOK == -7)
            {
                //这种情况现在不会存在
                IsAnalysed = true;
                if (File.Exists("FlvRestoreFailed.txt")) File.Delete("FlvRestoreFailed.txt");
                WrongReason += "传输过程有关键数据包丢失，web解析无法完成 \n";
                return;
            }
            else if (AnalysOK == -8)
            {
                IsAnalysed = true;
                if (File.Exists("NoFlvDetected.txt")) File.Delete("NoFlvDetected.txt");
                WrongReason += "播放文件不是flv/f4v/hlv格式，web解析无法完成 \n";
                return;
            }
            else if (!((File.Exists("flv_tag.txt")) && (File.Exists("data_flow_smoothed.txt")) && (File.Exists("data_flow_unsmoothed.txt")) && (File.Exists("play_flow_unsmoothed.txt")) && (File.Exists("play_flow_smoothed.txt")) && (File.Exists("user_event.txt"))))
            {
                //这种情况现在也不会存在
                IsAnalysed = true;
                WrongReason += "web解析异常，得到的数据信息不完整 \n";
                return;
            }

            //根据调用VideoStreamMediaFlv(string strPcapFileName)生成的7个txt文件进一步分析，写入到LV中，或者画图

            //rdAnaly("data_flow_unsmoothed.txt", this.LVDataFlow);
            //rdAnaly("play_flow_unsmoothed.txt", this.LVPlayFlow);
            //rdAnaly("flv_tag.txt", this.LVFlvTag);
            rdAnaly("data_flow_smoothed.txt");
            rdAnaly("play_flow_smoothed.txt");
            rdAnaly("flv_tag.txt");

            //计算网络流和视频流的最值和均值
            FileStream fs1 = new FileStream("data_flow_smoothed.txt", FileMode.Open, FileAccess.Read);
            FileStream fs2 = new FileStream("play_flow_smoothed.txt", FileMode.Open, FileAccess.Read);

            StreamReader srNet = new StreamReader(fs1, Encoding.Default);//网络流
            StreamReader srVideo = new StreamReader(fs2, Encoding.Default);//视频流
            long sumNetDataAvg1 = 0;
            long sumNetDataMax1 = 0;
            long sumNetDataMin1 = 0;
            long sumVideoDataAvg1 = 0;
            long sumVideoDataMax1 = 0;
            long sumVideoDataMin1 = 0;

            long realNetDataAvg1 = 0;
            long realNetDataMax1 = 0;
            long realNetDataMin1 = 0;
            long realVideoDataAvg1 = 0;
            long realVideoDataMax1 = 0;
            long realVideoDataMin1 = 0;

            string str = srNet.ReadLine();
            string str1 = srVideo.ReadLine();
            int i = 0;
            int j = 0;
            str = srNet.ReadLine();
            str1 = srVideo.ReadLine();
            string[] strNet = str.Split('\t');
            string[] strVideo = str1.Split('\t');
            sumNetDataMax1 = sumNetDataMin1 = Convert.ToInt32(strNet[2]);
            realNetDataMax1 = realNetDataMin1 = Convert.ToInt32(strNet[1]);
            sumNetDataAvg1 = realNetDataAvg1 = 0;

            sumVideoDataMax1 = sumVideoDataMin1 = Convert.ToInt32(strVideo[2]);
            realVideoDataMax1 = realVideoDataMin1 = Convert.ToInt32(strVideo[1]);
            sumVideoDataAvg1 = realVideoDataAvg1 = 0;
            while (str != null && (!str.Equals("错误:之后的数据传送出现错误，无法继续进行播放流量统计")))
            {
                i++;
                strNet = str.Split('\t');
                if (Convert.ToInt32(strNet[2]) >= sumNetDataMax1)
                    sumNetDataMax1 = Convert.ToInt32(strNet[2]);
                if (Convert.ToInt32(strNet[2]) <= sumNetDataMin1)
                    sumNetDataMin1 = Convert.ToInt32(strNet[2]);

                if (Convert.ToInt32(strNet[1]) >= realNetDataMax1)
                    realNetDataMax1 = Convert.ToInt32(strNet[1]);
                if (Convert.ToInt32(strNet[1]) <= realNetDataMin1)
                    realNetDataMin1 = Convert.ToInt32(strNet[1]);
                sumNetDataAvg1 += Convert.ToInt32(strNet[2]) / 1024;
                realNetDataAvg1 += Convert.ToInt32(strNet[1]) / 1024;

                str = srNet.ReadLine();
                if (srNet.Peek() < 0)
                    break;

            }
            sumNetDataAvg1 /= i;
            realNetDataAvg1 /= i;
            while (str1 != null && (!str1.Equals("错误:之后的数据传送出现错误，无法继续进行播放流量统计")))
            {
                j++;
                strVideo = str1.Split('\t');
                if (Convert.ToInt32(strVideo[2]) >= sumVideoDataMax1)
                    sumVideoDataMax1 = Convert.ToInt32(strVideo[2]);
                if (Convert.ToInt32(strVideo[2]) <= sumVideoDataMin1)
                    sumVideoDataMin1 = Convert.ToInt32(strVideo[2]);

                if (Convert.ToInt32(strVideo[1]) >= realVideoDataMax1)
                    realVideoDataMax1 = Convert.ToInt32(strVideo[1]);
                if (Convert.ToInt32(strVideo[1]) <= realVideoDataMin1)
                    realVideoDataMin1 = Convert.ToInt32(strVideo[1]);
                sumVideoDataAvg1 += Convert.ToInt32(strVideo[2]) / 1024;
                realVideoDataAvg1 += Convert.ToInt32(strVideo[1]) / 1024;

                str1 = srVideo.ReadLine();
                if (srVideo.Peek() < 0)
                    break;

            }
            sumVideoDataAvg1 /= j;
            realVideoDataAvg1 /= j;

            //显示最值和均值
            //this.sumNetDataMax.Text += (sumNetDataMax1 / 1024).ToString() + "(KB)";
            //this.sumNetDataMin.Text += (sumNetDataMin1 / 1024).ToString() + "(KB)";
            //this.sumNetDataAvg.Text += sumNetDataAvg1.ToString() + "(KB)";
            //this.sumVideoDataMax.Text += (sumVideoDataMax1 / 1024).ToString() + "(KB)";
            //this.sumVideoDataMin.Text += (sumVideoDataMin1 / 1024).ToString() + "(KB)";
            //this.sumVideoDataAvg.Text += sumVideoDataAvg1.ToString() + "(KB)";

            //this.realNetDataAvg.Text += realNetDataAvg1.ToString() + "(KB)";
            //this.realNetDataMax.Text += (realNetDataMax1 / 1024).ToString() + "(KB)";
            //this.realNetDataMin.Text += (realNetDataMin1 / 1024).ToString() + "(KB)";
            //this.realVideoDataAvg.Text += realVideoDataAvg1.ToString() + "(KB)";
            //this.realVideoDataMax.Text += (realVideoDataMax1 / 1024).ToString() + "(KB)";
            //this.realVideoDataMin.Text += (realVideoDataMin1 / 1024).ToString() + "(KB)";

            //this.sumNetDataAvg.Visible = true;
            //this.sumNetDataMax.Visible = true;
            //this.sumNetDataMin.Visible = true;
            //this.sumVideoDataMax.Visible = true;
            //this.sumVideoDataAvg.Visible = true;
            //this.sumVideoDataMin.Visible = true;

            //this.realNetDataAvg.Visible = true;
            //this.realNetDataMax.Visible = true;
            //this.realNetDataMin.Visible = true;
            //this.realVideoDataMax.Visible = true;
            //this.realVideoDataMin.Visible = true;
            //this.realVideoDataAvg.Visible = true;

            //调节坐标轴上限
            double maxY, minY;
            double maxX, minX;

            //确定ChartDataPlayFlow图第一部分的横纵轴

            maxY = this.ChartAccumulatedTraffic.Series["累积网络数据流"].Points.FindMaxValue("Y1").YValues[0] > this.ChartAccumulatedTraffic.Series["累积视频播放流"].Points.FindMaxValue("Y1").YValues[0] ? this.ChartAccumulatedTraffic.Series["累积网络数据流"].Points.FindMaxValue("Y1").YValues[0] : this.ChartAccumulatedTraffic.Series["累积视频播放流"].Points.FindMaxValue("Y1").YValues[0];
            this.ChartAccumulatedTraffic.ChartAreas[0].AxisY.Maximum = ((int)(maxY / 100) + 1) * 100;
            minY = this.ChartAccumulatedTraffic.Series["累积网络数据流"].Points.FindMinValue("Y1").YValues[0] > this.ChartAccumulatedTraffic.Series["累积视频播放流"].Points.FindMinValue("Y1").YValues[0] ? this.ChartAccumulatedTraffic.Series["累积网络数据流"].Points.FindMinValue("Y1").YValues[0] : this.ChartAccumulatedTraffic.Series["累积视频播放流"].Points.FindMinValue("Y1").YValues[0];
            this.ChartAccumulatedTraffic.ChartAreas[0].AxisY.Minimum = ((int)(minY / 100) + 1) * 100;
            maxX = this.ChartAccumulatedTraffic.Series["累积网络数据流"].Points.FindMaxValue("X").XValue > this.ChartAccumulatedTraffic.Series["累积视频播放流"].Points.FindMaxValue("X").XValue ? this.ChartAccumulatedTraffic.Series["累积网络数据流"].Points.FindMaxValue("X").XValue : this.ChartAccumulatedTraffic.Series["累积视频播放流"].Points.FindMaxValue("X").XValue;
            this.ChartAccumulatedTraffic.ChartAreas[0].AxisX.Maximum = maxX + 5;
            minX = this.ChartAccumulatedTraffic.Series["累积网络数据流"].Points.FindMinValue("X").XValue > this.ChartAccumulatedTraffic.Series["累积视频播放流"].Points.FindMinValue("X").XValue ? this.ChartAccumulatedTraffic.Series["累积网络数据流"].Points.FindMinValue("X").XValue : this.ChartAccumulatedTraffic.Series["累积视频播放流"].Points.FindMinValue("X").XValue;
            this.ChartAccumulatedTraffic.ChartAreas[0].AxisX.Minimum = minX + 5;

            //确定ChartDataPlayFlow图第二部分的横纵轴

            maxY = this.ChartRealTraffic.Series["实时网络数据流"].Points.FindMaxValue("Y1").YValues[0] > this.ChartRealTraffic.Series["实时视频播放流"].Points.FindMaxValue("Y1").YValues[0] ? this.ChartRealTraffic.Series["实时网络数据流"].Points.FindMaxValue("Y1").YValues[0] : this.ChartRealTraffic.Series["实时视频播放流"].Points.FindMaxValue("Y1").YValues[0];
            this.ChartRealTraffic.ChartAreas[0].AxisY.Maximum = ((int)(maxY / 100) + 1) * 100;
            minY = this.ChartRealTraffic.Series["实时网络数据流"].Points.FindMinValue("Y1").YValues[0] > this.ChartRealTraffic.Series["实时视频播放流"].Points.FindMinValue("Y1").YValues[0] ? this.ChartRealTraffic.Series["实时网络数据流"].Points.FindMinValue("Y1").YValues[0] : this.ChartRealTraffic.Series["实时视频播放流"].Points.FindMinValue("Y1").YValues[0];
            this.ChartRealTraffic.ChartAreas[0].AxisY.Minimum = ((int)(minY / 100) + 1) * 100;
            maxX = this.ChartRealTraffic.Series["实时网络数据流"].Points.FindMaxValue("X").XValue > this.ChartRealTraffic.Series["实时视频播放流"].Points.FindMaxValue("X").XValue ? this.ChartRealTraffic.Series["实时网络数据流"].Points.FindMaxValue("X").XValue : this.ChartRealTraffic.Series["实时视频播放流"].Points.FindMaxValue("X").XValue;
            this.ChartRealTraffic.ChartAreas[0].AxisX.Maximum = maxX + 5;
            minX = this.ChartRealTraffic.Series["实时网络数据流"].Points.FindMinValue("X").XValue > this.ChartRealTraffic.Series["实时视频播放流"].Points.FindMinValue("X").XValue ? this.ChartRealTraffic.Series["实时网络数据流"].Points.FindMinValue("X").XValue : this.ChartRealTraffic.Series["实时视频播放流"].Points.FindMinValue("X").XValue;
            this.ChartRealTraffic.ChartAreas[0].AxisX.Minimum = minX + 5;


            //确定ChartVideoFrameSeq图的横纵轴
            maxY = minY = maxX = minX = 0;
            foreach (Series iSeries in this.ChartSequence.Series)
            {
                //必须加上点数判断，不然可能会出现“未将应用的对象赋给---”
                if (iSeries.Points.Count == 0)
                    continue;
                if (iSeries.Points.FindMaxValue("Y1").YValues[0] > maxY)
                    maxY = iSeries.Points.FindMaxValue("Y1").YValues[0];
                if (iSeries.Points.FindMinValue("Y1").YValues[0] < minY)
                    minY = iSeries.Points.FindMinValue("Y1").YValues[0];

                if (iSeries.Points.FindMaxValue("X").XValue > maxX)
                    maxX = iSeries.Points.FindMaxValue("X").XValue;
                if (iSeries.Points.FindMinValue("X").XValue < maxX)
                    minX = iSeries.Points.FindMinValue("X").XValue;

            }
            maxX = 5 - maxX % 5 + maxX;
            minX = 5 - minX % 5 + minX;
            this.ChartSequence.ChartAreas[0].AxisY.Maximum = ((int)(maxY / 100) + 1) * 100;
            this.ChartSequence.ChartAreas[0].AxisY.Minimum = ((int)(minY / 100) + 1) * 100;
            this.ChartSequence.ChartAreas[0].AxisX.Maximum = maxX;
            this.ChartSequence.ChartAreas[0].AxisX.Minimum = minX;
            this.ChartSequence.ChartAreas[0].AxisX.Interval = (maxX / 5 > 0) ? (maxX / 5) : 1;

            //使能画图区域的滚动条
            this.EnableScroll(ChartAccumulatedTraffic.ChartAreas[0]);
            this.EnableScroll(ChartRealTraffic.ChartAreas[0]);
            this.EnableScroll(ChartSequence.ChartAreas[0]);

            //图像重构
            this.ChartAccumulatedTraffic.Invalidate();
            this.ChartRealTraffic.Invalidate();
            this.ChartSequence.Invalidate();

            return;
        }

        private void rdAnaly(string strfile)
        {
            if (!File.Exists(strfile))
            {
                //为了让程序界面更加友好，把弹出的Messagebox去掉了！
                return;
            }

            bool GiveUpLastLine = false;
            if ((strfile == "play_flow_smoothed.txt") || (strfile == "flv_tag.txt"))
                GiveUpLastLine = true;

            FileStream fs1 = new FileStream(strfile, FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(fs1, Encoding.Default);

            if (strfile.Equals("flv_tag.txt"))
            {
                sr.ReadLine();
                sr.ReadLine();
                sr.ReadLine();
            }

            char[] seper = { '\t', ' ' };
            sr.ReadLine();
            while (sr.Peek() >= 0)
            {
                string[] ld = sr.ReadLine().Split(seper);

                if (GiveUpLastLine)
                {
                    if (sr.Peek() < 0) break;  //滤除掉最后一行
                }

                if (strfile.Equals("data_flow_smoothed.txt"))
                {
                    this.ChartAccumulatedTraffic.Invoke(addDataDel, this.ChartAccumulatedTraffic, this.ChartAccumulatedTraffic.Series["累积网络数据流"], Convert.ToDouble(ld[0]), Convert.ToDouble(ld[2]) / 1000);
                    this.ChartRealTraffic.Invoke(addDataDel, this.ChartRealTraffic, this.ChartRealTraffic.Series["实时网络数据流"], Convert.ToDouble(ld[0]), Convert.ToDouble(ld[1]) / 1000);
                }
                else if (strfile.Equals("play_flow_smoothed.txt"))
                {
                    this.ChartAccumulatedTraffic.Invoke(addDataDel, this.ChartAccumulatedTraffic, this.ChartAccumulatedTraffic.Series["累积视频播放流"], Convert.ToDouble(ld[0]), Convert.ToDouble(ld[2]) / 1000);
                    this.ChartRealTraffic.Invoke(addDataDel, this.ChartRealTraffic, this.ChartRealTraffic.Series["实时视频播放流"], Convert.ToDouble(ld[0]), Convert.ToDouble(ld[1]) / 1000);
                }
                else if (strfile.Equals("flv_tag.txt"))
                {
                    double x = Convert.ToDouble(ld[0]);

                    if (ld[3].Equals("I视频帧"))
                    {
                        this.ChartSequence.Series[0].Points.AddXY(x, Convert.ToDouble(ld[4]));
                        this.ChartSequence.Series[2].Points.AddXY(x, Convert.ToDouble(ld[5]));
                    }
                    else if (ld[3].Equals("P视频帧"))
                    {
                        this.ChartSequence.Series[1].Points.AddXY(x, Convert.ToDouble(ld[4]));
                        this.ChartSequence.Series[3].Points.AddXY(x, Convert.ToDouble(ld[5]));
                    }
                }
            }
            sr.Close();
            fs1.Close();

        }

        private void rdAnaly(string strfile, ListView lv)
        {
            if (!File.Exists(strfile))
            {
                //为了让程序界面更加友好，把弹出的Messagebox去掉了！
                return;
            }

            bool GiveUpLastLine = false;
            if ((strfile == "play_flow_unsmoothed.txt") || (strfile == "flv_tag.txt"))
                GiveUpLastLine = true;

            FileStream fs1 = new FileStream(strfile, FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(fs1, Encoding.Default);

            if (strfile == "flv_tag.txt")
            {
                AverValue.FrameRate = sr.ReadLine();
                AverValue.FrameRate += ("\r\n" + sr.ReadLine());
                AverValue.FrameRate += ("\r\n" + sr.ReadLine());
            }

            char[] seper = { '\t', ' ' };
            sr.ReadLine();
            while (sr.Peek() >= 0)
            {
                string[] ld = sr.ReadLine().Split(seper);
                if (GiveUpLastLine)
                {
                    if (sr.Peek() < 0) break;  //滤除掉最后一行
                }
                ListViewItem lvItem = new ListViewItem();
                for (int i = 0; i < ld.Length; i++)
                {
                    if (i == 0)
                        lvItem.SubItems[0].Text = ld[i];
                    else
                        lvItem.SubItems.Add(ld[i]);
                }
                lv.Items.Add(lvItem);

            }
            sr.Close();
            fs1.Close();

        }

        //清除掉上次分析生成的txt文件,这些文件仅限WebInfoAnalys
        private void ClearTmpFile()
        {
            string[] strTmpfile = { "data_flow_smoothed.txt", "data_flow_unsmoothed.txt", "play_flow_unsmoothed.txt", "play_flow_smoothed.txt", "flv_tag.txt", "user_event.txt", "FlvMetaData.txt" };

            foreach (string str in strTmpfile)
            {
                if (File.Exists(str))
                {
                    File.Delete(str);
                }
            }

        }

        //清除上一次的chart绘图
        private void InitWebChart()
        {
            this.ChartAccumulatedTraffic.Invoke(clearDataDel, this.ChartAccumulatedTraffic);
            this.ChartRealTraffic.Invoke(clearDataDel, this.ChartRealTraffic);
            this.ChartSequence.Invoke(clearDataDel, this.ChartSequence);
        }

        /***************************************************************
              对Pcap包分析，得到文件概要、数据包、TCP、DNS、HTTP等信息
          *****************************************************************/
        private void PcapTcpDnsHttpAnalys()
        {
            int i = -1;
            try
            {
                i = pcap_file_dissect_inCS(PcapFileName);
            }
            catch (System.Exception ex)
            {
                i = -1;
                Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
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
                    Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
                }
                WrongReason = "数据包打开错误，无法进行文件概要、数据包、TCP、DNS、HTTP的分析 \n";
                return;
            }
            else
            {
                if (!ShowLVSum()) WrongReason += "文件概要分析异常 \n";
                if (!ShowLVDNSAnalys()) WrongReason += "DNS分析异常 \n";
                if (!ShowLVHTTPAnalys()) WrongReason += "HTTP分析异常 \n";
                try
                {
                    DelayJitterAnalys();
                }
                catch
                {
                    WrongReason += "无法获得延时抖动信息\n";
                }
                if (!ShowTCPStream()) WrongReason += "TCP流分析异常 \n";
                if (!ShowLVPacketAnalys()) WrongReason += "数据包解析异常 \n";
            }
            try
            {
                pcap_file_close_inCS();
            }
            catch (System.Exception ex)
            {
                Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
            }
            return;

        }

        //打开Pcap文件(用于文件概要、数据包解析、TCP解析、DNS解析、HTTP解析)
        [DllImport("NetpryDll.dll")]
        public extern static int pcap_file_dissect_inCS(string pathfilename);

        //关闭Pcap文件(用于文件概要、数据包解析、TCP解析、DNS解析、HTTP解析)
        [DllImport("NetpryDll.dll")]
        public extern static void pcap_file_close_inCS();

        //向文件概要列表填充数据函数
        [DllImport("NetpryDll.dll")]
        //public extern static int pf_summary_tostr(string tmpfileName);
        public extern static int pf_summary_tofile(string tmpfilename);
        private bool ShowLVSum()
        {
            //清除原有的项
            LVSum.Items.Clear();

            //临时文件名称
            string tmpfileName = "dissectSum.tmp";

            //创建临时文件
            FileStream fs = File.Create(tmpfileName);
            fs.Close();

            //调用文件概要信息获取函数
            int retCode = -1;
            try
            {
                //retCode = pf_summary_tostr(tmpfileName);
                retCode = pf_summary_tofile(tmpfileName);
            }
            catch (System.Exception ex)
            {
                retCode = -1;
                Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
            }

            if (retCode < 0)
            {
                return false;
            }

            //创建文件读流
            StreamReader sr = new StreamReader(tmpfileName, Encoding.Default);
            string strLine = sr.ReadLine();

            //属性字符串
            string[] propoties = new string[]{"文件名", "文件长度(字节)", "链路类型",
                "第一个数据包到达时间", "最后一个数据包到达时间", "持续时间(秒)", 
                "数据包总个数", "总数据流量(字节)", "捕获主机IP地址", "捕获主机MAC地址"};
            int propo_index = 0;

            //ListView数据项和子数据项
            ListViewItem lvi;
            ListViewItem.ListViewSubItem lvsi;

            //读取每一行数据
            while (strLine != null)
            {
                lvi = new ListViewItem();
                lvi.Text = propoties[propo_index++];

                lvsi = new ListViewItem.ListViewSubItem();
                lvsi.Text = strLine;
                lvi.SubItems.Add(lvsi);

                //加入ListView
                ListView.CheckForIllegalCrossThreadCalls = false;
                LVSum.Items.Add(lvi);

                strLine = sr.ReadLine();
            }
            sr.Close();

            //删除临时文件
            File.Delete(tmpfileName);
            return true;
        }

        //向数据包列表填充数据函数
        [DllImport("NetpryDll.dll")]
        public extern static int pcb_list_tofile(string tmpfileName);
        [DllImport("NetpryDll.dll")]
        public extern static int getPacketNum();

        private bool ShowLVPacketAnalys()
        {

            //清除原有的项
            LVPacketAnalys.Items.Clear();

            //临时文件名称
            string tmpfileName = "dissectPacket.tmp";

            //删除上一次的临时文件（和其他模块不同，记录太多要分页）
            if (File.Exists(tmpfileName))
            {
                File.Delete(tmpfileName);
            }

            //创建临时文件
            FileStream fs = File.Create(tmpfileName);
            fs.Close();

            //调用数据包解析信息获取函数
            int retCode = -1;
            try
            {
                retCode = pcb_list_tofile(tmpfileName);             //将内存中的包链表写入文件中
            }
            catch (System.Exception ex)
            {
                retCode = -1;
                Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
            }

            if (retCode < 0)
            {
                return false;
            }


            currentPage = 1;    //每次分析都将页码初始化为1
            //这里要用接口获取包总数
            totalNum = getPacketNum();
            //totalNum=200000;
            if (totalNum / PACKETPAGESIZE > 0)
            {
                if (totalNum % PACKETPAGESIZE == 0)
                {
                    pageNum = totalNum / PACKETPAGESIZE;
                }
                else
                    pageNum = totalNum / PACKETPAGESIZE + 1;
            }
            else
                pageNum = 1;
            //初始化时上一页不能用，下一页要判断总页数和每一页的记录数2000的大小
            if (pageNum > 1)
            {
                btnNextPage.Enabled = true;
            }

            btnJump.Enabled = true;     //使能跳转，在响应函数里判断输入范围

            labelTotal.Text = "总记录数:" + totalNum + " 总页数:" + pageNum;
            labelTotal.Enabled = true;

            comboxJumpPage.Items.Clear();
            comboxJumpPage.Enabled = true;    //初始选择combox
            for (int i = 0; i < pageNum; i++)
            {
                comboxJumpPage.Items.Add(i + 1);
            }
            comboxJumpPage.SelectedIndex = 0;

            getPageRecord(tmpfileName, currentPage);

            //删除临时文件(修改后不删除，用于分页)
            // File.Delete(tmpfileName);
            return true;

        }

        //向TCP流列表和TCP流异常列表填充数据函数
        //TCP流信息获取函数
        [DllImport("NetpryDll.dll")]
        public extern static int tcp_stream_tofile(string tmpfileName);
        //TCP流异常信息获取函数
        [DllImport("NetpryDll.dll")]
        public extern static int tcps_exception_tofile(string tmpfileName);
        [DllImport("NetpryDll.dll")]
        public extern static int getTcpStreamNum();
        private bool ShowTCPStream()
        {


            /**********填充TCP常规列表**********/
            //清除原有的项
            LVTCPGeneral.Items.Clear();

            //临时文件名称
            string tmpfileName = "dissectTcp.tmp";

            //删除上一次的临时文件（和其他模块不同，记录太多要分页）
            if (File.Exists(tmpfileName))
            {
                File.Delete(tmpfileName);
            }

            //创建临时文件
            FileStream fs = File.Create(tmpfileName);
            fs.Close();

            //调用TCP流信息获取函数             
            int retCode = -1;
            try
            {
                retCode = tcp_stream_tofile(tmpfileName);
            }
            catch (System.Exception ex)
            {
                retCode = -1;
                Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
            }

            if (retCode < 0)
            {
                return false;
            }

            currentPageTcpGene = 1;    //每次分析都将页码初始化为1
            //这里要用接口获取包总数
            totalNumTcpGene = getTcpStreamNum();
            //totalNumTcpGene = 200000;
            if (totalNumTcpGene / PACKETPAGESIZE > 0)
            {
                if (totalNumTcpGene % PACKETPAGESIZE == 0)
                {
                    pageNumTcpGene = totalNumTcpGene / PACKETPAGESIZE;
                }
                else
                    pageNumTcpGene = totalNumTcpGene / PACKETPAGESIZE + 1;
            }
            else
                pageNumTcpGene = 1;
            //初始化时上一页不能用，下一页要判断总页数和每一页的记录数2000的大小
            if (pageNumTcpGene > 1)
            {
                btnNextPageTcpGene.Enabled = true;
            }

            btnJumpTcpGene.Enabled = true;     //使能跳转，在响应函数里判断输入范围

            labelTotalTcpGene.Text = "总记录数:" + totalNumTcpGene + "总页数:" + pageNumTcpGene;
            labelTotalTcpGene.Enabled = true;

            comboxJumpPageTcpGene.Items.Clear();
            comboxJumpPageTcpGene.Enabled = true;    //初始选择combox
            for (int i = 0; i < pageNumTcpGene; i++)
            {
                comboxJumpPageTcpGene.Items.Add(i + 1);
            }
            comboxJumpPageTcpGene.SelectedIndex = 0;

            getPageRecordTcpGene(tmpfileName, currentPageTcpGene);   //获取前面2000条记录

            //创建文件读流
            StreamReader sr = new StreamReader(tmpfileName);
            string strLine = sr.ReadLine();

            //记录tcp连接的各种参数值
            int tcpconnecttimes = 0;
            int tcpuptimes = 0;
            int tcpdowntimes = 0;
            int tcpupflow = 0;
            int tcpdownflow = 0;
            double averrtt = 0.0;

            //读取每一行数据
            while (strLine != null)
            {
                //得到每一单元数据
                string[] str = strLine.Split(new Char[] { '\t' }, 12);

                //记录TCP连接的参数值
                tcpconnecttimes++;
                tcpuptimes += int.Parse(str[5]);
                tcpupflow += int.Parse(str[6]);
                tcpdowntimes += int.Parse(str[7]);
                tcpdownflow += int.Parse(str[8]);
                averrtt += double.Parse(str[11]);

                strLine = sr.ReadLine();

            }


            sr.Close();

            //删除临时文件
            //File.Delete(tmpfileName);

            //获取tcp连接信息，写入最后的日志文件中
            averrtt /= tcpconnecttimes;
            averrtt = Math.Round(averrtt, 3);
            AverValue.TcpInfo = "TCP连接信息如下：\t\r\n";
            AverValue.TcpInfo += "TCP连接个数 \t" + tcpconnecttimes.ToString() + "\t\r\n";
            AverValue.TcpInfo += "TCP上行包个数 \t" + tcpuptimes.ToString() + "\t\r\n";
            AverValue.TcpInfo += "TCP上行包流量(字节)  \t" + tcpupflow.ToString() + "\t\r\n";
            AverValue.TcpInfo += "TCP下行包个数 \t" + tcpdowntimes.ToString() + "\t\r\n";
            AverValue.TcpInfo += "TCP下行包流量(字节)  \t" + tcpdownflow.ToString() + "\t\r\n";
            AverValue.TcpInfo += "RTT均值(秒) \t" + averrtt.ToString() + "\t";


            /***********填充TCP异常列表********************/

            //清除原有的项
            LVTCPEx.Items.Clear();

            //临时文件名称
            tmpfileName = "dissectTcpExcep.tmp";

            //创建临时文件
            fs = File.Create(tmpfileName);
            fs.Close();

            //调用TCP流信息获取函数
            retCode = -1;
            try
            {
                retCode = tcps_exception_tofile(tmpfileName);
            }
            catch (System.Exception ex)
            {
                retCode = -1;
                Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);

            }

            if (retCode < 0)
            {
                return false;
            }

            //创建文件读流
            int groupCount = 50;
            StreamReader srExcep;
            string strLineExcep;
            srExcep = new StreamReader(tmpfileName);
            strLineExcep = srExcep.ReadLine();

            ListViewItem[] lvi;
            ListViewItem.ListViewSubItem lvsi;
            int lineCount;
            lvi = new ListViewItem[groupCount];
            lineCount = 0;

            int tcplastpaclost = 0;
            int reack = 0;
            int wrongorder = 0;
            int redeliver = 0;
            double redeliverrate = 0.0;

            int tcpCnt = 0;
            //读取每一行数据
            while (strLineExcep != null)
            {
                tcpCnt++;
                //得到每一单元数据
                string[] str = strLineExcep.Split(new Char[] { '\t' }, 10);

                lvi[lineCount] = new ListViewItem();
                lvi[lineCount].Text = str[0];

                for (int i = 1; i < 10; i++)
                {
                    lvsi = new ListViewItem.ListViewSubItem();
                    lvsi.Text = str[i];
                    lvi[lineCount].SubItems.Add(lvsi);
                }

                tcplastpaclost += int.Parse(str[5]);
                reack += int.Parse(str[6]);
                wrongorder += int.Parse(str[7]);
                redeliver += int.Parse(str[8]);
                redeliverrate += double.Parse(str[9]);

                strLineExcep = srExcep.ReadLine();
                lineCount++;

                if (lineCount % groupCount == 0)
                {
                    LVTCPEx.BeginUpdate();
                    //加入ListView
                    LVTCPEx.Items.AddRange(lvi);
                    LVTCPEx.EndUpdate();

                    lvi = new ListViewItem[groupCount];
                    lineCount = 0;
                }
            }

            LVTCPEx.BeginUpdate();
            //加入最后一批
            for (int i = 0; i < lineCount; i++)
            {
                LVTCPEx.Items.Add(lvi[i]);
            }
            LVTCPEx.EndUpdate();
            srExcep.Close();
            redeliverrate = Math.Round(redeliverrate * 100 / tcpCnt, 3);

            AverValue.TcpEx = "TCP异常信息如下：\t\r\n";
            AverValue.TcpEx += "前数据包丢失次数 \t" + tcplastpaclost.ToString() + "\t\r\n";
            AverValue.TcpEx += "重复确认次数 \t" + reack.ToString() + "\t\r\n";
            AverValue.TcpEx += "乱序次数 \t" + wrongorder.ToString() + "\t\r\n";
            AverValue.TcpEx += "重传次数 \t" + redeliver.ToString() + "\t\r\n";
            AverValue.TcpEx += "重传率 \t" + redeliverrate.ToString() + "%\t\r\n";
            //删除临时文件
            File.Delete(tmpfileName);
            return true;
        }

        //向DNS列表填充数据函数
        //DNS信息获取函数
        [DllImport("NetPryDll.dll")]
        public extern static int dns_anal_tofile(string tmpfileName);
        private bool ShowLVDNSAnalys()
        {

            //清除原有的项
            LVDNSAnalys.Items.Clear();

            //临时文件名称
            string tmpfileName = "dissectDNS.tmp";

            //创建临时文件
            FileStream fs = File.Create(tmpfileName);
            fs.Close();

            //调用DNS分析信息获取函数             
            int retCode = -1;
            try
            {
                //retCode = dns_anal_tostr(tmpfileName);
                retCode = dns_anal_tofile(tmpfileName);
            }
            catch (System.Exception ex)
            {
                retCode = -1;
                Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
            }

            if (retCode < 0)
            {
                return false;
            }

            //创建文件读流
            StreamReader sr = new StreamReader(tmpfileName);
            sr.ReadLine();
            string strLine = sr.ReadLine();
            //ListView数据项和子数据项
            ListViewItem[] lvi;
            ListViewItem.ListViewSubItem lvsi;

            const int groupCount = 50;
            lvi = new ListViewItem[groupCount];
            int lineCount = 0;

            //设置图像的横纵坐标
            double xValue = 1.0;
            double yValue = 0.0;
            bool yValueGet = false;
            // Set series chart type
            ChartDNS.Series["响应时间(秒)"].Type = SeriesChartType.Bar;
            // Set series point width
            ChartDNS.Series["响应时间(秒)"]["PointWidth"] = "1.0";
            // Show data points labels
            ChartDNS.Series["响应时间(秒)"].ShowLabelAsValue = false;
            // Set data points label style
            ChartDNS.Series["响应时间(秒)"]["BarLabelStyle"] = "Center";
            // Display chart as 3D
            ChartDNS.ChartAreas[0].Area3DStyle.Enable3D = false;
            // Draw the chart as embossed
            ChartDNS.Series["响应时间(秒)"]["DrawingStyle"] = "Emboss";

            //读取每一行数据
            while (strLine != null)
            {
                //得到每一单元数据
                string[] str = strLine.Split(new Char[] { '\t' });

                lvi[lineCount] = new ListViewItem();
                lvi[lineCount].Text = str[0];

                for (int i = 1; i < 9; i++)
                {
                    lvsi = new ListViewItem.ListViewSubItem();
                    lvsi.Text = str[i];
                    lvi[lineCount].SubItems.Add(lvsi);
                }

                strLine = sr.ReadLine();
                lineCount++;

                if (lineCount % groupCount == 0)
                {
                    LVDNSAnalys.BeginUpdate();
                    //加入ListView
                    LVDNSAnalys.Items.AddRange(lvi);
                    LVDNSAnalys.EndUpdate();

                    lvi = new ListViewItem[groupCount];
                    lineCount = 0;
                }

                yValueGet = double.TryParse(str[8], out yValue);
                if (!yValueGet) yValue = 0.0;
                AverValue.AverDNS += yValue;

                ChartDNS.Invoke(addDataDel, ChartDNS, ChartDNS.Series["响应时间(秒)"], xValue++, yValue);

            }
            if (xValue > 0)
                AverValue.AverDNS /= (int)xValue;
            AverValue.AverDNS = Math.Round(AverValue.AverDNS, 6);
            LVDNSAnalys.BeginUpdate();
            //加入最后一批
            for (int i = 0; i < lineCount; i++)
            {
                LVDNSAnalys.Items.Add(lvi[i]);
            }
            LVDNSAnalys.EndUpdate();
            sr.Close();

            //曲线图更新
            ChartDNS.Invalidate();
            Console.WriteLine("DNS into Mysql!");
            //txt文件压入到数据库
            //Application.StartupPath +"\\文件.txt"
            if (mysqlWebFlag && serverTest)
                mysqlWeb.TxTInsertMySQL("DNSAnalysis", currentId + "#" + "Video", Application.StartupPath + "\\" + tmpfileName);
            //删除临时文件,调试需要注释
#if RELEASE
            File.Delete(tmpfileName);
#endif
            return true;
        }

        //向HTTP列表填充数据函数
        //HTTP信息获取函数
        [DllImport("NetPryDll.dll")]
        public extern static int http_anal_tofile(string tmpfileName);
        private bool ShowLVHTTPAnalys()
        {

            //清除原有的项
            LVHTTPAnalys.Items.Clear();

            //临时文件名称
            string tmpfileName = "dissectHTTP.tmp";

            //创建临时文件
            FileStream fs = File.Create(tmpfileName);
            fs.Close();

            //调用HTTP分析信息获取函数,会生成dissect.tmp文件        
            int retCode = -1;
            try
            {
                retCode = http_anal_tofile(tmpfileName);      //这个函数应该是读取这一次的数据到临时文件中     
            }
            catch (System.Exception ex)
            {
                retCode = -1;
                Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
            }
            if (retCode < 0)
            {
                return false;
            }

            //创建文件读流
            StreamReader sr = new StreamReader(tmpfileName);
            string strLine = sr.ReadLine();

            //ListView数据项和子数据项
            ListViewItem[] lvi;
            ListViewItem.ListViewSubItem lvsi;

            const int groupCount = 50;
            lvi = new ListViewItem[groupCount];
            int lineCount = 0;
            int SumLine = 0;
            double HttpDelay = 0.0;
            bool HttpDelayGet = false;
            strLine = sr.ReadLine();

            //读取每一行数据
            while (strLine != null)
            {

                //得到每一单元数据
                //string[] str = strLine.Split(new Char[] { '\t' }, 7);
                string[] str = strLine.Split(new Char[] { '\t' });
                //lvi[lineCount] = new ListViewItem();  //序号
                //lvi[lineCount].Text = str[0];
                lvi[lineCount] = new ListViewItem();
                lvi[lineCount].UseItemStyleForSubItems = false;
                lvi[lineCount].Text = (SumLine + 1).ToString();

                //for (int i = 0; i < 8; i++)
                //{
                //    lvsi = new ListViewItem.ListViewSubItem();
                //    lvsi.Text = str[i];
                //    lvi[lineCount].SubItems.Add(lvsi);
                //}

                //lvsi = new ListViewItem.ListViewSubItem();  //添加序号
                //lvsi.Text = str[0];
                //lvi[lineCount].SubItems.Add(lvsi);
                //lvi[lineCount].SubItems[1].ForeColor = System.Drawing.Color.Red;

                lvsi = new ListViewItem.ListViewSubItem();  //添加客户端IP+端口
                lvsi.Text = str[1];
                lvi[lineCount].SubItems.Add(lvsi);
                lvi[lineCount].SubItems[1].ForeColor = System.Drawing.Color.Gray;

                lvsi = new ListViewItem.ListViewSubItem();  //添加交互方式
                lvsi.Text = str[2];
                lvi[lineCount].SubItems.Add(lvsi);
                lvi[lineCount].SubItems[2].ForeColor = System.Drawing.Color.Green;

                lvsi = new ListViewItem.ListViewSubItem();  //添加URL
                lvsi.Text = str[3];
                lvi[lineCount].SubItems.Add(lvsi);
                lvi[lineCount].SubItems[3].ForeColor = System.Drawing.Color.Green;

                lvsi = new ListViewItem.ListViewSubItem();  //添加服务器IP+端口
                lvsi.Text = str[4];
                lvi[lineCount].SubItems.Add(lvsi);
                lvi[lineCount].SubItems[4].ForeColor = System.Drawing.Color.Gray;

                lvsi = new ListViewItem.ListViewSubItem();  //添加版本号
                lvsi.Text = str[5];
                lvi[lineCount].SubItems.Add(lvsi);
                lvi[lineCount].SubItems[5].ForeColor = System.Drawing.Color.Gray;

                lvsi = new ListViewItem.ListViewSubItem();    //添加响应延时
                lvsi.Text = str[6];
                lvi[lineCount].SubItems.Add(lvsi);
                lvi[lineCount].SubItems[6].ForeColor = System.Drawing.Color.Gray;

                //lineCount++;

                //lvi[lineCount] = new ListViewItem();
                //lvi[lineCount].UseItemStyleForSubItems = false;

                //lvsi = new ListViewItem.ListViewSubItem();
                //lvsi.Text = str[0];
                //lvi[lineCount].SubItems.Add(lvsi);
                //lvi[lineCount].SubItems[1].ForeColor = System.Drawing.Color.Red;

                //lvsi = new ListViewItem.ListViewSubItem();
                //lvsi.Text = str[6] + " " + str[4]+" "+str[5];
                //lvi[lineCount].SubItems.Add(lvsi);
                //lvi[lineCount].SubItems[2].ForeColor = System.Drawing.Color.Blue;

                //lvsi = lvsi = new ListViewItem.ListViewSubItem();
                //lvsi.Text = str[1];
                //lvi[lineCount].SubItems.Add(lvsi);
                //lvi[lineCount].SubItems[3].ForeColor = System.Drawing.Color.Green;


                lineCount++;
                SumLine++;
                strLine = sr.ReadLine();
                //计算AverHTTP的值
                HttpDelayGet = double.TryParse((str[6]), out HttpDelay);
                if (!HttpDelayGet) HttpDelay = 0.0;
                AverValue.AverHTTP += HttpDelay;

                if (lineCount % groupCount == 0)
                {
                    LVHTTPAnalys.BeginUpdate();
                    //加入ListView
                    LVHTTPAnalys.Items.AddRange(lvi);
                    LVHTTPAnalys.EndUpdate();

                    lvi = new ListViewItem[groupCount];
                    lineCount = 0;
                }
            }

            LVHTTPAnalys.BeginUpdate();
            //加入最后一批
            for (int i = 0; i < lineCount; i++)
            {
                LVHTTPAnalys.Items.Add(lvi[i]);
            }
            LVHTTPAnalys.EndUpdate();
            sr.Close();

            //计算服务器响应时间
            if (SumLine > 0)
                AverValue.AverHTTP /= SumLine;
            AverValue.AverHTTP = Math.Round(AverValue.AverHTTP, 6);

            //txt文件压入到数据库  
            if (mysqlWebFlag && serverTest)
                mysqlWeb.TxTInsertMySQL("HttpAnalysis", currentId + "#" + "Video", Application.StartupPath + "\\" + tmpfileName);
            //删除临时文件
#if RELEASE
            File.Delete(tmpfileName);
#endif
            return true;
        }

        /********************************************************************************
                              对Pcap包分析，得到吞吐量和帧长列表           
          ********************************************************************************/
        private void InOutFrameLenAnalys()
        {
            ScaleComboBox.SelectedIndex = 0;
            if (!ShowLVInOut(PcapFileName, ScaleComboBox.SelectedIndex))
            {
                WrongReason += "吞吐量分析异常 \n";
            }

            if (!ShowLVFrameLength())
            {
                WrongReason += "帧长分布分析异常 \n";
            }

            return;
        }

        //引入pcap文件解析函数(Link)，负责吞吐量和帧长分布列表
        [DllImport("LinkAnal.dll")]
        public extern static int link_analyze_inCS(string PcapFile, double scale, string tmpfileName,
    ref int totalPktCount, int[] rangeCount);
        //向吞吐量列表添加数据
        private bool ShowLVInOut(string PcapFile, int selectIndex)
        {
            int index = selectIndex;
            double scale = timeScale[index];
            string tmpfileName = "LinkAnal.tmp";
            int SumLine = 0;

            //创建临时文本文件 
            FileStream fs = File.Create(tmpfileName);
            fs.Close();

            //调用链路分析函数,会生成LinkAnal.tmp文件
            int retCode = -1;
            try
            {
                retCode = link_analyze_inCS(PcapFile, scale, tmpfileName, ref totalPacketCnt, rangeCount);
            }
            catch (System.Exception ex)
            {
                retCode = -1;
                Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
            }
            if (retCode < 0)
            {
                //删除临时文件
                File.Delete(tmpfileName);
                return false;
            }



            //清空列表
            LVInOut.Items.Clear();

            ListViewItem lvi;
            ListViewItem.ListViewSubItem lvsi;

            //创建文件读流
            StreamReader sr = new StreamReader(tmpfileName);
            string strLine = sr.ReadLine();
            int counter = 1;
            //设置图像曲线上点的横纵坐标
            double xValue = 0.0;
            double yValue = 0.0;
            bool yValueGet = false;
            double MaxInOut = double.Parse(strLine);
            double MinInOut = double.Parse(strLine);

            //读取每一行数据
            while (strLine != null)
            {
                lvi = new ListViewItem();
                lvi.Text = (counter++ * scale).ToString();

                lvsi = new ListViewItem.ListViewSubItem();
                lvsi.Text = strLine;
                lvi.SubItems.Add(lvsi);
                //吞吐量最值
                if (Convert.ToDouble(strLine) >= MaxInOut)
                    MaxInOut = Convert.ToDouble(strLine);
                if (Convert.ToDouble(strLine) <= MinInOut)
                    MinInOut = Convert.ToDouble(strLine);

                //加入ListView
                LVInOut.Items.Add(lvi);

                yValueGet = double.TryParse(strLine, out yValue);
                if (!yValueGet) yValue = 0.0;
                AverValue.AverInOut += yValue;
                SumLine++;
                xValue += scale;
                ChartInOut.Invoke(addDataDel, ChartInOut, ChartInOut.Series["吞吐量曲线"], xValue, yValue);

                strLine = sr.ReadLine();

            }
            sr.Close();

            //计算吞吐量均值
            if (SumLine > 0)
            {
                if (index == 1) SumLine /= 10;
                else if (index == 2) SumLine /= 100;
                AverValue.AverInOut /= SumLine;
            }
            AverValue.AverInOut = Math.Round(AverValue.AverInOut, 2);

            //显示吞吐量最值和均值
            this.InOutMax.Text += MaxInOut.ToString() + "字节";
            this.InOutMin.Text += MinInOut.ToString() + "字节";
            this.InOutAvg.Text += AverValue.AverInOut.ToString() + "字节";
            this.InOutAvg.Visible = true;
            this.InOutMax.Visible = true;
            this.InOutMin.Visible = true;

            //确定横纵坐标轴的尺寸
            double maxY = 0;
            double minY = 0;
            double maxX = 0;
            double minX = 0;
            foreach (Series iSeries in this.ChartInOut.Series)
            {
                if (iSeries.Points.Count == 0)
                    continue;
                if (iSeries.Points.FindMaxValue("Y1").YValues[0] > maxY)
                    maxY = iSeries.Points.FindMaxValue("Y1").YValues[0];
                if (iSeries.Points.FindMinValue("Y1").YValues[0] < minY)
                    minY = iSeries.Points.FindMinValue("Y1").YValues[0];

                if (iSeries.Points.FindMaxValue("X").XValue > maxX)
                    maxX = iSeries.Points.FindMaxValue("X").XValue;
                if (iSeries.Points.FindMinValue("X").XValue < maxX)
                    minX = iSeries.Points.FindMinValue("X").XValue;
            }

            //确定横纵轴的最大小值,起止点的值为5的倍数
            //minX = 5 - minX % 5 + minX;
            minX = 0;
            maxX = 5 + maxX % 5 + maxX;
            minY = 5 - minY % 5 + minY;
            maxY = 5 - maxY % 5 + maxY;

            this.ChartInOut.ChartAreas[0].AxisX.Minimum = minX;
            this.ChartInOut.ChartAreas[0].AxisX.Maximum = maxX;
            this.ChartInOut.ChartAreas[0].AxisY.Minimum = minY;
            this.ChartInOut.ChartAreas[0].AxisY.Maximum = maxY;

            //确定横纵轴的间隔数10,保证轴的间隔不能为0(当曲线上只有一点时可能会出现这种情况)
            this.ChartInOut.ChartAreas[0].AxisX.Interval = (((maxX - minX) / 10 > 0) ? ((maxX - minX) / 10) : 1);
            this.ChartInOut.ChartAreas[0].AxisY.Interval = (((maxY - minY) / 10 > 0) ? ((maxY - minY) / 10) : 1);

            //更新图像
            this.ChartInOut.Invalidate();
            //txt文件压入到数据库
            //if (mysqlWebFlag && serverTest)
            //此处还要修改********************************************************************
            //mysqlWeb.TxTInsertMySQL("InOutAnalysis", currentId + "#" + "Video",tmpfileName);
            //删除临时文件
#if RELEASE
            File.Delete(tmpfileName);
#endif
            return true;
        }

        //调整时间尺度(只对吞吐量有影响)
        private void ScaleComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ScaleComboBox.SelectedIndex < 0 || ScaleComboBox.SelectedIndex == prevSelectIndex)
            {
                return;
            }

            if (PcapFileName == null)
            {
                return;
            }
            InOutAvg.Text = "平均值：";
            InOutMax.Text = "最大值：";
            InOutMin.Text = "最小值：";
            //先将ChartInOut中的画面清空
            ChartInOut.Invoke(clearDataDel, ChartInOut);
            //刷新数据
            ShowLVInOut(PcapFileName, ScaleComboBox.SelectedIndex);
            //刷新选项索引值
            prevSelectIndex = ScaleComboBox.SelectedIndex;

        }

        //向帧长分析列表添加数据
        private bool ShowLVFrameLength()
        {
            //清空帧长分布列表
            LVFrameLength.Items.Clear();

            string[] strRange = new string[] { "0-100", "100-200", "200-300", "300-400",
                "400-500", "500-600", "600-700", "700-800", "800-900", "900-1000", "1000-1514"};
            string[] xValue = strRange;
            double[] yValue = new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            //ListView数据项和子数据项
            ListViewItem lvi;
            ListViewItem.ListViewSubItem lvsi;

            for (int i = 0; i < strRange.Length; i++)
            {
                lvi = new ListViewItem();
                lvi.Text = strRange[i];

                lvsi = new ListViewItem.ListViewSubItem();
                lvsi.Text = rangeCount[i].ToString();
                lvi.SubItems.Add(lvsi);

                lvsi = new ListViewItem.ListViewSubItem();
                double percent;
                if (totalPacketCnt == 0)
                    percent = 0.0;
                else
                    percent = rangeCount[i] * 100.0 / totalPacketCnt;
                lvsi.Text = percent.ToString("F2") + "%";
                yValue[i] = Math.Round(percent, 2);
                lvi.SubItems.Add(lvsi);
                //加入ListView
                LVFrameLength.Items.Add(lvi);
            }
            //画帧长分布的饼图
            ChartFrameLength.Series["帧长分布"].Points.DataBindXY(strRange, yValue);
            ChartFrameLength.Invalidate();

            return true;
        }

        /********************************************************************************
                             对Pcap包分析，得到延时抖动           
         ********************************************************************************/

        private void DelayJitterAnalys()
        {
            bool AnalysOK = false;
            //加try catch可以避免延时抖动分析代码中bug对整个分析结果的影响
            try
            {
                AnalysOK = ShowLVDelayJitter(PcapFileName);
                if (!AnalysOK)
                    WrongReason += "延时抖动分析异常 \n";
            }
            catch (System.Exception ex)
            {
                Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
            }

            return;

        }

        //向延时抖动列表添加数据
        [DllImport("NetpryDll.dll")]
        public extern static int delay_jitter_tofile(string tmpfileName);
        public bool ShowLVDelayJitter(string PcapFile)
        {
            //清空DelayJitter中的所有数据
            LVDelayJitter.Items.Clear();

            string tmpfileName = "DelayJitter.txt";

            int rect = -1;
            try
            {
                // rect = DelayJitterAnalyze(PcapFile);
                rect = delay_jitter_tofile(tmpfileName);
            }
            catch (System.Exception ex)
            {
                rect = -1;
                Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
            }

            if (rect < 0)
            {
                return false;
            }

            StreamReader sr = new StreamReader(tmpfileName);
            //读第一行,其中记录的是文字信息
            string strLine = sr.ReadLine();
            strLine = sr.ReadLine();
            int linecount = 0;
            const int graphcount = 50;
            ListViewItem[] lv = new ListViewItem[graphcount];
            ListViewItem.ListViewSubItem lvsi;

            //延时抖动曲线上点初值
            double xValue = 0.0;   //横坐标以解析的帧序号为标准,从1开始累加
            double yDelayValue = 0.0; //延时曲线的点纵坐标
            double yJitterValue = 0.0; //抖动曲线的点纵坐标

            //延时抖动最值
            double MaxDelay = 0.0;
            double MinDelay = 1.0;
            double MaxJitter = 0.0;
            double MinJitter = 1.0;

            bool yDelayValueGet = false;
            bool yJitterValueGet = false;

            while (strLine != null)
            {

                string[] str = strLine.Split(new Char[] { '\t' });

                //滤除掉那些由于操作导致的过大的延时和抖动
                if (double.Parse(str[3]) > 2.0 || double.Parse(str[4]) > 2.0)
                {
                    strLine = sr.ReadLine();
                    continue;
                }

                lv[linecount] = new ListViewItem();
                lv[linecount].Text = str[2];

                //求延时抖动最值
                if (Convert.ToDouble(str[3]) >= MaxDelay)
                    MaxDelay = Convert.ToDouble(str[3]);
                if (Convert.ToDouble(str[3]) <= MinDelay)
                    MinDelay = Convert.ToDouble(str[3]);

                if (Convert.ToDouble(str[4]) >= MaxJitter)
                    MaxJitter = Convert.ToDouble(str[4]);
                if (Convert.ToDouble(str[4]) <= MinJitter)
                    MinJitter = Convert.ToDouble(str[4]);

                yDelayValueGet = double.TryParse(str[3], out yDelayValue);
                if (!yDelayValueGet) yDelayValue = 0.0;
                AverValue.AverDelay += yDelayValue;
                yJitterValueGet = double.TryParse(str[4], out yJitterValue);
                if (!yJitterValueGet) yJitterValue = 0.0;
                AverValue.AverJitter += yJitterValue;
                //画图时以毫秒为单位
                yDelayValue *= 1000;
                yJitterValue *= 1000;

                ChartDelayJitter.Invoke(addDataDel, ChartDelayJitter, ChartDelayJitter.Series["延时曲线"], xValue, yDelayValue);
                ChartDelayJitter.Invoke(addDataDel, ChartDelayJitter, ChartDelayJitter.Series["抖动曲线"], xValue++, yJitterValue);

                lvsi = new ListViewItem.ListViewSubItem();
                lvsi.Text = str[2];
                lv[linecount].SubItems.Add(lvsi);
                for (int i = 3; i < 5; i++)
                {
                    lvsi = new ListViewItem.ListViewSubItem();
                    lvsi.Text = str[i];
                    lv[linecount].SubItems.Add((double.Parse(lvsi.Text) * 1000).ToString());
                }

                strLine = sr.ReadLine();
                linecount++;

                if (linecount % graphcount == 0)
                {
                    LVDelayJitter.BeginUpdate();//加入ListView
                    LVDelayJitter.Items.AddRange(lv);
                    LVDelayJitter.EndUpdate();
                    linecount = 0;
                }

            }

            LVDelayJitter.BeginUpdate();
            for (int i = 0; i < linecount; i++)
            {
                LVDelayJitter.Items.Add(lv[i]);
            }
            LVDelayJitter.EndUpdate();
            sr.Close();
            // File.Delete(tmpfileName);
            if (xValue > 0)
            {
                AverValue.AverDelay /= (int)xValue;
                AverValue.AverJitter /= (int)xValue;

            }
            AverValue.AverDelay = Math.Round(AverValue.AverDelay, 6);
            AverValue.AverJitter = Math.Round(AverValue.AverJitter, 6);

            //显示延时抖动的最值和均值
            this.DelayMax.Text += (MaxDelay * 1000).ToString() + "ms";
            this.DelayMin.Text += (MinDelay * 1000).ToString() + "ms";
            this.DelayAvg.Text += (AverValue.AverDelay * 1000).ToString() + "ms";

            this.JitterMax.Text += (MaxJitter * 1000).ToString() + "ms";
            this.JitterMin.Text += (MinJitter * 1000).ToString() + "ms";
            this.JitterAvg.Text += (AverValue.AverJitter * 1000).ToString() + "ms";

            this.DelayAvg.Visible = true;
            this.DelayMax.Visible = true;
            this.DelayMin.Visible = true;
            this.JitterAvg.Visible = true;
            this.JitterMax.Visible = true;
            this.JitterMin.Visible = true;


            //找到横纵轴的最大最小值,确定尺度
            double maxY = 0;
            double minY = 0;
            double maxX = 0;
            double minX = 0;
            foreach (Series iSeries in this.ChartDelayJitter.Series)
            {
                //添加判断曲线上点个数是否为零
                if (iSeries.Points.Count == 0)
                    continue;
                if (iSeries.Points.FindMaxValue("Y1").YValues[0] > maxY)
                    maxY = iSeries.Points.FindMaxValue("Y1").YValues[0];
                if (iSeries.Points.FindMinValue("Y1").YValues[0] < minY)
                    minY = iSeries.Points.FindMinValue("Y1").YValues[0];

                if (iSeries.Points.FindMaxValue("X").XValue > maxX)
                    maxX = iSeries.Points.FindMaxValue("X").XValue;
                if (iSeries.Points.FindMinValue("X").XValue < maxX)
                    minX = iSeries.Points.FindMinValue("X").XValue;
            }

            //确定横纵轴的最大小值,起止点的值为5的倍数
            minX = 0;
            maxX = 5 - maxX % 5 + maxX;
            minY = 0;
            maxY = 5 - maxY % 5 + maxY;

            this.ChartDelayJitter.ChartAreas[0].AxisX.Minimum = minX;
            this.ChartDelayJitter.ChartAreas[0].AxisX.Maximum = maxX;
            this.ChartDelayJitter.ChartAreas[0].AxisY.Minimum = minY;
            this.ChartDelayJitter.ChartAreas[0].AxisY.Maximum = maxY;

            //确定横纵轴的间隔数10
            this.ChartDelayJitter.ChartAreas[0].AxisX.Interval = (((maxX - minX) / 10 > 0) ? ((maxX - minX) / 10) : 1);
            this.ChartDelayJitter.ChartAreas[0].AxisY.Interval = (((maxY - minY) / 10 > 0) ? ((maxY - minY) / 10) : 1);

            //图像重构
            this.ChartDelayJitter.Invalidate();
            //txt文件压入到数据库
            if (mysqlWebFlag && serverTest)
                mysqlWeb.TxTInsertMySQL("DelayJitter", currentId + "#" + "Video", Application.StartupPath + "\\" + tmpfileName);
#if RELEASE
            File.Delete(tmpfileName);
#endif
            return true;

        }

        /**************************************************************************************
                        测试报告显示
       ****************************************************************************************/
        private void ResultDisplay()
        {
            ResultDisplay2();              //测试报告生成总的txt形式

            //txt写入listview
            StreamReader readData = new StreamReader(strTxtResult, Encoding.Default);//开启读的文件流
            string lineData = null;    //每一行的数据，标准格式，以分隔符分割
            lsvResult.BeginUpdate();
            while ((lineData = readData.ReadLine()) != null)
            {
                string[] temp = lineData.Split('\t');
                ListViewItem lvi = new ListViewItem();
                lvi.Text = temp[0];
                for (int i = 1; i < temp.Length; i++)
                {
                    lvi.SubItems.Add(temp[i]);
                }
                lsvResult.Items.Add(lvi);

            }
            lsvResult.EndUpdate();
            readData.Close();

        }

        /**************************************************************************************
                             对图像的处理
        ****************************************************************************************/
        //定义添加数据的委托
        public delegate void AddDataDelegate(Chart ichart, Series ptSeries, double xValue, double yValue);
        public AddDataDelegate addDataDel;
        //定义清除数据的委托
        public delegate void ClearDataDelegate(Chart ichart);
        public ClearDataDelegate clearDataDel;
        //给委托绑定函数
        private void RealTimechart()
        {
            clearDataDel += new ClearDataDelegate(ClearChartData);
            addDataDel += new AddDataDelegate(AddData);
        }
        //给图像添加数据
        public void AddData(Chart ichart, Series ptSeries, double xValue, double yValue)
        {
            AddNewPoint(ptSeries, xValue, yValue);
        }
        //给曲线添加点
        public void AddNewPoint(Series ptSeries, double xValue, double yValue)
        {
            // Add new data point to its series.
            ptSeries.Points.AddXY(xValue, yValue);
        }
        //使概要page下的图像曲线可以拖动
        private void EnableScroll(ChartArea ChartAreas)
        {
            ChartAreas.AxisX.Minimum = 0;
            ChartAreas.AxisX.ScrollBar.Enabled = true;
            ChartAreas.AxisX.ScrollBar.PositionInside = true;
            ChartAreas.AxisX.View.Zoomable = true;
            ChartAreas.AxisX.View.ZoomReset();
            ChartAreas.CursorX.UserEnabled = true;
            ChartAreas.CursorX.UserSelection = true;

            ChartAreas.AxisY.Minimum = 0;
            ChartAreas.AxisY.ScrollBar.Enabled = true;
            ChartAreas.AxisY.ScrollBar.PositionInside = true;
            ChartAreas.AxisY.View.Zoomable = true;
            ChartAreas.AxisY.View.ZoomReset();
            ChartAreas.CursorY.UserEnabled = true;
            ChartAreas.CursorY.UserSelection = true;
        }
        //清空图像画面
        public void ClearChartData(Chart ichart)
        {
            foreach (Series ptSeries in ichart.Series)
            {
                ptSeries.Points.Clear();
            }

            ichart.Invalidate();
        }

        //批处理结果
        private void ResultDisplay2()
        {
            string resultTxt = "ResultTxt.tmp";
            FileStream fs3 = new FileStream(resultTxt, FileMode.Append, FileAccess.Write);
            StreamWriter ResultTmp = new StreamWriter(fs3, Encoding.Default);  //临时总结报告文件，用于满足特定的格式压入数据库
            int index = 0;
            if (!File.Exists(strTxtResult))
            {
                using (StreamWriter swlog = new StreamWriter(File.Create(strTxtResult), Encoding.UTF8))
                {

                    //视频编码、分辨率信息
                    swlog.Write("\r\nWEB测试过程中视频信息如下：\r\n");

                    string strMediaInfo = "FlvMetaData.txt";

                    string startPath = Application.StartupPath;    //获取应用程序路径
                    string[] files = Directory.GetFiles(startPath);     //获取路径下所有文件
                    string fileType = null;
                    foreach (string str in files)
                    {
                        if (str.Contains("meida_file"))
                            fileType = str.Substring(str.LastIndexOf(".") + 1);

                    }

                    if (File.Exists(strMediaInfo))
                    {
                        try
                        {
                            swlog.Write("视频格式:" + "\t" + fileType + "\r\n");
                            ResultTmp.Write((++index).ToString() + "\t" + "VideoFormat\t" + fileType + "\r\n");
                            FileStream fs1 = new FileStream(strMediaInfo, FileMode.Open, FileAccess.Read);
                            StreamReader sr1 = new StreamReader(fs1, Encoding.Default);
                            String[] MediaInfo = null;
                            strMediaInfo = sr1.ReadLine();    //持续时间(s):	232.33
                            MediaInfo = strMediaInfo.Split('\t');
                            if (MediaInfo.Length == 2)
                                ResultTmp.Write((++index).ToString() + "\t" + "DurationTime" + "\t" + MediaInfo[1] + "\r\n");
                            strMediaInfo = sr1.ReadLine();   //Videosize:	10797209.00
                            MediaInfo = strMediaInfo.Split('\t');
                            if (MediaInfo.Length == 2)
                                ResultTmp.Write((++index).ToString() + "\t" + "VideoSize" + "\t" + MediaInfo[1] + "\r\n");
                            strMediaInfo = sr1.ReadLine();   //视频帧率(fps):	15.01
                            MediaInfo = strMediaInfo.Split('\t');
                            if (MediaInfo.Length == 2)
                                ResultTmp.Write((++index).ToString() + "\t" + "VideoFps" + "\t" + MediaInfo[1] + "\r\n");
                            swlog.Write(strMediaInfo + "\r\n");
                            strMediaInfo = sr1.ReadLine();   //视频码率(kbps):	361.78
                            MediaInfo = strMediaInfo.Split('\t');
                            if (MediaInfo.Length == 2)
                                ResultTmp.Write((++index).ToString() + "\t" + "VideoCodeRate" + "\t" + MediaInfo[1] + "\r\n");
                            {
                                swlog.Write(strMediaInfo + "\r\n");
                                strMediaInfo = sr1.ReadLine();   //videocodecid:	7.00	AVC-H.264
                                MediaInfo = strMediaInfo.Split('\t');
                                if (MediaInfo.Length == 3)
                                {
                                    swlog.Write("视频编码方式:" + "\t" + MediaInfo[2] + "\r\n");
                                    ResultTmp.Write((++index).ToString() + "\t" + "CodeingFormat" + "\t" + MediaInfo[2] + "\r\n");
                                }
                                else if (MediaInfo.Length == 2)
                                {
                                    swlog.Write("视频编码方式:" + "\t" + MediaInfo[1] + "\r\n");
                                    ResultTmp.Write((++index).ToString() + "\t" + "CodeingFormat" + "\t" + MediaInfo[1] + "\r\n");
                                }
                                MediaInfo = null;
                                strMediaInfo = sr1.ReadLine();  //width:	480.00
                                MediaInfo = strMediaInfo.Split('\t');
                                string width = MediaInfo[1];
                                MediaInfo = null;
                                strMediaInfo = sr1.ReadLine();  //height:	270.00
                                MediaInfo = strMediaInfo.Split('\t');
                                string height = MediaInfo[1];
                                MediaInfo = null;
                                swlog.Write("视频分辨率:" + "\t" + width + "*" + height + "\r\n");
                                ResultTmp.Write((++index).ToString() + "\t" + "VideoResolutionRate" + "\t" + width + "*" + height + "\r\n");
                            }
                            sr1.Close();
                            fs1.Close();
                        }
                        catch (Exception ex)
                        {
                            Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
                            MessageBox.Show("无法获取视频编码信息！");
                        }
                    }
                    else
                    {
                        strMediaInfo = "播放器没有接收到数据，无法解析视频信息！\r\n";
                        swlog.Write(strMediaInfo + "\r\n");
                    }

                    //保存得到的各种缺陷指标的均值
                    swlog.Write("\r\nWEB测试中各个参数均值如下：\r\n");
                    if (AverValue.FrameRate == null)
                        AverValue.FrameRate = "WEB分析不成功，无法获得视频帧率";
                    //swlog.Write(AverValue.FrameRate + "\t\r\n");
                    swlog.Write("DNS响应平均延时(秒)\t" + AverValue.AverDNS.ToString() + "\t\r\n");
                    ResultTmp.Write((++index).ToString() + "\t" + "DNS mean_delay(s)\t" + AverValue.AverDNS.ToString() + "\r\n");

                    swlog.Write("HTTP响应平均延时(秒)\t" + AverValue.AverHTTP.ToString() + "\t\r\n");
                    ResultTmp.Write((++index).ToString() + "\t" + "HTTP mean_delay(s)\t" + AverValue.AverHTTP.ToString() + "\r\n");

                    swlog.Write("服务器响应平均延时(秒)\t" + AverValue.AverHTTP.ToString() + "\t\r\n");
                    ResultTmp.Write((++index).ToString() + "\t" + "SERVER mean_delay(s)\t" + AverValue.AverHTTP.ToString() + "\r\n");

                    swlog.Write("吞吐量均值(字节/秒))\t" + AverValue.AverInOut.ToString() + "\t\r\n");
                    ResultTmp.Write((++index).ToString() + "\t" + "InOut mean_delay(s)\t" + AverValue.AverInOut.ToString() + "\r\n");

                    swlog.Write("平均延时(秒)\t" + AverValue.AverDelay.ToString() + "\t\r\n");
                    ResultTmp.Write((++index).ToString() + "\t" + "mean_delay(s)\t" + AverValue.AverDelay.ToString() + "\r\n");

                    swlog.Write("平均抖动(秒)\t" + AverValue.AverJitter.ToString() + "\t\r\n");
                    ResultTmp.Write((++index).ToString() + "\t" + "mean_jitter(s)\t" + AverValue.AverJitter.ToString() + "\r\n");
                    //写TCP连接信息
                    swlog.Write("\r\n" + AverValue.TcpInfo + "\r\n");
                    string[] tcpInfo = AverValue.TcpInfo.Split(new string[] { "\t\r\n" }, StringSplitOptions.RemoveEmptyEntries); ;
                    for (int i = 0; i < tcpInfo.Length; i++)
                    {
                        if (i == 0) continue;
                        ResultTmp.Write((++index).ToString() + "\t" + tcpInfo[i] + "\r\n");
                    }
                    //写入TCP异常信息
                    swlog.Write("\r\n" + AverValue.TcpEx + "\r\n");
                    string[] tcpExInfo = AverValue.TcpEx.Split(new string[] { "\t\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < tcpExInfo.Length; i++)
                    {
                        if (i == 0) continue;
                        ResultTmp.Write((++index).ToString() + "\t" + tcpExInfo[i] + "\r\n");
                    }

                    swlog.Close();
                    ResultTmp.Close();
                    fs3.Close();
                    //txt文件压入到数据库
                    if (mysqlWebFlag && serverTest)
                        mysqlWeb.TxTInsertMySQL("TestReport", currentId + "#" + "Video", Application.StartupPath + "\\" + resultTxt);
                    //删除临时文件
                    File.Delete(resultTxt);
                }
            }
        }

        private void btnWebSelCap_Click(object sender, EventArgs e)
        {
            OpenFileDialog capFile = new OpenFileDialog();
            capFile.RestoreDirectory = true;
            capFile.Multiselect = false;
            capFile.Filter = "pcap文件|*.pcap";
            if (capFile.ShowDialog() == DialogResult.OK)
            {
                PcapFileName = capFile.FileName;
                strXlsLogFile = capFile.FileName.Replace(".pcap", ".xlsx");
                inis.IniWriteValue("Flv", "PcapFile", PcapFileName);
                MessageBox.Show("操作完成！");
                isSelectPcap = true;        //选择了pcap文件           
            }
            else
            {
                MessageBox.Show("请选择抓包文件！");
                return;
            }
        }

        private void btnLastPage_Click(object sender, EventArgs e)    //上一页的响应函数
        {
            currentPage--;    //当前页码自减
            btnNextPage.Enabled = true;
            if (currentPage == 1)
            {
                btnLastPage.Enabled = false;    //第一页时上一页不能用
            }
            comboxJumpPage.SelectedIndex = currentPage - 1;
            string packageFile = "dissectPacket.tmp";
            getPageRecord(packageFile, currentPage);    //获取当前页的记录
        }


        private void btnNextPage_Click(object sender, EventArgs e)
        {
            currentPage++;
            btnLastPage.Enabled = true;    //上一页使能
            if (currentPage == pageNum)    //当前页是总页数，没有下一页
            {
                btnNextPage.Enabled = false;
            }
            comboxJumpPage.SelectedIndex = currentPage - 1;
            string packageFile = "dissectPacket.tmp";
            getPageRecord(packageFile, currentPage);    //获取当前页的记录
        }

        private void btnJump_Click(object sender, EventArgs e)
        {
            string selectText = comboxJumpPage.Text;
            currentPage = int.Parse(selectText);
            if (currentPage == 1)
            {
                btnLastPage.Enabled = false;
                btnNextPage.Enabled = true;
            }
            else if (currentPage == pageNum)
            {
                btnNextPage.Enabled = false;
                btnLastPage.Enabled = true;
            }
            else
            {
                btnLastPage.Enabled = true;
                btnNextPage.Enabled = true;
            }
            string packageFile = "dissectPacket.tmp";
            getPageRecord(packageFile, currentPage);    //获取当前页的记录
        }


        private bool getPageRecord(string packageFile, int currentPage)
        {
            if (File.Exists(packageFile))    //打开包文件进行偏移取数
            {
                try
                {
                    FileStream fsPacket = new FileStream(packageFile, FileMode.Open, FileAccess.Read);
                    StreamReader srPacket = new StreamReader(fsPacket, Encoding.Default);
                    //string strLine = srPacket.ReadLine();    //第一行是标题行，去掉
                    string strLine = null;
                    if (currentPage == 1)
                    {
                        strLine = srPacket.ReadLine();     //第一页时读取第一行
                    }
                    int count = 0;
                    while (count < (currentPage - 1) * PACKETPAGESIZE)
                    {
                        strLine = srPacket.ReadLine();     //相当于偏移掉前面(currentPage-1) * packetPageSize,第1页从0条开始取，第2页从第2000条开始取
                        count++;
                    }
                    //清除上一次的列表
                    LVPacketAnalys.Items.Clear();
                    //开始取数,取往后的2000条
                    int tempSumInPage = 0;
                    //ListView数据项和子数据项
                    ListViewItem[] lvi;
                    ListViewItem.ListViewSubItem lvsi;
                    lvi = new ListViewItem[GROUPCOUNT];     //200个为一组，批量刷新list
                    int lineCount = 0;
                    while (strLine != null && tempSumInPage < PACKETPAGESIZE) //文件没有读取到记录或达到一页的展示量2000
                    {
                        //得到每一单元数据
                        string[] str = strLine.Split(new Char[] { '\t' }, 11);

                        lvi[lineCount] = new ListViewItem();
                        lvi[lineCount].Text = str[0];

                        for (int i = 1; i < 11; i++)
                        {
                            lvsi = new ListViewItem.ListViewSubItem();
                            lvsi.Text = str[i];
                            lvi[lineCount].SubItems.Add(lvsi);
                        }

                        strLine = srPacket.ReadLine();
                        lineCount++;
                        tempSumInPage++;

                        if (lineCount % GROUPCOUNT == 0)              //每200条记录刷新一次
                        {
                            LVPacketAnalys.BeginUpdate();       //控制刷新
                            //加入ListView
                            LVPacketAnalys.Items.AddRange(lvi);
                            LVPacketAnalys.EndUpdate();

                            lvi = new ListViewItem[GROUPCOUNT];
                            lineCount = 0;
                        }
                    }

                    LVPacketAnalys.BeginUpdate();     //当没有达到50条时没有刷新所以在这里要刷新
                    //加入最后一批
                    for (int i = 0; i < lineCount; i++)
                    {
                        LVPacketAnalys.Items.Add(lvi[i]);
                    }
                    LVPacketAnalys.EndUpdate();
                    srPacket.Close();
                    return true;
                }
                catch (System.Exception ex)
                {
                    Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
                    return false;     //加载记录失败
                }
            }
            else
            {
                return false;
            }
        }

        private void btnLastPageTcpGene_Click(object sender, EventArgs e)    //上一页的响应函数
        {
            currentPageTcpGene--;    //当前页码自减
            btnNextPageTcpGene.Enabled = true;
            if (currentPageTcpGene == 1)
            {
                btnLastPageTcpGene.Enabled = false;    //第一页时上一页不能用
            }
            comboxJumpPageTcpGene.SelectedIndex = currentPageTcpGene - 1;
            string packageFile = "dissectTcp.tmp";
            getPageRecordTcpGene(packageFile, currentPageTcpGene);    //获取当前页的记录
        }


        private void btnNextPageTcpGene_Click(object sender, EventArgs e)
        {
            currentPageTcpGene++;
            btnLastPageTcpGene.Enabled = true;    //上一页使能
            if (currentPageTcpGene == pageNumTcpGene)    //当前页是总页数，没有下一页
            {
                btnNextPageTcpGene.Enabled = false;
            }
            comboxJumpPageTcpGene.SelectedIndex = currentPageTcpGene - 1;
            string packageFile = "dissectTcp.tmp";
            getPageRecordTcpGene(packageFile, currentPageTcpGene);    //获取当前页的记录
        }

        private void btnJumpTcpGene_Click(object sender, EventArgs e)
        {
            string selectText = comboxJumpPageTcpGene.Text;
            currentPageTcpGene = int.Parse(selectText);
            if (currentPageTcpGene == 1)
            {
                btnLastPageTcpGene.Enabled = false;
                btnNextPageTcpGene.Enabled = true;
            }
            else if (currentPageTcpGene == pageNumTcpGene)
            {
                btnNextPageTcpGene.Enabled = false;
                btnLastPageTcpGene.Enabled = true;
            }
            else
            {
                btnLastPageTcpGene.Enabled = true;
                btnNextPageTcpGene.Enabled = true;
            }
            string packageFile = "dissectTcp.tmp";
            getPageRecordTcpGene(packageFile, currentPageTcpGene);    //获取当前页的记录
        }

        private bool getPageRecordTcpGene(string tcpGeneFile, int currentPageTcp)
        {
            if (File.Exists(tcpGeneFile))    //打开包文件进行偏移取数
            {
                try
                {
                    FileStream fsTcp = new FileStream(tcpGeneFile, FileMode.Open, FileAccess.Read);
                    StreamReader srTcp = new StreamReader(fsTcp, Encoding.Default);
                    //string strLine = srPacket.ReadLine();    //第一行是标题行，去掉
                    string strLine = null;
                    if (currentPageTcp == 1)
                    {
                        strLine = srTcp.ReadLine();     //第一页时读取第一行,这里要验证
                    }
                    int count = 0;
                    while (count < (currentPageTcp - 1) * PACKETPAGESIZE)
                    {
                        strLine = srTcp.ReadLine();     //相当于偏移掉前面(currentPage-1) * packetPageSize,第1页从0条开始取，第2页从第2000条开始取
                        count++;
                    }
                    //清除上一次的列表
                    LVTCPGeneral.Items.Clear();
                    //开始取数,取往后的2000条
                    int tempSumInPage = 0;
                    //ListView数据项和子数据项
                    ListViewItem[] lvi;
                    ListViewItem.ListViewSubItem lvsi;
                    lvi = new ListViewItem[GROUPCOUNT];     //200个为一组，批量刷新list
                    int lineCount = 0;
                    //设置图像的横纵坐标
                    double xValue = 1.0;
                    //double yEndValue = 0.0;
                    double yLastValue = 0.0;  //持续时间
                    double yStartValue = 0.0;
                    bool TcpDelayGet = false;

                    // Set series chart type
                    //ChartTcpGenr.Series["起始时间(秒)"].Type = SeriesChartType.Bar;
                    //// Set series point width
                    //ChartTcpGenr.Series["起始时间(秒)"]["PointWidth"] = "1.0";
                    //// Show data points labels
                    //ChartTcpGenr.Series["起始时间(秒)"].ShowLabelAsValue = false;
                    //// Set data points label style
                    //ChartTcpGenr.Series["起始时间(秒)"]["BarLabelStyle"] = "Center";
                    // Display chart as 3D
                    ChartTcpGenr.ChartAreas[0].Area3DStyle.Enable3D = false;
                    // Draw the chart as embossed
                    ChartTcpGenr.Series["持续时间(秒)"]["DrawingStyle"] = "Emboss";
                    // Set series chart type
                    ChartTcpGenr.Series["持续时间(秒)"].Type = SeriesChartType.Bar;
                    // Set series point width
                    ChartTcpGenr.Series["持续时间(秒)"]["PointWidth"] = "1.0";
                    // Show data points labels
                    ChartTcpGenr.Series["持续时间(秒)"].ShowLabelAsValue = false;
                    // Set data points label style
                    ChartTcpGenr.Series["持续时间(秒)"]["BarLabelStyle"] = "Center";
                    // Display chart as 3D
                    //ChartTcpGenr.ChartAreas[0].Area3DStyle.Enable3D = false;
                    // Draw the chart as embossed
                    //ChartTcpGenr.Series["结束时间(秒)"]["DrawingStyle"] = "Emboss";

                    while (strLine != null && tempSumInPage < PACKETPAGESIZE) //文件没有读取到记录或达到一页的展示量2000
                    {
                        //得到每一单元数据
                        string[] str = strLine.Split(new Char[] { '\t' }, 12);

                        lvi[lineCount] = new ListViewItem();
                        lvi[lineCount].Text = str[0];

                        for (int i = 1; i < 12; i++)
                        {
                            lvsi = new ListViewItem.ListViewSubItem();
                            lvsi.Text = str[i];
                            lvi[lineCount].SubItems.Add(lvsi);
                        }

                        strLine = srTcp.ReadLine();
                        lineCount++;
                        tempSumInPage++;

                        if (lineCount % GROUPCOUNT == 0)              //每200条记录刷新一次
                        {
                            LVTCPGeneral.BeginUpdate();       //控制刷新
                            //加入ListView
                            LVTCPGeneral.Items.AddRange(lvi);
                            LVTCPGeneral.EndUpdate();

                            lvi = new ListViewItem[GROUPCOUNT];
                            lineCount = 0;
                        }
                        //计算AverHTTPWeb的值
                        TcpDelayGet = double.TryParse((str[9]), out yStartValue);
                        if (!TcpDelayGet) yStartValue = 0.0;
                        //ChartTcpGenr.Invoke(addDataDel, ChartTcpGenr, ChartTcpGenr.Series["起始时间(秒)"], xValue, yStartValue);
                        TcpDelayGet = double.TryParse((str[10]), out yLastValue);
                        if (!TcpDelayGet) yLastValue = 0.0;
                        ChartTcpGenr.Invoke(addDataDel, ChartTcpGenr, ChartTcpGenr.Series["持续时间(秒)"], xValue++, yStartValue + yLastValue);
                    }

                    LVTCPGeneral.BeginUpdate();     //当没有达到50条时没有刷新所以在这里要刷新
                    //加入最后一批
                    for (int i = 0; i < lineCount; i++)
                    {
                        LVTCPGeneral.Items.Add(lvi[i]);
                    }
                    LVTCPGeneral.EndUpdate();
                    srTcp.Close();


                    //曲线图更新
                    ChartTcpGenr.Invalidate();

                    return true;
                }
                catch (System.Exception ex)
                {
                    Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
                    return false;     //加载记录失败
                }
            }
            else
            {
                return false;
            }
        }

    }
}



