/*! 
@file RoomInterface.cs
@author Woong Gyu La a.k.a Chris. <juhgiyo@gmail.com>
		<http://github.com/juhgiyo/epserverengine.cs>
@date April 01, 2014
@brief Room Interface
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

A Room Interface.

*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EpServerEngine.cs
{
    public interface IRoom
    {
        /// <summary>
        /// Room name property
        /// </summary>
        string RoomName
        {
            get;
        }
        /// <summary>
        /// Callback Object property
        /// </summary>
        IRoomCallback CallBackObj
        {
            get;
            set;
        }
        
        /// <summary>
        /// Return the client socket list
        /// </summary>
        /// <returns>the client socket list</returns>
        List<INetworkSocket> GetSocketList();

        /// <summary>
        /// Broadcast the given packet to all the client, connected
        /// </summary>
        /// <param name="packet">packet to broadcast</param>
        void Broadcast(Packet packet);
        

        /// <summary>
        /// Broadcast the given packet to all the client, connected
        /// </summary>
        /// <param name="data">data in byte array</param>
        /// <param name="offset">offset in bytes</param>
        /// <param name="dataSize">data size in bytes</param>
        void Broadcast(byte[] data, int offset, int dataSize);
        

        /// <summary>
        /// Broadcast the given packet to all the client, connected
        /// </summary>
        /// <param name="data">data in byte array</param>
        void Broadcast(byte[] data);

        /// <summary>
        /// OnCreated event
        /// </summary>
        OnRoomCreatedDelegate OnCreated
        {
            get;
            set;
        }
        /// <summary>
        /// OnJoin event
        /// </summary>
        OnRoomJoinDelegate OnJoin
        {
            get;
            set;
        }
        /// <summary>
        /// OnLeave event
        /// </summary>
        OnRoomLeaveDelegate OnLeave
        {
            get;
            set;
        }
        /// <summary>
        /// OnBroadcast event
        /// </summary>
        OnRoomBroadcastDelegate OnBroadcast
        {
            get;
            set;
        }
        /// <summary>
        /// OnDestroy event
        /// </summary>
        OnRoomDestroyDelegate OnDestroy
        {
            get;
            set;
        }

    }

        public delegate void OnRoomCreatedDelegate(IRoom room);
        public delegate void OnRoomJoinDelegate(IRoom room, INetworkSocket socket);
        public delegate void OnRoomLeaveDelegate(IRoom room, INetworkSocket socket);
        public delegate void OnRoomBroadcastDelegate(IRoom room, Packet packet);
        public delegate void OnRoomDestroyDelegate(IRoom room);

    public interface IRoomCallback
    {
        /// <summary>
        /// Room created callback
        /// </summary>
        /// <param name="room">room</param>
        void OnCreated(IRoom room);

        /// <summary>
        /// Join callback
        /// </summary>
        /// <param name="room">room</param>
        /// <param name="socket">socket</param>
        void OnJoin(IRoom room, INetworkSocket socket);

        /// <summary>
        /// Leave callback
        /// </summary>
        /// <param name="room">room</param>
        /// <param name="socket">socket</param>
        void OnLeave(IRoom room, INetworkSocket socket);

        /// <summary>
        /// Broadcast callback
        /// </summary>
        /// <param name="room">room</param>
        /// <param name="packet">broadcasted packet</param>
        void OnBroadcast(IRoom room, Packet packet);


        /// <summary>
        /// Room destroyed callback
        /// </summary>
        /// <param name="room"></param>
        void OnDestroy(IRoom room);



    }
}
