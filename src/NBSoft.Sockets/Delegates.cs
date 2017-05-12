
namespace NBsoft.Sockets
{
    public delegate void MessageDelegate(object sender, SockMessageEventArgs e);
    public delegate void FileDelegate(object sender, FileEventArgs e);
    public delegate void ErrorDelegate(object sender, ErrorEventArgs e);
    public delegate void ClientDelegate(object sender, ClientEventArgs e);
    public delegate void LogDelegate(object sender, LogEventArgs e);    
}
