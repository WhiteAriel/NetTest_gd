/*************************************************************
 * 1.一键化选择样本分为ref和unref
 * 2.ref和unref里面分别包含web和rtsp
 * 3.一键化样本都在release目录下相应目录下
 * 4.本地有参考视频在release目录相应目录下，里面有web和rtsp有参考对应参考视频说明txt
 * 
 * ***********************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Web;
using System.Net;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using DevExpress.XtraEditors;
using System.Threading;
//using Jiang.frmLoading;
using SharpPcap;
using PacketDotNet;
using SharpPcap.LibPcap;
using System.Collections;    //zc
using System.Net.Sockets;    //zc
using System.Collections.Specialized;   //zc
using Newtonsoft.Json;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using NetLog;


namespace NetTest
{
    public partial class FlvSetting : DevExpress.XtraEditors.XtraUserControl
    {
        IniFile inis = new IniFile(Application.StartupPath + "\\settings.ini");
        IniFile inisvlc = new IniFile(Application.StartupPath + "\\VideoPlayer\\vlc.ini");
        //删除操作时要使用
        ArrayList listtemp = new ArrayList();
        ArrayList list_temp_aid = new ArrayList();
        //网卡信息
        public SharpPcap.LibPcap.PcapDevice device;
        //web播放所选地址类型
        enum SELWEBTYPE { ieurl, realurl };
        private SELWEBTYPE Selwebtype = SELWEBTYPE.ieurl;
        //视频网站类型
        enum WEBSITETYPE { sina,ifeng,netbase,sohu,none};
        private WEBSITETYPE webtype = WEBSITETYPE.ifeng;     //首选凤凰卫视

        //web分析成功与否
        enum WEBFINDSUC { suc, fail, unfinish,excep };
        private WEBFINDSUC WebFindSuc = WEBFINDSUC.unfinish;
        //解析计时
        //public WaitWindowClass waitwin = null;
        //设置界面线程
        Thread setTrd = null;
        //web网址变化解析线程
        //Thread webThd = null;
        //等待解析地址线程返回的事件
        //AutoResetEvent myEvent;
        //更新视频网站类型和可用链接线程
        Thread searchWeb = null;
        //更新特定视频网站可用链接线程
        Thread searchWebUrl = null;
        //c#调用python的引擎
        //更新网站的引擎
        static string path = @"findIeUrl2.py";
        static ScriptEngine engine =null; 
        static ScriptScope scope =null;
        //更新地址的引擎
        static ScriptEngine engine2 =null; 
        static ScriptScope scope2 =null; 
        //解析地址门限
        int threshold=4;
       // private int iTest = 1;              //连续播放了多少次

        public FlvSetting()
        {
            InitializeComponent();
            try
            {
                Log.Info("python engine of flv setting initialize.....");
                engine=Python.CreateEngine();
                scope =engine.CreateScope();
                engine2 =Python.CreateEngine();
                scope2 =engine2.CreateScope();
            }
            catch(Exception ex)
            {
                Log.Info("python engine of flv setting initialize fail!Please check the python dll.");
                Log.Console(Environment.StackTrace,ex); 
                Log.Error(Environment.StackTrace,ex);
            }

        }

        /**********************************************************************************************************/
        private void IniCboxWebSel()      //添加网址选择框
        {
            this.cbSelwebtype.Items.Clear();
            this.cbSelwebtype.Items.Add("IE链接");
            this.cbSelwebtype.Items.Add("真实地址");
            Selwebtype = SELWEBTYPE.ieurl;
            this.cbSelwebtype.Text = "IE链接";
        }


        private bool IniAutoWebSiteSel()
        {
            this.cbAutoWebSite.Items.Clear();
            string tmp;
            for (int i = 0; i < 4;i++)       //取配置文件中的前4个website
            {
                tmp=inis.IniReadValue("Flv", "website" + (i + 1));
                if (tmp != "")
                {
                    if (tmp.Contains("sohu"))
                        this.cbAutoWebSite.Items.Add("搜狐视频");
                    else if (tmp.Contains("ifeng"))
                        this.cbAutoWebSite.Items.Add("凤凰视频");
                    else if (tmp.Contains("163"))
                        this.cbAutoWebSite.Items.Add("网易视频");
                    else if (tmp.Contains("sina"))
                        this.cbAutoWebSite.Items.Add("新浪视频");                   
                }
            }
            string firstWebsite=inis.IniReadValue("Flv", "website1");
            if (firstWebsite!="")   //存在可以解析的记录
            {
                this.cbAutoWebSite.SelectedIndex = 0;    //默认首项
                if (firstWebsite.Contains("sohu"))
                    webtype = WEBSITETYPE.sohu;
                else if (firstWebsite.Contains("ifeng"))
                    webtype = WEBSITETYPE.ifeng;
                else if (firstWebsite.Contains("163"))
                    webtype = WEBSITETYPE.netbase;
                else if (firstWebsite.Contains("sina"))
                    webtype = WEBSITETYPE.sina;              
                return true;
            }
            else
            {
                this.cbAutoWebSite.SelectedIndex = -1;
                webtype = WEBSITETYPE.none;
                return false;
            }

        }

        private void IniAutoRealSel()
        {
            this.cbAutoReal.Items.Clear();
            if (webtype == WEBSITETYPE.sohu)
            {
                for (int i = 0; i < 5; i++)
                {
                    if (inis.IniReadValue("Flv", "sohu" + (i + 1)) != "" && inis.IniReadValue("Flv", "sohu" + (i + 1)) != "0")
                        this.cbAutoReal.Items.Add(inis.IniReadValue("Flv", "sohu" + (i + 1)));
                }
                if (inis.IniReadValue("Flv", "sohu1")!= "")
                {
                    //this.cbAutoReal.SelectedIndex = 0;
                }
            }
            else if (webtype == WEBSITETYPE.ifeng)
            {
                for (int i = 0; i < 5; i++)
                {
                    if (inis.IniReadValue("Flv", "ifeng" + (i + 1)) != "" && inis.IniReadValue("Flv", "ifeng" + (i + 1)) != "0")
                        this.cbAutoReal.Items.Add(inis.IniReadValue("Flv", "ifeng" + (i + 1)));
                }
                if (inis.IniReadValue("Flv", "ifeng1") != "")
                {
                    this.cbAutoReal.SelectedIndex = 0;
                }
            }
            else if (webtype == WEBSITETYPE.sina)
            {
                for (int i = 0; i < 5; i++)
                {
                    if (inis.IniReadValue("Flv", "sina" + (i + 1)) != "" && inis.IniReadValue("Flv", "sina" + (i + 1)) != "0")
                        this.cbAutoReal.Items.Add(inis.IniReadValue("Flv", "sina" + (i + 1)));
                }
                if (inis.IniReadValue("Flv", "sina1") != "")
                {
                    this.cbAutoReal.SelectedIndex = 0;
                }
            }
            else if (webtype == WEBSITETYPE.netbase)
            {
                for (int i = 0; i < 5; i++)
                {
                    string tmp = inis.IniReadValue("Flv", "netbase" + (i + 1));
                    if (tmp != ""&&tmp != "0")
                        this.cbAutoReal.Items.Add(inis.IniReadValue("Flv", "netbase" + (i + 1)));
                }
                if (inis.IniReadValue("Flv", "netbase1") != "")
                {
                    this.cbAutoReal.SelectedIndex = 0;
                }
            }
        }


        //在FlvTest文件里做了播放后的列表更新
        private void IniCboxWeb()
        {
            this.cbSelweb.Items.Clear();
            //下面的if语句是向ini文件中写入关于Flv的信息的。
            if (Selwebtype == SELWEBTYPE.ieurl)     //IE地址时
            {
                //向web下拉菜单中添加真实地址测试记录，读取保存的6个地址
                for (int i = 0; i < 6; i++)
                {
                    if (inis.IniReadValue("Flv", "ieurl" + (i + 1)) != "")
                        this.cbSelweb.Items.Add(inis.IniReadValue("Flv", "ieurl" + (i + 1)));
                }
                //ieurl1中存储的为最新测试记录，应在web框中显示
                //this.cbSelweb.Text = inis.IniReadValue("Flv", "ieurl1");
            }
            else     //真实地址时
            {
                
                //向web下拉菜单中添加真实地址测试记录
                for (int i = 0; i < 6; i++)        //存6个真实地址
                {
                    if (inis.IniReadValue("Flv", "relurl" + (i + 1)) != "")
                        this.cbSelweb.Items.Add(inis.IniReadValue("Flv", "relurl"+(i + 1)));  //如果有真实地址就填入框中             
                }
                //this.cbSelweb.Text = inis.IniReadValue("Flv", "relurl1");
            }
            if (this.cbSelweb.Items.Count>0)
            {
                this.cbSelweb.SelectedIndex=0;   //默认第一项
            }
            else
                 this.cbSelweb.SelectedIndex=-1;   //没有可选项

            if (inis.IniReadValue("Flv", "Envir") == "web")   //当前为web测试时
                this.cbAdapter.Enabled = true;
        }

        public void Init()
        {
            searchAdapter();

            //流媒体手动资源选项
            this.IniCboxWebSel();
            this.IniCboxWeb();

            //流媒体自动资源选项
            if (this.IniAutoWebSiteSel())
                this.IniAutoRealSel();    //如果存在网站资源就根据网站类型填充真实地址

            //初始化生成的播放Log文件(txt file)和抓包文件(pcap file)路径
            string strwhole = null;
            strwhole = inis.IniReadValue("Flv", "PlayPcapPath");   //抓包地址
            this.txtPlayPcapPath.Text = strwhole;
            try
            {
                if (!Directory.Exists(this.txtPlayPcapPath.Text))    //路径不存在就创建目录
                    Directory.CreateDirectory(this.txtPlayPcapPath.Text);
            }
            catch (System.Exception ex)        //pcap文件路径异常 
            {
               Log.Console(Environment.StackTrace,ex); Log.Warn(Environment.StackTrace,ex);
                this.txtPlayPcapPath.Text = null;
            }

            //初始化是（否）连续测试     
            int iBoolCC;
            int.TryParse(inis.IniReadValue("Flv", "CheckContinuous"), out iBoolCC);
            if (iBoolCC > 0)
            {
                this.chkContinue.Checked = true;
                this.txtContinueNo.Enabled = true;
                this.txtContinueNo.Text = inis.IniReadValue("Flv", "NumContinuous");//记录连续次数
            }
            else
            {
                this.chkContinue.Checked = false;
                this.txtContinueNo.Enabled = false;
                this.txtContinueNo.Text = null;
                this.txtContinueNo.Text = "";
            }

            this.textThreshold.Text = inis.IniReadValue("Flv", "Threshold");

            //初始ini文件中为web时进行的初始化设置。
            if (inis.IniReadValue("Flv", "Envir").Equals("web"))
            {
                if (inis.IniReadValue("Flv", "LinksType") == "Manual")   //手动设置
                {
                    this.rBtnweb.Checked = true;
                    rBtnweb_Click(this.rBtnweb, new EventArgs());
                }
                else                                                    //自动设置
                {
                    this.rAutoCheck.Checked = true;
                    rAutoCheck_Click(this.rAutoCheck, new EventArgs());
                }
                this.cbAdapter.Enabled = true;
            }
        }

        /*************************************************************
         * 解析网址以及保存设置信息
         * ***************************************************************/
        private void setOK()
        {
            this.btnSetCancel.Enabled = false;
            inis.IniWriteValue("Flv", "NumContinuous", this.txtContinueNo.Text);
            inis.IniWriteValue("Flv", "Adapter", this.cbAdapter.SelectedIndex.ToString());

            int tmpThreshold = int.Parse(this.textThreshold.Text);
            if (tmpThreshold < 4)
                this.textThreshold.Text = "4";
            else if (tmpThreshold > 40)
                this.textThreshold.Text = "40";
            inis.IniWriteValue("Flv", "Threshold", (this.textThreshold.Text).ToString());
            threshold = int.Parse(this.textThreshold.Text);   //检测超时门限


            //ip信息写入ini文件
            if (cbAdapter.Text.StartsWith("网卡") && cbAdapter.Text.Length > 8)
                inis.IniWriteValue("Flv", "IpAddress", this.cbAdapter.Text.Substring(8, cbAdapter.Text.Length - 8));
            else if (cbAdapter.Text.StartsWith("TD") && cbAdapter.Text.Length > 14)
                inis.IniWriteValue("Flv", "IpAddress", this.cbAdapter.Text.Substring(14, cbAdapter.Text.Length - 14));
            else if (cbAdapter.Text.StartsWith("无线") && cbAdapter.Text.Length > 9)
                inis.IniWriteValue("Flv", "IpAddress", this.cbAdapter.Text.Substring(9, cbAdapter.Text.Length - 9));
            else
                inis.IniWriteValue("Flv", "IpAddress", "未确定信息");
            //
            //抓包和日志文件输出路径
            //
            if (this.txtPlayPcapPath.Text.Length == 0)
            {
                MessageBox.Show("请指定抓包和日志文件的正确输出路径!");
                Log.Warn("请指定抓包和日志文件的正确输出路径!");
                this.btnSetCancel.Enabled = true;
                return;
            }
            else
            {
                try
                {
                    if (!Directory.Exists(this.txtPlayPcapPath.Text))
                        Directory.CreateDirectory(this.txtPlayPcapPath.Text);
                    inis.IniWriteValue("Flv", "PlayPcapPath", this.txtPlayPcapPath.Text);  //不抛出异常说明正确，写入配置
                }
                catch (System.Exception ex)
                {
                    Log.Console(Environment.StackTrace,ex); 
                    Log.Error(Environment.StackTrace,ex);
                    this.txtPlayPcapPath.Text = inis.IniReadValue("Flv", "PlayPcapPath"); //读上一次的
                    MessageBox.Show("抓包和日志文件输出路径错误！");   //出错不写入配置
                    Log.Info("抓包和日志文件输出路径错误!");
                    return;
                }
            }

            if (this.chkContinue.Checked)
            {
                try
                {
                    int a = int.Parse(this.txtContinueNo.Text);
                    if (a < 0)
                    {
                        inis.IniWriteValue("Flv", "NumContinuous", "3");
                        MessageBox.Show("请正确输入连续次数！");
                        return;
                    }
                    inis.IniWriteValue("Flv", "NumContinuous", a.ToString());
                }
                catch
                {
                    MessageBox.Show("请正确输入连续次数!");
                    Log.Warn("无效的连续次数.");
                    inis.IniWriteValue("Flv", "NumContinuous", "3");
                    return;
                }
                inis.IniWriteValue("Flv", "CheckContinuous", "1");
            }
            else
                inis.IniWriteValue("Flv", "CheckContinuous", "0");

            //web测试时对ini文件进行的相关写入及相关显示
          if (inis.IniReadValue("Flv", "Envir") == "web")
          {
             if (inis.IniReadValue("Flv", "LinksType") == "Manual")    //手动设置
             {
                if (this.cbSelweb.Text != "")      //网址不为空
                {
                    if (Selwebtype == SELWEBTYPE.ieurl)
                    {
                               
                        inis.IniWriteValue("Flv", "urltemp", this.cbSelweb.Text);
                        inis.IniWriteValue("Flv", "isurl", "ieurl");
                        btnSetOK.Enabled = false;
                        btnSetCancel.Enabled = false;
                        rBtnweb.Enabled = false;
                        rAutoCheck.Enabled = false;
                        Thread findreladd = new Thread(new ThreadStart(ThreadProcFindAdd));    //新接口进来在这里改
                        findreladd.Start();
                        findreladd.Join(threshold*1000);   //主线程阻塞至超时门限后往下跑
                        try 
                        {
                            if (findreladd.IsAlive)//如果地址解析线程还没返回，解析超时
                            {
                                WebFindSuc = WEBFINDSUC.unfinish;
                                findreladd.Abort();
                                Log.Info("解析地址超时.");
                                Log.Warn("解析地址超时.");
                            }
                        }
                        catch (ThreadAbortException ExFind)
                        {
                            Log.Info("解析地址超时.");
                            Log.Error(Environment.StackTrace, ExFind);
                            Thread.ResetAbort();
                        }
                        findreladd.Join(100);  //如果Abort不成功,那么就执行Join,(调用线程挂起，等待被调用线程ThreadProcFindAdd执行完毕后，继续执行)

                   try             //创建线程去解析地址
                   {

                       if (WebFindSuc == WEBFINDSUC.excep)
                        {
                            MessageBox.Show("解析异常！请稍后再试或直接输入真实文件地址");
                            inis.IniWriteValue("Flv", "urlPage", "");    //解析失败后为urlPage置为空
                            UpdateList(WebFindSuc);
                            btnSetOK.Enabled = true;
                            btnSetCancel.Enabled = true;
                            rBtnweb.Enabled = true;
                            rAutoCheck.Enabled = true;
                            Log.Info("解析地址异常!请稍后再试或直接输入真实文件地址.");
                            Log.Warn("解析异常!请稍后再试或直接输入真实文件地址.");
                            return;
                        }
                        else if (WebFindSuc == WEBFINDSUC.fail)
                        {
                            MessageBox.Show("解析失败！请稍后再试或直接输入真实文件地址！");
                            inis.IniWriteValue("Flv", "urlPage", "");    //解析失败后为urlPage置为空
                            UpdateList(WebFindSuc);
                            btnSetOK.Enabled = true;
                            btnSetCancel.Enabled = true;
                            rBtnweb.Enabled = true;
                            rAutoCheck.Enabled = true;
                            Log.Info("解析地址失败！请稍后再试或直接输入真实文件地址.");
                            Log.Warn("解析地址失败！请稍后再试或直接输入真实文件地址.");
                            return;
                        }

                        else if (WebFindSuc == WEBFINDSUC.unfinish)
                        {
                            MessageBox.Show("解析超时，请稍后再试或直接输入真实文件地址！");
                            inis.IniWriteValue("Flv", "urlPage", "");     //解析失败后为urlPage置为空
                            UpdateList(WebFindSuc);
                            btnSetOK.Enabled = true;
                            btnSetCancel.Enabled = true;
                            rBtnweb.Enabled = true;
                            rAutoCheck.Enabled = true;
                            Log.Info("解析地址超时，请稍后再试或直接输入真实文件地址.");
                            Log.Warn("解析地址超时，请稍后再试或直接输入真实文件地址.");
                            return;
                        }
                        
                        else if (WebFindSuc == WEBFINDSUC.suc)
                        {

                                string returnUriTmp = "test.txt";
                                List<string> returnUrl = new List<string>();   //将python接出来的地址放在list中
                                returnUrl = JsonConvert.DeserializeObject<List<string>>(System.IO.File.ReadAllText(returnUriTmp));
                                if (returnUrl.Count > 0)
                                {
                                    //视频第一小段地址
                                    inis.IniWriteValue("Flv", "urlPage", returnUrl[0]);    //urlPage视频播放器获取的真实地址key
                                    Log.Info("解析地址成功.真实链接地址:" + returnUrl[0]);
                                }
                                else
                                    Log.Info("解析地址成功.但没用的返回地址。");
                                UpdateList(WebFindSuc);
                                
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Log.Console(Environment.StackTrace,ex);
                        Log.Error(Environment.StackTrace,ex);
                        Log.Info("解析地址异常,请查看error日志.");
                        this.btnSetCancel.Enabled = true;
                        return;
                    }                      
                  }
                else       //Selwebtype == SELWEBTYPE.realurl情况
                {
                    WebFindSuc = WEBFINDSUC.suc;
                    inis.IniWriteValue("Flv", "urlPage", this.cbSelweb.Text);   //urlPage保存的是真实地址
                    inis.IniWriteValue("Flv", "isurl", "relurl");
                    UpdateList(WebFindSuc);
                }
             }
                else
                {
                    MessageBox.Show("请输入流媒体文件地址！");
                    this.btnSetCancel.Enabled = true;
                    return;
                }
           }
            else    //自动设置
            {
                  inis.IniWriteValue("Flv", "urlPage", this.cbAutoReal.Text);    //把选择的地址写入配置
            }
         }
            Thread.Sleep(1000);      //setok本身用一个线程执行
            MessageBox.Show("参数设置成功！");
            btnSetOK.Enabled = true;
            btnSetCancel.Enabled = true;
            rBtnweb.Enabled = true;
            rAutoCheck.Enabled = true;
  }



        /******************************************************************************
        播放列表记录更新
       *******************************************************************************/

        private void UpdateList(WEBFINDSUC WebFindSuc)
        {
            ArrayList ieUrlList = new ArrayList();    
            ArrayList relUrlList = new ArrayList();
            ieUrlList.Clear();      //存储rtsp记录、local记录及web测试的ierul记录
            relUrlList.Clear();      //存储web测试的relrul记录
            //读取本次测试信息
            string temp2 = inis.IniReadValue("Flv", "urltemp");
            string temp3 = inis.IniReadValue("Flv", "urlPage");

            if (inis.IniReadValue("Flv", "Envir") == "web")      //ie地址记录信息和真实地址记录信息同时变化
            {
                if (inis.IniReadValue("Flv", "isurl") == "ieurl")     //IE时可更新，且IE地址与真实地址可同步变化
                {
                    for (int i = 0; i < 6; i++)                     //解析成功就把真实地址也写进去，否则不用写
                    {
                        if (inis.IniReadValue("Flv", "ieurl" + (i + 1)) != "")
                            ieUrlList.Add(inis.IniReadValue("Flv", "ieurl" + (i + 1)));
                    }
                    int n = ieUrlList.Count-1;
                    while (n >= 1)    //所有记录后移
                    {
                        ieUrlList[n] = ieUrlList[n - 1];
                        n--;
                    }
                    ieUrlList.Insert(0, temp2);    //记录插到第一行
                    if (WebFindSuc == WEBFINDSUC.suc)
                    {
                        for (int m = 0; m < 6; m++)   //6个真实地址
                        {
                            if (inis.IniReadValue("Flv", "relurl" + (m + 1)) != "")
                                relUrlList.Add(inis.IniReadValue("Flv", "relurl" + (m + 1)));
                        }
                        int i = relUrlList.Count-1;
                        while (i >= 1)    //所有记录后移
                        {
                            relUrlList[i] = relUrlList[i - 1];
                            i--;
                        }
                        relUrlList.Insert(0, temp3);    //记录插到第一行
                    }
                }
                else   //真实地址
                {
                    for (int m = 0; m < 6; m++)   //6个真实地址
                    {
                        if (inis.IniReadValue("Flv", "relurl" + (m + 1)) != "")
                            relUrlList.Add(inis.IniReadValue("Flv", "relurl" + (m + 1)));
                    }
                    int j = relUrlList.Count-1;
                    while (j >= 1)    //所有记录后移
                    {
                        relUrlList[j] = relUrlList[j - 1];
                        j--;
                    }
                    relUrlList.Insert(0, temp3);    //记录插到第一行
                }
            }

            for (int num = 0; num < ieUrlList.Count; num++)     //重新写入ini文件
                inis.IniWriteValue("Flv", "ieurl" + (num + 1), (string)ieUrlList[num]);
            for (int num = 0; num < relUrlList.Count; num++)
                inis.IniWriteValue("Flv", "relurl" + (num + 1), (string)relUrlList[num]);
            }


    


        private void btnSetOK_Click(object sender, EventArgs e)
        {
            setTrd = new Thread(new ThreadStart(setOK));
            setTrd.Start();
        }

        private void btnSetCancel_Click(object sender, EventArgs e)
        {
            this.btnSetCancel.Enabled = false;
            this.Selwebtype = SELWEBTYPE.ieurl;
            this.cbSelwebtype.Text = "IE链接";
            this.cbSelweb.Text = inis.IniReadValue("Flv", "utltemp");     //算是读取上一次的记录
            this.cbAdapter.SelectedIndex = Convert.ToInt32(inis.IniReadValue("Flv", "Adapter"));
            this.txtPlayPcapPath.Text = inis.IniReadValue("Flv", "PlayPcapPath");
            this.txtContinueNo.Text = inis.IniReadValue("Flv", "NumContinuous");

            int iBoolCC;
            int.TryParse(inis.IniReadValue("Flv", "CheckContinuous"), out iBoolCC);
            if (iBoolCC > 0)
            {
                this.chkContinue.Checked = true;
                this.txtContinueNo.Enabled = true;
                this.txtContinueNo.Text = inis.IniReadValue("Flv", "NumContinuous");
            }
            else
            {
                this.chkContinue.Checked = false;
                this.txtContinueNo.Enabled = false;
                this.txtContinueNo.Text = null;
            }


        }

        private void btnPlayPcapPath_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbDlg = new FolderBrowserDialog();  //open a folder browser box
            if (fbDlg.ShowDialog() == DialogResult.OK)
            {
                this.txtPlayPcapPath.Text = fbDlg.SelectedPath;
                //inis.IniWriteValue("Flv", "PlayPcapPath", this.txtPlayPcapPath.Text); //正确的写入配置，错误的不写入
            }
        }

        private void searchAdapter()
        {
            Log.Info("flv setting搜索可用联网网卡....");
            System.Version ver = System.Environment.OSVersion.Version;

            var devices = LibPcapLiveDeviceList.Instance;
            cbAdapter.Items.Clear();
            int idev = 1;

            if (ver.Major == 5 && ver.Minor == 1)     //XP系统时ip地址获取
            {
                inis.IniWriteValue("Flv", "SystemInfo", "XP");
                for (int i = 0; i < devices.Count; i++)
                {
                    /* Description */
                    string strNetName = devices[i].Description;
                    if (devices[i].Addresses.Count != 0 && devices[i].Opened)
                    {
                        if ((strNetName.Contains("PPP")) || (strNetName.Contains("SLIP")) || (strNetName.Contains("ppp")))
                            cbAdapter.Items.Add("TD-CDMA网卡" + "--ip:" + devices[i].Addresses[0].Addr.ipAddress);
                        else
                        {
                            if ((strNetName.Contains("Wireless")) || (strNetName.Contains("wireless")))
                            {
                                if (devices[i].Addresses[0].Addr.ipAddress != null
                                    && !devices[i].Addresses[0].Addr.ipAddress.ToString().Contains("0.0.0.0"))
                                    cbAdapter.Items.Add("无线网卡" + "--ip:" + devices[i].Addresses[0].Addr.ipAddress);
                                else
                                    cbAdapter.Items.Add("无线网卡");
                            }
                            else
                            {
                                if (strNetName.Contains("VPN"))
                                    cbAdapter.Items.Add("VPN");
                                else
                                {
                                    if (devices[i].Addresses[0].Addr.ipAddress != null
                                        && !devices[i].Addresses[0].Addr.ipAddress.ToString().Contains("0.0.0.0"))
                                        cbAdapter.Items.Add("网卡" + idev + "--ip:" + devices[i].Addresses[0].Addr.ipAddress);
                                    else
                                        cbAdapter.Items.Add("网卡" + idev + "--ip:" + devices[i].Addresses[0].Addr.ipAddress);
                                    idev++;
                                }
                            }
                        }
                    }
                }
            }
            else if (ver.Major == 6 && ver.Minor == 1)     //Win7系统时ip地址获取
            {
                inis.IniWriteValue("Flv", "SystemInfo", "Win7");
                for (int i = 0; i < devices.Count; i++)
                {
                    /* Description */
                    string strNetName = devices[i].Description;
                    if (devices[i].Addresses.Count != 0 && devices[i].Opened)
                    {
                        if ((strNetName.Contains("PPP")) || (strNetName.Contains("SLIP")) || (strNetName.Contains("ppp")))
                            cbAdapter.Items.Add("TD-CDMA网卡" + "--ip:" + devices[i].Addresses[1].Addr.ipAddress);
                        else
                        {
                            if ((strNetName.Contains("Wireless")) || (strNetName.Contains("wireless")))
                            {
                                if (devices[i].Addresses[1].Addr.ipAddress != null
                                    && !devices[i].Addresses[1].Addr.ipAddress.ToString().Contains("0.0.0.0"))
                                    cbAdapter.Items.Add("无线网卡" + "--ip:" + devices[i].Addresses[1].Addr.ipAddress);
                                else
                                    cbAdapter.Items.Add("无线网卡");
                            }
                            else
                            {
                                if (strNetName.Contains("VPN"))
                                    cbAdapter.Items.Add("VPN");
                                else
                                {
                                    if (devices[i].Addresses[1].Addr.ipAddress != null
                                        && !devices[i].Addresses[1].Addr.ipAddress.ToString().Contains("0.0.0.0"))
                                        cbAdapter.Items.Add("网卡" + idev + "--ip:" + devices[i].Addresses[1].Addr.ipAddress);
                                    else
                                        cbAdapter.Items.Add("网卡" + idev + "--ip:" + devices[i].Addresses[0].Addr.ipAddress);
                                    idev++;
                                }
                            }
                        }
                    }

                }
            }
            //初始化网卡设置
            int b;
            int.TryParse(inis.IniReadValue("Flv", "Adapter"), out b);
            if (b < 0) { inis.IniWriteValue("Flv", "Adapter", "0"); b = 0; }
            if ((cbAdapter.Items.Count > 0) && (cbAdapter.Items.Count > b))
                cbAdapter.SelectedIndex = Convert.ToInt32(inis.IniReadValue("Flv", "Adapter"));
            else    //当前机器上没有联网网卡时
            {
                Log.Info("当前没有可用的联网网卡!");
                Log.Warn("当前没有可用的联网网卡!");
                cbAdapter.SelectedIndex = -1;
                inis.IniWriteValue("Flv", "Adapter", "-1");
            }
        }

        private void btnSearchAdapter_Click(object sender, EventArgs e)    //检测网卡
        {
            searchAdapter();
        }

      
        private void chkContinue_CheckedChanged(object sender, EventArgs e)
        {
            if (this.chkContinue.Checked == true)
            {
                this.txtContinueNo.Enabled = true;
                this.txtContinueNo.Text = "3";
                inis.IniWriteValue("Flv", "NumContinuous", this.txtContinueNo.Text);
                inis.IniWriteValue("Flv", "CheckContinuous", "1");
            }
            else
            {
                this.txtContinueNo.Enabled = false;
                this.txtContinueNo.Text = null;
                inis.IniWriteValue("Flv", "CheckContinuous", "0");
                inis.IniWriteValue("Flv", "NumContinuous", "0");
            }
        }


        private void cbSelwebtype_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.cbSelwebtype.Text == "IE链接")
            {
                Selwebtype = SELWEBTYPE.ieurl;
            }
            else if (this.cbSelwebtype.Text == "真实地址")
            {
                Selwebtype = SELWEBTYPE.realurl;
            }
            IniCboxWeb();
        }

        private void cbAutoWebSite_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.cbAutoWebSite.Text == "搜狐视频")
            {
                webtype = WEBSITETYPE.sohu;
            }
            else if (this.cbAutoWebSite.Text == "网易视频")
            {
                webtype = WEBSITETYPE.netbase;
            }
            else if (this.cbAutoWebSite.Text == "凤凰视频")
            {
                webtype = WEBSITETYPE.ifeng;
            }
            else if (this.cbAutoWebSite.Text == "新浪视频")
            {
                webtype = WEBSITETYPE.sina;
            }
            inis.IniWriteValue("Flv", "webindex", (this.cbAutoWebSite.SelectedIndex).ToString());
            IniAutoRealSel();
        }

        private void ThreadProcFindAdd()
        {
            //int returnUri = 0;
            WebFindSuc = WEBFINDSUC.unfinish;
            //调用地址解析函数
            try
            {
                string url = inis.IniReadValue("Flv", "urltemp");
                scope.SetVariable("url", url);
                var result = engine.CreateScriptSourceFromFile(path).Execute(scope);
                var func = engine.GetVariable<Func<string, int>>(scope, "entrance2");
                int returnUri = func(url);
                //地址写到一个文件里test.txt
                //Thread.Sleep(5000);
                if (returnUri == 0)
                {
                    WebFindSuc = WEBFINDSUC.fail;
                }
                else if (returnUri == 1)    //地址解析成功
                {
                    WebFindSuc = WEBFINDSUC.suc;
                }
            }
            catch (System.Exception ex)
            {
                WebFindSuc = WEBFINDSUC.excep;
                Log.Console(Environment.StackTrace,ex);
                Log.Info("解析线程ThreadProcFindAdd出现异常,请查看error日志.");
                Log.Error(Environment.StackTrace,ex);
            }
            return;
   
        }

        private void txtResultPath_EditValueChanged(object sender, EventArgs e)
        {

        }

        private void txtContinueNo_EditValueChanged(object sender, EventArgs e)
        {
            if (chkContinue.Checked == true)
            {
                inis.IniWriteValue("Flv", "NumContinuous", this.txtContinueNo.Text);
            }
            else
            {
                inis.IniWriteValue("Flv", "NumContinuous", "0");
            }
        }

        //web样本选择
        private void sBtnWeb_Click(object sender, EventArgs e)
        {
            //一键化选择样本
            OpenFileDialog fileDlg = new OpenFileDialog();
            fileDlg.Filter = "(样本).ini|*.ini";
            fileDlg.Multiselect = false;
            fileDlg.RestoreDirectory = true;

            if (fileDlg.ShowDialog() == DialogResult.OK)
            {
                string filename = fileDlg.SafeFileName;

                if (filename == null)
                {
                    MessageBox.Show("请选择web样本文件！");
                    return;
                }

                ////web无参考
                if (filename.Contains("unrefWeburl"))
                {
                    //chkref.Checked = false;
                    //web框相关显示及授权操作
                    rBtnweb.Enabled = true;
                    rBtnweb.Checked = true;
                    cbSelweb.Enabled = true;
                    cbSelwebtype.Enabled = true;
                    cbSelweb.Text = "";
                    cbSelwebtype.SelectedIndex = 0;
                    //WebRecordDel.Enabled = true;
        
                    //“日志、抓包文件存储路径”栏设置
                    txtPlayPcapPath.Text = @"D:\TestLog\Web";

                    IniFile inis_web_unref_temp = new IniFile(Application.StartupPath + "\\UnrefIni" + "\\web" + "\\" + filename);
                    this.cbSelweb.Text = inis_web_unref_temp.IniReadValue("unrefweb", "ieurl1");
                }
                inis.IniWriteValue("Flv", "Envir", "web");
                cbAdapter.Enabled = true;     //zc
              }
            else
            {
                MessageBox.Show("请选择样本！");
                return;
            }
        }


        private void btnAutoWebSite_Click(object sender, EventArgs e)    //更新可用网站和可用链接，这个界面线程不能阻塞
        {
            int tmpThreshold = int.Parse(this.textThreshold.Text);
            if (tmpThreshold < 4)
                this.textThreshold.Text = "4";
            else if (tmpThreshold > 20)
                this.textThreshold.Text = "40";
            inis.IniWriteValue("Flv", "Threshold", (this.textThreshold.Text).ToString());
            threshold = int.Parse(this.textThreshold.Text);   //检测超时门限
            searchWeb = new Thread(new ThreadStart(searchWebFunc));   //线程函数
            searchWeb.Start();
            btnSetOK.Enabled = false;
            btnSetCancel.Enabled = false;
            btnAutoReal.Enabled = false;
            rAutoCheck.Enabled = false;
            rBtnweb.Enabled = false;
            btnAutoWebSite.Enabled = false;
        }

        private void searchFunc()
        {
            try
            {
                var result = engine.CreateScriptSourceFromFile(path).Execute(scope);
                var func = engine.GetVariable<Func<int>>(scope, "getflvcdvedio");    
                int re=func();     //执行python入口函数
                if (re==1)
                    //网站链接写到一个文件里links.txt，相应的网站可用真实地址写入相应的文件中
                    WebFindSuc = WEBFINDSUC.suc;
                else
                    WebFindSuc = WEBFINDSUC.fail;
            }
            catch (System.Exception ex)
            {
                Log.Info("搜索网址可用链接线程searchFunc发生异常,请查看error日志.");
                Log.Console(Environment.StackTrace,ex); 
                Log.Warn(Environment.StackTrace,ex);
                WebFindSuc = WEBFINDSUC.excep;
            }
       }

        private void searchWebFunc()            //直接在这里做检测也可以，不过要放一个定时器做超时检测
        {
            Log.Info("搜索网站可用链接线程开始....");
            Thread search = new Thread(new ThreadStart(searchFunc));     //新开线程做超时检测
            search.Start();
            search.Join(threshold * 1000);   //超时设置
            try
            {
                if (search.IsAlive)//如果网站更新线程还没返回，解析超时
                {
                    WebFindSuc = WEBFINDSUC.unfinish;
                    search.Abort();
                    Log.Info("搜索网站可用链接线程超时.");
                    Log.Warn("搜索网站可用链接线程超时.");
                }
            }
            catch (ThreadAbortException ExFind)
            {
                Log.Info("搜索网站可用链接线程abort异常,该异常已捕获处理.");
                Log.Warn("搜索网站可用链接线程abort异常,请查看error日志.");
                Thread.ResetAbort();
            }
            search.Join(100);  //如果Abort不成功,那么就执行Join,(调用线程挂起，等待被调用线程ThreadProcFindAdd执行完毕后，继续执行)

            
       try             
       {

           if (WebFindSuc == WEBFINDSUC.excep)
            {
                MessageBox.Show("网站更新异常！请稍后重试");
                btnSetOK.Enabled = true;
                btnSetCancel.Enabled = true;
                btnAutoReal.Enabled = true;
                rAutoCheck.Enabled = true;
                rBtnweb.Enabled = true;
                btnAutoWebSite.Enabled = true;
                Log.Info("网站更新异常！请稍后重试.");
                Log.Warn("网站更新异常！请稍后重试.");
                return;
            }

           else if (WebFindSuc == WEBFINDSUC.unfinish)
           {
               MessageBox.Show("网站更新超时,请增加超时门限后重试");
               //if (searchWeb.IsAlive)
               //    searchWeb.Abort();
               //Thread.Sleep(1000);
               btnSetOK.Enabled = true;
               btnSetCancel.Enabled = true;
               Log.Info("网站更新超时,请增加超时门限后重试.");
               Log.Warn("网站更新超时,请增加超时门限后重试.");
               return;
           }

           else if (WebFindSuc == WEBFINDSUC.fail)
           {
               MessageBox.Show("网站更新失败,请稍后重试！");
               btnSetOK.Enabled = true;
               btnSetCancel.Enabled = true;
               btnAutoReal.Enabled = true;
               rAutoCheck.Enabled = true;
               rBtnweb.Enabled = true;
               btnAutoWebSite.Enabled = true;
               Log.Info("网站更新失败,请稍后重试.");
               Log.Warn("网站更新失败,请稍后重试.");
               return;
           }

            if (WebFindSuc == WEBFINDSUC.suc)
            {
                //读取links文件
                MessageBox.Show("网站更新成功！");
                string returnUriTmp = "links.txt";
                List<string> returnUrl = new List<string>();   //将python接出来的地址放在list中
                returnUrl = JsonConvert.DeserializeObject<List<string>>(System.IO.File.ReadAllText(returnUriTmp));
                if (returnUrl.Count == 0)
                {
                    MessageBox.Show("无可用测试网站，请更新地址检测模块！");
                    Log.Info("网站更新成功！但无可用测试网站！");
                    Log.Warn("网站更新成功！但无可用测试网站！");
                }
                else
                {
                    for (int i = 0; i < 4; i++)
                    {
                        inis.IniWriteValue("Flv", "website" + (i + 1), "");   //清空保存的记录
                    }
                    for (int i = 0; i < returnUrl.Count; i++)
                    {
                        inis.IniWriteValue("Flv", "website" + (i + 1), returnUrl[i]);
                    }//更新ini
                    if (this.IniAutoWebSiteSel())     //重新从配置文件中加载资源
                    {
                        //this.refleshWebsite(returnUrl);    //真实地址写到配置文件
                        this.IniAutoRealSel();    //如果存在网站资源就根据网站类型填充真实地址
                    }
                    btnSetOK.Enabled = true;
                    btnSetCancel.Enabled = true;
                    btnAutoReal.Enabled = true;
                    rAutoCheck.Enabled = true;
                    rBtnweb.Enabled = true;
                    btnAutoWebSite.Enabled = true;
                    Log.Info("网站更新成功!");
                    return;
                }

            }

        }
            catch (System.Exception ex)
            {
                Log.Info("异常,请查看error日志");
                Log.Console(Environment.StackTrace,ex); 
                Log.Warn(Environment.StackTrace,ex);
                btnSetOK.Enabled = true;
                btnSetCancel.Enabled = true;
                btnAutoReal.Enabled = true;
                rAutoCheck.Enabled = true;
                rBtnweb.Enabled = true;
                btnAutoWebSite.Enabled = true;
                return;
            }
                
     }


        private void refleshWebsite(string url)
        {
            string returnUriTmp;
            List<string> ListUrl = new List<string>();   //将python接出来的地址放在list中
            //foreach (string url in returnUrl)       //更新真实地址
            //{
                if (url.Contains("sina"))
                {
                    returnUriTmp = "sina.txt";
                    ListUrl = JsonConvert.DeserializeObject<List<string>>(System.IO.File.ReadAllText(returnUriTmp));
                    for (int i = 0; i < 5; i++)
                    {
                        inis.IniWriteValue("Flv", "sina" + (i + 1), "");   //清空保存的记录
                    }
                    for (int i = 0; i < ListUrl.Count; i++)                //更新ini
                    {
                        inis.IniWriteValue("Flv", "sina" + (i + 1), ListUrl[i]);
                    }
                    webtype = WEBSITETYPE.sina;
                    //ListUrl.Clear();
                }
                else if (url.Contains("sohu"))
                {
                    returnUriTmp = "sohu.txt";
                    ListUrl = JsonConvert.DeserializeObject<List<string>>(System.IO.File.ReadAllText(returnUriTmp));
                    for (int i = 0; i < 5; i++)
                    {
                        inis.IniWriteValue("Flv", "sohu" + (i + 1), "");   //清空保存的记录
                    }
                    for (int i = 0; i < ListUrl.Count; i++)                //更新ini
                    {
                        inis.IniWriteValue("Flv", "sohu" + (i + 1), ListUrl[i]);
                    }
                    webtype = WEBSITETYPE.sohu;
                    //ListUrl.Clear();
                }
                else if (url.Contains("ifeng"))
                {
                    returnUriTmp = "ifeng.txt";
                    ListUrl = JsonConvert.DeserializeObject<List<string>>(System.IO.File.ReadAllText(returnUriTmp));
                    for (int i = 0; i < 5; i++)
                    {
                        inis.IniWriteValue("Flv", "ifeng" + (i + 1), "");   //清空保存的记录
                    }
                    for (int i = 0; i < ListUrl.Count; i++)                //更新ini
                    {
                        inis.IniWriteValue("Flv", "ifeng" + (i + 1), ListUrl[i]);
                    }
                    webtype = WEBSITETYPE.ifeng;
                    //ListUrl.Clear();
                }
                else if (url.Contains("163"))
                {
                    returnUriTmp = "netbase.txt";
                    ListUrl = JsonConvert.DeserializeObject<List<string>>(System.IO.File.ReadAllText(returnUriTmp));
                    for (int i = 0; i < 5; i++)
                    {
                        inis.IniWriteValue("Flv", "netbase" + (i + 1), "");   //清空保存的记录
                    }
                    for (int i = 0; i < ListUrl.Count; i++)                //更新ini
                    {
                        inis.IniWriteValue("Flv", "netbase" + (i + 1), ListUrl[i]);
                    }
                    webtype = WEBSITETYPE.netbase;
                    //ListUrl.Clear();
                }
            //}
        }


        private void btnAutoReal_Click(object sender, EventArgs e)    //更新特点网站的真实地址
        {
            int tmpThreshold = int.Parse(this.textThreshold.Text);
            if (tmpThreshold < 4)
                this.textThreshold.Text = "4";
            else if (tmpThreshold > 20)
                this.textThreshold.Text = "40";
            inis.IniWriteValue("Flv", "Threshold", (this.textThreshold.Text).ToString());
            threshold = int.Parse(this.textThreshold.Text);   //检测超时门限
            searchWebUrl = new Thread(new ThreadStart(searchUrlFunc));   //线程函数
            searchWebUrl.Start();
            btnSetOK.Enabled = false;
            btnSetCancel.Enabled = false;
            btnAutoReal.Enabled = false;
            btnAutoWebSite.Enabled = false;
            rAutoCheck.Enabled = false;
            rBtnweb.Enabled = false;    
        }

        private void searchFunc2()
        {
            Log.Info("获取网址链接线程searchFunc2开启.....");
            int index = this.cbAutoWebSite.SelectedIndex;
            string url = inis.IniReadValue("Flv", "website" + (index + 1));   //获取网站链接
            Console.Write(url);
            try
            {
                scope2.SetVariable("url", url);
                var result2 = engine2.CreateScriptSourceFromFile(path).Execute(scope2);
                var func2 = engine.GetVariable<Func<string, int>>(scope2, "getrealUrl2");
                int returnUri = func2(url);
                if (returnUri==1)
                    //网站链接写到一个文件里.txt，相应的网站可用真实地址写入相应的文件中
                    WebFindSuc = WEBFINDSUC.suc;
                else
                    WebFindSuc = WEBFINDSUC.fail;
                
            }
            catch (System.Exception ex)
            {
                WebFindSuc = WEBFINDSUC.excep;
                Log.Info("获取网址链接线程searchFunc2异常,请查看error日志");
                Log.Console(Environment.StackTrace,ex);
                Log.Warn(Environment.StackTrace,ex);
            }

        }



        private void searchUrlFunc()            //直接在这里做检测也可以，不过要放一个定时器做超时检测
        {
            Thread searchUrl = new Thread(new ThreadStart(searchFunc2));     //新开线程做超时检测
            searchUrl.Start();
            searchUrl.Join(threshold * 1000);    //超时设置
            try
            {
                if (searchUrl.IsAlive)//如果网站更新线程还没返回，解析超时
                {
                    WebFindSuc = WEBFINDSUC.unfinish;
                    searchUrl.Abort();
                }
            }
            catch (ThreadAbortException ExFind)
            {
                Thread.ResetAbort();
                Log.Info("searchFunc2线程abort异常,已捕获处理");
                Log.Console(Environment.StackTrace, ExFind);
                Log.Warn(Environment.StackTrace, ExFind);
            }
            searchUrl.Join(100);  //如果Abort不成功,那么就执行Join,(调用线程挂起，等待被调用线程ThreadProcFindAdd执行完毕后，继续执行)
            try             //创建线程去解析地址
            {

                if (WebFindSuc == WEBFINDSUC.excep)
                {
                    MessageBox.Show("可用网站更新异常！");
                    btnSetOK.Enabled = true;
                    btnSetCancel.Enabled = true;
                    btnAutoReal.Enabled = true;
                    btnAutoWebSite.Enabled = true;
                    rAutoCheck.Enabled = true;
                    rBtnweb.Enabled = true;
                    Log.Info("可用网站更新异常！");
                    Log.Warn("可用网站更新异常！");
                    return;
                }
                else if (WebFindSuc == WEBFINDSUC.unfinish)
                {
                    MessageBox.Show("地址更新超时，请重试！");
                    btnSetOK.Enabled = true;
                    btnSetCancel.Enabled = true;
                    btnAutoReal.Enabled = true;
                    btnAutoWebSite.Enabled = true;
                    rAutoCheck.Enabled = true;
                    rBtnweb.Enabled = true;
                    Log.Info("可用网站更新超时，请重试！");
                    Log.Warn("可用网站更新超时，请重试！");
                    return;
                }
                else if (WebFindSuc == WEBFINDSUC.fail)
                {
                    MessageBox.Show("没有可用地址链接！");
                    btnSetOK.Enabled = true;
                    btnSetCancel.Enabled = true;
                    btnAutoReal.Enabled = true;
                    btnAutoWebSite.Enabled = true;
                    rAutoCheck.Enabled = true;
                    rBtnweb.Enabled = true;
                    Log.Info("没有可用地址链接！");
                    Log.Warn("没有可用地址链接！");
                    return;

                }
                else if (WebFindSuc == WEBFINDSUC.suc)
                {
                       MessageBox.Show("地址更新成功！");
                       int index = this.cbAutoWebSite.SelectedIndex;
                       string url = inis.IniReadValue("Flv", "website" + (index + 1));   //获取网站链接
                       this.refleshWebsite(url);    //真实地址写到配置文件
                       this.IniAutoRealSel();    //如果存在网站资源就根据网站类型填充真实地址
                       btnSetOK.Enabled = true;
                       btnSetCancel.Enabled = true;
                       btnAutoReal.Enabled = true;
                       btnAutoWebSite.Enabled = true;
                       rAutoCheck.Enabled = true;
                       rBtnweb.Enabled = true;
                       Log.Info("地址更新成功！");
                        return;
                }
            }
            catch (System.Exception ex)
            {
                Log.Info("异常,请查看error日志");
                Log.Console(Environment.StackTrace,ex); 
                Log.Warn(Environment.StackTrace,ex);
                btnSetOK.Enabled = true;
                btnSetCancel.Enabled = true;
                btnAutoReal.Enabled = true;
                btnAutoWebSite.Enabled = true;
                rAutoCheck.Enabled = true;
                rBtnweb.Enabled = true;
                return;
            }
                
        }



        private void rBtnweb_Click(object sender, EventArgs e)
        {
            this.cbSelwebtype.Enabled = true;
            this.cbSelweb.Enabled = true;
            this.cbAutoWebSite.Enabled = false;
            this.cbAutoReal.Enabled = false;
            this.rAutoCheck.Checked = false;
            this.btnAutoWebSite.Enabled = false;
            this.btnAutoReal.Enabled = false;
            inis.IniWriteValue("Flv", "LinksType", "Manual");
        }

        private void rAutoCheck_Click(object sender, EventArgs e)
        {
            this.cbSelwebtype.Enabled = false;
            this.cbSelweb.Enabled = false;
            this.rBtnweb.Checked = false;
            this.cbAutoWebSite.Enabled = true;
            this.cbAutoReal.Enabled = true;   
            this.btnAutoWebSite.Enabled = true;
            this.btnAutoReal.Enabled = true;
            inis.IniWriteValue("Flv", "LinksType", "Auto");
            //inis.IniWriteValue("Flv", "webindex", "1");
            
        }

        private void WebRecordDel_Click(object sender, EventArgs e)   //记录删除
        {
            listtemp.Clear();
            list_temp_aid.Clear();

            if (Selwebtype == SELWEBTYPE.ieurl)
            {
                inis.IniWriteValue("Flv", "urltemp", this.cbSelweb.Text);
                string temp1 = inis.IniReadValue("Flv", "urltemp");
                //读取已有测试记录的ie地址和真实地址
                for (int i = 0; i < 10; i++)
                {
                    if (inis.IniReadValue("Flv", "ieurl" + (i + 1)) != "")
                        listtemp.Add(inis.IniReadValue("Flv", "ieurl" + (i + 1)));
                    if (inis.IniReadValue("Flv", "relurl" + (i + 1)) != "")     //这里有问题，真实地址可能有多段
                        list_temp_aid.Add(inis.IniReadValue("Flv", "relurl" + (i + 1)));
                }

                if (listtemp.Count == 0)
                {
                    MessageBox.Show("当前无记录！");
                    return;
                }

                if (!listtemp.Contains(temp1))
                {
                    MessageBox.Show("请选择要删除的记录！");
                    return;
                }

                //移除操作，ie测试记录与真实地址记录要同时变化
                {
                    int n = listtemp.IndexOf(temp1);
                    listtemp.RemoveAt(n);
                    list_temp_aid.RemoveAt(n);                   //多段记录要删除
                }

                //清空settings.ini中相关记录信息
                for (int a = 0; a < 10; a++)
                    inis.IniWriteValue("Flv", "ieurl" + (a + 1), "");
                for (int b = 0; b < 10; b++)
                    inis.IniWriteValue("Flv", "relurl" + (b + 1), "");

                //在ini中重新显示更新listtemp和list_temp_aid后的内容
                for (int m = 0; m < listtemp.Count; m++)
                    inis.IniWriteValue("Flv", "ieurl" + (m + 1), (string)listtemp[m]);
                for (int c = 0; c < list_temp_aid.Count; c++)
                    inis.IniWriteValue("Flv", "relurl" + (c + 1), (string)list_temp_aid[c]);

                //更新web下拉菜单内容
                this.cbSelweb.Items.Clear();             //重写下拉菜单

                for (int b = 0; b < 10; b++)
                    if (inis.IniReadValue("Flv", "ieurl" + (b + 1)) != "")
                        this.cbSelweb.Items.Add(inis.IniReadValue("Flv", "ieurl" + (b + 1)));

                //MessageBox.Show("操作完成！");
                if (listtemp.Count != 0)
                    this.cbSelweb.Text = (string)listtemp[0];
                else
                    this.cbSelweb.Text = "";

                listtemp.Clear();
                list_temp_aid.Clear();
            }

            if (Selwebtype == SELWEBTYPE.realurl)
            {
                inis.IniWriteValue("Flv", "urlPage", this.cbSelweb.Text);
                string temp2 = inis.IniReadValue("Flv", "urlPage");
                //读取已有测试记录，包括ie地址和真实地址
                for (int i = 0; i < 10; i++)
                {
                    if (inis.IniReadValue("Flv", "ieurl" + (i + 1)) != "")
                        listtemp.Add(inis.IniReadValue("Flv", "ieurl" + (i + 1)));
                    if (inis.IniReadValue("Flv", "relurl" + (i + 1)) != "")
                        list_temp_aid.Add(inis.IniReadValue("Flv", "relurl" + (i + 1)));
                }

                if (list_temp_aid.Count == 0)
                {
                    MessageBox.Show("当前无记录！");
                    return;
                }

                if (!list_temp_aid.Contains(temp2))
                {
                    MessageBox.Show("请选择要删除的记录！");
                    return;
                }

                //移除操作，ie测试记录与真实地址记录要同时变化
                {
                    int n = list_temp_aid.IndexOf(temp2);
                    listtemp.RemoveAt(n);
                    list_temp_aid.RemoveAt(n);
                }

                //清空settings.ini中相关记录信息
                for (int a = 0; a < 10; a++)
                    inis.IniWriteValue("Flv", "ieurl" + (a + 1), "");
                for (int b = 0; b < 10; b++)
                    inis.IniWriteValue("Flv", "relurl" + (b + 1), "");

                //在ini中重新显示更新listtemp和list_temp_aid后的内容
                for (int m = 0; m < listtemp.Count; m++)
                    inis.IniWriteValue("Flv", "ieurl" + (m + 1), (string)listtemp[m]);
                for (int c = 0; c < list_temp_aid.Count; c++)
                    inis.IniWriteValue("Flv", "relurl" + (c + 1), (string)list_temp_aid[c]);

                //更新web下拉菜单内容
                this.cbSelweb.Items.Clear();

                for (int b = 0; b < 10; b++)
                    if (inis.IniReadValue("Flv", "relurl" + (b + 1)) != "")
                        this.cbSelweb.Items.Add(inis.IniReadValue("Flv", "relurl" + (b + 1)));

                //MessageBox.Show("操作完成！");
                if (listtemp.Count != 0)
                    this.cbSelweb.Text = (string)list_temp_aid[0];
                else
                    this.cbSelweb.Text = "";

                listtemp.Clear();
                list_temp_aid.Clear();
            }
        }

    }
}