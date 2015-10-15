/*! 
@file Packet.cs
@author Woong Gyu La a.k.a Chris. <juhgiyo@gmail.com>
		<http://github.com/juhgiyo/epserverengine.cs>
@date April 01, 2014
@brief Packet Interface
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

A Packet Class.

*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Diagnostics;

namespace EpServerEngine.cs
{
    /// <summary>
    /// Packet class
    /// </summary>
    public sealed class Packet
    {

        /// <summary>
        /// packet
        /// </summary>
        private byte[] m_packet;
        /// <summary>
        /// packet byte size
        /// </summary>
        private int m_packetSize;

        /// <summary>
        /// offset from start idx
        /// </summary>
        private int m_offset;
        /// <summary>
        /// flag whether memory is allocated in this object or not
        /// </summary>
        private bool m_isAllocated;
        /// <summary>
        /// lock
        /// </summary>
        private Object m_packetLock = new Object();

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="packet">packet</param>
        /// <param name="offset">offset in byte</param>
        /// <param name="byteSize">packet size in byte</param>
        /// <param name="shouldAllocate">flag whether to allocate memory or not</param>
        public Packet(byte[] packet = null, int offset=0, int byteSize = 0, bool shouldAllocate = true)
        {
            m_packet = null;
            m_packetSize = 0;
            m_isAllocated = shouldAllocate;
            if (shouldAllocate)
            {
                if (byteSize > 0)
                {
                    m_packet = new byte[byteSize];
                    if (packet != null)
                    {
                        Array.Copy(packet,offset, m_packet,0, byteSize);
                    }
                    else
                    {
                        // Comment out due to performance issue
                        //Array.Clear(m_packet, 0, m_packet.Count());
                    }
                    m_offset = 0;
                    m_packetSize = byteSize;
                }
            }
            else
            {
                m_packet = packet;
                m_offset = offset;
                m_packetSize = byteSize;
            }
        }

        /// <summary>
        /// Default copy constructor
        /// </summary>
        /// <param name="b">the object to copy from</param>
        public Packet(Packet b)
        {

            lock(b.m_packetLock)
            {
                m_packet=null;
	            if(b.m_isAllocated)
	            {
		            if(b.m_packetSize>0)
		            {
			            m_packet=new byte[b.m_packetSize];
                        Array.Copy(b.m_packet, m_packet, b.m_packetSize);
		            }
	            }
	            else
	            {
		            m_packet=b.m_packet;
	            }
                m_packetSize = b.m_packetSize;
                m_offset = b.m_offset;
	            m_isAllocated=b.m_isAllocated;
            }
	        
        }

        /// <summary>
        /// Return the size of packet in byte
        /// </summary>
        /// <returns>the size of packet in byte</returns>
        public int GetPacketByteSize()
        {
            return m_packetSize;
        }

        /// <summary>
        /// Return the offset of the raw data
        /// </summary>
        /// <returns>the offset of the raw data</returns>
        public int GetOffset()
        {
            return m_offset;
        }

        /// <summary>
        /// Return the size of packet allocated in byte
        /// </summary>
        /// <returns>the size of packet allocated in byte</returns>
        public int GetAllocatedByteSize()
        {
            if (m_packet != null)
            {
                return m_packet.Length;
            }
            return 0;
        }

        /// <summary>
        /// Check the flag whether allocated memory or not
        /// </summary>
        /// <returns>true if allocated, otherwise false</returns>
        public bool IsAllocated()
		{
			return m_isAllocated;
		}
        /// <summary>
        /// Return the actual packet
        /// </summary>
        /// <returns>the actual packet</returns>
        public byte[] GetPacket()
        {
            return m_packet;
        }
        /// <summary>
        /// Set the packet with given byte array
        /// </summary>
        /// <param name="packet">packet data</param>
        /// <param name="offset">offset in byte</param>
        /// <param name="packetByteSize">packet in byte size</param>
        public void SetPacket(byte[] packet, int offset, int packetByteSize)
        {
            lock (m_packetLock)
            {
               	if(m_isAllocated)
	            {
                    if (m_packet != null)
                    {
                        if (m_packet.Length >= packetByteSize)
                        {
                            Array.Copy(packet,offset, m_packet,0, packetByteSize);
                            m_packetSize = packetByteSize;
                            m_offset = 0;
                            return;
                        }
                    }
		            m_packet=null;
		            if(packetByteSize>0)
		            {
			            m_packet=new byte[packetByteSize];
			            Debug.Assert(m_packet!=null);
		            }
                    if (packet != null)
                        Array.Copy(packet,offset, m_packet,0, packetByteSize);
                    else
                    {
                        // Comment out due to performance issue
                        //Array.Clear(m_packet, 0, m_packet.Count());
                    }
		            m_packetSize=packetByteSize;
                    m_offset = 0;

	            }
	            else
	            {
		            m_packet=packet;
		            m_packetSize=packetByteSize;
                    m_offset = offset;
	            }
            }
        }

        /// <summary>
        /// Reset the packet
        /// </summary>
        private void resetPacket()
        {
            m_packet = null;
        }


    }
}
