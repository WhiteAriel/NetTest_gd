/******************************************************************************
 
 web��rtsp�����޸�
 * 1.�������--------�������Ӳ�����ɺ������������������ʾ������ϣ�Ҳ������excel��
 * 
 * 2.��������--------�������Զ�����Ӻ�һ���������������������ʾ��ֻ������excel��
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
        public static double AverHTTP = 0.0;    //Web��Ӧ��ʱ
        public static double AverRTSPRTT = 0.0; //Rtsp��Ӧ��ʱ
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

        const int PACKETPAGESIZE = 2000;      //���ݰ�չʾҳ��ÿ�μ���1000��
        const int GROUPCOUNT = 200;           //ÿ�����ݱ�ˢ��200����¼
        //���Դ���
        // int testNum = 0;
        //ȫ�ֱ������ж��Ƿ��������
        bool IsAnalysed = false;
        //ץ���ļ���
        string PcapFileName = null;
        //������־�ļ�
        string TxtFileName = null;
        //������ʱ
        //int AnalysingTimeCount = 0;
        //��¼ǰһ��ѡ��������
        int prevSelectIndex = 0;
        //��������ѡ�ĳ߶�ѡ��
        double[] timeScale = new double[] { 1.0, 0.1, 0.01 };
        //�������ݰ�����
        int totalPacketCnt = 0;
        //֡���ֲ��в�ͬ֡����Ӧ����
        int[] rangeCount = new int[11];
        //������Ϣ��¼
        string WrongReason = null;
        //���Ա���txt��ʽ
        public string strTxtResult = null;
        //Excel�������
        ExcelProcess processExcel = new ExcelProcess();
        //xls�ļ�
        string strXlsLogFile = null;
        //�ж�txtת��Ϊxsl
        bool iTxt2Xls = false;
        //f��������
        // int iStartAnalyze = 0;
        //������cap�ļ�����
        //string[] filesinpath = null;
        //�ж��Ƿ���ѡ������pcap�ļ�
        public static bool isSelectPcap = false;

        private bool analyzeOn = false;
        public bool serverTest = false;

        //���ݿ����
        private MySQLInterface mysqlWeb = null;
        private bool mysqlWebFlag = false;
        //private MySQLInterface mysqlWeb = new MySQLInterface(inis.IniReadValue("Mysql", "serverIp"), inis.IniReadValue("Mysql", "user"), inis.IniReadValue("Mysql", "passwd"), inis.IniReadValue("Mysql", "dbname"));
        //���ý����߳�
        Thread setParseTrd = null;

        //���ݰ���ҳ����
        int currentPage = 1;  //��ǰҳ��
        int totalNum = 0;    //�ܼ�¼������ʼ��Ϊ0
        int pageNum = 0;      //��ҳ��

        //tcp��������ҳ
        int currentPageTcpGene = 1;  //��ǰҳ��
        int totalNumTcpGene = 0;    //�ܼ�¼������ʼ��Ϊ0
        int pageNumTcpGene = 0;      //��ҳ��


        //��ǰ����id������
        string currentId;


        ArrayList datalist = ArrayList.Synchronized(new ArrayList());//���ݰ�arraylist������cap������

        //�������listview��BackgroundWorker����ֹ����������� 
        //private BackgroundWorker m_AsyncWorkerTcp = new BackgroundWorker();  //Tcp���棬��ʱ��֧��ֹͣ
        //private BackgroundWorker m_AsyncWorkerSave = new BackgroundWorker(); //�����ӳٽ���

        public void Init()
        {
            PacAnaly.SelectedTabPageIndex = 12;
            //lsvResult.Items.Clear();
            //���÷�������ж�ָʾ
            IsAnalysed = false;
            //��ץ���ļ����ÿ�
            PcapFileName = null;
            //��������־�ļ����ÿ�
            TxtFileName = null;
            //���ͼ��
            //this.InitChart();
            //this.InitListView();
            //����ϴμ����ƽ��ֵ
            //AverValue.InitValue();

            //��֡��������ʱ��߶ȱ�ʶ��
            int i = 0;
            object[] obj = new object[timeScale.Length];
            foreach (double s in timeScale)
            {
                obj[i++] = s.ToString() + "��";
            }
            ScaleComboBox.Items.Clear();
            //��ӳ߶�ѡ����Ͽ�
            ScaleComboBox.Items.AddRange(obj);
            //�߶�Ĭ��Ϊ1.0
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

            DelayMax.Text = "��ʱ���ֵ:";
            DelayMin.Text = "��ʱ��Сֵ:";
            DelayAvg.Text = "��ʱƽ��ֵ:";
            JitterMax.Text = "�������ֵ:";
            JitterMin.Text = "������Сֵ:";
            JitterAvg.Text = "����ƽ��ֵ:";

            InOutAvg.Text = "������ƽ��ֵ:";
            InOutMax.Text = "���������ֵ:";
            InOutMin.Text = "��������Сֵ:";
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
            //4���ֵĽ�������

            currentId = inis.IniReadValue("Task", "currentVideoId");
            try
            {
                WebInfoAnalys();         //web���ŵ�������ݽ���
                InOutFrameLenAnalys();    //������������֡���ֲ���Ϣ
                PcapTcpDnsHttpAnalys();  //�����ļ���Ҫ�����ݰ���TCP��DNS��HTTP����Ϣ
                ResultDisplay();          //���Ա�������ʾ
                //storeResult();
            }
            catch (System.Exception ex)
            {
                Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
            }
            if (WrongReason != null && !serverTest)
            {
                MessageBox.Show(WrongReason + "������ܵ�ԭ���У�\n 1��û����ص����ݰ� \n 2������ѡ����ȷ \n 3����������û�������Ƶ�ļ� \n 4��������֧�ֶ�Ӧ����Ƶ��ʽ \n");
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
                    //���Excel����
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
                    if (isSelectPcap == false)              //���û���ڷ���֮ǰѡ��pcap�ļ�ʱ��ȡ��һ�ε��ļ�
                    {
                        PcapFileName = inis.IniReadValue("Flv", "PcapFile");
                        TxtFileName = inis.IniReadValue("Flv", "PlayerFile");
                    }


                    if (!File.Exists(PcapFileName))
                    {
                        MessageBox.Show("�Ҳ������ݰ��ļ���" + PcapFileName);
                        Log.Warn("�Ҳ������ݰ��ļ�");
                        return;
                    }

                    IsAnalysed = true;    //�Ƿ�����˷���
                    WrongReason = null;   //��մ�����Ϣ
                    //���ͼ��
                    this.InitChart();
                    this.InitListView();
                    //����ϴμ����ƽ��ֵ
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
                    Thread.Sleep(1500);   //���ߵȴ�
            }

        }


        public void StartTerminalAnalyzeFunc()
        {
                    //���Excel����
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
                    if (isSelectPcap == false)              //���û���ڷ���֮ǰѡ��pcap�ļ�ʱ��ȡ��һ�ε��ļ�
                    {
                        PcapFileName = inis.IniReadValue("Flv", "PcapFile");
                        TxtFileName = inis.IniReadValue("Flv", "PlayerFile");
                    }


                    if (!File.Exists(PcapFileName))
                    {
                        MessageBox.Show("�Ҳ������ݰ��ļ���" + PcapFileName);
                        Log.Warn("�Ҳ������ݰ��ļ�");
                        return;
                    }

                    IsAnalysed = true;    //�Ƿ�����˷���
                    WrongReason = null;   //��մ�����Ϣ
                    //���ͼ��
                    this.InitChart();
                    this.InitListView();
                    //����ϴμ����ƽ��ֵ
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

        //������Ա���
        private void storeResult()
        {
            //�����ͼ
            {
                //���ݽ��������ݰ���ָ������ͼƬ�ļ�·��
                string SavedPicPath = PcapFileName;
                //ָ������ͼƬ�ļ�·��
                SavedPicPath = PcapFileName;
                SavedPicPath = SavedPicPath.Remove(SavedPicPath.Length - 5);
                if (!Directory.Exists(SavedPicPath))
                    Directory.CreateDirectory(SavedPicPath);
                //�ȶ�ͼƬ����Ŀ¼�������
                string[] FileInPath = Directory.GetFiles(SavedPicPath);
                foreach (string str in FileInPath)
                    File.Delete(str);
                //ָ������ͼƬ�ļ���ʽ
                ChartImageFormat format = ChartImageFormat.Jpeg;
                //����ͼƬ
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


            // txtתexcel�ļ�
            //strXlsLogFile = inis.IniReadValue("Flv", "LogFile");
            iTxt2Xls = processExcel.txt2Xlsx(strTxtResult, strXlsLogFile);
            if (!iTxt2Xls)
            {
                MessageBox.Show("��־�ļ�" + strXlsLogFile + "����ʧ�ܣ�\n ���鱾���Ƿ�װ��Office�����");
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
                           ��Pcap���������õ�Web�������Ϣ
         * *********************************************************************/
        [DllImport("VSMFlv.dll")]
        public extern static int VideoStreamMediaFlv(string strPcapFileName);
        private void WebInfoAnalys()
        {
            int AnalysOK = 0;
            //����ϴη����������ʱ�ļ�
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
                WrongReason += " ���ݰ����䲻�ǲ���httpЭ�鴫�䣬web�����޷���� \n";
                return;
            }
            else if (AnalysOK == -7)
            {
                //����������ڲ������
                IsAnalysed = true;
                if (File.Exists("FlvRestoreFailed.txt")) File.Delete("FlvRestoreFailed.txt");
                WrongReason += "��������йؼ����ݰ���ʧ��web�����޷���� \n";
                return;
            }
            else if (AnalysOK == -8)
            {
                IsAnalysed = true;
                if (File.Exists("NoFlvDetected.txt")) File.Delete("NoFlvDetected.txt");
                WrongReason += "�����ļ�����flv/f4v/hlv��ʽ��web�����޷���� \n";
                return;
            }
            else if (!((File.Exists("flv_tag.txt")) && (File.Exists("data_flow_smoothed.txt")) && (File.Exists("data_flow_unsmoothed.txt")) && (File.Exists("play_flow_unsmoothed.txt")) && (File.Exists("play_flow_smoothed.txt")) && (File.Exists("user_event.txt"))))
            {
                //�����������Ҳ�������
                IsAnalysed = true;
                WrongReason += "web�����쳣���õ���������Ϣ������ \n";
                return;
            }

            //���ݵ���VideoStreamMediaFlv(string strPcapFileName)���ɵ�7��txt�ļ���һ��������д�뵽LV�У����߻�ͼ

            //rdAnaly("data_flow_unsmoothed.txt", this.LVDataFlow);
            //rdAnaly("play_flow_unsmoothed.txt", this.LVPlayFlow);
            //rdAnaly("flv_tag.txt", this.LVFlvTag);
            rdAnaly("data_flow_smoothed.txt");
            rdAnaly("play_flow_smoothed.txt");
            rdAnaly("flv_tag.txt");

            //��������������Ƶ������ֵ�;�ֵ
            FileStream fs1 = new FileStream("data_flow_smoothed.txt", FileMode.Open, FileAccess.Read);
            FileStream fs2 = new FileStream("play_flow_smoothed.txt", FileMode.Open, FileAccess.Read);

            StreamReader srNet = new StreamReader(fs1, Encoding.Default);//������
            StreamReader srVideo = new StreamReader(fs2, Encoding.Default);//��Ƶ��
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
            while (str != null && (!str.Equals("����:֮������ݴ��ͳ��ִ����޷��������в�������ͳ��")))
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
            while (str1 != null && (!str1.Equals("����:֮������ݴ��ͳ��ִ����޷��������в�������ͳ��")))
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

            //��ʾ��ֵ�;�ֵ
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

            //��������������
            double maxY, minY;
            double maxX, minX;

            //ȷ��ChartDataPlayFlowͼ��һ���ֵĺ�����

            maxY = this.ChartAccumulatedTraffic.Series["�ۻ�����������"].Points.FindMaxValue("Y1").YValues[0] > this.ChartAccumulatedTraffic.Series["�ۻ���Ƶ������"].Points.FindMaxValue("Y1").YValues[0] ? this.ChartAccumulatedTraffic.Series["�ۻ�����������"].Points.FindMaxValue("Y1").YValues[0] : this.ChartAccumulatedTraffic.Series["�ۻ���Ƶ������"].Points.FindMaxValue("Y1").YValues[0];
            this.ChartAccumulatedTraffic.ChartAreas[0].AxisY.Maximum = ((int)(maxY / 100) + 1) * 100;
            minY = this.ChartAccumulatedTraffic.Series["�ۻ�����������"].Points.FindMinValue("Y1").YValues[0] > this.ChartAccumulatedTraffic.Series["�ۻ���Ƶ������"].Points.FindMinValue("Y1").YValues[0] ? this.ChartAccumulatedTraffic.Series["�ۻ�����������"].Points.FindMinValue("Y1").YValues[0] : this.ChartAccumulatedTraffic.Series["�ۻ���Ƶ������"].Points.FindMinValue("Y1").YValues[0];
            this.ChartAccumulatedTraffic.ChartAreas[0].AxisY.Minimum = ((int)(minY / 100) + 1) * 100;
            maxX = this.ChartAccumulatedTraffic.Series["�ۻ�����������"].Points.FindMaxValue("X").XValue > this.ChartAccumulatedTraffic.Series["�ۻ���Ƶ������"].Points.FindMaxValue("X").XValue ? this.ChartAccumulatedTraffic.Series["�ۻ�����������"].Points.FindMaxValue("X").XValue : this.ChartAccumulatedTraffic.Series["�ۻ���Ƶ������"].Points.FindMaxValue("X").XValue;
            this.ChartAccumulatedTraffic.ChartAreas[0].AxisX.Maximum = maxX + 5;
            minX = this.ChartAccumulatedTraffic.Series["�ۻ�����������"].Points.FindMinValue("X").XValue > this.ChartAccumulatedTraffic.Series["�ۻ���Ƶ������"].Points.FindMinValue("X").XValue ? this.ChartAccumulatedTraffic.Series["�ۻ�����������"].Points.FindMinValue("X").XValue : this.ChartAccumulatedTraffic.Series["�ۻ���Ƶ������"].Points.FindMinValue("X").XValue;
            this.ChartAccumulatedTraffic.ChartAreas[0].AxisX.Minimum = minX + 5;

            //ȷ��ChartDataPlayFlowͼ�ڶ����ֵĺ�����

            maxY = this.ChartRealTraffic.Series["ʵʱ����������"].Points.FindMaxValue("Y1").YValues[0] > this.ChartRealTraffic.Series["ʵʱ��Ƶ������"].Points.FindMaxValue("Y1").YValues[0] ? this.ChartRealTraffic.Series["ʵʱ����������"].Points.FindMaxValue("Y1").YValues[0] : this.ChartRealTraffic.Series["ʵʱ��Ƶ������"].Points.FindMaxValue("Y1").YValues[0];
            this.ChartRealTraffic.ChartAreas[0].AxisY.Maximum = ((int)(maxY / 100) + 1) * 100;
            minY = this.ChartRealTraffic.Series["ʵʱ����������"].Points.FindMinValue("Y1").YValues[0] > this.ChartRealTraffic.Series["ʵʱ��Ƶ������"].Points.FindMinValue("Y1").YValues[0] ? this.ChartRealTraffic.Series["ʵʱ����������"].Points.FindMinValue("Y1").YValues[0] : this.ChartRealTraffic.Series["ʵʱ��Ƶ������"].Points.FindMinValue("Y1").YValues[0];
            this.ChartRealTraffic.ChartAreas[0].AxisY.Minimum = ((int)(minY / 100) + 1) * 100;
            maxX = this.ChartRealTraffic.Series["ʵʱ����������"].Points.FindMaxValue("X").XValue > this.ChartRealTraffic.Series["ʵʱ��Ƶ������"].Points.FindMaxValue("X").XValue ? this.ChartRealTraffic.Series["ʵʱ����������"].Points.FindMaxValue("X").XValue : this.ChartRealTraffic.Series["ʵʱ��Ƶ������"].Points.FindMaxValue("X").XValue;
            this.ChartRealTraffic.ChartAreas[0].AxisX.Maximum = maxX + 5;
            minX = this.ChartRealTraffic.Series["ʵʱ����������"].Points.FindMinValue("X").XValue > this.ChartRealTraffic.Series["ʵʱ��Ƶ������"].Points.FindMinValue("X").XValue ? this.ChartRealTraffic.Series["ʵʱ����������"].Points.FindMinValue("X").XValue : this.ChartRealTraffic.Series["ʵʱ��Ƶ������"].Points.FindMinValue("X").XValue;
            this.ChartRealTraffic.ChartAreas[0].AxisX.Minimum = minX + 5;


            //ȷ��ChartVideoFrameSeqͼ�ĺ�����
            maxY = minY = maxX = minX = 0;
            foreach (Series iSeries in this.ChartSequence.Series)
            {
                //������ϵ����жϣ���Ȼ���ܻ���֡�δ��Ӧ�õĶ��󸳸�---��
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

            //ʹ�ܻ�ͼ����Ĺ�����
            this.EnableScroll(ChartAccumulatedTraffic.ChartAreas[0]);
            this.EnableScroll(ChartRealTraffic.ChartAreas[0]);
            this.EnableScroll(ChartSequence.ChartAreas[0]);

            //ͼ���ع�
            this.ChartAccumulatedTraffic.Invalidate();
            this.ChartRealTraffic.Invalidate();
            this.ChartSequence.Invalidate();

            return;
        }

        private void rdAnaly(string strfile)
        {
            if (!File.Exists(strfile))
            {
                //Ϊ���ó����������Ѻã��ѵ�����Messageboxȥ���ˣ�
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
                    if (sr.Peek() < 0) break;  //�˳������һ��
                }

                if (strfile.Equals("data_flow_smoothed.txt"))
                {
                    this.ChartAccumulatedTraffic.Invoke(addDataDel, this.ChartAccumulatedTraffic, this.ChartAccumulatedTraffic.Series["�ۻ�����������"], Convert.ToDouble(ld[0]), Convert.ToDouble(ld[2]) / 1000);
                    this.ChartRealTraffic.Invoke(addDataDel, this.ChartRealTraffic, this.ChartRealTraffic.Series["ʵʱ����������"], Convert.ToDouble(ld[0]), Convert.ToDouble(ld[1]) / 1000);
                }
                else if (strfile.Equals("play_flow_smoothed.txt"))
                {
                    this.ChartAccumulatedTraffic.Invoke(addDataDel, this.ChartAccumulatedTraffic, this.ChartAccumulatedTraffic.Series["�ۻ���Ƶ������"], Convert.ToDouble(ld[0]), Convert.ToDouble(ld[2]) / 1000);
                    this.ChartRealTraffic.Invoke(addDataDel, this.ChartRealTraffic, this.ChartRealTraffic.Series["ʵʱ��Ƶ������"], Convert.ToDouble(ld[0]), Convert.ToDouble(ld[1]) / 1000);
                }
                else if (strfile.Equals("flv_tag.txt"))
                {
                    double x = Convert.ToDouble(ld[0]);

                    if (ld[3].Equals("I��Ƶ֡"))
                    {
                        this.ChartSequence.Series[0].Points.AddXY(x, Convert.ToDouble(ld[4]));
                        this.ChartSequence.Series[2].Points.AddXY(x, Convert.ToDouble(ld[5]));
                    }
                    else if (ld[3].Equals("P��Ƶ֡"))
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
                //Ϊ���ó����������Ѻã��ѵ�����Messageboxȥ���ˣ�
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
                    if (sr.Peek() < 0) break;  //�˳������һ��
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

        //������ϴη������ɵ�txt�ļ�,��Щ�ļ�����WebInfoAnalys
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

        //�����һ�ε�chart��ͼ
        private void InitWebChart()
        {
            this.ChartAccumulatedTraffic.Invoke(clearDataDel, this.ChartAccumulatedTraffic);
            this.ChartRealTraffic.Invoke(clearDataDel, this.ChartRealTraffic);
            this.ChartSequence.Invoke(clearDataDel, this.ChartSequence);
        }

        /***************************************************************
              ��Pcap���������õ��ļ���Ҫ�����ݰ���TCP��DNS��HTTP����Ϣ
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
                //���ݰ��򿪴���ֱ���˳���                
                try
                {
                    pcap_file_close_inCS();
                }
                catch (System.Exception ex)
                {
                    Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
                }
                WrongReason = "���ݰ��򿪴����޷������ļ���Ҫ�����ݰ���TCP��DNS��HTTP�ķ��� \n";
                return;
            }
            else
            {
                if (!ShowLVSum()) WrongReason += "�ļ���Ҫ�����쳣 \n";
                if (!ShowLVDNSAnalys()) WrongReason += "DNS�����쳣 \n";
                if (!ShowLVHTTPAnalys()) WrongReason += "HTTP�����쳣 \n";
                try
                {
                    DelayJitterAnalys();
                }
                catch
                {
                    WrongReason += "�޷������ʱ������Ϣ\n";
                }
                if (!ShowTCPStream()) WrongReason += "TCP�������쳣 \n";
                if (!ShowLVPacketAnalys()) WrongReason += "���ݰ������쳣 \n";
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

        //��Pcap�ļ�(�����ļ���Ҫ�����ݰ�������TCP������DNS������HTTP����)
        [DllImport("NetpryDll.dll")]
        public extern static int pcap_file_dissect_inCS(string pathfilename);

        //�ر�Pcap�ļ�(�����ļ���Ҫ�����ݰ�������TCP������DNS������HTTP����)
        [DllImport("NetpryDll.dll")]
        public extern static void pcap_file_close_inCS();

        //���ļ���Ҫ�б�������ݺ���
        [DllImport("NetpryDll.dll")]
        //public extern static int pf_summary_tostr(string tmpfileName);
        public extern static int pf_summary_tofile(string tmpfilename);
        private bool ShowLVSum()
        {
            //���ԭ�е���
            LVSum.Items.Clear();

            //��ʱ�ļ�����
            string tmpfileName = "dissectSum.tmp";

            //������ʱ�ļ�
            FileStream fs = File.Create(tmpfileName);
            fs.Close();

            //�����ļ���Ҫ��Ϣ��ȡ����
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

            //�����ļ�����
            StreamReader sr = new StreamReader(tmpfileName, Encoding.Default);
            string strLine = sr.ReadLine();

            //�����ַ���
            string[] propoties = new string[]{"�ļ���", "�ļ�����(�ֽ�)", "��·����",
                "��һ�����ݰ�����ʱ��", "���һ�����ݰ�����ʱ��", "����ʱ��(��)", 
                "���ݰ��ܸ���", "����������(�ֽ�)", "��������IP��ַ", "��������MAC��ַ"};
            int propo_index = 0;

            //ListView���������������
            ListViewItem lvi;
            ListViewItem.ListViewSubItem lvsi;

            //��ȡÿһ������
            while (strLine != null)
            {
                lvi = new ListViewItem();
                lvi.Text = propoties[propo_index++];

                lvsi = new ListViewItem.ListViewSubItem();
                lvsi.Text = strLine;
                lvi.SubItems.Add(lvsi);

                //����ListView
                ListView.CheckForIllegalCrossThreadCalls = false;
                LVSum.Items.Add(lvi);

                strLine = sr.ReadLine();
            }
            sr.Close();

            //ɾ����ʱ�ļ�
            File.Delete(tmpfileName);
            return true;
        }

        //�����ݰ��б�������ݺ���
        [DllImport("NetpryDll.dll")]
        public extern static int pcb_list_tofile(string tmpfileName);
        [DllImport("NetpryDll.dll")]
        public extern static int getPacketNum();

        private bool ShowLVPacketAnalys()
        {

            //���ԭ�е���
            LVPacketAnalys.Items.Clear();

            //��ʱ�ļ�����
            string tmpfileName = "dissectPacket.tmp";

            //ɾ����һ�ε���ʱ�ļ���������ģ�鲻ͬ����¼̫��Ҫ��ҳ��
            if (File.Exists(tmpfileName))
            {
                File.Delete(tmpfileName);
            }

            //������ʱ�ļ�
            FileStream fs = File.Create(tmpfileName);
            fs.Close();

            //�������ݰ�������Ϣ��ȡ����
            int retCode = -1;
            try
            {
                retCode = pcb_list_tofile(tmpfileName);             //���ڴ��еİ�����д���ļ���
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


            currentPage = 1;    //ÿ�η�������ҳ���ʼ��Ϊ1
            //����Ҫ�ýӿڻ�ȡ������
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
            //��ʼ��ʱ��һҳ�����ã���һҳҪ�ж���ҳ����ÿһҳ�ļ�¼��2000�Ĵ�С
            if (pageNum > 1)
            {
                btnNextPage.Enabled = true;
            }

            btnJump.Enabled = true;     //ʹ����ת������Ӧ�������ж����뷶Χ

            labelTotal.Text = "�ܼ�¼��:" + totalNum + " ��ҳ��:" + pageNum;
            labelTotal.Enabled = true;

            comboxJumpPage.Items.Clear();
            comboxJumpPage.Enabled = true;    //��ʼѡ��combox
            for (int i = 0; i < pageNum; i++)
            {
                comboxJumpPage.Items.Add(i + 1);
            }
            comboxJumpPage.SelectedIndex = 0;

            getPageRecord(tmpfileName, currentPage);

            //ɾ����ʱ�ļ�(�޸ĺ�ɾ�������ڷ�ҳ)
            // File.Delete(tmpfileName);
            return true;

        }

        //��TCP���б��TCP���쳣�б�������ݺ���
        //TCP����Ϣ��ȡ����
        [DllImport("NetpryDll.dll")]
        public extern static int tcp_stream_tofile(string tmpfileName);
        //TCP���쳣��Ϣ��ȡ����
        [DllImport("NetpryDll.dll")]
        public extern static int tcps_exception_tofile(string tmpfileName);
        [DllImport("NetpryDll.dll")]
        public extern static int getTcpStreamNum();
        private bool ShowTCPStream()
        {


            /**********���TCP�����б�**********/
            //���ԭ�е���
            LVTCPGeneral.Items.Clear();

            //��ʱ�ļ�����
            string tmpfileName = "dissectTcp.tmp";

            //ɾ����һ�ε���ʱ�ļ���������ģ�鲻ͬ����¼̫��Ҫ��ҳ��
            if (File.Exists(tmpfileName))
            {
                File.Delete(tmpfileName);
            }

            //������ʱ�ļ�
            FileStream fs = File.Create(tmpfileName);
            fs.Close();

            //����TCP����Ϣ��ȡ����             
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

            currentPageTcpGene = 1;    //ÿ�η�������ҳ���ʼ��Ϊ1
            //����Ҫ�ýӿڻ�ȡ������
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
            //��ʼ��ʱ��һҳ�����ã���һҳҪ�ж���ҳ����ÿһҳ�ļ�¼��2000�Ĵ�С
            if (pageNumTcpGene > 1)
            {
                btnNextPageTcpGene.Enabled = true;
            }

            btnJumpTcpGene.Enabled = true;     //ʹ����ת������Ӧ�������ж����뷶Χ

            labelTotalTcpGene.Text = "�ܼ�¼��:" + totalNumTcpGene + "��ҳ��:" + pageNumTcpGene;
            labelTotalTcpGene.Enabled = true;

            comboxJumpPageTcpGene.Items.Clear();
            comboxJumpPageTcpGene.Enabled = true;    //��ʼѡ��combox
            for (int i = 0; i < pageNumTcpGene; i++)
            {
                comboxJumpPageTcpGene.Items.Add(i + 1);
            }
            comboxJumpPageTcpGene.SelectedIndex = 0;

            getPageRecordTcpGene(tmpfileName, currentPageTcpGene);   //��ȡǰ��2000����¼

            //�����ļ�����
            StreamReader sr = new StreamReader(tmpfileName);
            string strLine = sr.ReadLine();

            //��¼tcp���ӵĸ��ֲ���ֵ
            int tcpconnecttimes = 0;
            int tcpuptimes = 0;
            int tcpdowntimes = 0;
            int tcpupflow = 0;
            int tcpdownflow = 0;
            double averrtt = 0.0;

            //��ȡÿһ������
            while (strLine != null)
            {
                //�õ�ÿһ��Ԫ����
                string[] str = strLine.Split(new Char[] { '\t' }, 12);

                //��¼TCP���ӵĲ���ֵ
                tcpconnecttimes++;
                tcpuptimes += int.Parse(str[5]);
                tcpupflow += int.Parse(str[6]);
                tcpdowntimes += int.Parse(str[7]);
                tcpdownflow += int.Parse(str[8]);
                averrtt += double.Parse(str[11]);

                strLine = sr.ReadLine();

            }


            sr.Close();

            //ɾ����ʱ�ļ�
            //File.Delete(tmpfileName);

            //��ȡtcp������Ϣ��д��������־�ļ���
            averrtt /= tcpconnecttimes;
            averrtt = Math.Round(averrtt, 3);
            AverValue.TcpInfo = "TCP������Ϣ���£�\t\r\n";
            AverValue.TcpInfo += "TCP���Ӹ��� \t" + tcpconnecttimes.ToString() + "\t\r\n";
            AverValue.TcpInfo += "TCP���а����� \t" + tcpuptimes.ToString() + "\t\r\n";
            AverValue.TcpInfo += "TCP���а�����(�ֽ�)  \t" + tcpupflow.ToString() + "\t\r\n";
            AverValue.TcpInfo += "TCP���а����� \t" + tcpdowntimes.ToString() + "\t\r\n";
            AverValue.TcpInfo += "TCP���а�����(�ֽ�)  \t" + tcpdownflow.ToString() + "\t\r\n";
            AverValue.TcpInfo += "RTT��ֵ(��) \t" + averrtt.ToString() + "\t";


            /***********���TCP�쳣�б�********************/

            //���ԭ�е���
            LVTCPEx.Items.Clear();

            //��ʱ�ļ�����
            tmpfileName = "dissectTcpExcep.tmp";

            //������ʱ�ļ�
            fs = File.Create(tmpfileName);
            fs.Close();

            //����TCP����Ϣ��ȡ����
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

            //�����ļ�����
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
            //��ȡÿһ������
            while (strLineExcep != null)
            {
                tcpCnt++;
                //�õ�ÿһ��Ԫ����
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
                    //����ListView
                    LVTCPEx.Items.AddRange(lvi);
                    LVTCPEx.EndUpdate();

                    lvi = new ListViewItem[groupCount];
                    lineCount = 0;
                }
            }

            LVTCPEx.BeginUpdate();
            //�������һ��
            for (int i = 0; i < lineCount; i++)
            {
                LVTCPEx.Items.Add(lvi[i]);
            }
            LVTCPEx.EndUpdate();
            srExcep.Close();
            redeliverrate = Math.Round(redeliverrate * 100 / tcpCnt, 3);

            AverValue.TcpEx = "TCP�쳣��Ϣ���£�\t\r\n";
            AverValue.TcpEx += "ǰ���ݰ���ʧ���� \t" + tcplastpaclost.ToString() + "\t\r\n";
            AverValue.TcpEx += "�ظ�ȷ�ϴ��� \t" + reack.ToString() + "\t\r\n";
            AverValue.TcpEx += "������� \t" + wrongorder.ToString() + "\t\r\n";
            AverValue.TcpEx += "�ش����� \t" + redeliver.ToString() + "\t\r\n";
            AverValue.TcpEx += "�ش��� \t" + redeliverrate.ToString() + "%\t\r\n";
            //ɾ����ʱ�ļ�
            File.Delete(tmpfileName);
            return true;
        }

        //��DNS�б�������ݺ���
        //DNS��Ϣ��ȡ����
        [DllImport("NetPryDll.dll")]
        public extern static int dns_anal_tofile(string tmpfileName);
        private bool ShowLVDNSAnalys()
        {

            //���ԭ�е���
            LVDNSAnalys.Items.Clear();

            //��ʱ�ļ�����
            string tmpfileName = "dissectDNS.tmp";

            //������ʱ�ļ�
            FileStream fs = File.Create(tmpfileName);
            fs.Close();

            //����DNS������Ϣ��ȡ����             
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

            //�����ļ�����
            StreamReader sr = new StreamReader(tmpfileName);
            sr.ReadLine();
            string strLine = sr.ReadLine();
            //ListView���������������
            ListViewItem[] lvi;
            ListViewItem.ListViewSubItem lvsi;

            const int groupCount = 50;
            lvi = new ListViewItem[groupCount];
            int lineCount = 0;

            //����ͼ��ĺ�������
            double xValue = 1.0;
            double yValue = 0.0;
            bool yValueGet = false;
            // Set series chart type
            ChartDNS.Series["��Ӧʱ��(��)"].Type = SeriesChartType.Bar;
            // Set series point width
            ChartDNS.Series["��Ӧʱ��(��)"]["PointWidth"] = "1.0";
            // Show data points labels
            ChartDNS.Series["��Ӧʱ��(��)"].ShowLabelAsValue = false;
            // Set data points label style
            ChartDNS.Series["��Ӧʱ��(��)"]["BarLabelStyle"] = "Center";
            // Display chart as 3D
            ChartDNS.ChartAreas[0].Area3DStyle.Enable3D = false;
            // Draw the chart as embossed
            ChartDNS.Series["��Ӧʱ��(��)"]["DrawingStyle"] = "Emboss";

            //��ȡÿһ������
            while (strLine != null)
            {
                //�õ�ÿһ��Ԫ����
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
                    //����ListView
                    LVDNSAnalys.Items.AddRange(lvi);
                    LVDNSAnalys.EndUpdate();

                    lvi = new ListViewItem[groupCount];
                    lineCount = 0;
                }

                yValueGet = double.TryParse(str[8], out yValue);
                if (!yValueGet) yValue = 0.0;
                AverValue.AverDNS += yValue;

                ChartDNS.Invoke(addDataDel, ChartDNS, ChartDNS.Series["��Ӧʱ��(��)"], xValue++, yValue);

            }
            if (xValue > 0)
                AverValue.AverDNS /= (int)xValue;
            AverValue.AverDNS = Math.Round(AverValue.AverDNS, 6);
            LVDNSAnalys.BeginUpdate();
            //�������һ��
            for (int i = 0; i < lineCount; i++)
            {
                LVDNSAnalys.Items.Add(lvi[i]);
            }
            LVDNSAnalys.EndUpdate();
            sr.Close();

            //����ͼ����
            ChartDNS.Invalidate();
            Console.WriteLine("DNS into Mysql!");
            //txt�ļ�ѹ�뵽���ݿ�
            //Application.StartupPath +"\\�ļ�.txt"
            if (mysqlWebFlag && serverTest)
                mysqlWeb.TxTInsertMySQL("DNSAnalysis", currentId + "#" + "Video", Application.StartupPath + "\\" + tmpfileName);
            //ɾ����ʱ�ļ�,������Ҫע��
