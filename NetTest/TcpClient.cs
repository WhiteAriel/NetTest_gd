using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ParseJsonandBuildJson1;
using System.Threading;
using System.Collections;


namespace socket_client
{

    public class TcpClientEx
    {
        private TcpClient clientSocket = default(TcpClient);

        //判断TcpClient是否连接
        private bool IsOnline()
        {
            try
            {       
                return !((clientSocket.Client.Poll(1000, SelectMode.SelectRead) && (clientSocket.Client.Available == 0)) || !clientSocket.Client.Connected);
            }
            catch (System.Exception ex)
            {
                return false;
            }
            
        }


        private bool InitTcpClient(TcpListener client)
        {
            try
            {
                client.Start();
                clientSocket = client.AcceptTcpClient();
                return true;
            }
            catch (System.Exception ex)
            {
                return false;
            }

        }

        //初始化TcpClient
        public TcpClientEx(String ListenIp, int ListenPort)
        {
            try
            {
                IPAddress ipAddress = IPAddress.Parse(ListenIp);
                TcpListener client = new TcpListener(ipAddress, ListenPort);
                InitTcpClient(client);
            }
            catch (System.Exception ex)
            {                           
            }

        }

        //监听服务器消息，有效消息写入队列中，并收到回复
        public bool ListenServerAndResponse(ref Queue<Data2> que, int recycle, string serverIp)
		{

            NetworkStream receiveStream=null;
            NetworkStream returnStream=null;
            byte[] bytesFrom = new byte[65536]; 
            List<Data2> taskList=new List<Data2>;
			while (true)
            {
                try
                {
                    if (clientSocket.IsOnline())
                    {       //当网络连接未中断时循环                     
                            receiveStream = clientSocket.GetStream();                                                                                 
                            receiveStream.Read(bytesFrom, 0, clientSocket.ReceiveBufferSize);
                            string dataFromServer = System.Text.Encoding.UTF8.GetString(bytesFrom);
                            dataFromServer = dataFromServer.TrimEnd('\0');
						    //数据格式"{Recycle:0/1,ServerIp:192.168.1.1:8088,Task:[ {Id:xxxx,Type:xxxxx,Url:xxxxxxxx },{………},{…………},{…………} ] }<EOF>"
                            dataFromServer= dataFromServer.Substring(0, dataFromServer.Length - 5);
                            int sign = ParseJsonandBuildJson.ParseJson(dataFromServer, taskList, ref recycle,ref serverIp);//解析从服务器端接收的json数据包

                            // sign 标志从服务器端发来的数据的格式的正确性 0表示空，1表示格式正确，2表示格式错误
                            switch (sign)
                            {
                                case 1:
                                    {
                                        //向服务器返回json格式的数据，并将数据写入流中
                                        if(clientSocket.IsOnline())
                                        {
                                            foreach(Data2 data in taskList)
                                            {
                                                 que.Enqueue(data);
                                                 string taskid=data.Id;
                                                 string  returnMsg = ParseJsonandBuildJson.BuildJson(taskid, 0, "Receive task "+taskid+" successfully!");//json的封装
                                                  returnStream = clientSocket.GetStream();
                                                  byte[] outStream = System.Text.Encoding.UTF8.GetBytes(returnMsg+"<EOF>");
                                                  returnStream.Write(outStream, 0, outStream.Length);
                                                  returnStream.Flush();
                                            }
                                        }
                                        else
                                            InitTcpClient(client);   //链接中断，重新链接	                                   
                                        break;               
                                    }
                                default: continue;
                            }
                        }
                    else
                        InitTcpClient(client);   //链接中断，重新链接	
                }
                catch(System.Exception ex)
                {
                    return false;
                }
		   }	
			return true;			
	    }
    }
}
