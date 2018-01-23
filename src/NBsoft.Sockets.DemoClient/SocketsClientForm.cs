using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NBsoft.Sockets.DemoClient
{
    public partial class SocketsClientForm : Form
    {
        SocketClient client;
        // buffer manager
        
        public SocketsClientForm()
        {
            InitializeComponent();
            this.FormClosed += SocketsClientForm_FormClosed;
            client = new SocketClient();
            client.Connected += Client_Connected;
            client.Disconnected += Client_Disconnected;
            client.Error += Client_Error;
            client.DataReceived += Client_DataReceived;
        }

        private void SocketsClientForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            client.Disconnect();
            client.Dispose();
        }

        private void Client_DataReceived(object sender, SockMessageEventArgs e)
        {
            //Addind data to visual control must be run in form thread.
            // Invoke it
            this.Invoke((MethodInvoker)(() =>
            {
                string msg = Encoding.Unicode.GetString(e.SockMessage);
                AddLine(msg);
            }));
;        }

        private void Client_Error(object sender, ErrorEventArgs e)
        {
            MessageBox.Show(e.Exception.Message);
        }

        private void Client_Disconnected(object sender, EventArgs e)
        {
            AddLine("Client Disconected");
        }

        private void Client_Connected(object sender, EventArgs e)
        {
            AddLine("Client Connected");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!client.IsConnected)
            {
                //Connect to localhost on port 5550(demo server)
                client.Connect(IPAddress.Parse("127.0.0.1"), 5550);
                button1.Text = "Disconnect";
                label1.Text = $"connected to {client.RemoteEndPoint}";
            }
            else
            {
                client.Disconnect();
                button1.Text = "Connect";
                label1.Text = "disconnected";
            }
        }
        private void AddLine(string line)
        {
            listBox1.Items.Add(line);
            listBox1.SelectedIndex = listBox1.Items.Count - 1;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SendText();
        }

        private void SendText()
        {
            if (textBox1.Text.Length > 0)
                client.Send(Encoding.Unicode.GetBytes(textBox1.Text));
            textBox1.Text = "";
        }

        private void textBox1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                SendText();
        }
    }
}
