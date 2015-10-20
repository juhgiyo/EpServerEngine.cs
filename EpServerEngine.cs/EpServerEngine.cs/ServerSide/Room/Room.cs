/*! 
@file Room.cs
@author Woong Gyu La a.k.a Chris. <juhgiyo@gmail.com>
		<http://github.com/juhgiyo/epserverengine.cs>
@date April 01, 2014
@brief Room class
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

A Room Class.

*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpServerEngine.cs
{
    public sealed class Room: IRoom
    {
        /// <summary>
        /// socket list
        /// </summary>
        private HashSet<INetworkSocket> m_socketList = new HashSet<INetworkSocket>();
        
        /// <summary>
        /// name of the room
        /// </summary>
        private string m_roomName;

        /// <summary>
        /// general lock
        /// </summary>
        private Object m_generalLock = new Object();

        /// <summary>
        /// list lock
        /// </summary>
        private Object m_listLock = new Object();

        /// <summary>
        /// callback object
        /// </summary>
        private IRoomCallback m_callBackObj;
        /// <summary>
        /// Callback Object property
        /// </summary>
        public IRoomCallback CallBackObj
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
        /// Room name property
        /// </summary>
        public string RoomName
        {
            get
            {
                lock (m_generalLock)
                {
                    return m_roomName;
                }
            }
            private set
            {
                lock (m_generalLock)
                {
                    m_roomName = value;
                }
            }
        }
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="roomName">name of the room</param>
        /// <param name="callbackObj">callback Obj</param>
        public Room(string roomName, IRoomCallback callbackObj=null)
        {
            RoomName = roomName;
            CallBackObj = callbackObj;
            if (CallBackObj != null)
            {
                Task t = new Task(delegate()
                {
                    CallBackObj.OnCreated(this);
                });
                t.Start();
            }
        }

        public void AddSocket(INetworkSocket socket)
        {
            lock (m_listLock)
            {
                m_socketList.Add(socket);
            }
            if (CallBackObj != null)
            {
                Task t = new Task(delegate()
                {
                    CallBackObj.OnJoin(this,socket);
                });
                t.Start();
            }
        }

        /// <summary>
        /// Return the client socket list
        /// </summary>
        /// <returns>the client socket list</returns>
        public List<INetworkSocket> GetSocketList()
        {
            lock (m_listLock)
            {
                return new List<INetworkSocket>(m_socketList);
            }
        }

        /// <summary>
        /// Detach the given client from the server management
        /// </summary>
        /// <param name="clientSocket">the client to detach</param>
        /// <returns>the number of socket in the room</returns>
        public int DetachClient(INetworkSocket socket)
        {
            lock (m_listLock)
            {
                m_socketList.Remove(socket);
                if (CallBackObj != null)
                {
                    Task t = new Task(delegate()
                    {
                        CallBackObj.OnLeave(this, socket);
                    });
                    t.Start();
                }
                return m_socketList.Count;
            }
        }

        /// <summary>
        /// Broadcast the given packet to all the client, connected
        /// </summary>
        /// <param name="packet">packet to broadcast</param>
        public void Broadcast(Packet packet)
        {
            List<INetworkSocket> list = GetSocketList();
            foreach (INetworkSocket socket in list)
            {
                socket.Send(packet);
            }
            if (CallBackObj != null)
            {
                Task t = new Task(delegate()
                {
                    CallBackObj.OnBroadcast(this,packet);
                });
                t.Start();
            }
        }

        /// <summary>
        /// Broadcast the given packet to all the client, connected
        /// </summary>
        /// <param name="data">data in byte array</param>
        /// <param name="offset">offset in bytes</param>
        /// <param name="dataSize">data size in bytes</param>
        public void Broadcast(byte[] data, int offset, int dataSize)
        {
            Broadcast(new Packet(data, offset, dataSize,false));
        }

        /// <summary>
        /// Broadcast the given packet to all the client, connected
        /// </summary>
        /// <param name="data">data in byte array</param>
        public void Broadcast(byte[] data)
        {
            Broadcast(new Packet(data,0,data.Count(),false));
        }

  
    }
}
