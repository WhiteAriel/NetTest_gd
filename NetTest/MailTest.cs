using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using System.Net;
using System.Net.Mail;
using System.IO;
using Tamir.IPLib;
using Tamir.IPLib.Packets;
using System.Threading;
using OpenPOP.POP3;
using OpenPOP.MIMEParser;
using System.Collections;
using System.Runtime.InteropServices;



namespace NetTest
{
    public partial class MailTest : DevExpress.XtraEditors.XtraUserControl
    {
        private BackgroundWorker m_AsyncWorker = new BackgroundWorker();
        private BackgroundWorker m2_AsyncWorker = new BackgroundWorker();

        private StringBuilder strbFile = new StringBuilder();
        public string strFile = "";
        private string strLogFile;
        private StringBuilder strbFile2 = new StringBuilder();
        public string strFile2 = "";
        private string strLogFile2;
        private int iDevice;
        private PcapDevice device;
        IniFile inis = new IniFile(Application.StartupPath + "\\settings.ini");
        IniFile indll = new IniFile(Application.StartupPath + "\\net.dll");
        private DateTime dtStart;
        private DateTime dtPopStart;
        //private bool t1, t2,t3=true;
        //private Pop3.Pop3MimeClient DemoClient;
        ReceiveMail messageBox;
        private string txtmessage = "";
        private OpenPOP.MIMEParser.Message m = null;
        private int index = 1;
        //private int iNumContinuous = int.Parse(inis.IniReadValue("Mail", "NumContinuous"));
        private int iTest = 0;
        //private bool bSend = false;
        [DllImport("pktanalyser")]
        public static extern bool testmain(string CapFile, int a, int b, int c, int d);
        [DllImport("pktanalyser")]
        public static extern int GetCounts();
        [DllImport("pktanalyser")]
        public static extern int GetLossnum();
        [DllImport("pktanalyser")]
        public static extern int GetDupacknum();
        [DllImport("pktanalyser")]
        public static extern int GetRetransmitnum();
        [DllImport("pktanalyser")]
        public static extern int GetMisordernum();

        public MailTest()
        {
            InitializeComponent();

            m_AsyncWorker.WorkerSupportsCancellation = true;
            //m_AsyncWorker.ProgressChanged += new ProgressChangedEventHandler(bwAsync_ProgressChanged);
            m_AsyncWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bwAsync_RunWorkerCompleted);
            m_AsyncWorker.DoWork += new DoWorkEventHandler(bwAsync_DoWork);

            m2_AsyncWorker.WorkerSupportsCancellation = true;
            //m_AsyncWorker.ProgressChanged += new ProgressChangedEventHandler(bwAsync_ProgressChanged);
            m2_AsyncWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bwAsync2_RunWorkerCompleted);
            m2_AsyncWorker.DoWork += new DoWorkEventHandler(bwAsync2_DoWork);
            
            Control.CheckForIllegalCrossThreadCalls = false;

            readButton.Enabled = false;
            saveButton.Enabled = false;
            changeButton.Enabled = false;
            messageNO.ReadOnly = true;
            //this.textBox1.Visible = true;
            //this.textBox1.Text = "Hello world";
            //this.textBox1.Dock = DockStyle.Fill;

            //this.iNumContinuous = int.Parse(inis.IniReadValue("Mail", "NumContinuous"));
            this.proBar.Value = 0;
            this.proBar.Visible = false;
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            this.btnSend.Enabled = false;
            
            this.ClearDns();
            Thread.Sleep(1000);
            //this.t1 = true;
            //this.t2 = false;
            string dir = inis.IniReadValue("Mail", "Path");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            this.labelMsg.Text = "";
            if (strbFile.Length > 0) strbFile.Remove(0, strbFile.Length);

            strFile = inis.IniReadValue("Mail", "Path") + "\\Mail-SMTP-" + DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString()
            + "-" + DateTime.Now.Day.ToString() + "-" + DateTime.Now.Hour.ToString() + "-" + DateTime.Now.Minute.ToString() + "-" +
            DateTime.Now.Second.ToString() + ".cap";  // inis.IniReadValue("FTP","Host")+
            strLogFile = inis.IniReadValue("Mail", "Path") + "\\Mail-SMTP-" + DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString()
                + "-" + DateTime.Now.Day.ToString() + "-" + DateTime.Now.Hour.ToString() + "-" + DateTime.Now.Minute.ToString() + "-" +
                DateTime.Now.Second.ToString() + ".xls";

