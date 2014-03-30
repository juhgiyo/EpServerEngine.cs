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
    public class IocpTcpServer:ThreadEx, ServerInterface
    {
        private String m_port=ServerConf.DEFAULT_PORT;
        private TcpListener m_listener=null;
        private ServerOps m_serverOps = null;

        private ServerCallbackInterface m_callBackObj=null;

        private Object m_generalLock = new Object();

        private Object m_listLock = new Object();
        private List<IocpTcpSocket> m_socketList=new List<IocpTcpSocket>();

        public IocpTcpServer()
            : base()
        {
        }
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
        public String GetPort()
        {
            return m_port;
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

        public bool IsServerStarted()
        {
            if (m_listener != null)
                return true;
            return false;
        }

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

        public void Broadcast(Packet packet)
        {
            List<IocpTcpSocket> socketList = GetClientSocketList();

            foreach (IocpTcpSocket socket in socketList)
            {
                socket.Send(packet);
            }
            m_socketList.Clear();
            
        }

        public List<IocpTcpSocket> GetClientSocketList()
        {
            lock (m_listLock)
            {
                return new List<IocpTcpSocket>(m_socketList);
            }
        }

        public bool DetachClient(IocpTcpSocket clientSocket)
        {
            lock (m_listLock)
            {
                return m_socketList.Remove(clientSocket);
            }
        }

    }
}
