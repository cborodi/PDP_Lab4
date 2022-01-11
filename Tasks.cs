using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PDP_Lab4_TestImplementation
{
    class Tasks
    {
        private static List<string> hosts;

        public static void run(List<string> hostnames)
        {
            hosts = hostnames;
            var tasks = new List<Task>();

            for (var i = 0; i < hostnames.Count; i++)
            {
                tasks.Add(Task.Factory.StartNew(DoStart, i));
            }

            Task.WaitAll(tasks.ToArray());
        }

        private static void DoStart(object idObject)
        {
            var id = (int)idObject;

            StartClient(hosts[id], id);
        }

        private static void StartClient(string host, int id)
        {
            var ipHostInfo = Dns.GetHostEntry(host.Split('/')[0]);
            var ipAddr = ipHostInfo.AddressList[0];
            var remEndPoint = new IPEndPoint(ipAddr, 80);

            var client = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            var requestSocket = new Custom_Socket
            {
                sock = client,
                hostname = host.Split('/')[0],
                endpoint = host.Contains("/") ? host.Substring(host.IndexOf("/", StringComparison.Ordinal)) : "/",
                remoteEndPoint = remEndPoint,
                id = id
            };

            Connect(requestSocket).Wait();
            Send(requestSocket, Parser.GetRequestString(requestSocket.hostname, requestSocket.endpoint)).Wait();
            Receive(requestSocket).Wait();

            String x = requestSocket.responseContent.ToString();

            Console.WriteLine("Connection {0} > Content-length is:{1}", requestSocket.id, Parser.GetLength(x));

            client.Shutdown(SocketShutdown.Both);
            client.Close();
        }

        private static Task Connect(Custom_Socket state)
        {
            state.sock.BeginConnect(state.remoteEndPoint, ConnectCallback, state);

            return Task.FromResult(state.connectDone.WaitOne());
        }

        private static void ConnectCallback(IAsyncResult ar)
        {
            var resultSocket = (Custom_Socket)ar.AsyncState;
            var clientSocket = resultSocket.sock;
            var clientId = resultSocket.id;
            var hostname = resultSocket.hostname;

            clientSocket.EndConnect(ar);

            Console.WriteLine("Connection {0} to {1} ({2})", clientId, hostname, clientSocket.RemoteEndPoint);

            resultSocket.connectDone.Set();
        }

        private static Task Send(Custom_Socket state, string data)
        {
            var byteData = Encoding.ASCII.GetBytes(data);

            state.sock.BeginSend(byteData, 0, byteData.Length, 0, SendCallback, state);

            return Task.FromResult(state.sendDone.WaitOne());
        }

        private static void SendCallback(IAsyncResult ar)
        {
            var resultSocket = (Custom_Socket)ar.AsyncState;
            var clientSocket = resultSocket.sock;
            var clientId = resultSocket.id;

            var bytesSent = clientSocket.EndSend(ar);

            Console.WriteLine("Connection {0}: {1} bytes sent to server.", clientId, bytesSent);

            resultSocket.sendDone.Set();
        }

        private static Task Receive(Custom_Socket state)
        {
            state.sock.BeginReceive(state.buffer, 0, Custom_Socket.BUFF_SIZE, 0, ReceiveCallback, state);

            return Task.FromResult(state.receiveDone.WaitOne());
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            var resultSocket = (Custom_Socket)ar.AsyncState;
            var clientSocket = resultSocket.sock;

            try
            {
                var bytesRead = clientSocket.EndReceive(ar);

                resultSocket.responseContent.Append(Encoding.ASCII.GetString(resultSocket.buffer, 0, bytesRead));

                if (!Parser.ResponseHeaderObtained(resultSocket.responseContent.ToString()))
                {
                    clientSocket.BeginReceive(resultSocket.buffer, 0, Custom_Socket.BUFF_SIZE, 0, ReceiveCallback, resultSocket);
                }
                else
                {
                    resultSocket.receiveDone.Set();    
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

        }
    }
}
