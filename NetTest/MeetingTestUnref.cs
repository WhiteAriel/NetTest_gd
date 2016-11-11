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
using System.Reflection;
using System.IO.Pipes;
using System.Drawing.Drawing2D;
using Dundas.Charting.WinControl;

namespace NetTest
{
    public partial class MeetingTestUnref : DevExpress.XtraEditors.XtraUserControl
    {
        /// <summary>
        /// 无参考评分函数
        /// </summary>
        /// <param name="strlog">无参考检测结果</param>
        /// <param name="key_hsi">哪些物理参数参与评分0x001:清晰度0x010:亮度色度饱和度对比度标准差0x100熵</param>
        /// <param name="result">评分结果保存文件</param>
        /// <param name="key_ab">哪些缺陷参数参与评分只能为0x1111</param>
        /// <param name="width">视频帧宽度</param>
        /// <param name="height">视频帧高度</param>
        /// <param name="NeedSlide">曲线拟合模式下是否根据高频分量调整块效应评分</param>
        /// <param name="Mode">评分模式：0曲线拟合1神经网络</param>
        /// <returns></returns>
        [DllImport("VideoScore.dll")]
        public static extern double vScore(string strlog, int key_hsi, string result, int key_ab, int width, int height, bool NeedSlide, int Mode);
    
        //IniFile inis = new IniFile(Application.StartupPath + "\\SipTerminal\\desktop.ini");
        IniFile iniDesktop = new IniFile(Application.StartupPath + "\\desktop.ini");
        IniFile iniAnalyze = new IniFile(Application.StartupPath + "\\analysis.ini");

        public bool DoTest;
        private bool show;                                         //显示播放模块状态

        private int testTime;                                       //测试时间长度

        private string strfcap;                                     //网络抓包文件
        private string strfqoe;                                     //用户QOE记录文件
        private string strfscore;                                   //用户QOE评分文件
        private string rTxt;                                        //结果txt
        private string rXlsx;                                       //结果xlsx
        private ArrayList datalist;                                 //保存的结果


        private PacketCap mPcap;                                    //网络抓包模块

        private DateTime dtStart;
        private DateTime dtStop; 
        public DateTime dtLogin;
        public DateTime dtConnect;

        private MeetingPlayer mPlayer;
        private QoeDetector mQoeDetector;

        private RtChart mChartDefi;
        private RtChart mChartLight;
        private RtChart mChartChrom;
        private RtChart mChartSatu;
        private RtChart mChartDistra;

        private StringBuilder strbFile = new StringBuilder();


        private QoePipe mQoePipe;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public MeetingTestUnref()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public void Init()
        {
            this.testTime = int.Parse(iniDesktop.IniReadValue("system", "Testtime"));

            int idevice = int.Parse(iniDesktop.IniReadValue("system", "Adapter"));
            this.mPcap = new PacketCap(idevice);           
            
            this.mPlayer = new MeetingPlayer();           

            this.mChartDefi = new RtChart(this.chart1);
            this.mChartLight = new RtChart(this.chart2);
            this.mChartChrom = new RtChart(this.chart3);
            this.mChartSatu = new RtChart(this.chart4);
            this.mChartDistra = new RtChart(this.chart5);

            this.mQoeDetector = new QoeDetector();

            this.mQoePipe = new QoePipe(this.mChartDefi, this.mChartLight, this.mChartChrom, this.mChartSatu,
                this.mChartDistra, this.gaugeContainer1, this.gaugeContainer2, this.gaugeContainer3);
        }  

