/*! 
@file IocpTcpServer.cs
@author Woong Gyu La a.k.a Chris. <juhgiyo@gmail.com>
		<http://github.com/juhgiyo/epserverengine.cs>
@date April 01, 2014
@brief IocpTcpServer Interface
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

A IocpTcpServer Class.

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

namespace EpServerEngine.cs
{
    /// <summary>
    /// IOCP TCP Server
    /// </summary>
    public sealed class IocpTcpServer:ThreadEx, INetworkServer
    {
        /// <summary>
        /// port
        /// </summary>
        private String m_port=ServerConf.DEFAULT_PORT;

        /// <summary>
        /// NoDelay flag
        /// </summary>
        private bool m_noDelay = true;

        /// <summary>
        /// maximum socket count
        /// </summary>
        private int m_maxSocketCount = SocketCount.Infinite;
        /// <summary>
        /// listner
        /// </summary>
        private TcpListener m_listener=null;
        /// <summary>
        /// server option
        /// </summary>
        private ServerOps m_serverOps = null;

        /// <summary>
        /// callback object
        /// </summary>
        private INetworkServerCallback m_callBackObj=null;

        /// <summary>
        /// room callback object
        /// </summary>
        private IRoomCallback m_roomCallBackObj = null;

        /// <summary>
        /// general lock
        /// </summary>
        private Object m_generalLock = new Object();

        /// <summary>
        /// client socket list lock
        /// </summary>
        private Object m_listLock = new Object();

        /// <summary>
        /// client socket room lock
        /// </summary>
        private Object m_roomLock = new Object();
        /// <summary>
        /// client socket list
        /// </summary>
        private HashSet<IocpTcpSocket> m_socketList = new HashSet<IocpTcpSocket>();

        /// <summary>
        /// room list
        /// </summary>
        private Dictionary<string, Room> m_roomMap = new Dictionary<string, Room>();

        /// <summary>
        /// Default constructor
        /// </summary>
        public IocpTcpServer()
            : base()
        {
        }

        /// <summary>
        /// Default copy constructor
        /// </summary>
        /// <param name="b">the object to copy from</param>
        public IocpTcpServer(IocpTcpServer b)
            : base(b)
        {
            m_port = b.m_port;
            m_serverOps = b.m_serverOps;
        }

        ~IocpTcpServer()
        {
            if(IsServerStarted)
                StopServer();
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
        /// callback object
        /// </summary>
        public INetworkServerCallback CallBackObj
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
                    m_callBackObj = value;
                }
            }
        }

        /// <summary>
        /// room callback object
        /// </summary>
        public IRoomCallback RoomCallBackObj
        {
            get
            {
                lock (m_generalLock)
                {
                    return m_roomCallBackObj;
                }
            }
            set
            {
                lock (m_generalLock)
                {
                    m_roomCallBackObj = value;
                }
            }
        }

        /// <summary>
        /// No delay property
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
                }
            }
        }

        /// <summary>
        /// maximum socket count property
        /// </summary>
        public int MaxSocketCount
        {
            get
            {
                lock (m_generalLock)
                {
                    return m_maxSocketCount;
                }
            }
            set
            {
                lock (m_generalLock)
                {
                    m_maxSocketCount = value;
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
        /// Start the server and start accepting the client
        /// </summary>
        protected override void execute()
        {
            StartStatus status=StartStatus.FAIL_SOCKET_ERROR;
            try
            {
                lock (m_generalLock)
                {
                    if (IsServerStarted)
                    {
                        status = StartStatus.FAIL_ALREADY_STARTED;
                        throw new CallbackException();
                    }

                    CallBackObj = m_serverOps.CallBackObj;
                    RoomCallBackObj = m_serverOps.RoomCallBackObj;
                    NoDelay = m_serverOps.NoDelay;
                    Port = m_serverOps.Port;
                    MaxSocketCount = m_serverOps.MaxSocketCount;
                    
                    if (Port == null || Port.Length == 0)
                    {
                        Port = ServerConf.DEFAULT_PORT;
                    }
                    lock (m_listLock)
                    {
                        m_socketList.Clear();
                    }
                    lock (m_roomLock)
                    {
                        m_roomMap.Clear();
                    }                   

                    m_listener = new TcpListener(IPAddress.Any, Convert.ToInt32(m_port));
                    m_listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    m_listener.Start();
                    m_listener.BeginAcceptTcpClient(new AsyncCallback(IocpTcpServer.onAccept), this);
                }
            
            }
            catch (CallbackException)
            {
                if (CallBackObj != null)
                    CallBackObj.OnServerStarted(this, status);
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " >" + ex.StackTrace);
                if (m_listener != null)
                    m_listener.Stop();
                m_listener = null;
                if (CallBackObj != null)
                    CallBackObj.OnServerStarted(this, StartStatus.FAIL_SOCKET_ERROR);
                return;
            }
            if (CallBackObj != null)
                CallBackObj.OnServerStarted(this, StartStatus.SUCCESS);
        }

        /// <summary>
        /// Accept callback function
        /// </summary>
        /// <param name="result">result</param>
        private static void onAccept(IAsyncResult result)
        {
            IocpTcpServer server = result.AsyncState as IocpTcpServer;
            TcpClient client=null;
            try { client = server.m_listener.EndAcceptTcpClient(result); }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " >" + ex.StackTrace);
                if (client != null)
                {
                    try
                    {
                        client.Client.Shutdown(SocketShutdown.Both);
                        //client.Client.Disconnect(true);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message + " >" + e.StackTrace);
                    }
                    client.Close();
                    client = null;
                }
            }
            
            try { server.m_listener.BeginAcceptTcpClient(new AsyncCallback(IocpTcpServer.onAccept), server); }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " >" + ex.StackTrace); 
                if (client != null)
                    client.Close();
                server.StopServer(); 
                return;
            }

            if (client != null)
            {
                IocpTcpSocket socket = new IocpTcpSocket(client, server);
                lock (server.m_listLock)
                {
                    if (server.MaxSocketCount!=SocketCount.Infinite && server.m_socketList.Count>server.MaxSocketCount)
                    {
                        socket.Disconnect();
                        return;
                    }
                }
                if (server.CallBackObj == null)
                {
                    socket.Disconnect();
                    return;
                }
                INetworkSocketCallback socketCallbackObj = server.CallBackObj.OnAccept(server, socket.IPInfo);
                if (socketCallbackObj == null)
                {
                    socket.Disconnect();
                }
                else
                {
                    socket.CallBackObj=socketCallbackObj;
                    socket.Start();
                    lock (server.m_listLock)
                    {
                        server.m_socketList.Add(socket);
                    }
                }
            }
           

        }

        /// <summary>
        /// Start the server with given option
        /// </summary>
        /// <param name="ops">options</param>
        public void StartServer(ServerOps ops)
        {
            if (ops == null)
                ops = ServerOps.defaultServerOps;
            if (ops.CallBackObj == null)
                throw new NullReferenceException("callBackObj is null!");
            lock (m_generalLock)
            {
                m_serverOps = ops;
            }
            Start();
        }
        /// <summary>
        /// Stop the server
        /// </summary>
        public void StopServer()
        {
            lock (m_generalLock)
            {
                if (!IsServerStarted)
                    return;

                m_listener.Stop();
                m_listener = null;
            }
            ShutdownAllClient();

            if (CallBackObj != null)
                CallBackObj.OnServerStopped(this);
        }

        /// <summary>
        /// Check if the server is started
        /// </summary>
        /// <returns>true if the server is started, otherwise false</returns>
        public bool IsServerStarted
        {
            get
            {
                lock (m_generalLock)
                {
                    if (m_listener != null)
                        return true;
                    return false;
                }
            }
        }
        /// <summary>
        /// Shut down all the client, connected
        /// </summary>
        public void ShutdownAllClient()
        {
            lock (m_listLock)
            {
                List<IocpTcpSocket> socketList = GetClientSocketList();
                foreach (IocpTcpSocket socket in socketList)
                {
                    socket.Disconnect();
                }
            }
        }
        /// <summary>
        /// Broadcast the given packet to the all client, connected
        /// </summary>
        /// <param name="packet">the packet to broadcast</param>
        public void Broadcast(Packet packet)
        {
            List<IocpTcpSocket> socketList = GetClientSocketList();

            foreach (IocpTcpSocket socket in socketList)
            {
                socket.Send(packet);
            }
        }
        /// <summary>
        /// Broadcast given data to the server
        /// </summary>
        /// <param name="data">data in byte array</param>
        /// <param name="offset">offset in bytes</param>
        /// <param name="dataSize">data size in bytes</param>
        public void Broadcast(byte[] data, int offset, int dataSize)
        {
            List<IocpTcpSocket> socketList = GetClientSocketList();

            foreach (IocpTcpSocket socket in socketList)
            {
                socket.Send(data, offset, dataSize);
            }
        }

        /// <summary>
        /// Broadcast given data to the server
        /// </summary>
        /// <param name="data">data in byte array</param>
        public void Broadcast(byte[] data)
        {
            List<IocpTcpSocket> socketList = GetClientSocketList();

            foreach (IocpTcpSocket socket in socketList)
            {
                socket.Send(data);
            }
        }

        /// <summary>
        /// Return the client socket list
        /// </summary>
        /// <returns>the client socket list</returns>
        public List<IocpTcpSocket> GetClientSocketList()
        {
            lock (m_listLock)
            {
                return new List<IocpTcpSocket>(m_socketList);
            }
        }

        /// <summary>
        /// Detach the given client from the server management
        /// </summary>
        /// <param name="clientSocket">the client to detach</param>
        /// <returns></returns>
        public bool DetachClient(IocpTcpSocket clientSocket)
        {
            lock (m_listLock)
            {
                return m_socketList.Remove(clientSocket);
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
        /// <param name="socket">socket</param>
        /// <param name="roomName">room name</param>
        /// <returns>the instance of the room</returns>
        public Room Join(INetworkSocket socket, string roomName)
        {
            lock (m_roomLock)
            {
                Room curRoom=null;
                if (m_roomMap.ContainsKey(roomName))
                {
                    curRoom=m_roomMap[roomName];
                }
                else
                {
                    curRoom= new Room(roomName,RoomCallBackObj);
                    m_roomMap[roomName] = curRoom;
                }
                curRoom.AddSocket(socket);
                return curRoom;
            }
        }

        /// <summary>
        /// Detach given socket from the given room
        /// </summary>
        /// <param name="socket">socket to detach</param>
        /// <param name="roomName">room name</param>
        /// <returns>number of sockets left in the room</returns>
        public int Leave(INetworkSocket socket, string roomName)
        {
            lock (m_roomLock)
            {
                if (m_roomMap.ContainsKey(roomName))
                {
                    int numSocketLeft = m_roomMap[roomName].DetachClient(socket);
                    if (numSocketLeft == 0)
                    {
                        m_roomMap.Remove(roomName);
                    }
                    return numSocketLeft;
                }
                return 0;
            }
        }

        /// <summary>
        /// Broadcast the given packet to all the client, connected
        /// </summary>
        /// <param name="packet">packet to broadcast</param>
        public void Broadcast(string roomName, Packet packet)
        {
            lock (m_roomLock)
            {
                if (m_roomMap.ContainsKey(roomName))
                {
                    m_roomMap[roomName].Broadcast(packet);
                }
            }
        }


        /// <summary>
        /// Broadcast the given packet to all the client, connected
        /// </summary>
        /// <param name="data">data in byte array</param>
        /// <param name="offset">offset in bytes</param>
        /// <param name="dataSize">data size in bytes</param>
        public void Broadcast(string roomName, byte[] data, int offset, int dataSize)
        {
            lock (m_roomLock)
            {
                if (m_roomMap.ContainsKey(roomName))
                {
                    m_roomMap[roomName].Broadcast(data,offset, dataSize);
                }
            }
        }


        /// <summary>
        /// Broadcast the given packet to all the client, connected
        /// </summary>
        /// <param name="data">data in byte array</param>
        public void Broadcast(string roomName, byte[] data)
        {
            lock (m_roomLock)
            {
                if (m_roomMap.ContainsKey(roomName))
                {
                    m_roomMap[roomName].Broadcast(data);
                }
            }
        }

    }
}
