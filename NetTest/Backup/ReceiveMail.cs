using System;
using System.Collections.Generic;
using System.Text;
using OpenPOP.POP3;
using System.Collections;
using OpenPOP.MIMEParser;
using System.Windows.Forms;


namespace NetTest
{
    class ReceiveMail
    {

        private Hashtable msgs = new Hashtable();
        private POPClient popClient=new POPClient();
        private int count;
        private string POPServer, port, login, password;
        private OpenPOP.MIMEParser.Message m;
        private bool useSSL;

        public ReceiveMail(string POPServer, string port, string login, string password,bool SSL)
        {
            this.POPServer = POPServer;
            this.port = port;
            this.login = login;
            this.password = password;
            this.useSSL = SSL;
        }

        public OpenPOP.MIMEParser.Message currentMessage
        {
            get { return m; }
        }

        public bool setMessage(int index)
        {
            
            m = (OpenPOP.MIMEParser.Message)msgs[index];
            if (m == null)
                receiveMail(index);
            m = (OpenPOP.MIMEParser.Message)msgs[index];
                return m.HasRealAttachment;
            

        }

        #region 连接服务器
        //连接服务器，并返回邮件总数
        public int connect()
        {
            //连接POP3服务器
            OpenPOP.POP3.Utility.Log = true;
            popClient.Disconnect();
            popClient.useSSL = this.useSSL;
            popClient.Connect(POPServer, int.Parse(port),this.useSSL);
            popClient.Authenticate(login, password);
            
            
            //得到邮件总数
            count = popClient.GetMessageCount();
            msgs.Clear();
            return count;
        }
        #endregion

        #region 接收邮件
        public void receiveMail(int beginIndex,int endIndex)
        {
            //收取邮件
            for (int i =beginIndex ; i <=endIndex; i++)
            {
                receiveMail(i);
            }
        }

        public void receiveMail(int index)
        {
            if (index > count)
                index = count;
            OpenPOP.MIMEParser.Message m = popClient.GetMessage(index, false);
            try
            {
                msgs.Add(index, m);
            }
            catch (ArgumentException)
            {
                //MessageBox.Show(ex.Message);
            }
        }

        #endregion

        #region 删除邮件

        public void delMail(int index)
        {
            if (index > count)
                index = count;
            popClient.DeleteMessage(index);

        }

        public void delMail(int beginIndex, int endIndex)
        {
            for (int i = beginIndex; i <= endIndex; i--)
            {
                delMail(i);
            }
            MessageBox.Show("邮件删除完成");
        }

        public void delMail()
        {
            popClient.DeleteAllMessages();
            MessageBox.Show("邮箱已清空");
        }

        #endregion

        #region 附件相关

        public ArrayList getAttachmentName()
        {
            ArrayList attachmentName = new ArrayList();
            int count = m.AttachmentCount;
            for (int i = 0; i < count; i++)
            {
                if (m.GetAttachment(i).NotAttachment)
                    continue;
                attachmentName.Add(m.GetAttachmentFileName(m.GetAttachment(i)));
            }
            return attachmentName;
        }

        public void getAttachments(string savePath)
        {
            m.SaveAttachments(savePath);
        }

        #endregion
    }
}
