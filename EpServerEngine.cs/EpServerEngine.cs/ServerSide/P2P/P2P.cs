/*! 
@file P2P.cs
@author Woong Gyu La a.k.a Chris. <juhgiyo@gmail.com>
		<http://github.com/juhgiyo/epserverengine.cs>
@date April 01, 2014
@brief P2P class
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

A P2P Class.

*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpServerEngine.cs
{
    /// <summary>
    /// P2P class
    /// </summary>
    public sealed class P2P:IP2P,INetworkSocketCallback
    {
        /// <summary>
        /// first socket
        /// </summary>
        INetworkSocket m_socket1;
        /// <summary>
        /// second socket
        /// </summary>
        INetworkSocket m_socket2;

        /// <summary>
        /// flag whether p2p is paired
        /// </summary>
        bool m_isPaired = false;

        /// <summary>
        /// general lock
        /// </summary>
        Object m_generalLock = new Object();

        /// <summary>
        /// callback object
        /// </summary>
        IP2PCallback m_callBackObj;

        /// <summary>
        /// flag whether P2P is paired
        /// </summary>
        public bool Paired
        {
            get
            {
                lock (m_generalLock)
                {
                    return m_isPaired;
                }
            }
            private set
            {
                lock (m_generalLock)
                {
                    m_isPaired = value;
                }
            }
        }

        /// <summary>
        /// callback object
        /// </summary>
        public IP2PCallback CallBackObj
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
        /// Default constructor
        /// </summary>
        public P2P()
        {
        }

        /// <summary>
        /// Connect given two socket as p2p
        /// </summary>
        /// <param name="socket1">first socket</param>
        /// <param name="socket2">second socket</param>
        /// <param name="callback">callback object</param>
        /// <returns>true if paired otherwise false</returns>
        public bool ConnectPair(INetworkSocket socket1, INetworkSocket socket2, IP2PCallback callBackObj)
        {
            if (!Paired)
            {
                if (socket1 != null && socket2 != null && socket1.IsConnectionAlive && socket2.IsConnectionAlive)
                {
                    lock (m_generalLock)
                    {
                        m_socket1 = socket1;
                        m_socket2 = socket2;
                        m_socket1.CallBackObj = this;
                        m_socket2.CallBackObj = this;
                        Paired = true;
                        CallBackObj = callBackObj;
                        return true;
                    }
                    
                }
            }
            return false;
        }

        /// <summary>
        /// Detach pair
        /// </summary>
        public void DetachPair()
        {
            if (Paired)
            {
                lock (m_generalLock)
                {
                    if (m_socket1 != null)
                        m_socket1.CallBackObj = null;
                    if (m_socket2 != null)
                        m_socket2.CallBackObj = null;
                    Paired = false;
                    if (CallBackObj != null)
                    {
                        if (CallBackObj != null)
                            {
                                Task t = new Task(delegate()
                                {
                                    CallBackObj.OnDetached(this, m_socket1, m_socket2);
                                });
                                t.Start();
                            }
                 
                    }
                    m_socket1 = null;
                    m_socket2 = null;
                }
            }
        }

        /// <summary>
        /// NewConnection callback
        /// </summary>
        /// <param name="socket">client socket</param>
        public void OnNewConnection(INetworkSocket socket)
        {
            // Will never get called
        }

        /// <summary>
        /// Receive callback
        /// </summary>
        /// <param name="socket">client socket</param>
        /// <param name="receivedPacket">received packet</param>
        public void OnReceived(INetworkSocket socket, Packet receivedPacket)
        {
            lock (m_generalLock)
            {
                if (socket == m_socket1)
                {
                    m_socket2.Send(receivedPacket);
                }
                else
                {
                    m_socket1.Send(receivedPacket);
                }
            }
        }

        /// <summary>
        /// Send callback
        /// </summary>
        /// <param name="socket">client socket</param>
        /// <param name="status">stend status</param>
        /// <param name="sentPacket">sent packet</param>
        public void OnSent(INetworkSocket socket, SendStatus status, Packet sentPacket)
        {
        }

        /// <summary>
        /// Disconnect callback
        /// </summary>
        /// <param name="socket">client socket</param>
        public void OnDisconnect(INetworkSocket socket)
        {
            DetachPair();
        }
    }
}
