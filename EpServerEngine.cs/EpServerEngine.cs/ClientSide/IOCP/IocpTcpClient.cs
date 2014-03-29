using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Threading;
using System.Diagnostics;
using EpLibrary.cs;

namespace IocpServer
{
    public class IocpTcpClient : ThreadEx, ClientInterface
    {

        private TcpClient m_client=new TcpClient();
        private ClientOps m_clientOps = null;

        private Object m_generalLock = new Object();

        private Object m_sendLock = new Object();
        private Object m_sendQueueLock = new Object();
        private Queue<PacketTransporter> m_sendQueue = new Queue<PacketTransporter>();

        private ClientCallbackInterface m_callBackObj=null;
        private String m_hostName;
        private String m_port;
        private bool m_noDelay;
        private int m_waitTimeInMilliSec;

        private EventEx m_timeOutEvent = new EventEx(false, EventResetMode.AutoReset);
        private EventEx m_sendEvent = new EventEx();

        /// Message Size Packet;
        private Packet m_recvSizePacket = new Packet(null, 4);
        private Packet m_sendSizePacket = new Packet(null, 4, false);

        public IocpTcpClient():base()
        {

        }

        public IocpTcpClient(IocpTcpClient b)
            : base(b)
        {
            m_clientOps = b.m_clientOps;
        }
        ~IocpTcpClient()
        {
            if (IsConnectionAlive())
                Disconnect();
        }

        public String GetHostName()
        {
            lock (m_generalLock)
            {
                return m_hostName;
            }
        }

        public String GetPort()
        {
            lock (m_generalLock)
            {
                return m_port;
            }
            
        }
        private class CallbackException : Exception
        {
            public CallbackException()
                : base()
            {

            }
            public CallbackException(String message)
                : base(message)
            {

            }
        }
        protected override void execute()
        {
            ConnectStatus status = ConnectStatus.SUCCESS;
            try
            {
                lock (m_generalLock)
                {
                    if (IsConnectionAlive())
                    {
                        status = ConnectStatus.FAIL_ALREADY_CONNECTED;
                        throw new CallbackException();
                    }

                    m_callBackObj = m_clientOps.callBackObj;
                    m_hostName = m_clientOps.hostName;
                    m_port = m_clientOps.port;
                    m_noDelay = m_clientOps.noDelay;
                    m_waitTimeInMilliSec = m_clientOps.waitTimeInMilliSec;

                    if (m_hostName == null || m_hostName.Length == 0)
                    {
                        m_hostName = ServerConf.DEFAULT_HOSTNAME;
                    }

                    if (m_port == null || m_port.Length == 0)
                    {
                        m_port = ServerConf.DEFAULT_PORT;
                    }


                    m_client.NoDelay = m_noDelay;

                    m_client.Client.BeginConnect(m_hostName, Convert.ToInt32(m_port), new AsyncCallback(IocpTcpClient.onConnected), this);
                    if (m_timeOutEvent.WaitForEvent(m_waitTimeInMilliSec))
                    {
                        if (!m_client.Connected)
                        {
                            status = ConnectStatus.FAIL_SOCKET_ERROR;
                            throw new CallbackException();
                        }
                    }
                    else
                    {
                        m_client.Close();
                        status = ConnectStatus.FAIL_TIME_OUT;
                        throw new CallbackException();
                    }
              
                }
            }
            catch(CallbackException)
            {
                if (m_callBackObj != null) 
                    m_callBackObj.OnConnected(this, status);
                return;
            }
            catch
            {
                if (m_callBackObj != null)
                    m_callBackObj.OnConnected(this, ConnectStatus.FAIL_SOCKET_ERROR);
                return;
            }
            startReceive();

        }
        public void Connect(ClientOps ops)
        {

            if (ops == null)
                ops = ClientOps.defaultClientOps;
            if (ops.callBackObj == null)
                throw new NullReferenceException("callBackObj is null!");
            lock (m_generalLock)
            {
                m_clientOps = ops;
            }
            Start();
      
        }

