/*! 
@file IocpTcpSocket.cs
@author Woong Gyu La a.k.a Chris. <juhgiyo@gmail.com>
		<http://github.com/juhgiyo/epserverengine.cs>
@date April 01, 2014
@brief IocpTcpSocket Interface
@version 2.0

@section LICENSE

The MIT License (MIT)

Copyright (c) 2014 Woong Gyu La <juhgiyo@gmail.com>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

@section DESCRIPTION

A IocpTcpSocket Class.

*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Diagnostics;
using EpLibrary.cs;
using System.Threading.Tasks;

namespace EpServerEngine.cs
{
    /// <summary>
    /// IOCP TCP Socket class
    /// </summary>
    public sealed class IocpTcpSocket:ThreadEx,INetworkSocket
    {
        /// <summary>
        /// actual client
        /// </summary>
        private TcpClient m_client=null;
        /// <summary>
        /// managing server
        /// </summary>
        private INetworkServer m_server = null;
        /// <summary>
        /// IP information
        /// </summary>
        private IPInfo m_ipInfo;
        /// <summary>
        /// general lock
        /// </summary>
        private Object m_generalLock = new Object();
        /// <summary>
        /// send lock
        /// </summary>
        private Object m_sendLock = new Object();
        /// <summary>
        /// send queue lock
        /// </summary>
        private Object m_sendQueueLock = new Object();
        /// <summary>
        /// client socket room lock
        /// </summary>
        private Object m_roomLock = new Object();

        /// <summary>
        /// send queue
        /// </summary>
        private Queue<PacketTransporter> m_sendQueue = new Queue<PacketTransporter>();
        /// <summary>
        /// callback object
        /// </summary>
        private INetworkSocketCallback m_callBackObj=null;
        
        /// <summary>
        /// send event
        /// </summary>
        private EventEx m_sendEvent = new EventEx();

        /// <summary>
        /// receive size packet
        /// </summary>
        private Packet m_recvSizePacket = new Packet(null,0, Preamble.SIZE_PACKET_LENGTH);

        /// <summary>
        /// flag for connection check
        /// </summary>
        private bool m_isConnected = false;

        /// <summary>
        /// flag for no delay
        /// </summary>
        private bool m_noDelay = true;


        /// <summary>
        /// OnNewConnected event
        /// </summary>
        OnSocketNewConnectionDelegate m_onNewConnection = delegate { };
        /// <summary>
        /// OnRecevied event
        /// </summary>
        OnSocketReceivedDelegate m_onReceived = delegate { };
        /// <summary>
        /// OnSent event
        /// </summary>
        OnSocketSentDelegate m_onSent = delegate { };
        /// <summary>
        /// OnDisconnect event
        /// </summary>
        OnSocketDisconnectDelegate m_onDisconnect = delegate { };

        /// <summary>
        /// OnNewConnected event
        /// </summary>
        public OnSocketNewConnectionDelegate OnNewConnection
        {
            get
            {
                return m_onNewConnection;
            }
            set
            {
                if (value == null)
                {
                    m_onNewConnection = delegate { };
                    if (CallBackObj != null)
                        m_onNewConnection += CallBackObj.OnNewConnection;
                }
                else
                {
                    m_onNewConnection = CallBackObj != null && CallBackObj.OnNewConnection != value ? CallBackObj.OnNewConnection + (value - CallBackObj.OnNewConnection) : value;
                }
            }
        }
        /// <summary>
        /// OnRecevied event
        /// </summary>
        public OnSocketReceivedDelegate OnReceived
        {
            get
            {
                return m_onReceived;
            }
            set
            {
                if (value == null)
                {
                    m_onReceived = delegate { };
                    if (CallBackObj != null)
                        m_onReceived += CallBackObj.OnReceived;
                }
                else
                {
                    m_onReceived = CallBackObj != null && CallBackObj.OnReceived != value ? CallBackObj.OnReceived + (value - CallBackObj.OnReceived) : value;
                }
            }
        }
        /// <summary>
        /// OnSent event
        /// </summary>
        public OnSocketSentDelegate OnSent
        {
            get
            {
                return m_onSent;
            }
            set
            {
                if (value == null)
                {
                    m_onSent = delegate { };
                    if (CallBackObj != null)
                        m_onSent += CallBackObj.OnSent;
                }
                else
                {
                    m_onSent = CallBackObj != null && CallBackObj.OnSent != value ? CallBackObj.OnSent + (value - CallBackObj.OnSent) : value;
                }
            }
        }
        /// <summary>
        /// OnDisconnect event
        /// </summary>
        public OnSocketDisconnectDelegate OnDisconnect
        {
            get
            {
                return m_onDisconnect;
            }
            set
            {
                if (value == null)
                {
                    m_onDisconnect = delegate { };
                    if (CallBackObj != null)
                        m_onDisconnect += CallBackObj.OnDisconnect;
                }
                else
                {
                    m_onDisconnect = CallBackObj != null && CallBackObj.OnDisconnect != value ? CallBackObj.OnDisconnect + (value - CallBackObj.OnDisconnect) : value;
                }
            }
        }
        

        /// <summary>
        /// room list
        /// </summary>
        private Dictionary<string, Room> m_roomMap = new Dictionary<string, Room>();

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="client">client</param>
        /// <param name="server">managing server</param>
        public IocpTcpSocket(TcpClient client, INetworkServer server):base()
        {
            m_client=client;
            m_server = server;
            NoDelay = server.NoDelay;
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
            if (IsConnectionAlive)
                Disconnect();
        }

        /// <summary>
        /// Get IP information
        /// </summary>
        /// <returns>IP information</returns>
        public IPInfo IPInfo
        {
            get
            {
                lock (m_generalLock)
                {
                    return m_ipInfo;
                }
            }
        }

        /// <summary>
        /// Get managing server
        /// </summary>
        /// <returns>managing server</returns>
        public INetworkServer Server
        {
            get
            {
                lock (m_generalLock)
                {
                    return m_server;
                }
            }
        }

        /// <summary>
        /// Flag for NoDelay
        /// </summary>
        public bool NoDelay
        {
            get
            {
                lock (m_generalLock)
                {
                    return m_noDelay;
                }
            }
            set
            {
                lock (m_generalLock)
                {
                    m_noDelay = value;
                    m_client.NoDelay = m_noDelay;
                }
            }
        }
        /// <summary>
        /// callback obj property
        /// </summary>
        public INetworkSocketCallback CallBackObj
        {
            get
            {
                lock (m_generalLock)
                {
                    return m_callBackObj;
                }
            }
            set
            {
                lock (m_generalLock)
                {
                    if (m_callBackObj != null)
                    {
                        m_onNewConnection -= m_callBackObj.OnNewConnection;
                        m_onSent -= m_callBackObj.OnSent;
                        m_onReceived -= m_callBackObj.OnReceived;
                        m_onDisconnect -= m_callBackObj.OnDisconnect;
                    }
                    m_callBackObj = value;
                    if (m_callBackObj != null)
                    {
                        m_onNewConnection += m_callBackObj.OnNewConnection;
                        m_onSent += m_callBackObj.OnSent;
                        m_onReceived += m_callBackObj.OnReceived;
                        m_onDisconnect += m_callBackObj.OnDisconnect;
                    }
                }
            }
        }
       
        /// <summary>
        /// Start the new connection, and inform the callback object, that the new connection is made
        /// </summary>
        protected override void execute()
        {
            IsConnectionAlive = true;
            startReceive();
            OnNewConnection(this);
        }

        /// <summary>
        /// Disconnect the client socket
        /// </summary>
        public void Disconnect()
        {
            lock (m_generalLock)
            {
                if (!IsConnectionAlive)
                    return;
                try
                {
                    m_client.Client.Shutdown(SocketShutdown.Both);
                    //m_client.Client.Disconnect(true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + " >" + ex.StackTrace);
                }
                m_client.Close();
                IsConnectionAlive = false;
            }
            m_server.DetachClient(this);

            List<IRoom> roomList = Rooms;
            foreach (IRoom room in roomList)
            {
                ((IocpTcpServer)m_server).Leave(this, room.RoomName);
            }
            lock (m_roomLock)
            {
                m_roomMap.Clear();
            }

            lock (m_sendQueueLock)
            {
                m_sendQueue.Clear();
            }
            Task t = new Task(delegate()
            {
                OnDisconnect(this);
            });
            t.Start();

        }

        /// <summary>
        /// Check if the connection is alive
        /// </summary>
        /// <returns>true if connection is alive, otherwise false</returns>
        public bool IsConnectionAlive
        {
            get
            {
                lock (m_generalLock)
                {
                    return m_isConnected;
                    // 	        try
                    // 	        {
                    // 	            return m_client.Connected;
                    // 	        }
                    // 	        catch (Exception ex)
                    // 	        {
                    // 	            Console.WriteLine(ex.Message + " >" + ex.StackTrace);
                    // 	            return false;
                    // 	        }
                }
            }
            private set
            {
                lock (m_generalLock)
                {
                    m_isConnected = value;
                }
            }
        }

        /// <summary>
        /// Send given packet to the client
        /// </summary>
        /// <param name="packet">the packet to send</param>
        public void Send(Packet packet)
        {
            if (!IsConnectionAlive)
            {
                Task t = new Task(delegate()
                {
                    OnSent(this, SendStatus.FAIL_NOT_CONNECTED, packet);
                });
                t.Start();
             
                return;
            }
            if (packet.PacketByteSize <= 0)
            {
                Task t = new Task(delegate()
                {
                    OnSent(this, SendStatus.FAIL_INVALID_PACKET, packet);
                });
                t.Start();

                return;
            }

            lock (m_sendLock)
            {
                Packet sendSizePacket = new Packet(null, 0, Preamble.SIZE_PACKET_LENGTH, false);
                PacketTransporter transport = new PacketTransporter(PacketType.SIZE, sendSizePacket, 0, Preamble.SIZE_PACKET_LENGTH, this, packet);
                
                //sendSizePacket.SetPacket(BitConverter.GetBytes(packet.GetPacketByteSize()), 4);
                sendSizePacket.SetPacket(Preamble.ToPreamblePacket(packet.PacketByteSize), 0, Preamble.SIZE_PACKET_LENGTH);
                if (m_sendEvent.TryLock())
                {
                    try { m_client.Client.BeginSend(sendSizePacket.PacketRaw, 0, Preamble.SIZE_PACKET_LENGTH, SocketFlags.None, new AsyncCallback(IocpTcpSocket.onSent), transport); }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message + " >" + ex.StackTrace);
                        Disconnect(); 
                        return;
                    }
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

        /// <summary>
        /// Send given data to the client
        /// </summary>
        /// <param name="data">data in byte array</param>
        /// <param name="offset">offset in bytes</param>
        /// <param name="dataSize">data size in bytes</param>
        public void Send(byte[] data, int offset, int dataSize)
        {
            Packet sendPacket = new Packet(data,offset, dataSize, false);
//             byte[] packet = new byte[dataSize];
//             MemoryStream stream = new MemoryStream(packet);
//             stream.Write(data, offset, dataSize);
            //             Packet sendPacket = new Packet(packet,0, packet.Count(), false);
            Send(sendPacket);
        }

        /// <summary>
        /// Send given data to the client
        /// </summary>
        /// <param name="data">data in byte array</param>
        public void Send(byte[] data)
        {
            Send(data, 0, data.Count());
        }

        /// <summary>
        /// Broadcast given data to all client other than this
        /// </summary>
        /// <param name="packet">packet to broadcast</param>
        public void Broadcast(Packet packet)
        {
            ((IocpTcpServer)Server).Broadcast(this, packet);
        }

        /// <summary>
        /// Broadcast given data to all client other than this
        /// </summary>
        /// <param name="data">data in byte array</param>
        /// <param name="offset">offset in bytes</param>
        /// <param name="dataSize">data size in bytes</param>
        public void Broadcast(byte[] data, int offset, int dataSize)
        {
            ((IocpTcpServer)Server).Broadcast(this, data, offset, dataSize);
        }

        /// <summary>
        /// Broadcast given data to all client other than this
        /// </summary>
        /// <param name="data">data in byte array</param>
        public void Broadcast(byte[] data)
        {
            ((IocpTcpServer)Server).Broadcast(this, data);
        }

        /// <summary>
        /// Enumerator for packet type
        /// </summary>
        private enum PacketType
        {
            /// <summary>
            /// Send type
            /// </summary>
            SIZE = 0,
            /// <summary>
            /// Receive type
            /// </summary>
            DATA
        }
        /// <summary>
        /// Packet Transporter class
        /// </summary>
        private class PacketTransporter
        {
            /// <summary>
            /// packet to transport
            /// </summary>
            public Packet m_packet;
            /// <summary>
            /// data packet for send
            /// </summary>
            public Packet m_dataPacket;
            /// <summary>
            /// offset
            /// </summary>
            public int m_offset;
            /// <summary>
            /// packet size in byte
            /// </summary>
            public int m_size;
            /// <summary>
            /// client socket
            /// </summary>
            public IocpTcpSocket m_iocpTcpClient;
            /// <summary>
            /// packet type
            /// </summary>
            public PacketType m_packetType;
            /// <summary>
            /// Default constructor
            /// </summary>
            /// <param name="packetType">packet type</param>
            /// <param name="packet">packet</param>
            /// <param name="offset">offset</param>
            /// <param name="size">size of packet in byte</param>
            /// <param name="iocpTcpClient">client socket</param>
            /// <param name="dataPacket">data packet for send</param>
            public PacketTransporter(PacketType packetType, Packet packet, int offset, int size, IocpTcpSocket iocpTcpClient, Packet dataPacket = null)
            {
                m_packetType = packetType;
                m_packet = packet;
                m_offset = offset;
                m_size = size;
                m_iocpTcpClient = iocpTcpClient;
                m_dataPacket = dataPacket;
            }
        }
        /// <summary>
        /// Start to receive packet from the server
        /// </summary>
        private void startReceive()
        {
            PacketTransporter transport = new PacketTransporter(PacketType.SIZE, m_recvSizePacket, 0, Preamble.SIZE_PACKET_LENGTH, this);
            try { m_client.Client.BeginReceive(m_recvSizePacket.PacketRaw, 0, Preamble.SIZE_PACKET_LENGTH, SocketFlags.None, new AsyncCallback(IocpTcpSocket.onReceived), transport); }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " >" + ex.StackTrace);
                Disconnect();
                return;
            }
            
        }

        /// <summary>
        /// Receive callback function
        /// </summary>
        /// <param name="result">result</param>
        private static void onReceived(IAsyncResult result)
        {
            PacketTransporter transport = result.AsyncState as PacketTransporter;
            Socket socket = transport.m_iocpTcpClient.m_client.Client;
            
            int readSize=0;
            try { readSize = socket.EndReceive(result); }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " >" + ex.StackTrace); 
                transport.m_iocpTcpClient.Disconnect(); 
                return;
            }
            if (readSize == 0)
            {
                transport.m_iocpTcpClient.Disconnect();
                return;
            }
            if (readSize < transport.m_size)
            {
                transport.m_offset = transport.m_offset + readSize;
                transport.m_size = transport.m_size - readSize;
                try { socket.BeginReceive(transport.m_packet.PacketRaw, transport.m_offset, transport.m_size, SocketFlags.None, new AsyncCallback(IocpTcpSocket.onReceived), transport); }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + " >" + ex.StackTrace);
                    transport.m_iocpTcpClient.Disconnect(); 
                    return;
                }
            }
            else
            {
                if (transport.m_packetType == PacketType.SIZE)
                {
                    //int shouldReceive = BitConverter.ToInt32(transport.m_packet.PacketRaw, 0);
                    int shouldReceive = Preamble.ToShouldReceive(transport.m_packet.PacketRaw);

                    // preamble packet is corrupted
                    // try to receive another byte to check preamble
                    if (shouldReceive < 0)
                    {
                        int preambleOffset = Preamble.CheckPreamble(transport.m_packet.PacketRaw);
                        // set offset to length - preamble offset
                        transport.m_offset = transport.m_packet.PacketByteSize - preambleOffset;
                        // need to receive as much as preamble offset
                        transport.m_size = preambleOffset;
                        try
                        {
                            // shift to left by preamble offset
                            Buffer.BlockCopy(transport.m_packet.PacketRaw, preambleOffset, transport.m_packet.PacketRaw, 0, transport.m_packet.PacketByteSize - preambleOffset);
                            // receive rest of bytes at the end
                            socket.BeginReceive(transport.m_packet.PacketRaw, transport.m_offset, transport.m_size, SocketFlags.None, new AsyncCallback(IocpTcpSocket.onReceived), transport);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message + " >" + ex.StackTrace);
                            transport.m_iocpTcpClient.Disconnect(); return;
                        }
                        return;
                    }
                    Packet recvPacket = new Packet(null, 0, shouldReceive);
                    PacketTransporter dataTransport = new PacketTransporter(PacketType.DATA, recvPacket, 0, shouldReceive, transport.m_iocpTcpClient);
                    try { socket.BeginReceive(recvPacket.PacketRaw, 0, shouldReceive, SocketFlags.None, new AsyncCallback(IocpTcpSocket.onReceived), dataTransport); }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message + " >" + ex.StackTrace); 
                        transport.m_iocpTcpClient.Disconnect(); 
                        return;
                    }
                }
                else
                {
                    PacketTransporter sizeTransport = new PacketTransporter(PacketType.SIZE, transport.m_iocpTcpClient.m_recvSizePacket, 0, Preamble.SIZE_PACKET_LENGTH, transport.m_iocpTcpClient);
                    try { socket.BeginReceive(sizeTransport.m_packet.PacketRaw, 0, Preamble.SIZE_PACKET_LENGTH, SocketFlags.None, new AsyncCallback(IocpTcpSocket.onReceived), sizeTransport); }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message + " >" + ex.StackTrace);
                        transport.m_iocpTcpClient.Disconnect(); 
                        return;
                    }
                    transport.m_iocpTcpClient.OnReceived(transport.m_iocpTcpClient, transport.m_packet);
                }
            }
          }

        /// <summary>
        /// Send callback function
        /// </summary>
        /// <param name="result">result</param>
        private static void onSent(IAsyncResult result)
        {
            PacketTransporter transport = result.AsyncState as PacketTransporter;
            Socket socket = transport.m_iocpTcpClient.m_client.Client;
 
            int sentSize=0;
            try { sentSize = socket.EndSend(result); }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " >" + ex.StackTrace); 
                transport.m_iocpTcpClient.Disconnect();
                transport.m_iocpTcpClient.OnSent(transport.m_iocpTcpClient, SendStatus.FAIL_SOCKET_ERROR, transport.m_dataPacket);
                return; 
            }
            if (sentSize == 0)
            {
                transport.m_iocpTcpClient.Disconnect();
                transport.m_iocpTcpClient.OnSent(transport.m_iocpTcpClient, SendStatus.FAIL_CONNECTION_CLOSING, transport.m_dataPacket);
                return;
            }
            if (sentSize < transport.m_size)
            {
                transport.m_offset = transport.m_offset + sentSize;
                transport.m_size = transport.m_size - sentSize;
                try { socket.BeginSend(transport.m_packet.PacketRaw, transport.m_offset, transport.m_size, SocketFlags.None, new AsyncCallback(IocpTcpSocket.onSent), transport); }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + " >" + ex.StackTrace);
                    transport.m_iocpTcpClient.Disconnect();
                    transport.m_iocpTcpClient.OnSent(transport.m_iocpTcpClient, SendStatus.FAIL_SOCKET_ERROR, transport.m_dataPacket);
                    return;
                }
            }
            else
            {
                if (transport.m_packetType == PacketType.SIZE)
                {
                    transport.m_packet = transport.m_dataPacket;
                    transport.m_offset = transport.m_dataPacket.PacketOffset;
                    transport.m_packetType = PacketType.DATA;
                    transport.m_size = transport.m_dataPacket.PacketByteSize;
                    try { socket.BeginSend(transport.m_packet.PacketRaw, transport.m_offset, transport.m_size, SocketFlags.None, new AsyncCallback(IocpTcpSocket.onSent), transport); }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message + " >" + ex.StackTrace);
                        transport.m_iocpTcpClient.Disconnect();
                        transport.m_iocpTcpClient.OnSent(transport.m_iocpTcpClient, SendStatus.FAIL_SOCKET_ERROR, transport.m_dataPacket);
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
                        try { socket.BeginSend(delayedTransport.m_packet.PacketRaw, 0, Preamble.SIZE_PACKET_LENGTH, SocketFlags.None, new AsyncCallback(IocpTcpSocket.onSent), delayedTransport); }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message + " >" + ex.StackTrace);
                            delayedTransport.m_iocpTcpClient.Disconnect();
                            delayedTransport.m_iocpTcpClient.OnSent(delayedTransport.m_iocpTcpClient, SendStatus.FAIL_SOCKET_ERROR, delayedTransport.m_dataPacket);
                            return;
                        }
                    }
                    else
                    {
                        transport.m_iocpTcpClient.m_sendEvent.Unlock();
                    }
                    transport.m_iocpTcpClient.OnSent(transport.m_iocpTcpClient, SendStatus.SUCCESS, transport.m_dataPacket);
                }
            }

        }

        /// <summary>
        /// Return the room instance of given room name
        /// </summary>
        /// <param name="roomName">room name</param>
        /// <returns>the room instance</returns>
        public IRoom GetRoom(string roomName)
        {
            lock (m_roomLock)
            {
                if (m_roomMap.ContainsKey(roomName))
                    return m_roomMap[roomName];
                return null;
            }
        }

        /// <summary>
        /// Return the list of names of the room
        /// </summary>
        public List<string> RoomNames
        {
            get
            {
                lock (m_roomLock)
                {
                    return new List<string>(m_roomMap.Keys);
                }
            }
        }

        /// <summary>
        /// Return the list of rooms
        /// </summary>
        public List<IRoom> Rooms
        {
            get
            {
                lock (m_roomLock)
                {
                    return new List<IRoom>(m_roomMap.Values);
                }
            }
        }

        /// <summary>
        /// Join the room
        /// </summary>
        /// <param name="roomName">room name</param>
        /// <returns>the instance of the room</returns>
        public void Join(string roomName)
        {
            lock (m_roomLock)
            {
                Room curRoom = ((IocpTcpServer)m_server).Join(this, roomName);
                if (!m_roomMap.ContainsKey(roomName))
                {
                    m_roomMap[roomName]=curRoom;
                }
            }
        }

        /// <summary>
        /// Detach given socket from the given room
        /// </summary>
        /// <param name="roomName">room name</param>
        /// <returns>number of sockets left in the room</returns>
        public void Leave(string roomName)
        {
            lock (m_roomLock)
            {
                if (m_roomMap.ContainsKey(roomName))
                {
                    int numSocketLeft = ((IocpTcpServer)m_server).Leave(this,roomName);
                    if (numSocketLeft == 0)
                    {
                        m_roomMap.Remove(roomName);
                    }
                }
            }
        }

        /// <summary>
        /// Broadcast the given packet to all the client, connected
        /// </summary>
        /// <param name="packet">packet to broadcast</param>
        public void BroadcastToRoom(string roomName, Packet packet)
        {
            lock (m_roomLock)
            {
                if (m_roomMap.ContainsKey(roomName))
                {
                    m_roomMap[roomName].Broadcast(this, packet);
                }
            }
        }


        /// <summary>
        /// Broadcast the given packet to all the client, connected
        /// </summary>
        /// <param name="data">data in byte array</param>
        /// <param name="offset">offset in bytes</param>
        /// <param name="dataSize">data size in bytes</param>
        public void BroadcastToRoom(string roomName, byte[] data, int offset, int dataSize)
        {
            lock (m_roomLock)
            {
                if (m_roomMap.ContainsKey(roomName))
                {
                    m_roomMap[roomName].Broadcast(this, data, offset, dataSize);
                }
            }
        }


        /// <summary>
        /// Broadcast the given packet to all the client, connected
        /// </summary>
        /// <param name="data">data in byte array</param>
        public void BroadcastToRoom(string roomName, byte[] data)
        {
            lock (m_roomLock)
            {
                if (m_roomMap.ContainsKey(roomName))
                {
                    m_roomMap[roomName].Broadcast(this, data);
                }
            }
        }

    }
}
