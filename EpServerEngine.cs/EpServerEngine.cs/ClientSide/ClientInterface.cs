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
using System.Threading.Tasks;
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
		public ClientCallbackInterface callBackObj;
        /// <summary>
        /// hostname
        /// </summary>
        public String hostName;
        /// <summary>
        /// port
        /// </summary>
		public String port;
        /// <summary>
        /// flag for no delay
        /// </summary>
        public bool noDelay;
        /// <summary>
        /// wait time in millisecond
        /// </summary>
        public int waitTimeInMilliSec;

        /// <summary>
        /// Default constructor
        /// </summary>
        public ClientOps()
		{
			callBackObj=null;
			hostName=ServerConf.DEFAULT_HOSTNAME;
			port=ServerConf.DEFAULT_PORT;
            noDelay = true;
            waitTimeInMilliSec = Timeout.Infinite;
		}
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="callBackObj">callback object</param>
        /// <param name="hostName">hostname</param>
        /// <param name="port">port</param>
        /// <param name="noDelay">flag for no delay</param>
        /// <param name="waitTimeInMilliSec">wait time in millisecond</param>
        public ClientOps(ClientCallbackInterface callBackObj, String hostName, String port, bool noDelay = true, int waitTimeInMilliSec = Timeout.Infinite)
        {
            this.callBackObj = callBackObj;
            this.hostName = hostName;
            this.port = port;
            this.noDelay = noDelay;
            this.waitTimeInMilliSec = waitTimeInMilliSec;
        }
        /// <summary>
        /// default client option
        /// </summary>
		public static ClientOps defaultClientOps=new ClientOps();
	};

    /// <summary>
    /// Client interface
    /// </summary>
    public interface ClientInterface
    {
        /// <summary>
        /// Return the hostname
        /// </summary>
        /// <returns>hostname</returns>
        String GetHostName();

        /// <summary>
        /// Return the port
        /// </summary>
        /// <returns>port</returns>
        String GetPort();

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
        bool IsConnectionAlive();

        /// <summary>
        /// Send given packet to the server
        /// </summary>
        /// <param name="packet">packet to send</param>
        void Send(Packet packet);

        
    }

	public interface ClientCallbackInterface{
        /// <summary>
        /// Connection callback
        /// </summary>
        /// <param name="client">client</param>
        /// <param name="status">connection status</param>
        void OnConnected(ClientInterface client, ConnectStatus status);

        /// <summary>
        /// Receive callback
        /// </summary>
        /// <param name="client">client</param>
        /// <param name="receivedPacket">received packet</param>
	    void OnReceived(ClientInterface client, Packet receivedPacket);

        /// <summary>
        /// Send callback
        /// </summary>
        /// <param name="client">client</param>
        /// <param name="status">send status</param>
        void OnSent(ClientInterface client, SendStatus status);

        /// <summary>
        /// Disconnect callback
        /// </summary>
        /// <param name="client">client</param>
        void OnDisconnect(ClientInterface client);
	};
}
