using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace NBsoft.Sockets
{
    public class SocketServer
    {

        #region Variables        
        protected List<SocketClientBase> _Clients;
        protected Int64 _ClientCounter;
        private Socket _Listener;
        private IPEndPoint _EndPoint;
        private IAsyncResult _LastRes;
        private bool _Waiting;


        #endregion

        #region Constructor
        public SocketServer(IPEndPoint Endpoint)
        {
            _Listener = null;            
            _Clients = new List<SocketClientBase>();
            _EndPoint = Endpoint;
            _Waiting = false;
            _ClientCounter = 0;

        }
        #endregion

        #region Methods
        public void Listen()
        {
            Listen(false, 1024 * 8, 1024 * 8, 1000, 1000);
        }
        public void Listen(bool noNagle, int receiveBufferSize, int sendBufferSize, int receiveTimeout, int sendTimeout)
        {
            _Listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            try
            {
                _Listener.NoDelay = noNagle;
                _Listener.ExclusiveAddressUse = true;
                _Listener.ReceiveBufferSize = receiveBufferSize;
                _Listener.SendBufferSize = sendBufferSize;
                _Listener.ReceiveTimeout = receiveTimeout;
                _Listener.SendTimeout = sendTimeout;


                _Listener.Bind(_EndPoint);
                _Listener.Listen(100);
                _Waiting = true;
                _LastRes = _Listener.BeginAccept(
                        new AsyncCallback(ConnectionAccepted),
                        _Listener);
            }            
            catch
            {
                throw;
            }
        }
        private void ConnectionAccepted(IAsyncResult ar)
        {
            if (!_Waiting)
                return;
            _Waiting = false;
            _ClientCounter++;

            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);
            SocketClientBase NewConn = AcceptIncomingConnectionRequest(handler);
            _Clients.Add(NewConn);
            OnClientConnected(new ClientEventArgs((SocketClient)NewConn));
            _Listener.BeginAccept(
                           new AsyncCallback(ConnectionAccepted),
                           _Listener);
            _Waiting = true;
        }
        protected virtual SocketClientBase AcceptIncomingConnectionRequest(Socket IncomingRequestSocket)
        {
            SocketClient sclient = new SocketClient();
            sclient.Tag = Guid.NewGuid();
            sclient.SetSocket(IncomingRequestSocket);
            sclient.DataReceived += new MessageDelegate(sclient_DataReceived);
            sclient.FileReceived += new FileDelegate(sclient_FileReceived);
            sclient.Disconnected += new EventHandler(sclient_Disconnected);
            sclient.Error += new ErrorDelegate(sclient_Error);
            sclient.LogEntry += new LogDelegate(sclient_LogEntry);            
            return sclient;
        }

        
                
        public void AbortListen()
        {
            _Waiting = false;


            if (_Listener != null)
                _Listener.Close();
            _Listener = null;
            DisconnectAll();
        }

        private void DisconnectAll()
        {
            Thread.Sleep(50);
            while (_Clients.Count > 0)
            {
                if (_Clients.Count > 0)
                {
                    try
                    {
                        _Clients[0].Disconnect();
                        Thread.Sleep(20);
                    }
                    catch
                    {
                        try { _Clients.RemoveAt(0); }
                        catch { }
                    }
                    try { _Clients[0].Dispose(); }
                    catch { }
                }
            }
            _Clients.Clear();
        }

        public void SendAll(byte[] Message)
        {
            foreach (SocketClientBase client in _Clients)
            {
                SendData(Message, client);
                System.Threading.Thread.Sleep(50);
            }
        }
        public void SendOne(byte[] Message, SocketClientBase client)
        {
            SendData(Message, client);
        }

        public void KickClient(SocketClient client)
        {
            try
            {
                client.Disconnect();                                
            }
            catch  (Exception ex01)
            {
                Console.WriteLine("Error Kicking Client: {0}\n\r{1}", ex01.Message, ex01.StackTrace);
            }
        }

        private void SendData(byte[] Message, SocketClientBase client)
        {
            //Console.WriteLine("Server>S:{0}", Message.Length);
            try { client.Send(Message); }
            catch (Exception ex01)
            {
                OnClientError(new ErrorEventArgs(ex01, "SendData"));
                KickClient((SocketClient)client);
            }
        }
        public void SendFile(System.IO.FileInfo file, SocketClientBase client, CompressionType Compression)
        {
            try { client.SendFile(file, "", Compression); }
            catch (Exception ex01) { OnClientError(new ErrorEventArgs(ex01, "SendFile")); }
        }


        #endregion

        #region Event Handlers
        void sclient_Disconnected(object sender, EventArgs e)
        {
            SocketClient sdr = (SocketClient)sender;
            if (sdr == null)
                return;

            sdr.DataReceived -= sclient_DataReceived;
            sdr.Disconnected -= sclient_Disconnected;

            _Clients.Remove(sdr);
            
            OnClientDisconnected(new ClientEventArgs(sdr));                
            
            try { sdr.Dispose(); }
            catch { }
            sdr = null;
        }

        void sclient_DataReceived(object sender, SockMessageEventArgs e)
        {
            SocketClient sdr = (SocketClient)sender;
            if (sdr == null || !sdr.IsConnected)
                return;
            
            OnDataReceived(new ClientEventArgs(sdr, e.SockMessage));
        }
        void sclient_FileReceived(object sender, FileEventArgs e)
        {
            SocketClient sdr = (SocketClient)sender;
            if (sdr == null || !sdr.IsConnected)
                return;

            OnFileReceived(new ClientFileEventArgs(e.FileName, e.File, e.DirectoryName, e.LastWriteTimeUTC, sdr));

        }
        void sclient_LogEntry(object sender, LogEventArgs e)
        {
            OnClientLog(sender, e);
        }
        void sclient_Error(object sender, ErrorEventArgs e)
        {
            OnClientError(e);
        }
        #endregion

        protected virtual void OnClientDisconnected(ClientEventArgs e)
        {
            ClientDisconnected?.Invoke(this, e);
        }
        protected virtual void OnClientConnected(ClientEventArgs e)
        {
            ClientConnected?.Invoke(this, e);
        }
        protected virtual void OnDataReceived(ClientEventArgs e)
        {
            ClientDataReceived?.Invoke(this, e);
        }
        protected virtual void OnFileReceived(ClientFileEventArgs e)
        {
            ClientFileReceived?.Invoke(this, e);

        }
        protected virtual void OnClientLog(object sender, LogEventArgs e)
        {
            ClientLog?.Invoke(sender, e);
        }
        protected virtual void OnClientError(ErrorEventArgs e)
        {
            ClientError?.Invoke(this, e);
        }


        #region Accessors
        public SocketClientBase[] Clients { get { return _Clients.ToArray(); } }
        public bool AcceptingConnections { get { return _Waiting; } }
        #endregion

        #region Events
        public event ClientDelegate ClientDisconnected;
        public event ClientDelegate ClientConnected;
        public event ClientDelegate ClientDataReceived;
        public event FileDelegate ClientFileReceived;
        public event ErrorDelegate ClientError;
        public event LogDelegate ClientLog;
        #endregion
    }
}