        private static void onConnected(IAsyncResult result)
        {
            IocpTcpClient tcpclient = result.AsyncState as IocpTcpClient;
            if (tcpclient.m_client.Client != null)
            {
                tcpclient.m_client.Client.EndConnect(result);
                tcpclient.m_timeOutEvent.SetEvent();
                if (tcpclient.m_callBackObj != null) 
                    tcpclient.m_callBackObj.OnConnected(tcpclient, ConnectStatus.SUCCESS);
                return;
            }
            tcpclient.m_timeOutEvent.SetEvent();
        }

        public void Disconnect()
        {
            lock (m_generalLock)
            {
                if (!IsConnectionAlive())
                    return;
                m_client.Close();
            }

            lock (m_sendQueueLock)
            {
                m_sendQueue.Clear();
            }
            if (m_callBackObj != null) 
                m_callBackObj.OnDisconnect(this);
        }

        public bool IsConnectionAlive()
        {
            return m_client.Connected;
        }

        public void Send(Packet packet)
        {

            if (!IsConnectionAlive())
            {
                if (m_callBackObj != null) 
                    m_callBackObj.OnSent(this, SendStatus.FAIL_NOT_CONNECTED);
                return;
            }
            if (packet.GetPacketByteSize() <= 0)
            {
                if (m_callBackObj != null) 
                    m_callBackObj.OnSent(this, SendStatus.FAIL_INVALID_PACKET);
                return;
            }

            lock (m_sendLock)
            {
                PacketTransporter transport = new PacketTransporter(PacketType.SIZE, m_sendSizePacket, 0, 4, this, packet);
                m_sendSizePacket.SetPacket(BitConverter.GetBytes(packet.GetPacketByteSize()), 4);
                if (m_sendEvent.TryLock())
                {
                    try { m_client.Client.BeginSend(m_sendSizePacket.GetPacket(), 0, 4, SocketFlags.None, new AsyncCallback(IocpTcpClient.onSent), transport); }
                    catch { Disconnect(); return; }
                }
                else
                {
                    lock (m_sendQueueLock)
                    {
                        m_sendQueue.Enqueue(transport);
                    }
                }
            }
            
            
        }

        private enum PacketType
        {
            SIZE = 0,
            DATA
        }
        private class PacketTransporter
        {
            public Packet m_packet;
            public Packet m_dataPacket; // for send
            public int m_offset;
            public int m_size;
            public IocpTcpClient m_iocpTcpClient;
            public PacketType m_packetType;
            public ClientCallbackInterface m_callBackObj;
            public PacketTransporter(PacketType packetType,Packet packet, int offset, int size, IocpTcpClient iocpTcpClient,Packet dataPacket=null)
            {
                m_packetType = packetType;
                m_packet = packet;
                m_offset = offset;
                m_size = size;
                m_iocpTcpClient = iocpTcpClient;
                m_dataPacket = dataPacket;
                m_callBackObj = iocpTcpClient.m_callBackObj;
            }
        }
        private void startReceive()
        {
            PacketTransporter transport = new PacketTransporter(PacketType.SIZE,m_recvSizePacket, 0, 4, this);
            try { m_client.Client.BeginReceive(m_recvSizePacket.GetPacket(), 0, 4, SocketFlags.None, new AsyncCallback(IocpTcpClient.onReceived), transport); }
            catch { Disconnect(); return; }
            
        }

        private static void onReceived(IAsyncResult result)
        {
            PacketTransporter transport = result.AsyncState as PacketTransporter;
            Socket socket = transport.m_iocpTcpClient.m_client.Client;
            
            int readSize=0;
            try { readSize = socket.EndReceive(result); }
            catch {  transport.m_iocpTcpClient.Disconnect();  return;}
            if (readSize == 0)
            {
                transport.m_iocpTcpClient.Disconnect();
                return;
            }
            if (readSize < transport.m_size)
            {
                transport.m_offset = transport.m_offset + readSize;
                transport.m_size = transport.m_size - readSize;
                try{socket.BeginReceive(transport.m_packet.GetPacket(), transport.m_offset, transport.m_size, SocketFlags.None, new AsyncCallback(IocpTcpClient.onReceived), transport);}
                catch { transport.m_iocpTcpClient.Disconnect(); return; }
            }
            else
            {
                if (transport.m_packetType == PacketType.SIZE)
                {
                    int shouldReceive = BitConverter.ToInt32(transport.m_packet.GetPacket(), 0);
                    Packet recvPacket = new Packet(null, shouldReceive);
                    PacketTransporter dataTransport = new PacketTransporter(PacketType.DATA, recvPacket, 0, shouldReceive, transport.m_iocpTcpClient);
                    try{socket.BeginReceive(recvPacket.GetPacket(), 0, shouldReceive, SocketFlags.None, new AsyncCallback(IocpTcpClient.onReceived), dataTransport);}
                    catch {  transport.m_iocpTcpClient.Disconnect(); return; }
                }
                else
                {
                    PacketTransporter sizeTransport = new PacketTransporter(PacketType.SIZE, transport.m_iocpTcpClient.m_recvSizePacket, 0, 4, transport.m_iocpTcpClient);
                    try{socket.BeginReceive(sizeTransport.m_packet.GetPacket(), 0, 4, SocketFlags.None, new AsyncCallback(IocpTcpClient.onReceived), transport);}
                    catch {  transport.m_iocpTcpClient.Disconnect(); return;}
                    transport.m_callBackObj.OnReceived(transport.m_iocpTcpClient, transport.m_packet);
                }
            }
          }

