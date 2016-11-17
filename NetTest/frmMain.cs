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

        //任务队列,两者的速度不一样，选择分开
        Queue<TaskItems> videoTaskQue = new Queue<TaskItems>(); //视频检测任务队列
        Queue<TaskItems> webTaskQue = new Queue<TaskItems>();   //网页测评队列
        object videoQueLocker = new object();
        object webQueLocker = new object();
        //TcpClient对象
        //TcpListener client = null;
        //TcpClient clientSocket = null;
        //TcpServer对象,需要讨论下是不是需要

        string serverIp = null;
        int recycle = 0;
        // bool pause = false;
        //数据库
        private MySQLInterface mysqlInit = null;
        private bool mysqlFlag = false;   //数据库初始化标志
        //控制线程
        ManualResetEvent mEvent = new ManualResetEvent(true);
        StackTrace st = new StackTrace(new StackFrame(true));

        //第一次扫描条件
        bool firstScan = true;




        public frmMain()
        {
            InitializeComponent();
            mysqlInit = new MySQLInterface(inis.IniReadValue("Mysql", "serverIp"), inis.IniReadValue("Mysql", "user"), inis.IniReadValue("Mysql", "passwd"), inis.IniReadValue("Mysql", "dbname"), null);
            if (mysqlInit.MysqlInit(inis.IniReadValue("Mysql", "dbname")))
            {
                mysqlFlag = true;
                Log.Info(string.Format("数据库初始化成功!IP:{0};User:{1};Passwd:{2};DBName:{3}", inis.IniReadValue("Mysql", "serverIp"), inis.IniReadValue("Mysql", "user"), inis.IniReadValue("Mysql", "passwd"), inis.IniReadValue("Mysql", "dbname")));
            }
            else
            {
                mysqlFlag = false;
                Log.Warn(string.Format("数据库初始化失败!IP:{0};User:{1};Passwd:{2};DBName:{3}", inis.IniReadValue("Mysql", "serverIp"), inis.IniReadValue("Mysql", "user"), inis.IniReadValue("Mysql", "passwd"), inis.IniReadValue("Mysql", "dbname")));
            }
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            //添加软件使用期限
            String registerTime;

            //读取注册表，如果没有就写入注册表；如果存在即读取注册表时间与当前时间对比
            try
            {
                //获取注册表中时间
                registerTime = (String)Registry.GetValue("HKEY_CURRENT_USER\\RegistryTime", "regTime", "");
                if (registerTime == null)
                {
                    //首次使用软件
                    Registry.SetValue("HKEY_CURRENT_USER\\RegistryTime", "regTime", DateTime.Now.ToString("yyyy-MM-dd"), RegistryValueKind.String);
                    this.textBox1.Text = DateTime.Now.ToString("yyyy-MM-dd");
                    MessageBox.Show("欢迎使用本软件,试用期30天", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    //this.Text = "移动互联网业务质量测评系统 试用版（30天）";
                    Log.Info("欢迎使用本软件,试用期30天!");
                }
                else
                {
                    //非首次使用本软件
                    DateTime endTime = DateTime.Now;   //当前时间
                    DateTime startTime = Convert.ToDateTime(registerTime);
                    TimeSpan span = endTime.Subtract(startTime);
                    if (span.Days > 30)
                    {
                        //MessageBox.Show("软件使用时限过期！请获取完整版！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        Log.Info("软件使用时限过期！请获取完整版！");
                        //return;
                    }
                }
                //this.Text = "移动互联网业务质量测评系统V1.0 试用版（30天）";

            }
            catch (Exception ex)
            {
                Log.Console(Environment.StackTrace, ex);
                Log.Error(Environment.StackTrace, ex);
                MessageBox.Show("注册表操作异常,程序退出!");
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
                    Log.Info("数据库所有表格创建成功!");
                else
                    Log.Warn(mysqlInit.errorInfo);
            }

            //mysqlInit.CreatVideoPara();



            //启动主程序时，必须保证将点播类的播放器进程先杀掉！！！
            string strProcessFile = "VLCDialog";
            Process[] pro = Process.GetProcessesByName(strProcessFile);
            foreach (Process ProCe in pro)
            {
                ProCe.Kill();
                Thread.Sleep(100);
            }
            //如果进程没法杀死，那就直接退出主程序
            if (pro.Length > 0)
            {
                Log.Error("播放器进程无法关闭！软件退出！");
                Application.Exit();
            }
            string strPcap = System.Environment.GetFolderPath(Environment.SpecialFolder.System) + "\\wpcap.dll";
            if (!File.Exists(strPcap))
            {
                MessageBox.Show("运行本程序前请先安装WinPcap！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Log.Error("未安装WinPcap!软件退出！");
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
                        //MessageBox.Show("超过使用范围,程序退出");
                        //this.Dispose();
                        //}
                    }
                }
            }

            var devices = LibPcapLiveDeviceList.Instance;
            if (devices.Count < 1)
            {
                MessageBox.Show("未发现有效网卡,程序退出！\r\n可能是因为WireShark没有安装成功导致的！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Log.Error("未发现有效网卡,程序退出！可能是因为WireShark没有安装成功导致的！");
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

            //20161016 socket交互线程
            //Thread socketThread = new Thread(SocketFunc);
            //socketThread.Start();
            TcpSocketConfig serverConfig = new TcpSocketConfig();
            //serverConfig.Ip = "192.168.50.101";   //在TcpSocketServer类里做了处理，如果没有指定ip就监听本机上所有ip的指定端口
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


            //20161029  task线程
            Thread videoTaskThread = new Thread(videoTaskFunc);
            videoTaskThread.Start();
            Log.Info("video Task Thread Start!");
            Thread webTaskThread = new Thread(webTaskFunc);
            webTaskThread.Start();
            Log.Info("web Task Thread Start!");
            //任务状态上传线程，过滤为“同步状态！=执行状态”
            Thread statusUploadThread = new Thread(statusUploadFunc);
            statusUploadThread.Start();
            //任务扫描线程,过滤为“执行状态=收到1”，记录存在不同的队列里
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
            lbText.Text = "流媒体测试...";
        }

        public void serverReadCompleteFunc(string taskJson)   //回调函数，用于json串解析后压入数据库
        {
            List<AttributeJson> taskList = new List<AttributeJson>();
            int sign = OperateJson.ParseJson(taskJson, ref taskList, ref recycle, ref serverIp);//解析从服务器端接收的json数据包

            // sign 标志从服务器端发来的数据的格式的正确性 0表示空，1表示格式正确，2表示格式错误
            switch (sign)
            {
                case 1://任务数据，向服务器返回json格式的数据，并将数据写入流中
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
                mEvent.WaitOne();   //门一直开着，除非在SocketFunc中获取到暂停的指令调用reset
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
                    webTest1.serverTest = true;   //服务器任务
                    webAnalyse1.serverTest = true;
                    if (mysqlFlag)
                        mysqlInit.UpdateTaskListColumn("ActionStatus", 3, "TaskId=" + "'" + webTask.taskId + "'");  //读取任务后状态改成等待
                    webTest1.WebServerTaskStartFunc();   //在结束条件下能自动调用停止函数，//内部做了阻塞等待
                    webTest1.serverTest = false;   //服务器任务结束
                    webAnalyse1.WebServerAnalyzeStartFunc();  //内部做了阻塞等待
                    webAnalyse1.serverTest = false;
                    if (mysqlFlag)
                        mysqlInit.UpdateTaskListColumn("ActionStatus", 4, "TaskId=" + "'" + webTask.taskId + "'");  //读取任务后状态改成等待
                    Thread.Sleep(4000);
                }
                else
                    Thread.Sleep(200);    //每200ms查询队列中是否有任务 
            }
        }

        void videoTaskFunc()
        {
            TaskItems videoTask = null;
            while (true)
            {
                mEvent.WaitOne();   //门一直开着，除非在SocketFunc中获取到暂停的指令调用reset
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
                    flvTest1.serverTest = true;   //服务器任务
                    flvWebAnalyze1.serverTest = true;
                    if (mysqlFlag)
                        mysqlInit.UpdateTaskListColumn("ActionStatus", 3, "TaskId=" + "'" + videoTask.taskId + "'");  //读取任务后状态改成开始
                    //flvTest1.startFunc();   
                    flvTest1.StartServerTaskFunc();    //2016.11.16现在为非阻塞
                    Log.Info("flv Test start!");
                    //仅用于测试，1分钟后停止,实际播放器停止需要播放器的信息
                    Thread.Sleep(60000);
                    //flvTest1.stopFunc();    //内部做了阻塞等待
                    flvTest1.StopServerTaskFunc();
                    Log.Info("flv Test stop!");
                    flvTest1.serverTest = false;   //服务器任务结束
                    //flvWebAnalyze1.startFunc();
                    flvWebAnalyze1.StartServerAnalyzeFunc();
                    Log.Info("Analyze start!");
                    if (mysqlFlag)
                        mysqlInit.UpdateTaskListColumn("ActionStatus", 4, "TaskId=" + "'" + videoTask.taskId + "'");  //读取任务后状态改成结束
                    Thread.Sleep(4000);
                }
                else
                    Thread.Sleep(200);    //每200ms查询队列中是否有任务           
            }
        }


        //每5s会进来一次
        void statusUploadFunc()
        {
            if (mysqlFlag)
            {
                while (true)
                {
                    //遍历：将taskid和执行状态上传,修改对应记录的同步状态字段为执行状态
                    try
                    {
                        //调用数据库对象获取“同步状态！=执行状态”的记录
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
                            //调用数据库对象获取“执行状态=收到1、等待2和开始3”的记录
                            taskItemsLists = mysqlInit.TaskListFilter("ActionStatus=1 or ActionStatus=2 or ActionStatus=3");
                            firstScan = false;
                        }
                        else
                            //调用数据库对象获取“执行状态=收到1"的记录
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
                                        mysqlInit.UpdateTaskListColumn("ActionStatus", 2, "TaskId=" + "'" + item.taskId + "'");  //读取任务后状态改成等待2
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
            this.lbText.Text = "网页测试...";
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
            this.lbText.Text = "网页设置...";
        }

        private void navBarWebAnalyse_LinkClicked(object sender, DevExpress.XtraNavBar.NavBarLinkEventArgs e)
        {
            if (WebTest.DoTest)
            {
                MessageBox.Show("测试正在进行中，请勿随意切换面板！");
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
            this.lbText.Text = "网页网络分析...";
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
            // 如果当前没有在测试则将测试初始化
            if (!flvTest1.taskon)
            {
                this.flvTest1.Init();
            }

            lbText.Text = "流媒体测评...";
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
            lbText.Text = "流媒体设置...";
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
                MessageBox.Show("测试正在进行中，请勿随意切换面板！");
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
                lbText.Text = "流媒体网络分析...";
            }
        }
    }
}