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
    class Callbacks
    {
        public static void run(List<string> hostnames)
        {
            for (var i = 0; i < hostnames.Count; i++)
            {
                StartClient(hostnames[i], i);
                Thread.Sleep(1000);
            }
        }

        private static void StartClient(string host, int id)
        {
            var ipHostInfo = Dns.GetHostEntry(host.Split('/')[0]);
            var ipAddress = ipHostInfo.AddressList[0];
            var remoteEndpoint = new IPEndPoint(ipAddress, 80);

            var client = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            var requestSocket = new Custom_Socket
            {
                sock = client,
                hostname = host.Split('/')[0],
                endpoint = host.Contains("/") ? host.Substring(host.IndexOf("/")) : "/",
                remoteEndPoint = remoteEndpoint,
                id = id
            };

            requestSocket.sock.BeginConnect(requestSocket.remoteEndPoint, Connected, requestSocket);
        }

        private static void Connected(IAsyncResult ar)
        {
            var resultSocket = (Custom_Socket)ar.AsyncState;

            var clientSocket = resultSocket.sock;
            var clientId = resultSocket.id;
            var hostname = resultSocket.hostname;

            clientSocket.EndConnect(ar);
            Console.WriteLine("Connection {0} to {1} ({2})", clientId, hostname, clientSocket.RemoteEndPoint);

            var byteData = Encoding.ASCII.GetBytes(Parser.GetRequestString(resultSocket.hostname, resultSocket.endpoint));

            resultSocket.sock.BeginSend(byteData, 0, byteData.Length, 0, Sent, resultSocket);
        }

        private static void Sent(IAsyncResult ar)
        {
            var resultSocket = (Custom_Socket)ar.AsyncState;
            var clientSocket = resultSocket.sock;
            var clientId = resultSocket.id;

            var bytesSent = clientSocket.EndSend(ar);
            Console.WriteLine("Connection {0}: {1} bytes sent to server.", clientId, bytesSent);

            resultSocket.sock.BeginReceive(resultSocket.buffer, 0, Custom_Socket.BUFF_SIZE, 0, Receiving, resultSocket);
        }

        private static void Receiving(IAsyncResult ar)
        {
            var resultSocket = (Custom_Socket)ar.AsyncState;
            var clientSocket = resultSocket.sock;
            var clientId = resultSocket.id;

            try
            {
                var bytesRead = clientSocket.EndReceive(ar);

                resultSocket.responseContent.Append(Encoding.ASCII.GetString(resultSocket.buffer, 0, bytesRead));
                
                if (!Parser.ResponseHeaderObtained(resultSocket.responseContent.ToString()))
                {
                    clientSocket.BeginReceive(resultSocket.buffer, 0, Custom_Socket.BUFF_SIZE, 0, Receiving, resultSocket);
                }
                else
                {
                    Console.WriteLine("Content-length is:{0}", Parser.GetLength(resultSocket.responseContent.ToString()));

                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
