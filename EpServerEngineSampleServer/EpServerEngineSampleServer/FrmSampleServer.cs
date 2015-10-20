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

namespace EpServerEngineSampleServer
{
    public partial class FrmSampleServer : Form, INetworkServerAcceptor, INetworkServerCallback, INetworkSocketCallback
    {
        INetworkServer m_server = new IocpTcpServer();
        public FrmSampleServer()
        {
            InitializeComponent();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (btnConnect.Text.Equals("Start"))
            {
                string port = tbPort.Text;
                tbPort.Enabled = false;
                btnConnect.Text = "Stop";
                tbSend.Enabled = true;
                btnSend.Enabled = true;
                ServerOps ops = new ServerOps(this, port,this);
                m_server.StartServer(ops);
            }
            else
            {
                tbPort.Enabled = true;
                btnConnect.Text = "Start";
                tbSend.Enabled = false;
                btnSend.Enabled = false;
                if(m_server.IsServerStarted)
                    m_server.StopServer();
            }
            

        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            string sendText= tbSend.Text.Trim();
            if (sendText.Length <= 0)
            {
                MessageBox.Show("Please type in something to send.");
            }
            byte[] bytes=BytesFromString(sendText);
            Packet packet=new Packet(bytes,0,bytes.Count(),false);
            m_server.Broadcast(packet);

        }

        public void OnServerStarted(INetworkServer server, StartStatus status)
        {
            if (status == StartStatus.FAIL_ALREADY_STARTED || status == StartStatus.SUCCESS)
            {
            }
            else
                MessageBox.Show("Unknown Error occurred");
            
        }

        public bool OnAccept(INetworkServer server, IPInfo ipInfo)
        {
            return true;
        }

        public INetworkSocketCallback GetSocketCallback()
        {
            return this;
        }

        public void OnServerAccepted(INetworkServer server, INetworkSocket socket)
        {
        }
        public void OnServerStopped(INetworkServer server)
        {
        }

        List<INetworkSocket> m_socketList = new List<INetworkSocket>();
        public void OnNewConnection(INetworkSocket socket)
        {
            m_socketList.Add(socket);
            String sendString = "** New user(" + socket.IPInfo.IPAddress + ") connected!";
            AddMsg(sendString);

            byte[] sendBuff = BytesFromString(sendString);

            foreach (var socketObj in m_socketList)
            {
                if (socketObj != socket)
                {
                    socketObj.Send(new Packet(sendBuff,0, sendBuff.Count(), false));
                }
            }
        }

        public void OnReceived(INetworkSocket socket, Packet receivedPacket)
        {
            string sendString = "User(" + socket.IPInfo.IPAddress + ") : " + StringFromByteArr(receivedPacket.PacketRaw);
            AddMsg(sendString);
            foreach (var socketObj in m_socketList)
            {
                if (socketObj != socket)
                {
                    socketObj.Send(receivedPacket);
                }
            }
        }



        public void OnSent(INetworkSocket socket, SendStatus status, Packet sentPacket)
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

        public void OnDisconnect(INetworkSocket socket)
        {
            m_socketList.Remove(socket);

            String sendString = "** User(" + socket.IPInfo.IPAddress + ") disconnected!";
            AddMsg(sendString);

            byte[] sendBuff= BytesFromString(sendString);
            
            foreach (var socketObj in m_socketList)
            {
                if (socketObj != socket)
                {
                    socketObj.Send(new Packet(sendBuff,0,sendBuff.Count(),false));
                }
            }
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
