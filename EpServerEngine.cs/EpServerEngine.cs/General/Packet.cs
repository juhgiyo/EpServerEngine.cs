using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace EpServerEngine.cs
{
    public sealed class Packet
    {

        /// packet
        private byte[] m_packet;
        /// packet Byte Size
        private int m_packetSize;
        /// flag whether memory is allocated in this object or now
        private bool m_isAllocated;
        /// lock
        protected Object m_packetLock = new Object();


        public Packet(byte[] packet = null, int byteSize = 0, bool shouldAllocate = true)
        {
            m_packet = null;
            m_packetSize = 0;
            m_isAllocated = shouldAllocate;
            if (shouldAllocate)
            {
                if (byteSize > 0)
                {
                    m_packet = new byte[byteSize];
                    if (m_packet != null)
                    {
                        Array.Copy(packet, m_packet, byteSize);
                    }
                    else
                    {
                        Array.Clear(m_packet, 0, m_packet.Count());
                    }
                }
            }
            else
            {
                m_packet = packet;
                m_packetSize = byteSize;
            }
        }

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
		            m_packetSize=b.m_packetSize;
	            }
	            else
	            {
		            m_packet=b.m_packet;
		            m_packetSize=b.m_packetSize;
	            }
	            m_isAllocated=b.m_isAllocated;
            }
	        
        }

        ~Packet()
        {
            resetPacket();
        }

        public int GetPacketByteSize()
        {
            return m_packetSize;
        }
        
        public bool IsAllocated()
		{
			return m_isAllocated;
		}
        public byte[] GetPacket()
        {
            return m_packet;
        }
        public void SetPacket(byte[] packet, int packetByteSize)
        {
            lock (m_packetLock)
            {
               	if(m_isAllocated)
	            {
		            m_packet=null;
		            if(packetByteSize>0)
		            {
			            m_packet=new byte[packetByteSize];
			            Debug.Assert(m_packet!=null);
		            }
		            if(packet!=null)
                        Array.Copy(packet,m_packet,packetByteSize);
		            else
                        Array.Clear(m_packet,0,m_packet.Count());
		            m_packetSize=packetByteSize;

	            }
	            else
	            {
		            m_packet=packet;
		            m_packetSize=packetByteSize;
	            }
            }
        }

        private void resetPacket()
        {
            m_packet = null;
        }


    }
}
