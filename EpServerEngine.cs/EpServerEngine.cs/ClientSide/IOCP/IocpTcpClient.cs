/*! 
@file IocpTcpClient.cs
@author Woong Gyu La a.k.a Chris. <juhgiyo@gmail.com>
		<http://github.com/juhgiyo/epserverengine.cs>
@date April 01, 2014
@brief IocpTcpClient Interface
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

A IocpTcpClient Class.

*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Threading;
using System.Diagnostics;
using EpLibrary.cs;
using System.Threading.Tasks;

namespace EpServerEngine.cs
{
    /// <summary>
    /// A IOCP TCP Client class.
    /// </summary>
    public sealed class IocpTcpClient : ThreadEx, INetworkClient
    {
        /// <summary>
        /// Actual TCP client
        /// </summary>
        private TcpClient m_client=new TcpClient();
        /// <summary>
        /// client options
        /// </summary>
        private ClientOps m_clientOps = null;

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
        /// send queue
        /// </summary>
        private Queue<PacketTransporter> m_sendQueue = new Queue<PacketTransporter>();

        /// <summary>
        /// callback object
        /// </summary>
        private INetworkClientCallback m_callBackObj=null;
        /// <summary>
        /// hostname
        /// </summary>
        private String m_hostName;
        /// <summary>
        /// port
        /// </summary>
        private String m_port;
        /// <summary>
        /// flag for nodelay
        /// </summary>
        private bool m_noDelay;
        /// <summary>
        /// connection time out in millisecond
        /// </summary>
        private int m_connectionTimeOut;

        /// <summary>
        /// connection time-out event
        /// </summary>
        private EventEx m_timeOutEvent = new EventEx(false, EventResetMode.AutoReset);
        /// <summary>
        /// send event
        /// </summary>
        private EventEx m_sendEvent = new EventEx();

        
        /// <summary>
        /// receive message size packet
        /// </summary>
        private Packet m_recvSizePacket = new Packet(null, 0, Preamble.SIZE_PACKET_LENGTH);

        /// <summary>
        /// flag for connection check
        /// </summary>
        private bool m_isConnected = false;


        /// <summary>
        /// Default constructor
        /// </summary>
        public IocpTcpClient():base()
        {

        }

        /// <summary>
        /// Default copy constructor
        /// </summary>
        /// <param name="b">the object to copy from</param>
        public IocpTcpClient(IocpTcpClient b)
            : base(b)
        {
            m_clientOps = b.m_clientOps;
        }
        ~IocpTcpClient()
        {
            if (IsConnectionAlive)
                Disconnect();
        }

        /// <summary>
        /// Return hostname
        /// </summary>
        /// <returns>hostname</returns>
        public String HostName
        {
            get
            {
                lock (m_generalLock)
                {
                    return m_hostName;
                }
            }
            private set
            {
                lock (m_generalLock)
                {
                    m_hostName = value;
                }
            }
        }

        /// <summary>
        /// Return port
        /// </summary>
        /// <returns>port</returns>
        public String Port
        {
            get
            {
                lock (m_generalLock)
                {
                    return m_port;
                }
            }
            private set
            {
                lock (m_generalLock)
                {
                    m_port = value;
                }
            }
        }

        /// <summary>
        /// Return no delay flag
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
            private set
            {
                lock (m_generalLock)
                {
                    m_noDelay = value;
                }
            }
        }

        /// <summary>
        /// Return connection time out in milliseconds
        /// </summary>
        public int ConnectionTimeOut
        {
            get
            {
                lock (m_generalLock)
                {
                    return m_connectionTimeOut;
                }
            }
            private set
            {
                lock (m_generalLock)
                {
                    m_connectionTimeOut = value;
                }
            }
        }

        /// <summary>
        /// callback object
        /// </summary>
        public INetworkClientCallback CallBackObj
        {
            get
            {
                lock (m_generalLock)
                {
                    return m_callBackObj;
                }
            }
            private set
            {
                lock (m_generalLock)
                {
                    m_callBackObj = value;
                }
            }
        }
        /// <summary>
        /// Callback Exception class
        /// </summary>
        private class CallbackException : Exception
        {
            /// <summary>
            /// Default constructor
            /// </summary>
            public CallbackException()
                : base()
            {

            }
            /// <summary>
            /// Default constructor
            /// </summary>
            /// <param name="message">message for exception</param>
            public CallbackException(String message)
                : base(message)
            {

            }
        }
        /// <summary>
        /// Make the connection to the server and start receiving
        /// </summary>
        protected override void execute()
        {
            ConnectStatus status = ConnectStatus.SUCCESS;
            try
            {
                lock (m_generalLock)
                {
                    if (IsConnectionAlive)
                    {
                        status = ConnectStatus.FAIL_ALREADY_CONNECTED;
                        throw new CallbackException();
                    }

                    CallBackObj = m_clientOps.CallBackObj;
                    HostName = m_clientOps.HostName;
                    Port = m_clientOps.Port;
                    NoDelay = m_clientOps.NoDelay;
                    ConnectionTimeOut = m_clientOps.ConnectionTimeOut;


                    if (HostName == null || HostName.Length == 0)
                    {
                        HostName = ServerConf.DEFAULT_HOSTNAME;
                    }

                    if (Port == null || Port.Length == 0)
                    {
                        Port = ServerConf.DEFAULT_PORT;
                    }


                    m_client.NoDelay = NoDelay;

                    m_client.Client.BeginConnect(HostName, Convert.ToInt32(Port), new AsyncCallback(IocpTcpClient.onConnected), this);
                    if (m_timeOutEvent.WaitForEvent(ConnectionTimeOut))
                    {
                        if (!m_client.Connected)
                        {
                            status = ConnectStatus.FAIL_SOCKET_ERROR;
                            throw new CallbackException();
                        }
                        IsConnectionAlive = true;
                        if (CallBackObj != null)
                        {
                            Task t = new Task(delegate()
                            {
                                CallBackObj.OnConnected(this, ConnectStatus.SUCCESS);
                            });
                            t.Start();
                        }

                    }
                    else
                    {
                        try
                        {
                            m_client.Client.Shutdown(SocketShutdown.Both);
                            //Client.Client.Disconnect(true);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message + " >" + ex.StackTrace);
                        }
                        m_client.Close();
                        status = ConnectStatus.FAIL_TIME_OUT;
                        throw new CallbackException();
                    }
                }
            }
            catch(CallbackException)
            {
                if (CallBackObj != null)
                {
                    Task t = new Task(delegate()
                    {
                        CallBackObj.OnConnected(this, status);
                    });
                    t.Start();
                    
                }
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " >" + ex.StackTrace);
                if (CallBackObj != null)
                {
                    Task t = new Task(delegate()
                    {
                        CallBackObj.OnConnected(this, ConnectStatus.FAIL_SOCKET_ERROR);
                    });
                    t.Start();
                   
                }
                return;
            }
            startReceive();

        }
        /// <summary>
        /// Connect to the server with given options
        /// </summary>
        /// <param name="ops">options for client</param>
        public void Connect(ClientOps ops)
        {
//             if (IsConnectionAlive)
//             {
//                 ConnectStatus status = ConnectStatus.FAIL_ALREADY_CONNECTED;
//                 if (ops.CallBackObj != null)
//                 {
//                     Task t = new Task(delegate()
//                     {
//                         ops.CallBackObj.OnConnected(this, status);
//                     });
//                     t.Start();
// 
//                 }
//                 return;
            if (ops == null)
                ops = ClientOps.defaultClientOps;
            if (ops.CallBackObj == null)
                throw new NullReferenceException("callBackObj is null!");
            lock (m_generalLock)
            {
                m_clientOps = ops;
            }
            Start();
      
        }

        /// <summary>
        /// Connection callback function
        /// </summary>
        /// <param name="result">result</param>
        private static void onConnected(IAsyncResult result)
        {
            IocpTcpClient tcpclient = result.AsyncState as IocpTcpClient;
     
            try { tcpclient.m_client.Client.EndConnect(result); }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " >" + ex.StackTrace);
                tcpclient.m_timeOutEvent.SetEvent();
                return;
            }
            tcpclient.m_timeOutEvent.SetEvent();
            //if (tcpclient.m_callBackObj != null) 
            //    tcpclient.m_callBackObj.OnConnected(tcpclient, ConnectStatus.SUCCESS);
            return;
          
        }

        /// <summary>
        /// Disconnect from the server
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

            lock (m_sendQueueLock)
            {
                m_sendQueue.Clear();
            }
            if (CallBackObj != null)
            {
                Task t = new Task(delegate()
                {
                    CallBackObj.OnDisconnect(this);
                });
                t.Start();
            }
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
                }
                
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
            private set
            {
                lock (m_generalLock)
                {
                    m_isConnected = value;
                }
            }

        }

        /// <summary>
        /// Send given packet to the server
        /// </summary>
        /// <param name="packet">packet to send</param>
        public void Send(Packet packet)
        {

            if (!IsConnectionAlive)
            {
                if (CallBackObj != null)
                {
                    Task t = new Task(delegate()
                    {
                        CallBackObj.OnSent(this, SendStatus.FAIL_NOT_CONNECTED, packet);
                    });
                    t.Start();
                }
                return;
            }
            if (packet.PacketByteSize <= 0)
            {
                if (CallBackObj != null)
                {
                    Task t = new Task(delegate()
                    {
                        CallBackObj.OnSent(this, SendStatus.FAIL_INVALID_PACKET, packet);
                    });
                    t.Start();
                }
                return;
            }

            lock (m_sendLock)
            {
                Packet sendSizePacket = new Packet(null,0, Preamble.SIZE_PACKET_LENGTH, false);
                PacketTransporter transport = new PacketTransporter(PacketType.SIZE, sendSizePacket, 0, Preamble.SIZE_PACKET_LENGTH, this, packet);
                //sendSizePacket.SetPacket(BitConverter.GetBytes(packet.GetPacketByteSize()), ServerConf.SIZE_PACKET_LENGTH);
                sendSizePacket.SetPacket(Preamble.ToPreamblePacket(packet.PacketByteSize), 0, Preamble.SIZE_PACKET_LENGTH);
                if (m_sendEvent.TryLock())
                {
                    try { m_client.Client.BeginSend(sendSizePacket.PacketRaw, 0, Preamble.SIZE_PACKET_LENGTH, SocketFlags.None, new AsyncCallback(IocpTcpClient.onSent), transport); }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message + " >" + ex.StackTrace);
                        if (CallBackObj != null)
                            CallBackObj.OnSent(this, SendStatus.FAIL_SOCKET_ERROR, packet);
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
        /// Send given data to the server
        /// </summary>
        /// <param name="data">data in byte array</param>
        /// <param name="offset">offset in bytes</param>
        /// <param name="dataSize">data size in bytes</param>
        public void Send(byte[] data, int offset, int dataSize)
        {
            Packet sendPacket=null;
            sendPacket = new Packet(data,offset, dataSize, false);
//          byte[] packet = new byte[dataSize];
//          MemoryStream stream = new MemoryStream(packet);
//          stream.Write(data, offset, dataSize);
            //          Packet sendPacket = new Packet(packet,0, packet.Count(), false);

            Send(sendPacket);

        }

        /// <summary>
        /// Send given data to the server
        /// </summary>
        /// <param name="data">data in byte array</param>
        public void Send(byte[] data)
        {
            Send(data, 0, data.Count());
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
            /// client
            /// </summary>
            public IocpTcpClient m_iocpTcpClient;
            /// <summary>
            /// packet type
            /// </summary>
            public PacketType m_packetType;
            /// <summary>
            /// callback object
            /// </summary>
            public INetworkClientCallback m_callBackObj;
            /// <summary>
            /// Default constructor
            /// </summary>
            /// <param name="packetType">packet type</param>
            /// <param name="packet">packet</param>
            /// <param name="offset">offset</param>
            /// <param name="size">size of packet in byte</param>
            /// <param name="iocpTcpClient">client</param>
            /// <param name="dataPacket">data packet for send</param>
            public PacketTransporter(PacketType packetType,Packet packet, int offset, int size, IocpTcpClient iocpTcpClient,Packet dataPacket=null)
            {
                m_packetType = packetType;
                m_packet = packet;
                m_offset = offset;
                m_size = size;
                m_iocpTcpClient = iocpTcpClient;
                m_dataPacket = dataPacket;
                m_callBackObj = iocpTcpClient.CallBackObj;
            }
        }
        /// <summary>
        /// Start to receive packet from the server
        /// </summary>
        private void startReceive()
        {
            PacketTransporter transport = new PacketTransporter(PacketType.SIZE, m_recvSizePacket, 0, Preamble.SIZE_PACKET_LENGTH, this);
            try { m_client.Client.BeginReceive(m_recvSizePacket.PacketRaw, 0, Preamble.SIZE_PACKET_LENGTH, SocketFlags.None, new AsyncCallback(IocpTcpClient.onReceived), transport); }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " >" + ex.StackTrace);
                Disconnect(); return;
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
                transport.m_iocpTcpClient.Disconnect(); return;
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
                try{socket.BeginReceive(transport.m_packet.PacketRaw, transport.m_offset, transport.m_size, SocketFlags.None, new AsyncCallback(IocpTcpClient.onReceived), transport);}
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + " >" + ex.StackTrace);
                    transport.m_iocpTcpClient.Disconnect(); return;
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
                            socket.BeginReceive(transport.m_packet.PacketRaw, transport.m_offset, transport.m_size, SocketFlags.None, new AsyncCallback(IocpTcpClient.onReceived), transport);
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
                    try { socket.BeginReceive(recvPacket.PacketRaw, 0, shouldReceive, SocketFlags.None, new AsyncCallback(IocpTcpClient.onReceived), dataTransport); }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message + " >" + ex.StackTrace); 
                        transport.m_iocpTcpClient.Disconnect(); return;
                    }
                }
                else
                {
                    PacketTransporter sizeTransport = new PacketTransporter(PacketType.SIZE, transport.m_iocpTcpClient.m_recvSizePacket, 0, Preamble.SIZE_PACKET_LENGTH, transport.m_iocpTcpClient);
                    try { socket.BeginReceive(sizeTransport.m_packet.PacketRaw, 0, Preamble.SIZE_PACKET_LENGTH, SocketFlags.None, new AsyncCallback(IocpTcpClient.onReceived), sizeTransport); }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message + " >" + ex.StackTrace);
                        transport.m_iocpTcpClient.Disconnect(); return;
                    }
                    transport.m_callBackObj.OnReceived(transport.m_iocpTcpClient, transport.m_packet);
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
                transport.m_callBackObj.OnSent(transport.m_iocpTcpClient, SendStatus.FAIL_SOCKET_ERROR, transport.m_dataPacket);
                return; }
            if (sentSize == 0)
            {
                transport.m_iocpTcpClient.Disconnect();
                transport.m_callBackObj.OnSent(transport.m_iocpTcpClient, SendStatus.FAIL_CONNECTION_CLOSING, transport.m_dataPacket);
                return;
            }
            if (sentSize < transport.m_size)
            {
                transport.m_offset = transport.m_offset + sentSize;
                transport.m_size = transport.m_size - sentSize;
                try { socket.BeginSend(transport.m_packet.PacketRaw, transport.m_offset, transport.m_size, SocketFlags.None, new AsyncCallback(IocpTcpClient.onSent), transport); }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + " >" + ex.StackTrace);
                    transport.m_iocpTcpClient.Disconnect();
                    transport.m_callBackObj.OnSent(transport.m_iocpTcpClient, SendStatus.FAIL_SOCKET_ERROR, transport.m_dataPacket);
                    return;
                }
            }
            else
            {
                if (transport.m_packetType == PacketType.SIZE)
                {
                    transport.m_packet = transport.m_dataPacket;
                    transport.m_offset = transport.m_dataPacket.PacketOffset; ;
                    transport.m_packetType = PacketType.DATA;
                    transport.m_size = transport.m_dataPacket.PacketByteSize;
                    try { socket.BeginSend(transport.m_packet.PacketRaw, transport.m_offset, transport.m_size, SocketFlags.None, new AsyncCallback(IocpTcpClient.onSent), transport); }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message + " >" + ex.StackTrace);
                        transport.m_iocpTcpClient.Disconnect();
                        transport.m_callBackObj.OnSent(transport.m_iocpTcpClient, SendStatus.FAIL_SOCKET_ERROR, transport.m_dataPacket);
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
                        try { socket.BeginSend(delayedTransport.m_packet.PacketRaw, 0, Preamble.SIZE_PACKET_LENGTH, SocketFlags.None, new AsyncCallback(IocpTcpClient.onSent), delayedTransport); }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message + " >" + ex.StackTrace);
                            transport.m_callBackObj.OnSent(transport.m_iocpTcpClient, SendStatus.SUCCESS, transport.m_dataPacket);
                            delayedTransport.m_iocpTcpClient.Disconnect();
                            delayedTransport.m_callBackObj.OnSent(delayedTransport.m_iocpTcpClient, SendStatus.FAIL_SOCKET_ERROR, delayedTransport.m_dataPacket);
                            return;
                        }
                    }
                    else
                    {
                        transport.m_iocpTcpClient.m_sendEvent.Unlock();
                    }
                    transport.m_callBackObj.OnSent(transport.m_iocpTcpClient, SendStatus.SUCCESS, transport.m_dataPacket);
                }
            }

        }

    }
}
