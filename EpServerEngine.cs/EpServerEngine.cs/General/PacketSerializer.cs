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

namespace IocpServer
{
    public class PacketSerializer<PacketStruct> where PacketStruct : class,ISerializable
    {
        private PacketStruct m_packet=null;
        private MemoryStream m_stream = null;
        private IFormatter m_formatter = new BinaryFormatter();
        
		
		/// lock
        private Object m_packetContainerLock = new Object();

        public PacketSerializer(PacketStruct packet=null)
        {
             m_formatter.Serialize(m_stream,m_packet);      
        }


        public PacketSerializer(byte[] rawData)
        {
            m_stream = new MemoryStream(rawData);
        }

        public PacketSerializer(PacketSerializer<PacketStruct> orig)
        {
            m_stream=orig.m_stream;
        }

        ~PacketSerializer()
        {
            m_formatter = null;
        }



        public PacketStruct GetPacket()
        {
            return (PacketStruct)m_formatter.Deserialize(m_stream);
        }

        public byte[] GetPacketRaw()
        {
            //return m_stream.ToArray();
            return m_stream.GetBuffer();

        }

        public long GetPacketByteSize()
        {
            return m_stream.Length;   
        }

        public void SetPacket(PacketStruct packet)
        {
            m_formatter.Serialize(m_stream,m_packet);            
        }


        public void SetPacket(byte[] rawData)
        {
            m_stream = new MemoryStream(rawData);

        }

    }
}
