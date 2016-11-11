using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using NetLog;
using System.Threading;

namespace RC.Core.Sockets
{
    class TcpSocketClient
    {
        /// <summary>  
        /// 定义数据  
        /// </summary>  
        private byte[] result = new byte[1024];

        /// <summary>  
        /// 客户端IP  
        /// </summary>  
        private string ip;
        public string IP
        {
            get { return ip; }
            set { ip = value; }
        }

        /// <summary>  
        /// 客户端端口号  
        /// </summary>  
        private int port;
        public int Port
        {
            get { return port; }
            set { port = value; }
        }

        /// <summary>  
        /// IP终端  
        /// </summary>  
        private IPEndPoint ipEndPoint;

        /// <summary>  
        /// 客户端Socket  
        /// </summary>  
        private Socket mClientSocket;

        /// <summary>  
        /// 是否连接到了服务器  
        /// 默认为flase  
        /// </summary>  
        private bool isConnected = false;

        /// <summary>  
        /// 构造函数  
        /// </summary>  
        /// <param name="ip">IP地址</param>  
        /// <param name="port">端口号</param>  
        public TcpSocketClient(string ip, int port)
        {
            if (string.IsNullOrEmpty(ip))
                throw new ArgumentException(ip);
            this.ip = ip;
            this.port = port;
            try 
            {
            //初始化IP终端  
            this.ipEndPoint = new IPEndPoint(IPAddress.Parse(this.ip), this.port);
            //初始化客户端Socket  
            mClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            }
            catch (ArgumentNullException ex)
            {
                Log.Error(Environment.StackTrace, ex);
                throw new ArgumentNullException("ip is null");               
            }
            catch (FormatException ex)
            {
                Log.Error(Environment.StackTrace, ex);
                throw new FormatException("Wrong Ip Format");              
            }            
        }

        /// <summary>  
        /// 连接到服务器  
        /// </summary>  
        public void ConnectToServer()
        {
            //当没有连接到服务器时开始连接  
            int maxConnectCount = 5; //最多重连5次
            while (!isConnected && maxConnectCount > 0)
            {
                try
                {
                    //开始连接  
                    mClientSocket.Connect(this.ipEndPoint);
                    this.isConnected = true;
                }
                catch (Exception ex)
                {
                    //输出Debug信息  
                    Log.Warn(string.Format("暂时无法连接到服务器，错误信息为:{0}", ex.Message));
                    this.isConnected = false;
                    --maxConnectCount;
                }

                //等待5秒钟后尝试再次连接  
                Thread.Sleep(200);
                Log.Warn("正在尝试重新连接...");
            }
        }

        /// <summary>  
        /// 发送消息  
        /// </summary>  
        /// <param name="msg">消息文本</param>  
        public void SendMessage(string msg)
        {
            if (msg == string.Empty || this.mClientSocket == null) return;
            if (isConnected)
            {
                mClientSocket.Send(Encoding.UTF8.GetBytes(msg));
                Log.Info(string.Format("Seng Message:{0}", msg));
            }
            else
                Log.Warn(string.Format("Seng Message Fail:{0}", msg));
        }

        public bool IsConnect()
        {
            return isConnected;
        }

        public void ShutConnect()
        {
            if (mClientSocket == null && !isConnected) return;
            //mClientSocket.Shutdown(SocketShutdown.Both);
        }

    } 
}
