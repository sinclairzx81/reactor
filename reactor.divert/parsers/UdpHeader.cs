/*--------------------------------------------------------------------------

Reactor.Divert

The MIT License (MIT)

Copyright (c) 2015 Haydn Paterson (sinclair) <haydn.developer@gmail.com>

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

---------------------------------------------------------------------------*/


using System.IO;
using System.Net;

namespace Reactor.Divert.Parsers
{
    public class Udpheader
    {
        #region Header

        // 0      7 8     15 16    23 24    31 
        // +--------+--------+--------+--------+
        // |          source address           |
        // +--------+--------+--------+--------+
        // |        destination address        |
        // +--------+--------+--------+--------+
        // |  zero  |protocol|   UDP length    |
        // +--------+--------+--------+--------+

        #endregion

        private ushort usSourcePort;            //Sixteen bits for the source port number

        private ushort usDestinationPort;       //Sixteen bits for the destination port number
        
        private ushort usLength;                //Length of the UDP header
        
        private short  sChecksum;               //Sixteen bits for the checksum

        private byte[] byUDPData = new byte[4096];  //Data carried by the UDP packet

        public Udpheader(byte [] data)
        {
            using(var stream = new MemoryStream(data, 0, data.Length))
            {
                using (var reader = new BinaryReader(stream))
                {
                    //-----------------------------------------------
                    // The first sixteen bits contain the source port
                    //-----------------------------------------------

                    this.usSourcePort      = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());

                    //-----------------------------------------------
                    // The next sixteen bits contain the destination port
                    //-----------------------------------------------

                    this.usDestinationPort = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());

                    //-----------------------------------------------
                    // The next sixteen bits contain the length of the UDP packet
                    //-----------------------------------------------

                    this.usLength          = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());

                    //-----------------------------------------------
                    // The next sixteen bits contain the checksum
                    //-----------------------------------------------

                    this.sChecksum         = IPAddress.NetworkToHostOrder(reader.ReadInt16());

                    //-----------------------------------------------
                    // Copy the data carried by the UDP packet into the data buffer
                    //-----------------------------------------------

                    System.Buffer.BlockCopy(data,
                                            8,               
                                            byUDPData,
                                            0,
                                            data.Length - 8);
                }
            }


        }

        public string SourcePort      
        {
            get
            {
                return usSourcePort.ToString();
            }
        }

        public string DestinationPort 
        {
            get
            {
                return usDestinationPort.ToString();
            }
        }

        public string Length          
        {
            get
            {
                return usLength.ToString ();
            }
        }

        public string Checksum        
        {
            get
            {
                //Return the checksum in hexadecimal format
                return string.Format("0x{0:x2}", sChecksum);
            }
        }

        public byte[] Data            
        {
            get
            {
                return byUDPData;
            }
        }

        #region Statics

        public static Udpheader Create(byte [] data)
        {
            return new Udpheader(data);
        }

        #endregion
    }
}