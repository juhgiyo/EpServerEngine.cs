/*! 
@file ServerInterface.cs
@author Woong Gyu La a.k.a Chris. <juhgiyo@gmail.com>
		<http://github.com/juhgiyo/epserverengine.cs>
@date April 01, 2014
@brief ServerInterface Interface
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

A ServerInterface Class.

*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace EpServerEngine.cs
{
    /// <summary>
    /// Server option class
    /// </summary>
    public sealed class ServerOps
    {
        /// <summary>
        /// callback object
        /// </summary>
        public INetworkServerCallback callBackObj;

        /// <summary>
        /// port
        /// </summary>
        public String port;

        /// <summary>
        /// Default constructor
        /// </summary>
        public ServerOps()
        {
            callBackObj = null;
            port = ServerConf.DEFAULT_PORT;
        }
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="callBackObj">callback object</param>
        /// <param name="port">port</param>
        public ServerOps(INetworkServerCallback callBackObj, String port)
        {
            this.port = port;
            this.callBackObj = callBackObj;
        }

        /// <summary>
        /// Default server option
        /// </summary>
        public static ServerOps defaultServerOps = new ServerOps();
    };

    /// <summary>
    /// Server interface
    /// </summary>
    public interface INetworkServer
    {
        /// <summary>
        /// Return the port
        /// </summary>
        /// <returns>port</returns>
        String GetPort();

        /// <summary>
        /// Start the server with given option
        /// </summary>
        /// <param name="ops">option for the server</param>
        void StartServer(ServerOps ops);

        /// <summary>
        /// Stop the server
        /// </summary>
        void StopServer();

        /// <summary>
        /// Check whether server is started or not
        /// </summary>
        /// <returns>true if server is started, otherwise false</returns>
        bool IsServerStarted();
        /// <summary>
        /// Shutdown all the client, connected
        /// </summary>
        void ShutdownAllClient();

        /// <summary>
        /// Broadcast the given packet to all the client, connected
        /// </summary>
        /// <param name="packet">packet to broadcast</param>
        void Broadcast(Packet packet);

        /// <summary>
        /// Return the connected client list
        /// </summary>
        /// <returns>the connected client list</returns>
        List<IocpTcpSocket> GetClientSocketList();

        /// <summary>
        /// Detach the given client from the server management
        /// </summary>
        /// <param name="clientSocket">the client to detach</param>
        /// <returns>true if successful, otherwise false</returns>
        bool DetachClient(IocpTcpSocket clientSocket);

    }

    /// <summary>
    /// Server callback interface
    /// </summary>
    public interface INetworkServerCallback
    {
        /// <summary>
        /// Server started callback
        /// </summary>
        /// <param name="server">server</param>
        /// <param name="status">start status</param>
        void OnServerStarted(INetworkServer server, StartStatus status);
        /// <summary>
        /// Accept callback
        /// </summary>
        /// <param name="server">server</param>
        /// <param name="ipInfo">connection info</param>
        /// <returns>the socket callback interface</returns>
        INetworkSocketCallback OnAccept(INetworkServer server, IPInfo ipInfo);
        /// <summary>
        /// Server stopped callback
        /// </summary>
        /// <param name="server">server</param>
        void OnServerStopped(INetworkServer server);
    };

    /// <summary>
    /// Socket interface
    /// </summary>
    public interface INetworkSocket
    {
        /// <summary>
        /// Disconnect the client
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Check if the connection is alive
        /// </summary>
        /// <returns>true if the connection is alive, otherwise false</returns>
        bool IsConnectionAlive();

        /// <summary>
        /// Send given packet to the client
        /// </summary>
        /// <param name="packet">the packet to send</param>
        void Send(Packet packet);

        /// <summary>
        /// Return the IP information of the client
        /// </summary>
        /// <returns>the IP information of the client</returns>
        IPInfo GetIPInfo();

        /// <summary>
        /// Return the server managing this socket
        /// </summary>
        /// <returns>the server managing this socket</returns>
        INetworkServer GetServer();

    }

    /// <summary>
    /// Socket callback interface
    /// </summary>
    public interface INetworkSocketCallback
    {
        /// <summary>
        /// NewConnection callback
        /// </summary>
        /// <param name="socket">client socket</param>
        void OnNewConnection(INetworkSocket socket);

        /// <summary>
        /// Receive callback
        /// </summary>
        /// <param name="socket">client socket</param>
        /// <param name="receivedPacket">received packet</param>
        void OnReceived(INetworkSocket socket, Packet receivedPacket);

        /// <summary>
        /// Send callback
        /// </summary>
        /// <param name="socket">client socket</param>
        /// <param name="status">stend status</param>
        void OnSent(INetworkSocket socket, SendStatus status);

        /// <summary>
        /// Disconnect callback
        /// </summary>
        /// <param name="socket">client socket</param>
        void OnDisconnect(INetworkSocket socket);
    };
    
    /// <summary>
    /// IP End-point type
    /// </summary>
    public enum IPEndPointType
    {
        /// <summary>
        /// local
        /// </summary>
        LOCAL = 0,
        /// <summary>
        /// remote
        /// </summary>
        REMOTE
    }

    /// <summary>
    /// IP Information class
    /// </summary>
    public sealed class IPInfo
    {
        /// <summary>
        /// IP Address string
        /// </summary>
        String m_ipAddress;
        /// <summary>
        /// IP End-Point
        /// </summary>
        IPEndPoint m_ipEndPoint;
        /// <summary>
        /// IP End-Point type
        /// </summary>
        IPEndPointType m_ipEndPointType;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="ipAddress">IP Address string</param>
        /// <param name="ipEndPoint">IP End-Point</param>
        /// <param name="ipEndPointType">IP End-Point type</param>
        public IPInfo(String ipAddress, IPEndPoint ipEndPoint, IPEndPointType ipEndPointType)
        {
            m_ipAddress = ipAddress;
            m_ipEndPoint = ipEndPoint;
            m_ipEndPointType = ipEndPointType;
        }
        /// <summary>
        /// Return the IP address string
        /// </summary>
        /// <returns>the IP address string</returns>
        public String GetIPAddress()
        {
            return m_ipAddress;
        }

        /// <summary>
        /// Return the IP End-point
        /// </summary>
        /// <returns>the IP End-point</returns>
        public IPEndPoint GetIPEndPoint()
        {
            return m_ipEndPoint;
        }

        /// <summary>
        /// Return the IP End-point type
        /// </summary>
        /// <returns>the IP End-point type</returns>
        public IPEndPointType GetIPEndPointType()
        {
            return m_ipEndPointType;
        }
    }
}
