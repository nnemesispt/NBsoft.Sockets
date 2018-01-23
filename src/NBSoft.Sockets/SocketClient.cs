using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace NBsoft.Sockets
{
    public class SocketClient : SocketClientBase
    {
        #region Variables

        IPEndPoint iPEndPoint;              // Remote host endpoint
        Socket sock;                        // Socket        
        List<byte> _ReceivedMessage;        // Incoming message
        bool isConnected;                   // is Connected Flag        
        bool isDisposing;
        bool stopped;
        int sendSize;

        bool isFile = false;                // is receiving file flag
        string fileName;                    // receiving file name
        string directoryName;               // receiving file dir
        DateTime lastWriteTimeUTC;          // receiving last write date
        ulong fileSendSize = 0;
        System.IO.FileStream file;          // receiving file stream
        //int ctr;
        ulong bytesSent;
        ulong bytesReceived;

        CompressionType isCompressed = 0x00;

        #endregion

        #region Constructor

        /// <summary>
        /// Base constructor, buffer manager to boost performance.
        /// </summary>
        /// <param name="Manager">Existing System.ServiceModel.Channels.BufferManager</param>
        public SocketClient()
        {
            sock = null;
            isConnected = false;            
            isDisposing = false;

            bytesSent = 0;
            bytesReceived = 0;
        }
        ~SocketClient()
        {
            Clear();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Clears all resources and sets disposed flag to true
        /// </summary>
        private void Clear()
        {
            if (isDisposing)
                return;
            isDisposing = true;
            if (isConnected)
            {
                try { Disconnect(); }
                catch { }
            }
            try { _ReceivedMessage.Clear(); }
            catch { }
            iPEndPoint = null;
            sock = null;
            
            _ReceivedMessage = null;
            
        }

        /// <summary>
        /// Creaates a new connection to the remote host
        /// </summary>
        private void Connect()
        {            
            if (!sock.Connected)
                throw new ApplicationException("Cannot assign a closed socket");

            //sock = Sock;
            sock.Ttl = 40;
            sock.SendTimeout = 500;
            sock.ReceiveTimeout = 500;
            //_Sock.LingerState = new LingerOption(true, 1);
            isConnected = sock.Connected;

            OnConnected(new EventArgs());
            stopped = false;
            OnLogEntry(new LogEventArgs(string.Format("New Socket connection from [{0}]", sock.RemoteEndPoint), DateTime.Now));
            Receive();
        }

        public override void Connect(IPAddress ServerAddress, int Port)
        {
            iPEndPoint = new IPEndPoint(ServerAddress, Port);
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sock.Connect(iPEndPoint);
            Connect();
        }

        /// <summary>
        /// Disconnects from remote host
        /// </summary>
        public override void Disconnect()
        {
            Disconnect(null);
        }
        /// <summary>
        /// Private Disconnect overload for error handling
        /// </summary>
        /// <param name="ex01"></param>
        private void Disconnect(Exception ex)
        {
            if (ex != null)
            {
                System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace();
                string MethodName;
                try { MethodName = st.GetFrame(1).GetMethod().Name; }
                catch { MethodName = ""; }
                OnError(new ErrorEventArgs(ex, MethodName));
            }

            stopped = true;
            if (!isConnected || sock == null)
                return;

            try
            {                
                sock.Close();       // Close Socket
                sock.Dispose();     // Dispose socket
            }
            catch (Exception ex01)
            {
                OnError(new ErrorEventArgs(ex01, "Sock.Close()"));
            }
            sock = null;           
            isConnected = false;

            OnDisconnected(new EventArgs());   // Raise Disconnected event
        }

        
        /// <summary>
        /// Sends data to remote host asynchronously.
        /// </summary>
        /// <param name="Msg">Data to send</param>
        public override void Send(byte[] msg)
        {
            try
            {
                sendSize = msg.Length;
                StateObject so = new StateObject(sock);            // Create message object
                //so.Buffer = bManager.TakeBuffer(msg.Length + 10);   // Retrieve buffer from manager
                so.Buffer = new byte[msg.Length];                   // Retrieve buffer from manager
                Array.Copy(msg, so.Buffer, msg.Length);             // Copy message to buffer            
                so.Sock.BeginSend(so.Buffer, 0, msg.Length,         // Send message async
                    SocketFlags.None, EndSend, so);
                msg = null;                                         // Force clear message array
            }
            catch (Exception ex)
            {
                if (stopped)
                {
                    // We forcefully closed this socket therefore this exception was expected and we can ignore it
                }
                else
                {
                    Disconnect(ex);
                }
            }
        }

        public override void SendFile(System.IO.FileInfo File)
        {
            SendFile(File, "", CompressionType.NoCompression);
        }
        public override void SendFile(System.IO.FileInfo File, string Directory, CompressionType Compression)
        {
            // File Signature
            byte[] header = new byte[] { 0xFF, 0xFE, 0xFD, 0xFC };
            
            // File Name            
            byte[] fName = System.Text.Encoding.Unicode.GetBytes(File.Name);
            
            // Directory
            if (Directory == null)
                Directory = "";
            byte[] dName = System.Text.Encoding.Unicode.GetBytes(Directory);

            List<byte> hdr = new List<byte>();
            hdr.AddRange(header);
            hdr.Add((byte)Compression);
                        
            hdr.AddRange(BitConverter.GetBytes((int)fName.Length));
            hdr.AddRange(fName);
            
            hdr.AddRange(BitConverter.GetBytes((int)dName.Length));
            hdr.AddRange(dName);

            byte[] lastwrite = BitConverter.GetBytes(File.LastWriteTimeUtc.ToBinary());
            hdr.AddRange(lastwrite);

            //Console.WriteLine("{0}| FileSize: {1}", DateTime.Now.ToString("HH:mm:ss.fff"), File.Length);
            
            // Compress file
            System.IO.FileInfo fileTosend;
            
            switch (Compression)
            {                
                case CompressionType.NoCompression:
                    fileTosend = File;                    
                    break;
                case CompressionType.GZCompression:
                    fileTosend = Compressor.CompressGZip(File);
                    break;
                default:
                case CompressionType.LZ4Compression:
                    fileTosend = Compressor.CompressLZ4(File);
                    break;                
            }
            SendFile(fileTosend.FullName, hdr.ToArray(), header, false, Compression);

        }
        
        protected override void SendFile(string FileName, byte[] header, byte[] footer, bool Async, CompressionType Compression)
        {
            try
            {
                fileSendSize = (uint)header.Length + (uint)footer.Length + (ulong)(new System.IO.FileInfo(FileName).Length);
                
                if (Async)                                    
                    sock.BeginSendFile(FileName, header, footer, TransmitFileOptions.UseSystemThread, EndSendFile, sock);                
                else
                {
                    sock.SendFile(FileName, header, footer, TransmitFileOptions.UseDefaultWorkerThread);
                    bytesSent = bytesSent + fileSendSize;
                    //Console.WriteLine("Sent File: [{0}] Total:[{1}]", fileSent, _bytesSent);
                    if (Compression != CompressionType.NoCompression)
                        try { System.IO.File.Delete(FileName); }
                        catch { }
                }
            }
            catch (Exception ex)
            {
                if (stopped)
                {
                    // We forcefully closed this socket therefore this exception was expected and we can ignore it
                }
                else
                {
                    Disconnect(ex);
                }
            }
        }
        /// <summary>
        /// BeginSend async callback
        /// </summary>
        /// <param name="ar">IAsyncResult</param>
        private void EndSend(IAsyncResult ar)
        {
            StateObject so = (StateObject)ar.AsyncState;        // Message object
            try
            {                
                so.Sock.EndSend(ar);
                if (isDisposing)                                      // If callback is called after instance 
                    return;                                         // disposal then abort.
                
                bytesSent = bytesSent + (uint)sendSize;                
                OnDataSent(new SockMessageEventArgs(so.Buffer));        // Raise DataSent event
                //bManager.ReturnBuffer(so.Buffer);                   // Return buffer to manager
                so.Buffer = null;
            }
            catch (Exception ex01)
            {
                if (stopped)
                {
                    // We forcefully closed this socket therefore this exception was expected and we can ignore it
                }
                else if (ex01 is SocketException)
                {
                    SocketException SockEx = (SocketException)ex01;

                    string endpoint;
                    try
                    {
                        endpoint = so.Sock.RemoteEndPoint.ToString();
                    }
                    catch { endpoint = "NullEndPoint"; }
                    OnLogEntry(new LogEventArgs(string.Format("{0} - EndSend SocketError: [{1}] - {2}", endpoint, SockEx.ErrorCode, SockEx.Message), DateTime.Now));
                    

                    // Windows Sockets Error Codes
                    // http://msdn.microsoft.com/en-us/library/windows/desktop/ms740668%28v=vs.85%29.aspx
                    switch (SockEx.ErrorCode)
                    {

                        default:
                            Disconnect(ex01);
                            break;
                        case 10054:     // WSAECONNRESET - Connection reset by peer.
                            Disconnect();
                            break;
                        case 995:       // WSA_OPERATION_ABORTED - Overlapped operation aborted.                                                                                                               
                            break;
                    }
                }
                else
                {
                    Disconnect(ex01);
                }
            }
        }
        private void EndSendFile(IAsyncResult ar)
        {
            try
            {
                Socket s = (Socket)ar.AsyncState;
                s.EndSendFile(ar);                
                bytesSent = bytesSent + fileSendSize;                
            }
            catch (Exception ex01)
            {
                if (stopped)
                {
                    // We forcefully closed this socket therefore this exception was expected and we can ignore it
                }
                else
                {
                    Disconnect(ex01);
                }
            }
        }


        /// <summary>
        /// Receives data from remote host asynchronously.
        /// </summary>
        private void Receive()
        {         
            _ReceivedMessage = new List<byte>();        // Reset received message
            StateObject so = new StateObject(sock);    // Create message object            
            //so.Buffer = bManager.TakeBuffer(1024 * 8);      // Retrieve buffer from manager
            so.Buffer = new byte[1024 * 8];      // Retrieve buffer from manager
            try
            {
                // Receive data asynchronously
                so.Sock.BeginReceive(so.Buffer, 0, so.Buffer.Length,
                    SocketFlags.None, EndReceive, so);
            }
            catch (SocketException ex)
            {
                if (stopped)
                {
                    // We forcefully closed this socket therefore this exception was expected and we can ignore it
                }
                else
                {
                    Disconnect(ex);
                }
            }

        }
        /// <summary>
        /// BeginReceive async callback
        /// </summary>
        /// <param name="ar">IAsyncResult</param>
        private void EndReceive(IAsyncResult ar)
        {
            StateObject so = (StateObject)ar.AsyncState;    // Create message object
            int readedbytes = 0;                            // Bytes readed from remote host
            bool canceled = false;
            
            try
            {
                readedbytes = so.Sock.EndReceive(ar);
                bytesReceived = bytesReceived + (uint)readedbytes;                
            }            
            catch (Exception ex)
            {
                if (stopped)
                {
                    // We forcefully closed this socket therefore this exception was expected and we can ignore it
                }
                else if (ex is SocketException)
                {
                    SocketException SockEx = (SocketException)ex;

                    string endpoint;
                    try
                    {
                        endpoint = so.Sock.RemoteEndPoint.ToString();
                    }
                    catch { endpoint = "NullEndPoint"; }
                    OnLogEntry(new LogEventArgs(string.Format("{0} - EndReceive SocketError: [{1}] - {2}", endpoint, SockEx.ErrorCode, SockEx.Message), DateTime.Now));

                    // Windows Sockets Error Codes
                    // http://msdn.microsoft.com/en-us/library/windows/desktop/ms740668%28v=vs.85%29.aspx
                    switch (SockEx.ErrorCode)
                    {
                            
                        default:
                            Disconnect(ex);
                            break;
                        case 10054:     // WSAECONNRESET - Connection reset by peer.
                            Disconnect();
                            break;
                        case 995:       // WSA_OPERATION_ABORTED - Overlapped operation aborted.                            
                            Receive();
                            break;
                    }   
                }
                else
                {
                    Disconnect(ex);                    
                }
                canceled = true;
            }            
            if (isDisposing || canceled)
                return;

            byte[] msg = new byte[readedbytes];             // Byte array with correct size
            Array.Copy(so.Buffer, msg, readedbytes);        // Copy readed bytes from buffer to byte array
            //bManager.ReturnBuffer(so.Buffer);               // Return buffer to manager
            so.Buffer = null;
            _ReceivedMessage.AddRange(msg);                 // Append received bytes to ncoming message
            msg = null;                                     // Force byte array clear

            if (readedbytes == 0)       // If readed bytes equals zero 
            {
                // TODO: doesn't trigger at end of message
#if DEBUG
                Console.WriteLine("Readed ZERO");
#endif
                if (_ReceivedMessage.Count > 0)             // If incoming message has bytes then message has finished
                    ReceiveFinished(); 
                    
                else
                    Disconnect(new ApplicationException(    // If incoming message has 0 bytes then connection terminated
                        "Connection Lost"));

            }
            else // If readed bytes >0
            {              
                ReceiveFinished();                 
            }

        }
        
        /// <summary>
        /// Message finished.
        /// </summary>        
        private void ReceiveFinished()
        {
            try
            {                
                // Check Message size:
                if (_ReceivedMessage.Count >= 4)
                {

                    // File header received
                    if (!isFile && _ReceivedMessage[0] == 0xFF && _ReceivedMessage[1] == 0xFE && _ReceivedMessage[2] == 0xFD && _ReceivedMessage[3] == 0xFC)
                    {
                        OnLogEntry(new LogEventArgs(string.Format("Receiving File... Buffer Size: {0}", _ReceivedMessage.Count), DateTime.Now));
                        // 4 bytes = Header (0xFF 0xFE 0xFD 0xFC)
                        // 4 bytes (Int32) = FileName Size (FNS)
                        // FNS bytes (string) = File Name
                        // 4 bytes (Int32) = Directory Name Size (DNS)
                        // DNS bytes (string) = Directory Name
                        // x bytes = File Data bytes
                        // 4 bytes  = Footer (0xFF 0xFE 0xFD 0xFC)
                        isFile = true;                                             // Set the file flag as true
                        file = new System.IO.FileStream(System.IO.Path.GetTempFileName(), System.IO.FileMode.OpenOrCreate);
                        //Console.WriteLine("    >>  {0}-New File {1} ", DateTime.Now.ToString("HH:mm:ss.fff"), _file.Name);
                        //_File = new List<byte>();

                        //Check if header filename size bytes arrived:
                        if (_ReceivedMessage.Count >= 21)
                        {
                            byte Compressed = _ReceivedMessage[4];
                            isCompressed = (CompressionType)Compressed;
                            int fileNameSize = BitConverter.ToInt32(                    // Get the filename size
                                _ReceivedMessage.GetRange(5, 4).ToArray(), 0);


                            fileName = System.Text.Encoding.Unicode.GetString(         // Get the file name
                                _ReceivedMessage.GetRange(9, fileNameSize).ToArray());


                            int directoryNameSize = BitConverter.ToInt32(                    // Get the Directory name size
                                _ReceivedMessage.GetRange(9 + fileNameSize, 4).ToArray(), 0);


                            directoryName = System.Text.Encoding.Unicode.GetString(         // Get the Directory name
                                _ReceivedMessage.GetRange(13 + fileNameSize, directoryNameSize).ToArray());


                            long LastWriteTimeUTC = BitConverter.ToInt64(                   // Get the last write time to pass on tho the file
                                _ReceivedMessage.GetRange(13 + fileNameSize + directoryNameSize, 8).ToArray(), 0);
                            lastWriteTimeUTC = DateTime.FromBinary(LastWriteTimeUTC);

                            int HeaderSize = 21 + fileNameSize + directoryNameSize;     // Calculate the header size
                            _ReceivedMessage.RemoveRange(0, HeaderSize);                // Remove Header from received message so it doesn't get added to the file

                        }
                    }
                }

                if (isFile)
                {
                    // File Footer received
                    // File Is Finished
                    if (_ReceivedMessage[_ReceivedMessage.Count - 4] == 0xFF && _ReceivedMessage[_ReceivedMessage.Count - 3] == 0xFE && _ReceivedMessage[_ReceivedMessage.Count - 2] == 0xFD && _ReceivedMessage[_ReceivedMessage.Count - 1] == 0xFC)
                    {                        
                        file.Write(_ReceivedMessage.GetRange(0, _ReceivedMessage.Count - 4).ToArray(), 0, _ReceivedMessage.Count - 4);
                        long fsize = file.Length;
                        file.Close();
#if DEBUG
                        Console.WriteLine("Received File: [{0}] Total:[{1}]", _ReceivedMessage.Count, bytesReceived);
#endif
                        string log = string.Format("File Receive Finished... Buffer Size: {0}. File Size: {1}", _ReceivedMessage.Count, fsize);
                        OnLogEntry(new LogEventArgs(log, DateTime.Now));                        
                        isFile = false;

                        // Decompress file
                        System.IO.FileInfo receivedFile;
                        switch (isCompressed)
                        {
                            case CompressionType.NoCompression:
                                receivedFile = new System.IO.FileInfo(file.Name);
                                break;
                            case CompressionType.GZCompression:                                
                                receivedFile = Compressor.DecompressGZip(new System.IO.FileInfo(file.Name));
                                break;
                            default:
                            case CompressionType.LZ4Compression:
                                receivedFile = Compressor.DecompressLZ4(new System.IO.FileInfo(file.Name));
                                break;
                        }
                        OnFileReceived(new FileEventArgs(fileName, receivedFile, directoryName, lastWriteTimeUTC));
                        fileName = null;                        
                        file = null;
                    }
                    else
                    {
                        // Write receive data to filestream
                        file.Write(_ReceivedMessage.ToArray(), 0, _ReceivedMessage.Count);
                    }
                }
                else
                {
                    // Raise DataReceived event
                    OnDataReceived(new SockMessageEventArgs(_ReceivedMessage.ToArray()));    
                }

                // Restart receive process
                Receive();
            }
            catch (Exception ex01)
            {
                Disconnect(ex01);
            }
        }


        #region IDisposable Members
        /// <summary>
        /// Clear resources
        /// </summary>
        public override void Dispose()
        {
            Clear();
        }

        #endregion







        #endregion

        #region Properties
        public string Name { get; set; }
        public override object Tag { get; set; }

        public bool IsConnected { get { return isConnected; } }
        protected Socket Sock { get { return sock; } }
        public override EndPoint LocalEndPoint { get { return sock.LocalEndPoint; } }
        public override EndPoint RemoteEndPoint { get { return sock.RemoteEndPoint; } }

        /// <summary>
        /// Total bytes send
        /// </summary>
        public ulong BytesSent { get { return bytesSent; } }

        /// <summary>
        /// Total Bytes Sent
        /// </summary>
        public ulong BytesReceived { get { return bytesReceived; } }
        #endregion

        #region Events
        /// <summary>
        /// Raised when data received from remote host
        /// </summary>
        public event MessageDelegate DataReceived;
        /// <summary>
        /// Raised when a file is received
        /// </summary>
        public event FileDelegate FileReceived;
        /// <summary>
        /// Raised when data is sent to remote host
        /// </summary>
        public event MessageDelegate DataSent;
        /// <summary>
        /// Raised when disconnected from remote host
        /// </summary>
        public event EventHandler Disconnected;
        /// <summary>
        /// Raised when connected to remote host
        /// </summary>
        public event EventHandler Connected;
        /// <summary>
        /// Raised when an error occurs on the sockets
        /// </summary>
        public event ErrorDelegate Error;
        /// <summary>
        /// Raised when log's are generated
        /// </summary>
        public event LogDelegate LogEntry;

        #endregion

        /// <summary>
        /// Assigns an existing socket connection to the current instance.
        /// </summary>
        /// <param name="sock"></param>
        public override void SetSocket(Socket sock)
        {
            this.sock = sock;
            Connect();
        }
        /// <summary>
        /// Raises the DataReceived event
        /// </summary>
        /// <param name="messageEventArgs"></param>
        protected override void OnDataReceived(SockMessageEventArgs e)
        {
            DataReceived?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the FileReceived event;
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFileReceived(FileEventArgs e)
        {
            FileReceived?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the DataSent event
        /// </summary>
        /// <param name="messageEventArgs"></param>
        protected override void OnDataSent(SockMessageEventArgs e)
        {
            DataSent?.Invoke(this, e);
        }
        /// <summary>
        /// Raises the Disconnected event
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDisconnected(EventArgs e)
        {
            EventHandler copy = Disconnected;

            if (copy != null)
                try { copy(this, e); }
                catch { }
        }
        /// <summary>
        /// Raises the Connected event
        /// </summary>
        /// <param name="e"></param>
        protected override void OnConnected(EventArgs e)
        {
            Connected?.Invoke(this, e);
        }
        /// <summary>
        /// Raises the error event
        /// </summary>
        /// <param name="e"></param>
        protected override void OnError(ErrorEventArgs e)
        {
            Error?.Invoke(this, e);
        }
        /// <summary>
        /// Raises the LogEntry event
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLogEntry(LogEventArgs e)
        {
            LogEntry?.Invoke(this, e);
        }


        
    }
}