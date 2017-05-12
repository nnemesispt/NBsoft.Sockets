using System;
using System.Collections.Generic;

namespace NBsoft.Sockets
{
    public class SockMessageEventArgs : EventArgs
    {
        byte[] message;
        public SockMessageEventArgs()
        {
            message = null;
        }
        public SockMessageEventArgs(byte[] Message)
            : this()
        {
            message = Message;
        }
        public byte[] SockMessage { get { return message; } set { message = value; } }
    }
    public class ErrorEventArgs : EventArgs
    {
        Exception _exception;
        string _senderMethod;
        public ErrorEventArgs()
        {
            _exception = null;
            _senderMethod = "";
        }
        public ErrorEventArgs(Exception Exception,string SenderMethod)
            : this()
        {
            _exception = Exception;
            _senderMethod = SenderMethod;
        }
        public Exception Exception { get { return _exception; } set { _exception = value; } }
        public string SenderMethod { get { return _senderMethod; } set { _senderMethod = value; } }

    }
    public class ClientEventArgs : EventArgs
    {
        SocketClient _client;
        Exception _exception;
        byte[] _data;
        public ClientEventArgs(SocketClient Client)
        {
            _client = Client;
            _data = null;
            _exception = null;
        }
        public ClientEventArgs(SocketClient Client, Exception ex)
            : this(Client)
        {
            _exception = ex;
        }
        public ClientEventArgs(SocketClient Client, byte[] Data)
            : this(Client)
        {
            _data = Data;
        }
        public ClientEventArgs(SocketClient Client, byte[] Data, Exception ex)
            : this(Client)
        {
            _data = Data;
            _exception = ex;
        }

        public SocketClient Connection { get { return _client; } }
        public Exception Error { get { return _exception; } }
        public byte[] Data { get { return _data; } }

    }
    public class FileEventArgs : EventArgs
    {
        string _FileName;
        string _DirectoryName;
        System.IO.FileInfo _File;
        DateTime _LastWriteTimeUTC;


        public FileEventArgs(string fileName, System.IO.FileInfo receivedFile, string directoryName, DateTime lastWriteTimeUTC)
        {
            _FileName = fileName;
            _File = receivedFile;
            _DirectoryName = directoryName;
            _LastWriteTimeUTC = lastWriteTimeUTC;
        }

        public string FileName { get { return _FileName; } set { _FileName = value; } }
        public System.IO.FileInfo File { get { return _File; } set { _File = value; } }
        public string DirectoryName { get { return _DirectoryName; } set { _DirectoryName = value; } }
        public DateTime LastWriteTimeUTC { get { return _LastWriteTimeUTC; } set { _LastWriteTimeUTC = value; } }

    }
    public class ClientFileEventArgs : FileEventArgs
    {
        SocketClientBase _Client;

        public ClientFileEventArgs(string fileName, System.IO.FileInfo receivedFile, string directoryName, DateTime lastWriteTimeUTC, SocketClientBase client)
            : base(fileName, receivedFile, directoryName, lastWriteTimeUTC)
        {
            _Client = client;
        }
        public SocketClientBase Client { get { return _Client; } set { _Client = value; } }

    }
    public class LogEventArgs:EventArgs
    {
        string logText;
        DateTime logEntryDate;
                
        public LogEventArgs(string LogText, DateTime LogEntryDate)
        {
            this.logEntryDate = LogEntryDate;
            this.logText = LogText;
        }

        public string LogText { get { return logText; } set { logText = value; } }
        public DateTime LogEntryDate { get { return logEntryDate; } set { logEntryDate = value; } }
    }
    //public class ProgressEventArgs : System.EventArgs
    //{
    //    double currentValue;
    //    double maxValue;
    //    object status;

    //    public ProgressEventArgs(double CurrentValue, double MaxValue, object Status)
    //    {
    //        currentValue = (ulong)CurrentValue;
    //        maxValue = (ulong)MaxValue;
    //        status = Status;
    //    }

    //    public double CurrentValue { get { return currentValue; } set { currentValue = value; } }
    //    public double MaxValue { get { return maxValue; } set { maxValue = value; } }
    //    public object Status { get { return status; } set { status = value; } }
    //}
}


