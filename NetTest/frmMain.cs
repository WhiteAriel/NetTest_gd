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
        //TcpClient����
        //TcpListener client = null;
        //TcpClient clientSocket = null;
        //TcpServer����,��Ҫ�������ǲ�����Ҫ

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




        public frmMain()
        {
            InitializeComponent();
            mysqlInit = new MySQLInterface(inis.IniReadValue("Mysql", "serverIp"), inis.IniReadValue("Mysql", "user"), inis.IniReadValue("Mysql", "passwd"), inis.IniReadValue("Mysql", "dbname"), null);
            if (mysqlInit.MysqlInit(inis.IniReadValue("Mysql", "dbname")))
            {
                mysqlFlag = true;
                Log.Info(string.Format("���ݿ��ʼ���ɹ�!IP:{0};User:{1};Passwd:{2};DBName:{3}", inis.IniReadValue("Mysql", "serverIp"), inis.IniReadValue("Mysql", "user"), inis.IniReadValue("Mysql", "passwd"), inis.IniReadValue("Mysql", "dbname")));
            }
            else
            {
                mysqlFlag = false;
                Log.Warn(string.Format("���ݿ��ʼ��ʧ��!IP:{0};User:{1};Passwd:{2};DBName:{3}", inis.IniReadValue("Mysql", "serverIp"), inis.IniReadValue("Mysql", "user"), inis.IniReadValue("Mysql", "passwd"), inis.IniReadValue("Mysql", "dbname")));
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
                    Log.Warn(mysqlInit.errorInfo);
            }

            //mysqlInit.CreatVideoPara();



            //����������ʱ�����뱣֤���㲥��Ĳ�����������ɱ��������
            string strProcessFile = "VLCDialog";
            Process[] pro = Process.GetProcessesByName(strProcessFile);
            foreach (Process ProCe in pro)
            {
                ProCe.Kill();
                Thread.Sleep(100);
            }
            //�������û��ɱ�����Ǿ�ֱ���˳�������
            if (pro.Length > 0)
            {
                Log.Error("�����������޷��رգ�����˳���");
                Application.Exit();
            }
            string strPcap = System.Environment.GetFolderPath(Environment.SpecialFolder.System) + "\\wpcap.dll";
            if (!File.Exists(strPcap))
            {
                MessageBox.Show("���б�����ǰ���Ȱ�װWinPcap��", "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                new TcpSocketServer(serverConfig).StartListening();
            }
            catch (System.Exception ex)
            {
                Log.Console(Environment.StackTrace, ex);
                Log.Error(Environment.StackTrace, ex);
            }


            //20161029  task�߳�
            Thread videoTaskThread = new Thread(videoTaskFunc);
            videoTaskThread.Start();
            Log.Info("video Task Thread Start!");
            Thread webTaskThread = new Thread(webTaskFunc);
            webTaskThread.Start();
            Log.Info("web Task Thread Start!");
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
                        //foreach (AttributeJson taskItem in taskList)
                        if (mysqlFlag)
                        {
                            for (int listcount = 0; listcount < taskList.Count; listcount++)
                                mysqlInit.TaskListInsertMySQL(taskList[listcount].BatchNo + "#" + taskList[listcount].Id + "#" + taskList[listcount].Type + "#" + taskList[listcount].Url + "#" + serverIp);
                        }
                        taskList.Clear();
                        break;
                    }
                case 220:   //start task
                    {
                        mEvent.Set();
                        break;
                    }
                case 221:   //stop task
                    {
                        mEvent.Reset();
                        break;
                    }
                case 0:
                case 3:
                case 300:
                default: break;
            }
        }

        public void serverReadExceptionFunc(Exception ex)
        {
            Log.Error(Environment.StackTrace, ex);
        }

        void webTaskFunc()
        {
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
                    }
                    inis.IniWriteValue("Task", "currentWebId", webTask.taskId);
                    Console.WriteLine("Web start,Url:{0}", webTask.taskUrl);
                    inis.IniWriteValue("Web", "WebPage", webTask.taskUrl);
                    webTest1.serverTest = true;   //����������
                    webAnalyse1.serverTest = true;
                    if (mysqlFlag)
                        mysqlInit.UpdateTaskListColumn("ActionStatus", 3, "TaskId=" + "'" + webTask.taskId + "'");  //��ȡ�����״̬�ĳɵȴ�
                    webTest1.WebServerTaskStartFunc();   //�ڽ������������Զ�����ֹͣ������//�ڲ����������ȴ�
                    webTest1.serverTest = false;   //�������������
                    webAnalyse1.WebServerAnalyzeStartFunc();  //�ڲ����������ȴ�
                    webAnalyse1.serverTest = false;
                    if (mysqlFlag)
                        mysqlInit.UpdateTaskListColumn("ActionStatus", 4, "TaskId=" + "'" + webTask.taskId + "'");  //��ȡ�����״̬�ĳɵȴ�
                    Thread.Sleep(4000);
                }
                else
                    Thread.Sleep(200);    //ÿ200ms��ѯ�������Ƿ������� 
            }
        }

        void videoTaskFunc()
        {
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
                    }
                    inis.IniWriteValue("Task", "currentVideoId", videoTask.taskId);
                    Console.WriteLine("Flv start,Url:{0}", videoTask.taskUrl);
                    inis.IniWriteValue("Flv", "UrlPage", videoTask.taskUrl);
                    flvTest1.serverTest = true;   //����������
                    flvWebAnalyze1.serverTest = true;
                    if (mysqlFlag)
                        mysqlInit.UpdateTaskListColumn("ActionStatus", 3, "TaskId=" + "'" + videoTask.taskId + "'");  //��ȡ�����״̬�ĳɿ�ʼ
                    //flvTest1.startFunc();   
                    flvTest1.StartServerTaskFunc();    //2016.11.16����Ϊ������
                    Log.Info("flv Test start!");
                    //�����ڲ��ԣ�1���Ӻ�ֹͣ,ʵ�ʲ�����ֹͣ��Ҫ����������Ϣ
                    Thread.Sleep(60000);
                    //flvTest1.stopFunc();    //�ڲ����������ȴ�
                    flvTest1.StopServerTaskFunc();
                    Log.Info("flv Test stop!");
                    flvTest1.serverTest = false;   //�������������
                    //flvWebAnalyze1.startFunc();
                    flvWebAnalyze1.StartServerAnalyzeFunc();
                    Log.Info("Analyze start!");
                    if (mysqlFlag)
                        mysqlInit.UpdateTaskListColumn("ActionStatus", 4, "TaskId=" + "'" + videoTask.taskId + "'");  //��ȡ�����״̬�ĳɽ���
                    Thread.Sleep(4000);
                }
                else
                    Thread.Sleep(200);    //ÿ200ms��ѯ�������Ƿ�������           
            }
        }


        //ÿ5s�����һ��
        void statusUploadFunc()
        {
            if (mysqlFlag)
            {
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
                                        msg = OperateJson.BuildJson(item.taskId, item.actionStatus, "") + "<EOF>";
                                        TcpSocketClient clientSocket = new TcpSocketClient(ipAndPort[0], Int32.Parse(ipAndPort[1]));
                                        Log.Console(string.Format("{0}", item.serverIp));
                                        clientSocket.ConnectToServer();
                                        if (clientSocket.IsConnect())
                                        {
                                            clientSocket.SendMessage(msg);
                                            clientSocket.ShutConnect();
                                            mysqlInit.UpdateTaskListColumn("SyncStatus", item.actionStatus, "TaskId=" + "'" + item.taskId + "'");
                                        }                                      
                                    }
                                   
                                }
                                catch (Exception ex)
                                {
                                    Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
                    }
                    Thread.Sleep(2000);
                }
            }
            else
                Console.WriteLine("Mysql Init fail! StatusUpload Thread end!");
        }

        void taskScanFunc()
        {
            if (mysqlFlag)
            {
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
                                        mysqlInit.UpdateTaskListColumn("ActionStatus", 2, "TaskId=" + "'" + item.taskId + "'");  //��ȡ�����״̬�ĳɵȴ�2
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Console(Environment.StackTrace, ex); Log.Error(Environment.StackTrace, ex);
                    }
                    Thread.Sleep(2000);
                }
            }
            else
                Console.WriteLine("Mysql Init fail! taskScan Thread end!");
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