        /// <summary>
        /// 启动测试
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnMeetingStart_Click(object sender, EventArgs e)
        {
            if (this.strbFile.Length > 0)
                this.strbFile.Remove(0, this.strbFile.Length);

            this.btnMeetingStart.Enabled = false;
            this.btnMeetingStop.Enabled = true;

            string strFile = iniDesktop.IniReadValue("system", "Path") + "\\Meeting" + "-" + iniDesktop.IniReadValue("system", "Testmode") + "-" + DateTime.Now.Year.ToString() + "-"
                + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString() + "-"
                + DateTime.Now.Hour.ToString() + "-" + DateTime.Now.Minute.ToString() + "-"
                + DateTime.Now.Second.ToString();

            string tmpPath = Application.StartupPath + "\\tmpAnalysis";

            this.strfqoe = tmpPath + "\\NewQoe.txt";
            this.strfscore = tmpPath + "\\NewScore.txt";
            this.rTxt = tmpPath + "\\NewResult.txt";
            this.datalist = new ArrayList();

            //this.strfqoe = strFile + "_unref_qoe.txt";
            //this.strfscore = strFile + "_log.txt";
            this.strfcap = strFile + ".pcap";
            this.rXlsx = strFile + ".xlsx";

            //分析模块需要的参数
            iniAnalyze.IniWriteValue("general", "filename", strFile);

            iniAnalyze.IniWriteValue("input", "qoefile", this.strfqoe);
            iniAnalyze.IniWriteValue("input", "capfile", this.strfcap);
            iniAnalyze.IniWriteValue("input", "qoeResult", this.rTxt);

            iniAnalyze.IniWriteValue("output","Result",this.rXlsx);

            iniAnalyze.IniWriteValue("rtpdissect", "ip", this.mPcap.mIP);  
          

            this.InitCharts();
            this.ClearDns();

            this.MeetingTesting();
        }

        /// <summary>
        /// 测试进行
        /// </summary>
        private void MeetingTesting()
        {
            this.DoTest = true;

            //开启抓包
            this.mPcap.Start(this.strfcap, 1000);            

            //启动播放模块
            IntPtr hwndParent = this.splitContainerControl1.Panel1.Handle;
            int cx = this.splitContainerControl1.Panel1.Width;
            int cy = this.splitContainerControl1.Panel1.Height;

            this.dtStart = DateTime.Now;
            this.lbMessage.Items.Add(this.dtStart.ToString() + "\t测试启动");
            this.datalist.Add("测试启动时刻:\t" + this.dtStart.ToLongTimeString());
            this.show = true;

            this.mPlayer.Start(hwndParent, -15, -40, cx+30, cy+60);
            //this.mPlayer.Start(hwndParent, 0, 0, cx, cy);
        }

        /// <summary>
        /// 中断测试
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnMeetingStop_Click(object sender, EventArgs e)
        {
            this.dtStop = DateTime.Now;
            this.lbMessage.Items.Add(this.dtStop.ToString() + "\t测试中止");
            this.datalist.Add("测试中止时刻:\t" + this.dtStop.ToLongTimeString());

            this.TestOver();
        }

        public double ScoreVideo()
        {
            double score=0;

            IniFile iniunref = new IniFile(Application.StartupPath + "\\UnrefTool\\VideoSet.ini");
            string strfIn = this.strfqoe;
            string strfOut = this.strfscore;
            int width=int.Parse(iniunref.IniReadValue("interface","width"));
            int height=int.Parse(iniunref.IniReadValue("interface","height"));
            int keyHsi = int.Parse(iniunref.IniReadValue("interface", "keys"));
            int keyAbnorm = int.Parse(iniunref.IniReadValue("interface", "keyAb"));
            int mode = int.Parse(iniunref.IniReadValue("interface", "assessmode"));
            bool slideblock = false;
            if (iniunref.IniReadValue("interface", "assessmode").Equals("true") || iniunref.IniReadValue("interface", "assessmode").Equals("TRUE"))
                slideblock = true;
            try
            {
                score = vScore(strfIn, keyHsi, strfOut, keyAbnorm, width, height, slideblock, mode);
            }
            catch (Exception ex)
            {
                return -1;
            }

            return score;
        }

