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

        //任务队列,两者的速度不一样，选择分开
        Queue<TaskItems> videoTaskQue = new Queue<TaskItems>(); //视频检测任务队列
        Queue<TaskItems> webTaskQue = new Queue<TaskItems>();   //网页测评队列
        object videoQueLocker = new object();
        object webQueLocker = new object();

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
        private string pythonPath = @"findIeUrl2.py";
        private ScriptEngine taskEngine = null;
        private ScriptScope taskScope = null;

        //解析真实地址线程返回值
        int parseThreadRet = -2;
        //解析真实地址线程使用的临时地址
        string parseThreadUrlTem = "";

        public frmMain()
        {
            InitializeComponent();
            mysqlInit = new MySQLInterface(inis.IniReadValue("Mysql", "serverIp"), inis.IniReadValue("Mysql", "user"), inis.IniReadValue("Mysql", "passwd"), inis.IniReadValue("Mysql", "dbname"),inis.IniReadValue("Mysql", "port")) ;
            if (mysqlInit.MysqlInit(inis.IniReadValue("Mysql", "dbname")))
            {
                mysqlFlag = true;
                Log.Info(string.Format("数据库初始化成功!IP:{0};User:{1};Passwd:{2};Port:{3};DBName:{4}", inis.IniReadValue("Mysql", "serverIp"), inis.IniReadValue("Mysql", "user"), inis.IniReadValue("Mysql", "passwd"), inis.IniReadValue("Mysql", "port"), inis.IniReadValue("Mysql", "dbname")));
            }
            else
            {
                mysqlFlag = false;
                Log.Info(string.Format("数据库初始化失败!IP:{0};User:{1};Passwd:{2};Port:{3};DBName:{4}", inis.IniReadValue("Mysql", "serverIp"), inis.IniReadValue("Mysql", "user"), inis.IniReadValue("Mysql", "passwd"), inis.IniReadValue("Mysql", "port"), inis.IniReadValue("Mysql", "dbname")));
                Log.Error(string.Format("数据库初始化失败!IP:{0};User:{1};Passwd:{2};Port:{3};DBName:{4}", inis.IniReadValue("Mysql", "serverIp"), inis.IniReadValue("Mysql", "user"), inis.IniReadValue("Mysql", "passwd"), inis.IniReadValue("Mysql", "port"), inis.IniReadValue("Mysql", "dbname")));
            }
            //调用python爬虫获取真实地址
            try
            {
                Log.Info("初始化python engine.");
                taskEngine = Python.CreateEngine();
                taskScope = taskEngine.CreateScope();
            }
            catch (System.Exception ex)
            {
                Log.Info("初始化python engine异常,请查看error日志");
                Log.Console(Environment.StackTrace, ex);
                Log.Error(Environment.StackTrace, ex);
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
                Log.Info("操作注册表异常,请查看error日志.");
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
                {
                    Log.Info("数据库部分表格创建失败!");
                    Log.Error(mysqlInit.errorInfo);
                }
            }

            //启动主程序时，必须保证将点播类的播放器进程先杀掉！！！
            //string strProcessFile = "VLCDialog";
            //Process[] pro = Process.GetProcessesByName(strProcessFile);
            //foreach (Process ProCe in pro)
            //{
            //    ProCe.Kill();
            //    Thread.Sleep(100);
            //}
            ////如果进程没法杀死，那就直接退出主程序
            //if (pro.Length > 0)
            //{
            //    Log.Error("播放器进程无法关闭！软件退出！");
            //    Application.Exit();
            //}
            string strPcap = System.Environment.GetFolderPath(Environment.SpecialFolder.System) + "\\wpcap.dll";
            if (!File.Exists(strPcap))
            {
                MessageBox.Show("运行本程序前请先安装WinPcap！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Log.Info("未安装WinPcap!软件退出！");
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
                Log.Info("未发现有效网卡,程序退出！可能是因为WireShark没有安装成功导致的！");
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
                Log.Info("Tcp socket监听服务开启.......");
                new TcpSocketServer(serverConfig).StartListening();
            }
            catch (System.Exception ex)
            {
                Log.Info("Tcp socket监听服务开启异常,请查看error日志");
                Log.Console(Environment.StackTrace, ex);
                Log.Error(Environment.StackTrace, ex);
            }


            //20161029  task线程
            Thread videoTaskThread = new Thread(videoTaskFunc);
            videoTaskThread.Start();
            Thread webTaskThread = new Thread(webTaskFunc);
            webTaskThread.Start();
           
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
                        //Log.Info("接收到服务的json命令.");
                        if (mysqlFlag)
                        {
                            for (int listcount = 0; listcount < taskList.Count; listcount++)
                            {
                                Log.Info("服务器json串:BatchNo:" + taskList[listcount].BatchNo + "#TaskId:" + taskList[listcount].Id + "#Type:" + taskList[listcount].Type + "#UrlType:" + taskList[listcount].UrlType + "#Url:" + taskList[listcount].Url + "#ServerIp:" + serverIp);
                                mysqlInit.TaskListInsertMySQL(taskList[listcount].BatchNo + "#" + taskList[listcount].Id + "#" + taskList[listcount].Type + "#" + taskList[listcount].UrlType + "#" + taskList[listcount].Url + "#" + serverIp);
                            }
                        }
                        taskList.Clear();
                        break;
                    }
                case 220:   //start task
                    {
                        mEvent.Set();
                        Log.Info("启动任务.");
                        break;
                    }
                case 221:   //stop task
                    {
                        mEvent.Reset();
                        Log.Info("结束任务.");
                        break;
                    }
                case 0:
                case 3:
                case 300:
                default:
                    Log.Info("Json串解析其他异常，请查看info日志中接收到的字符串格式.");
                    Log.Warn("Json串解析其他异常，请查看info日志中接收到的字符串格式.");
                break;
            }
        }

        public void serverReadExceptionFunc(Exception ex)
        {
            Log.Error(Environment.StackTrace, ex);
        }

        void webTaskFunc()
        {
            Log.Info("web任务扫描执行线程开启!");
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
                        Log.Info("web任务扫描执行线程取出任务:Url:" + webTask.taskUrl+"#ServerIp:"+webTask.serverIp);
                    }
                    inis.IniWriteValue("Task", "currentWebId", webTask.taskId);
                    //Console.WriteLine("Web start,Url:{0}", webTask.taskUrl);
                    inis.IniWriteValue("Web", "WebPage", webTask.taskUrl);
                    webTest1.serverTest = true;   //服务器任务
                    webAnalyse1.serverTest = true;
                    if (mysqlFlag)
                        mysqlInit.UpdateTaskListColumn("ActionStatus", "3", "TaskId=" + "'" + webTask.taskId + "'");  //读取任务后状态改成等待
                    webTest1.WebServerTaskStartFunc();   //在结束条件下能自动调用停止函数，//内部做了阻塞等待
                    webTest1.serverTest = false;   //服务器任务结束
                    webAnalyse1.WebServerAnalyzeStartFunc();  //内部做了阻塞等待
                    webAnalyse1.serverTest = false;
                    if (mysqlFlag)
                        mysqlInit.UpdateTaskListColumn("ActionStatus", "4", "TaskId=" + "'" + webTask.taskId + "'");  //读取任务后状态改成等待
                    Thread.Sleep(1000);
                }
                else
                {
                    //Log.Info("web终端任务在执行，等待200ms.");
                    Thread.Sleep(200);    //每200ms查询队列中是否有任务
                }
            }
        }

        void videoTaskFunc()
        {
            Log.Info("Flv 任务扫描执行线程开启!");
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
                        Log.Info("web任务扫描执行线程取出任务:Url:" + videoTask.taskUrl + "#ServerIp:" + videoTask.serverIp);
                    }
					                    //不是真实地址
                    if (videoTask.taskUrlType == 1)
                    {
                        Log.Info("取出任务为ie链接,启动解析地址线程ParseServerReal.");
                        parseThreadUrlTem = videoTask.taskUrl;
                        Thread ParseUrlTh = new Thread(ParseServerReal);
                        ParseUrlTh.Start();
                        ParseUrlTh.Join(10000);
                        if (ParseUrlTh.IsAlive)
                        {
                            ParseUrlTh.Abort();
                            if (mysqlFlag)
                            {
                                //增加错误备注
                                Log.Info("解析地址:" + videoTask.taskUrl + "超时");
                                Log.Error("解析地址:" + videoTask.taskUrl + "超时");
                                mysqlInit.UpdateTaskListColumn("ActionStatus", "5", "TaskId=" + "'" + videoTask.taskId + "'");  //读取任务后状态改成开始
                                mysqlInit.UpdateTaskListColumn("Remarks", "ParseUrlTimeOut", "TaskId=" + "'" + videoTask.taskId + "'");  //读取任务后状态改成开始
                            }
                            continue;
                        }
                        if (parseThreadRet == -1)
                        {
                            if (mysqlFlag)
                            {
                                Log.Info("解析地址:" + videoTask.taskUrl + "异常");
                                Log.Error("解析地址:" + videoTask.taskUrl + "异常");
                                //增加错误备注
                                mysqlInit.UpdateTaskListColumn("ActionStatus", "5", "TaskId=" + "'" + videoTask.taskId + "'");  //读取任务后状态改成开始
                                mysqlInit.UpdateTaskListColumn("Remarks", "ParseUrlException", "TaskId=" + "'" + videoTask.taskId + "'");  //读取任务后状态改成开始
                            }
                            continue;
                        }
                        else if (parseThreadRet == 0)
                        {
                            if (mysqlFlag)
                            {
                                //增加错误备注
                                Log.Info("无法获取解析地址:" + videoTask.taskUrl );
                                Log.Error("无法获取解析地址:" + videoTask.taskUrl );
                                mysqlInit.UpdateTaskListColumn("ActionStatus", "5", "TaskId=" + "'" + videoTask.taskId + "'");  //读取任务后状态改成开始
                                mysqlInit.UpdateTaskListColumn("Remarks", "CannotGetRealUrl", "TaskId=" + "'" + videoTask.taskId + "'");  //读取任务后状态改成开始
                            }
                            continue;
                        }
                        else if (parseThreadRet == 1)
                        {
                            string returnUriTmp = "test.txt";
                            List<string> returnUrl = new List<string>();   //将python接出来的地址放在list中
                            returnUrl = JsonConvert.DeserializeObject<List<string>>(System.IO.File.ReadAllText(returnUriTmp));
                            if (returnUrl.Count > 0)
                            {
                                //视频第一小段地址
                                Log.Info("获取解析真实地址:" + returnUrl[0]);
                                inis.IniWriteValue("Flv", "urlPage", returnUrl[0]);    //urlPage视频播放器获取的真实地址key
                            }
                            else
                            {
                                if (mysqlFlag)
                                {
                                    //增加错误备注
                                    Log.Info("无法获取解析地址:" + videoTask.taskUrl);
                                    Log.Error("无法获取解析地址:" + videoTask.taskUrl);
                                    mysqlInit.UpdateTaskListColumn("ActionStatus", "5", "TaskId=" + "'" + videoTask.taskId + "'");  //读取任务后状态改成开始
                                    mysqlInit.UpdateTaskListColumn("Remarks", "CannotGetRealUrl", "TaskId=" + "'" + videoTask.taskId + "'");  //读取任务后状态改成开始
                                }
                                continue;
                            }
                                
                        }                     
                    }
                    else if (videoTask.taskUrlType == 0)
                    {
                        Log.Info("真实地址:" + videoTask.taskUrl);
                        inis.IniWriteValue("Flv", "UrlPage", videoTask.taskUrl);
                    }
                    else
                    {
                        Log.Info("未定义测试地址类型:" + videoTask.taskUrlType);
                        Log.Error("未定义测试地址类型:" + videoTask.taskUrlType);
                        continue;
                    }
                    inis.IniWriteValue("Task", "currentVideoId", videoTask.taskId);
                    flvTest1.serverTest = true;   //服务器任务
                    flvWebAnalyze1.serverTest = true;
                    if (mysqlFlag)
                        mysqlInit.UpdateTaskListColumn("ActionStatus", "3", "TaskId=" + "'" + videoTask.taskId + "'");  //读取任务后状态改成开始 
                    int taskRetCode=flvTest1.StartServerTaskFunc();   //阻塞
                    switch (taskRetCode)
                    {
                        case -19996:
                            {
								//增加错误备注
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
                                //mysqlInit.WrongInfInsertTaskList("启动播放任务失败");
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
                    flvTest1.serverTest = false;   //服务器任务结束
                    if (taskRetCode == 0)
                    {
                        flvWebAnalyze1.StartServerAnalyzeFunc();
                        Log.Info("Analyze start!");
                        if (mysqlFlag)
                            mysqlInit.UpdateTaskListColumn("ActionStatus", "4", "TaskId=" + "'" + videoTask.taskId + "'");  //读取任务后状态改成结束
                    }
                    Thread.Sleep(4000);
                }
                else
                    Thread.Sleep(200);    //每200ms查询队列中是否有任务           
            }
        }

        void ParseServerReal()
        {
            Log.Info("解析链接地址线程启动.");
            try
            {
                taskScope.SetVariable("url", parseThreadUrlTem);
                var result = taskEngine.CreateScriptSourceFromFile(pythonPath).Execute(taskScope);
                var ParseReal = taskEngine.GetVariable<Func<string, int>>(taskScope, "entrance2");
                parseThreadRet = ParseReal(parseThreadUrlTem);   //0表示错误，1表示正常
            }
            catch (Exception ex)
            {
                Log.Info("解析链接地址线程异常.");
                Log.Error("解析链接地址线程异常.");
                Log.Console(Environment.StackTrace, ex); 
                Log.Error(Environment.StackTrace, ex);
                parseThreadRet = -1;  //  异常 
            }
        }


        //每5s会进来一次
        void statusUploadFunc()
        {
          
            if (mysqlFlag)
            {
                Log.Info("任务状态上传线程启动......");
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
                                                Log.Info("上传状态:任务id:" + item.taskId + "#任务url:" + item.taskUrl + "#执行状态:" + item.actionStatus + "#备注:" + item.remarks);
                                            }                                           
                                        }                                      
                                    }
                                   
                                }
                                catch (Exception ex)
                                {
                                    Log.Info("任务状态上传线程异常，请查看error日志");
                                    Log.Console(Environment.StackTrace, ex); 
                                    Log.Error(Environment.StackTrace, ex);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Info("任务状态上传线程异常，请查看error日志");
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
                Log.Info("任务扫描线程启动......");
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
                                        mysqlInit.UpdateTaskListColumn("ActionStatus", "2", "TaskId=" + "'" + item.taskId + "'");  //读取任务后状态改成等待2
                                        Log.Info("任务扫描线程取到任务:"+"#任务url:"+item.taskUrl+"#执行状态:"+item.actionStatus+"#备注:"+item.remarks);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Info("任务扫描线程异常，请查看error日志");
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