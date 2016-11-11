using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RC.Core.Sockets
{
    /// <summary>
    /// Socket参数配置
    /// </summary>
    public class TcpSocketConfig
    {
        /// <summary>
        /// 结束标识符 &lt;EOF&gt;
        /// </summary>
        public const string END_FLAG = "<EOF>";

        /// <summary>
        /// 
        /// </summary>
        private int maxConnQueue = 100;

        /// <summary>
        /// 
        /// </summary>
        private Encoding encoding = Encoding.UTF8;

        /// <summary>
        /// IPAddress
        /// </summary>
        public string Ip { get; set; }

        /// <summary>
        /// 端口
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 最大连接数（默认为100）
        /// </summary>
        public int MaxConnQueue
        {
            get
            {
                return this.maxConnQueue;
            }
            set
            {
                this.maxConnQueue = value;
            }
        }

        /// <summary>
        /// 字符串编码（默认UTF-8）
        /// </summary>
        public Encoding Encoding
        {
            get
            {
                return this.encoding;
            }
            set
            {
                this.encoding = value;
            }
        }

        /// <summary>
        /// 发送完成后的回调函数
        /// </summary>
        public Action<Object> SendCompleteCallback { get; set; }

        /// <summary>
        /// 读取完成后的回调函数
        /// </summary>
        public Action<string> ReadCompleteCallback { get; set; }

        /// <summary>
        /// 读取时发生异常时的回调函数
        /// </summary>
        public Action<Exception> ReadExceptionCallback { get; set; }

        /// <summary>
        /// 发送时发生异常时的回调函数（带业务对象）
        /// </summary>
        public Action<Object, Exception> SendExceptionCallback { get; set; }
    }
}
