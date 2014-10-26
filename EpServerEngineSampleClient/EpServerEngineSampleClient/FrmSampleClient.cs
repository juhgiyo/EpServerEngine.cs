using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using EpServerEngine.cs;
using System.Diagnostics;

namespace EpServerEngineSampleClient
{
    public partial class FrmSampleClient : Form,INetworkClientCallback
    {
        INetworkClient m_client = new IocpTcpClient();
        public FrmSampleClient()
        {
            InitializeComponent();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (btnConnect.Text.Equals("Connect"))
            {
                string hostname = tbHostname.Text;
                string port = tbPort.Text;
                tbHostname.Enabled = false;
                tbPort.Enabled = false;
                tbSend.Enabled = true;
                btnSend.Enabled = true;
                btnConnect.Text = "Disconnect";
                ClientOps ops = new ClientOps(this,hostname, port);
                m_client.Connect(ops);
            }
            else
            {
                tbHostname.Enabled = true;
                tbPort.Enabled = true;
                btnConnect.Text = "Connect";
                tbSend.Enabled = false;
                btnSend.Enabled = false;
                if (m_client.IsConnectionAlive())
                    m_client.Disconnect();
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            string sendText = tbSend.Text.Trim();
            if (sendText.Length <= 0)
            {
                MessageBox.Show("Please type in something to send.");
            }
            byte[] bytes = BytesFromString(sendText);
            Packet packet=new Packet(bytes,bytes.Count(),false);
            m_client.Send(packet);
        }

        public void OnConnected(INetworkClient client, ConnectStatus status)
        {
            MessageBox.Show("Connected to the server!");
        }

        public void OnReceived(INetworkClient client, Packet receivedPacket)
        {
            string sendString = StringFromByteArr(receivedPacket.GetPacket()) + "\r\n";
            AddMsg(sendString);
        }

        public void OnSent(INetworkClient client, SendStatus status)
        {
            switch (status)
            {
                case SendStatus.SUCCESS:
                    Debug.WriteLine("SEND Success");
                    break;
                case SendStatus.FAIL_CONNECTION_CLOSING:
                    Debug.WriteLine("SEND failed due to connection closing");
                    break;
                case SendStatus.FAIL_INVALID_PACKET:
                    Debug.WriteLine("SEND failed due to invalid socket");
                    break;
                case SendStatus.FAIL_NOT_CONNECTED:
                    Debug.WriteLine("SEND failed due to no connection");
                    break;
                case SendStatus.FAIL_SOCKET_ERROR:
                    Debug.WriteLine("SEND Socket Error");
                    break;
            }
        }

        public void OnDisconnect(INetworkClient client)
        {
            tbHostname.Enabled = true;
            tbPort.Enabled = true;
            btnConnect.Text = "Connect";
            tbSend.Enabled = false;
            btnSend.Enabled = false;
            MessageBox.Show("Disconnected from the server!");
        }

        delegate void AddMsg_Involk(string message);
        public void AddMsg(string message)
        {
            if (tbReceived.InvokeRequired)
            {
                AddMsg_Involk CI = new AddMsg_Involk(AddMsg);
                tbReceived.Invoke(CI, message);
            }
            else
            {
                tbReceived.Text += message + "\r\n";
            }
        }

        String StringFromByteArr(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }

        byte[] BytesFromString(String str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }
    }
}
