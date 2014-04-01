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
using System.Threading.Tasks;
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
    public sealed class IocpTcpServer:ThreadEx, ServerInterface
    {
        /// <summary>
        /// port
        /// </summary>
        private String m_port=ServerConf.DEFAULT_PORT;
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
        private ServerCallbackInterface m_callBackObj=null;

        /// <summary>
        /// general lock
        /// </summary>
        private Object m_generalLock = new Object();

        /// <summary>
        /// client socket list lock
        /// </summary>
        private Object m_listLock = new Object();
        /// <summary>
        /// client socket list
        /// </summary>
        private List<IocpTcpSocket> m_socketList=new List<IocpTcpSocket>();

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
            if(IsServerStarted())
                StopServer();
        }

        /// <summary>
        /// Return port
        /// </summary>
        /// <returns>port</returns>
        public String GetPort()
        {
            return m_port;
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
                    if (IsServerStarted())
                    {
                        status = StartStatus.FAIL_ALREADY_STARTED;
                        throw new CallbackException();
                    }

                    m_callBackObj = m_serverOps.callBackObj;
                    m_port = m_serverOps.port;

                    if (m_port == null || m_port.Length == 0)
                    {
                        m_port = ServerConf.DEFAULT_PORT;
                    }


                    m_listener = new TcpListener(IPAddress.Any, Convert.ToInt32(m_port));
                    m_listener.Start();
                    m_listener.BeginAcceptTcpClient(new AsyncCallback(IocpTcpServer.onAccept), this);
                }
            }
            catch (CallbackException)
            {
                m_callBackObj.OnServerStarted(this, status);
                return;
            }
            catch
            {
                if (m_listener != null)
                    m_listener.Stop();
                m_listener = null;
                m_callBackObj.OnServerStarted(this, StartStatus.FAIL_SOCKET_ERROR);
                return;
            }
            m_callBackObj.OnServerStarted(this, StartStatus.SUCCESS);
        }

        /// <summary>
        /// Accept callback function
        /// </summary>
        /// <param name="result">result</param>
        private static void onAccept(IAsyncResult result)
        {
            IocpTcpServer server = result.AsyncState as IocpTcpServer;
            TcpClient client = server.m_listener.EndAcceptTcpClient(result);
            
            try { server.m_listener.BeginAcceptTcpClient(new AsyncCallback(IocpTcpServer.onAccept), server); }
            catch {
                client.Close();
                server.StopServer(); 
                return; 
            }

            IocpTcpSocket socket = new IocpTcpSocket(client, server);
            SocketCallbackInterface socketCallbackObj=server.m_callBackObj.OnAccept(server, socket.GetIPInfo());
            if (socketCallbackObj == null)
            {
                socket.Disconnect();
            }
            else
            {
                socket.SetSocketCallbackInterface(socketCallbackObj);
                socket.Start();
                lock (server.m_listLock)
                {
                    server.m_socketList.Add(socket);
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
            if (ops.callBackObj == null)
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
                if (!IsServerStarted())
                    return;
                m_listener.Stop();
                m_listener = null;
            }
            ShutdownAllClient();

            if(m_callBackObj!=null) 
                m_callBackObj.OnServerStopped(this);
        }

        /// <summary>
        /// Check if the server is started
        /// </summary>
        /// <returns>true if the server is started, otherwise false</returns>
        public bool IsServerStarted()
        {
            if (m_listener != null)
                return true;
            return false;
        }
        /// <summary>
        /// Shut down all the client, connected
        /// </summary>
        public void ShutdownAllClient()
        {
            lock (m_listLock)
            {
                foreach (IocpTcpSocket socket in m_socketList)
                {
                    socket.Disconnect();
                }
                m_socketList.Clear();
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
            m_socketList.Clear();
            
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

    }
}
