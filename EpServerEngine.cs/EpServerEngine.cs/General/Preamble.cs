/*! 
@file Preamble.cs
@author Woong Gyu La a.k.a Chris. <juhgiyo@gmail.com>
		<http://github.com/juhgiyo/epserverengine.cs>
@date April 01, 2014
@brief Preamble Interface
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

A Preamble Class.

*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace EpServerEngine.cs
{
    /// <summary>
    /// Preamble class
    /// </summary>
    public class Preamble
    {
        /// <summary>
        /// byte size of size packet
        /// </summary>
        public const int SIZE_PACKET_LENGTH = 12;
        /// <summary>
        /// preamble
        /// </summary>
        private const ulong preamble = 0x00F0F0F0F0F0F0F8;

        /// <summary>
        /// Built preamble byte array by size of shouldReceive
        /// </summary>
        /// <param name="shouldReceive">size of byte to make preamble byte array</param>
        /// <returns>byte array created, and null if shouldReceive < 0</returns>
        public static  byte[] ToPreamblePacket(int shouldReceive)
        {
            if (shouldReceive < 0)
                return null;
            byte[] byteArr = new byte[Preamble.SIZE_PACKET_LENGTH];
            using (MemoryStream stream = new MemoryStream(byteArr))
            {
                stream.Write(BitConverter.GetBytes(preamble), 0, 8);
                stream.Write(BitConverter.GetBytes(shouldReceive), 0, 4);
                return byteArr;
            }
        }
        /// <summary>
        /// Returns positive shouldReceive size from preamble packet
        /// </summary>
        /// <param name="preamblePacket">preamble packet to convert</param>
        /// <returns>positive number if preamble is correct and positive number of shouldReceive is found otherwise -1 </returns>
        public static int ToShouldReceive(byte[] preamblePacket)
        {
            using (MemoryStream stream = new MemoryStream(preamblePacket))
            {
                ulong curPreamble = BitConverter.ToUInt64(preamblePacket, 0);
                int shouldReceive = BitConverter.ToInt32(preamblePacket, 8);
                if (preamble != curPreamble || shouldReceive < 0)
                    return -1;
                return shouldReceive;
            }
        }

        /// <summary>
        /// Return the possible preamble start index from preamblePacket received
        /// </summary>
        /// <param name="preamblePacket">preamble packet received</param>
        /// <returns>index of possible preamble start of preamblePacket</returns>
        public static int CheckPreamble(byte[] preamblePacket)
        {
            byte[] correctPreamble =BitConverter.GetBytes(preamble);
            int preTrav = 0;
            for(preTrav=0;preTrav<preamblePacket.Length;preTrav++)
            {
                bool contains = true;
                for (int idx = 0; idx < correctPreamble.Length; idx++)
                {
                    if (idx + preTrav >= preamblePacket.Length)
                        break;
                    if (correctPreamble[idx] != preamblePacket[preTrav + idx])
                    {
                        contains = false;
                        break;
                    }
                }
                if (contains == true)
                    break;
                
            }
            return preTrav;
        }
    }
}
