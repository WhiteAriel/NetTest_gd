using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace RC.Core.Sockets
{
    /// <summary>
    /// State object for reading client data asynchronously
    /// </summary>
    public class SocketStateObject
    {
        /// <summary>
        /// Client  socket
        /// </summary>
        public Socket workSocket = null;

        /// <summary>
        /// Size of receive buffer
        /// </summary>
        public const int BufferSize = 1024;

        /// <summary>
        /// Receive buffer
        /// </summary>
        public byte[] buffer = new byte[BufferSize];

        /// <summary>
        /// Received data string
        /// </summary>
        public StringBuilder sb = new StringBuilder();
    }
}
