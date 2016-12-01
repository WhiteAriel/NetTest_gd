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
    public struct AverValueWeb
    {
        public static double AverDNSWeb = 0.0;
        public static double AverHTTPWeb = 0.0;    //Web响应延时
        public static double AverRTSPRTTWeb = 0.0; //Rtsp响应延时
        public static double AverInOutWeb = 0.0;
        public static double AverDelayWeb = 0.0;
        public static double AverJitterWeb = 0.0;
        public static string TcpInfoWeb = "";
        public static string TcpExWeb = "";
        public static string FrameRateWeb = "";


        public static void InitValue()
        {
            AverDNSWeb = 0.0;
            AverHTTPWeb = 0.0;
            AverRTSPRTTWeb = 0.0;
            AverInOutWeb = 0.0;
            AverDelayWeb = 0.0;
            AverJitterWeb = 0.0;
            TcpInfoWeb = "";
            TcpExWeb = "";
            FrameRateWeb = "";
        }
    }

    public partial class WebAnalyse : DevExpress.XtraEditors.XtraUserControl
    {
        IniFile inisWeb = new IniFile(Application.StartupPath + "\\settings.ini");  //ini class
        IniFile inisWebvlc = new IniFile(Application.StartupPath + "\\VideoPlayer" + "\\vlc.ini");
        IniFile inisWebref = new IniFile(Application.StartupPath + "\\refTool" + "\\referencesetup.ini");

        const int PACKETPAGESIZEWEB = 2000;      //数据包展示页面每次加载1000条
        const int GROUPCOUNTWEB = 200;           //每次数据表刷新200条记录
        //测试次数
        //int testNumWeb = 0;
        //全局变量，判断是否分析结束
        bool IsAnalysedWeb = false;
        //抓包文件名
        string PcapFileNameWeb = null;
        //播放日志文件
        //string TxtFileNameWeb = null;
        //分析耗时
        //int AnalysingTimeCountWeb = 0;
        //记录前一个选择项索引
        int prevSelectIndexWeb = 0;
        //吞吐量所选的尺度选项
        double[] timeScaleWeb = new double[] { 1.0, 0.1, 0.01 };
        //所有数据包个数
        int totalPacketCntWeb = 0;
        //帧长分布中不同帧长对应个数
        int[] rangeCountWeb = new int[11];
        //错误信息记录
        string WrongReasonWeb = null;
        //测试报告txt格式
        public string strTxtResultWeb = null;
        //Excel处理程序
        ExcelProcess processExcelWeb = new ExcelProcess();
        //xls文件
        string strXlsLogFileWeb = null;
        //判断txt转换为xsl
        bool iTxt2XlsWeb = false;
        //f分析次数
        //int iStartAnalyzeWeb = 0;
        //批处理cap文件名称
        // string[] filesinpathWeb = null;
        //判断是否是选择现在pcap文件
        //public static bool isSelectPcapWeb = false;

        //设置解析线程
        Thread setWebParseTrd = null;


        private volatile bool analyzeOn = false;
        public volatile bool serverTest = false;


        //数据库部分
        private string currentId;
        private MySQLInterface mysqlWebA = null;
        private bool mysqlWebFlagA = false;
        //数据包分页变量
        int currentPageWeb = 1;  //当前页码
        int totalNumWeb = 0;    //总记录数，初始化为0
        int pageNumWeb = 0;      //总页数

        //tcp正常流分页
        int currentPageWebTcpGene = 1;  //当前页码
        int totalNumWebTcpGene = 0;    //总记录数，初始化为0
        int pageNumWebTcpGene = 0;      //总页数


        ArrayList datalistWeb = ArrayList.Synchronized(new ArrayList());//数据包arraylist，保存cap包名字

        //定义填充listview的BackgroundWorker，防止界面进程阻塞 
        //private BackgroundWorker m_AsyncWorkerTcp = new BackgroundWorker();  //Tcp界面，暂时不支持停止
        //private BackgroundWorker m_AsyncWorkerSave = new BackgroundWorker(); //抖动延迟界面

        public void Init()
        {
            PacWebAnaly.SelectedTabPageIndex = 12;
            //lsvResult.Items.Clear();
            //设置分析完成判断指示
            IsAnalysedWeb = false;
            //将抓包文件名置空
            PcapFileNameWeb = null;
            //将播放日志文件名置空
            //TxtFileNameWeb = null;
            //清空图表
            //this.InitChart();
            //this.InitListView();
            //清空上次计算的平均值
            //AverValueWeb.InitValue();

            //将帧长分析的时间尺度标识出
            int i = 0;
            object[] obj = new object[timeScaleWeb.Length];
            foreach (double s in timeScaleWeb)
            {
                obj[i++] = s.ToString() + "秒";
            }
            ScaleComboBoxWeb.Items.Clear();
            //添加尺度选择组合框
            ScaleComboBoxWeb.Items.AddRange(obj);
            //尺度默认为1.0
            ScaleComboBoxWeb.SelectedIndex = 0;

            this.btnStartWebAnaly.Enabled = true;
        }

        public void InitChart()
        {
            //this.InitWebChart();
            this.ChartDNSWeb.Invoke(clearDataDel, this.ChartDNSWeb);
            this.ChartHttpWeb.Invoke(clearDataDel, this.ChartHttpWeb);
            this.ChartInOutWeb.Invoke(clearDataDel, this.ChartInOutWeb);
            this.ChartFrameLengthWeb.Invoke(clearDataDel, this.ChartFrameLengthWeb);
            this.ChartDelayJitterWeb.Invoke(clearDataDel, this.ChartDelayJitterWeb);
        }

        public void InitListView()
        {
            LVSumWeb.Items.Clear();
            LVPacketAnalysWeb.Items.Clear();
            LVTCPGeneralWeb.Items.Clear();
            LVTCPExWeb.Items.Clear();
            LVDNSWebAnalys.Items.Clear();
            LVHTTPAnalysWeb.Items.Clear();
            LVInOutWeb.Items.Clear();
            LVFrameLengthWeb.Items.Clear();
            LVDelayJitterWeb.Items.Clear();
            lsvResultWeb.Items.Clear();
            strTxtResultWeb = Application.StartupPath + "\\TxtResultWeb.txt";

            DelayAvgWeb.Visible = false;
            DelayMaxWeb.Visible = false;
            DelayMinWeb.Visible = false;
            JitterAvgWeb.Visible = false;
            JitterMaxWeb.Visible = false;
            JitterMinWeb.Visible = false;

            InOutAvgWeb.Visible = false;
            InOutMaxWeb.Visible = false;
            InOutMinWeb.Visible = false;

            DelayMaxWeb.Text = "延时最大值:";
            DelayMinWeb.Text = "延时最小值:";
            DelayAvgWeb.Text = "延时平均值:";
            JitterMaxWeb.Text = "抖动最大值:";
            JitterMinWeb.Text = "抖动最小值:";
            JitterAvgWeb.Text = "抖动平均值:";

            InOutAvgWeb.Text = "吞吐量平均值:";
            InOutMaxWeb.Text = "吞吐量最大值:";
            InOutMinWeb.Text = "吞吐量最小值:";
            if (File.Exists(strTxtResultWeb))
                File.Delete(strTxtResultWeb);
        }

        public WebAnalyse()
        {
            InitializeComponent();
            this.RealTimechart();
            datalistWeb.Clear();
            mysqlWebA = new MySQLInterface(inisWeb.IniReadValue("Mysql", "serverIp"), inisWeb.IniReadValue("Mysql", "user"), inisWeb.IniReadValue("Mysql", "passwd"), inisWeb.IniReadValue("Mysql", "dbname"), null);
            if (mysqlWebA.MysqlInit(inisWeb.IniReadValue("Mysql", "dbname")))
                mysqlWebFlagA = true;
            Init();
        }


        private void ParseWebPacket()
        {
            currentId = inisWeb.IniReadValue("Task", "currentWebId");
            //4部分的解析函数
            try
            {
                InOutFrameLenAnalys();    //分析吞吐量、帧长分布信息
                PcapTcpDnsHttpAnalys();  //分析文件概要、数据包、TCP、DNS、HTTP等信息
                ResultDisplay();          //测试报告结果显示
                //storeResult();
            }
            catch (System.Exception ex)
            {
                Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
            }
            if (WrongReasonWeb != null)
            {
                if (!serverTest)
                {
                    MessageBox.Show(WrongReasonWeb + "出错可能的原因有：\n 1、没有相关的数据包 \n 2、网卡选择不正确 \n ");
                    Log.Error(WrongReasonWeb);
                }
                else
                    Log.Error(WrongReasonWeb);
            }
            btnStartWebAnaly.Enabled = true;
            btnWebSelCapWeb.Enabled = true;
            analyzeOn = false;

        }



        public void WebServerAnalyzeStartFunc()
        {
            while (true)
            {
                if (!analyzeOn)
                {
                    analyzeOn = true;
                    //清除Excel进程
                    Process[] p = Process.GetProcessesByName("EXCEL");
                    if (p.Length > 0)
                    {
                        for (int i = 0; i < p.Length; i++)
                        {
                            p[i].CloseMainWindow();
                            p[i].Kill();
                        }
                    }

                    PcapFileNameWeb = inisWeb.IniReadValue("Web", "webPcapPath");

                    if (!File.Exists(PcapFileNameWeb))
                    {
                        MessageBox.Show("找不到数据包文件：" + PcapFileNameWeb);
                        return;
                    }

                    IsAnalysedWeb = true;    //是否进行了分析
                    WrongReasonWeb = null;   //清空错误信息
                    //清空图表
                    this.InitChart();
                    this.InitListView();
                    //清空上次计算的平均值
                    AverValueWeb.InitValue();

                    btnStartWebAnaly.Enabled = false;
                    btnWebSelCapWeb.Enabled = false;

                    try
                    {
                        setWebParseTrd = new Thread(new ThreadStart(ParseWebPacket));
                        setWebParseTrd.Start();
                    }
                    catch (System.Exception ex)
                    {
                        Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
                    }
                    setWebParseTrd.Join();
                    break;
                }
                else
                    Thread.Sleep(500);
            }
        }


        public void WebTerminalAnalyzeStartFunc()
        {

            analyzeOn = true;
            //清除Excel进程
            Process[] p = Process.GetProcessesByName("EXCEL");
            if (p.Length > 0)
            {
                for (int i = 0; i < p.Length; i++)
                {
                    p[i].CloseMainWindow();
                    p[i].Kill();
                }
            }

            PcapFileNameWeb = inisWeb.IniReadValue("Web", "webPcapPath");

            if (!File.Exists(PcapFileNameWeb))
            {
                MessageBox.Show("找不到数据包文件：" + PcapFileNameWeb);
                return;
            }

            IsAnalysedWeb = true;    //是否进行了分析
            WrongReasonWeb = null;   //清空错误信息
            //清空图表
            this.InitChart();
            this.InitListView();
            //清空上次计算的平均值
            AverValueWeb.InitValue();

            btnStartWebAnaly.Enabled = false;
            btnWebSelCapWeb.Enabled = false;

            try
            {
                setWebParseTrd = new Thread(new ThreadStart(ParseWebPacket));
                setWebParseTrd.Start();
            }
            catch (System.Exception ex)
            {
                Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
            }
        }

        public void btnStartWebAnaly_Click(object sender, EventArgs e)
        {
            //webStartFunc();
            WebTerminalAnalyzeStartFunc();
        }

        //保存测试报告
        private void storeResult()
        {
            //保存截图
            {
                //根据解析的数据包名指定保存图片文件路径
                string SavedPicPath = PcapFileNameWeb;
                //指定保存图片文件路径
                SavedPicPath = PcapFileNameWeb;
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
                ChartDNSWeb.SaveAsImage(SavedPicPath + "\\ChartDNSWebWeb.jpg", format);
                ChartHttpWeb.SaveAsImage(SavedPicPath + "\\ChartHttpWeb.jpg", format);
                ChartInOutWeb.SaveAsImage(SavedPicPath + "\\ChartInOutWebWeb.jpg", format);
                ChartFrameLengthWeb.SaveAsImage(SavedPicPath + "\\ChartFrameLengthWebWeb.jpg", format);
                ChartDelayJitterWeb.SaveAsImage(SavedPicPath + "\\ChartDelayJitterWebWeb.jpg", format);
            }


            // txt转excel文件
            //strXlsLogFileWeb = inisWeb.IniReadValue("Flv", "LogFile");
            iTxt2XlsWeb = processExcelWeb.txt2Xlsx(strTxtResultWeb, strXlsLogFileWeb);
            if (!iTxt2XlsWeb)
            {
                MessageBox.Show("日志文件" + strXlsLogFileWeb + "创建失败！\n 请检查本机是否安装好Office软件！");
                return;
            }

            StreamReader sReader = null;
            try
            {
                string path = strTxtResultWeb;
                string lineContent = null;

                sReader = new StreamReader(path, Encoding.Default);
                while ((lineContent = sReader.ReadLine()) != null)
                {
                  
                }
                sReader.Close();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
                Log.Error(Environment.StackTrace, ex);
                if (sReader != null)
                {
                    sReader.Close();
                }
            }
            return;
        }


        /********************************************************************
                           对Pcap包分析，得到Web的相关信息
         * *********************************************************************/
        [DllImport("VSMFlv.dll")]
        public extern static int VideoStreamMediaFlv(string strPcapFileNameWeb);
        private void WebInfoAnalys()
        {
            int AnalysOK = 0;
            //清空上次分析留存的临时文件
            ClearTmpFile();
            //InitWebChart();

            try
            {
                AnalysOK = VideoStreamMediaFlv(PcapFileNameWeb);
            }
            catch (System.Exception ex)
            {
                Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
            }

            if (AnalysOK == -6)
            {
                IsAnalysedWeb = true;
                if (File.Exists("NoHttpDetectedError")) File.Delete("NoHttpDetectedError");
                WrongReasonWeb += " 数据包传输不是采用http协议传输，web解析无法完成 \n";
                return;
            }
            else if (AnalysOK == -7)
            {
                //这种情况现在不会存在
                IsAnalysedWeb = true;
                if (File.Exists("FlvRestoreFailed.txt")) File.Delete("FlvRestoreFailed.txt");
                WrongReasonWeb += "传输过程有关键数据包丢失，web解析无法完成 \n";
                return;
            }
            else if (AnalysOK == -8)
            {
                IsAnalysedWeb = true;
                if (File.Exists("NoFlvDetected.txt")) File.Delete("NoFlvDetected.txt");
                WrongReasonWeb += "播放文件不是flv/f4v/hlv格式，web解析无法完成 \n";
                return;
            }
            else if (!((File.Exists("flv_tag.txt")) && (File.Exists("data_flow_smoothed.txt")) && (File.Exists("data_flow_unsmoothed.txt")) && (File.Exists("play_flow_unsmoothed.txt")) && (File.Exists("play_flow_smoothed.txt")) && (File.Exists("user_event.txt"))))
            {
                //这种情况现在也不会存在
                IsAnalysedWeb = true;
                WrongReasonWeb += "web解析异常，得到的数据信息不完整 \n";
                return;
            }

            //根据调用VideoStreamMediaFlv(string strPcapFileNameWeb)生成的7个txt文件进一步分析，写入到LV中，或者画图

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

            maxY = this.ChartAccumulatedTrafficWeb.Series["累积网络数据流"].Points.FindMaxValue("Y1").YValues[0] > this.ChartAccumulatedTrafficWeb.Series["累积视频播放流"].Points.FindMaxValue("Y1").YValues[0] ? this.ChartAccumulatedTrafficWeb.Series["累积网络数据流"].Points.FindMaxValue("Y1").YValues[0] : this.ChartAccumulatedTrafficWeb.Series["累积视频播放流"].Points.FindMaxValue("Y1").YValues[0];
            this.ChartAccumulatedTrafficWeb.ChartAreas[0].AxisY.Maximum = ((int)(maxY / 100) + 1) * 100;
            minY = this.ChartAccumulatedTrafficWeb.Series["累积网络数据流"].Points.FindMinValue("Y1").YValues[0] > this.ChartAccumulatedTrafficWeb.Series["累积视频播放流"].Points.FindMinValue("Y1").YValues[0] ? this.ChartAccumulatedTrafficWeb.Series["累积网络数据流"].Points.FindMinValue("Y1").YValues[0] : this.ChartAccumulatedTrafficWeb.Series["累积视频播放流"].Points.FindMinValue("Y1").YValues[0];
            this.ChartAccumulatedTrafficWeb.ChartAreas[0].AxisY.Minimum = ((int)(minY / 100) + 1) * 100;
            maxX = this.ChartAccumulatedTrafficWeb.Series["累积网络数据流"].Points.FindMaxValue("X").XValue > this.ChartAccumulatedTrafficWeb.Series["累积视频播放流"].Points.FindMaxValue("X").XValue ? this.ChartAccumulatedTrafficWeb.Series["累积网络数据流"].Points.FindMaxValue("X").XValue : this.ChartAccumulatedTrafficWeb.Series["累积视频播放流"].Points.FindMaxValue("X").XValue;
            this.ChartAccumulatedTrafficWeb.ChartAreas[0].AxisX.Maximum = maxX + 5;
            minX = this.ChartAccumulatedTrafficWeb.Series["累积网络数据流"].Points.FindMinValue("X").XValue > this.ChartAccumulatedTrafficWeb.Series["累积视频播放流"].Points.FindMinValue("X").XValue ? this.ChartAccumulatedTrafficWeb.Series["累积网络数据流"].Points.FindMinValue("X").XValue : this.ChartAccumulatedTrafficWeb.Series["累积视频播放流"].Points.FindMinValue("X").XValue;
            this.ChartAccumulatedTrafficWeb.ChartAreas[0].AxisX.Minimum = minX + 5;

            //确定ChartDataPlayFlow图第二部分的横纵轴

            maxY = this.ChartRealTrafficWeb.Series["实时网络数据流"].Points.FindMaxValue("Y1").YValues[0] > this.ChartRealTrafficWeb.Series["实时视频播放流"].Points.FindMaxValue("Y1").YValues[0] ? this.ChartRealTrafficWeb.Series["实时网络数据流"].Points.FindMaxValue("Y1").YValues[0] : this.ChartRealTrafficWeb.Series["实时视频播放流"].Points.FindMaxValue("Y1").YValues[0];
            this.ChartRealTrafficWeb.ChartAreas[0].AxisY.Maximum = ((int)(maxY / 100) + 1) * 100;
            minY = this.ChartRealTrafficWeb.Series["实时网络数据流"].Points.FindMinValue("Y1").YValues[0] > this.ChartRealTrafficWeb.Series["实时视频播放流"].Points.FindMinValue("Y1").YValues[0] ? this.ChartRealTrafficWeb.Series["实时网络数据流"].Points.FindMinValue("Y1").YValues[0] : this.ChartRealTrafficWeb.Series["实时视频播放流"].Points.FindMinValue("Y1").YValues[0];
            this.ChartRealTrafficWeb.ChartAreas[0].AxisY.Minimum = ((int)(minY / 100) + 1) * 100;
            maxX = this.ChartRealTrafficWeb.Series["实时网络数据流"].Points.FindMaxValue("X").XValue > this.ChartRealTrafficWeb.Series["实时视频播放流"].Points.FindMaxValue("X").XValue ? this.ChartRealTrafficWeb.Series["实时网络数据流"].Points.FindMaxValue("X").XValue : this.ChartRealTrafficWeb.Series["实时视频播放流"].Points.FindMaxValue("X").XValue;
            this.ChartRealTrafficWeb.ChartAreas[0].AxisX.Maximum = maxX + 5;
            minX = this.ChartRealTrafficWeb.Series["实时网络数据流"].Points.FindMinValue("X").XValue > this.ChartRealTrafficWeb.Series["实时视频播放流"].Points.FindMinValue("X").XValue ? this.ChartRealTrafficWeb.Series["实时网络数据流"].Points.FindMinValue("X").XValue : this.ChartRealTrafficWeb.Series["实时视频播放流"].Points.FindMinValue("X").XValue;
            this.ChartRealTrafficWeb.ChartAreas[0].AxisX.Minimum = minX + 5;


            //确定ChartVideoFrameSeq图的横纵轴
            maxY = minY = maxX = minX = 0;
            foreach (Series iSeries in this.ChartSequenceWeb.Series)
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
            this.ChartSequenceWeb.ChartAreas[0].AxisY.Maximum = ((int)(maxY / 100) + 1) * 100;
            this.ChartSequenceWeb.ChartAreas[0].AxisY.Minimum = ((int)(minY / 100) + 1) * 100;
            this.ChartSequenceWeb.ChartAreas[0].AxisX.Maximum = maxX;
            this.ChartSequenceWeb.ChartAreas[0].AxisX.Minimum = minX;
            this.ChartSequenceWeb.ChartAreas[0].AxisX.Interval = (maxX / 5 > 0) ? (maxX / 5) : 1;

            //使能画图区域的滚动条
            this.EnableScroll(ChartAccumulatedTrafficWeb.ChartAreas[0]);
            this.EnableScroll(ChartRealTrafficWeb.ChartAreas[0]);
            this.EnableScroll(ChartSequenceWeb.ChartAreas[0]);

            //图像重构
            this.ChartAccumulatedTrafficWeb.Invalidate();
            this.ChartRealTrafficWeb.Invalidate();
            this.ChartSequenceWeb.Invalidate();

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
                    this.ChartAccumulatedTrafficWeb.Invoke(addDataDel, this.ChartAccumulatedTrafficWeb, this.ChartAccumulatedTrafficWeb.Series["累积网络数据流"], Convert.ToDouble(ld[0]), Convert.ToDouble(ld[2]) / 1000);
                    this.ChartRealTrafficWeb.Invoke(addDataDel, this.ChartRealTrafficWeb, this.ChartRealTrafficWeb.Series["实时网络数据流"], Convert.ToDouble(ld[0]), Convert.ToDouble(ld[1]) / 1000);
                }
                else if (strfile.Equals("play_flow_smoothed.txt"))
                {
                    this.ChartAccumulatedTrafficWeb.Invoke(addDataDel, this.ChartAccumulatedTrafficWeb, this.ChartAccumulatedTrafficWeb.Series["累积视频播放流"], Convert.ToDouble(ld[0]), Convert.ToDouble(ld[2]) / 1000);
                    this.ChartRealTrafficWeb.Invoke(addDataDel, this.ChartRealTrafficWeb, this.ChartRealTrafficWeb.Series["实时视频播放流"], Convert.ToDouble(ld[0]), Convert.ToDouble(ld[1]) / 1000);
                }
                else if (strfile.Equals("flv_tag.txt"))
                {
                    double x = Convert.ToDouble(ld[0]);

                    if (ld[3].Equals("I视频帧"))
                    {
                        this.ChartSequenceWeb.Series[0].Points.AddXY(x, Convert.ToDouble(ld[4]));
                        this.ChartSequenceWeb.Series[2].Points.AddXY(x, Convert.ToDouble(ld[5]));
                    }
                    else if (ld[3].Equals("P视频帧"))
                    {
                        this.ChartSequenceWeb.Series[1].Points.AddXY(x, Convert.ToDouble(ld[4]));
                        this.ChartSequenceWeb.Series[3].Points.AddXY(x, Convert.ToDouble(ld[5]));
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
                AverValueWeb.FrameRateWeb = sr.ReadLine();
                AverValueWeb.FrameRateWeb += ("\r\n" + sr.ReadLine());
                AverValueWeb.FrameRateWeb += ("\r\n" + sr.ReadLine());
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
            this.ChartAccumulatedTrafficWeb.Invoke(clearDataDel, this.ChartAccumulatedTrafficWeb);
            this.ChartRealTrafficWeb.Invoke(clearDataDel, this.ChartRealTrafficWeb);
            this.ChartSequenceWeb.Invoke(clearDataDel, this.ChartSequenceWeb);
        }

        /***************************************************************
              对Pcap包分析，得到文件概要、数据包、TCP、DNS、HTTP等信息
          *****************************************************************/
        private void PcapTcpDnsHttpAnalys()
        {
            int i = -1;
            try
            {
                i = pcap_file_dissect_inCS(PcapFileNameWeb);
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
                WrongReasonWeb = "数据包打开错误，无法进行文件概要、数据包、TCP、DNS、HTTP的分析 \n";
                return;
            }
            else
            {
                if (!ShowLVSum()) WrongReasonWeb += "文件概要分析异常 \n";
                if (!ShowLVDNSWebAnalys()) WrongReasonWeb += "DNS分析异常 \n";
                if (!ShowLVHTTPAnalysWeb()) WrongReasonWeb += "HTTP分析异常 \n";
                try
                {
                    DelayJitterAnalys();
                }
                catch
                {
                    WrongReasonWeb += "无法获得延时抖动信息\n";
                }
                if (!ShowTCPStream()) WrongReasonWeb += "TCP流分析异常 \n";
                if (!ShowLVPacketAnalysWeb()) WrongReasonWeb += "数据包解析异常 \n";

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
            LVSumWeb.Items.Clear();

            //临时文件名称
            string tmpfileName = "dissectWebSum.tmp";

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
                LVSumWeb.Items.Add(lvi);

                strLine = sr.ReadLine();
            }
            sr.Close();

            //删除临时文件
            File.Delete(tmpfileName);
            return true;
        }

        //向数据包列表填充数据函数
        [DllImport("NetpryDll.dll")]
        //public extern static int pcb_list_tostr(string tmpfileName);
        public extern static int pcb_list_tofile(string tmpfileName);
        [DllImport("NetpryDll.dll")]
        //public extern static int pcb_list_tostr(string tmpfileName);
        public extern static int getPacketNum();

        private bool ShowLVPacketAnalysWeb()
        {

            //清除原有的项
            LVPacketAnalysWeb.Items.Clear();

            //临时文件名称
            string tmpfileName = "dissectWebPacket.tmp";

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
                //retCode = pcb_list_tostr(tmpfileName);
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


            currentPageWeb = 1;    //每次分析都将页码初始化为1
            //这里要用接口获取包总数
            totalNumWeb = getPacketNum();
            //totalNumWeb=200000;
            if (totalNumWeb / PACKETPAGESIZEWEB > 0)
            {
                if (totalNumWeb % PACKETPAGESIZEWEB == 0)
                {
                    pageNumWeb = totalNumWeb / PACKETPAGESIZEWEB;
                }
                else
                    pageNumWeb = totalNumWeb / PACKETPAGESIZEWEB + 1;
            }
            else
                pageNumWeb = 1;
            //初始化时上一页不能用，下一页要判断总页数和每一页的记录数2000的大小
            if (pageNumWeb > 1)
            {
                btnNextPageWeb.Enabled = true;
            }

            btnJumpWeb.Enabled = true;     //使能跳转，在响应函数里判断输入范围

            labelTotalWeb.Text = "总记录数:" + totalNumWeb + " 总页数:" + pageNumWeb;
            labelTotalWeb.Enabled = true;

            comboxJumpPageWeb.Items.Clear();
            comboxJumpPageWeb.Enabled = true;    //初始选择combox
            for (int i = 0; i < pageNumWeb; i++)
            {
                comboxJumpPageWeb.Items.Add(i + 1);
            }
            comboxJumpPageWeb.SelectedIndex = 0;

            getPageRecord(tmpfileName, currentPageWeb);
            //创建文件读流
            //StreamReader sr = new StreamReader(tmpfileName);
            //string strLine = sr.ReadLine();

            //ListView数据项和子数据项
            //ListViewItem[] lvi;
            //ListViewItem.ListViewSubItem lvsi;

            //const int GROUPCOUNTWEB = 200;
            //lvi = new ListViewItem[GROUPCOUNTWEB];     //200个为一组，批量刷新list
            //int lineCount = 0;
            //int sumInPage = 0;
            //读取每一行数据，第一次加载数据
            //while (strLine != null && sumInPage < PACKETPAGESIZEWEB) //文件没有读取到记录或达到一页的展示量2000
            //{
            //    //得到每一单元数据
            //    string[] str = strLine.Split(new Char[] { '\t' }, 11);

            //    lvi[lineCount] = new ListViewItem();
            //    lvi[lineCount].Text = str[0];

            //    for (int i = 1; i < 11; i++)
            //    {
            //        lvsi = new ListViewItem.ListViewSubItem();
            //        lvsi.Text = str[i];
            //        lvi[lineCount].SubItems.Add(lvsi);
            //    }

            //    strLine = sr.ReadLine();
            //    lineCount++;
            //    sumInPage++;

            //    if (lineCount % GROUPCOUNTWEB == 0)              //每200条记录刷新一次
            //    {
            //        LVPacketAnalysWeb.BeginUpdate();       //控制刷新
            //        //加入ListView
            //        LVPacketAnalysWeb.Items.AddRange(lvi);
            //        LVPacketAnalysWeb.EndUpdate();

            //        lvi = new ListViewItem[GROUPCOUNTWEB];
            //        lineCount = 0;
            //    }
            //}

            //LVPacketAnalysWeb.BeginUpdate();     //当没有达到50条时没有刷新所以在这里要刷新
            ////加入最后一批
            //for (int i = 0; i < lineCount; i++)
            //{
            //    LVPacketAnalysWeb.Items.Add(lvi[i]);
            //}
            //LVPacketAnalysWeb.EndUpdate();
            //sr.Close();

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
            //PcapFileNameWeb = inisWeb.IniReadValue("Flv", "PcapFile");
            //TxtFileNameWeb = inisWeb.IniReadValue("Flv", "PlayerFile");

            /**********填充TCP常规列表**********/
            //清除原有的项
            LVTCPGeneralWeb.Items.Clear();

            //临时文件名称
            string tmpfileName = "dissectWebTcp.tmp";

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

            //调用Tcp流的BackgroundWorker，用于填充Tcp流的界面，防止阻塞
            //m_AsyncWorkerTcp.RunWorkerAsync(tmpfileName);

            currentPageWebTcpGene = 1;    //每次分析都将页码初始化为1
            //这里要用接口获取包总数
            totalNumWebTcpGene = getTcpStreamNum();
            //totalNumWebTcpGene = 200000;
            if (totalNumWebTcpGene / PACKETPAGESIZEWEB > 0)
            {
                if (totalNumWebTcpGene % PACKETPAGESIZEWEB == 0)
                {
                    pageNumWebTcpGene = totalNumWebTcpGene / PACKETPAGESIZEWEB;
                }
                else
                    pageNumWebTcpGene = totalNumWebTcpGene / PACKETPAGESIZEWEB + 1;
            }
            else
                pageNumWebTcpGene = 1;
            //初始化时上一页不能用，下一页要判断总页数和每一页的记录数2000的大小
            if (pageNumWebTcpGene > 1)
            {
                btnNextPageWebTcpGene.Enabled = true;
            }

            btnJumpWebTcpGene.Enabled = true;     //使能跳转，在响应函数里判断输入范围

            labelTotalWebTcpGene.Text = "总记录数:" + totalNumWebTcpGene + "总页数:" + pageNumWebTcpGene;
            labelTotalWebTcpGene.Enabled = true;

            comboxJumpPageWebTcpGene.Items.Clear();
            comboxJumpPageWebTcpGene.Enabled = true;    //初始选择combox
            for (int i = 0; i < pageNumWebTcpGene; i++)
            {
                comboxJumpPageWebTcpGene.Items.Add(i + 1);
            }
            comboxJumpPageWebTcpGene.SelectedIndex = 0;

            getPageRecordTcpGene(tmpfileName, currentPageWebTcpGene);   //获取前面2000条记录

            //创建文件读流
            StreamReader sr = new StreamReader(tmpfileName);
            string strLine = sr.ReadLine();

            //ListView数据项和子数据项
            //ListViewItem[] lvi;
            //ListViewItem.ListViewSubItem lvsi;

            //const int GROUPCOUNTWEB = 50;
            //lvi = new ListViewItem[GROUPCOUNTWEB];
            //int lineCount = 0;
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

                //lvi[lineCount] = new ListViewItem();
                //lvi[lineCount].Text = str[0];

                //for (int i = 1; i < 12; i++)
                //{
                //    lvsi = new ListViewItem.ListViewSubItem();
                //    lvsi.Text = str[i];
                //    lvi[lineCount].SubItems.Add(lvsi);
                //}
                //记录TCP连接的参数值
                tcpconnecttimes++;
                tcpuptimes += int.Parse(str[5]);
                tcpupflow += int.Parse(str[6]);
                tcpdowntimes += int.Parse(str[7]);
                tcpdownflow += int.Parse(str[8]);
                averrtt += double.Parse(str[11]);

                strLine = sr.ReadLine();
                //lineCount++;

                //if (lineCount % GROUPCOUNTWEB == 0)
                //{
                //    LVTCPGeneralWeb.BeginUpdate();
                //    //加入ListView
                //    LVTCPGeneralWeb.Items.AddRange(lvi);
                //    LVTCPGeneralWeb.EndUpdate();

                //    lvi = new ListViewItem[GROUPCOUNTWEB];
                //    lineCount = 0;
                //}
            }

            //LVTCPGeneralWeb.BeginUpdate();
            ////加入最后一批
            //for (int i = 0; i < lineCount; i++)
            //{
            //    LVTCPGeneralWeb.Items.Add(lvi[i]);
            //}
            //LVTCPGeneralWeb.EndUpdate();
            sr.Close();

            //删除临时文件
            //File.Delete(tmpfileName);

            //获取tcp连接信息，写入最后的日志文件中
            averrtt /= tcpconnecttimes;
            averrtt = Math.Round(averrtt, 3);
            AverValueWeb.TcpInfoWeb = "TCP连接信息如下：\t\r\n";
            AverValueWeb.TcpInfoWeb += "TCP连接个数 \t" + tcpconnecttimes.ToString() + "\t\r\n";
            AverValueWeb.TcpInfoWeb += "TCP上行包个数 \t" + tcpuptimes.ToString() + "\t\r\n";
            AverValueWeb.TcpInfoWeb += "TCP上行包流量(字节)  \t" + tcpupflow.ToString() + "\t\r\n";
            AverValueWeb.TcpInfoWeb += "TCP下行包个数 \t" + tcpdowntimes.ToString() + "\t\r\n";
            AverValueWeb.TcpInfoWeb += "TCP下行包流量(字节)  \t" + tcpdownflow.ToString() + "\t\r\n";
            AverValueWeb.TcpInfoWeb += "RTT均值(秒) \t" + averrtt.ToString() + "\t";


            /***********填充TCP异常列表********************/

            //清除原有的项
            LVTCPExWeb.Items.Clear();

            //临时文件名称
            tmpfileName = "dissectTcpExWebcep.tmp";

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
            int groupcountExc = 50;
            StreamReader srExcep;
            string strLineExcep;
            srExcep = new StreamReader(tmpfileName);
            strLineExcep = srExcep.ReadLine();

            ListViewItem[] lvi;
            ListViewItem.ListViewSubItem lvsi;
            int lineCount;
            lvi = new ListViewItem[groupcountExc];
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

                if (lineCount % groupcountExc == 0)
                {
                    LVTCPExWeb.BeginUpdate();
                    //加入ListView
                    LVTCPExWeb.Items.AddRange(lvi);
                    LVTCPExWeb.EndUpdate();

                    lvi = new ListViewItem[groupcountExc];
                    lineCount = 0;
                }
            }

            LVTCPExWeb.BeginUpdate();
            //加入最后一批
            for (int i = 0; i < lineCount; i++)
            {
                LVTCPExWeb.Items.Add(lvi[i]);
            }
            LVTCPExWeb.EndUpdate();
            srExcep.Close();
            redeliverrate = Math.Round(redeliverrate * 100 / tcpCnt, 3);

            AverValueWeb.TcpExWeb = "TCP异常信息如下：\t\r\n";
            AverValueWeb.TcpExWeb += "前数据包丢失次数 \t" + tcplastpaclost.ToString() + "\t\r\n";
            AverValueWeb.TcpExWeb += "重复确认次数 \t" + reack.ToString() + "\t\r\n";
            AverValueWeb.TcpExWeb += "乱序次数 \t" + wrongorder.ToString() + "\t\r\n";
            AverValueWeb.TcpExWeb += "重传次数 \t" + redeliver.ToString() + "\t\r\n";
            AverValueWeb.TcpExWeb += "重传率 \t" + redeliverrate.ToString() + "%\t\r\n";
            //删除临时文件
            File.Delete(tmpfileName);
            return true;
        }

        //向DNS列表填充数据函数
        //DNS信息获取函数
        [DllImport("NetpryDll.dll")]
        //public extern static int dns_anal_tostr(string tmpfileName);
        public extern static int dns_anal_tofile(string tmpfileName);
        private bool ShowLVDNSWebAnalys()
        {

            //清除原有的项
            LVDNSWebAnalys.Items.Clear();

            //临时文件名称
            string tmpfileName = "dissectWebDNS.tmp";

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

            const int groupcountdns = 50;
            lvi = new ListViewItem[groupcountdns];
            int lineCount = 0;

            //设置图像的横纵坐标
            double xValue = 1.0;
            double yValue = 0.0;
            bool yValueGet = false;
            // Set series chart type
            ChartDNSWeb.Series["响应时间(秒)"].Type = SeriesChartType.Bar;
            // Set series point width
            ChartDNSWeb.Series["响应时间(秒)"]["PointWidth"] = "1.0";
            // Show data points labels
            ChartDNSWeb.Series["响应时间(秒)"].ShowLabelAsValue = false;
            // Set data points label style
            ChartDNSWeb.Series["响应时间(秒)"]["BarLabelStyle"] = "Center";
            // Display chart as 3D
            ChartDNSWeb.ChartAreas[0].Area3DStyle.Enable3D = false;
            // Draw the chart as embossed
            ChartDNSWeb.Series["响应时间(秒)"]["DrawingStyle"] = "Emboss";

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

                if (lineCount % groupcountdns == 0)
                {
                    LVDNSWebAnalys.BeginUpdate();
                    //加入ListView
                    LVDNSWebAnalys.Items.AddRange(lvi);
                    LVDNSWebAnalys.EndUpdate();

                    lvi = new ListViewItem[groupcountdns];
                    lineCount = 0;
                }

                yValueGet = double.TryParse(str[8], out yValue);
                if (!yValueGet) yValue = 0.0;
                AverValueWeb.AverDNSWeb += yValue;

                ChartDNSWeb.Invoke(addDataDel, ChartDNSWeb, ChartDNSWeb.Series["响应时间(秒)"], xValue++, yValue);

            }
            if (xValue > 0)
                AverValueWeb.AverDNSWeb /= (int)xValue;
            AverValueWeb.AverDNSWeb = Math.Round(AverValueWeb.AverDNSWeb, 6);
            LVDNSWebAnalys.BeginUpdate();
            //加入最后一批
            for (int i = 0; i < lineCount; i++)
            {
                LVDNSWebAnalys.Items.Add(lvi[i]);
            }
            LVDNSWebAnalys.EndUpdate();
            sr.Close();

            //曲线图更新
            ChartDNSWeb.Invalidate();
            Console.WriteLine("DNS into Mysql!");
            //txt文件压入到数据库
            if (mysqlWebFlagA && serverTest)
                mysqlWebA.TxTInsertMySQL("DNSAnalysis", currentId + "#" + "Web", Application.StartupPath + "\\" + tmpfileName);
            //删除临时文件
