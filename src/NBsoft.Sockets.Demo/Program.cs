using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBsoft.Sockets.DemoServer
{
    class Program
    {
        static SocketServer server;
        const int port = 5550;
        static void Main(string[] args)
        {
            server = new SocketServer(new System.Net.IPEndPoint(System.Net.IPAddress.Any, port));
            server.ClientConnected += Server_ClientConnected;
            server.ClientDisconnected += Server_ClientDisconnected;
            server.ClientLog += Server_ClientLog;
            server.ClientDataReceived += Server_ClientDataReceived;
            server.ClientError += Server_ClientError;
            server.Listen();
            Console.WriteLine("Server started. Waiting connections on port {0}", port);
            Console.ReadLine();
        }

        private static void Server_ClientError(object sender, ErrorEventArgs e)
        {
            Log("ERROR", e.Exception.Message);
        }

        private static void Server_ClientDataReceived(object sender, ClientEventArgs e)
        {
            string msg = Encoding.Unicode.GetString(e.Data);
            string broadcastMsg = $"{e.Connection.Tag} > {msg}";
            Log("MESSAGE", broadcastMsg);

            // Forward message to all clients
            server.SendAll(Encoding.Unicode.GetBytes(broadcastMsg));
        }

        private static void Server_ClientLog(object sender, LogEventArgs e)
        {
            Log("INFO", e.LogText);
        }

        private static void Server_ClientDisconnected(object sender, ClientEventArgs e)
        {
            string msg = $"Client Disconnected: {e.Connection.Tag}.\n\rClientList:";
            foreach (var item in server.Clients)
            {
                msg += $"\n\r\t> {item.RemoteEndPoint} ({item.Tag})";
            }
            Log("INFO", msg);
            server.SendAll(Encoding.Unicode.GetBytes($"Client Left: {e.Connection.Tag}"));
        }

        private static void Server_ClientConnected(object sender, ClientEventArgs e)
        {
            string msg = $"Client Connected: {e.Connection.RemoteEndPoint}.\n\rClientList:";
            foreach (var item in server.Clients)
            {
                msg += $"\n\r\t> {item.RemoteEndPoint} ({item.Tag})";
            }
            Log("INFO", msg);
            server.SendAll(Encoding.Unicode.GetBytes($"Client Joined: {e.Connection.Tag}"));
        }

        private static void Log(string type, string text)
        {
            Console.WriteLine("{0}|{1}|{2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), type, text);
        }
    }
}
