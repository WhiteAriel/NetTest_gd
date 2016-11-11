using System;
using System.Collections.Generic;
using System.Text;
using System;
using System.IO;
using System.IO.Pipes;
using System.Diagnostics;

namespace NetTest
{
   
    public class CMyPipe
    {
        private string m_PipeName;//管道名全称
        public string PipeName
        {
            get { return m_PipeName; }
            set { m_PipeName = value; }
        }
 
        private IntPtr m_HPipe;//管道句柄
        public IntPtr HPipe
        {
            get { return m_HPipe; }
            set { m_HPipe = value; }
        }
 
        public CMyPipe()//无参构造函数
        {
            m_HPipe = (IntPtr)NamedPipeNative.INVALID_HANDLE_VALUE;
            m_PipeName = "\\\\.\\pipe\\robinTest";
        }
 
        public CMyPipe(string pipeName)//有参构造函数
        {
            m_HPipe = (IntPtr)NamedPipeNative.INVALID_HANDLE_VALUE;
            m_PipeName = pipeName;
        }
 
        ~CMyPipe()//析构函数
        {
            Dispose();
        }
 
        public void Dispose()
        {
            lock (this)
            {
                if (m_HPipe != (IntPtr)NamedPipeNative.INVALID_HANDLE_VALUE)
                {
                    NamedPipeNative.CloseHandle(m_HPipe);
                    m_HPipe = (IntPtr)NamedPipeNative.INVALID_HANDLE_VALUE;
                }
            }
        }
 
        public void CreatePipe()//创建管道
        {
            m_HPipe = NamedPipeNative.CreateNamedPipe(m_PipeName,
                            NamedPipeNative.PIPE_ACCESS_DUPLEX,         // 数据双工通信
                            NamedPipeNative. PIPE_TYPE_MESSAGE | NamedPipeNative.PIPE_WAIT,                            100,                         // 最大实例个数
                            128,                           // 流出数据缓冲大小
                            128,                           // 流入数据缓冲大小
                            150,                         // 超时，毫秒
                            IntPtr.Zero                  // 安全信息
                            );
            if (m_HPipe.ToInt32() == NamedPipeNative.INVALID_HANDLE_VALUE)
            {
                frmServer.ActivityRef.AppendText("创建命名管道失败" );
                frmServer.ActivityRef.AppendText(Environment.NewLine);
                return;
            }
            frmServer.ActivityRef.AppendText("创建命名管道完毕" );
            frmServer.ActivityRef.AppendText(Environment.NewLine);           
        }
 
        public void ReadCurveData()//读取曲线数据
        {
            int nCurvesToRead = 0;
            nCurvesToRead = ReadInt(HPipe);//先读取需要读取的曲线条数,如
            frmServer.CurveDataList.Clear();
            for (int j = 1; j <= nCurvesToRead; j++)
            {
                string curveName = ReadString(HPipe);//读取该曲线名称
                int nCount = 0;
                nCount = ReadInt(HPipe);//读取该曲线的数据总数（数组大小）
                double[] readData = new double[nCount];
                for (int i = 0; i < nCount; i++)
                {
                    readData[i] = ReadDouble(HPipe);//顺序读取曲线的数据
                }
                CCurve newCurve = new CCurve();
                newCurve.CurveName = curveName;
                newCurve.CurveData = readData;
                frmServer.CurveDataList.Add(newCurve);
            }
        }
 
        public void ReadTextInfo()//读取文本信息
        {
            string textInfo = ReadString(HPipe);//读取该曲线名称
            frmServer.ActivityRef.AppendText(textInfo + Environment.NewLine);
            frmServer.ActivityRef.AppendText(Environment.NewLine);
        }
 
        public void ReadData()
        {
            //read data
            int flag = -1;
            flag = ReadInt(HPipe);//获取当前要读取的数据的信息
            if (flag == 0)//flag==0表示读取曲线数据
            {
                ReadCurveData();
            }
            else if (flag == 1)//flag==1表示读取文本信息
            {
                ReadTextInfo();
            }
        }
 
//写字符串，由于字符串的大小不同，所以先将字符串的大小写入，然后写字符串内容，对应的，
//在c++端，读字符串时先读取字符数组大小，从而给字符数组开辟相应的空间，然后读取字符串内容。
        public bool WriteString(IntPtr HPipe, string str)
        {
            byte[] Val = System.Text.Encoding.UTF8.GetBytes(str);
            byte[] dwBytesWrite = new byte[4];
            int len = Val.Length;
            byte[] lenBytes = System.BitConverter.GetBytes(len);
            if (NamedPipeNative.WriteFile(HPipe, lenBytes, 4, dwBytesWrite, 0))
                return (NamedPipeNative.WriteFile(HPipe, Val, (uint)len, dwBytesWrite, 0));
            else
                return false;
        }
 
        public string ReadString(IntPtr HPipe)
        {
            string Val = "";
            byte[] bytes = ReadBytes(HPipe);
            if (bytes != null)
            {
                Val = System.Text.Encoding.UTF8.GetString(bytes);
            }
            return Val;
        }
 
        public byte[] ReadBytes(IntPtr HPipe)
        {
            //传字节流
            byte[] szMsg = null;
            byte[] dwBytesRead = new byte[4];
            byte[] lenBytes = new byte[4];
            int len;
            if (NamedPipeNative.ReadFile(HPipe, lenBytes, 4, dwBytesRead, 0))//先读大小
            {
                len = System.BitConverter.ToInt32(lenBytes, 0);
                szMsg = new byte[len];
                if (!NamedPipeNative.ReadFile(HPipe, szMsg, (uint)len, dwBytesRead, 0))
                {
                    //frmServer.ActivityRef.AppendText("读取数据失败");
                    return null;
                }
            }
            return szMsg;
        }
 
        public float ReadFloat(IntPtr HPipe)
        {
            float Val = 0;
            byte[] bytes = new byte[4]; //单精度需4个字节存储
            byte[] dwBytesRead = new byte[4];
            if (!NamedPipeNative.ReadFile(HPipe, bytes, 4, dwBytesRead, 0))
            {
                //frmServer.ActivityRef.AppendText("读取数据失败");
                return 0;
            }
            Val = System.BitConverter.ToSingle(bytes, 0);
            return Val;
        }
 
        public double ReadDouble(IntPtr HPipe)
        {
            double Val = 0;
            byte[] bytes = new byte[8]; //双精度需8个字节存储
            byte[] dwBytesRead = new byte[4];
 
            if (!NamedPipeNative.ReadFile(HPipe, bytes, 8, dwBytesRead, 0))
            {
                //frmServer.ActivityRef.AppendText("读取数据失败");
                return 0;
            }
            Val = System.BitConverter.ToDouble(bytes, 0);
            return Val;
        }
 
        public int ReadInt(IntPtr HPipe)
        {
            int Val = 0;
            byte[] bytes = new byte[4]; //整型需4个字节存储
            byte[] dwBytesRead = new byte[4];
 
            if (!NamedPipeNative.ReadFile(HPipe, bytes, 4, dwBytesRead, 0))
            {
                //frmServer.ActivityRef.AppendText("读取数据失败");
                return 0;
            }
            Val = System.BitConverter.ToInt32(bytes, 0);
            return Val;
        }
    }

}
