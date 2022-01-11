using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PDP_Lab4_TestImplementation
{
    class Custom_Socket
    {
        public ManualResetEvent connectDone = new ManualResetEvent(false);
        public ManualResetEvent sendDone = new ManualResetEvent(false);
        public ManualResetEvent receiveDone = new ManualResetEvent(false);

        public Socket sock = null;

        public const int BUFF_SIZE = 1024;

        public byte[] buffer = new byte[BUFF_SIZE];

        public StringBuilder responseContent = new StringBuilder();

        public int id;
        public string hostname;
        public string endpoint;

        public IPEndPoint remoteEndPoint;
    }
}
