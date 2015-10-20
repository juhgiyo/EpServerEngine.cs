using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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


        public string RoomName
        {
            get
            {
                lock (m_generalLock)
                {
                    return m_roomName;
                }
            }
        }
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="roomName">name of the room</param>
        public Room(string roomName)
        {
            m_roomName = roomName;
        }

        public void AddSocket(INetworkSocket socket)
        {
            lock (m_listLock)
            {
                m_socketList.Add(socket);
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
        public int DetachClient(INetworkSocket clientSocket)
        {
            lock (m_listLock)
            {
                m_socketList.Remove(clientSocket);
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
