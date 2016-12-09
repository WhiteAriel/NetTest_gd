using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Management;
using System.Net.Mail;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;
using System.Threading;
using Microsoft.Win32;
using SharpPcap;
using PacketDotNet;
using SharpPcap.LibPcap;
using ParseandBuildJson;
using System.Net;
using MultiMySQL;
using RC.Core.Sockets;
using NetLog;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using Newtonsoft.Json;


namespace NetTest
{
    public partial class frmMain : Form
    {
        static IniFile inis = new IniFile(Application.StartupPath + "\\settings.ini");
        IniFile inivlc = new IniFile(Application.StartupPath + "\\vlc.ini");
        IniFile indll = new IniFile(Application.StartupPath + "\\net.dll");

        //�������,���ߵ��ٶȲ�һ����ѡ��ֿ�
        Queue<TaskItems> videoTaskQue = new Queue<TaskItems>(); //��Ƶ����������
        Queue<TaskItems> webTaskQue = new Queue<TaskItems>();   //��ҳ��������
        object videoQueLocker = new object();
        object webQueLocker = new object();

        string serverIp = null;
        int recycle = 0;
        // bool pause = false;
        //���ݿ�
        private MySQLInterface mysqlInit = null;
        private bool mysqlFlag = false;   //���ݿ��ʼ����־
        //�����߳�
        ManualResetEvent mEvent = new ManualResetEvent(true);
        StackTrace st = new StackTrace(new StackFrame(true));

        //��һ��ɨ������
        bool firstScan = true;
        private string pythonPath = @"findIeUrl2.py";
        private ScriptEngine taskEngine = null;
        private ScriptScope taskScope = null;

        //������ʵ��ַ�̷߳���ֵ
        int parseThreadRet = -2;
        //������ʵ��ַ�߳�ʹ�õ���ʱ��ַ
        string parseThreadUrlTem = "";

