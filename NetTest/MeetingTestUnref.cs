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
        /// �޲ο����ֺ���
        /// </summary>
        /// <param name="strlog">�޲ο������</param>
        /// <param name="key_hsi">��Щ���������������0x001:������0x010:����ɫ�ȱ��ͶȶԱȶȱ�׼��0x100��</param>
        /// <param name="result">���ֽ�������ļ�</param>
        /// <param name="key_ab">��Щȱ�ݲ�����������ֻ��Ϊ0x1111</param>
        /// <param name="width">��Ƶ֡���</param>
        /// <param name="height">��Ƶ֡�߶�</param>
        /// <param name="NeedSlide">�������ģʽ���Ƿ���ݸ�Ƶ����������ЧӦ����</param>
        /// <param name="Mode">����ģʽ��0�������1������</param>
        /// <returns></returns>
        [DllImport("VideoScore.dll")]
        public static extern double vScore(string strlog, int key_hsi, string result, int key_ab, int width, int height, bool NeedSlide, int Mode);
    
        //IniFile inis = new IniFile(Application.StartupPath + "\\SipTerminal\\desktop.ini");
        IniFile iniDesktop = new IniFile(Application.StartupPath + "\\desktop.ini");
        IniFile iniAnalyze = new IniFile(Application.StartupPath + "\\analysis.ini");

        public bool DoTest;
        private bool show;                                         //��ʾ����ģ��״̬

        private int testTime;                                       //����ʱ�䳤��

        private string strfcap;                                     //����ץ���ļ�
        private string strfqoe;                                     //�û�QOE��¼�ļ�
        private string strfscore;                                   //�û�QOE�����ļ�
        private string rTxt;                                        //���txt
        private string rXlsx;                                       //���xlsx
        private ArrayList datalist;                                 //����Ľ��


        private PacketCap mPcap;                                    //����ץ��ģ��

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
        /// ���캯��
        /// </summary>
        public MeetingTestUnref()
        {
            InitializeComponent();
        }

        /// <summary>
        /// ��ʼ��
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
        /// ��������
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

            //����ģ����Ҫ�Ĳ���
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
        /// ���Խ���
        /// </summary>
        private void MeetingTesting()
        {
            this.DoTest = true;

            //����ץ��
            this.mPcap.Start(this.strfcap, 1000);            

            //��������ģ��
            IntPtr hwndParent = this.splitContainerControl1.Panel1.Handle;
            int cx = this.splitContainerControl1.Panel1.Width;
            int cy = this.splitContainerControl1.Panel1.Height;

            this.dtStart = DateTime.Now;
            this.lbMessage.Items.Add(this.dtStart.ToString() + "\t��������");
            this.datalist.Add("��������ʱ��:\t" + this.dtStart.ToLongTimeString());
            this.show = true;

            this.mPlayer.Start(hwndParent, -15, -40, cx+30, cy+60);
            //this.mPlayer.Start(hwndParent, 0, 0, cx, cy);
        }

        /// <summary>
        /// �жϲ���
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnMeetingStop_Click(object sender, EventArgs e)
        {
            this.dtStop = DateTime.Now;
            this.lbMessage.Items.Add(this.dtStop.ToString() + "\t������ֹ");
            this.datalist.Add("������ֹʱ��:\t" + this.dtStop.ToLongTimeString());

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

            //ֹͣʵʱ���Ź��̡��������̼�ץ��
            this.mPlayer.Close();
            this.mPcap.Stop();

            this.mQoePipe.Close();
            Thread.Sleep(300);
            this.mQoeDetector.Stop();

            Thread.Sleep(300);
            //�޲ο�����
            double score = this.ScoreVideo();
            this.gaugeContainer4.Values["Default"].Value = score*10;
            this.gaugeContainer4.Visible = true;
                        
            //���ɽ���ļ�
            ExcelProcess proExcel = new ExcelProcess();
            proExcel.Txt2Alist(this.strfscore,ref datalist);
            proExcel.Alist2Xlsx(datalist, this.rXlsx);          //����xlsx�ļ�

            //������ʱ�Ľ���ļ�,������ģ�����
            FileStream fs = new FileStream(this.rTxt, FileMode.Create, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs,Encoding.Default);
            foreach (string str in datalist)
                sw.WriteLine(str);
            sw.Close();
            fs.Close();
        }

        /// <summary>
        /// �ﵽ����ʱ�����������ԡ�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timTest_Tick(object sender, EventArgs e)
        {
            this.dtStop = DateTime.Now;
            this.lbMessage.Items.Add(this.dtStop.ToString() + "\t���Խ���");
            this.datalist.Add("���Խ���ʱ��:\t" + this.dtStop.ToLongTimeString());

            this.TestOver();
        }

        /// <summary>
        /// ���ݲ���ģ���״̬�����Ʋ�������
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
                    this.lbMessage.Items.Add(this.dtLogin.ToString() + "\tע��ɹ�");
                    this.datalist.Add("ע��ɹ�ʱ��:\t" + this.dtLogin.ToLongTimeString());
                }
            }
            else if(e.StatusFlag.Equals(iniDesktop.IniReadValue("status", "registerfail")))
            {
                this.dtLogin = DateTime.Now;
                if (this.show)
                {
                    this.lbMessage.Items.Add(this.dtLogin.ToString() + "\tע��ʧ�ܣ������ԣ�");
                    this.datalist.Add("ע��ʧ��ʱ��:\t" + this.dtLogin.ToLongTimeString());
                }
            }
            else if (e.StatusFlag.Equals(iniDesktop.IniReadValue("status", "connectsuccess")))
            {
                if (isplaying)
                    return;

                this.dtConnect = DateTime.Now;
                if (this.show)
                {
                    this.lbMessage.Items.Add(this.dtConnect.ToString() + "\t���гɹ�");
                    this.datalist.Add("���гɹ�ʱ��:\t" + this.dtConnect.ToLongTimeString());
                }
                this.show = false;

                isplaying = true;
            }
            else if (e.StatusFlag.Equals(iniDesktop.IniReadValue("status", "connectfail")))
            {
                this.dtConnect = DateTime.Now;

                if (this.show)
                {
                    this.lbMessage.Items.Add(this.dtConnect.ToString() + "\t����ʧ�ܣ�");
                    this.datalist.Add("����ʧ��ʱ��:\t" + this.dtConnect.ToLongTimeString());

                }
                this.show = false;

                isplaying = false;
            }

            if (isplaying)
            {
                //����ʵʱ����
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
        /// ��ʼ������ͼ
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
        /// ����DNS
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
