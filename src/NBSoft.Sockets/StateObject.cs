using System;
using System.Net.Sockets;


namespace NBsoft.Sockets
{
    public class StateObject : IDisposable
    {
        private Socket _Sock;
        private byte[] _Buffer;
        public StateObject(Socket Sock)
        {
            _Sock = Sock;
        }
        public byte[] Buffer { get { return _Buffer; } set { _Buffer = value; } }
        public Socket Sock { get { return _Sock; } set { _Sock = value; } }


        #region IDisposable Members

        public void Dispose()
        {
            if (_Sock != null)
                try { _Sock.Dispose(); }
                catch { }
            _Sock = null;
            _Buffer = null;
        }

        #endregion
    }
}
