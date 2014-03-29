using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Diagnostics;
using EpLibrary.cs;

namespace IocpServer
{
    public class IocpTcpSocket:ThreadEx,SocketInterface
    {
        private TcpClient m_client=null;
        private ServerInterface m_server = null;
        private IPInfo m_ipInfo;

        private Object m_generalLock = new Object();

        private Object m_sendLock = new Object();
        private Object m_sendQueueLock = new Object();
        private Queue<PacketTransporter> m_sendQueue = new Queue<PacketTransporter>();

        private SocketCallbackInterface m_callBackObj=null;
        

        private EventEx m_sendEvent = new EventEx();

        /// Message Size Packet;
        private Packet m_recvSizePacket = new Packet(null, 4);
        private Packet m_sendSizePacket = new Packet(null, 4, false);

        public IocpTcpSocket(TcpClient client, ServerInterface server):base()
        {
            m_client=client;
            m_server = server;
            IPEndPoint remoteIpEndPoint = m_client.Client.RemoteEndPoint as IPEndPoint;
            IPEndPoint localIpEndPoint = m_client.Client.LocalEndPoint as IPEndPoint;
            if (remoteIpEndPoint != null)
            {
                String socketHostName = remoteIpEndPoint.Address.ToString();
                m_ipInfo = new IPInfo(socketHostName, remoteIpEndPoint, IPEndPointType.REMOTE);
            }
            else if (localIpEndPoint != null)
            {
                String socketHostName = localIpEndPoint.Address.ToString();
                m_ipInfo = new IPInfo(socketHostName, localIpEndPoint, IPEndPointType.LOCAL);
            }
            
        }

        ~IocpTcpSocket()
        {
            if (IsConnectionAlive())
                Disconnect();
        }

        public IPInfo GetIPInfo()
        {
            return m_ipInfo;
        }

        public ServerInterface GetServer()
        {
            return m_server;
        }

        public void SetSocketCallbackInterface(SocketCallbackInterface callBackObj)
        {
            m_callBackObj = callBackObj;
        }
        public SocketCallbackInterface GetSocketCallbackInterface()
        {
            return m_callBackObj;
        }

        protected override void execute()
        {
            startReceive();
            if(m_callBackObj!=null) 
                m_callBackObj.OnNewConnection(this);
        }


        public void Disconnect()
        {
            lock (m_generalLock)
            {
                if (!IsConnectionAlive())
                    return;
                m_client.Close();
            }
            m_server.DetachClient(this);

            lock (m_sendQueueLock)
            {
                m_sendQueue.Clear();
            }
            if(m_callBackObj!=null) 
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
                    try { m_client.Client.BeginSend(m_sendSizePacket.GetPacket(), 0, 4, SocketFlags.None, new AsyncCallback(IocpTcpSocket.onSent), transport); }
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
            public IocpTcpSocket m_iocpTcpClient;
            public PacketType m_packetType;
            public SocketCallbackInterface m_callBackObj;
            public PacketTransporter(PacketType packetType, Packet packet, int offset, int size, IocpTcpSocket iocpTcpClient, Packet dataPacket = null)
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
            try { m_client.Client.BeginReceive(m_recvSizePacket.GetPacket(), 0, 4, SocketFlags.None, new AsyncCallback(IocpTcpSocket.onReceived), transport); }
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
                try { socket.BeginReceive(transport.m_packet.GetPacket(), transport.m_offset, transport.m_size, SocketFlags.None, new AsyncCallback(IocpTcpSocket.onReceived), transport); }
                catch { transport.m_iocpTcpClient.Disconnect(); return; }
            }
            else
            {
                if (transport.m_packetType == PacketType.SIZE)
                {
                    int shouldReceive = BitConverter.ToInt32(transport.m_packet.GetPacket(), 0);
                    Packet recvPacket = new Packet(null, shouldReceive);
                    PacketTransporter dataTransport = new PacketTransporter(PacketType.DATA, recvPacket, 0, shouldReceive, transport.m_iocpTcpClient);
                    try { socket.BeginReceive(recvPacket.GetPacket(), 0, shouldReceive, SocketFlags.None, new AsyncCallback(IocpTcpSocket.onReceived), dataTransport); }
                    catch {  transport.m_iocpTcpClient.Disconnect(); return; }
                }
                else
                {
                    PacketTransporter sizeTransport = new PacketTransporter(PacketType.SIZE, transport.m_iocpTcpClient.m_recvSizePacket, 0, 4, transport.m_iocpTcpClient);
                    try { socket.BeginReceive(sizeTransport.m_packet.GetPacket(), 0, 4, SocketFlags.None, new AsyncCallback(IocpTcpSocket.onReceived), transport); }
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
                try { socket.BeginSend(transport.m_packet.GetPacket(), transport.m_offset, transport.m_size, SocketFlags.None, new AsyncCallback(IocpTcpSocket.onSent), transport); }
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
                    try { socket.BeginSend(transport.m_packet.GetPacket(), 0, transport.m_size, SocketFlags.None, new AsyncCallback(IocpTcpSocket.onSent), transport); }
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
                        try { socket.BeginSend(delayedTransport.m_packet.GetPacket(), 0, 4, SocketFlags.None, new AsyncCallback(IocpTcpSocket.onSent), delayedTransport); }
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