        public void TestOver()
        {
            this.DoTest = false;
            this.btnMeetingStart.Enabled = true;
            this.btnMeetingStop.Enabled = false;

            this.timTest.Stop();
            this.timTest.Enabled = false;

            //停止实时播放过程、分析过程及抓包
            this.mPlayer.Close();
            this.mPcap.Stop();

            this.mQoePipe.Close();
            Thread.Sleep(300);
            this.mQoeDetector.Stop();

            Thread.Sleep(300);
            //无参考评分
            double score = this.ScoreVideo();
            this.gaugeContainer4.Values["Default"].Value = score*10;
            this.gaugeContainer4.Visible = true;
                        
            //生成结果文件
            ExcelProcess proExcel = new ExcelProcess();
            proExcel.Txt2Alist(this.strfscore,ref datalist);
            proExcel.Alist2Xlsx(datalist, this.rXlsx);          //生成xlsx文件

            //生成临时的结果文件,供分析模块调用
            FileStream fs = new FileStream(this.rTxt, FileMode.Create, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs,Encoding.Default);
            foreach (string str in datalist)
                sw.WriteLine(str);
            sw.Close();
            fs.Close();
        }

        /// <summary>
        /// 达到测试时长，结束测试。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timTest_Tick(object sender, EventArgs e)
        {
            this.dtStop = DateTime.Now;
            this.lbMessage.Items.Add(this.dtStop.ToString() + "\t测试结束");
            this.datalist.Add("测试结束时刻:\t" + this.dtStop.ToLongTimeString());

            this.TestOver();
        }

        /// <summary>
        /// 根据播放模块的状态，控制测试流程
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void StatusChange(object sender, StatusEventArgs e)
        {
            bool isplaying=false;

            if(e.StatusFlag.Equals(iniDesktop.IniReadValue("status", "registersuccess")))
            {
                this.dtLogin = DateTime.Now;
                if (this.show)
                {
                    this.lbMessage.Items.Add(this.dtLogin.ToString() + "\t注册成功");
                    this.datalist.Add("注册成功时刻:\t" + this.dtLogin.ToLongTimeString());
                }
            }
            else if(e.StatusFlag.Equals(iniDesktop.IniReadValue("status", "registerfail")))
            {
                this.dtLogin = DateTime.Now;
                if (this.show)
                {
                    this.lbMessage.Items.Add(this.dtLogin.ToString() + "\t注册失败，请重试！");
                    this.datalist.Add("注册失败时刻:\t" + this.dtLogin.ToLongTimeString());
                }
            }
            else if (e.StatusFlag.Equals(iniDesktop.IniReadValue("status", "connectsuccess")))
            {
                if (isplaying)
                    return;

                this.dtConnect = DateTime.Now;
                if (this.show)
                {
                    this.lbMessage.Items.Add(this.dtConnect.ToString() + "\t呼叫成功");
                    this.datalist.Add("呼叫成功时刻:\t" + this.dtConnect.ToLongTimeString());
                }
                this.show = false;

                isplaying = true;
            }
            else if (e.StatusFlag.Equals(iniDesktop.IniReadValue("status", "connectfail")))
            {
                this.dtConnect = DateTime.Now;

                if (this.show)
                {
                    this.lbMessage.Items.Add(this.dtConnect.ToString() + "\t呼叫失败！");
                    this.datalist.Add("呼叫失败时刻:\t" + this.dtConnect.ToLongTimeString());

                }
                this.show = false;

                isplaying = false;
            }

            if (isplaying)
            {
                //开启实时分析
                this.mQoePipe.Open();
                Thread.Sleep(1000);
                this.mQoeDetector.Start(this.strfqoe);

                //
                this.timTest.Interval = this.testTime*1000;
                this.timTest.Enabled = true;
                this.timTest.Start();
            }            
        }

        /// <summary>
        /// 初始化曲线图
        /// </summary>
        private void InitCharts()
        {
            this.mChartDefi.ResetmChart();
            this.mChartLight.ResetmChart();
            this.mChartChrom.ResetmChart();
            this.mChartSatu.ResetmChart();
            this.mChartDistra.ResetmChart();

            this.gaugeContainer4.Visible = false;
        }

        /// <summary>
        /// 清理DNS
        /// </summary>
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

    }
}