            if (!Directory.Exists(inis.IniReadValue("Mail", "Path"))) Directory.CreateDirectory(inis.IniReadValue("Mail", "Path"));
            //if (!m_AsyncWorker.IsBusy)
            //{
            //    m_AsyncWorker.RunWorkerAsync();
            //}
            if (inis.IniReadValue("Mail", "CheckContinuous") == "1")
            {
                int iTemp = iTest + 1;
                this.labelMsg.Text ="第 "+iTemp+ " 次发送中...请稍等";
            }
            else this.labelMsg.Text = "发送中...请稍等";
            this.proBar.Visible = true;
            this.proBar.Value = 0;
            this.timSMTP.Interval = int.Parse(inis.IniReadValue("Mail", "TimeSMTP")) * 1000;
            this.timSMTP.Enabled = true;
            this.timSMTP.Start();
            this.timBar.Enabled = true;
            this.timBar.Start();
            this.timer1.Enabled = false;
            this.timer1.Stop();
            this.MailTesting();
        }
        private void MailTesting()
        {
            iTest++;
            iDevice = int.Parse(inis.IniReadValue("Mail", "Adapter"));

            PcapDeviceList devices = SharpPcap.GetAllDevices();

            device = devices[iDevice];
            string ip = device.PcapIpAddress;
            strbFile.Append("第" + iTest.ToString() + "次测试\r\n");
            strbFile.Append("初始化...\r\n");
            strbFile.Append("网卡: " + device.PcapDescription + "\r\n");
            strbFile.Append("IP地址: " + device.PcapIpAddress + "\r\n");
            //if(t1)
            strbFile.Append("邮件源地址: " + inis.IniReadValue("Mail","Account") + "\r\n");
        //if (t2)
        //    strbFile.Append("目的地址: " + inis.IniReadValue("Mail","Account") + "\r\n");
            //strbFile.Append("测试开始时间: " + dtStart.ToString() + "\r\n");

            Thread.Sleep(500);
            this.dtStart = DateTime.Now;
            strbFile.Append("测试开始时间: " + dtStart.ToString() + "\r\n");


            //Register our handler function to the 'packet arrival' event
            device.PcapOnPacketArrival +=
                new SharpPcap.PacketArrivalEvent(device_PcapOnPacketArrival);
            device.PcapOpen(true, 100);
            device.PcapSetFilter("(tcp or udp) and host " + ip);
            device.PcapDumpOpen(strFile);
                    if (!m_AsyncWorker.IsBusy)
                {
                    m_AsyncWorker.RunWorkerAsync();
                }
                try
                {
                    //SmtpMail sm = new SmtpMail();
                    ////sm.
                    //sm.MailDomain = inis.IniReadValue("Mail", "SMTP");
                    //sm.MailDomainPort = int.Parse(inis.IniReadValue("Mail", "PortSMTP"));
                    //sm.MailServerUserName = inis.IniReadValue("Mail", "Account");
                    //sm.MailServerPassWord = indll.IniReadValue("Mail", "Pass");
                    //sm.From = inis.IniReadValue("Mail", "Account");
                    //sm.FromName = inis.IniReadValue("Mail", "Account");
                    //sm.Body = this.textBody.Text;
                    //sm.Subject = this.txtCap.Text;
                    //string[] to = new string[1];
                    //to[0] = this.txtSend.Text;
                    //sm.AddRecipient(to);
                    //to[0]=this.txtAttach.Text;
                    //sm.AddAttachment(to);
                    //sm.Send();

                    //device.PcapStopCapture();

                    ////device.PcapClose();
                    //DateTime dtEnd = DateTime.Now;
                    //TimeSpan ts = dtEnd - dtStart;
                    //float ts2 = ts.Seconds + (float)ts.Milliseconds / 1000;


                    //strbFile.Append("测试结束,耗时 " + ts.Minutes + "分 " + ts2.ToString() + "秒" + "\r\n");
                    //strbFile.Append("抓包文件: " + strFile + " 创建\r\n");

                    //if (!File.Exists(this.strLogFile))
                    //{ //File.Create(this.strLogFile); }
                    //    FileStream fs1 = new FileStream(this.strLogFile, FileMode.CreateNew, FileAccess.Write);
                    //    StreamWriter sw = new StreamWriter(fs1);
                    //    sw.Write(this.strbFile.ToString());
                    //    sw.Close();
                    //    fs1.Close();
                    //}
                    //else
                    //{
                    //    FileStream fs1 = new FileStream(this.strLogFile, FileMode.Append, FileAccess.Write);
                    //    StreamWriter sw = new StreamWriter(fs1);
                    //    sw.Write(this.strbFile.ToString());
                    //    sw.Close();
                    //    fs1.Close();
                    //}
                    //this.labelMsg.Text = "邮件发送成功,抓包文件已创建";

                    MailAddress from = new MailAddress(inis.IniReadValue("Mail", "Account"));
                    //收件人地址
                    MailAddress to = new MailAddress(this.txtSend.Text);
                    MailMessage message = new MailMessage(from, to);
                    message.Subject = this.txtCap.Text; // 设置邮件的标题
                    message.Body = this.textBody.Text;
                    message.BodyEncoding = System.Text.Encoding.GetEncoding("GB2312");
                    if (this.txtAttach.Text != "")
                        message.Attachments.Add(new System.Net.Mail.Attachment(this.txtAttach.Text));
                    SmtpClient client = new SmtpClient(inis.IniReadValue("Mail", "SMTP"));
                    client.Port = int.Parse(inis.IniReadValue("Mail", "PortSMTP"));
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.UseDefaultCredentials = false;
                    if (inis.IniReadValue("Mail", "SSL") == "1") client.EnableSsl = true;
                    else
                    client.EnableSsl = false;

                    //身份认证
                    client.Credentials = new System.Net.NetworkCredential(inis.IniReadValue("Mail", "Account"),
                        indll.IniReadValue("Mail", "Pass"));
                    client.SendCompleted += new SendCompletedEventHandler(SendCompletedCallback);
                    
                    client.SendAsync(message, "测试");

                    //message.Dispose();

                }
                catch (Exception ex)
                {
                    //strbFile.Append(ex.Message);
                    this.timSMTP.Stop();
                    this.timBar.Stop();
                    this.proBar.Visible = false;
                }
            
                

        }
        private void bwAsync_DoWork(object sender, DoWorkEventArgs e)
        {
            device.PcapStartCapture();

        }

        public void SendCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            this.btnSend.Enabled = true;
            string token = (string)e.UserState;
            if (e.Cancelled)
            {
                MessageBox.Show("取消发送" + token);
                this.btnSend.Enabled = true;
            }
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.ToString());
                this.btnSend.Enabled = true;
            }
            else
            {
                //MessageBox.Show("OK");
                //bSend = true;
                device.PcapStopCapture();

                //device.PcapClose();
                DateTime dtEnd = DateTime.Now;
                TimeSpan ts = dtEnd - dtStart;
                float ts2 = ts.Seconds + (float)ts.Milliseconds / 1000;

                
                strbFile.Append("测试结束,耗时 " + ts.Minutes + "分 " + ts2.ToString() + "秒" + "\r\n");
                strbFile.Append("\r\n");
                strbFile.Append("抓包文件: " + strFile + " 创建\r\n");
                this.performance(ts2,0);
                if (!File.Exists(this.strLogFile))
                { //File.Create(this.strLogFile); }
                    FileStream fs1 = new FileStream(this.strLogFile, FileMode.CreateNew, FileAccess.Write);
                    StreamWriter sw = new StreamWriter(fs1, Encoding.Default);
                    sw.Write(this.strbFile.ToString());
                    sw.Close();
                    fs1.Close();
                }
                else
                {
                    FileStream fs1 = new FileStream(this.strLogFile, FileMode.Append, FileAccess.Write);
                    StreamWriter sw = new StreamWriter(fs1, Encoding.Default);
                    sw.Write(this.strbFile.ToString());
                    sw.Close();
                    fs1.Close();
                }
                this.labelMsg.Text = "邮件发送成功,抓包文件已创建";
                this.timSMTP.Stop();
                this.timBar.Stop();
                this.proBar.Visible = false;
                this.btnSend.Enabled = true;
                if (inis.IniReadValue("Mail", "CheckContinuous")=="1")
                {
                    if (iTest < int.Parse(inis.IniReadValue("Mail", "NumContinuous"))) //iNumContinuous)
                    {
                        //Thread.Sleep(1000);
                        this.timer1.Interval = Convert.ToInt32(inis.IniReadValue("Mail", "Interval")) * 1000;
                        this.timer1.Enabled = true;
                        this.timer1.Start();
                    }
                    else
                    {
                        this.timer1.Enabled = false;
                        this.timer1.Stop();
                        this.iTest = 0;
                        
                    }
                }
                else { iTest = 0; }
                


                
            }
                //mailSent = true;

        }

        private void timSMTP_Tick(object sender, EventArgs e)
        {
            device.PcapStopCapture();

            //device.PcapClose();
            DateTime dtEnd = DateTime.Now;
            TimeSpan ts = dtEnd - dtStart;
            float ts2 = ts.Seconds + (float)ts.Milliseconds / 1000;

            
            strbFile.Append("测试结束,耗时 " + ts.Minutes + "分 " + ts2.ToString() + "秒" + "\r\n");
            strbFile.Append("抓包文件: " + strFile + " 创建\r\n");
            this.performance(ts2,0);
            if (!File.Exists(this.strLogFile))
            { //File.Create(this.strLogFile); }
                FileStream fs1 = new FileStream(this.strLogFile, FileMode.CreateNew, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs1, Encoding.Default);
                sw.Write(this.strbFile.ToString());
                sw.Close();
                fs1.Close();
            }
            else
            {
                FileStream fs1 = new FileStream(this.strLogFile, FileMode.Append, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs1, Encoding.Default);
                sw.Write(this.strbFile.ToString());
                sw.Close();
                fs1.Close();
            }
            this.labelMsg.Text = "邮件发送超时,抓包文件已创建";
            this.proBar.Visible = false;
        }

        private void timBar_Tick(object sender, EventArgs e)
        {
            if (this.proBar.Value < this.proBar.Maximum) this.proBar.Value++;
            else this.proBar.Value = 0;
        }
        private void bwAsync_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                this.strbFile.Append("邮件错误");
                //this.label1.Text = "浏览器错误";
                return;
            }

            // Check to see if the background process was cancelled.
            if (e.Cancelled)
            {
                this.strbFile.Append("任务撤销");
                //this.label1.Text = "任务撤销";
                return;
            }
            


        }

        private void bwAsync2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                this.strbFile2.Append("邮件错误");
                //this.label1.Text = "浏览器错误";
                return;
            }

            // Check to see if the background process was cancelled.
            if (e.Cancelled)
            {
                this.strbFile2.Append("任务撤销");
                //this.label1.Text = "任务撤销";
                return;
            }



        }

        private static void device_PcapOnPacketArrival(object sender, Packet packet)
        {
            PcapDevice device = (PcapDevice)sender;
            //if device has a dump file opened
            if (device.PcapDumpOpened)
            {
                //dump the packet to the file
                device.PcapDump(packet);
                //this.memoPcap.Text+="Packet dumped to file.\n";
            }
        }

        private void btnAttach_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofg = new OpenFileDialog();
            ofg.Multiselect = false;
            if (ofg.ShowDialog() == DialogResult.OK)
            {
                this.txtAttach.Text = ofg.FileName;
            }
        }
       
        
        private void bwAsync2_DoWork(object sender, DoWorkEventArgs e)
        {
            device.PcapStartCapture();
            

        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            messageBox.getAttachments(inis.IniReadValue("Mail", "DownPath"));
            this.labelShowPop3.Text = "附件已保存至 " + inis.IniReadValue("Mail", "DownPath");
            //this.textBox1.Visible = true;
            //this.textBox1.Text = "附件已保存至 " + inis.IniReadValue("Mail", "DownPath");
        }

        private void changeButton_Click(object sender, EventArgs e)
        {
            if (changeButton.Text == "切换至HTML格式")
                showMessage(true);
            else
                showMessage(false);
        }

        private void showMessage(bool HTML)
        {
            //if (!HTML)
            //{
            //    //txtPanel.Controls.Clear();
            //    //TextBox txtBox = new TextBox();
            //    this.textBox1.Visible = true;
            //    this.webBrowser1.Visible = false;
            //    textBox1.Text = txtmessage;
            //    //textBox1.Properties.ReadOnly = true;
            //    //txtPanel.Controls.Add(textBox1);
            //    textBox1.Dock = DockStyle.Fill;
            //    changeButton.Text = "切换至HTML格式";
            //}
            //else
            //{
            //    //txtPanel.Controls.Clear();
            //    //WebBrowser txtBox = new WebBrowser();WebBrowser1
            //webBrowser1.Visible = true;
            //    this.textBox1.Visible = false;
            //    webBrowser1.DocumentText = txtmessage;
            //    //txtPanel.Controls.Add(webBrowser1);
            //    webBrowser1.Dock = DockStyle.Fill;
            //    changeButton.Text = "切换至文本格式";
            //}
            //textFrom.Text = m.From + "<" + m.FromEmail + ">";
            //textTo.Text = m.TO[0];
            if (!HTML)
            {
                txtPanel.Controls.Clear();
                TextBox txtBox = new TextBox();
                txtBox.Multiline = true;
                txtBox.Text = txtmessage;
                txtBox.ReadOnly = true;
                txtPanel.Controls.Add(txtBox);
                txtBox.Dock = DockStyle.Fill;
                changeButton.Text = "切换至HTML格式";
            }
            else
            {
                txtPanel.Controls.Clear();
                WebBrowser txtBox = new WebBrowser();
                txtBox.DocumentText = txtmessage;
                txtPanel.Controls.Add(txtBox);
                txtBox.Dock = DockStyle.Fill;
                changeButton.Text = "切换至文本格式";
            }
            textFrom.Text = m.From + "<" + m.FromEmail + ">";
            textTo.Text = m.TO[0];
        }

        private void btnStopPop3_Click(object sender, EventArgs e)
        {
            //this.txtPanel.Visible = false;
            this.btnStopPop3.Enabled = false;
            device.PcapStopCapture();

            //device.PcapClose();
            DateTime dtEnd = DateTime.Now;
            TimeSpan ts = dtEnd - dtPopStart;
            float ts2 = ts.Seconds + (float)ts.Milliseconds / 1000;


            strbFile2.Append("测试结束,耗时 " + ts.Minutes + "分 " + ts2.ToString() + "秒" + "\r\n");
            strbFile2.Append("\r\n");
            strbFile2.Append("抓包文件: " + strFile2 + " 创建\r\n");
            this.performance(ts2,1);
            if (!File.Exists(this.strLogFile2))
            { //File.Create(this.strLogFile); }
                FileStream fs1 = new FileStream(this.strLogFile2, FileMode.CreateNew, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs1, Encoding.Default);
                sw.Write(this.strbFile2.ToString());
                sw.Close();
                fs1.Close();
            }
            else
            {
                FileStream fs1 = new FileStream(this.strLogFile2, FileMode.Append, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs1, Encoding.Default);
                sw.Write(this.strbFile2.ToString());
                sw.Close();
                fs1.Close();
            }
            this.labelShowPop3.Text = "邮件接收测试结束,抓包文件已创建";
            readButton.Enabled = false;
            saveButton.Enabled = false;
            changeButton.Enabled = false;
            changeButton.Text = "切换";
            messageNO.ReadOnly = true;
            this.textFrom.Text = "";
            this.textTo.Text = "";
            this.messageCount.Text = "";
            this.messageNO.Text = "";
            this.attachmentName.Text = "";
            this.subjectText.Text = "";
            //this.txtPanel.Controls.Clear();
            //this.webBrowser1
            this.webBrowser1.Visible = false;
            //this.textBox1.Visible = false;
        }

        private void readButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (int.Parse(this.messageNO.Text) > int.Parse(this.messageCount.Text))
                {
                    MessageBox.Show("请检查邮件编号范围...");
                    return;
                }
            }
            catch { }
            this.labelShowPop3.Text = "请稍等...";
            //ReadMail();

            //Thread th=new Thread(new ThreadStart(this.ReadMail));
            try
            {
                changeButton.Enabled = true;
                subjectText.Text = "";
                attachmentName.Text = "";
                //this.btnStopPop3.Enabled = false;
                index = Int16.Parse(messageNO.Text);
                //Thread th = new Thread(new ThreadStart(this.ReadMail));
                //th.Start();
                this.ReadMail();
                //showMessage(!m.HTML);
            }
            catch(Exception ex) {
                
                this.labelShowPop3.Text = ex.Message;
            }
            //if (th..IsAlive) th.Abort();
        }

        private void ReadMail()
        {
            
           
            try
            {
                
                bool hasAttachment = messageBox.setMessage(index);

                ArrayList name = new ArrayList();
                m = messageBox.currentMessage;

                int count = m.AttachmentCount;
                if (count > 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (m.GetAttachment(i).NotAttachment)
                            continue;
                        OpenPOP.MIMEParser.Attachment at1 = m.GetAttachment(i);

                        name.Add(m.GetAttachmentFileName(at1));
                    }
                }
                messageBox.setMessage(index);
                int number = name.Count;
                string names = "";
                for (int i = 0; i < number; i++)
                {
                    names = names + name[i].ToString() + " ";
                }
                attachmentName.Text = names;
                subjectText.Text = m.Subject;
                if (m.MessageBody.Count != 0)
                    txtmessage = (string)m.MessageBody[0];
                else
                    txtmessage = "";
                this.labelShowPop3.Text = "邮件读取完成...";
                showMessage(!m.HTML);
                this.btnStopPop3.Enabled = true;
            }
            catch(Exception ex)
            { this.labelShowPop3.Text = ex.Message;}// "连接服务器错误...请稍后重新连接"; }
            
            
        }

        private void btnConPop3_Click(object sender, EventArgs e)
        {
            this.btnStopPop3.Enabled = true;
            this.ClearDns();
            this.labelShowPop3.Text = "开始连接中...";

            //this.txtPanel.Visible = true;
            string dir = inis.IniReadValue("Mail", "Path");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            this.labelMsg.Text = "";
            if (strbFile2.Length > 0) strbFile2.Remove(0, strbFile2.Length);

            strFile2 = inis.IniReadValue("Mail", "Path") + "\\Mail-POP3" + DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString()
            + "-" + DateTime.Now.Day.ToString() + "-" + DateTime.Now.Hour.ToString() + "-" + DateTime.Now.Minute.ToString() + "-" +
            DateTime.Now.Second.ToString() + ".cap";  // inis.IniReadValue("FTP","Host")+
            strLogFile2 = inis.IniReadValue("Mail", "Path") + "\\Mail-POP3" + DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString()
                + "-" + DateTime.Now.Day.ToString() + "-" + DateTime.Now.Hour.ToString() + "-" + DateTime.Now.Minute.ToString() + "-" +
                DateTime.Now.Second.ToString() + ".xls";

            if (!Directory.Exists(inis.IniReadValue("Mail", "Path"))) Directory.CreateDirectory(inis.IniReadValue("Mail", "Path"));
            //if (!m_AsyncWorker.IsBusy)
            //{
            //    m_AsyncWorker.RunWorkerAsync();
            //}
            //this.dtStart = DateTime.Now;
            this.POP3Testing();
        }

        private void POP3Testing()
        {

            iDevice = int.Parse(inis.IniReadValue("Mail", "Adapter"));

            PcapDeviceList devices = SharpPcap.GetAllDevices();

            device = devices[iDevice];
            string ip = device.PcapIpAddress;
            strbFile2.Append("第1次测试开始\r\n");
            strbFile2.Append("初始化...\r\n");
            strbFile2.Append("网卡: " + device.PcapDescription + "\r\n");
            strbFile2.Append("IP地址: " + device.PcapIpAddress + "\r\n");
            //if(t1)
            strbFile.Append("邮件源地址: " + this.txtSend.Text + "\r\n");
            //if (t2)
            //    strbFile.Append("目的地址: " + inis.IniReadValue("Mail","Account") + "\r\n");
            //strbFile.Append("测试开始时间: " + dtStart.ToString() + "\r\n");

            Thread.Sleep(100);
            this.dtPopStart = DateTime.Now;
            strbFile2.Append("测试开始时间: " + dtPopStart.ToString() + "\r\n");


            //Register our handler function to the 'packet arrival' event
            device.PcapOnPacketArrival +=
                new SharpPcap.PacketArrivalEvent(device_PcapOnPacketArrival);
            device.PcapOpen(true, 100);
            device.PcapSetFilter("(tcp or udp) and host " + ip);
            device.PcapDumpOpen(strFile2);
            if (!m2_AsyncWorker.IsBusy)
            {
                m2_AsyncWorker.RunWorkerAsync();
            }
            //Thread th;// = new Thread(new ThreadStart(this.POP3Init));
            try
            {
                Thread th = new Thread(new ThreadStart(this.POP3Init));
                th.Start();
               
                
            }
            catch (Exception ex)
            {
                
                //strbFile.Append(ex.Message);
            }
            //if (th.IsAlive) th.Abort();
        }

        private void POP3Init()
        {
            readButton.Enabled = false;
            saveButton.Enabled = false;
            messageNO.ReadOnly = true;
            string popServer = inis.IniReadValue("Mail", "POP3");
            string login = inis.IniReadValue("Mail", "Account");
            string password = indll.IniReadValue("Mail", "Pass");
            string iPort = inis.IniReadValue("Mail", "PortPOP3");
            if(inis.IniReadValue("Mail","SSL")=="0")
            messageBox = new ReceiveMail(popServer, iPort, login, password,false);
            else
            messageBox = new ReceiveMail(popServer, iPort, login, password, true);
            
            try
            {
                int count = messageBox.connect();
                messageCount.Text = count + "";
                this.labelShowPop3.Text = "已连接...";
            }
            catch (Exception ex) {
                this.labelShowPop3.Text = ex.Message;
                return;
            }
            changeButton.Enabled = false;
            subjectText.Text = "";
            attachmentName.Text = "";
            //txtPanel.Controls.Clear();
            readButton.Enabled = true;
            saveButton.Enabled = true;
            messageNO.ReadOnly = false;
            //txtPanel.Visible = false;
            
            
        }

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
            //string Text = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

        }

        private void performance(float ts2,int iSource)
        {
            string ip = device.PcapIpAddress;
            string temp = ip.Substring(0, ip.IndexOf("."));
            int a = int.Parse(temp);
            ip = ip.Substring(temp.Length + 1);
            temp = ip.Substring(0, ip.IndexOf("."));
            int b = int.Parse(temp);
            ip = ip.Substring(temp.Length + 1);
            temp = ip.Substring(0, ip.IndexOf("."));
            int c = int.Parse(temp);
            ip = ip.Substring(temp.Length + 1);
            int d = int.Parse(ip);
            bool opencap;
            if(iSource==0)
               opencap = testmain(strFile, a, b, c, d);//OpenCap(strFile);OpenCap(strFile);////
            else
               opencap = testmain(strFile2, a, b, c, d);
            if (opencap)
            {
                int count = GetCounts();
                int lossnum = GetLossnum();
                int dupacknum = GetDupacknum();
                int misordernum = GetMisordernum();
                int retransmitnum = GetRetransmitnum();
                if (count > 0)
                {
                    float loss = (float)lossnum * 100 / count;
                    float retrans = (float)retransmitnum * 100 / count;
                    float mis = (float)misordernum * 100 / count;
                    string perf1, perf2, perf3;
                    if (loss < 1.8) perf1 = "良  好";
                    else
                    {
                        if (loss < 2.1) perf1 = "一  般";
                        else perf1 = "较  差";
                    }
                    if (retrans < 2) perf2 = "良  好";
                    else
                    {
                        if (retrans < 2.2) perf2 = "一  般";
                        else perf2 = "较  差";
                    }
                    if (mis < 1.9) perf3 = "良  好";
                    else
                    {
                        if (mis < 2.2) perf3 = "一  般";
                        else perf3 = "较  差";
                    }
                    strbFile.Append("--------------------------------------\r\n");
                    strbFile.Append("|  量 化 指 标  |  数  值  |  评  分  |\r\n");
                    strbFile.Append("--------------------------------------\r\n");
                    strbFile.Append("|  业务延时(秒) |  " + ts2.ToString("F2") + "   |          |\r\n");
                    strbFile.Append("--------------------------------------\r\n");
                    strbFile.Append("|  丢 包 率(%)  |  " + loss.ToString("F2") + "  |" + perf1 + "  |\r\n");
                    strbFile.Append("--------------------------------------\r\n");
                    strbFile.Append("|  重 传 率(%)  |  " + retrans.ToString("F2") + "  |" + perf2 + "  |\r\n");
                    strbFile.Append("--------------------------------------\r\n");
                    strbFile.Append("|  失 序 率(%)  |  " + mis.ToString("F2") + "  |" + perf3 + "  |\r\n");
                    strbFile.Append("--------------------------------------\r\n");
                    //strbFile.Append("count=" + count.ToString() + "\r\n");
                    //strbFile.Append("lossnum=" + lossnum.ToString() + "\r\n");
                    //strbFile.Append("dupacknum=" + dupacknum.ToString() + "\r\n");
                    //strbFile.Append("misordernum=" + misordernum.ToString() + "\r\n");
                    //strbFile.Append("retransmitnum=" + retransmitnum.ToString() + "\r\n");
                }
                else strbFile.Append("总包数丢失...\r\n");
            }
            else
            {
                strbFile.Append("包指标分析失败...\r\n");
            }
        }


    }
}
