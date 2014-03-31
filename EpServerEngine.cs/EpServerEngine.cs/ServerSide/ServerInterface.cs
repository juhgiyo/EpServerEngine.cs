using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace EpServerEngine.cs
{
    public sealed class ServerOps
    {

        public ServerCallbackInterface callBackObj;

        public String port;

        public ServerOps()
        {
            callBackObj = null;
            port = ServerConf.DEFAULT_PORT;
        }

        public ServerOps(ServerCallbackInterface callBackObj, String port)
        {
            this.port = port;
            this.callBackObj = callBackObj;
        }

        public static ServerOps defaultServerOps = new ServerOps();
    };

    public interface ServerInterface
    {

        String GetPort();

        void StartServer(ServerOps ops);

        void StopServer();

        bool IsServerStarted();

        void ShutdownAllClient();

        void Broadcast(Packet packet);

        List<IocpTcpSocket> GetClientSocketList();

        bool DetachClient(IocpTcpSocket clientSocket);

    }

    public interface ServerCallbackInterface
    {
        void OnServerStarted(ServerInterface server, StartStatus status);
        SocketCallbackInterface OnAccept(ServerInterface server, IPInfo ipInfo);
        void OnServerStopped(ServerInterface server);
    };

    public interface SocketInterface
    {
        void Disconnect();

        bool IsConnectionAlive();

        void Send(Packet packet);

        IPInfo GetIPInfo();

        ServerInterface GetServer();

    }

    public interface SocketCallbackInterface
    {
        void OnNewConnection(SocketInterface socket);

        void OnReceived(SocketInterface socket, Packet receivedPacket);

        void OnSent(SocketInterface socket, SendStatus status);

        void OnDisconnect(SocketInterface socket);
    };
    

    public enum IPEndPointType
    {
        LOCAL = 0,
        REMOTE
    }

    public sealed class IPInfo
    {

        String m_ipAddress;
        IPEndPoint m_ipEndPoint;
        IPEndPointType m_ipEndPointType;

        public IPInfo(String ipAddress, IPEndPoint ipEndPoint, IPEndPointType ipEndPointType)
        {
            m_ipAddress = ipAddress;
            m_ipEndPoint = ipEndPoint;
            m_ipEndPointType = ipEndPointType;
        }
        public String GetIPAddress()
        {
            return m_ipAddress;
        }

        public IPEndPoint GetIPEndPoint()
        {
            return m_ipEndPoint;
        }

        public IPEndPointType GetIPEndPointType()
        {
            return m_ipEndPointType;
        }
    }
}
