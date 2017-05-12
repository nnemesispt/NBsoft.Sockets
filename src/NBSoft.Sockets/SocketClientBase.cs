using System;
using System.Collections.Generic;
using System.Net.Sockets;


namespace NBsoft.Sockets
{
    public abstract class SocketClientBase : IDisposable
    {
        public abstract object Tag { get; set; }
        public abstract System.Net.EndPoint LocalEndPoint { get; }
        public abstract System.Net.EndPoint RemoteEndPoint { get; }

        public abstract void Connect(System.Net.IPAddress Address, int Port);
        public abstract void Disconnect();
        public abstract void Send(byte[] msg);
        public abstract void SendFile(System.IO.FileInfo File);
        public abstract void SendFile(System.IO.FileInfo File, string Directory, CompressionType Compression);
        protected abstract void SendFile(string FileName, byte[] Header, byte[] Footer, bool Async, CompressionType Compression);
        public abstract void SetSocket(Socket sock);


        protected abstract void OnDataReceived(SockMessageEventArgs e);
        protected abstract void OnFileReceived(FileEventArgs e);
        protected abstract void OnDataSent(SockMessageEventArgs e);
        protected abstract void OnDisconnected(EventArgs e);
        protected abstract void OnConnected(EventArgs e);
        protected abstract void OnError(ErrorEventArgs e);
        protected abstract void OnLogEntry(LogEventArgs e);
        

        #region IDisposable Members

        public abstract void Dispose();

        #endregion
    }
}
