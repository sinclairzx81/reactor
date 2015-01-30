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
    public class TcpHeader
    {
        #region Header

        // -----------------------------------------------------------------
        //  0                   1                   2                   3   
        //  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 
        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        // |          Source Port          |       Destination Port        |
        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        // |                        Sequence Number                        |
        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        // |                    Acknowledgment Number                      |
        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        // |  Data |           |U|A|P|R|S|F|                               |
        // | Offset| Reserved  |R|C|S|S|Y|I|            Window             |
        // |       |           |G|K|H|T|N|N|                               |
        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        // |           Checksum            |         Urgent Pointer        |
        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        // |                    Options                    |    Padding    |
        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        // |                             data                              |
        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

        #endregion

        private ushort usSourcePort;                
        
        private ushort usDestinationPort;           
        
        private uint   uiSequenceNumber;       
        
        private uint   uiAcknowledgementNumber;
        
        private ushort usDataOffsetAndFlags;
        
        private ushort usWindow;
        
        private short  sChecksum;

        private ushort usUrgentPointer;        

        private byte   byHeaderLength;

        private ushort usMessageLength;

        private byte[] byTCPData = new byte[4096]; 
       
        public TcpHeader(byte [] data)
        {
            using (var stream = new MemoryStream(data, 0, data.Length))
            {
                using(var reader = new BinaryReader(stream))
                {
                    //-----------------------------------------------
                    // The first sixteen bits contain the source port
                    //-----------------------------------------------
                    usSourcePort            = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16 ());

                    //-----------------------------------------------
                    // The next sixteen contain the destiination port
                    //-----------------------------------------------
                    usDestinationPort       = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16 ());

                    //-----------------------------------------------
                    // Next thirty two have the sequence number
                    //-----------------------------------------------
                    uiSequenceNumber        = (uint)IPAddress.NetworkToHostOrder(reader.ReadInt32());

                    //-----------------------------------------------
                    // Next thirty two have the acknowledgement number
                    //-----------------------------------------------
                    uiAcknowledgementNumber = (uint)IPAddress.NetworkToHostOrder(reader.ReadInt32());

                    //-----------------------------------------------
                    // The next sixteen bits hold the flags and the data offset
                    //-----------------------------------------------
                    usDataOffsetAndFlags    = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());

                    //-----------------------------------------------
                    // The next sixteen contain the window size
                    //-----------------------------------------------
                    usWindow                = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());

                    //-----------------------------------------------
                    // In the next sixteen we have the checksum
                    //-----------------------------------------------
                    sChecksum               = (short)IPAddress.NetworkToHostOrder(reader.ReadInt16());

                    //-----------------------------------------------
                    // The following sixteen contain the urgent pointer
                    //-----------------------------------------------
                    usUrgentPointer         = (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());

                    //-----------------------------------------------
                    // The data offset indicates where the data begins, so using it we calculate the header length
                    //-----------------------------------------------
                    byHeaderLength          = (byte)(usDataOffsetAndFlags >> 12);

                    byHeaderLength         *= 4;

                    //-----------------------------------------------
                    // Message length = Total length of the TCP packet - Header length
                    //-----------------------------------------------

                    usMessageLength         = (ushort)(data.Length - byHeaderLength);

                    //-----------------------------------------------
                    // Copy the TCP data into the data buffer
                    //-----------------------------------------------
                    System.Buffer.BlockCopy(data, byHeaderLength, byTCPData, 0, data.Length - byHeaderLength);
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
                return usDestinationPort.ToString ();
            }
        }

        public string SequenceNumber
        {
            get
            {
                return uiSequenceNumber.ToString();
            }
        }

        public string AcknowledgementNumber
        {
            get
            {
                //If the ACK flag is set then only we have a valid value in
                //the acknowlegement field, so check for it beore returning 
                //anything
                if ((usDataOffsetAndFlags & 0x10) != 0)
                {
                    return uiAcknowledgementNumber.ToString();
                }
                else
                    return "";
            }
        }

        public string HeaderLength
        {
            get
            {
                return byHeaderLength.ToString();
            }
        }

        public string WindowSize
        {
            get
            {
                return usWindow.ToString();
            }
        }

        public string UrgentPointer
        {
            get
            {
                //If the URG flag is set then only we have a valid value in
                //the urgent pointer field, so check for it beore returning 
                //anything
                if ((usDataOffsetAndFlags & 0x20) != 0)
                {
                    return usUrgentPointer.ToString();
                }
                else
                    return "";
            }
        }

        public string Flags
        {
            get
            {
                //The last six bits of the data offset and flags contain the
                //control bits

                //First we extract the flags
                int nFlags = usDataOffsetAndFlags & 0x3F;
 
                string strFlags = string.Format ("0x{0:x2} (", nFlags);

                //Now we start looking whether individual bits are set or not
                if ((nFlags & 0x01) != 0)
                {
                    strFlags += "FIN, ";
                }
                if ((nFlags & 0x02) != 0)
                {
                    strFlags += "SYN, ";
                }
                if ((nFlags & 0x04) != 0)
                {
                    strFlags += "RST, ";
                }
                if ((nFlags & 0x08) != 0)
                {
                    strFlags += "PSH, ";
                }
                if ((nFlags & 0x10) != 0)
                {
                    strFlags += "ACK, ";
                }
                if ((nFlags & 0x20) != 0)
                {
                    strFlags += "URG";
                }
                strFlags += ")";

                if (strFlags.Contains("()"))
                {
                    strFlags = strFlags.Remove(strFlags.Length - 3);
                }
                else if (strFlags.Contains(", )"))
                {
                    strFlags = strFlags.Remove(strFlags.Length - 3, 2);
                }

                return strFlags;
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
                return byTCPData;
            }
        }

        public ushort MessageLength
        {
            get
            {
                return usMessageLength;
            }
        }

        #region Statics

        public static TcpHeader Create(Reactor.Divert.Parsers.IpHeader header)
        {
            return new TcpHeader(header.Data);
        }

        #endregion
    }
}