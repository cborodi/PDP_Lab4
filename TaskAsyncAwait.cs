using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PDP_Lab4_TestImplementation
{
    class TaskAsyncAwait
    {
        private static List<string> hosts;

        public static void run(List<string> hostnames)
        {
            hosts = hostnames;
            var tasks = new List<Task>();

            for (var i = 0; i < hostnames.Count; i++)
            {
                tasks.Add(Task.Factory.StartNew(DoStartAsync, i));
            }

            Task.WaitAll(tasks.ToArray());
        }

        private static void DoStartAsync(object idObject)
        {
            var id = (int)idObject;

            StartAsyncClient(hosts[id], id);
        }

        private static async void StartAsyncClient(string host, int id)
        {
            var ipHostInfo = Dns.GetHostEntry(host.Split('/')[0]);
            var ipAddress = ipHostInfo.AddressList[0];
            var remoteEndpoint = new IPEndPoint(ipAddress, 80);

            var client = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp); // create client socket

            var requestSocket = new Custom_Socket
            {
                sock = client,
                hostname = host.Split('/')[0],
                endpoint = host.Contains("/") ? host.Substring(host.IndexOf("/", StringComparison.Ordinal)) : "/",
                remoteEndPoint = remoteEndpoint,
                id = id
            };

            await ConnectAsync(requestSocket);

            await SendAsync(requestSocket,
                Parser.GetRequestString(requestSocket.hostname, requestSocket.endpoint));

            await ReceiveAsync(requestSocket);

            Console.WriteLine("Connection {0}. Content-length is:{1}", requestSocket.id, Parser.GetLength(requestSocket.responseContent.ToString()));

            client.Shutdown(SocketShutdown.Both);
            client.Close();
        }

        private static async Task ConnectAsync(Custom_Socket state)
        {
            state.sock.BeginConnect(state.remoteEndPoint, ConnectCallback, state);

            await Task.FromResult<object>(state.connectDone.WaitOne());
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

        private static async Task SendAsync(Custom_Socket state, string data)
        {
            var byteData = Encoding.ASCII.GetBytes(data);

            state.sock.BeginSend(byteData, 0, byteData.Length, 0, SendCallback, state);

            await Task.FromResult<object>(state.sendDone.WaitOne());
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

        private static async Task ReceiveAsync(Custom_Socket state)
        {
            state.sock.BeginReceive(state.buffer, 0, Custom_Socket.BUFF_SIZE, 0, ReceiveCallback, state);

            await Task.FromResult<object>(state.receiveDone.WaitOne());
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