        public frmMain()
        {
            InitializeComponent();
            mysqlInit = new MySQLInterface(inis.IniReadValue("Mysql", "serverIp"), inis.IniReadValue("Mysql", "user"), inis.IniReadValue("Mysql", "passwd"), inis.IniReadValue("Mysql", "dbname"),inis.IniReadValue("Mysql", "port")) ;
            if (mysqlInit.MysqlInit(inis.IniReadValue("Mysql", "dbname")))
            {
                mysqlFlag = true;
                Log.Info(string.Format("���ݿ��ʼ���ɹ�!IP:{0};User:{1};Passwd:{2};Port:{3};DBName:{4}", inis.IniReadValue("Mysql", "serverIp"), inis.IniReadValue("Mysql", "user"), inis.IniReadValue("Mysql", "passwd"), inis.IniReadValue("Mysql", "port"), inis.IniReadValue("Mysql", "dbname")));
            }
            else
            {
                mysqlFlag = false;
                Log.Info(string.Format("���ݿ��ʼ��ʧ��!IP:{0};User:{1};Passwd:{2};Port:{3};DBName:{4}", inis.IniReadValue("Mysql", "serverIp"), inis.IniReadValue("Mysql", "user"), inis.IniReadValue("Mysql", "passwd"), inis.IniReadValue("Mysql", "port"), inis.IniReadValue("Mysql", "dbname")));
                Log.Error(string.Format("���ݿ��ʼ��ʧ��!IP:{0};User:{1};Passwd:{2};Port:{3};DBName:{4}", inis.IniReadValue("Mysql", "serverIp"), inis.IniReadValue("Mysql", "user"), inis.IniReadValue("Mysql", "passwd"), inis.IniReadValue("Mysql", "port"), inis.IniReadValue("Mysql", "dbname")));
            }
            //����python�����ȡ��ʵ��ַ
            try
            {
                Log.Info("��ʼ��python engine.");
                taskEngine = Python.CreateEngine();
                taskScope = taskEngine.CreateScope();
            }
            catch (System.Exception ex)
            {
                Log.Info("��ʼ��python engine�쳣,��鿴error��־");
                Log.Console(Environment.StackTrace, ex);
                Log.Error(Environment.StackTrace, ex);
            }
            
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            //������ʹ������
            String registerTime;

            //��ȡע������û�о�д��ע���������ڼ���ȡע���ʱ���뵱ǰʱ��Ա�
            try
            {
                //��ȡע�����ʱ��
                registerTime = (String)Registry.GetValue("HKEY_CURRENT_USER\\RegistryTime", "regTime", "");
                if (registerTime == null)
                {
                    //�״�ʹ�����
                    Registry.SetValue("HKEY_CURRENT_USER\\RegistryTime", "regTime", DateTime.Now.ToString("yyyy-MM-dd"), RegistryValueKind.String);
                    this.textBox1.Text = DateTime.Now.ToString("yyyy-MM-dd");
                    MessageBox.Show("��ӭʹ�ñ����,������30��", "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    //this.Text = "�ƶ�������ҵ����������ϵͳ ���ð棨30�죩";
                    Log.Info("��ӭʹ�ñ����,������30��!");
                }
                else
                {
                    //���״�ʹ�ñ����
                    DateTime endTime = DateTime.Now;   //��ǰʱ��
                    DateTime startTime = Convert.ToDateTime(registerTime);
                    TimeSpan span = endTime.Subtract(startTime);
                    if (span.Days > 30)
                    {
                        //MessageBox.Show("���ʹ��ʱ�޹��ڣ����ȡ�����棡", "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        Log.Info("���ʹ��ʱ�޹��ڣ����ȡ�����棡");
                        //return;
                    }
                }
                //this.Text = "�ƶ�������ҵ����������ϵͳV1.0 ���ð棨30�죩";

            }
            catch (Exception ex)
            {
                Log.Info("����ע����쳣,��鿴error��־.");
                Log.Console(Environment.StackTrace, ex);
                Log.Error(Environment.StackTrace, ex);
                MessageBox.Show("ע�������쳣,�����˳�!");
                this.Close();
            }

            if (mysqlFlag)
            {
                mysqlInit.CreatTaskList();
                mysqlInit.CreatDelayJitter();
                mysqlInit.CreatDNSAnalysis();
                mysqlInit.CreatFrameLengthAnalysis();
                mysqlInit.CreatHttpAnalysis();
                mysqlInit.CreatInOutAnalysis();
                mysqlInit.CreatTestReport();
                if (string.IsNullOrEmpty(mysqlInit.errorInfo))
                    Log.Info("���ݿ����б�񴴽��ɹ�!");
                else
                {
                    Log.Info("���ݿⲿ�ֱ�񴴽�ʧ��!");
                    Log.Error(mysqlInit.errorInfo);
                }
            }

            //����������ʱ�����뱣֤���㲥��Ĳ�����������ɱ��������
            //string strProcessFile = "VLCDialog";
            //Process[] pro = Process.GetProcessesByName(strProcessFile);
            //foreach (Process ProCe in pro)
            //{
            //    ProCe.Kill();
            //    Thread.Sleep(100);
            //}
            ////�������û��ɱ�����Ǿ�ֱ���˳�������
            //if (pro.Length > 0)
            //{
            //    Log.Error("�����������޷��رգ�����˳���");
            //    Application.Exit();
            //}
            string strPcap = System.Environment.GetFolderPath(Environment.SpecialFolder.System) + "\\wpcap.dll";
            if (!File.Exists(strPcap))
            {
                MessageBox.Show("���б�����ǰ���Ȱ�װWinPcap��", "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Log.Info("δ��װWinPcap!����˳���");
                Log.Error("δ��װWinPcap!����˳���");
                Application.Exit();
            }
            string cpuInfo = "";
            ManagementClass cimobject = new ManagementClass("Win32_Processor");
            ManagementObjectCollection moc = cimobject.GetInstances();
            foreach (ManagementObject mo in moc)
            {
                cpuInfo = mo.Properties["ProcessorId"].Value.ToString();
            }
            string strPC1 = indll.IniReadValue("ID", "new1");
            string strPC2 = indll.IniReadValue("ID", "new2");
            string strPC3 = indll.IniReadValue("ID", "new3");
            if ((strPC1 == "") || (strPC1 == cpuInfo))
            {
                indll.IniWriteValue("ID", "new1", cpuInfo);
            }
            else
            {
                if ((strPC2 == "") || (strPC2 == cpuInfo)) { indll.IniWriteValue("ID", "new2", cpuInfo); }
                else
                {
                    if ((strPC3 == "") || (strPC3 == cpuInfo)) { indll.IniWriteValue("ID", "new3", cpuInfo); }
                    else
                    {
                        //if ((strPC1 != cpuInfo) && (strPC2 != cpuInfo) && (strPC3 != cpuInfo))
                        //{
                        //MessageBox.Show("����ʹ�÷�Χ,�����˳�");
                        //this.Dispose();
                        //}
                    }
                }
            }

            var devices = LibPcapLiveDeviceList.Instance;
            if (devices.Count < 1)
            {
                MessageBox.Show("δ������Ч����,�����˳���\r\n��������ΪWireSharkû�а�װ�ɹ����µģ�", "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Log.Info("δ������Ч����,�����˳�����������ΪWireSharkû�а�װ�ɹ����µģ�");
                Log.Error("δ������Ч����,�����˳�����������ΪWireSharkû�а�װ�ɹ����µģ�");
                Application.Exit();
            }

            Process[] p = Process.GetProcessesByName("NetaTest.exe");
            if (p.Length > 0)
            {
                for (int i = 0; i < p.Length; i++)
                {
                    p[i].CloseMainWindow();
                    p[i].Kill();
                }
            }

            if (inis.IniReadValue("All", "ShowTips") != "0")
            {
                frmTips tips = new frmTips();
                tips.ShowDialog();
            }

            //20161016 socket�����߳�
            //Thread socketThread = new Thread(SocketFunc);
            //socketThread.Start();
            TcpSocketConfig serverConfig = new TcpSocketConfig();
            //serverConfig.Ip = "192.168.50.101";   //��TcpSocketServer�������˴������û��ָ��ip�ͼ�������������ip��ָ���˿�
            serverConfig.Port = Int32.Parse(inis.IniReadValue("Task", "ListenPort"));
            serverConfig.ReadCompleteCallback = serverReadCompleteFunc;
            serverConfig.ReadExceptionCallback = serverReadExceptionFunc;
            try
            {
                Log.Info("Tcp socket����������.......");
                new TcpSocketServer(serverConfig).StartListening();
            }
            catch (System.Exception ex)
            {
                Log.Info("Tcp socket�����������쳣,��鿴error��־");
                Log.Console(Environment.StackTrace, ex);
                Log.Error(Environment.StackTrace, ex);
            }


            //20161029  task�߳�
            Thread videoTaskThread = new Thread(videoTaskFunc);
            videoTaskThread.Start();
            Thread webTaskThread = new Thread(webTaskFunc);
            webTaskThread.Start();
           
            //����״̬�ϴ��̣߳�����Ϊ��ͬ��״̬��=ִ��״̬��
            Thread statusUploadThread = new Thread(statusUploadFunc);
            statusUploadThread.Start();
            //����ɨ���߳�,����Ϊ��ִ��״̬=�յ�1������¼���ڲ�ͬ�Ķ�����
            Thread taskScanThread = new Thread(taskScanFunc);
            taskScanThread.Start();

            this.flvSetting1.Visible = false;
            this.webSettings1.Visible = false;
            this.webTest1.Visible = false;
            this.webAnalyse1.Visible = false;
            flvWebAnalyze1.Visible = false;
            this.flvTest1.Visible = false;
            this.flvTest1.Visible = true;
            this.flvTest1.Dock = DockStyle.Fill;
            this.flvTest1.Init();
            this.flvWebAnalyze1.Init();
            //this.flvSetting1.Init(); 
            lbText.Text = "��ý�����...";
        }

        public void serverReadCompleteFunc(string taskJson)   //�ص�����������json��������ѹ�����ݿ�
        {
            List<AttributeJson> taskList = new List<AttributeJson>();
            int sign = OperateJson.ParseJson(taskJson, ref taskList, ref recycle, ref serverIp);//�����ӷ������˽��յ�json���ݰ�

            // sign ��־�ӷ������˷��������ݵĸ�ʽ����ȷ�� 0��ʾ�գ�1��ʾ��ʽ��ȷ��2��ʾ��ʽ����
            switch (sign)
            {
                case 1://�������ݣ������������json��ʽ�����ݣ���������д������
                    {
                        //Log.Info("���յ������json����.");
                        if (mysqlFlag)
                        {
                            for (int listcount = 0; listcount < taskList.Count; listcount++)
                            {
                                Log.Info("������json��:BatchNo:" + taskList[listcount].BatchNo + "#TaskId:" + taskList[listcount].Id + "#Type:" + taskList[listcount].Type + "#UrlType:" + taskList[listcount].UrlType + "#Url:" + taskList[listcount].Url + "#ServerIp:" + serverIp);
                                mysqlInit.TaskListInsertMySQL(taskList[listcount].BatchNo + "#" + taskList[listcount].Id + "#" + taskList[listcount].Type + "#" + taskList[listcount].UrlType + "#" + taskList[listcount].Url + "#" + serverIp);
                            }
                        }
                        taskList.Clear();
                        break;
                    }
                case 220:   //start task
                    {
                        mEvent.Set();
                        Log.Info("��������.");
                        break;
                    }
                case 221:   //stop task
                    {
                        mEvent.Reset();
                        Log.Info("��������.");
                        break;
                    }
                case 0:
                case 3:
                case 300:
                default:
                    Log.Info("Json�����������쳣����鿴info��־�н��յ����ַ�����ʽ.");
                    Log.Warn("Json�����������쳣����鿴info��־�н��յ����ַ�����ʽ.");
                break;
            }
        }

        public void serverReadExceptionFunc(Exception ex)
        {
            Log.Error(Environment.StackTrace, ex);
        }

        void webTaskFunc()
        {
            Log.Info("web����ɨ��ִ���߳̿���!");
            TaskItems webTask = null;
            while (true)
            {
                mEvent.WaitOne();   //��һֱ���ţ�������SocketFunc�л�ȡ����ͣ��ָ�����reset
                int countWeb = 0;
                lock (webQueLocker)
                {
                    countWeb = webTaskQue.Count;
                }
                if (countWeb > 0)
                {
                    lock (webQueLocker)
                    {                        
                        webTask = webTaskQue.Dequeue();
                        Log.Info("web����ɨ��ִ���߳�ȡ������:Url:" + webTask.taskUrl+"#ServerIp:"+webTask.serverIp);
                    }
                    inis.IniWriteValue("Task", "currentWebId", webTask.taskId);
                    //Console.WriteLine("Web start,Url:{0}", webTask.taskUrl);
                    inis.IniWriteValue("Web", "WebPage", webTask.taskUrl);
                    webTest1.serverTest = true;   //����������
                    webAnalyse1.serverTest = true;
                    if (mysqlFlag)
                        mysqlInit.UpdateTaskListColumn("ActionStatus", "3", "TaskId=" + "'" + webTask.taskId + "'");  //��ȡ�����״̬�ĳɵȴ�
                    webTest1.WebServerTaskStartFunc();   //�ڽ������������Զ�����ֹͣ������//�ڲ����������ȴ�
                    webTest1.serverTest = false;   //�������������
                    webAnalyse1.WebServerAnalyzeStartFunc();  //�ڲ����������ȴ�
                    webAnalyse1.serverTest = false;
                    if (mysqlFlag)
                        mysqlInit.UpdateTaskListColumn("ActionStatus", "4", "TaskId=" + "'" + webTask.taskId + "'");  //��ȡ�����״̬�ĳɵȴ�
                    Thread.Sleep(1000);
                }
                else
                {
                    //Log.Info("web�ն�������ִ�У��ȴ�200ms.");
                    Thread.Sleep(200);    //ÿ200ms��ѯ�������Ƿ�������
                }
            }
        }

        void videoTaskFunc()
        {
            Log.Info("Flv ����ɨ��ִ���߳̿���!");
            TaskItems videoTask = null;
            while (true)
            {
                mEvent.WaitOne();   //��һֱ���ţ�������SocketFunc�л�ȡ����ͣ��ָ�����reset
                int countVideo = 0;
                lock (videoQueLocker)
                {
                    countVideo = videoTaskQue.Count;
                }
                if (countVideo > 0)
                {
                    lock (videoQueLocker)
                    {
                        videoTask = videoTaskQue.Dequeue();
                        Log.Info("web����ɨ��ִ���߳�ȡ������:Url:" + videoTask.taskUrl + "#ServerIp:" + videoTask.serverIp);
                    }
					                    //������ʵ��ַ
                    if (videoTask.taskUrlType == 1)
                    {
                        Log.Info("ȡ������Ϊie����,����������ַ�߳�ParseServerReal.");
                        parseThreadUrlTem = videoTask.taskUrl;
                        Thread ParseUrlTh = new Thread(ParseServerReal);
                        ParseUrlTh.Start();
                        ParseUrlTh.Join(10000);
                        if (ParseUrlTh.IsAlive)
                        {
                            ParseUrlTh.Abort();
                            if (mysqlFlag)
                            {
                                //���Ӵ���ע
                                Log.Info("������ַ:" + videoTask.taskUrl + "��ʱ");
                                Log.Error("������ַ:" + videoTask.taskUrl + "��ʱ");
                                mysqlInit.UpdateTaskListColumn("ActionStatus", "5", "TaskId=" + "'" + videoTask.taskId + "'");  //��ȡ�����״̬�ĳɿ�ʼ
                                mysqlInit.UpdateTaskListColumn("Remarks", "ParseUrlTimeOut", "TaskId=" + "'" + videoTask.taskId + "'");  //��ȡ�����״̬�ĳɿ�ʼ
                            }
                            continue;
                        }
                        if (parseThreadRet == -1)
                        {
                            if (mysqlFlag)
                            {
                                Log.Info("������ַ:" + videoTask.taskUrl + "�쳣");
                                Log.Error("������ַ:" + videoTask.taskUrl + "�쳣");
                                //���Ӵ���ע
                                mysqlInit.UpdateTaskListColumn("ActionStatus", "5", "TaskId=" + "'" + videoTask.taskId + "'");  //��ȡ�����״̬�ĳɿ�ʼ
                                mysqlInit.UpdateTaskListColumn("Remarks", "ParseUrlException", "TaskId=" + "'" + videoTask.taskId + "'");  //��ȡ�����״̬�ĳɿ�ʼ
                            }
                            continue;
                        }
                        else if (parseThreadRet == 0)
                        {
                            if (mysqlFlag)
                            {
                                //���Ӵ���ע
                                Log.Info("�޷���ȡ������ַ:" + videoTask.taskUrl );
                                Log.Error("�޷���ȡ������ַ:" + videoTask.taskUrl );
                                mysqlInit.UpdateTaskListColumn("ActionStatus", "5", "TaskId=" + "'" + videoTask.taskId + "'");  //��ȡ�����״̬�ĳɿ�ʼ
                                mysqlInit.UpdateTaskListColumn("Remarks", "CannotGetRealUrl", "TaskId=" + "'" + videoTask.taskId + "'");  //��ȡ�����״̬�ĳɿ�ʼ
                            }
                            continue;
                        }
                        else if (parseThreadRet == 1)
                        {
                            string returnUriTmp = "test.txt";
                            List<string> returnUrl = new List<string>();   //��python�ӳ����ĵ�ַ����list��
                            returnUrl = JsonConvert.DeserializeObject<List<string>>(System.IO.File.ReadAllText(returnUriTmp));
                            if (returnUrl.Count > 0)
                            {
                                //��Ƶ��һС�ε�ַ
                                Log.Info("��ȡ������ʵ��ַ:" + returnUrl[0]);
                                inis.IniWriteValue("Flv", "urlPage", returnUrl[0]);    //urlPage��Ƶ��������ȡ����ʵ��ַkey
                            }
                            else
                            {
                                if (mysqlFlag)
                                {
                                    //���Ӵ���ע
                                    Log.Info("�޷���ȡ������ַ:" + videoTask.taskUrl);
                                    Log.Error("�޷���ȡ������ַ:" + videoTask.taskUrl);
                                    mysqlInit.UpdateTaskListColumn("ActionStatus", "5", "TaskId=" + "'" + videoTask.taskId + "'");  //��ȡ�����״̬�ĳɿ�ʼ
                                    mysqlInit.UpdateTaskListColumn("Remarks", "CannotGetRealUrl", "TaskId=" + "'" + videoTask.taskId + "'");  //��ȡ�����״̬�ĳɿ�ʼ
                                }
                                continue;
                            }
                                
                        }                     
                    }
                    else if (videoTask.taskUrlType == 0)
                    {
                        Log.Info("��ʵ��ַ:" + videoTask.taskUrl);
                        inis.IniWriteValue("Flv", "UrlPage", videoTask.taskUrl);
                    }
                    else
                    {
                        Log.Info("δ������Ե�ַ����:" + videoTask.taskUrlType);
                        Log.Error("δ������Ե�ַ����:" + videoTask.taskUrlType);
                        continue;
                    }
                    inis.IniWriteValue("Task", "currentVideoId", videoTask.taskId);
                    flvTest1.serverTest = true;   //����������
                    flvWebAnalyze1.serverTest = true;
                    if (mysqlFlag)
                        mysqlInit.UpdateTaskListColumn("ActionStatus", "3", "TaskId=" + "'" + videoTask.taskId + "'");  //��ȡ�����״̬�ĳɿ�ʼ 
                    int taskRetCode=flvTest1.StartServerTaskFunc();   //����
                    switch (taskRetCode)
                    {
                        case -19996:
                            {
								//���Ӵ���ע
                                Log.Error("SamplingRateIsNegative");
                                mysqlInit.UpdateTaskListColumn("ActionStatus", "5", "TaskId=" + "'" + videoTask.taskId + "'");
                                mysqlInit.UpdateTaskListColumn("Remarks", "SamplingRateIsNegative", "TaskId=" + "'" + videoTask.taskId + "'"); 
                            }
                            break;
                        case -19997:
                            {
                                Log.Error("InvalidParameter");
                                mysqlInit.UpdateTaskListColumn("ActionStatus", "5", "TaskId=" + "'" + videoTask.taskId + "'");
                                mysqlInit.UpdateTaskListColumn("Remarks", "InvalidParameter", "TaskId=" + "'" + videoTask.taskId + "'"); 
                            }
                            break;
                        case -19998:
                            {
                                Log.Error("PlayTaskFailed");
                                mysqlInit.UpdateTaskListColumn("ActionStatus", "5", "TaskId=" + "'" + videoTask.taskId + "'");
                                mysqlInit.UpdateTaskListColumn("Remarks", "PlayTaskFailed", "TaskId=" + "'" + videoTask.taskId + "'"); 
                                //mysqlInit.WrongInfInsertTaskList("������������ʧ��");
                            }
                            break;
                        case -19999:
                            {
                                Log.Error("NoTask");
                                mysqlInit.UpdateTaskListColumn("ActionStatus", "5", "TaskId=" + "'" + videoTask.taskId + "'");
                                mysqlInit.UpdateTaskListColumn("Remarks", "NoTask", "TaskId=" + "'" + videoTask.taskId + "'"); 
                            }
                            break;
                        case -20000:
                            {
                                Log.Error("OpenUrlFailed");
                                mysqlInit.UpdateTaskListColumn("ActionStatus", "5", "TaskId=" + "'" + videoTask.taskId + "'");
                                mysqlInit.UpdateTaskListColumn("Remarks", "OpenUrlFailed", "TaskId=" + "'" + videoTask.taskId + "'"); 
                            }
                            break;
                        case -20001:
                            {
                                Log.Error("OpenVideoStreamFailed");
                                mysqlInit.UpdateTaskListColumn("ActionStatus", "5", "TaskId=" + "'" + videoTask.taskId + "'");
                                mysqlInit.UpdateTaskListColumn("Remarks", "OpenVideoStreamFailed", "TaskId=" + "'" + videoTask.taskId + "'"); 
                            }
                            break;
                        case -20002:
                            {
                                Log.Error("NoVideoStream");
                                mysqlInit.UpdateTaskListColumn("ActionStatus", "5", "TaskId=" + "'" + videoTask.taskId + "'");
                                mysqlInit.UpdateTaskListColumn("Remarks", "NoVideoStream", "TaskId=" + "'" + videoTask.taskId + "'"); 
                            }
                            break;
                        case -20003:
                            {
                                Log.Error("CannotFindEncoder");
                                mysqlInit.UpdateTaskListColumn("ActionStatus", "5", "TaskId=" + "'" + videoTask.taskId + "'");
                                mysqlInit.UpdateTaskListColumn("Remarks", "CannotFindEncoder", "TaskId=" + "'" + videoTask.taskId + "'"); 
                            }
                            break;
                        case -20004:
                            {
                                Log.Error("CannotOpenEncoder");
                                mysqlInit.UpdateTaskListColumn("ActionStatus", "5", "TaskId=" + "'" + videoTask.taskId + "'");
                                mysqlInit.UpdateTaskListColumn("Remarks", "CannotOpenEncoder", "TaskId=" + "'" + videoTask.taskId + "'"); 
                            }
                            break;
                        case -20005:
                            {
                                Log.Error("WrongSDLInterval");
                                mysqlInit.UpdateTaskListColumn("ActionStatus", "5", "TaskId=" + "'" + videoTask.taskId + "'");
                                mysqlInit.UpdateTaskListColumn("Remarks", "WrongSDLInterval", "TaskId=" + "'" + videoTask.taskId + "'"); 
                            }
                            break;
                        default:
                            break;
                    }
                    flvTest1.serverTest = false;   //�������������
                    if (taskRetCode == 0)
                    {
                        flvWebAnalyze1.StartServerAnalyzeFunc();
                        Log.Info("Analyze start!");
                        if (mysqlFlag)
                            mysqlInit.UpdateTaskListColumn("ActionStatus", "4", "TaskId=" + "'" + videoTask.taskId + "'");  //��ȡ�����״̬�ĳɽ���
                    }
                    Thread.Sleep(4000);
                }
                else
                    Thread.Sleep(200);    //ÿ200ms��ѯ�������Ƿ�������           
            }
        }

        void ParseServerReal()
        {
            Log.Info("�������ӵ�ַ�߳�����.");
            try
            {
                taskScope.SetVariable("url", parseThreadUrlTem);
                var result = taskEngine.CreateScriptSourceFromFile(pythonPath).Execute(taskScope);
                var ParseReal = taskEngine.GetVariable<Func<string, int>>(taskScope, "entrance2");
                parseThreadRet = ParseReal(parseThreadUrlTem);   //0��ʾ����1��ʾ����
            }
            catch (Exception ex)
            {
                Log.Info("�������ӵ�ַ�߳��쳣.");
                Log.Error("�������ӵ�ַ�߳��쳣.");
                Log.Console(Environment.StackTrace, ex); 
                Log.Error(Environment.StackTrace, ex);
                parseThreadRet = -1;  //  �쳣 
            }
        }


        //ÿ5s�����һ��
        void statusUploadFunc()
        {
          
            if (mysqlFlag)
            {
                Log.Info("����״̬�ϴ��߳�����......");
                while (true)
                {
                    //��������taskid��ִ��״̬�ϴ�,�޸Ķ�Ӧ��¼��ͬ��״̬�ֶ�Ϊִ��״̬
                    try
                    {
                        //�������ݿ�����ȡ��ͬ��״̬��=ִ��״̬���ļ�¼
                        List<TaskItems> saTasks = mysqlInit.TaskListFilter("SyncStatus<>ActionStatus");
                        if (saTasks != null && saTasks.Count > 0)
                        {
                            string msg = null;
                            foreach (TaskItems item in saTasks)
                            {
                                try
                                {
                                    string[] ipAndPort = item.serverIp.Split(':');
                                    if (ipAndPort.Length == 2)
                                    {
                                        msg = OperateJson.BuildJson(item.taskId, item.actionStatus,item.remarks) + "<EOF>";
                                        TcpSocketClient clientSocket = new TcpSocketClient(ipAndPort[0], Int32.Parse(ipAndPort[1]));
                                        Log.Console(string.Format("{0}", item.serverIp));
                                        
                                        clientSocket.ConnectToServer();
                                        if (clientSocket.IsConnect())
                                        {
                                            clientSocket.SendMessage(msg);
                                            clientSocket.ShutConnect();
                                            if (mysqlFlag)
                                            {
                                                mysqlInit.UpdateTaskListColumn("SyncStatus", item.actionStatus, "TaskId=" + "'" + item.taskId + "'");
                                                Log.Info("�ϴ�״̬:����id:" + item.taskId + "#����url:" + item.taskUrl + "#ִ��״̬:" + item.actionStatus + "#��ע:" + item.remarks);
                                            }                                           
                                        }                                      
                                    }
                                   
                                }
                                catch (Exception ex)
                                {
                                    Log.Info("����״̬�ϴ��߳��쳣����鿴error��־");
                                    Log.Console(Environment.StackTrace, ex); 
                                    Log.Error(Environment.StackTrace, ex);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Info("����״̬�ϴ��߳��쳣����鿴error��־");
                        Log.Console(Environment.StackTrace, ex); 
                        Log.Error(Environment.StackTrace, ex);
                    }
                    Thread.Sleep(2000);
                }
            }
            else
                //Console.WriteLine("Mysql Init fail! StatusUpload Thread end!");
            {
                Log.Info("Mysql Init fail! StatusUpload Thread end!");
                Log.Error("Mysql Init fail! StatusUpload Thread end!");
            }
        }

        void taskScanFunc()
        {
            if (mysqlFlag)
            {
                Log.Info("����ɨ���߳�����......");
                while (true)
                {
                    try
                    {
                        List<TaskItems> taskItemsLists = null;
                        if (firstScan)
                        {
                            //�������ݿ�����ȡ��ִ��״̬=�յ�1���ȴ�2�Ϳ�ʼ3���ļ�¼
                            taskItemsLists = mysqlInit.TaskListFilter("ActionStatus=1 or ActionStatus=2 or ActionStatus=3");
                            firstScan = false;
                        }
                        else
                            //�������ݿ�����ȡ��ִ��״̬=�յ�1"�ļ�¼
                            taskItemsLists = mysqlInit.TaskListFilter("ActionStatus=1");
                        if (taskItemsLists != null && taskItemsLists.Count > 0)
                        {
                            lock (videoQueLocker)
                            {
                                lock (webQueLocker)
                                {
                                    foreach (TaskItems item in taskItemsLists)
                                    {
                                        if (item.taskType == "Web")
                                            webTaskQue.Enqueue(item);
                                        else if (item.taskType == "Video")
                                            videoTaskQue.Enqueue(item);
                                        mysqlInit.UpdateTaskListColumn("ActionStatus", "2", "TaskId=" + "'" + item.taskId + "'");  //��ȡ�����״̬�ĳɵȴ�2
                                        Log.Info("����ɨ���߳�ȡ������:"+"#����url:"+item.taskUrl+"#ִ��״̬:"+item.actionStatus+"#��ע:"+item.remarks);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Info("����ɨ���߳��쳣����鿴error��־");
                        Log.Console(Environment.StackTrace, ex); 
                        Log.Error(Environment.StackTrace, ex);
                    }
                    Thread.Sleep(2000);
                }
            }
            else
            {
                Log.Info("Mysql Init fail! StatusUpload Thread end!");
                Log.Error("Mysql Init fail! StatusUpload Thread end!");
            }
                //Console.WriteLine("Mysql Init fail! taskScan Thread end!");
        }





        private void navBarWeb_LinkClicked(object sender, DevExpress.XtraNavBar.NavBarLinkEventArgs e)
        {
            Thread.Sleep(500);
            this.webTest1.Visible = true;
            this.webTest1.Dock = DockStyle.Fill;
            this.webSettings1.Visible = false;
            this.webAnalyse1.Visible = false;
            this.flvSetting1.Visible = false;
            this.flvTest1.Visible = false;
            flvWebAnalyze1.Visible = false;
            if (!WebTest.DoTest)
            {
                this.webTest1.Init();
            }
            this.lbText.Text = "��ҳ����...";
        }

        private void navBarWebSet_LinkClicked(object sender, DevExpress.XtraNavBar.NavBarLinkEventArgs e)
        {
            Thread.Sleep(500);
            this.webSettings1.Visible = true;
            this.webSettings1.Dock = DockStyle.Fill;
            this.webTest1.Visible = false;
            this.webAnalyse1.Visible = false;
            this.flvSetting1.Visible = false;
            this.flvTest1.Visible = false;
            flvWebAnalyze1.Visible = false;
            this.webSettings1.Init();
            this.lbText.Text = "��ҳ����...";
        }

        private void navBarWebAnalyse_LinkClicked(object sender, DevExpress.XtraNavBar.NavBarLinkEventArgs e)
        {
            if (WebTest.DoTest)
            {
                MessageBox.Show("�������ڽ����У����������л���壡");
                return;
            }
            Thread.Sleep(500);
            this.webAnalyse1.Visible = true;
            this.webAnalyse1.Dock = DockStyle.Fill;
            this.webSettings1.Visible = false;
            this.webTest1.Visible = false;
            this.flvSetting1.Visible = false;
            this.flvTest1.Visible = false;
            flvWebAnalyze1.Visible = false;
            this.webAnalyse1.Init();
            this.lbText.Text = "��ҳ�������...";
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.flvTest1.StopClosePlayer();
            System.Diagnostics.Process.GetCurrentProcess().Kill();

            //if (FlvTest.serverThread != null && FlvTest.serverThread.IsAlive)
            //{
            //    if (FlvTest.mysock != null && FlvTest.mysock.Connected)
            //    {
            //        FlvTest.mysock.Shutdown(System.Net.Sockets.SocketShutdown.Both);
            //    }
            //    FlvTest.serverThread.Abort();
            //}

        }

        private void navBarFlv_LinkClicked(object sender, DevExpress.XtraNavBar.NavBarLinkEventArgs e)
        {
            Thread.Sleep(500);
            this.flvSetting1.Visible = false;
            this.flvTest1.Visible = true;
            this.flvTest1.Dock = DockStyle.Fill;
            flvWebAnalyze1.Visible = false;
            this.webAnalyse1.Visible = false;
            webTest1.Visible = false;
            webSettings1.Visible = false;
            // �����ǰû���ڲ����򽫲��Գ�ʼ��
            if (!flvTest1.taskon)
            {
                this.flvTest1.Init();
            }

            lbText.Text = "��ý�����...";
        }

        private void navBarFlvSet_LinkClicked(object sender, DevExpress.XtraNavBar.NavBarLinkEventArgs e)
        {
            Thread.Sleep(500);
            this.flvSetting1.Visible = true;
            this.flvSetting1.Dock = DockStyle.Fill;
            flvWebAnalyze1.Visible = false;
            this.webAnalyse1.Visible = false;
            webTest1.Visible = false;
            webSettings1.Visible = false;
            this.flvTest1.Visible = false;
            this.flvSetting1.Init();
            lbText.Text = "��ý������...";
        }

        public void navBarFlvanaly_LinkClicked(object sender, DevExpress.XtraNavBar.NavBarLinkEventArgs e)
        {
            Analyze();
        }

        private void barButtonTip_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            frmTips tips = new frmTips();
            tips.ShowDialog();
        }

        private void barBtnHelp_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            System.Diagnostics.Process.Start("readme.txt");
        }

        public void Analyze()
        {
            if (FlvTest.DoTest)
            {
                MessageBox.Show("�������ڽ����У����������л���壡");
                return;
            }
            Thread.Sleep(500);
            this.flvTest1.Visible = false;
            this.flvSetting1.Visible = false;
            if (inis.IniReadValue("Flv", "Envir").Equals("web"))
            {
                flvWebAnalyze1.Visible = true;
                webAnalyse1.Visible = false;
                flvWebAnalyze1.Dock = DockStyle.Fill;
                flvWebAnalyze1.Init();
                lbText.Text = "��ý���������...";
            }
        }
    }
}