#if RELEASE
            File.Delete(tmpfileName);
#endif
            return true;
        }

        //向HTTP列表填充数据函数
        //HTTP信息获取函数
        [DllImport("NetpryDll.dll")]
        public extern static int http_anal_tofile(string tmpfileName);
        //[DllImport("NetpryDll.dll")]
        //public extern static int http_anal_tofile2(string tmpfileName);
        private bool ShowLVHTTPAnalysWeb()
        {

            //清除原有的项
            LVHTTPAnalysWeb.Items.Clear();

            //临时文件名称
            string tmpfileName = "dissectWebHTTP.tmp";

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
            sr.ReadLine();
            string strLine = sr.ReadLine();
            //ListView数据项和子数据项
            ListViewItem[] lvi;
            ListViewItem.ListViewSubItem lvsi;

            const int groupcounthttp = 50;
            lvi = new ListViewItem[groupcounthttp];
            int lineCount = 0;
            int SumLine = 0;
            double HttpDelay = 0.0;


            //设置图像的横纵坐标
            double xValue = 1.0;
            double yValue = 0.0;
            bool HttpDelayGet = false;
            // Set series chart type
            //ChartHttpWeb.Series["响应时间(秒)"].Type = SeriesChartType.Bar;
            // Set series point width
            ChartHttpWeb.Series["响应时间(毫秒)"]["PointWidth"] = "1.0";
            // Show data points labels
            ChartHttpWeb.Series["响应时间(毫秒)"].ShowLabelAsValue = false;
            // Set data points label style
            ChartHttpWeb.Series["响应时间(毫秒)"]["BarLabelStyle"] = "Center";
            // Display chart as 3D
            ChartHttpWeb.ChartAreas[0].Area3DStyle.Enable3D = false;
            // Draw the chart as embossed
            ChartHttpWeb.Series["响应时间(毫秒)"]["DrawingStyle"] = "Emboss";


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


                lineCount++;
                SumLine++;
                strLine = sr.ReadLine();

                if (lineCount % groupcounthttp == 0)
                {
                    LVHTTPAnalysWeb.BeginUpdate();
                    //加入ListView
                    LVHTTPAnalysWeb.Items.AddRange(lvi);
                    LVHTTPAnalysWeb.EndUpdate();

                    lvi = new ListViewItem[groupcounthttp];
                    lineCount = 0;
                }
                //计算AverHTTPWeb的值
                HttpDelayGet = double.TryParse((str[6]), out HttpDelay);
                if (!HttpDelayGet) HttpDelay = 0.0;
                yValue = HttpDelay * 1000;
                AverValueWeb.AverHTTPWeb += HttpDelay;

                ChartHttpWeb.Invoke(addDataDel, ChartHttpWeb, ChartHttpWeb.Series["响应时间(毫秒)"], xValue++, yValue);
            }
            //计算服务器响应时间
            if (SumLine > 0)
                AverValueWeb.AverHTTPWeb /= SumLine;
            AverValueWeb.AverHTTPWeb = Math.Round(AverValueWeb.AverHTTPWeb, 6);

            LVHTTPAnalysWeb.BeginUpdate();
            //加入最后一批
            for (int i = 0; i < lineCount; i++)
            {
                LVHTTPAnalysWeb.Items.Add(lvi[i]);
            }
            LVHTTPAnalysWeb.EndUpdate();
            sr.Close();


            //曲线图更新
            ChartHttpWeb.Invalidate();

            //txt文件压入到数据库 
            if (mysqlWebFlagA && serverTest)
                mysqlWebA.TxTInsertMySQL("HttpAnalysis", currentId + "#" + "Web", Application.StartupPath + "\\" + tmpfileName);

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
            if (!ShowLVInOutWeb(PcapFileNameWeb, ScaleComboBoxWeb.SelectedIndex))
            {
                WrongReasonWeb += "吞吐量分析异常 \n";
            }

            if (!ShowLVFrameLengthWeb())
            {
                WrongReasonWeb += "帧长分布分析异常 \n";
            }

            return;
        }

        //引入pcap文件解析函数(Link)，负责吞吐量和帧长分布列表
        [DllImport("LinkAnal.dll")]
        public extern static int link_analyze_inCS(string PcapFile, double scale, string tmpfileName,
    ref int totalPktCount, int[] rangeCountWeb);
        //向吞吐量列表添加数据
        private bool ShowLVInOutWeb(string PcapFile, int selectIndex)
        {
            int index = selectIndex;
            double scale = timeScaleWeb[index];
            string tmpfileName = "LinkAnalWeb.tmp";
            string fileName = "InOutWeb.txt";
            int SumLine = 0;

            //创建临时文本文件 
            FileStream fs = File.Create(tmpfileName);
            fs.Close();

            //调用链路分析函数,会生成LinkAnal.tmp文件
            int retCode = -1;
            try
            {
                retCode = link_analyze_inCS(PcapFile, scale, tmpfileName, ref totalPacketCntWeb, rangeCountWeb);
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
            LVInOutWeb.Items.Clear();

            ListViewItem lvi;
            ListViewItem.ListViewSubItem lvsi;

            //创建文件读流
            StreamReader sr = new StreamReader(tmpfileName);

            FileStream fstream = new FileStream(fileName, FileMode.Create);
            StreamWriter sw = new StreamWriter(fstream, Encoding.Default);
            sw.Write("序号\t时间\t流量\n");
            int ind = 1;



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

                //写入inout.txt
                sw.Write("{0}\t{1}\t{2}\n", ind++, ((counter - 1) * scale).ToString(), strLine);




                //吞吐量最值
                if (Convert.ToDouble(strLine) >= MaxInOut)
                    MaxInOut = Convert.ToDouble(strLine);
                if (Convert.ToDouble(strLine) <= MinInOut)
                    MinInOut = Convert.ToDouble(strLine);

                //加入ListView
                LVInOutWeb.Items.Add(lvi);

                yValueGet = double.TryParse(strLine, out yValue);
                if (!yValueGet) yValue = 0.0;
                AverValueWeb.AverInOutWeb += yValue;
                SumLine++;
                xValue += scale;
                ChartInOutWeb.Invoke(addDataDel, ChartInOutWeb, ChartInOutWeb.Series["吞吐量曲线"], xValue, yValue);

                strLine = sr.ReadLine();

            }
            sr.Close();
            sw.Close();
            fstream.Close();

            //计算吞吐量均值
            if (SumLine > 0)
            {
                if (index == 1) SumLine /= 10;
                else if (index == 2) SumLine /= 100;
                AverValueWeb.AverInOutWeb /= SumLine;
            }
            AverValueWeb.AverInOutWeb = Math.Round(AverValueWeb.AverInOutWeb, 2);

            //显示吞吐量最值和均值
            this.InOutMaxWeb.Text += MaxInOut.ToString() + "字节";
            this.InOutMinWeb.Text += MinInOut.ToString() + "字节";
            this.InOutAvgWeb.Text += AverValueWeb.AverInOutWeb.ToString() + "字节";
            this.InOutAvgWeb.Visible = true;
            this.InOutMaxWeb.Visible = true;
            this.InOutMinWeb.Visible = true;

            //确定横纵坐标轴的尺寸
            double maxY = 0;
            double minY = 0;
            double maxX = 0;
            double minX = 0;
            foreach (Series iSeries in this.ChartInOutWeb.Series)
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

            this.ChartInOutWeb.ChartAreas[0].AxisX.Minimum = minX;
            this.ChartInOutWeb.ChartAreas[0].AxisX.Maximum = maxX;
            this.ChartInOutWeb.ChartAreas[0].AxisY.Minimum = minY;
            this.ChartInOutWeb.ChartAreas[0].AxisY.Maximum = maxY;

            //确定横纵轴的间隔数10,保证轴的间隔不能为0(当曲线上只有一点时可能会出现这种情况)
            this.ChartInOutWeb.ChartAreas[0].AxisX.Interval = (((maxX - minX) / 10 > 0) ? ((maxX - minX) / 10) : 1);
            this.ChartInOutWeb.ChartAreas[0].AxisY.Interval = (((maxY - minY) / 10 > 0) ? ((maxY - minY) / 10) : 1);

            //更新图像
            this.ChartInOutWeb.Invalidate();

            //txt文件压入到数据
            //if (mysqlWebFlagA && serverTest)
            mysqlWebA.TxTInsertMySQL("InOutAnalysis", currentId + "#" + "Web", Application.StartupPath + "\\" + fileName);


            //删除临时文件
            //  File.Delete(tmpfileName);
            return true;
        }

        //调整时间尺度(只对吞吐量有影响)
        private void ScaleComboBoxWeb_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ScaleComboBoxWeb.SelectedIndex < 0 || ScaleComboBoxWeb.SelectedIndex == prevSelectIndexWeb)
            {
                return;
            }

            if (PcapFileNameWeb == null)
            {
                return;
            }
            InOutAvgWeb.Text = "平均值：";
            InOutMaxWeb.Text = "最大值：";
            InOutMinWeb.Text = "最小值：";
            //先将ChartInOutWeb中的画面清空
            ChartInOutWeb.Invoke(clearDataDel, ChartInOutWeb);
            //刷新数据
            ShowLVInOutWeb(PcapFileNameWeb, ScaleComboBoxWeb.SelectedIndex);
            //刷新选项索引值
            prevSelectIndexWeb = ScaleComboBoxWeb.SelectedIndex;

        }

        //向帧长分析列表添加数据
        private bool ShowLVFrameLengthWeb()
        {
            //清空帧长分布列表
            LVFrameLengthWeb.Items.Clear();
            string tmpfileName = "FrameLengthWeb.txt";

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
                lvsi.Text = rangeCountWeb[i].ToString();
                lvi.SubItems.Add(lvsi);

                lvsi = new ListViewItem.ListViewSubItem();
                double percent;
                if (totalPacketCntWeb == 0)
                    percent = 0.0;
                else
                    percent = rangeCountWeb[i] * 100.0 / totalPacketCntWeb;
                lvsi.Text = percent.ToString("F2") + "%";
                yValue[i] = Math.Round(percent, 2);
                lvi.SubItems.Add(lvsi);
                //加入ListView
                LVFrameLengthWeb.Items.Add(lvi);
            }
            //画帧长分布的饼图
            ChartFrameLengthWeb.Series["帧长分布"].Points.DataBindXY(strRange, yValue);
            ChartFrameLengthWeb.Invalidate();

            //重新生成txt
            FileStream fs = new FileStream(tmpfileName, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);
            sw.Write("序号\t帧长范围\t数量\t比率\n");

            //fprintf(fp, "序号\t帧长范围\t数量\t比率\n");
            int index = 1;
            for (int i = 0; i < 11; i++)
            {

                sw.Write("{0}\t{1}\t{2}\t{3}\n", index++, strRange[i], rangeCountWeb[i], (rangeCountWeb[i] * 100.0 / totalPacketCntWeb).ToString("F2") + "%");
            }
            sw.Close();
            fs.Close();

            //if (mysqlWebFlagA && serverTest)
            mysqlWebA.TxTInsertMySQL("FrameLengthAnalysis", currentId + "#" + "Web", Application.StartupPath + "\\" + tmpfileName);

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
                AnalysOK = ShowLVDelayJitterWeb(PcapFileNameWeb);
                if (!AnalysOK)
                    WrongReasonWeb += "延时抖动分析异常 \n";
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
        public bool ShowLVDelayJitterWeb(string PcapFile)
        {
            //清空DelayJitter中的所有数据
            LVDelayJitterWeb.Items.Clear();

            string tmpfileName = "DelayJitterWeb.txt";

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
                lv[linecount].Text = str[1];

                //求延时抖动最值
                if (Convert.ToDouble(str[3]) >= MaxDelay)
                    MaxDelay = Convert.ToDouble(str[3]);
                if (Convert.ToDouble(str[3]) <= MinDelay)
                    MinDelay = Convert.ToDouble(str[3]);

                if (Convert.ToDouble(str[4]) >= MaxJitter)
                    MaxJitter = Convert.ToDouble(str[4]);
                if (Convert.ToDouble(str[4]) <= MinJitter)
                    MinJitter = Convert.ToDouble(str[3]);

                yDelayValueGet = double.TryParse(str[3], out yDelayValue);
                if (!yDelayValueGet) yDelayValue = 0.0;
                AverValueWeb.AverDelayWeb += yDelayValue;
                yJitterValueGet = double.TryParse(str[4], out yJitterValue);
                if (!yJitterValueGet) yJitterValue = 0.0;
                AverValueWeb.AverJitterWeb += yJitterValue;
                //画图时以毫秒为单位
                yDelayValue *= 1000;
                yJitterValue *= 1000;

                ChartDelayJitterWeb.Invoke(addDataDel, ChartDelayJitterWeb, ChartDelayJitterWeb.Series["延时曲线"], xValue, yDelayValue);
                ChartDelayJitterWeb.Invoke(addDataDel, ChartDelayJitterWeb, ChartDelayJitterWeb.Series["抖动曲线"], xValue++, yJitterValue);

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
                    LVDelayJitterWeb.BeginUpdate();//加入ListView
                    LVDelayJitterWeb.Items.AddRange(lv);
                    LVDelayJitterWeb.EndUpdate();
                    linecount = 0;
                }

            }

            LVDelayJitterWeb.BeginUpdate();
            for (int i = 0; i < linecount; i++)
            {
                LVDelayJitterWeb.Items.Add(lv[i]);
            }
            LVDelayJitterWeb.EndUpdate();
            sr.Close();
            // File.Delete(tmpfileName);
            if (xValue > 0)
            {
                AverValueWeb.AverDelayWeb /= (int)xValue;
                AverValueWeb.AverJitterWeb /= (int)xValue;

            }
            AverValueWeb.AverDelayWeb = Math.Round(AverValueWeb.AverDelayWeb, 6);
            AverValueWeb.AverJitterWeb = Math.Round(AverValueWeb.AverJitterWeb, 6);

            //显示延时抖动的最值和均值
            this.DelayMaxWeb.Text += (MaxDelay * 1000).ToString() + "ms";
            this.DelayMinWeb.Text += (MinDelay * 1000).ToString() + "ms";
            this.DelayAvgWeb.Text += (AverValueWeb.AverDelayWeb * 1000).ToString() + "ms";

            this.JitterMaxWeb.Text += (MaxJitter * 1000).ToString() + "ms";
            this.JitterMinWeb.Text += (MinJitter * 1000).ToString() + "ms";
            this.JitterAvgWeb.Text += (AverValueWeb.AverJitterWeb * 1000).ToString() + "ms";

            this.DelayAvgWeb.Visible = true;
            this.DelayMaxWeb.Visible = true;
            this.DelayMinWeb.Visible = true;
            this.JitterAvgWeb.Visible = true;
            this.JitterMaxWeb.Visible = true;
            this.JitterMinWeb.Visible = true;


            //找到横纵轴的最大最小值,确定尺度
            double maxY = 0;
            double minY = 0;
            double maxX = 0;
            double minX = 0;
            foreach (Series iSeries in this.ChartDelayJitterWeb.Series)
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

            this.ChartDelayJitterWeb.ChartAreas[0].AxisX.Minimum = minX;
            this.ChartDelayJitterWeb.ChartAreas[0].AxisX.Maximum = maxX;
            this.ChartDelayJitterWeb.ChartAreas[0].AxisY.Minimum = minY;
            this.ChartDelayJitterWeb.ChartAreas[0].AxisY.Maximum = maxY;

            //确定横纵轴的间隔数10
            this.ChartDelayJitterWeb.ChartAreas[0].AxisX.Interval = (((maxX - minX) / 10 > 0) ? ((maxX - minX) / 10) : 1);
            this.ChartDelayJitterWeb.ChartAreas[0].AxisY.Interval = (((maxY - minY) / 10 > 0) ? ((maxY - minY) / 10) : 1);

            //txt文件压入到数据库
            if (mysqlWebFlagA && serverTest)
                mysqlWebA.TxTInsertMySQL("DelayJitter", currentId + "#" + "Web", Application.StartupPath + "\\" + tmpfileName);

            //图像重构
            this.ChartDelayJitterWeb.Invalidate();
            return true;

        }

        /**************************************************************************************
                        测试报告显示
       ****************************************************************************************/
        private void ResultDisplay()
        {
            ResultDisplay2();              //测试报告生成总的txt形式

            //txt写入listview
            StreamReader readData = new StreamReader(strTxtResultWeb, Encoding.Default);//开启读的文件流
            string lineData = null;    //每一行的数据，标准格式，以分隔符分割
            lsvResultWeb.BeginUpdate();
            while ((lineData = readData.ReadLine()) != null)
            {
                string[] temp = lineData.Split('\t');
                ListViewItem lvi = new ListViewItem();
                lvi.Text = temp[0];
                for (int i = 1; i < temp.Length; i++)
                {
                    lvi.SubItems.Add(temp[i]);
                }
                lsvResultWeb.Items.Add(lvi);

            }
            lsvResultWeb.EndUpdate();
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
            string resultTxt = "ResultTxtWeb.tmp";
            StreamWriter ResultTmp = new StreamWriter(File.Create(resultTxt), Encoding.Default);  //临时总结报告文件，用于满足特定的格式压入数据库
            int index = 0;
            ResultTmp.Write("Index\tColumn\tValue\r\n");
            if (!File.Exists(strTxtResultWeb))
            {
                using (StreamWriter swlog = new StreamWriter(File.Create(strTxtResultWeb), Encoding.Default))
                {
                    //保存得到的各种缺陷指标的均值
                    swlog.Write("\r\nWEB测试中各个参数均值如下：\r\n");
                    if (AverValueWeb.FrameRateWeb == null)
                        AverValueWeb.FrameRateWeb = "WEB分析不成功，无法获得视频帧率";
                    //swlog.Write(AverValue.FrameRate + "\t\r\n");
                    swlog.Write("DNS响应平均延时(秒)\t" + AverValueWeb.AverDNSWeb.ToString() + "\t\r\n");
                    ResultTmp.Write((++index).ToString() + "\t" + "DNS响应平均延时(s)\t" + AverValueWeb.AverDNSWeb.ToString() + "\r\n");

                    swlog.Write("HTTP响应平均延时(秒)\t" + AverValueWeb.AverHTTPWeb.ToString() + "\t\r\n");
                    ResultTmp.Write((++index).ToString() + "\t" + "HTTP响应平均延时(s)\t" + AverValueWeb.AverHTTPWeb.ToString() + "\r\n");

                    swlog.Write("服务器响应平均延时(秒)\t" + AverValueWeb.AverHTTPWeb.ToString() + "\t\r\n");
                    ResultTmp.Write((++index).ToString() + "\t" + "服务器响应平均延时(s)\t" + AverValueWeb.AverHTTPWeb.ToString() + "\r\n");

                    swlog.Write("吞吐量均值(字节/秒))\t" + AverValueWeb.AverInOutWeb.ToString() + "\t\r\n");
                    ResultTmp.Write((++index).ToString() + "\t" + "吞吐量均值(byte)\t" + AverValueWeb.AverInOutWeb.ToString() + "\r\n");

                    swlog.Write("平均延时(秒)\t" + AverValueWeb.AverDelayWeb.ToString() + "\t\r\n");
                    ResultTmp.Write((++index).ToString() + "\t" + "平均延时(s)\t" + AverValueWeb.AverDelayWeb.ToString() + "\r\n");

                    swlog.Write("平均抖动(秒)\t" + AverValueWeb.AverJitterWeb.ToString() + "\t\r\n");
                    ResultTmp.Write((++index).ToString() + "\t" + "平均抖动(s)\t" + AverValueWeb.AverJitterWeb.ToString() + "\r\n");
                    //写TCP连接信息
                    swlog.Write("\r\n" + AverValueWeb.TcpInfoWeb + "\r\n");
                    string[] tcpInfo = AverValueWeb.TcpInfoWeb.Split(new string[] { "\t\r\n" }, StringSplitOptions.RemoveEmptyEntries); ;
                    for (int i = 0; i < tcpInfo.Length; i++)
                    {
                        if (i == 0) continue;
                        ResultTmp.Write((++index).ToString() + "\t" + tcpInfo[i] + "\r\n");
                    }
                    //写入TCP异常信息
                    swlog.Write("\r\n" + AverValueWeb.TcpExWeb + "\r\n");
                    string[] tcpExInfo = AverValueWeb.TcpExWeb.Split(new string[] { "\t\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < tcpExInfo.Length; i++)
                    {
                        if (i == 0) continue;
                        ResultTmp.Write((++index).ToString() + "\t" + tcpExInfo[i] + "\r\n");
                    }
                    //加个随机的网页评分，后面再改
                    Random ro = new Random();
                    int iUp = 100;
                    int iDown = 50;
                    //int iResult = ro.Next(iDown, iUp);
                    string iResult = inisWeb.IniReadValue("Web", "score");
                    ResultTmp.WriteLine((++index).ToString() + "\tWebScore\t" + iResult);
                    swlog.Close();
                    ResultTmp.Close();
                    //txt文件压入到数据库
                    if (mysqlWebFlagA && serverTest)
                        mysqlWebA.TxTInsertMySQL("TestReport", currentId + "#" + "Web", Application.StartupPath + "\\" + resultTxt);
                    //删除临时文件
#if RELEASE
                    File.Delete(resultTxt);
#endif
                }
            }
        }


        private void btnWebSelCapWeb_Click(object sender, EventArgs e)
        {
            OpenFileDialog capFile = new OpenFileDialog();
            capFile.RestoreDirectory = true;
            capFile.Multiselect = false;
            capFile.Filter = "pcap文件|*.pcap";
            if (capFile.ShowDialog() == DialogResult.OK)
            {
                PcapFileNameWeb = capFile.FileName;
                strXlsLogFileWeb = capFile.FileName.Replace(".pcap", ".xlsx");
                inisWeb.IniWriteValue("Web", "webPcapPath", PcapFileNameWeb);
                MessageBox.Show("操作完成！");
                //isSelectPcapWeb = true;        //选择了pcap文件           
            }
            else
            {
                MessageBox.Show("请选择抓包文件！");
                return;
            }
        }

        private void btnLastPageWeb_Click(object sender, EventArgs e)    //上一页的响应函数
        {
            currentPageWeb--;    //当前页码自减
            btnNextPageWeb.Enabled = true;
            if (currentPageWeb == 1)
            {
                btnLastPageWeb.Enabled = false;    //第一页时上一页不能用
            }
            comboxJumpPageWeb.SelectedIndex = currentPageWeb - 1;
            string packageFile = "dissectWebPacket.tmp";
            getPageRecord(packageFile, currentPageWeb);    //获取当前页的记录
        }


        private void btnNextPageWeb_Click(object sender, EventArgs e)
        {
            currentPageWeb++;
            btnLastPageWeb.Enabled = true;    //上一页使能
            if (currentPageWeb == pageNumWeb)    //当前页是总页数，没有下一页
            {
                btnNextPageWeb.Enabled = false;
            }
            comboxJumpPageWeb.SelectedIndex = currentPageWeb - 1;
            string packageFile = "dissectWebPacket.tmp";
            getPageRecord(packageFile, currentPageWeb);    //获取当前页的记录
        }

        private void btnJumpWeb_Click(object sender, EventArgs e)
        {
            string selectText = comboxJumpPageWeb.Text;
            currentPageWeb = int.Parse(selectText);
            if (currentPageWeb == 1)
            {
                btnLastPageWeb.Enabled = false;
                btnNextPageWeb.Enabled = true;
            }
            else if (currentPageWeb == pageNumWeb)
            {
                btnNextPageWeb.Enabled = false;
                btnLastPageWeb.Enabled = true;
            }
            else
            {
                btnLastPageWeb.Enabled = true;
                btnNextPageWeb.Enabled = true;
            }
            string packageFile = "dissectWebPacket.tmp";
            getPageRecord(packageFile, currentPageWeb);    //获取当前页的记录
        }


        private bool getPageRecord(string packageFile, int currentPageWeb)
        {
            if (File.Exists(packageFile))    //打开包文件进行偏移取数
            {
                try
                {
                    FileStream fsPacket = new FileStream(packageFile, FileMode.Open, FileAccess.Read);
                    StreamReader srPacket = new StreamReader(fsPacket, Encoding.Default);
                    //string strLine = srPacket.ReadLine();    //第一行是标题行，去掉
                    string strLine = null;
                    if (currentPageWeb == 1)
                    {
                        strLine = srPacket.ReadLine();     //第一页时读取第一行
                    }
                    int count = 0;
                    while (count < (currentPageWeb - 1) * PACKETPAGESIZEWEB)
                    {
                        strLine = srPacket.ReadLine();     //相当于偏移掉前面(currentPageWeb-1) * PACKETPAGESIZEWEB,第1页从0条开始取，第2页从第2000条开始取
                        count++;
                    }
                    //清除上一次的列表
                    LVPacketAnalysWeb.Items.Clear();
                    //开始取数,取往后的2000条
                    int tempSumInPage = 0;
                    //ListView数据项和子数据项
                    ListViewItem[] lvi;
                    ListViewItem.ListViewSubItem lvsi;
                    lvi = new ListViewItem[GROUPCOUNTWEB];     //200个为一组，批量刷新list
                    int lineCount = 0;
                    while (strLine != null && tempSumInPage < PACKETPAGESIZEWEB) //文件没有读取到记录或达到一页的展示量2000
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

                        if (lineCount % GROUPCOUNTWEB == 0)              //每200条记录刷新一次
                        {
                            LVPacketAnalysWeb.BeginUpdate();       //控制刷新
                            //加入ListView
                            LVPacketAnalysWeb.Items.AddRange(lvi);
                            LVPacketAnalysWeb.EndUpdate();

                            lvi = new ListViewItem[GROUPCOUNTWEB];
                            lineCount = 0;
                        }
                    }

                    LVPacketAnalysWeb.BeginUpdate();     //当没有达到50条时没有刷新所以在这里要刷新
                    //加入最后一批
                    for (int i = 0; i < lineCount; i++)
                    {
                        LVPacketAnalysWeb.Items.Add(lvi[i]);
                    }
                    LVPacketAnalysWeb.EndUpdate();
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

        private void btnLastPageWebTcpGene_Click(object sender, EventArgs e)    //上一页的响应函数
        {
            currentPageWebTcpGene--;    //当前页码自减
            btnNextPageWebTcpGene.Enabled = true;
            if (currentPageWebTcpGene == 1)
            {
                btnLastPageWebTcpGene.Enabled = false;    //第一页时上一页不能用
            }
            comboxJumpPageWebTcpGene.SelectedIndex = currentPageWebTcpGene - 1;
            string packageFile = "dissectWebTcp.tmp";
            getPageRecordTcpGene(packageFile, currentPageWebTcpGene);    //获取当前页的记录
        }


        private void btnNextPageWebTcpGene_Click(object sender, EventArgs e)
        {
            currentPageWebTcpGene++;
            btnLastPageWebTcpGene.Enabled = true;    //上一页使能
            if (currentPageWebTcpGene == pageNumWebTcpGene)    //当前页是总页数，没有下一页
            {
                btnNextPageWebTcpGene.Enabled = false;
            }
            comboxJumpPageWebTcpGene.SelectedIndex = currentPageWebTcpGene - 1;
            string packageFile = "dissectWebTcp.tmp";
            getPageRecordTcpGene(packageFile, currentPageWebTcpGene);    //获取当前页的记录
        }

        private void btnJumpWebTcpGene_Click(object sender, EventArgs e)
        {
            string selectText = comboxJumpPageWebTcpGene.Text;
            currentPageWebTcpGene = int.Parse(selectText);
            if (currentPageWebTcpGene == 1)
            {
                btnLastPageWebTcpGene.Enabled = false;
                btnNextPageWebTcpGene.Enabled = true;
            }
            else if (currentPageWebTcpGene == pageNumWebTcpGene)
            {
                btnNextPageWebTcpGene.Enabled = false;
                btnLastPageWebTcpGene.Enabled = true;
            }
            else
            {
                btnLastPageWebTcpGene.Enabled = true;
                btnNextPageWebTcpGene.Enabled = true;
            }
            string packageFile = "dissectWebTcp.tmp";
            getPageRecordTcpGene(packageFile, currentPageWebTcpGene);    //获取当前页的记录
        }

        private bool getPageRecordTcpGene(string tcpGeneFile, int currentPageWebTcp)
        {
            if (File.Exists(tcpGeneFile))    //打开包文件进行偏移取数
            {
                try
                {
                    FileStream fsTcp = new FileStream(tcpGeneFile, FileMode.Open, FileAccess.Read);
                    StreamReader srTcp = new StreamReader(fsTcp, Encoding.Default);
                    //string strLine = srPacket.ReadLine();    //第一行是标题行，去掉
                    string strLine = null;
                    if (currentPageWebTcp == 1)
                    {
                        strLine = srTcp.ReadLine();     //第一页时读取第一行,这里要验证
                    }
                    int count = 0;
                    while (count < (currentPageWebTcp - 1) * PACKETPAGESIZEWEB)
                    {
                        strLine = srTcp.ReadLine();     //相当于偏移掉前面(currentPageWeb-1) * PACKETPAGESIZEWEB,第1页从0条开始取，第2页从第2000条开始取
                        count++;
                    }
                    //清除上一次的列表
                    LVTCPGeneralWeb.Items.Clear();
                    //开始取数,取往后的2000条
                    int tempSumInPage = 0;
                    //ListView数据项和子数据项
                    ListViewItem[] lvi;
                    ListViewItem.ListViewSubItem lvsi;
                    lvi = new ListViewItem[GROUPCOUNTWEB];     //200个为一组，批量刷新list
                    int lineCount = 0;
                    while (strLine != null && tempSumInPage < PACKETPAGESIZEWEB) //文件没有读取到记录或达到一页的展示量2000
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

                        if (lineCount % GROUPCOUNTWEB == 0)              //每200条记录刷新一次
                        {
                            LVTCPGeneralWeb.BeginUpdate();       //控制刷新
                            //加入ListView
                            LVTCPGeneralWeb.Items.AddRange(lvi);
                            LVTCPGeneralWeb.EndUpdate();

                            lvi = new ListViewItem[GROUPCOUNTWEB];
                            lineCount = 0;
                        }
                    }

                    LVTCPGeneralWeb.BeginUpdate();     //当没有达到50条时没有刷新所以在这里要刷新
                    //加入最后一批
                    for (int i = 0; i < lineCount; i++)
                    {
                        LVTCPGeneralWeb.Items.Add(lvi[i]);
                    }
                    LVTCPGeneralWeb.EndUpdate();
                    srTcp.Close();
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
