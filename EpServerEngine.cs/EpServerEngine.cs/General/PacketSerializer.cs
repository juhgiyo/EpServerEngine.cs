/*! 
@file PacketSerializer.cs
@author Woong Gyu La a.k.a Chris. <juhgiyo@gmail.com>
		<http://github.com/juhgiyo/epserverengine.cs>
@date April 01, 2014
@brief PacketSerializer Interface
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

A PacketSerializer Class.

*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace EpServerEngine.cs
{
    /// <summary>
    /// Packet Serializer
    /// </summary>
    /// <typeparam name="PacketStruct">packet class type</typeparam>
    public sealed class PacketSerializer<PacketStruct> where PacketStruct : class,ISerializable
    {
        /// <summary>
        /// packet class object
        /// </summary>
        private PacketStruct m_packet=null;
        /// <summary>
        /// stream
        /// </summary>
        private MemoryStream m_stream = null;
        /// <summary>
        /// formatter
        /// </summary>
        private IFormatter m_formatter = new BinaryFormatter();
        
        /// <summary>
        /// lock
        /// </summary>
        private Object m_packetContainerLock = new Object();

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="packet">packet class object</param>
        public PacketSerializer(PacketStruct packet=null)
        {
             m_formatter.Serialize(m_stream,m_packet);      
        }

        /// <summary>
        /// Default  constructor
        /// </summary>
        /// <param name="rawData">serialized packet</param>
        public PacketSerializer(byte[] rawData)
        {
            m_stream = new MemoryStream(rawData);
        }

        /// <summary>
        /// Default copy constructor
        /// </summary>
        /// <param name="orig">the object to copy from</param>
        public PacketSerializer(PacketSerializer<PacketStruct> orig)
        {
            m_stream=orig.m_stream;
        }

        ~PacketSerializer()
        {
            m_formatter = null;
        }


        /// <summary>
        /// Get packet class object
        /// </summary>
        /// <returns>packet class object</returns>
        public PacketStruct GetPacket()
        {
            return (PacketStruct)m_formatter.Deserialize(m_stream);
        }

        /// <summary>
        /// Get serialized packet
        /// </summary>
        /// <returns>serialized packet</returns>
        public byte[] GetPacketRaw()
        {
            //return m_stream.ToArray();
            return m_stream.GetBuffer();

        }
        /// <summary>
        /// Return size of packet in byte
        /// </summary>
        /// <returns>size of packet in byte</returns>
        public long GetPacketByteSize()
        {
            return m_stream.Length;   
        }

        /// <summary>
        /// Set packet class object
        /// </summary>
        /// <param name="packet">packet class object</param>
        public void SetPacket(PacketStruct packet)
        {
            m_formatter.Serialize(m_stream,m_packet);            
        }

        /// <summary>
        /// Set serialize packet
        /// </summary>
        /// <param name="rawData">serialize packet</param>
        public void SetPacket(byte[] rawData)
        {
            m_stream = new MemoryStream(rawData);

        }

    }
}
