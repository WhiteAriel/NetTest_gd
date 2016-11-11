using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace RC.Core.Sockets
{
    /// <summary>
    /// Socket服务端
    /// </summary>
    public class TcpSocketServer
    {
        /// <summary>
        /// Thread signal
        /// </summary>
        private ManualResetEvent allDone = new ManualResetEvent(false);

        /// <summary>
        /// 基本配置
        /// </summary>
        private TcpSocketConfig config = null;

        /// <summary>
        /// 创建一个服务端socket监听对象
        /// </summary>
        /// <param name="config"></param>
        public TcpSocketServer(TcpSocketConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            //if (string.IsNullOrEmpty(config.Ip))
            //{
            //    throw new ArgumentNullException("config.Ip");
            //}

            if (config.Port <= 0)
            {
                throw new ArgumentException("config.port error");
            }

            this.config = config;
        }

        /// <summary>
        /// 开始监听
        /// </summary>
        public void StartListening()
        {
            Thread thread = new Thread(ThreadStart);
            thread.Start();
        }

        /// <summary>
        /// 线程启动监听
        /// </summary>
        private void ThreadStart()
        {
            // Create a TCP/IP socket.
            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                if (string.IsNullOrEmpty(config.Ip))
                    listener.Bind(new IPEndPoint(IPAddress.Any,config.Port));
                else
                    listener.Bind(new IPEndPoint(IPAddress.Parse(config.Ip), config.Port));
                listener.Listen(config.MaxConnQueue);

                while (true)
                {
                    // Set the event to nonsignaled state.
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.
                    Console.WriteLine("Waiting for a connection...");
                    listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

                    // Wait until a connection is made before continuing.
                    allDone.WaitOne();
                }
            }
            catch (Exception e)
            {
                if (config.ReadExceptionCallback != null)
                {
                    config.ReadExceptionCallback(e);
                }
            }
        }

        /// <summary>
        /// 接收到请求的回调方法
        /// </summary>
        /// <param name="ar"></param>
        public void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.
            allDone.Set();

            // Get the socket that handles the client request.
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Create the state object.
            SocketStateObject state = new SocketStateObject();
            state.workSocket = handler;

            // Read data
            handler.BeginReceive(state.buffer, 0, SocketStateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
        }

        /// <summary>
        /// 读取数据的回调方法
        /// </summary>
        /// <param name="ar"></param>
        public void ReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;

            // Retrieve the state object and the handler socket from the asynchronous state object.
            SocketStateObject state = (SocketStateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket. 
            int bytesRead = handler.EndReceive(ar);
            if (bytesRead > 0)
            {
                // There might be more data, so store the data received so far.
                state.sb.Append(config.Encoding.GetString(state.buffer, 0, bytesRead));

                // Check for end-of-file tag. If it is not there, read more data
                content = state.sb.ToString();
                if (content.IndexOf(TcpSocketConfig.END_FLAG) > -1)
                {
                    if (config.ReadCompleteCallback != null)
                    {
                        content = content.Substring(0, content.Length - TcpSocketConfig.END_FLAG.Length);
                        config.ReadCompleteCallback(content);
                    }
                }
                else
                {
                    // Not all data received. Get more.
                    handler.BeginReceive(state.buffer, 0, SocketStateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
                }
            }
        }
    }
}
