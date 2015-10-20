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
        /// OnCreated event
        /// </summary>
        OnRoomCreatedDelegate m_onCreated = delegate { };
        /// <summary>
        /// OnJoin event
        /// </summary>
        OnRoomJoinDelegate m_onJoin = delegate { };
        /// <summary>
        /// OnLeave event
        /// </summary>
        OnRoomLeaveDelegate m_onLeave = delegate { };
        /// <summary>
        /// OnBroadcast event
        /// </summary>
        OnRoomBroadcastDelegate m_onBroadcast = delegate { };
        /// <summary>
        /// OnDestroy event
        /// </summary>
        OnRoomDestroyDelegate m_onDestroy = delegate { };

        /// <summary>
        /// OnCreated event
        /// </summary>
        public OnRoomCreatedDelegate OnCreated
        {
            get
            {
                return m_onCreated;
            }
            set
            {
                if (value == null)
                {
                    m_onCreated = delegate { };
                }
                else
                {
                    m_onCreated = value;
                }
            }
        }
        /// <summary>
        /// OnJoin event
        /// </summary>
        public OnRoomJoinDelegate OnJoin
        {
            get
            {
                return m_onJoin;
            }
            set
            {
                if (value == null)
                {
                    m_onJoin = delegate { };
                }
                else
                {
                    m_onJoin = value;
                }
            }
        }
        /// <summary>
        /// OnLeave event
        /// </summary>
        public OnRoomLeaveDelegate OnLeave
        {
            get
            {
                return m_onLeave;
            }
            set
            {
                if (value == null)
                {
                    m_onLeave = delegate { };
                }
                else
                {
                    m_onLeave = value;
                }
            }
        }
        /// <summary>
        /// OnBroadcast event
        /// </summary>
        public OnRoomBroadcastDelegate OnBroadcast
        {
            get
            {
                return m_onBroadcast;
            }
            set
            {
                if (value == null)
                {
                    m_onBroadcast = delegate { };
                }
                else
                {
                    m_onBroadcast = value;
                }
            }
        }
        /// <summary>
        /// OnDestroy event
        /// </summary>
        public OnRoomDestroyDelegate OnDestroy
        {
            get
            {
                return m_onDestroy;
            }
            set
            {
                if (value == null)
                {
                    m_onDestroy = delegate { };
                }
                else
                {
                    m_onDestroy = value;
                }
            }
        }


        /// <summary>
        /// OnCreated event
        /// </summary>
        OnRoomCreatedDelegate OnCreatedDefault=delegate{};
        /// <summary>
        /// OnJoin event
        /// </summary>
        OnRoomJoinDelegate OnJoinDefault=delegate{};
        /// <summary>
        /// OnLeave event
        /// </summary>
        OnRoomLeaveDelegate OnLeaveDefault=delegate{};
        /// <summary>
        /// OnBroadcast event
        /// </summary>
        OnRoomBroadcastDelegate OnBroadcastDefault = delegate { };
        /// <summary>
        /// OnDestroy event
        /// </summary>
        OnRoomDestroyDelegate OnDestroyDefault = delegate { };

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
                    if (m_callBackObj != null)
                    {
                        OnCreatedDefault -= m_callBackObj.OnCreated;
                        OnJoinDefault -= m_callBackObj.OnJoin;
                        OnLeaveDefault -= m_callBackObj.OnLeave;
                        OnBroadcastDefault -= m_callBackObj.OnBroadcast;
                        OnDestroyDefault -= m_callBackObj.OnDestroy;
                    }
                    m_callBackObj = value;
                    if (m_callBackObj != null)
                    {
                        OnCreatedDefault += m_callBackObj.OnCreated;
                        OnJoinDefault += m_callBackObj.OnJoin;
                        OnLeaveDefault += m_callBackObj.OnLeave;
                        OnBroadcastDefault += m_callBackObj.OnBroadcast;
                        OnDestroyDefault += m_callBackObj.OnDestroy;
                    }
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
            Task t = new Task(delegate()
            {
                OnCreatedDefault(this);
                OnCreated(this);
            });
            t.Start();

        }

        ~Room()
        {
            Task t = new Task(delegate()
            {
                OnDestroyDefault(this);
                OnDestroy(this);
            });
            t.Start();
        }

        public void AddSocket(INetworkSocket socket)
        {
            lock (m_listLock)
            {
                m_socketList.Add(socket);
            }
            
            Task t = new Task(delegate()
            {
                OnJoinDefault(this, socket);
                OnJoin(this,socket);
            });
            t.Start();
            
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
                
                Task t = new Task(delegate()
                {
                    OnLeaveDefault(this, socket);
                    OnLeave(this, socket);
                });
                t.Start();
                
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
            
            Task t = new Task(delegate()
            {
                OnBroadcastDefault(this, packet);
                OnBroadcast(this,packet);
            });
            t.Start();
            
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
