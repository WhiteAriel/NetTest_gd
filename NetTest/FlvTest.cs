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
        public static IniFile inisvlcout = new IniFile(Application.StartupPath + "\\vlc.ini"); //��һЩ����Ϊ�йأ�������Ĭ�϶�\\VideoPlayer\\vlc.ini�����ֶ�\\vlc.ini
        public static IniFile inisvlc = new IniFile(Application.StartupPath + "\\VideoPlayer\\vlc.ini"); //ini class
        //IniFile inisref = new IniFile(Application.StartupPath + "\\RefTool" + "\\referencesetup.ini");

        public bool taskon = false;    //��ʾ����û������
        public bool serverTest = false;   //��ʾִ�е��Ƿ������������ն��Լ�������


        ArrayList player_list = new ArrayList();    //���沥����������Ϣ
        ArrayList port_list = new ArrayList();      //���沥�����Ķ˿ں�

        private int iTest = 0;              //���������˶��ٴ�
        private static int intCheckContinuous;     //�Ƿ���������
        private static int iNumContinuous = 0;     //���������ܴ���

        public string strPlayer;            //����������������·����  
        public string strPcapFile = "";     //ץ���ļ���
        public int iDevice = 0;                 //��������
        public int lastPlayerIndex = 0;
        public LibPcapLiveDevice device;
        private ArrayList StartTimeList = new ArrayList();           //��ʼ���Ե�ʱ��

        private string strPlayFile;         //qoe�ļ�
        private string strLogResult = null;
        private string strXlsLogFile;       //log file path (xls file(xls��ʽ) path)
        private ArrayList strbFileList = new ArrayList();    //contents of log file (content of xls file)
        private StringBuilder ScoreParam = new StringBuilder();  //�ٷ��Ʋ�����ȡ

        public static bool DoTest = false;
        public bool IsStartPlay;
        public bool StartStopTest;

        private static PacketCap pcap_packet;


        private MySQLInterface mysqlTest = null;
        private bool mysqlTestFlag = false;
        //�ж�һ��ʱ�����Ƿ���̹ܵ������ݹ����Ķ�ʱ��
        System.Timers.Timer myTimer;


        //������Ƶ�ļ�
        private BackgroundWorker m_AsyncWorker = new BackgroundWorker();
        //�ܵ��߳��б�ÿ����������Ӧһ�������Ĺܵ�
        ArrayList pipeList = new ArrayList();
        //ץ������
        private BackgroundWorker m_AsyncWorker_cap = new BackgroundWorker();

        private double Definition;          //������
        private double Brightness;          //����
        private double Color;               //ɫ��
        private double Saturation;          //���Ͷ�
        private double Contrast;            //�Աȶ�

        private int Static = 0;              //��֡
        private int Skip = 0;                //��֡
        private int Blur = 0;                //ģ��

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

            //�������Զ��庯���ľ�������BackgroundWorker��DoWork��RunWorkerCompleted�¼�
            m_AsyncWorker.WorkerSupportsCancellation = true;
            m_AsyncWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bwAsync_RunWorkerCompleted);
            m_AsyncWorker.DoWork += new DoWorkEventHandler(bwAsync_DoWork);

            m_AsyncWorker_cap.WorkerSupportsCancellation = true;
            m_AsyncWorker_cap.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bwAsync_cap_RunWorkerCompleted);
            m_AsyncWorker_cap.DoWork += new DoWorkEventHandler(bwAsync_cap_DoWork);

            Control.CheckForIllegalCrossThreadCalls = false;
            DoTest = false;


            //���ݿ�����ʼ��
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
            //��ձ�ͼ
            this.InitChart();

            //��ȡ����������Ϣ
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
                MessageBox.Show("�����������Բ��������ã�");
                Log.Error(Environment.StackTrace, ex);
            }

            if (intCheckContinuous > 0)     //����������
            {
                string nc = inis.IniReadValue("Flv", "NumContinuous");
                if (nc != null)
                    iNumContinuous = int.Parse(nc);
                else
                    iNumContinuous = 0;
            }
            else
                iNumContinuous = 0;

            //��ȡ������Ϣ
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
                        MessageBox.Show("�������:" + iDevice.ToString());
                    }
                }
                else
                {
                    MessageBox.Show("��������ȷѡ�������������½��в��ԣ�");
                    return;
                }
            }

            //����������Ŀ¼
            inis.IniWriteValue("Flv", "Player", Application.StartupPath + "\\VideoPlayer");
            //��ȡ��������Ϣ
            strPlayer = inis.IniReadValue("Flv", "Player") + "\\VLCDialog.exe";
            this.dataGridView1.Visible = false;

            if (iDevice < 0)
            {
                iDevice = 0;
            }
            pcap_packet = new PacketCap(iDevice);

            //�����ܵ���ʱ��
            myTimer = new System.Timers.Timer(1000);//��ʱ����50����
            myTimer.Elapsed += myTimer_Elapsed;//��2���������¼�
            myTimer.AutoReset = false; //�Ƿ񲻶��ظ���ʱ������

        }


        //50�����Ķ�ʱ������
        void myTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            gaugeContainer1.Values["Default"].Value = 100;
        }

        /******************************************************************************
        pipe worker
        /*******************************************************************************/
        private void bwAsync_pipe_DoWork(object sender, DoWorkEventArgs e)     //��ɺ�����bwAsync_pipe_RunWorkerCompleted
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            bool createVideoPara = false;
            if (mysqlTestFlag)
                createVideoPara = mysqlTest.CreatVideoPara();
            videoPara vp = new videoPara();
            string ipandtype = inis.IniReadValue("Task", "currentVideoId") + "#" + "Video";
            try
            {
                //�½��ܵ�
                NamedPipeServerStream pipeServer = new NamedPipeServerStream("MyPipe", PipeDirection.InOut, 10);
                //�ȴ�����
                pipeServer.WaitForConnection();
                //���ܵ���
                StreamReader sr = new StreamReader(pipeServer);

                //��¼�ܵ���������
                char[] buffer = new char[256];
                int n;

                string[] arrErro = new string[5];
                Queue<string> qTime = new Queue<string>();
                Queue<int> qStatic = new Queue<int>();     //��ֹ
                Queue<int> qSkip = new Queue<int>();      //��֡
                Queue<int> qBlur = new Queue<int>();      //ģ��

                bool cleared = false;
                while (pipeServer.IsConnected)
                {

                    n = sr.Read(buffer, 0, buffer.Length);  //���Read�Ǹ������������������ڸó����ﲥ�����ش��ĳ�����������Ǵ���whileѭ���ģ������������жϵ�ʱ��Read�����ǲ����ص�
                    myTimer.Enabled = false; //ι����ʱ�䳬��1s��Ϊ��Ƶ������
                    if (n == 0)
                    {
                        if (pipeServer != null)
                        {
                            if (pipeServer.IsConnected)
                            {
                                pipeServer.Disconnect();
                                Thread.Sleep(1000);//�����ӳ٣��ȴ��ܵ��ڵ�whileѭ��̽�⵽���ӶϿ������ٽ������ݶ�д
                            }
                            pipeServer.Close();
                        }

                        return;
                    }

                    string x = null;
                    for (int i = 0; i <= n - 1; i++)
                    {
                        x += buffer[i].ToString();      //xΪ��¼���ݵĳ��ַ���
                    }

                    char[] seper = { '\t', ' ' };
                    string[] ld = x.Split(seper);      //��x�����ݰ�"\t"��Ϊ�ָ�����ȡ�����ݴ���ld����

                    if (ld.Length >= 13)          //��Ϊ���ã�����ld�����ݳ��ȴ���13��ʼ����
                    {
                        myTimer.Enabled = true;  //�������Ź�
                        IsStartPlay = true;

                        if (IsStartPlay && !vlcStart)
                        {
                            startTime = DateTime.Now;
                            vlcStart = true;
                        }

                        //�Ǳ������ͼ����
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

                        //��������ͼ����ȡ����
                        this.Definition = Convert.ToDouble(ld[7 - 1]);        //ld[7-1]: ld�����һ�е����е�Ԫ��
                        this.Brightness = Convert.ToDouble(ld[8 - 1]);
                        this.Color = Convert.ToDouble(ld[9 - 1]);
                        this.Saturation = Convert.ToDouble(ld[10 - 1]);
                        this.Contrast = Convert.ToDouble(ld[11 - 1]);

                        vp.clarity = this.Definition;
                        vp.brightness = this.Brightness;
                        vp.Chroma = this.Color;
                        vp.saturation = this.Saturation;
                        vp.Contrast = this.Saturation;

                        chart1.Invoke(addDataDel, this.chart1, this.Definition);     //������
                        chart2.Invoke(addDataDel, this.chart2, this.Brightness);    //����
                        chart3.Invoke(addDataDel, this.chart3, this.Color);          //ɫ��
                        chart4.Invoke(addDataDel, this.chart4, this.Saturation);    //���Ͷ�
                        chart5.Invoke(addDataDel, this.chart5, this.Contrast);      //�Աȶ�
                        frameNum++;

                        //�����Ǳ��̣����в�����
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

                            //��¼����mysql����VideoPara
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
                                Thread.Sleep(1000);//�����ӳ٣��ȴ��ܵ��ڵ�whileѭ��̽�⵽���ӶϿ������ٽ������ݶ�д
                            }
                            pipeServer.Close();

                        }
                        //�ͷŶ�ʱ����Դ
                        myTimer.Close(); //�ͷ�Timerռ�õ���Դ
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
            {//    �˴������׳��������о�
                Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
                DisplayState(" �����ܵ������������٣�");
                return;
            }
            //�ͷŶ�ʱ����Դ
            myTimer.Close(); //�ͷ�Timerռ�õ���Դ
            myTimer.Dispose();
        }

        /******************************************************************************
           pipe worker instance complete
        /*******************************************************************************/
        private void bwAsync_pipe_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                DisplayState("�����ܵ�����");
                return;
            }

            // Check to see if the background process was cancelled.
            if (e.Cancelled)
            {
                //if (this.m_pipeServer != null)
                //    this.m_pipeServer.Close();
                DisplayState("�����ܵ�����");
                return;
            }
        }

        /******************************************************************************
           pcap worker
        /*******************************************************************************/
        private void bwAsync_cap_DoWork(object sender, DoWorkEventArgs e)      //��ɺ�����bwAsync_cap_RunWorkerCompleted
        {                                                                                                                   //ץ���߳�
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
                DisplayState("ץ������\r\n");
                return;
            }

            // Check to see if the background process was cancelled.
            if (e.Cancelled)
            {
                pcap_packet.Stop();
                //StopCapture();
                DisplayState("ץ��������\r\n");
                return;
            }

        }

        //Thread auotoStopThread = null;

        /******************************************************************************
           start the test single or loop, the main process call WebTesting() 
        /*******************************************************************************/
        public void StartServerTaskFunc()   //���ݷ�����������ն���������ն�������ִ�У�����������ȴ�
        {
            while (true)
            {
                if (!taskon)
                {
                    Log.Info("It's serverTask!");
                    this.btnFlvStart.Enabled = false;
                    this.btnFlvStop.Enabled = false;   //���������������ն�ֹͣ
                    this.taskon = true;

                    if (inis.IniReadValue("Flv", "urlPage").Equals(""))
                    {
                        DisplayState("�������Ƶ������ַ,���������ã���");
                        Log.Warn("�������Ƶ������ַ,����������!");
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

                    //��������δ��ʼ���ţ�Ҳ�������������л�û������
                    this.IsStartPlay = false;
                    //����ֹͣ���Եı�ʾλ
                    this.StartStopTest = false;

                    //ָ��������־�ļ���xls�ļ���ץ���ļ���
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
                    inis.IniWriteValue("LogResult", str, strLogResult);///������־+�޲ο����
                    ///
                    //�����һ�ε㿪ʼ����
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

        public void StartTerminalTaskFunc()   //���ݷ�����������ն���������ն�������ִ�У�����������ȴ�
        {
                    Log.Info("It's serverTask!");
                    this.btnFlvStart.Enabled = false;
                    this.btnFlvStop.Enabled = true;     //�ն����������ͣ
                    this.taskon = true;

                    if (inis.IniReadValue("Flv", "urlPage").Equals(""))
                    {
                        DisplayState("�������Ƶ������ַ,���������ã���");
                        Log.Warn("�������Ƶ������ַ,����������!");
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

                    //��������δ��ʼ���ţ�Ҳ�������������л�û������
                    this.IsStartPlay = false;
                    //����ֹͣ���Եı�ʾλ
                    this.StartStopTest = false;

                    //ָ��������־�ļ���xls�ļ���ץ���ļ���
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
                    inis.IniWriteValue("LogResult", str, strLogResult);///������־+�޲ο����
                    ///
                    //�����һ�ε㿪ʼ����
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
            //stop button������
            this.btnFlvStart.Enabled = true;
            this.btnFlvStop.Enabled = false;
            //����ֹͣ���Ա�ʶλ
            StartStopTest = true;

            //ֹͣ���ţ��ص����������˳�ץ�������š��ܵ�����
            this.StopClosePlayer();

            Thread.Sleep(500);

            inis.IniWriteValue("Flv", "counts", iTest.ToString());
            //���Ŵ�������
            iTest = 0;
            timer1.Stop();
            timer1.Enabled = false;

            //memoPcap��Ϣ���
            DateTime dtEnd = DateTime.Now;
            memoPcap.Items.Clear();

            //��ռ�⾲֡��֡ģ���ı���
            this.ClearGuageData();


            if (inis.IniReadValue("Flv", "Envir").Equals("web"))
            {
                //�޲ο�ģʽ�Ĵ�֣������ֽ��
                for (int i = 0; i <= port_list.Count; i++)
                {
                    //���������Ķ˿ں�д�������ļ������ְ�����ʹ��
                    //ushort port = (ushort)(port_list[i]);
                    //inis.IniWriteValue("port", "test" + (i + 1), Convert.ToString(port));

                    DateTime dtStart = (DateTime)(StartTimeList[i]);
                    StringBuilder strbFile = (StringBuilder)(strbFileList[i]);
                    //string strpcap = inis.IniReadValue("result", "test" + (i + 1));
                    string strpcap = inis.IniReadValue("Flv", "PcapFile");
                    TimeSpan ts = dtEnd - dtStart;
                    float ts2 = ts.Seconds + (float)ts.Milliseconds / 1000;

                    strbFile.Append("���Խ���,��ʱ " + ts.Minutes + "�� " + ts2.ToString() + "��" + "\r\n");
                    strbFile.Append("ץ���ļ�: " + strpcap + "\r\n");
                    DisplayState("ץ���ļ�: " + strpcap + "����\r\n");

                    //�Բ����ļ�������������
                    string strfScore = "qoe_score.txt";   //����ļ�ֻ�����������Ե�ʱ��Ż���
                    if (File.Exists(strfScore))     //ɾ��������һ�β��Ե�qoe_score.txt�ļ�
                    {
                        File.Delete(strfScore);
                    }
                    Thread.Sleep(500);

                    //���
                    try
                    {
                        double score = this.UnRefScore(i + 1, strfScore);      //�ɹ�����UnRefScore����������ɱ��β��Ե�qoe_score.txt�ļ�
                        StreamWriter ResultTmp = new StreamWriter(File.Create("ResultTxt.tmp"), Encoding.Default);
                        //��ʱ�ܽᱨ���ļ������������ض��ĸ�ʽѹ�����ݿ�
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
                            sr1.ReadLine();    //���Բ鿴qoe_score.txt�ĸ�ʽ
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
                            //�Ӿ�ȱ��-ָ������(100����):	98.60
                            //��ЧӦ-��Ƶ��������(100����):	47.80
                            int[] column2s = { 2, 2 };
                            for (int j = 0; j < column2s.Length; j++)
                            {
                                videoInfo = sr1.ReadLine();
                                infoNames = videoInfo.Split(':');
                                if (infoNames.Length == 2)
                                    ResultTmp.WriteLine((++resultIndex).ToString() + "\t" + infoNames[0] + infoNames[1]);
                            }
                            //�ۺ�����(100����):	63.04
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



                    //���Ž���,дxls�ļ�
                    strbFile.Append("���Ա��û��ж�\r\n");
                    //��ȡ����ģ�鴦����,д��xls�ļ�����
                    if (File.Exists(strfScore))       //�����β������ɵ�qoe_score.txt�е�����д��strbFile
                    {
                        strbFile.Append("\r\n");
                        FileStream fs1 = new FileStream(strfScore, FileMode.Open, FileAccess.Read);
                        StreamReader sr = new StreamReader(fs1, Encoding.Default);
                        strbFile.Append(sr.ReadToEnd());
                        sr.Close();
                        fs1.Close();
                    }

                    //д��xls(txt��ʽ)�ļ�
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
                    DisplayState("��־�ļ�:" + strLogResult + "���ɳɹ�\r\n");
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

                    strbFile.Append("���Խ���,��ʱ " + ts.Minutes + "�� " + ts2.ToString() + "��" + "\r\n");
                    //�Բ����ļ�������������
                    string strfScore = "qoe_score.txt";
                    if (File.Exists(strfScore))     //ɾ��������һ�β��Ե�qoe_score.txt�ļ�
                    {
                        File.Delete(strfScore);
                    }
                    Thread.Sleep(500);
                    //���
                    double score = this.UnRefScore(i + 1, strfScore);      //�ɹ�����UnRefScore����������ɱ��β��Ե�qoe_score.txt�ļ�

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
                        Log.Warn(string.Format("��������!����:{0}", score.ToString()));
                    }


                    //���Ž���,дxls�ļ�
                    strbFile.Append("���Ա��û��ж�\r\n");
                    //��ȡ����ģ�鴦����,д��xls�ļ�����
                    if (File.Exists(strfScore))       //�����β������ɵ�qoe_score.txt�е�����д��strbFile
                    {
                        strbFile.Append("\r\n");
                        FileStream fs1 = new FileStream(strfScore, FileMode.Open, FileAccess.Read);
                        StreamReader sr = new StreamReader(fs1, Encoding.Default);
                        strbFile.Append(sr.ReadToEnd());
                        sr.Close();
                        fs1.Close();
                    }

                    //д��xls(txt��ʽ)�ļ�
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
                    DisplayState("��־�ļ�:" + strLogResult + "���ɳɹ�\r\n");
                }
            }

            DateTime start = (DateTime)(StartTimeList[0]);
            TimeSpan timediff = dtEnd - start;
            float timediff2 = timediff.Seconds + (float)timediff.Milliseconds / 1000;
            DisplayState("���Խ���,��ʱ " + timediff.Minutes + "�� " + timediff2.ToString() + "��" + "\r\n");
            DisplayState("---------------�������---------------\r\n");

            port_list.Clear();
            player_list.Clear();
            StartTimeList.Clear();
            strbFileList.Clear();
            comboBox1.Items.Clear();
            comboBox1.Text = "";
            this.dataGridView1.Visible = true;
            //�������ڲ��Եı�ʾλ
            DoTest = false;
            //���ÿ�ʼֹͣ���Եı�ʶλ
            StartStopTest = false;
            //ֹͣ���ƣ�Ϊ�˲����ֶ����Զ���ͻ
            taskon = false;
        }


        public void StopTerminalTaskFunc()
        {
            //stop button������
            this.btnFlvStart.Enabled = true;
            this.btnFlvStop.Enabled = false;
            //����ֹͣ���Ա�ʶλ
            StartStopTest = true;

            //ֹͣ���ţ��ص����������˳�ץ�������š��ܵ�����
            this.StopClosePlayer();

            Thread.Sleep(500);

            inis.IniWriteValue("Flv", "counts", iTest.ToString());
            //���Ŵ�������
            iTest = 0;
            timer1.Stop();
            timer1.Enabled = false;

            //memoPcap��Ϣ���
            DateTime dtEnd = DateTime.Now;
            memoPcap.Items.Clear();

            //��ռ�⾲֡��֡ģ���ı���
            this.ClearGuageData();


            if (inis.IniReadValue("Flv", "Envir").Equals("web"))
            {
                //�޲ο�ģʽ�Ĵ�֣������ֽ��
                for (int i = 0; i <= port_list.Count; i++)
                {
                    //���������Ķ˿ں�д�������ļ������ְ�����ʹ��
                    //ushort port = (ushort)(port_list[i]);
                    //inis.IniWriteValue("port", "test" + (i + 1), Convert.ToString(port));

                    DateTime dtStart = (DateTime)(StartTimeList[i]);
                    StringBuilder strbFile = (StringBuilder)(strbFileList[i]);
                    //string strpcap = inis.IniReadValue("result", "test" + (i + 1));
                    string strpcap = inis.IniReadValue("Flv", "PcapFile");
                    TimeSpan ts = dtEnd - dtStart;
                    float ts2 = ts.Seconds + (float)ts.Milliseconds / 1000;

                    strbFile.Append("���Խ���,��ʱ " + ts.Minutes + "�� " + ts2.ToString() + "��" + "\r\n");
                    strbFile.Append("ץ���ļ�: " + strpcap + "\r\n");
                    DisplayState("ץ���ļ�: " + strpcap + "����\r\n");

                    //�Բ����ļ�������������
                    string strfScore = "qoe_score.txt";   //����ļ�ֻ�����������Ե�ʱ��Ż���
                    if (File.Exists(strfScore))     //ɾ��������һ�β��Ե�qoe_score.txt�ļ�
                    {
                        File.Delete(strfScore);
                    }
                    Thread.Sleep(500);

                    //���
                    try
                    {
                        double score = this.UnRefScore(i + 1, strfScore);      //�ɹ�����UnRefScore����������ɱ��β��Ե�qoe_score.txt�ļ�
                        StreamWriter ResultTmp = new StreamWriter(File.Create("ResultTxt.tmp"), Encoding.Default);
                        //��ʱ�ܽᱨ���ļ������������ض��ĸ�ʽѹ�����ݿ�
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
                            sr1.ReadLine();    //���Բ鿴qoe_score.txt�ĸ�ʽ
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
                            //�Ӿ�ȱ��-ָ������(100����):	98.60
                            //��ЧӦ-��Ƶ��������(100����):	47.80
                            int[] column2s = { 2, 2 };
                            for (int j = 0; j < column2s.Length; j++)
                            {
                                videoInfo = sr1.ReadLine();
                                infoNames = videoInfo.Split(':');
                                if (infoNames.Length == 2)
                                    ResultTmp.WriteLine((++resultIndex).ToString() + "\t" + infoNames[0] + infoNames[1]);
                            }
                            //�ۺ�����(100����):	63.04
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



                    //���Ž���,дxls�ļ�
                    strbFile.Append("���Ա��û��ж�\r\n");
                    //��ȡ����ģ�鴦����,д��xls�ļ�����
                    if (File.Exists(strfScore))       //�����β������ɵ�qoe_score.txt�е�����д��strbFile
                    {
                        strbFile.Append("\r\n");
                        FileStream fs1 = new FileStream(strfScore, FileMode.Open, FileAccess.Read);
                        StreamReader sr = new StreamReader(fs1, Encoding.Default);
                        strbFile.Append(sr.ReadToEnd());
                        sr.Close();
                        fs1.Close();
                    }

                    //д��xls(txt��ʽ)�ļ�
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
                    DisplayState("��־�ļ�:" + strLogResult + "���ɳɹ�\r\n");
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

                    strbFile.Append("���Խ���,��ʱ " + ts.Minutes + "�� " + ts2.ToString() + "��" + "\r\n");
                    //�Բ����ļ�������������
                    string strfScore = "qoe_score.txt";
                    if (File.Exists(strfScore))     //ɾ��������һ�β��Ե�qoe_score.txt�ļ�
                    {
                        File.Delete(strfScore);
                    }
                    Thread.Sleep(500);
                    //���
                    double score = this.UnRefScore(i + 1, strfScore);      //�ɹ�����UnRefScore����������ɱ��β��Ե�qoe_score.txt�ļ�

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
                        Log.Warn(string.Format("��������!����:{0}", score.ToString()));
                    }


                    //���Ž���,дxls�ļ�
                    strbFile.Append("���Ա��û��ж�\r\n");
                    //��ȡ����ģ�鴦����,д��xls�ļ�����
                    if (File.Exists(strfScore))       //�����β������ɵ�qoe_score.txt�е�����д��strbFile
                    {
                        strbFile.Append("\r\n");
                        FileStream fs1 = new FileStream(strfScore, FileMode.Open, FileAccess.Read);
                        StreamReader sr = new StreamReader(fs1, Encoding.Default);
                        strbFile.Append(sr.ReadToEnd());
                        sr.Close();
                        fs1.Close();
                    }

                    //д��xls(txt��ʽ)�ļ�
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
                    DisplayState("��־�ļ�:" + strLogResult + "���ɳɹ�\r\n");
                }
            }

            DateTime start = (DateTime)(StartTimeList[0]);
            TimeSpan timediff = dtEnd - start;
            float timediff2 = timediff.Seconds + (float)timediff.Milliseconds / 1000;
            DisplayState("���Խ���,��ʱ " + timediff.Minutes + "�� " + timediff2.ToString() + "��" + "\r\n");
            DisplayState("---------------�������---------------\r\n");

            port_list.Clear();
            player_list.Clear();
            StartTimeList.Clear();
            strbFileList.Clear();
            comboBox1.Items.Clear();
            comboBox1.Text = "";
            this.dataGridView1.Visible = true;
            //�������ڲ��Եı�ʾλ
            DoTest = false;
            //���ÿ�ʼֹͣ���Եı�ʶλ
            StartStopTest = false;
            //ֹͣ���ƣ�Ϊ�˲����ֶ����Զ���ͻ
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

            //���ͼ��
            this.InitChart();
            this.ClearGuageData();

            //��¼��β�����Ϣ                           
            DisplayState("--------------------------------\r\n");
            DisplayState("�� " + iTest + " �β���......\r\n");
            strbFile.Append("�� " + iTest + " �β���......\r\n");

            //����������
            try
            {
                if (!File.Exists(strPlayer))
                {
                    DisplayState("�����жϣ��޷��ҵ�������");
                    //����Ҳ�������������ô��ֱ���жϳ���
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

            //��ȡ������IP��Ϣ
            if (inis.IniReadValue("Flv", "Envir").Equals("web"))   //zc
            {
                DisplayState("����: " + inis.IniReadValue("Flv", "IpAddress"));
                strbFile.Append("����: " + inis.IniReadValue("Flv", "IpAddress") + "\r\n");
            }

            Thread.Sleep(100);
            DateTime dtStart = DateTime.Now;
            DisplayState("���Կ�ʼʱ��: " + dtStart.ToString());
            strbFile.Append("���Կ�ʼʱ��: " + dtStart.ToString() + "\r\n");
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

            if (inis.IniReadValue("Flv", "Envir").Equals("web"))    //ץ���ĺ�̨�߳�
            {
                if (!m_AsyncWorker_cap.IsBusy)
                {
                    m_AsyncWorker_cap.RunWorkerAsync();         //����bwAsync_cap_DoWork�¼�
                }
                Thread.Sleep(100);
            }

            //��ͨ�벥��ģ��Ĺܵ�
            if (!m_AsyncWorker_pipe.IsBusy)
            {
                m_AsyncWorker_pipe.RunWorkerAsync();         //����bwAsync_pipe_DoWork�¼�
                Thread.Sleep(delay);
            }
            pipeList.Add(m_AsyncWorker_pipe);

            //������Ƶ�ļ�������
            if (!m_AsyncWorker.IsBusy)                 //������Ƶ�ļ��ĺ�̨�߳�
            {
                m_AsyncWorker.RunWorkerAsync();       //����bwAsync_DoWork�¼�
            }
        }


        /******************************************************************************
           ������Ƶ�ļ�
        /*******************************************************************************/
        private void bwAsync_DoWork(object sender, DoWorkEventArgs e)       //��ɺ�����bwAsync_RunWorkerCompleted
        {                                                                                                             //��Ƶ�����߳�
            ////����url��ַ�����ڴ������ݸ�vlc
            string strfplay = null;
            if (inis.IniReadValue("Flv", "Envir").Equals("web"))
            {
                strfplay = inis.IniReadValue("Flv", "urlPage");     //������ʲôie��ַ������ʵ��ַ������urlPage��
            }


            //д�벥��������ʵ��ַ
            string keyname = "relurl" + iTest;
            inisvlc.IniWriteValue("URL", keyname, strfplay);
            Thread.Sleep(500);

            //ע���˴�ֱ�Ӵ�strPlayer��ȡ��������������ȷ�ģ���Ϊ��set�����У���web��rtspѡ���л�ʱ(���õ��ȷ��)
            //���Ѿ���"Flv""Envir"�޸��ˣ����ڴ����õ����Ź��̵��л�ʱ����ִ��Init(),������strPlayer�ͻᱻ�޸�
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
                    DisplayState("ҳ��: " + inis.IniReadValue("Flv", "urlPage"));
                    strbFile.Append("ҳ��: " + inis.IniReadValue("Flv", "urlPage") + "\r\n");
                }

                //����vlc
                ps.Start();

                if (ps.WaitForInputIdle())
                {
                    while (ps.MainWindowHandle.ToInt32() == 0)
                    {
                        Thread.Sleep(100);
                        ps.Refresh();//����ˢ��״̬�������»��
                        ps.StartInfo = psi;
                    }

                    ShowWindow(ps.MainWindowHandle, 5);
                    SetParent(ps.MainWindowHandle, this.PanelVI.Handle);
                    MoveWindow(ps.MainWindowHandle, -19, -41, 522, 475, true);

                    this.splitContainerControl1.Panel1.Refresh();
                    player_list.Add(ps);
                    string player = "������" + iTest;
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
                DisplayState("����������");
                return;
            }

            // Check to see if the background process was cancelled.
            if (e.Cancelled)
            {
                DisplayState("������");
                return;
            }
        }

        /******************************************************************************
           close the same player before a test start ֹͣץ���Ͳ���
        /*******************************************************************************/
        public void StopClosePlayer()
        {
            if (strPlayer == "")
                return;
            //���ǵ�VI�������кܶ��쳣����û�����,������ʱ���ܻᱨ��,�Ƽ�ͨ��ֱ��ɱ�������˳�
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

            //ȡ�������߳�
            Thread.Sleep(2000);
            this.m_AsyncWorker.CancelAsync();
            this.m_AsyncWorker.Dispose();

            //ȡ���ܵ��߳�
            BackgroundWorker bw;
            for (int i = 0; i < pipeList.Count; i++)
            {
                bw = (BackgroundWorker)(pipeList[i]);
                bw.CancelAsync();
                bw.Dispose();
            }
            pipeList.Clear();

            //ֹͣץ��
            if (DoTest && (inis.IniReadValue("Flv", "Envir").Equals("web")))
            {
                //StopCapture();
                pcap_packet.Stop();
            }
            //ȡ��ץ���߳�
            this.m_AsyncWorker_cap.CancelAsync();
            this.m_AsyncWorker_cap.Dispose();
        }

        /******************************************************************************
            call cmd: ipconfig/flushdns ����DNS��¼�Ա���ץ������
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
         *                      �����޲ο����ģ��
         * ***********************************************************************/
        public double UnRefScore(int i, string strfOut)
        {
            double score = 0;

            string strfIn = inisvlc.IniReadValue("result", "test" + i);  //���벥����������־�ļ�
            //string resolution = inisvlc.IniReadValue("resolution", "test" + i);
            //int n = resolution.IndexOf("*");
            //int width = Convert.ToInt32(resolution.Substring(0, n));                 //��Ƶ���
            //int height = Convert.ToInt32(resolution.Substring(n + 1, resolution.Length - n - 1));                 
            //��Ƶ�߶ȣ����ڴ��ģ��ߴ�ֻ������352*288,640*480,720*576�����Դ˴�ֻ���������ƣ�352*272~~352*288��
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

            //�ĳ�֡���
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

        //public static Thread serverThread = null;  //���ڽ���Զ�̿��ƶ˷��͵����ò���
        //TcpListener mylsn;      //���������� 
        public static Socket mysock;          //�������׽���

        string destIp = "127.0.0.1";           //Ŀ��ip��ַ
        int destPort = 8002;                   //Ŀ�Ķ˿�
        string localIp = "127.0.0.1";          //����Ip
        int localPort = 8001;                  //���ض˿�

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

        public static TcpClient playClient;   //�ͻ��ˣ���������������䲥������ʵʱ����
        DateTime startTime;     //��ʼ����ʱ��
        //DateTime endTime;       //ֹͣ����ʱ��
        //static int playTime;           //����ʱ��
        bool vlcStart = false;

        delegate void deleScore(string s);

        // ��ʱ��timer1 ÿ�봥��һ�Σ�������ⲥ����������Ƶʹ�õĶ˿ں�
        private void timer1_Elapsed(object sender, System.Timers.ElapsedEventArgs e)    //1s��ʱ����������ȡ�˿ں�
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

