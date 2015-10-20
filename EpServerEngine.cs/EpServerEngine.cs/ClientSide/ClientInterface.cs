/*! 
@file ClientInterface.cs
@author Woong Gyu La a.k.a Chris. <juhgiyo@gmail.com>
		<http://github.com/juhgiyo/epserverengine.cs>
@date April 01, 2014
@brief ClientInterface Interface
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

A ClientInterface Class.

*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace EpServerEngine.cs
{
    /// <summary>
    /// Client Option class
    /// </summary>
    public sealed class ClientOps{
        /// <summary>
        /// callback object
        /// </summary>
        public INetworkClientCallback CallBackObj
        {
            get;
            set;
        }
        /// <summary>
        /// hostname
        /// </summary>
        public String HostName
        {
            get;
            set;
        }
        /// <summary>
        /// port
        /// </summary>
        public String Port
        {
            get;
            set;
        }
        /// <summary>
        /// flag for no delay
        /// </summary>
        public bool NoDelay
        {
            get;
            set;
        }
        /// <summary>
        /// connection time out in millisecond
        /// </summary>
        public int ConnectionTimeOut
        {
            get;
            set;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public ClientOps()
		{
			CallBackObj=null;
			HostName=ServerConf.DEFAULT_HOSTNAME;
			Port=ServerConf.DEFAULT_PORT;
            NoDelay = true;
            ConnectionTimeOut = Timeout.Infinite;
		}
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="callBackObj">callback object</param>
        /// <param name="hostName">hostname</param>
        /// <param name="port">port</param>
        /// <param name="noDelay">flag for no delay</param>
        /// <param name="connectionTimeOut">connection wait time in millisecond</param>
        public ClientOps(INetworkClientCallback callBackObj, String hostName, String port, bool noDelay = true, int connectionTimeOut = Timeout.Infinite)
        {
            this.CallBackObj = callBackObj;
            this.HostName = hostName;
            this.Port = port;
            this.NoDelay = noDelay;
            this.ConnectionTimeOut = connectionTimeOut;
        }
        /// <summary>
        /// default client option
        /// </summary>
		public static ClientOps defaultClientOps=new ClientOps();
	};

    /// <summary>
    /// Client interface
    /// </summary>
    public interface INetworkClient
    {
        /// <summary>
        /// Return the hostname
        /// </summary>
        /// <returns>hostname</returns>
        String HostName { get; }

        /// <summary>
        /// Return the port
        /// </summary>
        /// <returns>port</returns>
        String Port{get;}
        /// <summary>
        /// Return no delay flag
        /// </summary>
        bool NoDelay
        {
            get;
        }

        /// <summary>
        /// Return connection time out in milliseconds
        /// </summary>
        int ConnectionTimeOut
        {
            get;
        }
        /// <summary>
        /// callback object
        /// </summary>
        INetworkClientCallback CallBackObj
        {
            get;
            set;
        }


        /// <summary>
        /// Connect to server with given option
        /// </summary>
        /// <param name="ops">option for client</param>
        void Connect(ClientOps ops);

        /// <summary>
        /// Disconnect from the server
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Check if the connection is alive
        /// </summary>
        /// <returns></returns>
        bool IsConnectionAlive { get; }

        /// <summary>
        /// Send given packet to the server
        /// </summary>
        /// <param name="packet">packet to send</param>
        void Send(Packet packet);

        /// <summary>
        /// Send given data to the server
        /// </summary>
        /// <param name="data">data in byte array</param>
        /// <param name="offset">offset in bytes</param>
        /// <param name="dataSize">data size in bytes</param>
        void Send(byte[] data, int offset, int dataSize);

        /// <summary>
        /// Send given data to the server
        /// </summary>
        /// <param name="data">data in byte array</param>
        void Send(byte[] data);

        
    }

	public interface INetworkClientCallback{
        /// <summary>
        /// Connection callback
        /// </summary>
        /// <param name="client">client</param>
        /// <param name="status">connection status</param>
        void OnConnected(INetworkClient client, ConnectStatus status);

        /// <summary>
        /// Receive callback
        /// </summary>
        /// <param name="client">client</param>
        /// <param name="receivedPacket">received packet</param>
	    void OnReceived(INetworkClient client, Packet receivedPacket);

        /// <summary>
        /// Send callback
        /// </summary>
        /// <param name="client">client</param>
        /// <param name="status">send status</param>
        /// <param name="sentPacket">sent packet</param>
        void OnSent(INetworkClient client, SendStatus status, Packet sentPacket);

        /// <summary>
        /// Disconnect callback
        /// </summary>
        /// <param name="client">client</param>
        void OnDisconnect(INetworkClient client);
	};
}
