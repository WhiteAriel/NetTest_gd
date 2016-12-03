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
    //��Ҫȷ���������˵��ڴ�����ֽ���
    [StructLayoutAttribute(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct ParaStuct
    {
        public ulong videotime;		//��Ƶʱ��
        public int systime;			//ϵͳʱ��
        public int still; 				//��֡	
        public int blur;				//ģ��
        public int skip;				//��֡
        public int black;				//�ڳ�
        public int definition;			//������
        public int brightness;			//����
        public int chroma;				//ɫ��
        public int saturation;			//���Ͷ�
        public int contraction;		//�Աȶ�
        public int dev;				///��׼��
        public int entro;				//��
        public double block;			//��ЧӦֵ
        public double highenerge;		//��Ƶ����
    };  


    public partial class FlvTest : DevExpress.XtraEditors.XtraUserControl
    {
        public static IniFile inis = new IniFile(Application.StartupPath + "\\settings.ini");  //ini class
        public static IniFile inisvlcout = new IniFile(Application.StartupPath + "\\vlc.ini"); //��һЩ����Ϊ�йأ�������Ĭ�϶�\\VideoPlayer\\vlc.ini�����ֶ�\\vlc.ini
        public static IniFile inisvlc = new IniFile(Application.StartupPath + "\\VideoPlayer\\vlc.ini"); //ini class
        //IniFile inisref = new IniFile(Application.StartupPath + "\\RefTool" + "\\referencesetup.ini");

        public volatile  bool taskon = false;    //��ʾ����û������
        public volatile bool serverTest = false;   //��ʾִ�е��Ƿ������������ն��Լ�������

        private int iTest = 0;              //���������˶��ٴ�
        private static int intCheckContinuous;     //�Ƿ���������
        private static int iNumContinuous = 0;     //���������ܴ���

        public string strPlayer;            //����������������·����  
        public string strPcapFile = "";     //ץ���ļ���
        public int iDevice = 0;                 //��������
        public int lastPlayerIndex = 0;
        public LibPcapLiveDevice device;
        private DateTime StartTime =new DateTime();         //��ʼ���Ե�ʱ��

        private string strPlayFile;         //qoe�ļ�
        private string strLogResult = null;
        private string strXlsLogFile;       //log file path (xls file(xls��ʽ) path)
        private StringBuilder strbFile = new StringBuilder();    //contents of log file (content of xls file)
        private StringBuilder ScoreParam = new StringBuilder();  //�ٷ��Ʋ�����ȡ

        public static volatile bool DoTest = false;

        private static PacketCap pcap_packet;

        private MySQLInterface mysqlTest = null;
        private bool mysqlTestFlag = false;

        //�¼�������������
        private AutoResetEvent videoEndEvent = new AutoResetEvent(true); 
        private int videoHandle = 0;  //StartPlay�����ľ������Ҫ�������ֹͣ 

        //ץ������
        private BackgroundWorker m_AsyncWorker_cap = new BackgroundWorker();

        Queue<ParaStuct> paraQue = new Queue<ParaStuct>();
        object queLock = new object();

        //д�ļ���������
        //FileStream scoreInfile = null;
        //StreamWriter scoreInWriter = null; 

        //�����߳�
        Thread paraShowThread = null;

        //ö�ٱ�ʾ�û������������������쳣
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


        //��������װ
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

            //�������Զ��庯���ľ�������BackgroundWorker��DoWork��RunWorkerCompleted�¼�

            m_AsyncWorker_cap.WorkerSupportsCancellation = true;
            m_AsyncWorker_cap.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bwAsync_cap_RunWorkerCompleted);
            m_AsyncWorker_cap.DoWork += new DoWorkEventHandler(bwAsync_cap_DoWork);

            Control.CheckForIllegalCrossThreadCalls = false;
            DoTest = false;

            videoEndEvent.Reset();

            //���ݿ�����ʼ��
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
            if (iDevice < 0)
            {
                iDevice = 0;
            }
            pcap_packet = new PacketCap(iDevice);

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

        /******************************************************************************
           start the test single or loop, the main process call WebTesting() 
        /*******************************************************************************/
        public int StartServerTaskFunc()   //����ն�������ִ�У�����������ȴ�
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
                        return -1;
                    }

                    this.iTest++;
                    frameNum = 1;

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
                    inisvlc.IniWriteValue("Flv", str, strPlayFile);
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
            //ֹͣ���ţ��ص����������˳�ץ�������š��ܵ�����
            this.StopClosePlayer();

            Thread.Sleep(500);

            inis.IniWriteValue("Flv", "counts", iTest.ToString());
            //���Ŵ�������
            iTest = 0;

            //memoPcap��Ϣ���
            DateTime EndTime = DateTime.Now;
            memoPcap.Items.Clear();

            //��ռ�⾲֡��֡ģ���ı���
            this.ClearGuageData();

            TimeSpan ts = EndTime - StartTime;
            float ts2 = ts.Seconds + (float)ts.Milliseconds / 1000;

            //�޲ο�ģʽ�Ĵ�֣������ֽ��
            string strpcap = inis.IniReadValue("Flv", "PcapFile");
            strbFile.Append("���Խ���,��ʱ " + ts.Minutes + "�� " + ts2.ToString() + "��" + "\r\n");
            strbFile.Append("ץ���ļ�: " + strpcap + "\r\n");
            DisplayState("ץ���ļ�: " + strpcap + "����\r\n");

            //�������ڲ��Եı�ʾλ
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
                double score = this.UnRefScore(strfScore);      //�ɹ�����UnRefScore����������ɱ��β��Ե�qoe_score.txt�ļ�
                StreamWriter ResultTmp = new StreamWriter(File.Create("ResultTxt.tmp"), Encoding.Default);
                //��ʱ�ܽᱨ���ļ������������ض��ĸ�ʽѹ�����ݿ�
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
                    this.dataGridView1.Rows[index].Cells[0].Value =1;
                    this.dataGridView1.Rows[index].Cells[1].Value = inis.IniReadValue("Flv", "Envir");
                    this.dataGridView1.Rows[index].Cells[2].Value = 0;
                }

            }
            catch (System.Exception ex)
            {
                Log.Error(Environment.StackTrace, ex);
            }

            //���Ž���,дxls�ļ�
            strbFile.Append("���Խ���\r\n");
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
            DisplayState("���Խ���,��ʱ " + ts.Minutes + "�� " + ts.ToString() + "��" + "\r\n");
            DisplayState("---------------�������---------------\r\n");

            comboBox1.Items.Clear();
            comboBox1.Text = "";
            this.dataGridView1.Visible = true;
            //ֹͣ���ƣ�Ϊ�˲����ֶ����Զ���ͻ
            taskon = false;
            //stop button������
            this.btnFlvStart.Enabled = true;
            this.btnFlvStop.Enabled = false;
        }

        private void btnFlvStart_Click(object sender, EventArgs e)
        {
            StartTerminalTaskFunc();
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
                    frameNum = 1;

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
                    int ret=this.FlvTesting();
                    if (ret < 0)  //�����쳣
                    {
                        switch (ret)
                        {
                            case -19996:
                                //���Ӵ���ע
                                 DisplayState("��Ч�Ĳ����ʲ���");
                                break;
                            case -19997:
                                //���Ӵ���ע
                                DisplayState("��Ч�Ĳ���");
                                break;
                            case -19998:
                                //���Ӵ���ע
                                DisplayState("��������������ʧ��");
                                break;
                            case -19999:
                                    //���Ӵ���ע
                                    DisplayState("û�п�ִ������");
                                    break;
                            case -20000:
                                    //���Ӵ���ע
                                    DisplayState("��URL����");
                                    break;
                            case -20001:
                                    //���Ӵ���ע
                                    DisplayState("����Ƶ������");
                                    break;
                            case -20002:
                                    //���Ӵ���ע
                                    DisplayState("û�п�����Ƶ��");
                                    break;
                            case -20003:
                                    //���Ӵ���ע
                                    DisplayState("�Ҳ���������");
                                    break;
                            case -20004:
                                    //���Ӵ���ע
                                    DisplayState("�޷��򿪱�����");
                                    break;
                            case -20005:
                                    //���Ӵ���ע
                                    DisplayState("�����SDL���");
                                    break;
                            default:
                                    DisplayState("�����쳣");
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
            //ֹͣ���ţ��ص����������˳�ץ�������š��ܵ�����
            this.StopClosePlayer();
            Thread.Sleep(500);
            //�������ڲ��Եı�ʾλ
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
            //���Ŵ�������
            iTest = 0;
            //memoPcap��Ϣ���
            DateTime EndTime = DateTime.Now;
            memoPcap.Items.Clear();
            //��ռ�⾲֡��֡ģ���ı���
            this.InitChart();
            this.ClearGuageData();
            TimeSpan ts = EndTime - StartTime;
            float ts2 = ts.Seconds + (float)ts.Milliseconds / 1000;
            //�޲ο�ģʽ�Ĵ�֣������ֽ��
            string strpcap = inis.IniReadValue("Flv", "PcapFile");
            strbFile.Append("���Խ���,��ʱ " + ts.Minutes + "�� " + ts2.ToString() + "��" + "\r\n");
            strbFile.Append("ץ���ļ�: " + strpcap + "\r\n");
            DisplayState("ץ���ļ�: " + strpcap + "����\r\n");

            //�Բ����ļ�������������
            string strfScore = "qoe_score.txt";   //����ļ�ֻ�����������Ե�ʱ��Ż���
            if (File.Exists(strfScore))     //ɾ��������һ�β��Ե�qoe_score.txt�ļ�
            {
                File.Delete(strfScore);
            }
            //���
            try
            {
                Thread.Sleep(300);
                double score = this.UnRefScore(strfScore);      //�ɹ�����UnRefScore����������ɱ��β��Ե�qoe_score.txt�ļ�
                StreamWriter ResultTmp = new StreamWriter(File.Create("ResultTxt.tmp"), Encoding.Default);
                //��ʱ�ܽᱨ���ļ������������ض��ĸ�ʽѹ�����ݿ�
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
                    this.dataGridView1.Rows[index].Cells[0].Value = 1;
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
            DisplayState("���Խ���,��ʱ " + ts.Minutes + "�� " + ts.ToString() + "��" + "\r\n");
            DisplayState("---------------�������---------------\r\n");

            comboBox1.Items.Clear();
            comboBox1.Text = "";
            this.dataGridView1.Visible = true;
            //ֹͣ���ƣ�Ϊ�˲����ֶ����Զ���ͻ
            taskon = false;
            //stop button������
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
                        //���chart����
                        chart1.Invoke(addDataDel, this.chart1, (ps.definition ));    //������
                        chart2.Invoke(addDataDel, this.chart2, (ps.brightness ));    //����
                        chart3.Invoke(addDataDel, this.chart3, (ps.chroma));        //ɫ��
                        chart4.Invoke(addDataDel, this.chart4, (ps.saturation ));    //���Ͷ�
                        chart5.Invoke(addDataDel, this.chart5, (ps.contraction ));   //�Աȶ�
                        frameNum++;
                        //���gauge data,0/1ȡֵ
                        gaugeContainer1.Values["Default"].Value = ps.still * 80;
                        gaugeContainer2.Values["Default"].Value = ps.skip * 80;
                        gaugeContainer3.Values["Default"].Value = ps.blur * 80;

                        Log.Console(String.Format("{0},{1},{2},{3},{4}", ps.brightness, ps.contraction, ps.still, ps.skip, ps.blur));
                        videoPara vp = new videoPara(ps.still, ps.blur, ps.skip, ps.black, ps.definition, ps.brightness, ps.chroma, ps.saturation, ps.contraction, ps.dev, ps.entro, ps.block, ps.highenerge, -1);

                        //scoreInWriter.WriteLine(String.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}\t{14}\t{15}\t{16}\t{17}\t{18}\t{19}", ps.systime, ps.videotime, ps.still, ps.skip, ps.blur, ps.black, ps.definition, ps.brightness, ps.chroma, ps.saturation, ps.contraction, ps.dev, ps.dev, ps.block, ps.entro, ps.highenerge, ps.highenergeLU, ps.highenergeRU, ps.highenergeLD, ps.highenergeRD));
                        //��¼����mysql����VideoPara
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
                        DisplayState("��⵽������֡");
                        
                    }
                }
                else if (callbackType == -20001)  //ERROR_STREAM_EXCEPTION
                {
                    user_act = USER_ACT.STREAM_EXC;
                    if (serverTest)
                        videoEndEvent.Set();
                    else
                    {
                        DisplayState("ȡ֡�쳣,�������˳�");
                       
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
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] //�ص����ߵĵ���Լ����C#Ĭ��stdcall��C++��Ĭ�ϵ�cdcall
        public delegate void VideoCallBack(IntPtr para, int callBackType,IntPtr user_data); //���岥����ί��
        public VideoCallBack vcb;
        private int  FlvTesting()
        {
            DoTest = true;
            //���ͼ��
            this.InitChart();
            this.ClearGuageData();

            //��¼��β�����Ϣ                           
            DisplayState("--------------------------------\r\n");
            DisplayState("�� " + iTest + " �β���......\r\n");
            strbFile.Append("�� " + iTest + " �β���......\r\n");

            //��ȡ������IP��Ϣ

            DisplayState("����: " + inis.IniReadValue("Flv", "IpAddress"));
            strbFile.Append("����: " + inis.IniReadValue("Flv", "IpAddress") + "\r\n");
 

            Thread.Sleep(100);
            StartTime = DateTime.Now;
            DisplayState("���Կ�ʼʱ��: " + StartTime.ToString());
            strbFile.Append("���Կ�ʼʱ��: " + StartTime.ToString() + "\r\n");

            //Open the device for capturing
            //true -- means promiscuous mode
            //1000 -- means a read wait of 1000ms
            int capTimeOut = Convert.ToInt32(inis.IniReadValue("Flv", "captimeout"));
            int delay = Convert.ToInt32(inis.IniReadValue("Flv", "delay"));

            //���ò������ӿڣ�ͬʱ�ڻص��ﴦ���صĲ���
            ////����url��ַ�����ڴ������ݸ�vlc
            string strfplay="";
            strfplay = inis.IniReadValue("Flv", "urlPage");     //������ʲôie��ַ������ʵ��ַ������urlPage��
            DisplayState("����url: " + strfplay);
            strbFile.Append("����url: " + strfplay + "\r\n");

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
                                      
                    if (inis.IniReadValue("Flv", "Envir").Equals("web"))    //ץ���ĺ�̨�߳�
                    {
                        if (!m_AsyncWorker_cap.IsBusy)
                        {
                            m_AsyncWorker_cap.RunWorkerAsync();         //����bwAsync_cap_DoWork�¼�
                        }
                        Thread.Sleep(100);
                        paraShowThread = new Thread(videoParaShow);
                        paraShowThread.Start();
                        Thread listenTerminentThread = new Thread(ListenTerminent);
                        listenTerminentThread.Start();
                    }
                    return 0;
                }
                else    //�����쳣
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
            DisplayState("�����쳣\r\n");
            //stop button������
            this.btnFlvStart.Enabled = true;
            this.btnFlvStop.Enabled = false;
            comboBox1.Items.Clear();
            comboBox1.Text = "";
            //��ռ�⾲֡��֡ģ���ı���
            this.InitChart();
            this.ClearGuageData();
            //�������ڲ��Եı�ʾλ
            DoTest = false;
            //ֹͣ���ƣ�Ϊ�˲����ֶ����Զ���ͻ
            taskon = false;
        }

        /******************************************************************************
           close the same player before a test start ֹͣץ���Ͳ���
        /*******************************************************************************/
        public void StopClosePlayer()
        {
            //������Ҫ�����Ƿ���Ҫֹͣ
            if (user_act != USER_ACT.STREAM_END && user_act != USER_ACT.STREAM_EXC)
            {
                StopPlay(videoHandle);                
            }
            user_act = USER_ACT.DEFAULT;
            this.videoPictureBox.Visible = false;
            //ֹͣץ��
            pcap_packet.Stop();
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
        public double UnRefScore(string strfOut)
        {
            double score = 0;

            string strfIn = inisvlc.IniReadValue("result", "test1");  //���벥����������־�ļ�
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