        private static void onSent(IAsyncResult result)
        {
            PacketTransporter transport = result.AsyncState as PacketTransporter;
            Socket socket = transport.m_iocpTcpClient.m_client.Client;
 
            int sentSize=0;
            try { sentSize = socket.EndSend(result); }
            catch { 
                transport.m_iocpTcpClient.Disconnect();
                transport.m_callBackObj.OnSent(transport.m_iocpTcpClient, SendStatus.FAIL_SOCKET_ERROR);
                return; }
            if (sentSize == 0)
            {
                transport.m_iocpTcpClient.Disconnect();
                transport.m_callBackObj.OnSent(transport.m_iocpTcpClient, SendStatus.FAIL_CONNECTION_CLOSING);
                return;
            }
            if (sentSize < transport.m_size)
            {
                transport.m_offset = transport.m_offset + sentSize;
                transport.m_size = transport.m_size - sentSize;
                try { socket.BeginSend(transport.m_packet.GetPacket(), transport.m_offset, transport.m_size, SocketFlags.None, new AsyncCallback(IocpTcpClient.onSent), transport); }
                catch
                {
                    transport.m_iocpTcpClient.Disconnect();
                    transport.m_callBackObj.OnSent(transport.m_iocpTcpClient, SendStatus.FAIL_SOCKET_ERROR);
                    return;
                }
            }
            else
            {
                if (transport.m_packetType == PacketType.SIZE)
                {
                    transport.m_packet = transport.m_dataPacket;
                    transport.m_offset = 0;
                    transport.m_packetType = PacketType.DATA;
                    transport.m_size = transport.m_dataPacket.GetPacketByteSize();
                    try { socket.BeginSend(transport.m_packet.GetPacket(), 0, transport.m_size, SocketFlags.None, new AsyncCallback(IocpTcpClient.onSent), transport); }
                    catch
                    {
                        transport.m_iocpTcpClient.Disconnect();
                        transport.m_callBackObj.OnSent(transport.m_iocpTcpClient, SendStatus.FAIL_SOCKET_ERROR);
                        return;
                    }
                }
                else
                {
                    PacketTransporter delayedTransport = null;
                    lock (transport.m_iocpTcpClient.m_sendQueueLock)
                    {
                        Queue<PacketTransporter> sendQueue = transport.m_iocpTcpClient.m_sendQueue;
                        if (sendQueue.Count > 0)
                        {
                            delayedTransport = sendQueue.Dequeue();
                        }
                    }
                    if (delayedTransport != null)
                    {
                        try { socket.BeginSend(delayedTransport.m_packet.GetPacket(), 0, 4, SocketFlags.None, new AsyncCallback(IocpTcpClient.onSent), delayedTransport); }
                        catch
                        {
                            transport.m_iocpTcpClient.Disconnect();
                            transport.m_callBackObj.OnSent(transport.m_iocpTcpClient, SendStatus.FAIL_SOCKET_ERROR);
                            return;
                        }
                    }
                    else
                    {
                        transport.m_iocpTcpClient.m_sendEvent.Unlock();
                    }
                    transport.m_callBackObj.OnSent(transport.m_iocpTcpClient, SendStatus.SUCCESS);
                }
            }

        }

    }
}