#if RELEASE
            File.Delete(tmpfileName);
#endif
            return true;
        }

        //��HTTP�б�������ݺ���
        //HTTP��Ϣ��ȡ����
        [DllImport("NetPryDll.dll")]
        public extern static int http_anal_tofile(string tmpfileName);
        private bool ShowLVHTTPAnalys()
        {

            //���ԭ�е���
            LVHTTPAnalys.Items.Clear();

            //��ʱ�ļ�����
            string tmpfileName = "dissectHTTP.tmp";

            //������ʱ�ļ�
            FileStream fs = File.Create(tmpfileName);
            fs.Close();

            //����HTTP������Ϣ��ȡ����,������dissect.tmp�ļ�        
            int retCode = -1;
            try
            {
                retCode = http_anal_tofile(tmpfileName);      //�������Ӧ���Ƕ�ȡ��һ�ε����ݵ���ʱ�ļ���     
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

            //�����ļ�����
            StreamReader sr = new StreamReader(tmpfileName);
            string strLine = sr.ReadLine();

            //ListView���������������
            ListViewItem[] lvi;
            ListViewItem.ListViewSubItem lvsi;

            const int groupCount = 50;
            lvi = new ListViewItem[groupCount];
            int lineCount = 0;
            int SumLine = 0;
            double HttpDelay = 0.0;
            bool HttpDelayGet = false;
            strLine = sr.ReadLine();

            //��ȡÿһ������
            while (strLine != null)
            {

                //�õ�ÿһ��Ԫ����
                //string[] str = strLine.Split(new Char[] { '\t' }, 7);
                string[] str = strLine.Split(new Char[] { '\t' });
                //lvi[lineCount] = new ListViewItem();  //���
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

                //lvsi = new ListViewItem.ListViewSubItem();  //������
                //lvsi.Text = str[0];
                //lvi[lineCount].SubItems.Add(lvsi);
                //lvi[lineCount].SubItems[1].ForeColor = System.Drawing.Color.Red;

                lvsi = new ListViewItem.ListViewSubItem();  //��ӿͻ���IP+�˿�
                lvsi.Text = str[1];
                lvi[lineCount].SubItems.Add(lvsi);
                lvi[lineCount].SubItems[1].ForeColor = System.Drawing.Color.Gray;

                lvsi = new ListViewItem.ListViewSubItem();  //��ӽ�����ʽ
                lvsi.Text = str[2];
                lvi[lineCount].SubItems.Add(lvsi);
                lvi[lineCount].SubItems[2].ForeColor = System.Drawing.Color.Green;

                lvsi = new ListViewItem.ListViewSubItem();  //���URL
                lvsi.Text = str[3];
                lvi[lineCount].SubItems.Add(lvsi);
                lvi[lineCount].SubItems[3].ForeColor = System.Drawing.Color.Green;

                lvsi = new ListViewItem.ListViewSubItem();  //��ӷ�����IP+�˿�
                lvsi.Text = str[4];
                lvi[lineCount].SubItems.Add(lvsi);
                lvi[lineCount].SubItems[4].ForeColor = System.Drawing.Color.Gray;

                lvsi = new ListViewItem.ListViewSubItem();  //��Ӱ汾��
                lvsi.Text = str[5];
                lvi[lineCount].SubItems.Add(lvsi);
                lvi[lineCount].SubItems[5].ForeColor = System.Drawing.Color.Gray;

                lvsi = new ListViewItem.ListViewSubItem();    //�����Ӧ��ʱ
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
                //����AverHTTP��ֵ
                HttpDelayGet = double.TryParse((str[6]), out HttpDelay);
                if (!HttpDelayGet) HttpDelay = 0.0;
                AverValue.AverHTTP += HttpDelay;

                if (lineCount % groupCount == 0)
                {
                    LVHTTPAnalys.BeginUpdate();
                    //����ListView
                    LVHTTPAnalys.Items.AddRange(lvi);
                    LVHTTPAnalys.EndUpdate();

                    lvi = new ListViewItem[groupCount];
                    lineCount = 0;
                }
            }

            LVHTTPAnalys.BeginUpdate();
            //�������һ��
            for (int i = 0; i < lineCount; i++)
            {
                LVHTTPAnalys.Items.Add(lvi[i]);
            }
            LVHTTPAnalys.EndUpdate();
            sr.Close();

            //�����������Ӧʱ��
            if (SumLine > 0)
                AverValue.AverHTTP /= SumLine;
            AverValue.AverHTTP = Math.Round(AverValue.AverHTTP, 6);

            //txt�ļ�ѹ�뵽���ݿ�  
            if (mysqlWebFlag && serverTest)
                mysqlWeb.TxTInsertMySQL("HttpAnalysis", currentId + "#" + "Video", Application.StartupPath + "\\" + tmpfileName);
            //ɾ����ʱ�ļ�
#if RELEASE
            File.Delete(tmpfileName);
#endif
            return true;
        }

        /********************************************************************************
                              ��Pcap���������õ���������֡���б�           
          ********************************************************************************/
        private void InOutFrameLenAnalys()
        {
            ScaleComboBox.SelectedIndex = 0;
            if (!ShowLVInOut(PcapFileName, ScaleComboBox.SelectedIndex))
            {
                WrongReason += "�����������쳣 \n";
            }

            if (!ShowLVFrameLength())
            {
                WrongReason += "֡���ֲ������쳣 \n";
            }

            return;
        }

        //����pcap�ļ���������(Link)��������������֡���ֲ��б�
        [DllImport("LinkAnal.dll")]
        public extern static int link_analyze_inCS(string PcapFile, double scale, string tmpfileName,
    ref int totalPktCount, int[] rangeCount);
        //���������б��������
        private bool ShowLVInOut(string PcapFile, int selectIndex)
        {
            int index = selectIndex;
            double scale = timeScale[index];
            string tmpfileName = "LinkAnal.tmp";
            int SumLine = 0;

            //������ʱ�ı��ļ� 
            FileStream fs = File.Create(tmpfileName);
            fs.Close();

            //������·��������,������LinkAnal.tmp�ļ�
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
                //ɾ����ʱ�ļ�
                File.Delete(tmpfileName);
                return false;
            }



            //����б�
            LVInOut.Items.Clear();

            ListViewItem lvi;
            ListViewItem.ListViewSubItem lvsi;

            //�����ļ�����
            StreamReader sr = new StreamReader(tmpfileName);
            string strLine = sr.ReadLine();
            int counter = 1;
            //����ͼ�������ϵ�ĺ�������
            double xValue = 0.0;
            double yValue = 0.0;
            bool yValueGet = false;
            double MaxInOut = double.Parse(strLine);
            double MinInOut = double.Parse(strLine);

            //��ȡÿһ������
            while (strLine != null)
            {
                lvi = new ListViewItem();
                lvi.Text = (counter++ * scale).ToString();

                lvsi = new ListViewItem.ListViewSubItem();
                lvsi.Text = strLine;
                lvi.SubItems.Add(lvsi);
                //��������ֵ
                if (Convert.ToDouble(strLine) >= MaxInOut)
                    MaxInOut = Convert.ToDouble(strLine);
                if (Convert.ToDouble(strLine) <= MinInOut)
                    MinInOut = Convert.ToDouble(strLine);

                //����ListView
                LVInOut.Items.Add(lvi);

                yValueGet = double.TryParse(strLine, out yValue);
                if (!yValueGet) yValue = 0.0;
                AverValue.AverInOut += yValue;
                SumLine++;
                xValue += scale;
                ChartInOut.Invoke(addDataDel, ChartInOut, ChartInOut.Series["����������"], xValue, yValue);

                strLine = sr.ReadLine();

            }
            sr.Close();

            //������������ֵ
            if (SumLine > 0)
            {
                if (index == 1) SumLine /= 10;
                else if (index == 2) SumLine /= 100;
                AverValue.AverInOut /= SumLine;
            }
            AverValue.AverInOut = Math.Round(AverValue.AverInOut, 2);

            //��ʾ��������ֵ�;�ֵ
            this.InOutMax.Text += MaxInOut.ToString() + "�ֽ�";
            this.InOutMin.Text += MinInOut.ToString() + "�ֽ�";
            this.InOutAvg.Text += AverValue.AverInOut.ToString() + "�ֽ�";
            this.InOutAvg.Visible = true;
            this.InOutMax.Visible = true;
            this.InOutMin.Visible = true;

            //ȷ������������ĳߴ�
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

            //ȷ������������Сֵ,��ֹ���ֵΪ5�ı���
            //minX = 5 - minX % 5 + minX;
            minX = 0;
            maxX = 5 + maxX % 5 + maxX;
            minY = 5 - minY % 5 + minY;
            maxY = 5 - maxY % 5 + maxY;

            this.ChartInOut.ChartAreas[0].AxisX.Minimum = minX;
            this.ChartInOut.ChartAreas[0].AxisX.Maximum = maxX;
            this.ChartInOut.ChartAreas[0].AxisY.Minimum = minY;
            this.ChartInOut.ChartAreas[0].AxisY.Maximum = maxY;

            //ȷ��������ļ����10,��֤��ļ������Ϊ0(��������ֻ��һ��ʱ���ܻ�����������)
            this.ChartInOut.ChartAreas[0].AxisX.Interval = (((maxX - minX) / 10 > 0) ? ((maxX - minX) / 10) : 1);
            this.ChartInOut.ChartAreas[0].AxisY.Interval = (((maxY - minY) / 10 > 0) ? ((maxY - minY) / 10) : 1);

            //����ͼ��
            this.ChartInOut.Invalidate();
            //txt�ļ�ѹ�뵽���ݿ�
            //if (mysqlWebFlag && serverTest)
            //�˴���Ҫ�޸�********************************************************************
            //mysqlWeb.TxTInsertMySQL("InOutAnalysis", currentId + "#" + "Video",tmpfileName);
            //ɾ����ʱ�ļ�
#if RELEASE
            File.Delete(tmpfileName);
#endif
            return true;
        }

        //����ʱ��߶�(ֻ����������Ӱ��)
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
            InOutAvg.Text = "ƽ��ֵ��";
            InOutMax.Text = "���ֵ��";
            InOutMin.Text = "��Сֵ��";
            //�Ƚ�ChartInOut�еĻ������
            ChartInOut.Invoke(clearDataDel, ChartInOut);
            //ˢ������
            ShowLVInOut(PcapFileName, ScaleComboBox.SelectedIndex);
            //ˢ��ѡ������ֵ
            prevSelectIndex = ScaleComboBox.SelectedIndex;

        }

        //��֡�������б��������
        private bool ShowLVFrameLength()
        {
            //���֡���ֲ��б�
            LVFrameLength.Items.Clear();

            string[] strRange = new string[] { "0-100", "100-200", "200-300", "300-400",
                "400-500", "500-600", "600-700", "700-800", "800-900", "900-1000", "1000-1514"};
            string[] xValue = strRange;
            double[] yValue = new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            //ListView���������������
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
                //����ListView
                LVFrameLength.Items.Add(lvi);
            }
            //��֡���ֲ��ı�ͼ
            ChartFrameLength.Series["֡���ֲ�"].Points.DataBindXY(strRange, yValue);
            ChartFrameLength.Invalidate();

            return true;
        }

        /********************************************************************************
                             ��Pcap���������õ���ʱ����           
         ********************************************************************************/

        private void DelayJitterAnalys()
        {
            bool AnalysOK = false;
            //��try catch���Ա�����ʱ��������������bug���������������Ӱ��
            try
            {
                AnalysOK = ShowLVDelayJitter(PcapFileName);
                if (!AnalysOK)
                    WrongReason += "��ʱ���������쳣 \n";
            }
            catch (System.Exception ex)
            {
                Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
            }

            return;

        }

        //����ʱ�����б��������
        [DllImport("NetpryDll.dll")]
        public extern static int delay_jitter_tofile(string tmpfileName);
        public bool ShowLVDelayJitter(string PcapFile)
        {
            //���DelayJitter�е���������
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
            //����һ��,���м�¼����������Ϣ
            string strLine = sr.ReadLine();
            strLine = sr.ReadLine();
            int linecount = 0;
            const int graphcount = 50;
            ListViewItem[] lv = new ListViewItem[graphcount];
            ListViewItem.ListViewSubItem lvsi;

            //��ʱ���������ϵ��ֵ
            double xValue = 0.0;   //�������Խ�����֡���Ϊ��׼,��1��ʼ�ۼ�
            double yDelayValue = 0.0; //��ʱ���ߵĵ�������
            double yJitterValue = 0.0; //�������ߵĵ�������

            //��ʱ������ֵ
            double MaxDelay = 0.0;
            double MinDelay = 1.0;
            double MaxJitter = 0.0;
            double MinJitter = 1.0;

            bool yDelayValueGet = false;
            bool yJitterValueGet = false;

            while (strLine != null)
            {

                string[] str = strLine.Split(new Char[] { '\t' });

                //�˳�����Щ���ڲ������µĹ������ʱ�Ͷ���
                if (double.Parse(str[3]) > 2.0 || double.Parse(str[4]) > 2.0)
                {
                    strLine = sr.ReadLine();
                    continue;
                }

                lv[linecount] = new ListViewItem();
                lv[linecount].Text = str[2];

                //����ʱ������ֵ
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
                //��ͼʱ�Ժ���Ϊ��λ
                yDelayValue *= 1000;
                yJitterValue *= 1000;

                ChartDelayJitter.Invoke(addDataDel, ChartDelayJitter, ChartDelayJitter.Series["��ʱ����"], xValue, yDelayValue);
                ChartDelayJitter.Invoke(addDataDel, ChartDelayJitter, ChartDelayJitter.Series["��������"], xValue++, yJitterValue);

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
                    LVDelayJitter.BeginUpdate();//����ListView
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

            //��ʾ��ʱ��������ֵ�;�ֵ
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


            //�ҵ�������������Сֵ,ȷ���߶�
            double maxY = 0;
            double minY = 0;
            double maxX = 0;
            double minX = 0;
            foreach (Series iSeries in this.ChartDelayJitter.Series)
            {
                //����ж������ϵ�����Ƿ�Ϊ��
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

            //ȷ������������Сֵ,��ֹ���ֵΪ5�ı���
            minX = 0;
            maxX = 5 - maxX % 5 + maxX;
            minY = 0;
            maxY = 5 - maxY % 5 + maxY;

            this.ChartDelayJitter.ChartAreas[0].AxisX.Minimum = minX;
            this.ChartDelayJitter.ChartAreas[0].AxisX.Maximum = maxX;
            this.ChartDelayJitter.ChartAreas[0].AxisY.Minimum = minY;
            this.ChartDelayJitter.ChartAreas[0].AxisY.Maximum = maxY;

            //ȷ��������ļ����10
            this.ChartDelayJitter.ChartAreas[0].AxisX.Interval = (((maxX - minX) / 10 > 0) ? ((maxX - minX) / 10) : 1);
            this.ChartDelayJitter.ChartAreas[0].AxisY.Interval = (((maxY - minY) / 10 > 0) ? ((maxY - minY) / 10) : 1);

            //ͼ���ع�
            this.ChartDelayJitter.Invalidate();
            //txt�ļ�ѹ�뵽���ݿ�
            if (mysqlWebFlag && serverTest)
                mysqlWeb.TxTInsertMySQL("DelayJitter", currentId + "#" + "Video", Application.StartupPath + "\\" + tmpfileName);
#if RELEASE
            File.Delete(tmpfileName);
#endif
            return true;

        }

        /**************************************************************************************
                        ���Ա�����ʾ
       ****************************************************************************************/
        private void ResultDisplay()
        {
            ResultDisplay2();              //���Ա��������ܵ�txt��ʽ

            //txtд��listview
            StreamReader readData = new StreamReader(strTxtResult, Encoding.Default);//���������ļ���
            string lineData = null;    //ÿһ�е����ݣ���׼��ʽ���Էָ����ָ�
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
                             ��ͼ��Ĵ���
        ****************************************************************************************/
        //����������ݵ�ί��
        public delegate void AddDataDelegate(Chart ichart, Series ptSeries, double xValue, double yValue);
        public AddDataDelegate addDataDel;
        //����������ݵ�ί��
        public delegate void ClearDataDelegate(Chart ichart);
        public ClearDataDelegate clearDataDel;
        //��ί�а󶨺���
        private void RealTimechart()
        {
            clearDataDel += new ClearDataDelegate(ClearChartData);
            addDataDel += new AddDataDelegate(AddData);
        }
        //��ͼ���������
        public void AddData(Chart ichart, Series ptSeries, double xValue, double yValue)
        {
            AddNewPoint(ptSeries, xValue, yValue);
        }
        //��������ӵ�
        public void AddNewPoint(Series ptSeries, double xValue, double yValue)
        {
            // Add new data point to its series.
            ptSeries.Points.AddXY(xValue, yValue);
        }
        //ʹ��Ҫpage�µ�ͼ�����߿����϶�
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
        //���ͼ����
        public void ClearChartData(Chart ichart)
        {
            foreach (Series ptSeries in ichart.Series)
            {
                ptSeries.Points.Clear();
            }

            ichart.Invalidate();
        }

        //��������
        private void ResultDisplay2()
        {
            string resultTxt = "ResultTxt.tmp";
            FileStream fs3 = new FileStream(resultTxt, FileMode.Append, FileAccess.Write);
            StreamWriter ResultTmp = new StreamWriter(fs3, Encoding.Default);  //��ʱ�ܽᱨ���ļ������������ض��ĸ�ʽѹ�����ݿ�
            int index = 0;
            if (!File.Exists(strTxtResult))
            {
                using (StreamWriter swlog = new StreamWriter(File.Create(strTxtResult), Encoding.UTF8))
                {

                    //��Ƶ���롢�ֱ�����Ϣ
                    swlog.Write("\r\nWEB���Թ�������Ƶ��Ϣ���£�\r\n");

                    string strMediaInfo = "FlvMetaData.txt";

                    string startPath = Application.StartupPath;    //��ȡӦ�ó���·��
                    string[] files = Directory.GetFiles(startPath);     //��ȡ·���������ļ�
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
                            swlog.Write("��Ƶ��ʽ:" + "\t" + fileType + "\r\n");
                            ResultTmp.Write((++index).ToString() + "\t" + "VideoFormat\t" + fileType + "\r\n");
                            FileStream fs1 = new FileStream(strMediaInfo, FileMode.Open, FileAccess.Read);
                            StreamReader sr1 = new StreamReader(fs1, Encoding.Default);
                            String[] MediaInfo = null;
                            strMediaInfo = sr1.ReadLine();    //����ʱ��(s):	232.33
                            MediaInfo = strMediaInfo.Split('\t');
                            if (MediaInfo.Length == 2)
                                ResultTmp.Write((++index).ToString() + "\t" + "DurationTime" + "\t" + MediaInfo[1] + "\r\n");
                            strMediaInfo = sr1.ReadLine();   //Videosize:	10797209.00
                            MediaInfo = strMediaInfo.Split('\t');
                            if (MediaInfo.Length == 2)
                                ResultTmp.Write((++index).ToString() + "\t" + "VideoSize" + "\t" + MediaInfo[1] + "\r\n");
                            strMediaInfo = sr1.ReadLine();   //��Ƶ֡��(fps):	15.01
                            MediaInfo = strMediaInfo.Split('\t');
                            if (MediaInfo.Length == 2)
                                ResultTmp.Write((++index).ToString() + "\t" + "VideoFps" + "\t" + MediaInfo[1] + "\r\n");
                            swlog.Write(strMediaInfo + "\r\n");
                            strMediaInfo = sr1.ReadLine();   //��Ƶ����(kbps):	361.78
                            MediaInfo = strMediaInfo.Split('\t');
                            if (MediaInfo.Length == 2)
                                ResultTmp.Write((++index).ToString() + "\t" + "VideoCodeRate" + "\t" + MediaInfo[1] + "\r\n");
                            {
                                swlog.Write(strMediaInfo + "\r\n");
                                strMediaInfo = sr1.ReadLine();   //videocodecid:	7.00	AVC-H.264
                                MediaInfo = strMediaInfo.Split('\t');
                                if (MediaInfo.Length == 3)
                                {
                                    swlog.Write("��Ƶ���뷽ʽ:" + "\t" + MediaInfo[2] + "\r\n");
                                    ResultTmp.Write((++index).ToString() + "\t" + "CodeingFormat" + "\t" + MediaInfo[2] + "\r\n");
                                }
                                else if (MediaInfo.Length == 2)
                                {
                                    swlog.Write("��Ƶ���뷽ʽ:" + "\t" + MediaInfo[1] + "\r\n");
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
                                swlog.Write("��Ƶ�ֱ���:" + "\t" + width + "*" + height + "\r\n");
                                ResultTmp.Write((++index).ToString() + "\t" + "VideoResolutionRate" + "\t" + width + "*" + height + "\r\n");
                            }
                            sr1.Close();
                            fs1.Close();
                        }
                        catch (Exception ex)
                        {
                            Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
                            MessageBox.Show("�޷���ȡ��Ƶ������Ϣ��");
                        }
                    }
                    else
                    {
                        strMediaInfo = "������û�н��յ����ݣ��޷�������Ƶ��Ϣ��\r\n";
                        swlog.Write(strMediaInfo + "\r\n");
                    }

                    //����õ��ĸ���ȱ��ָ��ľ�ֵ
                    swlog.Write("\r\nWEB�����и���������ֵ���£�\r\n");
                    if (AverValue.FrameRate == null)
                        AverValue.FrameRate = "WEB�������ɹ����޷������Ƶ֡��";
                    //swlog.Write(AverValue.FrameRate + "\t\r\n");
                    swlog.Write("DNS��Ӧƽ����ʱ(��)\t" + AverValue.AverDNS.ToString() + "\t\r\n");
                    ResultTmp.Write((++index).ToString() + "\t" + "DNS mean_delay(s)\t" + AverValue.AverDNS.ToString() + "\r\n");

                    swlog.Write("HTTP��Ӧƽ����ʱ(��)\t" + AverValue.AverHTTP.ToString() + "\t\r\n");
                    ResultTmp.Write((++index).ToString() + "\t" + "HTTP mean_delay(s)\t" + AverValue.AverHTTP.ToString() + "\r\n");

                    swlog.Write("��������Ӧƽ����ʱ(��)\t" + AverValue.AverHTTP.ToString() + "\t\r\n");
                    ResultTmp.Write((++index).ToString() + "\t" + "SERVER mean_delay(s)\t" + AverValue.AverHTTP.ToString() + "\r\n");

                    swlog.Write("��������ֵ(�ֽ�/��))\t" + AverValue.AverInOut.ToString() + "\t\r\n");
                    ResultTmp.Write((++index).ToString() + "\t" + "InOut mean_delay(s)\t" + AverValue.AverInOut.ToString() + "\r\n");

                    swlog.Write("ƽ����ʱ(��)\t" + AverValue.AverDelay.ToString() + "\t\r\n");
                    ResultTmp.Write((++index).ToString() + "\t" + "mean_delay(s)\t" + AverValue.AverDelay.ToString() + "\r\n");

                    swlog.Write("ƽ������(��)\t" + AverValue.AverJitter.ToString() + "\t\r\n");
                    ResultTmp.Write((++index).ToString() + "\t" + "mean_jitter(s)\t" + AverValue.AverJitter.ToString() + "\r\n");
                    //дTCP������Ϣ
                    swlog.Write("\r\n" + AverValue.TcpInfo + "\r\n");
                    string[] tcpInfo = AverValue.TcpInfo.Split(new string[] { "\t\r\n" }, StringSplitOptions.RemoveEmptyEntries); ;
                    for (int i = 0; i < tcpInfo.Length; i++)
                    {
                        if (i == 0) continue;
                        ResultTmp.Write((++index).ToString() + "\t" + tcpInfo[i] + "\r\n");
                    }
                    //д��TCP�쳣��Ϣ
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
                    //txt�ļ�ѹ�뵽���ݿ�
                    if (mysqlWebFlag && serverTest)
                        mysqlWeb.TxTInsertMySQL("TestReport", currentId + "#" + "Video", Application.StartupPath + "\\" + resultTxt);
                    //ɾ����ʱ�ļ�
                    File.Delete(resultTxt);
                }
            }
        }

        private void btnWebSelCap_Click(object sender, EventArgs e)
        {
            OpenFileDialog capFile = new OpenFileDialog();
            capFile.RestoreDirectory = true;
            capFile.Multiselect = false;
            capFile.Filter = "pcap�ļ�|*.pcap";
            if (capFile.ShowDialog() == DialogResult.OK)
            {
                PcapFileName = capFile.FileName;
                strXlsLogFile = capFile.FileName.Replace(".pcap", ".xlsx");
                inis.IniWriteValue("Flv", "PcapFile", PcapFileName);
                MessageBox.Show("������ɣ�");
                isSelectPcap = true;        //ѡ����pcap�ļ�           
            }
            else
            {
                MessageBox.Show("��ѡ��ץ���ļ���");
                return;
            }
        }

        private void btnLastPage_Click(object sender, EventArgs e)    //��һҳ����Ӧ����
        {
            currentPage--;    //��ǰҳ���Լ�
            btnNextPage.Enabled = true;
            if (currentPage == 1)
            {
                btnLastPage.Enabled = false;    //��һҳʱ��һҳ������
            }
            comboxJumpPage.SelectedIndex = currentPage - 1;
            string packageFile = "dissectPacket.tmp";
            getPageRecord(packageFile, currentPage);    //��ȡ��ǰҳ�ļ�¼
        }


        private void btnNextPage_Click(object sender, EventArgs e)
        {
            currentPage++;
            btnLastPage.Enabled = true;    //��һҳʹ��
            if (currentPage == pageNum)    //��ǰҳ����ҳ����û����һҳ
            {
                btnNextPage.Enabled = false;
            }
            comboxJumpPage.SelectedIndex = currentPage - 1;
            string packageFile = "dissectPacket.tmp";
            getPageRecord(packageFile, currentPage);    //��ȡ��ǰҳ�ļ�¼
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
            getPageRecord(packageFile, currentPage);    //��ȡ��ǰҳ�ļ�¼
        }


        private bool getPageRecord(string packageFile, int currentPage)
        {
            if (File.Exists(packageFile))    //�򿪰��ļ�����ƫ��ȡ��
            {
                try
                {
                    FileStream fsPacket = new FileStream(packageFile, FileMode.Open, FileAccess.Read);
                    StreamReader srPacket = new StreamReader(fsPacket, Encoding.Default);
                    //string strLine = srPacket.ReadLine();    //��һ���Ǳ����У�ȥ��
                    string strLine = null;
                    if (currentPage == 1)
                    {
                        strLine = srPacket.ReadLine();     //��һҳʱ��ȡ��һ��
                    }
                    int count = 0;
                    while (count < (currentPage - 1) * PACKETPAGESIZE)
                    {
                        strLine = srPacket.ReadLine();     //�൱��ƫ�Ƶ�ǰ��(currentPage-1) * packetPageSize,��1ҳ��0����ʼȡ����2ҳ�ӵ�2000����ʼȡ
                        count++;
                    }
                    //�����һ�ε��б�
                    LVPacketAnalys.Items.Clear();
                    //��ʼȡ��,ȡ�����2000��
                    int tempSumInPage = 0;
                    //ListView���������������
                    ListViewItem[] lvi;
                    ListViewItem.ListViewSubItem lvsi;
                    lvi = new ListViewItem[GROUPCOUNT];     //200��Ϊһ�飬����ˢ��list
                    int lineCount = 0;
                    while (strLine != null && tempSumInPage < PACKETPAGESIZE) //�ļ�û�ж�ȡ����¼��ﵽһҳ��չʾ��2000
                    {
                        //�õ�ÿһ��Ԫ����
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

                        if (lineCount % GROUPCOUNT == 0)              //ÿ200����¼ˢ��һ��
                        {
                            LVPacketAnalys.BeginUpdate();       //����ˢ��
                            //����ListView
                            LVPacketAnalys.Items.AddRange(lvi);
                            LVPacketAnalys.EndUpdate();

                            lvi = new ListViewItem[GROUPCOUNT];
                            lineCount = 0;
                        }
                    }

                    LVPacketAnalys.BeginUpdate();     //��û�дﵽ50��ʱû��ˢ������������Ҫˢ��
                    //�������һ��
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
                    return false;     //���ؼ�¼ʧ��
                }
            }
            else
            {
                return false;
            }
        }

        private void btnLastPageTcpGene_Click(object sender, EventArgs e)    //��һҳ����Ӧ����
        {
            currentPageTcpGene--;    //��ǰҳ���Լ�
            btnNextPageTcpGene.Enabled = true;
            if (currentPageTcpGene == 1)
            {
                btnLastPageTcpGene.Enabled = false;    //��һҳʱ��һҳ������
            }
            comboxJumpPageTcpGene.SelectedIndex = currentPageTcpGene - 1;
            string packageFile = "dissectTcp.tmp";
            getPageRecordTcpGene(packageFile, currentPageTcpGene);    //��ȡ��ǰҳ�ļ�¼
        }


        private void btnNextPageTcpGene_Click(object sender, EventArgs e)
        {
            currentPageTcpGene++;
            btnLastPageTcpGene.Enabled = true;    //��һҳʹ��
            if (currentPageTcpGene == pageNumTcpGene)    //��ǰҳ����ҳ����û����һҳ
            {
                btnNextPageTcpGene.Enabled = false;
            }
            comboxJumpPageTcpGene.SelectedIndex = currentPageTcpGene - 1;
            string packageFile = "dissectTcp.tmp";
            getPageRecordTcpGene(packageFile, currentPageTcpGene);    //��ȡ��ǰҳ�ļ�¼
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
            getPageRecordTcpGene(packageFile, currentPageTcpGene);    //��ȡ��ǰҳ�ļ�¼
        }

        private bool getPageRecordTcpGene(string tcpGeneFile, int currentPageTcp)
        {
            if (File.Exists(tcpGeneFile))    //�򿪰��ļ�����ƫ��ȡ��
            {
                try
                {
                    FileStream fsTcp = new FileStream(tcpGeneFile, FileMode.Open, FileAccess.Read);
                    StreamReader srTcp = new StreamReader(fsTcp, Encoding.Default);
                    //string strLine = srPacket.ReadLine();    //��һ���Ǳ����У�ȥ��
                    string strLine = null;
                    if (currentPageTcp == 1)
                    {
                        strLine = srTcp.ReadLine();     //��һҳʱ��ȡ��һ��,����Ҫ��֤
                    }
                    int count = 0;
                    while (count < (currentPageTcp - 1) * PACKETPAGESIZE)
                    {
                        strLine = srTcp.ReadLine();     //�൱��ƫ�Ƶ�ǰ��(currentPage-1) * packetPageSize,��1ҳ��0����ʼȡ����2ҳ�ӵ�2000����ʼȡ
                        count++;
                    }
                    //�����һ�ε��б�
                    LVTCPGeneral.Items.Clear();
                    //��ʼȡ��,ȡ�����2000��
                    int tempSumInPage = 0;
                    //ListView���������������
                    ListViewItem[] lvi;
                    ListViewItem.ListViewSubItem lvsi;
                    lvi = new ListViewItem[GROUPCOUNT];     //200��Ϊһ�飬����ˢ��list
                    int lineCount = 0;
                    //����ͼ��ĺ�������
                    double xValue = 1.0;
                    //double yEndValue = 0.0;
                    double yLastValue = 0.0;  //����ʱ��
                    double yStartValue = 0.0;
                    bool TcpDelayGet = false;

                    // Set series chart type
                    //ChartTcpGenr.Series["��ʼʱ��(��)"].Type = SeriesChartType.Bar;
                    //// Set series point width
                    //ChartTcpGenr.Series["��ʼʱ��(��)"]["PointWidth"] = "1.0";
                    //// Show data points labels
                    //ChartTcpGenr.Series["��ʼʱ��(��)"].ShowLabelAsValue = false;
                    //// Set data points label style
                    //ChartTcpGenr.Series["��ʼʱ��(��)"]["BarLabelStyle"] = "Center";
                    // Display chart as 3D
                    ChartTcpGenr.ChartAreas[0].Area3DStyle.Enable3D = false;
                    // Draw the chart as embossed
                    ChartTcpGenr.Series["����ʱ��(��)"]["DrawingStyle"] = "Emboss";
                    // Set series chart type
                    ChartTcpGenr.Series["����ʱ��(��)"].Type = SeriesChartType.Bar;
                    // Set series point width
                    ChartTcpGenr.Series["����ʱ��(��)"]["PointWidth"] = "1.0";
                    // Show data points labels
                    ChartTcpGenr.Series["����ʱ��(��)"].ShowLabelAsValue = false;
                    // Set data points label style
                    ChartTcpGenr.Series["����ʱ��(��)"]["BarLabelStyle"] = "Center";
                    // Display chart as 3D
                    //ChartTcpGenr.ChartAreas[0].Area3DStyle.Enable3D = false;
                    // Draw the chart as embossed
                    //ChartTcpGenr.Series["����ʱ��(��)"]["DrawingStyle"] = "Emboss";

                    while (strLine != null && tempSumInPage < PACKETPAGESIZE) //�ļ�û�ж�ȡ����¼��ﵽһҳ��չʾ��2000
                    {
                        //�õ�ÿһ��Ԫ����
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

                        if (lineCount % GROUPCOUNT == 0)              //ÿ200����¼ˢ��һ��
                        {
                            LVTCPGeneral.BeginUpdate();       //����ˢ��
                            //����ListView
                            LVTCPGeneral.Items.AddRange(lvi);
                            LVTCPGeneral.EndUpdate();

                            lvi = new ListViewItem[GROUPCOUNT];
                            lineCount = 0;
                        }
                        //����AverHTTPWeb��ֵ
                        TcpDelayGet = double.TryParse((str[9]), out yStartValue);
                        if (!TcpDelayGet) yStartValue = 0.0;
                        //ChartTcpGenr.Invoke(addDataDel, ChartTcpGenr, ChartTcpGenr.Series["��ʼʱ��(��)"], xValue, yStartValue);
                        TcpDelayGet = double.TryParse((str[10]), out yLastValue);
                        if (!TcpDelayGet) yLastValue = 0.0;
                        ChartTcpGenr.Invoke(addDataDel, ChartTcpGenr, ChartTcpGenr.Series["����ʱ��(��)"], xValue++, yStartValue + yLastValue);
                    }

                    LVTCPGeneral.BeginUpdate();     //��û�дﵽ50��ʱû��ˢ������������Ҫˢ��
                    //�������һ��
                    for (int i = 0; i < lineCount; i++)
                    {
                        LVTCPGeneral.Items.Add(lvi[i]);
                    }
                    LVTCPGeneral.EndUpdate();
                    srTcp.Close();


                    //����ͼ����
                    ChartTcpGenr.Invalidate();

                    return true;
                }
                catch (System.Exception ex)
                {
                    Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
                    return false;     //���ؼ�¼ʧ��
                }
            }
            else
            {
                return false;
            }
        }

    }
}



